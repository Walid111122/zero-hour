using NUnit.Framework;
using ZeroHour.Sim;

namespace ZeroHour.Tests.EditMode
{
    /// <summary>
    /// Asserts that the ZeroHour.Sim.dll loaded by Unity behaves identically to the copy the
    /// server runs (`23 §3`).
    ///
    /// This is not a duplicate of the sim's own xUnit suite. That suite proves the *source* is
    /// correct when compiled by the .NET SDK. This one proves the *artefact sitting in
    /// Assets/Plugins* is that same code, running under Unity's Mono/IL2CPP toolchain — a copy
    /// step that can silently go stale, and a runtime that is genuinely different. The pinned
    /// values below are the same ones `/health/sim` returns on the server, so a divergence
    /// between client and server shows up here rather than as a desync in a live battle.
    /// </summary>
    public class SimDeterminismTests
    {
        [Test]
        public void FixedMultiply_MatchesPinnedRawValue()
        {
            // 21 in Q32.32 is 21 << 32. Asserting the raw integer rather than the decimal
            // rendering is the point: a formatting change could hide a real arithmetic bug.
            Fixed product = Fixed.FromInt(3) * Fixed.FromInt(7);

            Assert.AreEqual(90194313216L, product.Raw,
                "3 * 7 must be exactly 21 << 32 in Q32.32. A mismatch here means the DLL in "
                + "Assets/Plugins is not the build the server is running.");
        }

        [Test]
        public void DetRandom_SameSeed_ProducesSameSequence()
        {
            var a = new DetRandom(12345UL);
            var b = new DetRandom(12345UL);

            for (int i = 0; i < 32; i++)
            {
                Assert.AreEqual(a.NextULong(), b.NextULong(),
                    "Sequences diverged at draw " + i + " despite an identical seed.");
            }
        }

        [Test]
        public void DetRandom_DifferentSeeds_DoNotProduceSameSequence()
        {
            // Guards the opposite failure: a generator that ignores its seed would satisfy the
            // reproducibility test above perfectly while being useless.
            var a = new DetRandom(1UL);
            var b = new DetRandom(2UL);

            bool anyDifference = false;
            for (int i = 0; i < 8; i++)
            {
                if (a.NextULong() != b.NextULong())
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.IsTrue(anyDifference, "Different seeds produced an identical prefix.");
        }

        [Test]
        public void Hash_MatchesPinnedVector()
        {
            // If this value ever changes, every recorded determinism fixture must be
            // regenerated deliberately — so it changing by accident needs to fail loudly.
            Assert.AreEqual(0x2BC75A111F39F5D5UL, Hash.Of("a"));
        }
    }
}
