using System;
using System.Globalization;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// A tick-local buffer of disposable presentation instructions. It applies the coalescing policy at
/// publication and may degrade at its ceiling, because nothing here is authoritative.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "Presentation events
/// are disposable instructions", "Presentation events may be coalesced by an explicit visual policy",
/// and "Consumers never infer authoritative state solely from presentation events."
/// <c>CTR-SIM-002</c> in doc 115 § Cross-boundary contract registry: on failure "noncritical
/// visual/audio event may degrade; authority unaffected".
/// </para>
/// <para>
/// <b>The deliberate asymmetry with <see cref="DomainEventBuffer"/>.</b> That buffer has no drop
/// branch at all; this one has <see cref="Discard"/> and degrades at its ceiling. That is not
/// inconsistency, it is the whole distinction the two contracts draw, and it is why they are two types
/// rather than one parameterised one: a shared implementation with a "may drop" flag would put the
/// authoritative guarantee one boolean away from being wrong.
/// </para>
/// <para>
/// <b>Ordering happens before merging, always.</b> Merging walks the ordered batch and combines
/// adjacent records, so the merged result is a function of the authoritative order and never of the
/// order events arrived in or of any container's enumeration.
/// </para>
/// </remarks>
public sealed class PresentationEventBuffer
{
    private readonly int _hardMaximumCapacity;
    private PresentationEvent[] _events;
    private int _count;
    private long _tick;
    private bool _isOpenForTick;
    private long _degradedCount;

    /// <summary>Creates a buffer with an initial capacity and a ceiling at which it degrades.</summary>
    /// <param name="initialCapacity">The expected records per tick. Exceeding it grows the buffer.</param>
    /// <param name="hardMaximumCapacity">The ceiling beyond which further records degrade rather than being stored.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="initialCapacity"/> is not positive, or <paramref name="hardMaximumCapacity"/> is
    /// below it.
    /// </exception>
    public PresentationEventBuffer(int initialCapacity, int hardMaximumCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(hardMaximumCapacity, initialCapacity);
        _events = new PresentationEvent[initialCapacity];
        _hardMaximumCapacity = hardMaximumCapacity;
    }

    /// <summary>The ceiling beyond which records degrade.</summary>
    public int HardMaximumCapacity => _hardMaximumCapacity;

    /// <summary>The current backing capacity.</summary>
    public int Capacity => _events.Length;

    /// <summary>How many records the current tick holds.</summary>
    public int Count => _count;

    /// <summary>The tick this buffer is open for.</summary>
    public long Tick => _tick;

    /// <summary>Whether a tick is open for appending.</summary>
    public bool IsOpenForTick => _isOpenForTick;

    /// <summary>
    /// How many records were degraded at the ceiling since the buffer was created.
    /// </summary>
    /// <remarks>
    /// Counted rather than silent: doc 20 § Capacity and overload behavior lists "rejected visual
    /// requests" as a diagnostic metric, so a degrading frame is visible in the metrics even though it
    /// changes nothing authoritative.
    /// </remarks>
    public long DegradedCount => _degradedCount;

    /// <summary>Opens the buffer for one tick.</summary>
    /// <param name="tick">The authoritative tick. Must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tick"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">A tick is already open.</exception>
    /// <remarks>
    /// Requires the previous tick to have been published or discarded, so a batch cannot silently
    /// carry across a tick boundary and be attributed to the wrong tick's presentation.
    /// </remarks>
    public void BeginTick(long tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);
        if (_isOpenForTick)
        {
            throw new InvalidOperationException(
                "tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + " is still open; publish or discard it before beginning another");
        }

        _count = 0;
        _tick = tick;
        _isOpenForTick = true;
    }

    /// <summary>Appends one presentation instruction, or degrades it at the ceiling.</summary>
    /// <param name="presentationEvent">The complete event. Its provenance tick must be this buffer's tick.</param>
    /// <returns><see langword="false"/> when the record was degraded at the ceiling.</returns>
    /// <exception cref="ArgumentException">The event is incomplete, or belongs to a different tick.</exception>
    /// <exception cref="InvalidOperationException">No tick is open.</exception>
    public bool TryAppend(in PresentationEvent presentationEvent)
    {
        if (!_isOpenForTick)
        {
            throw new InvalidOperationException("no tick is open");
        }

        if (!presentationEvent.IsComplete)
        {
            throw new ArgumentException(
                "an incomplete presentation event cannot be appended; every field is required",
                nameof(presentationEvent));
        }

        if (presentationEvent.Provenance.Tick != _tick)
        {
            throw new ArgumentException(
                "the event belongs to tick "
                    + presentationEvent.Provenance.Tick.ToString(CultureInfo.InvariantCulture)
                    + " but the buffer is open for tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + "; buffers are tick-local",
                nameof(presentationEvent));
        }

        if (_count == _events.Length)
        {
            if (_count >= _hardMaximumCapacity)
            {
                _degradedCount++;
                return false;
            }

            Array.Resize(ref _events, Math.Min(_events.Length * 2, _hardMaximumCapacity));
        }

        _events[_count] = presentationEvent;
        _count++;
        return true;
    }

