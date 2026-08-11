using System;
using System.Globalization;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Player;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.World;

/// <summary>
/// The authoritative simulation world: the first production implementation of
/// <see cref="ISimulationWorld"/> in this repository, and the place the fourteen tick phases
/// execute.
/// </summary>
/// <remarks>
/// <para>
/// <c>CMP-SIM-001</c> simulation world in
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry:
/// it owns "entities, system stores, schedules, run-local state", runs on the "fixed 60 Hz
/// authoritative tick", and is forbidden "[engine]/files/platform/wall-time access". Every one
/// of those four prohibitions holds by construction here: this type has no field of a
/// platform type, opens no file, reads no clock, and constructs no engine node. The only
/// duration it can see is <see cref="TickRate.SecondsPerTick"/>, a compile-time constant.
/// </para>
/// <para>
/// <b>What it owns in this slice is one player and nothing else.</b> Enemies, contact damage,
/// weapons, mining, spawning, and run termination are the next slice. Their phases are present,
/// named, ordered, and empty, which is deliberate: doc 10 § System phase ordering says a
/// subdivision "keeps its parent phase's number" and a new phase "takes the next unused
/// number", so an empty numbered phase is where the next package's work goes, whereas an absent
/// phase is a place the next package has to decide about while working on something else.
/// </para>
/// <para>
/// <b>Authoritative randomness: none.</b> Nothing in this slice makes a random decision, so no
/// stream is drawn from and no family is added. <c>RandomStreamFamilies</c> is the registry and
/// a family may not be added without registering it there; a world that accepted a
/// <c>RandomStreamSet</c> it never read would be a claim that nothing checks. The first
/// gameplay package that needs a roll takes the set as a constructor dependency then.
/// </para>
/// <para>
/// <b>A throw from <see cref="AdvanceTick"/> ends the run.</b> The host records the technical
/// failure, rethrows unchanged, and refuses every later step: doc 20 § Tick transaction requires
/// an exception before commit to "end the run through the safe technical-failure path" and never
/// to publish partial state. So every argument is validated in the constructor where a defect is
/// still merely a construction error, and the tick body's own guards describe invariants rather
/// than input.
/// </para>
/// </remarks>
public sealed class GameplayWorld : ISimulationWorld
{
    private readonly CommandAdmissionGate _commandGate;
    private readonly SnapshotPublisher _publisher;
    private readonly DomainEventBuffer _domainEvents;
    private readonly PresentationEventBuffer _presentationEvents;
    private readonly PresentationCoalescingPolicy _coalescingPolicy;
    private readonly IPlanarBounds _bounds;
    private readonly int[] _phasesEntered = new int[TickPhase.Last];

    private PlayerState _player;
    private MovementIntent _heldIntent;
    private HudViewModel _hud;
    private int _phaseCount;
    private long _committedTickCount;
    private long _boundaryEvaluationCount;
    private long _scheduledEventCount;
    private string _lastScheduledEventId = string.Empty;

