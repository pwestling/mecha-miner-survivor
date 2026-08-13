using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// A recording tick target: it records every call the host makes, in order, and does nothing
/// else.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-007</c>, <c>VER-SIM-001-009</c>, <c>VER-SIM-001-010</c>,
/// <c>VER-SIM-001-012</c>, <c>VER-SIM-002-002</c>, <c>VER-SIM-002-003</c>,
/// <c>VER-SIM-002-007</c>, <c>VER-SIM-002-009</c>.
/// </para>
/// <para>
/// <c>tests/verification/SIM-001.json</c> is explicit that this is the right subject at SIM-001:
/// "at SIM-001 nothing can yet mine, deal damage, or resolve a terminal outcome. The gate is
/// that the host evaluates the terminal boundary before it will admit any event scheduled at or
/// after 35:00, which is falsifiable with a recording stub." What is under test is the host's
/// call sequence, not a world.
/// </para>
/// <para>
/// Consecutive <see cref="AdvanceTick(SimulationTick)"/> calls are collapsed into one range line
/// as they arrive, so a full 126,000-tick run yields a reviewable log instead of 126,000 lines.
/// The count column keeps the collapse lossless. Every tick index is also kept in order for the
/// gaps-and-repeats assertions.
/// </para>
/// </remarks>
internal sealed class RecordingWorld : ISimulationWorld
{
    private readonly List<string> _lines = new();
    private readonly List<long> _advancedTicks = new();
    private long _pendingRangeFirst;
    private long _pendingRangeLast;
    private int _pendingRangeCount;

    /// <summary>
    /// Invoked from inside <see cref="AdvanceTick(SimulationTick)"/> when set, so a test can
    /// observe what the host does while a tick is in flight.
    /// </summary>
    internal Action<SimulationTick>? DuringTick { get; set; }

    /// <summary>Every tick index the host advanced, in the order it did so.</summary>
    internal ImmutableArray<long> AdvancedTicks => _advancedTicks.ToImmutableArray();

    /// <summary>How many ticks the host has advanced in total.</summary>
    internal int AdvanceTickCallCount => _advancedTicks.Count;

    /// <summary>How many times the host evaluated the terminal boundary.</summary>
    internal int TerminalBoundaryCallCount { get; private set; }

    /// <summary>How many scheduled events the host admitted and began.</summary>
    internal int ScheduledEventCallCount { get; private set; }

    /// <summary>The ordered call log, with any pending tick range flushed.</summary>
    internal ImmutableArray<string> Lines
    {
        get
        {
            List<string> flushed = new(_lines);
            string? pending = RenderPendingRange();
            if (pending is not null)
            {
                flushed.Add(pending);
            }

            return flushed.ToImmutableArray();
        }
    }

    /// <summary>
    /// Appends the caller's own observation to the same ordered log, so a host decision and the
    /// world calls it caused appear in one sequence.
    /// </summary>
    /// <param name="line">A canonical, tab-separated log line.</param>
    internal void Append(string line)
    {
        FlushPendingRange();
        _lines.Add(line);
    }

    /// <inheritdoc />
    public void AdvanceTick(SimulationTick tick)
    {
        _advancedTicks.Add(tick.Index);
        if (_pendingRangeCount == 0)
        {
            _pendingRangeFirst = tick.Index;
        }

        _pendingRangeLast = tick.Index;
        _pendingRangeCount++;

        DuringTick?.Invoke(tick);
    }

    /// <inheritdoc />
    public void EvaluateTerminalBoundary(SimulationTick boundaryTick)
    {
        TerminalBoundaryCallCount++;
        Append("evaluate-terminal-boundary\t" + boundaryTick.ToString());
    }

    /// <inheritdoc />
    public void BeginScheduledEvent(SimulationTick scheduledTick, string scheduleEventId)
    {
        ScheduledEventCallCount++;
        Append("begin-scheduled-event\t" + scheduledTick.ToString() + "\t" + scheduleEventId);
    }

    private void FlushPendingRange()
    {
        string? pending = RenderPendingRange();
        if (pending is null)
        {
            return;
        }

        _lines.Add(pending);
        _pendingRangeCount = 0;
    }

    private string? RenderPendingRange()
    {
        if (_pendingRangeCount == 0)
        {
            return null;
        }

        return string.Concat(
            "advance-tick-range\t",
            _pendingRangeFirst.ToString(CultureInfo.InvariantCulture),
            "\t",
            _pendingRangeLast.ToString(CultureInfo.InvariantCulture),
            "\t",
            _pendingRangeCount.ToString(CultureInfo.InvariantCulture));
    }
}
