using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Categories;

/// <summary>One validated resource definition.</summary>
public sealed class ResourceDefinition : ContentDefinition
{
    internal ResourceDefinition(
        DefinitionEnvelope envelope,
        string resourceClass,
        string? canonicalLetter,
        string inventoryScope,
        string persistenceClass,
        long? maximumSafeCount,
        ResonanceBehavior? resonance)
        : base(envelope, DefinitionKind.Resource)
    {
        ResourceClass = resourceClass;
        CanonicalLetter = canonicalLetter;
        InventoryScope = inventoryScope;
        PersistenceClass = persistenceClass;
        MaximumSafeCount = maximumSafeCount;
        Resonance = resonance;
    }

    /// <summary>Which of the three resource shapes this definition is.</summary>
    public string ResourceClass { get; }

    /// <summary>
    /// The player-visible letter <c>A</c>..<c>F</c>, on a specialized material only.
    /// </summary>
    public string? CanonicalLetter { get; }

    /// <summary>Whether units survive the end of a run.</summary>
    public string InventoryScope { get; }

    /// <summary>The persistence class.</summary>
    public string PersistenceClass { get; }

    /// <summary>
    /// The maximum count a run may safely hold, or null where no document states one.
    /// </summary>
    /// <remarks>
    /// Null here is absence of an authored bound and never an authored <c>null</c>: the
    /// codec rejects those. No accepted document states a maximum for any of the eight
    /// resources, so all eight omit the key today and the compiler materializes the
    /// documented default into the bundle.
    /// </remarks>
    public long? MaximumSafeCount { get; }

    /// <summary>The resonance behavior, on a specialized material only.</summary>
    public ResonanceBehavior? Resonance { get; }

    /// <summary>The resonance behavior a specialized material registers.</summary>
    public sealed class ResonanceBehavior
    {
        internal ResonanceBehavior(
            string? behaviorKind,
            string effectName,
            long modifierPercent,
            string modifierDirection,
            string? edgeCaseRule)
        {
            BehaviorKind = behaviorKind;
            EffectName = effectName;
            ModifierPercent = modifierPercent;
            ModifierDirection = modifierDirection;
            EdgeCaseRule = edgeCaseRule;
        }

        /// <summary>
        /// The registered behavior token, absent until the behavior registry mints one.
        /// </summary>
        /// <remarks>
        /// Doc 40 § Resources requires "resonance behavior registration if applicable",
        /// so the field is declared and its grammar validated. No definition carries a
        /// token yet, because minting them is the behavior registry's work; the field
        /// is therefore optional rather than required, and becomes required in the same
        /// change that mints the vocabulary. Declaring it optional and unvalidated would
        /// be the thing doc 40 § Agent content-change workflow forbids; declaring it
        /// optional and grammar-validated is a field that cannot silently accept prose.
        /// </remarks>
        public string? BehaviorKind { get; }

        /// <summary>The effect's authored name.</summary>
        public string EffectName { get; }

        /// <summary>The magnitude, in percentage points.</summary>
        public long ModifierPercent { get; }

        /// <summary>Which way the modifier moves the statistic.</summary>
        public string ModifierDirection { get; }

        /// <summary>The edge case this material's field resolves, where one is stated.</summary>
        public string? EdgeCaseRule { get; }
    }
}

/// <summary>The wire shape of a resource definition's domain fields.</summary>
/// <remarks>
/// Numbers are <c>double?</c> rather than <c>long?</c> for the reason
/// <c>EnvelopeDto</c> gives: an <c>int?</c> property turns a fractional value into a
/// deserialization exception with no pointer and no code, where a JSON number checked
/// for integrality by the validator produces a diagnostic naming the field.
/// </remarks>
internal sealed class ResourceDto
{
    [JsonPropertyName("resource_class")]
    public string? ResourceClass { get; set; }

    [JsonPropertyName("canonical_letter")]
    public string? CanonicalLetter { get; set; }

    [JsonPropertyName("inventory_scope")]
    public string? InventoryScope { get; set; }

    [JsonPropertyName("persistence_class")]
    public string? PersistenceClass { get; set; }

    [JsonPropertyName("maximum_safe_count")]
    public double? MaximumSafeCount { get; set; }

    [JsonPropertyName("resonance_behavior")]
    public ResonanceBehaviorDto? ResonanceBehavior { get; set; }

    internal sealed class ResonanceBehaviorDto
    {
        [JsonPropertyName("behavior_kind")]
        public string? BehaviorKind { get; set; }

        [JsonPropertyName("effect_name")]
        public string? EffectName { get; set; }

        [JsonPropertyName("modifier_percent")]
        public double? ModifierPercent { get; set; }

        [JsonPropertyName("modifier_direction")]
        public string? ModifierDirection { get; set; }

