using MechaMiner.Simulation.Mining;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Mining;

/// <summary>
/// The standard ore seam's authored payout profile.
/// </summary>
/// <remarks>Verification: <c>VER-MIN-001-001</c>.</remarks>
[TestFixture]
internal sealed class StandardOreSeamProfileTests
{
    [Test]
    public void TheProfileEqualsTheAuthoredPayoutProfile()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                StandardOreSeamProfile.OrePerInstallment,
                Is.EqualTo(10),
                "docs/40:62 - 'awards 10 common ore whenever the player completes a 1.5-second extraction "
                    + "installment'");
            Assert.That(
                StandardOreSeamProfile.InstallmentSeconds,
                Is.EqualTo(1.5).Within(1e-12),
                "docs/40:62");
            Assert.That(
                StandardOreSeamProfile.InstallmentCount,
                Is.EqualTo(10),
                "docs/40:62 - 'It contains ten installments'");
            Assert.That(
                StandardOreSeamProfile.ResourceContentId,
                Is.EqualTo("common-ore"));
        });
    }

    [Test]
    public void TheDerivedTotalsAreTheOnesTheDocumentStatesIndependently()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                StandardOreSeamProfile.TotalOre,
                Is.EqualTo(100),
                "docs/40:62 - 'paying 100 ore in total'. Derived from the two factors above rather than "
                    + "stored, so a stored third number cannot disagree with them");
            Assert.That(
                StandardOreSeamProfile.DepletionTicks / (double)TickRate.TicksPerSecond,
                Is.EqualTo(15.0).Within(1e-12),
                "docs/40:62 - 'takes 15 seconds of uninterrupted forward extraction to deplete'");
        });
    }

    [Test]
    public void AnInstallmentLandsOnAWholeNumberOfTicks()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                StandardOreSeamProfile.InstallmentTicks,
                Is.EqualTo(90),
                "1.5 s at 60 Hz is 90 ticks exactly");
            Assert.That(
                StandardOreSeamProfile.InstallmentTicks / (double)TickRate.TicksPerSecond,
                Is.EqualTo(StandardOreSeamProfile.InstallmentSeconds).Within(1e-12),
                "and converts back with no rounding, so no rounding direction was silently chosen");
        });
    }

    [Test]
    public void TheExtractionConstantsThatDoHaveSourcesAreTheDocumentedOnes()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                GrayboxExtraction.ExitGraceTicks,
                Is.EqualTo(30),
                "docs/40:52 - a 0.5-second grace, which is 30 ticks");
            Assert.That(
                GrayboxExtraction.DecayRateMultiple,
                Is.EqualTo(4),
                "docs/40:52 - 'decays linearly at four times that point's forward extraction rate'");
            Assert.That(
                GrayboxExtraction.ZoneRadiusMeters,
                Is.GreaterThan(0.0),
                "the radius is graybox with no source claimed - docs/40:48, DEC-031:51 and docs/10:75 each "
                    + "say it remains open in OQ-004 - so the only thing asserted about it here is that it "
                    + "is a usable circle. Nothing in this suite asserts its VALUE, deliberately");
        });
    }
}
