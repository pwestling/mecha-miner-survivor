using System;
using System.Globalization;
using MechaMiner.Simulation.Events;

namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// <c>CMP-SIM-003</c>: the one component that owns the snapshot double buffers and the event sequence, and
/// publishes the snapshot and both ordered event batches as a single tick result.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry,
/// <c>CMP-SIM-003</c>: state "double buffers and event sequence", input "committed simulation
/// state/events", output "immutable presentation snapshot and ordered event batch", timing "end of
/// committed tick", and explicitly not "owning authoritative gameplay state".
/// <c>docs/technical/20-simulation-core.md</c> § Tick transaction step 6: "Publish the committed state,
/// snapshot, events, and diagnostics as one tick result."
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering phase 14: "Publish metrics,
/// ordered events, and the presentation snapshot."
/// </para>
/// <para>
/// <b>One publisher, not two.</b> The event batches hand off <em>through</em> this type rather than
/// publishing themselves, because <c>CMP-SIM-003</c> is one component and doc 115 § Mutable-state
/// ownership matrix requires "each mutable datum has exactly one row owner". A separate event publisher
/// would give the event sequence a second writer.
/// </para>
/// <para>
/// <b>Staging, then one atomic publication.</b> A caller stages the tick's presentation state during the
/// tick and publishes once at phase 14. Until <see cref="Publish"/> returns, nothing is observable through
/// <see cref="Buffer"/>: doc 20 § Tick transaction requires that an invalidated tick "never publishes a
/// partial state", so <see cref="InvalidateTick"/> discards the staged state without touching the double
/// buffer or advancing the version.
/// </para>
/// <para>
/// <b>Allocation-free after warm-up.</b> Every array - staged entities, domain batch, presentation batch,
/// and both snapshot pages - is preallocated at construction. Publication copies into them and returns a
/// struct, so a churn-free tick allocates nothing (doc 20 § Presentation snapshot; doc 10 § Performance
/// posture).
/// </para>
/// </remarks>
public sealed class SnapshotPublisher
{
    private readonly ulong _runSession;
    private readonly SnapshotDoubleBuffer _buffer;
    private readonly SnapshotEntity[] _stagedEntities;
    private readonly DomainEvent[] _domainBatch;
    private readonly PresentationEvent[] _presentationBatch;

    private long _tick;
    private bool _isTickOpen;
    private int _stagedEntityCount;
    private double _playerPositionX;
    private double _playerPositionY;
    private double _playerFacingRadians;
    private bool _isTerminal;
    private HudViewModel _stagedHud;
    private long _nextEventSequence;
    private long _invalidatedTickCount;

    /// <summary>Creates the publisher and preallocates every page and batch buffer.</summary>
    /// <param name="runSession">The run session every publication is fenced to. Must not be zero.</param>
    /// <param name="visibleEntityCapacity">The largest visible-entity population a publication may carry.</param>
    /// <param name="domainEventCapacity">The largest domain batch a tick may publish.</param>
    /// <param name="presentationEventCapacity">The largest presentation batch a tick may publish.</param>
    /// <exception cref="ArgumentOutOfRangeException">The run session is zero, or a capacity is out of range.</exception>
    public SnapshotPublisher(
        ulong runSession,
        int visibleEntityCapacity,
        int domainEventCapacity,
        int presentationEventCapacity)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run'");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(visibleEntityCapacity);
        ArgumentOutOfRangeException.ThrowIfLessThan(domainEventCapacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(presentationEventCapacity, 1);

        _runSession = runSession;
        _buffer = new SnapshotDoubleBuffer(runSession, visibleEntityCapacity);
        _stagedEntities = new SnapshotEntity[visibleEntityCapacity];
        _domainBatch = new DomainEvent[domainEventCapacity];
        _presentationBatch = new PresentationEvent[presentationEventCapacity];
    }

    /// <summary>The run session every publication is fenced to.</summary>
    public ulong RunSession => _runSession;

