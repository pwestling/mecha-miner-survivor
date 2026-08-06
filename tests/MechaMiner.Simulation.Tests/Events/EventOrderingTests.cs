using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Verification: <c>VER-SIM-006-011</c>, <c>VER-SIM-006-008</c>. <c>VER-SIM-006-011</c> supersedes the
/// retired <c>VER-SIM-006-003</c>, which enumerated a three-key comparator - system phase, then
/// emission sequence, then the full entity ID - that no longer exists.
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
        + "# event batch the keys are: tick, then explicit emission sequence. That is all\n"
        + "# of them.\n"
        + "#\n"
        + "# Two keys, because the emission sequence is per-tick global: CMP-SIM-003 issues\n"
        + "# it monotonically across the whole tick regardless of phase or emitter, so\n"
        + "# (tick, sequence) is a total order on its own. Neither the emitting system phase\n"
        + "# nor the emitting entity ID can discriminate any legal pair, so neither is a\n"
        + "# sort key. Both survive as recorded provenance, and the two facts that made them\n"
        + "# redundant are now checked instead of assumed:\n"
        + "#\n"
        + "#   1. Sequence is unique within a tick. A duplicate is an impossible input - a\n"
        + "#      defect in the issuer, not a tie - so it fails loudly rather than falling\n"
        + "#      through to a further key that would silently order it and hide the bug.\n"
        + "#      Enforced by EventOrdering.AssertTotalOrder: with a two-key comparator two\n"
        + "#      records sharing a tick and a sequence compare equal, so the adjacency scan\n"
        + "#      is itself the uniqueness check.\n"
        + "#   2. Phase agrees with sequence. Sequence is assigned at emission and emission\n"
        + "#      happens in phase order, so along ascending sequence within one tick the\n"
        + "#      phase must be non-decreasing. This is precisely what makes phase redundant\n"
        + "#      as a sort key, so removing the key without checking the fact would have\n"
        + "#      thrown the fact away. A system emitting out of phase order is a real\n"
        + "#      defect and this names it. Enforced by\n"
        + "#      EventOrdering.AssertPhaseAgreesWithSequenceWithinTick.\n"
        + "#\n"
        + "# This scoping is events only. Doc 20 § Boundary and tie ordering defines a\n"
        + "# separate five-key sort for damage instances - system phase, explicit attack\n"
        + "# sequence, target ID, source ID, then insertion sequence - and that is untouched\n"
        + "# by the rule above.\n"
        + "#\n"
        + "# Fixture: run session 0xE7E00001, tick 7, eight domain events across four system\n"
        + "# phases (3, 8, 10, 11), two emissions per phase. Sequence ascends 0 to 7 and\n"
        + "# phase is non-decreasing with it, so the fixture is a legal input. The events\n"
        + "# are appended in sequence order 5,0,3,6,1,7,2,4 - and that batch is also\n"
        + "# published reversed - so neither append order is the documented order and the\n"
        + "# sort cannot be a no-op.\n"
        + "#\n"
        + "# The previous fixture achieved that same non-no-op property by pairing phase 11\n"
        + "# with sequence 0 and phase 3 with sequence 1, deliberately disagreeing with\n"
        + "# phase order. Under the contract above that is an input the system cannot\n"
        + "# produce, so the old golden pinned an impossible state. Every observable field\n"
        + "# here is now a function of the sequence rather than of the append position, so\n"
        + "# the rows stay derivable once append order stops matching them.\n"
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
        + "# it. An earlier fixture drew its subject from Pickup, which sits after all three\n"
        + "# doc 22 ceilings, which is why the literal 3830 was baked in here and would have\n"
        + "# made this ordering golden fail when the enemy-projectile ceiling moved.\n"
        + "#\n"
        + "# Derived by: the documented rule read off doc 10 and doc 20, computed in an\n"
        + "# independent Python reference before any C# ran, and cross-checked against an\n"
        + "# independent list sort in EventOrderingTests. Not by accepting whatever the\n"
        + "# buffer emitted.\n"
        + "#\n";

    /// <summary>
    /// The emitting phase each emission sequence belongs to, indexed by sequence.
    /// </summary>
    /// <remarks>
    /// Two emissions per phase across four phases, with phase non-decreasing as the sequence rises,
    /// which is the legality condition
    /// <c>EventOrdering.AssertPhaseAgreesWithSequenceWithinTick</c> enforces.
    /// </remarks>
    private static readonly int[] PhaseOfSequence = [3, 3, 8, 8, 10, 10, 11, 11];

    /// <summary>
    /// The order the fixture's events are appended in, as emission sequences.
    /// </summary>
    /// <remarks>
    /// Neither this order nor its reverse is sequence order, so the sort cannot be a no-op - which is
    /// what the fixture needs, and it no longer costs an illegal input to get.
    /// </remarks>
    private static readonly int[] AppendSequences = [5, 0, 3, 6, 1, 7, 2, 4];

    /// <summary>
    /// Verification: <c>VER-SIM-006-011</c> (successor to the retired <c>VER-SIM-006-003</c>).
    ///
    /// Two runs that emit the same events in different append order produce identical batches, ordered
    /// by tick then emission sequence; a duplicate sequence within a tick fails loudly; and phase must
    /// not decrease as the sequence rises.
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

        AssertTheFixtureIsALegalInput(events);
        AssertOutOfPhaseOrderEmissionIsRejected();

        GoldenText.Matches("events-simultaneous-ordering.txt", GoldenHeader + firstRendering);
    }

    /// <summary>
    /// Asserts the fixture satisfies both invariants the two-key comparator rests on, so the golden
    /// pins a state the simulation can actually produce.
    /// </summary>
    /// <param name="events">The fixture's events, in append order.</param>
    /// <remarks>
    /// The previous fixture failed this: it paired a phase-11 emission with sequence 0 and a phase-3
    /// emission with sequence 1. Checking legality here is what stops that recurring, since the
    /// property is easy to break while still producing a stable, golden-matching batch - a fixture can
    /// be perfectly deterministic and still describe nothing real.
    /// </remarks>
    private static void AssertTheFixtureIsALegalInput(List<DomainEvent> events)
    {
        List<DomainEvent> bySequence = ReferenceSort(events);
        HashSet<string> pairs = new(StringComparer.Ordinal);
        Expect.Multiple(() =>
        {
            // Both loops below are no-ops on an empty list, so an empty fixture would satisfy every
            // legality check here. Assert the fixture is non-empty rather than letting it be exempt.
            Assert.That(
                events,
                Is.Not.Empty,
                "the fixture must contain records, or its legality is unfalsifiable");

            foreach (DomainEvent record in events)
            {
                string pair = record.Provenance.Tick.ToString(CultureInfo.InvariantCulture)
                    + "/"
                    + record.Provenance.Sequence.ToString(CultureInfo.InvariantCulture);
                Assert.That(
                    pairs.Add(pair),
                    Is.True,
                    "the fixture must not contain a duplicate tick and sequence: " + pair);
            }

            for (int index = 1; index < bySequence.Count; index++)
            {
                Assert.That(
                    bySequence[index].Provenance.SystemPhase,
                    Is.GreaterThanOrEqualTo(bySequence[index - 1].Provenance.SystemPhase),
                    "the fixture's phase must not decrease as the emission sequence rises, or it is "
                        + "an input the simulation cannot produce and the golden pins an impossible "
                        + "state");
            }
        });
    }

    /// <summary>
    /// Proves the phase-agreement invariant can fail: a batch whose phase decreases as the sequence
    /// rises is rejected, naming both offending records.
    /// </summary>
    /// <remarks>
    /// This is the control for the invariant that replaced the phase sort key. Without it, removing
    /// phase from the comparator would have silently discarded the reason phase was removable, and
    /// nothing would notice a system emitting outside its phase.
    /// </remarks>
    private static void AssertOutOfPhaseOrderEmissionIsRejected()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId emitter), Is.True);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject), Is.True);

        DomainEventBuffer buffer = new(initialCapacity: 4, hardMaximumCapacity: 16);
        buffer.BeginTick(7);

        // Phase 11 emitted at sequence 0 and phase 3 at sequence 1: the sequences are unique, so the
        // uniqueness invariant is satisfied and this is specifically the phase check firing.
        buffer.Append(EventFixture.Domain(
            EventFixture.EntityDefeated, 7, 11, 0, emitter, subject, 1));
        buffer.Append(EventFixture.Domain(
            EventFixture.EntityDefeated, 7, 3, 1, emitter, subject, 2));

        DomainEvent[] batch = new DomainEvent[buffer.Count];
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => buffer.CopyOrderedTo(batch));

        Expect.Multiple(() =>
        {
            Assert.That(
                failure.Message,
                Does.Contain("phase must not decrease as the sequence rises"),
                "the phase-agreement invariant must be what failed, not the uniqueness check");
            Assert.That(
                failure.Message,
                Does.Contain("system phase 3"),
                "the failure must name the offending phase so the emitting system is findable");
            Assert.That(
                failure.Message,
                Does.Contain("from phase 11"),
                "and the phase it disagreed with");
        });
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
    /// Eight legal events across four phases, appended in an order that is neither the documented
    /// order nor its reverse.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every observable field is a function of the emission sequence - phase, emitter, kind, quantity,
    /// position - so the golden's rows stay derivable from the sequence alone once the append order
    /// stops matching them. <see cref="AppendSequences"/> supplies the append order and
    /// <see cref="PhaseOfSequence"/> the phase mapping.
    /// </para>
    /// <para>
    /// <b>This fixture is legal, and the previous one was not.</b> The earlier version paired phase 11
    /// with sequence 0 and phase 3 with sequence 1 so that "sorting cannot be a no-op". But the
    /// sequence is issued at emission and emission happens in phase order, so a phase-11 emission
    /// cannot precede a phase-3 emission in the same tick: that golden pinned a state the simulation
    /// cannot produce, which is worth no more than a golden that pins nothing. The non-no-op property
    /// is now obtained from the append order instead, which costs nothing and stays legal.
    /// </para>
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

        List<DomainEvent> events = new(AppendSequences.Length);
        foreach (int sequence in AppendSequences)
        {
            events.Add(EventFixture.Domain(
                sequence % 2 == 0 ? EventFixture.EntityDefeated : EventFixture.ResourceAwarded,
                tick: 7,
                systemPhase: PhaseOfSequence[sequence],
                sequence: sequence,
                emitter: emitters[sequence % emitters.Length],
                subject: subject,
                quantity: sequence + 1));
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
    /// implementation agreeing with itself. Two keys, matching the rule: the emission sequence is
    /// per-tick global, so neither phase nor identity follows it.
    /// </remarks>
    private static List<DomainEvent> ReferenceSort(List<DomainEvent> events)
    {
        List<DomainEvent> sorted = new(events);
        sorted.Sort((left, right) =>
        {
            int byTick = left.Provenance.Tick.CompareTo(right.Provenance.Tick);
            return byTick != 0
                ? byTick
                : left.Provenance.Sequence.CompareTo(right.Provenance.Sequence);
        });

        return sorted;
    }
}
