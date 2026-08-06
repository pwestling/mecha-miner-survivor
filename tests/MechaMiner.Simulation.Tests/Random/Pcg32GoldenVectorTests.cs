using System.Globalization;
using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// The PCG-XSH-RR 64/32 output sequence against its committed golden vector.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-001</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract, the generator. Fixture:
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-stream-initialization.txt</c>, derived
/// by an independent reference before this implementation existed.
/// </para>
/// <para>
/// Every assertion reaches the generator through <see cref="RandomStreamSet"/>, because that is
/// the only way to reach it: <c>Pcg32</c> is internal, so no test and no consumer can hold a
/// stream value. That is deliberate - a copied stream forks silently - and it means these tests
/// exercise the code path production actually uses rather than a construction only a test can
/// perform.
/// </para>
/// <para>
/// No test in this namespace uses <c>DeterministicCase</c>. Every assertion here is
/// deterministic by construction - a pinned master seed, a pinned family and instance key, and
/// a committed golden - so there is no randomized body for a harness seed to reproduce.
/// <c>DeterministicCase</c> supplies test-harness <c>System.Random</c>, which
/// <c>tests/shared/README.md</c> says "must never be replaced by, compared against, or confused
/// with" this contract.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class Pcg32GoldenVectorTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-005-001</c>. Every output of every pinned stream equals the
    /// committed vector.
    /// </summary>
    [Test]
    public void OutputSequenceMatchesTheCommittedVector()
    {
        string rendered = RandomGoldenHeaders.StreamInitialization
            + RandomVectorRendering.StreamInitializationBody(ProductionVectorEngine.Instance);

        GoldenText.Matches(RandomVectorRendering.StreamInitializationGolden, rendered);
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-001</c>. The one assertion in this package anchored to a
    /// value published outside this repository.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The initialization rule of doc 20 § Authoritative random-number contract is canonical
    /// <c>pcg32_srandom_r</c> with the state seed as <c>initstate</c> and the selector as
    /// <c>initseq</c>, so the published <c>pcg32-demo</c> inputs 42 and 54 must reproduce the
    /// published "Round 1" 32-bit output. Every other check in this package compares this
    /// repository against itself; this one catches a misreading of the document that the
    /// implementation and its golden could otherwise share.
    /// </para>
    /// <para>
    /// The published demo pins outputs, not internal state, and no production API initializes a
    /// stream from an arbitrary seed pair - deliberately, since that would be exactly the
    /// unregistered-randomness path the family table exists to prevent. So the independent
    /// reference computes the primed state and odd increment for 42/54, and production is
    /// driven from them through run recovery, which is the one public path that installs an
    /// explicit state. Both implementations are then asserted against the published values.
    /// </para>
    /// </remarks>
    [Test]
    public void ThePublishedPcgDemoVectorIsReproduced()
    {
        uint[] published =
        {
            0xA15C02B7U, 0x7B47F409U, 0xBA1D3330U, 0x83D2F293U, 0xBFA4784BU, 0xCBED606EU,
        };

        IRandomVectorStream reference = ReferenceVectorEngine.Canonical.OpenFromSeeds(42UL, 54UL);
        ulong primedState = reference.State;
        ulong increment = reference.Increment;

        RandomStreamKey key = RandomStreamKey.Create(0x0100, 0UL);
        RandomStreamSet production = RandomStreamSet.Restore(
            RandomSchemaVersion.Current,
            0UL,
            new[]
            {
                new RandomStreamRecoveryState(
                    RandomSchemaVersion.Current,
                    key.FamilyKey,
                    key.InstanceKey,
                    primedState,
                    increment),
            });

        Expect.Multiple(() =>
        {
            Assert.That(
                increment,
                Is.EqualTo(109UL),
                "pcg32_srandom_r sets inc = (initseq << 1) | 1, so 54 gives 109");

            for (int index = 0; index < published.Length; index++)
            {
                string label = "pcg32_srandom_r(&rng, 42u, 54u) output "
                    + index.ToString(CultureInfo.InvariantCulture);
                Assert.That(reference.NextUInt32(), Is.EqualTo(published[index]), "reference: " + label);
                Assert.That(production.NextUInt32(key), Is.EqualTo(published[index]), "production: " + label);
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-001</c>. The output is the transformation of the prior
    /// state, not the state itself and not the state after the advance.
    /// </summary>
    /// <remarks>
    /// A generator that returned the low 32 bits of its state, or that transformed the state
    /// <em>after</em> advancing, would still look random and would still be reproducible. It
    /// would simply be a different generator than doc 20 § Authoritative random-number contract
    /// specifies, so this asserts the relationship between the observed state and the observed
    /// output directly.
    /// </remarks>
    [Test]
    public void OutputIsTheTransformationOfTheStateReadBeforeTheAdvance()
    {
        RandomStreamSet set = new(RandomSchemaVersion.Current, 0x0123456789ABCDEFUL);
        RandomStreamKey key = RandomStreamKey.Create(0x0201, 0UL);
        ulong increment = set.IncrementOf(key);

        Expect.Multiple(() =>
        {
            for (int index = 0; index < 8; index++)
            {
                ulong priorState = set.StateOf(key);
                uint output = set.NextUInt32(key);
                ulong advancedState = set.StateOf(key);

                uint expectedXorShift = (uint)(((priorState >> 18) ^ priorState) >> 27);
                int expectedRotation = (int)(priorState >> 59);
                uint expected = expectedRotation == 0
                    ? expectedXorShift
                    : (expectedXorShift >> expectedRotation) | (expectedXorShift << (32 - expectedRotation));

                Assert.That(output, Is.EqualTo(expected), "XSH-RR of the prior state");
                Assert.That(
                    advancedState,
                    Is.EqualTo(unchecked((priorState * 6364136223846793005UL) + increment)),
                    "the state advances by the documented LCG step");
                Assert.That(
                    set.DrawCountOf(key),
                    Is.EqualTo((ulong)index + 1UL),
                    "each caller-visible draw is counted exactly once");
            }
        });
    }
}
