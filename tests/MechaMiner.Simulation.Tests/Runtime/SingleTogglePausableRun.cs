using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// A deliberately broken subject that consults a single boolean "the player paused" toggle instead
/// of the reason set.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-002-010</c> (negative control for <c>VER-SIM-002-002</c> and
/// <c>VER-SIM-002-003</c>).
/// </para>
/// <para>
/// This is precisely the design doc 10 § Pause contract rejects in its first sentence: "Pause is
/// represented as a set of reasons rather than a single toggle." The stub is the plausible wrong
/// implementation, not a nonsense one - it tracks which reasons were raised, so it can answer
/// <see cref="Contains(PauseReason)"/> correctly, and it only gets blocking wrong:
/// </para>
/// <list type="bullet">
/// <item><description>
/// only <see cref="PauseReason.GeneralPause"/> sets the toggle, so the other six reasons do not
/// stop ticks - which is how it fails <c>VER-SIM-002-002</c>; and
/// </description></item>
/// <item><description>
/// clearing any reason clears the toggle, so an overlapping pause resumes early - which is how it
/// fails <c>VER-SIM-002-003</c>.
/// </description></item>
/// </list>
/// <para>
/// It is a wrong stub, not an invalid fixture: valid C# whose behaviour a test proves wrong.
/// </para>
/// </remarks>
internal sealed class SingleTogglePausableRun : IPausableRun
{
    private readonly HashSet<PauseReason> _raised = new();
    private readonly FixedStepAccumulator _accumulator = new(CatchUpPolicy.Default);
    private bool _paused;

    /// <inheritdoc />
    public bool IsBlocking => _paused;

    /// <inheritdoc />
    public long CommittedTickCount { get; private set; }

    /// <inheritdoc />
    public double UiClockSeconds { get; private set; }

    /// <inheritdoc />
    public void Raise(PauseReason reason)
    {
        _raised.Add(reason);
        if (reason == PauseReason.GeneralPause)
        {
            _paused = true;
        }
    }

    /// <inheritdoc />
    public void ClearReason(PauseReason reason)
    {
        _raised.Remove(reason);
        _paused = false;
    }

    /// <inheritdoc />
    public bool Contains(PauseReason reason)
    {
        return _raised.Contains(reason);
    }

    /// <inheritdoc />
    public void RecoverFocus()
    {
        ClearReason(PauseReason.FocusLoss);
    }

    /// <inheritdoc />
    public int Step(double elapsedSeconds)
    {
        UiClockSeconds += elapsedSeconds;
        if (_paused)
        {
            return 0;
        }

        TickBudget budget = _accumulator.Advance(Math.Min(elapsedSeconds, TickRate.SecondsForTicks(4)));
        CommittedTickCount += budget.TickCount;
        return budget.TickCount;
    }
}
