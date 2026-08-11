namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// Why presentation must snap to the newest transform instead of interpolating between the two most
/// recent snapshots.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot: presentation "interpolates
/// transforms between the two most recent complete snapshots but snaps on spawn, teleport, re-entry,
/// terminal transition, or a distance threshold that would make interpolation misleading".
/// <c>docs/technical/30-presentation-and-rendering.md</c> § Snapshot synchronization: "Spawned actors
/// appear at the newest transform without extrapolating backward" and "Teleports, boss re-entry, large
/// correction, and terminal transitions snap".
/// </para>
/// <para>
/// Members are declared in evaluation precedence order, highest first after <see cref="None"/>. The
/// order matters: an entity that both spawned and moved further than the threshold in one interval
/// reports <see cref="Spawn"/>, because that is the cause and the displacement is its consequence.
/// </para>
/// </remarks>
public enum InterpolationSnapReason
{
    /// <summary>Nothing forces a snap; presentation interpolates.</summary>
    None = 0,

    /// <summary>The entity did not exist in the older snapshot.</summary>
    /// <remarks>doc 30 § Snapshot synchronization: a spawned actor appears "at the newest transform without extrapolating backward".</remarks>
    Spawn = 1,

    /// <summary>The entity was moved discontinuously by an authoritative rule.</summary>
    Teleport = 2,

    /// <summary>A boss re-entered after leaving, so its previous transform is not on a continuous path.</summary>
    /// <remarks>doc 20 § Authoritative population categories gives a boss "re-entry state"; doc 23 § Recycling policy makes boss re-entry distinct from ordinary recycling.</remarks>
    BossReEntry = 3,

    /// <summary>The run reached death or extraction, so continuing to interpolate would animate past the outcome.</summary>
    /// <remarks>doc 20 § Boundary and tie ordering: after the final pre-boundary tick "no later simulation step can deal damage".</remarks>
    TerminalTransition = 4,

    /// <summary>
    /// The displacement between the two snapshots exceeds what the fastest legal authoritative movement
    /// could cover, so interpolating it would draw motion that never happened.
    /// </summary>
    /// <remarks>
    /// The backstop, not the primary trigger: spawn, teleport, and boss re-entry are enumerated above
    /// and exceed the threshold by orders of magnitude anyway. It catches the cases nobody enumerated.
    /// </remarks>
    DistanceThresholdExceeded = 5,
}
