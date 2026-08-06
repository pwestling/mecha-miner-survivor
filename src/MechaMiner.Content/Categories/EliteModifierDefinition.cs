using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Categories;

/// <summary>The validated shared elite modifier profile.</summary>
public sealed class EliteModifierDefinition : ContentDefinition
{
    internal EliteModifierDefinition(
        DefinitionEnvelope envelope,
        double maximumHullMultiplier,
        double movementSpeedMultiplier,
        double contactDamageMultiplier,
        double bodyScaleMultiplier,
        long addedControlResistancePercent,
        long controlResistanceMaximumPercent,
        double postHardControlImmunitySeconds,
        long maximumScheduledElitesAtOnce)
        : base(envelope, DefinitionKind.EliteModifiers)
    {
        MaximumHullMultiplier = maximumHullMultiplier;
        MovementSpeedMultiplier = movementSpeedMultiplier;
        ContactDamageMultiplier = contactDamageMultiplier;
        BodyScaleMultiplier = bodyScaleMultiplier;
        AddedControlResistancePercent = addedControlResistancePercent;
        ControlResistanceMaximumPercent = controlResistanceMaximumPercent;
        PostHardControlImmunitySeconds = postHardControlImmunitySeconds;
        MaximumScheduledElitesAtOnce = maximumScheduledElitesAtOnce;
    }

    /// <summary>How much an elite's maximum Hull is scaled.</summary>
    public double MaximumHullMultiplier { get; }

    /// <summary>How much an elite's movement speed is scaled.</summary>
    public double MovementSpeedMultiplier { get; }

    /// <summary>How much an elite's contact damage is scaled.</summary>
    public double ContactDamageMultiplier { get; }

    /// <summary>How much an elite's body scale is scaled, on top of the enemy's own.</summary>
    public double BodyScaleMultiplier { get; }

    /// <summary>Percentage points of control resistance an elite gains.</summary>
    public long AddedControlResistancePercent { get; }

    /// <summary>
    /// The ceiling an elite's control resistance may reach after the addition.
    /// </summary>
    /// <remarks>
    /// Two bounds live on this profile - an addition and a ceiling - so both carry a
    /// qualifier that says which is which. Doc 40's one spelling for a ceiling is
    /// <c>maximum_</c>, written out.
    /// </remarks>
    public long ControlResistanceMaximumPercent { get; }

    /// <summary>An elite's immunity after a hard control effect resolves.</summary>
    public double PostHardControlImmunitySeconds { get; }

    /// <summary>How many scheduled elites may be alive at once.</summary>
    public long MaximumScheduledElitesAtOnce { get; }
}

/// <summary>The wire shape of the elite modifier profile's domain fields.</summary>
internal sealed class EliteModifierDto
{
    [JsonPropertyName("maximum_hull_multiplier")]
    public double? MaximumHullMultiplier { get; set; }

    [JsonPropertyName("movement_speed_multiplier")]
    public double? MovementSpeedMultiplier { get; set; }

    [JsonPropertyName("contact_damage_multiplier")]
    public double? ContactDamageMultiplier { get; set; }

    [JsonPropertyName("body_scale_multiplier")]
    public double? BodyScaleMultiplier { get; set; }

    [JsonPropertyName("added_control_resistance_percent")]
    public double? AddedControlResistancePercent { get; set; }

    [JsonPropertyName("control_resistance_maximum_percent")]
    public double? ControlResistanceMaximumPercent { get; set; }

    [JsonPropertyName("post_hard_control_immunity_seconds")]
    public double? PostHardControlImmunitySeconds { get; set; }

    [JsonPropertyName("maximum_scheduled_elites_at_once")]
    public double? MaximumScheduledElitesAtOnce { get; set; }

    [JsonPropertyName("modifier_application_order")]
    public List<string>? ModifierApplicationOrder { get; set; }
}

/// <summary>Source-generated metadata for <see cref="EliteModifierDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(EliteModifierDto))]
internal sealed partial class EliteModifierJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates the shared elite modifier profile.</summary>
public static class EliteModifierReader
{
    /// <summary>Reads the elite modifier profile.</summary>
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

