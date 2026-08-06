using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Categories;

/// <summary>The validated player baseline.</summary>
public sealed class PlayerBaselineDefinition : ContentDefinition
{
    internal PlayerBaselineDefinition(
        DefinitionEnvelope envelope,
        long maximumHullIntegrity,
        long armor,
        double recoveryHullPerSecond,
        long revivalCharges,
        double movementSpeedMetresPerSecond,
        double collisionDiameterMetres,
        double sameEnemyContactRepeatIntervalSeconds,
        double globalContactGraceAfterResolvedContactSeconds,
        double enemyBodyScaleReferenceDiameterMetres)
        : base(envelope, DefinitionKind.PlayerBaseline)
    {
        MaximumHullIntegrity = maximumHullIntegrity;
        Armor = armor;
        RecoveryHullPerSecond = recoveryHullPerSecond;
        RevivalCharges = revivalCharges;
        MovementSpeedMetresPerSecond = movementSpeedMetresPerSecond;
        CollisionDiameterMetres = collisionDiameterMetres;
        SameEnemyContactRepeatIntervalSeconds = sameEnemyContactRepeatIntervalSeconds;
        GlobalContactGraceAfterResolvedContactSeconds =
            globalContactGraceAfterResolvedContactSeconds;
        EnemyBodyScaleReferenceDiameterMetres = enemyBodyScaleReferenceDiameterMetres;
    }

    /// <summary>Maximum Hull integrity before any mech trait or PowerUp.</summary>
    public long MaximumHullIntegrity { get; }

    /// <summary>Armor before any mech trait or PowerUp.</summary>
    public long Armor { get; }

    /// <summary>Passive Hull recovery per second.</summary>
    public double RecoveryHullPerSecond { get; }

    /// <summary>Revival charges per run before any PowerUp.</summary>
    public long RevivalCharges { get; }

    /// <summary>
    /// Base movement speed, in mech collision diameters per second. Every enemy and
    /// boss world speed is derived from this and their own percentage.
    /// </summary>
    public double MovementSpeedMetresPerSecond { get; }

    /// <summary>
    /// The mech's collision circle. Half of it is the term in every derived
    /// contact-begin centre distance, which is why those are derived and not authored.
    /// </summary>
    public double CollisionDiameterMetres { get; }

    /// <summary>How long before the same enemy can deal contact damage again.</summary>
    public double SameEnemyContactRepeatIntervalSeconds { get; }

    /// <summary>The global grace after any contact resolves.</summary>
    public double GlobalContactGraceAfterResolvedContactSeconds { get; }

    /// <summary>
    /// The diameter an enemy's <c>body_scale_multiplier</c> scales.
    /// </summary>
    /// <remarks>
    /// This is an alien dimension living on the player baseline, and it is the one field
    /// here that is arguably in the wrong home: it is neither a player statistic nor an
    /// elite one. The alternative is a fifth aggregate in the enemies catalog, which
    /// needs an ID grant. It is recorded here so the ten enemy files stop each carrying
    /// their own copy of it, which is the larger of the two defects.
    /// </remarks>
    public double EnemyBodyScaleReferenceDiameterMetres { get; }
}

/// <summary>The wire shape of the player baseline's domain fields.</summary>
internal sealed class PlayerBaselineDto
{
    [JsonPropertyName("maximum_hull_integrity")]
    public double? MaximumHullIntegrity { get; set; }

    [JsonPropertyName("armor")]
    public double? Armor { get; set; }

    [JsonPropertyName("recovery_hull_per_second")]
    public double? RecoveryHullPerSecond { get; set; }

    [JsonPropertyName("revival_charges")]
    public double? RevivalCharges { get; set; }

    [JsonPropertyName("movement_speed_m_per_s")]
    public double? MovementSpeedMetresPerSecond { get; set; }

    [JsonPropertyName("collision_diameter_m")]
    public double? CollisionDiameterMetres { get; set; }

    [JsonPropertyName("collision_shape")]
    public string? CollisionShape { get; set; }

    [JsonPropertyName("mining_extraction_rate_percent")]
    public double? MiningExtractionRatePercent { get; set; }

    [JsonPropertyName("weapon_damage_percent")]
    public double? WeaponDamagePercent { get; set; }

    [JsonPropertyName("weapon_attack_rate_percent")]
    public double? WeaponAttackRatePercent { get; set; }

    [JsonPropertyName("weapon_area_percent")]
    public double? WeaponAreaPercent { get; set; }

    [JsonPropertyName("same_enemy_contact_repeat_interval_seconds")]
    public double? SameEnemyContactRepeatIntervalSeconds { get; set; }

    [JsonPropertyName("global_contact_grace_after_resolved_contact_seconds")]
    public double? GlobalContactGraceAfterResolvedContactSeconds { get; set; }

    [JsonPropertyName("enemy_body_scale_reference_diameter_m")]
    public double? EnemyBodyScaleReferenceDiameterMetres { get; set; }
}

/// <summary>Source-generated metadata for <see cref="PlayerBaselineDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(PlayerBaselineDto))]
internal sealed partial class PlayerBaselineJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates the player baseline.</summary>
public static class PlayerBaselineReader
{
    /// <summary>The reference percentage every named-statistic modifier is relative to.</summary>
    /// <remarks>
    /// One hundred percentage points is the baseline by definition; the five reference
    /// fields state it once each so that a modifier stack has something to resolve
    /// against. They are pinned rather than merely bounded, because a baseline that was
    /// not one hundred percent would silently rescale every trait, utility and PowerUp
    /// in the tree.
    /// </remarks>
    public const int ReferencePercent = 100;

