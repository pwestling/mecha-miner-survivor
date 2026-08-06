namespace MechaMiner.Content.Envelope;

/// <summary>
/// One parsed <c>source_refs</c> element.
/// </summary>
/// <remarks>
/// The element is kept as structure, not as the string it was authored from. A
/// traceability report has to answer "which definitions cite <c>GDD-MINING</c>",
/// "which cite its <c>#geode-resonance-fields</c> section", and "which field of which
/// definition does each citation account for"; every one of those is a substring
/// search against a raw string and a field access against this type.
/// </remarks>
public sealed class SourceRef
{
    internal SourceRef(
        string text,
        SourceRefScope? scope,
        SourceRefKind kind,
        string reference,
        string documentId,
        string? anchor)
    {
        Text = text;
        Scope = scope;
        Kind = kind;
        Reference = reference;
        DocumentId = documentId;
        Anchor = anchor;
    }

    /// <summary>The element exactly as authored.</summary>
    public string Text { get; }

    /// <summary>
    /// The scope prefix, or null when the reference accounts for the whole definition.
    /// </summary>
    public SourceRefScope? Scope { get; }

    /// <summary>What kind of authority is referenced.</summary>
    public SourceRefKind Kind { get; }

    /// <summary>The reference part, without the scope prefix and including any anchor.</summary>
    public string Reference { get; }

    /// <summary>
    /// The stable identifier of the referenced document, decision, or requirement,
    /// without its anchor.
    /// </summary>
    public string DocumentId { get; }

    /// <summary>The <c>#anchor</c> part without its <c>#</c>, or null when there is none.</summary>
    public string? Anchor { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Text;
    }
}
