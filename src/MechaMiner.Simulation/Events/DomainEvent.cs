using System;
using MechaMiner.Simulation.Entities;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// An immutable authoritative fact: an entity was defeated, a resource was awarded, a threshold
/// was crossed, the run reached a terminal result.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "Domain events
/// are immutable facts used by other authoritative or application systems", and "domain events
/// may not be dropped".
/// </para>
/// <para>
/// <c>CTR-SIM-001</c> in doc 115 § Cross-boundary contract registry: produced by "simulation
/// components", consumed by "other simulation owners and <c>CMP-OBS-001</c>", delivered
/// "tick-local, stable sequence, never dropped", and on failure "invariant failure ends run
/// safely rather than omitting authoritative event". The never-dropped half is enforced by
/// <see cref="DomainEventBuffer"/>, which has no branch that could drop one; this type is the
/// immutable record itself.
/// </para>
/// <para>
/// Immutable in the strong sense: a readonly struct with no setter and no member whose type can be
/// mutated, so there is nothing for a consumer to write through.
/// </para>
/// </remarks>
public readonly struct DomainEvent : IEquatable<DomainEvent>
{
    private readonly EventKind _kind;
    private readonly EventProvenance _provenance;
    private readonly EntityId _subjectId;
    private readonly double _positionX;
    private readonly double _positionY;
    private readonly EventPayload _payload;

    private DomainEvent(
        EventKind kind,
        EventProvenance provenance,
        EntityId subjectId,
        double positionX,
        double positionY,
        EventPayload payload)
    {
        _kind = kind;
        _provenance = provenance;
        _subjectId = subjectId;
        _positionX = positionX;
        _positionY = positionY;
        _payload = payload;
    }

    /// <summary>The stable event kind, declared by the emitting system.</summary>
    public EventKind Kind => _kind;

    /// <summary>Tick, system phase, emission sequence, emitting entity, and source content ID.</summary>
    public EventProvenance Provenance => _provenance;

    /// <summary>
    /// The entity the fact is about, which may differ from the emitting entity, or the explicit
    /// "no entity" identity.
    /// </summary>
    /// <remarks>
    /// doc 20 § Domain and presentation events says events carry "relevant entity/content IDs",
    /// plural. A defeat emitted by a weapon actor is about its target, so the emitter and the
    /// subject are two different identities and both are carried.
    /// </remarks>
    public EntityId SubjectId => _subjectId;

    /// <summary>
    /// The planar X component of the event position, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> this component and <see cref="PositionY"/> must be replaced by the single
    /// planar position type that <c>W2-GEO</c> owns once <c>GEO-001</c> lands. The change is:
    /// delete both <c>double</c> components and the <c>positionX</c>/<c>positionY</c> parameters of
    /// <see cref="Create"/>, replace them with one planar-position parameter and one property, and
    /// update <see cref="ToString"/>. Do not introduce a planar vector type in this package - doc
    /// 20 § Numeric and unit conventions gives positions to the geometry boundary, and a second
    /// vector type is what the directory split exists to prevent.
    /// </remarks>
    public double PositionX => _positionX;

    /// <summary>
    /// The planar Y component of the event position, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> see <see cref="PositionX"/>. Double rather than single precision because
    /// doc 20 § Numeric and unit conventions permits single for planar transforms only "after tests
    /// confirm the accepted map scale remains safely within precision bounds", and that
    /// confirmation does not exist.
    /// </remarks>
    public double PositionY => _positionY;

    /// <summary>The typed payload, interpreted by <see cref="Kind"/> and its schema version.</summary>
    public EventPayload Payload => _payload;

    /// <summary>True when every required field was supplied rather than defaulted.</summary>
    public bool IsComplete =>
        _kind.IsDeclared
        && _provenance.IsComplete
        && !_subjectId.IsUnset
        && _payload.IsTyped
        && double.IsFinite(_positionX)
        && double.IsFinite(_positionY);

    /// <summary>Constructs a domain event. Every field is required.</summary>
    /// <param name="kind">The declared event kind.</param>
    /// <param name="provenance">The complete provenance.</param>
    /// <param name="subjectId">The entity the fact is about, or the explicit "no entity" identity.</param>
    /// <param name="positionX">The planar X component in gameplay meters. Must be finite. <b>GEO-001</b>.</param>
    /// <param name="positionY">The planar Y component in gameplay meters. Must be finite. <b>GEO-001</b>.</param>
    /// <param name="payload">The typed payload.</param>
    /// <exception cref="ArgumentException">The kind is undeclared, the provenance is incomplete, or the payload is untyped.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The subject is unset, or a position component is not finite.</exception>
    /// <remarks>
    /// Validation rather than tolerance, because doc 20 § Domain and presentation events makes
    /// these facts the input to other authoritative systems: an incomplete fact would be an
    /// authoritative record that nothing can interpret, and <c>CTR-SIM-001</c> says the answer to
    /// that is to fail rather than to deliver it.
    /// </remarks>
    public static DomainEvent Create(
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
                "the payload must be constructed through EventPayload.Typed, so its schema version "
                    + "and content reference are present",
                nameof(payload));
        }

        if (subjectId.IsUnset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(subjectId),
                subjectId,
                "the subject may not be the unset identity; a run-scoped fact uses EntityId.NoEntityIn");
        }

        RequireFinite(positionX, nameof(positionX));
        RequireFinite(positionY, nameof(positionY));

        return new DomainEvent(kind, provenance, subjectId, positionX, positionY, payload);
    }

    /// <summary>Compares two events for exact equality of every field.</summary>
    public static bool operator ==(DomainEvent left, DomainEvent right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two events for inequality.</summary>
    public static bool operator !=(DomainEvent left, DomainEvent right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(DomainEvent other)
    {
        return _kind == other._kind
            && _provenance == other._provenance
            && _subjectId == other._subjectId
            && _positionX.Equals(other._positionX)
            && _positionY.Equals(other._positionY)
            && _payload == other._payload;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is DomainEvent other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_kind, _provenance, _subjectId, _positionX, _positionY, _payload);
    }

    /// <summary>Renders the event as canonical invariant text for goldens and diagnostics.</summary>
    public override string ToString()
    {
        return "domain "
            + _provenance.ToString()
            + " kind="
            + _kind.ToString()
            + " subject="
            + _subjectId.ToString()
            + " at=("
            + _positionX.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
            + ","
            + _positionY.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
            + ") payload="
            + _payload.ToString();
    }

    private static void RequireFinite(double component, string parameterName)
    {
        if (!double.IsFinite(component))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                component,
                "an event position component must be finite; doc 20 § Scope and invariants requires "
                    + "authoritative positions to be valid planar positions");
        }
    }
}
