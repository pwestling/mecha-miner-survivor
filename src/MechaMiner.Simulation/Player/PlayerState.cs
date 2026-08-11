using System;
using System.Globalization;
using MechaMiner.Simulation.Geometry;

namespace MechaMiner.Simulation.Player;

/// <summary>
/// The player's committed authoritative state: where the body is, which way it faces, and its
/// Hull.
/// </summary>
/// <remarks>
/// <para>
/// <c>CMP-SIM-001</c> owns "run-local state" per
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry,
/// and this is the player's part of it.
/// </para>
/// <para>
/// <b>It is a value, and that is what makes the single-writer rule structural.</b>
/// <c>docs/technical/10-runtime-architecture.md</c> § Commands and mutations requires every
/// mutable state to have exactly one registered writer. Expressed as a mutable object, that
/// rule is a convention every phase has to keep; expressed as an immutable value held in one
/// field of the world, the only way to change the player is to assign that field, so the
/// registered writer is whatever assigns it and nothing else can pretend to be one.
/// <see cref="PlayerMovement"/> is therefore a pure function returning a new state rather than
/// a system mutating this one.
/// </para>
/// <para>
/// Facing is stored rather than derived from the current direction because it outlives the
/// direction: docs/30-combat-weapons-movement-camera.md:70 - "Releasing movement preserves the
/// last nonzero facing direction." A stopped body has no direction and still has a facing, so
/// the two cannot be the same field.
/// </para>
/// <para>
/// Hull is an integer, per doc 20 § Numeric and unit conventions, and is validated nonnegative
/// and at or below the maximum. Nothing in this slice reduces it: contact damage is out of
/// scope. It is present because the HUD publishes it and because publishing a Hull that no
/// type constrains would make the first damage package's job to discover the constraint.
/// </para>
/// </remarks>
public readonly struct PlayerState : IEquatable<PlayerState>
{
    private readonly PlanarVector _position;
    private readonly double _facingRadians;
    private readonly int _hull;

    private PlayerState(PlanarVector position, double facingRadians, int hull)
    {
        _position = position;
        _facingRadians = facingRadians;
        _hull = hull;
    }

    /// <summary>The authoritative ground-plane centre, in gameplay meters.</summary>
    public PlanarVector Position => _position;

    /// <summary>
    /// The persistent facing, in radians counterclockwise from simulation east.
    /// </summary>
    public double FacingRadians => _facingRadians;

    /// <summary>The current Hull, in <c>[0, PlayerBaseline.MaximumHull]</c>.</summary>
    public int Hull => _hull;

    /// <summary>Whether Hull has reached zero.</summary>
    /// <remarks>
    /// Reported, not acted on. doc 10 § System phase ordering puts the terminal decision at
    /// phase 13, and run termination is out of this slice's scope, so nothing reads this yet.
    /// </remarks>
    public bool IsDestroyed => _hull == 0;

    /// <summary>The player's collision footprint at the current position.</summary>
    public PlanarCircle Footprint =>
        PlanarCircle.FromCentreAndRadius(_position, PlayerBaseline.CollisionRadiusMeters);

    /// <summary>
    /// The state a run begins in: at <paramref name="position"/>, facing east, at full Hull.
    /// </summary>
    /// <param name="position">The deployment position.</param>
    /// <remarks>
    /// Facing comes from <see cref="PlayerBaseline.InitialFacingRadians"/> and Hull from
    /// <see cref="PlayerBaseline.StartingHull"/>, so a fresh run cannot disagree with the
    /// baseline table. Choosing the deployment position is <c>MAP-005</c>'s and is a parameter
    /// rather than a constant for that reason.
    /// </remarks>
    public static PlayerState Deploy(PlanarVector position)
    {
        return new PlayerState(
            position,
            PlayerBaseline.InitialFacingRadians,
            PlayerBaseline.StartingHull);
    }

    /// <summary>
    /// Creates a state explicitly, validating every field.
    /// </summary>
    /// <param name="position">The ground-plane centre.</param>
    /// <param name="facingRadians">The persistent facing in radians. Must be finite.</param>
    /// <param name="hull">The Hull. Must be within <c>[0, PlayerBaseline.MaximumHull]</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="facingRadians"/> is not finite, or <paramref name="hull"/> is outside the
    /// validated domain.
    /// </exception>
    public static PlayerState Create(PlanarVector position, double facingRadians, int hull)
    {
        if (!double.IsFinite(facingRadians))
        {
            throw new ArgumentOutOfRangeException(
                nameof(facingRadians),
                facingRadians,
                "a facing is a finite angle in radians");
        }

        if (hull < 0 || hull > PlayerBaseline.MaximumHull)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hull),
                hull,
                "Hull lies within [0, "
                    + PlayerBaseline.MaximumHull.ToString(CultureInfo.InvariantCulture)
                    + "]; doc 20 § Numeric and unit conventions gives it a \"validated nonnegative "
                    + "domain\", and doc 20 § Scope and invariants forbids a durability value above its "
                    + "maximum");
        }

        return new PlayerState(position, facingRadians, hull);
    }

    /// <summary>Returns this state moved to <paramref name="position"/> and facing <paramref name="facingRadians"/>.</summary>
    /// <param name="position">The new ground-plane centre.</param>
    /// <param name="facingRadians">The new persistent facing in radians. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="facingRadians"/> is not finite.</exception>
    public PlayerState WithPlacement(PlanarVector position, double facingRadians)
    {
        return Create(position, facingRadians, _hull);
    }

    /// <summary>Compares two states for exact equality of every field.</summary>
    public static bool operator ==(PlayerState left, PlayerState right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two states for inequality.</summary>
    public static bool operator !=(PlayerState left, PlayerState right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(PlayerState other)
    {
        return _position.Equals(other._position)
            && _facingRadians.Equals(other._facingRadians)
            && _hull == other._hull;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PlayerState other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_position, _facingRadians, _hull);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "player(at="
            + _position.ToString()
            + ",facing="
            + _facingRadians.ToString("R", CultureInfo.InvariantCulture)
            + "rad,hull="
            + _hull.ToString(CultureInfo.InvariantCulture)
            + ")";
    }
}
