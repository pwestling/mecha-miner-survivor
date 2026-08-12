using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Combat;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Combat;

/// <summary>
/// The attack schedule: exact 2.6667 activations per second from integer arithmetic.
/// </summary>
/// <remarks>Verification: <c>VER-COM-005-002</c>.</remarks>
[TestFixture]
internal sealed class WeaponScheduleTests
{
    [Test]
    public void TheScheduleBeginsReady()
    {
        Assert.That(
            WeaponSchedule.Ready.IsReady,
            Is.True,
            "docs/71:48 - 'Burst 10 assumes the weapon begins ready to attack', which is the assumption "
                + "the catalog's own burst column is computed under");
    }

    [Test]
    public void ActivationsFallOnAlternatingTwentyTwoAndTwentyThreeTickGaps()
    {
        WeaponSchedule schedule = WeaponSchedule.Ready;
        List<int> firedAt = new();

        for (int tick = 0; tick < 200; tick++)
        {
            if (tick > 0)
            {
                schedule = schedule.Advanced();
            }

            if (schedule.IsReady)
            {
                firedAt.Add(tick);
                schedule = schedule.Fired();
            }
        }

        List<int> gaps = new();
        for (int index = 1; index < firedAt.Count; index++)
        {
            gaps.Add(firedAt[index] - firedAt[index - 1]);
        }

        Expect.Multiple(() =>
        {
            Assert.That(firedAt[0], Is.EqualTo(0), "the first activation is on the first tick");
            Assert.That(
                gaps,
                Is.All.InRange(22, 23),
                "every gap is 22 or 23 ticks. A single fixed gap of either would be the rounding "
                    + "docs/71:28 forbids");
            Assert.That(
                gaps,
                Has.Some.EqualTo(22).And.Some.EqualTo(23),
                "and BOTH occur, which is what makes the average 22.5 rather than one rounded value");
        });
    }

    [Test]
    public void SixtySecondsProducesTheDocumentedNumberOfActivations()
    {
        WeaponSchedule schedule = WeaponSchedule.Ready;
        int activations = 0;

        for (int tick = 0; tick < 3600; tick++)
        {
            if (tick > 0)
            {
                schedule = schedule.Advanced();
            }

            if (!schedule.IsReady)
            {
                continue;
            }

            activations++;
            schedule = schedule.Fired();
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                activations,
                Is.EqualTo(160),
                "3,600 ticks is 60 s, and the schedule fires on ticks 45k and 45k+23 for k = 0..79. A "
                    + "weapon rounded to 22 ticks fires 164 times and one rounded to 23 fires 157, so this "
                    + "count distinguishes the exact schedule from both roundings - and 157 is what the "
                    + "first version of this accumulator produced, because capping credit at exactly one "
                    + "activation discarded the half-tick");
            Assert.That(
                activations * PulseRepeaterBaseline.DamagePerProjectile / 60.0,
                Is.EqualTo(32.0).Within(0.01),
                "and 160 activations of 12 damage over 60 s is exactly the 32.0 'Sustained 30' figure "
                    + "docs/71:57 states for this weapon");
        });
    }

    [Test]
    public void IdleCreditDoesNotAccumulatePastOneActivation()
    {
        WeaponSchedule schedule = WeaponSchedule.Ready;
        for (int tick = 0; tick < 600; tick++)
        {
            schedule = schedule.Advanced();
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                schedule.Credit,
                Is.EqualTo(WeaponSchedule.MaximumCredit),
                "ten seconds with no target leaves one activation plus the largest remainder a tick can "
                    + "carry - 46, not 26 activations' worth. Capping at exactly 45 instead was a measured "
                    + "defect: it threw the half-tick away and made every gap 23 ticks");
            Assert.That(
                schedule.Fired().IsReady,
                Is.False,
                "so meeting the first enemy produces one projectile and then the ordinary cadence, rather "
                    + "than a stored burst no document describes");
        });
    }

    [Test]
    public void FiringWhileNotReadyIsRefusedRatherThanGoingNegative()
    {
        WeaponSchedule spent = WeaponSchedule.Ready.Fired();

        Expect.Multiple(() =>
        {
            Assert.That(spent.IsReady, Is.False);
            Assert.That(
                Expect.Throws<InvalidOperationException>(() => spent.Fired()).Message,
                Does.Contain("a function of how often the caller asked"),
                "a schedule that let credit go negative would make the weapon's rate depend on the call "
                    + "pattern rather than on its attack-rate stat");
        });
    }
}
