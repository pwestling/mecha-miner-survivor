using System;
using System.Globalization;
using MechaMiner.Simulation.Geometry;

namespace MechaMiner.Simulation.Combat;

/// <summary>
/// One live Pulse Repeater projectile: a point travelling on the bearing it was fired along, with a
/// bounded number of ticks left.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/66-weapon-catalog-and-resource-graph.md</c>:94 - Pulse Repeater "fires rapid projectiles
/// toward its current position ... Projectiles continue on their fired trajectory rather than homing
/// after launch." So the direction is a field, fixed at creation, and there is no target reference:
/// a projectile that stored the enemy it was fired at could be tempted to follow it, and would also
/// need a rule for what happens when that enemy dies mid-flight, which is a rule doc 66 replaces with
/// "rather than homing".
/// </para>
/// <para>
/// <b>A projectile is a point, not a circle.</b> doc 71:81 gives Pulse Repeater no beam width or blast
/// radius - those are <c>W-AB</c>'s and <c>W-AC</c>'s stats - so the hit test is the enemy's own
/// contact footprint containing the projectile's position. Giving the projectile a radius would widen
/// every hit by a number no document states.
/// </para>
/// </remarks>
public readonly struct ProjectileState : IEquatable<ProjectileState>
{
    private readonly PlanarVector _position;
    private readonly PlanarVector _direction;
    private readonly int _remainingTicks;

    private ProjectileState(PlanarVector position, PlanarVector direction, int remainingTicks)
    {
        _position = position;
        _direction = direction;
        _remainingTicks = remainingTicks;
    }

    /// <summary>The projectile's authoritative ground-plane position.</summary>
    public PlanarVector Position => _position;

    /// <summary>The unit direction it travels along, fixed at launch.</summary>
    public PlanarVector Direction => _direction;

    /// <summary>How many ticks of travel remain before it expires unspent.</summary>
    public int RemainingTicks => _remainingTicks;

    /// <summary>Whether this projectile has travelled its whole range without hitting anything.</summary>
    public bool IsSpent => _remainingTicks <= 0;

    /// <summary>Whether this state was built rather than defaulted.</summary>
    public bool IsLaunched => !_direction.IsZero;

    /// <summary>
    /// Launches a projectile.
    /// </summary>
    /// <param name="origin">Where it starts - the mech's authoritative centre.</param>
    /// <param name="direction">The bearing to travel along. Must be nonzero.</param>
    /// <exception cref="ArgumentException"><paramref name="direction"/> is zero.</exception>
    /// <remarks>
    /// The direction is normalized here rather than trusted, so a caller that passes a
    /// target-minus-origin displacement gets a projectile at the weapon's stated speed rather than one
    /// whose speed scales with how far away the target was.
    /// </remarks>
    public static ProjectileState Launch(PlanarVector origin, PlanarVector direction)
    {
        PlanarVector unit = direction.Normalized();
        if (unit.IsZero)
        {
            throw new ArgumentException(
                "a projectile is launched along a nonzero bearing; a zero direction is a target at the "
                    + "muzzle, which the firing rule must decline rather than resolve here",
                nameof(direction));
        }

        return new ProjectileState(origin, unit, PulseRepeaterBaseline.ProjectileLifetimeTicks);
    }

    /// <summary>Returns this projectile advanced one tick along its bearing.</summary>
    public ProjectileState Advanced()
    {
        return new ProjectileState(
            _position + (_direction * PulseRepeaterBaseline.ProjectileDisplacementPerTickMeters),
            _direction,
            _remainingTicks - 1);
    }

    /// <inheritdoc/>
    public static bool operator ==(ProjectileState left, ProjectileState right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(ProjectileState left, ProjectileState right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(ProjectileState other)
    {
        return _position.Equals(other._position)
            && _direction.Equals(other._direction)
            && _remainingTicks == other._remainingTicks;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is ProjectileState other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_position, _direction, _remainingTicks);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "projectile(at="
            + _position.ToString()
            + ",dir="
            + _direction.ToString()
            + ",ticksLeft="
            + _remainingTicks.ToString(CultureInfo.InvariantCulture)
            + ")";
    }
}
