using System;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Tests.Commands;

/// <summary>
/// A real run session's worth of collaborators for the <c>SIM-004</c> tests: the gate, a run clock, and the
/// publisher and buffers a paused transaction commits through.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports every <c>VER-SIM-004-*</c> entry.
/// </para>
/// <para>
/// The publisher and the event buffers are the real <c>SIM-006</c> and <c>SIM-007</c> types rather than
/// stand-ins, so a transaction that claims to have published a replacement snapshot before resumption is
/// checked against the actual <c>CMP-SIM-003</c> that presentation reads.
/// </para>
/// </remarks>
internal sealed class CommandFixture
{
    /// <summary>The run session everything in the fixture is fenced to.</summary>
    internal const ulong RunSession = 0x5A70_0004UL;

    /// <summary>A different, equally valid run session, for the run-fence tests.</summary>
    internal const ulong ForeignRunSession = 0x5A70_BEEFUL;

    /// <summary>A reversible fabrication action: no confirmation token required.</summary>
    internal const string InstallActionId = "A-INSTALL-WEAPON";

    /// <summary>An irreversible action, which doc 20 § Paused transactions gives a confirmation token.</summary>
    internal const string AbandonActionId = "A-ABANDON-RUN";

    /// <summary>The typed selection's stable content ID.</summary>
    internal const string BlueprintSelectionId = "B-AUTOCANNON";

    /// <summary>An action identity that is deliberately never registered.</summary>
    internal const string UnregisteredActionId = "A-NOT-REGISTERED";

    /// <summary>The confirmation token an irreversible action is submitted with.</summary>
    internal const string ConfirmationToken = "T-CONFIRM-ABANDON";

    /// <summary>
    /// The message <see cref="StageReplacementState"/> throws with while <see cref="StagingThrows"/> is set.
    /// </summary>
    /// <remarks>
    /// Distinctive enough that a test can tell this failure from the gate's own
    /// <see cref="InvalidOperationException"/>s, which is what the mid-commit tests need in order to assert
    /// that the exception the caller sees is the one the staging callback raised and not one the recovery
    /// path substituted.
    /// </remarks>
    internal const string StagingFailureMessage =
        "the staging callback refused to stage the replacement state";

    /// <summary>
    /// The tick <see cref="StageReplacementState"/> opens the presentation buffer for while
    /// <see cref="StagingOpensThePresentationBuffer"/> is set.
    /// </summary>
    /// <remarks>
    /// Deliberately not the tick a transaction commits over, so the commit's own
    /// <c>PresentationEventBuffer.BeginTick</c> refuses and the failure lands after the buffer has been
    /// opened during the commit. That is the only position from which the recovery's presentation-buffer
    /// branch is reachable at all.
    /// </remarks>
    internal const long StrayPresentationTick = 41L;

    private HudViewModel _hud;
    private int _stagedStep;

    /// <summary>Builds the fixture and registers the two transaction actions.</summary>
    internal CommandFixture()
    {
        Gate = new CommandAdmissionGate(RunSession);
        Publisher = new SnapshotPublisher(
            RunSession,
            visibleEntityCapacity: 4,
            domainEventCapacity: 16,
            presentationEventCapacity: 16);
        DomainEvents = new DomainEventBuffer(initialCapacity: 4, hardMaximumCapacity: 256);
        PresentationEvents = new PresentationEventBuffer(initialCapacity: 4, hardMaximumCapacity: 256);
        Clock = new RunClock();
        ItemInstalled = EventKind.Declare(4001, "item-installed");
        RunAbandoned = EventKind.Declare(4002, "run-abandoned");
        DomainAccepts = true;

        Gate.RegisterTransactionAction(
            InstallActionId,
            ItemInstalled,
            requiresConfirmation: false,
            domainValidator: RecordAndDecide);
        Gate.RegisterTransactionAction(
            AbandonActionId,
            RunAbandoned,
            requiresConfirmation: true,
            domainValidator: RecordAndDecide);
    }

