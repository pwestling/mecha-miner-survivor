using System;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// The focus-loss, operating-system-suspension, and resume entry points: each raises or
/// clears exactly its own pause reason, and each resume discards the elapsed wall time it
/// spanned.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "Operating-system
/// suspension or focus-loss pause discards elapsed wall time rather than catching up
/// gameplay." § Pause contract: "Focus recovery never dismisses a menu, tutorial, relic
/// choice, or user-requested pause."
/// </para>
/// <para>
/// Separate from <see cref="SimulationHost"/> because a resume is not a frame:
/// <c>VER-SIM-001-009</c> invokes one directly with no step in between, and
/// <c>VER-SIM-002-005</c> and <c>VER-SIM-002-006</c> exercise the hooks against a set that
/// already holds unrelated reasons. It writes through <see cref="RunClock"/> rather than
/// keeping its own state, because doc 115 § Mutable-state ownership matrix gives run
/// pause state exactly one writer.
/// </para>
/// <para>
/// <b>These hooks read no clock.</b> They do not measure how long the interruption lasted;
/// they arm the accumulator so that whatever interval the caller reports next is discarded.
/// doc 20 § Scope and invariants forbids the simulation any dependency on wall time, and an
/// interruption's duration is exactly the kind of wall-time fact the simulation must not
/// learn.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-PRS-001</c> in <c>game/</c>
/// receives the engine's focus and suspension notifications and calls these. Hence
/// <c>public</c>.
/// </para>
/// </remarks>
public sealed class RunLifecycleHooks
{
    private readonly RunClock _clock;
    private readonly FixedStepAccumulator _accumulator;

    /// <summary>Creates the hooks over a run clock and its accumulator.</summary>
    /// <param name="clock">The run clock, the sole writer of pause state.</param>
    /// <param name="accumulator">The accumulator whose elapsed time a resume discards.</param>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    public RunLifecycleHooks(RunClock clock, FixedStepAccumulator accumulator)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accumulator);

        _clock = clock;
        _accumulator = accumulator;
    }

    /// <summary>Raises <see cref="PauseReason.FocusLoss"/> because the application lost focus.</summary>
    /// <returns>The typed outcome, including the resulting set.</returns>
    /// <remarks>Idempotent: losing focus while already unfocused changes nothing.</remarks>
    public PauseTransitionResult OnFocusLost()
    {
        return _clock.Raise(PauseReason.FocusLoss);
    }

    /// <summary>
    /// Clears <see cref="PauseReason.FocusLoss"/> and only that, and discards the elapsed wall
    /// time the focus loss spanned.
    /// </summary>
    /// <returns>
    /// The typed outcome. The resulting set is still blocking if any other reason - a pause
    /// menu, a tutorial, a fabrication session, a relic choice - is present.
    /// </returns>
    /// <remarks>
    /// doc 10 § Pause contract: "Focus recovery never dismisses a menu, tutorial, relic choice,
    /// or user-requested pause." <c>VER-SIM-002-005</c> asserts it against each of those four.
    /// </remarks>
    public PauseTransitionResult OnFocusRegained()
    {
        PauseTransitionResult result = _clock.Clear(PauseReason.FocusLoss);
        _accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.FocusLoss);
        return result;
    }

    /// <summary>
    /// Raises <see cref="PauseReason.OperatingSystemSuspension"/> because the operating system
    /// suspended the process.
    /// </summary>
    /// <returns>The typed outcome, including the resulting set.</returns>
    public PauseTransitionResult OnOperatingSystemSuspended()
    {
        return _clock.Raise(PauseReason.OperatingSystemSuspension);
    }

    /// <summary>
    /// Clears <see cref="PauseReason.OperatingSystemSuspension"/> and only that, and discards
    /// the elapsed wall time the suspension spanned.
    /// </summary>
    /// <returns>
    /// The typed outcome. The resulting set is still blocking if any other reason is present.
    /// </returns>
    /// <remarks>
    /// A suspension can span hours, so its elapsed wall time is discarded rather than caught up
    /// (doc 10 § Clock domains). The next step therefore runs zero ticks and reports the
    /// discarded seconds, leaving the tick index and run clock exactly where the suspension
    /// found them - what <c>VER-SIM-001-009</c> and <c>VER-SIM-002-006</c> assert.
    /// </remarks>
    public PauseTransitionResult OnOperatingSystemResumed()
    {
        PauseTransitionResult result = _clock.Clear(PauseReason.OperatingSystemSuspension);
        _accumulator.ArmLifecycleDiscard(AccumulatorDiscardReason.OperatingSystemSuspension);
        return result;
    }
}
