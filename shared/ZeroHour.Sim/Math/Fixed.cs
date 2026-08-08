using System;
using System.Runtime.CompilerServices;

namespace ZeroHour.Sim
{
    /// <summary>
    /// Q32.32 signed fixed-point number backed by a single <see cref="long"/>.
    /// <para>
    /// Every arithmetic operation is pure integer math, so results are bit-identical on
    /// x86, ARM32 and ARM64, in Mono and IL2CPP, on client and server. This is the reason
    /// the simulation never uses <c>float</c> or <c>double</c>: IEEE-754 permits differences
    /// in intermediate precision and rounding between platforms, and a single such
    /// difference compounds into a full desync over a long battle.
    /// </para>
    /// <para>Range is roughly ±2.1 billion with ~2.3e-10 resolution.</para>
    /// </summary>
    public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
    {
        /// <summary>Number of fractional bits (32 for Q32.32).</summary>
        public const int FractionalBits = 32;

        private const long RawOne = 1L << FractionalBits;
        private const long FractionalMask = RawOne - 1L;

        /// <summary>The underlying raw integer. Serialise this, never a formatted string.</summary>
        public readonly long Raw;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Fixed(long raw) => Raw = raw;

        // ---------------------------------------------------------------- constants

        /// <summary>Zero.</summary>
        public static Fixed Zero => new Fixed(0L);

        /// <summary>One.</summary>
        public static Fixed One => new Fixed(RawOne);

        /// <summary>Negative one.</summary>
        public static Fixed MinusOne => new Fixed(-RawOne);

        /// <summary>One half.</summary>
        public static Fixed Half => new Fixed(RawOne >> 1);

        /// <summary>Smallest representable value.</summary>
        public static Fixed MinValue => new Fixed(long.MinValue);

        /// <summary>Largest representable value.</summary>
        public static Fixed MaxValue => new Fixed(long.MaxValue);

        /// <summary>Smallest positive step (1 raw unit).</summary>
        public static Fixed Epsilon => new Fixed(1L);

        // ---------------------------------------------------------------- factories

        /// <summary>Wraps a raw Q32.32 integer without conversion.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed FromRaw(long raw) => new Fixed(raw);

        /// <summary>Converts a whole number to fixed-point.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed FromInt(int value) => new Fixed((long)value << FractionalBits);

        /// <summary>
        /// Builds an exact fraction, e.g. <c>FromFraction(1, 3)</c> for one third.
        /// Prefer this over parsing decimal literals so balance data stays exact.
        /// </summary>
        /// <param name="numerator">Numerator.</param>
        /// <param name="denominator">Denominator; must not be zero.</param>
        public static Fixed FromFraction(long numerator, long denominator)
        {
            if (denominator == 0L)
            {
                throw new DivideByZeroException("Fixed.FromFraction denominator was zero.");
            }

            // Divide() already shifts the dividend left by 32 inside a 128-bit intermediate,
            // so feeding the plain integers in as raw values yields exactly num * 2^32 / den
            // without ever overflowing the denominator.
            return Divide(new Fixed(numerator), new Fixed(denominator));
        }

        /// <summary>
        /// Parses a decimal string such as <c>"1.75"</c> or <c>"-0.001"</c> using integer math only.
        /// Intended for loading balance CSVs at startup, not for hot paths.
        /// </summary>
        /// <param name="text">The value to parse.</param>
        public static Fixed Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("Fixed.Parse received an empty value.");
            }

            string s = text!.Trim();
            bool negative = s[0] == '-';
            if (negative || s[0] == '+')
            {
                s = s.Substring(1);
            }

            int dot = s.IndexOf('.');
            string wholePart = dot < 0 ? s : s.Substring(0, dot);
            string fracPart = dot < 0 ? string.Empty : s.Substring(dot + 1);

            long whole = 0L;
            for (int i = 0; i < wholePart.Length; i++)
            {
                char c = wholePart[i];
                if (c < '0' || c > '9')
                {
                    throw new FormatException("Fixed.Parse found a non-numeric character: " + text);
                }

                whole = (whole * 10L) + (c - '0');
            }

            // Accumulate the fraction as numerator/10^n, then convert with one division.
            long fracNumerator = 0L;
            long fracScale = 1L;
            for (int i = 0; i < fracPart.Length && i < 18; i++)
            {
                char c = fracPart[i];
                if (c < '0' || c > '9')
                {
                    throw new FormatException("Fixed.Parse found a non-numeric character: " + text);
                }

                fracNumerator = (fracNumerator * 10L) + (c - '0');
                fracScale *= 10L;
            }

            Fixed result = new Fixed(whole << FractionalBits);
            if (fracNumerator != 0L)
            {
                result += FromFraction(fracNumerator, fracScale);
            }

