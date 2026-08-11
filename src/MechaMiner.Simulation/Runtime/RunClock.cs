using System;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// The run's authoritative tick index, derived run time, pause-reason set, and terminal
/// state. The single writer of all four.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Mutable-state
/// ownership matrix names the run session as the sole writer of "Run clock/pause/terminal
/// state", so those four facts live behind one object with one set of mutating methods.
/// <c>TR-CTR-002</c> requires exactly one registered writer, which is why
/// <see cref="SimulationHost"/> and <see cref="RunLifecycleHooks"/> both go through this
/// type rather than keeping their own copies.
/// </para>
/// <para>
/// <b>This type reads no clock.</b> doc 20 § Scope and invariants forbids the simulation any
/// dependency on wall time. <see cref="RunSeconds"/> is derived from
/// <see cref="CurrentTick"/> by one division and nothing else.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-PRS-001</c> and
/// <c>CMP-UIX-001</c> in <c>game/</c> read the run timer and the pause set for the HUD and
/// pause presentation and raise or clear reasons from menus and engine notifications;
/// <c>MechaMiner.Game.Tests</c> asserts on the run clock across the engine boundary. Hence
/// <c>public</c>.
/// </para>
/// </remarks>
public sealed class RunClock
{
    /// <summary>The run's authored length in whole minutes.</summary>
    /// <remarks>
    /// <c>docs/technical/20-simulation-core.md</c> § Boundary and tie ordering: "Active ticks
    /// cover times strictly before 35:00." Held as whole minutes so the boundary tick is an
    /// exact integer rather than a rounded product of seconds.
    /// </remarks>
    public const int FinalBoundaryMinutes = 35;

    /// <summary>
    /// The first tick index that lies at or after the 35:00 terminal boundary, and therefore
    /// the first tick that never runs.
    /// </summary>
    /// <remarks>
    /// <c>35 * 60 * 60 = 126,000</c>, by exact integer arithmetic against the rational tick
    /// rate. A tick with index <c>i</c> covers <c>[i / 60, (i + 1) / 60)</c>, so index
    /// 125,999 is the final pre-boundary tick and index 126,000 is the boundary itself.
    /// </remarks>
    public static SimulationTick FinalBoundaryTick =>
        new SimulationTick((long)FinalBoundaryMinutes * TickRate.TicksPerMinute);

    /// <summary>The terminal boundary in seconds of run time.</summary>
    public static double FinalBoundarySeconds => TickRate.SecondsForTicks(
        (long)FinalBoundaryMinutes * TickRate.TicksPerMinute);

    /// <summary>
    /// The tick the run is about to execute, which is also the count of ticks already
    /// committed.
    /// </summary>
    /// <remarks>
    /// A run starts at <see cref="SimulationTick.Zero"/>: no tick has committed and tick 0 is
    /// next.
    /// </remarks>
    public SimulationTick CurrentTick { get; private set; }

    /// <summary>The number of authoritative ticks committed so far.</summary>
    public long CommittedTickCount => CurrentTick.Index;

    /// <summary>
    /// Run time in seconds, derived from the committed tick count by a single division.
    /// </summary>
    /// <remarks>
    /// doc 10 § Clock domains: game time is "derived from integer tick count, not accumulated
    /// floating-point frame deltas". <c>VER-SIM-001-011</c> asserts that this advances only on
    /// committed ticks and equals the tick index over 60 exactly.
    /// </remarks>
    public double RunSeconds => CurrentTick.Seconds;

    /// <summary>The blocking reasons currently present.</summary>
    public PauseReasonSet BlockingReasons { get; private set; }

    /// <summary>Whether any blocking reason is present, in which case no tick executes.</summary>
    public bool IsBlocking => BlockingReasons.IsBlocking;

    /// <summary>Whether the run has reached the 35:00 terminal boundary.</summary>
    public bool HasReachedFinalBoundary => CurrentTick >= FinalBoundaryTick;

    /// <summary>
    /// Whether the terminal boundary has been evaluated, which happens exactly once per run.
    /// </summary>
    /// <remarks>
    /// doc 20 § Scope and invariants: "a run terminal result is assigned once and is
    /// immutable." Recorded so that
    /// <see cref="SimulationHost.Step(double)"/> cannot evaluate it twice.
    /// </remarks>
    public bool TerminalBoundaryEvaluated { get; private set; }

