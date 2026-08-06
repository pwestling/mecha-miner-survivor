using System;

namespace MechaMiner.Simulation.Time;

/// <summary>
/// Turns measured elapsed seconds into a whole number of complete ticks, retaining the
/// fractional remainder and never exposing it.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "The host uses an
/// accumulator to execute zero or more complete ticks per rendered frame. It never passes
/// a variable delta to authoritative systems. A bounded catch-up limit prevents an
/// unresponsive spiral after a stall; reaching that bound produces a performance
/// diagnostic. Operating-system suspension or focus-loss pause discards elapsed wall time
/// rather than catching up gameplay."
/// </para>
/// <para>
/// <b>This type reads no clock.</b> <c>docs/technical/20-simulation-core.md</c> § Scope and
/// invariants: the simulation "has no dependency on Godot, files, Steam, rendering, audio,
/// wall time, or mutable global services." Elapsed seconds arrive as a parameter measured
/// by the caller; nothing here calls <c>DateTime</c>, <c>Environment.TickCount</c>, or a
/// <c>Stopwatch</c>.
/// </para>
/// <para>
/// <b>Why the remainder is not observable.</b> The retained fraction is a private field
/// and no member returns it. If it were readable, a tick target could interpolate against
/// it and gameplay would once again depend on a variable delta, which doc 10 forbids.
/// Presentation interpolation is a snapshot concern (doc 20 § Presentation snapshot), not
/// an accumulator concern.
/// </para>
/// </remarks>
public sealed class FixedStepAccumulator
{
    /// <summary>
    /// How close to a whole tick the retained time may fall short and still be treated as
    /// having reached it, in ticks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a numerical-robustness bound, not a gameplay tolerance. One tick interval
    /// is not exactly representable as a <see cref="double"/>, so a sequence of exactly
    /// representable frame deltas whose true sum is exactly <c>n</c> ticks can compute to
    /// a hair under <c>n</c>. Without a snap the accumulator would report <c>n - 1</c>
    /// ticks for time that genuinely elapsed, which is exactly the long-run drift
    /// <c>VER-SIM-001-003</c> forbids.
    /// </para>
    /// <para>
    /// Magnitude, bounded from both sides so it cannot be tuned:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>Above the accumulator's own error.</b> The retained fraction is always less than
    /// one tick, so each step's rounding error is on the order of <c>2^-58</c> seconds and
    /// the accumulated error across a full 35-minute run is under <c>1e-13</c> seconds,
    /// about <c>6e-12</c> ticks. <c>1e-9</c> is more than two orders of magnitude above
    /// that.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>Far below any measurable delta.</b> <c>1e-9</c> ticks is 16.7 picoseconds. A
    /// monotonic frame clock resolves about 100 nanoseconds, or <c>6e-6</c> ticks, so the
    /// snap is four orders of magnitude below the smallest difference a caller could
    /// actually measure. It can therefore never turn a genuinely incomplete interval into
    /// a tick.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <c>VER-SIM-001-003</c> asserts both bounds so the constant cannot silently drift into
    /// either failure mode.
    /// </para>
    /// </remarks>
    public const double TickBoundarySnapTicks = 1e-9;

    private readonly CatchUpPolicy _policy;

    /// <summary>
    /// Elapsed seconds that have not yet become a tick. Strictly less than one tick
    /// interval after every step, and never exposed.
    /// </summary>
    private double _retainedSeconds;

    /// <summary>
    /// The lifecycle discard armed for the next step, or
    /// <see cref="AccumulatorDiscardReason.None"/>.
    /// </summary>
    private AccumulatorDiscardReason _armedDiscardReason;

    /// <summary>Creates an accumulator bound by <paramref name="policy"/>.</summary>
    /// <param name="policy">
    /// The catch-up bound. Must admit at least one tick per step, or no run could advance.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="policy"/> admits fewer than one tick per step.
    /// </exception>
    public FixedStepAccumulator(CatchUpPolicy policy)
    {
        if (policy.MaximumTicksPerStep < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy.MaximumTicksPerStep,
                "the catch-up bound must admit at least one tick per host step; a default-constructed "
                + "CatchUpPolicy admits none, so use CatchUpPolicy.Default or "
                + "CatchUpPolicy.FromStallTolerance");
        }