    /// <summary>Reads the player baseline.</summary>
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

        PlayerBaselineDto? dto = JsonSerializer.Deserialize(
            utf8, PlayerBaselineJsonContext.Default.PlayerBaselineDto);
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

        PlayerBaselineDefinition definition = new(
            envelope,
            (long)dto.MaximumHullIntegrity!.Value,
            (long)dto.Armor!.Value,
            dto.RecoveryHullPerSecond!.Value,
            (long)dto.RevivalCharges!.Value,
            dto.MovementSpeedMetresPerSecond!.Value,
            dto.CollisionDiameterMetres!.Value,
            dto.SameEnemyContactRepeatIntervalSeconds!.Value,
            dto.GlobalContactGraceAfterResolvedContactSeconds!.Value,
            dto.EnemyBodyScaleReferenceDiameterMetres!.Value);

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        PlayerBaselineDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.Token(
            dto.CollisionShape, CombatShapes.ContactShapes, root.AppendProperty("collision_shape"),
            context, id, bag);

        SemanticCheck.Integer(
            dto.MaximumHullIntegrity, root.AppendProperty("maximum_hull_integrity"), context, id,
            bag, "maximum_hull_integrity");
        SemanticCheck.GreaterThan(
            dto.MaximumHullIntegrity, 0, root.AppendProperty("maximum_hull_integrity"), context,
            id, bag,
            "maximum_hull_integrity is positive; it is the denominator of every derived "
                + "hits-to-defeat figure in the survivability report");

        SemanticCheck.Integer(dto.Armor, root.AppendProperty("armor"), context, id, bag, "armor");
        SemanticCheck.AtLeast(
            dto.Armor, 0, root.AppendProperty("armor"), context, id, bag,
            "armor is nonnegative. It is zero at baseline and stated rather than omitted, because "
                + "the survivability report has an armour column and an omitted baseline would "
                + "leave it with nothing to compare against");

        SemanticCheck.AtLeast(
            dto.RecoveryHullPerSecond, 0, root.AppendProperty("recovery_hull_per_second"), context,
            id, bag,
            "recovery_hull_per_second is a rate and doc 40 § Unit and numeric policy makes rates "
                + "nonnegative");

        SemanticCheck.Integer(
            dto.RevivalCharges, root.AppendProperty("revival_charges"), context, id, bag,
            "revival_charges");
        SemanticCheck.AtLeast(
            dto.RevivalCharges, 0, root.AppendProperty("revival_charges"), context, id, bag,
            "revival_charges is a count and is nonnegative");

        SemanticCheck.GreaterThan(
            dto.MovementSpeedMetresPerSecond, 0, root.AppendProperty("movement_speed_m_per_s"),
            context, id, bag,
            "movement_speed_m_per_s is positive; every enemy and boss world speed is this "
                + "multiplied by their own percentage, so a zero here would derive a stationary "
                + "roster");

        SemanticCheck.GreaterThan(
            dto.CollisionDiameterMetres, 0, root.AppendProperty("collision_diameter_m"), context,
            id, bag,
            "collision_diameter_m is positive; it is the unit M itself, one unmodified mech "
                + "collision diameter, and half of it is a term in every derived contact-begin "
                + "centre distance");

        SemanticCheck.GreaterThan(
            dto.EnemyBodyScaleReferenceDiameterMetres, 0,
            root.AppendProperty("enemy_body_scale_reference_diameter_m"), context, id, bag,
            "enemy_body_scale_reference_diameter_m is the diameter an enemy's body scale "
                + "multiplies and is positive");

        SemanticCheck.AtLeast(
            dto.SameEnemyContactRepeatIntervalSeconds, 0,
            root.AppendProperty("same_enemy_contact_repeat_interval_seconds"), context, id, bag,
            "same_enemy_contact_repeat_interval_seconds is a duration and durations are "
                + "nonnegative");
        SemanticCheck.AtLeast(
            dto.GlobalContactGraceAfterResolvedContactSeconds, 0,
            root.AppendProperty("global_contact_grace_after_resolved_contact_seconds"), context,
            id, bag,
            "global_contact_grace_after_resolved_contact_seconds is a duration and durations are "
                + "nonnegative");

        PinReference(dto.MiningExtractionRatePercent, "mining_extraction_rate_percent", context, id, bag);
        PinReference(dto.WeaponDamagePercent, "weapon_damage_percent", context, id, bag);
        PinReference(dto.WeaponAttackRatePercent, "weapon_attack_rate_percent", context, id, bag);
        PinReference(dto.WeaponAreaPercent, "weapon_area_percent", context, id, bag);
    }

    private static void PinReference(
        double? value,
        string field,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty(field);
        SemanticCheck.Integer(value, pointer, context, id, bag, field);
        SemanticCheck.Within(
            value, ReferencePercent, ReferencePercent, pointer, context, id, bag,
            field + " is exactly one hundred percentage points: it is the reference point every "
                + "mech trait, utility, and PowerUp modifies, so it is the definition of the "
                + "denominator rather than a tunable value. Changing it would rescale every "
                + "modifier in the tree without any of them changing");
    }
}
