using System;

namespace ZeroHour.Sim
{
    /// <summary>
    /// Deterministic pseudo-random generator (xorshift128+ seeded through SplitMix64).
    /// <para>
    /// <see cref="System.Random"/> is banned inside the simulation: its algorithm is not
    /// contractually stable across runtimes, and an unseeded instance is time-dependent.
    /// This generator produces the same sequence forever, on every platform, which is what
    /// lets the server re-simulate a client's battle and expect an identical result.
    /// </para>
    /// <para>
    /// The state is two <see cref="ulong"/> values, so it can be serialised into a battle
    /// replay and resumed exactly (docs/05, docs/11).
    /// </para>
    /// </summary>
    public struct DetRandom : IEquatable<DetRandom>
    {
        private ulong _state0;
        private ulong _state1;

        /// <summary>
        /// Creates a generator from a seed. The same seed always yields the same sequence.
        /// </summary>
        /// <param name="seed">Any value; 0 is fine and is remapped internally.</param>
        public DetRandom(ulong seed)
        {
            // SplitMix64 expands a single seed into well-distributed state, so that
            // sequential seeds (1, 2, 3...) do not produce correlated streams.
            ulong z = seed + 0x9E3779B97F4A7C15UL;
            _state0 = Mix(ref z);
            _state1 = Mix(ref z);

            if (_state0 == 0UL && _state1 == 0UL)
            {
                _state1 = 0x9E3779B97F4A7C15UL;
            }
        }

        private static ulong Mix(ref ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            ulong result = z;
            result = (result ^ (result >> 30)) * 0xBF58476D1CE4E5B9UL;
            result = (result ^ (result >> 27)) * 0x94D049BB133111EBUL;
            return result ^ (result >> 31);
        }

        /// <summary>Restores a generator from previously captured state.</summary>
        /// <param name="state0">First state word.</param>
        /// <param name="state1">Second state word.</param>
        public static DetRandom FromState(ulong state0, ulong state1)
        {
            DetRandom r = default;
            r._state0 = state0;
            r._state1 = state1;
            if (r._state0 == 0UL && r._state1 == 0UL)
            {
                r._state1 = 0x9E3779B97F4A7C15UL;
            }

            return r;
        }

        /// <summary>First state word; persist this to resume the stream later.</summary>
        public ulong State0 => _state0;

        /// <summary>Second state word; persist this to resume the stream later.</summary>
        public ulong State1 => _state1;

        /// <summary>Advances the stream and returns the next 64 raw bits.</summary>
        public ulong NextULong()
        {
            ulong s1 = _state0;
            ulong s0 = _state1;
            ulong result = s0 + s1;

            _state0 = s0;
            s1 ^= s1 << 23;
            _state1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);

            return result;
        }

        /// <summary>Returns a non-negative value below <see cref="int.MaxValue"/>.</summary>
        public int NextInt() => (int)(NextULong() >> 33);

        /// <summary>
        /// Returns a value in <c>[minInclusive, maxExclusive)</c> with no modulo bias.
        /// </summary>
        /// <param name="minInclusive">Lower bound, included.</param>
        /// <param name="maxExclusive">Upper bound, excluded; must exceed the lower bound.</param>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    "DetRandom.Range requires maxExclusive > minInclusive.");
            }

            ulong span = (ulong)((long)maxExclusive - minInclusive);

            // Rejection sampling removes the bias a plain modulo would introduce.
            // The loop almost always exits on the first iteration.
            ulong limit = ulong.MaxValue - (ulong.MaxValue % span) - 1UL;
            ulong value;
            do
            {
                value = NextULong();
            }
            while (value > limit);

            return (int)((long)minInclusive + (long)(value % span));
        }

        /// <summary>Returns a fixed-point value in <c>[0, 1)</c>.</summary>
        public Fixed NextFixed01() => Fixed.FromRaw((long)(NextULong() >> 32));

        /// <summary>
        /// Returns a fixed-point value in <c>[min, max)</c>.
        /// </summary>
        /// <param name="min">Lower bound, included.</param>
        /// <param name="max">Upper bound, excluded.</param>
        public Fixed RangeFixed(Fixed min, Fixed max) => min + ((max - min) * NextFixed01());

        /// <summary>
        /// Rolls against a probability in <c>[0, 1]</c>. Used for drop tables and gacha,
        /// so it must stay exactly reproducible for audit (docs/05, docs/22).
        /// </summary>
        /// <param name="probability">Chance of returning true.</param>
        public bool Chance(Fixed probability) => NextFixed01() < probability;

        /// <summary>
        /// Shuffles in place with Fisher-Yates. Deterministic given the generator state,
        /// unlike any sort that relies on hash ordering.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="items">The array to shuffle.</param>
        public void Shuffle<T>(T[] items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }

        /// <inheritdoc />
        public bool Equals(DetRandom other) => _state0 == other._state0 && _state1 == other._state1;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is DetRandom other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => (int)(_state0 ^ (_state0 >> 32) ^ _state1 ^ (_state1 >> 32));

        /// <inheritdoc />
        public override string ToString() => "DetRandom(" + _state0.ToString("X16") + ", " + _state1.ToString("X16") + ")";
    }
}
