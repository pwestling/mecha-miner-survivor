using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// Proves that simultaneous events use the documented stable ordering, and that buffers are tick-local and
/// start empty.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-006-003</c>, <c>VER-SIM-006-008</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering: "Simultaneous outcomes use
/// documented stable ordering rather than collection or thread timing."
/// <c>docs/technical/20-simulation-core.md</c> § Boundary and tie ordering and § Tick transaction.
/// </remarks>
[TestFixture]
internal sealed class EventOrderingTests
{
    /// <summary>
    /// The provenance header committed with <c>events-simultaneous-ordering.txt</c>.
    /// </summary>
    /// <remarks>
    /// doc 91 § Determinism and fixture policy: "Golden outputs are canonical, ordered, and reviewable
    /// text". A reviewer needs the rule and the fixture, not only the bytes.
    /// </remarks>
    private const string GoldenHeader =
        "# events-simultaneous-ordering\n"
        + "#\n"
        + "# Rule under test: doc 10 § System phase ordering - \"Simultaneous outcomes use\n"
        + "# documented stable ordering rather than collection or thread timing\" - and doc 20\n"
        + "# § Entity identity - \"Stable ordering uses the full entity ID after a system's\n"
        + "# authored priority keys.\" For an event batch the keys are: tick, then emitting\n"
        + "# system phase, then explicit emission sequence, then the full emitting entity ID.\n"
        + "#\n"
        + "# Fixture: run session 0xE7E00001, tick 7, eight domain events emitted by four\n"
        + "# system phases (3, 8, 10, 11) with explicit emission sequences that deliberately\n"
        + "# disagree with phase order, so sorting cannot be a no-op. The same eight events are\n"
        + "# appended in two different orders - ascending and reversed - and both batches must\n"
        + "# equal the text below.\n"
        + "#\n"
        + "# Derived by: the documented rule read off doc 10 and doc 20, cross-checked against\n"
        + "# an independent list sort in EventOrderingTests, not by accepting whatever the\n"
        + "# buffer emitted.\n"
        + "#\n";

    /// <summary>
    /// Verification: <c>VER-SIM-006-003</c>.
    ///
    /// Two runs that emit the same events in different append order produce identical batches, ordered by
    /// system phase, then emission sequence, then the full entity ID.
    /// </summary>
    [Test]
    public void SimultaneousEventsUseDocumentedStableOrdering()
    {
        List<DomainEvent> events = BuildSimultaneousEvents();

        List<DomainEvent> ascending = new(events);
        List<DomainEvent> reversed = new(events);
        reversed.Reverse();

        string firstRendering = EventContractAssertions.RenderDomainBatch(PublishOrdered(ascending));
        string secondRendering = EventContractAssertions.RenderDomainBatch(PublishOrdered(reversed));
        string expectedRendering = EventContractAssertions.RenderDomainBatch(ReferenceSort(events));

        EventContractAssertions.BatchOrderMatchesTheDocumentedComparison(
            "the domain event batch",
            expectedRendering,
            firstRendering,
            secondRendering);

        Expect.Multiple(() =>
        {
            Assert.That(
                firstRendering,
                Is.Not.EqualTo(EventContractAssertions.RenderDomainBatch(ascending)),
                "the fixture's append order must differ from the documented order, or sorting is a no-op "
                    + "and the gate proves nothing");
            Assert.That(
                firstRendering,
                Is.Not.EqualTo(EventContractAssertions.RenderDomainBatch(reversed)),
                "and it must differ from the reversed append order too");
        });

        GoldenText.Matches("events-simultaneous-ordering.txt", GoldenHeader + firstRendering);
    }

