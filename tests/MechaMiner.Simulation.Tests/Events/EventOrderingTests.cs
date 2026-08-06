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
        + "# documented stable ordering rather than collection or thread timing.\" For an\n"
        + "# event batch the keys are: tick, then emitting system phase, then explicit\n"
        + "# emission sequence. There is no fourth key.\n"
        + "#\n"
        + "# There is no fourth key because the emission sequence is per-tick global:\n"
        + "# CMP-SIM-003 issues it monotonically across the whole tick regardless of phase\n"
        + "# or emitter, so (tick, sequence) is already a total order. A duplicate sequence\n"
        + "# within a tick is therefore an impossible input - a defect in the issuer, not a\n"
        + "# tie - and a comparator that fell through to a further key on a duplicate would\n"
        + "# silently order it and hide the bug that produced it. The former fourth key, the\n"
        + "# full emitting entity ID, has been replaced by a live invariant: sequence is\n"
        + "# unique within a tick, and a duplicate fails loudly. See\n"
        + "# EventOrdering.AssertSequenceUniqueWithinTick.\n"
        + "#\n"
        + "# This scoping is events only. Doc 20 § Boundary and tie ordering defines a\n"
        + "# separate five-key sort for damage instances - system phase, explicit attack\n"
        + "# sequence, target ID, source ID, then insertion sequence - and that is untouched\n"
        + "# by the rule above.\n"
        + "#\n"
        + "# Fixture: run session 0xE7E00001, tick 7, eight domain events emitted by four\n"
        + "# system phases (3, 8, 10, 11) with explicit emission sequences that deliberately\n"
        + "# disagree with phase order, so sorting cannot be a no-op. The same eight events\n"
        + "# are appended in two different orders - ascending and reversed - and both batches\n"
        + "# must equal the text below.\n"
        + "#\n"
        + "# The stored phase numerals are correct by contract, not by coincidence: doc 10 §\n"
        + "# System phase ordering numbers a fixed fourteen phases and those numerals are\n"
        + "# stable normative identifiers. Renumbering is forbidden; a new phase takes the\n"
        + "# next unused number and a subdivision keeps its parent's. So a fixture may store\n"
        + "# 3, 8, 10, 11 and rely on their meaning.\n"
        + "#\n"
        + "# Every identity here - four emitters and the shared subject - is drawn from the\n"
        + "# OrdinaryEnemy partition, which begins immediately after Player. Player's\n"
        + "# capacity is the doc 20 § Scope and invariants invariant \"exactly one player\n"
        + "# entity exists until terminal resolution\", so the OrdinaryEnemy offset is 1 by\n"
        + "# simulation invariant and no doc 22 § Performance and capacity combat ceiling\n"
        + "# precedes it. The fixture asserts that computed containment rather than assuming\n"
        + "# it. The previous fixture drew its subject from Pickup, which sits after all\n"
        + "# three doc 22 ceilings, which is why the literal 3830 was baked in here and\n"
        + "# would have made this ordering golden fail when the enemy-projectile ceiling\n"
        + "# moved.\n"
        + "#\n"
        + "# Derived by: the documented rule read off doc 10 and doc 20, computed in an\n"
        + "# independent Python reference before any C# ran, and cross-checked against an\n"
        + "# independent list sort in EventOrderingTests. Not by accepting whatever the\n"
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

        // The subject is a fifth ordinary enemy, not a pickup. See
        // AssertEveryIdentityIsIndependentOfCombatCeilings for why that matters.
        Assert.That(
            allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject),
            Is.True);

        AssertEveryIdentityIsIndependentOfCombatCeilings(allocator, emitters, subject);

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

    /// <summary>
    /// Asserts that every absolute storage index this golden renders is computed from partitions no
    /// <c>docs/technical/22-combat-and-weapon-runtime.md</c> ceiling can move.
    /// </summary>
    /// <param name="allocator">The fixture's allocator.</param>
    /// <param name="emitters">The four emitting identities.</param>
    /// <param name="subject">The shared subject identity.</param>
    /// <remarks>
    /// <para>
    /// The golden renders absolute storage indices, because it renders through production
    /// <c>DomainEvent.ToString()</c> and pinning that rendering is worth keeping. The cost of an
    /// absolute index is that it moves whenever any partition above it moves, and doc 22 § Performance
    /// and capacity explicitly reserves the right to move three of them: "Profiling and legal
    /// maximum-output analysis must tighten or expand them before content complete."
    /// </para>
    /// <para>
    /// So the independence is asserted rather than hoped for. Every identity here comes from the
    /// OrdinaryEnemy partition, and every category the canonical order places above OrdinaryEnemy must
    /// carry an authority other than <see cref="CapacityAuthority.CombatRuntimeCeiling"/>. If someone
    /// later inserts a combat-ceilinged category above OrdinaryEnemy, this fails with a message about
    /// partition offsets - which is the diagnosis - instead of the golden failing with a byte diff,
    /// which is the symptom that gets a golden regenerated instead of investigated.
    /// </para>
    /// </remarks>
    private static void AssertEveryIdentityIsIndependentOfCombatCeilings(
        EntityIdAllocator allocator,
        EntityId[] emitters,
        EntityId subject)
    {
        int enemyOffset = allocator.SlotOffsetFor(PopulationCategory.OrdinaryEnemy);
        int enemyEnd = enemyOffset + allocator.CapacityFor(PopulationCategory.OrdinaryEnemy).HardCapacity;

        List<string> ceilingedAbove = new();
        int computedOffset = 0;
        foreach (PopulationCategory category in StoreCapacities.Categories)
        {
            if (category == PopulationCategory.OrdinaryEnemy)
            {
                break;
            }

            StoreCapacity capacity = allocator.CapacityFor(category);
            if (capacity.Authority == CapacityAuthority.CombatRuntimeCeiling)
            {
                ceilingedAbove.Add(category.ToString());
            }

            computedOffset += capacity.HardCapacity;
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                ceilingedAbove,
                Is.Empty,
                "no doc 22 § Performance and capacity ceiling may sit above the OrdinaryEnemy "
                    + "partition, or the absolute storage indices this golden renders move whenever "
                    + "doc 22 moves and an ordering golden fails for a capacity reason: "
                    + string.Join(", ", ceilingedAbove));
            Assert.That(
                enemyOffset,
                Is.EqualTo(computedOffset),
                "the partition offset must be the running sum of the capacity table above it, "
                    + "computed here rather than written down as a literal");
            Assert.That(
                subject.Index,
                Is.InRange(enemyOffset, enemyEnd - 1),
                "the subject must live in the OrdinaryEnemy partition");
            foreach (EntityId emitter in emitters)
            {
                Assert.That(
                    emitter.Index,
                    Is.InRange(enemyOffset, enemyEnd - 1),
                    "every emitter must live in the OrdinaryEnemy partition");
            }
        });
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
    /// <remarks>
    /// doc 91 § Reference models: agreement is then evidence about the rule, not about the
    /// implementation agreeing with itself. Three keys, matching the rule: the emission sequence is
    /// per-tick global, so no identity tiebreak follows it.
    /// </remarks>
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

            return left.Provenance.Sequence.CompareTo(right.Provenance.Sequence);
        });

        return sorted;
    }
}
