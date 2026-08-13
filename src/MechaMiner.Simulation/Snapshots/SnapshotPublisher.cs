using System;
using System.Globalization;
using MechaMiner.Simulation.Entities;
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
    /// <exception cref="ArgumentException">
    /// The record was defaulted rather than constructed, or its identity is fenced to a different run session.
    /// </exception>
    /// <remarks>
    /// <b>Two refusals, not one.</b> <see cref="SnapshotEntity.IsPresent"/> catches only a defaulted record,
    /// whose run session is zero. A record built from a well-formed identity belonging to <em>another</em> run
    /// is present, so it passes that test while naming nothing in this run: doc 20 § Entity identity says "IDs
    /// are unique only within one run session", which makes a leaked cross-run reference indistinguishable
    /// from a live one on index and generation alone. <c>PackedEntityStore</c> needs no such check because it
    /// mints every identity it holds from its own allocator; this collection accepts records from a caller, so
    /// the fence has to be checked here or it is not checked at all.
    /// </remarks>
    public void StageVisibleEntity(in SnapshotEntity entity)
    {
        RequireOpenTick();
        if (!entity.IsPresent)
        {
            throw new ArgumentException(
                "a defaulted snapshot entity cannot be staged; use SnapshotEntity.Create",
                nameof(entity));
        }

        if (entity.Id.RunSession != _runSession)
        {
            throw new ArgumentException(
                "the staged entity "
                    + entity.Id.ToString()
                    + " is fenced to run session "
                    + entity.Id.RunSession.ToString(CultureInfo.InvariantCulture)
                    + " but this publisher publishes run session "
                    + _runSession.ToString(CultureInfo.InvariantCulture)
                    + ". doc 20 § Entity identity scopes identities to one run session, so a reference from "
                    + "another run names nothing here and must not be published as live",
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
    /// <para>
    /// The domain batch is copied first and in full: doc 20 § Domain and presentation events forbids
    /// dropping a domain event, so a domain batch that does not fit is a failed invariant rather than a
    /// truncation. The domain records are marked consumed only after they are in the published batch, which
    /// is the order doc 20 requires - "Statistics consume domain/damage records before their buffers are
    /// released."
    /// </para>
    /// <para>
    /// <b>The run-session fence is checked on the assembled batch, not at each append.</b> Neither event
    /// buffer carries a run session, and neither should: doc 115 § Mutable-state ownership matrix requires
    /// "each mutable datum has exactly one row owner", and this publisher already owns the run session, so a
    /// copy on each buffer would be a second owner of the same fact and would change every construction site
    /// to say something the publisher already knows. Checking here also matches how ordering is handled:
    /// <c>EventOrdering.AssertTotalOrder</c> deliberately checks the batch rather than each append, because a
    /// defect of this kind is invisible until the records are assembled together.
    /// </para>
    /// <para>
    /// <b>Every check runs before the page flip, and that is a hard rule rather than a preference.</b>
    /// <see cref="InvalidateTick"/> is available up to the moment <c>SnapshotDoubleBuffer.Publish</c> flips
    /// the page and this method closes the tick, and unavailable from then on: the snapshot is observable
    /// through <see cref="Buffer"/> and there is nothing left to invalidate. A throw before the flip is
    /// therefore the case <c>docs/technical/20-simulation-core.md</c> § Tick transaction settles - "an
    /// exception or invariant failure before commit invalidates the tick ... it never publishes a partial
    /// state" - while a throw after it would leave a published snapshot the run cannot retract. So both
    /// run-session fences and both capacity checks are hoisted above the flip, as are the two batch views
    /// and the policy name the result carries, and nothing below the flip can throw. That last claim is
    /// checked by <c>PostPublicationRegionTests</c> rather than left as a reading of this method.
    /// </para>
    /// <para>
    /// <b>The version reconciliation that used to sit below the flip is gone, not relocated.</b> It
    /// compared the version <c>SnapshotDoubleBuffer.Publish</c> returned against the version on the page it
    /// read back from <see cref="Latest"/>, which is a comparison that cannot be made before the write it
    /// is about. It is unnecessary now for a structural reason rather than a probabilistic one: that method
    /// returns the page it wrote, so the published snapshot and its version are one value and cannot
    /// disagree.
    /// </para>
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

        RequireDomainBatchIsOwnRunSession(domainCount);
        RequirePresentationBatchIsOwnRunSession(presentationCount);

        // Everything the result needs that does not depend on the publication is built here, above the page
        // flip, so that the region below it holds no construction and no bounds check. Both batch views
        // validate their offsets in a constructor that can throw; both are over this publisher's own arrays
        // at counts already established above, so they cannot throw here - and here is where a throw is
        // still recoverable.
        ReadOnlyMemory<DomainEvent> domainBatch = new(_domainBatch, 0, domainCount);
        ReadOnlyMemory<PresentationEvent> presentationBatch = new(_presentationBatch, 0, presentationCount);
        string coalescingPolicyName = coalescingPolicy.Name;

        // ---- the point of no return ----
        // Everything above is a refusal, a copy into this publisher's own arrays, or a value built from them:
        // nothing outside is observable and InvalidateTick is still available, so a throw above ends the run
        // through the technical-failure path with no partial state published. The page flip below makes the
        // snapshot observable through Buffer.Latest and closes the tick, after which InvalidateTick would be
        // a lie rather than a retraction. Nothing below it can throw, and that is enforced rather than
        // promised: PostPublicationRegionTests reads this method's compiled body and fails if a statement
        // after this call site constructs an object, throws, or calls any simulation member beyond the two
        // named there.
        PresentationSnapshot published = _buffer.Publish(
            _tick,
            _playerPositionX,
            _playerPositionY,
            _playerFacingRadians,
            _isTerminal,
            _stagedHud,
            new ReadOnlySpan<SnapshotEntity>(_stagedEntities, 0, _stagedEntityCount));

        domainEvents.RecordAllConsumed();
        _isTickOpen = false;

        return TickPublication.Published(
            published,
            domainBatch,
            presentationBatch,
            presentationSourceCount,
            coalescingPolicyName);
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

    /// <summary>Requires every record of the assembled domain batch to be fenced to this run session.</summary>
    /// <param name="count">How many leading records of the batch are live.</param>
    /// <remarks>
    /// One linear pass over the assembled domain batch, checking the two identities every event carries: the
    /// emitting entity of its provenance and its subject. Duplicated per event type rather than unified behind
    /// an interface for the same reason <c>EventOrdering</c>'s checks are: an indirect call per record is not
    /// affordable on the publication path.
    /// </remarks>
    private void RequireDomainBatchIsOwnRunSession(int count)
    {
        for (int index = 0; index < count; index++)
        {
            EventProvenance provenance = _domainBatch[index].Provenance;
            if (provenance.EmittingEntityId.RunSession != _runSession)
            {
                throw new InvalidOperationException(BuildForeignSessionMessage(
                    "domain", "emitting entity", provenance.EmittingEntityId, provenance));
            }

            EntityId subjectId = _domainBatch[index].SubjectId;
            if (subjectId.RunSession != _runSession)
            {
                throw new InvalidOperationException(BuildForeignSessionMessage(
                    "domain", "subject", subjectId, provenance));
            }
        }
    }

    /// <summary>
    /// Requires every record of the assembled presentation batch to be fenced to this run session.
    /// </summary>
    /// <param name="count">How many leading records of the batch are live.</param>
    /// <remarks>
    /// The same check as <see cref="RequireDomainBatchIsOwnRunSession(int)"/>, whose remarks give the reason.
    /// It runs over the coalesced batch, so a policy that merged a foreign record into a local one is caught
    /// through whichever identity survived the merge.
    /// </remarks>
    private void RequirePresentationBatchIsOwnRunSession(int count)
    {
        for (int index = 0; index < count; index++)
        {
            EventProvenance provenance = _presentationBatch[index].Provenance;
            if (provenance.EmittingEntityId.RunSession != _runSession)
            {
                throw new InvalidOperationException(BuildForeignSessionMessage(
                    "presentation", "emitting entity", provenance.EmittingEntityId, provenance));
            }

            EntityId subjectId = _presentationBatch[index].SubjectId;
            if (subjectId.RunSession != _runSession)
            {
                throw new InvalidOperationException(BuildForeignSessionMessage(
                    "presentation", "subject", subjectId, provenance));
            }
        }
    }

    /// <summary>Builds the failed-invariant message naming the record and the identity that is foreign.</summary>
    /// <param name="channel">Which batch the record came from, for the message.</param>
    /// <param name="role">Which of the record's two identities is foreign, for the message.</param>
    /// <param name="offender">The foreign identity.</param>
    /// <param name="provenance">The offending record's provenance, which locates it in the batch.</param>
    private string BuildForeignSessionMessage(
        string channel,
        string role,
        EntityId offender,
        EventProvenance provenance)
    {
        return "the "
            + channel
            + " event at tick "
            + provenance.Tick.ToString(CultureInfo.InvariantCulture)
            + " sequence "
            + provenance.Sequence.ToString(CultureInfo.InvariantCulture)
            + " names "
            + offender.ToString()
            + " as its "
            + role
            + ", which is fenced to run session "
            + offender.RunSession.ToString(CultureInfo.InvariantCulture)
            + " rather than to this publisher's run session "
            + _runSession.ToString(CultureInfo.InvariantCulture)
            + ". doc 20 § Entity identity scopes identities to one run session, so the batch would carry a "
            + "reference that resolves to nothing; doc 20 § Tick transaction makes that a failed invariant "
            + "rather than something to publish.";
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
