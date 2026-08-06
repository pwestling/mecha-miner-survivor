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
/// after each; and when the clock has reached 35:00, evaluate the terminal boundary and raise
/// <see cref="PauseReason.TerminalTransition"/> before the step returns. doc 10 § System phase
/// ordering, phase 2: "the 35:00 terminal boundary is handled before another tick can begin."
/// </para>
/// <para>
/// <b>The boundary is evaluated from both positions the condition occupies, not only after a
/// commit.</b> A step can begin with the clock already at or past 35:00 and the boundary not yet
/// evaluated - the clock's <see cref="RunClock.CommitTick"/> is public and the host is
/// constructed over a caller-supplied clock - and phase 2's rule is owed there as well. A step
/// that only stopped at that position left the run past 35:00, unblocked, still admitting
/// scheduled events, and returning a zero-tick result forever.
/// <see cref="EvaluateFinalBoundary"/> is idempotent, so occupying both positions still
/// evaluates once per run.
/// </para>
/// <para>
/// <b>A tick that cannot be committed ends the run.</b> The tick call and the commit are one
/// region: doc 20 § Tick transaction requires an exception or invariant failure before commit to
/// invalidate the tick and "end[] the run through the safe technical-failure path", and doc 90
/// § Crash handling requires reporting "without attempting to continue corrupted simulation".
/// Once the tick target has returned, the world has moved and the clock has not, so a refused
/// commit cannot be retried: a later step that re-ran the same tick would break
/// <c>VER-SIM-001-010</c>'s "no gap and no repeat". <see cref="HasEndedInTechnicalFailure"/> is
/// the recorded fact, and every later <see cref="Step(double)"/> refuses.
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
    private Exception? _technicalFailure;
    private SimulationTick _technicalFailureTick;

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
    /// Whether the run ended through the safe technical-failure path, so no later step runs a
    /// tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>docs/technical/20-simulation-core.md</c> § Tick transaction: "An exception or invariant
    /// failure before commit invalidates the tick and ends the run through the safe
    /// technical-failure path; it never publishes a partial state."
    /// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Crash handling is
    /// what "safe" means for the step loop: reporting is registered "without attempting to
    /// continue corrupted simulation". A tick that was applied to the world but not committed to
    /// the clock has left the two disagreeing, so continuing would either re-run the tick or
    /// carry the disagreement forward, and <c>VER-SIM-001-010</c> forbids the first.
    /// </para>
    /// <para>
    /// <b>Why this is host state and not run-clock state.</b> <see cref="RunClock"/> owns the
    /// run's terminal state, and doc 20 § Scope and invariants makes a terminal result something
    /// "assigned once" and "immutable". A technical failure is not a terminal result: doc 20
    /// § Tick transaction says such a failure "never publishes a partial state", so it must not
    /// occupy the field a real extraction outcome will be written to, and
    /// <see cref="ISimulationWorld.EvaluateTerminalBoundary(SimulationTick)"/> is deliberately
    /// not called. The host owns step ordering across ticks, and refusing to run another tick is
    /// a step-ordering fact.
    /// </para>
    /// </remarks>
    public bool HasEndedInTechnicalFailure => _technicalFailure is not null;

    /// <summary>
    /// The failure that ended the run, or <see langword="null"/> while the run is healthy.
    /// </summary>
    /// <remarks>
    /// Retained rather than only counted, so the diagnostic names the real defect. The same
    /// exception was rethrown unchanged to the caller of <see cref="Step(double)"/>, on the
    /// precedent doc 20 § Mid-commit invalidation sets for the other half of the tick: the
    /// failure "is then rethrown unchanged".
    /// </remarks>
    public Exception? TechnicalFailure => _technicalFailure;

    /// <summary>
    /// The tick that was in flight when the run ended technically, meaningful only while
    /// <see cref="HasEndedInTechnicalFailure"/>.
    /// </summary>
    public SimulationTick TechnicalFailureTick => _technicalFailureTick;

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
    /// Called from inside <see cref="ISimulationWorld.AdvanceTick(SimulationTick)"/> - doc 10
    /// § Concurrency baseline runs the authoritative simulation serially, and a re-entrant step
    /// would run a tick inside a tick - or called after the run ended through the
    /// technical-failure path, in which case the refusal carries that failure as its inner
    /// exception.
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

        if (_technicalFailure is not null)
        {
            throw new InvalidOperationException(
                "the run ended through the safe technical-failure path at tick "
                + _technicalFailureTick.ToString()
                + ", so no later step runs a tick: doc 90 § Crash handling registers reporting \"without "
                + "attempting to continue corrupted simulation\", and doc 20 § Tick transaction ends the run "
                + "rather than retrying the tick whose call failed",
                _technicalFailure);
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
                // The boundary can already have been reached before this step began, and the
                // evaluation is owed at that position too. doc 10 § System phase ordering, phase
                // 2: "the 35:00 terminal boundary is handled before another tick can begin." A
                // step that only broke out of the loop here left the run past 35:00, unblocked,
                // still admitting scheduled events, and evaluating the boundary never.
                // EvaluateFinalBoundary is idempotent, so visiting both positions cannot
                // evaluate twice.
                boundaryEvaluated = EvaluateFinalBoundary() || boundaryEvaluated;
                break;
            }

            SimulationTick tick = _clock.CurrentTick;
            try
            {
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
            }
            catch (Exception failure)
            {
                // Everything from the tick call to the commit is one region, and a failure
                // anywhere in it leaves the world and the clock disagreeing about whether this
                // tick happened. doc 20 § Tick transaction: such a failure "invalidates the tick
                // and ends the run through the safe technical-failure path". Two failures reach
                // here. The tick target threw, which doc 20 names directly. Or the tick target
                // returned and the commit was refused, which is the pause-set condition in its
                // second position: RunClock.CommitTick refuses while a blocking reason is
                // present, so a reason raised from inside the tick makes that tick uncommittable.
                // Ending the run is what stops the next step from re-running the same tick, which
                // is what VER-SIM-001-010's "no repeat" forbids. The exception is rethrown
                // unchanged, on the precedent of doc 20 § Mid-commit invalidation.
                EndRunInTechnicalFailure(tick, failure);
                throw;
            }

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

    /// <summary>
    /// Records that the run ended through the safe technical-failure path, so no later step runs
    /// a tick.
    /// </summary>
    /// <param name="tick">The tick that was in flight.</param>
    /// <param name="failure">The failure, which the caller rethrows unchanged.</param>
    /// <remarks>
    /// It records and refuses; it publishes nothing and evaluates no boundary. doc 20 § Tick
    /// transaction: such a failure "never publishes a partial state", and <c>TR-RUN-007</c> in
    /// <c>docs/technical/112-normative-requirement-index.md</c> § Foundation and runtime states
    /// the same requirement unqualified.
    /// </remarks>
    private void EndRunInTechnicalFailure(SimulationTick tick, Exception failure)
    {
        _technicalFailure = failure;
        _technicalFailureTick = tick;
    }
}
