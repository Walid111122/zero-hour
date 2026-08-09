using System;

namespace ZeroHour.Sim.Runner
{
    /// <summary>The operator a gate applies to the squad (docs/06 §4.2).</summary>
    public enum GateOp : byte
    {
        /// <summary>Add N soldiers. Green. The bread-and-butter gate.</summary>
        Add = 0,

        /// <summary>Multiply the squad by N. Gold. The dopamine hit.</summary>
        Multiply = 1,

        /// <summary>Remove N soldiers. Red. The bad option in a pair.</summary>
        Subtract = 2,

        /// <summary>Divide the squad by N. Dark red. Punishing.</summary>
        Divide = 3,

        /// <summary>Upgrade the weapon tier. Blue. Damage, not count.</summary>
        WeaponUp = 4,

        /// <summary>Switch troop type. Purple. Matters for the boss counter.</summary>
        TypeSwap = 5,

        /// <summary>Grant temporary damage immunity. Cyan. The rescue mechanic.</summary>
        Shield = 6,
    }

    /// <summary>
    /// A single gate: one operator plus its operand (docs/06 §4).
    /// <para>
    /// Gates are the heart of the game. They always appear in pairs, the player passes through
    /// exactly one, and the effect applies instantly.
    /// </para>
    /// <para>
    /// <b>Rounding rule:</b> <see cref="GateOp.Multiply"/> and <see cref="GateOp.Divide"/> both
    /// floor their result, so the count stays an exact integer with no accumulated drift. A
    /// consequence worth knowing: ×N followed by ÷N only returns the original count when the
    /// intermediate divides evenly. With 7 soldiers, ×2 then ÷2 gives 7, but ×3 then ÷2 gives
    /// 10 rather than 10.5. That is deliberate — the alternative is fractional soldiers, which
    /// cannot be drawn on screen.
    /// </para>
    /// </summary>
    public readonly struct Gate : IEquatable<Gate>
    {
        /// <summary>The operator this gate applies.</summary>
        public readonly GateOp Op;

        /// <summary>
        /// The operand, whose meaning depends on <see cref="Op"/>:
        /// soldier count for add/subtract, factor for multiply/divide, tier steps for
        /// weapon-up, and milliseconds for shield. Unused by type-swap.
        /// </summary>
        public readonly Fixed Value;

        /// <summary>The type a <see cref="GateOp.TypeSwap"/> switches to. Ignored otherwise.</summary>
        public readonly TroopType SwapTo;

        private Gate(GateOp op, Fixed value, TroopType swapTo)
        {
            Op = op;
            Value = value;
            SwapTo = swapTo;
        }

        /// <summary>Creates a <c>+N</c> gate.</summary>
        /// <param name="soldiers">Soldiers to add; must not be negative.</param>
        /// <returns>The gate.</returns>
        public static Gate Add(int soldiers)
        {
            if (soldiers < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(soldiers), soldiers, "An Add gate cannot be negative; use Subtract.");
            }

            return new Gate(GateOp.Add, Fixed.FromInt(soldiers), default);
        }

        /// <summary>Creates a <c>×N</c> gate.</summary>
        /// <param name="factor">Multiplier; must be positive.</param>
        /// <returns>The gate.</returns>
        public static Gate Multiply(Fixed factor)
        {
            if (factor <= Fixed.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(factor), factor.ToString(), "A Multiply gate must be positive.");
            }

