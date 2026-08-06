using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-utility</c>: the field table of one utility.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Utilities: "Fields include
/// assigned material or ore-only radar exception, unlock ownership, one-unit
/// fabrication cost, slot behavior, behavior kind, base value, three rank values/prices
/// where applicable, affected named stats, stacking classification, and presentation.
/// Validators enforce no duplicate installed identity, allowed rank count, and exactly
/// the accepted fresh/unlocked distribution."
/// </para>
/// <para>
/// <b>One boolean replaces nine nullable fields.</b> The resource radar carried nine
/// <c>null</c>s, every one of them saying "this is the ore-only exception". Doc 40 names
/// that exception as a <em>field</em>, so it is one - and the nine become nine
/// conditional omissions gated by it, which is a rule a schema can express and nine
/// nulls are not.
/// </para>
/// <para>
/// <b>Ranks are integers with zero meaning Installed.</b> The authored strings
/// - "Installed", "Rank 1" - are display text, and they are ordinal, so they sort and
/// validate as integers and not as strings. The array then runs 0 through
/// <c>rank_count</c>, which makes the length invariant explicit and checkable.
/// </para>
/// </remarks>
public static class UtilitySchema
{
    /// <summary>How many utilities are available on a fresh profile.</summary>
    /// <remarks>
    /// <c>docs/68-utility-catalog.md</c> § Fresh profile and unlocked availability. The
    /// three counts are asserted together across the catalog: moving one utility from
    /// one pool to the other leaves every file individually valid and the catalog wrong.
    /// </remarks>
    public const int FreshProfileCount = 6;

    /// <summary>How many utilities the utility-suite unlock adds.</summary>
    public const int HyperGoldUnlockCount = 6;

    /// <summary>How many always-available utilities there are: the ore-only radar.</summary>
    public const int AlwaysAvailableCount = 1;

    /// <summary>Where a utility comes from.</summary>
    public static ClosedVocabulary PoolAvailabilities { get; } = new(
        "a pool availability",
        "GDD-UTILITY-CATALOG",
        "fresh-profile",
        "hyper-gold-unlock",
        "always-available");

    /// <summary>How a utility's rank value combines with the statistic it names.</summary>
    /// <remarks>
    /// The closest thing to a registered behavior kind anywhere in the authored tree:
    /// five clean tokens already in kebab case, one per shape of contribution. The
    /// behavior registry should look here first.
    /// </remarks>
    public static ClosedVocabulary EffectKinds { get; } = new(
        "a utility effect kind",
        "GDD-UTILITY-CATALOG",
        "additive-percent",
        "flat-additive-hull",
        "recharging-charge",
        "additive-hull-per-second",
        "directional-bearings");

    /// <summary>One rank row.</summary>
    /// <remarks>
    /// A uniform <c>{rank, value}</c> replaces thirteen differently-named payload keys,
    /// one per utility. The unit comes from <c>effect_kind</c> and the statistic from
    /// <c>affected_stat_names</c>, so nothing is lost and the rank arrays stop being
    /// thirteen structurally different shapes for one concept.
    /// </remarks>
    public static DefinitionShape Rank { get; } = DefinitionShape.Of(
        "a utility rank",
        DefinitionField.Integer("rank"),
        DefinitionField.Number("value"));

    /// <summary>The acquisition sub-shape.</summary>
    public static DefinitionShape Acquisition { get; } = DefinitionShape.Of(
        "how a utility is acquired",
        DefinitionField.Integer("rank_count"),
        DefinitionField.OptionalArrayOf(
            "rank_ore_costs", DefinitionField.ElementOf(FieldShape.Integer)),
        DefinitionField.OptionalInteger("material_unit_cost"),
        DefinitionField.OptionalInteger("common_ore_cost"),
        DefinitionField.Integer("utility_slots_filled"),
        DefinitionField.Flag("unique_in_loadout"),
        DefinitionField.Flag("dismantlable"),
        DefinitionField.Flag("replaceable"),
        DefinitionField.Flag("sellable"),
        DefinitionField.Flag("refundable_during_run"),
        DefinitionField.Flag("requires_manual_activation"),
        DefinitionField.Flag("has_alternative_recipes"),
        DefinitionField.Flag("has_branches"),
        DefinitionField.OptionalFlag("previews_all_four_effect_tiers"),
        DefinitionField.OptionalFlag("rank_values_are_totals_not_cumulative_additions"));

