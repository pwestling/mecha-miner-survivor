using System;
using MechaMiner.Simulation.Encounters;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Random;

namespace MechaMiner.Simulation.World;

/// <summary>
/// Where the graybox run's mining sites are, and how many there are.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every number here is graybox with no source claimed, and it is a separate type so that is
/// visible.</b> <c>docs/40-mining-and-extraction.md</c>:66 does author a count - "Each standard map
/// contains 20 standard seams at randomized locations" - but it authors it for a <em>standard map</em>,
/// and this is not one. <c>GrayboxArenaBounds</c> is a 40 m square of scaffolding that <c>MAP-007</c>
/// replaces with a validated geometry manifest; placing twenty authored seams in it would be a claim
/// that the arena is a standard map, which is the one thing its own documentation insists it is not.
/// </para>
/// <para>
/// <b>The bearings are drawn from a registered stream, and that part is not graybox.</b>
/// <c>RandomStreamFamilies.StandardSeamPlacement</c> (family <c>0x0210</c>, "standard-seam placement")
/// exists in the registry for exactly this draw, and doc 20 § Authoritative random-number contract
/// requires a registered family rather than one invented at a call site. So two runs of the same seed
/// place the same seams, which is half of what makes a transcript comparable, and the number nobody
/// authored is only the ring the seams sit on.
/// </para>
/// </remarks>
public static class GrayboxRunLayout
{
    /// <summary>How many standard ore seams the graybox arena carries: <c>3</c>. <b>Graybox.</b></summary>
    /// <remarks>
    /// Three rather than one, because one site cannot exercise the case doc 40:169 lists as open -
    /// "The player is within range of more than one mining point" - and a HUD that shows one site's
    /// progress has to choose between sites before that choice can be wrong. Three rather than twenty,
    /// for the reason on the type.
    /// </remarks>
    public const int MiningSiteCount = 3;

    /// <summary>
    /// The radius of the ring the seams are placed on, in gameplay meters: <c>6.0</c>. <b>Graybox.</b>
    /// </summary>
    /// <remarks>
    /// Two base-travel seconds from the deployment position at the 3.0 m/s baseline speed of doc 72:39,
    /// so a mech can reach the nearest seam quickly enough that a short capture shows drilling, and well
    /// inside the camera's 12 m vertical half-extent so all three are visible at once. Those are reasons
    /// the value is convenient, not derivations of it.
    /// </remarks>
    public const double MiningSiteRingRadiusMeters = 6.0;

    /// <summary>
    /// Draws the seam positions for a run.
    /// </summary>
    /// <param name="streams">The run's authoritative stream set.</param>
    /// <param name="centre">The arena centre the ring is measured from.</param>
    /// <returns>Exactly <see cref="MiningSiteCount"/> positions, in draw order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="streams"/> is null.</exception>
    /// <remarks>
    /// One draw per seam, in a fixed order, from the seam-placement stream's zero instance key -
    /// <c>InstanceKeyRule.Zero</c> is what the registry assigns that family. Positions are returned
    /// rather than applied so the caller decides what to do with them and this stays testable without a
    /// world.
    /// </remarks>
    public static PlanarVector[] DrawMiningSitePositions(RandomStreamSet streams, PlanarVector centre)
    {
        ArgumentNullException.ThrowIfNull(streams);

        RandomStreamKey key = RandomStreamKey.Create(RandomStreamFamilies.StandardSeamPlacement, 0UL);
        PlanarVector[] positions = new PlanarVector[MiningSiteCount];
        for (int index = 0; index < positions.Length; index++)
        {
            double bearing = streams.NextUnitDouble(key) * Math.Tau;
            positions[index] = centre + (PlanarVector.FromBearing(bearing) * MiningSiteRingRadiusMeters);
        }

        return positions;
    }

    /// <summary>
    /// The spawn ring the graybox arena admits for the roster's baseline enemy.
    /// </summary>
    /// <param name="centre">The arena centre.</param>
    /// <param name="halfExtentMeters">The arena's half extent.</param>
    /// <remarks>
    /// Derived from the arena and the Skitterling's own footprint by
    /// <see cref="GrayboxSpawnRing.LargestFittingRadius"/>. See that type for why "outside the active
    /// camera" - doc 32:23's actual rule - is not satisfiable in a 40 m arena under a camera 42.7 m wide.
    /// </remarks>
    public static GrayboxSpawnRing SpawnRing(PlanarVector centre, double halfExtentMeters)
    {
        return new GrayboxSpawnRing(
            centre,
            GrayboxSpawnRing.LargestFittingRadius(
                halfExtentMeters,
                EnemyRoster.Skitterling.ContactRadiusMeters));
    }
}
