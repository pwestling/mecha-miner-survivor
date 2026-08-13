using System;

namespace MechaMiner.Simulation.Time;

/// <summary>
/// What one host step is allowed to run: a whole number of ticks, and what - if
/// anything - was discarded to arrive at it.
/// </summary>
/// <remarks>
/// <para>
/// The output of <see cref="FixedStepAccumulator.Advance(double)"/>. It carries
/// <b>no partial delta</b>: doc 10 § Clock domains requires the host to "execute zero or
/// more complete ticks per rendered frame" and states that it "never passes a variable
/// delta to authoritative systems", so there is deliberately no member here from which a
/// tick target could read a fractional remainder. The remainder exists, is retained
/// inside the accumulator, and is not observable.
/// </para>
/// <para>
/// <see cref="DiscardedSeconds"/> is not a partial delta: it is time that has been thrown
/// away and can never become a tick, reported so the step above can diagnose it.
/// </para>
/// <para>
/// It also carries no UI clock. <c>Time/</c> has no UI clock and must not learn about one;
/// elapsed UI seconds are a host-layer fact and live on
/// <c>MechaMiner.Simulation.Runtime.HostStepResult</c>.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>MechaMiner.Tools</c> renders
/// the budget into the <c>PERF-04</c> benchmark report ("frame/tick catch-up count and
/// accumulator debt", doc 90 § Frame metrics). Hence <c>public</c>.
/// </para>
/// </remarks>
public readonly struct TickBudget : IEquatable<TickBudget>
{
    private TickBudget(
        int tickCount,
        int discardedTickCount,
        double discardedSeconds,
        AccumulatorDiscardReason discardReason)
    {
        TickCount = tickCount;
        DiscardedTickCount = discardedTickCount;
        DiscardedSeconds = discardedSeconds;
        DiscardReason = discardReason;
    }

    /// <summary>A step that runs no ticks and discards nothing.</summary>
    public static TickBudget Empty => default;

    /// <summary>The whole number of complete ticks this step runs. Never fractional, never negative.</summary>
    public int TickCount { get; }

    /// <summary>
    /// Whole ticks of debt that were discarded rather than queued. Zero unless the
    /// catch-up bound was reached.
    /// </summary>
    public int DiscardedTickCount { get; }

    /// <summary>
    /// The elapsed seconds that were discarded: the accumulator debt doc 90 § Frame
    /// metrics requires a catch-up diagnostic to carry.
    /// </summary>
    public double DiscardedSeconds { get; }

    /// <summary>Why time was discarded, distinguishing a defect from expected behaviour.</summary>
    public AccumulatorDiscardReason DiscardReason { get; }

    /// <summary>
    /// Whether the catch-up bound of <see cref="CatchUpPolicy.MaximumTicksPerStep"/> was
    /// reached, which doc 10 § Clock domains requires to produce a performance diagnostic.
    /// </summary>
    public bool CatchUpBoundReached => DiscardReason == AccumulatorDiscardReason.CatchUpBoundReached;

    /// <summary>A step that runs <paramref name="tickCount"/> ticks and discards nothing.</summary>
    /// <param name="tickCount">The whole ticks to run; must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tickCount"/> is negative.</exception>
    public static TickBudget OfTicks(int tickCount)
    {
        if (tickCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                tickCount,
                "a tick budget is a count of complete ticks and is never negative");
        }

        return new TickBudget(tickCount, 0, 0.0, AccumulatorDiscardReason.None);
    }

    /// <summary>
    /// A step that runs <paramref name="tickCount"/> ticks and discarded the surplus
    /// because the catch-up bound was reached.
    /// </summary>
    /// <param name="tickCount">The ticks the bound permits; must not be negative.</param>
    /// <param name="discardedTickCount">The whole ticks of debt dropped; must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either count is out of range.</exception>
    public static TickBudget CatchUpBounded(int tickCount, int discardedTickCount)
    {
        if (tickCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                tickCount,
                "a tick budget is a count of complete ticks and is never negative");
        }

        if (discardedTickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discardedTickCount),
                discardedTickCount,
                "the catch-up bound is only reported as reached when whole ticks of debt were actually "
                + "discarded");
        }

        return new TickBudget(
            tickCount,
            discardedTickCount,
            TickRate.SecondsForTicks(discardedTickCount),
            AccumulatorDiscardReason.CatchUpBoundReached);
    }

    /// <summary>
    /// A step that runs no ticks because elapsed wall time was discarded by design, on
    /// focus loss or operating-system suspension.
    /// </summary>
    /// <param name="reason">
    /// Must be <see cref="AccumulatorDiscardReason.FocusLoss"/> or
    /// <see cref="AccumulatorDiscardReason.OperatingSystemSuspension"/>: doc 10 § Clock
    /// domains gives those two, and only those two, as discards that are correct rather
    /// than a defect.
    /// </param>
    /// <param name="discardedSeconds">The wall seconds thrown away; must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">The reason or the duration is out of range.</exception>
    public static TickBudget LifecycleDiscarded(AccumulatorDiscardReason reason, double discardedSeconds)
    {
        if (reason != AccumulatorDiscardReason.FocusLoss
            && reason != AccumulatorDiscardReason.OperatingSystemSuspension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "only focus loss and operating-system suspension discard elapsed wall time by design "
                + "(doc 10 § Clock domains); a catch-up discard is reported as such");
        }

        if (!double.IsFinite(discardedSeconds) || discardedSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discardedSeconds),
                discardedSeconds,
                "discarded elapsed time is a finite, nonnegative duration");
        }

        return new TickBudget(0, 0, discardedSeconds, reason);
    }

    /// <inheritdoc />
    public bool Equals(TickBudget other)
    {
        return TickCount == other.TickCount
            && DiscardedTickCount == other.DiscardedTickCount
            && DiscardedSeconds.Equals(other.DiscardedSeconds)
            && DiscardReason == other.DiscardReason;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is TickBudget other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(TickCount, DiscardedTickCount, DiscardedSeconds, DiscardReason);
    }

    /// <summary>Compares two budgets for equality in every field.</summary>
    public static bool operator ==(TickBudget left, TickBudget right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two budgets for inequality in any field.</summary>
    public static bool operator !=(TickBudget left, TickBudget right)
    {
        return !left.Equals(right);
    }
}
