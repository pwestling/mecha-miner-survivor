namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// What happened when a blocking reason was raised or cleared.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract requires an invalid
/// transaction to "return a typed rejection reason for UI presentation" rather than change
/// nothing silently, and <c>VER-SIM-002-008</c> requires an attempt to clear the terminal
/// transition to be "rejected rather than silently ignored". A <c>void</c> raise or clear
/// cannot report either, so every transition returns a
/// <see cref="PauseTransitionResult"/> carrying one of these.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-UIX-001</c> in
/// <c>game/</c> raises and clears reasons and must be able to tell "already in that state"
/// - which is fine and idempotent - from "refused", which is a defect in the caller. Hence
/// <c>public</c>.
/// </para>
/// </remarks>
public enum PauseTransitionOutcome
{
    /// <summary>The reason was absent and is now present.</summary>
    Raised = 0,

    /// <summary>
    /// The reason was already present, so the set is unchanged. Not an error: doc 10 § Pause
    /// contract makes the set a set, and <c>VER-SIM-002-004</c> requires adding a reason
    /// already present to yield an equal set and not to fail.
    /// </summary>
    AlreadyPresent = 1,

    /// <summary>The reason was present and is now absent.</summary>
    Cleared = 2,

    /// <summary>
    /// The reason was already absent, so the set is unchanged. Not an error, for the same
    /// reason as <see cref="AlreadyPresent"/>.
    /// </summary>
    AlreadyAbsent = 3,

    /// <summary>
    /// A clear of <see cref="PauseReason.TerminalTransition"/> was refused because the
    /// reason is one-way.
    /// </summary>
    /// <remarks>
    /// <c>docs/technical/20-simulation-core.md</c> § Scope and invariants: "a run terminal
    /// result is assigned once and is immutable." Resuming an active run after the terminal
    /// transition would contradict that invariant, so the writer refuses and says so.
    /// </remarks>
    RefusedTerminalTransitionIsOneWay = 4,
}
