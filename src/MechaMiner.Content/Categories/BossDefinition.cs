using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Categories;

/// <summary>One validated interval boss definition.</summary>
public sealed class BossDefinition : ContentDefinition
{
    internal BossDefinition(
        DefinitionEnvelope envelope,
        string arrivalTimecode,
        long initialHull,
        long contactDamage,
        long controlResistancePercent,
        double postHardControlImmunitySeconds,
        long movementSpeedPercentOfMechBase,
        double contactAndWeaponHurtDiameterMetres,
        string behaviorKind,
        string abilityKind,
        double abilityCadenceSeconds)
        : base(envelope, DefinitionKind.Boss)
    {
        ArrivalTimecode = arrivalTimecode;
        InitialHull = initialHull;
        ContactDamage = contactDamage;
        ControlResistancePercent = controlResistancePercent;
        PostHardControlImmunitySeconds = postHardControlImmunitySeconds;
        MovementSpeedPercentOfMechBase = movementSpeedPercentOfMechBase;
        ContactAndWeaponHurtDiameterMetres = contactAndWeaponHurtDiameterMetres;
        BehaviorKind = behaviorKind;
        AbilityKind = abilityKind;
        AbilityCadenceSeconds = abilityCadenceSeconds;
    }

    /// <summary>The run timecode at which the boss arrives, as authored.</summary>
    public string ArrivalTimecode { get; }

    /// <summary>Hull integrity on arrival.</summary>
    public long InitialHull { get; }

    /// <summary>Damage one contact deals.</summary>
    public long ContactDamage { get; }

    /// <summary>Control resistance, in percentage points.</summary>
    public long ControlResistancePercent { get; }

    /// <summary>Immunity after a hard control effect resolves.</summary>
    public double PostHardControlImmunitySeconds { get; }

    /// <summary>Movement speed as a percentage of the mech's base speed.</summary>
    public long MovementSpeedPercentOfMechBase { get; }

    /// <summary>
    /// The authored contact circle, in mech collision diameters. Authored rather than
    /// derived because no accepted document gives a boss a body scale to derive it from.
    /// </summary>
    public double ContactAndWeaponHurtDiameterMetres { get; }

    /// <summary>The registered behavior token.</summary>
    public string BehaviorKind { get; }

    /// <summary>Which of the four ability arms this boss uses.</summary>
    public string AbilityKind { get; }

    /// <summary>The interval between ability activations.</summary>
    public double AbilityCadenceSeconds { get; }
}

/// <summary>The wire shape of a boss definition's domain fields.</summary>
internal sealed class BossDto
{
    [JsonPropertyName("arrival")]
    public ArrivalDto? Arrival { get; set; }

    [JsonPropertyName("initial_hull")]
    public double? InitialHull { get; set; }

    [JsonPropertyName("contact_damage")]
    public double? ContactDamage { get; set; }

    [JsonPropertyName("control_resistance_percent")]
    public double? ControlResistancePercent { get; set; }

    [JsonPropertyName("post_hard_control_immunity_seconds")]
    public double? PostHardControlImmunitySeconds { get; set; }

    [JsonPropertyName("movement_speed_percent_of_mech_base")]
    public double? MovementSpeedPercentOfMechBase { get; set; }

    [JsonPropertyName("contact_shape")]
    public string? ContactShape { get; set; }

    [JsonPropertyName("contact_and_weapon_hurt_diameter_m")]
    public double? ContactAndWeaponHurtDiameterMetres { get; set; }

    [JsonPropertyName("behavior_kind")]
    public string? BehaviorKind { get; set; }

    [JsonPropertyName("ability")]
    public AbilityDto? Ability { get; set; }

    [JsonPropertyName("defeat_reward")]
    public DefeatRewardDto? DefeatReward { get; set; }

    internal sealed class ArrivalDto
    {
        [JsonPropertyName("timecode")]
        public string? Timecode { get; set; }
    }

    internal sealed class AbilityDto
    {
        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("cadence_seconds")]
        public double? CadenceSeconds { get; set; }

