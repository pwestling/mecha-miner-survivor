using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// The pure-pursuer steering rule: every tick, straight at the mech's current authoritative centre,
/// at the identity's own world speed.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/31-initial-alien-roster.md</c> § Shared ordinary-enemy rules:22 - "Nine identities
/// continuously pursue the mech and threaten only through contact." There is no acceleration, no
/// turn rate, no prediction and no flocking, and their absence is the specification rather than a
/// simplification: doc 31:16 says most danger "comes from large numbers of simple contact pursuers
/// whose fixed durability, speed, size, and density become threatening in different combinations",
/// and doc 31 § Elite treatment:113 says elites "add no attacks, phases, aura, support AI".
/// </para>
/// <para>
/// <b>Enemies do not avoid each other and do not stop on contact.</b> doc 31:26 - ordinary enemies
/// "do not collide with the mech, one another, mining points, pickups, or other enemies", and
/// <c>docs/72-player-survivability-and-damage-baseline.md</c>:59 - "Crossing through an enemy does
/// not slow or redirect the mech, but its contact footprint can still deal damage during the
/// overlap." So a pursuer that reaches the mech keeps walking through it, and the resulting
/// sustained overlap is exactly the condition doc 31:27's repeat interval governs. Separation
/// behaviour would be a rule neither document states.
/// </para>
/// <para>
/// <b>No world constraint is applied.</b> doc 31:26 does say "Solid world terrain still constrains
/// their navigation and spawn positions", and that constraint is <c>GEO-005</c>'s navigable-ground
/// query, which does not exist. The graybox arena is the only bound available and it is scaffolding
/// (<c>GrayboxArenaBounds</c>); clamping a pursuer to it would make the arena wall a documented
/// navigation rule, and an enemy hugging a wall it cannot leave is a behaviour nothing asked for.
/// A pursuer therefore walks in a straight line toward the mech and the mech never leaves the arena,
/// so a pursuer never has cause to.
/// </para>
/// </remarks>
public static class EnemyPursuit
{
    /// <summary>
    /// The displacement one tick of pursuit produces for <paramref name="profile"/>.
    /// </summary>
    /// <param name="profile">The identity's fixed profile.</param>
    /// <remarks>
    /// Derived from <see cref="TickRate.SecondsPerTick"/> and nothing else, for the same reason
    /// <c>PlayerMovement.BaseDisplacementPerTickMeters</c> is: doc 10 § Clock domains forbids a
    /// variable delta reaching an authoritative system, and a per-tick constant is structurally
    /// incapable of receiving one.
    /// </remarks>
    public static double DisplacementPerTickMeters(EnemyProfile profile)
    {
        return profile.WorldSpeedMetersPerSecond * TickRate.SecondsPerTick;
    }

    /// <summary>
    /// Steps one enemy one tick toward <paramref name="target"/>.
    /// </summary>
    /// <param name="state">The enemy's committed state.</param>
    /// <param name="profile">The identity's fixed profile.</param>
    /// <param name="target">The mech's authoritative ground-plane centre.</param>
    /// <returns>The enemy's state after one tick of pursuit.</returns>
    /// <remarks>
    /// <para>
    /// An enemy exactly on the target does not move, and does not acquire a facing out of nothing.
    /// That case is reachable rather than theoretical: doc 31:26 makes enemies non-solid, so a
    /// pursuer walks into the mech and its centre passes through the mech's.
    /// </para>
    /// <para>
    /// The step is not clamped to the remaining distance. An overshoot of at most one tick's
    /// displacement - 0.021 m for a Skitterling - is what a fixed-step integrator does, and
    /// snapping to the target instead would make a pursuer's speed depend on how close it already
    /// was, which is a rule doc 31 does not have.
    /// </para>
    /// </remarks>
    public static EnemyState Advance(EnemyState state, EnemyProfile profile, PlanarVector target)
    {
        PlanarVector toTarget = target - state.Position;
        PlanarVector direction = toTarget.Normalized();
        if (direction.IsZero)
        {
            return state;
        }

        return state.MovedTo(state.Position + (direction * DisplacementPerTickMeters(profile)));
    }
}
