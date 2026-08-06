using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Tools.Toolchain;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// The source-generated <c>System.Text.Json</c> metadata for every document this
/// process reads or writes.
/// </summary>
/// <remarks>
/// Doc 40 § JSON codec and schema baseline: "Use the built-in
/// <c>System.Text.Json</c> reader/writer with explicit typed DTOs and
/// source-generated serialization metadata; do not add Newtonsoft.Json, runtime
/// contract reflection, or dynamic JSON objects to production paths." Property
/// names are <c>snake_case</c>, unknown fields are errors, and fields are written
/// in schema-declared order, which for source generation is declaration order.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.Unspecified,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    WriteIndented = true,
    NumberHandling = JsonNumberHandling.Strict,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(InvocationRecord))]
[JsonSerializable(typeof(ToolchainPins))]
internal sealed partial class ToolsJsonContext : JsonSerializerContext
{
    /// <summary>
    /// Writes <paramref name="record"/> as canonical UTF-8 JSON text with a
    /// trailing newline, so the document is a reviewable text artifact.
    /// </summary>
    internal static string Serialize(InvocationRecord record)
    {
        return JsonSerializer.Serialize(record, Default.InvocationRecord) + "\n";
    }

    /// <summary>Reads <c>build/toolchain.json</c>, rejecting unknown fields.</summary>
    internal static ToolchainPins DeserializePins(string json)
    {
        return JsonSerializer.Deserialize(json, Default.ToolchainPins)
            ?? throw new JsonException("build/toolchain.json deserialized to null");
    }
}
