namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-player-baseline</c>: the field table of <c>PLAYER-01</c>.
/// </summary>
/// <remarks>
/// <para>
/// The player baseline is the denominator of most of the tree. Every enemy and boss
/// movement percentage resolves against its speed, every derived contact centre
/// distance carries its collision radius, and the five reference percentages are the
/// hundred-percent point that every mech trait, utility, and PowerUp modifies. Before
/// it existed as a definition, fourteen combat files carried copies of two of its
/// constants and nothing compared the copies with each other.
/// </para>
/// <para>
/// It is a contract rather than an embodied definition, so it omits <c>name_key</c> and
/// <c>presentation_id</c> on the terms doc 40 § Declared-optional envelope fields sets
/// for an aggregate.
/// </para>
/// <para>
/// <b>There is no post-hit invulnerability field.</b> The survivability document states
/// it as "None" with no duration. Authoring zero would claim a zero-second
/// invulnerability window, which is a different claim from there being no such
/// mechanic, and only the second is stated. The named immunity that does exist is
/// <c>post_hard_control_immunity_seconds</c>, and it lives on the combatants that have
/// one.
/// </para>
/// </remarks>
public static class PlayerBaselineSchema
{
    /// <summary>The player baseline field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "the player baseline",
        DefinitionField.Integer("maximum_hull_integrity"),
        DefinitionField.Integer("armor"),
        DefinitionField.Number("recovery_hull_per_second"),
        DefinitionField.Integer("revival_charges"),
        DefinitionField.Number("movement_speed_m_per_s"),
        DefinitionField.Number("collision_diameter_m"),
        DefinitionField.Text("collision_shape"),
        DefinitionField.Integer("mining_extraction_rate_percent"),
        DefinitionField.Integer("weapon_damage_percent"),
        DefinitionField.Integer("weapon_attack_rate_percent"),
        DefinitionField.Integer("weapon_area_percent"),
        DefinitionField.Flag("starting_hull_is_current_maximum"),
        DefinitionField.Number("same_enemy_contact_repeat_interval_seconds"),
        DefinitionField.Number("global_contact_grace_after_resolved_contact_seconds"),
        DefinitionField.Number("enemy_body_scale_reference_diameter_m"));

    /// <summary>The values the compiler derives for the player baseline.</summary>
    /// <remarks>
    /// <c>movement_speed_percent</c> is the clearest case in the tree: it is one hundred
    /// by definition of being the baseline, so authoring it states a tautology that a
    /// later edit could make false.
    /// </remarks>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "movement_speed_percent",
            "one hundred by definition: the baseline is the denominator of every movement "
                + "percentage, so its own percentage is not an independent value"),
        DerivedField.At(
            "collision_radius_m",
            "half of collision_diameter_m",
            "/collision_diameter_m"),
        DerivedField.At(
            "passive_recovery_hull_per_second",
            "a second name for recovery_hull_per_second, carried only to preserve a source table's "
                + "row name",
            "/recovery_hull_per_second"),
        DerivedField.At(
            "health_pack_repair_hull",
            "a health pack is a world prop, so the map generation contract is its writer",
            "MGC-01"),
    });
}
