using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-mining-site</c>: the field table of one mining-site class.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Mining sites: "Fields
/// include site class, count rule, zone/field dimensions, base work seconds,
/// installment thresholds/payouts, decay/grace, resource result, beacon thresholds,
/// presentation, map marker, and spawn exclusions. Standard mode validates exactly four
/// accepted classes and their totals."
/// </para>
/// <para>
/// <b>Every payout total is derived and none is authored.</b> A seam's per-seam total
/// is its installment payout times its installment count; its per-map total is that
/// times its count per map; its uninterrupted extraction per map is its installment
/// duration times both counts. All six reproduce exactly from operands in the same
/// file, so authoring them puts two writers on one number - and the rich seam's three
/// "twice the standard seam" multipliers are worse, because they are derived from
/// <em>another definition</em> and nothing in the rich seam looks wrong until the
/// standard seam changes.
/// </para>
/// </remarks>
public static class MiningSiteSchema
{
    /// <summary>What a depleted site becomes.</summary>
    public static ClosedVocabulary DepletedStateKinds { get; } = new(
        "a depleted-site state",
        "GDD-MINING",
        "non-interactive-landmark");

    /// <summary>Who a resonance field applies to.</summary>
    public static ClosedVocabulary ResonanceTargets { get; } = new(
        "a resonance field target",
        "GDD-MINING",
        "ordinary-enemies",
        "elites",
        "bosses");

    /// <summary>What starts a beacon response package.</summary>
    /// <remarks>
    /// The authored field was a string holding either "Activation" or a percentage, so
    /// one field carried two kinds of thing and duplicated a sibling on three rows out
    /// of four. A kind token plus an optional percentage says the same thing once.
    /// </remarks>
    public static ClosedVocabulary BeaconTriggerKinds { get; } = new(
        "a beacon trigger kind",
        "GDD-MINING",
        "activation",
        "progress-threshold");

