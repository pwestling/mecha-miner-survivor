namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The subject seam the accumulator assertions run against, so the same assertions can be
/// pointed at the real accumulator and at a deliberately broken stub.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-002</c>, <c>VER-SIM-001-003</c>, <c>VER-SIM-001-005</c>, and
/// the negative control <c>VER-SIM-001-008</c>.
/// </para>
/// <para>
/// <see cref="AdvanceAndCountTicks(double)"/> returns a <see cref="double"/> even though a
/// real tick count is always a whole number. That is deliberate: <c>VER-SIM-001-008</c>
/// requires a stub "that emits a fractional tick" to make the whole-tick assertion fail, and
/// a seam typed <c>int</c> would make that stub unrepresentable - the gate would then pass
/// because the type system prevented the counterexample, not because the implementation is
/// right. The real subject's own budget is an <c>int</c>; only this test seam widens it.
/// </para>
/// </remarks>
internal interface IStepwiseAccumulator
{
    /// <summary>The catch-up bound this subject claims to enforce, in whole ticks per step.</summary>
    int MaximumTicksPerStep { get; }

    /// <summary>Elapsed seconds the most recent step discarded rather than turned into ticks.</summary>
    double LastDiscardedSeconds { get; }

    /// <summary>Accepts one step's elapsed seconds and returns the ticks it yielded.</summary>
    /// <param name="elapsedSeconds">Seconds since the previous step.</param>
    /// <returns>The ticks yielded, which a correct accumulator always reports as a whole number.</returns>
    double AdvanceAndCountTicks(double elapsedSeconds);
}
