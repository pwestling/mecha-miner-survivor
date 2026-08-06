using System.Collections.Generic;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;

namespace MechaMiner.Content.Envelope;

/// <summary>The outcome of reading one source definition's envelope.</summary>
/// <remarks>
/// The result carries no exit class and no build verdict. Exit classes are a
/// build-tool contract owned by <c>docs/technical/100</c> § Standard command surface;
/// a pure library that knew about them would make <c>MechaMiner.Content</c> depend on
/// the CLI's vocabulary. A verb maps <see cref="ContentDiagnosticSeverity"/> onto an
/// exit class at the boundary.
/// </remarks>
public sealed class EnvelopeReadResult
{
    internal EnvelopeReadResult(
        DefinitionEnvelope? envelope,
        IReadOnlyList<ContentDiagnostic> diagnostics,
        JsonStructure structure)
    {
        Envelope = envelope;
        Diagnostics = diagnostics;
        Structure = structure;
    }

    /// <summary>The validated envelope, or null when any error was reported.</summary>
    public DefinitionEnvelope? Envelope { get; }

    /// <summary>Every diagnostic produced, in the order the stages produced them.</summary>
    public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }

    /// <summary>The scanned document shape, for a caller that needs to resolve pointers.</summary>
    public JsonStructure Structure { get; }

    /// <summary>True when the definition validated and an envelope was produced.</summary>
    public bool IsValid => Envelope is not null;
}