    /// <summary>The gate under test, which is <c>CMP-SIM-002</c>.</summary>
    internal CommandAdmissionGate Gate { get; }

    /// <summary>The run's publisher, which is <c>CMP-SIM-003</c>.</summary>
    internal SnapshotPublisher Publisher { get; }

    /// <summary>The domain event buffer a transaction's applied fact is appended to.</summary>
    internal DomainEventBuffer DomainEvents { get; }

    /// <summary>The presentation buffer a publication needs.</summary>
    internal PresentationEventBuffer PresentationEvents { get; }

    /// <summary>The run clock, which owns the pause reasons and the committed tick count.</summary>
    internal RunClock Clock { get; }

    /// <summary>The declared domain event kind an install emits.</summary>
    internal EventKind ItemInstalled { get; }

    /// <summary>The declared domain event kind an abandonment emits.</summary>
    internal EventKind RunAbandoned { get; }

    /// <summary>
    /// What the registered domain validators decide. Set false to exercise
    /// <see cref="TransactionRejectionReason.DomainRefused"/>.
    /// </summary>
    internal bool DomainAccepts { get; set; }

    /// <summary>
    /// How many times a registered domain validator has been invoked.
    /// </summary>
    /// <remarks>
    /// The observable that makes "the domain rule is the last check before the commit" assertable: a
    /// refusal decided earlier must leave this unchanged, and an acceptance must raise it. Asserting only
    /// that it stays at zero would pass even if the validators were never wired up at all, so every test
    /// that checks it also drives a case that raises it.
    /// </remarks>
    internal int DomainValidatorInvocations { get; private set; }

    /// <summary>
    /// Whether <see cref="StageReplacementState"/> throws instead of staging anything.
    /// </summary>
    /// <remarks>
    /// The staging callback is the only outward call a commit makes, so it is the one place a technical
    /// failure can be injected part way through a commit without a stub standing in for a production type.
    /// It throws before staging anything, so the failure is genuinely mid-commit: the publisher's tick is
    /// open and nothing has been staged into it.
    /// </remarks>
    internal bool StagingThrows { get; set; }

    /// <summary>
    /// Whether <see cref="StageReplacementState"/> opens the presentation buffer for
    /// <see cref="StrayPresentationTick"/> instead of leaving it alone.
    /// </summary>
    /// <remarks>
    /// A caller defect, injected through the one outward call a commit makes, so that the commit fails
    /// after it has opened the presentation buffer itself. Every other injected failure fails earlier and
    /// leaves that buffer untouched, which is why the recovery's presentation branch had no test.
    /// </remarks>
    internal bool StagingOpensThePresentationBuffer { get; set; }

    /// <summary>Builds an envelope for this run session.</summary>
    /// <param name="targetTick">The tick the intent is for.</param>
    /// <param name="sequence">The producer's monotonic sequence.</param>
    /// <param name="rawInputX">The raw planar X component as sampled.</param>
    /// <param name="rawInputY">The raw planar Y component as sampled.</param>
    internal static CommandEnvelope Envelope(long targetTick, long sequence, double rawInputX, double rawInputY)
    {
        return CommandEnvelope.Create(
            RunSession,
            new SimulationTick(targetTick),
            sequence,
            rawInputX,
            rawInputY);
    }

    /// <summary>Builds an envelope that names <see cref="ForeignRunSession"/> instead.</summary>
    /// <param name="targetTick">The tick the intent is for.</param>
    /// <param name="sequence">The producer's monotonic sequence.</param>
    /// <param name="rawInputX">The raw planar X component as sampled.</param>
    /// <param name="rawInputY">The raw planar Y component as sampled.</param>
    internal static CommandEnvelope ForeignEnvelope(
        long targetTick,
        long sequence,
        double rawInputX,
        double rawInputY)
    {
        return CommandEnvelope.Create(
            ForeignRunSession,
            new SimulationTick(targetTick),
            sequence,
            rawInputX,
            rawInputY);
    }

