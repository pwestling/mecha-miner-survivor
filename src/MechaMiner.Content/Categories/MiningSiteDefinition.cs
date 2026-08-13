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

/// <summary>One validated mining-site class definition.</summary>
public sealed class MiningSiteDefinition : ContentDefinition
{
    internal MiningSiteDefinition(
        DefinitionEnvelope envelope,
        string siteClass,
        long countPerStandardMap,
        double extractionZoneRadiusMetres,
        double extractionDurationSeconds,
        double? resonanceFieldRadiusMetres,
        IReadOnlyList<string> eligibleMaterialIds)
        : base(envelope, DefinitionKind.MiningSite)
    {
        SiteClass = siteClass;
        CountPerStandardMap = countPerStandardMap;
        ExtractionZoneRadiusMetres = extractionZoneRadiusMetres;
        ExtractionDurationSeconds = extractionDurationSeconds;
        ResonanceFieldRadiusMetres = resonanceFieldRadiusMetres;
        EligibleMaterialIds = eligibleMaterialIds;
    }

    /// <summary>Which of the four accepted classes this site is.</summary>
    public string SiteClass { get; }

    /// <summary>How many of this class a standard map holds.</summary>
    public long CountPerStandardMap { get; }

    /// <summary>
    /// The base extraction circle, in mech collision diameters. The left operand of
    /// both relational constraints this package declares.
    /// </summary>
    public double ExtractionZoneRadiusMetres { get; }

    /// <summary>Uninterrupted seconds to complete one extraction.</summary>
    public double ExtractionDurationSeconds { get; }

    /// <summary>The resonance field's radius, on a geode only.</summary>
    public double? ResonanceFieldRadiusMetres { get; }

    /// <summary>The materials this site can yield, by resource ID.</summary>
    public IReadOnlyList<string> EligibleMaterialIds { get; }
}

/// <summary>The wire shape of a mining-site definition's domain fields.</summary>
internal sealed class MiningSiteDto
{
    [JsonPropertyName("site_class")]
    public string? SiteClass { get; set; }

    [JsonPropertyName("count_per_standard_map")]
    public double? CountPerStandardMap { get; set; }

    [JsonPropertyName("extraction_zone_radius_m")]
    public double? ExtractionZoneRadiusMetres { get; set; }

    [JsonPropertyName("extraction_duration_seconds")]
    public double? ExtractionDurationSeconds { get; set; }

    [JsonPropertyName("installment_count")]
    public double? InstallmentCount { get; set; }

    [JsonPropertyName("installment_duration_seconds")]
    public double? InstallmentDurationSeconds { get; set; }

    [JsonPropertyName("payout_per_installment")]
    public PayoutDto? PayoutPerInstallment { get; set; }

    [JsonPropertyName("completion_payout")]
    public List<PayoutDto>? CompletionPayout { get; set; }

    [JsonPropertyName("progress_decay")]
    public ProgressDecayDto? ProgressDecay { get; set; }

    [JsonPropertyName("resonance_field")]
    public ResonanceFieldDto? ResonanceField { get; set; }

    [JsonPropertyName("beacon_thresholds")]
    public List<BeaconThresholdDto>? BeaconThresholds { get; set; }

    [JsonPropertyName("depleted_state_kind")]
    public string? DepletedStateKind { get; set; }

    [JsonPropertyName("persistence_class")]
    public string? PersistenceClass { get; set; }

    [JsonPropertyName("eligible_material_ids")]
    public List<string>? EligibleMaterialIds { get; set; }

    [JsonPropertyName("present_materials_per_run")]
    public double? PresentMaterialsPerRun { get; set; }

    [JsonPropertyName("material_units_per_geode")]
    public double? MaterialUnitsPerGeode { get; set; }

    [JsonPropertyName("geodes_per_present_material")]
    public IntegerRangeDto? GeodesPerPresentMaterial { get; set; }

    internal sealed class PayoutDto
    {
        [JsonPropertyName("amount")]
        public double? Amount { get; set; }

        [JsonPropertyName("resource_id")]
        public string? ResourceId { get; set; }
    }

    internal sealed class ProgressDecayDto
    {
        [JsonPropertyName("decay_rate_multiplier_of_forward_rate")]
        public double? DecayRateMultiplierOfForwardRate { get; set; }

        [JsonPropertyName("grace_seconds")]
        public double? GraceSeconds { get; set; }
    }

