namespace MechaMiner.Content.Categories;

/// <summary>
/// The declared shape of one field in a category's field table.
/// </summary>
/// <remarks>
/// <para>
/// These are authoring shapes, not JSON value kinds, which is why
/// <see cref="Integer"/> and <see cref="Number"/> are separate members even though
/// both are a JSON number. <c>docs/technical/40-content-data-and-validation.md</c>
/// § Unit and numeric policy requires "integer currency and rank values" to be
/// integral in source; a shape that could not say so would push integrality into
/// per-field prose and lose it.
/// </para>
/// <para>
/// The structural pass can only assert the JSON kind, because a scanned structure
/// carries locations and kinds and deliberately not values. Integrality is therefore
/// asserted by the typed value pass, against the same declaration. Both passes read
/// this one table, so the two cannot drift.
/// </para>
/// </remarks>
public enum FieldShape
{
    /// <summary>Reserved so a default-initialised field is never a real shape.</summary>
    Unspecified = 0,

    /// <summary>A JSON string.</summary>
    Text,

    /// <summary>A JSON number the value pass additionally requires to be integral.</summary>
    Integer,

    /// <summary>A JSON number with no integrality requirement.</summary>
    Number,

    /// <summary>A JSON <c>true</c> or <c>false</c>.</summary>
    Flag,

    /// <summary>A JSON object whose properties are the declared nested field table.</summary>
    Object,

    /// <summary>A JSON array whose elements all have the declared element shape.</summary>
    Array,

    /// <summary>
    /// A JSON object whose keys are <em>not</em> declared here, because their contract
    /// belongs to a registered behavior descriptor rather than to the content schema.
    /// </summary>
    /// <remarks>
    /// Doc 40 § Behavior registries: "The content compiler verifies every content
    /// <c>behavior_kind</c>, targeting policy, formula, modifier hook, formation,
    /// effect, and presentation recipe has exactly one registered descriptor with a
    /// compatible parameter schema" - so the per-kind parameter schema lives with the
    /// descriptor, and duplicating it in the content schema would create a second
    /// writer on it. Three fields in the tree genuinely have no shared structure to
    /// factor: relic effects use seventy-four keys with none shared between any two
    /// relics, branch effects use three hundred and ninety with twelve shared, and
    /// weapon fixed properties use forty-seven with four shared. A discriminated union
    /// with forty-five arms is the same open map spelled worse.
    /// <para>
    /// What this shape does <em>not</em> do is let arbitrary content through
    /// unvalidated: the map's values are still scanned by the strict codec, so a
    /// <c>null</c>, a comment, a duplicate key, or a non-<c>snake_case</c> key inside
    /// one is still an error. Only the key <em>vocabulary</em> is deferred.
    /// </para>
    /// </remarks>
    ParameterMap,
}
