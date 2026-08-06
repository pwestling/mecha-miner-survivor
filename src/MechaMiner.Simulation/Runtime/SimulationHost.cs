using System;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// The fixed-step host: it turns measured elapsed seconds into zero or more complete
/// authoritative ticks, consults the pause-reason set first, bounds catch-up, and handles
/// the 35:00 terminal boundary before another tick can begin.
/// </summary>
/// <remarks>
/// <para>
/// <c>CMP-RUN-001</c> run session in
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry.
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "The host uses an
/// accumulator to execute zero or more complete ticks per rendered frame. It never passes a
/// variable delta to authoritative systems."
/// </para>
/// <para>
/// <b>This type reads no clock.</b> doc 20 § Scope and invariants: the simulation has no
/// dependency on "wall time". Elapsed seconds arrive as a parameter to
/// <see cref="Step(double)"/>; nothing here calls <c>DateTime.Now</c>,
/// <c>Environment.TickCount</c>, or <c>Stopwatch</c>. Making the caller measure the interval
/// is also what makes the host deterministic and testable without a clock at all.
/// </para>
/// <para>
/// <b>Ordering within one step.</b> A step does, in order: consult the pause set; if
/// unblocked, ask the accumulator for whole ticks; record a performance diagnostic if the
/// catch-up bound was reached; run each tick once in ascending order, committing the run clock
/// after each; and when the clock reaches 35:00, evaluate the terminal boundary and raise
/// <see cref="PauseReason.TerminalTransition"/> before the step returns. doc 10 § System phase
/// ordering, phase 2: "the 35:00 terminal boundary is handled before another tick can begin."
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-PRS-001</c> in <c>game/</c>
/// owns the frame loop and drives this host once per rendered frame;
/// <c>MechaMiner.Game.Tests</c> drives it in the headless integration runner;
/// <c>MechaMiner.Tools</c> drives it for scenarios and benchmarks. An all-internal host could
/// not be consumed by the game at all, which is why this type is <c>public</c>.
/// </para>
/// </remarks>
public sealed class SimulationHost
{
    private readonly ISimulationWorld _world;
    private readonly RunClock _clock;
    private readonly FixedStepAccumulator _accumulator;
    private readonly PerformanceDiagnostics _diagnostics;
    private readonly RunLifecycleHooks _lifecycle;
    private bool _inTick;

    /// <summary>Creates a host over an explicit clock, accumulator, and diagnostics sink.</summary>
    /// <param name="world">The tick target.</param>
    /// <param name="clock">The run clock, which is also the sole writer of pause and terminal state.</param>
    /// <param name="accumulator">The fixed-step accumulator.</param>
    /// <param name="diagnostics">Where catch-up diagnostics are recorded.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public SimulationHost(
        ISimulationWorld world,
        RunClock clock,
        FixedStepAccumulator accumulator,
        PerformanceDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(accumulator);
        ArgumentNullException.ThrowIfNull(diagnostics);

