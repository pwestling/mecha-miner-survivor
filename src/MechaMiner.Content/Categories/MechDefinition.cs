using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>One validated mech definition.</summary>
public sealed class MechDefinition : ContentDefinition
{
    internal MechDefinition(
        DefinitionEnvelope envelope,
        string signatureWeaponId,
        long selectionOrder,
        bool isRecommendedDefault,
        InherentTrait trait,
        string? matchingUtilityId)
        : base(envelope, DefinitionKind.Mech)
    {
        SignatureWeaponId = signatureWeaponId;
        SelectionOrder = selectionOrder;
        IsRecommendedDefault = isRecommendedDefault;
        Trait = trait;
        MatchingUtilityId = matchingUtilityId;
    }

    /// <summary>The weapon this mech deploys with.</summary>
    public string SignatureWeaponId { get; }

    /// <summary>Where the mech sits in the selection screen's order.</summary>
    public long SelectionOrder { get; }

    /// <summary>
    /// Whether this is the mech the selection screen recommends. False is the
    /// materialized default for the five that omit the key.
    /// </summary>
    public bool IsRecommendedDefault { get; }

    /// <summary>The mech's one always-on modifier.</summary>
    public InherentTrait Trait { get; }

    /// <summary>The utility that modifies the same statistic as the trait, where one does.</summary>
    public string? MatchingUtilityId { get; }

    /// <summary>A mech's one always-on modifier.</summary>
    public sealed class InherentTrait
    {
        internal InherentTrait(
            string nameKey,
            string affectedStatistic,
            string modifierKind,
            double modifierValue,
            string? behaviorKind)
        {
            NameKey = nameKey;
            AffectedStatistic = affectedStatistic;
            ModifierKind = modifierKind;
            ModifierValue = modifierValue;
            BehaviorKind = behaviorKind;
        }

        /// <summary>The localization key of the trait's player-facing name.</summary>
        public string NameKey { get; }

        /// <summary>The named statistic the trait modifies.</summary>
        public string AffectedStatistic { get; }

        /// <summary>How the value combines with the baseline.</summary>
        public string ModifierKind { get; }

        /// <summary>The magnitude, in the unit the kind implies.</summary>
        public double ModifierValue { get; }

        /// <summary>The registered behavior token, absent until the registry mints one.</summary>
        public string? BehaviorKind { get; }
    }
}

/// <summary>The wire shape of a mech definition's domain fields.</summary>
internal sealed class MechDto
{
    [JsonPropertyName("signature_weapon_id")]
    public string? SignatureWeaponId { get; set; }

    [JsonPropertyName("selection_order")]
    public double? SelectionOrder { get; set; }

    [JsonPropertyName("is_recommended_default")]
    public bool? IsRecommendedDefault { get; set; }

    [JsonPropertyName("inherent_trait")]
    public InherentTraitDto? InherentTrait { get; set; }

    [JsonPropertyName("matching_utility_id")]
    public string? MatchingUtilityId { get; set; }

    internal sealed class InherentTraitDto
    {
        [JsonPropertyName("name_key")]
        public string? NameKey { get; set; }

        [JsonPropertyName("affected_statistic")]
        public string? AffectedStatistic { get; set; }

        [JsonPropertyName("modifier_kind")]
        public string? ModifierKind { get; set; }

        [JsonPropertyName("modifier_value")]
        public double? ModifierValue { get; set; }

        [JsonPropertyName("behavior_kind")]
        public string? BehaviorKind { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="MechDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(MechDto))]
internal sealed partial class MechJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one mech definition.</summary>
public static class MechReader
{
    /// <summary>Reads one mech.</summary>
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

        MechDto? dto = JsonSerializer.Deserialize(utf8, MechJsonContext.Default.MechDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        string? id = envelope?.Id.Value;
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.ReferenceGrammar(
            dto.SignatureWeaponId, ContentCategory.Weapon,
            root.AppendProperty("signature_weapon_id"), context, id, bag);

        SemanticCheck.Integer(
            dto.SelectionOrder, root.AppendProperty("selection_order"), context, id, bag,
            "selection_order");
        SemanticCheck.AtLeast(
            dto.SelectionOrder, 1, root.AppendProperty("selection_order"), context, id, bag,
            "selection_order is a one-based position in the selection screen's order, so it is at "
                + "least one; that no two mechs share a position is a catalog-level check");

        if (dto.MatchingUtilityId is not null)
        {
            SemanticCheck.ReferenceGrammar(
                dto.MatchingUtilityId, ContentCategory.Utility,
                root.AppendProperty("matching_utility_id"), context, id, bag);
        }

        ValidateTrait(dto.InherentTrait, context, id, bag);

        if (bag.HasErrors || envelope is null || dto.InherentTrait is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        MechDefinition definition = new(
            envelope,
            dto.SignatureWeaponId!,
            (long)dto.SelectionOrder!.Value,
            dto.IsRecommendedDefault ?? false,
            new MechDefinition.InherentTrait(
                dto.InherentTrait.NameKey!,
                dto.InherentTrait.AffectedStatistic!,
                dto.InherentTrait.ModifierKind!,
                dto.InherentTrait.ModifierValue!.Value,
                dto.InherentTrait.BehaviorKind),
            dto.MatchingUtilityId);

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void ValidateTrait(
        MechDto.InherentTraitDto? trait,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (trait is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("inherent_trait");

        SemanticCheck.Token(
            trait.AffectedStatistic, ContentVocabularies.NamedStatistics,
            pointer.AppendProperty("affected_statistic"), context, id, bag);
        SemanticCheck.Token(
            trait.ModifierKind, MechSchema.TraitModifierKinds,
            pointer.AppendProperty("modifier_kind"), context, id, bag);
        SemanticCheck.GreaterThan(
            trait.ModifierValue, 0, pointer.AppendProperty("modifier_value"), context, id, bag,
            "an inherent trait is a positive, always-on modifier, so its value is positive; "
                + "docs/35 § Signature and trait states the positivity as a roster principle "
                + "rather than as a per-mech fact, which is what makes it checkable here");

        if (trait.BehaviorKind is not null)
        {
            SemanticCheck.BehaviorToken(
                trait.BehaviorKind, pointer.AppendProperty("behavior_kind"), context, id, bag);
        }

        if (trait.NameKey is null)
        {
            return;
        }

        if (!LocalizationKey.TryParse(trait.NameKey, out LocalizationKey? key)
            || key!.Role != LocalizationRole.Name)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.LocalizationKeyMalformed,
                context.SourcePath,
                pointer.AppendProperty("name_key"),
                id,
                "a trait's name_key is a localization key of the form "
                    + "<category>.<stable_id>.<role> with the role 'name', never literal "
                    + "player-facing text; the trait name appears on the selection screen, which "
                    + "is what makes it a string players read"));
        }
    }
}
