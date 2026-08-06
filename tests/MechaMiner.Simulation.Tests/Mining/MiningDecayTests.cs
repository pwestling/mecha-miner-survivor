using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Mining;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Mining;

/// <summary>
/// The exit grace, the four-times decay, and the checkpoint decay cannot touch.
/// </summary>
/// <remarks>Verification: <c>VER-MIN-001-003</c>.</remarks>
[TestFixture]
internal sealed class MiningDecayTests
{
    private static readonly PlanarVector Centre = PlanarVector.Zero;

    private static PlanarVector Inside => Centre;

    private static PlanarVector Outside =>
        Centre + PlanarVector.FromComponents(GrayboxExtraction.ZoneRadiusMeters + 1.0, 0.0);

    private static MiningSiteState AfterTicksInside(int ticks)
    {
        MiningSiteState seam = MiningSiteState.Available(Centre);
        for (int tick = 0; tick < ticks; tick++)
        {
            seam = MiningAdvance.Advance(seam, Inside).State;
        }

        return seam;
    }

    [Test]
    public void UnfinishedProgressHoldsUnchangedForExactlyThirtyTicksAfterLeaving()
    {
        MiningSiteState seam = AfterTicksInside(60);
        int progressOnLeaving = seam.InstallmentProgressTicks;

        for (int tick = 0; tick < GrayboxExtraction.ExitGraceTicks; tick++)
        {
            seam = MiningAdvance.Advance(seam, Outside).State;
            Assert.That(
                seam.InstallmentProgressTicks,
                Is.EqualTo(progressOnLeaving),
                "docs/40:52 - 'Unfinished progress holds steady during that grace period.' Tick "
                    + (tick + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + " of the grace changed it");
        }

        Assert.That(
            MiningAdvance.Advance(seam, Outside).State.InstallmentProgressTicks,
            Is.EqualTo(progressOnLeaving - GrayboxExtraction.DecayRateMultiple),
            "and the thirty-first tick outside is the first that decays");
    }

    [Test]
    public void ProgressDecaysAtFourTimesTheForwardRate()
    {
        MiningSiteState seam = AfterTicksInside(80);
        int progressOnLeaving = seam.InstallmentProgressTicks;

        // Past the grace, then ten more ticks of decay.
        for (int tick = 0; tick < GrayboxExtraction.ExitGraceTicks + 10; tick++)
        {
            seam = MiningAdvance.Advance(seam, Outside).State;
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                seam.InstallmentProgressTicks,
                Is.EqualTo(progressOnLeaving - 40),
                "ten ticks of decay removed forty ticks of progress. docs/40:52 - 'decays linearly at four "
                    + "times that point's forward extraction rate'");
            Assert.That(seam.Phase, Is.EqualTo(MiningPhase.Decaying));
        });
    }

    [Test]
    public void HalfAFullExtractionDisappearsInOneEighthOfTheTimeItTookToBuild()
    {
        // docs/40:54 states this consequence directly, which makes it a check on the multiple rather than
        // a restatement of it: "half of a full extraction's progress disappears in one eighth of the time
        // needed to complete that extraction from zero, after the grace period."
        MiningSiteState seam = AfterTicksInside(StandardOreSeamProfile.InstallmentTicks / 2);
        int half = seam.InstallmentProgressTicks;

        for (int tick = 0; tick < GrayboxExtraction.ExitGraceTicks; tick++)
        {
            seam = MiningAdvance.Advance(seam, Outside).State;
        }

        int decayTicks = 0;
        while (seam.InstallmentProgressTicks > 0)
        {
            seam = MiningAdvance.Advance(seam, Outside).State;
            decayTicks++;
        }

        Expect.Multiple(() =>
        {
            Assert.That(half, Is.EqualTo(45), "half of a 90-tick installment");
            Assert.That(
                decayTicks,
                Is.EqualTo(12),
                "docs/40:54's figure is one eighth of 90 ticks, which is 11.25 - not a whole number of "
                    + "ticks. Integer decay of 4 per tick removes 45 in twelve ticks, the ceiling of "
                    + "11.25, so the last tick removes 1 rather than 4. Asserting 11 would be asserting a "
                    + "fractional tick, and asserting 'about 11' would be a comparison that could not "
                    + "fail");
            Assert.That(
                decayTicks * GrayboxExtraction.DecayRateMultiple,
                Is.GreaterThanOrEqualTo(half).And.LessThan(half + GrayboxExtraction.DecayRateMultiple),
                "and twelve is the ceiling rather than a rounder number: eleven ticks removes only 44 of "
                    + "the 45");
            Assert.That(
                seam.Phase,
                Is.EqualTo(MiningPhase.Available),
                "docs/40's state diagram - 'Decaying --> Available: Progress decays to zero'. Counting "
                    + "progress in ticks rather than as a floating fraction is what makes zero reachable "
                    + "rather than asymptotic");
        });
    }

    [Test]
    public void ReEnteringResumesFromTheProgressThatRemains()
    {
        MiningSiteState seam = AfterTicksInside(80);

        for (int tick = 0; tick < GrayboxExtraction.ExitGraceTicks + 5; tick++)
        {
            seam = MiningAdvance.Advance(seam, Outside).State;
        }

        int remaining = seam.InstallmentProgressTicks;
        MiningTickOutcome resumed = MiningAdvance.Advance(seam, Inside);

        Expect.Multiple(() =>
        {
            Assert.That(remaining, Is.EqualTo(60), "80 built, 20 decayed over 5 ticks");
            Assert.That(
                resumed.State.InstallmentProgressTicks,
                Is.EqualTo(61),
                "docs/40:52 - 'Re-entering before progress reaches zero resumes extraction from the "
                    + "remaining progress.' It does not restart and does not jump back to 80");
            Assert.That(
                resumed.State.TicksOutside,
                Is.EqualTo(0),
                "and the grace is armed again from zero, so a second exit gets a full 0.5 s");
        });
    }

    [Test]
    public void APaidInstallmentIsNeverReducedByAnyAmountOfDecay()
    {
        MiningSiteState seam = AfterTicksInside(StandardOreSeamProfile.InstallmentTicks * 3 + 40);
        int paid = seam.InstallmentsPaid;

        // Long enough outside to decay the current interval to zero several times over.
        for (int tick = 0; tick < 600; tick++)
        {
            seam = MiningAdvance.Advance(seam, Outside).State;
        }

        Expect.Multiple(() =>
        {
            Assert.That(paid, Is.EqualTo(3));
            Assert.That(
                seam.InstallmentsPaid,
                Is.EqualTo(3),
                "docs/40:64 - 'it never removes previously awarded ore'. A single combined progress figure "
                    + "would have let decay eat into a banked installment and the seam would have refunded "
                    + "ore the run already held");
            Assert.That(seam.InstallmentProgressTicks, Is.EqualTo(0), "only the unfinished interval went");
            Assert.That(seam.Phase, Is.EqualTo(MiningPhase.Available));
        });
    }

    [Test]
    public void AnInstallmentThatCompletesOnTheTickTheMechIsStillInsideIsPaid()
    {
        // docs/40:175 lists "An ore seam is interrupted at the exact instant an installment completes" as
        // an open edge case. This resolves it in the player's favour, because docs/40:64 makes a completed
        // installment "a permanent checkpoint for that run".
        MiningSiteState seam = AfterTicksInside(StandardOreSeamProfile.InstallmentTicks - 1);
        MiningTickOutcome completing = MiningAdvance.Advance(seam, Inside);
        MiningTickOutcome afterLeaving = MiningAdvance.Advance(completing.State, Outside);

        Expect.Multiple(() =>
        {
            Assert.That(completing.OreAwarded, Is.EqualTo(10));
            Assert.That(
                afterLeaving.State.InstallmentsPaid,
                Is.EqualTo(1),
                "leaving on the following tick cannot retract it");
        });
    }
}
