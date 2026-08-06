using System;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// What one host step actually did.
/// </summary>
/// <remarks>
/// <para>
/// The return value of <see cref="SimulationHost.Step(double)"/>. It reports whole ticks
/// only, plus the two facts the caller cannot derive: which clock domains advanced, and
/// whether anything was discarded.
/// </para>
/// <para>
/// <b>It carries elapsed UI seconds; <see cref="TickBudget"/> deliberately does not.</b>
/// doc 10 § Clock domains keeps the UI clock and the simulation tick in separate domains, and
/// § Pause contract requires that "Render and UI clocks continue so menus remain responsive
/// and pause presentation can animate." A zero-tick step must therefore still report UI time,
/// which is a host-layer fact - <c>MechaMiner.Simulation.Time</c> has no UI clock and must not
/// learn about one. <c>VER-SIM-002-007</c> asserts exactly this.
/// </para>
/// <para>
/// <b>It exposes no partial delta.</b> There is no fractional remainder, no interpolation
/// alpha, and no sub-tick position here: doc 10 § Clock domains says the host "never passes a
/// variable delta to authoritative systems", and <c>VER-SIM-001-002</c> asserts that neither
/// the accumulator nor the host exposes a member a tick target could read one from.
/// <see cref="DiscardedSeconds"/> is not such a member: it is time that has been thrown away
/// and can never become a tick.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-PRS-001</c> in <c>game/</c>
/// calls the step once per rendered frame and reads this to drive pause presentation and the
/// development overlay; <c>MechaMiner.Game.Tests</c> asserts on it. Hence <c>public</c>.
/// </para>
/// </remarks>
public readonly struct HostStepResult : IEquatable<HostStepResult>
{
    private HostStepResult(
        int tickCount,
        SimulationTick firstTick,
        SimulationTick lastTick,
        PauseReasonSet blockingReasons,
        double elapsedUiSeconds,
        double discardedSeconds,
        AccumulatorDiscardReason discardReason,
        bool terminalBoundaryEvaluated)
    {
        TickCount = tickCount;
        FirstTick = firstTick;
        LastTick = lastTick;
        BlockingReasons = blockingReasons;
        ElapsedUiSeconds = elapsedUiSeconds;
        DiscardedSeconds = discardedSeconds;
        DiscardReason = discardReason;
        TerminalBoundaryEvaluated = terminalBoundaryEvaluated;
    }

    /// <summary>The whole ticks executed by this step. Zero or more, never fractional.</summary>
    public int TickCount { get; }

    /// <summary>
    /// The first tick this step executed. Meaningful only when <see cref="TickCount"/> is
    /// positive; otherwise <see cref="SimulationTick.Zero"/>.
    /// </summary>
    public SimulationTick FirstTick { get; }

    /// <summary>
    /// The last tick this step executed. Meaningful only when <see cref="TickCount"/> is
    /// positive; otherwise <see cref="SimulationTick.Zero"/>.
    /// </summary>
    public SimulationTick LastTick { get; }

    /// <summary>The blocking reasons present when the step ran.</summary>
    public PauseReasonSet BlockingReasons { get; }

    /// <summary>
    /// The seconds of UI clock this step covered, which is always the elapsed seconds the
    /// caller supplied - including while the run is blocked.
    /// </summary>
    public double ElapsedUiSeconds { get; }

    /// <summary>Elapsed seconds discarded rather than turned into ticks, or zero.</summary>
    public double DiscardedSeconds { get; }

    /// <summary>Why time was discarded, distinguishing a defect from expected behaviour.</summary>
    public AccumulatorDiscardReason DiscardReason { get; }

    /// <summary>
    /// Whether this step evaluated the 35:00 terminal boundary, which happens once per run.
    /// </summary>
    public bool TerminalBoundaryEvaluated { get; }

    /// <summary>Whether the run was blocked and therefore executed no tick.</summary>
    public bool WasBlocked => BlockingReasons.IsBlocking;

    /// <summary>
    /// Whether the catch-up bound was reached, which doc 10 § Clock domains requires to produce
    /// a performance diagnostic.
    /// </summary>
    public bool CatchUpBoundReached => DiscardReason == AccumulatorDiscardReason.CatchUpBoundReached;

    /// <summary>
    /// A step that executed no tick because the run is blocked, but whose UI clock advanced.
    /// </summary>
    /// <param name="blockingReasons">The reasons present; must be non-empty.</param>
    /// <param name="elapsedUiSeconds">The UI seconds this step covered.</param>
    /// <exception cref="ArgumentException"><paramref name="blockingReasons"/> is empty.</exception>
    public static HostStepResult Blocked(PauseReasonSet blockingReasons, double elapsedUiSeconds)
    {
        if (blockingReasons.IsEmpty)
        {
            throw new ArgumentException(
                "a blocked step has at least one blocking reason (doc 10 § Pause contract)",
                nameof(blockingReasons));
        }

        return new HostStepResult(
            0,
            SimulationTick.Zero,
            SimulationTick.Zero,
            blockingReasons,
            elapsedUiSeconds,
            0.0,
            AccumulatorDiscardReason.None,
            false);
    }

    /// <summary>A step that ran the ticks its budget allowed.</summary>
    /// <param name="tickCount">The whole ticks executed.</param>
    /// <param name="firstTick">The first tick executed, or <see cref="SimulationTick.Zero"/>.</param>
    /// <param name="lastTick">The last tick executed, or <see cref="SimulationTick.Zero"/>.</param>
    /// <param name="blockingReasons">The reasons present after the step.</param>
    /// <param name="elapsedUiSeconds">The UI seconds this step covered.</param>
    /// <param name="budget">The budget the accumulator returned for this step.</param>
    /// <param name="terminalBoundaryEvaluated">Whether this step evaluated the 35:00 boundary.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tickCount"/> is negative.</exception>
    public static HostStepResult Ran(
        int tickCount,
        SimulationTick firstTick,
        SimulationTick lastTick,
        PauseReasonSet blockingReasons,
        double elapsedUiSeconds,
        TickBudget budget,
        bool terminalBoundaryEvaluated)
    {
        if (tickCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                tickCount,
                "a host step executes a whole, nonnegative number of ticks");
        }

        return new HostStepResult(
            tickCount,
            firstTick,
            lastTick,
            blockingReasons,
            elapsedUiSeconds,
            budget.DiscardedSeconds,
            budget.DiscardReason,
            terminalBoundaryEvaluated);
    }

    /// <inheritdoc />
    public bool Equals(HostStepResult other)
    {
        return TickCount == other.TickCount
            && FirstTick.Equals(other.FirstTick)
            && LastTick.Equals(other.LastTick)
            && BlockingReasons.Equals(other.BlockingReasons)
            && ElapsedUiSeconds.Equals(other.ElapsedUiSeconds)
            && DiscardedSeconds.Equals(other.DiscardedSeconds)
            && DiscardReason == other.DiscardReason
            && TerminalBoundaryEvaluated == other.TerminalBoundaryEvaluated;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is HostStepResult other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            TickCount,
            FirstTick,
            LastTick,
            BlockingReasons,
            ElapsedUiSeconds,
            DiscardedSeconds,
            DiscardReason,
            TerminalBoundaryEvaluated);
    }

    /// <summary>Compares two step results for equality in every field.</summary>
    public static bool operator ==(HostStepResult left, HostStepResult right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two step results for inequality in any field.</summary>
    public static bool operator !=(HostStepResult left, HostStepResult right)
    {
        return !left.Equals(right);
    }
}
