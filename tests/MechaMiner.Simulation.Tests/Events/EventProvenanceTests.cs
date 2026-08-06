using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// Proves that every event carries its complete provenance and cannot be constructed without it, and that
/// the pair of tick and sequence is a total order across a run.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-006-004</c>, <c>VER-SIM-006-005</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "Events carry tick,
/// sequence, stable event kind, relevant entity/content IDs, position, and typed payload."
/// </remarks>
[TestFixture]
internal sealed class EventProvenanceTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-006-004</c>.
    ///
    /// Every required field is present on a constructed event, and an event missing any one of them cannot
    /// be constructed rather than being emitted with a default.
    /// </summary>
    [Test]
    public void EveryEventCarriesItsCompleteProvenance()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject), Is.True);
        EntityId emitter = allocator.PlayerId;

        DomainEvent complete = DomainEvent.Create(
            EventFixture.EntityDefeated,
            EventProvenance.Create(42, 10, 7, emitter, "E-SKITTERLING"),
            subject,
            positionX: 12.5,
            positionY: -3.25,
            EventPayload.Typed(EventPayload.InitialSchemaVersion, 40, 0.75, "R-COMMON-ORE"));

        PresentationEvent presentation = PresentationEvent.Create(
            EventFixture.HitConfirmed,
            EventProvenance.Create(42, 8, 6, emitter, "W-AUTOCANNON"),
            subject,
            positionX: 12.5,
            positionY: -3.25,
            EventPayload.Typed(EventPayload.InitialSchemaVersion, 12, 0.0, EventPayload.NoContentId));

        Expect.Multiple(() =>
        {
            // tick, sequence, stable event kind, entity IDs, content ID, position, typed payload.
            Assert.That(complete.Provenance.Tick, Is.EqualTo(42L), "tick");
            Assert.That(complete.Provenance.Sequence, Is.EqualTo(7L), "sequence");
            Assert.That(complete.Provenance.SystemPhase, Is.EqualTo(10), "emitting system phase");
            Assert.That(complete.Kind, Is.EqualTo(EventFixture.EntityDefeated), "stable event kind");
            Assert.That(complete.Kind.StableId, Is.EqualTo(1001), "and the kind's stable numeric identity");
            Assert.That(complete.Provenance.EmittingEntityId, Is.EqualTo(emitter), "emitting entity ID");
            Assert.That(complete.SubjectId, Is.EqualTo(subject), "subject entity ID");
            Assert.That(
                complete.Provenance.SourceContentId,
                Is.EqualTo("E-SKITTERLING"),
                "source content ID");
            Assert.That(complete.PositionX, Is.EqualTo(12.5), "position X");
            Assert.That(complete.PositionY, Is.EqualTo(-3.25), "position Y");
            Assert.That(complete.Payload.SchemaVersion, Is.EqualTo(1), "typed payload schema version");
            Assert.That(complete.Payload.Quantity, Is.EqualTo(40L), "payload quantity");
            Assert.That(complete.Payload.ContentId, Is.EqualTo("R-COMMON-ORE"), "payload content reference");
            Assert.That(complete.IsComplete, Is.True);
            Assert.That(presentation.IsComplete, Is.True);
            Assert.That(
                presentation.SourceEventCount,
                Is.EqualTo(1),
                "a verbatim presentation event stands for one emission");
        });

        AssertIncompleteEventsCannotBeConstructed(emitter, subject);
    }

    /// <summary>
    /// Verification: <c>VER-SIM-006-005</c>.
    ///
    /// Sequence is strictly increasing within a tick, its per-tick origin is asserted rather than assumed,
    /// and no two events in a run share the same (tick, sequence) pair.
    /// </summary>
    [Test]
    public void TickAndSequenceFormATotalOrder()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject), Is.True);

        // CMP-SIM-003 owns the sequence, so it is drawn from the publisher, not invented per call site.
        SnapshotPublisher publisher = new(
            EventFixture.RunSession,
            visibleEntityCapacity: 4,
            domainEventCapacity: 64,
            presentationEventCapacity: 64);

        HashSet<string> seenPairs = new(StringComparer.Ordinal);
        List<long> firstSequenceOfEachTick = new();
        List<long> allSequences = new();

        for (long tick = 0; tick < 4; tick++)
        {
            publisher.BeginTick(tick);
            long previousSequence = -1;

            for (int emission = 0; emission < 5; emission++)
            {
                long sequence = publisher.NextEventSequence();
                if (emission == 0)
                {
                    firstSequenceOfEachTick.Add(sequence);
                }

                Assert.That(
                    sequence,
                    Is.GreaterThan(previousSequence),
                    "the sequence must strictly increase within a tick");
                previousSequence = sequence;

                DomainEvent record = EventFixture.Domain(
                    EventFixture.EntityDefeated,
                    tick,
                    systemPhase: 10,
                    sequence: sequence,
                    emitter: allocator.PlayerId,
                    subject: subject,
                    quantity: emission + 1);

                string pair = record.Provenance.Tick.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "/"
                    + record.Provenance.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Assert.That(
                    seenPairs.Add(pair),
                    Is.True,
                    "no two events in one run may share a tick and sequence: " + pair);
                allSequences.Add(sequence);
            }

            publisher.InvalidateTick("the fixture only needs the sequence allocator");
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                firstSequenceOfEachTick,
                Is.All.EqualTo(0L),
                "the documented per-tick origin: the sequence restarts at zero each tick, because the pair "
                    + "of tick and sequence - not the sequence alone - is what forms the run-long order");
            Assert.That(seenPairs, Has.Count.EqualTo(allSequences.Count), "every pair was distinct");
            Assert.That(seenPairs, Has.Count.EqualTo(20));
            Expect.Throws<InvalidOperationException>(
                () => publisher.NextEventSequence());
        });

        AssertDuplicateSequencesFailTheBatch(allocator, subject);
    }

    private static void AssertIncompleteEventsCannotBeConstructed(EntityId emitter, EntityId subject)
    {
        EventProvenance provenance = EventProvenance.Create(1, 10, 0, emitter, "E-SKITTERLING");
        EventPayload payload = EventPayload.Typed(1, 0, 0.0, EventPayload.NoContentId);

        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentException>(
                () => DomainEvent.Create(default, provenance, subject, 0.0, 0.0, payload));
            Expect.Throws<ArgumentException>(
                () => DomainEvent.Create(EventFixture.EntityDefeated, default, subject, 0.0, 0.0, payload));
            Expect.Throws<ArgumentException>(
                () => DomainEvent.Create(EventFixture.EntityDefeated, provenance, subject, 0.0, 0.0, default));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => DomainEvent.Create(
                    EventFixture.EntityDefeated, provenance, EntityId.Unset, 0.0, 0.0, payload));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => DomainEvent.Create(
                    EventFixture.EntityDefeated, provenance, subject, double.NaN, 0.0, payload));

            Expect.Throws<ArgumentOutOfRangeException>(
                () => EventProvenance.Create(-1, 10, 0, emitter, "E-X"));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => EventProvenance.Create(1, 0, 0, emitter, "E-X"));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => EventProvenance.Create(1, 15, 0, emitter, "E-X"));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => EventProvenance.Create(1, 10, -1, emitter, "E-X"));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => EventProvenance.Create(1, 10, 0, EntityId.Unset, "E-X"));
            Expect.Throws<ArgumentException>(
                () => EventProvenance.Create(1, 10, 0, emitter, "  "));

            Expect.Throws<ArgumentException>(
                () => EventPayload.Typed(1, 0, 0.0, "  "));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => EventPayload.Typed(0, 0, 0.0, EventPayload.NoContentId));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => EventPayload.Typed(1, 0, double.PositiveInfinity, EventPayload.NoContentId));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => EventKind.Declare(0, "zero-id"));
            Expect.Throws<ArgumentException>(
                () => EventKind.Declare(1, " "));

            // A run-scoped event says "no entity" explicitly rather than defaulting.
            EntityId noEntity = EntityId.NoEntityIn(EventFixture.RunSession);
            Expect.DoesNotThrow(() => DomainEvent.Create(
                EventFixture.RunTerminal,
                EventProvenance.Create(1, 13, 0, noEntity, "RUN"),
                noEntity,
                0.0,
                0.0,
                payload));
        });
    }

    /// <summary>
    /// A batch containing two events with the same tick and sequence fails, by every route a duplicate
    /// can arrive through and not only the one that happens to land adjacent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Checked over the resulting batch rather than at the append that caused it: a duplicate sequence is
    /// invisible until the batch is assembled, and the batch is where the ambiguity would become
    /// observable.
    /// </para>
    /// <para>
    /// <b>Three routes, because this check used to intercept only one of them.</b> The comparison ends at
    /// the emission sequence, so two records sharing a tick, a phase, and a sequence compare equal and the
    /// adjacency scan sees them. Two other shapes do not land adjacent, and an earlier revision of this
    /// test - one duplicate, same phase, same emitter - exercised neither:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// Same phase, <em>different emitters</em>. While the comparison still ended with the emitting entity
    /// ID, this pair did not compare equal: the removed tiebreak gave the duplicate an order and the batch
    /// passed. The old fixture only failed because both its records happened to share an emitter.
    /// </description></item>
    /// <item><description>
    /// <em>Different phases</em>. The batch sorts by phase before sequence, so a sequence issued twice to
    /// two phases lands in two separate blocks and no adjacency scan can reach it.
    /// <c>EventOrdering.AssertSequenceUniqueWithinTick</c> merges the blocks by sequence for exactly this.
    /// </description></item>
    /// <item><description>Same phase, same emitter - the original case, kept.</description></item>
    /// </list>
    /// </remarks>
    private static void AssertDuplicateSequencesFailTheBatch(EntityIdAllocator allocator, EntityId subject)
    {
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId other), Is.True);

        AssertDuplicateSequenceFailsThroughOneRoute(
            "the same phase and two different emitters, which the removed entity-ID tiebreak used to "
                + "give an order",
            firstPhase: 10,
            secondPhase: 10,
            firstEmitter: allocator.PlayerId,
            secondEmitter: other,
            subject);

        AssertDuplicateSequenceFailsThroughOneRoute(
            "two different phases, which no adjacency scan over a phase-sorted batch can reach",
            firstPhase: 3,
            secondPhase: 11,
            firstEmitter: allocator.PlayerId,
            secondEmitter: allocator.PlayerId,
            subject);

        AssertDuplicateSequenceFailsThroughOneRoute(
            "the same phase and the same emitter, where the records compare equal outright",
            firstPhase: 10,
            secondPhase: 10,
            firstEmitter: allocator.PlayerId,
            secondEmitter: allocator.PlayerId,
            subject);
    }

    /// <summary>
    /// Appends two records that share tick 0 and emission sequence 5 through one arrival shape, and
    /// asserts the batch refuses them with a diagnosable message.
    /// </summary>
    private static void AssertDuplicateSequenceFailsThroughOneRoute(
        string route,
        int firstPhase,
        int secondPhase,
        EntityId firstEmitter,
        EntityId secondEmitter,
        EntityId subject)
    {
        DomainEventBuffer buffer = new(initialCapacity: 4, hardMaximumCapacity: 16);
        buffer.BeginTick(0);
        buffer.Append(EventFixture.Domain(
            EventFixture.EntityDefeated,
            tick: 0,
            systemPhase: firstPhase,
            sequence: 5,
            emitter: firstEmitter,
            subject: subject,
            quantity: 1));
        buffer.Append(EventFixture.Domain(
            EventFixture.EntityDefeated,
            tick: 0,
            systemPhase: secondPhase,
            sequence: 5,
            emitter: secondEmitter,
            subject: subject,
            quantity: 2));

        DomainEvent[] batch = new DomainEvent[buffer.Count];
        InvalidOperationException tie = Expect.Throws<InvalidOperationException>(
            () => buffer.CopyOrderedTo(batch));

        Expect.Multiple(() =>
        {
            Assert.That(
                tie.Message,
                Does.Contain("share tick 0 and emission sequence 5"),
                "a duplicate arriving through " + route
                    + " must be refused, and the failure must name the duplicated pair so the emitting "
                    + "system is findable");
            Assert.That(
                tie.Message,
                Does.Contain("CMP-SIM-003 owns the sequence"),
                "and must name who is responsible for issuing each sequence once");
        });
    }
}
