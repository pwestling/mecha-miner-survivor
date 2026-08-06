using System;
using System.Globalization;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Snapshots;

namespace MechaMiner.Simulation.Commands;

/// <summary>
/// <c>CTR-RUN-003</c> outbound: either a new state version with the domain event it produced and the
/// replacement snapshot it published, or a typed rejection that changed nothing.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Paused transactions: "Validation returns either a new
/// complete state/version plus domain events or a typed rejection with no mutation."
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract: a transaction "mutates the frozen
/// simulation atomically between ticks and publishes a replacement snapshot before resumption", and an
/// invalid or stale one "change[s] nothing and return[s] a typed rejection reason for UI presentation".
/// <c>CTR-RUN-003</c> in doc 115 § Cross-boundary contract registry: "all-or-nothing typed result; stale
/// preview changes nothing".
/// </para>
/// <para>
/// <b>"No mutation on rejection" needs no enumeration of mutation sites.</b> Like
/// <see cref="CommandRejection"/>, this is a readonly struct of scalars, strings, one immutable
/// <see cref="DomainEvent"/>, and one <see cref="SnapshotVersion"/>. It holds no reference to the gate, no
/// buffer, no collection and no delegate, so a holder of a rejected result has nothing to write through -
/// whether or not every mutation site in the gate was found.
/// </para>
/// <para>
/// <b><see cref="TransactionRejectionReason.AlreadyApplied"/> is a success-shaped rejection.</b>
/// <c>VER-SIM-004-009</c> requires a replay to observe the applied result, not merely to be refused, so
/// <see cref="Replayed"/> carries the original application's state version, domain event and snapshot
/// version through unchanged and only changes the reason. <see cref="WasApplied"/> is therefore true for an
/// acceptance and for a replay alike, and it is the property a caller uses to decide whether the action
/// happened - <see cref="IsAccepted"/> only says whether <em>this</em> submission was the one that did it.
/// </para>
/// <para>
/// <b>One domain event, for now.</b> doc 20 § Paused transactions says validation returns "domain events",
/// plural, and the registry note in <c>tests/verification/SIM-004.json</c> reserves the domain content to
/// the packages that own fabrication, relics, and PowerUps. The shell emits exactly the one fact it can
/// state on its own - that this action was applied at this version - and carries it here. When a domain
/// package emits its own richer batch it will append to the same
/// <c>DomainEventBuffer</c> the gate already opens, so the count reported by
/// <see cref="DomainEventCount"/> grows without this type changing shape.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-UI-001</c> in <c>game/</c> presents the
/// outcome - doc 10 § Pause contract gives the reason "for UI presentation" - and
/// <c>MechaMiner.Game.Tests</c> asserts on it. Hence <c>public</c>.
/// </para>
/// </remarks>
public readonly struct PausedTransactionResult : IEquatable<PausedTransactionResult>
{
    private readonly int _encodedReason;
    private readonly ulong _runSession;
    private readonly string? _actionId;
    private readonly long _clientCommandSequence;
    private readonly long _stateVersion;
    private readonly int _domainEventCount;
    private readonly DomainEvent _appliedEvent;
    private readonly SnapshotVersion _publishedSnapshotVersion;
    private readonly string? _detail;

    private PausedTransactionResult(
        int encodedReason,
        ulong runSession,
        string actionId,
        long clientCommandSequence,
        long stateVersion,
        int domainEventCount,
        DomainEvent appliedEvent,
        SnapshotVersion publishedSnapshotVersion,
        string detail)
    {
        _encodedReason = encodedReason;
        _runSession = runSession;
        _actionId = actionId;
        _clientCommandSequence = clientCommandSequence;
        _stateVersion = stateVersion;
        _domainEventCount = domainEventCount;
        _appliedEvent = appliedEvent;
        _publishedSnapshotVersion = publishedSnapshotVersion;
        _detail = detail;
    }

    /// <summary>Whether this value is a result at all rather than the default.</summary>
    public bool IsPresent => _runSession != 0;

    /// <summary>Whether this submission is the one that applied the transaction.</summary>
    public bool IsAccepted => IsPresent && _encodedReason == 0;

    /// <summary>Whether this submission was refused, including as a replay of an applied one.</summary>
    public bool IsRejected => IsPresent && _encodedReason != 0;

    /// <summary>
    /// Whether the action has been applied, by this submission or by an earlier one with the same
    /// idempotency key.
    /// </summary>
    public bool WasApplied =>
        IsAccepted || (IsRejected && Reason == TransactionRejectionReason.AlreadyApplied);

    /// <summary>The typed reason the submission was refused.</summary>
    /// <exception cref="InvalidOperationException">This result was accepted, or is the default.</exception>
    /// <remarks>
    /// Stored offset by one so <c>default</c> carries no reason rather than the zero member. An accepted
    /// result has no reason, so none is invented for it.
    /// </remarks>
    public TransactionRejectionReason Reason => IsRejected
        ? (TransactionRejectionReason)(_encodedReason - 1)
        : throw new InvalidOperationException(
            "this result carries no rejection reason; check IsRejected first");

    /// <summary>The run session the request was raised under.</summary>
    public ulong RunSession => _runSession;

    /// <summary>The action identity the request named.</summary>
    public string ActionId => _actionId ?? string.Empty;

    /// <summary>The idempotency key the request carried.</summary>
    public long ClientCommandSequence => _clientCommandSequence;

    /// <summary>
    /// The authoritative state version after this result: the new version when applied, and the
    /// unchanged current version when refused.
    /// </summary>
    /// <remarks>
    /// One member rather than a "before" and an "after", because a rejection has no "after" that differs
    /// and offering two would invite a caller to compare them and conclude a rejection had advanced
    /// something.
    /// </remarks>
    public long StateVersion => _stateVersion;

    /// <summary>How many domain events the application emitted; zero for a refusal that applied nothing.</summary>
    public int DomainEventCount => _domainEventCount;

    /// <summary>Whether this result carries the applied domain event.</summary>
    public bool HasAppliedEvent => _domainEventCount > 0;

    /// <summary>The domain event the application emitted.</summary>
    /// <exception cref="InvalidOperationException">Nothing was applied, so there is no event.</exception>
    public DomainEvent AppliedEvent => HasAppliedEvent
        ? _appliedEvent
        : throw new InvalidOperationException(
            "this result applied nothing, so it carries no domain event; check HasAppliedEvent first");

    /// <summary>The version of the replacement snapshot the application published.</summary>
    /// <remarks>
    /// <see cref="SnapshotVersion.Unpublished"/> for a refusal, because a refusal publishes nothing - which
    /// is what <c>VER-SIM-004-010</c>'s "before resumption" is measured against.
    /// </remarks>
    public SnapshotVersion PublishedSnapshotVersion => _publishedSnapshotVersion;

    /// <summary>What happened, in words, for a diagnostic or the UI.</summary>
    public string Detail => _detail ?? string.Empty;

    /// <summary>Builds the result of an application that committed.</summary>
    /// <param name="request">The request that was applied.</param>
    /// <param name="newStateVersion">The state version the commit produced. Must be positive.</param>
    /// <param name="appliedEvent">The domain event the commit emitted.</param>
    /// <param name="domainEventCount">How many domain events the commit emitted. Must be positive.</param>
    /// <param name="publishedSnapshotVersion">The replacement snapshot's version. Must be published.</param>
    /// <param name="detail">What was applied. Must not be blank.</param>
    /// <exception cref="ArgumentOutOfRangeException">A numeric argument is outside its domain.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> was defaulted, <paramref name="appliedEvent"/> is incomplete, or
    /// <paramref name="detail"/> is blank.
    /// </exception>
    public static PausedTransactionResult Accepted(
        in PausedTransactionRequest request,
        long newStateVersion,
        in DomainEvent appliedEvent,
        int domainEventCount,
        SnapshotVersion publishedSnapshotVersion,
        string detail)
    {
        RequirePresentRequest(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(newStateVersion, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(domainEventCount, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        if (!appliedEvent.IsComplete)
        {
            throw new ArgumentException(
                "an accepted transaction carries the complete domain event it emitted; doc 20 § Paused "
                    + "transactions returns \"a new complete state/version plus domain events\"",
                nameof(appliedEvent));
        }

        if (!publishedSnapshotVersion.IsPublished)
        {
            throw new ArgumentOutOfRangeException(
                nameof(publishedSnapshotVersion),
                publishedSnapshotVersion,
                "an accepted transaction has already published its replacement snapshot; doc 10 § Pause "
                    + "contract requires it \"before resumption\"");
        }

        return new PausedTransactionResult(
            encodedReason: 0,
            request.RunSession,
            request.ActionId,
            request.ClientCommandSequence,
            newStateVersion,
            domainEventCount,
            appliedEvent,
            publishedSnapshotVersion,
            detail);
    }

    /// <summary>Builds a typed rejection that applied nothing.</summary>
    /// <param name="reason">The typed reason. Must not be <see cref="TransactionRejectionReason.AlreadyApplied"/>.</param>
    /// <param name="request">The request that was refused.</param>
    /// <param name="unchangedStateVersion">The authoritative version, which this refusal did not change.</param>
    /// <param name="detail">Why it was refused. Must not be blank.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="reason"/> is undefined or is <see cref="TransactionRejectionReason.AlreadyApplied"/>,
    /// or <paramref name="unchangedStateVersion"/> is not positive.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="request"/> was defaulted, or <paramref name="detail"/> is blank.</exception>
    /// <remarks>
    /// <see cref="TransactionRejectionReason.AlreadyApplied"/> is refused here because a replay is not a
    /// rejection that applied nothing - it reports a result that <em>was</em> applied, and there is only one
    /// way to build one: <see cref="Replayed"/>, from the original result. That closes the route by which a
    /// caller could report "already applied" while carrying no applied state version, event, or snapshot.
    /// </remarks>
    public static PausedTransactionResult Rejected(
        TransactionRejectionReason reason,
        in PausedTransactionRequest request,
        long unchangedStateVersion,
        string detail)
    {
        RequirePresentRequest(request);
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "a rejection reason must be a defined member of TransactionRejectionReason; an undefined "
                    + "value would be an untyped refusal, which CTR-RUN-003 forbids");
        }

        if (reason == TransactionRejectionReason.AlreadyApplied)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "AlreadyApplied carries the original application's result and is built with Replayed, not "
                    + "with Rejected");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(unchangedStateVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        return new PausedTransactionResult(
            (int)reason + 1,
            request.RunSession,
            request.ActionId,
            request.ClientCommandSequence,
            unchangedStateVersion,
            domainEventCount: 0,
            appliedEvent: default,
            SnapshotVersion.Unpublished,
            detail);
    }

    /// <summary>
    /// Builds the answer to a replay: the original application's result, marked
    /// <see cref="TransactionRejectionReason.AlreadyApplied"/>.
    /// </summary>
    /// <param name="original">The result the first submission produced. Must be an accepted result.</param>
    /// <exception cref="ArgumentException"><paramref name="original"/> is not an accepted result.</exception>
    /// <remarks>
    /// Every payload field is carried through by construction rather than re-derived, so the replay cannot
    /// disagree with the original about the state version, the event, or the snapshot -
    /// <c>VER-SIM-004-009</c>'s "the second submission returns the first result" is then a property of there
    /// being one stored result, not of two computations agreeing.
    /// </remarks>
    public static PausedTransactionResult Replayed(in PausedTransactionResult original)
    {
        if (!original.IsAccepted)
        {
            throw new ArgumentException(
                "only an accepted result can be replayed; a refused submission applied nothing, so a later "
                    + "submission of the same key is not a replay",
                nameof(original));
        }

        return new PausedTransactionResult(
            (int)TransactionRejectionReason.AlreadyApplied + 1,
            original._runSession,
            original.ActionId,
            original._clientCommandSequence,
            original._stateVersion,
            original._domainEventCount,
            original._appliedEvent,
            original._publishedSnapshotVersion,
            "client command sequence "
                + original._clientCommandSequence.ToString(CultureInfo.InvariantCulture)
                + " was already applied at state version "
                + original._stateVersion.ToString(CultureInfo.InvariantCulture)
                + "; the applied result is returned rather than applied again");
    }

    /// <summary>Compares two results for exact equality of every field.</summary>
    public static bool operator ==(PausedTransactionResult left, PausedTransactionResult right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two results for inequality.</summary>
    public static bool operator !=(PausedTransactionResult left, PausedTransactionResult right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Whether this result and <paramref name="other"/> report the same application, ignoring which
    /// submission produced them.
    /// </summary>
    /// <param name="other">The other result.</param>
    /// <remarks>
    /// The comparison <c>VER-SIM-004-009</c> needs: a replay differs from the original in exactly its reason
    /// and its detail, so equality of everything else is the assertion "the second submission returns the
    /// first result".
    /// </remarks>
    public bool ReportsTheSameApplicationAs(in PausedTransactionResult other)
    {
        return _runSession == other._runSession
            && _clientCommandSequence == other._clientCommandSequence
            && _stateVersion == other._stateVersion
            && _domainEventCount == other._domainEventCount
            && _appliedEvent == other._appliedEvent
            && _publishedSnapshotVersion == other._publishedSnapshotVersion
            && string.Equals(ActionId, other.ActionId, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public bool Equals(PausedTransactionResult other)
    {
        return _encodedReason == other._encodedReason
            && ReportsTheSameApplicationAs(other)
            && string.Equals(Detail, other.Detail, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PausedTransactionResult other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(_encodedReason);
        hash.Add(_runSession);
        hash.Add(_clientCommandSequence);
        hash.Add(_stateVersion);
        hash.Add(_domainEventCount);
        hash.Add(_appliedEvent);
        hash.Add(_publishedSnapshotVersion);
        hash.Add(ActionId, StringComparer.Ordinal);
        hash.Add(Detail, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Render();
    }

    /// <summary>Renders the result as canonical invariant text for a diagnostic or a golden.</summary>
    public string Render()
    {
        if (!IsPresent)
        {
            return "transaction-result(none)";
        }

        return "transaction-result "
            + (IsAccepted ? "accepted" : "rejected " + Reason.ToString())
            + " run="
            + _runSession.ToString("X16", CultureInfo.InvariantCulture)
            + " action="
            + ActionId
            + " clientSeq="
            + _clientCommandSequence.ToString(CultureInfo.InvariantCulture)
            + " version="
            + _stateVersion.ToString(CultureInfo.InvariantCulture)
            + " events="
            + _domainEventCount.ToString(CultureInfo.InvariantCulture)
            + " snapshot="
            + _publishedSnapshotVersion.ToString();
    }

    private static void RequirePresentRequest(in PausedTransactionRequest request)
    {
        if (!request.IsPresent)
        {
            throw new ArgumentException(
                "a result answers a created request; a defaulted one names no action and no run",
                nameof(request));
        }
    }
}
