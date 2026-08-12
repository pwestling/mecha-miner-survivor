using System;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Encounters;

/// <summary>
/// The two contact intervals: 0.75 seconds per enemy and 0.20 seconds for the mech.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-ENC-001-003</c>.
/// </para>
/// <para>
/// The pair matters more than either number. Only the repeat interval and a crowd of a hundred deals
/// five hundred damage in one tick; only the grace and one Skitterling deals 25 Hull per second forever.
/// Two of the cases below are exactly those two failures, driven as sequences rather than asserted as
/// constants, because a constant check cannot tell whether both rules are wired.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class ContactDamageCadenceTests
{
    [Test]
    public void BothIntervalsAreWholeNumbersOfTicksAtSixtyHertz()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                ContactDamageCadence.RepeatIntervalTicks,
                Is.EqualTo(45),
                "0.75 s (docs/72:42) at 60 Hz is 45 ticks exactly");
            Assert.That(
                ContactDamageCadence.GraceTicks,
                Is.EqualTo(12),
                "0.20 s (docs/72:43) at 60 Hz is 12 ticks exactly");
            Assert.That(
                ContactDamageCadence.RepeatIntervalTicks
                    / (double)TickRate.TicksPerSecond,
                Is.EqualTo(ContactDamageCadence.RepeatIntervalSeconds).Within(1e-12),
                "and the tick count converts back to the documented duration with no rounding, so no "
                    + "silent rounding direction was chosen");
            Assert.That(
                ContactDamageCadence.GraceTicks / (double)TickRate.TicksPerSecond,
                Is.EqualTo(ContactDamageCadence.GraceSeconds).Within(1e-12));
        });
    }

    [Test]
    public void AFirstOverlapIsEligibleImmediately()
    {
        Assert.That(
            ContactDamageCadence.IsEligible(
                tick: 0,
                lastGlobalContactTick: ContactDamageCadence.NeverContacted,
                lastContactFromThisEnemyTick: ContactDamageCadence.NeverContacted),
            Is.True,
            "docs/31:27 - an overlapping enemy 'deals its listed contact damage immediately when "
                + "eligible'. There is no wind-up");
    }

    [Test]
    public void TheSameEnemyRepeatsOnExactlyTheFortyFifthTickAndNotTheFortyFourth()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                ContactDamageCadence.IsEligible(44, ContactDamageCadence.NeverContacted, 0),
                Is.False,
                "44 ticks after the last instance is 0.7333 s, which is short of 0.75");
            Assert.That(
                ContactDamageCadence.IsEligible(45, ContactDamageCadence.NeverContacted, 0),
                Is.True,
                "45 ticks is 0.75 s exactly, and requiring a 46th would make the real interval 0.7667 s "
                    + "- a 2.2% damage shortfall over a sustained overlap");
        });
    }

    [Test]
    public void TheGraceBlocksADifferentEnemyForExactlyTwelveTicks()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                ContactDamageCadence.IsEligible(
                    tick: 11,
                    lastGlobalContactTick: 0,
                    lastContactFromThisEnemyTick: ContactDamageCadence.NeverContacted),
                Is.False,
                "docs/31:28 - 'Other enemies cannot deal another contact instance during that grace', "
                    + "even one that has never touched the mech");
            Assert.That(
                ContactDamageCadence.IsEligible(
                    tick: 12,
                    lastGlobalContactTick: 0,
                    lastContactFromThisEnemyTick: ContactDamageCadence.NeverContacted),
                Is.True,
                "0.20 s later it is eligible again");
        });
    }

    [Test]
    public void ACrowdOfAnySizeCannotExceedOneInstanceEveryTwelveTicks()
    {
        // The failure this case exists for: with only the per-enemy interval implemented, a hundred
        // overlapping Skitterlings would each be eligible on tick 0 and deal 500 damage at once.
        const int crowd = 100;
        long[] perEnemyLastContact = new long[crowd];
        Array.Fill(perEnemyLastContact, ContactDamageCadence.NeverContacted);

        long globalLast = ContactDamageCadence.NeverContacted;
        int instances = 0;
        const int ticks = 600;

        for (long tick = 0; tick < ticks; tick++)
        {
            for (int enemy = 0; enemy < crowd; enemy++)
            {
                if (!ContactDamageCadence.IsEligible(tick, globalLast, perEnemyLastContact[enemy]))
                {
                    continue;
                }

                instances++;
                globalLast = tick;
                perEnemyLastContact[enemy] = tick;
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                instances,
                Is.EqualTo(50),
                "600 ticks is 10 s and the grace admits one instance every 12 ticks, so 50 is the ceiling "
                    + "for a crowd of any size. With only the per-enemy interval, the same loop yields "
                    + "1,400");
            Assert.That(
                instances * EnemyRoster.SkitterlingContactDamage / 10.0,
                Is.EqualTo(25.0).Within(1e-12),
                "which is 25 Hull per second - the intake docs/72:25's 'Two maximum-strength standard "
                    + "damage instances cannot kill a full fresh mech' is written against");
        });
    }

    [Test]
    public void OnePursuerAloneIsCappedByItsOwnIntervalAndNotByTheGrace()
    {
        // The mirror failure: with only the grace implemented, one Skitterling would deal an instance
        // every 12 ticks, which is the crowd rate from a single fragile body.
        long globalLast = ContactDamageCadence.NeverContacted;
        long thisEnemyLast = ContactDamageCadence.NeverContacted;
        int instances = 0;

        for (long tick = 0; tick < 600; tick++)
        {
            if (!ContactDamageCadence.IsEligible(tick, globalLast, thisEnemyLast))
            {
                continue;
            }

            instances++;
            globalLast = tick;
            thisEnemyLast = tick;
        }

        Assert.That(
            instances,
            Is.EqualTo(14),
            "600 ticks at one instance every 45 gives 14 (ticks 0, 45, 90 ... 585). With only the grace "
                + "the same loop yields 50, which is 3.6 times the damage from one body");
    }

    [Test]
    public void ANegativeTickIsRefusedRatherThanTreatedAsNeverContacted()
    {
        Assert.That(
            Expect.Throws<ArgumentOutOfRangeException>(
                () => ContactDamageCadence.IsEligible(-1, 0, 0)).ParamName,
            Is.EqualTo("tick"),
            "the sentinel for 'never' is negative, so accepting a negative CURRENT tick would make the "
                + "elapsed-interval arithmetic compare two different things");
    }
}
