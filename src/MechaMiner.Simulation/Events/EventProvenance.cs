using System;
using System.Globalization;
using MechaMiner.Simulation.Entities;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// Where an event came from: the tick, the system phase that emitted it, the emission sequence,
/// the emitting entity, and the source content ID.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "Events carry
/// tick, sequence, stable event kind, relevant entity/content IDs, position, and typed payload."
/// The kind, position, and payload live on the event; the rest is provenance, because it is what
/// the ordering rule reads.
/// </para>
/// <para>
/// <b>The sequence is authored data, not the order the buffer happened to receive it in.</b>
/// <c>VER-SIM-006-003</c> requires two runs that emit the same events in different collection
/// order to produce identical batches. If the buffer stamped a sequence on arrival, a different
/// arrival order would produce different sequences and therefore a different batch, and the
/// ordering rule would be untestable. So the emitting system obtains a sequence from
/// <c>CMP-SIM-003</c> - the one component doc 115 § Component registry says owns "the event
/// sequence" - and the buffer never assigns one.
/// </para>
/// <para>
/// <b>This is not <c>docs/technical/22-combat-and-weapon-runtime.md</c>'s attack provenance.</b>
/// Different concept in a different package; do not merge them.
/// </para>
/// <para>
/// Public because <c>CTR-SIM-001</c> and <c>CTR-SIM-002</c> deliver events carrying it to
/// consumers outside this assembly, and doc 30 § Snapshot synchronization requires presentation
/// to detect "missed event sequence numbers", which it can only do if the sequence crosses the
/// boundary.
/// </para>
/// </remarks>
public readonly struct EventProvenance : IEquatable<EventProvenance>
{
    /// <summary>The first system phase of doc 10 § System phase ordering.</summary>
    public const int FirstSystemPhase = 1;

    /// <summary>
    /// The last system phase of doc 10 § System phase ordering: "Publish metrics, ordered events,
    /// and the presentation snapshot."
    /// </summary>
    /// <remarks>
    /// Fourteen, exactly, because doc 10 § System phase ordering numbers a fixed fourteen-phase
    /// order and says "observable ordering changes require regression tests and an update here".
    /// A phase outside the range is a system that does not exist.
    /// </remarks>
    public const int LastSystemPhase = 14;

    private readonly long _tick;
    private readonly int _systemPhase;
    private readonly long _sequence;
    private readonly EntityId _emittingEntityId;
    private readonly string? _sourceContentId;

    private EventProvenance(
        long tick,
        int systemPhase,
        long sequence,
        EntityId emittingEntityId,
        string sourceContentId)
    {
        _tick = tick;
        _systemPhase = systemPhase;
        _sequence = sequence;
        _emittingEntityId = emittingEntityId;
        _sourceContentId = sourceContentId;
    }

    /// <summary>The authoritative tick this event belongs to.</summary>
    /// <remarks>doc 20 § Numeric and unit conventions: run time is a "64-bit integer simulation tick".</remarks>
    public long Tick => _tick;

    /// <summary>The system phase that emitted it, from doc 10 § System phase ordering.</summary>
    /// <remarks>The first ordering key, because a later phase's outcome must never sort before an earlier phase's.</remarks>
    public int SystemPhase => _systemPhase;

    /// <summary>The emission sequence within the tick, issued by <c>CMP-SIM-003</c>.</summary>
    public long Sequence => _sequence;

    /// <summary>
    /// The entity that emitted the event, or the explicit "no entity" identity for a run-scoped
    /// event.
    /// </summary>
    /// <remarks>
    /// doc 20 § Entity identity: "Cross-system references store entity IDs, never direct mutable
    /// object references." The last ordering key, per "Stable ordering uses the full entity ID
    /// after a system's authored priority keys".
    /// </remarks>
    public EntityId EmittingEntityId => _emittingEntityId;

    /// <summary>The stable content ID of the definition that caused the event.</summary>
    /// <remarks>doc 20 § Scope and invariants: "every content reference resolves through the immutable run content registry".</remarks>
    public string SourceContentId => _sourceContentId ?? string.Empty;

    /// <summary>True when every required component was supplied rather than defaulted.</summary>
    public bool IsComplete =>
        _systemPhase >= FirstSystemPhase
        && _systemPhase <= LastSystemPhase
        && _tick >= 0
        && _sequence >= 0
        && !_emittingEntityId.IsUnset
        && SourceContentId.Length > 0;

    /// <summary>Constructs a complete provenance. Every component is required.</summary>
    /// <param name="tick">The authoritative tick. Must not be negative.</param>
    /// <param name="systemPhase">The emitting phase, in <see cref="FirstSystemPhase"/>..<see cref="LastSystemPhase"/>.</param>
    /// <param name="sequence">The emission sequence within the tick. Must not be negative.</param>
    /// <param name="emittingEntityId">
    /// The emitting entity, or <see cref="EntityId.NoEntityIn"/> for a run-scoped event. Must not
    /// be the unset default.
    /// </param>
    /// <param name="sourceContentId">The causing definition's stable content ID. Must not be blank.</param>
    /// <exception cref="ArgumentOutOfRangeException">A numeric component is outside its domain, or the entity is unset.</exception>
    /// <exception cref="ArgumentException"><paramref name="sourceContentId"/> is blank.</exception>
    /// <remarks>
    /// Rejecting <see cref="EntityId.Unset"/> rather than tolerating it is the point: doc 20 §
    /// Entity identity requires invalid references to fail closed, and an event carrying a
    /// defaulted identity would name slot zero of no run - which is the reserved player slot's
    /// index. A run-scoped event says so with <see cref="EntityId.NoEntityIn"/>.
    /// </remarks>
    public static EventProvenance Create(
        long tick,
        int systemPhase,
        long sequence,
        EntityId emittingEntityId,
        string sourceContentId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);
        ArgumentOutOfRangeException.ThrowIfNegative(sequence);
        ArgumentOutOfRangeException.ThrowIfLessThan(systemPhase, FirstSystemPhase);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(systemPhase, LastSystemPhase);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceContentId);

        if (emittingEntityId.IsUnset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emittingEntityId),
                emittingEntityId,
                "an event may not carry the unset identity; a run-scoped event uses "
                    + "EntityId.NoEntityIn so that 'no entity' is explicit rather than defaulted");
        }

        return new EventProvenance(tick, systemPhase, sequence, emittingEntityId, sourceContentId);
    }

    /// <summary>
    /// Whether two provenances share an origin: the same tick, phase, emitting entity, and source
    /// content, differing only in sequence.
    /// </summary>
    /// <remarks>
    /// This is what "the same provenance" can mean for coalescing. Two events emitted by one
    /// system in one tick necessarily carry different sequence numbers, so requiring the sequence
    /// to match too would make coalescing impossible rather than merely explicit.
    /// <c>VER-SIM-006-002</c>'s "never merges events of ... different provenance" is enforced
    /// through this predicate.
    /// </remarks>
    public bool SharesOriginWith(EventProvenance other)
    {
        return _tick == other._tick
            && _systemPhase == other._systemPhase
            && _emittingEntityId == other._emittingEntityId
            && string.Equals(SourceContentId, other.SourceContentId, StringComparison.Ordinal);
    }

    /// <summary>
    /// The documented stable order: tick, then system phase, then emission sequence. There is no
    /// fourth key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// doc 10 § System phase ordering: "Simultaneous outcomes use documented stable ordering rather
    /// than collection or thread timing." The tick leads because a buffer that somehow held two
    /// ticks must still order them, even though a tick-local buffer never does.
    /// </para>
    /// <para>
    /// <b>Why the emitting entity ID is not a tiebreak here.</b> The emission sequence is per-tick
    /// global: <c>CMP-SIM-003</c> issues it monotonically across the whole tick regardless of phase
    /// or emitter, so <c>(tick, sequence)</c> is already a total order by itself. Two events in one
    /// tick sharing a sequence is therefore not a tie to be broken but an impossible input - a
    /// defect in the issuer - and a comparator that fell through to a further key would silently
    /// give it an order and hide the bug that produced it. The key that used to sit here was
    /// consequently unreachable for every legal input. It has been replaced by a live invariant:
    /// <see cref="EventOrdering.AssertSequenceUniqueWithinTick(DomainEvent[], int)"/> fails loudly
    /// on a duplicate.
    /// </para>
    /// <para>
    /// <b>Scoped to events.</b> doc 20 § Boundary and tie ordering defines a separate five-key sort
    /// for damage instances - "resolve by system phase, explicit attack sequence, target ID, source
    /// ID, then insertion sequence" - which does carry identity keys and is untouched by the
    /// reasoning above. Do not generalise this comparison beyond the event buffers.
    /// </para>
    /// </remarks>
    public static int Compare(EventProvenance left, EventProvenance right)
    {
        int byTick = left._tick.CompareTo(right._tick);
        if (byTick != 0)
        {
            return byTick;
        }

        int byPhase = left._systemPhase.CompareTo(right._systemPhase);
        if (byPhase != 0)
        {
            return byPhase;
        }

        return left._sequence.CompareTo(right._sequence);
    }

    /// <summary>Compares two provenances for equality of every component.</summary>
    public static bool operator ==(EventProvenance left, EventProvenance right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two provenances for inequality.</summary>
    public static bool operator !=(EventProvenance left, EventProvenance right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(EventProvenance other)
    {
        return _tick == other._tick
            && _systemPhase == other._systemPhase
            && _sequence == other._sequence
            && _emittingEntityId == other._emittingEntityId
            && string.Equals(SourceContentId, other.SourceContentId, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EventProvenance other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _tick,
            _systemPhase,
            _sequence,
            _emittingEntityId,
            StringComparer.Ordinal.GetHashCode(SourceContentId));
    }

    /// <summary>Renders the provenance as canonical invariant text.</summary>
    public override string ToString()
    {
        return "tick="
            + _tick.ToString(CultureInfo.InvariantCulture)
            + " phase="
            + _systemPhase.ToString(CultureInfo.InvariantCulture).PadLeft(2)
            + " seq="
            + _sequence.ToString(CultureInfo.InvariantCulture).PadLeft(4)
            + " from="
            + _emittingEntityId.ToString()
            + " source="
            + SourceContentId;
    }
}
