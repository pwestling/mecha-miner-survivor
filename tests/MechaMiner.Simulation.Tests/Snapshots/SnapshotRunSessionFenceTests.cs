using System;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// Proves that the two ordered collections a caller fills, the staged visible-entity list and the assembled
/// event batch, refuse an identity fenced to another run session.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-007-012</c>, <c>VER-SIM-006-012</c>.
/// </para>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Entity identity: "IDs are unique only within one run
/// session", and "Invalid, expired, or generation-mismatched references fail closed and produce a diagnostic
/// counter". § Tick transaction: an invariant failure before commit "never publishes a partial state".
/// </para>
/// <para>
/// <b>Why the foreign identity is minted by a second allocator rather than by a factory.</b> The identity has
/// to be one a real caller could actually be holding, and the route a real caller has is the allocator of
/// another run: an earlier run's allocator issued it, the reference outlived that run, and nothing about the
/// value marks it as dead. A hand-built identity would demonstrate only that the guard reads a field. This is
/// the shape <see cref="EntityIdTests.AnIdFromAnotherRunSessionNeverResolves"/> already uses for
/// <c>PackedEntityStore</c>'s guard, and both tests below assert the collision on storage index and generation
/// that makes the run fence the only thing separating the two identities.
/// </para>
/// <para>
/// <b>Why these two collections and not <c>PackedEntityStore</c>.</b> The store mints every identity it holds
/// from its own allocator, so it is fenced by construction and has nothing to check. These two accept records
/// from a caller, so for them the fence is a check or it is nothing.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SnapshotRunSessionFenceTests
{
    /// <summary>A run session that is not <see cref="SnapshotFixture.RunSession"/>, so its identities are foreign.</summary>
    private const ulong ForeignRunSession = 0x5A70_0002UL;

    /// <summary>
    /// Verification: <c>VER-SIM-007-012</c>.
    ///
    /// A snapshot entity built from another run's identity is refused by staging, and the local identity that
    /// it collides with on storage index and generation is accepted, so the run fence is what decided.
    /// </summary>
    [Test]
    public void AForeignRunSessionEntityCannotBeStaged()
    {
        SnapshotFixture fixture = new(enemyCount: 3);
        EntityId foreign = ForeignOrdinaryEnemyId();
        EntityId local = fixture.EnemyIds[0];

        fixture.Publisher.BeginTick(0);
        int stagedBefore = fixture.Publisher.StagedEntityCount;

        ArgumentException failure = Expect.Throws<ArgumentException>(
            () => fixture.Publisher.StageVisibleEntity(ForeignRecord(foreign)));

        Expect.Multiple(() =>
        {
            Assert.That(
                foreign.Index,
                Is.EqualTo(local.Index),
                "the foreign identity must collide on storage index, or nothing is being proved: doc 20 "
                    + "§ Entity identity says IDs are unique only within one run session");
            Assert.That(
                foreign.Generation,
                Is.EqualTo(local.Generation),
                "and on generation too, so only the run fence distinguishes them");
            Assert.That(
                failure.Message,
                Does.Contain("is fenced to run session"),
                "the run-session fence must be the refusal, not the defaulted-record refusal");
            Assert.That(
                fixture.Publisher.StagedEntityCount,
                Is.EqualTo(stagedBefore),
                "a refused record must not be staged");
            Assert.That(
                local.RunSession,
                Is.EqualTo(SnapshotFixture.RunSession),
                "the local identity belongs to the publisher's run");
        });

        // The same record shape built from the local identity must be accepted, so the refusal above is about
        // the run session and not about the record.
        Expect.DoesNotThrow(() => fixture.Publisher.StageVisibleEntity(LocalRecord(local)));
        Assert.That(
            fixture.Publisher.StagedEntityCount,
            Is.EqualTo(stagedBefore + 1),
            "the local record must be staged, or the control cannot tell a fence from a blanket refusal");
    }

    /// <summary>
    /// Verification: <c>VER-SIM-006-012</c>.
    ///
    /// An assembled batch carrying another run's identity as an event's emitting entity, or as its subject,
    /// fails the tick invariant and publishes nothing; the same batch built from local identities publishes.
    /// </summary>
    /// <remarks>
    /// Both identity fields are exercised, because they are separate fields on separate types: the emitting
    /// entity lives on <c>EventProvenance</c> and the subject on the record itself, so a guard that read one
    /// and not the other would leave half the gap open. Publication is checked to have produced nothing after
    /// each refusal, which is doc 20 § Tick transaction's requirement rather than merely that a throw
    /// happened.
    /// </remarks>
    [Test]
    public void AForeignRunSessionEventCannotBePublished()
    {
        AssertAForeignEmitterFailsTheBatchInvariant();
        AssertAForeignSubjectFailsTheBatchInvariant();
        AssertALocalBatchPublishes();
    }

    /// <summary>An event whose provenance names another run's emitter must fail the batch invariant.</summary>
    private static void AssertAForeignEmitterFailsTheBatchInvariant()
    {
        SnapshotFixture fixture = new(enemyCount: 2);
        EntityId foreign = ForeignOrdinaryEnemyId();

        OpenTick(fixture, tick: 4);
        fixture.DomainEvents.Append(DomainEvent.Create(
            fixture.EnemyDefeated,
            EventProvenance.Create(4, 10, fixture.Publisher.NextEventSequence(), foreign, "E-FIXTURE"),
            fixture.EnemyIds[0],
            positionX: 1.0,
            positionY: -1.0,
            EventPayload.Typed(EventPayload.InitialSchemaVersion, 1, 0.5, "R-COMMON-ORE")));

        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => fixture.Publisher.Publish(
                fixture.DomainEvents,
                fixture.PresentationEvents,
                PresentationCoalescingPolicy.Verbatim));

        Expect.Multiple(() =>
        {
            Assert.That(
                failure.Message,
                Does.Contain("as its emitting entity"),
                "the guard must name which of the record's two identities is foreign");
            Assert.That(
                failure.Message,
                Does.Contain("rather than to this publisher's run session"),
                "and it must name the fence it failed");
            Assert.That(
                fixture.Publisher.Latest,
                Is.Null,
                "a tick that fails an invariant must publish nothing at all");
        });
    }

    /// <summary>An event whose subject is another run's identity must fail the batch invariant too.</summary>
    private static void AssertAForeignSubjectFailsTheBatchInvariant()
    {
        SnapshotFixture fixture = new(enemyCount: 2);
        EntityId foreign = ForeignOrdinaryEnemyId();

        OpenTick(fixture, tick: 4);
        fixture.DomainEvents.Append(DomainEvent.Create(
            fixture.EnemyDefeated,
            EventProvenance.Create(
                4, 10, fixture.Publisher.NextEventSequence(), fixture.Allocator.PlayerId, "E-FIXTURE"),
            foreign,
            positionX: 1.0,
            positionY: -1.0,
            EventPayload.Typed(EventPayload.InitialSchemaVersion, 1, 0.5, "R-COMMON-ORE")));

        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => fixture.Publisher.Publish(
                fixture.DomainEvents,
                fixture.PresentationEvents,
                PresentationCoalescingPolicy.Verbatim));

        Expect.Multiple(() =>
        {
            Assert.That(
                failure.Message,
                Does.Contain("as its subject"),
                "the subject field must be checked as well as the emitting entity");
            Assert.That(
                fixture.Publisher.Latest,
                Is.Null,
                "a tick that fails an invariant must publish nothing at all");
        });
    }

    /// <summary>
    /// The same batch shape built entirely from this run's identities must publish, or the two refusals above
    /// prove only that publication can fail.
    /// </summary>
    private static void AssertALocalBatchPublishes()
    {
        SnapshotFixture fixture = new(enemyCount: 2);

        OpenTick(fixture, tick: 4);
        fixture.DomainEvents.Append(DomainEvent.Create(
            fixture.EnemyDefeated,
            EventProvenance.Create(
                4, 10, fixture.Publisher.NextEventSequence(), fixture.Allocator.PlayerId, "E-FIXTURE"),
            fixture.EnemyIds[0],
            positionX: 1.0,
            positionY: -1.0,
            EventPayload.Typed(EventPayload.InitialSchemaVersion, 1, 0.5, "R-COMMON-ORE")));

        TickPublication publication = fixture.Publisher.Publish(
            fixture.DomainEvents,
            fixture.PresentationEvents,
            PresentationCoalescingPolicy.Verbatim);

        Expect.Multiple(() =>
        {
            Assert.That(publication.IsPublished, Is.True, "a wholly local batch must publish");
            Assert.That(
                publication.DomainEvents.Length,
                Is.EqualTo(1),
                "and it must carry the event, so the guard is not simply dropping records");
        });

        fixture.Publisher.ReleaseTick(fixture.DomainEvents, fixture.PresentationEvents);
    }

    /// <summary>Opens the tick on the publisher and both event buffers.</summary>
    /// <param name="fixture">The fixture whose publisher and buffers to open.</param>
    /// <param name="tick">The tick to open.</param>
    private static void OpenTick(SnapshotFixture fixture, long tick)
    {
        fixture.Publisher.BeginTick(tick);
        fixture.DomainEvents.BeginTick(tick);
        fixture.PresentationEvents.BeginTick(tick);
        fixture.Publisher.StagePlayer(0.0, 0.0, 0.0);
        fixture.Publisher.StageHud(HudViewModel.Unpublished);
        fixture.Publisher.StageTerminalState(false);
    }

    /// <summary>
    /// Mints the first ordinary-enemy identity of a <em>different</em> run, which collides with the fixture's
    /// first enemy on storage index and generation.
    /// </summary>
    /// <remarks>
    /// A second <see cref="EntityIdAllocator"/> built on the same manifest counts partitions its slot space
    /// identically, so its first ordinary-enemy allocation lands on the same slot at the same generation as
    /// the fixture's. That collision is the point: the run session is then the only component that differs,
    /// which is exactly the leaked-reference case doc 20 § Entity identity describes.
    /// </remarks>
    private static EntityId ForeignOrdinaryEnemyId()
    {
        EntityIdAllocator foreignRun = new(
            ForeignRunSession,
            SnapshotFixture.MiningSiteManifestCount,
            SnapshotFixture.StaticWorldObjectManifestCount);

        Assert.That(
            foreignRun.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId foreign),
            Is.True,
            "the other run must issue an ordinary-enemy identity");
        return foreign;
    }

    /// <summary>Builds the staged record for a foreign identity.</summary>
    /// <param name="foreign">The other run's identity.</param>
    private static SnapshotEntity ForeignRecord(EntityId foreign)
    {
        return SnapshotEntity.Create(
            foreign,
            PopulationCategory.OrdinaryEnemy,
            positionX: 3.0,
            positionY: -3.0,
            facingRadians: 0.0,
            presentationFlags: 1);
    }

    /// <summary>Builds the same record shape for a local identity.</summary>
    /// <param name="local">This run's identity.</param>
    private static SnapshotEntity LocalRecord(EntityId local)
    {
        return SnapshotEntity.Create(
            local,
            PopulationCategory.OrdinaryEnemy,
            positionX: 3.0,
            positionY: -3.0,
            facingRadians: 0.0,
            presentationFlags: 1);
    }
}
