using System;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Schema;

/// <summary>One assertion an instance failed.</summary>
/// <remarks>
/// A schema error is not a <c>ContentDiagnostic</c>, and deliberately so. The
/// project-owned typed validators are authoritative
/// (<c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline); the JSON Schema is the interoperability mirror, and a fixture corpus
/// proves the two reach the same verdict. Giving the mirror the power to emit stable
/// diagnostic codes would make it a second authority, and two authorities on the same
/// question is the problem the corpus exists to detect.
/// </remarks>
public sealed class JsonSchemaError
{
    internal JsonSchemaError(JsonPointer instanceLocation, string keyword, string message)
    {
        InstanceLocation = instanceLocation;
        Keyword = keyword;
        Message = message;
    }

    /// <summary>Where in the instance the assertion failed.</summary>
    public JsonPointer InstanceLocation { get; }

    /// <summary>The schema keyword that failed.</summary>
    public string Keyword { get; }

    /// <summary>What the keyword required.</summary>
    public string Message { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        string location = InstanceLocation.IsRoot ? "(root)" : InstanceLocation.Value;
        return location + " " + Keyword + ": " + Message;
    }
}