            return negative ? -result : result;
        }

        // ---------------------------------------------------------------- conversions

        /// <summary>Truncates toward zero.</summary>
        public int ToInt() => (int)(Raw / RawOne);

        /// <summary>Rounds toward negative infinity.</summary>
        public int FloorToInt() => (int)(Raw >> FractionalBits);

        /// <summary>Rounds toward positive infinity.</summary>
        public int CeilToInt() => (int)((Raw + FractionalMask) >> FractionalBits);

        /// <summary>Rounds half away from zero.</summary>
        public int RoundToInt()
        {
            long half = RawOne >> 1;
            long shifted = Raw >= 0L ? Raw + half : Raw - half;
            return (int)(shifted / RawOne);
        }

        /// <summary>The fractional part, always in [0, 1).</summary>
        public Fixed Fraction() => new Fixed(Raw & FractionalMask);

        // ---------------------------------------------------------------- operators

        /// <summary>Adds two values.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator +(Fixed a, Fixed b) => new Fixed(a.Raw + b.Raw);

        /// <summary>Subtracts one value from another.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator -(Fixed a, Fixed b) => new Fixed(a.Raw - b.Raw);

        /// <summary>Negates a value.</summary>
        /// <param name="a">The operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator -(Fixed a) => new Fixed(-a.Raw);

        /// <summary>Multiplies two values.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static Fixed operator *(Fixed a, Fixed b) => Multiply(a, b);

        /// <summary>Divides one value by another.</summary>
        /// <param name="a">Dividend.</param>
        /// <param name="b">Divisor.</param>
        public static Fixed operator /(Fixed a, Fixed b) => Divide(a, b);

        /// <summary>Multiplies by a whole number.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Fixed operator *(Fixed a, int b) => new Fixed(a.Raw * b);

        /// <summary>Divides by a whole number.</summary>
        /// <param name="a">Dividend.</param>
        /// <param name="b">Divisor.</param>
        public static Fixed operator /(Fixed a, int b)
        {
            if (b == 0)
            {
                throw new DivideByZeroException("Fixed division by zero.");
            }

            return new Fixed(a.Raw / b);
        }

        /// <summary>Tests equality.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static bool operator ==(Fixed a, Fixed b) => a.Raw == b.Raw;

        /// <summary>Tests inequality.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static bool operator !=(Fixed a, Fixed b) => a.Raw != b.Raw;

        /// <summary>Tests whether the left operand is smaller.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static bool operator <(Fixed a, Fixed b) => a.Raw < b.Raw;

        /// <summary>Tests whether the left operand is larger.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static bool operator >(Fixed a, Fixed b) => a.Raw > b.Raw;

        /// <summary>Tests whether the left operand is smaller or equal.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static bool operator <=(Fixed a, Fixed b) => a.Raw <= b.Raw;

        /// <summary>Tests whether the left operand is larger or equal.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static bool operator >=(Fixed a, Fixed b) => a.Raw >= b.Raw;

        /// <summary>Implicitly widens a whole number to fixed-point.</summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator Fixed(int value) => FromInt(value);

        // ---------------------------------------------------------------- math

        /// <summary>Absolute value.</summary>
        /// <param name="v">The operand.</param>
        public static Fixed Abs(Fixed v) => v.Raw < 0L ? new Fixed(-v.Raw) : v;

        /// <summary>Returns -1, 0 or 1.</summary>
        /// <param name="v">The operand.</param>
        public static int Sign(Fixed v) => v.Raw < 0L ? -1 : (v.Raw > 0L ? 1 : 0);

        /// <summary>Smaller of two values.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static Fixed Min(Fixed a, Fixed b) => a.Raw <= b.Raw ? a : b;

        /// <summary>Larger of two values.</summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static Fixed Max(Fixed a, Fixed b) => a.Raw >= b.Raw ? a : b;

        /// <summary>Constrains a value to an inclusive range.</summary>
        /// <param name="v">Value to clamp.</param>
        /// <param name="min">Lower bound.</param>
        /// <param name="max">Upper bound.</param>
        public static Fixed Clamp(Fixed v, Fixed min, Fixed max)
            => v.Raw < min.Raw ? min : (v.Raw > max.Raw ? max : v);

        /// <summary>Constrains a value to [0, 1].</summary>
        /// <param name="v">Value to clamp.</param>
        public static Fixed Clamp01(Fixed v) => Clamp(v, Zero, One);

        /// <summary>
        /// Linear interpolation with <paramref name="t"/> clamped to [0, 1].
        /// </summary>
        /// <param name="a">Value at t = 0.</param>
        /// <param name="b">Value at t = 1.</param>
        /// <param name="t">Interpolation factor.</param>
        public static Fixed Lerp(Fixed a, Fixed b, Fixed t)
        {
            Fixed k = Clamp01(t);
            return a + Multiply(b - a, k);
        }

        /// <summary>
        /// Multiplies two values using a 128-bit intermediate so no precision is lost
        /// before the shift back down to Q32.32.
        /// </summary>
        /// <param name="a">Left operand.</param>
        /// <param name="b">Right operand.</param>
        public static Fixed Multiply(Fixed a, Fixed b)
        {
            bool negative = (a.Raw < 0L) ^ (b.Raw < 0L);
            ulong ua = AbsToUnsigned(a.Raw);
            ulong ub = AbsToUnsigned(b.Raw);

            MultiplyUnsigned(ua, ub, out ulong high, out ulong low);

            // (high:low) >> 32, keeping the low 64 bits.
            ulong shifted = (high << (64 - FractionalBits)) | (low >> FractionalBits);
            long result = (long)shifted;
            return new Fixed(negative ? -result : result);
        }

        /// <summary>
        /// Divides using a 128-bit numerator so small quotients keep full fractional precision.
        /// </summary>
        /// <param name="a">Dividend.</param>
        /// <param name="b">Divisor.</param>
        public static Fixed Divide(Fixed a, Fixed b)
        {
            if (b.Raw == 0L)
            {
                throw new DivideByZeroException("Fixed division by zero.");
            }

            bool negative = (a.Raw < 0L) ^ (b.Raw < 0L);
            ulong ua = AbsToUnsigned(a.Raw);
            ulong ub = AbsToUnsigned(b.Raw);

            // numerator = ua << 32, as a 128-bit value
            ulong high = ua >> (64 - FractionalBits);
            ulong low = ua << FractionalBits;

            ulong quotient = DivideUnsigned128(high, low, ub);
            long result = (long)quotient;
            return new Fixed(negative ? -result : result);
        }

        /// <summary>
        /// Square root by binary search on the result. Exact to one raw unit and fully
        /// deterministic; never uses a hardware square-root instruction.
        /// </summary>
        /// <param name="v">Value to take the root of; must not be negative.</param>
        public static Fixed Sqrt(Fixed v)
        {
            if (v.Raw < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(v), "Fixed.Sqrt of a negative value.");
            }

            if (v.Raw == 0L)
            {
                return Zero;
            }

            // Result cannot exceed 2^48 raw units for any representable input.
            ulong low = 0UL;
            ulong high = 1UL << 48;

            while (low < high)
            {
                ulong mid = low + ((high - low + 1UL) >> 1);
                MultiplyUnsigned(mid, mid, out ulong sqHigh, out ulong sqLow);
                ulong squared = (sqHigh << (64 - FractionalBits)) | (sqLow >> FractionalBits);

                if (squared <= (ulong)v.Raw)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1UL;
                }
            }

            return new Fixed((long)low);
        }

        // ---------------------------------------------------------------- 128-bit helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong AbsToUnsigned(long value)
            => value < 0L ? unchecked((ulong)(-value)) : (ulong)value;

        /// <summary>Unsigned 64x64 to 128-bit multiply, portable to every runtime.</summary>
        private static void MultiplyUnsigned(ulong a, ulong b, out ulong high, out ulong low)
        {
            ulong aLow = a & 0xFFFFFFFFUL;
            ulong aHigh = a >> 32;
            ulong bLow = b & 0xFFFFFFFFUL;
            ulong bHigh = b >> 32;

            ulong lowLow = aLow * bLow;
            ulong crossA = aLow * bHigh;
            ulong crossB = aHigh * bLow;
            ulong highHigh = aHigh * bHigh;

            ulong middle = crossA + crossB;
            ulong middleCarry = middle < crossA ? 1UL << 32 : 0UL;

            ulong lowResult = lowLow + (middle << 32);
            ulong lowCarry = lowResult < lowLow ? 1UL : 0UL;

            low = lowResult;
            high = highHigh + (middle >> 32) + middleCarry + lowCarry;
        }

        /// <summary>
        /// Restoring 128 / 64 division. Slower than a hardware divide but identical everywhere,
        /// which is the only property that matters here.
        /// </summary>
        private static ulong DivideUnsigned128(ulong high, ulong low, ulong divisor)
        {
            ulong quotient = 0UL;
            ulong remainder = 0UL;

            for (int bit = 127; bit >= 0; bit--)
            {
                ulong nextBit = bit >= 64
                    ? (high >> (bit - 64)) & 1UL
                    : (low >> bit) & 1UL;

                remainder = (remainder << 1) | nextBit;

                if (remainder >= divisor)
                {
                    remainder -= divisor;
                    if (bit < 64)
                    {
                        quotient |= 1UL << bit;
                    }
                }
            }

            return quotient;
        }

        // ---------------------------------------------------------------- equality & text

        /// <inheritdoc />
        public bool Equals(Fixed other) => Raw == other.Raw;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is Fixed other && Raw == other.Raw;

        /// <inheritdoc />
        public override int GetHashCode() => Raw.GetHashCode();

        /// <inheritdoc />
        public int CompareTo(Fixed other) => Raw.CompareTo(other.Raw);

        /// <summary>
        /// Human-readable decimal form with six fractional digits. Debug and log use only;
        /// persist <see cref="Raw"/> instead so no precision is lost.
        /// </summary>
        public override string ToString()
        {
            bool negative = Raw < 0L;
            ulong magnitude = AbsToUnsigned(Raw);

            ulong whole = magnitude >> FractionalBits;
            ulong fractionRaw = magnitude & (ulong)FractionalMask;
            ulong sixDigits = (fractionRaw * 1000000UL) >> FractionalBits;

            string sign = negative ? "-" : string.Empty;
            string digits = sixDigits.ToString(System.Globalization.CultureInfo.InvariantCulture)
                                     .PadLeft(6, '0');
            return sign + whole.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + digits;
        }
    }
}
