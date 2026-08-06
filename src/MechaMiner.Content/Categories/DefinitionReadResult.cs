using System.Collections.Generic;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;

namespace MechaMiner.Content.Categories;

/// <summary>The outcome of reading one category definition.</summary>
/// <remarks>
/// Like <c>EnvelopeReadResult</c>, this carries no exit class and no build verdict: a
/// pure library that knew about exit classes would make <c>MechaMiner.Content</c>
/// depend on the CLI's vocabulary, and a verb maps severities onto one at the boundary.
/// </remarks>
public sealed class DefinitionReadResult
{
    internal DefinitionReadResult(
        ContentDefinition? definition,
        IReadOnlyList<ContentDiagnostic> diagnostics,
        JsonStructure structure)
    {
        Definition = definition;
        Diagnostics = diagnostics;
        Structure = structure;
    }

    /// <summary>The validated definition, or null when any error was reported.</summary>
    public ContentDefinition? Definition { get; }

    /// <summary>Every diagnostic produced, in the order the stages produced them.</summary>
    public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }

    /// <summary>The scanned document shape.</summary>
    public JsonStructure Structure { get; }

    /// <summary>True when the definition validated and a model was produced.</summary>
    public bool IsValid => Definition is not null;
}