        [JsonPropertyName("spawn_enemy_id")]
        public string? SpawnEnemyId { get; set; }
    }

    internal sealed class DefeatRewardDto
    {
        [JsonPropertyName("common_ore")]
        public double? CommonOre { get; set; }

        [JsonPropertyName("specialized_material_units")]
        public double? SpecializedMaterialUnits { get; set; }

        [JsonPropertyName("unsecured_hyper_gold")]
        public double? UnsecuredHyperGold { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="BossDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(BossDto))]
internal sealed partial class BossJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one interval boss definition.</summary>
public static class BossReader
{
    /// <summary>The accepted arrival timecode pattern, as written.</summary>
    public const string TimecodePattern = "^[0-9]{1,2}:[0-5][0-9]$";

    private static readonly Regex Timecode = new(
        TimecodePattern,
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>Reads one boss.</summary>
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

        BossDto? dto = JsonSerializer.Deserialize(utf8, BossJsonContext.Default.BossDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        string? id = envelope?.Id.Value;
        Validate(dto, outline, context, id, bag);

        if (bag.HasErrors || envelope is null || dto.Ability is null || dto.Arrival is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        BossDefinition definition = new(
            envelope,
            dto.Arrival.Timecode!,
            (long)dto.InitialHull!.Value,
            (long)dto.ContactDamage!.Value,
            (long)dto.ControlResistancePercent!.Value,
            dto.PostHardControlImmunitySeconds!.Value,
            (long)dto.MovementSpeedPercentOfMechBase!.Value,
            dto.ContactAndWeaponHurtDiameterMetres!.Value,
            dto.BehaviorKind!,
            dto.Ability.Kind!,
            dto.Ability.CadenceSeconds!.Value);

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        BossDto dto,
        DocumentOutline outline,
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

        SemanticCheck.Integer(
            dto.InitialHull, root.AppendProperty("initial_hull"), context, id, bag, "initial_hull");
        SemanticCheck.GreaterThan(
            dto.InitialHull, 0, root.AppendProperty("initial_hull"), context, id, bag,
            "initial_hull is positive");

        SemanticCheck.Integer(
            dto.ContactDamage, root.AppendProperty("contact_damage"), context, id, bag,
            "contact_damage");
        SemanticCheck.GreaterThan(
            dto.ContactDamage, 0, root.AppendProperty("contact_damage"), context, id, bag,
            "contact_damage is positive; the compiler divides one hundred Hull by it to derive "
                + "hits-to-defeat");

        SemanticCheck.Integer(
            dto.ControlResistancePercent, root.AppendProperty("control_resistance_percent"),
            context, id, bag, "control_resistance_percent");
        SemanticCheck.Within(
            dto.ControlResistancePercent, 0, 100,
            root.AppendProperty("control_resistance_percent"), context, id, bag,
            "control_resistance_percent is a share and lies between zero and one hundred "
                + "percentage points");

        SemanticCheck.AtLeast(
            dto.PostHardControlImmunitySeconds, 0,
            root.AppendProperty("post_hard_control_immunity_seconds"), context, id, bag,
            "post_hard_control_immunity_seconds is a duration and durations are nonnegative");

        SemanticCheck.Integer(
            dto.MovementSpeedPercentOfMechBase,
            root.AppendProperty("movement_speed_percent_of_mech_base"), context, id, bag,
            "movement_speed_percent_of_mech_base");
        SemanticCheck.GreaterThan(
            dto.MovementSpeedPercentOfMechBase, 0,
            root.AppendProperty("movement_speed_percent_of_mech_base"), context, id, bag,
            "movement_speed_percent_of_mech_base is a positive share of the mech's base speed");

        SemanticCheck.GreaterThan(
            dto.ContactAndWeaponHurtDiameterMetres, 0,
            root.AppendProperty("contact_and_weapon_hurt_diameter_m"), context, id, bag,
            "contact_and_weapon_hurt_diameter_m is a diameter in mech collision diameters and is "
                + "positive; the compiler halves it and adds the player's collision radius to "
                + "derive the centre distance that begins contact");

        ValidateArrival(dto.Arrival, context, id, bag);
        ValidateDefeatReward(dto.DefeatReward, context, id, bag);
        ValidateAbility(dto.Ability, outline, context, id, bag);
    }

    private static void ValidateArrival(
        BossDto.ArrivalDto? arrival,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (arrival?.Timecode is null)
        {
            return;
        }

        if (Timecode.IsMatch(arrival.Timecode))
        {
            return;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.ValueOutOfRange,
            context.SourcePath,
            JsonPointer.Root.AppendProperty("arrival").AppendProperty("timecode"),
            id,
            "an arrival timecode matches " + TimecodePattern
                + ": minutes and seconds, with the seconds below sixty. The seconds into the run "
                + "are derived from it, so a timecode the parser cannot read would derive nothing "
                + "rather than derive something wrong"));
    }

    private static void ValidateDefeatReward(
        BossDto.DefeatRewardDto? reward,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (reward is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("defeat_reward");
        foreach ((string field, double? value) in new (string, double?)[]
                 {
                     ("common_ore", reward.CommonOre),
                     ("specialized_material_units", reward.SpecializedMaterialUnits),
                     ("unsecured_hyper_gold", reward.UnsecuredHyperGold),
                 })
        {
            SemanticCheck.Integer(value, pointer.AppendProperty(field), context, id, bag, field);
            SemanticCheck.AtLeast(
                value, 0, pointer.AppendProperty(field), context, id, bag,
                field + " is a count of awarded units and so is nonnegative; a boss that awards "
                    + "none of a resource records zero rather than omitting the field, because "
                    + "the economy report sums the column across all four bosses");
        }
    }

    private static void ValidateAbility(
        BossDto.AbilityDto? ability,
        DocumentOutline outline,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (ability is null)
        {
            return;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty("ability");

        bool kindIsKnown = SemanticCheck.Token(
            ability.Kind, BossSchema.AbilityKinds, pointer.AppendProperty("kind"), context, id,
            bag);

        SemanticCheck.GreaterThan(
            ability.CadenceSeconds, 0, pointer.AppendProperty("cadence_seconds"), context, id, bag,
            "cadence_seconds is the interval between ability activations and doc 40 § Semantic "
                + "names positive cadence as a semantic rule");

        if (ability.SpawnEnemyId is not null)
        {
            SemanticCheck.ReferenceGrammar(
                ability.SpawnEnemyId, ContentCategory.Enemy,
                pointer.AppendProperty("spawn_enemy_id"), context, id, bag);
        }

        if (!kindIsKnown || ability.Kind is null)
        {
            return;
        }

        HashSet<string> accepted = new(BossSchema.CommonAbilityFields, StringComparer.Ordinal);
        foreach (string field in BossSchema.ArmFields(ability.Kind))
        {
            accepted.Add(field);
        }

        foreach (string present in outline.PropertyNamesAt(pointer))
        {
            if (accepted.Contains(present))
            {
                continue;
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.DiscriminatorArmMismatch,
                context.SourcePath,
                pointer.AppendProperty(present),
                id,
                "'" + present + "' is a parameter of a different boss ability arm. The arm '"
                    + ability.Kind + "' accepts " + string.Join(", ", accepted)
                    + "; a parameter of another arm is neither ignored nor merged, because a "
                    + "charge duration on a leap would describe a timeline the ability does not "
                    + "have"));
        }
    }
}
