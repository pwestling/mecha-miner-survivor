using System;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Player;

/// <summary>
/// Phase 5 for the player: integrate one tick of movement and enforce the world constraint.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c>:148, phase 5: "Integrate movement and
/// enforce terrain/world constraints."
/// </para>
/// <para>
/// <b>No member of this type takes a duration, and that is the point.</b> doc 10 § Clock
/// domains: game time "is derived from integer tick count, not accumulated floating-point frame
/// deltas", and the host "never passes a variable delta to authoritative systems". A movement
/// integrator that accepted a <c>double seconds</c> would be correct today and wrong the first
/// time a caller passed it a frame delta, and no test could distinguish the two callers. Taking
/// no duration at all makes it structurally impossible: the only duration in scope is
/// <see cref="TickRate.SecondsPerTick"/>, which is a compile-time constant.
/// </para>
/// <para>
/// The motion model is deliberately trivial and the documents are emphatic that it must stay
/// so. docs/30-combat-weapons-movement-camera.md:64: "Standard movement has no acceleration,
/// braking lag, momentum, turn radius, sprint, dash, dodge, stamina, reverse penalty, or
/// strafing penalty." So there is no velocity carried between ticks and no state to hold one
/// in: the displacement of a tick is a function of that tick's steering alone, which is why
/// this type is static and holds nothing.
/// </para>
/// </remarks>
public static class PlayerMovement
{
    /// <summary>
    /// The distance a body at base speed covers in exactly one tick, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <c>3.0 m/s / 60 ticks/s = 0.05 m/tick</c>. Both factors are compile-time constants -
    /// <see cref="PlayerBaseline.BaseMovementSpeedMetersPerSecond"/> and
    /// <see cref="TickRate.SecondsPerTick"/> - so this is one too, and it is exposed so a test
    /// can assert the derivation rather than restate the product.
    /// </remarks>
    public const double BaseDisplacementPerTickMeters =
        PlayerBaseline.BaseMovementSpeedMetersPerSecond * TickRate.SecondsPerTick;

    /// <summary>
    /// Integrates one tick of movement for the player and returns the committed state.
    /// </summary>
    /// <param name="state">The state committed by the previous tick.</param>
    /// <param name="steering">This tick's resolved steering, from phase 4.</param>
    /// <param name="bounds">The world constraint to enforce.</param>
    /// <returns>The state this tick commits.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="bounds"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// The facing from <paramref name="steering"/> is committed whether or not the body moved.
    /// A body pressed into a wall still turns to face the direction the player is pushing,
    /// which is what docs/30:70 - "While movement input is nonzero, the mech faces in that
    /// direction" - says without qualification by whether the move succeeded.
    /// </para>
    /// <para>
    /// A stop is short-circuited rather than integrated as a zero-length displacement. The two
    /// would agree arithmetically, but the short circuit also skips the bounds call, which
    /// matters because it means a stationary body already legally placed is never re-resolved
    /// and so cannot be nudged by a future implementation's rounding.
    /// </para>
    /// </remarks>
    public static PlayerState Integrate(PlayerState state, PlayerSteering steering, IPlanarBounds bounds)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        if (steering.IsStop)
        {
            // docs/30:64 "Releasing input stops the mech immediately." Immediately means this
            // tick and with no residual displacement, so there is nothing to integrate.
            return state.WithPlacement(state.Position, steering.FacingRadians);
        }

        PlanarVector proposed = state.Position + (steering.Direction * BaseDisplacementPerTickMeters);
        PlanarVector resolved = bounds.ResolveMove(
            state.Position,
            proposed,
            PlayerBaseline.CollisionRadiusMeters);

        return state.WithPlacement(resolved, steering.FacingRadians);
    }
}
