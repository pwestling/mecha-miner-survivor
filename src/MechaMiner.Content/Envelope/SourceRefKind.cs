namespace MechaMiner.Content.Envelope;

/// <summary>
/// What kind of authority a <c>source_refs</c> element points at.
/// </summary>
/// <remarks>
/// The five kinds are the union of the identifier families
/// <c>docs/conventions.md</c> § Stable identifiers and
/// <c>docs/technical/conventions.md</c> § Stable identifiers mint for things a
/// definition can implement. The kind is retained rather than discarded after parsing
/// because a traceability report groups by it: "which definitions implement this
/// decision" and "which definitions cite this document" are different questions.
/// </remarks>
public enum SourceRefKind
{
    /// <summary>A gameplay document, <c>GDD-&lt;DOMAIN&gt;</c>.</summary>
    GameplayDocument = 0,

    /// <summary>A technical design document, <c>TDD-&lt;DOMAIN&gt;</c>.</summary>
    TechnicalDocument = 1,

    /// <summary>A gameplay decision, <c>DEC-###</c>.</summary>
    GameplayDecision = 2,

    /// <summary>A technical decision record, <c>TDR-###</c>.</summary>
    TechnicalDecision = 3,

    /// <summary>A technical requirement, <c>TR-&lt;DOMAIN&gt;-###</c>.</summary>
    TechnicalRequirement = 4,
}
