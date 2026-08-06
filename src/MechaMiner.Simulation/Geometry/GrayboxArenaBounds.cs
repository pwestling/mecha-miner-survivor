using System;
using System.Globalization;

namespace MechaMiner.Simulation.Geometry;

/// <summary>
/// A hardcoded rectangular arena with no obstacles. <b>Graybox scaffolding that MAP-007
/// replaces.</b>
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not a map, and it must never be mistaken for one.</b> A real map is the
/// immutable, checksummed, fully validated geometry manifest of
/// <c>docs/technical/21-world-geometry-navigation-and-spatial-queries.md</c> § Static
/// geometry manifest, carrying an outer traversable boundary, obstacle polygons, walkable
/// connected components, clearance, region adjacency, connector centrelines, landmark
/// footprints, exclusion zones, a navigation raster, and stable generated IDs. This type has
/// four numbers. <c>docs/technical/delivery-waves.md</c> § W3-MAP assigns the manifest to
/// <c>MAP-007</c>, and this type exists only so that movement integration has something to
/// enforce against before <c>MAP-007</c> lands. It sits behind
/// <see cref="IPlanarBounds"/> precisely so that replacing it is a composition change and
/// not a movement change.
/// </para>
/// <para>
/// What it deliberately does not do, so that nobody mistakes its behaviour for the real
/// contract: no obstacles, no swept resolution, no slide along a tangent, no clearance, no
/// connectivity, no navigation, and no correction of an already-penetrating body toward the
/// nearest validated free point. doc 21 § Player and enemy movement requires the earliest
/// contact and a tangent slide with two iterations; a rectangle resolves per axis
/// independently, which happens to give the correct sliding behaviour along a wall for the
/// axis-aligned case only, and gives it for the wrong reason.
/// </para>
/// <para>
/// The clamp is applied to the body's whole footprint rather than to its centre. A centre
/// clamp would let half the collision circle leave the arena, which is the more plausible
/// wrong answer: it looks correct while the body is small relative to the arena and is
/// visibly wrong at the corners.
/// </para>
/// <para>
/// The arena's own extent is a presentation-and-readability choice with no authoritative
/// source, so it is stated here rather than cited. See
/// <see cref="DefaultHalfExtentMeters"/>.
/// </para>
/// </remarks>
public sealed class GrayboxArenaBounds : IPlanarBounds
{
    /// <summary>
    /// Half the arena's side length in gameplay meters, giving a 40 m by 40 m square centred
    /// on the origin.
    /// </summary>
    /// <remarks>
    /// <b>No document specifies this number and none is cited for it.</b> It is chosen so the
    /// arena is comfortably larger than the camera's viewport - the camera shows 24 m
    /// vertically and about 42.7 m horizontally at 16:9
    /// (<c>docs/technical/30-presentation-and-rendering.md</c> § Camera) - so that a player
    /// can see the mech move relative to the ground and still reach a wall in a few seconds
    /// at the 3.0 m/s base speed. It is graybox scaffolding, not a tuned value, and
    /// <c>MAP-007</c> deletes it rather than replacing its number.
    /// </remarks>
    public const double DefaultHalfExtentMeters = 20.0;

    private readonly double _minimumX;
    private readonly double _minimumY;
    private readonly double _maximumX;
    private readonly double _maximumY;

