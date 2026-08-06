namespace MechaMiner.Simulation.Commands;

/// <summary>
/// Why a paused transaction was refused, or - for <see cref="AlreadyApplied"/> - why it was answered
/// with a result it had already produced.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Paused transactions: "Validation returns either a new
/// complete state/version plus domain events or a typed rejection with no mutation."
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract: "Invalid or stale transactions
/// change nothing and return a typed rejection reason for UI presentation."
/// <c>CTR-RUN-003</c> in doc 115 § Cross-boundary contract registry: "all-or-nothing typed result; stale
/// preview changes nothing".
/// </para>
/// <para>
/// The domain refusals doc 20 § Paused transactions enumerates - ownership, availability, slot capacity,
/// duplication, cost, prerequisites, branch exclusivity, integer overflow - are not members here. They
/// belong to the packages that own fabrication, relics, and PowerUps, and they arrive through
/// <see cref="DomainRefused"/> until those packages register their own reasons.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-UI-001</c> in <c>game/</c> presents
/// the refusal - doc 10 § Pause contract says the reason exists "for UI presentation" - and
/// <c>MechaMiner.Game.Tests</c> asserts on it. Hence <c>public</c>.
/// </para>
/// </remarks>
public enum TransactionRejectionReason
{
    /// <summary>
    /// The expected state version no longer matches the authoritative one: the immutable view captured
    /// when the pause opened has been superseded.
    /// </summary>
    StaleExpectedStateVersion = 0,

    /// <summary>
    /// This client command sequence was already applied. The result carries the original application's
    /// state version, domain event, and snapshot version rather than applying anything again.
    /// </summary>
    /// <remarks>
    /// A success-shaped rejection: <c>VER-SIM-004-009</c> requires a replay to observe the applied
    /// result, not merely to be refused, so <see cref="PausedTransactionResult.WasApplied"/> is true for
    /// this reason and for an acceptance alike.
    /// </remarks>
    AlreadyApplied = 1,

    /// <summary>The request carries another run session's identity.</summary>
    ForeignRunSession = 2,

    /// <summary>No action with that identity is registered, so there is nothing to validate against.</summary>
    UnknownAction = 3,

    /// <summary>
    /// The action is irreversible and the request carried no confirmation token.
    /// </summary>
    /// <remarks>
    /// doc 20 § Paused transactions lists an "optional confirmation token for irreversible actions";
    /// optional in the payload, required for the actions that declare it.
    /// </remarks>
    ConfirmationRequired = 4,

    /// <summary>
    /// The owning domain component refused the request on its own rules.
    /// </summary>
    /// <remarks>
    /// The last check before any mutation, so a domain refusal is still a rejection with no mutation.
    /// </remarks>
    DomainRefused = 5,
}
