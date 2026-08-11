using System;
using System.Globalization;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// A tick-local append-only buffer of authoritative facts. It grows rather than dropping, and it
/// refuses release while a record is unconsumed.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "domain events may
/// not be dropped" and "Statistics consume domain/damage records before their buffers are released."
/// § Tick transaction: systems "append damage, spawn, removal, payout, domain, presentation, and
/// metric records to tick-local buffers", and an invariant failure before commit "ends the run
/// through the safe technical-failure path".
/// <c>CTR-SIM-001</c> in doc 115 § Cross-boundary contract registry: "never dropped", and on failure
/// "invariant failure ends run safely rather than omitting authoritative event".
/// </para>
/// <para>
/// <b>Dropping is absent, not guarded against.</b> This type has no eviction, no overwrite-on-full,
/// no ring buffer, no priority discard, and no discard method. There are exactly two ways a record
/// leaves: <see cref="Release"/>, which throws unless every record has been consumed, and nothing
/// else. <see cref="Append"/> either stores the record, grows the array and stores it, or throws;
/// there is no fourth branch. So "a domain event is never dropped" is a property of the type's
/// shape rather than of the paths a test happened to exercise -
/// <c>DomainEventBufferTests.NoRemovalPathExistsBeyondAConsumedRelease</c> pins the public surface
/// so a discard branch cannot be added later without a red test.
/// </para>
/// <para>
/// <b>Why a hard maximum exists at all.</b> Unbounded growth is not a stronger guarantee, it is a
/// different failure: an emitter looping forever would exhaust memory with no diagnosis. doc 20 §
/// Capacity and overload behavior's answer to a genuine authoritative ceiling is a failed invariant,
/// so at <see cref="HardMaximumCapacity"/> the tick fails loudly with every already-appended record
/// still present and inspectable, which is the opposite of omitting one.
/// </para>
/// <para>
/// Public because <c>CTR-SIM-001</c>'s consumers - "other simulation owners and <c>CMP-OBS-001</c>"
/// - are outside this assembly, and the never-dropped and consume-before-release obligations are
/// theirs to satisfy as well as this type's to enforce.
/// </para>
/// </remarks>
public sealed class DomainEventBuffer
{
    private readonly int _hardMaximumCapacity;
    private DomainEvent[] _events;
    private int _count;
    private int _consumedCount;
    private long _tick;
    private bool _isOpenForTick;
    private int _growthCount;
    private long _appendedInRun;
    private long _releasedInRun;

    /// <summary>Creates a buffer with an initial capacity and a hard ceiling.</summary>
    /// <param name="initialCapacity">
    /// The expected records per tick. Exceeding it grows the buffer; it is a starting size, not a
    /// limit.
    /// </param>
    /// <param name="hardMaximumCapacity">
    /// The ceiling at which the tick fails its invariant rather than growing further.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="initialCapacity"/> is not positive, or <paramref name="hardMaximumCapacity"/>
    /// is below it.
    /// </exception>
    public DomainEventBuffer(int initialCapacity, int hardMaximumCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(hardMaximumCapacity, initialCapacity);
        _events = new DomainEvent[initialCapacity];
        _hardMaximumCapacity = hardMaximumCapacity;
    }

    /// <summary>The ceiling at which appending fails the tick invariant.</summary>
    public int HardMaximumCapacity => _hardMaximumCapacity;

    /// <summary>The current backing capacity, which grows on demand.</summary>
    public int Capacity => _events.Length;

    /// <summary>How many times the buffer has grown rather than dropped a record.</summary>
    /// <remarks>Observable so a test can prove growth happened rather than inferring it from the absence of loss.</remarks>
    public int GrowthCount => _growthCount;

    /// <summary>How many records the current tick holds.</summary>
    public int Count => _count;

    /// <summary>How many of the current tick's records statistics have acknowledged.</summary>
    public int ConsumedCount => _consumedCount;

    /// <summary>The tick this buffer is open for.</summary>
    public long Tick => _tick;

    /// <summary>Whether a tick is open for appending.</summary>
    public bool IsOpenForTick => _isOpenForTick;

    /// <summary>Every record appended since the buffer was created.</summary>
    /// <remarks>
    /// Paired with <see cref="ReleasedInRun"/> it is a run-long invariant rather than a per-call
    /// assertion: the two must be equal at every tick boundary, and no code path can make them
    /// differ without either failing the tick or refusing the release.
    /// </remarks>
    public long AppendedInRun => _appendedInRun;

    /// <summary>Every record released after being consumed, since the buffer was created.</summary>
    public long ReleasedInRun => _releasedInRun;

    /// <summary>Opens the buffer for one tick.</summary>
    /// <param name="tick">The authoritative tick. Must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tick"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">
    /// A tick is already open, or the previous tick was never released.
    /// </exception>
    /// <remarks>
    /// doc 20 § Tick transaction makes the buffers tick-local, so a tick cannot begin over the
    /// residue of the last one. Refusing here rather than clearing is the point: clearing would be a
    /// drop.
    /// </remarks>
    public void BeginTick(long tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);
        if (_isOpenForTick)
        {
            throw new InvalidOperationException(
                "tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + " is still open; a tick-local buffer cannot begin a second tick over the first");
        }

        if (_count != 0)
        {
            throw new InvalidOperationException(
                "the previous tick left "
                    + _count.ToString(CultureInfo.InvariantCulture)
                    + " record(s) unreleased; beginning a tick over them would drop authoritative "
                    + "events, which doc 20 § Domain and presentation events forbids");
        }

        _tick = tick;
        _isOpenForTick = true;
    }

