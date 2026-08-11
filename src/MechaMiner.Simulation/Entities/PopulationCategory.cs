namespace MechaMiner.Simulation.Entities;

/// <summary>
/// The twelve authoritative population categories, one purpose-built packed store
/// each.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative population categories
/// tabulates exactly these twelve, in this order. Declaration order is that table's
/// row order and is authoritative: <see cref="StoreCapacities.Categories"/> iterates
/// it, and <c>EntityIdAllocator</c> partitions the run's slot space by it, so a
/// reordering would silently move every category's slot range.
/// </para>
/// <para>
/// There is no thirteenth member. A new category is an edit to doc 20 plus a new
/// verification-registry entry, never an unregistered store
/// (<c>tests/verification/SIM-003.json</c> notes).
/// </para>
/// <para>
/// doc 20 § Entity identity: "The implementation uses purpose-built packed stores by
/// population category, not a general reflection-driven ECS framework."
/// </para>
/// <para>
/// Public because <c>CTR-SIM-003</c>'s snapshot names a category per visible entity and
/// its registered consumers - <c>CMP-PRE-001</c>, <c>CMP-UI-001</c>,
/// <c>CMP-AUD-001</c> in doc 115 § Cross-boundary contract registry - are outside this
/// assembly.
/// </para>
/// </remarks>
public enum PopulationCategory
{
    /// <summary>
    /// Transform, facing, movement, Hull, modifiers, loadout, run inventory, contact
    /// grace; run lifetime.
    /// </summary>
    /// <remarks>
    /// Zero-valued deliberately, so that the enum's default is a real category rather
    /// than an undefined one, and so the reserved player slot is index zero.
    /// </remarks>
    Player = 0,

    /// <summary>
    /// Definition, transform, motion, Hull, contact cooldown, control state, spawn tags;
    /// spawn to death, recycle, or run end.
    /// </summary>
    OrdinaryEnemy = 1,

    /// <summary>
    /// Ordinary state plus elite modifiers and marker; event or beacon spawn to death or
    /// run end.
    /// </summary>
    Elite = 2,

    /// <summary>
    /// Definition, transform, Hull, behavior state machine, contact state, re-entry
    /// state; scheduled arrival to death or run end.
    /// </summary>
    Boss = 3,

    /// <summary>
    /// Origin identity, transform, velocity, damage snapshot, lifetime, collision flags;
    /// fire to impact, terrain, or expiry.
    /// </summary>
    EnemyProjectile = 4,

    /// <summary>
    /// Weapon provenance, owner slot, transform, timing, branch/relic snapshot or live
    /// modifier policy; attack-specific.
    /// </summary>
    /// <remarks>
    /// doc 20 § Authoritative population categories: weapon actors "cover projectiles,
    /// beams, mines, pods, drones, orbiters, trails, delayed echoes, and other attack
    /// state" and "use specialized packed stores when their update pattern differs
    /// materially".
    /// </remarks>
    WeaponActor = 5,

    /// <summary>
    /// Geometry, provenance, tick policy, affected-target memory, expiry;
    /// attack-specific.
    /// </summary>
    DamageZone = 6,

    /// <summary>
    /// Class, position, zone, progress, checkpoint state, completion, beacon thresholds;
    /// map lifetime.
    /// </summary>
    MiningSite = 7,

    /// <summary>
    /// Resource kind, amount, position, collection radius, provenance; spawn to
    /// collection or run end.
    /// </summary>
    Pickup = 8,

    /// <summary>Position, Hull, footprint, drop-roll state; spawn to destruction or recycle.</summary>
    DestructibleRock = 9,

    /// <summary>Position, assigned relic, discovery and open state; map lifetime.</summary>
    RelicCache = 10,

    /// <summary>Stable map ID, geometry and presentation references; map lifetime.</summary>
    StaticWorldObject = 11,
}
