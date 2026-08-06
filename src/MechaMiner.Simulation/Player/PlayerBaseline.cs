namespace MechaMiner.Simulation.Player;

/// <summary>
/// The shared player baseline, hardcoded. <b>Every value here is owed to the typed content
/// layer.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> makes these values authored content
/// rather than code, and <c>DAT-006</c> / <c>CMP-CNT-002</c> load them. Neither exists on this
/// ref, so they are stated here, in one file, so that the later data-driven pass has a
/// checklist rather than a search. Each constant carries the source file and the document line
/// it was read from. When the content layer lands, this type is deleted and not merely
/// repointed - a constant that survives alongside a loaded definition is a second authority.
/// </para>
/// <para>
/// The source is <c>docs/72-player-survivability-and-damage-baseline.md</c> § Shared Player
/// Baseline, a table at lines 32-46 of that file at the commit this was written against. Line
/// numbers are given per constant and are a convenience for the porting pass, not an identity:
/// the section heading is what actually resolves.
/// </para>
/// <para>
/// What is deliberately <b>not</b> here, because this slice does not implement it and a
/// constant nothing reads is a claim nothing checks: Armor, revival charges, the same-enemy
/// contact repeat interval, the global contact grace, and health-pack repair. Those belong to
/// the damage half of <c>PLY-001</c> and to <c>COM-003</c>, and each is in the same table.
/// </para>
/// </remarks>
public static class PlayerBaseline
{
    /// <summary>
    /// Maximum Hull Integrity: <c>100</c>.
    /// </summary>
    /// <remarks>
    /// docs/72-player-survivability-and-damage-baseline.md:34
    /// "| Maximum Hull Integrity | 100 |".
    /// Integral because doc 20 § Numeric and unit conventions represents "Hull, Armor,
    /// resources, ranks, counts" as "signed or unsigned integers with checked conversion and
    /// validated nonnegative domain".
    /// </remarks>
    public const int MaximumHull = 100;

    /// <summary>
    /// Starting Hull Integrity: the current maximum.
    /// </summary>
    /// <remarks>
    /// docs/72-player-survivability-and-damage-baseline.md:35
    /// "| Starting Hull Integrity | Current maximum |".
    /// Expressed as an alias of <see cref="MaximumHull"/> rather than as a second literal
    /// <c>100</c>, because the table states a relationship and two independent literals would
    /// let a later maximum change leave the starting value behind.
    /// </remarks>
    public const int StartingHull = MaximumHull;

    /// <summary>
    /// Passive Recovery: <c>0</c> Hull per second.
    /// </summary>
    /// <remarks>
    /// docs/72-player-survivability-and-damage-baseline.md:37
    /// "| Passive Recovery | 0 Hull/s |".
    /// Stated as a constant rather than omitted, because zero passive recovery is a design
    /// position the same document argues for at line 26 - "Recovery is uncertain and
    /// exploration-driven unless the player invests in explicit passive Recovery" - and an
    /// absent constant would read as an unimplemented feature rather than as the accepted
    /// value.
    /// </remarks>
    public const double PassiveRecoveryHullPerSecond = 0.0;

    /// <summary>
    /// Base movement speed: <c>3.0</c> gameplay meters per second.
    /// </summary>
    /// <remarks>
    /// <para>
    /// docs/72-player-survivability-and-damage-baseline.md:39
    /// "| Base movement speed | 3.0M/s |", where the same file at line 44 defines "<c>M</c> is
    /// one unmodified mech collision diameter" and states "One base-travel second therefore
    /// equals 3.0M of shortest-path travel". Since <see cref="CollisionDiameterMeters"/> is
    /// exactly 1.0 m, 3.0 M/s is 3.0 m/s and the two units coincide numerically.
    /// </para>
    /// <para>
    /// They coincide only because the diameter is 1.0. Content that authors a speed in
    /// <c>M</c> must multiply by the diameter; this constant is already in meters per second
    /// and must not be multiplied again.
    /// </para>
    /// </remarks>
    public const double BaseMovementSpeedMetersPerSecond = 3.0;

    /// <summary>
    /// Mech collision diameter: <c>1.0</c> gameplay meter, and the shape is a circle.
    /// </summary>
    /// <remarks>
    /// docs/72-player-survivability-and-damage-baseline.md:40
    /// "| Mech collision diameter | 1.0M |" and :41 "| Mech collision shape | Circle |".
    /// The same file at line 48 confines what may change it: "Decorative limbs, weapons,
    /// shadows, antennae, and effects never enlarge it."
    /// </remarks>
    public const double CollisionDiameterMeters = 1.0;

    /// <summary>
    /// Half of <see cref="CollisionDiameterMeters"/>, which is what every geometry call wants.
    /// </summary>
    /// <remarks>
    /// Derived rather than authored. The document states a diameter and every collision
    /// primitive takes a radius, so the division happens exactly once, here, instead of at each
    /// call site where half of them would eventually forget it.
    /// </remarks>
    public const double CollisionRadiusMeters = CollisionDiameterMeters / 2.0;

    /// <summary>
    /// The facing before any input has been received: simulation east, which is zero radians.
    /// </summary>
    /// <remarks>
    /// docs/30-combat-weapons-movement-camera.md:70
    /// "Before the first input, the mech faces east - screen-right on the fixed north-up
    /// camera - so facing-based starting weapons have a deterministic initial attack."
    /// Zero radians is east under this assembly's internal convention, which is radians
    /// counterclockwise from east; see <c>PlanarVector</c>'s remarks for why that is not the
    /// player-facing bearing.
    /// </remarks>
    public const double InitialFacingRadians = 0.0;
}