    /// <summary>Appends one authoritative fact.</summary>
    /// <param name="domainEvent">The complete event. Its provenance tick must be this buffer's tick.</param>
    /// <exception cref="ArgumentException">The event is incomplete, or belongs to a different tick.</exception>
    /// <exception cref="InvalidOperationException">
    /// No tick is open, or the hard maximum capacity would be exceeded, which fails the tick
    /// invariant.
    /// </exception>
    /// <remarks>
    /// Three outcomes and no others: stored, stored after growth, or a thrown invariant failure. In
    /// particular there is no outcome in which the call returns having not stored the record.
    /// </remarks>
    public void Append(in DomainEvent domainEvent)
    {
        if (!_isOpenForTick)
        {
            throw new InvalidOperationException(
                "no tick is open; doc 20 § Tick transaction appends only within a tick");
        }

        if (!domainEvent.IsComplete)
        {
            throw new ArgumentException(
                "an incomplete domain event cannot be appended; every field is required",
                nameof(domainEvent));
        }

        if (domainEvent.Provenance.Tick != _tick)
        {
            throw new ArgumentException(
                "the event belongs to tick "
                    + domainEvent.Provenance.Tick.ToString(CultureInfo.InvariantCulture)
                    + " but the buffer is open for tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + "; buffers are tick-local",
                nameof(domainEvent));
        }

        if (_count == _events.Length)
        {
            Grow();
        }

        _events[_count] = domainEvent;
        _count++;
        _appendedInRun++;
    }

    /// <summary>
    /// Copies every record of the current tick into <paramref name="destination"/> in the documented
    /// order.
    /// </summary>
    /// <param name="destination">
    /// A caller-owned buffer of at least <see cref="Count"/> elements. Caller-owned because doc 115 §
    /// Cross-boundary contract registry forbids exposing a mutable collection across a boundary.
    /// </param>
    /// <returns>How many records were written, which is always <see cref="Count"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is too small.</exception>
    /// <exception cref="InvalidOperationException">Two records share a tick and emission sequence.</exception>
    /// <remarks>
    /// Always writes every record. There is no filter, no cap, and no partial mode, so a caller
    /// cannot ask for a subset and there is no code path by which a record is present in the buffer
    /// but absent from the batch.
    /// </remarks>
    public int CopyOrderedTo(DomainEvent[] destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < _count)
        {
            throw new ArgumentException(
                "destination holds "
                    + destination.Length.ToString(CultureInfo.InvariantCulture)
                    + " but the tick holds "
                    + _count.ToString(CultureInfo.InvariantCulture)
                    + "; a domain batch is never truncated",
                nameof(destination));
        }

        Array.Copy(_events, destination, _count);
        EventOrdering.Sort(destination, _count);
        EventOrdering.AssertTotalOrder(destination, _count);
        return _count;
    }

    /// <summary>Acknowledges that statistics have consumed <paramref name="count"/> further records.</summary>
    /// <param name="count">How many more records were consumed.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is negative, or would push the consumed count past
    /// <see cref="Count"/>.
    /// </exception>
    /// <remarks>
    /// doc 20 § Domain and presentation events: "Statistics consume domain/damage records before
    /// their buffers are released." Acknowledgement is separate from delivery because copying a batch
    /// out is not the same act as a statistics owner having folded it in.
    /// </remarks>
    public void RecordConsumed(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_consumedCount + count, _count);
        _consumedCount += count;
    }

    /// <summary>Acknowledges that statistics have consumed every record of the current tick.</summary>
    public void RecordAllConsumed()
    {
        _consumedCount = _count;
    }

    /// <summary>
    /// Closes the tick and empties the buffer, refusing while any record is unconsumed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// No tick is open, or a record has not been consumed - releasing then would drop an
    /// authoritative event.
    /// </exception>
    /// <remarks>
    /// The only path by which a record leaves this buffer, and it is conditional. That is what makes
    /// "never dropped" structural: there is nowhere else to go.
    /// </remarks>
    public void Release()
    {
        if (!_isOpenForTick)
        {
            throw new InvalidOperationException("no tick is open, so there is nothing to release");
        }

        if (_consumedCount != _count)
        {
            throw new InvalidOperationException(
                "tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + " holds "
                    + _count.ToString(CultureInfo.InvariantCulture)
                    + " record(s) of which "
                    + _consumedCount.ToString(CultureInfo.InvariantCulture)
                    + " were consumed; doc 20 § Domain and presentation events requires statistics "
                    + "to consume domain records before their buffers are released, so releasing "
                    + "now would drop "
                    + (_count - _consumedCount).ToString(CultureInfo.InvariantCulture)
                    + " authoritative event(s)");
        }

        Array.Clear(_events, 0, _count);
        _releasedInRun += _count;
        _count = 0;
        _consumedCount = 0;
        _isOpenForTick = false;
    }

    private void Grow()
    {
        if (_count >= _hardMaximumCapacity)
        {
            throw new InvalidOperationException(
                "the domain event buffer reached its hard maximum of "
                    + _hardMaximumCapacity.ToString(CultureInfo.InvariantCulture)
                    + " records on tick "
                    + _tick.ToString(CultureInfo.InvariantCulture)
                    + ". doc 20 § Domain and presentation events forbids dropping a domain event, so "
                    + "the tick fails its invariant and the run ends through the safe "
                    + "technical-failure path with every appended record still present. This is a "
                    + "defect in the emitter, not a capacity to raise casually.");
        }

        int grown = Math.Min(_events.Length * 2, _hardMaximumCapacity);
        Array.Resize(ref _events, grown);
        _growthCount++;
    }
}
