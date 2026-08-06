using System;
using System.Globalization;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Random;

namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// Where a materialized enemy enters: a bearing drawn from an authoritative stream, on a ring around
/// the arena centre.
/// </summary>
/// <remarks>
/// <para>
/// <b>Graybox scaffolding, and the reason it cannot be the documented rule is measured.</b>
/// <c>docs/32-standard-wave-and-beacon-schedule.md</c>:23 - "Ordinary baseline and event enemies
/// spawn on valid navigable ground outside the active camera." Two of those three clauses have no
/// implementation available here. Navigable ground is <c>GEO-005</c>/<c>MAP-007</c> and does not
/// exist; the only bound on this ref is <c>GrayboxArenaBounds</c>, which is scaffolding.
/// </para>
/// <para>
/// <b>"Outside the active camera" is not satisfiable in the graybox arena at all, and the arithmetic
/// is worth writing down rather than discovering.</b> The gameplay camera shows 24 m vertically
/// (<c>docs/technical/30-presentation-and-rendering.md</c>:53) and about 42.7 m horizontally at 16:9,
/// so its half-diagonal is <c>hypot(21.35, 12)</c> = 24.5 m. The graybox arena's half extent is
/// 20 m. Every point of the arena is therefore within 28.3 m of the centre and every point on any
/// ring the arena admits is inside the camera's horizontal half-extent for bearings near east and
/// west. A ring at 19.6 m is outside the camera for bearings within about 56 degrees of north or
/// south and inside it elsewhere. So this ring honours doc 32:23 for part of its circumference and
/// cannot for the rest, and that is reported rather than papered over: the fix is a map larger than
/// the camera, which is <c>MAP-007</c>'s.
/// </para>
/// <para>
/// The consolation is that a visible entrance is better graybox evidence than a correct one would
/// be. A frame showing a Skitterling walk in from the arena edge is a frame someone can look at.
/// </para>
/// </remarks>
public sealed class GrayboxSpawnRing
{
    private readonly PlanarVector _centre;
    private readonly double _radiusMeters;

    /// <summary>
    /// Builds a ring.
    /// </summary>
    /// <param name="centre">The ring's centre in gameplay meters.</param>
    /// <param name="radiusMeters">The ring's radius. Must be finite and positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radiusMeters"/> is outside its domain.</exception>
    public GrayboxSpawnRing(PlanarVector centre, double radiusMeters)
    {
        if (!double.IsFinite(radiusMeters) || radiusMeters <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radiusMeters),
                radiusMeters,
                "a spawn ring has a finite positive radius; a degenerate ring would materialize every "
                    + "enemy on top of the mech, which is an entrance nobody could read");
        }

        _centre = centre;
        _radiusMeters = radiusMeters;
    }

    /// <summary>The ring's centre.</summary>
    public PlanarVector Centre => _centre;

    /// <summary>The ring's radius in gameplay meters.</summary>
    public double RadiusMeters => _radiusMeters;

    /// <summary>
    /// The largest ring a square arena of half extent <paramref name="halfExtentMeters"/> admits for a
    /// body of radius <paramref name="bodyRadiusMeters"/>.
    /// </summary>
    /// <param name="halfExtentMeters">The arena's half extent.</param>
    /// <param name="bodyRadiusMeters">The entering body's footprint radius.</param>
    /// <remarks>
    /// The arena's inscribed circle, inset by the body's own radius so the whole footprint is inside
    /// the legal region at the moment it materializes. Derived rather than chosen: doc 40 and doc 32
    /// state no spawn distance, and the alternative to deriving it from the two lengths that do exist
    /// would be inventing a third.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The body does not fit inside the arena, so there is no ring.
    /// </exception>
    public static double LargestFittingRadius(double halfExtentMeters, double bodyRadiusMeters)
    {
        double radius = halfExtentMeters - bodyRadiusMeters;
        if (!double.IsFinite(radius) || radius <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bodyRadiusMeters),
                bodyRadiusMeters,
                "a body of this radius does not fit inside an arena of half extent "
                    + halfExtentMeters.ToString("R", CultureInfo.InvariantCulture)
                    + " m, so there is no ring it can enter on");
        }

        return radius;
    }

    /// <summary>
    /// Draws one entrance position from <paramref name="streams"/>.
    /// </summary>
    /// <param name="streams">The run's authoritative stream set.</param>
    /// <param name="key">The stream key to draw the bearing from.</param>
    /// <returns>A position on the ring.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="streams"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// <b>Exactly one draw per enemy, from one named stream.</b> That is what makes a same-seed run
    /// byte-identical: the number of draws is a function of the number of enemies materialized, which
    /// is a function of committed ticks and deaths, all of which are themselves deterministic. A
    /// second conditional draw - a variant roll taken only sometimes, say - would make the stream
    /// position depend on a branch, and two runs that diverged for one tick would then never
    /// reconverge.
    /// </para>
    /// <para>
    /// The bearing is <c>NextUnitDouble</c> scaled by a full turn, so it is uniform over the ring.
    /// <c>BoundedRandom.NextUnitDouble</c> is the repository's own conversion and is golden-vector
    /// tested; a hand-rolled <c>uint</c>-to-angle division here would be a second, unversioned copy
    /// of a derivation doc 20 § Authoritative random-number contract versions deliberately.
    /// </para>
    /// </remarks>
    public PlanarVector DrawEntrance(RandomStreamSet streams, RandomStreamKey key)
    {
        ArgumentNullException.ThrowIfNull(streams);

        double bearing = streams.NextUnitDouble(key) * Math.Tau;
        return _centre + (PlanarVector.FromBearing(bearing) * _radiusMeters);
    }
}
