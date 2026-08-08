using System;

namespace ZeroHour.Sim
{
    /// <summary>
    /// Stable FNV-1a 64-bit hashing used to fingerprint simulation state.
    /// <para>
    /// <see cref="object.GetHashCode"/> is deliberately not used for this: .NET randomises
    /// string hash seeds per process, so it differs between two runs of the same binary.
    /// These functions are fixed forever, which is what makes a state fingerprint
    /// comparable between a player's device and the server (docs/23 determinism fixtures).
    /// </para>
    /// </summary>
    public struct Hash : IEquatable<Hash>
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        private ulong _value;

        /// <summary>Creates an accumulator primed with the FNV offset basis.</summary>
        public static Hash Create() => new Hash { _value = Offset };

        /// <summary>The accumulated fingerprint.</summary>
        public ulong Value => _value;

        /// <summary>Folds in a single byte.</summary>
        /// <param name="b">The byte to absorb.</param>
        public Hash Add(byte b)
        {
            _value = (_value ^ b) * Prime;
            return this;
        }

        /// <summary>Folds in a 32-bit integer, low byte first.</summary>
        /// <param name="value">The value to absorb.</param>
        public Hash Add(int value) => Add(unchecked((uint)value));

        /// <summary>Folds in an unsigned 32-bit integer, low byte first.</summary>
        /// <param name="value">The value to absorb.</param>
        public Hash Add(uint value)
        {
            for (int shift = 0; shift < 32; shift += 8)
            {
                _value = (_value ^ (byte)(value >> shift)) * Prime;
            }

            return this;
        }

        /// <summary>Folds in a 64-bit integer, low byte first.</summary>
        /// <param name="value">The value to absorb.</param>
        public Hash Add(long value) => Add(unchecked((ulong)value));

        /// <summary>Folds in an unsigned 64-bit integer, low byte first.</summary>
        /// <param name="value">The value to absorb.</param>
        public Hash Add(ulong value)
        {
            for (int shift = 0; shift < 64; shift += 8)
            {
                _value = (_value ^ (byte)(value >> shift)) * Prime;
            }

            return this;
        }

        /// <summary>Folds in a fixed-point value via its raw representation.</summary>
        /// <param name="value">The value to absorb.</param>
        public Hash Add(Fixed value) => Add(value.Raw);

        /// <summary>Folds in a boolean.</summary>
        /// <param name="value">The value to absorb.</param>
        public Hash Add(bool value) => Add(value ? (byte)1 : (byte)0);

        /// <summary>
        /// Folds in a string as UTF-16 code units. Ordinal and culture-independent, so a
        /// Turkish locale cannot change the result.
        /// </summary>
        /// <param name="value">The value to absorb; null is folded in as a distinct marker.</param>
        public Hash Add(string? value)
        {
            if (value == null)
            {
                return Add((byte)0xFF);
            }

            Add(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                Add((uint)value[i]);
            }

            return this;
        }

        /// <summary>Convenience one-shot hash of a 64-bit value.</summary>
        /// <param name="value">The value to hash.</param>
        public static ulong Of(long value) => Create().Add(value).Value;

        /// <summary>Convenience one-shot hash of a string.</summary>
        /// <param name="value">The value to hash.</param>
        public static ulong Of(string? value) => Create().Add(value).Value;

        /// <summary>
        /// Converts a stable string id into a seed, so content ids in CSV data can drive
        /// randomness without a lookup table.
        /// </summary>
        /// <param name="id">The content id, e.g. <c>"stage_012"</c>.</param>
        public static ulong SeedFromId(string id) => Of(id);

        /// <inheritdoc />
        public bool Equals(Hash other) => _value == other._value;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Hash other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (int)(_value ^ (_value >> 32));

        /// <summary>Hex form, used in test failure messages and desync reports.</summary>
        public override string ToString() => _value.ToString("X16");
    }
}