        _policy = policy;
    }

    /// <summary>Creates an accumulator bound by the accepted provisional baseline.</summary>
    public FixedStepAccumulator()
        : this(CatchUpPolicy.Default)
    {
    }

    /// <summary>The catch-up bound and the derivation it came from.</summary>
    public CatchUpPolicy Policy => _policy;

    /// <summary>
    /// Whether a lifecycle discard is armed, so the next step will discard its elapsed
    /// time instead of accumulating it.
    /// </summary>
    /// <remarks>
    /// Reports whether a discard is pending, not how much time is pending: it exposes no
    /// duration and no fraction.
    /// </remarks>
    public bool IsLifecycleDiscardArmed => _armedDiscardReason != AccumulatorDiscardReason.None;

    /// <summary>
    /// Arms the next step to discard its elapsed wall time rather than catch it up.
    /// </summary>
    /// <param name="reason">
    /// <see cref="AccumulatorDiscardReason.FocusLoss"/> or
    /// <see cref="AccumulatorDiscardReason.OperatingSystemSuspension"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Called on resume, not on suspend: doc 10 § Clock domains discards the elapsed wall
    /// time of the interruption, and the caller only measures that interval when it hands
    /// over the first frame delta after the interruption ends. Arming also clears the
    /// retained fraction, so the run resumes from a clean timing baseline rather than from
    /// a fraction measured before an arbitrarily long blackout.
    /// </para>
    /// <para>
    /// Arming twice before a step is idempotent in effect but changes the reported reason
    /// to the most recent one, because that is the interruption the discarded interval
    /// actually spanned.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="reason"/> is not one of the two lifecycle discards.
    /// </exception>
    public void ArmLifecycleDiscard(AccumulatorDiscardReason reason)
    {
        if (reason != AccumulatorDiscardReason.FocusLoss
            && reason != AccumulatorDiscardReason.OperatingSystemSuspension)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason),
                reason,
                "doc 10 § Clock domains names exactly two discards that are correct rather than a "
                + "defect: focus loss and operating-system suspension");
        }

        _armedDiscardReason = reason;
    }

    /// <summary>
    /// Accepts one step's measured elapsed seconds and returns the whole ticks it produced.
    /// </summary>
    /// <param name="elapsedSeconds">
    /// Seconds measured by the caller since the previous step. Must be finite and not
    /// negative: a monotonic clock never runs backwards, so a negative delta is a defect
    /// in the caller rather than a value to absorb.
    /// </param>
    /// <returns>The whole ticks to run, and what was discarded to arrive at them.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="elapsedSeconds"/> is negative or not finite.
    /// </exception>
    public TickBudget Advance(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds),
                elapsedSeconds,
                "an elapsed frame delta is a finite, nonnegative duration; a monotonic clock never "
                + "runs backwards (doc 10 § Clock domains)");
        }

        if (_armedDiscardReason != AccumulatorDiscardReason.None)
        {
            AccumulatorDiscardReason reason = _armedDiscardReason;
            double discardedSeconds = elapsedSeconds + Math.Max(_retainedSeconds, 0.0);
            _armedDiscardReason = AccumulatorDiscardReason.None;
            _retainedSeconds = 0.0;
            return TickBudget.LifecycleDiscarded(reason, discardedSeconds);
        }

        _retainedSeconds += elapsedSeconds;

        // One multiplication against the exact rational rate, then one floor. The
        // retained fraction is always small, so this is a local computation whose error
        // cannot accumulate across a run - unlike either a running total of elapsed
        // seconds or a repeated subtraction of SecondsPerTick.
        double dueTicksExact = (_retainedSeconds * TickRate.TicksPerSecondNumerator)
            / TickRate.TicksPerSecondDenominator;
        long dueTicks = (long)Math.Floor(dueTicksExact + TickBoundarySnapTicks);
        if (dueTicks < 0)
        {
            dueTicks = 0;
        }

        // Every whole tick that came due is accounted for here, whether it is run or
        // discarded. That is what makes a discarded surplus impossible to queue into a
        // later step: doc 10 § Clock domains requires the bound to "prevent an
        // unresponsive spiral", which a queued surplus would reintroduce.
        _retainedSeconds -= TickRate.SecondsForTicks(dueTicks);

        int maximum = _policy.MaximumTicksPerStep;
        if (dueTicks > maximum)
        {
            long discardedTicks = dueTicks - maximum;
            return TickBudget.CatchUpBounded(
                maximum,
                discardedTicks > int.MaxValue ? int.MaxValue : (int)discardedTicks);
        }

        return TickBudget.OfTicks((int)dueTicks);
    }
}
