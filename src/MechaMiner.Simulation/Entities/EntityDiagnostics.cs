using System.Globalization;

namespace MechaMiner.Simulation.Entities;

/// <summary>
/// The named diagnostic counters for one population category's identity space and store.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Capacity and overload behavior:
/// "Capacity, high-water mark, queue depth, reuse count, and rejected visual requests are
/// diagnostic metrics." § Entity identity adds the sixth: "Invalid, expired, or
/// generation-mismatched references fail closed and produce a diagnostic counter."
/// </para>
/// <para>
/// Reading never resets. A counter that a read clears cannot be reconciled against the
/// operations that produced it, and
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Frame metrics
/// expects these to be sampled repeatedly during a run.
/// </para>
/// <para>
/// <b>Writers, so that doc 115 § Mutable-state ownership matrix's "each mutable datum has
/// exactly one row owner" holds field by field.</b> <c>EntityIdAllocator</c> is the sole
/// writer of <see cref="LiveCount"/>, <see cref="HighWaterMark"/>,
/// <see cref="ReuseCount"/>, <see cref="RejectedRequests"/>, and
/// <see cref="RetiredSlotCount"/>, because it is the only thing that issues and recycles a
/// slot. <c>PackedEntityStore{TState}</c> is the sole writer of
/// <see cref="QueueDepth"/>, <see cref="StaleReferenceResolutions"/>, and
/// <see cref="StoreGrowthCount"/>, because queueing, resolution, and storage are store
/// operations. No field has two writers, and every mutator is
/// internal so nothing outside this assembly can forge a counter.
/// </para>
/// <para>
/// Public read surface because doc 115 § Component registry names <c>CMP-OBS-001</c> - the
/// diagnostics service, outside this assembly - as the consumer of stable counters
/// (<c>CTR-OBS-001</c>).
/// </para>
/// </remarks>
public sealed class EntityDiagnostics
{
    private readonly PopulationCategory _category;
    private readonly StoreCapacity _capacity;
    private int _liveCount;
    private int _highWaterMark;
    private int _queueDepth;
    private long _reuseCount;
    private long _rejectedRequests;
    private long _staleReferenceResolutions;
    private int _retiredSlotCount;
    private int _storeGrowthCount;

    internal EntityDiagnostics(PopulationCategory category, StoreCapacity capacity)
    {
        _category = category;
        _capacity = capacity;
    }

    /// <summary>The category these counters describe.</summary>
    public PopulationCategory Category => _category;

    /// <summary>The declared capacity, so a counter is readable against its bound.</summary>
    public StoreCapacity Capacity => _capacity;

    /// <summary>How many records of this category are live now.</summary>
    public int LiveCount => _liveCount;

    /// <summary>The largest <see cref="LiveCount"/> reached so far in this run.</summary>
    /// <remarks>doc 20 § Capacity and overload behavior lists the high-water mark as a diagnostic metric.</remarks>
    public int HighWaterMark => _highWaterMark;

    /// <summary>How many authored requests are queued waiting for capacity.</summary>
    /// <remarks>
    /// doc 20 § Capacity and overload behavior: "Authored enemies that reach a gameplay
    /// ceiling queue and later enter; they are not silently canceled or converted." A
    /// nonzero depth is the evidence that nothing was cancelled.
    /// </remarks>
    public int QueueDepth => _queueDepth;

    /// <summary>How many times a freed slot has been issued again.</summary>
    /// <remarks>Each reuse incremented that slot's generation (doc 20 § Entity identity).</remarks>
    public long ReuseCount => _reuseCount;

    /// <summary>How many allocation requests were refused because the partition was full.</summary>
    public long RejectedRequests => _rejectedRequests;

    /// <summary>
    /// How many references failed to resolve because they were unset, foreign to this run,
    /// outside this category, or of a mismatched generation.
    /// </summary>
    /// <remarks>
    /// doc 20 § Entity identity: "Invalid, expired, or generation-mismatched references
    /// fail closed and produce a diagnostic counter." Exactly one increment per failed
    /// resolution, so the counter is a count of failures and not of checks.
    /// </remarks>
    public long StaleReferenceResolutions => _staleReferenceResolutions;