    /// <summary>The double buffer holding the two most recent complete snapshots.</summary>
    /// <remarks>
    /// Exposed read-only so a consumer reads the pair doc 30 § Snapshot synchronization interpolates
    /// between. The write path stays internal to the buffer, so this publisher remains its one writer.
    /// </remarks>
    public SnapshotDoubleBuffer Buffer => _buffer;

    /// <summary>The most recent complete snapshot, or <see langword="null"/> before the first publication.</summary>
    public PresentationSnapshot? Latest => _buffer.Latest;

    /// <summary>The snapshot published immediately before <see cref="Latest"/>.</summary>
    public PresentationSnapshot? Previous => _buffer.Previous;

    /// <summary>The version of the most recent publication.</summary>
    public SnapshotVersion LatestVersion => _buffer.LatestVersion;

    /// <summary>The tick currently staged.</summary>
    public long Tick => _tick;

    /// <summary>Whether a tick is open for staging.</summary>
    public bool IsTickOpen => _isTickOpen;

    /// <summary>How many visible entities have been staged for the open tick.</summary>
    public int StagedEntityCount => _stagedEntityCount;

    /// <summary>How many ticks were invalidated and published nothing.</summary>
    /// <remarks>doc 90 § Frame metrics expects a diagnostic for a tick that failed before commit.</remarks>
    public long InvalidatedTickCount => _invalidatedTickCount;

    /// <summary>
    /// Issues the next event emission sequence for the open tick.
    /// </summary>
    /// <exception cref="InvalidOperationException">No tick is open.</exception>
    /// <remarks>
    /// This is the single source of event sequence numbers, because doc 115 § Component registry gives
    /// <c>CMP-SIM-003</c> "the event sequence" as its state. Sequences restart at zero each tick, which is
    /// the documented per-tick origin: the pair of tick and sequence is what forms the run-long total order,
    /// so the sequence itself only has to be unique within its tick. Issuing each number once is what makes
    /// <c>EventOrdering</c>'s comparison a total order, and <c>EventOrdering.AssertTotalOrder</c> checks the
    /// resulting batch rather than trusting this method.
    /// </remarks>
    public long NextEventSequence()
    {
        if (!_isTickOpen)
        {
            throw new InvalidOperationException(
                "no tick is open; an emission sequence belongs to a tick");
        }

        long sequence = _nextEventSequence;
        _nextEventSequence++;
        return sequence;
    }

    /// <summary>Opens a tick for staging and resets the per-tick emission sequence.</summary>
    /// <param name="tick">The authoritative tick. Must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tick"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">A tick is already open.</exception>
    public void BeginTick(long tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);
        if (_isTickOpen)
        {
            throw new InvalidOperationException(
                "tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + " is still open; publish or invalidate it first");
        }

        _tick = tick;
        _isTickOpen = true;
        _stagedEntityCount = 0;
        _nextEventSequence = 0;
        _playerPositionX = 0.0;
        _playerPositionY = 0.0;
        _playerFacingRadians = 0.0;
        _isTerminal = false;
    }

    /// <summary>Stages the player transform for the open tick.</summary>
    /// <param name="positionX">The planar X component in gameplay meters. Must be finite. <b>GEO-001</b>.</param>
    /// <param name="positionY">The planar Y component in gameplay meters. Must be finite. <b>GEO-001</b>.</param>
    /// <param name="facingRadians">The facing in radians. Must be finite.</param>
    /// <exception cref="InvalidOperationException">No tick is open.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite.</exception>
    /// <remarks>
    /// <b>GEO-001:</b> replace <paramref name="positionX"/> and <paramref name="positionY"/> with one
    /// planar-position parameter once <c>GEO-001</c> lands, matching the change in
    /// <c>PresentationSnapshot</c>.
    /// </remarks>
    public void StagePlayer(double positionX, double positionY, double facingRadians)
    {
        RequireOpenTick();
        RequireFinite(positionX, nameof(positionX));
        RequireFinite(positionY, nameof(positionY));
        RequireFinite(facingRadians, nameof(facingRadians));
        _playerPositionX = positionX;
        _playerPositionY = positionY;
        _playerFacingRadians = facingRadians;
    }

