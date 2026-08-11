using System;

namespace MechaMiner.Simulation.Geometry;

/// <summary>
/// The one place the simulation plane's mapping into the 3D presentation world is stated.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/delivery-waves.md</c> § W2 gives <c>GEO-003</c> the "coordinate
/// presentation adapter contract", and
/// <c>docs/technical/21-world-geometry-navigation-and-spatial-queries.md</c> § Coordinate
/// spaces requires that "conversions are explicit named operations". This is that named
/// operation, and it lives inside the simulation assembly for two reasons.
/// </para>
/// <para>
/// The first is ownership: the mapping is a term of the coordinate contract in
/// <c>docs/technical/decisions/TDR-005-simulate-gameplay-on-a-two-dimensional-plane.md</c>
/// § Coordinate contract, so it is a rule rather than a rendering detail, and a rule stated
/// in two places is a rule that will disagree with itself. The second is that it is the only
/// placement under which it can be verified at all: <c>tests/MechaMiner.Game.Tests</c>
/// references the three pure projects and not the presentation assembly, and
/// <c>build/verify-architecture.sh</c> § 3 asserts that reference set exactly, so a mapping
/// written in the presentation assembly is unreachable from every test project in this
/// repository.
/// </para>
/// <para>
/// The mapping, verbatim from TDR-005 § Coordinate contract: "Simulation X increases east and
/// simulation Y increases north", and presentation "maps simulation east to world positive X,
/// north to world negative Z, and vertical height to world positive Y". The sign on the
/// northward axis is the whole content of this type and the single most likely thing for a
/// hand-written conversion to get wrong, because dropping it produces a world that is
/// mirrored north-to-south and still renders, still moves, and still looks plausible until
/// something asks which way north is.
/// </para>
/// <para>
/// <b>This type produces three numbers and knows nothing about how they are used.</b> It
/// names no engine type, constructs nothing, and imports nothing but
/// <c>System</c>. Presentation is what assembles the three components into its own vector.
/// That boundary is deliberate: doc 20 § Numeric and unit conventions puts "all conversion to
/// [engine] vectors" in presentation, so the arithmetic is the rule's and the vector type is
/// presentation's.
/// </para>
/// <para>
/// Height is carried through unchanged and is presentation's alone. There is no authoritative
/// third axis - TDR-005 exists to say so - so a height supplied here can only ever be a
/// presentation offset such as a model's resting elevation above the ground plane. It can
/// never be read back into gameplay, because <see cref="ToSimulationPlane"/> discards it.
/// </para>
/// </remarks>
public static class PresentationGroundMapping
{
    /// <summary>
    /// Maps an authoritative planar position to the three components of a presentation world
    /// position.
    /// </summary>
    /// <param name="position">The authoritative ground-plane position, in gameplay meters.</param>
    /// <param name="height">
    /// The presentation height above the ground plane, in meters. Presentation-only; there is
    /// no authoritative height. Must be finite.
    /// </param>
    /// <param name="worldX">Receives the world eastward component.</param>
    /// <param name="worldY">Receives the world upward component, which is <paramref name="height"/>.</param>
    /// <param name="worldZ">Receives the world component along the axis north runs negative along.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="height"/> is not finite.</exception>
    public static void ToPresentationWorld(
        PlanarVector position,
        double height,
        out double worldX,
        out double worldY,
        out double worldZ)
    {
        if (!double.IsFinite(height))
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "a presentation height is a finite number of meters");
        }

        // TDR-005 § Coordinate contract, the whole of it:
        //   simulation east  (+X) -> world +X
        //   presentation up        -> world +Y
        //   simulation north (+Y) -> world -Z
        worldX = position.X;
        worldY = height;
        worldZ = -position.Y;
    }

    /// <summary>
    /// Recovers the authoritative planar position from a presentation world position,
    /// discarding height.
    /// </summary>
    /// <param name="worldX">The world eastward component.</param>
    /// <param name="worldZ">The world component along the axis north runs negative along.</param>
    /// <returns>The authoritative ground-plane position.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite.</exception>
    /// <remarks>
    /// <para>
    /// This exists to make the mapping's invertibility assertable, and for presentation code
    /// that must answer a question in gameplay meters about something it holds in world
    /// coordinates - a camera's ground footprint, for instance.
    /// </para>
    /// <para>
    /// <b>It is not a route for presentation to write authoritative state.</b> doc 10
    /// § Architectural style makes the simulation the sole authority, so a caller that
    /// converts a rendered transform back to the plane and stores it as a position has
    /// inverted the direction of authority no matter how correct this arithmetic is. The
    /// authoritative position is the one the snapshot carries; this returns a value, not a
    /// fact.
    /// </para>
    /// </remarks>
    public static PlanarVector ToSimulationPlane(double worldX, double worldZ)
    {
        return PlanarVector.FromComponents(worldX, -worldZ);
    }
}
