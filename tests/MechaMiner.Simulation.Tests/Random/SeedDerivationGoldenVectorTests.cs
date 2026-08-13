using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// The four-step seed-derivation chain of doc 20 § Authoritative random-number contract against
/// its committed golden vector.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-002</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract (doc 20 § Authoritative random-number contract) and
/// <c>docs/technical/10-runtime-architecture.md</c> § Randomness and reproducibility. Fixture:
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-seed-derivation.txt</c>, which pins all
/// four intermediate values for nine triples including master seed zero, master seed all-ones,
/// instance key zero, and an all-ones instance key.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SeedDerivationGoldenVectorTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-005-002</c>. Every intermediate value of every pinned triple
    /// equals the committed vector.
    /// </summary>
    [Test]
    public void DerivationChainMatchesTheCommittedVector()
    {
        string rendered = RandomGoldenHeaders.SeedDerivation
            + RandomVectorRendering.SeedDerivationBody(ProductionVectorEngine.Instance);

        GoldenText.Matches(RandomVectorRendering.SeedDerivationGolden, rendered);
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-002</c>. <c>Mix</c> is exactly SplitMix64's
    /// <c>next()</c>, checked against a value published outside this repository.
    /// </summary>
    /// <remarks>
    /// Vigna's SplitMix64 reference (<c>https://prng.di.unimi.it/splitmix64.c</c>) emits
    /// <c>0xE220A8397B1DCDAF</c> as the first output for state zero, which is precisely
    /// <c>Mix(0)</c> under the derivation chain of doc 20 § Authoritative random-number
    /// contract's definition. Anchoring the mixing function to an external value catches a
    /// wrong shift or a transposed multiplier independently of the derivation golden, which was
    /// produced from the same document text.
    /// </remarks>
    [Test]
    public void MixIsSplitMix64AtItsPublishedSeedZeroValue()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                SeedDerivation.Mix(0UL),
                Is.EqualTo(0xE220A8397B1DCDAFUL),
                "SplitMix64 next() for state 0");
            Assert.That(
                SeedDerivation.Mix(SeedDerivation.Mix(0UL)),
                Is.Not.EqualTo(SeedDerivation.Mix(0UL)),
                "Mix is not idempotent, so it is being applied rather than skipped");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-002</c>. Each of the four steps depends on the input the
    /// document gives it, so no step can be silently skipped.
    /// </summary>
    /// <remarks>
    /// The chain's shape is what isolates streams: if <c>d1</c> ignored the family key, or the
    /// state seed ignored the instance key, every family or every instance would share one
    /// sequence and every other golden in this package would still pass for the master seed it
    /// happens to pin. This asserts the dependencies directly.
    /// </remarks>
    [Test]
    public void EveryDerivationStepDependsOnItsDocumentedInput()
    {
        ulong d0 = SeedDerivation.DeriveD0(RandomSchemaVersion.Current, 0x0123456789ABCDEFUL);
        ulong otherSeedD0 = SeedDerivation.DeriveD0(RandomSchemaVersion.Current, 0x0123456789ABCDEEUL);
        ulong otherVersionD0 = SeedDerivation.DeriveD0(new RandomSchemaVersion(2), 0x0123456789ABCDEFUL);
        ulong d1 = SeedDerivation.DeriveD1(d0, 0x0100);
        ulong otherFamilyD1 = SeedDerivation.DeriveD1(d0, 0x0200);
        ulong stateSeed = SeedDerivation.DeriveStateSeed(d1, 0UL);
        ulong otherInstanceStateSeed = SeedDerivation.DeriveStateSeed(d1, 1UL);

        Expect.Multiple(() =>
        {
            Assert.That(otherSeedD0, Is.Not.EqualTo(d0), "d0 depends on the master seed");
            Assert.That(otherVersionD0, Is.Not.EqualTo(d0), "d0 depends on the schema version");
            Assert.That(otherFamilyD1, Is.Not.EqualTo(d1), "d1 depends on the family key");
            Assert.That(
                otherInstanceStateSeed,
                Is.Not.EqualTo(stateSeed),
                "the state seed depends on the instance key");
            Assert.That(
                SeedDerivation.DeriveSelector(stateSeed),
                Is.Not.EqualTo(stateSeed),
                "the selector is a further Mix of the state seed, not the state seed itself");
        });
    }
}