        EliteModifierDto? dto = JsonSerializer.Deserialize(
            utf8, EliteModifierJsonContext.Default.EliteModifierDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        Validate(dto, context, id, StructuralReport.Of(bag), bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        EliteModifierDefinition definition = new(
            envelope,
            dto.MaximumHullMultiplier!.Value,
            dto.MovementSpeedMultiplier!.Value,
            dto.ContactDamageMultiplier!.Value,
            dto.BodyScaleMultiplier!.Value,
            (long)dto.AddedControlResistancePercent!.Value,
            (long)dto.ControlResistanceMaximumPercent!.Value,
            dto.PostHardControlImmunitySeconds!.Value,
            (long)dto.MaximumScheduledElitesAtOnce!.Value);

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        EliteModifierDto dto,
        CategoryReadContext context,
        string? id,
        StructuralReport structural,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        foreach (string field in new[]
                 {
                     "maximum_hull_multiplier", "movement_speed_multiplier",
                     "contact_damage_multiplier", "body_scale_multiplier",
                 })
        {
            double? value = field switch
            {
                "maximum_hull_multiplier" => dto.MaximumHullMultiplier,
                "movement_speed_multiplier" => dto.MovementSpeedMultiplier,
                "contact_damage_multiplier" => dto.ContactDamageMultiplier,
                _ => dto.BodyScaleMultiplier,
            };

            SemanticCheck.GreaterThan(
                value, 0, root.AppendProperty(field), context, id, bag,
                field + " scales a base statistic, so it is positive; an elite that multiplied a "
                    + "statistic by zero would not be a stronger version of its base enemy, which "
                    + "is what the profile is for");
        }

        SemanticCheck.Integer(
            dto.AddedControlResistancePercent,
            root.AppendProperty("added_control_resistance_percent"), context, id, bag,
            "added_control_resistance_percent");
        SemanticCheck.Integer(
            dto.ControlResistanceMaximumPercent,
            root.AppendProperty("control_resistance_maximum_percent"), context, id, bag,
            "control_resistance_maximum_percent");
        SemanticCheck.Within(
            dto.ControlResistanceMaximumPercent, 0, 100,
            root.AppendProperty("control_resistance_maximum_percent"), context, id, bag,
            "control_resistance_maximum_percent is the ceiling an elite's resistance may reach "
                + "and is a share, so it lies between zero and one hundred percentage points");
        SemanticCheck.Within(
            dto.AddedControlResistancePercent, 0, 100,
            root.AppendProperty("added_control_resistance_percent"), context, id, bag,
            "added_control_resistance_percent is an addition in percentage points and cannot "
                + "exceed the full share");

        if (dto.AddedControlResistancePercent is not null
            && dto.ControlResistanceMaximumPercent is not null
            && dto.AddedControlResistancePercent.Value > dto.ControlResistanceMaximumPercent.Value)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.ValueOutOfRange,
                context.SourcePath,
                root.AppendProperty("added_control_resistance_percent"),
                id,
                "the addition cannot exceed the ceiling it is capped by; both bounds pass their "
                    + "own range check with the relation inverted, which is why the relation "
                    + "itself is asserted rather than only the two bands"));
        }

        SemanticCheck.AtLeast(
            dto.PostHardControlImmunitySeconds, 0,
            root.AppendProperty("post_hard_control_immunity_seconds"), context, id, bag,
            "post_hard_control_immunity_seconds is a duration and durations are nonnegative");

        SemanticCheck.Integer(
            dto.MaximumScheduledElitesAtOnce,
            root.AppendProperty("maximum_scheduled_elites_at_once"), context, id, bag,
            "maximum_scheduled_elites_at_once");
        SemanticCheck.AtLeast(
            dto.MaximumScheduledElitesAtOnce, 1,
            root.AppendProperty("maximum_scheduled_elites_at_once"), context, id, bag,
            "maximum_scheduled_elites_at_once is a ceiling on a live count and so is at least "
                + "one; a ceiling of zero is the absence of scheduled elites, which the schedule "
                + "expresses by not scheduling any");

        List<string> order = dto.ModifierApplicationOrder ?? new List<string>();
        JsonPointer orderPointer = root.AppendProperty("modifier_application_order");
        for (int index = 0; index < order.Count; index++)
        {
            SemanticCheck.Token(
                order[index], EliteModifierSchema.ModifierLayers,
                orderPointer.AppendIndex(index), context, id, bag);
        }

        SemanticCheck.Distinct(
            order, orderPointer, context, id, bag, "the modifier application layers");
        if (!structural.Reported(orderPointer))
        {
            SemanticCheck.ExactCount(
                order.Count, EliteModifierSchema.ModifierLayers.Tokens.Count, orderPointer,
                context, id, bag,
                "modifier_application_order lists every layer exactly once, so it is a total "
                    + "order over the closed layer vocabulary rather than a subset of it");
        }
    }
}
