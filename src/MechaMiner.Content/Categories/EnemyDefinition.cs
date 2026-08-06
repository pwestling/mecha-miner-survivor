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

/// <summary>One validated ordinary enemy definition.</summary>
public sealed class EnemyDefinition : ContentDefinition
{
    internal EnemyDefinition(
        DefinitionEnvelope envelope,
        string family,
        string? variantOf,
        long earliestMinute,
        long hull,
        long contactDamage,
        long controlResistancePercent,
        double postHardControlImmunitySeconds,
        long movementSpeedPercentOfMechBase,
        double bodyScaleMultiplier,
        string behaviorKind,
        bool eliteEligible,
        IReadOnlyList<string> snapshotAtCreation)
        : base(envelope, DefinitionKind.Enemy)
    {
        Family = family;
        VariantOf = variantOf;
        EarliestMinute = earliestMinute;
        Hull = hull;
        ContactDamage = contactDamage;
        ControlResistancePercent = controlResistancePercent;
        PostHardControlImmunitySeconds = postHardControlImmunitySeconds;
        MovementSpeedPercentOfMechBase = movementSpeedPercentOfMechBase;
        BodyScaleMultiplier = bodyScaleMultiplier;
        BehaviorKind = behaviorKind;
        EliteEligible = eliteEligible;
        SnapshotAtCreation = snapshotAtCreation;
    }

    /// <summary>The silhouette family this enemy belongs to.</summary>
    public string Family { get; }

    /// <summary>The enemy this one is a production variant of, where it is one.</summary>
    public string? VariantOf { get; }

    /// <summary>The first schedule minute this enemy may appear in.</summary>
    public long EarliestMinute { get; }

    /// <summary>Hull integrity.</summary>
    public long Hull { get; }

    /// <summary>Damage one contact deals.</summary>
    public long ContactDamage { get; }

    /// <summary>Control resistance, in percentage points.</summary>
    public long ControlResistancePercent { get; }

    /// <summary>Immunity after a hard control effect resolves.</summary>
    public double PostHardControlImmunitySeconds { get; }

    /// <summary>Movement speed as a percentage of the mech's base speed.</summary>
    /// <remarks>
    /// The percentage is authored and the world speed is derived. The relative form
    /// tracks the baseline automatically; a world speed authored beside it would stop
    /// tracking the moment the baseline moved, which is the defect that put a gameplay
    /// table and a technical table 0.004 M apart on one enemy.
    /// </remarks>
    public long MovementSpeedPercentOfMechBase { get; }

    /// <summary>
    /// Body scale against the enemy reference diameter. The only authored geometry an
    /// enemy carries; the contact diameter and centre distance are both derived from it.
    /// </summary>
    public double BodyScaleMultiplier { get; }

    /// <summary>The registered behavior token.</summary>
    public string BehaviorKind { get; }

    /// <summary>Whether the shared elite modifier profile may be applied to this enemy.</summary>
    public bool EliteEligible { get; }

    /// <summary>
    /// The projectile properties this enemy's specialist attack samples at creation,
    /// empty where it has no specialist attack.
    /// </summary>
    public IReadOnlyList<string> SnapshotAtCreation { get; }
}

