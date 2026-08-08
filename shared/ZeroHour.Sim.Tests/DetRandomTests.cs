using System;
using System.Collections.Generic;
using Xunit;
using ZeroHour.Sim;

namespace ZeroHour.Sim.Tests
{
    /// <summary>
    /// Reproducibility and distribution checks for the deterministic RNG and the
    /// state-fingerprint hash. These two together are what make server re-simulation
    /// (docs/20 §4) and the 1,000-fixture combat suite (docs/23 §3) possible.
    /// </summary>
    public class DetRandomTests
    {
        [Fact]
        public void Same_Seed_Produces_Identical_Sequences()
        {
            var a = new DetRandom(12345UL);
            var b = new DetRandom(12345UL);

            for (int i = 0; i < 10000; i++)
            {
                Assert.Equal(a.NextULong(), b.NextULong());
            }
        }

        [Fact]
        public void Different_Seeds_Produce_Different_Sequences()
        {
            var a = new DetRandom(1UL);
            var b = new DetRandom(2UL);

            int identical = 0;
            for (int i = 0; i < 1000; i++)
            {
                if (a.NextULong() == b.NextULong())
                {
                    identical++;
                }
            }

            // Sequential seeds must not correlate; SplitMix64 expansion is what prevents it.
            Assert.True(identical < 5, "Seeds 1 and 2 produced " + identical + " identical draws.");
        }

        [Fact]
        public void State_Round_Trip_Resumes_The_Same_Stream()
        {
            var original = new DetRandom(999UL);
            for (int i = 0; i < 50; i++)
            {
                original.NextULong();
            }

            // Capture, then continue both — this is exactly how a replay resumes mid-battle.
            var resumed = DetRandom.FromState(original.State0, original.State1);

            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(original.NextULong(), resumed.NextULong());
            }
        }

        [Fact]
        public void Zero_Seed_Is_Valid_And_Not_Degenerate()
        {
            var r = new DetRandom(0UL);

            var seen = new HashSet<ulong>();
            for (int i = 0; i < 100; i++)
            {
                seen.Add(r.NextULong());
            }

            Assert.True(seen.Count > 90, "Seed 0 collapsed to a short cycle.");
        }

        [Fact]
        public void Range_Stays_Within_Bounds()
        {
            var r = new DetRandom(777UL);

            for (int i = 0; i < 20000; i++)
            {
                int value = r.Range(5, 15);
                Assert.InRange(value, 5, 14);
            }
        }

        [Fact]
        public void Range_Is_Reasonably_Uniform()
        {
            var r = new DetRandom(4242UL);
            var buckets = new int[10];
            const int draws = 100000;

            for (int i = 0; i < draws; i++)
            {
                buckets[r.Range(0, 10)]++;
            }

            int expected = draws / 10;
            for (int i = 0; i < buckets.Length; i++)
            {
                // Rejection sampling should keep every bucket within 5% of even.
                int drift = Math.Abs(buckets[i] - expected);
                Assert.True(
                    drift < expected / 20,
                    "Bucket " + i + " drifted " + drift + " from " + expected + ".");
            }
        }

        [Fact]
        public void Range_Rejects_An_Empty_Interval()
        {
            var r = new DetRandom(1UL);
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Range(5, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Range(5, 4));
        }

        [Fact]
        public void NextFixed01_Stays_In_Unit_Interval()
        {
            var r = new DetRandom(31337UL);

            for (int i = 0; i < 20000; i++)
            {
                Fixed value = r.NextFixed01();
                Assert.True(value >= Fixed.Zero, "Value below zero: " + value);
                Assert.True(value < Fixed.One, "Value at or above one: " + value);
            }
        }

        [Fact]
        public void Chance_Approximates_The_Requested_Probability()
        {
            // This is the primitive behind gacha rates, so the published number has to be true.
            var r = new DetRandom(20260808UL);
            Fixed oneInTwenty = Fixed.FromFraction(5, 100);

            int hits = 0;
            const int trials = 200000;
            for (int i = 0; i < trials; i++)
            {
                if (r.Chance(oneInTwenty))
                {
                    hits++;
                }
            }

            double observed = (double)hits / trials;
            Assert.True(
                Math.Abs(observed - 0.05) < 0.003,
                "5% chance observed at " + observed.ToString("P3") + " over " + trials + " trials.");
        }

        [Fact]
        public void Shuffle_Is_A_Permutation_And_Reproducible()
        {
            int[] BuildDeck()
            {
                var deck = new int[52];
                for (int i = 0; i < deck.Length; i++)
                {
                    deck[i] = i;
                }

                return deck;
            }

            int[] first = BuildDeck();
            int[] second = BuildDeck();

            new DetRandom(5UL).Shuffle(first);
            new DetRandom(5UL).Shuffle(second);

            Assert.Equal(first, second);

            Array.Sort(first);
            Assert.Equal(BuildDeck(), first);
        }

        [Fact]
        public void Hash_Is_Stable_Across_Instances()
        {
            ulong first = Hash.Create().Add(42).Add(Fixed.Half).Add("squad_alpha").Value;
            ulong second = Hash.Create().Add(42).Add(Fixed.Half).Add("squad_alpha").Value;

            Assert.Equal(first, second);
        }

        [Fact]
        public void Hash_Detects_Order_And_Value_Changes()
        {
            ulong baseline = Hash.Create().Add(1).Add(2).Value;

            Assert.NotEqual(baseline, Hash.Create().Add(2).Add(1).Value);
            Assert.NotEqual(baseline, Hash.Create().Add(1).Add(3).Value);
            Assert.NotEqual(baseline, Hash.Create().Add(1).Value);
        }

        [Fact]
        public void Hash_Of_Known_Input_Never_Changes()
        {
            // Pinned so a future refactor of Hash cannot silently invalidate every stored
            // determinism fixture. If this fails, the hash algorithm changed and every
            // recorded fixture hash must be regenerated deliberately.
            //
            // Note this is NOT the textbook FNV-1a of the ASCII byte 'a'. Add(string)
            // length-prefixes and folds UTF-16 code units, which is what stops "ab" + "c"
            // from colliding with "a" + "bc" when several fields feed one fingerprint.
            Assert.Equal(0x2BC75A111F39F5D5UL, Hash.Of("a"));
        }

        [Fact]
        public void Hash_Is_Not_Vulnerable_To_Field_Boundary_Collisions()
        {
            // Two different field splits that concatenate to the same character stream
            // must not produce the same fingerprint.
            ulong split1 = Hash.Create().Add("ab").Add("c").Value;
            ulong split2 = Hash.Create().Add("a").Add("bc").Value;

            Assert.NotEqual(split1, split2);
        }

        [Fact]
        public void SeedFromId_Distinguishes_Content_Ids()
        {
            Assert.NotEqual(Hash.SeedFromId("stage_001"), Hash.SeedFromId("stage_002"));
            Assert.Equal(Hash.SeedFromId("stage_001"), Hash.SeedFromId("stage_001"));
        }
    }
}
