using System;
using Xunit;
using ZeroHour.Sim;

namespace ZeroHour.Sim.Tests
{
    /// <summary>
    /// Arithmetic correctness for the fixed-point type. Everything downstream — combat,
    /// economy, arena — is only as trustworthy as these operations.
    /// </summary>
    public class FixedTests
    {
        [Fact]
        public void FromInt_RoundTrips()
        {
            Assert.Equal(0, Fixed.FromInt(0).ToInt());
            Assert.Equal(1, Fixed.FromInt(1).ToInt());
            Assert.Equal(-1, Fixed.FromInt(-1).ToInt());
            Assert.Equal(1000000, Fixed.FromInt(1000000).ToInt());
            Assert.Equal(-1000000, Fixed.FromInt(-1000000).ToInt());
        }

        [Fact]
        public void Addition_And_Subtraction_Are_Exact()
        {
            Fixed a = Fixed.FromInt(7);
            Fixed b = Fixed.FromInt(5);

            Assert.Equal(Fixed.FromInt(12), a + b);
            Assert.Equal(Fixed.FromInt(2), a - b);
            Assert.Equal(Fixed.FromInt(-2), b - a);
        }

        [Fact]
        public void Multiplication_Of_Whole_Numbers_Is_Exact()
        {
            Assert.Equal(Fixed.FromInt(12), Fixed.FromInt(3) * Fixed.FromInt(4));
            Assert.Equal(Fixed.FromInt(-12), Fixed.FromInt(-3) * Fixed.FromInt(4));
            Assert.Equal(Fixed.FromInt(12), Fixed.FromInt(-3) * Fixed.FromInt(-4));
            Assert.Equal(Fixed.Zero, Fixed.FromInt(0) * Fixed.FromInt(99));
        }

        [Fact]
        public void Multiplication_By_Half_Halves()
        {
            Assert.Equal(Fixed.FromInt(50), Fixed.FromInt(100) * Fixed.Half);
            Assert.Equal(Fixed.Half, Fixed.One * Fixed.Half);
        }

        [Fact]
        public void Division_Of_Whole_Numbers_Is_Exact()
        {
            Assert.Equal(Fixed.FromInt(4), Fixed.FromInt(12) / Fixed.FromInt(3));
            Assert.Equal(Fixed.FromInt(-4), Fixed.FromInt(-12) / Fixed.FromInt(3));
            Assert.Equal(Fixed.FromInt(4), Fixed.FromInt(-12) / Fixed.FromInt(-3));
        }

        [Fact]
        public void Division_By_Zero_Throws()
        {
            Assert.Throws<DivideByZeroException>(() => Fixed.FromInt(1) / Fixed.Zero);
        }

        [Fact]
        public void Multiply_Then_Divide_Recovers_The_Original()
        {
            // The property that matters for gate math in the runner: x * n / n == x.
            for (int n = 2; n <= 12; n++)
            {
                Fixed x = Fixed.FromInt(9600);
                Fixed multiplier = Fixed.FromInt(n);
                Fixed round = (x * multiplier) / multiplier;
                Assert.Equal(x, round);
            }
        }

        [Fact]
        public void FromFraction_Is_Exact_For_Powers_Of_Two()
        {
            Assert.Equal(Fixed.Half, Fixed.FromFraction(1, 2));
            Assert.Equal(Fixed.FromFraction(1, 4) * Fixed.FromInt(4), Fixed.One);
            Assert.Equal(Fixed.FromFraction(3, 8) + Fixed.FromFraction(5, 8), Fixed.One);
        }

        [Fact]
        public void FromFraction_Handles_Large_Denominators_Without_Overflow()
        {
            // A denominator of 10^9 shifted left by 32 would overflow a long; the 128-bit
            // divide path is what keeps this correct.
            Fixed oneBillionth = Fixed.FromFraction(1, 1000000000L);
            Assert.True(oneBillionth > Fixed.Zero);
            Assert.True(oneBillionth < Fixed.FromFraction(1, 100000000L));
        }

        [Fact]
        public void FromFraction_Thirds_Sum_Close_To_One()
        {
            Fixed third = Fixed.FromFraction(1, 3);
            Fixed sum = third + third + third;

            // One third is not exactly representable; the residual must be within a few raw units.
            long delta = Math.Abs(Fixed.One.Raw - sum.Raw);
            Assert.True(delta <= 4, "Three thirds drifted by " + delta + " raw units.");
        }

        [Fact]
        public void Sqrt_Of_Perfect_Squares_Is_Exact()
        {
            Assert.Equal(Fixed.Zero, Fixed.Sqrt(Fixed.Zero));
            Assert.Equal(Fixed.One, Fixed.Sqrt(Fixed.One));
            Assert.Equal(Fixed.FromInt(2), Fixed.Sqrt(Fixed.FromInt(4)));
            Assert.Equal(Fixed.FromInt(12), Fixed.Sqrt(Fixed.FromInt(144)));
            Assert.Equal(Fixed.FromInt(1000), Fixed.Sqrt(Fixed.FromInt(1000000)));
        }