    /// <summary>
    /// Commits one authoritative tick and advances run time by exactly one tick interval.
    /// </summary>
    /// <returns>The new <see cref="CurrentTick"/>, that is, the tick that runs next.</returns>
    /// <remarks>
    /// doc 20 § Scope and invariants: "simulation time advances only by complete fixed ticks."
    /// There is deliberately no method that advances the clock by a duration.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// A blocking reason is present, or the run has already reached the terminal boundary.
    /// Both are defects in the caller rather than states to absorb: doc 10 § Pause contract
    /// says the simulation "executes no ticks while any blocking reason is present", and doc
    /// 20 § Boundary and tie ordering says active ticks cover only times strictly before
    /// 35:00.
    /// </exception>
    public SimulationTick CommitTick()
    {
        if (IsBlocking)
        {
            throw new InvalidOperationException(
                "no tick commits while a blocking reason is present (doc 10 § Pause contract); present: "
                + BlockingReasons.ToString());
        }

        if (HasReachedFinalBoundary)
        {
            throw new InvalidOperationException(
                "active ticks cover only times strictly before 35:00 (doc 20 § Boundary and tie "
                + "ordering); the run has already reached the terminal boundary at tick "
                + FinalBoundaryTick.ToString());
        }

        CurrentTick = CurrentTick.Next();
        return CurrentTick;
    }

    /// <summary>Raises one blocking reason.</summary>
    /// <param name="reason">The reason to raise.</param>
    /// <returns>The typed outcome, including whether the set actually changed.</returns>
    /// <remarks>Idempotent: raising a reason already present is not an error.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is unregistered.</exception>
    public PauseTransitionResult Raise(PauseReason reason)
    {
        if (BlockingReasons.Contains(reason))
        {
            return new PauseTransitionResult(
                reason,
                PauseTransitionOutcome.AlreadyPresent,
                BlockingReasons);
        }

        BlockingReasons = BlockingReasons.With(reason);
        return new PauseTransitionResult(reason, PauseTransitionOutcome.Raised, BlockingReasons);
    }

    /// <summary>Clears one blocking reason, unless it is one-way.</summary>
    /// <param name="reason">The reason to clear.</param>
    /// <returns>
    /// The typed outcome. A clear of <see cref="PauseReason.TerminalTransition"/> is refused
    /// with <see cref="PauseTransitionOutcome.RefusedTerminalTransitionIsOneWay"/> and changes
    /// nothing.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Clears exactly one reason. doc 10 § Pause contract: "Focus recovery never dismisses a
    /// menu, tutorial, relic choice, or user-requested pause", so there is deliberately no
    /// method that clears the whole set.
    /// </para>
    /// <para>
    /// Idempotent: clearing a reason that is absent is not an error. Refusing the terminal
    /// transition is not idempotence but a refusal, and is reported as one.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is unregistered.</exception>
    public PauseTransitionResult Clear(PauseReason reason)
    {
        if (reason == PauseReason.TerminalTransition && BlockingReasons.Contains(reason))
        {
            return new PauseTransitionResult(
                reason,
                PauseTransitionOutcome.RefusedTerminalTransitionIsOneWay,
                BlockingReasons);
        }

        if (!BlockingReasons.Contains(reason))
        {
            return new PauseTransitionResult(
                reason,
                PauseTransitionOutcome.AlreadyAbsent,
                BlockingReasons);
        }

        BlockingReasons = BlockingReasons.Without(reason);
        return new PauseTransitionResult(reason, PauseTransitionOutcome.Cleared, BlockingReasons);
    }

    /// <summary>
    /// Records that the terminal boundary has been evaluated, which may happen only once.
    /// </summary>
    /// <remarks>
    /// Internal because <see cref="SimulationHost"/> owns the ordering rule of doc 20
    /// § Boundary and tie ordering and is the only caller. Consumers observe the fact through
    /// <see cref="TerminalBoundaryEvaluated"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The boundary was already evaluated.</exception>
    internal void MarkTerminalBoundaryEvaluated()
    {
        if (TerminalBoundaryEvaluated)
        {
            throw new InvalidOperationException(
                "the terminal boundary is evaluated once per run; doc 20 § Scope and invariants: \"a run "
                + "terminal result is assigned once and is immutable\"");
        }

        TerminalBoundaryEvaluated = true;
    }
}
