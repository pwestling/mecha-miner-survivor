using System;
using System.Globalization;
using MechaMiner.Simulation.Entities;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// A disposable presentation instruction: attack fired, hit confirmed, mining installment,
/// warning, loot burst.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "Presentation
/// events are disposable instructions", "Presentation events may be coalesced by an explicit
/// visual policy", and "Consumers never infer authoritative state solely from presentation
/// events."
/// </para>
/// <para>
/// <c>CTR-SIM-002</c> in doc 115 § Cross-boundary contract registry: produced by
/// <c>CMP-SIM-003</c>, consumed by "presentation/UI/audio", and on failure "noncritical
/// visual/audio event may degrade; authority unaffected". That is the whole difference from
/// <see cref="DomainEvent"/>, and it is why the two buffers behave differently at their ceilings
/// rather than sharing one implementation.
/// </para>
/// <para>
/// <see cref="SourceEventCount"/> is what makes coalescing honest: a merged event says how many
/// emissions it stands for, so a consumer scaling an effect by count is not misled and a
/// diagnostic can reconcile emitted against delivered.
/// </para>
/// </remarks>
public readonly struct PresentationEvent : IEquatable<PresentationEvent>
{
    private readonly EventKind _kind;
    private readonly EventProvenance _provenance;
    private readonly EntityId _subjectId;
    private readonly double _positionX;
    private readonly double _positionY;
    private readonly EventPayload _payload;
    private readonly int _sourceEventCount;

    private PresentationEvent(
        EventKind kind,
        EventProvenance provenance,
        EntityId subjectId,
        double positionX,
        double positionY,
        EventPayload payload,
        int sourceEventCount)
    {
        _kind = kind;
        _provenance = provenance;
        _subjectId = subjectId;
        _positionX = positionX;
        _positionY = positionY;
        _payload = payload;
        _sourceEventCount = sourceEventCount;
    }

    /// <summary>The stable event kind, declared by the emitting system.</summary>
    public EventKind Kind => _kind;

    /// <summary>Tick, system phase, emission sequence, emitting entity, and source content ID.</summary>
    /// <remarks>
    /// A coalesced event keeps the provenance of the lowest-sequence source event, so the merged
    /// record still sorts where the first emission would have.
    /// </remarks>
    public EventProvenance Provenance => _provenance;

    /// <summary>The entity the instruction is about, or the explicit "no entity" identity.</summary>
    public EntityId SubjectId => _subjectId;

    /// <summary>
    /// The planar X component of the event position, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> this component and <see cref="PositionY"/> must be replaced by the single
    /// planar position type that <c>W2-GEO</c> owns once <c>GEO-001</c> lands. The change is:
    /// delete both <c>double</c> components and the <c>positionX</c>/<c>positionY</c> parameters of
    /// <see cref="Create"/>, replace them with one planar-position parameter and one property, and
    /// update <see cref="ToString"/>. Do not introduce a planar vector type in this package.
    /// </remarks>
    public double PositionX => _positionX;

    /// <summary>
    /// The planar Y component of the event position, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> see <see cref="PositionX"/>. Double rather than single precision per doc 20
    /// § Numeric and unit conventions, whose single-precision permission is conditional on a
    /// confirmation that does not exist yet.
    /// </remarks>
    public double PositionY => _positionY;

    /// <summary>The typed payload, interpreted by <see cref="Kind"/> and its schema version.</summary>
    public EventPayload Payload => _payload;

    /// <summary>
    /// How many emitted events this record stands for: one when verbatim, more when coalesced.
    /// </summary>
    public int SourceEventCount => _sourceEventCount;

    /// <summary>True when this record represents more than one emission.</summary>
    public bool IsCoalesced => _sourceEventCount > 1;

    /// <summary>True when every required field was supplied rather than defaulted.</summary>
    public bool IsComplete =>
        _kind.IsDeclared
        && _provenance.IsComplete
        && !_subjectId.IsUnset
        && _payload.IsTyped
        && _sourceEventCount >= 1
        && double.IsFinite(_positionX)
        && double.IsFinite(_positionY);

    /// <summary>Constructs a verbatim presentation event standing for exactly one emission.</summary>
    /// <param name="kind">The declared event kind.</param>
    /// <param name="provenance">The complete provenance.</param>
    /// <param name="subjectId">The entity the instruction is about, or the explicit "no entity" identity.</param>
    /// <param name="positionX">The planar X component in gameplay meters. Must be finite. <b>GEO-001</b>.</param>
    /// <param name="positionY">The planar Y component in gameplay meters. Must be finite. <b>GEO-001</b>.</param>
    /// <param name="payload">The typed payload.</param>
    /// <exception cref="ArgumentException">The kind is undeclared, the provenance is incomplete, or the payload is untyped.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The subject is unset, or a position component is not finite.</exception>
    public static PresentationEvent Create(
        EventKind kind,
        EventProvenance provenance,
        EntityId subjectId,
        double positionX,
        double positionY,
        EventPayload payload)
    {
        if (!kind.IsDeclared)
        {
            throw new ArgumentException("the event kind must be declared, not defaulted", nameof(kind));
        }

        if (!provenance.IsComplete)
        {
            throw new ArgumentException(
                "the provenance must carry tick, system phase, sequence, emitting entity, and "
                    + "source content ID",
                nameof(provenance));
        }

        if (!payload.IsTyped)
        {
            throw new ArgumentException(
                "the payload must be constructed through EventPayload.Typed",
                nameof(payload));
        }

        if (subjectId.IsUnset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subjectId),
                subjectId,
                "the subject may not be the unset identity; use EntityId.NoEntityIn");
        }

        RequireFinite(positionX, nameof(positionX));
        RequireFinite(positionY, nameof(positionY));

        return new PresentationEvent(kind, provenance, subjectId, positionX, positionY, payload, 1);
    }

    /// <summary>
    /// Returns this event standing for <paramref name="sourceEventCount"/> emissions.
    /// </summary>
    /// <param name="sourceEventCount">How many source events the record now represents. Must be at least one.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceEventCount"/> is less than one.</exception>
    /// <remarks>
    /// Only <see cref="PresentationEventBuffer"/> calls this, and only under an explicit policy.
    /// It returns a new value rather than mutating, because the event is immutable.
    /// </remarks>
    public PresentationEvent WithSourceEventCount(int sourceEventCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sourceEventCount, 1);
        return new PresentationEvent(
            _kind,
            _provenance,
            _subjectId,
            _positionX,
            _positionY,
            _payload,
            sourceEventCount);
    }

    /// <summary>Compares two events for exact equality of every field.</summary>
    public static bool operator ==(PresentationEvent left, PresentationEvent right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two events for inequality.</summary>
    public static bool operator !=(PresentationEvent left, PresentationEvent right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(PresentationEvent other)
    {
        return _kind == other._kind
            && _provenance == other._provenance
            && _subjectId == other._subjectId
            && _positionX.Equals(other._positionX)
            && _positionY.Equals(other._positionY)
            && _payload == other._payload
            && _sourceEventCount == other._sourceEventCount;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is PresentationEvent other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _kind,
            _provenance,
            _subjectId,
            _positionX,
            _positionY,
            _payload,
            _sourceEventCount);
    }

    /// <summary>Renders the event as canonical invariant text for goldens and diagnostics.</summary>
    public override string ToString()
    {
        return "presentation "
            + _provenance.ToString()
            + " kind="
            + _kind.ToString()
            + " subject="
            + _subjectId.ToString()
            + " at=("
            + _positionX.ToString("R", CultureInfo.InvariantCulture)
            + ","
            + _positionY.ToString("R", CultureInfo.InvariantCulture)
            + ") sources="
            + _sourceEventCount.ToString(CultureInfo.InvariantCulture)
            + " payload="
            + _payload.ToString();
    }

    private static void RequireFinite(double component, string parameterName)
    {
        if (!double.IsFinite(component))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                component,
                "an event position component must be finite");
        }
    }
}