    /// <summary>
    /// Verification: <c>VER-SIM-006-008</c>.
    ///
    /// Every buffer is empty at the start of each tick, an event appended during tick N never appears in
    /// tick N+1's batch, and an event belonging to another tick cannot be appended at all.
    /// </summary>
    [Test]
    public void BuffersAreTickLocalAndStartEmpty()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject), Is.True);
        DomainEventBuffer domain = new(initialCapacity: 4, hardMaximumCapacity: 64);
        PresentationEventBuffer presentation = new(initialCapacity: 4, hardMaximumCapacity: 64);

        Expect.Multiple(() =>
        {
            Assert.That(domain.Count, Is.EqualTo(0), "a fresh domain buffer is empty");
            Assert.That(presentation.Count, Is.EqualTo(0), "a fresh presentation buffer is empty");
            Assert.That(domain.IsOpenForTick, Is.False, "and neither is open for a tick");
            Assert.That(presentation.IsOpenForTick, Is.False);
            Expect.Throws<InvalidOperationException>(
                () => domain.Append(EventFixture.Domain(
                    EventFixture.EntityDefeated, 0, 10, 0, allocator.PlayerId, subject, 1)));
        });

        DomainEvent tickZeroRecord = EventFixture.Domain(
            EventFixture.EntityDefeated, 0, 10, 0, allocator.PlayerId, subject, 1);

        domain.BeginTick(0);
        presentation.BeginTick(0);
        Assert.That(domain.Count, Is.EqualTo(0), "opening a tick starts empty");
        Assert.That(presentation.Count, Is.EqualTo(0));

        domain.Append(tickZeroRecord);
        Assert.That(
            presentation.TryAppend(EventFixture.Presentation(
                EventFixture.HitConfirmed, 0, 8, 1, allocator.PlayerId, subject, 1)),
            Is.True);

        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentException>(
                () => domain.Append(EventFixture.Domain(
                    EventFixture.EntityDefeated, 1, 10, 2, allocator.PlayerId, subject, 2)));
            Expect.Throws<ArgumentException>(
                () => presentation.TryAppend(EventFixture.Presentation(
                    EventFixture.HitConfirmed, 1, 8, 3, allocator.PlayerId, subject, 2)));
        });

        DomainEvent[] tickZeroBatch = new DomainEvent[domain.Count];
        int tickZeroWritten = domain.CopyOrderedTo(tickZeroBatch);
        domain.RecordAllConsumed();
        domain.Release();
        presentation.Release();

        domain.BeginTick(1);
        presentation.BeginTick(1);

        DomainEvent[] tickOneBatch = new DomainEvent[8];
        int tickOneWritten = domain.CopyOrderedTo(tickOneBatch);

        Expect.Multiple(() =>
        {
            Assert.That(tickZeroWritten, Is.EqualTo(1), "tick zero published its one record");
            Assert.That(tickZeroBatch[0], Is.EqualTo(tickZeroRecord));
            Assert.That(domain.Count, Is.EqualTo(0), "tick one starts empty");
            Assert.That(presentation.Count, Is.EqualTo(0));
            Assert.That(
                tickOneWritten,
                Is.EqualTo(0),
                "an event appended during tick zero must not appear in tick one's batch");
            Assert.That(domain.Tick, Is.EqualTo(1L));
            Assert.That(presentation.Tick, Is.EqualTo(1L));
        });

        domain.Release();
        presentation.Release();
    }

    /// <summary>
    /// Eight events across four phases whose emission sequences deliberately disagree with phase order.
    /// </summary>
    /// <remarks>
    /// Sequences increase across the whole tick because <c>CMP-SIM-003</c> issues them once per tick, but the
    /// phases are shuffled relative to them, so the batch cannot be produced by sorting on either key alone.
    /// </remarks>
    private static List<DomainEvent> BuildSimultaneousEvents()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        EntityId[] emitters = new EntityId[4];
        for (int index = 0; index < emitters.Length; index++)
        {
            Assert.That(
                allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out emitters[index]),
                Is.True);
        }

        Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId subject), Is.True);

        int[] phases = [11, 3, 8, 10, 3, 11, 8, 10];
        List<DomainEvent> events = new(phases.Length);
        for (int index = 0; index < phases.Length; index++)
        {
            events.Add(EventFixture.Domain(
                index % 2 == 0 ? EventFixture.EntityDefeated : EventFixture.ResourceAwarded,
                tick: 7,
                systemPhase: phases[index],
                sequence: index,
                emitter: emitters[index % emitters.Length],
                subject: subject,
                quantity: index + 1));
        }

        return events;
    }

    private static List<DomainEvent> PublishOrdered(List<DomainEvent> appendOrder)
    {
        DomainEventBuffer buffer = new(initialCapacity: 2, hardMaximumCapacity: 256);
        buffer.BeginTick(appendOrder[0].Provenance.Tick);
        foreach (DomainEvent record in appendOrder)
        {
            buffer.Append(record);
        }

        DomainEvent[] batch = new DomainEvent[buffer.Count];
        int written = buffer.CopyOrderedTo(batch);
        buffer.RecordAllConsumed();
        buffer.Release();

        List<DomainEvent> result = new(written);
        for (int index = 0; index < written; index++)
        {
            result.Add(batch[index]);
        }

        return result;
    }

    /// <summary>
    /// Sorts by the documented keys with a deliberately simple list sort, independently of
    /// <c>EventOrdering</c>.
    /// </summary>
    /// <remarks>doc 91 § Reference models: agreement is then evidence about the rule, not about the implementation agreeing with itself.</remarks>
    private static List<DomainEvent> ReferenceSort(List<DomainEvent> events)
    {
        List<DomainEvent> sorted = new(events);
        sorted.Sort((left, right) =>
        {
            int byTick = left.Provenance.Tick.CompareTo(right.Provenance.Tick);
            if (byTick != 0)
            {
                return byTick;
            }

            int byPhase = left.Provenance.SystemPhase.CompareTo(right.Provenance.SystemPhase);
            if (byPhase != 0)
            {
                return byPhase;
            }

            int bySequence = left.Provenance.Sequence.CompareTo(right.Provenance.Sequence);
            if (bySequence != 0)
            {
                return bySequence;
            }

            int bySession = left.Provenance.EmittingEntityId.RunSession.CompareTo(
                right.Provenance.EmittingEntityId.RunSession);
            if (bySession != 0)
            {
                return bySession;
            }

            int byIndex = left.Provenance.EmittingEntityId.Index.CompareTo(
                right.Provenance.EmittingEntityId.Index);
            return byIndex != 0
                ? byIndex
                : left.Provenance.EmittingEntityId.Generation.CompareTo(
                    right.Provenance.EmittingEntityId.Generation);
        });

        return sorted;
    }
}