    /// <summary>
    /// How many slots were permanently retired because their generation counter was
    /// exhausted.
    /// </summary>
    /// <remarks>
    /// Retiring rather than wrapping is what stops a long-held reference from aliasing a
    /// live entity, which is the failure mode generations exist to prevent (doc 20 §
    /// Entity identity).
    /// </remarks>
    public int RetiredSlotCount => _retiredSlotCount;

    /// <summary>
    /// How many times the store enlarged a backing array rather than reusing the one it
    /// preallocated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Zero for the dense record region, always and by construction: those arrays are
    /// <see langword="readonly"/> fields sized to the hard capacity at construction, so
    /// nothing can replace them. Only the authored-spawn queue can grow, because doc 20 §
    /// Capacity and overload behavior says a queued authored enemy "later enter[s]" and the
    /// queue must therefore never be the thing that loses it.
    /// </para>
    /// <para>
    /// Observable because it is the decidable form of "the churn cycle allocates nothing":
    /// a store that never enlarges an array and never replaces one cannot allocate per
    /// churn, and that is a fact about the store rather than a measurement of the process.
    /// </para>
    /// </remarks>
    public int StoreGrowthCount => _storeGrowthCount;

    /// <summary>
    /// Renders every counter as canonical invariant text, for evidence bundles and
    /// failure messages.
    /// </summary>
    /// <remarks>
    /// <see cref="CultureInfo.InvariantCulture"/> throughout so the same run produces the
    /// same text on every platform (doc 91 § Determinism and fixture policy).
    /// </remarks>
    public string Render()
    {
        return string.Join(
            " ",
            "category=" + _category.ToString(),
            "live=" + _liveCount.ToString(CultureInfo.InvariantCulture),
            "soft=" + _capacity.SoftTarget.ToString(CultureInfo.InvariantCulture),
            "hard=" + _capacity.HardCapacity.ToString(CultureInfo.InvariantCulture),
            "high-water=" + _highWaterMark.ToString(CultureInfo.InvariantCulture),
            "queue-depth=" + _queueDepth.ToString(CultureInfo.InvariantCulture),
            "reuse=" + _reuseCount.ToString(CultureInfo.InvariantCulture),
            "rejected=" + _rejectedRequests.ToString(CultureInfo.InvariantCulture),
            "stale=" + _staleReferenceResolutions.ToString(CultureInfo.InvariantCulture),
            "retired=" + _retiredSlotCount.ToString(CultureInfo.InvariantCulture),
            "store-growth=" + _storeGrowthCount.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Records one issued identity. Written only by <c>EntityIdAllocator</c>.</summary>
    internal void RecordAllocated(bool wasReuse)
    {
        _liveCount++;
        if (_liveCount > _highWaterMark)
        {
            _highWaterMark = _liveCount;
        }

        if (wasReuse)
        {
            _reuseCount++;
        }
    }

    /// <summary>Records one freed identity. Written only by <c>EntityIdAllocator</c>.</summary>
    internal void RecordFreed()
    {
        _liveCount--;
    }

    /// <summary>Records one refused allocation. Written only by <c>EntityIdAllocator</c>.</summary>
    internal void RecordRejectedRequest()
    {
        _rejectedRequests++;
    }

    /// <summary>Records one retired slot. Written only by <c>EntityIdAllocator</c>.</summary>
    internal void RecordRetiredSlot()
    {
        _retiredSlotCount++;
    }

    /// <summary>Records one failed resolution. Written only by <c>PackedEntityStore{TState}</c>.</summary>
    internal void RecordStaleReference()
    {
        _staleReferenceResolutions++;
    }

    /// <summary>Sets the current queue depth. Written only by <c>PackedEntityStore{TState}</c>.</summary>
    internal void SetQueueDepth(int depth)
    {
        _queueDepth = depth;
    }

    /// <summary>Records one backing-array enlargement. Written only by <c>PackedEntityStore{TState}</c>.</summary>
    internal void RecordStoreGrowth()
    {
        _storeGrowthCount++;
    }
}
