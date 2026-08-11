using System;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Geometry;

namespace MechaMiner.Simulation.Player;

/// <summary>
/// The output of phase 4 for the player: a direction to travel and the facing that follows
/// from it.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c>:147, phase 4: "Resolve player intent and
/// enemy steering." This is the resolved player half. It exists as a named value rather than
/// as two locals inside phase 5 so that the boundary between deciding where to go and moving
/// there is visible in the code, and so that phase 5 takes a direction it cannot second-guess.
/// </para>
/// <para>
/// <b>The direction is unit length or exactly zero, and it carries no speed.</b> That is the
/// substance of doc 60 § Movement:65 - analog movement "remaps remaining magnitude to
/// <c>[0,1]</c>, then the gameplay rule converts <em>any</em> nonzero magnitude to full
/// movement speed while preserving direction", which doc 20 § Active commands:215 states again
/// as "immediate direction and full current speed for nonzero input", and
/// docs/30-combat-weapons-movement-camera.md:64 a third time as "Movement input sets the mech's
/// direction and full movement speed immediately". Three documents agree, so the intent's
/// sub-unit magnitude is discarded here deliberately: it survives normalization only to carry
/// direction, and treating it as a throttle would be the plausible wrong answer.
/// </para>
/// <para>
/// The radial deadzone that decides whether a sample counts as nonzero at all is the input
/// adapter's, not this type's - doc 60 § Movement:65 puts it in the adapter and makes it
/// configurable. By the time an intent reaches admission it has already been through the
/// deadzone, so a nonzero intent here means the player really is pushing.
/// </para>
/// </remarks>
public readonly struct PlayerSteering
{
    private readonly PlanarVector _direction;
    private readonly double _facingRadians;

    private PlayerSteering(PlanarVector direction, double facingRadians)
    {
        _direction = direction;
        _facingRadians = facingRadians;
    }

    /// <summary>The unit direction of travel, or <see cref="PlanarVector.Zero"/> for a stop.</summary>
    public PlanarVector Direction => _direction;

    /// <summary>The facing to commit, in radians counterclockwise from east.</summary>
    public double FacingRadians => _facingRadians;

    /// <summary>Whether this steering is a stop.</summary>
    public bool IsStop => _direction.IsZero;

    /// <summary>
    /// Resolves an admitted intent against the facing the body currently holds.
    /// </summary>
    /// <param name="intent">The tick's effective movement intent.</param>
    /// <param name="currentFacingRadians">
    /// The facing already committed, returned unchanged when the intent is a stop.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="currentFacingRadians"/> is not finite.
    /// </exception>
    /// <remarks>
    /// A stop preserves the incoming facing rather than resetting it, per
    /// docs/30-combat-weapons-movement-camera.md:70 - "Releasing movement preserves the last
    /// nonzero facing direction." Resetting to east on release is the obvious wrong behaviour
    /// and would silently re-aim every facing-based weapon each time the player let go.
    /// </remarks>
    public static PlayerSteering Resolve(MovementIntent intent, double currentFacingRadians)
    {
        if (!double.IsFinite(currentFacingRadians))
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentFacingRadians),
                currentFacingRadians,
                "a committed facing is a finite angle in radians");
        }

        if (intent.IsStop)
        {
            return new PlayerSteering(PlanarVector.Zero, currentFacingRadians);
        }

        // The intent's own magnitude is discarded here, not lost: see this type's remarks for
        // the three documents that require full speed for any nonzero input.
        PlanarVector direction = PlanarVector.FromComponents(intent.X, intent.Y).Normalized();

        // UNREACHABLE from any nonzero intent, and kept deliberately. The claim, stated so
        // someone can try to falsify it: a nonzero MovementIntent has at least one component
        // whose absolute value is at least double.Epsilon, double.Hypot of such a pair is at
        // least double.Epsilon and therefore not exactly zero, so Normalized() cannot return
        // zero here. It was originally believed reachable by a pair of subnormal components,
        // and a test asserting that failed: MovementIntent.Normalize divides by the largest
        // component, so (epsilon, epsilon) survives normalization intact and its hypotenuse is
        // subnormal but nonzero. See PlayerMovementTests.
        // TheSmallestRepresentableNonzeroIntentStillCommandsFullSpeed, which asserts the
        // reachable behaviour instead.
        //
        // The branch stays because the reason it is unreachable is a property of
        // MovementIntent's normalization rather than of this method, and a division by a
        // magnitude this method did not check would be a silent NaN facing rather than a
        // refusal. It costs one comparison per tick.
        if (direction.IsZero)
        {
            return new PlayerSteering(PlanarVector.Zero, currentFacingRadians);
        }

        return new PlayerSteering(direction, direction.BearingRadians());
    }
}
