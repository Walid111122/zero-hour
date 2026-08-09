using System;
using Xunit;
using ZeroHour.Sim;
using ZeroHour.Sim.Runner;

namespace ZeroHour.Sim.Tests.Runner
{
    /// <summary>
    /// Gate resolution tests (docs/06 §4, phase-1 §1.1).
    /// <para>
    /// Gates are the heart of the runner, so the arithmetic gets checked directly rather than
    /// inferred from a full stage simulation. A wrong gate is a wrong game.
    /// </para>
    /// </summary>
    public class GateTests
    {
        private static Squad Ten => Squad.Start(10, TroopType.Tank);

        [Fact]
        public void Add_Increases_Count()
        {
            Assert.Equal(22, Gate.Add(12).Apply(Ten).Count);
        }

        [Fact]
        public void Multiply_Scales_Count()
        {
            Assert.Equal(20, Gate.Multiply(Fixed.FromInt(2)).Apply(Ten).Count);
            Assert.Equal(30, Gate.Multiply(Fixed.FromInt(3)).Apply(Ten).Count);
        }

        [Fact]
        public void Multiply_By_Fraction_Floors_Rather_Than_Rounding()
        {
            // x1.5 on 15 soldiers is 22.5. Fractional soldiers cannot be drawn, so the sim
            // floors. Flooring (not rounding) keeps the direction of the error predictable.
            Squad fifteen = Squad.Start(15, TroopType.Tank);
            Assert.Equal(22, Gate.Multiply(Fixed.FromFraction(15, 10)).Apply(fifteen).Count);
        }

        [Fact]
        public void Subtract_Reduces_Count()
        {
            Assert.Equal(4, Gate.Subtract(6).Apply(Ten).Count);
        }

        [Fact]
        public void Divide_Floors_Count()
        {
            Assert.Equal(5, Gate.Divide(Fixed.FromInt(2)).Apply(Ten).Count);
            Assert.Equal(3, Gate.Divide(Fixed.FromInt(3)).Apply(Ten).Count);
        }

        [Fact]
        public void Gate_Never_Reduces_Squad_Below_One()
        {
            // phase-1 §1.1: "Gate math never produces a negative or zero squad below 1."
            // A gate is a choice; only combat may end the run.
            Assert.Equal(Squad.MinCount, Gate.Subtract(999).Apply(Ten).Count);
            Assert.Equal(Squad.MinCount, Gate.Subtract(10).Apply(Ten).Count);
            Assert.Equal(Squad.MinCount, Gate.Divide(Fixed.FromInt(1000)).Apply(Ten).Count);

            Squad one = Squad.Start(1, TroopType.Air);
            Assert.Equal(Squad.MinCount, Gate.Divide(Fixed.FromInt(2)).Apply(one).Count);
        }

        [Fact]
        public void Multiply_Then_Divide_By_Same_Integer_Returns_Original()
        {
            // phase-1 §1.1: "xN then ÷N returns the original count."
            // True for integer factors on any count, since xN can never lose precision.
            foreach (int n in new[] { 2, 3, 4 })
            {
                foreach (int start in new[] { 1, 7, 10, 43, 500 })
                {
                    Squad squad = Squad.Start(start, TroopType.Missile);
                    Squad round = Gate.Divide(Fixed.FromInt(n)).Apply(
                        Gate.Multiply(Fixed.FromInt(n)).Apply(squad));

                    Assert.Equal(start, round.Count);
                }
            }
        }

        [Fact]
        public void Multiply_Then_Divide_By_Mismatched_Factors_Loses_The_Remainder()
        {
            // The honest counterpart to the test above: the round trip only holds when the
            // intermediate divides evenly. x3 then ÷2 on 7 gives 21 -> 10, not 10.5. This is
            // asserted so the behaviour is a decision on record rather than a latent surprise.
            Squad seven = Squad.Start(7, TroopType.Tank);
            Squad result = Gate.Divide(Fixed.FromInt(2)).Apply(
                Gate.Multiply(Fixed.FromInt(3)).Apply(seven));

            Assert.Equal(10, result.Count);
        }

