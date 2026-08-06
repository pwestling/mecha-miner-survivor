using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Random;
using MechaMiner.Simulation.World;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.World;

/// <summary>
/// Two runs of one seed produce byte-identical transcripts, and two runs of different seeds do not.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-ENC-001-005</c>.
/// </para>
/// <para>
/// <b>Both halves are necessary and the second is the one that makes the first mean anything.</b> A
/// same-seed comparison passes trivially for a world that draws no randomness at all, or for one whose
/// draws never reach an observable quantity. The different-seed case is what proves the transcript is
/// sensitive to the streams, so the same-seed case is a statement about reproducibility rather than about
/// the transcript being constant.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RunDeterminismTests
{
    /// <summary>
    /// How many 32-bit draws one <c>NextUnitDouble</c> consumes: <c>2</c>.
    /// </summary>
    /// <remarks>
    /// <c>BoundedRandom.NextUnitDouble</c> takes a high and a low word to fill a 53-bit mantissa. Named
    /// here so the draw-count assertions below read as one bearing per enemy rather than as an unexplained
    /// factor of two.
    /// </remarks>
    private const int DrawsPerUnitDouble = 2;

    private const int Ticks = 1200;
    private const int SampleEvery = 60;

    private static string TranscriptFor(ulong seed)
    {
        return RunTranscript.Drive(
            RunComposition.CreateGraybox(seed),
            Ticks,
            RunTranscript.KiteOnRing(9.0),
            SampleEvery);
    }

    [Test]
    public void TwoRunsOfTheSameSeedProduceByteIdenticalTranscripts()
    {
        string first = TranscriptFor(0x5EED_0000_0000_0001UL);
        string second = TranscriptFor(0x5EED_0000_0000_0001UL);

        Expect.Multiple(() =>
        {
            Assert.That(
                second,
                Is.EqualTo(first),
                "every enemy position, every seam's progress, every counter, at every sampled tick over "
                    + "1,200 ticks. Positions render round-trip, so two doubles differing in the last bit "
                    + "would render differently");
            Assert.That(
                first,
                Does.Contain("spawned="),
                "and the transcript is not empty of the quantities the claim is about");
        });
    }

    [Test]
    public void TwoRunsOfDifferentSeedsDoNotProduceTheSameTranscript()
    {
        string first = TranscriptFor(0x5EED_0000_0000_0001UL);
        string different = TranscriptFor(0x5EED_0000_0000_0002UL);

        Assert.That(
            different,
            Is.Not.EqualTo(first),
            "without this the same-seed case would also pass for a world that drew no randomness, or one "
                + "whose draws never reached anything the transcript renders");
    }

    [Test]
    public void SeamPlacementItselfDiffersBySeed()
    {
        RunComposition first = RunComposition.CreateGraybox(0x5EED_0000_0000_0003UL);
        RunComposition different = RunComposition.CreateGraybox(0x5EED_0000_0000_0004UL);

        Assert.That(
            different.World.MiningSiteAt(0).Centre,
            Is.Not.EqualTo(first.World.MiningSiteAt(0).Centre),
            "the seams are drawn from the registered standard-seam-placement family, so they are part of "
                + "what a seed reproduces rather than a fixed layout the transcript happens to include");
    }

    [Test]
    public void OnlyTheTwoRegisteredFamiliesAreInstantiatedAndBothAreDrawnFrom()
    {
        RunComposition run = RunComposition.CreateGraybox(0x5EED_0000_0000_0005UL);
        _ = RunTranscript.Drive(run, 300, RunTranscript.Stand, 300);

        RandomStreamKey seams = RandomStreamKey.Create(
            RandomStreamFamilies.StandardSeamPlacement,
            0UL);
        RandomStreamKey entrances = RandomStreamKey.Create(
            RandomStreamFamilies.BaselineEncounterSectorsAndComposition,
            0UL);

        Expect.Multiple(() =>
        {
            Assert.That(
                run.Streams.InstantiatedKeys,
                Has.Count.EqualTo(2),
                "doc 20 § Authoritative random-number contract requires a registered family and forbids "
                    + "inventing one at a call site; a third instantiated key would mean a family was "
                    + "derived that this world does not document");
            Assert.That(
                run.Streams.DrawCountOf(seams),
                Is.EqualTo((ulong)(GrayboxRunLayout.MiningSiteCount * DrawsPerUnitDouble)),
                "exactly one bearing per seam. The draw COUNT is two per bearing because "
                    + "BoundedRandom.NextUnitDouble fills a 53-bit mantissa from two 32-bit draws - "
                    + "measured, not assumed; the first version of this assertion expected 3 and got 6");
            Assert.That(
                run.Streams.DrawCountOf(entrances),
                Is.EqualTo((ulong)(run.World.EnemiesMaterialized * DrawsPerUnitDouble)),
                "and exactly one entrance bearing per materialized enemy. That equality is what makes the "
                    + "stream position a function of committed ticks and deaths, so two runs of one seed "
                    + "stay in step for the whole run rather than only until the first branch");
            Assert.That(
                run.World.EnemiesMaterialized,
                Is.GreaterThan(0L),
                "and the count being compared is not zero on both sides");
        });
    }
}
