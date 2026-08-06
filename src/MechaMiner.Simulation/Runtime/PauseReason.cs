namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// The blocking reasons a run can be paused for. Exactly the seven of
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract, no more.
/// </summary>
/// <remarks>
/// <para>
/// doc 10 § Pause contract: "Pause is represented as a set of reasons rather than a single
/// toggle. Initial blocking reasons are general pause, fabrication, relic resolution,
/// blocking tutorial/modal, focus loss, operating-system suspension, and terminal
/// transition." The members below are those seven in that order.
/// </para>
/// <para>
/// Values are distinct single bits so that <see cref="PauseReasonSet"/> is a mask and the
/// exhaustive sweep over all 128 subsets that <c>VER-SIM-002-003</c> requires is a loop
/// rather than 128 hand-written cases. There is deliberately <b>no zero member</b>: an
/// eighth member would be an unregistered reason, and "no reason" is the empty set
/// (<see cref="PauseReasonSet.Empty"/>), not a reason.
/// </para>
/// <para>
/// The enum is not marked <c>[Flags]</c>. A combination is a
/// <see cref="PauseReasonSet"/> - a type that knows the difference between an empty set
/// and a set containing something - rather than a bare bitwise-or of enum values that
/// would lose that distinction.
/// </para>
/// <para>
/// Cross-boundary consumer (<c>docs/technical/115-component-contract-and-schema-registry.md</c>
/// § Component registry): <c>CMP-PRS-001</c> presentation and <c>CMP-UIX-001</c> in
/// <c>game/</c> raise and clear these reasons from the pause menu, the fabrication screen,
/// the relic screen, the tutorial gate, and the engine's focus and suspension
/// notifications, and read the set to decide what pause presentation to show. Hence
/// <c>public</c>.
/// </para>
/// </remarks>
public enum PauseReason
{
    /// <summary>The player opened the pause menu.</summary>
    /// <remarks>
    /// <c>docs/20-run-structure-and-timing.md</c> § General pause menu. Focus recovery
    /// never dismisses it (doc 10 § Pause contract).
    /// </remarks>
    GeneralPause = 1,

    /// <summary>An on-demand fabrication session is open.</summary>
    /// <remarks>
    /// <c>docs/20-run-structure-and-timing.md</c> § On-demand crafting pauses. doc 10:121:
    /// opening it "captures an immutable view of the relevant authoritative state".
    /// </remarks>
    Fabrication = 2,

    /// <summary>A relic choice is awaiting resolution.</summary>
    /// <remarks><c>docs/20-run-structure-and-timing.md</c> § Relic resolution pauses.</remarks>
    RelicResolution = 4,

    /// <summary>A blocking tutorial step or modal dialog is open.</summary>
    /// <remarks>
    /// doc 10 § Pause contract lists "blocking tutorial/modal" as one reason, not two: both
    /// are the same fact - a modal surface owns the input and the run must not advance
    /// behind it - so splitting them would create an eighth reason with no distinct
    /// semantics.
    /// </remarks>
    BlockingTutorialOrModal = 8,

    /// <summary>The application lost input focus.</summary>
    /// <remarks>
    /// Cleared by focus recovery, which "never dismisses a menu, tutorial, relic choice, or
    /// user-requested pause" (doc 10 § Pause contract), and whose resume discards the
    /// elapsed wall time (doc 10 § Clock domains).
    /// </remarks>
    FocusLoss = 16,

    /// <summary>The operating system suspended the process.</summary>
    /// <remarks>
    /// Its resume discards the elapsed wall time rather than catching gameplay up (doc 10
    /// § Clock domains). A suspension can span hours, so catching up is neither bounded nor
    /// correct.
    /// </remarks>
    OperatingSystemSuspension = 32,

    /// <summary>The run is resolving its terminal outcome.</summary>
    /// <remarks>
    /// One-way. doc 20 § Scope and invariants: "a run terminal result is assigned once and
    /// is immutable", so this reason is never cleared back into an active run. The refusal
    /// lives in the single writer, <see cref="RunClock"/>, and is reported through
    /// <see cref="PauseTransitionResult"/>.
    /// </remarks>
    TerminalTransition = 64,
}
