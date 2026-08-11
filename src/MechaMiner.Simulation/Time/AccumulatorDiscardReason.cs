namespace MechaMiner.Simulation.Time;

/// <summary>
/// Why the fixed-step accumulator discarded elapsed time instead of turning it into
/// ticks.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains describes two
/// discards with opposite meanings: "A bounded catch-up limit prevents an unresponsive
/// spiral after a stall; reaching that bound produces a performance diagnostic" - a
/// defect - and "Operating-system suspension or focus-loss pause discards elapsed wall
/// time rather than catching up gameplay" - correct, expected behaviour that must not be
/// diagnosed as a performance problem.
/// </para>
/// <para>
/// A single <c>bool discarded</c> would conflate the two, so <c>VER-SIM-001-005</c> (a
/// diagnosed defect) and <c>VER-SIM-001-009</c> (expected, not diagnosed) could not both
/// be asserted. Hence a reason rather than a flag.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-PRS-001</c> in
/// <c>game/</c> hands the host its frame delta and reads the step result to decide
/// whether to report a performance diagnostic, and <c>MechaMiner.Tools</c> renders the
/// reason into a benchmark report for <c>PERF-04</c>. Hence <c>public</c>.
/// </para>
/// </remarks>
public enum AccumulatorDiscardReason
{
    /// <summary>Nothing was discarded; all elapsed time became ticks or was retained.</summary>
    None = 0,

    /// <summary>
    /// Whole ticks of accumulated debt beyond the catch-up bound were dropped.
    /// </summary>
    /// <remarks>
    /// A performance defect. doc 10 § Clock domains: reaching the bound "produces a
    /// performance diagnostic". The bound is only reached after a stall that already
    /// exceeds the tolerance of
    /// <c>docs/technical/decisions/TDR-003-require-sixty-fps-on-steam-deck.md</c>
    /// § Performance contract, so this reason always means something is wrong.
    /// </remarks>
    CatchUpBoundReached = 1,

    /// <summary>
    /// Elapsed wall time spanning a focus-loss pause was discarded rather than caught up.
    /// </summary>
    /// <remarks>
    /// Expected and correct: doc 10 § Clock domains. Never a performance diagnostic.
    /// </remarks>
    FocusLoss = 2,

    /// <summary>
    /// Elapsed wall time spanning an operating-system suspension was discarded rather than
    /// caught up.
    /// </summary>
    /// <remarks>
    /// Expected and correct: doc 10 § Clock domains. A suspension can span hours, so
    /// catching it up would be both impossible within any bound and wrong.
    /// </remarks>
    OperatingSystemSuspension = 3,
}
