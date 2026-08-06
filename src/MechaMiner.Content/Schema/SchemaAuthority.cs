namespace MechaMiner.Content.Schema;

/// <summary>
/// The provenance of a numeric bound, recorded as the <c>x-authority</c> annotation.
/// </summary>
/// <remarks>
/// <para>
/// Without this, "which schema bounds need re-deriving now that a capacity section
/// changed" is answerable only from memory. With it, it is a query over
/// <c>content/schemas/**</c>.
/// </para>
/// <para>
/// <see cref="Section"/> is a heading name and never a line number, for the same reason
/// <c>source_refs</c> rejects a <c>path:line</c> pair: a heading survives an edit and a
/// line number does not.
/// </para>
/// </remarks>
public sealed class SchemaAuthority
{
    internal SchemaAuthority(
        SchemaAuthorityKind kind,
        string? source,
        string? section,
        string? derivation)
    {
        Kind = kind;
        Source = source;
        Section = section;
        Derivation = derivation;
    }

    /// <summary>Whether the bound is sourced, derived, or structural.</summary>
    public SchemaAuthorityKind Kind { get; }

    /// <summary>
    /// The document ID the bound comes from, in the same vocabulary <c>source_refs</c>
    /// uses. Null for a <see cref="SchemaAuthorityKind.Structural"/> bound.
    /// </summary>
    public string? Source { get; }

    /// <summary>The heading within <see cref="Source"/>. Null for a structural bound.</summary>
    public string? Section { get; }

    /// <summary>
    /// How the bound follows from its source, in plain language. Null for a structural
    /// bound, where <c>description</c> already carries the rationale.
    /// </summary>
    /// <remarks>
    /// <see cref="Source"/> says <em>where</em> a number came from; this says <em>why it
    /// is that number</em>, and the two go stale independently. A cited section can change
    /// in a way that invalidates the reasoning while leaving the bound arithmetically
    /// defensible, and nothing catches that except a stated derivation someone can
    /// re-check.
    /// </remarks>
    public string? Derivation { get; }

    /// <summary>The keywords that <b>require</b> an adjacent authority.</summary>
    /// <remarks>
    /// These are the numeric bounds an authority can go stale against. Presence keywords
    /// such as <c>required</c> and shape keywords such as <c>type</c> carry no number, so
    /// there is nothing about them to re-derive.
    /// </remarks>
    public static string[] BoundKeywords()
    {
        return new[] { "minimum", "maximum", "minItems", "maxItems", "maxLength", "multipleOf" };
    }

    /// <summary>The keywords an authority <b>may</b> annotate.</summary>
    /// <remarks>
    /// A superset of <see cref="BoundKeywords"/>. The three extras are numeric bounds
    /// too, and attributing one must not be an error just because the mandatory set does
    /// not name it; requiring attribution and permitting it are different questions, and
    /// conflating them would make good practice fail the build. Whether the mandatory set
    /// should grow to include them is an integration-owner decision, not one to take by
    /// widening a constant.
    /// </remarks>
    public static string[] AttributableKeywords()
    {
        return new[]
        {
            "minimum", "maximum", "minItems", "maxItems", "maxLength", "multipleOf",
            "minLength", "exclusiveMinimum", "exclusiveMaximum",
        };
    }

    /// <summary>The annotation's own property name.</summary>
    public const string Keyword = "x-authority";

    /// <inheritdoc/>
    public override string ToString()
    {
        return Kind == SchemaAuthorityKind.Structural
            ? "structural"
            : Kind.ToString().ToUpperInvariant() + " from " + Source + " § " + Section
                + " (" + Derivation + ")";
    }
}
