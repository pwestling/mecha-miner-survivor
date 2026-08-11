using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// A deliberately broken subject that yields the fractional tick amount an interval covers
/// instead of only complete ticks.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-008</c> (negative control for <c>VER-SIM-001-002</c>).
/// </para>
/// <para>
/// This is the mistake the whole-tick rule exists to prevent: handing authoritative systems
/// "0.6 of a tick" of movement, which doc 10 § Clock domains forbids when it says the host
/// "never passes a variable delta to authoritative systems". If
/// <c>AccumulatorContract.AssertOnlyWholeTicksAreYielded</c> did not actually check
/// wholeness, this subject would pass it, and <c>VER-SIM-001-002</c> would be asserting
/// nothing.
/// </para>
/// <para>
/// It is a stub inside a compiled test project, not an invalid fixture: it is valid C# whose
/// <i>behaviour</i> is wrong, so it compiles and the gate observes it failing.
/// <c>docs/technical/delivery-waves.md</c> forbids committing a deliberately invalid fixture
/// inside a compiled project; a deliberately wrong stub that a test proves wrong is the
/// opposite of that.
/// </para>
/// </remarks>
internal sealed class FractionalTickAccumulatorSubject : IStepwiseAccumulator
{
    /// <inheritdoc />
    public int MaximumTicksPerStep => CatchUpPolicy.Default.MaximumTicksPerStep;

    /// <inheritdoc />
    public double LastDiscardedSeconds => 0.0;

    /// <inheritdoc />
    public double AdvanceAndCountTicks(double elapsedSeconds)
    {
        return elapsedSeconds * TickRate.TicksPerSecondNumerator / TickRate.TicksPerSecondDenominator;
    }
}