/// <summary>The wire shape of an enemy definition's domain fields.</summary>
internal sealed class EnemyDto
{
    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("variant_of")]
    public string? VariantOf { get; set; }

    [JsonPropertyName("spawn_classification")]
    public string? SpawnClassification { get; set; }

    [JsonPropertyName("earliest_minute")]
    public double? EarliestMinute { get; set; }

    [JsonPropertyName("hull")]
    public double? Hull { get; set; }

    [JsonPropertyName("contact_damage")]
    public double? ContactDamage { get; set; }

    [JsonPropertyName("control_resistance_percent")]
    public double? ControlResistancePercent { get; set; }

    [JsonPropertyName("post_hard_control_immunity_seconds")]
    public double? PostHardControlImmunitySeconds { get; set; }

    [JsonPropertyName("movement_speed_percent_of_mech_base")]
    public double? MovementSpeedPercentOfMechBase { get; set; }

    [JsonPropertyName("body_scale_multiplier")]
    public double? BodyScaleMultiplier { get; set; }

    [JsonPropertyName("contact_shape")]
    public string? ContactShape { get; set; }

    [JsonPropertyName("behavior_kind")]
    public string? BehaviorKind { get; set; }

    [JsonPropertyName("specialist_attack")]
    public SpecialistAttackDto? SpecialistAttack { get; set; }

    [JsonPropertyName("elite_eligible")]
    public bool? EliteEligible { get; set; }

    [JsonPropertyName("first_playable_subset")]
    public FirstPlayableSubsetDto? FirstPlayableSubset { get; set; }

    internal sealed class FirstPlayableSubsetDto
    {
        [JsonPropertyName("included")]
        public bool? Included { get; set; }

        [JsonPropertyName("temporary_substitute_enemy_id")]
        public string? TemporarySubstituteEnemyId { get; set; }
    }

    internal sealed class SpecialistAttackDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("cadence_seconds")]
        public double? CadenceSeconds { get; set; }

        [JsonPropertyName("charge_duration_seconds")]
        public double? ChargeDurationSeconds { get; set; }

        [JsonPropertyName("movement_speed_while_charging_multiplier")]
        public double? MovementSpeedWhileChargingMultiplier { get; set; }

        [JsonPropertyName("projectile")]
        public ProjectileDto? Projectile { get; set; }
    }

    internal sealed class ProjectileDto
    {
        [JsonPropertyName("damage")]
        public double? Damage { get; set; }

        [JsonPropertyName("speed_percent_of_mech_base")]
        public double? SpeedPercentOfMechBase { get; set; }

        [JsonPropertyName("snapshot_at_creation")]
        public List<string>? SnapshotAtCreation { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="EnemyDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(EnemyDto))]
internal sealed partial class EnemyJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one ordinary enemy definition.</summary>
public static class EnemyReader
{
    /// <summary>Reads one enemy.</summary>
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

        EnemyDto? dto = JsonSerializer.Deserialize(utf8, EnemyJsonContext.Default.EnemyDto);
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

        EnemyDefinition definition = new(
            envelope,
            dto.Family!,
            dto.VariantOf,
            (long)dto.EarliestMinute!.Value,
            (long)dto.Hull!.Value,
            (long)dto.ContactDamage!.Value,
            (long)dto.ControlResistancePercent!.Value,
            dto.PostHardControlImmunitySeconds!.Value,
            (long)dto.MovementSpeedPercentOfMechBase!.Value,
            dto.BodyScaleMultiplier!.Value,
            dto.BehaviorKind!,
            dto.EliteEligible!.Value,
            Freeze(dto.SpecialistAttack?.Projectile?.SnapshotAtCreation));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        EnemyDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.BehaviorToken(
            dto.BehaviorKind, root.AppendProperty("behavior_kind"), context, id, bag);
        SemanticCheck.Token(
            dto.ContactShape, CombatShapes.ContactShapes,
            root.AppendProperty("contact_shape"), context, id, bag);

        if (dto.SpawnClassification is not null)
        {
            SemanticCheck.Token(
                dto.SpawnClassification, EnemySchema.SpawnClassifications,
                root.AppendProperty("spawn_classification"), context, id, bag);
        }

        if (dto.VariantOf is not null)
        {
            SemanticCheck.ReferenceGrammar(
                dto.VariantOf, ContentCategory.Enemy,
                root.AppendProperty("variant_of"), context, id, bag);
        }

        if (dto.FirstPlayableSubset?.TemporarySubstituteEnemyId is not null)
        {
            SemanticCheck.ReferenceGrammar(
                dto.FirstPlayableSubset.TemporarySubstituteEnemyId, ContentCategory.Enemy,
                root.AppendProperty("first_playable_subset")
                    .AppendProperty("temporary_substitute_enemy_id"),
                context, id, bag);
        }

        SemanticCheck.Integer(dto.Hull, root.AppendProperty("hull"), context, id, bag, "hull");
        SemanticCheck.GreaterThan(
            dto.Hull, 0, root.AppendProperty("hull"), context, id, bag,
            "hull is the amount of damage the enemy absorbs before it is defeated, so it is "
                + "positive; an enemy with no Hull could not be hit");

        SemanticCheck.Integer(
            dto.ContactDamage, root.AppendProperty("contact_damage"), context, id, bag,
            "contact_damage");
        SemanticCheck.GreaterThan(
            dto.ContactDamage, 0, root.AppendProperty("contact_damage"), context, id, bag,
            "contact_damage is positive; the compiler divides one hundred Hull by it to derive "
                + "hits-to-defeat, and a zero would make that derivation undefined rather than "
                + "merely wrong");

        SemanticCheck.Integer(
            dto.ControlResistancePercent, root.AppendProperty("control_resistance_percent"),
            context, id, bag, "control_resistance_percent");
        SemanticCheck.Within(
            dto.ControlResistancePercent, 0, 100,
            root.AppendProperty("control_resistance_percent"), context, id, bag,
            "control_resistance_percent is a share of an applied control effect that is resisted, "
                + "so it lies between zero and one hundred percentage points; the elite profile's "
                + "addition is bounded separately by its own maximum");

        SemanticCheck.AtLeast(
            dto.PostHardControlImmunitySeconds, 0,
            root.AppendProperty("post_hard_control_immunity_seconds"), context, id, bag,
            "post_hard_control_immunity_seconds is a duration and doc 40 § Unit and numeric "
                + "policy makes durations nonnegative");

        SemanticCheck.Integer(
            dto.EarliestMinute, root.AppendProperty("earliest_minute"), context, id, bag,
            "earliest_minute");
        SemanticCheck.Within(
            dto.EarliestMinute, 0, 34, root.AppendProperty("earliest_minute"), context, id, bag,
            "earliest_minute names a row of the 35-minute standard schedule, which runs from "
                + "minute 0 to minute 34 inclusive");

        SemanticCheck.Integer(
            dto.MovementSpeedPercentOfMechBase,
            root.AppendProperty("movement_speed_percent_of_mech_base"), context, id, bag,
            "movement_speed_percent_of_mech_base");
        SemanticCheck.GreaterThan(
            dto.MovementSpeedPercentOfMechBase, 0,
            root.AppendProperty("movement_speed_percent_of_mech_base"), context, id, bag,
            "movement_speed_percent_of_mech_base is a positive share of the mech's base speed; "
                + "doc 40 § Unit and numeric policy makes rates nonnegative and a stationary "
                + "enemy would be a different behavior, not a zero speed");

        SemanticCheck.GreaterThan(
            dto.BodyScaleMultiplier, 0, root.AppendProperty("body_scale_multiplier"), context, id,
            bag,
            "body_scale_multiplier scales the enemy reference diameter, so it is positive; the "
                + "compiler multiplies it to derive the contact diameter and a nonpositive scale "
                + "would derive a footprint with no area");

        ValidateSpecialistAttack(dto.SpecialistAttack, context, id, bag);
    }

    private static void ValidateSpecialistAttack(
        EnemyDto.SpecialistAttackDto? attack,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (attack is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("specialist_attack");

        SemanticCheck.BehaviorToken(
            attack.Kind, pointer.AppendProperty("kind"), context, id, bag);
        SemanticCheck.GreaterThan(
            attack.CadenceSeconds, 0, pointer.AppendProperty("cadence_seconds"), context, id, bag,
            "cadence_seconds is the interval between activations and doc 40 § Semantic names "
                + "positive cadence as a semantic rule; a zero interval is an activation every "
                + "frame rather than a cadence");
        SemanticCheck.AtLeast(
            attack.ChargeDurationSeconds, 0, pointer.AppendProperty("charge_duration_seconds"),
            context, id, bag,
            "charge_duration_seconds is a duration and durations are nonnegative");
        SemanticCheck.GreaterThan(
            attack.MovementSpeedWhileChargingMultiplier, 0,
            pointer.AppendProperty("movement_speed_while_charging_multiplier"), context, id, bag,
            "movement_speed_while_charging_multiplier scales the enemy's own speed while it "
                + "charges, so it is positive; a full stop is a zero-length charge window, not a "
                + "zero multiplier");

        if (attack.Projectile is null)
        {
            return;
        }

        JsonPointer projectile = pointer.AppendProperty("projectile");
        SemanticCheck.Integer(
            attack.Projectile.Damage, projectile.AppendProperty("damage"), context, id, bag,
            "damage");
        SemanticCheck.GreaterThan(
            attack.Projectile.Damage, 0, projectile.AppendProperty("damage"), context, id, bag,
            "a projectile's damage is positive; a projectile that deals none is a telegraph");
        SemanticCheck.Integer(
            attack.Projectile.SpeedPercentOfMechBase,
            projectile.AppendProperty("speed_percent_of_mech_base"), context, id, bag,
            "speed_percent_of_mech_base");
        SemanticCheck.GreaterThan(
            attack.Projectile.SpeedPercentOfMechBase, 0,
            projectile.AppendProperty("speed_percent_of_mech_base"), context, id, bag,
            "a projectile's speed is a positive share of the mech's base speed");

        List<string> snapshot = attack.Projectile.SnapshotAtCreation ?? new List<string>();
        JsonPointer snapshotPointer = projectile.AppendProperty("snapshot_at_creation");
        for (int index = 0; index < snapshot.Count; index++)
        {
            SemanticCheck.Token(
                snapshot[index], CombatShapes.SnapshotProperties,
                snapshotPointer.AppendIndex(index), context, id, bag);
        }

        SemanticCheck.Distinct(
            snapshot, snapshotPointer, context, id, bag,
            "the properties a projectile samples at creation");
    }

    private static IReadOnlyList<string> Freeze(List<string>? values)
    {
        return values is null
            ? Array.Empty<string>()
            : new ReadOnlyCollection<string>(new List<string>(values));
    }
}
