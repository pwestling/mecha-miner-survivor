using System;
using System.Text.RegularExpressions;
using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-resource</c>: the field table of one resource definition.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Resources: "Resource
/// definition fields include ID, canonical letter, localization keys,
/// icon/pattern/audio identity, inventory scope, persistence class, maximum safe
/// count, and resonance behavior registration if applicable."
/// </para>
/// <para>
/// <b>ID and canonical letter are two fields, because doc 40 lists them as two.</b>
/// The letter is a stable player-visible token that happens to coincide with nothing;
/// § Stable ID policy says "display names and localization keys may change without
/// changing IDs", and the converse - that a player-visible letter can outlive an ID
/// change - is what a separate field buys. Every cross-reference in the tree holds the
/// resource ID; the letter exists so a weapon recipe of opaque IDs can still be checked
/// against the weapon's own <c>W-xy</c> suffix.
/// </para>
/// <para>
/// <b>One schema with a discriminator, not two directories.</b> The catalog is really
/// two shapes - six specialized materials and two currencies, overlapping in eleven
/// fields - and <c>resource_class</c> already partitions them. Splitting the directory
/// would be a layout change, which § Accepted content repository layout requires to be
/// atomic across tooling, schemas, importers and clean-checkout tests; that is not
/// worth paying for eight files.
/// </para>
/// </remarks>
public static class ResourceSchema
{
    /// <summary>The <c>resource_class</c> discriminator's accepted tokens.</summary>
    /// <remarks>
    /// Retokenized from the authored prose classes ("specialized ordinary resource",
    /// "ordinary crafting resource", "cross-run progression resource"), which were
    /// three sentences doing an enum's job.
    /// </remarks>
    public static ClosedVocabulary ResourceClasses { get; } = new(
        "resource_class",
        "TDD-CONTENT-DATA",
        "specialized-material",
        "common-ore",
        "hyper-gold");

    /// <summary>Whether a resource's units survive the end of a run.</summary>
    public static ClosedVocabulary InventoryScopes { get; } = new(
        "inventory_scope",
        "TDD-CONTENT-DATA",
        "run-local",
        "cross-run");

    /// <summary>
    /// The persistence class doc 40 § Resources asks for, as a token rather than the
    /// sentence that was standing in for one.
    /// </summary>
    /// <remarks>
    /// The three authored sentences were one-to-one with these three classes, so
    /// nothing is lost by tokenizing; what is gained is that a validator can compare a
    /// resource's persistence with the persistence of the site that yields it, which no
    /// comparison of two English sentences could do.
    /// </remarks>
    public static ClosedVocabulary PersistenceClasses { get; } = new(
        "persistence_class",
        "TDD-CONTENT-DATA",
        "run-local-consumable",
        "run-local-currency",
        "banked-at-extraction");

    /// <summary>Which way a resonance modifier moves the statistic it names.</summary>
    public static ClosedVocabulary ModifierDirections { get; } = new(
        "a resonance modifier direction",
        "GDD-SPECIALIZED-RESOURCES",
        "increase",
        "decrease");

    /// <summary>The canonical letter pattern, as written.</summary>
    public const string CanonicalLetterPattern = "^[A-F]$";

    private static readonly Regex CanonicalLetter = new(
        CanonicalLetterPattern,
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>The resonance behavior sub-shape.</summary>
    public static DefinitionShape ResonanceBehavior { get; } = DefinitionShape.Of(
        "a resource's resonance behavior",
        DefinitionField.OptionalText("behavior_kind"),
        DefinitionField.Text("effect_name"),
        DefinitionField.Integer("modifier_percent"),
        DefinitionField.Text("modifier_direction"),
        DefinitionField.OptionalText("edge_case_rule"));

    /// <summary>The resource field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a resource definition",
        DefinitionField.Text("resource_class"),
        DefinitionField.OptionalText("canonical_letter"),
        DefinitionField.Text("inventory_scope"),
        DefinitionField.Text("persistence_class"),
        DefinitionField.OptionalInteger("maximum_safe_count"),
        DefinitionField.OptionalText("material_character"),
        DefinitionField.OptionalText("loose_association"),
        DefinitionField.OptionalText("primary_color"),
        DefinitionField.OptionalText("icon_and_silhouette_cue"),
        DefinitionField.OptionalText("audio_character"),
        DefinitionField.OptionalFlag("shared_economy_tier"),
        DefinitionField.OptionalObject("resonance_behavior", ResonanceBehavior),
        DefinitionField.OptionalText("scope"),
        DefinitionField.OptionalText("availability"),
        DefinitionField.OptionalText("primary_purpose"),
        DefinitionField.OptionalFlag("secured_only_by_mission_extraction"),
        DefinitionField.OptionalFlag("dropped_by_ordinary_enemies_and_elites"),
        DefinitionField.OptionalFlag("increased_by_power_ups"));

    /// <summary>
    /// The values the compiler derives for a resource: the per-map and per-run economy
    /// rollups the eight files were carrying.
    /// </summary>
    /// <remarks>
    /// Every number in these four fields also exists in a mining site, boss, or relic
    /// definition, verified equal. Doc 40 § Analytical requires the compiler to
    /// "recalculate ... resource totals", so the rollup is a report row with the site
    /// and boss definitions as its operands; a copy in the resource is a second writer
    /// that nothing compares against the first.
    /// </remarks>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "sources",
            "the per-source economy rollup is recomputed from the mining-site, boss, and relic "
                + "definitions that state each amount",
            "SITE-01", "SITE-02", "SITE-03", "SITE-04", "BOSS-01"),
        DerivedField.At(
            "seam_total_per_map",
            "SITE-01 total payout per seam multiplied by its count per standard map",
            "SITE-01"),
        DerivedField.At(
            "run_ceiling",
            "the sum of every Hyper Gold source's per-run total",
            "SITE-03", "BOSS-01"),
        DerivedField.At(
            "complete_run_ceiling_before_relic_sales",
            "the sum of every common-ore source's per-map total",
            "SITE-01", "SITE-02", "SITE-04"),
        DerivedField.At(
            "resonance_effect_name",
            "a copy of resonance_behavior.effect_name, verified identical in all six material files",
            "/resonance_behavior/effect_name"),
    });

    /// <summary>True when <paramref name="value"/> is one of the six canonical letters.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static bool IsCanonicalLetter(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return CanonicalLetter.IsMatch(value);
    }

    /// <summary>True when a resource of this class carries a canonical letter.</summary>
    public static bool CarriesCanonicalLetter(string? resourceClass)
    {
        return string.Equals(resourceClass, "specialized-material", StringComparison.Ordinal);
    }
}