        _world = world;
        _clock = clock;
        _accumulator = accumulator;
        _diagnostics = diagnostics;
        _lifecycle = new RunLifecycleHooks(clock, accumulator);
    }

    /// <summary>
    /// Creates a host over a fresh run clock, an accumulator bound by the accepted provisional
    /// catch-up baseline, and a fresh diagnostics sink.
    /// </summary>
    /// <param name="world">The tick target.</param>
    /// <exception cref="ArgumentNullException"><paramref name="world"/> is null.</exception>
    public SimulationHost(ISimulationWorld world)
        : this(world, new RunClock(), new FixedStepAccumulator(), new PerformanceDiagnostics())
    {
    }

    /// <summary>The run clock: tick index, run time, pause set, terminal state.</summary>
    public RunClock Clock => _clock;

    /// <summary>The catch-up bound and the derivation it came from.</summary>
    public CatchUpPolicy CatchUpPolicy => _accumulator.Policy;

    /// <summary>The performance diagnostics this run has recorded.</summary>
    public PerformanceDiagnostics Diagnostics => _diagnostics;

    /// <summary>The focus-loss, suspension, and resume entry points.</summary>
    public RunLifecycleHooks Lifecycle => _lifecycle;

    /// <summary>
    /// The UI clock: the total elapsed seconds handed to <see cref="Step(double)"/>, whether
    /// the run was blocked or not.
    /// </summary>
    /// <remarks>
    /// doc 10 § Pause contract: "Render and UI clocks continue so menus remain responsive and
    /// pause presentation can animate." This is a separate clock domain from
    /// <see cref="RunClock.RunSeconds"/> and never advances it. It is a sum of what the caller
    /// reported, not a reading of any clock.
    /// </remarks>
    public double UiClockSeconds { get; private set; }

    /// <summary>
    /// Advances the run by the whole ticks that <paramref name="elapsedSeconds"/> completed.
    /// </summary>
    /// <param name="elapsedSeconds">
    /// Seconds measured by the caller since the previous step. Must be finite and not negative.
    /// </param>
    /// <returns>What the step did.</returns>
    /// <remarks>
    /// <para>
    /// While any blocking reason is present the step accumulates nothing at all: doc 10 § Pause
    /// contract requires run time and every gameplay system to "remain unchanged", so paused
    /// wall time is not banked and cannot resume into a catch-up burst. That is what makes a
    /// paused run produce the identical tick sequence to an unpaused one
    /// (<c>VER-SIM-002-009</c>).
    /// </para>
    /// <para>
    /// The UI clock advances either way.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="elapsedSeconds"/> is negative or not finite.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Called from inside <see cref="ISimulationWorld.AdvanceTick(SimulationTick)"/>. Doc 10
    /// § Concurrency baseline runs the authoritative simulation serially, and a re-entrant step
    /// would run a tick inside a tick.
    /// </exception>
    public HostStepResult Step(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(elapsedSeconds),
                elapsedSeconds,
                "an elapsed frame delta is a finite, nonnegative duration; a monotonic clock never runs "
                + "backwards (doc 10 § Clock domains)");
        }

        if (_inTick)
        {
            throw new InvalidOperationException(
                "the authoritative simulation runs serially (doc 10 § Concurrency baseline); a host step "
                + "must not be started from inside a tick");
        }

        UiClockSeconds += elapsedSeconds;

        PauseReasonSet blocking = _clock.BlockingReasons;
        if (blocking.IsBlocking)
        {
            return HostStepResult.Blocked(blocking, elapsedSeconds);
        }

        TickBudget budget = _accumulator.Advance(elapsedSeconds);
        if (budget.CatchUpBoundReached)
        {
            _diagnostics.RecordCatchUpBoundReached(_clock.CurrentTick, budget);
        }

        int executed = 0;
        SimulationTick firstTick = SimulationTick.Zero;
        SimulationTick lastTick = SimulationTick.Zero;
        bool boundaryEvaluated = false;

        for (int index = 0; index < budget.TickCount; index++)
        {
            if (_clock.HasReachedFinalBoundary)
            {
                break;
            }

            SimulationTick tick = _clock.CurrentTick;
            _inTick = true;
            try
            {
                _world.AdvanceTick(tick);
            }
            finally
            {
                _inTick = false;
            }

            _clock.CommitTick();
            if (executed == 0)
            {
                firstTick = tick;
            }

            lastTick = tick;
            executed++;

            if (_clock.HasReachedFinalBoundary)
            {
                boundaryEvaluated = EvaluateFinalBoundary();
                break;
            }
        }

        return HostStepResult.Ran(
            executed,
            firstTick,
            lastTick,
            _clock.BlockingReasons,
            elapsedSeconds,
            budget,
            boundaryEvaluated);
    }

    /// <summary>
    /// Asks whether an authored event scheduled for <paramref name="scheduledTick"/> may begin,
    /// and begins it on the world if so.
    /// </summary>
    /// <param name="scheduledTick">The tick the event is authored for.</param>
    /// <param name="scheduleEventId">The stable content ID of the authored schedule row.</param>
    /// <returns><see langword="true"/> if the event was admitted and begun.</returns>
    /// <remarks>
    /// <para>
    /// doc 20 § Boundary and tie ordering: "Active ticks cover times strictly before 35:00.
    /// After the tick covering the final pre-boundary interval commits, the clock reaches 35:00
    /// and successful extraction is evaluated before any attack, spawn, hazard, or other event
    /// scheduled for 35:00 or later can begin", and "no later simulation step can deal damage".
    /// </para>
    /// <para>
    /// An event at or after the boundary is therefore refused unconditionally - before the
    /// boundary is reached as well as after it - so the terminal evaluation necessarily precedes
    /// every such event. Refusing it only after the boundary would leave a window in which a
    /// 35:00 event could begin during the final pre-boundary tick. <c>VER-SIM-001-012</c>
    /// asserts the resulting ordering against a recording stub.
    /// </para>
    /// <para>
    /// An event is also refused while any blocking reason is present, because doc 10 § Pause
    /// contract keeps spawning, attacks, and hazards unchanged while the run is blocked.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="scheduleEventId"/> is null or blank.</exception>
    public bool TryBeginScheduledEvent(SimulationTick scheduledTick, string scheduleEventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scheduleEventId);

        if (scheduledTick >= RunClock.FinalBoundaryTick)
        {
            return false;
        }

        if (_clock.IsBlocking)
        {
            return false;
        }

        _world.BeginScheduledEvent(scheduledTick, scheduleEventId);
        return true;
    }

    /// <summary>
    /// Evaluates the 35:00 terminal boundary once and raises the terminal transition.
    /// </summary>
    /// <returns><see langword="true"/> if this call performed the evaluation.</returns>
    private bool EvaluateFinalBoundary()
    {
        if (_clock.TerminalBoundaryEvaluated)
        {
            return false;
        }

        _world.EvaluateTerminalBoundary(RunClock.FinalBoundaryTick);
        _clock.MarkTerminalBoundaryEvaluated();
        _clock.Raise(PauseReason.TerminalTransition);
        return true;
    }
}