    /// <summary>
    /// Composes a world over the run's already-constructed command gate and publisher.
    /// </summary>
    /// <param name="commandGate">The run's admission gate. Must speak for the same run as the publisher.</param>
    /// <param name="publisher">The run's snapshot publisher.</param>
    /// <param name="domainEvents">The tick-local domain event buffer.</param>
    /// <param name="presentationEvents">The tick-local presentation event buffer.</param>
    /// <param name="coalescingPolicy">The policy publication applies to presentation events.</param>
    /// <param name="bounds">The world constraint phase 5 enforces.</param>
    /// <param name="deploymentPosition">Where the player's body begins.</param>
    /// <exception cref="ArgumentNullException">A dependency is null.</exception>
    /// <exception cref="ArgumentException">
    /// The gate and the publisher speak for different runs, or the deployment position is not a
    /// position the player may legally occupy.
    /// </exception>
    /// <remarks>
    /// The constructor opens the admission window for tick zero. Something has to, because
    /// <c>CommandAdmissionGate.TryAdmit</c> requires an open window and presentation submits
    /// before the first tick runs; doing it here means the window is open for exactly as long as
    /// the world exists, and <see cref="AdvanceTick"/> is the only thing that ever moves it
    /// forward.
    /// </remarks>
    public GameplayWorld(
        CommandAdmissionGate commandGate,
        SnapshotPublisher publisher,
        DomainEventBuffer domainEvents,
        PresentationEventBuffer presentationEvents,
        PresentationCoalescingPolicy coalescingPolicy,
        IPlanarBounds bounds,
        PlanarVector deploymentPosition)
    {
        ArgumentNullException.ThrowIfNull(commandGate);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(domainEvents);
        ArgumentNullException.ThrowIfNull(presentationEvents);
        ArgumentNullException.ThrowIfNull(coalescingPolicy);
        ArgumentNullException.ThrowIfNull(bounds);

        if (commandGate.RunSession != publisher.RunSession)
        {
            throw new ArgumentException(
                "the command gate speaks for run "
                    + commandGate.RunSession.ToString("X16", CultureInfo.InvariantCulture)
                    + " and the publisher for run "
                    + publisher.RunSession.ToString("X16", CultureInfo.InvariantCulture)
                    + "; one world cannot serve two runs, and doc 10 § Commands and mutations makes the "
                    + "run-session identity the thing that keeps a command from crossing between them",
                nameof(publisher));
        }

        if (!bounds.Contains(deploymentPosition, PlayerBaseline.CollisionRadiusMeters))
        {
            throw new ArgumentException(
                "the deployment position "
                    + deploymentPosition.ToString()
                    + " does not place the player's whole collision footprint inside the world bounds. "
                    + "Deploying a body already outside the legal region would be corrected by the first "
                    + "phase 5, which would look like movement nobody commanded",
                nameof(deploymentPosition));
        }

        _commandGate = commandGate;
        _publisher = publisher;
        _domainEvents = domainEvents;
        _presentationEvents = presentationEvents;
        _coalescingPolicy = coalescingPolicy;
        _bounds = bounds;
        _player = PlayerState.Deploy(deploymentPosition);
        _heldIntent = MovementIntent.Stop;
        _hud = HudViewModel.Unpublished;

        _commandGate.BeginTick(SimulationTick.Zero);
    }

    /// <summary>The player's committed state.</summary>
    /// <remarks>
    /// Read-only to everything outside this type. The single registered writer of player state
    /// is <see cref="AdvanceTick"/>'s phase 5, which is what makes doc 10 § Commands and
    /// mutations' one-writer rule a property of this class rather than a convention its callers
    /// keep.
    /// </remarks>
    public PlayerState Player => _player;

    /// <summary>The intent currently held, which persists until a later tick admits another.</summary>
    public MovementIntent HeldIntent => _heldIntent;

    /// <summary>The number of ticks this world has completed.</summary>
    public long CommittedTickCount => _committedTickCount;

    /// <summary>How many times the host has had this world evaluate the terminal boundary.</summary>
    public long BoundaryEvaluationCount => _boundaryEvaluationCount;

    /// <summary>How many authored scheduled events the host has handed to this world.</summary>
    public long ScheduledEventCount => _scheduledEventCount;

    /// <summary>The content ID of the most recent scheduled event, or empty if there has been none.</summary>
    public string LastScheduledEventId => _lastScheduledEventId;

    /// <summary>The number of phases the most recent tick entered.</summary>
    public int LastTickPhaseCount => _phaseCount;

    /// <summary>
    /// The phase identifiers the most recent tick entered, in the order it entered them.
    /// </summary>
    /// <param name="destination">
    /// Receives the identifiers. Must hold at least <see cref="LastTickPhaseCount"/> elements.
    /// </param>
    /// <returns>The number of identifiers written.</returns>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is too short.</exception>
    /// <remarks>
    /// This exists so the phase order is asserted rather than assumed. doc 10 § System phase
    /// ordering warns that "renumbering an unchanged order is invisible to any test that asserts
    /// only relative order", so a test needs the literal numbers, in sequence, as data - which
    /// means the world has to record them. The recording is into a fixed array allocated once,
    /// so observing the order costs no allocation per tick.
    /// </remarks>
    public int CopyLastTickPhases(int[] destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < _phaseCount)
        {
            throw new ArgumentException(
                "the destination holds "
                    + destination.Length.ToString(CultureInfo.InvariantCulture)
                    + " elements but the last tick entered "
                    + _phaseCount.ToString(CultureInfo.InvariantCulture)
                    + " phases",
                nameof(destination));
        }

