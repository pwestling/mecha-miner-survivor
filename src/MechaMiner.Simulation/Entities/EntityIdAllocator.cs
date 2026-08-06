using System;
using System.Globalization;

namespace MechaMiner.Simulation.Entities;

/// <summary>
/// Issues and recycles every entity identity in one run session, partitioning the run's
/// slot space by population category.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Entity identity is the whole contract:
/// "Reusing a slot increments its generation, making stale references invalid", "IDs are
/// unique only within one run session", and "The player has a stable reserved ID".
/// </para>
/// <para>
/// <b>Why the slot space is partitioned by category rather than shared.</b> doc 20 § Scope
/// and invariants requires that "every live entity ID resolves to exactly one live record
/// of the matching generation". If each store had its own index space, an identity issued
/// for a pickup would also name a live boss, so a mis-routed reference would resolve
/// instead of failing closed. Partitioning gives one run-global index space - satisfying
/// the invariant - while letting each store size its own arrays to its own capacity, so a
/// cross-category reference is out of the target store's range and fails closed for free.
/// </para>
/// <para>
/// <b>Retirement rather than wrapping.</b> When a slot's generation reaches
/// <see cref="MaximumGeneration"/> the slot is retired instead of returned to the free
/// list. Wrapping would let a reference held since generation one alias a live entity,
/// which is exactly the failure mode generations exist to prevent.
/// </para>
/// <para>
/// Public because <c>CTR-SIM-003</c>'s snapshot and <c>CTR-SIM-001</c>'s event batch both
/// carry entity identities to consumers outside this assembly
/// (<c>CMP-PRE-001</c>, <c>CMP-UI-001</c>, <c>CMP-AUD-001</c>, <c>CMP-OBS-001</c> in
/// doc 115 § Cross-boundary contract registry), and a test cannot exercise the identity
/// contract through those payloads without being able to issue an identity.
/// </para>
/// </remarks>
public sealed class EntityIdAllocator
{
    private const int CategoryCount = 12;

    private readonly ulong _runSession;
    private readonly uint _maximumGeneration;
    private readonly int[] _slotOffsets;
    private readonly StoreCapacity[] _capacities;
    private readonly EntityDiagnostics[] _diagnostics;
    private readonly uint[][] _generations;
    private readonly bool[][] _live;
    private readonly bool[][] _retired;
    private readonly int[][] _freeSlots;
    private readonly int[] _freeCounts;
    private readonly int[] _nextFreshSlots;
    private readonly EntityId _playerId;

    /// <summary>
    /// Creates the allocator for one run, sized from the validated map manifest.
    /// </summary>
    /// <param name="runSession">
    /// The run session fence. Must not be zero, which is reserved to mean "no run".
    /// </param>
    /// <param name="miningSiteManifestCount">The mining-site count the manifest declares.</param>
    /// <param name="staticWorldObjectManifestCount">The static-world-object count the manifest declares.</param>
    /// <exception cref="ArgumentOutOfRangeException">An argument is outside its documented domain.</exception>
    /// <remarks>
    /// The manifest counts are constructor arguments rather than defaults because doc 51
    /// fixes both populations at generation: a run cannot be started without a validated
    /// manifest (<c>CTR-MAP-002</c>), so a store set that could be built without one would
    /// model something that never happens.
    /// </remarks>
    public EntityIdAllocator(
        ulong runSession,
        int miningSiteManifestCount,
        int staticWorldObjectManifestCount)
        : this(runSession, miningSiteManifestCount, staticWorldObjectManifestCount, uint.MaxValue)
    {
    }

