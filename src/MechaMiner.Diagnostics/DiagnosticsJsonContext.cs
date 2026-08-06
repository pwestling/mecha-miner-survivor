using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Diagnostics.Identity;

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
}
