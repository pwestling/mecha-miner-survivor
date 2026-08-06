using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Snapshots;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// A minimal authoritative world plus a publisher, shared by the <c>SIM-007</c> tests.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports every <c>VER-SIM-007-*</c> entry.
/// </para>
/// <para>
/// The "world" is a real <c>PackedEntityStore</c> from <c>SIM-003</c> rather than a stand-in, so a
/// reconstruction test that claimed to mutate nothing is checked against the actual authoritative storage
/// that a later gameplay package will publish from.
/// </para>
/// </remarks>
internal sealed class SnapshotFixture
{
    /// <summary>The run session everything in the fixture is fenced to.</summary>
    internal const ulong RunSession = 0x5A70_0001UL;

    /// <summary>The mining-site manifest count the fixture's map declares.</summary>
    internal const int MiningSiteManifestCount = 8;

    /// <summary>The static-world-object manifest count the fixture's map declares.</summary>
    internal const int StaticWorldObjectManifestCount = 4;

    /// <summary>How many visible entities a publication may carry in the fixture.</summary>
    internal const int VisibleEntityCapacity = 16;

    private readonly EntityId[] _enemies;

    /// <summary>Builds the fixture with a given number of resident enemies.</summary>
    /// <param name="enemyCount">How many ordinary enemies exist in the world.</param>
    internal SnapshotFixture(int enemyCount)
    {
        Allocator = new EntityIdAllocator(
            RunSession,
            MiningSiteManifestCount,
            StaticWorldObjectManifestCount);
        Enemies = new PackedEntityStore<EnemyState>(PopulationCategory.OrdinaryEnemy, Allocator);
        Publisher = new SnapshotPublisher(
            RunSession,
            VisibleEntityCapacity,
            domainEventCapacity: 64,
            presentationEventCapacity: 64);
        DomainEvents = new DomainEventBuffer(initialCapacity: 8, hardMaximumCapacity: 512);
        PresentationEvents = new PresentationEventBuffer(initialCapacity: 8, hardMaximumCapacity: 512);
        EnemyDefeated = EventKind.Declare(3001, "enemy-defeated");

        _enemies = new EntityId[enemyCount];
        for (int index = 0; index < enemyCount; index++)
        {
            Assert.That(
                Enemies.TryAdmit(index, new EnemyState(index * 2.0, index * -1.5, 100 - index), out _enemies[index]),
                Is.True,
                "the fixture must admit every enemy");
        }
    }

    /// <summary>The run's allocator.</summary>
    internal EntityIdAllocator Allocator { get; }

    /// <summary>The authoritative ordinary-enemy store.</summary>
    internal PackedEntityStore<EnemyState> Enemies { get; }

    /// <summary>The one publisher, which is <c>CMP-SIM-003</c>.</summary>
    internal SnapshotPublisher Publisher { get; }

    /// <summary>The tick's domain buffer.</summary>
    internal DomainEventBuffer DomainEvents { get; }

    /// <summary>The tick's presentation buffer.</summary>
    internal PresentationEventBuffer PresentationEvents { get; }

    /// <summary>A declared domain event kind for the fixture.</summary>
    internal EventKind EnemyDefeated { get; }

    /// <summary>The resident enemy identities, in admission order.</summary>
    internal System.Collections.Generic.IReadOnlyList<EntityId> EnemyIds => _enemies;

    /// <summary>
    /// Runs one whole tick: opens the buffers, stages every resident enemy, emits one domain event, publishes,
    /// and ends the batch lease.
    /// </summary>
    /// <param name="tick">The tick to run.</param>
    /// <param name="previousHud">The HUD model currently published.</param>
    /// <param name="hud">The HUD model this tick published.</param>
    /// <returns>The rendered authoritative result of the tick.</returns>
    internal string RunTick(long tick, HudViewModel previousHud, out HudViewModel hud)
    {
        Publisher.BeginTick(tick);
        DomainEvents.BeginTick(tick);
        PresentationEvents.BeginTick(tick);

        Publisher.StagePlayer(tick * 0.05, tick * -0.05, tick * 0.01);
        hud = HudViewModel.Next(
            previousHud,
            authoritativeHull: 100.0 - tick,
            authoritativeArmor: 5.0,
            bankedCommonOre: 300 + tick,
            bankedHyperGold: 25,
            runClockSeconds: tick / 60.0,
            extractionProgress: tick / 1000.0);
        Publisher.StageHud(hud);
        Publisher.StageTerminalState(false);
        StageEveryEnemy(tick);

        DomainEvents.Append(DomainEvent.Create(
            EnemyDefeated,
            EventProvenance.Create(tick, 10, Publisher.NextEventSequence(), Allocator.PlayerId, "E-FIXTURE"),
            _enemies.Length > 0 ? _enemies[0] : EntityId.NoEntityIn(RunSession),
            positionX: tick * 1.0,
            positionY: tick * -1.0,
            EventPayload.Typed(EventPayload.InitialSchemaVersion, tick, tick * 0.5, "R-COMMON-ORE")));

        TickPublication publication = Publisher.Publish(
            DomainEvents,
            PresentationEvents,
            PresentationCoalescingPolicy.Verbatim);
        string rendered = publication.RenderAuthoritative();
        Publisher.ReleaseTick(DomainEvents, PresentationEvents);
        return rendered;
    }

    /// <summary>Stages a snapshot record for every resident enemy, reading its authoritative state.</summary>
    internal void StageEveryEnemy(long tick)
    {
        EntityId[] ordered = new EntityId[Enemies.Count];
        int written = Enemies.CopyOrderedTo(ordered);
        for (int index = 0; index < written; index++)
        {
            Assert.That(Enemies.TryGet(ordered[index], out EnemyState state), Is.True);
            Publisher.StageVisibleEntity(SnapshotEntity.Create(
                ordered[index],
                PopulationCategory.OrdinaryEnemy,
                state.PositionX + (tick * 0.01),
                state.PositionY - (tick * 0.01),
                facingRadians: 0.0,
                presentationFlags: state.Hull > 50 ? 1 : 2));
        }
    }

    /// <summary>Renders the authoritative world state as canonical invariant text.</summary>
    /// <remarks>
    /// The comparison target for a reconstruction test: doc 20 § Scope and invariants requires that
    /// "presentation cannot mutate simulation state", and a whole-state rendering is how that is checked
    /// rather than by inspecting the fields someone remembered.
    /// </remarks>
    internal string RenderWorld()
    {
        EntityId[] ordered = new EntityId[Enemies.Count];
        int written = Enemies.CopyOrderedTo(ordered);
        System.Text.StringBuilder builder = new();
        builder.Append("world ").Append(Enemies.Diagnostics.Render()).Append('\n');
        for (int index = 0; index < written; index++)
        {
            Assert.That(Enemies.TryGet(ordered[index], out EnemyState state), Is.True);
            builder
                .Append("  ")
                .Append(ordered[index].ToString())
                .Append(' ')
                .Append(state.ToString())
                .Append('\n');
        }

        return builder.ToString();
    }
}
