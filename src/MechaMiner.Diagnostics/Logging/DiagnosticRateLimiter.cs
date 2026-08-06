using System;
using System.Collections.Generic;

namespace MechaMiner.Diagnostics.Logging;

/// <summary>What the rate limiter decided about one record.</summary>
internal enum RateLimitDecision
{
    /// <summary>Emit the record.</summary>
    Emit,

    /// <summary>Suppress the record; its count is folded into a later summary.</summary>
    Suppress,
}

/// <summary>One closed window's suppression summary.</summary>
internal sealed class RateLimitSummary
{
    internal RateLimitSummary(string code, int suppressed, long windowIndex)
    {
        Code = code;
        Suppressed = suppressed;
        WindowIndex = windowIndex;
    }

    /// <summary>The code whose records were suppressed.</summary>
    internal string Code { get; }

    /// <summary>How many records were suppressed in the window.</summary>
    internal int Suppressed { get; }

    /// <summary>The window that closed, counted from the limiter's origin.</summary>
    internal long WindowIndex { get; }
}

/// <summary>
/// Per-code rate limiting with a suppressed-record summary.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-007</c> (<c>TASK-FND-007-001</c>). Authority:
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Structured
/// logging: "Rate-limit repetitive diagnostics and emit a summary count", and
/// <c>CTR-OBS-001</c>: "rate-limit/drop only declared diagnostics; never block or change
/// authority".
/// </para>
/// <para>
/// Only a code that declares a burst in <see cref="DiagnosticCatalog"/> is limited. That is
/// what "only declared diagnostics" means: a unique, non-repeating event is never dropped,
/// because no summary count could reconstruct what it said.
/// </para>
/// <para>
/// Time is an explicit monotonic tick count supplied by the caller, not a wall clock. Doc 91
/// § Flake policy: "Tests do not use wall-clock sleeps for simulation behavior", and a
/// limiter that read the clock itself could only be tested by sleeping.
/// </para>
/// <para>
/// A suppressed count is never silently discarded. Closing a window returns the summary, and
/// <see cref="DrainSummaries"/> returns the pending summaries in code order so the log line
/// sequence is stable rather than dependent on dictionary enumeration.
/// </para>
/// </remarks>
internal sealed class DiagnosticRateLimiter
{
    private sealed class Window
    {
        internal long Index { get; set; }

        internal int Emitted { get; set; }

        internal int Suppressed { get; set; }
    }

    private readonly Dictionary<string, Window> _windows = new(StringComparer.Ordinal);
    private readonly List<RateLimitSummary> _pending = new();
    private readonly long _windowTicks;

    /// <summary>Creates a limiter whose windows are <paramref name="windowTicks"/> long.</summary>
    internal DiagnosticRateLimiter(long windowTicks)
    {
        if (windowTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windowTicks),
                windowTicks,
                "a rate-limit window must be positive; a zero-length window would suppress nothing and "
                + "a negative one has no meaning");
        }

        _windowTicks = windowTicks;
    }

    /// <summary>Decides whether a record with <paramref name="code"/> is emitted at <paramref name="monotonicTicks"/>.</summary>
    internal RateLimitDecision Decide(DiagnosticCode code, long monotonicTicks)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (!code.IsRateLimited)
        {
            return RateLimitDecision.Emit;
        }

        long windowIndex = monotonicTicks / _windowTicks;
        if (!_windows.TryGetValue(code.Code, out Window? window))
        {
            window = new Window { Index = windowIndex };
            _windows[code.Code] = window;
        }
        else if (window.Index != windowIndex)
        {
            CloseWindow(code.Code, window);
            window.Index = windowIndex;
            window.Emitted = 0;
            window.Suppressed = 0;
        }

        if (window.Emitted < code.Burst)
        {
            window.Emitted++;
            return RateLimitDecision.Emit;
        }

        window.Suppressed++;
        return RateLimitDecision.Suppress;
    }

    /// <summary>
    /// Returns and clears the summaries for windows that have closed, in code order so the
    /// emitted line sequence is stable.
    /// </summary>
    internal IReadOnlyList<RateLimitSummary> DrainSummaries()
    {
        if (_pending.Count == 0)
        {
            return Array.Empty<RateLimitSummary>();
        }

        _pending.Sort(static (left, right) =>
        {
            int byCode = string.CompareOrdinal(left.Code, right.Code);
            return byCode != 0 ? byCode : left.WindowIndex.CompareTo(right.WindowIndex);
        });

        RateLimitSummary[] drained = _pending.ToArray();
        _pending.Clear();
        return drained;
    }

    /// <summary>
    /// Closes every open window without draining, so a shutdown cannot lose a suppression
    /// count that was never reported because its window had not yet elapsed.
    /// </summary>
    /// <remarks>
    /// Deliberately does not return the summaries. Draining here and then draining again in
    /// the caller's write loop would consume them twice and emit them zero times, which is
    /// exactly the defect this method exists to prevent.
    /// </remarks>
    internal void CloseAllWindows()
    {
        foreach (KeyValuePair<string, Window> entry in _windows)
        {
            CloseWindow(entry.Key, entry.Value);
            entry.Value.Emitted = 0;
            entry.Value.Suppressed = 0;
        }
    }

    private void CloseWindow(string code, Window window)
    {
        if (window.Suppressed > 0)
        {
            _pending.Add(new RateLimitSummary(code, window.Suppressed, window.Index));
        }
    }
}