    /// <summary>Stages the HUD view model for the open tick.</summary>
    /// <param name="hud">The versioned, already-rounded view model.</param>
    /// <exception cref="InvalidOperationException">No tick is open.</exception>
    public void StageHud(in HudViewModel hud)
    {
        RequireOpenTick();
        _stagedHud = hud;
    }

    /// <summary>Stages the run's terminal state for the open tick.</summary>
    /// <param name="isTerminal">Whether the run has reached a terminal result.</param>
    /// <exception cref="InvalidOperationException">No tick is open.</exception>
    public void StageTerminalState(bool isTerminal)
    {
        RequireOpenTick();
        _isTerminal = isTerminal;
    }

    /// <summary>Stages one visible entity for the open tick.</summary>
    /// <param name="entity">The entity record.</param>
    /// <exception cref="InvalidOperationException">No tick is open, or the visible-entity capacity is exhausted.</exception>
    /// <exception cref="ArgumentException">The record was defaulted rather than constructed.</exception>
    public void StageVisibleEntity(in SnapshotEntity entity)
    {
        RequireOpenTick();
        if (!entity.IsPresent)
        {
            throw new ArgumentException(
                "a defaulted snapshot entity cannot be staged; use SnapshotEntity.Create",
                nameof(entity));
        }

        if (_stagedEntityCount == _stagedEntities.Length)
        {
            throw new InvalidOperationException(
                "the staged visible-entity capacity of "
                    + _stagedEntities.Length.ToString(CultureInfo.InvariantCulture)
                    + " is exhausted on tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + "; growing inside a committed tick would allocate, so this is a failed invariant");
        }

        _stagedEntities[_stagedEntityCount] = entity;
        _stagedEntityCount++;
    }

    /// <summary>
    /// Publishes the staged state, the ordered domain batch, and the coalesced presentation batch as one
    /// tick result.
    /// </summary>
    /// <param name="domainEvents">The tick's domain buffer. Must be open for the same tick.</param>
    /// <param name="presentationEvents">The tick's presentation buffer. Must be open for the same tick.</param>
    /// <param name="coalescingPolicy">The explicit presentation coalescing policy.</param>
    /// <returns>The single tick result.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">A buffer is closed or open for a different tick.</exception>
    /// <exception cref="InvalidOperationException">No tick is open, or a batch exceeds its capacity.</exception>
    /// <remarks>
    /// The domain batch is copied first and in full: doc 20 § Domain and presentation events forbids
    /// dropping a domain event, so a domain batch that does not fit is a failed invariant rather than a
    /// truncation. The domain records are marked consumed only after they are in the published batch, which
    /// is the order doc 20 requires - "Statistics consume domain/damage records before their buffers are
    /// released."
    /// </remarks>
    public TickPublication Publish(
        DomainEventBuffer domainEvents,
        PresentationEventBuffer presentationEvents,
        PresentationCoalescingPolicy coalescingPolicy)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        ArgumentNullException.ThrowIfNull(presentationEvents);
        ArgumentNullException.ThrowIfNull(coalescingPolicy);
        RequireOpenTick();
        RequireBufferTick(domainEvents.IsOpenForTick, domainEvents.Tick, nameof(domainEvents));
        RequireBufferTick(presentationEvents.IsOpenForTick, presentationEvents.Tick, nameof(presentationEvents));

        if (domainEvents.Count > _domainBatch.Length)
        {
            throw new InvalidOperationException(
                "tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + " holds "
                    + domainEvents.Count.ToString(CultureInfo.InvariantCulture)
                    + " domain events but the published batch holds "
                    + _domainBatch.Length.ToString(CultureInfo.InvariantCulture)
                    + ". doc 20 § Domain and presentation events forbids dropping one, so this is a failed "
                    + "invariant rather than a truncated batch");
        }

        int domainCount = domainEvents.CopyOrderedTo(_domainBatch);

        int presentationSourceCount = presentationEvents.Count;
        int presentationCount = presentationEvents.PublishOrderedTo(coalescingPolicy, _presentationBatch);

        SnapshotVersion version = _buffer.Publish(
            _tick,
            _playerPositionX,
            _playerPositionY,
            _playerFacingRadians,
            _isTerminal,
            _stagedHud,
            new ReadOnlySpan<SnapshotEntity>(_stagedEntities, 0, _stagedEntityCount));

        domainEvents.RecordAllConsumed();
        _isTickOpen = false;

        PresentationSnapshot published = _buffer.Latest!;
        if (published.Version != version)
        {
            throw new InvalidOperationException(
                "the double buffer published version "
                    + published.Version.ToString()
                    + " but reported "
                    + version.ToString());
        }

        return TickPublication.Published(
            published,
            new ReadOnlyMemory<DomainEvent>(_domainBatch, 0, domainCount),
            new ReadOnlyMemory<PresentationEvent>(_presentationBatch, 0, presentationCount),
            presentationSourceCount,
            coalescingPolicy.Name);
    }

