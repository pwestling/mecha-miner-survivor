using MechaMiner.Simulation.Runtime;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// A deliberately broken subject whose focus recovery clears every blocking reason instead of only
/// focus loss.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-002-010</c> (negative control for <c>VER-SIM-002-005</c>).
/// </para>
/// <para>
/// The bug this models is a real one and is why doc 10 § Pause contract states the rule
/// explicitly: "Focus recovery never dismisses a menu, tutorial, relic choice, or user-requested
/// pause." An implementation that treats regaining focus as "the interruption is over, resume the
/// run" throws the player straight back into combat out of a pause menu, a tutorial step, or a
/// relic choice.
/// </para>
/// <para>
/// Everything else about this subject is the real run session, so it fails only
/// <c>VER-SIM-002-005</c>'s assertion and passes the others. That is what makes it a control for
/// that gate rather than a stub that fails everything.
/// </para>
/// </remarks>
internal sealed class ClearEverythingOnFocusRecoveryRun : IPausableRun
{
    private readonly HostPausableRun _inner = new();

    /// <inheritdoc />
    public bool IsBlocking => _inner.IsBlocking;

    /// <inheritdoc />
    public long CommittedTickCount => _inner.CommittedTickCount;

    /// <inheritdoc />
    public double UiClockSeconds => _inner.UiClockSeconds;

    /// <inheritdoc />
    public void Raise(PauseReason reason)
    {
        _inner.Raise(reason);
    }

    /// <inheritdoc />
    public void ClearReason(PauseReason reason)
    {
        _inner.ClearReason(reason);
    }

    /// <inheritdoc />
    public bool Contains(PauseReason reason)
    {
        return _inner.Contains(reason);
    }

    /// <inheritdoc />
    public void RecoverFocus()
    {
        foreach (PauseReason reason in PauseReasonSet.AllReasons)
        {
            _inner.ClearReason(reason);
        }
    }

    /// <inheritdoc />
    public int Step(double elapsedSeconds)
    {
        return _inner.Step(elapsedSeconds);
    }
}
