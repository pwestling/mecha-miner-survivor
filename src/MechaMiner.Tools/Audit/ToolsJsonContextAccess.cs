using System.Text.Json;
using System.Text.Json.Serialization;

namespace MechaMiner.Tools.Audit;

/// <summary>
/// Source-generated <c>System.Text.Json</c> metadata for <c>SCH-QUA-001</c> documents.
/// </summary>
/// <remarks>
/// <para>
/// A separate context from <c>MechaMiner.Tools.Cli.ToolsJsonContext</c> for one reason:
/// naming policy. The verification registries were authored in <c>camelCase</c> by
/// <c>FND-001</c> and every entry in them is a stable, never-renumbered record, so
/// rewriting three committed registries to <c>snake_case</c> would churn every field of
/// every entry to satisfy a convention doc 40 § JSON codec and schema baseline sets for
/// the <i>content</i> pipeline rather than for tooling data that doc 91 leaves unspecified.
/// Reading them as authored is the smaller and more reversible choice.
/// </para>
/// <para>
/// Unknown fields are still rejected: a field nobody reads is either a typo or an
/// unregistered contract change, and either way the registry validator should say so.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    NumberHandling = JsonNumberHandling.Strict,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(VerificationRegistryDocument))]
internal sealed partial class VerificationRegistryJsonContext : JsonSerializerContext
{
}

/// <summary>The registry validator's reader for <c>SCH-QUA-001</c> documents.</summary>
internal static class ToolsJsonContextAccess
{
    /// <summary>Reads a <c>SCH-QUA-001</c> verification registry, rejecting unknown fields.</summary>
    internal static VerificationRegistryDocument DeserializeVerificationRegistry(string json)
    {
        return JsonSerializer.Deserialize(json, VerificationRegistryJsonContext.Default.VerificationRegistryDocument)
            ?? throw new JsonException("the SCH-QUA-001 verification registry deserialized to null");
    }
}
