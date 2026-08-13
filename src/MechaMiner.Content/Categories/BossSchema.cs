using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-boss</c>: the field table of one interval boss.
/// </summary>
/// <remarks>
/// <para>
/// A boss shares most of an enemy's statistics and none of its geometry rule, which is
/// the reason the two are separate tables rather than one with a flag.
/// </para>
/// <para>
/// <b>The enemy/boss geometry asymmetry, stated once.</b> An enemy authors
/// <c>body_scale_multiplier</c> and the compiler derives its contact diameter; a boss
/// authors <c>contact_and_weapon_hurt_diameter_m</c> directly, because no accepted
/// document gives a boss a body scale and there is nothing to scale it from. The
/// compiler derives only the boss's contact-begin centre distance, which is half the
/// authored diameter plus the player's collision radius - a cross-catalog constant, and
/// therefore exactly the kind of value doc 40 § Enemies and bosses says must not be
/// authored. Neither family stores a derived value; they differ in which value is the
/// authored one.
/// </para>
/// <para>
/// <b>No <c>armor</c>, on the same reading as the enemy table.</b> Four boss files
/// carried <c>armor: null</c>, which is not "no armour" - it is a field with nothing to
/// hold.
/// </para>
/// </remarks>
public static class BossSchema
{
    /// <summary>The four accepted boss ability kinds.</summary>
    public static ClosedVocabulary AbilityKinds { get; } = new(
        "a boss ability kind",
        "GDD-INITIAL-ALIEN-ROSTER",
        "straight-charge",
        "incomplete-minion-ring",
        "radial-projectile-burst",
        "locked-marker-leap");

    /// <summary>The fields every ability arm carries.</summary>
    public static IReadOnlyList<string> CommonAbilityFields { get; } =
        new ReadOnlyCollection<string>(new List<string>
        {
            "kind", "cadence_seconds", "defines_own_damage_event", "rules",
        });

    /// <summary>The arrival sub-shape.</summary>
    /// <remarks>
    /// The timecode is authored and the seconds into the run are derived from it; the
    /// warning lead is the schedule's, not the boss's, and lives on <c>WAV-01</c>.
    /// </remarks>
    public static DefinitionShape Arrival { get; } = DefinitionShape.Of(
        "a boss's arrival",
        DefinitionField.Text("timecode"));

    /// <summary>The defeat-reward sub-shape.</summary>
    public static DefinitionShape DefeatReward { get; } = DefinitionShape.Of(
        "a boss's defeat reward",
        DefinitionField.Integer("common_ore"),
        DefinitionField.Integer("specialized_material_units"),
        DefinitionField.Integer("unsecured_hyper_gold"),
        DefinitionField.Text("material_selection"),
        DefinitionField.Text("delivery"),
        DefinitionField.Flag("pauses_timer_or_opens_ui"));

    /// <summary>The persistence sub-shape.</summary>
    public static DefinitionShape Persistence { get; } = DefinitionShape.Of(
        "how a boss persists",
        DefinitionField.Flag("persists_until_killed"),
        DefinitionField.Flag("never_despawns"),
        DefinitionField.Flag("required_for_extraction"),
        DefinitionField.Flag("counts_toward_ordinary_population"),
        DefinitionField.Flag("later_bosses_may_overlap"),
        DefinitionField.Integer("maximum_simultaneous_bosses"),
        DefinitionField.Flag("disposed_without_reward_if_run_extracts_while_alive"),
        DefinitionField.Text("spawn_rule"),
        DefinitionField.Object("reentry", DefinitionShape.Of(
            "how a boss re-enters",
            DefinitionField.Text("trigger"),
            DefinitionField.Text("behavior"),
            DefinitionField.Flag("non_damaging_while_pending"),
            DefinitionField.Flag("ability_cooldown_restarts_after_reentry"))));

    /// <summary>The resonance sub-shape.</summary>
    public static DefinitionShape Resonance { get; } = DefinitionShape.Of(
        "how a boss interacts with resonance fields",
        DefinitionField.Flag("receives_same_geode_resonance_modifiers_as_ordinary_enemies"),
        DefinitionField.Text("control_resistance_combines_with_driftmetal"));

