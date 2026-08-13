using System.Text.Json;
using System.Text.Json.Serialization;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// Source-generated <c>System.Text.Json</c> metadata for <see cref="EnvelopeDto"/>.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline requires "source-generated serialization metadata" and forbids "runtime
/// contract reflection". The generator ships with the .NET SDK, so this satisfies the
/// requirement without adding a package - which matters, because
/// <c>build/verify-architecture.sh</c> asserts <c>MechaMiner.Content</c>'s dependency
/// edge list is exactly empty.
/// </para>
/// <para>
/// The options are set on the context rather than at each call site so that no caller
/// can deserialize an envelope under different rules than another.
/// <c>PropertyNameCaseInsensitive</c> stays off, which is the default: doc 40 makes
/// tokens "exact case-sensitive ASCII", and a case-insensitive reader would silently
/// accept the <c>camelCase</c> property names the codec is there to reject.
/// </para>
/// </remarks>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(EnvelopeDto))]
internal sealed partial class EnvelopeJsonContext : JsonSerializerContext
{
}