    /// <summary>
    /// Creates the allocator with an explicit generation ceiling.
    /// </summary>
    /// <param name="runSession">The run session fence. Must not be zero.</param>
    /// <param name="miningSiteManifestCount">The mining-site count the manifest declares.</param>
    /// <param name="staticWorldObjectManifestCount">The static-world-object count the manifest declares.</param>
    /// <param name="maximumGeneration">
    /// The largest generation a slot may reach before it is retired. Production uses
    /// <see cref="uint.MaxValue"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">An argument is outside its documented domain.</exception>
    /// <remarks>
    /// The ceiling is carried as data for the same reason <c>StoreCapacity</c> carries its
    /// margin: the retirement path is a real behaviour with a registered gate
    /// (<c>VER-SIM-003-006</c>), and at <see cref="uint.MaxValue"/> it is reachable only
    /// after 4.29 billion recycles of one slot, which no test can execute. A ceiling the
    /// caller states is a testable ceiling; a hard-coded one is an untested branch.
    /// </remarks>
    public EntityIdAllocator(
        ulong runSession,
        int miningSiteManifestCount,
        int staticWorldObjectManifestCount,
        uint maximumGeneration)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run', so it can never fence a run's identities");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(miningSiteManifestCount);
        ArgumentOutOfRangeException.ThrowIfNegative(staticWorldObjectManifestCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumGeneration, EntityId.FirstGeneration);

        _runSession = runSession;
        _maximumGeneration = maximumGeneration;
        _slotOffsets = new int[CategoryCount];
        _capacities = new StoreCapacity[CategoryCount];
        _diagnostics = new EntityDiagnostics[CategoryCount];
        _generations = new uint[CategoryCount][];
        _live = new bool[CategoryCount][];
        _retired = new bool[CategoryCount][];
        _freeSlots = new int[CategoryCount][];
        _freeCounts = new int[CategoryCount];
        _nextFreshSlots = new int[CategoryCount];

        int offset = 0;
        for (int position = 0; position < StoreCapacities.Categories.Count; position++)
        {
            PopulationCategory category = StoreCapacities.Categories[position];
            int ordinal = (int)category;
            StoreCapacity capacity = StoreCapacities.For(
                category,
                miningSiteManifestCount,
                staticWorldObjectManifestCount);

            _slotOffsets[ordinal] = offset;
            _capacities[ordinal] = capacity;
            _diagnostics[ordinal] = new EntityDiagnostics(category, capacity);
            _generations[ordinal] = new uint[capacity.HardCapacity];
            _live[ordinal] = new bool[capacity.HardCapacity];
            _retired[ordinal] = new bool[capacity.HardCapacity];
            _freeSlots[ordinal] = new int[capacity.HardCapacity];
            offset += capacity.HardCapacity;
        }

        // doc 20 § Scope and invariants: "exactly one player entity exists until terminal
        // resolution". The player is allocated here and never freed, so its slot is
        // occupied for the whole run and ordinary allocation can never reach it.
        if (!TryAllocate(PopulationCategory.Player, out EntityId playerId))
        {
            throw new InvalidOperationException(
                "the reserved player slot could not be allocated, so the run's one "
                    + "invariant entity does not exist");
        }

