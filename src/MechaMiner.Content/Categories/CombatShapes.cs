using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// The sub-shapes the enemy and boss field tables share.
/// </summary>
/// <remarks>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Enemies and bosses names
/// "projectile or boss-ability parameters" as one field group, and the two projectiles
/// in the tree - the Needler's specialist attack and one boss's radial burst - carry
/// the same members. One shape rather than two is what stops them drifting into two
/// spellings of one concept, which is what happened to the same catalog's contact
/// cadence block before it was removed.
/// </remarks>
public static class CombatShapes
{
    /// <summary>The shape of a contact footprint, which is a circle in every case today.</summary>
    /// <remarks>
    /// Declared as a token rather than fixed to one value: a non-circular footprint is
    /// expressible without a schema change, and the closed vocabulary means adding one
    /// is a deliberate edit rather than a free-text value nobody notices.
    /// </remarks>
    public static ClosedVocabulary ContactShapes { get; } = new(
        "a contact footprint shape",
        "GDD-PLAYER-BASELINE",
        "circle");

    /// <summary>
    /// What a projectile samples when it is created rather than reading live.
    /// </summary>
    /// <remarks>
    /// Doc 40 § Weapons asks for "snapshot/live classifications". This is the only
    /// place in the tree where the distinction is actually authored, so the vocabulary
    /// is built from it; the weapon and branch halves of that pair are a documented gap.
    /// </remarks>
    public static ClosedVocabulary SnapshotProperties { get; } = new(
        "a snapshot-at-creation property",
        "GDD-INITIAL-ALIEN-ROSTER",
        "speed",
        "damage",
        "lifetime",
        "terrain-collision",
        "no-homing");

    /// <summary>The projectile sub-shape shared by enemy specialist attacks and boss abilities.</summary>
    /// <remarks>
    /// The speed is authored as a percentage of the mech's base speed and never as a
    /// world speed: the world speed is that percentage times the player baseline speed,
    /// which the compiler derives. <c>lifetime_seconds</c> is deliberately absent -
    /// both projectiles state their lifetime only as prose ("carries it slightly beyond
    /// one screen width"), and authoring a number no document states would be inventing
    /// a balance value.
    /// </remarks>
    public static DefinitionShape Projectile { get; } = DefinitionShape.Of(
        "a projectile",
        DefinitionField.Integer("damage"),
        DefinitionField.Integer("speed_percent_of_mech_base"),
        DefinitionField.Flag("homing"),
        DefinitionField.Flag("leads_target"),
        DefinitionField.Flag("retargets"),
        DefinitionField.Flag("splits"),
        DefinitionField.Flag("explodes"),
        DefinitionField.Flag("leaves_hazard"),
        DefinitionField.Flag("applies_status"),
        DefinitionField.ArrayOf("snapshot_at_creation", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The drop table shared by every combatant.</summary>
    public static DefinitionShape Drops { get; } = DefinitionShape.Of(
        "a defeat drop table",
        DefinitionField.Integer("xp"),
        DefinitionField.Integer("common_ore"),
        DefinitionField.Integer("specialized_material_units"),
        DefinitionField.Integer("hyper_gold_units"),
        DefinitionField.Integer("repair_pickups"),
        DefinitionField.Integer("temporary_pickups"));
}
