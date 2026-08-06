using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// The run's performance diagnostic counters, in occurrence order.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Frame metrics
/// requires "frame/tick catch-up count and accumulator debt" among the frame metrics, and
/// doc 10 § Clock domains requires reaching the catch-up bound to produce a performance
/// diagnostic. This type is where the host puts them; <c>VER-SIM-001-013</c> reads them
/// through the <c>PERF-04</c> benchmark report.
/// </para>
/// <para>
/// Records are kept in a list, in the order they occurred, not in a dictionary:
/// <c>docs/technical/114-autonomous-agent-execution-protocol.md</c> § C# and domain defaults
/// makes dictionaries "lookup indexes only" that "never define authoritative order", and
/// occurrence order is authoritative here - it is what distinguishes one diagnostic per
/// occurrence from one per discarded tick.
/// </para>
/// <para>
/// <b>This type reads no clock.</b> Every record is stamped with the run's tick index, which
/// the host supplies; nothing here consults wall time (doc 20 § Scope and invariants).
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>MechaMiner.Tools</c> renders
/// the counters into the benchmark and scenario reports, and <c>CMP-PRS-001</c> in
/// <c>game/</c> surfaces them in the development overlay. Hence <c>public</c>.
/// </para>
/// </remarks>
public sealed class PerformanceDiagnostics
{
    private readonly List<CatchUpDiagnostic> _catchUpOccurrences = new();

    /// <summary>
    /// How many times the catch-up bound has been reached: doc 90's "frame/tick catch-up
    /// count".
    /// </summary>
    /// <remarks>
    /// Exactly one increment per occurrence. <c>VER-SIM-001-013</c> requires a warmed
    /// ten-minute <c>PERF-04</c> capture to report zero.
    /// </remarks>
    public int CatchUpBoundReachedCount => _catchUpOccurrences.Count;

    /// <summary>
    /// The total accumulator debt discarded across the run, in whole ticks.
    /// </summary>
    public int TotalDiscardedTickCount
    {
        get
        {
            int total = 0;
            foreach (CatchUpDiagnostic occurrence in _catchUpOccurrences)
            {
                total += occurrence.DiscardedTickCount;
            }

            return total;
        }
    }

    /// <summary>
    /// The total accumulator debt discarded across the run, in seconds, derived from the whole
    /// tick total by one division.
    /// </summary>
    public double TotalDebtSeconds => TickRate.SecondsForTicks(TotalDiscardedTickCount);

    /// <summary>Every catch-up occurrence, in the order it happened.</summary>
    public ImmutableArray<CatchUpDiagnostic> CatchUpOccurrences => _catchUpOccurrences.ToImmutableArray();

    /// <summary>
    /// Records one occurrence of the catch-up bound being reached.
    /// </summary>
    /// <param name="tick">The tick the run stood at.</param>
    /// <param name="budget">The bounded budget the accumulator returned.</param>
    /// <remarks>
    /// Internal because <see cref="SimulationHost"/> is the only writer: doc 115
    /// § Mutable-state ownership matrix gives the run session sole ownership of its
    /// diagnostics, and <c>TR-CTR-002</c> requires exactly one registered writer. Consumers
    /// read through the public members above.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="budget"/> did not reach the catch-up bound, so there is no occurrence
    /// to record.
    /// </exception>
    internal void RecordCatchUpBoundReached(SimulationTick tick, TickBudget budget)
    {
        if (!budget.CatchUpBoundReached)
        {
            throw new ArgumentException(
                "a catch-up diagnostic is recorded only for a budget that actually reached the bound; "
                + "doc 10 § Clock domains ties the diagnostic to the bound, not to every step",
                nameof(budget));
        }

        _catchUpOccurrences.Add(
            new CatchUpDiagnostic(tick, budget.TickCount, budget.DiscardedTickCount));
    }
}
