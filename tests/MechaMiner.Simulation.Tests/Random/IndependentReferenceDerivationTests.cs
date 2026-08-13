using System;
using System.Collections.Generic;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Every committed vector, reproduced three ways: the committed golden, the production types,
/// and a second implementation written from doc 20 alone.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-004</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/91-verification-strategy.md</c> § Reference models and
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract.
/// Fixtures: all six vector files under
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/</c>.
/// </para>
/// <para>
/// This is the entry that keeps the goldens honest. A vector regenerated from the
/// implementation it is meant to constrain proves only self-consistency: a wrong shift would be
/// baked into the golden and every downstream consumer - map generation, encounters, combat,
/// progression - would agree with the bug undetectably, because all of them inherit these
/// numbers. The committed vectors were produced by a pure-Python reference before any C#
/// existed; <see cref="ReferenceVectorEngine"/> is a third derivation in C#, and the gate fails
/// if any one of the three changes alone.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class IndependentReferenceDerivationTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-005-004</c>. Golden, production, and independent reference
    /// agree on all six vector files.
    /// </summary>
    [Test]
    public void GoldenVectorsAreReproducedByAnIndependentImplementation()
    {
        // The bounded-conversion vector's own golden-text comparison lives here, because
        // VER-SIM-005-005 is registered as an assertion about rejection sampling rather than
        // as a golden comparison. Every other vector file is compared as whole text by the
        // entry that owns it.
        GoldenText.Matches(
            RandomVectorRendering.BoundedConversionGolden,
            RandomGoldenHeaders.BoundedConversion
                + RandomVectorRendering.BoundedConversionBody(ProductionVectorEngine.Instance));

        Expect.Multiple(() =>
        {
            foreach (KeyValuePair<string, Func<IRandomVectorEngine, string>> vector in Vectors())
            {
                string committed = RandomGoldenFile.Body(vector.Key);
                string fromProduction = RandomGoldenFile.Normalize(
                    vector.Value(ProductionVectorEngine.Instance));
                string fromReference = RandomGoldenFile.Normalize(
                    vector.Value(ReferenceVectorEngine.Canonical));

                Assert.That(
                    fromReference,
                    Is.EqualTo(committed),
                    vector.Key + ": the independent reference must reproduce the committed vector");
                Assert.That(
                    fromProduction,
                    Is.EqualTo(fromReference),
                    vector.Key + ": production and the independent reference must agree");
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-004</c>. The reference really is a second implementation:
    /// it agrees on the vectors and disagrees the moment one of its own constants is wrong.
    /// </summary>
    /// <remarks>
    /// Without this, "an independent reference reproduces the vectors" could be satisfied by a
    /// reference that returns the golden's own contents, or by one that delegates to
    /// production. A mutated reference must fail, which shows the comparison has teeth; the
    /// full mutation battery is <c>VER-SIM-005-016</c>.
    /// </remarks>
    [Test]
    public void TheReferenceIsIndependentEnoughToDisagreeWhenMutated()
    {
        ReferenceVectorEngine mutated = new(new ReferenceRandomConstants
        {
            Description = "LCG multiplier with bit 0 flipped",
            LcgMultiplier = 6364136223846793005UL ^ 1UL,
        });

        Expect.Multiple(() =>
        {
            Assert.That(
                RandomGoldenFile.Normalize(RandomVectorRendering.StreamInitializationBody(mutated)),
                Is.Not.EqualTo(RandomGoldenFile.Body(RandomVectorRendering.StreamInitializationGolden)),
                "a one-bit multiplier change must not reproduce the committed vector");
            Assert.That(
                RandomGoldenFile.Normalize(
                    RandomVectorRendering.SeedDerivationBody(ReferenceVectorEngine.Canonical)),
                Is.EqualTo(RandomGoldenFile.Body(RandomVectorRendering.SeedDerivationGolden)),
                "the unmutated reference is still the control and must reproduce the vector");
        });
    }

    private static IReadOnlyList<KeyValuePair<string, Func<IRandomVectorEngine, string>>> Vectors()
    {
        return new List<KeyValuePair<string, Func<IRandomVectorEngine, string>>>
        {
            new(RandomVectorRendering.SeedDerivationGolden, RandomVectorRendering.SeedDerivationBody),
            new(
                RandomVectorRendering.StreamInitializationGolden,
                RandomVectorRendering.StreamInitializationBody),
            new(RandomVectorRendering.BoundedConversionGolden, RandomVectorRendering.BoundedConversionBody),
            new(
                RandomVectorRendering.UnitDoubleConversionGolden,
                RandomVectorRendering.UnitDoubleConversionBody),
            new(RandomVectorRendering.StreamIndependenceGolden, RandomVectorRendering.StreamIndependenceBody),
            new(
                RandomVectorRendering.DegenerateSelectionGolden,
                RandomVectorRendering.DegenerateSelectionBody),
        };
    }
}
