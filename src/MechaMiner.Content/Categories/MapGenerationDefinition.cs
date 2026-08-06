using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Categories;

/// <summary>The validated standard map generation contract.</summary>
public sealed class MapGenerationDefinition : ContentDefinition
{
    internal MapGenerationDefinition(
        DefinitionEnvelope envelope,
        string mode,
        double obstacleFreeRadiusInMiningZoneDiameters)
        : base(envelope, DefinitionKind.MapGenerationContract)
    {
        Mode = mode;
        ObstacleFreeRadiusInMiningZoneDiameters = obstacleFreeRadiusInMiningZoneDiameters;
    }

    /// <summary>The run mode this contract generates for.</summary>
    public string Mode { get; }

    /// <summary>
    /// The deployment clearance, in mining-zone diameters. The left operand of RC-02.
    /// </summary>
    /// <remarks>
    /// The relative unit is deliberate: the clearance tracks the mining zone, so
    /// changing the zone changes the clearance without anyone editing this file. The
    /// absolute value in mech collision diameters is derived from it.
    /// </remarks>
    public double ObstacleFreeRadiusInMiningZoneDiameters { get; }
}

/// <summary>The wire shape of the map contract's domain fields.</summary>
internal sealed class MapGenerationDto
{
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("distance_bands")]
    public List<DistanceBandDto>? DistanceBands { get; set; }

    [JsonPropertyName("world_scale")]
    public WorldScaleDto? WorldScale { get; set; }

    [JsonPropertyName("deployment_and_opening_fairness")]
    public DeploymentFairnessDto? DeploymentAndOpeningFairness { get; set; }

    [JsonPropertyName("visible_mining_opportunities_in_normal_view")]
    public VisibleOpportunitiesDto? VisibleMiningOpportunitiesInNormalView { get; set; }

    internal sealed class DistanceBandDto
    {
        [JsonPropertyName("band")]
        public string? Band { get; set; }

        [JsonPropertyName("route_distance_m")]
        public HalfOpenMetreBandDto? RouteDistanceMetres { get; set; }

        [JsonPropertyName("base_travel_time_from_deployment")]
        public HalfOpenSecondBandDto? BaseTravelTimeFromDeployment { get; set; }
    }

    internal sealed class HalfOpenMetreBandDto
    {
        [JsonPropertyName("min_exclusive")]
        public double? MinimumExclusive { get; set; }

        [JsonPropertyName("max_inclusive")]
        public double? MaximumInclusive { get; set; }
    }

    internal sealed class HalfOpenSecondBandDto
    {
        [JsonPropertyName("min_seconds_exclusive")]
        public double? MinimumSecondsExclusive { get; set; }

        [JsonPropertyName("max_seconds_inclusive")]
        public double? MaximumSecondsInclusive { get; set; }
    }

    internal sealed class WorldScaleDto
    {
        [JsonPropertyName("major_region_count")]
        public RegionCountDto? MajorRegionCount { get; set; }

        [JsonPropertyName("traversable_diameter_m")]
        public TargetRangeDto? TraversableDiameterMetres { get; set; }

        [JsonPropertyName("traversable_diameter_base_travel_seconds")]
        public TargetRangeDto? TraversableDiameterBaseTravelSeconds { get; set; }
    }

    internal sealed class RegionCountDto
    {
        [JsonPropertyName("min")]
        public double? Minimum { get; set; }

        [JsonPropertyName("max")]
        public double? Maximum { get; set; }

        [JsonPropertyName("initial_target")]
        public double? InitialTarget { get; set; }
    }

    internal sealed class TargetRangeDto
    {
        [JsonPropertyName("min")]
        public double? Minimum { get; set; }

        [JsonPropertyName("max")]
        public double? Maximum { get; set; }

        [JsonPropertyName("target")]
        public double? Target { get; set; }
    }

    internal sealed class DeploymentFairnessDto
    {
        [JsonPropertyName("obstacle_free_radius_in_mining_zone_diameters")]
        public double? ObstacleFreeRadiusInMiningZoneDiameters { get; set; }

        [JsonPropertyName("nearest_standard_ore_seam_base_travel_seconds")]
        public IntegerRangeDto? NearestStandardOreSeamBaseTravelSeconds { get; set; }
    }

    internal sealed class IntegerRangeDto
    {
        [JsonPropertyName("min")]
        public double? Minimum { get; set; }

        [JsonPropertyName("max")]
        public double? Maximum { get; set; }
    }

    internal sealed class VisibleOpportunitiesDto
    {
        [JsonPropertyName("target_minimum")]
        public double? TargetMinimum { get; set; }

        [JsonPropertyName("target_maximum")]
        public double? TargetMaximum { get; set; }

        [JsonPropertyName("hard_maximum")]
        public double? HardMaximum { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="MapGenerationDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(MapGenerationDto))]
internal sealed partial class MapGenerationJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates the standard map generation contract.</summary>
/// <remarks>
/// Doc 40 § Map generation: "Semantic validation checks internal feasibility before
/// sampling maps." Every check below is that: a bound ordering or a target inside its
/// band, decided from this one file. Whether the contract is feasible <em>against other
/// definitions</em> - the deployment clearance against a mining point's cleared radius -
/// is RC-02, and runs after the whole catalog is loaded.
/// </remarks>
public static class MapGenerationReader
{
    /// <summary>Reads the map generation contract.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public static DefinitionReadResult Read(ReadOnlySpan<byte> utf8, CategoryReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DiagnosticBag bag = new();
        if (!CategoryPrelude.Run(
                utf8, context, bag, out DefinitionEnvelope? envelope,
                out DocumentOutline _, out JsonStructure structure))
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        MapGenerationDto? dto = JsonSerializer.Deserialize(
            utf8, MapGenerationJsonContext.Default.MapGenerationDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        string? id = envelope?.Id.Value;
        Validate(dto, context, id, bag);

        if (bag.HasErrors || envelope is null || dto.DeploymentAndOpeningFairness is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        MapGenerationDefinition definition = new(
            envelope,
            dto.Mode!,
            dto.DeploymentAndOpeningFairness.ObstacleFreeRadiusInMiningZoneDiameters!.Value);

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        MapGenerationDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.Token(
            dto.Mode, EncounterScheduleSchema.Modes, root.AppendProperty("mode"), context, id, bag);

        ValidateWorldScale(dto.WorldScale, context, id, bag);
        ValidateDistanceBands(dto.DistanceBands, context, id, bag);
        ValidateDeployment(dto.DeploymentAndOpeningFairness, context, id, bag);
        ValidateVisibleOpportunities(
            dto.VisibleMiningOpportunitiesInNormalView, context, id, bag);
    }

    private static void ValidateWorldScale(
        MapGenerationDto.WorldScaleDto? scale,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (scale is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("world_scale");

        if (scale.MajorRegionCount is not null)
        {
            SemanticCheck.FeasibleRange(
                scale.MajorRegionCount.Minimum,
                scale.MajorRegionCount.Maximum,
                scale.MajorRegionCount.InitialTarget,
                pointer.AppendProperty("major_region_count"), context, id, bag,
                "world_scale.major_region_count");
            SemanticCheck.AtLeast(
                scale.MajorRegionCount.Minimum, 1,
                pointer.AppendProperty("major_region_count").AppendProperty("min"), context, id,
                bag,
                "a map has at least one major region");
        }

        ValidateTargetRange(
            scale.TraversableDiameterMetres,
            pointer.AppendProperty("traversable_diameter_m"), context, id, bag,
            "world_scale.traversable_diameter_m");
        ValidateTargetRange(
            scale.TraversableDiameterBaseTravelSeconds,
            pointer.AppendProperty("traversable_diameter_base_travel_seconds"), context, id, bag,
            "world_scale.traversable_diameter_base_travel_seconds");
    }

    private static void ValidateTargetRange(
        MapGenerationDto.TargetRangeDto? range,
        JsonPointer pointer,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag,
        string subject)
    {
        if (range is null)
        {
            return;
        }

        SemanticCheck.FeasibleRange(
            range.Minimum, range.Maximum, range.Target, pointer, context, id, bag, subject);
        SemanticCheck.GreaterThan(
            range.Minimum, 0, pointer.AppendProperty("min"), context, id, bag,
            subject + "'s lower bound is a positive extent");
    }

    private static void ValidateDistanceBands(
        List<MapGenerationDto.DistanceBandDto>? bands,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<MapGenerationDto.DistanceBandDto> list = bands ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("distance_bands");
        List<string> names = new(list.Count);

        for (int index = 0; index < list.Count; index++)
        {
            MapGenerationDto.DistanceBandDto band = list[index];
            JsonPointer bandPointer = pointer.AppendIndex(index);
            names.Add(band.Band ?? string.Empty);

            if (band.RouteDistanceMetres is not null)
            {
                SemanticCheck.FeasibleRange(
                    band.RouteDistanceMetres.MinimumExclusive,
                    band.RouteDistanceMetres.MaximumInclusive,
                    null,
                    bandPointer.AppendProperty("route_distance_m"), context, id, bag,
                    "a distance band's route_distance_m");
                SemanticCheck.GreaterThan(
                    band.RouteDistanceMetres.MaximumInclusive, 0,
                    bandPointer.AppendProperty("route_distance_m").AppendProperty("max_inclusive"),
                    context, id, bag,
                    "a distance band's upper bound is a positive route distance");
            }

            if (band.BaseTravelTimeFromDeployment is not null)
            {
                SemanticCheck.FeasibleRange(
                    band.BaseTravelTimeFromDeployment.MinimumSecondsExclusive,
                    band.BaseTravelTimeFromDeployment.MaximumSecondsInclusive,
                    null,
                    bandPointer.AppendProperty("base_travel_time_from_deployment"), context, id,
                    bag,
                    "a distance band's base_travel_time_from_deployment");
            }
        }

        SemanticCheck.Distinct(names, pointer, context, id, bag, "the distance band names");
    }

    private static void ValidateDeployment(
        MapGenerationDto.DeploymentFairnessDto? deployment,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (deployment is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("deployment_and_opening_fairness");

        SemanticCheck.GreaterThan(
            deployment.ObstacleFreeRadiusInMiningZoneDiameters, 0,
            pointer.AppendProperty("obstacle_free_radius_in_mining_zone_diameters"), context, id,
            bag,
            "obstacle_free_radius_in_mining_zone_diameters is a positive clearance in mining-zone "
                + "diameters. That the clearance it resolves to is no tighter than a mining "
                + "point's cleared radius is RC-02, a relation across definitions, and is not "
                + "asserted from this file");

        if (deployment.NearestStandardOreSeamBaseTravelSeconds is not null)
        {
            SemanticCheck.FeasibleRange(
                deployment.NearestStandardOreSeamBaseTravelSeconds.Minimum,
                deployment.NearestStandardOreSeamBaseTravelSeconds.Maximum,
                null,
                pointer.AppendProperty("nearest_standard_ore_seam_base_travel_seconds"), context,
                id, bag,
                "nearest_standard_ore_seam_base_travel_seconds");
        }
    }

    private static void ValidateVisibleOpportunities(
        MapGenerationDto.VisibleOpportunitiesDto? visible,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (visible is null)
        {
            return;
        }

        JsonPointer pointer =
            JsonPointer.Root.AppendProperty("visible_mining_opportunities_in_normal_view");

        SemanticCheck.FeasibleRange(
            visible.TargetMinimum, visible.TargetMaximum, null, pointer, context, id, bag,
            "the target band of visible_mining_opportunities_in_normal_view");

        if (visible.TargetMaximum is null || visible.HardMaximum is null)
        {
            return;
        }

        if (visible.TargetMaximum.Value <= visible.HardMaximum.Value)
        {
            return;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.RangeInfeasible,
            context.SourcePath,
            pointer.AppendProperty("target_maximum"),
            id,
            "the target ceiling is no greater than the hard ceiling. Two ceilings on one quantity "
                + "need the relation asserted and not only their two ranges: both pass any "
                + "plausible range check with the relation inverted, and an inverted pair would "
                + "make the target unreachable rather than merely unusual"));
    }
}
