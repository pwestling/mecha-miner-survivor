using System;
using MechaMiner.Simulation.Combat;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Mining;
using MechaMiner.Simulation.Random;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.World;

/// <summary>
/// One assembled run: its world, the host that ticks it, and the seams a caller must reach.
/// </summary>
/// <remarks>
/// <para>
/// This exists so that presentation and the tests build the <em>same</em> run. A composition
/// duplicated between the two is a composition that will differ, and it would differ exactly
/// where it matters: a test that wires its own gate to its own publisher proves the world works
/// when correctly wired, and says nothing about whether the shipping wiring is correct.
/// </para>
/// <para>
/// It composes by explicit construction and holds no container, service locator, or mutable
/// global, per <c>docs/technical/114-autonomous-agent-execution-protocol.md</c> § C# and domain
/// defaults. It is a record of what was built, not a registry to look things up in.
/// </para>
/// <para>
/// <b>The master seed is the run session.</b> doc 20 § Authoritative random-number contract derives
/// every stream from one master seed under an explicit schema version, and this composition has exactly
/// one nonzero identity available to be that seed. Making them the same value means a caller that wants
/// two byte-identical runs asks for the same run session, which is a single argument rather than two
/// that have to agree - and it means a caller cannot accidentally reproduce one run's randomness under
/// another run's identity fence.
/// </para>
/// </remarks>
public sealed class RunComposition
{
    private readonly SimulationHost _host;
    private readonly GameplayWorld _world;
    private readonly CommandAdmissionGate _commandGate;
    private readonly SnapshotDoubleBuffer _snapshots;
    private readonly RandomStreamSet _streams;

    private RunComposition(
        SimulationHost host,
        GameplayWorld world,
        CommandAdmissionGate commandGate,
        SnapshotDoubleBuffer snapshots,
        RandomStreamSet streams)
    {
        _host = host;
        _world = world;
        _commandGate = commandGate;
        _snapshots = snapshots;
        _streams = streams;
    }

    /// <summary>The host that paces ticks. Drive the run by calling its step method.</summary>
    public SimulationHost Host => _host;

    /// <summary>The authoritative world.</summary>
    public GameplayWorld World => _world;

    /// <summary>The gate a producer submits command envelopes to.</summary>
    public CommandAdmissionGate CommandGate => _commandGate;

    /// <summary>The double buffer a consumer reads the two latest published snapshots from.</summary>
    public SnapshotDoubleBuffer Snapshots => _snapshots;

    /// <summary>The run's authoritative random streams.</summary>
    /// <remarks>
    /// Exposed so a determinism check can read stream state and draw counts, which doc 20
    /// § Authoritative random-number contract makes the recoverable half of reproducibility. Nothing
    /// outside the world draws from these.
    /// </remarks>
    public RandomStreamSet Streams => _streams;

    /// <summary>The run session every envelope for this run must carry.</summary>
    public ulong RunSession => _commandGate.RunSession;

    /// <summary>The tick the admission window is currently open for.</summary>
    /// <remarks>
    /// A producer targets this tick. It advances as ticks execute, so it is read per sample
    /// rather than cached.
    /// </remarks>
    public SimulationTick OpenTick => _commandGate.OpenTick;

    /// <summary>
    /// Assembles a run over the graybox arena with the player deployed at its centre.
    /// </summary>
    /// <param name="runSession">The run session identity, which is also the master seed. Must not be zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="runSession"/> is zero.</exception>
    /// <remarks>
    /// The arena is <see cref="GrayboxArenaBounds.Default"/>, which is graybox scaffolding that
    /// <c>MAP-007</c> replaces; the origin is its centre. Deployment position selection is
    /// <c>MAP-005</c>'s and the centre is a placeholder for it, not a rule.
    /// </remarks>
    public static RunComposition CreateGraybox(ulong runSession)
    {
        return CreateGraybox(runSession, MinuteZeroBaseline.Row);
    }

