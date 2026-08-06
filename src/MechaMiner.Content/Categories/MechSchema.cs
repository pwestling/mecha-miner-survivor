using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-mech</c>: the field table of one playable mech.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Mechs: "Fields include
/// signature weapon ID, trait behavior kind/parameters, base
/// Hull/Armor/Recovery/movement/footprint overrides, availability, presentation,
/// selection order, and comparison text."
/// </para>
/// <para>
/// <b><c>base_overrides</c> is declared and unpopulated, on purpose.</b> Doc 40 asks a
/// mech for base overrides and no mech has any: all six differ from the player
/// baseline only through their inherent trait. Declaring the object now is what makes
/// a future override expressible without a schema change, and what lets the reader say
/// "this mech overrides nothing" rather than "this schema has no way to say that".
/// Every member is optional, so an omitted object and an empty one mean the same
/// thing - the object itself is optional and the empty one is rejected, because an
/// object present with no members would be a second way to say absent.
/// </para>
/// </remarks>
public static class MechSchema
{
    /// <summary>How an inherent trait's value combines with the baseline.</summary>
    /// <remarks>
    /// The two arms are the tree's two authored shapes: five mechs carry a percentage
    /// and one carries flat Hull. Spelling them as a kind plus a value, rather than as
    /// two optional sibling numerics exactly one of which is present, makes "exactly
    /// one" structural instead of a rule someone has to remember to write.
    /// </remarks>
    public static ClosedVocabulary TraitModifierKinds { get; } = new(
        "an inherent trait's modifier kind",
        "GDD-INITIAL-MECH-CATALOG",
        "additive-percent",
        "additive-flat-hull");

    /// <summary>The inherent trait sub-shape.</summary>
    public static DefinitionShape InherentTrait { get; } = DefinitionShape.Of(
        "a mech's inherent trait",
        DefinitionField.Text("name_key"),
        DefinitionField.Text("affected_statistic"),
        DefinitionField.Text("modifier_kind"),
        DefinitionField.Number("modifier_value"),
        DefinitionField.OptionalText("behavior_kind"));

    /// <summary>The base-override sub-shape.</summary>
    public static DefinitionShape BaseOverrides { get; } = DefinitionShape.Of(
        "a mech's overrides of the player baseline",
        DefinitionField.OptionalInteger("maximum_hull_integrity"),
        DefinitionField.OptionalInteger("armor"),
        DefinitionField.OptionalNumber("recovery_hull_per_second"),
        DefinitionField.OptionalNumber("movement_speed_m_per_s"),
        DefinitionField.OptionalNumber("collision_diameter_m"));

    /// <summary>The mech field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a mech definition",
        DefinitionField.Text("signature_weapon_id"),
        DefinitionField.Integer("selection_order"),
        DefinitionField.Text("selection_role"),
        DefinitionField.OptionalFlag("is_recommended_default"),
        DefinitionField.Object("inherent_trait", InherentTrait),
        DefinitionField.OptionalText("matching_utility_id"),
        DefinitionField.OptionalObject("base_overrides", BaseOverrides),
        DefinitionField.ArrayOf("top_down_silhouette", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The values the compiler derives for a mech.</summary>
    /// <remarks>
    /// <c>trait_stacking</c> is the whole block: its <c>changed_baseline</c> is
    /// <c>100 + trait percent</c> and its rank-3 comparison is
    /// <c>100 + trait percent + the matching utility's rank-3 percent</c>, both verified
    /// exact on every mech that has them. Doc 40 § Mechs asks a mech for "comparison
    /// text", and comparison text computed from two other definitions is the two-writer
    /// defect § Enemies and bosses names; the balance report is where it goes, with
    /// <c>matching_utility_id</c> kept on the mech so the report knows which utility to
    /// pair it with.
    /// </remarks>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "trait_stacking",
            "the changed baseline is 100 plus the trait's percent, and the rank-3 comparison is "
                + "that plus the matching utility's rank-3 percent",
            "/inherent_trait/modifier_value", "/matching_utility_id"),
        DerivedField.At(
            "signature",
            "the display name of the weapon named by signature_weapon_id, which the string "
                + "catalog already holds",
            "/signature_weapon_id"),
        DerivedField.At(
            "resolved_baseline",
            "the player baseline's maximum hull integrity plus this mech's flat-hull trait value",
            "PLAYER-01", "/inherent_trait/modifier_value"),
        DerivedField.At(
            "cross_doc_notes",
            "world speeds recomputed from the player baseline speed and this mech's trait percent",
            "PLAYER-01", "/inherent_trait/modifier_value"),
    });
}