    /// <summary>
    /// Creates an axis-aligned rectangular arena.
    /// </summary>
    /// <param name="minimumX">The western edge, in gameplay meters.</param>
    /// <param name="minimumY">The southern edge, in gameplay meters.</param>
    /// <param name="maximumX">The eastern edge. Must exceed <paramref name="minimumX"/>.</param>
    /// <param name="maximumY">The northern edge. Must exceed <paramref name="minimumY"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">An edge is not finite, or an extent is not positive.</exception>
    public GrayboxArenaBounds(double minimumX, double minimumY, double maximumX, double maximumY)
    {
        RequireFinite(minimumX, nameof(minimumX));
        RequireFinite(minimumY, nameof(minimumY));
        RequireFinite(maximumX, nameof(maximumX));
        RequireFinite(maximumY, nameof(maximumY));

        if (maximumX <= minimumX)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumX),
                maximumX,
                "the arena's eastern edge must lie east of its western edge; a degenerate or inverted "
                    + "extent would make every position illegal and every clamp arbitrary");
        }

        if (maximumY <= minimumY)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumY),
                maximumY,
                "the arena's northern edge must lie north of its southern edge; a degenerate or inverted "
                    + "extent would make every position illegal and every clamp arbitrary");
        }

        _minimumX = minimumX;
        _minimumY = minimumY;
        _maximumX = maximumX;
        _maximumY = maximumY;
    }

    /// <summary>
    /// The default graybox arena: a square of side <c>2 *
    /// <see cref="DefaultHalfExtentMeters"/></c> centred on the origin.
    /// </summary>
    public static GrayboxArenaBounds Default => new(
        -DefaultHalfExtentMeters,
        -DefaultHalfExtentMeters,
        DefaultHalfExtentMeters,
        DefaultHalfExtentMeters);

    /// <summary>The western edge, in gameplay meters.</summary>
    public double MinimumX => _minimumX;

    /// <summary>The southern edge, in gameplay meters.</summary>
    public double MinimumY => _minimumY;

    /// <summary>The eastern edge, in gameplay meters.</summary>
    public double MaximumX => _maximumX;

    /// <summary>The northern edge, in gameplay meters.</summary>
    public double MaximumY => _maximumY;

    /// <inheritdoc/>
    /// <remarks>
    /// <paramref name="from"/> is ignored: a rectangle with no obstacles has nothing for a
    /// swept test to hit that clamping the endpoint does not already resolve. An
    /// implementation over real geometry must not ignore it, which is why the parameter is on
    /// the interface rather than absent from it.
    /// </remarks>
    public PlanarVector ResolveMove(PlanarVector from, PlanarVector proposed, double radius)
    {
        return ClampFootprint(proposed, radius);
    }

    /// <inheritdoc/>
    public bool Contains(PlanarVector centre, double radius)
    {
        RequireUsableRadius(radius, nameof(radius));

        return centre.X >= _minimumX + radius
            && centre.X <= _maximumX - radius
            && centre.Y >= _minimumY + radius
            && centre.Y <= _maximumY - radius;
    }

    /// <summary>
    /// Clamps a body's centre so that its whole footprint of <paramref name="radius"/> lies
    /// inside the arena.
    /// </summary>
    /// <param name="centre">The proposed ground-plane centre.</param>
    /// <param name="radius">The body's collision radius in gameplay meters.</param>
    /// <returns>The clamped centre, tangent to any edge the body was driven into.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="radius"/> is not finite, is negative, or is too large for the arena to
    /// hold.
    /// </exception>
    /// <remarks>
    /// Idempotent: clamping an already-legal centre returns it unchanged, and clamping a
    /// clamped centre changes nothing further. Phase 5 relies on that, because a
    /// non-idempotent clamp would drift a body held against a wall along the wall.
    /// </remarks>
    public PlanarVector ClampFootprint(PlanarVector centre, double radius)
    {
        RequireUsableRadius(radius, nameof(radius));

        return PlanarVector.FromComponents(
            Math.Clamp(centre.X, _minimumX + radius, _maximumX - radius),
            Math.Clamp(centre.Y, _minimumY + radius, _maximumY - radius));
    }

    private void RequireUsableRadius(double radius, string parameterName)
    {
        if (!double.IsFinite(radius))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                radius,
                "a collision radius is a finite number of gameplay meters");
        }

        if (radius < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                radius,
                "a collision radius is nonnegative");
        }

        // Math.Clamp throws when its minimum exceeds its maximum, which is what a body wider
        // than the arena produces. Refusing here names the actual problem - the body does not
        // fit - rather than surfacing an argument error about clamp bounds the caller never
        // supplied.
        double widestFittingRadius = Math.Min(_maximumX - _minimumX, _maximumY - _minimumY) / 2.0;
        if (radius > widestFittingRadius)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                radius,
                "a body of radius "
                    + radius.ToString("R", CultureInfo.InvariantCulture)
                    + " m does not fit inside this graybox arena, whose largest fitting radius is "
                    + widestFittingRadius.ToString("R", CultureInfo.InvariantCulture)
                    + " m; there is no legal position to clamp it to, so this is a composition defect "
                    + "rather than a movement outcome");
        }
    }

    private static void RequireFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "an arena edge is a finite number of gameplay meters");
        }
    }
}