    /// <summary>
    /// Assembles a run over the graybox arena under a named replenishment row.
    /// </summary>
    /// <param name="runSession">The run session identity, which is also the master seed. Must not be zero.</param>
    /// <param name="baselineRow">
    /// The row phase 3 executes. <see cref="MinuteZeroBaseline.Row"/> is what ships;
    /// <c>HarnessStressRows</c> holds labelled rows no document states, for harnesses that have to observe
    /// the loss path.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="runSession"/> is zero.</exception>
    public static RunComposition CreateGraybox(ulong runSession, BaselineReplenishmentRow baselineRow)
    {
        return Create(
            runSession,
            GrayboxArenaBounds.Default,
            PlanarVector.Zero,
            GrayboxArenaBounds.DefaultHalfExtentMeters,
            baselineRow);
    }

    /// <summary>
    /// Assembles a run.
    /// </summary>
    /// <param name="runSession">The run session identity, which is also the master seed. Must not be zero.</param>
    /// <param name="bounds">The world constraint phase 5 enforces.</param>
    /// <param name="deploymentPosition">Where the player's body begins.</param>
    /// <param name="spawnHalfExtentMeters">
    /// The half extent the enemy spawn ring is derived from, in gameplay meters.
    /// </param>
    /// <param name="baselineRow">The replenishment row phase 3 executes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="bounds"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="runSession"/> is zero.</exception>
    public static RunComposition Create(
        ulong runSession,
        IPlanarBounds bounds,
        PlanarVector deploymentPosition,
        double spawnHalfExtentMeters,
        BaselineReplenishmentRow baselineRow)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        EntityIdAllocator allocator = new(
            runSession,
            GrayboxRunLayout.MiningSiteCount,

            // No static world object exists in this slice. doc 51 § Landmarks, authored structures, and
            // repetition is what would size this, and StoreCapacities' own row for the category records
            // that no document states a total - so zero is the count of what this run actually carries
            // rather than a guess at what a map would.
            staticWorldObjectManifestCount: 0);

        PackedEntityStore<EnemyState> enemies = new(PopulationCategory.OrdinaryEnemy, allocator);
        PackedEntityStore<ProjectileState> projectiles = new(PopulationCategory.WeaponActor, allocator);
        PackedEntityStore<MiningSiteState> sites = new(PopulationCategory.MiningSite, allocator);

        // Every visible entity in one page. The bound is the three stores' own hard capacities summed,
        // which is the only bound that cannot be exceeded by a legal tick: StageVisibleEntity fails the
        // tick invariant rather than truncating, so a page sized to expected occupancy instead of to
        // capacity would turn a busy tick into a crash.
        int visibleEntityCapacity = enemies.Capacity.HardCapacity
            + projectiles.Capacity.HardCapacity
            + sites.Capacity.HardCapacity;

        // Domain events per tick, worst case, counted rather than guessed: 2 materializations
        // (doc 32:56's pulse batch), 1 resolved contact instance (the 0.20 s global grace of doc 72:43
        // admits at most one per tick), 2 enemy-damaged plus 2 enemy-destroyed (at most two projectiles
        // are in flight at a 0.375 s cadence and a 0.5 s flight time), 1 installment paid, 1 site
        // depleted, and 1 run settled. That is 10. The batch is 32 and the buffer's hard maximum is 256,
        // both above it, because the batch is what publication copies into and exceeding it is an
        // invariant failure rather than a truncation.
        const int domainEventCapacity = 32;
        const int presentationEventCapacity = 1;
        const int eventBufferHardMaximum = 256;

        CommandAdmissionGate commandGate = new(runSession);
        SnapshotPublisher publisher = new(
            runSession,
            visibleEntityCapacity,
            domainEventCapacity,
            presentationEventCapacity);
        DomainEventBuffer domainEvents = new(domainEventCapacity, eventBufferHardMaximum);
        PresentationEventBuffer presentationEvents = new(
            presentationEventCapacity,
            eventBufferHardMaximum);

        RandomStreamSet streams = new(RandomSchemaVersion.Current, runSession);

