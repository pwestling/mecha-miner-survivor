using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-weapon</c>: the field table of one weapon.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Weapons: "Fields include
/// recipe material pair, behavior kind, targeting policy, fixed properties, three
/// stat-track definitions, rank-zero values, increments, snapshot/live classifications,
/// all branch IDs, analytical-model registration, presentation/audio references, and
/// rock-targeting behavior."
/// </para>
/// <para>
/// <b>The recipe is two resource IDs and nothing else.</b> One weapon file carried the
/// same two materials four times - as letter codes, as display names, as a notation
/// string, and again inside a fabrication cost block - and its three branch entries
/// carried the branch material three more times each. Four encodings of one fact means
/// four writers and no validator between them.
/// </para>
/// <para>
/// <b>What the ID-only recipe costs, and what pays for it.</b> A recipe of opaque
/// resource IDs is no longer legible to a reader, so a mis-assigned pair stops being
/// visible by eye. The compensation is a relational check that resolves each recipe
/// resource to its canonical letter and requires the concatenation to equal the
/// weapon's own <c>W-xy</c> suffix. That check is why the resources catalog carries a
/// canonical letter at all.
/// </para>
/// </remarks>
public static class WeaponSchema
{
    /// <summary>How many unordered material-pair recipes the catalog accepts.</summary>
    /// <remarks>
    /// Doc 40 § Weapons: "The compiler verifies exactly 15 unordered material-pair
    /// recipes, no duplicate pair, exactly three stats". Fifteen is the number of
    /// unordered pairs from six materials, so it is a consequence of the material set
    /// rather than an independent choice - but it is asserted as a number, because the
    /// two could drift apart and only one of them is stated in a document.
    /// </remarks>
    public const int AcceptedRecipeCount = 15;

    /// <summary>How many ore-upgradeable stat tracks a weapon declares.</summary>
    public const int StatTrackCount = 3;

    /// <summary>How many branches a weapon has, one per transformation class.</summary>
    public const int BranchCount = 3;

    /// <summary>How many resources a recipe pair names.</summary>
    public const int RecipeMaterialCount = 2;

    /// <summary>The unit a stat track's rank-zero value and increment are measured in.</summary>
    /// <remarks>
    /// This is the one place in the tree where a unit is a value rather than a name
    /// suffix, and it is the right call here: <c>rank_zero</c> and
    /// <c>increment_per_rank</c> are a generic pair whose unit varies per row, so doc 40
    /// § Unit and numeric policy's suffix rule cannot apply and an explicit token is the
    /// alternative. <c>m</c> means mech collision diameters, which is what the unit M is
    /// throughout this project.
    /// </remarks>
    public static ClosedVocabulary StatUnits { get; } = new(
        "a stat track unit",
        "GDD-WEAPON-NUMERIC-CATALOG",
        "damage",
        "m",
        "activations-per-second",
        "damage-per-second",
        "seconds",
        "percentage-points-per-second",
        "count",
        "revolutions-per-second");

    /// <summary>One ore-upgradeable stat track.</summary>
    public static DefinitionShape StatTrack { get; } = DefinitionShape.Of(
        "an ore-upgradeable stat track",
        DefinitionField.Integer("slot"),
        DefinitionField.Text("name"),
        DefinitionField.Text("unit"),
        DefinitionField.Number("rank_zero"),
        DefinitionField.Number("increment_per_rank"),
        DefinitionField.Flag("discrete"));

    /// <summary>
    /// Which of a weapon's timings the global Attack Rate statistic reaches.
    /// </summary>
    /// <remarks>
    /// Both members are arrays of timing names rather than one sentence each, so a
    /// future validator can check that every listed timing is a timing the weapon has.
    /// The timing vocabulary itself is not closed yet: no accepted document enumerates
    /// one, and closing it from the fifteen authored sentences would be inventing it.
    /// </remarks>
    public static DefinitionShape GlobalAttackRateMapping { get; } = DefinitionShape.Of(
        "the global attack rate mapping",
        DefinitionField.ArrayOf("affected_timings", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.ArrayOf("unaffected_timings", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The weapon field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a weapon definition",
        DefinitionField.ArrayOf(
            "recipe_pair_material_ids", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.OptionalText("signature_mech_id"),
        DefinitionField.Text("behavior_kind"),
        DefinitionField.Text("targeting_policy"),
        DefinitionField.Text("rock_targeting_behavior"),
        DefinitionField.ArrayOf(
            "ore_upgradeable_stats", DefinitionField.ElementObject(StatTrack)),
        DefinitionField.ParameterMap("fixed_properties"),
        DefinitionField.Object("global_attack_rate_mapping", GlobalAttackRateMapping),
        DefinitionField.ArrayOf("branch_ids", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.Text("base_automatic_attack_text"),
        DefinitionField.Text("base_behavior_text"));

    /// <summary>The values the compiler derives for a weapon.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "damage_model",
            "the burst, sustained, and favorable-horde damage estimates are recomputed by the "
                + "analytical layer from the stat tracks and fixed properties; the accepted "
                + "gameplay table is what the recomputation is compared against, and it lives "
                + "with that comparison's fixtures rather than in source",
            "/ore_upgradeable_stats", "/fixed_properties"),
        DerivedField.At(
            "ore_upgradeable_stat_labels",
            "a UI label list is player-facing text, which belongs in the string catalog; the "
                + "authored list also disagreed with this file's own stat names on every weapon",
            "/ore_upgradeable_stats"),
        DerivedField.At(
            "branches",
            "the nested branch entries restate each branch's own name, funding material, and "
                + "cost; branch_ids references them instead",
            "/branch_ids"),
        DerivedField.At(
            "fabrication_cost",
            "one unit of each recipe material, which is the same rule on every weapon",
            "/recipe_pair_material_ids"),
        DerivedField.At(
            "recipe_pair",
            "the letter codes, display names, and notation are three renderings of "
                + "recipe_pair_material_ids",
            "/recipe_pair_material_ids"),
    });
}
