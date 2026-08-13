using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-powerup</c>: the field table of one permanent PowerUp.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § PowerUps and option
/// unlocks: "PowerUps include rank cap, fixed costs/values by rank, active-rank policy,
/// refundable flag, named-stat contribution, and UI grouping. Validators recompute
/// total catalog costs and maximum-account envelope."
/// </para>
/// <para>
/// <b>A PowerUp is a kind, a per-rank value, a cap, and a price array.</b> The authored
/// shape had twenty-five fields, of which the per-rank effect, the maximum effect, and
/// every rank's running total were three encodings of the same contribution and two of
/// the three were arithmetic over the first. The maximum is the cap-th multiple; each
/// rank's total is its index times the per-rank value; the cumulative cost is a running
/// sum; the catalog total is a sum of sums. All of them reproduce exactly, which is
/// what makes them derived rather than merely redundant.
/// </para>
/// </remarks>
public static class PowerUpSchema
{
    /// <summary>What the whole PowerUp catalog costs to buy out, in Hyper Gold.</summary>
    /// <remarks>
    /// <c>docs/62-permanent-powerup-catalog.md</c> § Fully upgraded account envelope.
    /// Recomputed by summing every rank price in every definition, so the number is
    /// checked against the parts rather than against an authored copy of itself.
    /// </remarks>
    public const int CatalogTotalHyperGold = 9450;

    /// <summary>The UI grouping a PowerUp appears under.</summary>
    public static ClosedVocabulary UiGroupings { get; } = new(
        "a PowerUp UI grouping",
        "GDD-POWERUP-CATALOG",
        "combat",
        "survivability",
        "mobility",
        "mining-economy");

    /// <summary>How a PowerUp's per-rank value contributes to the statistic it names.</summary>
    /// <remarks>
    /// A closed five-way union, one arm per shape of contribution, replacing five
    /// parallel optional value keys of which exactly one was ever present. The unit is
    /// carried by the token, which is why there is one <c>per_rank_value</c> and not
    /// five differently-named numbers.
    /// </remarks>
    public static ClosedVocabulary EffectKinds { get; } = new(
        "a PowerUp effect kind",
        "GDD-POWERUP-CATALOG",
        "additive-percent",
        "additive-hull",
        "additive-armor",
        "additive-hull-per-second",
        "revival-charge");

    /// <summary>One purchasable rank.</summary>
    public static DefinitionShape Rank { get; } = DefinitionShape.Of(
        "a PowerUp rank",
        DefinitionField.Integer("rank"),
        DefinitionField.Integer("price_hyper_gold"));

    /// <summary>The active-rank policy sub-shape.</summary>
    public static DefinitionShape ActiveRankPolicy { get; } = DefinitionShape.Of(
        "the active-rank policy",
        DefinitionField.Integer("minimum_active_rank"),
        DefinitionField.Flag("maximum_active_rank_equals_purchased_rank"),
        DefinitionField.Flag("changeable_between_runs"),
        DefinitionField.Flag("changeable_during_run"),
        DefinitionField.Integer("change_cost_hyper_gold"));

    /// <summary>The revival parameters, on the revival-charge kind only.</summary>
    public static DefinitionShape Revival { get; } = DefinitionShape.Of(
        "the revival parameters",
        DefinitionField.Flag("automatic"),
        DefinitionField.Text("displacement"),
        DefinitionField.Number("hull_restored_fraction_of_current_maximum"),
        DefinitionField.Number("invulnerability_active_simulation_seconds"),
        DefinitionField.Flag("pauses_timer_or_simulation"),
        DefinitionField.Flag("rechargeable_during_run"));

    /// <summary>The PowerUp field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a PowerUp definition",
        DefinitionField.Text("ui_grouping"),
        DefinitionField.Text("affected_statistic"),
        DefinitionField.Text("effect_kind"),
        DefinitionField.Number("per_rank_value"),
        DefinitionField.Integer("cap"),
        DefinitionField.ArrayOf("ranks", DefinitionField.ElementObject(Rank)),
        DefinitionField.OptionalObject("revival", Revival),
        DefinitionField.Object("active_rank_policy", ActiveRankPolicy),
        DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The values the compiler derives for a PowerUp.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "maximum_effect",
            "the per-rank value multiplied by the cap",
            "/per_rank_value", "/cap"),
        DerivedField.At(
            "total_cost_hyper_gold",
            "the sum of every rank's price",
            "/ranks"),
        DerivedField.At(
            "rank_effect_column",
            "a table column header rendered from affected_statistic",
            "/affected_statistic"),
        DerivedField.At(
            "domain",
            "a second name for ui_grouping, identical on every definition",
            "/ui_grouping"),
        DerivedField.Nested(
            new[] { "ranks", "cumulative_cost_hyper_gold" },
            "the running sum of the rank prices up to and including this rank",
            "/ranks"),
        DerivedField.Nested(
            new[] { "ranks", "total_effect" },
            "the rank number multiplied by the per-rank value",
            "/per_rank_value", "/ranks"),
    });
}

