using System;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// The typed outcome of raising or clearing one blocking reason, so a refused clear is
/// observable rather than silent.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract: "Invalid or stale
/// transactions change nothing and return a typed rejection reason for UI presentation."
/// The same principle applies to the pause set itself, and <c>VER-SIM-002-008</c> makes it
/// explicit: an attempt to clear the terminal transition must be "rejected rather than
/// silently ignored".
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-UIX-001</c> in
/// <c>game/</c> reads <see cref="ResultingSet"/> to decide whether pause presentation stays
/// up, and <see cref="Outcome"/> to distinguish an idempotent no-op from a refusal.
/// </para>
/// </remarks>
public readonly struct PauseTransitionResult : IEquatable<PauseTransitionResult>
{
    /// <summary>Records one transition.</summary>
    /// <param name="reason">The reason the transition was requested for.</param>
    /// <param name="outcome">What happened.</param>
    /// <param name="resultingSet">The set after the transition, changed or not.</param>
    public PauseTransitionResult(
        PauseReason reason,
        PauseTransitionOutcome outcome,
        PauseReasonSet resultingSet)
    {
        Reason = reason;
        Outcome = outcome;
        ResultingSet = resultingSet;
    }

    /// <summary>The reason the transition was requested for.</summary>
    public PauseReason Reason { get; }

    /// <summary>What happened.</summary>
    public PauseTransitionOutcome Outcome { get; }

    /// <summary>The set after the transition. Equal to the set before it when nothing changed.</summary>
    public PauseReasonSet ResultingSet { get; }

    /// <summary>Whether the transition changed the set.</summary>
    public bool ChangedTheSet =>
        Outcome == PauseTransitionOutcome.Raised || Outcome == PauseTransitionOutcome.Cleared;

    /// <summary>
    /// Whether the transition was refused. A refusal is a defect in the caller, unlike an
    /// idempotent no-op.
    /// </summary>
    public bool WasRefused => Outcome == PauseTransitionOutcome.RefusedTerminalTransitionIsOneWay;

    /// <summary>Whether the run is still blocked after the transition.</summary>
    public bool IsBlocking => ResultingSet.IsBlocking;

    /// <inheritdoc />
    public bool Equals(PauseTransitionResult other)
    {
        return Reason == other.Reason
            && Outcome == other.Outcome
            && ResultingSet.Equals(other.ResultingSet);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is PauseTransitionResult other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Reason, Outcome, ResultingSet);
    }

    /// <summary>Compares two results for equality in every field.</summary>
    public static bool operator ==(PauseTransitionResult left, PauseTransitionResult right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two results for inequality in any field.</summary>
    public static bool operator !=(PauseTransitionResult left, PauseTransitionResult right)
    {
        return !left.Equals(right);
    }
}