        Array.Copy(_phasesEntered, destination, _phaseCount);
        return _phaseCount;
    }

    /// <inheritdoc/>
    public void AdvanceTick(SimulationTick tick)
    {
        // The tick-local event buffers open before phase 1 so that any phase may append to them,
        // and phase 14 is what consumes and releases them. They are not a phase themselves.
        _domainEvents.BeginTick(tick.Index);
        _presentationEvents.BeginTick(tick.Index);
        _phaseCount = 0;

        // ---- phase 1: admit and normalize commands for the tick (doc 10:144) ----
        Enter(TickPhase.AdmitCommands);
        AdmittedCommandSet admitted = FreezeAdmissionFor(tick);
        if (!admitted.IsEmpty)
        {
            // A tick with no admitted command is NOT a release. doc 20 § Active commands makes a
            // stop an explicit zero-magnitude intent, so absence and zero are different facts:
            // during a catch-up burst only the first of several ticks carries a command, and
            // treating the rest as releases would stutter a held direction to a halt. The latest
            // admitted intent therefore persists until another is admitted.
            _heldIntent = admitted.LatestIntent;
        }

        // ---- phase 2: authored schedule boundaries (doc 10:145) ----
        // Empty. The authored schedule belongs to the encounter packages; the host owns the
        // terminal-boundary ordering and calls EvaluateTerminalBoundary for it.
        Enter(TickPhase.EvaluateScheduleBoundaries);

        // ---- phase 3: materialize queued spawns (doc 10:146) ----
        // Empty. Spawning is out of this slice's scope.
        Enter(TickPhase.MaterializeSpawns);

        // ---- phase 4: resolve player intent and enemy steering (doc 10:147) ----
        Enter(TickPhase.ResolveIntentAndSteering);
        PlayerSteering steering = PlayerSteering.Resolve(_heldIntent, _player.FacingRadians);

        // ---- phase 5: integrate movement, enforce terrain/world constraints (doc 10:148) ----
        Enter(TickPhase.IntegrateMovement);
        _player = PlayerMovement.Integrate(_player, steering, _bounds);

        // ---- phase 6: spatial-query structures (doc 10:149) ----
        // Empty. One body needs no broad phase; GEO-004 owns the uniform spatial hash.
        Enter(TickPhase.UpdateSpatialStructures);

        // ---- phase 7: automatic-weapon targets and attack schedules (doc 10:150) ----
        // Empty. Weapons are out of this slice's scope.
        Enter(TickPhase.AcquireTargets);

        // ---- phase 8: projectiles, beams, zones, pulses, drones, contacts (doc 10:151) ----
        // Empty. Weapons are out of this slice's scope.
        Enter(TickPhase.SimulateWeapons);

        // ---- phase 9: collision, overlap, and damage candidates (doc 10:152) ----
        // Empty. Contact damage is out of this slice's scope.
        Enter(TickPhase.CollectDamageCandidates);

        // ---- phase 10: damage, status, deaths, consequences (doc 10:153) ----
        // Empty. Contact damage is out of this slice's scope.
        Enter(TickPhase.ResolveDamage);

        // ---- phase 11: mining, extraction, payouts, pickups, transactions (doc 10:154) ----
        // Empty. Mining is out of this slice's scope.
        Enter(TickPhase.AdvanceMining);

        // ---- phase 12: deferred entity creation/removal and capacity queues (doc 10:155) ----
        // Empty, and empty for a structural reason rather than a scope one: nothing in this slice
        // creates or removes an entity, so there is nothing deferred to apply. doc 10 § System
        // phase ordering defers structural change "so systems do not invalidate collections while
        // iterating", which is a rule this phase enforces for phases 3, 9, and 10 once they exist.
        Enter(TickPhase.ApplyDeferredStructuralChanges);

        // ---- phase 13: death or extraction terminal conditions (doc 10:156) ----
        // Empty. Run termination is out of this slice's scope: the player cannot yet be damaged,
        // so Hull cannot reach zero, and extraction is MIN work. PlayerState.IsDestroyed exists
        // and is deliberately not read here - proposing a terminal result is the damage package's
        // to write, and a half-built one would be worse than none.
        Enter(TickPhase.EvaluateTerminalConditions);

        // ---- phase 14: publish metrics, ordered events, presentation snapshot (doc 10:157) ----
        Enter(TickPhase.Publish);
        PublishTick(tick);

        _committedTickCount++;

        // Opening the next tick's admission window belongs to no phase: it is the edge between
        // this tick and the next, and it happens here so that the window is open throughout the
        // gap in which presentation samples input and submits. Phase 1 of the next tick is what
        // closes it.
        _commandGate.BeginTick(tick.Next());
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>Deliberately narrow: this records that the host reached the boundary and decides
    /// nothing.</b> <see cref="ISimulationWorld"/>'s own remarks say "what extraction resolution
    /// actually does belongs to the packages that own damage and extraction", so proposing a
    /// terminal result here would be writing something <c>PRG-006</c> and the encounter packages
    /// own, and it would be thrown away.
    /// </para>
    /// <para>
    /// It publishes no snapshot. The boundary tick is never executed - it has no phase 14 - and
    /// staging a fifteenth-phase publication for it would invent a tick the run clock never
    /// committed.
    /// </para>
    /// </remarks>
    public void EvaluateTerminalBoundary(SimulationTick boundaryTick)
    {
        _boundaryEvaluationCount++;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Deliberately narrow: this records the event the host admitted and decides nothing.</b>
    /// <see cref="ISimulationWorld"/>'s own remarks say this member "is expected to be replaced
    /// by the schedule owner's contract, not to become one", so selecting, ordering, or acting on
    /// a schedule row here would be building <c>ENC-002</c>'s scheduler behind a seam that exists
    /// only to make the host's ordering observable.
    /// </remarks>
    public void BeginScheduledEvent(SimulationTick scheduledTick, string scheduleEventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleEventId);
        _scheduledEventCount++;
        _lastScheduledEventId = scheduleEventId;
    }

    private AdmittedCommandSet FreezeAdmissionFor(SimulationTick tick)
    {
        if (!_commandGate.IsAdmissionOpen || _commandGate.OpenTick != tick)
        {
            throw new InvalidOperationException(
                "phase "
                    + TickPhase.AdmitCommands.ToString(CultureInfo.InvariantCulture)
                    + " must freeze the admission window for tick "
                    + tick.ToString()
                    + ", but the gate "
                    + (_commandGate.IsAdmissionOpen
                        ? "has tick " + _commandGate.OpenTick.ToString() + " open"
                        : "has no window open")
                    + ". The world opens each window itself, so this is an invariant failure rather "
                    + "than a caller error, and doc 20 § Tick transaction ends the run rather than "
                    + "publishing a tick whose commands were never frozen");
        }

        return _commandGate.FreezeTick();
    }

    private void PublishTick(SimulationTick tick)
    {
        _publisher.BeginTick(tick.Index);

        // TDR-005 § Coordinate contract: "The authoritative position is the ground-plane center."
        // What is staged is that centre, unmodified. Presentation maps it; it does not adjust it.
        _publisher.StagePlayer(_player.Position.X, _player.Position.Y, _player.FacingRadians);

        _hud = HudViewModel.Next(
            _hud,
            _player.Hull,

            // Armor, banked common ore, banked Hyper Gold, and extraction progress are all zero
            // because none of the systems that change them exists in this slice. They are passed
            // explicitly rather than defaulted so that the first package to own one has a call
            // site to change rather than an argument to discover.
            authoritativeArmor: 0.0,
            bankedCommonOre: 0L,
            bankedHyperGold: 0L,

            // Derived from the integer tick index, never from accumulated frame time
            // (doc 10 § Clock domains).
            runClockSeconds: tick.Seconds,
            extractionProgress: 0.0);
        _publisher.StageHud(_hud);

        // No visible entity is staged: the player is a first-class field of the snapshot rather
        // than an entry in its entity list, and this slice has no other body.
        _publisher.Publish(_domainEvents, _presentationEvents, _coalescingPolicy);
        _publisher.ReleaseTick(_domainEvents, _presentationEvents);
    }

    private void Enter(int phase)
    {
        _phasesEntered[_phaseCount] = phase;
        _phaseCount++;
    }
}