    /// <summary>
    /// The ability sub-shape: the four arms' fields as one table, with arm membership
    /// enforced against <c>kind</c> by the typed validator.
    /// </summary>
    /// <remarks>
    /// The structural table is the union of the arms because a field table walks a
    /// scanned shape, which carries locations and kinds and deliberately not values -
    /// so it cannot read the discriminator. The draft 2020-12 mirror expresses the same
    /// rule as a <c>oneOf</c> over four arms each pinning <c>kind</c> with
    /// <c>const</c>, which it can, and both reject the same documents. The two report
    /// it with different codes; the agreement gate compares verdicts, not codes.
    /// </remarks>
    public static DefinitionShape Ability { get; } = DefinitionShape.Of(
        "a boss ability",
        DefinitionField.Text("kind"),
        DefinitionField.Number("cadence_seconds"),
        DefinitionField.Flag("defines_own_damage_event"),

        // straight-charge
        DefinitionField.OptionalInteger("charge_contact_damage"),
        DefinitionField.OptionalNumber("charge_duration_seconds"),
        DefinitionField.OptionalNumber("stop_and_telegraph_duration_seconds"),
        DefinitionField.OptionalInteger("ordinary_contact_damage_replaced_during_charge"),
        DefinitionField.OptionalInteger("charge_speed_percent_of_mech_base"),
        DefinitionField.OptionalFlag("turns_during_charge"),

        // incomplete-minion-ring
        DefinitionField.OptionalNumber("pause_duration_seconds"),
        DefinitionField.OptionalInteger("ring_opening_degrees"),
        DefinitionField.OptionalInteger("spawn_count"),
        DefinitionField.OptionalText("spawn_enemy_id"),
        DefinitionField.OptionalFlag("spawn_drops_loot"),
        DefinitionField.OptionalFlag("spawn_remains_linked_to_boss"),

        // radial-projectile-burst
        DefinitionField.OptionalNumber("stop_and_charge_duration_seconds"),
        DefinitionField.OptionalInteger("projectile_count"),
        DefinitionField.OptionalInteger("radial_offset_alternation_degrees"),
        DefinitionField.OptionalObject("projectile", CombatShapes.Projectile),

        // locked-marker-leap
        DefinitionField.OptionalNumber("crouch_duration_seconds"),
        DefinitionField.OptionalInteger("landing_damage"),
        DefinitionField.OptionalText("marker_shape"),
        DefinitionField.OptionalFlag("marker_tracks_after_appearing"),
        DefinitionField.OptionalFlag("airborne_deals_contact_damage"),
        DefinitionField.OptionalFlag("airborne_remains_targetable"),
        DefinitionField.OptionalFlag("resumes_pursuit_immediately_after_landing"),

        DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The boss field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a boss definition",
        DefinitionField.Object("arrival", Arrival),
        DefinitionField.Integer("initial_hull"),
        DefinitionField.Integer("contact_damage"),
        DefinitionField.Integer("control_resistance_percent"),
        DefinitionField.Number("post_hard_control_immunity_seconds"),
        DefinitionField.Integer("movement_speed_percent_of_mech_base"),
        DefinitionField.Text("contact_shape"),
        DefinitionField.Number("contact_and_weapon_hurt_diameter_m"),
        DefinitionField.Text("behavior_kind"),
        DefinitionField.Object("ability", Ability),
        DefinitionField.Flag("ability_timeline_cancellable_by_player_control"),
        DefinitionField.Object("persistence", Persistence),
        DefinitionField.Object("resonance", Resonance),
        DefinitionField.Object("defeat_reward", DefeatReward),
        DefinitionField.Text("description"),
        DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The values the compiler derives for a boss.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.Nested(
            new[] { "contact_footprint", "center_distance_that_begins_contact_m" },
            "half the authored contact_and_weapon_hurt_diameter_m plus the player's collision "
                + "radius. The diameter is authored because no document gives a boss a body "
                + "scale; the centre distance is not, because it carries the player's radius",
            "/contact_and_weapon_hurt_diameter_m", "PLAYER-01"),
        DerivedField.Nested(
            new[] { "arrival", "active_seconds_into_run" },
            "arrival.timecode parsed into seconds",
            "/arrival/timecode"),
        DerivedField.Nested(
            new[] { "movement_speed", "world_speed_m_per_s" },
            "movement_speed_percent_of_mech_base as a fraction, multiplied by the player "
                + "baseline's movement speed",
            "/movement_speed_percent_of_mech_base", "PLAYER-01"),
        DerivedField.At(
            "damage_pressure",
            "hits to defeat is one hundred Hull divided by contact damage, rounded up, and the "
                + "overlap time is one fewer hit multiplied by the player's contact repeat "
                + "interval",
            "/contact_damage", "PLAYER-01"),
        DerivedField.At(
            "contact_cadence",
            "the player baseline's contact repeat interval and post-contact grace",
            "PLAYER-01"),
        DerivedField.At(
            "defining_behavior",
            "a prose rendering of ability.kind",
            "/ability/kind"),
        DerivedField.Nested(
            new[] { "ability", "resonant_damage_reference" },
            "the ability's base damage multiplied by the resonance modifier of the material "
                + "whose field the boss stands in",
            "/ability", "RSC-01", "RSC-03"),
    });

    /// <summary>The fields the arm named by <paramref name="kind"/> accepts.</summary>
    /// <remarks>
    /// Returns the arm's own parameters only; the common fields are added by the caller,
    /// so an arm table cannot forget one.
    /// </remarks>
    public static IReadOnlyList<string> ArmFields(string kind)
    {
        ArgumentNullException.ThrowIfNull(kind);

        return kind switch
        {
            "straight-charge" => new[]
            {
                "charge_contact_damage", "charge_duration_seconds",
                "stop_and_telegraph_duration_seconds",
                "ordinary_contact_damage_replaced_during_charge",
                "charge_speed_percent_of_mech_base", "turns_during_charge",
            },
            "incomplete-minion-ring" => new[]
            {
                "pause_duration_seconds", "ring_opening_degrees", "spawn_count",
                "spawn_enemy_id", "spawn_drops_loot", "spawn_remains_linked_to_boss",
            },
            "radial-projectile-burst" => new[]
            {
                "stop_and_charge_duration_seconds", "projectile_count",
                "radial_offset_alternation_degrees", "projectile",
            },
            "locked-marker-leap" => new[]
            {
                "crouch_duration_seconds", "landing_damage", "marker_shape",
                "marker_tracks_after_appearing", "airborne_deals_contact_damage",
                "airborne_remains_targetable", "resumes_pursuit_immediately_after_landing",
            },
            _ => Array.Empty<string>(),
        };
    }
}