    /// <summary>The availability sub-shape.</summary>
    public static DefinitionShape Availability { get; } = DefinitionShape.Of(
        "when a utility is available",
        DefinitionField.Text("pool_availability"),
        DefinitionField.OptionalText("unlock_id"),
        DefinitionField.OptionalFlag("always_present_in_fixed_fabrication_catalog"));

    /// <summary>The radar's tracking sub-shape.</summary>
    public static DefinitionShape Tracking { get; } = DefinitionShape.Of(
        "what a utility tracks",
        DefinitionField.Flag("provides_direction_only"),
        DefinitionField.Flag("provides_exact_distance"),
        DefinitionField.Flag("provides_map_marker"),
        DefinitionField.Flag("tracks_relic_caches"),
        DefinitionField.Integer("maximum_simultaneous_bearings"),
        DefinitionField.Integer("bearing_fan_threshold_degrees"),
        DefinitionField.Integer("bearing_cluster_collapse_above_count"),
        DefinitionField.ArrayOf("tracked_categories", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The utility field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a utility definition",
        DefinitionField.OptionalText("material_id"),
        DefinitionField.Flag("ore_only_exception"),
        DefinitionField.OptionalText("primary_role"),
        DefinitionField.OptionalText("coverage_role"),
        DefinitionField.Text("behavior_kind"),
        DefinitionField.Text("effect_kind"),
        DefinitionField.ArrayOf("affected_stat_names", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.OptionalText("stacking_classification"),
        DefinitionField.OptionalInteger("stored_charges"),
        DefinitionField.OptionalArrayOf("ranks", DefinitionField.ElementObject(Rank)),
        DefinitionField.Object("acquisition", Acquisition),
        DefinitionField.Object("availability", Availability),
        DefinitionField.OptionalObject("tracking", Tracking),
        DefinitionField.ArrayOf("effect_rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The values the compiler derives for a utility.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.Nested(
            new[] { "acquisition", "total_rank_ore_cost" },
            "the sum of acquisition.rank_ore_costs",
            "/acquisition/rank_ore_costs"),
        DerivedField.Nested(
            new[] { "tracking", "tracked_category_count" },
            "the length of tracking.tracked_categories",
            "/tracking/tracked_categories"),
        DerivedField.At(
            "catalog_wide_rules",
            "fourteen sentences byte-identical across all thirteen utilities. A rule set no "
                + "per-utility validator can ever fire on is a catalog rule, and thirteen copies "
                + "are thirteen writers on it",
            "GDD-UTILITY-CATALOG"),
        DerivedField.At(
            "installed_to_rank_3",
            "a summary of the first and last rank values, carrying an arrow glyph and no envelope "
                + "slot",
            "/ranks"),
        DerivedField.At(
            "external_numerics",
            "cross-document numeric checks whose values are held by the definitions they check "
                + "against; they are analytical-report assertions rather than utility data"),
    });
}

/// <summary>One validated utility definition.</summary>
public sealed class UtilityDefinition : ContentDefinition
{
    internal UtilityDefinition(
        DefinitionEnvelope envelope,
        string? materialId,
        bool oreOnlyException,
        string behaviorKind,
        string effectKind,
        IReadOnlyList<string> affectedStatNames,
        string? primaryRole,
        string poolAvailability,
        string? unlockId,
        long rankCount,
        IReadOnlyList<UtilityRank> ranks)
        : base(envelope, DefinitionKind.Utility)
    {
        MaterialId = materialId;
        OreOnlyException = oreOnlyException;
        BehaviorKind = behaviorKind;
        EffectKind = effectKind;
        AffectedStatNames = affectedStatNames;
        PrimaryRole = primaryRole;
        PoolAvailability = poolAvailability;
        UnlockId = unlockId;
        RankCount = rankCount;
        Ranks = ranks;
    }

    /// <summary>The material this utility is assigned to, absent on the ore-only radar.</summary>
    public string? MaterialId { get; }

    /// <summary>Whether this is the ore-only radar exception.</summary>
    public bool OreOnlyException { get; }

    /// <summary>The registered behavior token.</summary>
    public string BehaviorKind { get; }

    /// <summary>How the rank values combine with the statistics they name.</summary>
    public string EffectKind { get; }

    /// <summary>The named statistics this utility modifies.</summary>
    public IReadOnlyList<string> AffectedStatNames { get; }