        _playerId = playerId;
        TotalSlotCapacity = offset;
    }

    /// <summary>The run session every identity this allocator issues is fenced to.</summary>
    /// <remarks>doc 20 § Entity identity: "IDs are unique only within one run session."</remarks>
    public ulong RunSession => _runSession;

    /// <summary>The generation at which a slot is retired instead of recycled.</summary>
    public uint MaximumGeneration => _maximumGeneration;

    /// <summary>The sum of every category's hard capacity: the run's whole slot space.</summary>
    public int TotalSlotCapacity { get; }

    /// <summary>The run's reserved player identity, allocated at construction and never freed.</summary>
    /// <remarks>
    /// doc 20 § Entity identity: "The player has a stable reserved ID." Its
    /// <see cref="EntityId.Index"/> is <see cref="EntityId.ReservedPlayerIndex"/> and its
    /// <see cref="EntityId.Generation"/> is <see cref="EntityId.FirstGeneration"/> in every
    /// run; only <see cref="EntityId.RunSession"/> differs, which it must, or the fence
    /// would not fence.
    /// </remarks>
    public EntityId PlayerId => _playerId;

    /// <summary>The declared capacity of one category.</summary>
    /// <param name="category">One of the twelve authoritative categories.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not one of the twelve.</exception>
    public StoreCapacity CapacityFor(PopulationCategory category)
    {
        return _capacities[RequireCategory(category)];
    }

    /// <summary>The diagnostic counters of one category.</summary>
    /// <param name="category">One of the twelve authoritative categories.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not one of the twelve.</exception>
    /// <remarks>
    /// The store for that category shares this instance, so allocation counters and store
    /// counters reconcile against one another rather than against two separate ledgers.
    /// </remarks>
    public EntityDiagnostics DiagnosticsFor(PopulationCategory category)
    {
        return _diagnostics[RequireCategory(category)];
    }

    /// <summary>The first run-global slot index belonging to one category.</summary>
    /// <param name="category">One of the twelve authoritative categories.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not one of the twelve.</exception>
    public int SlotOffsetFor(PopulationCategory category)
    {
        return _slotOffsets[RequireCategory(category)];
    }

    /// <summary>
    /// Determines which category an identity's slot belongs to, without resolving whether
    /// it is live.
    /// </summary>
    /// <param name="id">The identity to classify.</param>
    /// <param name="category">The owning category when the index is in range.</param>
    /// <returns><see langword="true"/> when the index falls inside a category's partition.</returns>
    public bool TryGetCategory(EntityId id, out PopulationCategory category)
    {
        if (!id.IsIssued || id.RunSession != _runSession)
        {
            category = default;
            return false;
        }

        for (int position = 0; position < StoreCapacities.Categories.Count; position++)
        {
            PopulationCategory candidate = StoreCapacities.Categories[position];
            int ordinal = (int)candidate;
            int start = _slotOffsets[ordinal];
            if (id.Index >= start && id.Index < start + _capacities[ordinal].HardCapacity)
            {
                category = candidate;
                return true;
            }
        }

        category = default;
        return false;
    }

    /// <summary>
    /// Issues the next identity for one category, reusing a freed slot when one is
    /// available.
    /// </summary>
    /// <param name="category">One of the twelve authoritative categories.</param>
    /// <param name="id">
    /// The issued identity, or <see cref="EntityId.Unset"/> when the partition is full so
    /// that a caller ignoring the result still holds something that fails closed.
    /// </param>
    /// <returns><see langword="false"/> when the partition is at hard capacity.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not one of the twelve.</exception>
    /// <remarks>
    /// Reuse increments the slot's generation before issuing, so the previously issued
    /// identity for that slot can never equal the new one (doc 20 § Entity identity).
    /// </remarks>
    public bool TryAllocate(PopulationCategory category, out EntityId id)
    {
        int ordinal = RequireCategory(category);
        EntityDiagnostics diagnostics = _diagnostics[ordinal];

        if (_freeCounts[ordinal] > 0)
        {
            int recycledSlot = _freeSlots[ordinal][--_freeCounts[ordinal]];
            _generations[ordinal][recycledSlot]++;
            _live[ordinal][recycledSlot] = true;
            diagnostics.RecordAllocated(wasReuse: true);
            id = EntityId.Create(
                _runSession,
                _slotOffsets[ordinal] + recycledSlot,
                _generations[ordinal][recycledSlot]);
            return true;
        }

        if (_nextFreshSlots[ordinal] < _capacities[ordinal].HardCapacity)
        {
            int freshSlot = _nextFreshSlots[ordinal]++;
            _generations[ordinal][freshSlot] = EntityId.FirstGeneration;
            _live[ordinal][freshSlot] = true;
            diagnostics.RecordAllocated(wasReuse: false);
            id = EntityId.Create(
                _runSession,
                _slotOffsets[ordinal] + freshSlot,
                EntityId.FirstGeneration);
            return true;
        }

        diagnostics.RecordRejectedRequest();
        id = EntityId.Unset;
        return false;
    }

    /// <summary>
    /// Releases an identity's slot, retiring it instead of recycling when its generation is
    /// exhausted.
    /// </summary>
    /// <param name="id">The identity to release.</param>
    /// <returns>
    /// <see langword="false"/> when <paramref name="id"/> does not name a live record of the
    /// matching generation, or names the reserved player.
    /// </returns>
    /// <remarks>
    /// Refusing to free the reserved player is what makes doc 20 § Scope and invariants'
    /// "exactly one player entity exists until terminal resolution" structural: the slot
    /// cannot re-enter the free list, so ordinary allocation can never hand it out.
    /// </remarks>
    public bool TryFree(EntityId id)
    {
        if (id.IsReservedPlayer)
        {
            return false;
        }

        if (!TryGetCategory(id, out PopulationCategory category))
        {
            return false;
        }

        int ordinal = (int)category;
        int slot = id.Index - _slotOffsets[ordinal];
        if (!_live[ordinal][slot] || _generations[ordinal][slot] != id.Generation)
        {
            return false;
        }

        _live[ordinal][slot] = false;
        _diagnostics[ordinal].RecordFreed();

        if (_generations[ordinal][slot] >= _maximumGeneration)
        {
            _retired[ordinal][slot] = true;
            _diagnostics[ordinal].RecordRetiredSlot();
            return true;
        }

        _freeSlots[ordinal][_freeCounts[ordinal]++] = slot;
        return true;
    }

    /// <summary>
    /// Whether an identity names a live record of the matching generation. Pure: it counts
    /// nothing.
    /// </summary>
    /// <param name="id">The identity to test.</param>
    /// <remarks>
    /// Deliberately free of side effects. doc 20 § Entity identity requires exactly one
    /// diagnostic per failed <em>resolution</em>, and a predicate that a caller may invoke
    /// speculatively would inflate that count; the store owns the counter.
    /// </remarks>
    public bool IsLive(EntityId id)
    {
        if (!TryGetCategory(id, out PopulationCategory category))
        {
            return false;
        }

        int ordinal = (int)category;
        int slot = id.Index - _slotOffsets[ordinal];
        return _live[ordinal][slot] && _generations[ordinal][slot] == id.Generation;
    }

    /// <summary>Whether a slot has been permanently retired for generation exhaustion.</summary>
    /// <param name="id">An identity naming the slot to test.</param>
    public bool IsRetired(EntityId id)
    {
        if (!TryGetCategory(id, out PopulationCategory category))
        {
            return false;
        }

        int ordinal = (int)category;
        return _retired[ordinal][id.Index - _slotOffsets[ordinal]];
    }

    /// <summary>How many records of one category are live now.</summary>
    /// <param name="category">One of the twelve authoritative categories.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not one of the twelve.</exception>
    public int LiveCount(PopulationCategory category)
    {
        return _diagnostics[RequireCategory(category)].LiveCount;
    }

    /// <summary>Whether one category's partition can issue another identity right now.</summary>
    /// <param name="category">One of the twelve authoritative categories.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not one of the twelve.</exception>
    public bool HasCapacity(PopulationCategory category)
    {
        int ordinal = RequireCategory(category);
        return _freeCounts[ordinal] > 0 || _nextFreshSlots[ordinal] < _capacities[ordinal].HardCapacity;
    }

    private static int RequireCategory(PopulationCategory category)
    {
        int ordinal = (int)category;
        if (ordinal < 0 || ordinal >= CategoryCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "not one of the twelve authoritative population categories of doc 20 "
                    + "§ Authoritative population categories (ordinal "
                    + ordinal.ToString(CultureInfo.InvariantCulture)
                    + ")");
        }

        return ordinal;
    }
}
