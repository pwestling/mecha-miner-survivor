using System.Text.Json;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The location and kind of one JSON value, with the value itself deliberately not
/// retained.
/// </summary>
/// <remarks>
/// This is document <em>metadata</em>, not a dynamic JSON tree. Doc 40 § JSON codec
/// and schema baseline forbids "dynamic JSON objects" in production paths because a
/// consumer that reads values out of one has bypassed its typed model. Nothing here
/// exposes a value, so the only thing a caller can ask is which pointers exist and
/// what kind each one is - exactly what an unknown-field check and a
/// <c>source_refs</c> scope resolution need, and nothing more.
/// </remarks>
public readonly struct JsonNodeInfo
{
    /// <summary>Creates a node record.</summary>
    public JsonNodeInfo(JsonPointer location, JsonValueKind kind)
    {
        Location = location;
        Kind = kind;
    }

    /// <summary>Where the value is.</summary>
    public JsonPointer Location { get; }

    /// <summary>What kind of value it is.</summary>
    public JsonValueKind Kind { get; }
}