    /// <summary>
    /// Abandons the open tick without publishing anything, leaving the double buffer and the version
    /// untouched.
    /// </summary>
    /// <param name="reason">Why the tick was invalidated. Must not be blank.</param>
    /// <returns>An unpublished result carrying the reason.</returns>
    /// <exception cref="InvalidOperationException">No tick is open.</exception>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is blank.</exception>
    /// <remarks>
    /// doc 20 § Tick transaction: an invariant failure before commit "invalidates the tick and ends the run
    /// through the safe technical-failure path; it never publishes a partial state". The event buffers are
    /// deliberately left as they are: their records are part of the failure's evidence, and releasing them
    /// here would be the very omission <c>CTR-SIM-001</c> forbids.
    /// </remarks>
    public TickPublication InvalidateTick(string reason)
    {
        RequireOpenTick();
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        _stagedEntityCount = 0;
        _isTickOpen = false;
        _invalidatedTickCount++;
        return TickPublication.Invalidated(_tick, reason);
    }

    /// <summary>
    /// Ends the published batches' lease and releases both event buffers.
    /// </summary>
    /// <param name="domainEvents">The tick's domain buffer.</param>
    /// <param name="presentationEvents">The tick's presentation buffer.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <remarks>
    /// doc 115 § Cross-boundary contract registry: "Producers may reuse internal buffers only after the
    /// consumer-facing snapshot/batch lifetime has ended under an explicit buffer-lease contract." This is
    /// that contract's end point, named so a caller cannot reuse a batch view by accident. The domain
    /// buffer refuses release if any record is unconsumed, so the lease cannot end over a dropped fact.
    /// </remarks>
    public void ReleaseTick(DomainEventBuffer domainEvents, PresentationEventBuffer presentationEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        ArgumentNullException.ThrowIfNull(presentationEvents);
        domainEvents.Release();
        presentationEvents.Release();
    }

    private void RequireOpenTick()
    {
        if (!_isTickOpen)
        {
            throw new InvalidOperationException(
                "no tick is open; doc 10 § System phase ordering publishes in phase 14 of a tick");
        }
    }

    private void RequireBufferTick(bool isOpen, long bufferTick, string parameterName)
    {
        if (!isOpen)
        {
            throw new ArgumentException(
                "the buffer is not open for a tick, so it cannot be published",
                parameterName);
        }

        if (bufferTick != _tick)
        {
            throw new ArgumentException(
                "the buffer is open for tick "
                    + bufferTick.ToString(CultureInfo.InvariantCulture)
                    + " but the publisher is on tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + "; a tick result is assembled from one tick's buffers only",
                parameterName);
        }
    }

    private static void RequireFinite(double component, string parameterName)
    {
        if (!double.IsFinite(component))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                component,
                "a staged transform component must be finite");
        }
    }
}
