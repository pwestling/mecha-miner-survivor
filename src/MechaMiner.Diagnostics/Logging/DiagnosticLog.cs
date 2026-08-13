using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Diagnostics.Identity;

namespace MechaMiner.Diagnostics.Logging;

/// <summary>Run and profile identity a log record carries while a run is active.</summary>
internal sealed class DiagnosticScope
{
    /// <summary>The run diagnostic ID, or empty outside a run.</summary>
    internal string RunId { get; set; } = string.Empty;

    /// <summary>The profile diagnostic ID, or empty before a profile loads.</summary>
    internal string ProfileId { get; set; } = string.Empty;

    /// <summary>The current simulation tick, or <c>-1</c> when no run is ticking.</summary>
    internal long Tick { get; set; } = -1;
}

/// <summary>
/// The bounded structured local log.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-007</c> (<c>TASK-FND-007-001</c>). Authority:
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Structured
/// logging, <c>CTR-OBS-001</c> ("stable code/ID, bounded structured fields, monotonic
/// sequence where ordered ... rate-limit/drop only declared diagnostics; never block or
/// change authority"), and <c>docs/technical/115</c> § Component registry, which states
/// <c>CMP-OBS-001</c>'s frame affinity as "never blocks authoritative tick on I/O".
/// Requirements: <c>TR-OBS-001</c>, <c>TR-OBS-002</c>, <c>TR-BLD-003</c>.
/// </para>
/// <para>
/// <b>Writing never touches I/O.</b> <see cref="Write"/> renders, redacts, rate-limits, and
/// appends to a bounded in-memory ring, then returns. <see cref="Drain"/> performs every
/// write to the sink and is called off the authoritative tick. No tick exists yet, so this
/// is a structural guarantee rather than an observed one, and it is asserted directly: a
/// test writes with a sink that records whether it was touched during <see cref="Write"/>.
/// </para>
/// <para>
/// The ring is bounded and drops the <b>oldest</b> record when it is full. That is the right
/// direction for a crash breadcrumb buffer, whose value is the records nearest the failure,
/// and the drop is counted and reported through <c>MMD-0006</c> rather than being silent.
/// </para>
/// <para>
/// Time is injected. The UTC timestamp and the monotonic tick both come from delegates, so a
/// test produces byte-stable lines without a wall-clock dependency (doc 91 § Flake policy).
/// </para>
/// <para>
/// One writer. Doc 115 § Mutable-state ownership matrix gives "Logs/metrics/evidence
/// buffers" a single owner, so this type is not thread-safe by design and is used from the
/// owning thread; an off-thread producer hands over a value rather than sharing this buffer.
/// </para>
/// </remarks>
internal sealed class DiagnosticLog
{
    /// <summary>The default ring capacity, which is also the crash breadcrumb depth.</summary>
    internal const int DefaultCapacity = 512;

    /// <summary>The default rate-limit window, in the injected clock's ticks.</summary>
    /// <remarks>One second when the injected clock counts milliseconds, which is what the callers do.</remarks>
    internal const long DefaultWindowTicks = 1000;

    private readonly ILogSink _sink;
    private readonly Redaction _redaction;
    private readonly DiagnosticRateLimiter _limiter;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Func<long> _monotonicTicks;
    private readonly LogRecord?[] _ring;
    private int _head;
    private int _count;
    private long _sequence;

