using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Mining;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Mining;

/// <summary>
/// Automatic proximity extraction: entering, remaining, the inclusive boundary, and depletion.
/// </summary>
/// <remarks>Verification: <c>VER-MIN-001-002</c>.</remarks>
[TestFixture]
internal sealed class MiningAdvanceTests
{
    private static readonly PlanarVector Centre = PlanarVector.FromComponents(4.0, -3.0);

    private static PlanarVector Inside => Centre;

    private static PlanarVector Outside =>
        Centre + PlanarVector.FromComponents(GrayboxExtraction.ZoneRadiusMeters + 1.0, 0.0);

    [Test]
    public void EnteringTheZoneAdvancesProgressWithNoCommandAtAll()
    {
        MiningSiteState seam = MiningSiteState.Available(Centre);

        MiningTickOutcome outcome = MiningAdvance.Advance(seam, Inside);

        Expect.Multiple(() =>
        {
            Assert.That(
                outcome.State.InstallmentProgressTicks,
                Is.EqualTo(1),
                "docs/40:16-18 - 'Mining is automatic ... No interaction button or repeated mining input "
                    + "is required.' The rule takes a position and nothing else; there is no parameter a "
                    + "command could arrive through");
            Assert.That(outcome.State.Phase, Is.EqualTo(MiningPhase.Extracting));
            Assert.That(outcome.OreAwarded, Is.EqualTo(0), "and no ore until the installment completes");
        });
    }

    [Test]
    public void RemainingInsideCompletesAnInstallmentOnExactlyTheNinetiethTick()
    {
        MiningSiteState seam = MiningSiteState.Available(Centre);
        int oreAtTick89 = -1;

        for (int tick = 1; tick <= 89; tick++)
        {
            MiningTickOutcome outcome = MiningAdvance.Advance(seam, Inside);
            seam = outcome.State;
            oreAtTick89 = outcome.OreAwarded;
        }

        MiningTickOutcome ninetieth = MiningAdvance.Advance(seam, Inside);

        Expect.Multiple(() =>
        {
            Assert.That(oreAtTick89, Is.EqualTo(0), "89 ticks is 1.483 s, short of the documented 1.5");
            Assert.That(
                ninetieth.OreAwarded,
                Is.EqualTo(10),
                "the ninetieth tick is 1.5 s exactly and pays the documented 10 ore");
            Assert.That(ninetieth.State.InstallmentsPaid, Is.EqualTo(1));
            Assert.That(
                ninetieth.State.InstallmentProgressTicks,
                Is.EqualTo(0),
                "and the next installment starts from zero rather than carrying a remainder");
        });
    }

    [Test]
    public void TheBoundaryTestIsInclusiveAtExactTangency()
    {
        MiningSiteState seam = MiningSiteState.Available(Centre);
        PlanarVector exactlyOnTheEdge =
            Centre + PlanarVector.FromComponents(GrayboxExtraction.ZoneRadiusMeters, 0.0);

        Assert.That(
            MiningAdvance.Advance(seam, exactlyOnTheEdge).State.InstallmentProgressTicks,
            Is.EqualTo(1),
            "docs/technical/21:103 makes the repository's containment and overlap tests inclusive, and "
                + "TR-MIN-001 says outright that 'occupancy is inclusive'");
    }

    [Test]
    public void FifteenSecondsInsideDepletesTheSeamForExactlyTheDocumentedTotal()
    {
        MiningSiteState seam = MiningSiteState.Available(Centre);
        int total = 0;

        for (int tick = 0; tick < StandardOreSeamProfile.DepletionTicks; tick++)
        {
            MiningTickOutcome outcome = MiningAdvance.Advance(seam, Inside);
            seam = outcome.State;
            total += outcome.OreAwarded;
        }

        Expect.Multiple(() =>
        {
            Assert.That(total, Is.EqualTo(100), "docs/40:62 - 100 ore over 15 s");
            Assert.That(seam.InstallmentsPaid, Is.EqualTo(10));
            Assert.That(seam.IsDepleted, Is.True);
            Assert.That(seam.Phase, Is.EqualTo(MiningPhase.Complete));
        });
    }

    [Test]
    public void ADepletedSeamAdvancesNothingAndCannotBeReactivated()
    {
        MiningSiteState seam = MiningSiteState.Available(Centre);
        for (int tick = 0; tick < StandardOreSeamProfile.DepletionTicks; tick++)
        {
            seam = MiningAdvance.Advance(seam, Inside).State;
        }

        // Leave and come back, which is what would reactivate it if anything could.
        for (int tick = 0; tick < 300; tick++)
        {
            seam = MiningAdvance.Advance(seam, Outside).State;
        }

        MiningTickOutcome afterReturning = MiningAdvance.Advance(seam, Inside);

        Expect.Multiple(() =>
        {
            Assert.That(
                afterReturning.OreAwarded,
                Is.EqualTo(0),
                "docs/40:90 - a depleted point 'remains as a non-interactive mapped landmark for the rest "
                    + "of the run and cannot be reactivated'");
            Assert.That(afterReturning.State.InstallmentsPaid, Is.EqualTo(10), "and pays no eleventh");
            Assert.That(afterReturning.State.InstallmentProgressTicks, Is.EqualTo(0));
            Assert.That(afterReturning.State.Phase, Is.EqualTo(MiningPhase.Complete));
        });
    }

    [Test]
    public void AtMostOneInstallmentCompletesPerTick()
    {
        // Forward progress is one tick of credit per tick and an installment costs 90, so two
        // completions in one tick would need 90 ticks of credit to arrive at once. The outcome carries an
        // ore amount rather than a count precisely so a caller can assert this.
        MiningSiteState seam = MiningSiteState.Available(Centre);
        int maximumInOneTick = 0;

        for (int tick = 0; tick < StandardOreSeamProfile.DepletionTicks; tick++)
        {
            MiningTickOutcome outcome = MiningAdvance.Advance(seam, Inside);
            seam = outcome.State;
            if (outcome.OreAwarded > maximumInOneTick)
            {
                maximumInOneTick = outcome.OreAwarded;
            }
        }

        Assert.That(
            maximumInOneTick,
            Is.EqualTo(StandardOreSeamProfile.OrePerInstallment),
            "no tick ever paid two installments' worth");
    }
}