    /// <summary>The role this utility fills, where a document states one.</summary>
    public string? PrimaryRole { get; }

    /// <summary>Which pool this utility comes from.</summary>
    public string PoolAvailability { get; }

    /// <summary>The unlock that grants it, where one does.</summary>
    public string? UnlockId { get; }

    /// <summary>How many ranks above Installed the utility has.</summary>
    public long RankCount { get; }

    /// <summary>The rank rows, starting at rank zero for Installed.</summary>
    public IReadOnlyList<UtilityRank> Ranks { get; }

    /// <summary>One utility rank.</summary>
    public sealed class UtilityRank
    {
        internal UtilityRank(long rank, double value)
        {
            Rank = rank;
            Value = value;
        }

        /// <summary>The rank, where zero means Installed.</summary>
        public long Rank { get; }

        /// <summary>The value at this rank, in the unit the effect kind implies.</summary>
        public double Value { get; }
    }
}

/// <summary>The wire shape of a utility definition's domain fields.</summary>
internal sealed class UtilityDto
{
    [JsonPropertyName("material_id")]
    public string? MaterialId { get; set; }

    [JsonPropertyName("ore_only_exception")]
    public bool? OreOnlyException { get; set; }

    [JsonPropertyName("primary_role")]
    public string? PrimaryRole { get; set; }

    [JsonPropertyName("behavior_kind")]
    public string? BehaviorKind { get; set; }

    [JsonPropertyName("effect_kind")]
    public string? EffectKind { get; set; }

    [JsonPropertyName("affected_stat_names")]
    public List<string>? AffectedStatNames { get; set; }

    [JsonPropertyName("stacking_classification")]
    public string? StackingClassification { get; set; }

    [JsonPropertyName("stored_charges")]
    public double? StoredCharges { get; set; }

    [JsonPropertyName("ranks")]
    public List<RankDto>? Ranks { get; set; }

    [JsonPropertyName("acquisition")]
    public AcquisitionDto? Acquisition { get; set; }

    [JsonPropertyName("availability")]
    public AvailabilityDto? Availability { get; set; }

    internal sealed class RankDto
    {
        [JsonPropertyName("rank")]
        public double? Rank { get; set; }

        [JsonPropertyName("value")]
        public double? Value { get; set; }
    }

    internal sealed class AcquisitionDto
    {
        [JsonPropertyName("rank_count")]
        public double? RankCount { get; set; }

        [JsonPropertyName("rank_ore_costs")]
        public List<double>? RankOreCosts { get; set; }

        [JsonPropertyName("material_unit_cost")]
        public double? MaterialUnitCost { get; set; }

        [JsonPropertyName("common_ore_cost")]
        public double? CommonOreCost { get; set; }

        [JsonPropertyName("utility_slots_filled")]
        public double? UtilitySlotsFilled { get; set; }
    }

    internal sealed class AvailabilityDto
    {
        [JsonPropertyName("pool_availability")]
        public string? PoolAvailability { get; set; }

