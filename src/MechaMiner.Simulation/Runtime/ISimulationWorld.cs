using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// The tick target: the only thing <see cref="SimulationHost"/> calls to advance
/// authoritative state.
/// </summary>
/// <remarks>
/// <para>
/// <c>CMP-SIM-001</c> simulation world in
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry.
/// The implementation belongs to the later SIM packages that own entity state; this
/// interface is the seam so the host's own contract - one call per whole tick, ascending,
/// never re-entrant, terminal boundary before any 35:00-or-later event - is assertable
/// against a recording stub before any of that state exists.
/// </para>
/// <para>
/// <b>No member takes a duration.</b> doc 10 § Clock domains: the host "never passes a
/// variable delta to authoritative systems." Every member takes a
/// <see cref="SimulationTick"/> and nothing else, which is what makes it structurally
/// impossible for a tick target to read a partial delta - the property
/// <c>VER-SIM-001-002</c> asserts.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): the host lives in this assembly
/// but the run is driven from <c>CMP-PRS-001</c> in <c>game/</c>, and
/// <c>MechaMiner.Game.Tests</c> supplies its own world to assert the integration runner.
/// Hence <c>public</c>.
/// </para>
/// </remarks>
public interface ISimulationWorld
{
    /// <summary>
    /// Runs one complete authoritative tick as a transaction over the prior committed state.
    /// </summary>
    /// <param name="tick">
    /// The tick being executed, which covers <c>[tick.Index / 60, (tick.Index + 1) / 60)</c>
    /// seconds of run time.
    /// </param>
    /// <remarks>
    /// <para>
    /// The whole tick transaction of <c>docs/technical/20-simulation-core.md</c> § Tick
    /// transaction happens inside this call: read the prior committed state and the admitted
    /// commands, execute the fixed phase order, append to tick-local buffers, apply each
    /// buffer in its commit phase, evaluate terminal state, and publish the committed state,
    /// snapshot, events, and diagnostics as one tick result. The host owns only step ordering
    /// across ticks, not the phases within one.
    /// </para>
    /// <para>
    /// Throwing invalidates the tick: doc 20 § Tick transaction requires an exception or
    /// invariant failure before commit to end the run through the safe technical-failure path
    /// and never to publish partial state. The host therefore does not commit the run clock
    /// for a tick whose call threw.
    /// </para>
    /// </remarks>
    void AdvanceTick(SimulationTick tick);

    /// <summary>
    /// Evaluates the 35:00 terminal boundary, once, after the final pre-boundary tick has
    /// committed.
    /// </summary>
    /// <param name="boundaryTick">
    /// The boundary tick itself - index 126,000 - which is never executed.
    /// </param>
    /// <remarks>
    /// doc 20 § Boundary and tie ordering: "After the tick covering the final pre-boundary
    /// interval commits, the clock reaches 35:00 and successful extraction is evaluated before
    /// any attack, spawn, hazard, or other event scheduled for 35:00 or later can begin." The
    /// host guarantees the ordering; what extraction resolution actually does belongs to the
    /// packages that own damage and extraction.
    /// </remarks>
    void EvaluateTerminalBoundary(SimulationTick boundaryTick);

    /// <summary>
    /// Begins an authored scheduled event the host has admitted.
    /// </summary>
    /// <param name="scheduledTick">The tick the event is scheduled for.</param>
    /// <param name="scheduleEventId">The stable content ID of the authored schedule row.</param>
    /// <remarks>
    /// <para>
    /// doc 10 § System phase ordering, phase 2: "Evaluate authored schedule boundaries for the
    /// current tick; the 35:00 terminal boundary is handled before another tick can begin."
    /// The host is therefore the admission point for a scheduled event, and this call happens
    /// only for an event it admitted - never for one at or after 35:00.
    /// </para>
    /// <para>
    /// The authored schedule itself belongs to the encounter packages. This member is the
    /// minimum seam the host needs to make its own ordering rule observable and is expected to
    /// be replaced by the schedule owner's contract, not to become one.
    /// </para>
    /// </remarks>
    void BeginScheduledEvent(SimulationTick scheduledTick, string scheduleEventId);
}
