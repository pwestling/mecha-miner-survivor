using System;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// Proves that the published snapshot is immutable and exposes no mutable store, that it carries complete
/// and increasing identity, and that an invalidated tick publishes nothing.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-007-001</c>, <c>VER-SIM-007-002</c>, <c>VER-SIM-007-005</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot and § Tick transaction;
/// <c>CTR-SIM-003</c> in doc 115 § Cross-boundary contract registry.
/// </remarks>
[TestFixture]
internal sealed class PresentationSnapshotTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-007-001</c>.
    ///
    /// Every member is read-only, no field of any payload type has a mutable type at any depth, and mutating
    /// the authoritative store after publication changes nothing in the published snapshot.
    /// </summary>
    [Test]
    public void SnapshotIsImmutableAndExposesNoMutableStore()
    {
        // Structural half: no member, public or private, of any payload type is mutable or of a mutable type.
        // This covers members added later by someone who never reads this test, which is what an inspection
        // of today's members cannot do.
        SnapshotContractAssertions.PayloadTypesAreStructurallyImmutable(
            "the CTR-SIM-003 payload types",
            typeof(PresentationSnapshot),
            typeof(SnapshotEntity),
            typeof(HudViewModel),
            typeof(SnapshotVersion),
            typeof(TickPublication),
            typeof(InterpolationSnapPolicy));

        // Behavioural half: the snapshot's entity view is not the store's backing array, proved by mutating
        // the store after publication and observing that the snapshot is unchanged.
        SnapshotFixture fixture = new(enemyCount: 3);
        HudViewModel hud = HudViewModel.Unpublished;
        fixture.RunTick(0, hud, out hud);

        PresentationSnapshot published = fixture.Publisher.Latest!;
        string before = published.Render();

        foreach (EntityId enemy in fixture.EnemyIds)
        {
            Assert.That(
                fixture.Enemies.TryUpdate(enemy, new EnemyState(999.0, -999.0, 1)),
                Is.True,
                "the authoritative store must accept the mutation, or the assertion proves nothing");
        }

        Assert.That(fixture.Enemies.TryRemove(fixture.EnemyIds[0]), Is.True);

        Expect.Multiple(() =>
        {
            Assert.That(
                published.Render(),
                Is.EqualTo(before),
                "mutating and shrinking the authoritative store after publication must not change the "
                    + "published snapshot; doc 20 § Presentation snapshot: snapshots do not expose mutable "
                    + "stores");
            Assert.That(
                published.VisibleEntityCount,
                Is.EqualTo(3),
                "the snapshot still carries the population it was published with");
            Assert.That(
                fixture.Enemies.Count,
                Is.EqualTo(2),
                "and the store really did change, so the comparison is not vacuous");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-007-002</c>.
    ///
    /// Every snapshot carries run identity, tick, and a strictly increasing version, so a consumer can always
    /// order two snapshots and tell whether either belongs to its run.
    /// </summary>
    [Test]
    public void RunTickAndVersionIdentityIsCompleteAndIncreasing()
    {
        SnapshotFixture fixture = new(enemyCount: 2);
        HudViewModel hud = HudViewModel.Unpublished;

        Assert.That(fixture.Publisher.Latest, Is.Null, "nothing is published before the first tick");
        Assert.That(
            fixture.Publisher.LatestVersion.IsPublished,
            Is.False,
            "and the version says so rather than defaulting to something orderable");

        SnapshotVersion previousVersion = SnapshotVersion.Unpublished;
        for (long tick = 0; tick < 6; tick++)
        {
            fixture.RunTick(tick, hud, out hud);
            PresentationSnapshot snapshot = fixture.Publisher.Latest!;

            long capturedTick = tick;
            SnapshotVersion capturedPrevious = previousVersion;
            Expect.Multiple(() =>
            {
                Assert.That(
                    snapshot.RunSession,
                    Is.EqualTo(SnapshotFixture.RunSession),
                    "run identity must be on the payload, so a consumer can fence it");
                Assert.That(snapshot.Tick, Is.EqualTo(capturedTick), "tick identity must be on the payload");
                Assert.That(snapshot.Version.IsPublished, Is.True);
                Assert.That(
                    snapshot.Version,
                    Is.GreaterThan(capturedPrevious),
                    "the version must strictly increase across publications");
                Assert.That(
                    snapshot.Version.Value,
                    Is.EqualTo(capturedTick + 1),
                    "the first publication is version one and each publication advances by one");
            });

            previousVersion = snapshot.Version;
        }

        // Two snapshots can share a tick - a paused transaction publishes a replacement between ticks - so
        // the version, not the tick, is what orders them.
        fixture.RunTick(5, hud, out hud);
        PresentationSnapshot replacement = fixture.Publisher.Latest!;
        PresentationSnapshot original = fixture.Publisher.Previous!;

        Expect.Multiple(() =>
        {
            Assert.That(replacement.Tick, Is.EqualTo(original.Tick), "the two snapshots share a tick");
            Assert.That(
                replacement.Version,
                Is.GreaterThan(original.Version),
                "so the version is what tells a consumer which is newer, without diffing fields");
            Assert.That(SnapshotVersion.First.Value, Is.EqualTo(1L));
            Assert.That(SnapshotVersion.Unpublished < SnapshotVersion.First, Is.True);
            Assert.That(SnapshotVersion.First.Next() > SnapshotVersion.First, Is.True);
            Assert.That(
                SnapshotVersion.Unpublished.IsPublished,
                Is.False,
                "the unpublished version must never look like a real one");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-007-005</c>.
    ///
    /// A tick invalidated before commit publishes nothing at all: the double buffer, the version, and the
    /// previously published snapshot are untouched, and no partial snapshot is observable.
    /// </summary>
    [Test]
    public void AnInvalidatedTickPublishesNoSnapshot()
    {
        SnapshotFixture fixture = new(enemyCount: 2);
        HudViewModel hud = HudViewModel.Unpublished;
        fixture.RunTick(0, hud, out hud);

        PresentationSnapshot lastGood = fixture.Publisher.Latest!;
        string lastGoodRendering = lastGood.Render();
        SnapshotVersion lastGoodVersion = fixture.Publisher.LatestVersion;

        // Stage a full tick's worth of state, then fail before commit.
        fixture.Publisher.BeginTick(1);
        fixture.DomainEvents.BeginTick(1);
        fixture.PresentationEvents.BeginTick(1);
        fixture.Publisher.StagePlayer(500.0, -500.0, 3.0);
        fixture.Publisher.StageHud(HudViewModel.Next(hud, 1.0, 0.0, 0, 0, 99.0, 1.0));
        fixture.Publisher.StageTerminalState(true);
        fixture.StageEveryEnemy(1);
        fixture.DomainEvents.Append(DomainEvent.Create(
            fixture.EnemyDefeated,
            EventProvenance.Create(1, 10, fixture.Publisher.NextEventSequence(), fixture.Allocator.PlayerId, "E-FIXTURE"),
            fixture.EnemyIds[0],
            0.0,
            0.0,
            EventPayload.Typed(EventPayload.InitialSchemaVersion, 1, 0.0, EventPayload.NoContentId)));

        Assert.That(
            fixture.Publisher.StagedEntityCount,
            Is.GreaterThan(0),
            "the tick must have staged real state, or 'publishes nothing' is trivially true");

        TickPublication invalidated = fixture.Publisher.InvalidateTick(
            "a deliberate pre-commit invariant failure");

        Expect.Multiple(() =>
        {
            Assert.That(invalidated.IsPublished, Is.False, "an invalidated tick publishes nothing");
            Assert.That(invalidated.Snapshot, Is.Null, "not even a partial snapshot");
            Assert.That(invalidated.Version.IsPublished, Is.False);
            Assert.That(invalidated.DomainEventCount, Is.EqualTo(0), "and no batch");
            Assert.That(invalidated.PresentationEventCount, Is.EqualTo(0));
            Assert.That(invalidated.Tick, Is.EqualTo(1L), "but it names the tick that failed");
            Assert.That(
                invalidated.InvalidationReason,
                Is.EqualTo("a deliberate pre-commit invariant failure"),
                "and why, so the safe-failure path has something to record");
            Assert.That(
                fixture.Publisher.LatestVersion,
                Is.EqualTo(lastGoodVersion),
                "the version must not advance for a tick that did not commit");
            Assert.That(
                fixture.Publisher.Latest,
                Is.SameAs(lastGood),
                "the latest complete snapshot must still be the last committed one");
            Assert.That(
                lastGood.Render(),
                Is.EqualTo(lastGoodRendering),
                "and it must be byte-identical: the staged state must not have leaked into either page");
            Assert.That(
                lastGood.Render(),
                Does.Not.Contain("terminal=yes"),
                "the invalidated tick staged a terminal transition, which must not be observable");
            Assert.That(fixture.Publisher.InvalidatedTickCount, Is.EqualTo(1L));
            Assert.That(
                fixture.DomainEvents.Count,
                Is.EqualTo(1),
                "the invalidated tick's domain events stay in the buffer as evidence; CTR-SIM-001 forbids "
                    + "omitting an authoritative event even on the failure path");
            Expect.Throws<InvalidOperationException>(
                () => fixture.Publisher.InvalidateTick("no tick is open any more"));
        });
    }
}