    /// <summary>Builds a request for the reversible install action.</summary>
    /// <param name="expectedStateVersion">The version the immutable view was captured at.</param>
    /// <param name="clientCommandSequence">The idempotency key.</param>
    internal static PausedTransactionRequest InstallRequest(
        long expectedStateVersion,
        long clientCommandSequence)
    {
        return PausedTransactionRequest.Create(
            RunSession,
            expectedStateVersion,
            InstallActionId,
            BlueprintSelectionId,
            selectionOrdinal: 2,
            clientCommandSequence);
    }

    /// <summary>Builds a request for the irreversible abandon action, without its confirmation token.</summary>
    /// <param name="expectedStateVersion">The version the immutable view was captured at.</param>
    /// <param name="clientCommandSequence">The idempotency key.</param>
    internal static PausedTransactionRequest UnconfirmedAbandonRequest(
        long expectedStateVersion,
        long clientCommandSequence)
    {
        return PausedTransactionRequest.Create(
            RunSession,
            expectedStateVersion,
            AbandonActionId,
            BlueprintSelectionId,
            selectionOrdinal: 0,
            clientCommandSequence);
    }

    /// <summary>Publishes one ordinary tick, so the publisher holds a snapshot to be replaced.</summary>
    /// <param name="tick">The tick to publish.</param>
    /// <returns>The published version.</returns>
    internal SnapshotVersion PublishTick(long tick)
    {
        Publisher.BeginTick(tick);
        StageReplacementState(Publisher);
        DomainEvents.BeginTick(tick);
        PresentationEvents.BeginTick(tick);
        TickPublication publication = Publisher.Publish(
            DomainEvents,
            PresentationEvents,
            PresentationCoalescingPolicy.Verbatim);
        Publisher.ReleaseTick(DomainEvents, PresentationEvents);
        return publication.Version;
    }

    /// <summary>
    /// Stages the post-transaction authoritative state onto <paramref name="publisher"/>.
    /// </summary>
    /// <param name="publisher">The publisher whose tick is already open.</param>
    /// <remarks>
    /// The callback <see cref="CommandAdmissionGate.Apply"/> invokes on its accept path only. Each call
    /// stages visibly different values, so a test can tell a replacement snapshot from the one it replaced
    /// without reading the version.
    /// </remarks>
    internal void StageReplacementState(SnapshotPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        if (StagingThrows)
        {
            throw new InvalidOperationException(StagingFailureMessage);
        }

        if (StagingOpensThePresentationBuffer)
        {
            PresentationEvents.BeginTick(StrayPresentationTick);
        }

        _stagedStep++;
        publisher.StagePlayer(_stagedStep * 0.25, _stagedStep * -0.25, facingRadians: 0.0);
        _hud = HudViewModel.Next(
            _hud,
            authoritativeHull: 100.0,
            authoritativeArmor: 5.0,
            bankedCommonOre: 100 + _stagedStep,
            bankedHyperGold: 25,
            runClockSeconds: 1.0,
            extractionProgress: 0.10);
        publisher.StageHud(_hud);
        publisher.StageTerminalState(isTerminal: false);
    }

    /// <summary>Applies one paused transaction through the gate, with this fixture's collaborators.</summary>
    /// <param name="request">The request to submit.</param>
    internal PausedTransactionResult Apply(in PausedTransactionRequest request)
    {
        return Gate.Apply(
            request,
            Clock.BlockingReasons,
            StageReplacementState,
            Publisher,
            DomainEvents,
            PresentationEvents,
            PresentationCoalescingPolicy.Verbatim);
    }

    private bool RecordAndDecide(PausedTransactionRequest request)
    {
        DomainValidatorInvocations++;
        return DomainAccepts;
    }
}
