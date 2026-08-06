using System;
using System.Globalization;
using MechaMiner.Simulation.Entities;

namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// One visible or potentially visible entity's transform and presentation-state flags, as published
/// in a snapshot.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot: the snapshot includes
/// "visible or potentially visible entity transforms and presentation-state flags".
/// </para>
/// <para>
/// A readonly struct of scalars only, so a snapshot's entity view can be a
/// <see cref="ReadOnlyMemory{T}"/> over a page-owned array with nothing in it that a consumer could
/// write through, and so the page costs one array rather than one object per entity.
/// </para>
/// <para>
/// <see cref="PresentationFlags"/> is an integer bitmask rather than an enumeration for the same
/// reason <c>EventKind</c> is not one: the presentation stream owns which bits exist, and an
/// enumeration here would make every later flag an edit to this package.
/// </para>
/// </remarks>
public readonly struct SnapshotEntity : IEquatable<SnapshotEntity>
{
    private readonly EntityId _id;
    private readonly PopulationCategory _category;
    private readonly double _positionX;
    private readonly double _positionY;
    private readonly double _facingRadians;
    private readonly int _presentationFlags;

    private SnapshotEntity(
        EntityId id,
        PopulationCategory category,
        double positionX,
        double positionY,
        double facingRadians,
        int presentationFlags)
    {
        _id = id;
        _category = category;
        _positionX = positionX;
        _positionY = positionY;
        _facingRadians = facingRadians;
        _presentationFlags = presentationFlags;
    }

    /// <summary>The authoritative identity, which the presentation bridge maps to a handle.</summary>
    /// <remarks>
    /// doc 30 § Snapshot synchronization: "The bridge maps simulation entity IDs to presentation
    /// handles." The identity crosses the boundary; the record never does.
    /// </remarks>
    public EntityId Id => _id;

    /// <summary>Which authoritative population this entity belongs to.</summary>
    public PopulationCategory Category => _category;

    /// <summary>
    /// The planar X component of the entity's transform, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> this component and <see cref="PositionY"/> must be replaced by the single
    /// planar position type that <c>W2-GEO</c> owns once <c>GEO-001</c> lands. The change is: delete
    /// both <c>double</c> components and the <c>positionX</c>/<c>positionY</c> parameters of
    /// <see cref="Create"/>, replace them with one planar-position parameter and one property, update
    /// <see cref="ToString"/>, and update <c>InterpolationSnapPolicy</c>'s displacement input to take
    /// the same type. Do not introduce a planar vector type in this package.
    /// </remarks>
    public double PositionX => _positionX;

    /// <summary>
    /// The planar Y component of the entity's transform, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> see <see cref="PositionX"/>. Double rather than single precision per doc 20 §
    /// Numeric and unit conventions.
    /// </remarks>
    public double PositionY => _positionY;

    /// <summary>The facing, in radians.</summary>
    /// <remarks>
    /// Radians internally; doc 20 § Numeric and unit conventions normalizes a player-facing bearing
    /// to degrees clockwise from north "only at display/content boundaries", which is presentation's
    /// job and not this record's.
    /// </remarks>
    public double FacingRadians => _facingRadians;

    /// <summary>The presentation-state bitmask, whose bits the presentation stream defines.</summary>
    public int PresentationFlags => _presentationFlags;

    /// <summary>True when this record was constructed rather than defaulted.</summary>
    public bool IsPresent => !_id.IsUnset;

    /// <summary>Constructs a snapshot entity record.</summary>
    /// <param name="id">The authoritative identity. Must be an issued identity.</param>
    /// <param name="category">The owning population category.</param>
    /// <param name="positionX">The planar X component in gameplay meters. Must be finite. <b>GEO-001</b>.</param>
    /// <param name="positionY">The planar Y component in gameplay meters. Must be finite. <b>GEO-001</b>.</param>
    /// <param name="facingRadians">The facing in radians. Must be finite.</param>
    /// <param name="presentationFlags">The presentation-state bitmask.</param>
    /// <exception cref="ArgumentOutOfRangeException">The identity was not issued, or a scalar is not finite.</exception>
    public static SnapshotEntity Create(
        EntityId id,
        PopulationCategory category,
        double positionX,
        double positionY,
        double facingRadians,
        int presentationFlags)
    {
        if (!id.IsIssued)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                id,
                "a snapshot entity must name an issued identity; doc 20 § Scope and invariants "
                    + "requires every live entity ID to resolve to exactly one live record");
        }

        RequireFinite(positionX, nameof(positionX));
        RequireFinite(positionY, nameof(positionY));
        RequireFinite(facingRadians, nameof(facingRadians));

        return new SnapshotEntity(id, category, positionX, positionY, facingRadians, presentationFlags);
    }

    /// <summary>Compares two records for exact equality of every field.</summary>
    public static bool operator ==(SnapshotEntity left, SnapshotEntity right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two records for inequality.</summary>
    public static bool operator !=(SnapshotEntity left, SnapshotEntity right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(SnapshotEntity other)
    {
        return _id == other._id
            && _category == other._category
            && _positionX.Equals(other._positionX)
            && _positionY.Equals(other._positionY)
            && _facingRadians.Equals(other._facingRadians)
            && _presentationFlags == other._presentationFlags;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is SnapshotEntity other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _id,
            _category,
            _positionX,
            _positionY,
            _facingRadians,
            _presentationFlags);
    }

    /// <summary>Renders the record as canonical invariant text for goldens and diagnostics.</summary>
    public override string ToString()
    {
        return _id.ToString()
            + " "
            + _category.ToString()
            + " at=("
            + _positionX.ToString("R", CultureInfo.InvariantCulture)
            + ","
            + _positionY.ToString("R", CultureInfo.InvariantCulture)
            + ") facing="
            + _facingRadians.ToString("R", CultureInfo.InvariantCulture)
            + " flags="
            + _presentationFlags.ToString(CultureInfo.InvariantCulture);
    }

    private static void RequireFinite(double component, string parameterName)
    {
        if (!double.IsFinite(component))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                component,
                "a snapshot transform component must be finite; doc 20 § Scope and invariants "
                    + "requires authoritative positions to be valid planar positions");
        }
    }
}
