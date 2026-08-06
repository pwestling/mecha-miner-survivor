using System;
using System.Globalization;

namespace MechaMiner.Simulation.Commands;

/// <summary>
/// <c>CTR-RUN-003</c> inbound: the complete description of one state-changing menu action, carrying every
/// field doc 20 § Paused transactions requires a transaction to carry.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Paused transactions: "Every transaction carries:
/// application or run-session identity; expected state version; action identity and typed selection;
/// client command sequence for deduplication; and optional confirmation token for irreversible actions."
/// <c>CTR-RUN-003</c> in doc 115 § Cross-boundary contract registry: produced by "<c>CMP-UI-001</c> through
/// application", consumed by "<c>CMP-SIM-002</c>, owning domain component", delivered as an "immutable
/// preview version plus idempotency key; commit between ticks".
/// </para>
/// <para>
/// <b>Five of doc 20's five fields, each as its own member.</b> They are not collapsed into a payload blob,
/// because <c>CMP-SIM-002</c> has to act on four of them itself - the run fence, the idempotency key, the
/// expected version, and the confirmation token - and only the selection belongs to the domain.
/// </para>
/// <para>
/// <b>What "typed selection" is here.</b> The shell carries the selection as a stable content ID plus an
/// ordinal: <see cref="SelectionId"/> names <em>what</em> - a blueprint, a relic, a PowerUp - and
/// <see cref="SelectionOrdinal"/> names <em>which</em> - a slot, a branch, a rank step. The registry note in
/// <c>tests/verification/SIM-004.json</c> reserves the domain validations to the packages that own
/// fabrication, relics, and PowerUps, so those packages supply the richer typed selections their own actions
/// need; the shell must not guess at them now.
/// </para>
/// <para>
/// <b>Immutable at any depth.</b> A readonly struct over scalars and strings, with a private constructor and
/// validating factories, so a request that has crossed the boundary cannot be edited by either side and
/// there is no collection for doc 115's "cross-boundary payloads never expose mutable collections" to catch.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-UI-001</c> in <c>game/</c> constructs
/// these through the application coordinator, and <c>MechaMiner.Game.Tests</c> asserts on them. Hence
/// <c>public</c>.
/// </para>
/// </remarks>
public readonly struct PausedTransactionRequest : IEquatable<PausedTransactionRequest>
{
    /// <summary>The first client command sequence a UI session may submit.</summary>
    public const long FirstClientCommandSequence = 0;

    private readonly ulong _runSession;
    private readonly long _expectedStateVersion;
    private readonly string? _actionId;
    private readonly string? _selectionId;
    private readonly long _selectionOrdinal;
    private readonly long _clientCommandSequence;
    private readonly string? _confirmationToken;

    private PausedTransactionRequest(
        ulong runSession,
        long expectedStateVersion,
        string actionId,
        string selectionId,
        long selectionOrdinal,
        long clientCommandSequence,
        string? confirmationToken)
    {
        _runSession = runSession;
        _expectedStateVersion = expectedStateVersion;
        _actionId = actionId;
        _selectionId = selectionId;
        _selectionOrdinal = selectionOrdinal;
        _clientCommandSequence = clientCommandSequence;
        _confirmationToken = confirmationToken;
    }

    /// <summary>The application or run-session identity the request was raised under.</summary>
    public ulong RunSession => _runSession;

    /// <summary>
    /// The authoritative state version the immutable view was captured at when the pause opened.
    /// </summary>
    /// <remarks>
    /// doc 10 § Pause contract: "Opening fabrication or relic resolution captures an immutable view of the
    /// relevant authoritative state", and an "Invalid or stale" transaction "change[s] nothing". This is the
    /// version of that view, so the gate can tell a request raised against the state it still has from one
    /// raised against a state that has since moved.
    ///
    /// The counter itself belongs to <c>CMP-SIM-002</c>, and <c>CommandAdmissionGate.TransactionStateVersion</c>
    /// records why, including why it is not <c>CMP-PRG-001</c>'s "loadout versions". A caller fills this field
    /// from that property and never from a version of its own.
    /// </remarks>
    public long ExpectedStateVersion => _expectedStateVersion;

    /// <summary>The action identity, a stable content ID.</summary>
    public string ActionId => _actionId ?? string.Empty;

    /// <summary>The typed selection's stable content ID: what was selected.</summary>
    public string SelectionId => _selectionId ?? string.Empty;

    /// <summary>The typed selection's ordinal: which slot, branch, or rank step was selected.</summary>
    public long SelectionOrdinal => _selectionOrdinal;

    /// <summary>The client command sequence, which is this request's idempotency key within the run.</summary>
    /// <remarks>
    /// doc 20 § Paused transactions calls it a "client command sequence for deduplication" and
    /// <c>CTR-RUN-003</c> calls it an "idempotency key"; they are the same field, so it is one member rather
    /// than two that could disagree. Keyed within the run session, which the gate fences before it looks the
    /// key up, so two runs reusing sequence numbers can never collide.
    /// </remarks>
    public long ClientCommandSequence => _clientCommandSequence;

    /// <summary>The confirmation token for an irreversible action, or the empty string.</summary>
    public string ConfirmationToken => _confirmationToken ?? string.Empty;

    /// <summary>Whether a confirmation token was supplied.</summary>
    public bool HasConfirmationToken => ConfirmationToken.Length > 0;

    /// <summary>Whether this request was constructed rather than defaulted.</summary>
    public bool IsPresent => _runSession != 0;

    /// <summary>Creates a request with no confirmation token.</summary>
    /// <param name="runSession">The application or run-session identity. Must not be zero.</param>
    /// <param name="expectedStateVersion">The version the immutable view was captured at. Must be positive.</param>
    /// <param name="actionId">The action's stable content ID. Must not be blank.</param>
    /// <param name="selectionId">The selection's stable content ID. Must not be blank.</param>
    /// <param name="selectionOrdinal">The selection's ordinal. Must not be negative.</param>
    /// <param name="clientCommandSequence">
    /// The idempotency key. Must not be below <see cref="FirstClientCommandSequence"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">A numeric argument is outside its domain.</exception>
    /// <exception cref="ArgumentException">A stable ID is blank.</exception>
    /// <remarks>
    /// These are the fields no request can meaningfully omit, so a missing one throws rather than becoming a
    /// rejection: a blank action identity is not an action the gate could refuse, it is a caller that has
    /// not said what it wants. The refusals that <em>are</em> typed rejections are the ones that depend on
    /// authoritative state - the run fence, the version, the registration, the token, the domain rules - and
    /// none of them is decidable here.
    /// </remarks>
    public static PausedTransactionRequest Create(
        ulong runSession,
        long expectedStateVersion,
        string actionId,
        string selectionId,
        long selectionOrdinal,
        long clientCommandSequence)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run'; doc 20 § Paused transactions requires every "
                    + "transaction to carry an application or run-session identity");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(expectedStateVersion, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(selectionOrdinal);
        ArgumentOutOfRangeException.ThrowIfLessThan(clientCommandSequence, FirstClientCommandSequence);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectionId);

        return new PausedTransactionRequest(
            runSession,
            expectedStateVersion,
            actionId,
            selectionId,
            selectionOrdinal,
            clientCommandSequence,
            confirmationToken: null);
    }

    /// <summary>Returns a copy of this request carrying <paramref name="confirmationToken"/>.</summary>
    /// <param name="confirmationToken">The token for an irreversible action. Must not be blank.</param>
    /// <exception cref="ArgumentException"><paramref name="confirmationToken"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">This request was defaulted rather than created.</exception>
    /// <remarks>
    /// A copy rather than a setter, so the request stays immutable and the confirmed and unconfirmed forms
    /// are two values a test can hold at once - which is what <c>VER-SIM-004-007</c>'s "a rejected one
    /// leaves the state version and every field unchanged" needs in order to compare them.
    /// </remarks>
    public PausedTransactionRequest WithConfirmationToken(string confirmationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationToken);
        if (!IsPresent)
        {
            throw new InvalidOperationException(
                "a defaulted request carries no action to confirm; use Create first");
        }

        return new PausedTransactionRequest(
            _runSession,
            _expectedStateVersion,
            ActionId,
            SelectionId,
            _selectionOrdinal,
            _clientCommandSequence,
            confirmationToken);
    }

    /// <summary>
    /// Returns a copy of this request with a different expected state version, for a caller re-raising the
    /// same action against a refreshed view.
    /// </summary>
    /// <param name="expectedStateVersion">The version the refreshed view was captured at. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expectedStateVersion"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">This request was defaulted rather than created.</exception>
    public PausedTransactionRequest WithExpectedStateVersion(long expectedStateVersion)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedStateVersion, 1);
        if (!IsPresent)
        {
            throw new InvalidOperationException(
                "a defaulted request carries no action to re-raise; use Create first");
        }

        return new PausedTransactionRequest(
            _runSession,
            expectedStateVersion,
            ActionId,
            SelectionId,
            _selectionOrdinal,
            _clientCommandSequence,
            _confirmationToken);
    }

    /// <summary>
    /// Returns a copy of this request with a different client command sequence, for a caller submitting a
    /// genuinely new action rather than a replay.
    /// </summary>
    /// <param name="clientCommandSequence">
    /// The new idempotency key. Must not be below <see cref="FirstClientCommandSequence"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="clientCommandSequence"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">This request was defaulted rather than created.</exception>
    public PausedTransactionRequest WithClientCommandSequence(long clientCommandSequence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(clientCommandSequence, FirstClientCommandSequence);
        if (!IsPresent)
        {
            throw new InvalidOperationException(
                "a defaulted request carries no action to resubmit; use Create first");
        }

        return new PausedTransactionRequest(
            _runSession,
            _expectedStateVersion,
            ActionId,
            SelectionId,
            _selectionOrdinal,
            clientCommandSequence,
            _confirmationToken);
    }

    /// <summary>Compares two requests for exact equality of every field.</summary>
    public static bool operator ==(PausedTransactionRequest left, PausedTransactionRequest right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two requests for inequality.</summary>
    public static bool operator !=(PausedTransactionRequest left, PausedTransactionRequest right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(PausedTransactionRequest other)
    {
        return _runSession == other._runSession
            && _expectedStateVersion == other._expectedStateVersion
            && _selectionOrdinal == other._selectionOrdinal
            && _clientCommandSequence == other._clientCommandSequence
            && string.Equals(ActionId, other.ActionId, StringComparison.Ordinal)
            && string.Equals(SelectionId, other.SelectionId, StringComparison.Ordinal)
            && string.Equals(ConfirmationToken, other.ConfirmationToken, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PausedTransactionRequest other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(_runSession);
        hash.Add(_expectedStateVersion);
        hash.Add(_selectionOrdinal);
        hash.Add(_clientCommandSequence);
        hash.Add(ActionId, StringComparer.Ordinal);
        hash.Add(SelectionId, StringComparer.Ordinal);
        hash.Add(ConfirmationToken, StringComparer.Ordinal);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Render();
    }

    /// <summary>Renders the request as canonical invariant text for a diagnostic or a golden.</summary>
    /// <remarks>
    /// The confirmation token is rendered as present or absent rather than verbatim: doc 20 § Paused
    /// transactions attaches it to irreversible actions, and a token reproduced in a diagnostic artifact
    /// would be a replayable authorization sitting in a log.
    /// </remarks>
    public string Render()
    {
        if (!IsPresent)
        {
            return "transaction-request(none)";
        }

        return "transaction-request run="
            + _runSession.ToString("X16", CultureInfo.InvariantCulture)
            + " action="
            + ActionId
            + " selection="
            + SelectionId
            + "#"
            + _selectionOrdinal.ToString(CultureInfo.InvariantCulture)
            + " expectedVersion="
            + _expectedStateVersion.ToString(CultureInfo.InvariantCulture)
            + " clientSeq="
            + _clientCommandSequence.ToString(CultureInfo.InvariantCulture)
            + " token="
            + (HasConfirmationToken ? "present" : "absent");
    }
}
