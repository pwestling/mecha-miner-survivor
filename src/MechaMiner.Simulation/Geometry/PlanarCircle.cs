using System;
using System.Globalization;

namespace MechaMiner.Simulation.Geometry;

/// <summary>
/// A circular gameplay footprint on the simulation plane: a centre and a radius in meters.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/21-world-geometry-navigation-and-spatial-queries.md</c> § Collision
/// primitives: "Player, enemies, bosses, rocks, pickups, and caches use circles with
/// gameplay-authored radii." This is that primitive. Its centre is the authoritative
/// ground-plane centre of
/// <c>docs/technical/decisions/TDR-005-simulate-gameplay-on-a-two-dimensional-plane.md</c>
/// § Coordinate contract, which "decorative model pivots and animation root motion never
/// modify".
/// </para>
/// <para>
/// doc 21 § Collision primitives also states that "decorative mesh bounds never substitute
/// for a gameplay primitive". Nothing in this type reads a mesh, a bounding box, or an
/// engine body, and nothing can: it is a pure value over two doubles and a radius.
/// </para>
/// <para>
/// <b>Overlap is inclusive and computed from squared distance.</b> doc 21 § Contact and
/// overlap: "Circle overlap uses squared distance and inclusive summed radii." Both halves
/// are load-bearing. Inclusive means tangency counts as contact, so a body that just
/// touches a hazard is in it rather than in an undefined gap. Squared distance means the
/// boundary case is decided without a square root, so exact tangency compares equal
/// instead of landing a rounding step either side of the threshold.
/// </para>
/// </remarks>
public readonly struct PlanarCircle : IEquatable<PlanarCircle>
{
    private readonly PlanarVector _centre;
    private readonly double _radius;

    private PlanarCircle(PlanarVector centre, double radius)
    {
        _centre = centre;
        _radius = radius;
    }

    /// <summary>The authoritative ground-plane centre.</summary>
    public PlanarVector Centre => _centre;

    /// <summary>The radius in gameplay meters. Never negative; may be zero.</summary>
    public double Radius => _radius;

    /// <summary>
    /// Creates a footprint.
    /// </summary>
    /// <param name="centre">The ground-plane centre.</param>
    /// <param name="radius">The radius in gameplay meters. Must be finite and nonnegative.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="radius"/> is not finite, or is negative.
    /// </exception>
    /// <remarks>
    /// A zero radius is accepted: doc 21 § Collision primitives gives projectiles circles,
    /// and a point projectile is the degenerate one. A negative radius is refused because
    /// it is not a smaller circle but an inverted overlap test, and every caller of
    /// <see cref="Overlaps"/> would silently get the opposite answer.
    /// </remarks>
    public static PlanarCircle FromCentreAndRadius(PlanarVector centre, double radius)
    {
        if (!double.IsFinite(radius))
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                radius,
                "a footprint radius is a finite number of gameplay meters");
        }

        if (radius < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(radius),
                radius,
                "a footprint radius is nonnegative; a negative radius is not a smaller circle but an "
                    + "inverted overlap test, and every caller would silently receive the opposite answer");
        }

        return new PlanarCircle(centre, radius);
    }

    /// <summary>
    /// Whether <paramref name="point"/> is inside or exactly on this footprint's boundary.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <remarks>
    /// Inclusive, per doc 21 § Contact and overlap. A point exactly at the radius is
    /// contained.
    /// </remarks>
    public bool Contains(PlanarVector point)
    {
        return _centre.DistanceSquaredTo(point) <= _radius * _radius;
    }

    /// <summary>
    /// Whether this footprint and <paramref name="other"/> touch or intersect.
    /// </summary>
    /// <param name="other">The other footprint.</param>
    /// <remarks>
    /// Inclusive summed radii over squared distance, per doc 21 § Contact and overlap: two
    /// footprints exactly at their summed radii overlap. This reports a candidate pair and
    /// nothing more - doc 21 § Contact and overlap puts cooldown, grace, Armor, and Hull
    /// changes in the damage system, so no caller may read a contact rule out of this
    /// answer.
    /// </remarks>
    public bool Overlaps(PlanarCircle other)
    {
        double summedRadii = _radius + other._radius;
        return _centre.DistanceSquaredTo(other._centre) <= summedRadii * summedRadii;
    }

    /// <summary>
    /// Whether the swept segment from <paramref name="from"/> to <paramref name="to"/> touches this
    /// circle.
    /// </summary>
    /// <param name="from">The segment's start.</param>
    /// <param name="to">The segment's end.</param>
    /// <remarks>
    /// <para>
    /// <c>docs/technical/10-runtime-architecture.md</c> § Entity and scene boundary: simulation-owned
    /// spatial queries are "suited to circular and swept-area tests". This is the swept half, and it
    /// exists because a point test on a moving body is wrong in two ways, one of which is already live.
    /// </para>
    /// <para>
    /// The live one is a blind interval at the muzzle: a projectile created at the mech's centre and
    /// first tested one step later can never hit a body overlapping the mech, which is exactly where a
    /// pure contact pursuer ends up, since doc 31:26 makes enemies non-solid and they walk through it.
    /// That is not a mis-hit, it makes the run unwinnable.
    /// </para>
    /// <para>
    /// The other is tunnelling, and the margin is worth stating rather than assuming: a Pulse Repeater
    /// projectile advances 0.267 m per tick against a 0.44 m Skitterling footprint, so the body is
    /// 1.65 times the step and cannot currently be tunnelled. The margin is a ratio, not a guarantee -
    /// it is consumed by any faster projectile or smaller body, and doc 71:76's Rail Lance already
    /// specifies a 30 m/s projectile, which is a 0.5 m step. <c>PlanarCircleTests</c> asserts the
    /// tunnelling case against a body small enough to demonstrate it and records the EN-01 ratio
    /// separately, so neither claim borrows the other's evidence.
    /// </para>
    /// <para>
    /// The test is the squared distance from the centre to the nearest point of the segment, compared
    /// inclusively against the squared radius - the same inclusive-and-squared convention
    /// <see cref="Contains"/> and <see cref="Overlaps"/> use, per doc 21:103. A degenerate segment
    /// whose endpoints coincide reduces to <see cref="Contains"/> rather than dividing by zero.
    /// </para>
    /// <para>
    /// Like <see cref="Overlaps"/>, this answers a geometric question and nothing more. It does not
    /// know what a projectile is, whether one may hit twice, or which of several touched bodies a
    /// weapon should pick.
    /// </para>
    /// </remarks>
    public bool OverlapsSegment(PlanarVector from, PlanarVector to)
    {
        PlanarVector along = to - from;
        double lengthSquared = along.MagnitudeSquared;
        if (lengthSquared == 0.0)
        {
            return Contains(from);
        }

        PlanarVector toCentre = _centre - from;
        double projection = ((toCentre.X * along.X) + (toCentre.Y * along.Y)) / lengthSquared;
        double clamped = Math.Clamp(projection, 0.0, 1.0);
        PlanarVector nearest = from + (along * clamped);
        return _centre.DistanceSquaredTo(nearest) <= _radius * _radius;
    }

    /// <summary>Returns this footprint moved to <paramref name="centre"/>, keeping its radius.</summary>
    /// <param name="centre">The new ground-plane centre.</param>
    public PlanarCircle MovedTo(PlanarVector centre)
    {
        return new PlanarCircle(centre, _radius);
    }

    /// <summary>Compares two footprints for exact equality of centre and radius.</summary>
    public static bool operator ==(PlanarCircle left, PlanarCircle right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two footprints for inequality.</summary>
    public static bool operator !=(PlanarCircle left, PlanarCircle right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(PlanarCircle other)
    {
        return _centre.Equals(other._centre) && _radius.Equals(other._radius);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PlanarCircle other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_centre, _radius);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "circle("
            + _centre.ToString()
            + ",r="
            + _radius.ToString("R", CultureInfo.InvariantCulture)
            + "m)";
    }
}
