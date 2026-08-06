using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Categories;

/// <summary>One validated weapon definition.</summary>
public sealed class WeaponDefinition : ContentDefinition
{
    internal WeaponDefinition(
        DefinitionEnvelope envelope,
        IReadOnlyList<string> recipePairMaterialIds,
        string? signatureMechId,
        string behaviorKind,
        IReadOnlyList<StatTrack> statTracks,
        IReadOnlyList<string> branchIds)
        : base(envelope, DefinitionKind.Weapon)
    {
        RecipePairMaterialIds = recipePairMaterialIds;
        SignatureMechId = signatureMechId;
        BehaviorKind = behaviorKind;
        StatTracks = statTracks;
        BranchIds = branchIds;
    }

    /// <summary>The two resource IDs this weapon is fabricated from, in authored order.</summary>
    /// <remarks>
    /// Order is authored and meaningful: the concatenation of the two resources'
    /// canonical letters, in this order, is the weapon ID's own suffix. The recipe as a
    /// <em>pair</em> is unordered for uniqueness purposes, which is why the catalog
    /// check sorts before comparing and this list does not.
    /// </remarks>
    public IReadOnlyList<string> RecipePairMaterialIds { get; }

    /// <summary>The mech that deploys with this weapon, where one does.</summary>
    public string? SignatureMechId { get; }

    /// <summary>The registered behavior token.</summary>
    public string BehaviorKind { get; }

    /// <summary>The three ore-upgradeable stat tracks, in slot order.</summary>
    public IReadOnlyList<StatTrack> StatTracks { get; }

    /// <summary>The three branch IDs, one per transformation class.</summary>
    public IReadOnlyList<string> BranchIds { get; }

    /// <summary>One ore-upgradeable stat track.</summary>
    public sealed class StatTrack
    {
        internal StatTrack(
            long slot,
            string name,
            string unit,
            double rankZero,
            double incrementPerRank,
            bool discrete)
        {
            Slot = slot;
            Name = name;
            Unit = unit;
            RankZero = rankZero;
            IncrementPerRank = incrementPerRank;
            Discrete = discrete;
        }

        /// <summary>The one-based track position.</summary>
        public long Slot { get; }

        /// <summary>The track's name, which branches reference.</summary>
        public string Name { get; }

        /// <summary>The unit the two numeric values are measured in.</summary>
        public string Unit { get; }

        /// <summary>The value at rank zero.</summary>
        public double RankZero { get; }

        /// <summary>How much one common-ore rank adds.</summary>
        public double IncrementPerRank { get; }

        /// <summary>Whether the value only takes whole steps.</summary>
        public bool Discrete { get; }
    }
}

/// <summary>The wire shape of a weapon definition's domain fields.</summary>
internal sealed class WeaponDto
{
    [JsonPropertyName("recipe_pair_material_ids")]
    public List<string>? RecipePairMaterialIds { get; set; }

    [JsonPropertyName("signature_mech_id")]
    public string? SignatureMechId { get; set; }

    [JsonPropertyName("behavior_kind")]
    public string? BehaviorKind { get; set; }

    [JsonPropertyName("targeting_policy")]
    public string? TargetingPolicy { get; set; }

    [JsonPropertyName("rock_targeting_behavior")]
    public string? RockTargetingBehavior { get; set; }

    [JsonPropertyName("ore_upgradeable_stats")]
    public List<StatTrackDto>? OreUpgradeableStats { get; set; }

    [JsonPropertyName("branch_ids")]
    public List<string>? BranchIds { get; set; }

    internal sealed class StatTrackDto
    {
        [JsonPropertyName("slot")]
        public double? Slot { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("rank_zero")]
        public double? RankZero { get; set; }

        [JsonPropertyName("increment_per_rank")]
        public double? IncrementPerRank { get; set; }