    /// <summary>The resonance-field sub-shape, present on a geode only.</summary>
    /// <remarks>
    /// <c>larger_than_extraction_zone</c> is not declared: it was a boolean asserting a
    /// relation between two numbers in the same file, and a boolean cannot be wrong in
    /// the way the relation can. The relation is asserted by RC-01 against the
    /// <em>maximum expanded</em> extraction zone, which is the version of the claim that
    /// is about the played geometry rather than about the base numbers.
    /// <para>
    /// <c>modifier_magnitude</c> is not declared either: the magnitude is the
    /// material's, stated once on the material's own resonance behavior. The site
    /// references the material; the material owns the behavior.
    /// </para>
    /// </remarks>
    public static DefinitionShape ResonanceField { get; } = DefinitionShape.Of(
        "a geode's resonance field",
        DefinitionField.Number("radius_m"),
        DefinitionField.Text("shape"),
        DefinitionField.Flag("active_while_unopened"),
        DefinitionField.Flag("active_during_interruptions"),
        DefinitionField.Flag("collapses_on_open"),
        DefinitionField.Flag("retained_after_leaving_field"),
        DefinitionField.Flag("summons_enemies"),
        DefinitionField.Flag("uses_progress_thresholds"),
        DefinitionField.Flag("fields_overlap_on_standard_maps"),
        DefinitionField.Flag("modifier_named_in_geode_label_or_contextual_hud"),
        DefinitionField.ArrayOf("applies_to", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.Text("generation_constraint"));

    /// <summary>One beacon threshold row.</summary>
    public static DefinitionShape BeaconThreshold { get; } = DefinitionShape.Of(
        "a beacon threshold",
        DefinitionField.Text("trigger_kind"),
        DefinitionField.OptionalInteger("trigger_progress_percent"),
        DefinitionField.OptionalText("detail"));

    /// <summary>One payout entry.</summary>
    public static DefinitionShape Payout { get; } = DefinitionShape.Of(
        "a payout",
        DefinitionField.Integer("amount"),
        DefinitionField.Text("resource_id"),
        DefinitionField.OptionalText("detail"));

    /// <summary>One abundance state a survey can report.</summary>
    public static DefinitionShape AbundanceState { get; } = DefinitionShape.Of(
        "a survey abundance state",
        DefinitionField.Text("survey_state"),
        DefinitionField.Integer("geodes_on_map"),
        DefinitionField.Text("meaning"));

    /// <summary>The progress-decay sub-shape.</summary>
    public static DefinitionShape ProgressDecay { get; } = DefinitionShape.Of(
        "how extraction progress decays",
        DefinitionField.Number("decay_rate_multiplier_of_forward_rate"),
        DefinitionField.Number("grace_seconds"),
        DefinitionField.OptionalText("secured_checkpoint"));

    /// <summary>An integer range with both bounds.</summary>
    public static DefinitionShape IntegerRange { get; } = DefinitionShape.Of(
        "an integer range",
        DefinitionField.Integer("min"),
        DefinitionField.Integer("max"));

    /// <summary>The mining-site field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a mining-site definition",
        DefinitionField.Text("site_class"),
        DefinitionField.Integer("count_per_standard_map"),
        DefinitionField.Text("placement"),
        DefinitionField.Number("extraction_zone_radius_m"),
        DefinitionField.Number("extraction_duration_seconds"),
        DefinitionField.OptionalInteger("installment_count"),
        DefinitionField.OptionalNumber("installment_duration_seconds"),
        DefinitionField.Flag("completion_only_reward"),
        DefinitionField.OptionalObject("payout_per_installment", Payout),
        DefinitionField.OptionalArrayOf(
            "completion_payout", DefinitionField.ElementObject(Payout)),
        DefinitionField.OptionalText("partial_payout"),
        DefinitionField.Object("progress_decay", ProgressDecay),
        DefinitionField.OptionalObject("resonance_field", ResonanceField),
        DefinitionField.OptionalArrayOf(
            "beacon_thresholds", DefinitionField.ElementObject(BeaconThreshold)),
        DefinitionField.OptionalArrayOf("beacon_rules", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.Text("depleted_state_kind"),
        DefinitionField.Flag("reactivatable"),
        DefinitionField.Text("persistence_class"),
        DefinitionField.OptionalArrayOf(
            "spawn_exclusions", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.OptionalText("map_marker_id"),
        DefinitionField.OptionalArrayOf(
            "eligible_material_ids", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.OptionalInteger("present_materials_per_run"),
        DefinitionField.OptionalInteger("material_units_per_geode"),
        DefinitionField.OptionalObject("geodes_per_present_material", IntegerRange),
        DefinitionField.OptionalArrayOf(
            "abundance_states", DefinitionField.ElementObject(AbundanceState)),
        DefinitionField.OptionalText("survey_disclosure"),
        DefinitionField.OptionalText("rarity"),
        DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The values the compiler derives for a mining site.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "total_payout_per_seam",
            "payout_per_installment.amount multiplied by installment_count",
            "/payout_per_installment/amount", "/installment_count"),
        DerivedField.At(
            "total_payout_per_map",
            "the per-seam total multiplied by count_per_standard_map",
            "/payout_per_installment/amount", "/installment_count", "/count_per_standard_map"),
        DerivedField.At(
            "total_depletion_seconds",
            "installment_duration_seconds multiplied by installment_count",
            "/installment_duration_seconds", "/installment_count"),
        DerivedField.At(
            "total_uninterrupted_extraction_per_map_seconds",
            "installment_duration_seconds multiplied by installment_count and by "
                + "count_per_standard_map",
            "/installment_duration_seconds", "/installment_count", "/count_per_standard_map"),
        DerivedField.At(
            "geodes_per_standard_map",
            "geodes_per_present_material multiplied by present_materials_per_run",
            "/geodes_per_present_material", "/present_materials_per_run"),
        DerivedField.At(
            "common_ore_from_completion_jackpots_per_map",
            "the common-ore completion payout multiplied by the derived geodes per standard map",
            "/completion_payout", "/geodes_per_present_material", "/present_materials_per_run"),
        DerivedField.At(
            "relative_to_standard_seam",
            "this site's payout rates divided by the standard ore seam's. A cross-definition "
                + "derivation, and the hardest kind to notice: nothing in this file looks wrong "
                + "until SITE-01 changes",
            "SITE-01"),
        DerivedField.At(
            "beacon_response_source",
            "the schedule owns the beacon response table; the reference belongs in source_refs, "
                + "which takes a stable ID rather than a repository path",
            "WAV-01"),
    });
}
