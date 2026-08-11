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
