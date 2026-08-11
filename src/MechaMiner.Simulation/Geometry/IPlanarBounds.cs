namespace MechaMiner.Simulation.Geometry;

/// <summary>
/// The world constraint that phase 5 enforces: where a circular body is allowed to be.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering, phase 5:
/// "Integrate movement and enforce terrain/world constraints." This interface is the
/// "terrain/world constraints" half, expressed as the narrowest thing movement integration
/// actually needs, so that the graybox this slice ships and the validated geometry manifest
/// that replaces it are interchangeable without movement integration changing.
/// </para>
/// <para>
/// <b>This is the seam <c>MAP-007</c> fills.</b>
/// <c>docs/technical/delivery-waves.md</c> § W3-MAP gives <c>MAP-007</c> the
/// "manifest/checksum/retry" payload that produces the run's immutable geometry manifest,
/// and <c>docs/technical/21-world-geometry-navigation-and-spatial-queries.md</c> § Static
/// geometry manifest makes that manifest "canonical for the run". The implementation
/// present today is <see cref="GrayboxArenaBounds"/>, which is a rectangle with no
/// obstacles and is not a map.
/// </para>
/// <para>
/// The real implementer will resolve a swept contact and slide along the remaining tangent
/// - doc 21 § Player and enemy movement: "resolving the earliest contact and sliding along
/// the remaining tangent", two iterations normally - which is why this member is shaped as
/// "given where a body wants to be, say where it ends up" rather than as a boolean test. A
/// boolean would force the caller to own the resolution, and the caller is exactly who must
/// not own it.
/// </para>
/// <para>
/// No member takes a duration, for the same structural reason
/// <c>ISimulationWorld</c> takes none: doc 10 § Clock domains forbids passing a variable
/// delta to an authoritative system, and a constraint that cannot see a duration cannot
/// scale a correction by one.
/// </para>
/// </remarks>
public interface IPlanarBounds
{
    /// <summary>
    /// Resolves a proposed move to the position the body actually reaches.
    /// </summary>
    /// <param name="from">The body's committed ground-plane centre before the move.</param>
    /// <param name="proposed">The ground-plane centre the body's velocity proposes.</param>
    /// <param name="radius">The body's collision radius in gameplay meters.</param>
    /// <returns>
    /// The resolved ground-plane centre. Equals <paramref name="proposed"/> when the move is
    /// unobstructed, and is always a position the body may legally occupy.
    /// </returns>
    /// <remarks>
    /// <paramref name="from"/> is supplied because a swept implementation needs the segment
    /// rather than only its end, and because it is the fallback an implementation returns
    /// when it can find no legal resolution at all. It is unused by the rectangular graybox.
    /// </remarks>
    PlanarVector ResolveMove(PlanarVector from, PlanarVector proposed, double radius);

    /// <summary>
    /// Whether a body of <paramref name="radius"/> centred at <paramref name="centre"/> is
    /// entirely within the legal region.
    /// </summary>
    /// <param name="centre">The ground-plane centre to test.</param>
    /// <param name="radius">The body's collision radius in gameplay meters.</param>
    bool Contains(PlanarVector centre, double radius);
}