    internal sealed class ResonanceFieldDto
    {
        [JsonPropertyName("radius_m")]
        public double? RadiusMetres { get; set; }

        [JsonPropertyName("applies_to")]
        public List<string>? AppliesTo { get; set; }
    }

    internal sealed class BeaconThresholdDto
    {
        [JsonPropertyName("trigger_kind")]
        public string? TriggerKind { get; set; }

        [JsonPropertyName("trigger_progress_percent")]
        public double? TriggerProgressPercent { get; set; }
    }

    internal sealed class IntegerRangeDto
    {
        [JsonPropertyName("min")]
        public double? Minimum { get; set; }

        [JsonPropertyName("max")]
        public double? Maximum { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="MiningSiteDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(MiningSiteDto))]
internal sealed partial class MiningSiteJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one mining-site class definition.</summary>
public static class MiningSiteReader
{
    /// <summary>The site class that carries a resonance field.</summary>
    public const string GeodeClass = "specialized-material-geode";

    /// <summary>The site class that carries a threat beacon.</summary>
    public const string HyperGoldClass = "hyper-gold-site";

    /// <summary>Reads one mining-site class.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public static DefinitionReadResult Read(ReadOnlySpan<byte> utf8, CategoryReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DiagnosticBag bag = new();
        if (!CategoryPrelude.Run(
                utf8, context, bag, out DefinitionEnvelope? envelope, out string? id,
                out DocumentOutline outline, out JsonStructure structure))
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        MiningSiteDto? dto = JsonSerializer.Deserialize(
            utf8, MiningSiteJsonContext.Default.MiningSiteDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        Validate(dto, outline, context, id, bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        MiningSiteDefinition definition = new(
            envelope,
            dto.SiteClass!,
            (long)dto.CountPerStandardMap!.Value,
            dto.ExtractionZoneRadiusMetres!.Value,
            dto.ExtractionDurationSeconds!.Value,
            dto.ResonanceField?.RadiusMetres,
            dto.EligibleMaterialIds is null
                ? Array.Empty<string>()
                : new ReadOnlyCollection<string>(new List<string>(dto.EligibleMaterialIds)));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        MiningSiteDto dto,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.Token(
            dto.SiteClass, ContentVocabularies.SiteClasses, root.AppendProperty("site_class"),
            context, id, bag);
        SemanticCheck.Token(
            dto.DepletedStateKind, MiningSiteSchema.DepletedStateKinds,
            root.AppendProperty("depleted_state_kind"), context, id, bag);
        SemanticCheck.Token(
            dto.PersistenceClass, ResourceSchema.PersistenceClasses,
            root.AppendProperty("persistence_class"), context, id, bag);

        SemanticCheck.Integer(
            dto.CountPerStandardMap, root.AppendProperty("count_per_standard_map"), context, id,
            bag, "count_per_standard_map");
        SemanticCheck.AtLeast(
            dto.CountPerStandardMap, 1, root.AppendProperty("count_per_standard_map"), context, id,
            bag,
            "count_per_standard_map is at least one; a class that appears zero times on a "
                + "standard map is not one of the four the mode accepts");

        SemanticCheck.GreaterThan(
            dto.ExtractionZoneRadiusMetres, 0, root.AppendProperty("extraction_zone_radius_m"),
            context, id, bag,
            "extraction_zone_radius_m is a radius in mech collision diameters and is positive; it "
                + "is the operand of both cross-definition relations this package declares, so a "
                + "zero here would make both of them vacuous rather than merely wrong");
        SemanticCheck.GreaterThan(
            dto.ExtractionDurationSeconds, 0, root.AppendProperty("extraction_duration_seconds"),
            context, id, bag,
            "extraction_duration_seconds is the uninterrupted work a completion takes and is "
                + "positive");

        ValidateInstallments(dto, context, id, bag);
        ValidatePayouts(dto, context, id, bag);
        ValidateDecay(dto, context, id, bag);
        ValidateResonanceField(dto, outline, context, id, bag);
        ValidateBeaconThresholds(dto, outline, context, id, bag);
        ValidateGeodeFields(dto, context, id, bag);
    }

    private static void ValidateInstallments(
        MiningSiteDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        SemanticCheck.Integer(
            dto.InstallmentCount, JsonPointer.Root.AppendProperty("installment_count"), context,
            id, bag, "installment_count");
        SemanticCheck.AtLeast(
            dto.InstallmentCount, 1, JsonPointer.Root.AppendProperty("installment_count"), context,
            id, bag,
            "installment_count is at least one where it is present; a site that pays in no "
                + "installments omits the field and pays on completion");
        SemanticCheck.GreaterThan(
            dto.InstallmentDurationSeconds, 0,
            JsonPointer.Root.AppendProperty("installment_duration_seconds"), context, id, bag,
            "installment_duration_seconds is a positive duration; the compiler multiplies it by "
                + "the installment count to derive the total depletion time");
    }

    private static void ValidatePayouts(
        MiningSiteDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (dto.PayoutPerInstallment is not null)
        {
            ValidatePayout(
                dto.PayoutPerInstallment, JsonPointer.Root.AppendProperty("payout_per_installment"),
                context, id, bag);
        }

        List<MiningSiteDto.PayoutDto> completion = dto.CompletionPayout ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("completion_payout");
        for (int index = 0; index < completion.Count; index++)
        {
            ValidatePayout(completion[index], pointer.AppendIndex(index), context, id, bag);
        }
    }

    private static void ValidatePayout(
        MiningSiteDto.PayoutDto payout,
        JsonPointer pointer,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        SemanticCheck.ReferenceGrammar(
            payout.ResourceId, ContentCategory.Resource, pointer.AppendProperty("resource_id"),
            context, id, bag);
        SemanticCheck.Integer(
            payout.Amount, pointer.AppendProperty("amount"), context, id, bag, "amount");
        SemanticCheck.AtLeast(
            payout.Amount, 1, pointer.AppendProperty("amount"), context, id, bag,
            "a payout's amount is at least one unit; a payout of nothing is the absence of the "
                + "payout entry");
    }

    private static void ValidateDecay(
        MiningSiteDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (dto.ProgressDecay is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("progress_decay");
        SemanticCheck.GreaterThan(
            dto.ProgressDecay.DecayRateMultiplierOfForwardRate, 0,
            pointer.AppendProperty("decay_rate_multiplier_of_forward_rate"), context, id, bag,
            "decay_rate_multiplier_of_forward_rate scales the site's own forward extraction rate "
                + "and is positive; a nonpositive multiplier would make progress decay upward");
        SemanticCheck.AtLeast(
            dto.ProgressDecay.GraceSeconds, 0, pointer.AppendProperty("grace_seconds"), context,
            id, bag,
            "grace_seconds is a duration and durations are nonnegative");
    }

    private static void ValidateResonanceField(
        MiningSiteDto dto,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty("resonance_field");
        bool expected = string.Equals(dto.SiteClass, GeodeClass, StringComparison.Ordinal);
        bool present = outline.Contains(pointer);

        if (!present)
        {
            if (expected)
            {
                SemanticCheck.RequiredBy(
                    pointer, context, id, bag,
                    "a specialized-material geode projects a resonance field, so the field and "
                        + "its radius are required rather than omitted. The radius is the left "
                        + "side of RC-01, and a geode without one would make that relation "
                        + "unevaluable rather than failing");
            }

            return;
        }

        if (!expected)
        {
            SemanticCheck.ForbiddenBy(
                pointer, context, id, bag,
                "only a specialized-material geode projects a resonance field; an ore seam and a "
                    + "Hyper Gold site have none");
            return;
        }

        if (dto.ResonanceField is null)
        {
            return;
        }

        SemanticCheck.GreaterThan(
            dto.ResonanceField.RadiusMetres, 0, pointer.AppendProperty("radius_m"), context, id,
            bag,
            "resonance_field.radius_m is a radius in mech collision diameters and is positive. "
                + "That it exceeds the maximum expanded extraction zone is RC-01, a relation "
                + "across definitions, and is not asserted here: this bound and that relation "
                + "are different claims and only one of them can be checked from one file");

        List<string> targets = dto.ResonanceField.AppliesTo ?? new List<string>();
        JsonPointer targetsPointer = pointer.AppendProperty("applies_to");
        for (int index = 0; index < targets.Count; index++)
        {
            SemanticCheck.Token(
                targets[index], MiningSiteSchema.ResonanceTargets, targetsPointer.AppendIndex(index),
                context, id, bag);
        }

        SemanticCheck.Distinct(
            targets, targetsPointer, context, id, bag, "a resonance field's targets");
    }

    private static void ValidateBeaconThresholds(
        MiningSiteDto dto,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty("beacon_thresholds");
        bool expected = string.Equals(dto.SiteClass, HyperGoldClass, StringComparison.Ordinal);
        bool present = outline.Contains(pointer);

        if (present && !expected)
        {
            SemanticCheck.ForbiddenBy(
                pointer, context, id, bag,
                "only a Hyper Gold site carries a threat beacon; the schedule's beacon response "
                    + "table is keyed by the same four triggers and is the single writer on what "
                    + "each response is");
            return;
        }

        if (!present && expected)
        {
            SemanticCheck.RequiredBy(
                pointer, context, id, bag,
                "a Hyper Gold site carries the progress thresholds at which its beacon fires");
            return;
        }

        List<MiningSiteDto.BeaconThresholdDto> thresholds = dto.BeaconThresholds ?? new();
        for (int index = 0; index < thresholds.Count; index++)
        {
            MiningSiteDto.BeaconThresholdDto threshold = thresholds[index];
            JsonPointer row = pointer.AppendIndex(index);

            SemanticCheck.Token(
                threshold.TriggerKind, MiningSiteSchema.BeaconTriggerKinds,
                row.AppendProperty("trigger_kind"), context, id, bag);

            bool isProgress = string.Equals(
                threshold.TriggerKind, "progress-threshold", StringComparison.Ordinal);

            if (isProgress && threshold.TriggerProgressPercent is null)
            {
                SemanticCheck.RequiredBy(
                    row.AppendProperty("trigger_progress_percent"), context, id, bag,
                    "a progress-threshold trigger states the progress it fires at; without it the "
                        + "row would be indistinguishable from the activation row");
            }

            if (!isProgress && threshold.TriggerProgressPercent is not null)
            {
                SemanticCheck.ForbiddenBy(
                    row.AppendProperty("trigger_progress_percent"), context, id, bag,
                    "an activation trigger fires at the first progress of any amount, so it has "
                        + "no threshold; a percentage here would be a second, contradictory "
                        + "statement of when it fires");
            }

            SemanticCheck.Integer(
                threshold.TriggerProgressPercent, row.AppendProperty("trigger_progress_percent"),
                context, id, bag, "trigger_progress_percent");
            SemanticCheck.Within(
                threshold.TriggerProgressPercent, 1, 99,
                row.AppendProperty("trigger_progress_percent"), context, id, bag,
                "trigger_progress_percent is a crossing strictly inside the extraction, so it "
                    + "lies between one and ninety-nine percentage points; zero is the activation "
                    + "trigger and one hundred is completion");
        }
    }

    private static void ValidateGeodeFields(
        MiningSiteDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        List<string> materials = dto.EligibleMaterialIds ?? new List<string>();
        JsonPointer materialsPointer = root.AppendProperty("eligible_material_ids");
        for (int index = 0; index < materials.Count; index++)
        {
            SemanticCheck.ReferenceGrammar(
                materials[index], ContentCategory.Resource, materialsPointer.AppendIndex(index),
                context, id, bag);
        }

        SemanticCheck.Distinct(
            materials, materialsPointer, context, id, bag, "a site's eligible material IDs");

        SemanticCheck.Integer(
            dto.PresentMaterialsPerRun, root.AppendProperty("present_materials_per_run"), context,
            id, bag, "present_materials_per_run");
        SemanticCheck.AtLeast(
            dto.PresentMaterialsPerRun, 1, root.AppendProperty("present_materials_per_run"),
            context, id, bag,
            "present_materials_per_run is the size of the run's geological profile and is at "
                + "least one");

        SemanticCheck.Integer(
            dto.MaterialUnitsPerGeode, root.AppendProperty("material_units_per_geode"), context,
            id, bag, "material_units_per_geode");
        SemanticCheck.AtLeast(
            dto.MaterialUnitsPerGeode, 1, root.AppendProperty("material_units_per_geode"), context,
            id, bag,
            "material_units_per_geode is at least one unit");

        if (dto.GeodesPerPresentMaterial is null)
        {
            return;
        }

        JsonPointer range = root.AppendProperty("geodes_per_present_material");
        SemanticCheck.Integer(
            dto.GeodesPerPresentMaterial.Minimum, range.AppendProperty("min"), context, id, bag,
            "min");
        SemanticCheck.Integer(
            dto.GeodesPerPresentMaterial.Maximum, range.AppendProperty("max"), context, id, bag,
            "max");
        SemanticCheck.FeasibleRange(
            dto.GeodesPerPresentMaterial.Minimum, dto.GeodesPerPresentMaterial.Maximum, null,
            range, context, id, bag, "geodes_per_present_material");
    }
}
