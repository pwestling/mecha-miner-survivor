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
        string? derivation,
        string? rationale)
    {
        Kind = kind;
        Source = source;
        Section = section;
        Derivation = derivation;
        Rationale = rationale;
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
    /// bound, which has no source to follow from and states a <see cref="Rationale"/>
    /// instead.
    /// </summary>
    /// <remarks>
    /// <see cref="Source"/> says <em>where</em> a number came from; this says <em>why it
    /// is that number</em>, and the two go stale independently. A cited section can change
    /// in a way that invalidates the reasoning while leaving the bound arithmetically
    /// defensible, and nothing catches that except a stated derivation someone can
    /// re-check.
    /// </remarks>
    public string? Derivation { get; }

    /// <summary>
    /// Why a structural bound is this number. Required for
    /// <see cref="SchemaAuthorityKind.Structural"/> and null for every other kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This used to be the enclosing subschema's <c>description</c>, which is an arity
    /// failure of exactly the kind <see cref="Kind"/> was moved into a per-bound map to fix.
    /// A <c>description</c> is per subschema, so one sentence licensed every structural
    /// bound under it: a subschema asserting <c>minLength</c> and <c>maxLength</c> beneath
    /// <c>"description": "the envelope is bounded"</c> satisfied the rule for both, and
    /// nothing could check which clause covered which number. A rationale is a property of
    /// a number, and it lives with the number.
    /// </para>
    /// <para>
    /// It is <em>absent</em> rather than optional on a sourced or derived bound, mirroring
    /// how <see cref="Source"/>, <see cref="Section"/>, and <see cref="Derivation"/> are
    /// absent here. Those kinds already state a <see cref="Derivation"/> — "why it is that
    /// number" — so a second prose field asking the same question would mean neither is the
    /// one to read, and the redundant one is the one that fills with filler. Each kind has
    /// exactly one complete field set, and which field carries the justification is decided
    /// by the kind rather than by whoever wrote the entry.
    /// </para>
    /// </remarks>
    public string? Rationale { get; }

    /// <summary>
    /// The keywords that <b>require</b> an adjacent authority, and equally the only
    /// keywords one <b>may</b> annotate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the numeric bounds an authority can go stale against. Presence keywords
    /// such as <c>required</c> and shape keywords such as <c>type</c> carry no number, so
    /// there is nothing about them to re-derive.
    /// </para>
    /// <para>
    /// The exclusive bounds are here because there is no principled line between "at most
    /// 2048" and "strictly less than 2049". The same number is being asserted either way,
    /// and a set that demanded provenance for one spelling and not the other would be
    /// asking about syntax when the question is about the number.
    /// </para>
    /// <para>
    /// <c>minLength</c> is here for the case the obvious argument misses. It will nearly
    /// always be <c>structural</c>, and saying so costs one line; the line it buys is the
    /// one place a genuinely sourced length — a localization key length, an ID length
    /// taken from a document — could otherwise sit unattributed. An exemption list is
    /// where a fail-open hides, and the argument for adding one is always that the cases
    /// are obviously structural.
    /// </para>
    /// <para>
    /// Required and permitted are one list rather than two. They were briefly separate,
    /// on the reasoning that attributing a bound outside the mandatory set must not be an
    /// error — but that reasoning only bites while some bound is outside the set, and
    /// none now is. Two lists that must stay equal are a drift risk with nothing on the
    /// other side of the ledger.
    /// </para>
    /// </remarks>
    public static string[] BoundKeywords()
    {
        return new[]
        {
            "minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum",
            "minItems", "maxItems", "minLength", "maxLength", "multipleOf",
        };
    }

    /// <summary>The annotation's own property name.</summary>
    public const string Keyword = "x-authority";

    /// <inheritdoc/>
    public override string ToString()
    {
        return Kind == SchemaAuthorityKind.Structural
            ? "structural (" + Rationale + ")"
            : Kind.ToString().ToUpperInvariant() + " from " + Source + " § " + Section
                + " (" + Derivation + ")";
    }
}