        [Fact]
        public void Sqrt_Squared_Returns_The_Input()
        {
            for (int i = 1; i <= 200; i++)
            {
                Fixed input = Fixed.FromInt(i);
                Fixed root = Fixed.Sqrt(input);
                Fixed squared = root * root;

                long delta = Math.Abs(squared.Raw - input.Raw);
                Assert.True(delta <= 1024, "sqrt(" + i + ")^2 drifted by " + delta + " raw units.");
            }
        }

        [Fact]
        public void Sqrt_Of_Negative_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Fixed.Sqrt(Fixed.FromInt(-1)));
        }

        [Fact]
        public void Comparisons_Order_Correctly()
        {
            Fixed small = Fixed.FromInt(-5);
            Fixed large = Fixed.FromInt(5);
            Fixed smallAgain = Fixed.FromInt(-5);
            Fixed largeAgain = Fixed.FromInt(5);

            Assert.True(small < large);
            Assert.True(large > small);
            Assert.True(small <= smallAgain);
            Assert.True(large >= largeAgain);
            Assert.True(small == smallAgain);
            Assert.False(small == large);
            Assert.True(small != large);
        }

        [Fact]
        public void Clamp_Constrains_To_Bounds()
        {
            Fixed min = Fixed.FromInt(10);
            Fixed max = Fixed.FromInt(20);

            Assert.Equal(min, Fixed.Clamp(Fixed.FromInt(5), min, max));
            Assert.Equal(max, Fixed.Clamp(Fixed.FromInt(25), min, max));
            Assert.Equal(Fixed.FromInt(15), Fixed.Clamp(Fixed.FromInt(15), min, max));
        }

        [Fact]
        public void Lerp_Clamps_And_Hits_Both_Ends()
        {
            Fixed a = Fixed.FromInt(0);
            Fixed b = Fixed.FromInt(100);

            Assert.Equal(a, Fixed.Lerp(a, b, Fixed.Zero));
            Assert.Equal(b, Fixed.Lerp(a, b, Fixed.One));
            Assert.Equal(Fixed.FromInt(50), Fixed.Lerp(a, b, Fixed.Half));
            Assert.Equal(a, Fixed.Lerp(a, b, Fixed.FromInt(-3)));
            Assert.Equal(b, Fixed.Lerp(a, b, Fixed.FromInt(3)));
        }

        [Fact]
        public void Rounding_Helpers_Behave()
        {
            Fixed value = Fixed.Parse("2.75");

            Assert.Equal(2, value.ToInt());
            Assert.Equal(2, value.FloorToInt());
            Assert.Equal(3, value.CeilToInt());
            Assert.Equal(3, value.RoundToInt());

            Fixed negative = Fixed.Parse("-2.75");
            Assert.Equal(-2, negative.ToInt());
            Assert.Equal(-3, negative.FloorToInt());
            Assert.Equal(-3, negative.RoundToInt());
        }

        [Fact]
        public void Parse_Reads_Decimal_Strings()
        {
            Assert.Equal(Fixed.FromInt(5), Fixed.Parse("5"));
            Assert.Equal(Fixed.Half, Fixed.Parse("0.5"));
            Assert.Equal(Fixed.FromInt(-3), Fixed.Parse("-3"));
            Assert.Equal(Fixed.FromFraction(1, 4), Fixed.Parse("0.25"));
            Assert.Equal(Fixed.Parse("1.5"), Fixed.One + Fixed.Half);
        }

        [Fact]
        public void Parse_Rejects_Garbage()
        {
            Assert.Throws<FormatException>(() => Fixed.Parse(""));
            Assert.Throws<FormatException>(() => Fixed.Parse("abc"));
            Assert.Throws<FormatException>(() => Fixed.Parse("1.2x"));
        }

        [Fact]
        public void ToString_Is_Readable()
        {
            Assert.Equal("0.500000", Fixed.Half.ToString());
            Assert.Equal("1.000000", Fixed.One.ToString());
            Assert.Equal("-1.000000", Fixed.MinusOne.ToString());
            Assert.Equal("2.750000", Fixed.Parse("2.75").ToString());
        }

        [Fact]
        public void Matches_Double_Reference_Within_Tolerance()
        {
            // Fixed must be *deterministic*, not identical to IEEE-754. This check only
            // confirms the arithmetic is mathematically right, using double as a reference.
            var cases = new (int a, int b)[] { (3, 7), (12, 5), (100, 3), (999, 41), (-17, 6) };

            foreach (var (a, b) in cases)
            {
                Fixed quotient = Fixed.FromInt(a) / Fixed.FromInt(b);
                double expected = (double)a / b;
                double actual = quotient.Raw / 4294967296.0;

                Assert.True(
                    Math.Abs(expected - actual) < 0.0000001,
                    a + "/" + b + " expected " + expected + " got " + actual);
            }
        }
    }
}
