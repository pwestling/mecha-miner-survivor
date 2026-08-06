using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The real subject: the production <see cref="FixedStepAccumulator"/> behind the test seam.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-001-002</c>, <c>VER-SIM-001-003</c>, <c>VER-SIM-001-005</c>.
///
/// This adapter adds nothing and hides nothing: it forwards the step and widens the whole
/// tick count to the seam's <see cref="double"/>. Every assertion the negative control
/// <c>VER-SIM-001-008</c> proves can fail is therefore run against the production type here.
/// </remarks>
internal sealed class FixedStepAccumulatorSubject : IStepwiseAccumulator
{
    private readonly FixedStepAccumulator _accumulator;

    /// <summary>Wraps a fresh accumulator bound by the accepted provisional baseline.</summary>
    internal FixedStepAccumulatorSubject()
    {
        _accumulator = new FixedStepAccumulator(CatchUpPolicy.Default);
    }

    /// <inheritdoc />
    public int MaximumTicksPerStep => _accumulator.Policy.MaximumTicksPerStep;

    /// <inheritdoc />
    public double LastDiscardedSeconds { get; private set; }

    /// <inheritdoc />
    public double AdvanceAndCountTicks(double elapsedSeconds)
    {
        TickBudget budget = _accumulator.Advance(elapsedSeconds);
        LastDiscardedSeconds = budget.DiscardedSeconds;
        return budget.TickCount;
    }
}
