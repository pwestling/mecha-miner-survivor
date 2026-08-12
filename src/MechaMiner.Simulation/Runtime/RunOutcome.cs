namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// How a run ended, or that it has not.
/// </summary>
/// <remarks>
/// <para>
/// Two ways to end, from <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering,
/// phase 13: "Evaluate death or extraction terminal conditions." Death is
/// <see cref="Destroyed"/> and reaching the 35:00 boundary alive is <see cref="Survived"/>.
/// </para>
/// <para>
/// <b><see cref="Undecided"/> is the default value on purpose.</b> doc 20 § Scope and invariants makes
/// a terminal result something "assigned once and is immutable", so the state before assignment has to
/// be representable, and a defaulted <c>RunOutcome</c> field must not read as an ended run. Zero is
/// therefore the not-ended value rather than one of the two endings.
/// </para>
/// <para>
/// <b>Abandonment is deliberately absent.</b> <c>docs/technical/110-implementation-plan-for-ai-agents.md</c>:289
/// gives <c>PRG-006</c> the "success/failure/abandon settlement model", so there is a third outcome,
/// and it needs a voluntary-abandonment command that no package has built. Adding an
/// <c>Abandoned</c> member that nothing can produce would be a claim that something can.
/// </para>
/// </remarks>
public enum RunOutcome
{
    /// <summary>The run is still going. No terminal result has been assigned.</summary>
    Undecided = 0,

    /// <summary>The run reached the 35:00 final boundary with the mech alive.</summary>
    Survived = 1,

    /// <summary>The mech's Hull reached zero before the final boundary.</summary>
    Destroyed = 2,
}
