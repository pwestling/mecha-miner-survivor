using System;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Encounters;

/// <summary>
/// Minute 0's authored replenishment row, and the row value type it is expressed in.
/// </summary>
/// <remarks>Verification: <c>VER-ENC-001-004</c>.</remarks>
[TestFixture]
internal sealed class MinuteZeroBaselineTests
{
    [Test]
    public void TheRowEqualsTheAuthoredMinuteZeroRow()
    {
        BaselineReplenishmentRow row = MinuteZeroBaseline.Row;

        Expect.Multiple(() =>
        {
            Assert.That(row.DesiredMinimumPopulation, Is.EqualTo(8), "docs/32:56 Minimum column");
            Assert.That(row.PulseBatchSize, Is.EqualTo(2), "docs/32:56 Pulse column, '2 / 1.50s'");
            Assert.That(
                row.PulseIntervalTicks,
                Is.EqualTo(90),
                "1.50 s at 60 Hz is 90 ticks exactly");
            Assert.That(
                row.PulseIntervalTicks / (double)TickRate.TicksPerSecond,
                Is.EqualTo(1.50).Within(1e-12));
            Assert.That(
                row.Label,
                Does.Contain("docs/32:56"),
                "the row carries its provenance, so a transcript that prints it says where it came from");
        });
    }

    [Test]
    public void APulseFiresOnTickZeroAndThenOnEveryNinetiethTick()
    {
        BaselineReplenishmentRow row = MinuteZeroBaseline.Row;

        Expect.Multiple(() =>
        {
            Assert.That(
                row.PulseSize(0, 0),
                Is.EqualTo(2),
                "docs/32 § Phase-level pressure curve:43 wants a few bodies from 0:00, so tick zero is a "
                    + "pulse tick and the opening is not empty for one interval");
            Assert.That(row.PulseSize(1, 0), Is.EqualTo(0), "and no tick between");
            Assert.That(row.PulseSize(89, 0), Is.EqualTo(0));
            Assert.That(row.PulseSize(90, 0), Is.EqualTo(2));
            Assert.That(row.PulseSize(180, 0), Is.EqualTo(2));
        });
    }

    [Test]
    public void ReplenishmentStopsOnceTheMinimumIsMetAndResumesWhenAttritionCreatesRoom()
    {
        BaselineReplenishmentRow row = MinuteZeroBaseline.Row;

        Expect.Multiple(() =>
        {
            Assert.That(
                row.PulseSize(90, 8),
                Is.EqualTo(0),
                "docs/32:20 - 'Once the minimum is met, baseline spawning waits until attrition creates "
                    + "room.' A pulse that fired regardless would reach the 730-record store ceiling in "
                    + "under ten minutes");
            Assert.That(row.PulseSize(90, 12), Is.EqualTo(0), "and stays quiet above the minimum too");
            Assert.That(row.PulseSize(90, 6), Is.EqualTo(2), "a shortfall of two admits the whole batch");
        });
    }

    [Test]
    public void ABatchIsTrimmedToTheShortfallRatherThanOvershootingTheDesiredMinimum()
    {
        Assert.That(
            MinuteZeroBaseline.Row.PulseSize(90, 7),
            Is.EqualTo(1),
            "docs/32:19 calls the minimum 'desired', so admitting a full batch of 2 into a shortfall of 1 "
                + "would put the population above the number the document names, by an amount whose sign "
                + "nothing decides");
    }

    [Test]
    public void ADefaultedRowAdmitsNothingRatherThanDividingByZero()
    {
        BaselineReplenishmentRow defaulted = default;

        Expect.Multiple(() =>
        {
            Assert.That(defaulted.IsAuthored, Is.False);
            Assert.That(
                defaulted.PulseSize(0, 0),
                Is.EqualTo(0),
                "a defaulted row has a zero interval, and 0 % 0 would throw inside a committed tick");
        });
    }

    [Test]
    public void ARowRefusesAnIntervalThatDoesNotLandOnAWholeTick()
    {
        // docs/32's later Pulse intervals include 0.16 s, which is 9.6 ticks. A compiler of those rows
        // has to decide what to do about the remainder; refusing here means it decides deliberately.
        ArgumentOutOfRangeException failure = Expect.Throws<ArgumentOutOfRangeException>(
            () => BaselineReplenishmentRow.Create("docs/32:90 minute-34 row", 420, 30, 0.16));

        Expect.Multiple(() =>
        {
            Assert.That(failure.ParamName, Is.EqualTo("pulseIntervalSeconds"));
            Assert.That(
                failure.Message,
                Does.Contain("9.6 ticks"),
                "the message names the offending value, so the reader sees why rather than only that");
        });
    }

    [Test]
    public void ARowRefusesToBeBuiltWithoutALabel()
    {
        Assert.That(
            Expect.Throws<ArgumentException>(
                () => BaselineReplenishmentRow.Create("   ", 8, 2, 1.5)).ParamName,
            Is.EqualTo("label"),
            "a row's provenance is the difference between a balance claim and a test fixture, and a "
                + "transcript printing three bare numbers could not tell a reader which it was looking at");
    }

    [Test]
    public void TheHarnessStressRowSaysInItsOwnLabelThatNoDocumentStatesIt()
    {
        BaselineReplenishmentRow stress = HarnessStressRows.LethalSwarm;

        Expect.Multiple(() =>
        {
            Assert.That(
                stress.Label,
                Does.Contain("NOT from any document"),
                "the label travels into ToString and from there into every transcript that prints the "
                    + "row, so a reader of an artifact cannot see these numbers without also seeing that "
                    + "nothing authored them");
            Assert.That(
                stress.ToString(),
                Does.Contain("NOT from any document"),
                "and ToString is what an artifact actually prints");
            Assert.That(
                stress.DesiredMinimumPopulation,
                Is.GreaterThan(MinuteZeroBaseline.DesiredMinimumPopulation),
                "it exists because minute 0's authored pressure cannot destroy the mech inside a capture "
                    + "someone can watch");
        });
    }
}