        [JsonPropertyName("edge_case_rule")]
        public string? EdgeCaseRule { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="ResourceDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(ResourceDto))]
internal sealed partial class ResourceJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one resource definition.</summary>
public static class ResourceReader
{
    /// <summary>Reads one resource.</summary>
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

        ResourceDto? dto = JsonSerializer.Deserialize(utf8, ResourceJsonContext.Default.ResourceDto);
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

        ResourceDefinition definition = new(
            envelope,
            dto.ResourceClass!,
            dto.CanonicalLetter,
            dto.InventoryScope!,
            dto.PersistenceClass!,
            dto.MaximumSafeCount is null ? null : (long)dto.MaximumSafeCount.Value,
            dto.ResonanceBehavior is null
                ? null
                : new ResourceDefinition.ResonanceBehavior(
                    dto.ResonanceBehavior.BehaviorKind,
                    dto.ResonanceBehavior.EffectName!,
                    (long)dto.ResonanceBehavior.ModifierPercent!.Value,
                    dto.ResonanceBehavior.ModifierDirection!,
                    dto.ResonanceBehavior.EdgeCaseRule));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        ResourceDto dto,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.Token(
            dto.ResourceClass, ResourceSchema.ResourceClasses,
            root.AppendProperty("resource_class"), context, id, bag);
        SemanticCheck.Token(
            dto.InventoryScope, ResourceSchema.InventoryScopes,
            root.AppendProperty("inventory_scope"), context, id, bag);
        SemanticCheck.Token(
            dto.PersistenceClass, ResourceSchema.PersistenceClasses,
            root.AppendProperty("persistence_class"), context, id, bag);

        SemanticCheck.Integer(
            dto.MaximumSafeCount, root.AppendProperty("maximum_safe_count"), context, id, bag,
            "maximum_safe_count");
        SemanticCheck.AtLeast(
            dto.MaximumSafeCount, 1, root.AppendProperty("maximum_safe_count"), context, id, bag,
            "maximum_safe_count is a count of units and so is at least one; a resource that can "
                + "hold no units is the absence of the resource");

        ValidateCanonicalLetter(dto, context, id, bag);
        ValidateResonance(dto, outline, context, id, bag);
    }

    private static void ValidateCanonicalLetter(
        ResourceDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty("canonical_letter");
        bool expected = ResourceSchema.CarriesCanonicalLetter(dto.ResourceClass);

        if (dto.CanonicalLetter is null)
        {
            if (expected)
            {
                SemanticCheck.RequiredBy(
                    pointer, context, id, bag,
                    "a specialized material carries a canonical_letter; doc 40 § Resources lists "
                        + "the ID and the canonical letter as two fields, and the letter is the "
                        + "only thing that keeps a weapon recipe of opaque resource IDs checkable "
                        + "against the weapon's own W-xy suffix");
            }

            return;
        }

        if (!expected)
        {
            SemanticCheck.ForbiddenBy(
                pointer, context, id, bag,
                "only a specialized material carries a canonical_letter; common ore and Hyper "
                    + "Gold are not lettered, and a letter on one would make the six-material set "
                    + "check count seven");
            return;
        }

        if (!ResourceSchema.IsCanonicalLetter(dto.CanonicalLetter))
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.ValueOutOfRange,
                context.SourcePath,
                pointer,
                id,
                "canonical_letter matches " + ResourceSchema.CanonicalLetterPattern
                    + ": one of the six accepted uppercase letters"));
        }
    }

    private static void ValidateResonance(
        ResourceDto dto,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty("resonance_behavior");
        bool expected = ResourceSchema.CarriesCanonicalLetter(dto.ResourceClass);
        bool present = outline.Contains(pointer);

        if (!present)
        {
            if (expected)
            {
                SemanticCheck.RequiredBy(
                    pointer, context, id, bag,
                    "a specialized material registers a resonance behavior; doc 40 § Resources "
                        + "asks for it 'if applicable', and it is applicable to exactly the six "
                        + "materials, whose geodes each project a field");
            }

            return;
        }

        if (!expected)
        {
            SemanticCheck.ForbiddenBy(
                pointer, context, id, bag,
                "only a specialized material registers a resonance behavior; neither common ore "
                    + "nor Hyper Gold has a geode that projects a field");
            return;
        }

        ResourceDto.ResonanceBehaviorDto? resonance = dto.ResonanceBehavior;
        if (resonance is null)
        {
            return;
        }

        if (resonance.BehaviorKind is not null)
        {
            SemanticCheck.BehaviorToken(
                resonance.BehaviorKind, pointer.AppendProperty("behavior_kind"), context, id, bag);
        }

        SemanticCheck.Token(
            resonance.ModifierDirection, ResourceSchema.ModifierDirections,
            pointer.AppendProperty("modifier_direction"), context, id, bag);
        SemanticCheck.Integer(
            resonance.ModifierPercent, pointer.AppendProperty("modifier_percent"), context, id, bag,
            "modifier_percent");
        SemanticCheck.GreaterThan(
            resonance.ModifierPercent, 0, pointer.AppendProperty("modifier_percent"), context, id,
            bag,
            "modifier_percent is a magnitude in percentage points and the direction is carried by "
                + "modifier_direction, so the magnitude itself is positive; a negative magnitude "
                + "would encode the direction twice and the two could disagree");
    }
}