        [JsonPropertyName("discrete")]
        public bool? Discrete { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="WeaponDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(WeaponDto))]
internal sealed partial class WeaponJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one weapon definition.</summary>
public static class WeaponReader
{
    /// <summary>Reads one weapon.</summary>
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

        WeaponDto? dto = JsonSerializer.Deserialize(utf8, WeaponJsonContext.Default.WeaponDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        string? id = envelope?.Id.Value;
        Validate(dto, context, id, bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        List<WeaponDefinition.StatTrack> tracks = new();
        foreach (WeaponDto.StatTrackDto track in dto.OreUpgradeableStats!)
        {
            tracks.Add(new WeaponDefinition.StatTrack(
                (long)track.Slot!.Value,
                track.Name!,
                track.Unit!,
                track.RankZero!.Value,
                track.IncrementPerRank!.Value,
                track.Discrete!.Value));
        }

        WeaponDefinition definition = new(
            envelope,
            new ReadOnlyCollection<string>(new List<string>(dto.RecipePairMaterialIds!)),
            dto.SignatureMechId,
            dto.BehaviorKind!,
            new ReadOnlyCollection<WeaponDefinition.StatTrack>(tracks),
            new ReadOnlyCollection<string>(new List<string>(dto.BranchIds!)));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        WeaponDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.BehaviorToken(
            dto.BehaviorKind, root.AppendProperty("behavior_kind"), context, id, bag);
        SemanticCheck.BehaviorToken(
            dto.TargetingPolicy, root.AppendProperty("targeting_policy"), context, id, bag);
        SemanticCheck.BehaviorToken(
            dto.RockTargetingBehavior, root.AppendProperty("rock_targeting_behavior"), context, id,
            bag);

        if (dto.SignatureMechId is not null)
        {
            SemanticCheck.ReferenceGrammar(
                dto.SignatureMechId, ContentCategory.Mech,
                root.AppendProperty("signature_mech_id"), context, id, bag);
        }

        ValidateRecipe(dto, context, id, bag);
        ValidateStatTracks(dto, context, id, bag);
        ValidateBranchIds(dto, context, id, bag);
    }

    private static void ValidateRecipe(
        WeaponDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<string> materials = dto.RecipePairMaterialIds ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("recipe_pair_material_ids");

        SemanticCheck.ExactCount(
            materials.Count, WeaponSchema.RecipeMaterialCount, pointer, context, id, bag,
            "a recipe pair names exactly two resources, because a weapon ID is a two-letter "
                + "material pair and the two have to line up");

        for (int index = 0; index < materials.Count; index++)
        {
            SemanticCheck.ReferenceGrammar(
                materials[index], ContentCategory.Resource, pointer.AppendIndex(index), context,
                id, bag);
        }

        SemanticCheck.Distinct(
            materials, pointer, context, id, bag,
            "a recipe pair's resource IDs. A pair of one material twice is not a pair, and the "
                + "weapon ID it would have to match cannot spell one");
    }

    private static void ValidateStatTracks(
        WeaponDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<WeaponDto.StatTrackDto> tracks = dto.OreUpgradeableStats ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("ore_upgradeable_stats");

        SemanticCheck.ExactCount(
            tracks.Count, WeaponSchema.StatTrackCount, pointer, context, id, bag,
            "a weapon declares exactly three ore-upgradeable stat tracks, which doc 40 § Weapons "
                + "states as a compiler check. A branch may not add a fourth, so three is the "
                + "whole vocabulary a branch's effects can name");

        List<string> names = new(tracks.Count);
        List<long> slots = new(tracks.Count);

        for (int index = 0; index < tracks.Count; index++)
        {
            WeaponDto.StatTrackDto track = tracks[index];
            JsonPointer trackPointer = pointer.AppendIndex(index);

            names.Add(track.Name ?? string.Empty);
            slots.Add(SemanticCheck.Integer(
                track.Slot, trackPointer.AppendProperty("slot"), context, id, bag, "slot"));

            SemanticCheck.Token(
                track.Unit, WeaponSchema.StatUnits, trackPointer.AppendProperty("unit"), context,
                id, bag);
            SemanticCheck.AtLeast(
                track.RankZero, 0, trackPointer.AppendProperty("rank_zero"), context, id, bag,
                "a stat track's rank-zero value is nonnegative in every unit the enum accepts; "
                    + "none of the eight is a quantity that can run below zero");
            SemanticCheck.GreaterThan(
                track.IncrementPerRank, 0, trackPointer.AppendProperty("increment_per_rank"),
                context, id, bag,
                "increment_per_rank is what one common-ore rank adds and is positive; a track "
                    + "that does not improve with rank is not ore-upgradeable");
        }

        SemanticCheck.Distinct(
            names, pointer, context, id, bag,
            "a weapon's stat track names. Branch effects reference a track by name, so two tracks "
                + "sharing one would make that reference ambiguous");
        SemanticCheck.Contiguous(
            slots, 1, pointer, context, id, bag, "a weapon's stat track slots");
    }

    private static void ValidateBranchIds(
        WeaponDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<string> branchIds = dto.BranchIds ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("branch_ids");

        SemanticCheck.ExactCount(
            branchIds.Count, WeaponSchema.BranchCount, pointer, context, id, bag,
            "a weapon has exactly three branches. That they are one amplification, one "
                + "functional, and one conversion is asserted from the branches catalog, because "
                + "branch_class lives on the branch");

        for (int index = 0; index < branchIds.Count; index++)
        {
            JsonPointer element = pointer.AppendIndex(index);
            if (!SemanticCheck.ReferenceGrammar(
                    branchIds[index], ContentCategory.Branch, element, context, id, bag))
            {
                continue;
            }

            if (id is null || branchIds[index].StartsWith(id + "-", StringComparison.Ordinal))
            {
                continue;
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.ReferenceGrammarMismatch,
                context.SourcePath,
                element,
                id,
                "a branch ID begins with its parent weapon's ID followed by a hyphen, so this "
                    + "weapon's branch_ids all begin '" + id + "-'. A foreign branch listed here "
                    + "would give that branch two parents, which doc 40 § Branches forbids",
                new[] { branchIds[index] }));
        }

        SemanticCheck.Distinct(branchIds, pointer, context, id, bag, "a weapon's branch IDs");
    }
}