        // The seams are placed before the world exists, so the world receives a populated store and
        // never has to decide whether a site is missing or merely not yet placed. Placement draws from
        // the seam-placement stream, so it is part of what a same-seed comparison covers.
        PlanarVector[] seamPositions = GrayboxRunLayout.DrawMiningSitePositions(
            streams,
            deploymentPosition);
        for (int index = 0; index < seamPositions.Length; index++)
        {
            if (!sites.TryAdmit(index, MiningSiteState.Available(seamPositions[index]), out _))
            {
                throw new InvalidOperationException(
                    "the mining-site store refused seam "
                        + index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + " even though its capacity was sized from the same count; a manifest-sized "
                        + "store that cannot hold its own manifest is a composition defect");
            }
        }

        GameplayWorld world = new(
            commandGate,
            publisher,
            domainEvents,
            presentationEvents,
            PresentationCoalescingPolicy.Verbatim,
            bounds,
            deploymentPosition,
            allocator,
            enemies,
            projectiles,
            sites,
            streams,
            GrayboxRunLayout.SpawnRing(deploymentPosition, spawnHalfExtentMeters),
            baselineRow);

        return new RunComposition(
            new SimulationHost(world),
            world,
            commandGate,
            publisher.Buffer,
            streams);
    }

    /// <summary>
    /// Builds a command envelope for the tick the admission window is currently open for.
    /// </summary>
    /// <param name="sequence">The producer's monotonic sequence.</param>
    /// <param name="rawInputX">The raw eastward input component as sampled.</param>
    /// <param name="rawInputY">The raw northward input component as sampled.</param>
    /// <returns>The envelope to submit.</returns>
    /// <remarks>
    /// The target tick is read from the gate rather than chosen by the producer, so a producer
    /// cannot address a tick that has already frozen by miscounting. Submitting the result is a
    /// separate call, because admission can reject and the producer has to see the rejection.
    /// </remarks>
    public CommandEnvelope ComposeEnvelope(long sequence, double rawInputX, double rawInputY)
    {
        return CommandEnvelope.Create(
            _commandGate.RunSession,
            _commandGate.OpenTick,
            sequence,
            rawInputX,
            rawInputY);
    }

    /// <summary>
    /// Raises the run clock's terminal transition once the world has assigned a terminal result.
    /// </summary>
    /// <returns>
    /// True when this call raised the transition; false when the run has not ended, or the transition
    /// was already present.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>This bridge exists because phase 13 must not raise the pause itself, and the reason is
    /// mechanical.</b> <c>RunClock.CommitTick</c> refuses to commit while a blocking reason is present,
    /// and <c>SimulationHost.Step</c> treats a refused commit as a failed tick transaction - doc 20
    /// § Tick transaction. So a world that raised <c>PauseReason.TerminalTransition</c> from inside the
    /// tick that settled the run would make that tick uncommittable, and the host would end the run
    /// through the technical-failure path: a crash report for a run that ended exactly as designed.
    /// Raising it here, between steps, stops the run through the ordinary pause contract instead.
    /// </para>
    /// <para>
    /// The 35:00 boundary needs no bridge. <c>SimulationHost.EvaluateFinalBoundary</c> raises the same
    /// reason itself after calling <c>EvaluateTerminalBoundary</c>, so a surviving run is already
    /// blocking by the time the step returns, and this call finds the reason present and reports false.
    /// The asymmetry is the host's, not this method's: the host knows when the clock reaches the
    /// boundary and cannot know when Hull reaches zero.
    /// </para>
    /// <para>
    /// Neither <c>ISimulationWorld</c> nor <c>SimulationHost</c> is changed to accommodate this.
    /// </para>
    /// </remarks>
    public bool SettleTerminalTransition()
    {
        if (!_world.HasEnded)
        {
            return false;
        }

        return _host.Clock.Raise(PauseReason.TerminalTransition).Outcome
            == PauseTransitionOutcome.Raised;
    }
}
