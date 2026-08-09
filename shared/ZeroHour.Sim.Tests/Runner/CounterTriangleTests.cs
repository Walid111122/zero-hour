using System;
using Xunit;
using ZeroHour.Sim;
using ZeroHour.Sim.Runner;

namespace ZeroHour.Sim.Tests.Runner
{
    /// <summary>
    /// Counter-triangle tests (docs/05 §1).
    /// <para>
    /// The design calls for a triangle that is symmetric, readable and tunable, and warns that
    /// no troop type may ever be strictly weaker — that would collapse the triangle and make a
    /// third of the roster dead content. These tests check those properties structurally, so
    /// the guarantee survives the retuning the doc promises is coming.
    /// </para>
    /// </summary>
    public class CounterTriangleTests
    {
        private static readonly TroopType[] AllTypes =
        {
            TroopType.Tank,
            TroopType.Air,
            TroopType.Missile,
        };

        [Fact]
        public void Triangle_Cycles_Tank_Air_Missile()
        {
            Assert.Equal(TroopType.Air, CounterTriangle.Beats(TroopType.Tank));
            Assert.Equal(TroopType.Missile, CounterTriangle.Beats(TroopType.Air));
            Assert.Equal(TroopType.Tank, CounterTriangle.Beats(TroopType.Missile));
        }

        [Fact]
        public void BeatenBy_Is_The_Inverse_Of_Beats()
        {
            foreach (TroopType type in AllTypes)
            {
                Assert.Equal(type, CounterTriangle.Beats(CounterTriangle.BeatenBy(type)));
                Assert.Equal(type, CounterTriangle.BeatenBy(CounterTriangle.Beats(type)));
            }
        }

        [Fact]
        public void Same_Type_Is_Neutral()
        {
            foreach (TroopType type in AllTypes)
            {
                Assert.Equal(CounterTriangle.Neutral, CounterTriangle.Multiplier(type, type));
            }
        }

        [Fact]
        public void Every_Type_Has_Exactly_One_Advantage_And_One_Disadvantage()
        {
            // This is the "no type is strictly weaker" guarantee from docs/05, checked as a
            // property of the table rather than as three hand-written cases. If a retune ever
            // makes one type counter two others, this fails.
            foreach (TroopType attacker in AllTypes)
            {
                int advantages = 0;
                int disadvantages = 0;

                foreach (TroopType defender in AllTypes)
                {
                    if (attacker == defender)
                    {
                        continue;
                    }

                    Fixed multiplier = CounterTriangle.Multiplier(attacker, defender);

                    if (multiplier == CounterTriangle.Advantage)
                    {
                        advantages++;
                    }
                    else if (multiplier == CounterTriangle.Disadvantage)
                    {
                        disadvantages++;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Unexpected neutral between " + attacker + " and " + defender + ".");
                    }
                }

                Assert.Equal(1, advantages);
                Assert.Equal(1, disadvantages);
            }
        }

        [Fact]
        public void Advantage_And_Disadvantage_Are_Symmetric()
        {
            // docs/05: disadvantage is ~1/advantage, so a matchup and its mirror multiply to
            // roughly 1. Exactly 1 is impossible with 1.30 and 0.77 at two decimal places, so
            // the tolerance is the rounding the design already accepted, not a fudge factor.
            Fixed product = CounterTriangle.Advantage * CounterTriangle.Disadvantage;
            Fixed drift = Fixed.Abs(product - Fixed.One);

            Assert.True(
                drift < Fixed.FromFraction(2, 100),
                "Triangle is asymmetric: 1.30 x 0.77 = " + product + ", drift " + drift);
        }

        [Fact]
        public void Countering_Beats_Neutral_Beats_Being_Countered()
        {
            Assert.True(CounterTriangle.Advantage > CounterTriangle.Neutral);
            Assert.True(CounterTriangle.Neutral > CounterTriangle.Disadvantage);
        }

        [Fact]
        public void A_TypeSwap_Gate_Can_Turn_A_Bad_Boss_Matchup_Into_A_Good_One()
        {
            // Why the type-swap gate exists (docs/06 §4.3): pairing "+30" against a swap when
            // the upcoming boss has a weakness is meant to reward knowing the triangle.
            const TroopType boss = TroopType.Air;
            Squad squad = Squad.Start(20, TroopType.Missile);

            Fixed before = CounterTriangle.Multiplier(squad.Type, boss);
            Assert.Equal(CounterTriangle.Disadvantage, before);

            Squad swapped = Gate.TypeSwap(CounterTriangle.BeatenBy(boss)).Apply(squad);
            Fixed after = CounterTriangle.Multiplier(swapped.Type, boss);

            Assert.Equal(CounterTriangle.Advantage, after);
            Assert.Equal(squad.Count, swapped.Count);
        }
    }
}
