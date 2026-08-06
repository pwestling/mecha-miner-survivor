using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-enemy</c>: the field table of one ordinary enemy.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Enemies and bosses:
/// "Fields include Hull, movement, contact damage/diameter/cadence, control
/// resistance, behavior registration, projectile or boss-ability parameters, elite
/// eligibility, presentation, spawn classification, and telemetry tags."
/// </para>
/// <para>
/// <b>There is no <c>armor</c> field, and its absence is a reading of doc 40 rather
/// than an oversight.</b> The enemy-and-boss field list above omits Armor; the mech
/// list, one section earlier, includes it - "base Hull/Armor/Recovery/movement/footprint
/// overrides". Armor is a mech statistic. Ten enemy files carried <c>armor: 0</c> and
/// four boss files carried <c>armor: null</c>, which is two different ways of saying
/// the field had nothing to hold.
/// </para>
/// <para>
/// <b>Contact cadence is not here either.</b> All three of its members were identical
/// across every enemy and every boss, and two of the three were the player baseline's
/// own constants copied verbatim. A constant copied into fourteen files is fourteen
/// writers on one value; <c>PLAYER-01</c> is the writer, and a survivability report
/// joins the two.
/// </para>
/// </remarks>
public static class EnemySchema
{
    /// <summary>How an enemy enters the world.</summary>
    /// <remarks>
    /// Doc 40 § Enemies and bosses requires a spawn classification and no enemy file
    /// carries one, so this vocabulary is assembled from the schedule's own director
    /// vocabulary, which is the only accepted document that distinguishes the three
    /// ways an enemy arrives. It is declared optional for that reason and becomes
    /// required when the roster states one per enemy.
    /// </remarks>
    public static ClosedVocabulary SpawnClassifications { get; } = new(
        "a spawn classification",
        "GDD-WAVE-SCHEDULE",
        "baseline-replenishment",
        "scheduled-formation",
        "event-overflow");

    /// <summary>The movement-collision sub-shape.</summary>
    /// <remarks>
    /// Four booleans, identical across all ten enemies today. They stay per-enemy
    /// rather than moving to a catalog aggregate because the mix - one true and three
    /// false - reads as configuration a variant could plausibly change, unlike the six
    /// all-false player-effect booleans that were removed as a negation sentence
    /// wearing a field's clothes.
    /// </remarks>
    public static DefinitionShape MovementCollision { get; } = DefinitionShape.Of(
        "an enemy's movement collision",
        DefinitionField.Flag("solid_to_mech"),
        DefinitionField.Flag("solid_to_other_enemies"),
        DefinitionField.Flag("solid_to_mining_points_and_pickups"),
        DefinitionField.Flag("constrained_by_solid_world_terrain"));

    /// <summary>The first-playable-subset sub-shape.</summary>
    public static DefinitionShape FirstPlayableSubset { get; } = DefinitionShape.Of(
        "an enemy's first-playable scoping",
        DefinitionField.Flag("included"),
        DefinitionField.OptionalText("temporary_substitute_enemy_id"));

    /// <summary>The specialist-attack sub-shape.</summary>
    /// <remarks>
    /// One arm today - the Needler's telegraphed straight projectile - declared as a
    /// discriminated union on <c>kind</c> so that a second arm is an added variant
    /// rather than a reshaped field. The boss abilities are the same concept with four
    /// arms and share the projectile sub-shape.
    /// </remarks>
    public static DefinitionShape SpecialistAttack { get; } = DefinitionShape.Of(
        "an enemy's specialist attack",
        DefinitionField.Text("kind"),
        DefinitionField.Number("cadence_seconds"),
        DefinitionField.Number("charge_duration_seconds"),
        DefinitionField.OptionalNumber("movement_speed_while_charging_multiplier"),
        DefinitionField.OptionalFlag("returns_to_full_pursuit_after_firing"),
        DefinitionField.OptionalObject("projectile", CombatShapes.Projectile),
        DefinitionField.OptionalArrayOf(
            "readability_requirements", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.OptionalArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The enemy field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "an enemy definition",
        DefinitionField.Text("family"),
        DefinitionField.OptionalText("variant_of"),
        DefinitionField.OptionalText("spawn_classification"),
        DefinitionField.Integer("earliest_minute"),
        DefinitionField.Object("first_playable_subset", FirstPlayableSubset),
        DefinitionField.Integer("hull"),
        DefinitionField.Integer("contact_damage"),
        DefinitionField.Integer("control_resistance_percent"),
        DefinitionField.Number("post_hard_control_immunity_seconds"),
        DefinitionField.Integer("movement_speed_percent_of_mech_base"),
        DefinitionField.Number("body_scale_multiplier"),
        DefinitionField.Text("contact_shape"),
        DefinitionField.Object("movement_collision", MovementCollision),
        DefinitionField.Text("behavior_kind"),
        DefinitionField.OptionalObject("specialist_attack", SpecialistAttack),
        DefinitionField.Flag("fixed_profile"),
        DefinitionField.Flag("scales_with_elapsed_time_or_player_state"),
        DefinitionField.Flag("elite_eligible"),
        DefinitionField.Object("drops", CombatShapes.Drops),
        DefinitionField.Text("description"));

    /// <summary>The values the compiler derives for an enemy.</summary>
    /// <remarks>
    /// This is the register doc 40 § Enemies and bosses is describing when it says "an
    /// enemy definition stores its authored <c>body_scale_multiplier</c>; the compiler
    /// derives the contact diameter from that multiplier and the reference diameter,
    /// and derives the contact-begin center distance from the result".
    /// </remarks>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.Nested(
            new[] { "contact_footprint", "contact_and_weapon_hurt_diameter_m" },
            "body_scale_multiplier multiplied by the enemy reference diameter",
            "/body_scale_multiplier", "PLAYER-01"),
        DerivedField.Nested(
            new[] { "contact_footprint", "center_distance_that_begins_contact_m" },
            "half the derived contact diameter plus the player's collision radius",
            "/body_scale_multiplier", "PLAYER-01"),
        DerivedField.Nested(
            new[] { "movement_speed", "world_speed_m_per_s" },
            "movement_speed_percent_of_mech_base as a fraction, multiplied by the player "
                + "baseline's movement speed",
            "/movement_speed_percent_of_mech_base", "PLAYER-01"),
        DerivedField.At(
            "damage_pressure",
            "hits to defeat is one hundred Hull divided by contact damage, rounded up, and the "
                + "overlap time is one fewer hit multiplied by the player's same-enemy contact "
                + "repeat interval",
            "/contact_damage", "PLAYER-01"),
        DerivedField.At(
            "contact_cadence",
            "the player baseline's contact repeat interval and post-contact grace, which are one "
                + "value each and not per-enemy",
            "PLAYER-01"),
    });
}