    /// <summary>Creates a log over one sink.</summary>
    internal DiagnosticLog(
        ILogSink sink,
        Redaction redaction,
        Func<DateTimeOffset> utcNow,
        Func<long> monotonicTicks,
        int capacity = DefaultCapacity,
        long windowTicks = DefaultWindowTicks)
    {
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(redaction);
        ArgumentNullException.ThrowIfNull(utcNow);
        ArgumentNullException.ThrowIfNull(monotonicTicks);
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "the ring must hold at least one record");
        }

        _sink = sink;
        _redaction = redaction;
        _limiter = new DiagnosticRateLimiter(windowTicks);
        _utcNow = utcNow;
        _monotonicTicks = monotonicTicks;
        _ring = new LogRecord?[capacity];
    }

    /// <summary>The run and profile identity every record carries.</summary>
    internal DiagnosticScope Scope { get; } = new();

    /// <summary>Records queued but not yet written to the sink.</summary>
    internal int Pending => _count;

    /// <summary>Records suppressed by rate limiting since construction.</summary>
    internal int Suppressed { get; private set; }

    /// <summary>Records the bounded ring discarded because it was full.</summary>
    internal int Overflowed { get; private set; }

    /// <summary>Records the sink refused to write.</summary>
    internal int Dropped { get; private set; }

    /// <summary>
    /// Queues one record. Performs no I/O and never throws for an expected condition.
    /// </summary>
    internal void Write(string code, string message, params LogField[] fields)
    {
        DiagnosticCode registered = DiagnosticCatalog.Require(code);
        long ticks = _monotonicTicks();

        if (_limiter.Decide(registered, ticks) == RateLimitDecision.Suppress)
        {
            Suppressed++;
            return;
        }

        Enqueue(Compose(registered, message, fields));
    }

    /// <summary>
    /// Writes every queued record, and every closed rate-limit summary, to the sink.
    /// </summary>
    /// <remarks>
    /// Called off the authoritative tick. The summary lines are emitted first so a reader
    /// sees the suppression that explains the gap before the records that follow it.
    /// </remarks>
    internal int Drain()
    {
        int written = 0;

        foreach (RateLimitSummary summary in _limiter.DrainSummaries())
        {
            LogRecord record = Compose(
                DiagnosticCatalog.Require(DiagnosticCatalog.LogRateLimitSummary),
                "suppressed repeated diagnostics in the window that just closed",
                new LogField { Name = "suppressed_code", Value = summary.Code },
                new LogField
                {
                    Name = "suppressed_count",
                    Value = summary.Suppressed.ToString(CultureInfo.InvariantCulture),
                },
                new LogField
                {
                    Name = "window_index",
                    Value = summary.WindowIndex.ToString(CultureInfo.InvariantCulture),
                });

            if (WriteToSink(record))
            {
                written++;
            }
        }

        while (_count > 0)
        {
            LogRecord? record = Take();
            if (record is not null && WriteToSink(record))
            {
                written++;
            }
        }

        return written;
    }

    /// <summary>
    /// Closes every rate-limit window and drains, so shutdown cannot lose a suppression
    /// count whose window had not yet elapsed.
    /// </summary>
    internal int Flush()
    {
        _limiter.CloseAllWindows();
        return Drain();
    }

    /// <summary>
    /// The queued records, oldest first, without writing them. This is the crash breadcrumb
    /// buffer doc 90 § Crash handling flushes on a best-effort basis.
    /// </summary>
    internal IReadOnlyList<LogRecord> Snapshot()
    {
        List<LogRecord> records = new(_count);
        for (int offset = 0; offset < _count; offset++)
        {
            LogRecord? record = _ring[(_head + offset) % _ring.Length];
            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private LogRecord Compose(DiagnosticCode code, string message, params LogField[] fields)
    {
        BuildManifest identity = BuildIdentity.Current;
        LogRecord record = new()
        {
            TimestampUtc = _utcNow().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            Sequence = ++_sequence,
            Severity = DiagnosticCatalog.NameOf(code.Severity),
            Category = DiagnosticCatalog.NameOf(code.Category),
            Code = code.Code,
            BuildIdentity = identity.IdentityLine,
            ContentIdentity = identity.Content.BundleSha256.Length > 0
                ? identity.Content.BundleSha256
                : identity.Content.Status + ":" + identity.Content.OwningWorkPackage,
            RunId = _redaction.Apply(Scope.RunId),
            ProfileId = _redaction.Apply(Scope.ProfileId),
            Tick = Scope.Tick,
            Message = _redaction.Apply(message),
        };

        foreach (LogField field in fields)
        {
            record.Fields.Add(new LogField
            {
                Name = field.Name,
                Value = _redaction.Apply(field.Value),
            });
        }

        return record;
    }

    private void Enqueue(LogRecord record)
    {
        if (_count == _ring.Length)
        {
            // Drop the oldest. A crash breadcrumb buffer's value is the records nearest the
            // failure, so the newest record wins; the loss is counted, never silent.
            _ring[_head] = null;
            _head = (_head + 1) % _ring.Length;
            _count--;
            Overflowed++;
        }

        _ring[(_head + _count) % _ring.Length] = record;
        _count++;
    }

    private LogRecord? Take()
    {
        LogRecord? record = _ring[_head];
        _ring[_head] = null;
        _head = (_head + 1) % _ring.Length;
        _count--;
        return record;
    }

    private bool WriteToSink(LogRecord record)
    {
        if (_sink.TryWriteLine(LogRecordText.Render(record)))
        {
            return true;
        }

        Dropped++;
        return false;
    }
}