        [Fact]
        public void WeaponUp_Raises_Tier_And_Leaves_Count_Alone()
        {
            Squad result = Gate.WeaponUp().Apply(Ten);

            Assert.Equal(2, result.WeaponTier);
            Assert.Equal(10, result.Count);
        }

        [Fact]
        public void TypeSwap_Changes_Type_And_Leaves_Count_Alone()
        {
            Squad result = Gate.TypeSwap(TroopType.Missile).Apply(Ten);

            Assert.Equal(TroopType.Missile, result.Type);
            Assert.Equal(10, result.Count);
        }

        [Fact]
        public void Shield_Gates_Stack_Rather_Than_Replace()
        {
            Squad once = Gate.Shield(3000).Apply(Ten);
            Squad twice = Gate.Shield(3000).Apply(once);

            Assert.True(once.IsShielded);
            Assert.Equal(3000, once.ShieldMs);
            Assert.Equal(6000, twice.ShieldMs);
        }

        [Fact]
        public void Apply_Does_Not_Mutate_The_Input_Squad()
        {
            Squad original = Ten;
            Gate.Add(50).Apply(original);

            Assert.Equal(10, original.Count);
        }

        [Fact]
        public void Malformed_Gates_Are_Rejected_At_Construction()
        {
            // Failing here beats producing a silently nonsensical squad mid-run.
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Add(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Subtract(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Multiply(Fixed.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Multiply(Fixed.MinusOne));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Divide(Fixed.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.WeaponUp(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.Shield(0));
        }

        [Fact]
        public void BreakEven_Matches_The_Design_Worked_Example()
        {
            // docs/06 §4.3 states x2 vs +40 breaks even at 40 soldiers.
            Fixed breakEven = Gate.BreakEvenSize(Fixed.FromInt(2), 40);

            Assert.Equal(40, breakEven.ToInt());
        }

        [Fact]
        public void BreakEven_Is_The_Point_Where_Both_Gates_Agree()
        {
            // The formula is only useful if it actually predicts gate outcomes, so this
            // verifies it against Apply rather than restating the algebra.
            Fixed multiplier = Fixed.FromInt(2);
            const int added = 40;

            int atBreakEven = Gate.BreakEvenSize(multiplier, added).ToInt();
            Squad at = Squad.Start(atBreakEven, TroopType.Tank);
            Assert.Equal(
                Gate.Add(added).Apply(at).Count,
                Gate.Multiply(multiplier).Apply(at).Count);

            // Below it, adding wins; above it, multiplying wins.
            Squad below = Squad.Start(atBreakEven - 20, TroopType.Tank);
            Assert.True(Gate.Add(added).Apply(below).Count > Gate.Multiply(multiplier).Apply(below).Count);

            Squad above = Squad.Start(atBreakEven + 20, TroopType.Tank);
            Assert.True(Gate.Multiply(multiplier).Apply(above).Count > Gate.Add(added).Apply(above).Count);
        }

        [Fact]
        public void BreakEven_Rejects_Multipliers_That_Make_The_Pair_Pointless()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.BreakEvenSize(Fixed.One, 40));
            Assert.Throws<ArgumentOutOfRangeException>(() => Gate.BreakEvenSize(Fixed.Half, 40));
        }

        [Fact]
        public void Same_Gate_Sequence_Produces_The_Same_State_Hash()
        {
            // The determinism property the whole sim exists to protect, at gate scope.
            Gate[] sequence =
            {
                Gate.Add(12),
                Gate.Multiply(Fixed.FromInt(2)),
                Gate.WeaponUp(),
                Gate.Subtract(7),
                Gate.TypeSwap(TroopType.Air),
                Gate.Divide(Fixed.FromInt(3)),
                Gate.Shield(2500),
            };

            ulong first = RunSequence(sequence);

            for (int run = 0; run < 100; run++)
            {
                Assert.Equal(first, RunSequence(sequence));
            }
        }

        private static ulong RunSequence(Gate[] gates)
        {
            Squad squad = Squad.Start(10, TroopType.Tank);

            foreach (Gate gate in gates)
            {
                squad = gate.Apply(squad);
            }

            return squad.Fold(Hash.Create()).Value;
        }
    }
}
