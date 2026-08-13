using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Diagnostics.Identity;
using MechaMiner.Diagnostics.Logging;
using MechaMiner.Diagnostics.Metrics;

namespace MechaMiner.Diagnostics;

/// <summary>
/// The source-generated <c>System.Text.Json</c> metadata for every document
/// <c>CMP-OBS-001</c> reads or writes.
/// </summary>
/// <remarks>
/// <para>
/// Doc 40 § JSON codec and schema baseline: "Use the built-in
/// <c>System.Text.Json</c> reader/writer with explicit typed DTOs and
/// source-generated serialization metadata; do not add Newtonsoft.Json, runtime
/// contract reflection, or dynamic JSON objects to production paths." Property names
/// are <c>snake_case</c>, unknown fields are errors, and fields are written in
/// declaration order, which is what makes every emitted document canonical.
/// </para>
/// <para>
/// One serializer, one options object. Two option sets would let two documents in the
/// same diagnostic package disagree about naming or number handling.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    WriteIndented = true,
    NumberHandling = JsonNumberHandling.Strict,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(BuildManifest))]
[JsonSerializable(typeof(DiagnosticRunRecord))]
[JsonSerializable(typeof(BenchmarkReport))]
internal sealed partial class DiagnosticsJsonContext : JsonSerializerContext
{
    /// <summary>Writes the <c>SCH-BLD-001</c> manifest as canonical UTF-8 JSON text with a trailing newline.</summary>
    internal static string Serialize(BuildManifest manifest)
    {
        return JsonSerializer.Serialize(manifest, Default.BuildManifest) + "\n";
    }

    /// <summary>Reads a <c>SCH-BLD-001</c> manifest, rejecting unknown fields.</summary>
    internal static BuildManifest DeserializeManifest(string json)
    {
        return JsonSerializer.Deserialize(json, Default.BuildManifest)
            ?? throw new JsonException("the SCH-BLD-001 build manifest deserialized to null");
    }

    /// <summary>Writes the <c>SCH-OBS-001</c> diagnostic run record.</summary>
    internal static string Serialize(DiagnosticRunRecord record)
    {
        return JsonSerializer.Serialize(record, Default.DiagnosticRunRecord) + "\n";
    }

    /// <summary>Reads a <c>SCH-OBS-001</c> diagnostic run record, rejecting unknown fields.</summary>
    internal static DiagnosticRunRecord DeserializeRunRecord(string json)
    {
        return JsonSerializer.Deserialize(json, Default.DiagnosticRunRecord)
            ?? throw new JsonException("the SCH-OBS-001 diagnostic run record deserialized to null");
    }

    /// <summary>Writes the <c>SCH-OBS-002</c> performance report.</summary>
    internal static string Serialize(BenchmarkReport report)
    {
        return JsonSerializer.Serialize(report, Default.BenchmarkReport) + "\n";
    }

    /// <summary>Reads a <c>SCH-OBS-002</c> performance report, rejecting unknown fields.</summary>
    internal static BenchmarkReport DeserializeBenchmarkReport(string json)
    {
        return JsonSerializer.Deserialize(json, Default.BenchmarkReport)
            ?? throw new JsonException("the SCH-OBS-002 performance report deserialized to null");
    }

    /// <summary>
    /// Writes one log record as a single JSON line.
    /// </summary>
    /// <remarks>
    /// A second context, not a second options object on the first, because a line-delimited
    /// log cannot be indented and <c>WriteIndented</c> is fixed per context. The rotating log
    /// file counts bytes per line and a crash that truncates the tail must cost one record
    /// rather than the whole file.
    /// </remarks>
    internal static string SerializeLine(LogRecord record)
    {
        return JsonSerializer.Serialize(record, CompactLogJsonContext.Default.LogRecord);
    }

    /// <summary>Reads one log line, rejecting unknown fields.</summary>
    internal static LogRecord DeserializeLogLine(string json)
    {
        return JsonSerializer.Deserialize(json, CompactLogJsonContext.Default.LogRecord)
            ?? throw new JsonException("a log line deserialized to null");
    }
}

/// <summary>The log-record shape written on one line.</summary>
/// <remarks>
/// Separate from <see cref="DiagnosticsJsonContext"/> only because
/// <c>WriteIndented</c> is a per-context option and a line-delimited log must not be
/// indented. Every other option matches, so a record round-trips through either.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    WriteIndented = false,
    NumberHandling = JsonNumberHandling.Strict,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(LogRecord))]
internal sealed partial class CompactLogJsonContext : JsonSerializerContext
{
}