    /// <summary>
    /// Orders the tick's records, applies <paramref name="policy"/>, and writes the published batch
    /// into <paramref name="destination"/>.
    /// </summary>
    /// <param name="policy">The explicit coalescing policy. Use <see cref="PresentationCoalescingPolicy.Verbatim"/> for no merging.</param>
    /// <param name="destination">A caller-owned buffer of at least <see cref="Count"/> elements.</param>
    /// <returns>How many records the published batch holds, which is at most <see cref="Count"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> or <paramref name="destination"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is too small.</exception>
    /// <exception cref="InvalidOperationException">No tick is open, or two records share a tick and emission sequence.</exception>
    /// <remarks>
    /// Two records merge only when the policy names their kind, their kinds are equal, and their
    /// provenances share an origin (<see cref="EventProvenance.SharesOriginWith"/>). The survivor keeps
    /// the lowest-sequence provenance and reports the combined
    /// <see cref="PresentationEvent.SourceEventCount"/>, so nothing about the merge is invisible.
    /// </remarks>
    public int PublishOrderedTo(PresentationCoalescingPolicy policy, PresentationEvent[] destination)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(destination);
        if (!_isOpenForTick)
        {
            throw new InvalidOperationException("no tick is open");
        }

        if (destination.Length < _count)
        {
            throw new ArgumentException(
                "destination holds "
                    + destination.Length.ToString(CultureInfo.InvariantCulture)
                    + " but the tick holds "
                    + _count.ToString(CultureInfo.InvariantCulture),
                nameof(destination));
        }

        Array.Copy(_events, destination, _count);
        EventOrdering.Sort(destination, _count);
        EventOrdering.AssertTotalOrder(destination, _count);

        int written = 0;
        for (int index = 0; index < _count; index++)
        {
            PresentationEvent candidate = destination[index];

            if (written > 0
                && MergesWith(policy, destination[written - 1], candidate))
            {
                destination[written - 1] = destination[written - 1].WithSourceEventCount(
                    destination[written - 1].SourceEventCount + candidate.SourceEventCount);
                continue;
            }

            destination[written] = candidate;
            written++;
        }

        for (int index = written; index < _count; index++)
        {
            destination[index] = default;
        }

        return written;
    }

    /// <summary>Closes the tick, discarding the batch.</summary>
    /// <exception cref="InvalidOperationException">No tick is open.</exception>
    /// <remarks>
    /// Legal precisely because presentation events are disposable (doc 20 § Domain and presentation
    /// events). Nothing authoritative depends on the batch, so discarding one changes no committed
    /// state, no domain event, and no later tick. There is no counterpart on
    /// <see cref="DomainEventBuffer"/>, and there must not be.
    /// </remarks>
    public void Discard()
    {
        if (!_isOpenForTick)
        {
            throw new InvalidOperationException("no tick is open, so there is nothing to discard");
        }

        Array.Clear(_events, 0, _count);
        _count = 0;
        _isOpenForTick = false;
    }

    /// <summary>Closes the tick after its batch has been published.</summary>
    /// <exception cref="InvalidOperationException">No tick is open.</exception>
    /// <remarks>
    /// Identical in effect to <see cref="Discard"/> and separate in name only so a reader of a call
    /// site can tell an intended publication from an intended discard. doc 115 § Cross-boundary
    /// contract registry: a producer may reuse an internal buffer "only after the consumer-facing
    /// snapshot/batch lifetime has ended", and this call is where that lifetime ends.
    /// </remarks>
    public void Release()
    {
        Discard();
    }

    private static bool MergesWith(
        PresentationCoalescingPolicy policy,
        PresentationEvent survivor,
        PresentationEvent candidate)
    {
        return survivor.Kind == candidate.Kind
            && policy.TryGetMergeRule(candidate.Kind, out string _)
            && survivor.Provenance.SharesOriginWith(candidate.Provenance)
            && survivor.SubjectId == candidate.SubjectId;
    }
}
