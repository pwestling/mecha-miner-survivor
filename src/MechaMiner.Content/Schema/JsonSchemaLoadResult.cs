using System.Collections.Generic;
using MechaMiner.Content.Diagnostics;

namespace MechaMiner.Content.Schema;

/// <summary>The outcome of loading a schema document.</summary>
public sealed class JsonSchemaLoadResult
{
    internal JsonSchemaLoadResult(
        JsonSchemaDocument? schema,
        IReadOnlyList<ContentDiagnostic> diagnostics)
    {
        Schema = schema;
        Diagnostics = diagnostics;
    }

    /// <summary>The loaded schema, or null when loading failed.</summary>
    public JsonSchemaDocument? Schema { get; }

    /// <summary>
    /// Why loading failed: an unimplemented keyword, an unresolvable <c>$ref</c>, or a
    /// document that is not a schema at all.
    /// </summary>
    public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }

    /// <summary>True when the schema loaded.</summary>
    public bool IsValid => Schema is not null;
}
