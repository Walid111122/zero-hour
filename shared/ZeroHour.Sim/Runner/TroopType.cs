using System;

namespace ZeroHour.Sim.Runner
{
    /// <summary>
    /// The three troop types that form the counter triangle (docs/05 §1).
    /// <para>
    /// Tank beats Air, Air beats Missile, Missile beats Tank. Three is the minimum for a
    /// meaningful triangle and the maximum a player will reason about mid-run.
    /// </para>
    /// </summary>
    public enum TroopType : byte
    {
        /// <summary>High HP, low damage, front line. Beats <see cref="Air"/>.</summary>
        Tank = 0,

        /// <summary>High damage, low HP, fast. Beats <see cref="Missile"/>.</summary>
        Air = 1,

        /// <summary>Long range, high burst, fragile. Beats <see cref="Tank"/>.</summary>
        Missile = 2,
    }

    /// <summary>
    /// The counter-triangle multipliers (docs/05 §1).
    /// <para>
    /// <b>These values are temporary.</b> docs/05 places them in
    /// <c>tools/balance/counter_triangle.csv</c>, hot-reloadable, and says in as many words
    /// "do not hardcode this" because they will be retuned repeatedly. They are inlined here
    /// only because the runner needs a working triangle before the data pipeline exists —
    /// that arrives with <c>generate_so</c> in Phase 2, at which point this class becomes the
    /// fallback for missing data rather than the source of truth.
    /// </para>
    /// </summary>
    public static class CounterTriangle
    {
        /// <summary>Damage multiplier when the attacker counters the defender (1.30).</summary>
        public static Fixed Advantage => Fixed.FromFraction(130, 100);

        /// <summary>Damage multiplier between types with no relationship (1.00).</summary>
        public static Fixed Neutral => Fixed.One;

        /// <summary>
        /// Damage multiplier when the attacker is countered (0.77).
        /// <para>Roughly 1/1.30, which is what keeps the triangle symmetric.</para>
        /// </summary>
        public static Fixed Disadvantage => Fixed.FromFraction(77, 100);

        /// <summary>
        /// Returns the damage multiplier for <paramref name="attacker"/> striking
        /// <paramref name="defender"/>.
        /// </summary>
        /// <param name="attacker">The attacking troop type.</param>
        /// <param name="defender">The defending troop type.</param>
        /// <returns>Advantage, neutral, or disadvantage.</returns>
        public static Fixed Multiplier(TroopType attacker, TroopType defender)
        {
            if (attacker == defender)
            {
                return Neutral;
            }

            bool counters =
                (attacker == TroopType.Tank && defender == TroopType.Air) ||
                (attacker == TroopType.Air && defender == TroopType.Missile) ||
                (attacker == TroopType.Missile && defender == TroopType.Tank);

            return counters ? Advantage : Disadvantage;
        }

        /// <summary>Returns the type this one is strong against.</summary>
        /// <param name="type">The attacking troop type.</param>
        /// <returns>The type it counters.</returns>
        public static TroopType Beats(TroopType type)
        {
            switch (type)
            {
                case TroopType.Tank: return TroopType.Air;
                case TroopType.Air: return TroopType.Missile;
                case TroopType.Missile: return TroopType.Tank;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown troop type.");
            }
        }

        /// <summary>Returns the type this one is weak to.</summary>
        /// <param name="type">The defending troop type.</param>
        /// <returns>The type that counters it.</returns>
        public static TroopType BeatenBy(TroopType type)
        {
            switch (type)
            {
                case TroopType.Tank: return TroopType.Missile;
                case TroopType.Air: return TroopType.Tank;
                case TroopType.Missile: return TroopType.Air;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown troop type.");
            }
        }
    }
}
