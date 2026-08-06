using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// Proves that a domain event is never dropped, that the type has no path by which one could be, and that
/// a buffer holding an unconsumed record refuses release.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-006-001</c>, <c>VER-SIM-006-006</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "domain events may not be
/// dropped" and "Statistics consume domain/damage records before their buffers are released."
/// <c>CTR-SIM-001</c> in doc 115 § Cross-boundary contract registry: "never dropped", and on failure
/// "invariant failure ends run safely rather than omitting authoritative event".
/// </remarks>
[TestFixture]
internal sealed class DomainEventBufferTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-006-001</c>.
    ///
    /// Beyond its initial capacity the buffer grows and delivers every appended record, across several
    /// ticks; and a buffer that genuinely cannot accept one fails the tick invariant rather than discarding
    /// it.
    /// </summary>
    [Test]
    public void DomainEventsAreNeverDropped()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        DomainEventBuffer buffer = new(initialCapacity: 4, hardMaximumCapacity: 4096);

        List<DomainEvent> appendedAcrossRun = new();
        List<DomainEvent> deliveredAcrossRun = new();

        // Several ticks, each appending far past the initial capacity, so growth is exercised repeatedly
        // rather than once.
        for (long tick = 0; tick < 5; tick++)
        {
            buffer.BeginTick(tick);
            List<DomainEvent> appendedThisTick = new();
            int recordsThisTick = 7 + (int)(tick * 11);

            for (int index = 0; index < recordsThisTick; index++)
            {
                Assert.That(
                    allocator.TryAllocate(PopulationCategory.Pickup, out EntityId subject),
                    Is.True,
                    "the fixture needs an identity per record");
                DomainEvent record = EventFixture.Domain(
                    index % 2 == 0 ? EventFixture.EntityDefeated : EventFixture.ResourceAwarded,
                    tick,
                    systemPhase: 10,
                    sequence: index,
                    emitter: allocator.PlayerId,
                    subject: subject,
                    quantity: index + 1);
                buffer.Append(record);
                appendedThisTick.Add(record);
                Assert.That(allocator.TryFree(subject), Is.True);
            }

            DomainEvent[] batch = new DomainEvent[buffer.Count];
            int written = buffer.CopyOrderedTo(batch);
            Assert.That(
                written,
                Is.EqualTo(recordsThisTick),
                "the batch must carry every record appended in the tick");

            List<DomainEvent> deliveredThisTick = new(written);
            for (int index = 0; index < written; index++)
            {
                deliveredThisTick.Add(batch[index]);
            }

            EventContractAssertions.NoDomainEventWasLost(
                "the domain buffer on tick " + tick.ToString(CultureInfo.InvariantCulture),
                appendedThisTick,
                deliveredThisTick,
                appendedInRun: buffer.AppendedInRun,
                accountedInRun: buffer.ReleasedInRun + written);

            appendedAcrossRun.AddRange(appendedThisTick);
            deliveredAcrossRun.AddRange(deliveredThisTick);

            buffer.RecordAllConsumed();
            buffer.Release();
        }

        EventContractAssertions.NoDomainEventWasLost(
            "the domain buffer across the whole run",
            appendedAcrossRun,
            deliveredAcrossRun,
            appendedInRun: buffer.AppendedInRun,
            accountedInRun: buffer.ReleasedInRun);

        Expect.Multiple(() =>
        {
            Assert.That(
                buffer.GrowthCount,
                Is.GreaterThan(0),
                "the buffer must have grown, or the at-and-beyond-capacity case was never reached");
            Assert.That(
                buffer.Capacity,
                Is.GreaterThan(4),
                "growth must have enlarged the backing store rather than recycling it");
            Assert.That(buffer.Count, Is.EqualTo(0), "every tick was released");
            Assert.That(
                buffer.ReleasedInRun,
                Is.EqualTo(buffer.AppendedInRun),
                "the run-long totals must agree exactly: nothing appended went unaccounted");
        });

        AssertHardCeilingFailsTheTickRatherThanDiscarding();
    }

    /// <summary>
    /// Verification: <c>VER-SIM-006-006</c>.
    ///
    /// Releasing while a record is unconsumed fails the invariant instead of dropping it, and the consumed
    /// count equals the emitted count exactly before a release succeeds.
    /// </summary>
    [Test]
    public void BuffersAreNotReleasedWithUnconsumedRecords()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        DomainEventBuffer buffer = new(initialCapacity: 8, hardMaximumCapacity: 64);
        buffer.BeginTick(0);

        for (int index = 0; index < 3; index++)
        {
            Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId subject), Is.True);
            buffer.Append(EventFixture.Domain(
                EventFixture.EntityDefeated,
                tick: 0,
                systemPhase: 10,
                sequence: index,
                emitter: allocator.PlayerId,
                subject: subject,
                quantity: index + 1));
        }

        InvalidOperationException nothingConsumed = Expect.Throws<InvalidOperationException>(buffer.Release);
        buffer.RecordConsumed(2);
        InvalidOperationException partlyConsumed = Expect.Throws<InvalidOperationException>(buffer.Release);

        Expect.Multiple(() =>
        {
            Assert.That(
                nothingConsumed.Message,
                Does.Contain("would drop 3 authoritative event(s)"),
                "the refusal must say how many records a release would have dropped");
            Assert.That(
                partlyConsumed.Message,
                Does.Contain("would drop 1 authoritative event(s)"),
                "and must account for the records that were consumed");
            Assert.That(
                buffer.Count,
                Is.EqualTo(3),
                "a refused release must leave every record present, which is the point of refusing");
            Assert.That(buffer.ConsumedCount, Is.EqualTo(2));
            Assert.That(
                buffer.ReleasedInRun,
                Is.EqualTo(0L),
                "nothing has been released, so the run-long released total is still zero");
        });

        buffer.RecordConsumed(1);
        Expect.DoesNotThrow(buffer.Release);

        Expect.Multiple(() =>
        {
            Assert.That(buffer.Count, Is.EqualTo(0));
            Assert.That(buffer.ConsumedCount, Is.EqualTo(0), "a released buffer starts the next tick clean");
            Assert.That(buffer.IsOpenForTick, Is.False);
            Assert.That(
                buffer.ReleasedInRun,
                Is.EqualTo(buffer.AppendedInRun),
                "the consumed count equalled the emitted count exactly");
            Expect.Throws<ArgumentOutOfRangeException>(() => buffer.RecordConsumed(1));
        });

        // A tick cannot begin over an unreleased tick, so residue cannot be cleared by starting again.
        buffer.BeginTick(1);
        Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId leftover), Is.True);
        buffer.Append(EventFixture.Domain(
            EventFixture.ResourceAwarded,
            tick: 1,
            systemPhase: 11,
            sequence: 0,
            emitter: allocator.PlayerId,
            subject: leftover,
            quantity: 9));

        InvalidOperationException secondTick = Expect.Throws<InvalidOperationException>(() => buffer.BeginTick(2));
        Assert.That(
            secondTick.Message,
            Does.Contain("is still open"),
            "beginning a tick over an open one must be refused rather than clearing it");
    }

    /// <summary>
    /// Verification: supports <c>VER-SIM-006-001</c>.
    ///
    /// The type has no removal path other than a release that requires full consumption, so dropping a
    /// domain event is absent from its shape rather than merely untaken on the paths a test exercised.
    /// </summary>
    /// <remarks>
    /// A structural assertion over the declared public surface. It exists so that adding a <c>Discard</c>,
    /// a <c>Clear</c>, an eviction, or an overwrite-on-full to this type turns a test red even if nobody
    /// thinks to write a test for the new path - which is precisely the case a behavioural no-loss test
    /// cannot cover.
    /// </remarks>
    [Test]
    public void NoRemovalPathExistsBeyondAConsumedRelease()
    {
        MethodInfo[] declared = typeof(DomainEventBuffer).GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        List<string> names = new(declared.Length);
        foreach (MethodInfo method in declared)
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            names.Add(method.Name);
        }

        names.Sort(StringComparer.Ordinal);

        Expect.Multiple(() =>
        {
            Assert.That(
                names,
                Is.EqualTo(new[]
                {
                    "Append",
                    "BeginTick",
                    "CopyOrderedTo",
                    "RecordAllConsumed",
                    "RecordConsumed",
                    "Release",
                }),
                "DomainEventBuffer's public surface is fixed: appending, opening a tick, copying the whole "
                    + "batch, acknowledging consumption, and a release that refuses while anything is "
                    + "unconsumed. A new member here is a new way for an authoritative event to disappear, "
                    + "so it needs its own registry entry, not a quiet addition");
            Assert.That(
                typeof(DomainEventBuffer).GetFields(BindingFlags.Public | BindingFlags.Instance),
                Is.Empty,
                "no public field, so no backing store is reachable to write through");
        });
    }

    /// <summary>
    /// At the hard ceiling the tick fails its invariant with every appended record still present, rather
    /// than the buffer discarding one.
    /// </summary>
    private static void AssertHardCeilingFailsTheTickRatherThanDiscarding()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        DomainEventBuffer buffer = new(initialCapacity: 2, hardMaximumCapacity: 4);
        buffer.BeginTick(0);

        for (int index = 0; index < 4; index++)
        {
            Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId subject), Is.True);
            buffer.Append(EventFixture.Domain(
                EventFixture.EntityDefeated,
                tick: 0,
                systemPhase: 10,
                sequence: index,
                emitter: allocator.PlayerId,
                subject: subject,
                quantity: index + 1));
        }

        Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId refused), Is.True);
        DomainEvent overCeiling = EventFixture.Domain(
            EventFixture.EntityDefeated,
            tick: 0,
            systemPhase: 10,
            sequence: 4,
            emitter: allocator.PlayerId,
            subject: refused,
            quantity: 99);

        InvalidOperationException breach = Expect.Throws<InvalidOperationException>(
            () => buffer.Append(overCeiling));

        DomainEvent[] batch = new DomainEvent[buffer.Count];
        int written = buffer.CopyOrderedTo(batch);

        Expect.Multiple(() =>
        {
            Assert.That(
                breach.Message,
                Does.Contain("forbids dropping a domain event"),
                "the failure must name the rule it is upholding");
            Assert.That(
                breach.Message,
                Does.Contain("fails its invariant"),
                "and must say that the tick fails rather than the record being lost");
            Assert.That(
                buffer.Count,
                Is.EqualTo(4),
                "every record appended before the breach must still be present and inspectable, which is "
                    + "the opposite of omitting one");
            Assert.That(written, Is.EqualTo(4));
            Assert.That(
                buffer.AppendedInRun,
                Is.EqualTo(4L),
                "the refused append must not be counted as appended, so the totals stay reconcilable");
        });
    }
}
