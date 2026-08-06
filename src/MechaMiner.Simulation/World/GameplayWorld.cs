using System;
using System.Globalization;
using MechaMiner.Simulation.Combat;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Mining;
using MechaMiner.Simulation.Player;
using MechaMiner.Simulation.Random;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.World;

/// <summary>
/// The authoritative simulation world: the production implementation of
/// <see cref="ISimulationWorld"/>, and the place the fourteen tick phases execute.
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
/// <b>What it owns.</b> One player, a population of ordinary <c>EN-01</c> pursuers, the mech's one
/// automatic weapon and its projectiles, three standard ore seams, the run's ore total, and the run's
/// terminal result. Twelve of the fourteen phases now do something. The two that do not are phase 2,
/// whose authored formations belong to <c>ENC-002</c>'s schedule compiler and which doc 32:56 gives
/// none at minute 0, and phase 6, whose broad-phase structures belong to <c>GEO-004</c> and which a
/// population bounded at eight bodies does not need.
/// </para>
/// <para>
/// <b>Authoritative randomness: two registered families.</b>
/// <c>RandomStreamFamilies.BaselineEncounterSectorsAndComposition</c> (<c>0x0300</c>) supplies enemy
/// entrance bearings and <c>RandomStreamFamilies.StandardSeamPlacement</c> (<c>0x0210</c>) supplies
/// seam positions. Both are in the registry already, which doc 20 § Authoritative random-number
/// contract requires: a family is never invented at a call site. Exactly one draw is taken per
/// materialized enemy and one per seam, so the number of draws is a function of committed ticks and
/// deaths, and two runs of the same seed stay in step for the whole run rather than only until the
/// first branch.
/// </para>
/// <para>
/// <b>A throw from <see cref="AdvanceTick"/> ends the run.</b> The host records the technical
/// failure, rethrows unchanged, and refuses every later step: doc 20 § Tick transaction requires
/// an exception before commit to "end the run through the safe technical-failure path" and never
/// to publish partial state. So every argument is validated in the constructor where a defect is
/// still merely a construction error, and the tick body's own guards describe invariants rather
/// than input.
/// </para>
/// <para>
/// <b>Every per-tick collection is a field allocated once.</b> The ordered-ID buffers, the two
/// candidate lists, and the removal lists are sized from the stores' own hard capacities in the
/// constructor, so a committed tick allocates nothing - which is what
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c>'s steady-state allocation
/// expectation needs and what makes the tick's cost independent of how busy it is.
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

    private readonly EntityIdAllocator _allocator;
    private readonly PackedEntityStore<EnemyState> _enemies;
    private readonly PackedEntityStore<ProjectileState> _projectiles;
    private readonly PackedEntityStore<MiningSiteState> _sites;
    private readonly RandomStreamSet _streams;
    private readonly GrayboxSpawnRing _spawnRing;
    private readonly EnemyProfile _enemyProfile;
    private readonly BaselineReplenishmentRow _baselineRow;
    private readonly RandomStreamKey _entranceStream;
    private readonly EntityId _runScopedSubject;

    private readonly EntityId[] _orderedEnemies;
    private readonly PlanarVector[] _enemySteering;
    private readonly EntityId[] _orderedProjectiles;
    private readonly EntityId[] _orderedSites;
    private readonly EntityId[] _contactCandidates;
    private readonly EntityId[] _weaponVictims;
    private readonly EntityId[] _deadEnemies;
    private readonly EntityId[] _spentProjectiles;
    private readonly PlanarVector[] _pendingProjectileBearings;

    private PlayerState _player;
    private MovementIntent _heldIntent;
    private HudViewModel _hud;
    private WeaponSchedule _weapon;
    private RunTerminalResult _terminal;

    private int _phaseCount;
    private long _committedTickCount;
    private long _boundaryEvaluationCount;
    private long _scheduledEventCount;
    private string _lastScheduledEventId = string.Empty;

    private int _orderedEnemyCount;
    private int _orderedProjectileCount;
    private int _orderedSiteCount;
    private int _contactCandidateCount;
    private int _weaponVictimCount;
    private int _deadEnemyCount;
    private int _spentProjectileCount;
    private int _pendingProjectileCount;

    private long _enemyAdmissionOrdinal;
    private long _projectileAdmissionOrdinal;
    private long _eventSequence;
    private long _lastGlobalContactTick = ContactDamageCadence.NeverContacted;
    private long _runLocalCommonOre;
    private long _enemiesMaterialized;
    private long _enemiesDestroyed;
    private long _contactInstancesResolved;
    private long _projectilesFired;
    private long _installmentsPaid;
    private long _enemiesRemovedInPhaseTwelve;
    private double _publishedExtractionFraction;

    /// <summary>
    /// Composes a world over the run's already-constructed stores, streams, and publisher.
    /// </summary>
    /// <param name="commandGate">The run's admission gate. Must speak for the same run as the publisher.</param>
    /// <param name="publisher">The run's snapshot publisher.</param>
    /// <param name="domainEvents">The tick-local domain event buffer.</param>
    /// <param name="presentationEvents">The tick-local presentation event buffer.</param>
    /// <param name="coalescingPolicy">The policy publication applies to presentation events.</param>
    /// <param name="bounds">The world constraint phase 5 enforces.</param>
    /// <param name="deploymentPosition">Where the player's body begins.</param>
    /// <param name="allocator">The run's entity identity allocator.</param>
    /// <param name="enemies">The ordinary-enemy store.</param>
    /// <param name="projectiles">The weapon-actor store.</param>
    /// <param name="sites">The mining-site store, already populated with the run's seams.</param>
    /// <param name="streams">The run's authoritative random streams.</param>
    /// <param name="spawnRing">Where materialized enemies enter.</param>
    /// <param name="baselineRow">
    /// The replenishment row phase 3 executes. <see cref="MinuteZeroBaseline.Row"/> is the authored one;
    /// a harness may pass a labelled stress row instead.
    /// </param>
    /// <exception cref="ArgumentNullException">A dependency is null.</exception>
    /// <exception cref="ArgumentException">
    /// The gate and the publisher speak for different runs, a store belongs to another run or holds
    /// the wrong population category, or the deployment position is not one the player may occupy.
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
        PlanarVector deploymentPosition,
        EntityIdAllocator allocator,
        PackedEntityStore<EnemyState> enemies,
        PackedEntityStore<ProjectileState> projectiles,
        PackedEntityStore<MiningSiteState> sites,
        RandomStreamSet streams,
        GrayboxSpawnRing spawnRing,
        BaselineReplenishmentRow baselineRow)
    {
        ArgumentNullException.ThrowIfNull(commandGate);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(domainEvents);
        ArgumentNullException.ThrowIfNull(presentationEvents);
        ArgumentNullException.ThrowIfNull(coalescingPolicy);
        ArgumentNullException.ThrowIfNull(bounds);
        ArgumentNullException.ThrowIfNull(allocator);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(projectiles);
        ArgumentNullException.ThrowIfNull(sites);
        ArgumentNullException.ThrowIfNull(streams);
        ArgumentNullException.ThrowIfNull(spawnRing);

        if (!baselineRow.IsAuthored)
        {
            throw new ArgumentException(
                "a replenishment row must be built through BaselineReplenishmentRow.Create, so it carries "
                    + "a label saying what it is and whether a document states it. A defaulted row admits "
                    + "nothing and would make an empty arena look like a schedule with no pressure",
                nameof(baselineRow));
        }

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

        if (allocator.RunSession != commandGate.RunSession)
        {
            throw new ArgumentException(
                "the entity allocator is fenced to run "
                    + allocator.RunSession.ToString("X16", CultureInfo.InvariantCulture)
                    + " and this world serves run "
                    + commandGate.RunSession.ToString("X16", CultureInfo.InvariantCulture)
                    + "; doc 20 § Entity identity scopes identities to one run session, so every ID this "
                    + "world published would name nothing in the run it published to",
                nameof(allocator));
        }

        RequireCategory(enemies, PopulationCategory.OrdinaryEnemy, nameof(enemies));
        RequireCategory(projectiles, PopulationCategory.WeaponActor, nameof(projectiles));
        RequireCategory(sites, PopulationCategory.MiningSite, nameof(sites));

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
        _allocator = allocator;
        _enemies = enemies;
        _projectiles = projectiles;
        _sites = sites;
        _streams = streams;
        _spawnRing = spawnRing;
        _baselineRow = baselineRow;
        _enemyProfile = EnemyRoster.Skitterling;
        _entranceStream = RandomStreamKey.Create(
            RandomStreamFamilies.BaselineEncounterSectorsAndComposition,
            0UL);
        _runScopedSubject = EntityId.NoEntityIn(commandGate.RunSession);

        int enemyCapacity = enemies.Capacity.HardCapacity;
        int projectileCapacity = projectiles.Capacity.HardCapacity;
        int siteCapacity = sites.Capacity.HardCapacity;

        _orderedEnemies = new EntityId[enemyCapacity];
        _enemySteering = new PlanarVector[enemyCapacity];
        _contactCandidates = new EntityId[enemyCapacity];
        _deadEnemies = new EntityId[enemyCapacity];

        // One victim per projectile, not one per enemy: a tick can record at most one hit for each
        // projectile in flight, and the weapon-actor ceiling exceeds the ordinary-enemy one, so sizing
        // this from the enemy store would be a bound the wrong side of the quantity it holds.
        _weaponVictims = new EntityId[projectileCapacity];
        _orderedProjectiles = new EntityId[projectileCapacity];
        _spentProjectiles = new EntityId[projectileCapacity];
        _pendingProjectileBearings = new PlanarVector[projectileCapacity];
        _orderedSites = new EntityId[siteCapacity];

        _player = PlayerState.Deploy(deploymentPosition);
        _heldIntent = MovementIntent.Stop;
        _hud = HudViewModel.Unpublished;
        _weapon = WeaponSchedule.Ready;
        _terminal = RunTerminalResult.Unassigned;

        _commandGate.BeginTick(SimulationTick.Zero);
    }

    /// <summary>The player's committed state.</summary>
    /// <remarks>
    /// Read-only to everything outside this type. The registered writers of player state are
    /// <see cref="AdvanceTick"/>'s phase 5, which moves it, and phase 10, which damages it. Both are
    /// inside this class, which is what makes doc 10 § Commands and mutations' one-writer rule a
    /// property of this type rather than a convention its callers keep.
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

    /// <summary>How many ordinary enemies are live.</summary>
    public int LiveEnemyCount => _enemies.Count;

    /// <summary>How many weapon projectiles are in flight.</summary>
    public int LiveProjectileCount => _projectiles.Count;

    /// <summary>How many mining sites the run carries.</summary>
    public int MiningSiteCount => _sites.Count;

    /// <summary>The replenishment row phase 3 executes, including the label that says where it came from.</summary>
    public BaselineReplenishmentRow BaselineRow => _baselineRow;

    /// <summary>The run-local common ore mined so far.</summary>
    /// <remarks>
    /// Run-local, per <c>docs/40-mining-and-extraction.md</c>:90: common ore is "run-local and
    /// [is] lost if unspent when the run ends". Nothing here banks it across runs.
    /// </remarks>
    public long RunLocalCommonOre => _runLocalCommonOre;

    /// <summary>Every enemy this run has materialized.</summary>
    public long EnemiesMaterialized => _enemiesMaterialized;

    /// <summary>Every enemy this run has killed.</summary>
    public long EnemiesDestroyed => _enemiesDestroyed;

    /// <summary>Every contact instance that has reduced the mech's Hull.</summary>
    public long ContactInstancesResolved => _contactInstancesResolved;

    /// <summary>Every weapon activation this run has produced.</summary>
    public long ProjectilesFired => _projectilesFired;

    /// <summary>Every mining installment this run has paid.</summary>
    public long InstallmentsPaid => _installmentsPaid;

    /// <summary>
    /// Every enemy record phase 12 has removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exists because <em>where</em> a removal happens is otherwise unobservable, and a test that cannot
    /// observe it cannot assert it. doc 10 § System phase ordering defers structural change "so systems
    /// do not invalidate collections while iterating", but within one tick a record removed in phase 10
    /// and a record removed in phase 12 are indistinguishable to every later phase: phase 11 does not read
    /// enemies and phase 14 stages after both. A negative control that moved the removal into phase 10 ran
    /// GREEN against every assertion in the loop fixture, which is how this counter came to exist.
    /// </para>
    /// <para>
    /// Compared against <see cref="EnemiesDestroyed"/>, it is the falsifiable form of the claim: a death
    /// resolved in phase 10 that was not applied in phase 12 makes the two disagree.
    /// </para>
    /// </remarks>
    public long EnemiesRemovedInPhaseTwelve => _enemiesRemovedInPhaseTwelve;

    /// <summary>
    /// The tick the mech last received a contact instance on, or
    /// <see cref="ContactDamageCadence.NeverContacted"/>.
    /// </summary>
    public long LastGlobalContactTick => _lastGlobalContactTick;

    /// <summary>The run's terminal result, assigned once and immutable.</summary>
    public RunTerminalResult Terminal => _terminal;

    /// <summary>Whether the run has ended.</summary>
    public bool HasEnded => _terminal.IsAssigned;

    /// <summary>
    /// The extraction fraction phase 14 most recently published, in <c>[0, 1]</c>.
    /// </summary>
    public double PublishedExtractionFraction => _publishedExtractionFraction;

    /// <summary>
    /// Reads one mining site's committed state, in the run's stable site order.
    /// </summary>
    /// <param name="ordinal">The site's position in stable order.</param>
    /// <exception cref="ArgumentOutOfRangeException">There is no site at that ordinal.</exception>
    /// <remarks>
    /// Exists so a test or a transcript can read a seam's progress without the world exposing its
    /// store. doc 91 § Test project separation requires test-only access "through explicit diagnostic
    /// APIs/assemblies, not reflection into private state", and this is that API.
    /// </remarks>
    public MiningSiteState MiningSiteAt(int ordinal)
    {
        int count = _sites.CopyOrderedTo(_orderedSites);
        if (ordinal < 0 || ordinal >= count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ordinal),
                ordinal,
                "the run carries " + count.ToString(CultureInfo.InvariantCulture) + " mining site(s)");
        }

        _ = _sites.TryGet(_orderedSites[ordinal], out MiningSiteState state);
        return state;
    }

    /// <summary>
    /// Reads one live enemy's committed state, in the run's stable enemy order.
    /// </summary>
    /// <param name="ordinal">The enemy's position in stable order.</param>
    /// <exception cref="ArgumentOutOfRangeException">There is no live enemy at that ordinal.</exception>
    public EnemyState EnemyAt(int ordinal)
    {
        int count = _enemies.CopyOrderedTo(_orderedEnemies);
        if (ordinal < 0 || ordinal >= count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ordinal),
                ordinal,
                "the run holds " + count.ToString(CultureInfo.InvariantCulture) + " live enemy/enemies");
        }

        _ = _enemies.TryGet(_orderedEnemies[ordinal], out EnemyState state);
        return state;
    }

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
        // and phase 14 is what consumes and releases them. They are not a phase themselves. The
        // emission sequence resets with them: doc 20 § Domain and presentation events orders
        // simultaneous events by phase then by emission sequence, and a sequence that carried over
        // between ticks would still order correctly but would grow without bound in a transcript.
        _domainEvents.BeginTick(tick.Index);
        _presentationEvents.BeginTick(tick.Index);
        _phaseCount = 0;
        _eventSequence = 0;

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
        // Empty, and empty for a reason the schedule itself states. doc 32 § Director vocabulary:21
        // makes an Event "a deterministic authored formation layered over baseline replenishment at
        // the listed time", and doc 32:56's minute-0 row has none: "Survey orientation; no
        // formation". The compiler that would read the other 34 rows is ENC-002's. The host owns the
        // terminal-boundary ordering and calls EvaluateTerminalBoundary for it.
        Enter(TickPhase.EvaluateScheduleBoundaries);

        // ---- phase 3: materialize queued spawns (doc 10:146) ----
        Enter(TickPhase.MaterializeSpawns);
        MaterializeBaselinePulse(tick);

        // ---- phase 4: resolve player intent and enemy steering (doc 10:147) ----
        Enter(TickPhase.ResolveIntentAndSteering);
        PlayerSteering steering = PlayerSteering.Resolve(_heldIntent, _player.FacingRadians);
        ResolveEnemySteering();

        // ---- phase 5: integrate movement, enforce terrain/world constraints (doc 10:148) ----
        Enter(TickPhase.IntegrateMovement);
        _player = PlayerMovement.Integrate(_player, steering, _bounds);
        IntegrateEnemyMovement();

        // ---- phase 6: spatial-query structures (doc 10:149) ----
        // Empty. GEO-004 owns the uniform spatial hash. doc 32:56 bounds this slice's ordinary
        // population at 8 bodies, so phases 8 and 9 compare against at most 8 footprints; a broad
        // phase over 8 records costs more than the comparisons it removes, and building one here
        // would be building GEO-004 without its tests.
        Enter(TickPhase.UpdateSpatialStructures);

        // ---- phase 7: automatic-weapon targets and attack schedules (doc 10:150) ----
        Enter(TickPhase.AcquireTargets);
        AcquireTargetAndAdvanceSchedule();

        // ---- phase 8: projectiles, beams, zones, pulses, drones, contacts (doc 10:151) ----
        Enter(TickPhase.SimulateWeapons);
        SimulateProjectiles();

        // ---- phase 9: collision, overlap, and damage candidates (doc 10:152) ----
        Enter(TickPhase.CollectDamageCandidates);
        CollectContactCandidates();

        // ---- phase 10: damage, status, deaths, consequences (doc 10:153) ----
        Enter(TickPhase.ResolveDamage);
        ResolveDamage(tick);

        // ---- phase 11: mining, extraction, payouts, pickups, transactions (doc 10:154) ----
        Enter(TickPhase.AdvanceMining);
        double extractionFraction = AdvanceMining(tick);

        // ---- phase 12: deferred entity creation/removal and capacity queues (doc 10:155) ----
        Enter(TickPhase.ApplyDeferredStructuralChanges);
        ApplyDeferredStructuralChanges();

        // ---- phase 13: death or extraction terminal conditions (doc 10:156) ----
        Enter(TickPhase.EvaluateTerminalConditions);
        EvaluateTerminalConditions(tick);

        // ---- phase 14: publish metrics, ordered events, presentation snapshot (doc 10:157) ----
        Enter(TickPhase.Publish);
        PublishTick(tick, extractionFraction);

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
    /// <b>This decides the surviving half of the run's outcome.</b> doc 20 § Boundary and tie
    /// ordering: "After the tick covering the final pre-boundary interval commits, the clock reaches
    /// 35:00 and successful extraction is evaluated before any attack, spawn, hazard, or other event
    /// scheduled for 35:00 or later can begin." Reaching that boundary with an unassigned terminal
    /// result means the mech is alive at 35:00, which is <see cref="RunOutcome.Survived"/>. Reaching
    /// it with one already assigned means phase 13 settled the run earlier, and doc 20 § Scope and
    /// invariants makes that result immutable, so this does nothing.
    /// </para>
    /// <para>
    /// It publishes no snapshot. The boundary tick is never executed - it has no phase 14 - and
    /// staging a fifteenth-phase publication for it would invent a tick the run clock never
    /// committed. It also emits no domain event, for the same reason: the tick-local buffers are
    /// closed, and opening one outside a tick would put a record in a batch nothing publishes.
    /// The result is observable through <see cref="Terminal"/>, which is where a settlement reads it.
    /// </para>
    /// </remarks>
    public void EvaluateTerminalBoundary(SimulationTick boundaryTick)
    {
        _boundaryEvaluationCount++;

        if (_terminal.IsAssigned)
        {
            return;
        }

        _terminal = RunTerminalResult.Assign(
            RunOutcome.Survived,
            boundaryTick.Index,
            _runLocalCommonOre);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <b>Deliberately narrow: this records the event the host admitted and decides nothing.</b>
    /// <see cref="ISimulationWorld"/>'s own remarks say this member "is expected to be replaced
    /// by the schedule owner's contract, not to become one", so selecting, ordering, or acting on
    /// a schedule row here would be building <c>ENC-002</c>'s scheduler behind a seam that exists
    /// only to make the host's ordering observable. Unlike
    /// <see cref="EvaluateTerminalBoundary"/>, whose contract doc 20 § Boundary and tie ordering
    /// states outright, no document says what this one does with a row.
    /// </remarks>
    public void BeginScheduledEvent(SimulationTick scheduledTick, string scheduleEventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleEventId);
        _scheduledEventCount++;
        _lastScheduledEventId = scheduleEventId;
    }

    private static void RequireCategory<TState>(
        PackedEntityStore<TState> store,
        PopulationCategory expected,
        string parameterName)
        where TState : struct
    {
        if (store.Category != expected)
        {
            throw new ArgumentException(
                "this store holds the "
                    + store.Category.ToString()
                    + " population and the world needs the "
                    + expected.ToString()
                    + " one. doc 20 § Authoritative population categories gives each category its own "
                    + "capacity, overflow behaviour, and identity range, so a store in the wrong slot "
                    + "would enforce another population's ceiling",
                parameterName);
        }
    }

    /// <summary>Phase 3: admits this tick's baseline pulse, drawing one entrance bearing per enemy.</summary>
    /// <remarks>
    /// <para>
    /// doc 10:146 - "Materialize queued spawns that have capacity and valid positions." The queue is
    /// produced and consumed inside this phase, which is deliberate: doc 32 § Director vocabulary:20
    /// makes baseline replenishment a continuous condition on the live population rather than an
    /// authored time, so there is no earlier phase at which a pulse becomes pending. Authored
    /// formations do have a time, and those are phase 2's - and doc 32:56 gives minute 0 none.
    /// </para>
    /// <para>
    /// The bearing is drawn before admission, so a pulse that met the store's ceiling and queued
    /// (<c>OverflowBehaviour.QueueAuthored</c>) enters later at the position it was drawn for rather
    /// than at a position drawn on a different tick. That also keeps the draw count a function of the
    /// pulse size alone, which is what a same-seed comparison needs.
    /// </para>
    /// </remarks>
    private void MaterializeBaselinePulse(SimulationTick tick)
    {
        int pulse = _baselineRow.PulseSize(tick.Index, _enemies.Count);
        for (int index = 0; index < pulse; index++)
        {
            PlanarVector entrance = _spawnRing.DrawEntrance(_streams, _entranceStream);
            EnemyState spawned = EnemyState.Spawn(_enemyProfile, entrance);

            if (!_enemies.TryAdmit(_enemyAdmissionOrdinal, spawned, out EntityId id))
            {
                continue;
            }

            _enemyAdmissionOrdinal++;
            _enemiesMaterialized++;
            AppendDomainEvent(
                GameplayEventKinds.EnemyMaterialized,
                TickPhase.MaterializeSpawns,
                tick,
                id,
                entrance,
                quantity: _enemyProfile.MaximumHull,
                magnitude: 0.0,
                contentId: _enemyProfile.ContentId);
        }
    }

    /// <summary>Phase 4: resolves each pursuer's steering direction toward the mech.</summary>
    private void ResolveEnemySteering()
    {
        _orderedEnemyCount = _enemies.CopyOrderedTo(_orderedEnemies);
        for (int index = 0; index < _orderedEnemyCount; index++)
        {
            _ = _enemies.TryGet(_orderedEnemies[index], out EnemyState state);
            _enemySteering[index] = (_player.Position - state.Position).Normalized();
        }
    }

    /// <summary>Phase 5: integrates each pursuer along the direction phase 4 resolved.</summary>
    /// <remarks>
    /// Split from the steering deliberately, because doc 10:147-148 splits it: phase 4 resolves
    /// "player intent and enemy steering" and phase 5 integrates movement. Every pursuer therefore
    /// steers toward the mech's position at the start of the tick, so no pursuer's path depends on
    /// where in the iteration order it happens to sit.
    /// </remarks>
    private void IntegrateEnemyMovement()
    {
        for (int index = 0; index < _orderedEnemyCount; index++)
        {
            EntityId id = _orderedEnemies[index];
            if (!_enemies.TryGet(id, out EnemyState state))
            {
                continue;
            }

            PlanarVector direction = _enemySteering[index];
            if (direction.IsZero)
            {
                continue;
            }

            _ = _enemies.TryUpdate(
                id,
                state.MovedTo(
                    state.Position + (direction * EnemyPursuit.DisplacementPerTickMeters(_enemyProfile))));
        }
    }

    /// <summary>
    /// Phase 7: selects the nearest enemy in range and, if the schedule is ready, queues one shot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>docs/66-weapon-catalog-and-resource-graph.md</c>:94 - Pulse Repeater "selects the nearest
    /// enemy within targeting range and fires rapid projectiles toward its current position ... It
    /// retargets between shots whenever another enemy becomes nearer or the current target becomes
    /// invalid." Selecting fresh every tick rather than holding a target is the same behaviour with no
    /// stored state to become stale, and it makes "the current target becomes invalid" a case that
    /// cannot arise rather than one handled.
    /// </para>
    /// <para>
    /// Range is measured centre to centre. doc 71:81 gives the stat as a bare length and doc 66:94 as
    /// "within targeting range", neither naming a footprint, and centre-to-centre is the measure every
    /// other distance in this world uses. Measuring to the near edge of the target's footprint would
    /// extend the weapon's reach by the target's radius, which would make an identity's body scale
    /// silently change the weapon's stated range.
    /// </para>
    /// <para>
    /// Ties break on the stable enemy order, so two pursuers at identical distance are resolved by
    /// entity identity rather than by iteration accident - doc 20 § Boundary and tie ordering.
    /// </para>
    /// </remarks>
    private void AcquireTargetAndAdvanceSchedule()
    {
        _pendingProjectileCount = 0;
        _weapon = _weapon.Advanced();

        _orderedEnemyCount = _enemies.CopyOrderedTo(_orderedEnemies);

        double bestDistanceSquared = PulseRepeaterBaseline.TargetingRangeMeters
            * PulseRepeaterBaseline.TargetingRangeMeters;
        bool haveTarget = false;
        PlanarVector targetPosition = PlanarVector.Zero;

        for (int index = 0; index < _orderedEnemyCount; index++)
        {
            if (!_enemies.TryGet(_orderedEnemies[index], out EnemyState state))
            {
                continue;
            }

            double distanceSquared = _player.Position.DistanceSquaredTo(state.Position);
            if (distanceSquared > bestDistanceSquared)
            {
                continue;
            }

            if (haveTarget && distanceSquared >= bestDistanceSquared)
            {
                continue;
            }

            bestDistanceSquared = distanceSquared;
            targetPosition = state.Position;
            haveTarget = true;
        }

        if (!haveTarget || !_weapon.IsReady)
        {
            return;
        }

        PlanarVector bearing = (targetPosition - _player.Position).Normalized();
        if (bearing.IsZero)
        {
            // A target whose centre is exactly the mech's centre. Reachable, because doc 31:26 makes
            // enemies non-solid and a pursuer walks through the mech. There is no bearing to fire
            // along, so the activation is not spent and the next tick tries again.
            return;
        }

        _pendingProjectileBearings[_pendingProjectileCount] = bearing;
        _pendingProjectileCount++;
        _weapon = _weapon.Fired();
        _projectilesFired++;
    }

    /// <summary>
    /// Phase 8: advances every projectile one tick and records the enemy each one hit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The hit test is over the swept segment from the projectile's committed position to its new one,
    /// not over either endpoint. <c>PlanarCircle.OverlapsSegment</c> records why: a point test tunnels,
    /// and it also leaves the muzzle unreachable, so a pursuer standing on the mech - which doc 31:26
    /// lets it do, since enemies are non-solid - would be permanently invulnerable to the mech's own
    /// weapon. That is not a subtle mis-hit; it makes the run unwinnable, and it was found by working
    /// the arithmetic rather than by watching a run.
    /// </para>
    /// <para>
    /// Among the enemies the segment touches, the one whose centre is nearest the projectile's new
    /// position is hit. The alternative - first in iteration order - would make the outcome depend on
    /// allocation history for two bodies that overlap each other, which doc 31:26 permits them to do
    /// freely since they do not collide.
    /// </para>
    /// <para>
    /// A projectile that hits is spent: doc 71 § Measurement Conventions:30 - "A target may be damaged
    /// once per projectile, pulse, mine, or other discrete attack object unless a weapon defines a
    /// repeat interval", and doc 71:81 defines none for <c>W-BC</c>. The removal is deferred to phase
    /// 12; only the damage candidate is recorded here.
    /// </para>
    /// </remarks>
    private void SimulateProjectiles()
    {
        _weaponVictimCount = 0;
        _spentProjectileCount = 0;

        _orderedProjectileCount = _projectiles.CopyOrderedTo(_orderedProjectiles);
        _orderedEnemyCount = _enemies.CopyOrderedTo(_orderedEnemies);

        for (int index = 0; index < _orderedProjectileCount; index++)
        {
            EntityId projectileId = _orderedProjectiles[index];
            if (!_projectiles.TryGet(projectileId, out ProjectileState projectile))
            {
                continue;
            }

            ProjectileState advanced = projectile.Advanced();
            _ = _projectiles.TryUpdate(projectileId, advanced);

            EntityId victim = EntityId.Unset;
            double bestDistanceSquared = double.PositiveInfinity;
            for (int enemyIndex = 0; enemyIndex < _orderedEnemyCount; enemyIndex++)
            {
                EntityId enemyId = _orderedEnemies[enemyIndex];
                if (!_enemies.TryGet(enemyId, out EnemyState enemy))
                {
                    continue;
                }

                if (!enemy.FootprintUnder(_enemyProfile)
                        .OverlapsSegment(projectile.Position, advanced.Position))
                {
                    continue;
                }

                double distanceSquared = enemy.Position.DistanceSquaredTo(advanced.Position);
                if (distanceSquared >= bestDistanceSquared)
                {
                    continue;
                }

                bestDistanceSquared = distanceSquared;
                victim = enemyId;
            }

            if (!victim.IsUnset)
            {
                _weaponVictims[_weaponVictimCount] = victim;
                _weaponVictimCount++;
                _spentProjectiles[_spentProjectileCount] = projectileId;
                _spentProjectileCount++;
                continue;
            }

            if (advanced.IsSpent)
            {
                _spentProjectiles[_spentProjectileCount] = projectileId;
                _spentProjectileCount++;
            }
        }
    }

    /// <summary>
    /// Phase 9: collects every enemy whose contact footprint overlaps the mech's, in stable order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// doc 10:152 - "Collect collision, overlap, and damage candidates." Candidates only: the cadence
    /// rules of doc 31:27-28 are eligibility, and applying them here would make the candidate list a
    /// list of resolved instances, which phase 10 is for. The distinction matters because the global
    /// grace makes eligibility depend on the order candidates are resolved in, and a list built under
    /// one order cannot then be resolved under another.
    /// </para>
    /// <para>
    /// The test is circle-circle overlap between the mech's 1.0 m footprint (doc 72:40) and the
    /// pursuer's scaled one (doc 31:35), inclusive at exact tangency, which is what
    /// <c>PlanarCircle.Overlaps</c> implements and doc 21:103 requires.
    /// </para>
    /// </remarks>
    private void CollectContactCandidates()
    {
        _contactCandidateCount = 0;
        PlanarCircle mech = _player.Footprint;

        _orderedEnemyCount = _enemies.CopyOrderedTo(_orderedEnemies);
        for (int index = 0; index < _orderedEnemyCount; index++)
        {
            EntityId id = _orderedEnemies[index];
            if (!_enemies.TryGet(id, out EnemyState state))
            {
                continue;
            }

            if (!state.FootprintUnder(_enemyProfile).Overlaps(mech))
            {
                continue;
            }

            _contactCandidates[_contactCandidateCount] = id;
            _contactCandidateCount++;
        }
    }

    /// <summary>
    /// Phase 10: resolves contact damage to the mech, then weapon damage and deaths, in stable order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Contact first, then weapon damage and deaths.</b> doc 10:153 asks for "damage, status
    /// changes, deaths, and boss/resource consequences in stable order" and does not order the two
    /// populations against each other, so this is a decision. Resolving the mech's inbound damage
    /// first means every contact instance is dealt by a body that is unambiguously alive at the moment
    /// it deals it, which removes the question of whether a pursuer killed on the same tick still
    /// touches you - a question with no documented answer and two defensible ones. Deaths then resolve
    /// after, in the same phase, which is the order doc 10:153 lists them in.
    /// </para>
    /// <para>
    /// Both loops walk the stable order phases 9 and 8 built, and the global contact grace is applied
    /// as it advances, so the first eligible candidate in stable order lands and the rest of that
    /// tick's candidates are refused by the grace it opened.
    /// </para>
    /// </remarks>
    private void ResolveDamage(SimulationTick tick)
    {
        _deadEnemyCount = 0;

        for (int index = 0; index < _contactCandidateCount; index++)
        {
            EntityId id = _contactCandidates[index];
            if (!_enemies.TryGet(id, out EnemyState state))
            {
                continue;
            }

            if (!ContactDamageCadence.IsEligible(
                    tick.Index,
                    _lastGlobalContactTick,
                    state.LastContactTick))
            {
                continue;
            }

            _player = _player.Damaged(_enemyProfile.ContactDamage);
            _lastGlobalContactTick = tick.Index;
            _contactInstancesResolved++;
            _ = _enemies.TryUpdate(id, state.WithContactAt(tick.Index));

            AppendDomainEvent(
                GameplayEventKinds.PlayerContactDamaged,
                TickPhase.ResolveDamage,
                tick,
                _allocator.PlayerId,
                _player.Position,
                quantity: _enemyProfile.ContactDamage,
                magnitude: _player.Hull,
                contentId: _enemyProfile.ContentId);
        }

        for (int index = 0; index < _weaponVictimCount; index++)
        {
            EntityId id = _weaponVictims[index];
            if (!_enemies.TryGet(id, out EnemyState state) || state.IsDead)
            {
                continue;
            }

            EnemyState damaged = state.Damaged(PulseRepeaterBaseline.DamagePerProjectile);
            _ = _enemies.TryUpdate(id, damaged);

            AppendDomainEvent(
                GameplayEventKinds.EnemyDamaged,
                TickPhase.ResolveDamage,
                tick,
                id,
                damaged.Position,
                quantity: PulseRepeaterBaseline.DamagePerProjectile,
                magnitude: damaged.Hull,
                contentId: PulseRepeaterBaseline.ContentId);

            if (!damaged.IsDead)
            {
                continue;
            }

            _deadEnemies[_deadEnemyCount] = id;
            _deadEnemyCount++;
            _enemiesDestroyed++;

            AppendDomainEvent(
                GameplayEventKinds.EnemyDestroyed,
                TickPhase.ResolveDamage,
                tick,
                id,
                damaged.Position,
                quantity: 0L,
                magnitude: 0.0,
                contentId: _enemyProfile.ContentId);
        }
    }

    /// <summary>
    /// Phase 11: advances every site, pays completed installments, and returns the fraction the HUD shows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The run's ore total is written in exactly one place, here, which is doc 10 § Commands and
    /// mutations' single-writer rule applied to run-local state.
    /// </para>
    /// <para>
    /// <b>Which site the HUD shows is a decision.</b> doc 40:169 lists "The player is within range of
    /// more than one mining point" as a case that "require[s] later rules", and doc 40:152 requires the
    /// HUD to show "Current progress, completion condition, and approximate remaining commitment" -
    /// singular. The site with the greatest unfinished progress is chosen, because that is the one the
    /// player has most to lose on leaving, which is the decision doc 40 § Push-your-luck pressure says
    /// the display exists to inform. Ties break on stable site order.
    /// </para>
    /// </remarks>
    private double AdvanceMining(SimulationTick tick)
    {
        double displayed = 0.0;

        _orderedSiteCount = _sites.CopyOrderedTo(_orderedSites);
        for (int index = 0; index < _orderedSiteCount; index++)
        {
            EntityId id = _orderedSites[index];
            if (!_sites.TryGet(id, out MiningSiteState state))
            {
                continue;
            }

            MiningTickOutcome outcome = MiningAdvance.Advance(state, _player.Position);
            _ = _sites.TryUpdate(id, outcome.State);

            if (outcome.CompletedInstallment)
            {
                _runLocalCommonOre += outcome.OreAwarded;
                _installmentsPaid++;

                AppendDomainEvent(
                    GameplayEventKinds.MiningInstallmentPaid,
                    TickPhase.AdvanceMining,
                    tick,
                    id,
                    outcome.State.Centre,
                    quantity: outcome.OreAwarded,
                    magnitude: _runLocalCommonOre,
                    contentId: StandardOreSeamProfile.ResourceContentId);

                if (outcome.State.IsDepleted)
                {
                    AppendDomainEvent(
                        GameplayEventKinds.MiningSiteDepleted,
                        TickPhase.AdvanceMining,
                        tick,
                        id,
                        outcome.State.Centre,
                        quantity: StandardOreSeamProfile.TotalOre,
                        magnitude: 0.0,
                        contentId: StandardOreSeamProfile.ContentId);
                }
            }

            if (outcome.State.InstallmentFraction > displayed)
            {
                displayed = outcome.State.InstallmentFraction;
            }
        }

        return displayed;
    }

    /// <summary>Phase 12: applies every structural change the earlier phases deferred.</summary>
    /// <remarks>
    /// <para>
    /// doc 10 § System phase ordering: "Structural changes are deferred so systems do not invalidate
    /// collections while iterating." Phase 10 marked deaths and phase 8 marked spent projectiles;
    /// neither removed anything, so the ordered ID buffers they were walking stayed valid for the
    /// whole of their own loops and for every later phase in the tick.
    /// </para>
    /// <para>
    /// Removals run before creations. A projectile slot freed this tick is therefore available to a
    /// projectile created this tick, which is what keeps the weapon-actor store's occupancy bounded by
    /// concurrent flights rather than by lifetime shots. <c>AdmitQueued</c> runs last because doc 20
    /// § Capacity and overload behavior has a queued authored enemy "later enter" as capacity opens,
    /// and the capacity that opened is the deaths applied a few lines above.
    /// </para>
    /// </remarks>
    private void ApplyDeferredStructuralChanges()
    {
        for (int index = 0; index < _spentProjectileCount; index++)
        {
            _ = _projectiles.TryRemove(_spentProjectiles[index]);
        }

        for (int index = 0; index < _deadEnemyCount; index++)
        {
            if (_enemies.TryRemove(_deadEnemies[index]))
            {
                _enemiesRemovedInPhaseTwelve++;
            }
        }

        for (int index = 0; index < _pendingProjectileCount; index++)
        {
            ProjectileState launched = ProjectileState.Launch(
                _player.Position,
                _pendingProjectileBearings[index]);
            if (_projectiles.TryAdmit(_projectileAdmissionOrdinal, launched, out _))
            {
                _projectileAdmissionOrdinal++;
            }
        }

        _ = _enemies.AdmitQueued();

        _spentProjectileCount = 0;
        _deadEnemyCount = 0;
        _pendingProjectileCount = 0;
    }

    /// <summary>Phase 13: settles the run if the mech has been destroyed.</summary>
    /// <remarks>
    /// <para>
    /// doc 10:156 - "Evaluate death or extraction terminal conditions." The death half is here; the
    /// extraction half is <see cref="EvaluateTerminalBoundary"/>, because doc 20 § Boundary and tie
    /// ordering puts the 35:00 evaluation after the final pre-boundary tick commits rather than inside
    /// one, and the host is what reaches that position.
    /// </para>
    /// <para>
    /// <b>This does not stop the clock, and it must not try.</b> <c>RunClock.CommitTick</c> refuses to
    /// commit while a blocking reason is present, so raising <c>PauseReason.TerminalTransition</c> from
    /// inside a tick would make the tick that settled the run uncommittable and the host would record
    /// a technical failure - a crash report for a run that ended correctly. The settlement is therefore
    /// state plus a published terminal flag, and raising the pause is the driver's, one step out:
    /// <c>RunComposition.SettleTerminalTransition</c>. Reaching 35:00 needs no such bridge because the
    /// host raises the reason itself.
    /// </para>
    /// </remarks>
    private void EvaluateTerminalConditions(SimulationTick tick)
    {
        if (_terminal.IsAssigned || !_player.IsDestroyed)
        {
            return;
        }

        _terminal = RunTerminalResult.Assign(
            RunOutcome.Destroyed,
            tick.Index,
            _runLocalCommonOre);

        AppendDomainEvent(
            GameplayEventKinds.RunSettled,
            TickPhase.EvaluateTerminalConditions,
            tick,
            _allocator.PlayerId,
            _player.Position,
            quantity: _runLocalCommonOre,
            magnitude: 0.0,
            contentId: RunOutcome.Destroyed.ToString());
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

    private void PublishTick(SimulationTick tick, double extractionFraction)
    {
        _publisher.BeginTick(tick.Index);

        // TDR-005 § Coordinate contract: "The authoritative position is the ground-plane center."
        // What is staged is that centre, unmodified. Presentation maps it; it does not adjust it.
        _publisher.StagePlayer(_player.Position.X, _player.Position.Y, _player.FacingRadians);

        // Mining sites first, then enemies, then projectiles, each in its own stable order. The order
        // is fixed so a snapshot rendered twice draws the same thing in the same sequence, and sites
        // come first because they are the only entities that never move: a consumer that pools nodes
        // by index gets a stable prefix.
        StageVisible(_sites, PopulationCategory.MiningSite, _orderedSites, static state => state.Centre);
        StageVisible(_enemies, PopulationCategory.OrdinaryEnemy, _orderedEnemies, static state => state.Position);
        StageVisible(
            _projectiles,
            PopulationCategory.WeaponActor,
            _orderedProjectiles,
            static state => state.Position);

        _hud = HudViewModel.Next(
            _hud,
            _player.Hull,

            // Armor stays zero because doc 72:36 sets the baseline to zero and no layer that changes it
            // exists. Hyper Gold stays zero because this slice authors no Hyper Gold site: doc 40:119
            // makes those a distinct mining-point class with a threat beacon, which is MIN-002's.
            authoritativeArmor: 0.0,
            bankedCommonOre: _runLocalCommonOre,
            bankedHyperGold: 0L,

            // Derived from the integer tick index, never from accumulated frame time
            // (doc 10 § Clock domains).
            runClockSeconds: tick.Seconds,
            extractionProgress: extractionFraction);
        _publisher.StageHud(_hud);
        _publisher.StageTerminalState(_terminal.IsAssigned);
        _publishedExtractionFraction = extractionFraction;

        _publisher.Publish(_domainEvents, _presentationEvents, _coalescingPolicy);
        _publisher.ReleaseTick(_domainEvents, _presentationEvents);
    }

    private void StageVisible<TState>(
        PackedEntityStore<TState> store,
        PopulationCategory category,
        EntityId[] scratch,
        Func<TState, PlanarVector> positionOf)
        where TState : struct
    {
        int count = store.CopyOrderedTo(scratch);
        for (int index = 0; index < count; index++)
        {
            if (!store.TryGet(scratch[index], out TState state))
            {
                continue;
            }

            PlanarVector position = positionOf(state);
            _publisher.StageVisibleEntity(
                SnapshotEntity.Create(
                    scratch[index],
                    category,
                    position.X,
                    position.Y,

                    // No visible entity in this slice has an authoritative facing. A pursuer's heading
                    // is a function of the mech's position, which presentation already has, and giving
                    // it a stored facing would make the world publish a value nothing decides.
                    facingRadians: 0.0,
                    presentationFlags: 0));
        }
    }

    private void AppendDomainEvent(
        EventKind kind,
        int phase,
        SimulationTick tick,
        EntityId subject,
        PlanarVector at,
        long quantity,
        double magnitude,
        string contentId)
    {
        _domainEvents.Append(
            DomainEvent.Create(
                kind,
                EventProvenance.Create(tick.Index, phase, _eventSequence, _runScopedSubject, contentId),
                subject,
                at.X,
                at.Y,
                EventPayload.Typed(EventPayload.InitialSchemaVersion, quantity, magnitude, contentId)));
        _eventSequence++;
    }

    private void Enter(int phase)
    {
        _phasesEntered[_phaseCount] = phase;
        _phaseCount++;
    }
}