/// <summary>One validated permanent PowerUp definition.</summary>
public sealed class PowerUpDefinition : ContentDefinition
{
    internal PowerUpDefinition(
        DefinitionEnvelope envelope,
        string uiGrouping,
        string affectedStatistic,
        string effectKind,
        double perRankValue,
        long cap,
        IReadOnlyList<PowerUpRank> ranks)
        : base(envelope, DefinitionKind.PowerUp)
    {
        UiGrouping = uiGrouping;
        AffectedStatistic = affectedStatistic;
        EffectKind = effectKind;
        PerRankValue = perRankValue;
        Cap = cap;
        Ranks = ranks;
    }

    /// <summary>Which shop group this PowerUp appears under.</summary>
    public string UiGrouping { get; }

    /// <summary>The named statistic it contributes to.</summary>
    public string AffectedStatistic { get; }

    /// <summary>How the per-rank value contributes.</summary>
    public string EffectKind { get; }

    /// <summary>What one rank adds, in the unit the kind implies.</summary>
    public double PerRankValue { get; }

    /// <summary>The highest purchasable rank.</summary>
    public long Cap { get; }

    /// <summary>The rank ladder, one row per rank from one to the cap.</summary>
    public IReadOnlyList<PowerUpRank> Ranks { get; }

    /// <summary>The total this PowerUp costs to buy out, recomputed from its ranks.</summary>
    public long TotalCostHyperGold
    {
        get
        {
            long total = 0;
            foreach (PowerUpRank rank in Ranks)
            {
                total += rank.PriceHyperGold;
            }

            return total;
        }
    }

    /// <summary>One purchasable rank.</summary>
    public sealed class PowerUpRank
    {
        internal PowerUpRank(long rank, long priceHyperGold)
        {
            Rank = rank;
            PriceHyperGold = priceHyperGold;
        }

        /// <summary>The one-based rank.</summary>
        public long Rank { get; }

        /// <summary>What this rank costs.</summary>
        public long PriceHyperGold { get; }
    }
}

/// <summary>The wire shape of a PowerUp definition's domain fields.</summary>
internal sealed class PowerUpDto
{
    [JsonPropertyName("ui_grouping")]
    public string? UiGrouping { get; set; }

    [JsonPropertyName("affected_statistic")]
    public string? AffectedStatistic { get; set; }

    [JsonPropertyName("effect_kind")]
    public string? EffectKind { get; set; }

    [JsonPropertyName("per_rank_value")]
    public double? PerRankValue { get; set; }

    [JsonPropertyName("cap")]
    public double? Cap { get; set; }

    [JsonPropertyName("ranks")]
    public List<RankDto>? Ranks { get; set; }

    [JsonPropertyName("active_rank_policy")]
    public ActiveRankPolicyDto? ActiveRankPolicy { get; set; }

    internal sealed class RankDto
    {
        [JsonPropertyName("rank")]
        public double? Rank { get; set; }

        [JsonPropertyName("price_hyper_gold")]
        public double? PriceHyperGold { get; set; }
    }

    internal sealed class ActiveRankPolicyDto
    {
        [JsonPropertyName("minimum_active_rank")]
        public double? MinimumActiveRank { get; set; }

