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
    /// <remarks>
    /// <para>
    /// <b>OPEN GRANT. This vocabulary is ungranted, and its member form is not settled.</b>
    /// <c>docs/technical/40-content-data-and-validation.md</c> § Minted value vocabularies grants
    /// exactly four closed vocabularies - <c>resource_class</c>, <c>persistence_class</c>,
    /// <c>modifier_direction</c> and <c>timestamp_provenance</c> - and
    /// <c>resonance_field.applies_to</c> is not among them. So "which token should this contain"
    /// is a question that cannot be answered before the vocabulary is granted, and the same
    /// statement is on the field's own <c>description</c> in
    /// <c>content/schemas/mining-site.schema.json</c> so a measuring pass over either artifact
    /// finds it.
    /// </para>
    /// <para>
    /// <b>Two facts hold at once, and both were measured.</b> The authored corpus writes the
    /// SPACE form: <c>content/mining-sites/specialized-material-geodes.json:83</c> authors
    /// <c>["ordinary enemies", "elites", "bosses"]</c>. And the space form cannot be a member of
    /// this vocabulary at all, because <see cref="ClosedVocabulary"/>'s constructor enforces
    /// <see cref="MechaMiner.Content.Vocabulary.TokenGrammar"/> and throws
    /// <see cref="System.ArgumentException"/> on any member that fails it - substituting
    /// <c>"ordinary enemies"</c> here throws in this type's initializer and takes 306 Content
    /// tests down with it. Nothing is red today because no typed reader is pointed at
    /// <c>content/mining-sites/</c>.
    /// </para>
    /// <para>
    /// <b>EITHER OF TWO THINGS DISCHARGES THIS.</b> Written as conditions rather than as a
    /// verdict, deliberately: a label saying the declaration and the corpus cannot meet would be
    /// a claim nothing checks, and such labels accumulate unfalsified because a green run is what
    /// both a true one and a false one produce. A later reader should check whether one of these
    /// has happened, not re-form an opinion.
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <b>The vocabulary is granted</b> by an accepted document. The member form then follows
    /// from the grammar, and it is the CONTENT that moves - a content change carrying its own
    /// provenance and justification, not a token edit here.
    /// </description></item>
    /// <item><description>
    /// <b>An accepted document states that the field is prose</b>, as its three siblings
    /// <c>generation_constraint</c>, <c>spawn_rule</c> and <c>material_selection</c> already are.
    /// The schema enum then comes out and this vocabulary stops governing the field.
    /// </description></item>
    /// </list>
    /// </remarks>
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
        // A three-member enum of audience tokens, not stable IDs, so the set clause does not
        // apply and no document classifies a token vocabulary. Interim, and the one authored
        // occurrence is why it matters: specialized-material-geodes authors ["ordinary
        // enemies", "elites", "bosses"], which a set treatment would resort, measured at
        // b6c9f86.
        DefinitionField.ArrayOf(
            "applies_to", DefinitionField.ElementOf(FieldShape.Text), ArrayOrder.OrderedArray),
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
            "completion_payout", DefinitionField.ElementObject(Payout), ArrayOrder.OrderedArray),
        DefinitionField.OptionalText("partial_payout"),
        DefinitionField.Object("progress_decay", ProgressDecay),
        DefinitionField.OptionalObject("resonance_field", ResonanceField),
        // Rows carry their own progress trigger and fire in crossing order.
        DefinitionField.OptionalArrayOf(
            "beacon_thresholds",
            DefinitionField.ElementObject(BeaconThreshold),
            ArrayOrder.OrderedArray),
        DefinitionField.OptionalArrayOf(
            "beacon_rules", DefinitionField.ElementOf(FieldShape.Text), ArrayOrder.OrderedArray),
        DefinitionField.Text("depleted_state_kind"),
        DefinitionField.Flag("reactivatable"),
        DefinitionField.Text("persistence_class"),
        // Prose placement exclusions, not stable IDs, so the set clause does not apply.
        // Interim: no authored file holds this field, so nothing is observable yet.
        DefinitionField.OptionalArrayOf(
            "spawn_exclusions",
            DefinitionField.ElementOf(FieldShape.Text),
            ArrayOrder.OrderedArray),
        DefinitionField.OptionalText("map_marker_id"),
        // Elements are ^RSC-[0-9]{2}$ with uniqueItems: a set of stable resource IDs.
        DefinitionField.OptionalArrayOf(
            "eligible_material_ids",
            DefinitionField.ElementOf(FieldShape.Text),
            ArrayOrder.IdSet),
        DefinitionField.OptionalInteger("present_materials_per_run"),
        DefinitionField.OptionalInteger("material_units_per_geode"),
        DefinitionField.OptionalObject("geodes_per_present_material", IntegerRange),
        // Survey states in increasing supply, each row carrying its own state.
        DefinitionField.OptionalArrayOf(
            "abundance_states",
            DefinitionField.ElementObject(AbundanceState),
            ArrayOrder.OrderedArray),
        DefinitionField.OptionalText("survey_disclosure"),
        DefinitionField.OptionalText("rarity"),
        DefinitionField.ArrayOf(
            "rules", DefinitionField.ElementOf(FieldShape.Text), ArrayOrder.OrderedArray));

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
