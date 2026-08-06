using System;
using System.Globalization;

namespace MechaMiner.Simulation.Commands;

/// <summary>
/// A normalized planar movement intent: a direction whose magnitude is in <c>[0,1]</c>, where zero is an
/// explicit stop rather than a tiny direction.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Active commands: "Movement intent is normalized to a
/// planar vector with magnitude <c>[0,1]</c>; digital diagonals normalize to unit length. The simulation
/// applies immediate direction and full current speed for nonzero input and stops on zero input."
/// </para>
/// <para>
/// <b>The magnitude invariant is structural, not checked.</b> The components are private, the constructor
/// is private, and the only way to obtain a value is <see cref="TryNormalize"/> or
/// <see cref="Normalize"/>, both of which route through one normalization. So there is no
/// <see cref="MovementIntent"/> anywhere whose magnitude exceeds <see cref="MaximumMagnitude"/>: an
/// over-unit intent is not something to reject, it is something that cannot be constructed.
/// <c>default</c> is the explicit stop, which is a legal normalized value, so there is also no
/// "uninitialized" state to defend against.
/// </para>
/// <para>
/// <b>Full current speed on nonzero input.</b> The magnitude is the fraction of current speed the
/// movement phase applies, so an analog stick held part way yields a partial magnitude and every one of
/// the eight digital directions yields <see cref="MaximumMagnitude"/> - which is what "full current
/// speed for nonzero input" means for a digital control. <see cref="IsStop"/> is the zero case, and it is
/// exact rather than a threshold: doc 20 gives no deadzone, and inventing one here would put a gameplay
/// tuning value in a transport type.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): the input adapter in <c>game/</c> produces
/// <c>CTR-RUN-002</c> and reads back the normalized payload it was given; <c>MechaMiner.Game.Tests</c>
/// asserts on it. Hence <c>public</c>.
/// </para>
/// </remarks>
public readonly struct MovementIntent : IEquatable<MovementIntent>
{
    /// <summary>
    /// The largest magnitude a normalized intent may have, which doc 20 § Active commands fixes at the
    /// upper end of <c>[0,1]</c>.
    /// </summary>
    public const double MaximumMagnitude = 1.0;

    private readonly double _x;
    private readonly double _y;

    private MovementIntent(double x, double y)
    {
        _x = x;
        _y = y;
    }

    /// <summary>The explicit stop: zero input, which doc 20 § Active commands makes a command in its own right.</summary>
    public static MovementIntent Stop => default;

    /// <summary>
    /// The planar X component of the normalized intent, in the interval
    /// <c>[-<see cref="MaximumMagnitude"/>, <see cref="MaximumMagnitude"/>]</c>.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> this component and <see cref="Y"/> must be replaced by the single planar vector
    /// type that <c>W2-GEO</c> owns once <c>GEO-001</c> lands. The change is: delete both <c>double</c>
    /// components and the <c>rawX</c>/<c>rawY</c> parameters of <see cref="TryNormalize"/> and
    /// <see cref="Normalize"/>, replace them with one planar-vector parameter and one property, take the
    /// magnitude from that type instead of <see cref="double.Hypot(double,double)"/>, and update
    /// <see cref="ToString"/>. Do not introduce a planar vector type in this package - doc 20 § Numeric
    /// and unit conventions gives planar transforms to the geometry boundary, and a second vector type is
    /// what the directory split exists to prevent.
    /// </remarks>
    public double X => _x;

    /// <summary>
    /// The planar Y component of the normalized intent, in the interval
    /// <c>[-<see cref="MaximumMagnitude"/>, <see cref="MaximumMagnitude"/>]</c>.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> see <see cref="X"/>. Double rather than single precision because doc 20 § Numeric
    /// and unit conventions permits single for planar transforms only "after tests confirm the accepted
    /// map scale remains safely within precision bounds", and that confirmation does not exist.
    /// </remarks>
    public double Y => _y;

    /// <summary>
    /// The intent's magnitude, always in <c>[0, <see cref="MaximumMagnitude"/>]</c> up to the rounding of
    /// the normalizing division.
    /// </summary>
    /// <remarks>
    /// Computed with <see cref="double.Hypot(double,double)"/> rather than
    /// <c>Math.Sqrt(x * x + y * y)</c>: squaring a component near the ends of the <c>double</c> range
    /// overflows or underflows, and a magnitude that reads as infinity or zero for a perfectly finite
    /// nonzero input would break the invariant this property is here to report.
    /// </remarks>
    public double Magnitude => double.Hypot(_x, _y);

    /// <summary>Whether this is the explicit stop: both components exactly zero.</summary>
    public bool IsStop => _x == 0.0 && _y == 0.0;

    /// <summary>
    /// Normalizes a raw planar input into an intent, clamping an over-unit magnitude to
    /// <see cref="MaximumMagnitude"/> and mapping zero to <see cref="Stop"/>.
    /// </summary>
    /// <param name="rawX">The raw X component as sampled, of any finite magnitude.</param>
    /// <param name="rawY">The raw Y component as sampled, of any finite magnitude.</param>
    /// <param name="intent">The normalized intent, or <see cref="Stop"/> when this returns false.</param>
    /// <returns>
    /// <see langword="false"/> when a component is not a finite number, which is the only raw input that
    /// has no normalized meaning. Every finite pair normalizes.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The single normalization in this package. <see cref="Normalize"/> delegates here and the admission
    /// gate reaches it only through <see cref="CommandEnvelope.TryNormalizePayload"/>, so
    /// "normalized once, the same way, everywhere" is a property of there being one implementation rather
    /// than of callers agreeing.
    /// </para>
    /// <para>
    /// A digital diagonal is a raw <c>(±1, ±1)</c>, whose magnitude is <c>sqrt(2)</c> and therefore
    /// over-unit, so the clamp is exactly what makes doc 20's "digital diagonals normalize to unit
    /// length" happen - there is no separate digital path to keep in step with this one. Clamping divides
    /// by the magnitude rather than by <c>sqrt(2)</c>, so the result is unit length to within the
    /// rounding of one division, which is why <c>VER-SIM-004-005</c> asserts unit length within a named
    /// tolerance rather than exactly.
    /// </para>
    /// <para>
    /// The division is set up to be well conditioned at any scale: the components are first divided by
    /// the larger absolute value, so the intermediate magnitude lies in <c>[1, sqrt(2)]</c> and can
    /// neither overflow nor underflow. A raw <c>(1e308, 1e308)</c> therefore clamps to the same unit
    /// diagonal as a raw <c>(1, 1)</c>, instead of producing a NaN from an infinite magnitude.
    /// </para>
    /// </remarks>
    public static bool TryNormalize(double rawX, double rawY, out MovementIntent intent)
    {
        if (!double.IsFinite(rawX) || !double.IsFinite(rawY))
        {
            intent = default;
            return false;
        }

        double largestComponent = Math.Max(Math.Abs(rawX), Math.Abs(rawY));
        if (largestComponent == 0.0)
        {
            // doc 20 § Active commands: zero input is a stop, so it is the stop value rather than a
            // direction of arbitrary axis with a zero length.
            intent = default;
            return true;
        }

        double scaledX = rawX / largestComponent;
        double scaledY = rawY / largestComponent;
        double scaledMagnitude = double.Hypot(scaledX, scaledY);
        double magnitude = scaledMagnitude * largestComponent;

        if (magnitude <= MaximumMagnitude)
        {
            intent = new MovementIntent(rawX, rawY);
            return true;
        }

        intent = new MovementIntent(
            scaledX / scaledMagnitude * MaximumMagnitude,
            scaledY / scaledMagnitude * MaximumMagnitude);
        return true;
    }

    /// <summary>Normalizes a raw planar input, throwing rather than reporting an unnormalizable one.</summary>
    /// <param name="rawX">The raw X component. Must be finite.</param>
    /// <param name="rawY">The raw Y component. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException">A component is not a finite number.</exception>
    /// <remarks>
    /// For a caller that already knows its input is finite. The admission gate does not use this: an
    /// envelope crossing an asynchronous boundary carries whatever the producer sent, so a non-finite
    /// component there is a <see cref="CommandRejectionReason.InvalidPayload"/> rejection rather than an
    /// exception.
    /// </remarks>
    public static MovementIntent Normalize(double rawX, double rawY)
    {
        if (!TryNormalize(rawX, rawY, out MovementIntent intent))
        {
            throw new ArgumentOutOfRangeException(
                double.IsFinite(rawX) ? nameof(rawY) : nameof(rawX),
                double.IsFinite(rawX) ? rawY : rawX,
                "a movement input component must be a finite number; doc 20 § Active commands "
                    + "normalizes to a planar vector with magnitude [0,1] and neither NaN nor infinity "
                    + "has one");
        }

        return intent;
    }

    /// <summary>Compares two intents for exact equality of both components.</summary>
    public static bool operator ==(MovementIntent left, MovementIntent right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two intents for inequality.</summary>
    public static bool operator !=(MovementIntent left, MovementIntent right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(MovementIntent other)
    {
        return _x.Equals(other._x) && _y.Equals(other._y);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is MovementIntent other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_x, _y);
    }

    /// <summary>Renders the intent as canonical invariant text for goldens and diagnostics.</summary>
    /// <remarks>
    /// Round-trip format and invariant culture, because doc 91 § Determinism and fixture policy requires
    /// canonical text and a culture-dependent decimal separator is not canonical.
    /// </remarks>
    public override string ToString()
    {
        if (IsStop)
        {
            return "stop";
        }

        return "intent("
            + _x.ToString("R", CultureInfo.InvariantCulture)
            + ","
            + _y.ToString("R", CultureInfo.InvariantCulture)
            + ")";
    }
}
