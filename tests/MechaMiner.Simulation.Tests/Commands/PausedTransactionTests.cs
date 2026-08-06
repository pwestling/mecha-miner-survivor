using System;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Commands;

/// <summary>
/// Pins the paused transaction shell of <c>CTR-RUN-003</c>: atomic commit between ticks, a stale expected
/// state version that changes nothing, an idempotency key that makes a replay observe the applied result, and
/// a replacement snapshot published before resumption.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-004-007</c>, <c>VER-SIM-004-008</c>, <c>VER-SIM-004-009</c>,
/// <c>VER-SIM-004-010</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Paused transactions and § Presentation snapshot;
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract;
/// <c>CTR-RUN-003</c> in <c>docs/technical/115-component-contract-and-schema-registry.md</c> §
/// Cross-boundary contract registry.
///
/// The domain validations doc 20 § Paused transactions lists - ownership, availability, slot capacity,
/// duplication, cost, prerequisites, branch exclusivity, integer overflow - are deliberately not asserted
/// here. <c>tests/verification/SIM-004.json</c> reserves them to the packages that own fabrication, relics,
/// and PowerUps; the shell only has to make a domain refusal a rejection that changed nothing, which
/// <see cref="CommitIsAllOrNothingBetweenTicks"/> does assert.
/// </remarks>
[TestFixture]
internal sealed class PausedTransactionTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-004-007</c>.
    ///
    /// An accepted request publishes exactly one new state version with its domain event, every kind of
    /// refusal leaves the whole authoritative rendering byte-identical, no tick commits across the
    /// transaction, and the gate refuses to run at all unless the run is blocked and no tick is in flight.
    /// </summary>
    [Test]
    public void CommitIsAllOrNothingBetweenTicks()
    {
        CommandFixture fixture = new();
        OpenAPauseAfterOneTick(fixture);

        AssertATransactionCannotRunWhileATickCould(fixture);
        AssertEveryRefusalChangesNothing(fixture);
        AssertAnAcceptedTransactionAdvancesExactlyOnce(fixture);
    }

    /// <summary>
    /// Verification: <c>VER-SIM-004-008</c>.
    ///
    /// A request raised against a superseded view is refused with
    /// <see cref="TransactionRejectionReason.StaleExpectedStateVersion"/>, changes nothing, and reports the
    /// authoritative version so the caller learns why - and the same request re-raised against the current
    /// version is accepted, so the refusal was about the version.
    /// </summary>
    [Test]
    public void AStaleExpectedStateVersionChangesNothing()
    {
        CommandFixture fixture = new();
        OpenAPauseAfterOneTick(fixture);

        long capturedViewVersion = fixture.Gate.TransactionStateVersion;
        PausedTransactionResult first = fixture.Apply(
            CommandFixture.InstallRequest(capturedViewVersion, clientCommandSequence: 0));
        Assert.That(first.IsAccepted, Is.True, "the first transaction applies against the captured view");

        // A second UI action raised against the view captured when the pause opened. The view is now one
        // version behind, which is exactly doc 10 § Pause contract's stale transaction.
        PausedTransactionRequest stale = CommandFixture.InstallRequest(
            capturedViewVersion,
            clientCommandSequence: 1);

        string before = fixture.Gate.RenderAuthoritative();
        SnapshotVersion snapshotBefore = fixture.Publisher.LatestVersion;
        long appendedBefore = fixture.DomainEvents.AppendedInRun;
        int validatorCallsBefore = fixture.DomainValidatorInvocations;

        PausedTransactionResult refused = fixture.Apply(stale);

        string after = fixture.Gate.RenderAuthoritative();

        CommandContractAssertions.NothingAuthoritativeChanged(
            "a request carrying a superseded expected state version",
            before,
            after);

        Expect.Multiple(() =>
        {
            Assert.That(refused.IsRejected, Is.True, "a stale request is refused");
            Assert.That(
                refused.Reason,
                Is.EqualTo(TransactionRejectionReason.StaleExpectedStateVersion),
                "with the typed reason doc 10 § Pause contract requires for UI presentation");
            Assert.That(refused.WasApplied, Is.False, "and nothing was applied");
            Assert.That(
                refused.StateVersion,
                Is.EqualTo(fixture.Gate.TransactionStateVersion),
                "the result reports the authoritative version, so the caller can refresh its view");
            Assert.That(
                refused.Detail,
                Does.Contain(capturedViewVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                "and says which version it expected");
            Assert.That(
                refused.PublishedSnapshotVersion.IsPublished,
                Is.False,
                "a refusal publishes no snapshot");
            Assert.That(
                fixture.Publisher.LatestVersion,
                Is.EqualTo(snapshotBefore),
                "so the published version did not move");
            Assert.That(
                fixture.DomainEvents.AppendedInRun,
                Is.EqualTo(appendedBefore),
                "and no domain event was emitted");
            Assert.That(
                fixture.DomainValidatorInvocations,
                Is.EqualTo(validatorCallsBefore),
                "the domain rule was not even consulted, because the version check precedes it");
            Assert.That(refused.HasAppliedEvent, Is.False, "and the result carries no applied event");
        });

        Expect.Throws<InvalidOperationException>(() => { _ = refused.AppliedEvent; });

        // The contrast that makes the refusal specific: the identical action re-raised against the refreshed
        // version is accepted, so the shell was refusing the version and not the request.
        PausedTransactionResult reraised = fixture.Apply(
            stale.WithExpectedStateVersion(fixture.Gate.TransactionStateVersion));

        Expect.Multiple(() =>
        {
            Assert.That(reraised.IsAccepted, Is.True, "the same action against the current version applies");
            Assert.That(
                reraised.StateVersion,
                Is.EqualTo(first.StateVersion + 1),
                "and advances the version by one more");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-004-009</c>.
    ///
    /// A second submission carrying the same idempotency key returns the first result, the state version
    /// advanced once, and the domain event was emitted once - while a different key against the refreshed
    /// version is applied, so deduplication is keyed on the sequence rather than refusing everything.
    /// </summary>
    [Test]
    public void ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult()
    {
        CommandFixture fixture = new();
        OpenAPauseAfterOneTick(fixture);

        long versionBefore = fixture.Gate.TransactionStateVersion;
        long appendedBefore = fixture.DomainEvents.AppendedInRun;

        PausedTransactionRequest request = CommandFixture.InstallRequest(versionBefore, clientCommandSequence: 7);
        PausedTransactionResult first = fixture.Apply(request);
        SnapshotVersion snapshotAfterFirst = fixture.Publisher.LatestVersion;
        string afterFirst = fixture.Gate.RenderAuthoritative();

        PausedTransactionResult replay = fixture.Apply(request);
        string afterReplay = fixture.Gate.RenderAuthoritative();

        CommandContractAssertions.StateVersionAdvancedExactlyOnce(
            "two submissions of one idempotency key",
            versionBefore,
            fixture.Gate.TransactionStateVersion);
        CommandContractAssertions.NothingAuthoritativeChanged(
            "a replay of an applied transaction",
            afterFirst,
            afterReplay);

        Expect.Multiple(() =>
        {
            Assert.That(first.IsAccepted, Is.True, "the first submission applies");
            Assert.That(replay.IsAccepted, Is.False, "the second does not apply again");
            Assert.That(
                replay.Reason,
                Is.EqualTo(TransactionRejectionReason.AlreadyApplied),
                "it is refused as already applied");
            Assert.That(
                replay.WasApplied,
                Is.True,
                "but it still reports that the action happened, which is what makes it observable rather "
                    + "than merely refused");
            Assert.That(
                replay.ReportsTheSameApplicationAs(first),
                Is.True,
                "and it reports the first result: same version, same event, same snapshot");
            Assert.That(replay.StateVersion, Is.EqualTo(first.StateVersion), "the same state version");
            Assert.That(replay.AppliedEvent, Is.EqualTo(first.AppliedEvent), "the same domain event");
            Assert.That(
                replay.PublishedSnapshotVersion,
                Is.EqualTo(first.PublishedSnapshotVersion),
                "and the same replacement snapshot");
            Assert.That(
                replay.DomainEventCount,
                Is.EqualTo(first.DomainEventCount),
                "with the same event count");
            Assert.That(
                fixture.DomainEvents.AppendedInRun,
                Is.EqualTo(appendedBefore + 1),
                "exactly one domain event was appended across both submissions");
            Assert.That(
                fixture.Publisher.LatestVersion,
                Is.EqualTo(snapshotAfterFirst),
                "and the replay published nothing further");
            Assert.That(
                fixture.Gate.AppliedTransactionCount,
                Is.EqualTo(1L),
                "one application, two submissions");
            Assert.That(
                fixture.Gate.TransactionRejectionCount(TransactionRejectionReason.AlreadyApplied),
                Is.EqualTo(1L),
                "and the replay was counted as such");
        });

        // The contrast: a genuinely different key against the refreshed version is applied, so the
        // deduplication is on the key rather than a blanket refusal after the first transaction.
        PausedTransactionResult second = fixture.Apply(
            CommandFixture.InstallRequest(fixture.Gate.TransactionStateVersion, clientCommandSequence: 8));

        Expect.Multiple(() =>
        {
            Assert.That(second.IsAccepted, Is.True, "a new idempotency key is applied");
            Assert.That(
                second.StateVersion,
                Is.EqualTo(first.StateVersion + 1),
                "advancing the version once more");
            Assert.That(
                fixture.Gate.AppliedTransactionCount,
                Is.EqualTo(2L),
                "for two applications in total");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-004-010</c>.
    ///
    /// An accepted transaction publishes its replacement snapshot inside the call, before the caller can clear
    /// a pause reason; the snapshot the pause opened over is still intact in the previous page; and a refused
    /// transaction publishes nothing at all.
    /// </summary>
    [Test]
    public void ReplacementSnapshotIsPublishedBeforeResumption()
    {
        CommandFixture fixture = new();
        OpenAPauseAfterOneTick(fixture);

        PresentationSnapshot? preTransaction = fixture.Publisher.Latest;
        Assert.That(preTransaction, Is.Not.Null, "the pause opened over a published snapshot");
        string preTransactionRendering = preTransaction!.Render();
        SnapshotVersion versionBeforeTransaction = fixture.Publisher.LatestVersion;

        // A refusal first, so "published before resumption" is measured against a case that publishes nothing.
        fixture.DomainAccepts = false;
        PausedTransactionResult refused = fixture.Apply(
            CommandFixture.InstallRequest(fixture.Gate.TransactionStateVersion, clientCommandSequence: 0));
        SnapshotVersion versionAfterRefusal = fixture.Publisher.LatestVersion;

        fixture.DomainAccepts = true;
        PausedTransactionResult accepted = fixture.Apply(
            CommandFixture.InstallRequest(fixture.Gate.TransactionStateVersion, clientCommandSequence: 1));

        Expect.Multiple(() =>
        {
            Assert.That(
                refused.Reason,
                Is.EqualTo(TransactionRejectionReason.DomainRefused),
                "the refusal was the domain's");
            Assert.That(
                versionAfterRefusal,
                Is.EqualTo(versionBeforeTransaction),
                "a refused transaction publishes no replacement snapshot");
            Assert.That(accepted.IsAccepted, Is.True, "the accepted one applies");
            Assert.That(
                accepted.PublishedSnapshotVersion,
                Is.EqualTo(fixture.Publisher.LatestVersion),
                "and the version it reports is the one the publisher now holds");
            Assert.That(
                accepted.PublishedSnapshotVersion,
                Is.GreaterThan(versionBeforeTransaction),
                "which is later than the one the pause opened over");
            Assert.That(
                fixture.Clock.IsBlocking,
                Is.True,
                "the run is still blocked, so the snapshot was published strictly before resumption");
            Assert.That(
                fixture.Clock.BlockingReasons.Contains(PauseReason.Fabrication),
                Is.True,
                "by the very reason the transaction was raised under");
            Assert.That(
                fixture.Publisher.Previous!.Render(),
                Is.EqualTo(preTransactionRendering).Using(StringComparer.Ordinal),
                "the snapshot the pause opened over is still readable as the previous page, so presentation "
                    + "holding it is not left without a state across the transaction");
            Assert.That(
                fixture.Publisher.Latest,
                Is.Not.SameAs(preTransaction),
                "and the replacement is a different snapshot from the one the pause opened over, not the "
                    + "same object edited in place");
            Assert.That(
                fixture.Publisher.Latest!.Render(),
                Is.Not.EqualTo(preTransactionRendering).Using(StringComparer.Ordinal),
                "carrying different staged state, so it is a real replacement");
        });

        // Resumption. The version presentation holds at the moment the first tick may run is the one the
        // transaction published, so no tick ran against a state presentation had not been given.
        PauseTransitionResult cleared = fixture.Clock.Clear(PauseReason.Fabrication);
        SnapshotVersion versionAtResumption = fixture.Publisher.LatestVersion;
        fixture.Clock.CommitTick();

        Expect.Multiple(() =>
        {
            Assert.That(cleared.ChangedTheSet, Is.True, "clearing the fabrication reason resumes the run");
            Assert.That(fixture.Clock.IsBlocking, Is.False, "with no blocking reason left");
            Assert.That(
                versionAtResumption,
                Is.EqualTo(accepted.PublishedSnapshotVersion),
                "and the version at the instant of resumption is the transaction's replacement");
            Assert.That(
                fixture.Clock.CommittedTickCount,
                Is.EqualTo(2L),
                "the tick after resumption is the first to commit since the pause opened");
        });
    }

    /// <summary>
    /// Runs one ordinary tick and opens a fabrication pause over it, which is the state doc 10 § Pause
    /// contract describes: "Opening fabrication or relic resolution captures an immutable view of the relevant
    /// authoritative state."
    /// </summary>
    private static void OpenAPauseAfterOneTick(CommandFixture fixture)
    {
        fixture.Gate.BeginTick(SimulationTick.Zero);
        fixture.Gate.TryAdmit(CommandFixture.Envelope(0, 0, 1.0, 0.0), out CommandRejection _);
        fixture.Gate.FreezeTick();
        fixture.PublishTick(0);
        fixture.Clock.CommitTick();
        fixture.Clock.Raise(PauseReason.Fabrication);
    }

    /// <summary>
    /// The three structural refusals: not blocked, a tick's admission window open, and a publisher from
    /// another run. None of them is a rejection reason, because each is a defect in the host rather than
    /// input a player supplied.
    /// </summary>
    private static void AssertATransactionCannotRunWhileATickCould(CommandFixture fixture)
    {
        PausedTransactionRequest request = CommandFixture.InstallRequest(
            fixture.Gate.TransactionStateVersion,
            clientCommandSequence: 0);

        InvalidOperationException notBlocked = Expect.Throws<InvalidOperationException>(
            () => fixture.Gate.Apply(
                request,
                PauseReasonSet.Empty,
                fixture.StageReplacementState,
                fixture.Publisher,
                fixture.DomainEvents,
                fixture.PresentationEvents,
                PresentationCoalescingPolicy.Verbatim));

        fixture.Gate.BeginTick(new SimulationTick(1));
        InvalidOperationException tickOpen = Expect.Throws<InvalidOperationException>(
            () => fixture.Apply(request));
        fixture.Gate.FreezeTick();

        SnapshotPublisher otherRun = new(
            CommandFixture.ForeignRunSession,
            visibleEntityCapacity: 1,
            domainEventCapacity: 1,
            presentationEventCapacity: 1);
        ArgumentException foreignPublisher = Expect.Throws<ArgumentException>(
            () => fixture.Gate.Apply(
                request,
                fixture.Clock.BlockingReasons,
                fixture.StageReplacementState,
                otherRun,
                fixture.DomainEvents,
                fixture.PresentationEvents,
                PresentationCoalescingPolicy.Verbatim));

        Expect.Multiple(() =>
        {
            Assert.That(
                notBlocked.Message,
                Does.Contain("not blocked"),
                "a transaction refuses to commit while a tick could be running");
            Assert.That(
                tickOpen.Message,
                Does.Contain("admission window"),
                "and while a tick's phase 1 is still open");
            Assert.That(
                foreignPublisher.ParamName,
                Is.EqualTo("publisher"),
                "and it will not publish through another run's publisher");
            Assert.That(
                fixture.Gate.AppliedTransactionCount,
                Is.Zero,
                "none of the three applied anything");
        });
    }

    /// <summary>Every typed refusal, each checked to have changed nothing at all.</summary>
    private static void AssertEveryRefusalChangesNothing(CommandFixture fixture)
    {
        long currentVersion = fixture.Gate.TransactionStateVersion;

        AssertRefusalChangesNothing(
            fixture,
            "a request from another run session",
            PausedTransactionRequest.Create(
                CommandFixture.ForeignRunSession,
                currentVersion,
                CommandFixture.InstallActionId,
                CommandFixture.BlueprintSelectionId,
                selectionOrdinal: 1,
                clientCommandSequence: 100),
            TransactionRejectionReason.ForeignRunSession,
            expectDomainConsulted: false);

        AssertRefusalChangesNothing(
            fixture,
            "a request naming an unregistered action",
            PausedTransactionRequest.Create(
                CommandFixture.RunSession,
                currentVersion,
                CommandFixture.UnregisteredActionId,
                CommandFixture.BlueprintSelectionId,
                selectionOrdinal: 1,
                clientCommandSequence: 101),
            TransactionRejectionReason.UnknownAction,
            expectDomainConsulted: false);

        AssertRefusalChangesNothing(
            fixture,
            "an irreversible action submitted without its confirmation token",
            CommandFixture.UnconfirmedAbandonRequest(currentVersion, clientCommandSequence: 102),
            TransactionRejectionReason.ConfirmationRequired,
            expectDomainConsulted: false);

        AssertRefusalChangesNothing(
            fixture,
            "a request carrying a state version that is not the authoritative one",
            CommandFixture.InstallRequest(currentVersion + 41, clientCommandSequence: 103),
            TransactionRejectionReason.StaleExpectedStateVersion,
            expectDomainConsulted: false);

        fixture.DomainAccepts = false;
        AssertRefusalChangesNothing(
            fixture,
            "a request the owning domain component refused",
            CommandFixture.InstallRequest(currentVersion, clientCommandSequence: 104),
            TransactionRejectionReason.DomainRefused,
            expectDomainConsulted: true);
        fixture.DomainAccepts = true;

        // The irreversible action with its token is accepted, so ConfirmationRequired above was about the
        // missing token rather than about the action being unusable.
        PausedTransactionResult confirmed = fixture.Apply(
            CommandFixture.UnconfirmedAbandonRequest(currentVersion, clientCommandSequence: 105)
                .WithConfirmationToken(CommandFixture.ConfirmationToken));

        Expect.Multiple(() =>
        {
            Assert.That(
                confirmed.IsAccepted,
                Is.True,
                "the irreversible action with its confirmation token applies");
            Assert.That(
                confirmed.StateVersion,
                Is.EqualTo(currentVersion + 1),
                "advancing the version exactly once");
        });
    }

    /// <summary>An accepted transaction: one new version, one domain event, one replacement snapshot.</summary>
    private static void AssertAnAcceptedTransactionAdvancesExactlyOnce(CommandFixture fixture)
    {
        long versionBefore = fixture.Gate.TransactionStateVersion;
        long committedTicksBefore = fixture.Clock.CommittedTickCount;
        long appendedBefore = fixture.DomainEvents.AppendedInRun;
        SnapshotVersion snapshotBefore = fixture.Publisher.LatestVersion;
        int validatorCallsBefore = fixture.DomainValidatorInvocations;

        PausedTransactionResult accepted = fixture.Apply(
            CommandFixture.InstallRequest(versionBefore, clientCommandSequence: 200));

        CommandContractAssertions.StateVersionAdvancedExactlyOnce(
            "one accepted paused transaction",
            versionBefore,
            fixture.Gate.TransactionStateVersion);

        Expect.Multiple(() =>
        {
            Assert.That(accepted.IsAccepted, Is.True, "the request is accepted");
            Assert.That(accepted.WasApplied, Is.True, "and the action happened");
            Assert.That(accepted.DomainEventCount, Is.EqualTo(1), "emitting one domain event");
            Assert.That(accepted.HasAppliedEvent, Is.True, "which the result carries");
            Assert.That(
                accepted.AppliedEvent.Kind,
                Is.EqualTo(fixture.ItemInstalled),
                "of the kind the action was registered with");
            Assert.That(
                accepted.AppliedEvent.Provenance.SystemPhase,
                Is.EqualTo(CommandAdmissionGate.TransactionCommitSystemPhase),
                "stamped with the admitting phase, which is phase 1");
            Assert.That(
                fixture.DomainEvents.AppendedInRun,
                Is.EqualTo(appendedBefore + 1),
                "exactly one event reached the buffer");
            Assert.That(
                fixture.Publisher.LatestVersion,
                Is.GreaterThan(snapshotBefore),
                "and the replacement snapshot was published");
            Assert.That(
                fixture.Clock.CommittedTickCount,
                Is.EqualTo(committedTicksBefore),
                "no tick committed while the transaction was in flight");
            Assert.That(fixture.Clock.IsBlocking, Is.True, "because the run stayed blocked throughout");
            Assert.That(
                fixture.DomainValidatorInvocations,
                Is.EqualTo(validatorCallsBefore + 1),
                "and the domain rule was consulted exactly once, which is what makes the \"not consulted\" "
                    + "assertions on the earlier refusals mean something");
        });
    }

    /// <summary>Submits one request that must be refused, and asserts nothing at all moved.</summary>
    private static void AssertRefusalChangesNothing(
        CommandFixture fixture,
        string subject,
        PausedTransactionRequest request,
        TransactionRejectionReason expectedReason,
        bool expectDomainConsulted)
    {
        string before = fixture.Gate.RenderAuthoritative();
        SnapshotVersion snapshotBefore = fixture.Publisher.LatestVersion;
        long appendedBefore = fixture.DomainEvents.AppendedInRun;
        long appliedBefore = fixture.Gate.AppliedTransactionCount;
        int validatorCallsBefore = fixture.DomainValidatorInvocations;

        PausedTransactionResult result = fixture.Apply(request);

        string after = fixture.Gate.RenderAuthoritative();
        SnapshotVersion snapshotAfter = fixture.Publisher.LatestVersion;
        int validatorCallsAfter = fixture.DomainValidatorInvocations;

        CommandContractAssertions.NothingAuthoritativeChanged(subject, before, after);

        Expect.Multiple(() =>
        {
            Assert.That(result.IsRejected, Is.True, subject + " must be refused");
            Assert.That(result.Reason, Is.EqualTo(expectedReason), subject + " must report its typed reason");
            Assert.That(result.WasApplied, Is.False, subject + " must apply nothing");
            Assert.That(result.Detail, Is.Not.Empty, subject + " must say why, for UI presentation");
            Assert.That(
                snapshotAfter,
                Is.EqualTo(snapshotBefore),
                subject + " must publish no replacement snapshot");
            Assert.That(
                fixture.DomainEvents.AppendedInRun,
                Is.EqualTo(appendedBefore),
                subject + " must emit no domain event");
            Assert.That(
                fixture.Gate.AppliedTransactionCount,
                Is.EqualTo(appliedBefore),
                subject + " must not count as an application");
            Assert.That(
                validatorCallsAfter,
                Is.EqualTo(expectDomainConsulted ? validatorCallsBefore + 1 : validatorCallsBefore),
                subject + " must reach the domain rule only if every shell check passed first");
        });
    }
}