        [JsonPropertyName("change_cost_hyper_gold")]
        public double? ChangeCostHyperGold { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="PowerUpDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(PowerUpDto))]
internal sealed partial class PowerUpJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one permanent PowerUp definition.</summary>
public static class PowerUpReader
{
    /// <summary>Reads one PowerUp.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public static DefinitionReadResult Read(ReadOnlySpan<byte> utf8, CategoryReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DiagnosticBag bag = new();
        if (!CategoryPrelude.Run(
                utf8, context, bag, out DefinitionEnvelope? envelope, out string? id,
                out DocumentOutline _, out JsonStructure structure))
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        PowerUpDto? dto = JsonSerializer.Deserialize(utf8, PowerUpJsonContext.Default.PowerUpDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        Validate(dto, context, id, StructuralReport.Of(bag), bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        List<PowerUpDefinition.PowerUpRank> ranks = new();
        foreach (PowerUpDto.RankDto rank in dto.Ranks!)
        {
            ranks.Add(new PowerUpDefinition.PowerUpRank(
                (long)rank.Rank!.Value, (long)rank.PriceHyperGold!.Value));
        }

        PowerUpDefinition definition = new(
            envelope,
            dto.UiGrouping!,
            dto.AffectedStatistic!,
            dto.EffectKind!,
            dto.PerRankValue!.Value,
            (long)dto.Cap!.Value,
            new ReadOnlyCollection<PowerUpDefinition.PowerUpRank>(ranks));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        PowerUpDto dto,
        CategoryReadContext context,
        string? id,
        StructuralReport structural,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.Token(
            dto.UiGrouping, PowerUpSchema.UiGroupings, root.AppendProperty("ui_grouping"), context,
            id, bag);
        SemanticCheck.Token(
            dto.AffectedStatistic, ContentVocabularies.NamedStatistics,
            root.AppendProperty("affected_statistic"), context, id, bag);
        SemanticCheck.Token(
            dto.EffectKind, PowerUpSchema.EffectKinds, root.AppendProperty("effect_kind"), context,
            id, bag);

        SemanticCheck.GreaterThan(
            dto.PerRankValue, 0, root.AppendProperty("per_rank_value"), context, id, bag,
            "per_rank_value is what one rank adds and is positive; a permanent PowerUp is an "
                + "always-on improvement, so a rank that took something away would be a different "
                + "concept");

        long cap = SemanticCheck.Integer(
            dto.Cap, root.AppendProperty("cap"), context, id, bag, "cap");
        SemanticCheck.AtLeast(
            dto.Cap, 1, root.AppendProperty("cap"), context, id, bag,
            "cap is the highest purchasable rank and is at least one");

        ValidateRanks(dto, cap, context, id, structural, bag);
        ValidateActiveRankPolicy(dto, cap, context, id, bag);
    }

    private static void ValidateRanks(
        PowerUpDto dto,
        long cap,
        CategoryReadContext context,
        string? id,
        StructuralReport structural,
        DiagnosticBag bag)
    {
        List<PowerUpDto.RankDto> ranks = dto.Ranks ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("ranks");
        JsonPointer capPointer = JsonPointer.Root.AppendProperty("cap");

        // The length is compared against the cap, so both are operands. Without the
        // guard a renamed cap defaults to zero and this reports "holds exactly 0
        // elements; found 5" over a file with five visible rows.
        if (!structural.ReportedEither(pointer, capPointer))
        {
            SemanticCheck.ExactCount(
                ranks.Count, (int)cap, pointer, context, id, bag,
                "the rank ladder holds one row per purchasable rank, so its length equals the "
                    + "cap. The two are checked against each other rather than both against a "
                    + "constant, so a cap raised without adding a row fails and so does the "
                    + "reverse");
        }

        List<long> ordinals = new(ranks.Count);
        List<JsonPointer> ordinalPointers = new(ranks.Count);
        for (int index = 0; index < ranks.Count; index++)
        {
            JsonPointer row = pointer.AppendIndex(index);
            JsonPointer ordinalPointer = row.AppendProperty("rank");
            ordinalPointers.Add(ordinalPointer);
            ordinals.Add(SemanticCheck.Integer(
                ranks[index].Rank, ordinalPointer, context, id, bag, "rank"));

            SemanticCheck.Integer(
                ranks[index].PriceHyperGold, row.AppendProperty("price_hyper_gold"), context, id,
                bag, "price_hyper_gold");
            SemanticCheck.AtLeast(
                ranks[index].PriceHyperGold, 1, row.AppendProperty("price_hyper_gold"), context,
                id, bag,
                "price_hyper_gold is at least one; prices are not required to increase with rank, "
                    + "because no accepted document says they must and asserting it would reject "
                    + "a flat ladder a designer is free to author");
        }

        // Every ordinal is an operand: one absent rank number defaults to zero and the
        // contiguity report then names an ordinal nobody wrote.
        if (!structural.ReportedAny(ordinalPointers))
        {
            SemanticCheck.Contiguous(
                ordinals, 1, pointer, context, id, bag, "a PowerUp's rank numbers");
        }
    }

    private static void ValidateActiveRankPolicy(
        PowerUpDto dto,
        long cap,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (dto.ActiveRankPolicy is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("active_rank_policy");

        SemanticCheck.Integer(
            dto.ActiveRankPolicy.MinimumActiveRank, pointer.AppendProperty("minimum_active_rank"),
            context, id, bag, "minimum_active_rank");
        SemanticCheck.Within(
            dto.ActiveRankPolicy.MinimumActiveRank, 0, cap,
            pointer.AppendProperty("minimum_active_rank"), context, id, bag,
            "minimum_active_rank is a rank this PowerUp has, so it lies between zero - meaning "
                + "the PowerUp may be switched off entirely - and the cap. A minimum above the "
                + "cap would make no purchased state satisfy it");

        SemanticCheck.Integer(
            dto.ActiveRankPolicy.ChangeCostHyperGold,
            pointer.AppendProperty("change_cost_hyper_gold"), context, id, bag,
            "change_cost_hyper_gold");
        SemanticCheck.AtLeast(
            dto.ActiveRankPolicy.ChangeCostHyperGold, 0,
            pointer.AppendProperty("change_cost_hyper_gold"), context, id, bag,
            "change_cost_hyper_gold is nonnegative; free is zero and is stated rather than "
                + "omitted, because the economy report has a column for it");
    }
}
