using System;
using System.Globalization;

namespace MechaMiner.Simulation.Entities;

/// <summary>
/// An index-addressed packed store for one authoritative population category: plain
/// arrays, a dense record region, and iteration ordered by authored priority key then full
/// entity ID.
/// </summary>
/// <typeparam name="TState">
/// The category's record. Constrained to a value type so the dense region is one
/// contiguous array with no per-record object and no boxing.
/// </typeparam>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Entity identity: "The implementation uses
/// purpose-built packed stores by population category, not a general reflection-driven ECS
/// framework." § Capacity and overload behavior gives the soft target, hard capacity, and
/// overflow behaviour. doc 20 § Entity identity gives the ordering rule: "Stable ordering
/// uses the full entity ID after a system's authored priority keys."
/// </para>
/// <para>
/// <b>Plain arrays, never pointers.</b> <c>AllowUnsafeBlocks</c> is false repository-wide
/// (<c>Directory.Build.props</c>), so the packing is an index-addressed array set plus a
/// free list rather than anything reinterpreted. That is sufficient for the allocation-free
/// churn the performance posture requires: after warm-up a full
/// admit-mutate-remove cycle touches only preallocated arrays.
/// </para>
/// <para>
/// <b>Removal is a swap-remove, so storage order is deliberately not insertion order.</b>
/// Iteration order therefore cannot accidentally inherit insertion order; it can only come
/// from the comparison in <see cref="CopyOrderedTo"/>. doc 10 § System phase ordering:
/// "Simultaneous outcomes use documented stable ordering rather than collection or thread
/// timing."
/// </para>
/// <para>
/// <b>The dense region never grows and is never replaced.</b> Every dense array is a
/// <see langword="readonly"/> field sized to the hard capacity at construction, so a churn
/// cycle cannot allocate: there is no code path that enlarges one or assigns a new one. Only
/// the authored-spawn queue may grow, and it reports doing so through
/// <see cref="EntityDiagnostics.StoreGrowthCount"/>.
/// </para>
/// <para>
/// <b>No dictionary anywhere.</b> Slot-to-record lookup is an array indexed by slot, not a
/// hash container, so no observable order can leak out of hash iteration - the failure this
/// store is most exposed to.
/// </para>
/// <para>
/// Public because <c>CTR-SIM-003</c>'s registered consumers are outside this assembly and
/// the store's capacity and ordering behaviour has no other observable surface; the store's
/// arrays themselves are private and are never handed out.
/// </para>
/// </remarks>
public sealed class PackedEntityStore<TState>
    where TState : struct
{
    private readonly PopulationCategory _category;
    private readonly StoreCapacity _capacity;
    private readonly EntityIdAllocator _allocator;
    private readonly EntityDiagnostics _diagnostics;
    private readonly int _slotOffset;

    private readonly int[] _slotToDense;
    private readonly EntityId[] _denseIds;
    private readonly TState[] _denseStates;
    private readonly long[] _densePriorityKeys;
    private readonly int[] _order;

    private long[] _queuedPriorityKeys;
    private TState[] _queuedStates;
    private int _queuedCount;
    private int _count;

    /// <summary>
    /// Creates the store for one category, sharing the run's allocator and that category's
    /// diagnostic counters.
    /// </summary>
    /// <param name="category">One of the twelve authoritative categories.</param>
    /// <param name="allocator">The run's allocator. Supplies the run fence and the slot partition.</param>
    /// <exception cref="ArgumentNullException"><paramref name="allocator"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="category"/> is not one of the twelve, so it is an unregistered
    /// category and has no store.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The category declares <see cref="OverflowBehaviour.DegradePresentation"/>, which no
    /// authoritative population may.
    /// </exception>
    public PackedEntityStore(PopulationCategory category, EntityIdAllocator allocator)
    {
        ArgumentNullException.ThrowIfNull(allocator);

        _capacity = allocator.CapacityFor(category);
        if (_capacity.Overflow == OverflowBehaviour.DegradePresentation)
        {
            throw new ArgumentException(
                "an authoritative population category may not degrade under pressure: doc 20 "
                    + "§ Capacity and overload behavior confines degradation to visual-only "
                    + "pools and states that authoritative records 'may not disappear because "
                    + "a visual pool is full'",
                nameof(category));
        }

        _category = category;
        _allocator = allocator;
        _diagnostics = allocator.DiagnosticsFor(category);
        _slotOffset = allocator.SlotOffsetFor(category);

        int hardCapacity = _capacity.HardCapacity;
        _slotToDense = new int[hardCapacity];
        _denseIds = new EntityId[hardCapacity];
        _denseStates = new TState[hardCapacity];
        _densePriorityKeys = new long[hardCapacity];
        _order = new int[hardCapacity];
        _queuedPriorityKeys = new long[hardCapacity];
        _queuedStates = new TState[hardCapacity];

        for (int slot = 0; slot < hardCapacity; slot++)
        {
            _slotToDense[slot] = -1;
        }

        // doc 20 § Scope and invariants: the player exists from the start of the run.
        // Its identity is issued by the allocator's constructor, so the player store has
        // to adopt it rather than allocate a second one.
        if (category == PopulationCategory.Player)
        {
            Place(allocator.PlayerId, 0L, default);
        }
    }

    /// <summary>The category this store holds.</summary>
    public PopulationCategory Category => _category;

    /// <summary>This store's declared soft target, margin, hard capacity, and overflow behaviour.</summary>
    public StoreCapacity Capacity => _capacity;

    /// <summary>The shared diagnostic counters for this category.</summary>
    public EntityDiagnostics Diagnostics => _diagnostics;

    /// <summary>How many records are resident.</summary>
    public int Count => _count;

    /// <summary>
    /// Whether the store has reached its soft target and is therefore reporting pressure.
    /// </summary>
    /// <remarks>
    /// doc 20 § Capacity and overload behavior distinguishes the soft target from the hard
    /// capacity: the soft target is the expected population, and exceeding it is a signal,
    /// not a fault.
    /// </remarks>
    public bool IsUnderPressure => _count >= _capacity.SoftTarget;

    /// <summary>How many authored requests are waiting for capacity.</summary>
    /// <remarks>
    /// doc 20 § Capacity and overload behavior: "Authored enemies that reach a gameplay
    /// ceiling queue and later enter; they are not silently canceled or converted."
    /// </remarks>
    public int QueueDepth => _queuedCount;

    /// <summary>
    /// Admits one record, or applies this store's overflow behaviour when the store is at
    /// hard capacity.
    /// </summary>
    /// <param name="priorityKey">
    /// The emitting system's authored priority key. Ordering compares it before the entity
    /// ID (doc 20 § Entity identity).
    /// </param>
    /// <param name="state">The record to store.</param>
    /// <param name="id">
    /// The issued identity, or <see cref="EntityId.Unset"/> when the record was queued
    /// instead of admitted.
    /// </param>
    /// <returns><see langword="true"/> when the record is resident now.</returns>
    /// <exception cref="InvalidOperationException">
    /// The store declares <see cref="OverflowBehaviour.FailInvariant"/> and is at hard
    /// capacity.
    /// </exception>
    /// <remarks>
    /// Returning <see langword="false"/> for a queued authored record is deliberate: the
    /// record is retained, not admitted, and <see cref="QueueDepth"/> is the evidence that
    /// nothing was dropped. No resident record is ever evicted to make room, which is what
    /// doc 20 § Capacity and overload behavior forbids when it says a hard breach "is a
    /// failed invariant ... not a runtime balancing tool".
    /// </remarks>
    public bool TryAdmit(long priorityKey, in TState state, out EntityId id)
    {
        if (_allocator.TryAllocate(_category, out EntityId allocated))
        {
            Place(allocated, priorityKey, state);
            id = allocated;
            return true;
        }

        id = EntityId.Unset;

        if (_capacity.Overflow == OverflowBehaviour.QueueAuthored)
        {
            Enqueue(priorityKey, state);
            return false;
        }

        throw new InvalidOperationException(
            "hard authoritative capacity breach in the "
                + _category.ToString()
                + " store at "
                + _capacity.HardCapacity.ToString(CultureInfo.InvariantCulture)
                + " records: doc 20 § Capacity and overload behavior makes this a failed "
                + "invariant, caught with the offending batch resident, not a runtime "
                + "balancing tool");
    }

    /// <summary>
    /// Admits as many queued authored records as the store now has capacity for, in the
    /// order they were queued.
    /// </summary>
    /// <returns>How many records entered.</returns>
    /// <remarks>
    /// doc 10 § System phase ordering phase 3 materializes queued spawns that have capacity,
    /// and phase 12 applies deferred creation and capacity queues. The queue drains in
    /// arrival order, which is authored order: a queue that reordered would make the
    /// schedule's authored sequence unobservable.
    /// </remarks>
    public int AdmitQueued()
    {
        int admitted = 0;
        while (_queuedCount > 0 && _allocator.HasCapacity(_category))
        {
            long priorityKey = _queuedPriorityKeys[0];
            TState state = _queuedStates[0];

            if (!_allocator.TryAllocate(_category, out EntityId id))
            {
                break;
            }

            Place(id, priorityKey, state);
            admitted++;

            _queuedCount--;
            Array.Copy(_queuedPriorityKeys, 1, _queuedPriorityKeys, 0, _queuedCount);
            Array.Copy(_queuedStates, 1, _queuedStates, 0, _queuedCount);
            _diagnostics.SetQueueDepth(_queuedCount);
        }

        return admitted;
    }

    /// <summary>
    /// Resolves an identity to its record, failing closed and counting one diagnostic when
    /// it does not name a live record of the matching generation.
    /// </summary>
    /// <param name="id">The identity to resolve.</param>
    /// <param name="state">The record, or <see langword="default"/> on failure.</param>
    /// <returns><see langword="false"/> for any reference that is unset, foreign to this run, outside this category, freed, or of a stale generation.</returns>
    /// <remarks>
    /// doc 20 § Entity identity: "Invalid, expired, or generation-mismatched references fail
    /// closed and produce a diagnostic counter." Exactly one increment per failed
    /// resolution, and the failure never falls through to whatever record now occupies the
    /// slot. This is the single place that increments
    /// <see cref="EntityDiagnostics.StaleReferenceResolutions"/>.
    /// </remarks>
    public bool TryGet(EntityId id, out TState state)
    {
        int dense = ResolveDense(id);
        if (dense < 0)
        {
            state = default;
            return false;
        }

        state = _denseStates[dense];
        return true;
    }

    /// <summary>Replaces the record an identity names, failing closed on a stale reference.</summary>
    /// <param name="id">The identity to resolve.</param>
    /// <param name="state">The replacement record.</param>
    /// <returns><see langword="false"/> when the reference did not resolve.</returns>
    public bool TryUpdate(EntityId id, in TState state)
    {
        int dense = ResolveDense(id);
        if (dense < 0)
        {
            return false;
        }

        _denseStates[dense] = state;
        return true;
    }

    /// <summary>Reads the authored priority key an identity was admitted with.</summary>
    /// <param name="id">The identity to resolve.</param>
    /// <param name="priorityKey">The stored key, or zero on failure.</param>
    /// <returns><see langword="false"/> when the reference did not resolve.</returns>
    public bool TryGetPriorityKey(EntityId id, out long priorityKey)
    {
        int dense = ResolveDense(id);
        if (dense < 0)
        {
            priorityKey = 0L;
            return false;
        }

        priorityKey = _densePriorityKeys[dense];
        return true;
    }

    /// <summary>Removes the record an identity names and releases its slot.</summary>
    /// <param name="id">The identity to remove.</param>
    /// <returns><see langword="false"/> when the reference did not resolve.</returns>
    /// <remarks>
    /// The dense region is compacted by moving the last record into the vacated position, so
    /// there are no holes and no per-record liveness flag in the hot path.
    /// </remarks>
    public bool TryRemove(EntityId id)
    {
        int dense = ResolveDense(id);
        if (dense < 0)
        {
            return false;
        }

        int lastDense = _count - 1;
        if (dense != lastDense)
        {
            EntityId movedId = _denseIds[lastDense];
            _denseIds[dense] = movedId;
            _denseStates[dense] = _denseStates[lastDense];
            _densePriorityKeys[dense] = _densePriorityKeys[lastDense];
            _slotToDense[movedId.Index - _slotOffset] = dense;
        }

        _denseIds[lastDense] = EntityId.Unset;
        _denseStates[lastDense] = default;
        _densePriorityKeys[lastDense] = 0L;
        _slotToDense[id.Index - _slotOffset] = -1;
        _count = lastDense;

        _allocator.TryFree(id);
        return true;
    }

    /// <summary>
    /// Copies every resident identity into <paramref name="destination"/> in the documented
    /// stable order.
    /// </summary>
    /// <param name="destination">
    /// A caller-owned buffer of at least <see cref="Count"/> elements. Caller-owned so the
    /// store never hands out its own array, which doc 115 § Cross-boundary contract registry
    /// forbids: "Cross-boundary payloads never expose mutable collections."
    /// </param>
    /// <returns>How many identities were written.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="destination"/> is too small.</exception>
    /// <remarks>
    /// Order is authored priority key ascending, then the full entity ID
    /// (<see cref="EntityId.Compare"/>). doc 20 § Entity identity: "Stable ordering uses the
    /// full entity ID after a system's authored priority keys." The sort is an in-place
    /// heapsort over a preallocated index array, so ordering allocates nothing; the
    /// comparison is a total order, so the result does not depend on the sort being stable.
    /// </remarks>
    public int CopyOrderedTo(EntityId[] destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < _count)
        {
            throw new ArgumentException(
                "destination holds "
                    + destination.Length.ToString(CultureInfo.InvariantCulture)
                    + " but the store holds "
                    + _count.ToString(CultureInfo.InvariantCulture),
                nameof(destination));
        }

        for (int index = 0; index < _count; index++)
        {
            _order[index] = index;
        }

        HeapSortOrder(_count);

        for (int index = 0; index < _count; index++)
        {
            destination[index] = _denseIds[_order[index]];
        }

        return _count;
    }

    private void Place(EntityId id, long priorityKey, in TState state)
    {
        int dense = _count;
        _denseIds[dense] = id;
        _denseStates[dense] = state;
        _densePriorityKeys[dense] = priorityKey;
        _slotToDense[id.Index - _slotOffset] = dense;
        _count = dense + 1;
    }

    private void Enqueue(long priorityKey, in TState state)
    {
        if (_queuedCount == _queuedPriorityKeys.Length)
        {
            // Growing rather than dropping: doc 20 § Capacity and overload behavior says an
            // authored enemy at the ceiling queues and "later enter[s]", so the queue is
            // never the thing that loses it.
            int grown = _queuedPriorityKeys.Length == 0 ? 4 : _queuedPriorityKeys.Length * 2;
            Array.Resize(ref _queuedPriorityKeys, grown);
            Array.Resize(ref _queuedStates, grown);
            _diagnostics.RecordStoreGrowth();
        }

        _queuedPriorityKeys[_queuedCount] = priorityKey;
        _queuedStates[_queuedCount] = state;
        _queuedCount++;
        _diagnostics.SetQueueDepth(_queuedCount);
    }

    private int ResolveDense(EntityId id)
    {
        if (!id.IsIssued
            || id.RunSession != _allocator.RunSession
            || id.Index < _slotOffset
            || id.Index >= _slotOffset + _capacity.HardCapacity)
        {
            _diagnostics.RecordStaleReference();
            return -1;
        }

        int dense = _slotToDense[id.Index - _slotOffset];
        if (dense < 0 || _denseIds[dense] != id)
        {
            _diagnostics.RecordStaleReference();
            return -1;
        }

        return dense;
    }

    private int CompareDense(int leftDense, int rightDense)
    {
        int byPriority = _densePriorityKeys[leftDense].CompareTo(_densePriorityKeys[rightDense]);
        return byPriority != 0
            ? byPriority
            : EntityId.Compare(_denseIds[leftDense], _denseIds[rightDense]);
    }

    private void HeapSortOrder(int count)
    {
        for (int root = (count / 2) - 1; root >= 0; root--)
        {
            SiftDown(root, count);
        }

        for (int end = count - 1; end > 0; end--)
        {
            (_order[0], _order[end]) = (_order[end], _order[0]);
            SiftDown(0, end);
        }
    }

    private void SiftDown(int root, int end)
    {
        int current = root;
        while (true)
        {
            int left = (2 * current) + 1;
            if (left >= end)
            {
                return;
            }

            int largest = left;
            int right = left + 1;
            if (right < end && CompareDense(_order[right], _order[left]) > 0)
            {
                largest = right;
            }

            if (CompareDense(_order[largest], _order[current]) <= 0)
            {
                return;
            }

            (_order[current], _order[largest]) = (_order[largest], _order[current]);
            current = largest;
        }
    }
}
