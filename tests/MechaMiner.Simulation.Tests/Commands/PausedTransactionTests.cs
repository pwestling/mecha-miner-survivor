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
    /// Verification: <c>VER-SIM-004-009</c>.
    ///
    /// A spent idempotency key carrying a <em>different</em> action is refused as
    /// <see cref="TransactionRejectionReason.SequenceRegression"/> and reports that nothing was applied,
    /// rather than being answered with the earlier application's result.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What the replay path answers, and what it must not.</b>
    /// <see cref="PausedTransactionResult.WasApplied"/> is documented as "the property a caller uses to
    /// decide whether the action happened", and <c>CMP-UI-001</c> in
    /// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Cross-boundary contract
    /// registry is the caller that reads it. A replay built for a submission naming another action answered
    /// that question yes about an action nobody submitted, and handed back the earlier submission's
    /// <see cref="PausedTransactionResult.ActionId"/> as though it were this one's.
    /// </para>
    /// <para>
    /// <b>The asymmetry was inside one type.</b> <c>TryAdmit</c> already refuses the identical reuse -
    /// a sequence "already spent" on another tick - as
    /// <see cref="CommandRejectionReason.SequenceRegression"/>, on the grounds that reusing it "would make
    /// the run's command sequence ambiguous". <c>CMP-SIM-002</c> is one component with one row of admitted
    /// sequence and idempotency history, so both halves now answer the ambiguity the same way and by the
    /// same name.
    /// </para>
    /// </remarks>
    [Test]
    public void ASpentIdempotencyKeyCarryingADifferentActionIsRefusedRatherThanReplayed()
    {
        CommandFixture fixture = new();
        OpenAPauseAfterOneTick(fixture);

        const long sharedSequence = 55L;
        PausedTransactionResult installed = fixture.Apply(
            CommandFixture.InstallRequest(fixture.Gate.TransactionStateVersion, sharedSequence));
        Assert.That(installed.IsAccepted, Is.True, "the install applies and spends the sequence");

        // The same key, the same current state version, a different action: nothing about this submission
        // says "stale view", and everything about it says "this key is not yours".
        PausedTransactionRequest differentAction = CommandFixture
            .UnconfirmedAbandonRequest(fixture.Gate.TransactionStateVersion, sharedSequence)
            .WithConfirmationToken(CommandFixture.ConfirmationToken);

        string before = fixture.Gate.RenderAuthoritative();
        SnapshotVersion snapshotBefore = fixture.Publisher.LatestVersion;
        long appendedBefore = fixture.DomainEvents.AppendedInRun;
        int validatorCallsBefore = fixture.DomainValidatorInvocations;

        PausedTransactionResult refused = fixture.Apply(differentAction);

        string after = fixture.Gate.RenderAuthoritative();

        CommandContractAssertions.NothingAuthoritativeChanged(
            "a spent client command sequence reused for a different action",
            before,
            after);

        Expect.Multiple(() =>
        {
            Assert.That(refused.IsRejected, Is.True, "the reuse is refused");
            Assert.That(
                refused.Reason,
                Is.EqualTo(TransactionRejectionReason.SequenceRegression),
                "by the same name the active half of this gate gives the same reuse");
            Assert.That(
                refused.WasApplied,
                Is.False,
                "and it reports that this action did not happen, which is the whole point: WasApplied is "
                    + "what CMP-UI-001 reads to decide whether its action was carried out");
            Assert.That(
                refused.ActionId,
                Is.EqualTo(CommandFixture.AbandonActionId),
                "the result names the action that was submitted, not the one that was applied earlier");
            Assert.That(
                refused.HasAppliedEvent,
                Is.False,
                "and carries no domain event, unlike a replay, which carries the earlier one");
            Assert.That(
                refused.Detail,
                Does.Contain(CommandFixture.InstallActionId),
                "the detail names the action the sequence was spent on, so a caller can see the collision");
            Assert.That(
                refused.Detail,
                Does.Contain("fresh sequence"),
                "and says what to do instead, because the history is never evicted and refreshing the view "
                    + "cannot help");
            Assert.That(
                fixture.Gate.TransactionRejectionCount(TransactionRejectionReason.AlreadyApplied),
                Is.Zero,
                "it is not counted as a replay");
            Assert.That(
                fixture.Gate.TransactionRejectionCount(TransactionRejectionReason.SequenceRegression),
                Is.EqualTo(1L),
                "it is counted as the regression it is");
            Assert.That(
                fixture.Publisher.LatestVersion,
                Is.EqualTo(snapshotBefore),
                "nothing was published");
            Assert.That(
                fixture.DomainEvents.AppendedInRun,
                Is.EqualTo(appendedBefore),
                "and no domain event was emitted");
            Assert.That(
                fixture.DomainValidatorInvocations,
                Is.EqualTo(validatorCallsBefore),
                "the domain rule was not consulted: the refusal precedes registration and confirmation");
        });

        // The contrast that keeps the refusal specific: the same key with the same action is still a replay,
        // and the same different action under a fresh key is applied.
        PausedTransactionResult replay = fixture.Apply(
            CommandFixture.InstallRequest(fixture.Gate.TransactionStateVersion, sharedSequence));
        PausedTransactionResult freshKey = fixture.Apply(
            CommandFixture
                .UnconfirmedAbandonRequest(fixture.Gate.TransactionStateVersion, sharedSequence + 1)
                .WithConfirmationToken(CommandFixture.ConfirmationToken));

        Expect.Multiple(() =>
        {
            Assert.That(
                replay.Reason,
                Is.EqualTo(TransactionRejectionReason.AlreadyApplied),
                "the same action under the spent key is still the replay VER-SIM-004-009 requires");
            Assert.That(replay.WasApplied, Is.True, "and still reports that the action happened");
            Assert.That(
                replay.ReportsTheSameApplicationAs(installed),
                Is.True,
                "carrying the first result through unchanged");
            Assert.That(
                freshKey.IsAccepted,
                Is.True,
                "and the refused action applies under a fresh sequence, so the refusal was about the key "
                    + "rather than about the action");
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
    /// Verification: <c>VER-SIM-004-013</c>.
    ///
    /// A commit that throws part way through invalidates the tick instead of leaving it open: for a throw from
    /// the staging callback and for a domain buffer handed in already open for another tick, nothing is
    /// published, the whole authoritative rendering is byte-identical, the publisher's tick is invalidated and
    /// counted, and both a subsequent <c>BeginTick</c> and a retry of the same transaction succeed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The last of those assertions is the point of the test.</b> "Nothing was published" held before this
    /// path existed; what did not hold was that the failure could end. A commit that threw left the
    /// publisher's tick open forever, so every later <c>BeginTick</c> and every retry threw "tick N is still
    /// open", <c>InvalidatedTickCount</c> stayed at zero, and the run wedged instead of ending through the
    /// technical-failure path. A test that only asserted the absence of a publication would have passed
    /// against that.
    /// </para>
    /// <para>
    /// <c>TR-RUN-007</c> in <c>docs/technical/112-normative-requirement-index.md</c> § Foundation and runtime
    /// states the requirement unqualified: "A run technical failure preserves the existing profile and does
    /// not publish partial state". <c>docs/technical/20-simulation-core.md</c> § Mid-commit invalidation is the
    /// mechanism for this half of the tick, and is what <c>TR-RUN-007</c> now cites: it requires the tick to be
    /// invalidated, only the commit's own fully consumed buffers to be released, and the exception to be
    /// rethrown unchanged, which is exactly what the two routes below assert.
    /// </para>
    /// <para>
    /// <b>Two routes, because they fail at different depths.</b> The staging callback throws with the
    /// publisher's tick open and neither event buffer opened yet, which is the shallowest possible failure
    /// past the point of no recovery. The already-open domain buffer throws one statement later, with a
    /// buffer the transaction does not own in play, which is what makes "release only what this commit
    /// opened" observable rather than a claim.
    /// </para>
    /// </remarks>
    [Test]
    public void AFailedCommitInvalidatesTheTickInsteadOfWedgingTheRun()
    {
        AssertAThrowingStagingCallbackInvalidatesTheTick();
        AssertADomainBufferOpenForAnotherTickInvalidatesTheTick();
    }

    /// <summary>
    /// Verification: <c>VER-SIM-004-013</c>.
    ///
    /// A commit that fails after it has opened the presentation buffer discards that buffer, releases the
    /// domain buffer it opened and left empty, invalidates its tick, and leaves the run able to continue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The third mid-commit route, and the only one that reaches the recovery's presentation-buffer branch.
    /// The other two fail before <c>PresentationEventBuffer.BeginTick</c> is ever called, so the buffer is
    /// closed either way and deleting the discard changed nothing they assert. Here the commit opens it and
    /// then fails, which is the case doc 20 § Mid-commit invalidation rules on: the buffers this commit
    /// "itself opened and left with nothing unconsumed" are released, and presentation records are
    /// disposable, so this one is discarded outright rather than kept as evidence.
    /// </para>
    /// <para>
    /// The failure is injected through the staging callback - the one outward call a commit makes - opening
    /// the presentation buffer for another tick. That is a caller defect, which is what the recovery path
    /// exists for; nothing internal is reached for and no production type is stubbed.
    /// </para>
    /// </remarks>
    [Test]
    public void AFailedCommitDiscardsThePresentationBufferItOpened()
    {
        CommandFixture fixture = new();
        OpenAPauseAfterOneTick(fixture);

        string before = fixture.Gate.RenderAuthoritative();
        SnapshotVersion snapshotBefore = fixture.Publisher.LatestVersion;
        long versionBefore = fixture.Gate.TransactionStateVersion;
        long appendedBefore = fixture.DomainEvents.AppendedInRun;
        long invalidatedBefore = fixture.Publisher.InvalidatedTickCount;

        PausedTransactionRequest request = CommandFixture.InstallRequest(
            versionBefore,
            clientCommandSequence: 302);

        fixture.StagingOpensThePresentationBuffer = true;
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => fixture.Apply(request));
        fixture.StagingOpensThePresentationBuffer = false;

        string after = fixture.Gate.RenderAuthoritative();

        CommandContractAssertions.NothingAuthoritativeChanged(
            "a commit that failed after opening the presentation buffer",
            before,
            after);

        Expect.Multiple(() =>
        {
            Assert.That(
                failure.Message,
                Does.Contain(CommandFixture.StrayPresentationTick.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)),
                "the presentation buffer must be what refused, so the failure is the one this route injects");
            Assert.That(
                fixture.PresentationEvents.IsOpenForTick,
                Is.False,
                "the presentation buffer this commit opened must be discarded, or the next tick cannot open "
                    + "one and the run wedges on a buffer nobody owns");
            Assert.That(
                fixture.DomainEvents.IsOpenForTick,
                Is.False,
                "and the domain buffer this commit opened and left empty is released");
            Assert.That(
                fixture.Publisher.IsTickOpen,
                Is.False,
                "the tick the commit opened must not be left open");
            Assert.That(
                fixture.Publisher.InvalidatedTickCount,
                Is.EqualTo(invalidatedBefore + 1),
                "it must be invalidated and counted");
            Assert.That(
                fixture.Publisher.LatestVersion,
                Is.EqualTo(snapshotBefore),
                "nothing was published");
            Assert.That(
                fixture.Gate.TransactionStateVersion,
                Is.EqualTo(versionBefore),
                "the authoritative state version did not advance");
            Assert.That(
                fixture.DomainEvents.AppendedInRun,
                Is.EqualTo(appendedBefore),
                "and no domain event reached the buffer, because the failure preceded the append");
            Assert.That(
                fixture.Gate.AbandonedCommitCount,
                Is.EqualTo(1L),
                "and the gate recorded the abandoned commit");
        });

        AssertTheRunCanContinueAfterTheAbandonedCommit(fixture, request, expectedAbandonedCommits: 1L);
    }

    /// <summary>
    /// The staging callback throws: the shallowest mid-commit failure, with the publisher's tick open and
    /// neither event buffer touched.
    /// </summary>
    private static void AssertAThrowingStagingCallbackInvalidatesTheTick()
    {
        CommandFixture fixture = new();
        OpenAPauseAfterOneTick(fixture);

        string before = fixture.Gate.RenderAuthoritative();
        SnapshotVersion snapshotBefore = fixture.Publisher.LatestVersion;
        long versionBefore = fixture.Gate.TransactionStateVersion;
        long appendedBefore = fixture.DomainEvents.AppendedInRun;
        long invalidatedBefore = fixture.Publisher.InvalidatedTickCount;
        PresentationSnapshot? latestBefore = fixture.Publisher.Latest;

        PausedTransactionRequest request = CommandFixture.InstallRequest(
            versionBefore,
            clientCommandSequence: 300);

        fixture.StagingThrows = true;
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => fixture.Apply(request));
        fixture.StagingThrows = false;

        string after = fixture.Gate.RenderAuthoritative();

        CommandContractAssertions.NothingAuthoritativeChanged(
            "a commit whose staging callback threw",
            before,
            after);

        Expect.Multiple(() =>
        {
            Assert.That(
                failure.Message,
                Does.Contain(CommandFixture.StagingFailureMessage),
                "the caller must see the failure the staging callback raised, unchanged: the recovery path "
                    + "rethrows rather than wrapping, so a diagnostic names the real defect");
            Assert.That(
                fixture.Publisher.LatestVersion,
                Is.EqualTo(snapshotBefore),
                "nothing was published");
            Assert.That(
                fixture.Publisher.Latest,
                Is.SameAs(latestBefore),
                "and the snapshot presentation holds is the same object, not a page rewritten in place");
            Assert.That(
                fixture.Gate.TransactionStateVersion,
                Is.EqualTo(versionBefore),
                "the authoritative state version did not advance");
            Assert.That(
                fixture.DomainEvents.AppendedInRun,
                Is.EqualTo(appendedBefore),
                "and no domain event reached the buffer, because the failure preceded the append");
            Assert.That(
                fixture.Publisher.IsTickOpen,
                Is.False,
                "the tick the commit opened must not be left open, or no later tick and no retry can run");
            Assert.That(
                fixture.Publisher.InvalidatedTickCount,
                Is.EqualTo(invalidatedBefore + 1),
                "it must be invalidated and counted, which is how the run ends through the "
                    + "technical-failure path rather than wedging");
            Assert.That(
                fixture.Gate.AbandonedCommitCount,
                Is.EqualTo(1L),
                "and the gate must record the abandoned commit as its own diagnostic");
            Assert.That(
                fixture.Gate.Render(),
                Does.Contain("abandonedCommits=1"),
                "observable to CMP-OBS-001, not only to this test");
            Assert.That(
                fixture.DomainEvents.IsOpenForTick,
                Is.False,
                "the domain buffer was never opened by this commit, so it is left closed");
            Assert.That(
                fixture.PresentationEvents.IsOpenForTick,
                Is.False,
                "and neither was the presentation buffer");
        });

        AssertTheRunCanContinueAfterTheAbandonedCommit(fixture, request, expectedAbandonedCommits: 1L);
    }

    /// <summary>
    /// A domain buffer handed in already open for another tick: <c>DomainEventBuffer.BeginTick</c> throws
    /// after the publisher's tick has been opened.
    /// </summary>
    /// <remarks>
    /// The second route the reviewer found, and the one that shows the recovery undoes only its own half: the
    /// stray buffer belongs to whatever opened it, so it must be left open and untouched rather than
    /// released, which would drop records this transaction never owned.
    /// </remarks>
    private static void AssertADomainBufferOpenForAnotherTickInvalidatesTheTick()
    {
        CommandFixture fixture = new();
        OpenAPauseAfterOneTick(fixture);

        const long strayTick = 41L;
        fixture.DomainEvents.BeginTick(strayTick);

        string before = fixture.Gate.RenderAuthoritative();
        SnapshotVersion snapshotBefore = fixture.Publisher.LatestVersion;
        long versionBefore = fixture.Gate.TransactionStateVersion;
        long invalidatedBefore = fixture.Publisher.InvalidatedTickCount;

        PausedTransactionRequest request = CommandFixture.InstallRequest(
            versionBefore,
            clientCommandSequence: 301);

        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => fixture.Apply(request));

        string after = fixture.Gate.RenderAuthoritative();

        CommandContractAssertions.NothingAuthoritativeChanged(
            "a commit handed a domain buffer already open for another tick",
            before,
            after);

        Expect.Multiple(() =>
        {
            Assert.That(
                failure.Message,
                Does.Contain("is still open"),
                "the tick-local buffer must be what refused, so the failure is the one this route injects");
            Assert.That(
                fixture.Publisher.LatestVersion,
                Is.EqualTo(snapshotBefore),
                "nothing was published");
            Assert.That(
                fixture.Gate.TransactionStateVersion,
                Is.EqualTo(versionBefore),
                "the authoritative state version did not advance");
            Assert.That(
                fixture.Publisher.IsTickOpen,
                Is.False,
                "the tick the commit opened must not be left open");
            Assert.That(
                fixture.Publisher.InvalidatedTickCount,
                Is.EqualTo(invalidatedBefore + 1),
                "it must be invalidated and counted");
            Assert.That(
                fixture.Gate.AbandonedCommitCount,
                Is.EqualTo(1L),
                "and the abandoned commit recorded");
            Assert.That(
                fixture.DomainEvents.IsOpenForTick,
                Is.True,
                "the stray buffer was not opened by this commit, so the recovery must leave it open rather "
                    + "than releasing a buffer it does not own");
            Assert.That(
                fixture.DomainEvents.Tick,
                Is.EqualTo(strayTick),
                "still open for the tick it was open for, untouched");
            Assert.That(
                fixture.PresentationEvents.IsOpenForTick,
                Is.False,
                "while the presentation buffer, which this commit never reached, stays closed");
        });

        // The host clears its own stray buffer; the gate never does. Only then can the run continue, which is
        // the honest limit of the recovery rather than something it papers over.
        fixture.DomainEvents.Release();
        AssertTheRunCanContinueAfterTheAbandonedCommit(fixture, request, expectedAbandonedCommits: 1L);
    }

    /// <summary>
    /// Asserts that after an abandoned commit a later tick can open and the same transaction can be retried
    /// and applied.
    /// </summary>
    /// <param name="fixture">The fixture whose commit was abandoned.</param>
    /// <param name="request">The request whose commit failed, resubmitted unchanged.</param>
    /// <param name="expectedAbandonedCommits">How many commits were abandoned before the retry.</param>
    /// <remarks>
    /// The explicit <c>BeginTick</c> and the retry are both asserted because they fail for the same reason and
    /// read differently: the first is the direct statement that no tick is left open, the second is that the
    /// transaction the run was in the middle of is not permanently unrepeatable. Before this path existed
    /// both threw "tick 0 is still open; publish or invalidate it first".
    /// </remarks>
    private static void AssertTheRunCanContinueAfterTheAbandonedCommit(
        CommandFixture fixture,
        PausedTransactionRequest request,
        long expectedAbandonedCommits)
    {
        const long laterTick = 8L;
        Expect.DoesNotThrow(() => fixture.Publisher.BeginTick(laterTick));
        TickPublication probe = fixture.Publisher.InvalidateTick(
            "the probe tick exists only to prove BeginTick succeeds after an abandoned commit");

        PausedTransactionResult retry = fixture.Apply(request);

        Expect.Multiple(() =>
        {
            Assert.That(
                probe.IsPublished,
                Is.False,
                "the probe tick published nothing, so it changed no authoritative state");
            Assert.That(
                retry.IsAccepted,
                Is.True,
                "the same request resubmitted unchanged applies, so the abandoned commit left no residue "
                    + "that makes the transaction permanently unrepeatable");
            Assert.That(
                retry.StateVersion,
                Is.EqualTo(CommandAdmissionGate.InitialTransactionStateVersion + 1),
                "advancing the authoritative version exactly once, from where the failed commit left it");
            Assert.That(
                fixture.Gate.AppliedTransactionCount,
                Is.EqualTo(1L),
                "one application in total: the failed commit counted as none");
            Assert.That(
                fixture.Gate.AbandonedCommitCount,
                Is.EqualTo(expectedAbandonedCommits),
                "and the retry did not add another abandoned commit");
            Assert.That(
                fixture.DomainEvents.IsOpenForTick,
                Is.False,
                "the retry released the buffers it opened, so the run is back in a clean state");
            Assert.That(
                fixture.PresentationEvents.IsOpenForTick,
                Is.False);
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

    /// <summary>Every typed refusal reachable from a fresh pause, each checked to have changed nothing.</summary>
    /// <remarks>
    /// The list below is written out rather than driven from <c>Enum.GetValues</c>, because each reason
    /// needs its own request shape and a loop would have to build them anyway. That makes it a hand-written
    /// claim to be exhaustive, so what it omits is stated here rather than left to the method name.
    /// <see cref="TransactionRejectionReason.AlreadyApplied"/> and
    /// <see cref="TransactionRejectionReason.SequenceRegression"/> are absent because neither is reachable
    /// from this state: both need a client command sequence already spent by an accepted transaction, which
    /// is a different fixture. They are covered, with the same no-mutation comparison, by
    /// <see cref="ReplayWithTheSameIdempotencyKeyObservesTheAppliedResult"/> and
    /// <see cref="ASpentIdempotencyKeyCarryingADifferentActionIsRefusedRatherThanReplayed"/>. Between the
    /// three, every declared member of the enum has a no-mutation assertion.
    /// </remarks>
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
