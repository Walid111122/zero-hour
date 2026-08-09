using System;

namespace ZeroHour.Sim.Runner
{
    /// <summary>
    /// The player's squad: the core number the whole runner is about (docs/06 §3).
    /// <para>
    /// Immutable — every mutation returns a new value. That keeps the sim a pure function of
    /// (state, input) and makes any mid-run state trivially snapshottable for replay.
    /// </para>
    /// </summary>
    public readonly struct Squad : IEquatable<Squad>
    {
        /// <summary>
        /// The floor a squad can be reduced to by a gate.
        /// <para>
        /// Gates clamp here rather than to zero: a gate is a <i>choice</i>, and a choice that
        /// ends the run before the boss is a trap rather than a decision. Combat is what kills
        /// the player (docs/06 §5), so only combat may take the count to zero.
        /// </para>
        /// </summary>
        public const int MinCount = 1;

        /// <summary>Number of soldiers. Never below <see cref="MinCount"/> after a gate.</summary>
        public readonly int Count;

        /// <summary>Weapon tier, driving damage rather than count. One-based.</summary>
        public readonly int WeaponTier;

        /// <summary>The squad's troop type, which the counter triangle applies to.</summary>
        public readonly TroopType Type;

        /// <summary>Remaining damage immunity in milliseconds. Integer, so it stays exact.</summary>
        public readonly int ShieldMs;

        /// <summary>Creates a squad, clamping every field into its legal range.</summary>
        /// <param name="count">Soldier count; clamped to at least <see cref="MinCount"/>.</param>
        /// <param name="weaponTier">Weapon tier; clamped to at least 1.</param>
        /// <param name="type">Troop type.</param>
        /// <param name="shieldMs">Remaining shield in milliseconds; clamped to at least 0.</param>
        public Squad(int count, int weaponTier, TroopType type, int shieldMs)
        {
            Count = count < MinCount ? MinCount : count;
            WeaponTier = weaponTier < 1 ? 1 : weaponTier;
            Type = type;
            ShieldMs = shieldMs < 0 ? 0 : shieldMs;
        }

        /// <summary>Creates a starting squad at weapon tier 1 with no shield.</summary>
        /// <param name="count">Starting soldier count.</param>
        /// <param name="type">Starting troop type.</param>
        /// <returns>A new squad.</returns>
        public static Squad Start(int count, TroopType type) => new Squad(count, 1, type, 0);

        /// <summary>True while damage immunity is active.</summary>
        public bool IsShielded => ShieldMs > 0;

        /// <summary>Returns a copy with a different soldier count.</summary>
        /// <param name="count">The new count.</param>
        /// <returns>The updated squad.</returns>
        public Squad WithCount(int count) => new Squad(count, WeaponTier, Type, ShieldMs);

        /// <summary>Returns a copy with a different weapon tier.</summary>
        /// <param name="weaponTier">The new tier.</param>
        /// <returns>The updated squad.</returns>
        public Squad WithWeaponTier(int weaponTier) => new Squad(Count, weaponTier, Type, ShieldMs);

        /// <summary>Returns a copy with a different troop type.</summary>
        /// <param name="type">The new type.</param>
        /// <returns>The updated squad.</returns>
        public Squad WithType(TroopType type) => new Squad(Count, WeaponTier, type, ShieldMs);

        /// <summary>Returns a copy with a different remaining shield.</summary>
        /// <param name="shieldMs">The new shield duration in milliseconds.</param>
        /// <returns>The updated squad.</returns>
        public Squad WithShieldMs(int shieldMs) => new Squad(Count, WeaponTier, Type, shieldMs);

        /// <summary>Folds the squad into a determinism fingerprint.</summary>
        /// <param name="hash">The accumulator to fold into.</param>
        /// <returns>The updated accumulator.</returns>
        public Hash Fold(Hash hash) =>
            hash.Add(Count).Add(WeaponTier).Add((int)Type).Add(ShieldMs);

        /// <inheritdoc />
        public bool Equals(Squad other) =>
            Count == other.Count &&
            WeaponTier == other.WeaponTier &&
            Type == other.Type &&
            ShieldMs == other.ShieldMs;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Squad other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (int)Fold(Hash.Create()).Value;

        /// <inheritdoc />
        public override string ToString() =>
            Count.ToString() + "x " + Type.ToString() + " T" + WeaponTier.ToString() +
            (IsShielded ? " shield " + ShieldMs.ToString() + "ms" : string.Empty);
    }
}
