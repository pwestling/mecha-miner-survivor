using System;
using System.Globalization;
using MechaMiner.Simulation.Geometry;

namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// One live ordinary enemy's mutable state: where it is, how much Hull it has left, and when it last
/// landed a contact instance on the mech.
/// </summary>
/// <remarks>
/// <para>
/// A value type, stored by <c>PackedEntityStore&lt;EnemyState&gt;</c>. Its profile is deliberately
/// <em>not</em> a field: doc 31:24 fixes one profile per identity for the whole run, and this slice
/// has one identity, so a per-instance profile copy would be a per-instance opportunity to differ.
/// The store that holds these knows which identity it holds.
/// </para>
/// <para>
/// <see cref="LastContactTick"/> is what makes doc 31:27's "once every 0.75 seconds while that same
/// overlap continues" a per-enemy fact rather than a global one. The global half of the rule - the
/// 0.20-second grace after any contact - belongs to the mech and lives on the world, because doc
/// 31:28 scopes it to the mech: "Other enemies cannot deal another contact instance during that
/// grace."
/// </para>
/// </remarks>
public readonly struct EnemyState : IEquatable<EnemyState>
{
    /// <summary>
    /// The <see cref="LastContactTick"/> value meaning this enemy has never landed a contact.
    /// </summary>
    /// <remarks>
    /// Negative rather than zero, because tick zero is a real tick on which a contact can land. A
    /// sentinel of zero would make a first-tick contact indistinguishable from no contact and would
    /// silently suppress the second instance 0.75 seconds later.
    /// </remarks>
    public const long NeverContacted = -1L;

    private readonly PlanarVector _position;
    private readonly int _hull;
    private readonly long _lastContactTick;

    private EnemyState(PlanarVector position, int hull, long lastContactTick)
    {
        _position = position;
        _hull = hull;
        _lastContactTick = lastContactTick;
    }

    /// <summary>The authoritative ground-plane centre.</summary>
    public PlanarVector Position => _position;

    /// <summary>Remaining Hull. Zero means this enemy is dead and awaiting phase 12's removal.</summary>
    public int Hull => _hull;

    /// <summary>
    /// The tick on which this enemy last landed a contact instance, or <see cref="NeverContacted"/>.
    /// </summary>
    public long LastContactTick => _lastContactTick;

    /// <summary>Whether Hull has reached zero.</summary>
    public bool IsDead => _hull == 0;

    /// <summary>Whether this state was built rather than defaulted.</summary>
    public bool IsMaterialized => _hull > 0 || _lastContactTick != 0L;

    /// <summary>
    /// Materializes an enemy at full Hull.
    /// </summary>
    /// <param name="profile">The identity's fixed profile.</param>
    /// <param name="position">Where it enters.</param>
    /// <exception cref="ArgumentException">The profile was not built from a roster row.</exception>
    public static EnemyState Spawn(EnemyProfile profile, PlanarVector position)
    {
        if (!profile.IsAuthored)
        {
            throw new ArgumentException(
                "an enemy materializes from an authored roster row; a defaulted profile names no "
                    + "identity and carries no Hull",
                nameof(profile));
        }

        return new EnemyState(position, profile.MaximumHull, NeverContacted);
    }

    /// <summary>Returns this state moved to a new position, with Hull and contact history intact.</summary>
    /// <param name="position">The new authoritative centre.</param>
    public EnemyState MovedTo(PlanarVector position)
    {
        return new EnemyState(position, _hull, _lastContactTick);
    }

    /// <summary>
    /// Returns this state with <paramref name="damage"/> subtracted from Hull, floored at zero.
    /// </summary>
    /// <param name="damage">The damage to apply. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="damage"/> is not positive.</exception>
    /// <remarks>
    /// Floored rather than allowed negative. doc 20 § Numeric and unit conventions gives durability a
    /// validated nonnegative domain, and an overkill remainder would make two enemies that both died
    /// to the same shot distinguishable by how far below zero they went - a difference no rule reads
    /// and every diagnostic would show.
    /// </remarks>
    public EnemyState Damaged(int damage)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(damage, 1);
        return new EnemyState(_position, Math.Max(0, _hull - damage), _lastContactTick);
    }

    /// <summary>Returns this state with its last-contact tick set.</summary>
    /// <param name="tick">The tick the contact instance resolved on.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tick"/> is negative.</exception>
    public EnemyState WithContactAt(long tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);
        return new EnemyState(_position, _hull, tick);
    }

    /// <summary>This enemy's contact footprint under <paramref name="profile"/>.</summary>
    /// <param name="profile">The identity's fixed profile.</param>
    public PlanarCircle FootprintUnder(EnemyProfile profile)
    {
        return PlanarCircle.FromCentreAndRadius(_position, profile.ContactRadiusMeters);
    }

    /// <inheritdoc/>
    public static bool operator ==(EnemyState left, EnemyState right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(EnemyState left, EnemyState right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(EnemyState other)
    {
        return _position.Equals(other._position)
            && _hull == other._hull
            && _lastContactTick == other._lastContactTick;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EnemyState other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_position, _hull, _lastContactTick);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "enemy(at="
            + _position.ToString()
            + ",hull="
            + _hull.ToString(CultureInfo.InvariantCulture)
            + ",lastContact="
            + _lastContactTick.ToString(CultureInfo.InvariantCulture)
            + ")";
    }
}