            return new Gate(GateOp.Multiply, factor, default);
        }

        /// <summary>Creates a <c>−N</c> gate.</summary>
        /// <param name="soldiers">Soldiers to remove; must not be negative.</param>
        /// <returns>The gate.</returns>
        public static Gate Subtract(int soldiers)
        {
            if (soldiers < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(soldiers), soldiers, "A Subtract gate takes a positive magnitude.");
            }

            return new Gate(GateOp.Subtract, Fixed.FromInt(soldiers), default);
        }

        /// <summary>Creates a <c>÷N</c> gate.</summary>
        /// <param name="divisor">Divisor; must be greater than zero.</param>
        /// <returns>The gate.</returns>
        public static Gate Divide(Fixed divisor)
        {
            if (divisor <= Fixed.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(divisor), divisor.ToString(), "A Divide gate must be positive.");
            }

            return new Gate(GateOp.Divide, divisor, default);
        }

        /// <summary>Creates a weapon-upgrade gate.</summary>
        /// <param name="tiers">Tiers to gain; must be at least 1.</param>
        /// <returns>The gate.</returns>
        public static Gate WeaponUp(int tiers = 1)
        {
            if (tiers < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tiers), tiers, "A WeaponUp gate must grant at least one tier.");
            }

            return new Gate(GateOp.WeaponUp, Fixed.FromInt(tiers), default);
        }

        /// <summary>Creates a type-swap gate.</summary>
        /// <param name="to">The troop type to switch to.</param>
        /// <returns>The gate.</returns>
        public static Gate TypeSwap(TroopType to) => new Gate(GateOp.TypeSwap, Fixed.Zero, to);

        /// <summary>Creates a shield gate.</summary>
        /// <param name="durationMs">Immunity duration in milliseconds; must be positive.</param>
        /// <returns>The gate.</returns>
        public static Gate Shield(int durationMs)
        {
            if (durationMs <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(durationMs), durationMs, "A Shield gate must last a positive time.");
            }

            return new Gate(GateOp.Shield, Fixed.FromInt(durationMs), default);
        }

        /// <summary>
        /// Applies this gate to a squad and returns the result.
        /// <para>Pure: the input squad is unchanged.</para>
        /// </summary>
        /// <param name="squad">The squad passing through.</param>
        /// <returns>The squad after the gate.</returns>
        public Squad Apply(Squad squad)
        {
            switch (Op)
            {
                case GateOp.Add:
                    return squad.WithCount(squad.Count + Value.FloorToInt());

                case GateOp.Multiply:
                    return squad.WithCount((Fixed.FromInt(squad.Count) * Value).FloorToInt());

                case GateOp.Subtract:
                    return squad.WithCount(squad.Count - Value.FloorToInt());

                case GateOp.Divide:
                    return squad.WithCount((Fixed.FromInt(squad.Count) / Value).FloorToInt());

                case GateOp.WeaponUp:
                    return squad.WithWeaponTier(squad.WeaponTier + Value.FloorToInt());

                case GateOp.TypeSwap:
                    return squad.WithType(SwapTo);

                case GateOp.Shield:
                    // Shields extend rather than replace, so taking two in a row is never a
                    // downgrade — which is what a player intuitively expects from a pickup.
                    return squad.WithShieldMs(squad.ShieldMs + Value.FloorToInt());

                default:
                    throw new ArgumentOutOfRangeException(nameof(Op), Op, "Unknown gate operator.");
            }
        }

        /// <summary>
        /// The squad size at which <c>×m</c> and <c>+a</c> are worth exactly the same
        /// (docs/06 §4.3): <c>a / (m - 1)</c>.
        /// <para>
        /// The stage generator uses this to place a pair's break-even point near the player's
        /// expected squad size at that moment, which is where the decision is hardest and the
        /// game is most interesting. Below the break-even the additive gate wins; above it the
        /// multiplier does.
        /// </para>
        /// </summary>
        /// <param name="multiplier">The <c>×m</c> factor; must be greater than 1.</param>
        /// <param name="added">The <c>+a</c> soldier count.</param>
        /// <returns>The break-even squad size.</returns>
        public static Fixed BreakEvenSize(Fixed multiplier, int added)
        {
            if (multiplier <= Fixed.One)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(multiplier),
                    multiplier.ToString(),
                    "Break-even is only defined for a multiplier above 1; at or below 1 the " +
                    "additive gate always wins and the pair is not a real choice.");
            }

            return Fixed.FromInt(added) / (multiplier - Fixed.One);
        }

        /// <summary>Folds the gate into a determinism fingerprint.</summary>
        /// <param name="hash">The accumulator to fold into.</param>
        /// <returns>The updated accumulator.</returns>
        public Hash Fold(Hash hash) => hash.Add((int)Op).Add(Value).Add((int)SwapTo);

        /// <inheritdoc />
        public bool Equals(Gate other) =>
            Op == other.Op && Value == other.Value && SwapTo == other.SwapTo;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Gate other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (int)Fold(Hash.Create()).Value;

        /// <inheritdoc />
        public override string ToString()
        {
            switch (Op)
            {
                case GateOp.Add: return "+" + Value.FloorToInt().ToString();
                case GateOp.Multiply: return "x" + Value.ToString();
                case GateOp.Subtract: return "-" + Value.FloorToInt().ToString();
                case GateOp.Divide: return "/" + Value.ToString();
                case GateOp.WeaponUp: return "Weapon+" + Value.FloorToInt().ToString();
                case GateOp.TypeSwap: return "Type>" + SwapTo.ToString();
                case GateOp.Shield: return "Shield " + Value.FloorToInt().ToString() + "ms";
                default: return Op.ToString();
            }
        }
    }
}
