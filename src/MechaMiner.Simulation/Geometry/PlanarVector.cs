using System;
using System.Globalization;

namespace MechaMiner.Simulation.Geometry;

/// <summary>
/// A displacement or position on the authoritative simulation plane, in gameplay meters.
/// </summary>
/// <remarks>
/// <para>
/// <c>CMP-GEO-001</c> geometry service in
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry.
/// <c>docs/technical/decisions/TDR-005-simulate-gameplay-on-a-two-dimensional-plane.md</c>
/// § Coordinate contract fixes what the two components mean: "Simulation X increases east
/// and simulation Y increases north." There is no third component, because there is no
/// authoritative third axis - height belongs entirely to presentation.
/// </para>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Numeric and unit conventions makes
/// "Position, distance, speed" into "floating-point values in gameplay meters on the
/// simulation plane", and this type is double precision throughout. That document permits
/// single precision for planar transforms "after tests confirm the accepted map scale
/// remains safely within precision bounds"; no such test exists, so the conservative
/// choice is the one taken here and narrowing it later is a measured change rather than a
/// free one.
/// </para>
/// <para>
/// <b>A component is never NaN or infinite.</b> Every entry point validates, because a
/// single non-finite component propagates silently through every later comparison: a NaN
/// position fails every bounds test without ever reporting that it failed one, and
/// <c>SnapshotPublisher.StagePlayer</c> would then reject the publication at phase 14 with
/// the origin of the corruption already many phases behind. Refusing at construction puts
/// the throw at the statement that produced the bad value.
/// </para>
/// <para>
/// Angles this type produces and consumes are radians counterclockwise from simulation
/// east, which is the ordinary <c>Math.Atan2(y, x)</c> convention. That is deliberately
/// <em>not</em> the player-facing bearing: doc 20 § Numeric and unit conventions puts
/// "degrees clockwise from north" at "display/content boundaries" only, so converting is
/// presentation's job and this type never does it.
/// </para>
/// </remarks>
public readonly struct PlanarVector : IEquatable<PlanarVector>
{
    private readonly double _x;
    private readonly double _y;

    private PlanarVector(double x, double y)
    {
        _x = x;
        _y = y;
    }

    /// <summary>The zero vector: the origin as a position, and no displacement as a delta.</summary>
    public static PlanarVector Zero => default;

    /// <summary>The unit vector pointing east, which is the zero-radian direction.</summary>
    public static PlanarVector East => new(1.0, 0.0);

    /// <summary>The unit vector pointing north.</summary>
    public static PlanarVector North => new(0.0, 1.0);

    /// <summary>The eastward component, in gameplay meters.</summary>
    public double X => _x;

    /// <summary>The northward component, in gameplay meters.</summary>
    public double Y => _y;

    /// <summary>
    /// The squared length, in squared gameplay meters.
    /// </summary>
    /// <remarks>
    /// Preferred over <see cref="Magnitude"/> wherever the comparison allows it, which is
    /// every distance threshold test: doc 21 § Contact and overlap requires circle overlap
    /// to use "squared distance and inclusive summed radii", so the square root is not
    /// merely an avoidable cost but an avoidable rounding step in a comparison whose
    /// boundary case is exact equality.
    /// </remarks>
    public double MagnitudeSquared => (_x * _x) + (_y * _y);

    /// <summary>The length, in gameplay meters.</summary>
    public double Magnitude => double.Hypot(_x, _y);

    /// <summary>Whether this is exactly the zero vector, which is an explicit zero direction.</summary>
    public bool IsZero => _x == 0.0 && _y == 0.0;

    /// <summary>Creates a vector from its eastward and northward components.</summary>
    /// <param name="x">The eastward component in gameplay meters. Must be finite.</param>
    /// <param name="y">The northward component in gameplay meters. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite.</exception>
    public static PlanarVector FromComponents(double x, double y)
    {
        RequireFinite(x, nameof(x));
        RequireFinite(y, nameof(y));
        return new PlanarVector(x, y);
    }

    /// <summary>
    /// Creates the unit vector at <paramref name="radians"/> counterclockwise from east.
    /// </summary>
    /// <param name="radians">The angle in radians. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="radians"/> is not finite.</exception>
    public static PlanarVector FromBearing(double radians)
    {
        RequireFinite(radians, nameof(radians));
        return new PlanarVector(Math.Cos(radians), Math.Sin(radians));
    }

    /// <summary>Adds two vectors componentwise.</summary>
    public static PlanarVector operator +(PlanarVector left, PlanarVector right)
    {
        return new PlanarVector(left._x + right._x, left._y + right._y);
    }

    /// <summary>Subtracts <paramref name="right"/> from <paramref name="left"/> componentwise.</summary>
    public static PlanarVector operator -(PlanarVector left, PlanarVector right)
    {
        return new PlanarVector(left._x - right._x, left._y - right._y);
    }

    /// <summary>Negates both components.</summary>
    public static PlanarVector operator -(PlanarVector value)
    {
        return new PlanarVector(-value._x, -value._y);
    }

    /// <summary>Scales a vector by a factor.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="factor"/> is not finite.</exception>
    public static PlanarVector operator *(PlanarVector value, double factor)
    {
        RequireFinite(factor, nameof(factor));
        return new PlanarVector(value._x * factor, value._y * factor);
    }

    /// <summary>Scales a vector by a factor.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="factor"/> is not finite.</exception>
    public static PlanarVector operator *(double factor, PlanarVector value)
    {
        return value * factor;
    }

    /// <summary>Adds two vectors componentwise.</summary>
    /// <param name="left">The left addend.</param>
    /// <param name="right">The right addend.</param>
    public static PlanarVector Add(PlanarVector left, PlanarVector right)
    {
        return left + right;
    }

    /// <summary>Subtracts <paramref name="right"/> from <paramref name="left"/> componentwise.</summary>
    /// <param name="left">The minuend.</param>
    /// <param name="right">The subtrahend.</param>
    public static PlanarVector Subtract(PlanarVector left, PlanarVector right)
    {
        return left - right;
    }

    /// <summary>Negates both components.</summary>
    /// <param name="value">The vector to negate.</param>
    public static PlanarVector Negate(PlanarVector value)
    {
        return -value;
    }

    /// <summary>Scales a vector by a factor.</summary>
    /// <param name="value">The vector to scale.</param>
    /// <param name="factor">The scale factor. Must be finite.</param>
    public static PlanarVector Multiply(PlanarVector value, double factor)
    {
        return value * factor;
    }

    /// <summary>The squared distance to <paramref name="other"/>, in squared gameplay meters.</summary>
    /// <param name="other">The other position.</param>
    public double DistanceSquaredTo(PlanarVector other)
    {
        return (this - other).MagnitudeSquared;
    }

    /// <summary>The distance to <paramref name="other"/>, in gameplay meters.</summary>
    /// <param name="other">The other position.</param>
    public double DistanceTo(PlanarVector other)
    {
        return (this - other).Magnitude;
    }

    /// <summary>
    /// The unit vector in this vector's direction, or <see cref="Zero"/> for the zero vector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// doc 20 § Numeric and unit conventions: a direction is a "normalized planar vector;
    /// <b>zero direction is explicit</b>". Returning zero rather than an arbitrary axis is
    /// that rule: a stopped body has no direction, and inventing east for it would make a
    /// released control indistinguishable from one held east.
    /// </para>
    /// <para>
    /// The components are scaled by the larger of their absolute values before the
    /// magnitude is taken, which is not a micro-optimization but a correctness fix.
    /// Dividing directly by <see cref="Magnitude"/> does not return a unit vector at the
    /// extremes of the range: for two subnormal components <c>double.Hypot</c> rounds to a
    /// subnormal, and <c>(epsilon, epsilon)</c> normalized that way yields <c>(1, 1)</c>,
    /// whose length is the square root of two. A direction one component of which is
    /// scaled to exactly one puts the magnitude in <c>[1, sqrt 2]</c>, where the division
    /// is well conditioned, so the unit-length guarantee holds across the whole range
    /// instead of only the middle of it. Movement integration multiplies this by a speed,
    /// so a direction longer than one is a body moving faster than its stat allows.
    /// </para>
    /// </remarks>
    public PlanarVector Normalized()
    {
        double largestComponent = Math.Max(Math.Abs(_x), Math.Abs(_y));
        if (largestComponent == 0.0)
        {
            return default;
        }

        double scaledX = _x / largestComponent;
        double scaledY = _y / largestComponent;
        double scaledMagnitude = double.Hypot(scaledX, scaledY);
        return new PlanarVector(scaledX / scaledMagnitude, scaledY / scaledMagnitude);
    }

    /// <summary>
    /// The direction's angle in radians counterclockwise from east, in <c>(-pi, pi]</c>.
    /// </summary>
    /// <remarks>
    /// The zero vector has no direction, so this returns zero for it rather than throwing.
    /// A caller that must distinguish "facing east" from "no direction" reads
    /// <see cref="IsZero"/>; every caller in the movement path already does, because
    /// preserving the last nonzero facing requires exactly that test.
    /// </remarks>
    public double BearingRadians()
    {
        if (IsZero)
        {
            return 0.0;
        }

        return Math.Atan2(_y, _x);
    }

    /// <summary>Compares two vectors for exact componentwise equality.</summary>
    public static bool operator ==(PlanarVector left, PlanarVector right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two vectors for componentwise inequality.</summary>
    public static bool operator !=(PlanarVector left, PlanarVector right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(PlanarVector other)
    {
        return _x.Equals(other._x) && _y.Equals(other._y);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PlanarVector other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_x, _y);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "("
            + _x.ToString("R", CultureInfo.InvariantCulture)
            + ","
            + _y.ToString("R", CultureInfo.InvariantCulture)
            + ")m";
    }

    private static void RequireFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "a planar component is a finite number of gameplay meters; neither NaN nor infinity "
                    + "is a position on the simulation plane, and admitting one would fail every later "
                    + "bounds comparison without reporting that it had failed one");
        }
    }
}
