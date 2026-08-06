using MechaMiner.Simulation.Combat;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Combat;

/// <summary>
/// <c>W-BC</c> Pulse Repeater's rank-zero catalog numbers and the two tick counts derived from them.
/// </summary>
/// <remarks>Verification: <c>VER-COM-005-001</c>.</remarks>
[TestFixture]
internal sealed class PulseRepeaterTests
{
    [Test]
    public void TheRankZeroNumbersEqualTheAuthoredCatalogRow()
    {
        Expect.Multiple(() =>
        {
            Assert.That(PulseRepeaterBaseline.ContentId, Is.EqualTo("W-BC"), "docs/66:46");
            Assert.That(
                PulseRepeaterBaseline.DamagePerProjectile,
                Is.EqualTo(12),
                "docs/71:81 Stat 1 'Damage | 12 / +1.2'");
            Assert.That(
                PulseRepeaterBaseline.TargetingRangeMeters,
                Is.EqualTo(8.0).Within(1e-12),
                "docs/71:81 Stat 3 'Range | 8M / +0.8M', with M = 1.0 m by docs/72:40 and :47");
            Assert.That(
                PulseRepeaterBaseline.ProjectileSpeedMetersPerSecond,
                Is.EqualTo(16.0).Within(1e-12),
                "docs/71:81 Fixed properties, '16M/s projectile'");
            Assert.That(
                PulseRepeaterBaseline.ActivationPeriodSeconds,
                Is.EqualTo(0.375).Within(1e-12),
                "docs/71:57 '12 every 0.375 s'");
        });
    }

    [Test]
    public void TheStatedRateAndTheStatedPeriodDescribeTheSameWeapon()
    {
        // The two columns are the same figure to different precisions, so they must agree to within the
        // rate column's four significant digits. If a later edit changed one and not the other, this is
        // what notices.
        Assert.That(
            1.0 / PulseRepeaterBaseline.ActivationPeriodSeconds,
            Is.EqualTo(PulseRepeaterBaseline.AttackRatePerSecond).Within(0.0005),
            "docs/71:57 gives the period as 0.375 s and docs/71:81 the rate as 2.667/s; the reciprocal of "
                + "the first is 2.66667, so the second is that rounded to four significant digits");
    }

    [Test]
    public void TheCadenceIsTwentyTwoAndAHalfTicksExpressedAsAnExactFraction()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                PulseRepeaterBaseline.CadenceNumeratorPerTick,
                Is.EqualTo(2),
                "each tick earns 2 credit");
            Assert.That(
                PulseRepeaterBaseline.CadenceDenominatorTicks,
                Is.EqualTo(45),
                "and an activation costs 45, so the period is 45/2 = 22.5 ticks");
            Assert.That(
                PulseRepeaterBaseline.CadenceDenominatorTicks
                    / (double)PulseRepeaterBaseline.CadenceNumeratorPerTick
                    / TickRate.TicksPerSecond,
                Is.EqualTo(PulseRepeaterBaseline.ActivationPeriodSeconds).Within(1e-12),
                "and the fraction converts back to the documented 0.375 s exactly. Rounding to 22 ticks "
                    + "would make the weapon 2.3% fast and to 23 ticks 2.2% slow, either of which is a "
                    + "balance change docs/71:28 forbids the tick rate from causing");
        });
    }

    [Test]
    public void AProjectilesLifetimeIsTheRangeDividedByTheSpeed()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                PulseRepeaterBaseline.ProjectileLifetimeTicks,
                Is.EqualTo(30),
                "8 m at 16 m/s is 0.5 s, or 30 ticks. docs/71:81 states a range and a speed and no "
                    + "separate lifetime, and this is the only lifetime those two jointly admit: longer "
                    + "would damage past the stated range, shorter would make the range unreachable");
            Assert.That(
                PulseRepeaterBaseline.ProjectileLifetimeTicks
                    * PulseRepeaterBaseline.ProjectileDisplacementPerTickMeters,
                Is.EqualTo(PulseRepeaterBaseline.TargetingRangeMeters).Within(1e-12),
                "and thirty ticks of travel is exactly the targeting range");
        });
    }
}
