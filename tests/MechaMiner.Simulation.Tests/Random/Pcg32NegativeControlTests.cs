using System;
using System.Collections.Generic;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// The negative control: every golden and independence assertion in this package must fail
/// against a deliberately wrong implementation.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-016</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence and
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract.
/// </para>
/// <para>
/// A green golden test proves the numbers match. It does not prove the comparison could ever
/// have failed - a renderer that emitted the golden's own contents, or a comparison that
/// compared nothing, would also be green. This fixture runs the mutations a reader of doc 20 §
/// Authoritative random-number contract could plausibly make and requires each one to be
/// caught, so the other fifteen entries are known to be load-bearing.
/// </para>
/// <para>
/// The mutations are applied to <see cref="ReferenceVectorEngine"/>, never to production. A
/// negative control must not require a gate to be disabled or a production constant to be
/// edited, which doc 114 § Failure and retry policy forbids outright.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class Pcg32NegativeControlTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-005-016</c>. Each one-bit or one-step mutation breaks the
    /// vector it should break, and the unmutated reference still reproduces everything.
    /// </summary>
    [Test]
    public void GoldenAndIndependenceAssertionsFailAgainstOneBitMutations()
    {
        Expect.Multiple(() =>
        {
            // The control. If this fails, no conclusion below means anything.
            Assert.That(
                DivergentVectors(ReferenceVectorEngine.Canonical),
                Is.Empty,
                "the unmutated reference must reproduce every committed vector");

            // One-bit and one-step mutations of the generator core. Each must break the
            // initialization vector, which is what VER-SIM-005-001 and VER-SIM-005-003 compare.
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "LCG multiplier, bit 0 flipped",
                    LcgMultiplier = 6364136223846793005UL ^ 1UL,
                },
                RandomVectorRendering.StreamInitializationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "LCG multiplier, bit 63 flipped",
                    LcgMultiplier = 6364136223846793005UL ^ 0x8000000000000000UL,
                },
                RandomVectorRendering.StreamInitializationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "first output xorshift 18 -> 17",
                    FirstOutputXorShift = 17,
                },
                RandomVectorRendering.StreamInitializationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "second output xorshift 27 -> 26",
                    SecondOutputXorShift = 26,
                },
                RandomVectorRendering.StreamInitializationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "rotation from the top six bits instead of the top five",
                    RotationSelectorShift = 58,
                },
                RandomVectorRendering.StreamInitializationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "rotate left instead of right",
                    RotateLeftInsteadOfRight = true,
                },
                RandomVectorRendering.StreamInitializationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "output taken after the advance instead of before",
                    OutputAfterAdvance = true,
                },
                RandomVectorRendering.StreamInitializationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "increment's mandatory low bit dropped",
                    ForceEvenIncrement = true,
                },
                RandomVectorRendering.StreamInitializationGolden);

            // Mutations of the derivation chain. Each must break the derivation vector, which is
            // what VER-SIM-005-002 compares.
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "Mix second shift 27 -> 26",
                    MixSecondShift = 26,
                },
                RandomVectorRendering.SeedDerivationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "Mix first shift 30 -> 31",
                    MixFirstShift = 31,
                },
                RandomVectorRendering.SeedDerivationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "Mix third shift 31 -> 32",
                    MixThirdShift = 32,
                },
                RandomVectorRendering.SeedDerivationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "Mix first multiplier, bit 0 flipped",
                    MixFirstMultiplier = 0xBF58476D1CE4E5B9UL ^ 1UL,
                },
                RandomVectorRendering.SeedDerivationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "Mix gamma, bit 0 flipped",
                    MixGamma = 0x9E3779B97F4A7C15UL ^ 1UL,
                },
                RandomVectorRendering.SeedDerivationGolden);
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "schema-version multiplier, bit 0 flipped",
                    SchemaVersionMultiplier = 0xD1B54A32D192ED03UL ^ 1UL,
                },
                RandomVectorRendering.SeedDerivationGolden);

            // The substitution a future maintainer is most likely to make: plain modulo reduction
            // in place of rejection sampling. Doc 20 § Authoritative random-number contract forbids it, and the bounded vector catches
            // it - both in the results and in the consumed-draw counts.
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "modulo reduction instead of rejection sampling",
                    UseModuloInsteadOfRejection = true,
                },
                RandomVectorRendering.BoundedConversionGolden);

            // The 53-bit layout decision the golden header fixes.
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "second draw used as the high half of the 53-bit pair",
                    SecondDrawIsHighHalf = true,
                },
                RandomVectorRendering.UnitDoubleConversionGolden);

            // The zero-draw selection rule of doc 20 § Authoritative random-number contract.
            AssertBreaks(
                new ReferenceRandomConstants
                {
                    Description = "singleton selection consumes a draw",
                    SingletonSelectionDrawsAnIndex = true,
                },
                RandomVectorRendering.DegenerateSelectionGolden);

            // The independence gate: a shared-state implementation that leaves the family key out
            // of the derivation makes every family replay one sequence.
            ReferenceRandomConstants shared = new()
            {
                Description = "family key omitted from the derivation, so all families share a state",
                IgnoreFamilyKey = true,
            };
            AssertBreaks(shared, RandomVectorRendering.StreamIndependenceGolden);

            string sharedBody = RandomVectorRendering.StreamIndependenceBody(new ReferenceVectorEngine(shared));
            HashSet<string> firstOutputs = new(StringComparer.Ordinal);
            int duplicateRows = 0;
            foreach (string line in sharedBody.Split('\n'))
            {
                string[] fields = line.Split('\t');
                if (fields.Length < 6)
                {
                    continue;
                }

                if (!firstOutputs.Add(fields[5]))
                {
                    duplicateRows++;
                }
            }

            Assert.That(
                duplicateRows,
                Is.EqualTo(22),
                "with the family key omitted, 22 of the 23 families duplicate the first family's "
                    + "output; the committed vector has 23 distinct outputs, so this is exactly the "
                    + "collision VER-SIM-005-011 exists to catch");
        });
    }

    private static void AssertBreaks(ReferenceRandomConstants mutation, string expectedBrokenGolden)
    {
        IReadOnlyList<string> divergent = DivergentVectors(new ReferenceVectorEngine(mutation));
        Assert.That(
            divergent,
            Does.Contain(expectedBrokenGolden),
            "mutation \"" + mutation.Description + "\" must be caught by " + expectedBrokenGolden);
    }

    /// <summary>The golden vectors whose body this engine fails to reproduce.</summary>
    private static IReadOnlyList<string> DivergentVectors(IRandomVectorEngine engine)
    {
        List<string> divergent = new();
        (string Golden, Func<IRandomVectorEngine, string> Render)[] vectors =
        {
            (RandomVectorRendering.SeedDerivationGolden, RandomVectorRendering.SeedDerivationBody),
            (RandomVectorRendering.StreamInitializationGolden, RandomVectorRendering.StreamInitializationBody),
            (RandomVectorRendering.BoundedConversionGolden, RandomVectorRendering.BoundedConversionBody),
            (RandomVectorRendering.UnitDoubleConversionGolden, RandomVectorRendering.UnitDoubleConversionBody),
            (RandomVectorRendering.StreamIndependenceGolden, RandomVectorRendering.StreamIndependenceBody),
            (RandomVectorRendering.DegenerateSelectionGolden, RandomVectorRendering.DegenerateSelectionBody),
        };

        foreach ((string golden, Func<IRandomVectorEngine, string> render) in vectors)
        {
            string rendered = RandomGoldenFile.Normalize(render(engine));
            if (!string.Equals(rendered, RandomGoldenFile.Body(golden), StringComparison.Ordinal))
            {
                divergent.Add(golden);
            }
        }

        return divergent;
    }
}
