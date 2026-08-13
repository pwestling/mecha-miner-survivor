using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-elite-modifiers</c>: the field table of <c>ELT-01</c>, the shared
/// elite modifier profile.
/// </summary>
/// <remarks>
/// <para>
/// This is not an enemy and does not validate against the enemy field table. It is a
/// modifier profile applied on top of one, and it shares only six fields with an enemy;
/// merging the two would produce a table in which almost everything is optional and the
/// real requirement is "these fields together or those fields together".
/// </para>
/// <para>
/// It lives in <c>content/enemies/</c> rather than in a constants directory because
/// doc 40 § Accepted content repository layout groups definitions "by stable item or
/// the smallest cohesive aggregate", and the smallest cohesive aggregate over the ten
/// enemies is a file beside them.
/// </para>
/// <para>
/// <b><c>body_scale_multiplier</c> keeps its name here even though an enemy also has
/// one.</b> Doc 40 § Unit and numeric policy: a multiplicative scale "keeps one name in
/// every scope it appears in". The composition chain reads
/// <c>reference diameter x enemy body_scale_multiplier x elite body_scale_multiplier</c>
/// in one vocabulary, and disambiguating by renaming one of them is the change that
/// breaks that property.
/// </para>
/// </remarks>
public static class EliteModifierSchema
{
    /// <summary>The order in which modifier layers apply.</summary>
    /// <remarks>
    /// The authored array's fourth entry was a relic's display name in one file and the
    /// same relic's fuller display name in another - two files disagreeing about the
    /// spelling of one ordinal, with a display name inside an enum vocabulary. The
    /// fourth layer is "relic"; which relic contributes to it is the relic's own
    /// definition.
    /// </remarks>
    public static ClosedVocabulary ModifierLayers { get; } = new(
        "a modifier application layer",
        "GDD-INITIAL-ALIEN-ROSTER",
        "base",
        "elite",
        "resonance",
        "relic");

    /// <summary>The recycling sub-shape.</summary>
    public static DefinitionShape Recycling { get; } = DefinitionShape.Of(
        "how elites are recycled",
        DefinitionField.Text("beacon_tagged_elites"),
        DefinitionField.Text("ordinary_elites"));

    /// <summary>The presentation-requirement sub-shape.</summary>
    public static DefinitionShape PresentationRequirements { get; } = DefinitionShape.Of(
        "what an elite must show",
        DefinitionField.ArrayOf("required", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.Text("insufficient"));

    /// <summary>The elite modifier field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "the shared elite modifier profile",
        DefinitionField.Number("maximum_hull_multiplier"),
        DefinitionField.Number("movement_speed_multiplier"),
        DefinitionField.Number("contact_damage_multiplier"),
        DefinitionField.Number("body_scale_multiplier"),
        DefinitionField.Integer("added_control_resistance_percent"),
        DefinitionField.Integer("control_resistance_maximum_percent"),
        DefinitionField.Number("post_hard_control_immunity_seconds"),
        DefinitionField.Flag("adds_behavior"),
        DefinitionField.Flag("adds_attacks_phases_aura_or_support_ai"),
        DefinitionField.Flag("adds_loot"),
        DefinitionField.Text("retains_base_identity_behavior"),
        DefinitionField.Integer("maximum_scheduled_elites_at_once"),
        DefinitionField.Text("beacon_elites_additional"),
        DefinitionField.ArrayOf(
            "modifier_application_order", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.Object("recycling", Recycling),
        DefinitionField.Object("presentation_requirements", PresentationRequirements));

    /// <summary>The values the compiler derives for the elite profile.</summary>
    /// <remarks>
    /// <c>worked_examples</c> is a table of elite contact damages recomputed from an
    /// enemy's contact damage and this profile's multiplier. It is a derived report row
    /// that happened to be authored as data, and its one entry disagreeing with its
    /// operands would be invisible.
    /// </remarks>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "worked_examples",
            "an enemy's contact damage multiplied by contact_damage_multiplier, and again by the "
                + "resonance modifier of the material whose field it stands in",
            "/contact_damage_multiplier", "EN-09", "RSC-01"),
    });
}
