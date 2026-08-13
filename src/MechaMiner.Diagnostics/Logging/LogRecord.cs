using System.Collections.Generic;

namespace MechaMiner.Diagnostics.Logging;

/// <summary>One structured field of a log record.</summary>
/// <remarks>
/// An ordered pair list rather than a dictionary. Doc 114 § C# and domain defaults: "Use
/// arrays or contiguous lists for ordered iteration; dictionaries are lookup indexes and
/// never define authoritative order." A log line must be byte-stable for the same inputs,
/// and a dictionary's enumeration order is not part of its contract.
/// </remarks>
internal sealed class LogField
{
    /// <summary>The field name, in <c>snake_case</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The redacted field value.</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// One structured log record, with every field doc 90 § Structured logging requires.
/// </summary>
/// <remarks>
/// <para>
/// Doc 90: "Logs use timestamp, monotonic sequence, severity, category, stable event code,
/// build/content identity, run/profile diagnostic IDs, tick where relevant, and structured
/// fields." Every one of those is a field here, and the schema test asserts the set rather
/// than a sample.
/// </para>
/// <para>
/// The timestamp is an explicit UTC value supplied by the caller's clock rather than read
/// from <c>DateTimeOffset.UtcNow</c> inside the record, so a test can produce a byte-stable
/// line without a wall-clock dependency (doc 91 § Flake policy).
/// </para>
/// <para>
/// The sequence is monotonic within a process and is what orders two records that share a
/// timestamp. Doc 90 requires it separately from the timestamp for exactly that reason.
/// </para>
/// </remarks>
internal sealed class LogRecord
{
    /// <summary>Stable schema identity of a log line.</summary>
    public string Schema { get; set; } = "MMD-LOG-RECORD";

    /// <summary>Version of the line's shape.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>UTC timestamp in round-trip format.</summary>
    public string TimestampUtc { get; set; } = string.Empty;

    /// <summary>Monotonic per-process sequence, which orders records that share a timestamp.</summary>
    public long Sequence { get; set; }

    /// <summary>Severity wire name.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Category wire name.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>The stable event code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The canonical <c>SCH-BLD-001</c> build identity line.</summary>
    public string BuildIdentity { get; set; } = string.Empty;

    /// <summary>The content bundle hash, or the declared unavailable status.</summary>
    public string ContentIdentity { get; set; } = string.Empty;

    /// <summary>The run diagnostic ID, or empty outside a run.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>The profile diagnostic ID, or empty before a profile loads.</summary>
    public string ProfileId { get; set; } = string.Empty;

    /// <summary>The simulation tick, or <c>-1</c> when the record is not tick-scoped.</summary>
    public long Tick { get; set; } = -1;

    /// <summary>The human-readable message, already redacted.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Structured fields, in the order the caller supplied them.</summary>
    public List<LogField> Fields { get; set; } = new();
}
