using System;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// A deliberately broken subject that yields every whole tick a stall accrued, ignoring the
/// catch-up bound.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-008</c> (negative control for <c>VER-SIM-001-005</c>).
/// </para>
/// <para>
/// This is the unresponsive spiral doc 10 § Clock domains describes the bound as preventing:
/// a long stall becomes a long recovery step, whose own cost accrues more debt. The subject
/// counts whole ticks correctly, so it passes the whole-tick assertion; it fails only the
/// bound assertion, which is what makes it a control for <c>VER-SIM-001-005</c> specifically
/// rather than a stub that fails everything.
/// </para>
/// </remarks>
internal sealed class UnboundedCatchUpAccumulatorSubject : IStepwiseAccumulator
{
    private double _retainedSeconds;

    /// <inheritdoc />
    public int MaximumTicksPerStep => CatchUpPolicy.Default.MaximumTicksPerStep;

    /// <inheritdoc />
    public double LastDiscardedSeconds => 0.0;

    /// <inheritdoc />
    public double AdvanceAndCountTicks(double elapsedSeconds)
    {
        _retainedSeconds += elapsedSeconds;
        double due = Math.Floor(
            (_retainedSeconds * TickRate.TicksPerSecondNumerator / TickRate.TicksPerSecondDenominator)
            + FixedStepAccumulator.TickBoundarySnapTicks);
        if (due < 0.0)
        {
            due = 0.0;
        }

        _retainedSeconds -= TickRate.SecondsForTicks((long)due);
        return due;
    }
}