        [JsonPropertyName("unlock_id")]
        public string? UnlockId { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="UtilityDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(UtilityDto))]
internal sealed partial class UtilityJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one utility definition.</summary>
public static class UtilityReader
{
    /// <summary>Reads one utility.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public static DefinitionReadResult Read(ReadOnlySpan<byte> utf8, CategoryReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DiagnosticBag bag = new();
        if (!CategoryPrelude.Run(
                utf8, context, bag, out DefinitionEnvelope? envelope,
                out DocumentOutline outline, out JsonStructure structure))
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        UtilityDto? dto = JsonSerializer.Deserialize(utf8, UtilityJsonContext.Default.UtilityDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        string? id = envelope?.Id.Value;
        Validate(dto, outline, context, id, bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        List<UtilityDefinition.UtilityRank> ranks = new();
        foreach (UtilityDto.RankDto rank in dto.Ranks ?? new List<UtilityDto.RankDto>())
        {
            ranks.Add(new UtilityDefinition.UtilityRank(
                (long)rank.Rank!.Value, rank.Value!.Value));
        }

        UtilityDefinition definition = new(
            envelope,
            dto.MaterialId,
            dto.OreOnlyException!.Value,
            dto.BehaviorKind!,
            dto.EffectKind!,
            new ReadOnlyCollection<string>(new List<string>(dto.AffectedStatNames!)),
            dto.PrimaryRole,
            dto.Availability!.PoolAvailability!,
            dto.Availability.UnlockId,
            (long)dto.Acquisition!.RankCount!.Value,
            new ReadOnlyCollection<UtilityDefinition.UtilityRank>(ranks));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        UtilityDto dto,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.BehaviorToken(
            dto.BehaviorKind, root.AppendProperty("behavior_kind"), context, id, bag);
        SemanticCheck.Token(
            dto.EffectKind, UtilitySchema.EffectKinds, root.AppendProperty("effect_kind"), context,
            id, bag);

        if (dto.StackingClassification is not null)
        {
            SemanticCheck.Token(
                dto.StackingClassification, ContentVocabularies.StackingClassifications,
                root.AppendProperty("stacking_classification"), context, id, bag);
        }

        List<string> stats = dto.AffectedStatNames ?? new();
        JsonPointer statsPointer = root.AppendProperty("affected_stat_names");
        for (int index = 0; index < stats.Count; index++)
        {
            SemanticCheck.Token(
                stats[index], ContentVocabularies.NamedStatistics, statsPointer.AppendIndex(index),
                context, id, bag);
        }

        SemanticCheck.Distinct(
            stats, statsPointer, context, id, bag, "a utility's affected statistics");

        SemanticCheck.Integer(
            dto.StoredCharges, root.AppendProperty("stored_charges"), context, id, bag,
            "stored_charges");
        SemanticCheck.AtLeast(
            dto.StoredCharges, 1, root.AppendProperty("stored_charges"), context, id, bag,
            "stored_charges is at least one where it is present; a recharging utility with no "
                + "charge to store is not one");

        ValidateOreOnlyException(dto, outline, context, id, bag);
        ValidateRanks(dto, outline, context, id, bag);
        ValidateAvailability(dto, context, id, bag);
    }

    private static void ValidateOreOnlyException(
        UtilityDto dto,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        bool exception = dto.OreOnlyException ?? false;
        JsonPointer root = JsonPointer.Root;

        if (exception)
        {
            if (dto.MaterialId is not null)
            {
                SemanticCheck.ForbiddenBy(
                    root.AppendProperty("material_id"), context, id, bag,
                    "the ore-only radar exception is exactly the case of a utility with no "
                        + "assigned material; doc 40 § Utilities names the two as alternatives, "
                        + "so carrying both says the utility is and is not the exception");
            }

            if (outline.Contains(root.AppendProperty("ranks")))
            {
                SemanticCheck.ForbiddenBy(
                    root.AppendProperty("ranks"), context, id, bag,
                    "the ore-only radar has no rank ladder: it has one state, and its rank_count "
                        + "is zero. A rank array here would need a rank_count to match, and there "
                        + "is none to match");
            }

            if (dto.Acquisition?.MaterialUnitCost is not null)
            {
                SemanticCheck.ForbiddenBy(
                    root.AppendProperty("acquisition").AppendProperty("material_unit_cost"),
                    context, id, bag,
                    "the ore-only radar is bought with common ore, so it has no material unit "
                        + "cost");
            }

            if (dto.Acquisition?.CommonOreCost is null)
            {
                SemanticCheck.RequiredBy(
                    root.AppendProperty("acquisition").AppendProperty("common_ore_cost"),
                    context, id, bag,
                    "the ore-only radar states its common-ore cost, which is the whole of what "
                        + "'ore-only' names");
            }

            return;
        }

        if (dto.MaterialId is null)
        {
            SemanticCheck.RequiredBy(
                root.AppendProperty("material_id"), context, id, bag,
                "a utility that is not the ore-only exception is assigned to a material; doc 40 "
                    + "§ Utilities makes the two alternatives, so a utility with neither is "
                    + "neither kind");
        }
        else
        {
            SemanticCheck.ReferenceGrammar(
                dto.MaterialId, ContentCategory.Resource, root.AppendProperty("material_id"),
                context, id, bag);
        }

        if (dto.Acquisition?.CommonOreCost is not null)
        {
            SemanticCheck.ForbiddenBy(
                root.AppendProperty("acquisition").AppendProperty("common_ore_cost"), context, id,
                bag,
                "only the ore-only radar is bought with common ore; a material utility's "
                    + "fabrication cost is its material_unit_cost");
        }

        if (!outline.Contains(root.AppendProperty("ranks")))
        {
            SemanticCheck.RequiredBy(
                root.AppendProperty("ranks"), context, id, bag,
                "a material utility has a rank ladder: Installed plus its rank_count ranks");
        }
    }

    private static void ValidateRanks(
        UtilityDto dto,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer acquisition = JsonPointer.Root.AppendProperty("acquisition");
        long rankCount = SemanticCheck.Integer(
            dto.Acquisition?.RankCount, acquisition.AppendProperty("rank_count"), context, id, bag,
            "rank_count");
        SemanticCheck.AtLeast(
            dto.Acquisition?.RankCount, 0, acquisition.AppendProperty("rank_count"), context, id,
            bag,
            "rank_count is a count of ranks above Installed and is nonnegative; the ore-only "
                + "radar's is zero");
        SemanticCheck.Integer(
            dto.Acquisition?.UtilitySlotsFilled, acquisition.AppendProperty("utility_slots_filled"),
            context, id, bag, "utility_slots_filled");
        SemanticCheck.AtLeast(
            dto.Acquisition?.UtilitySlotsFilled, 1,
            acquisition.AppendProperty("utility_slots_filled"), context, id, bag,
            "a utility occupies at least one loadout slot");

        List<double> oreCosts = dto.Acquisition?.RankOreCosts ?? new List<double>();
        JsonPointer costsPointer = acquisition.AppendProperty("rank_ore_costs");
        if (dto.Acquisition?.RankOreCosts is not null)
        {
            SemanticCheck.ExactCount(
                oreCosts.Count, (int)rankCount, costsPointer, context, id, bag,
                "rank_ore_costs holds one price per rank above Installed, so its length equals "
                    + "rank_count; the two are checked against each other rather than both "
                    + "against a constant");

            for (int index = 0; index < oreCosts.Count; index++)
            {
                SemanticCheck.Integer(
                    oreCosts[index], costsPointer.AppendIndex(index), context, id, bag,
                    "a rank ore cost");
                SemanticCheck.AtLeast(
                    oreCosts[index], 1, costsPointer.AppendIndex(index), context, id, bag,
                    "a rank ore cost is at least one unit of common ore");
            }
        }

        if (!outline.Contains(JsonPointer.Root.AppendProperty("ranks")))
        {
            return;
        }

        List<UtilityDto.RankDto> ranks = dto.Ranks ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("ranks");

        SemanticCheck.ExactCount(
            ranks.Count, (int)rankCount + 1, pointer, context, id, bag,
            "a utility's rank array holds one row per rank plus one for Installed, so its length "
                + "is rank_count plus one. The invariant is conditioned on the ore-only exception "
                + "rather than applied blindly, because the radar has no ladder at all");

        List<long> ordinals = new(ranks.Count);
        for (int index = 0; index < ranks.Count; index++)
        {
            ordinals.Add(SemanticCheck.Integer(
                ranks[index].Rank, pointer.AppendIndex(index).AppendProperty("rank"), context, id,
                bag, "rank"));
        }

        SemanticCheck.Contiguous(
            ordinals, 0, pointer, context, id, bag,
            "a utility's ranks, where zero is Installed");
    }

    private static void ValidateAvailability(
        UtilityDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (dto.Availability is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("availability");
        SemanticCheck.Token(
            dto.Availability.PoolAvailability, UtilitySchema.PoolAvailabilities,
            pointer.AppendProperty("pool_availability"), context, id, bag);

        bool unlocked = string.Equals(
            dto.Availability.PoolAvailability, "hyper-gold-unlock", StringComparison.Ordinal);

        if (unlocked && dto.Availability.UnlockId is null)
        {
            SemanticCheck.RequiredBy(
                pointer.AppendProperty("unlock_id"), context, id, bag,
                "a utility behind an unlock names the unlock that grants it; the cost and the "
                    + "unlock's name are the unlock's own fields, and a copy here would be a "
                    + "second writer on them");
        }

        if (!unlocked && dto.Availability.UnlockId is not null)
        {
            SemanticCheck.ForbiddenBy(
                pointer.AppendProperty("unlock_id"), context, id, bag,
                "a utility that is not behind an unlock names none; the pool token and the unlock "
                    + "reference would otherwise be two answers to one question");
        }

        if (dto.Availability.UnlockId is not null)
        {
            SemanticCheck.ReferenceGrammar(
                dto.Availability.UnlockId, ContentCategory.Unlock,
                pointer.AppendProperty("unlock_id"), context, id, bag);
        }
    }
}
