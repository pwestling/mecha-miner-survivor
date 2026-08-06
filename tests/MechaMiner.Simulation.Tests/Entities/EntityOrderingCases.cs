using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Entities;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// The ordering fixtures of <c>entities-store-ordering.txt</c> that a live store cannot produce,
/// plus the deliberately degraded comparators that prove each case is not vacuous.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports <c>VER-SIM-003-010</c> and <c>VER-SIM-003-012</c>.
/// </para>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Entity identity: "Stable ordering uses the full
/// entity ID after a system's authored priority keys", over an ID that "contains a reusable storage
/// index and a generation". Storage index precedes generation because it is the only component that
/// discriminates among simultaneously-live entities; generation exists to order records that share a
/// recycled slot.
/// </para>
/// <para>
/// <b>Why these cases are not built on a store.</b> That key order has a consequence: two
/// simultaneously-live entities have distinct storage indices, and storage index sorts first, so a
/// live store can never reach the generation key at all. A fixture drawn from a store therefore
/// leaves generation dead however many records it holds, and an implementation that compared
/// generation before storage index would emit byte-identical output. The only shape that reaches
/// generation is two records sharing one storage index with different generations, which means a
/// record set that retains a slot's earlier occupant alongside the occupant that recycled it:
/// recovery snapshots, persisted history, and retained diagnostic or statistics records. Not replay -
/// <c>docs/technical/decisions/TDR-002-use-seeded-reproducibility-without-lockstep-replay.md</c>
/// accepts seeded reproducibility without lockstep replay, so no replay log is implied here.
/// </para>
/// <para>
/// <b>Run session leads the comparison, and one case has to let two sessions meet.</b> doc 20 § Entity
/// identity states the order outright: "The full entity ID compares by run session, then storage index,
/// then generation", and "Run session leads because it is the outermost component of the identity, so the
/// comparison order follows the same nesting the identity has." A case that holds the session constant
/// cannot reach that leading key, and cases 1 to 3 all hold it constant: deleting the key from
/// <see cref="EntityId.Compare"/> left every one of them byte-identical and the whole suite green. Case 4 -
/// <see cref="RetainedCrossSessionRecords"/> - is the case that reaches it.
/// </para>
/// <para>
/// <b>What that case is not.</b> It is not a check that a foreign session is refused, and the comparator
/// must not become one. doc 20 § Entity identity places that refusal at the point an identity is resolved or
/// freed - "A store resolving an identity that carries a foreign or unset run session fails closed" - and
/// rules the comparator out explicitly: "The comparator is therefore not the place to detect it. A redundant
/// session check there would only make the boundary check look unnecessary." So case 4 pins a documented
/// total order over records from two sessions, and nothing more.
/// </para>
/// <para>
/// <b>Every identity here is issued by a real allocator, through allocate and free.</b> "Not built on a
/// store" above means the record <em>set</em> is not a snapshot of one live store, because such a
/// snapshot cannot reach the generation key; it does not mean the identities are hand-built. They are
/// obtained the way the retained records they model were obtained: allocate a slot, retain the identity,
/// free the slot, allocate it again. That is what makes these fixtures evidence about identities the
/// system can actually produce rather than about values that merely have the right shape, and it is why
/// <c>EntityId.Create</c> is internal to the simulation assembly.
/// </para>
/// </remarks>
internal static class EntityOrderingCases
{
    /// <summary>The <c>pickup</c> partition label the golden renders storage indices against.</summary>
    internal const string PickupPartitionLabel = "pickup";

    /// <summary>
    /// One ordered record: an authored priority key and the identity that follows it in the
    /// comparison.
    /// </summary>
    /// <param name="PriorityKey">The system's authored priority key.</param>
    /// <param name="Id">The full entity identity.</param>
    internal readonly record struct OrderedRecord(long PriorityKey, EntityId Id);

    /// <summary>
    /// A comparator with exactly one documented component removed or displaced.
    /// </summary>
    /// <remarks>
    /// Each member is a specific way the ordering rule could be got wrong. A case that cannot tell a
    /// degraded comparator from the real one is a case that does not test the missing component, and
    /// <c>EntityStoreNegativeControlTests</c> asserts which cases detect which degradation instead of
    /// leaving that in a comment.
    /// </remarks>
    internal enum Degradation
    {
        /// <summary>Priority key, then generation. Storage index dropped.</summary>
        WithoutStorageIndex = 0,

        /// <summary>Priority key, then storage index. Generation dropped.</summary>
        WithoutGeneration = 1,

        /// <summary>Storage index, then generation. The authored priority key dropped.</summary>
        WithoutPriorityKey = 2,

        /// <summary>
        /// Priority key, then generation, then storage index: the two identity components swapped.
        /// </summary>
        /// <remarks>
        /// The specific error doc 20's key order rules out. A live-store fixture cannot detect it,
        /// which is the reason the retained-record cases exist.
        /// </remarks>
        GenerationBeforeStorageIndex = 3,

        /// <summary>Priority key, then storage index, then generation. The run session dropped.</summary>
        /// <remarks>
        /// The degradation the golden had no control for at all: deleting the leading run-session key from
        /// <see cref="EntityId.Compare"/> left every session-constant case byte-identical and the whole suite
        /// green, which is what <see cref="RetainedCrossSessionRecords"/> exists to catch.
        /// </remarks>
        WithoutRunSession = 4,
    }

    /// <summary>Every degradation, in declaration order, for the control matrix.</summary>
    internal static IReadOnlyList<Degradation> AllDegradations { get; } =
    [
        Degradation.WithoutStorageIndex,
        Degradation.WithoutGeneration,
        Degradation.WithoutPriorityKey,
        Degradation.GenerationBeforeStorageIndex,
        Degradation.WithoutRunSession,
    ];

    /// <summary>
    /// Case 2 - <c>retained-recycled-slot</c>. Generation is the sole discriminator.
    /// </summary>
    /// <param name="allocator">
    /// An allocator dedicated to this case, whose Pickup partition has issued nothing yet.
    /// </param>
    /// <remarks>
    /// Slot 4 carries three generations at one priority key, so generation is the only component that
    /// can order them. Slot 2 carries generation 7, higher than any of the three, and still sorts
    /// first: that is the direct proof that storage index precedes generation rather than the reverse.
    /// Generation 7 is reached by recycling slot 2 six times, which is also the only honest way to hold
    /// an identity at that generation.
    /// </remarks>
    internal static List<OrderedRecord> RetainedRecycledSlot(EntityIdAllocator allocator)
    {
        List<EntityId> fresh = AllocateFreshPickups(allocator, count: 5);
        EntityId slotFourFirst = fresh[4];
        EntityId slotFourSecond = RecycleOnce(allocator, slotFourFirst);
        EntityId slotFourThird = RecycleOnce(allocator, slotFourSecond);
        EntityId slotTwo = AdvanceToGeneration(allocator, fresh[2], targetGeneration: 7);

        return
        [
            new OrderedRecord(10L, slotFourFirst),
            new OrderedRecord(10L, slotFourThird),
            new OrderedRecord(10L, slotFourSecond),
            new OrderedRecord(10L, slotTwo),
        ];
    }

    /// <summary>
    /// Case 3 - <c>retained-tied-priority-keys</c>. Storage index is the sole discriminator, and the
    /// priority key is shown to lead.
    /// </summary>
    /// <param name="allocator">
    /// An allocator dedicated to this case, whose Pickup partition has issued nothing yet.
    /// </param>
    /// <remarks>
    /// All three share generation 4, so generation can decide nothing. Two share priority key 5 and
    /// differ only in storage index. The third has priority key 42 and storage index 3, which falls
    /// between the other two, so it sorts last despite its middle index - which is what makes the
    /// priority key's precedence over the identity observable rather than assumed.
    /// </remarks>
    internal static List<OrderedRecord> RetainedTiedPriorityKeys(EntityIdAllocator allocator)
    {
        List<EntityId> fresh = AllocateFreshPickups(allocator, count: 10);

        return
        [
            new OrderedRecord(5L, AdvanceToGeneration(allocator, fresh[9], targetGeneration: 4)),
            new OrderedRecord(5L, AdvanceToGeneration(allocator, fresh[2], targetGeneration: 4)),
            new OrderedRecord(42L, AdvanceToGeneration(allocator, fresh[3], targetGeneration: 4)),
        ];
    }

    /// <summary>
    /// Case 4 - <c>retained-cross-session-records</c>. The run session is the sole discriminator.
    /// </summary>
    /// <param name="allocator">
    /// An allocator for the first run session, whose Pickup partition has issued nothing yet.
    /// </param>
    /// <param name="otherSessionAllocator">
    /// An allocator for a second, higher run session, whose Pickup partition has issued nothing yet.
    /// </param>
    /// <remarks>
    /// <para>
    /// Two records carry the same priority key, the same storage index, and the same generation, and differ
    /// only in which run session issued them - so for that pair the run session is the only component that
    /// can decide anything. A third record comes from the higher session at a <em>lower</em> storage index and
    /// still sorts last: that is the direct proof that the run session precedes the storage index rather than
    /// the other way round, which is the same shape
    /// <see cref="RetainedRecycledSlot"/> uses for generation.
    /// </para>
    /// <para>
    /// A retained record set for the same reason case 2 is, and a stronger one: no live collection holds
    /// identities from two runs at once. doc 20 § Entity identity names the three ordered collections that
    /// sort on the full entity ID and gives each an enforcement point that makes a cross-session record
    /// impossible in it - a packed store by construction, a tick's event batch at the boundary that assembles
    /// it, and presentation staging at the point an entity is staged. What legitimately outlives one run is
    /// the diagnostic and statistics record set, and that is what this case models.
    /// </para>
    /// <para>
    /// Both identities are minted by real allocators, as every other case's are. Two allocators over the same
    /// manifest counts issue the same partition slot at the same first generation, which is exactly doc 20's
    /// "Two runs legitimately allocate the same storage index at the same generation" - so the pair this case
    /// needs is not contrived, it is the situation the run session exists to disambiguate.
    /// </para>
    /// </remarks>
    internal static List<OrderedRecord> RetainedCrossSessionRecords(
        EntityIdAllocator allocator,
        EntityIdAllocator otherSessionAllocator)
    {
        ArgumentNullException.ThrowIfNull(allocator);
        ArgumentNullException.ThrowIfNull(otherSessionAllocator);
        Assert.That(
            otherSessionAllocator.RunSession,
            Is.GreaterThan(allocator.RunSession),
            "the second session must sort after the first, or the case cannot show that a lower storage "
                + "index in the later session still sorts last");

        List<EntityId> firstSession = AllocateFreshPickups(allocator, count: 4);
        List<EntityId> secondSession = AllocateFreshPickups(otherSessionAllocator, count: 4);

        return
        [
            new OrderedRecord(10L, secondSession[3]),
            new OrderedRecord(10L, firstSession[3]),
            new OrderedRecord(10L, secondSession[0]),
        ];
    }

    /// <summary>
    /// Sorts by the documented rule: authored priority key ascending, then the full entity ID through
    /// production <see cref="EntityId.Compare"/>.
    /// </summary>
    /// <param name="records">The record set, in whatever order it arrived.</param>
    /// <remarks>
    /// The identity half goes through the production comparison rather than a re-expression of it, so
    /// the golden is evidence about <see cref="EntityId.Compare"/> and not about a paraphrase that
    /// happens to agree with it.
    /// </remarks>
    internal static List<OrderedRecord> DocumentedSort(IReadOnlyList<OrderedRecord> records)
    {
        return StableSort(records, CompareDocumented);
    }

    /// <summary>Sorts by a comparator missing or misordering one documented component.</summary>
    /// <param name="records">The record set, in whatever order it arrived.</param>
    /// <param name="degradation">Which component to remove or displace.</param>
    internal static List<OrderedRecord> DegradedSort(
        IReadOnlyList<OrderedRecord> records,
        Degradation degradation)
    {
        return StableSort(records, (left, right) => CompareDegraded(left, right, degradation));
    }

    /// <summary>Renders a case's block of the golden, headed by its name and sole discriminator.</summary>
    /// <param name="caseName">The case's stable name.</param>
    /// <param name="soleDiscriminator">The component this case leaves as the only live one.</param>
    /// <param name="ordered">The records in the order under test.</param>
    /// <param name="partitionOffset">The Pickup partition's first slot index.</param>
    internal static string RenderCase(
        string caseName,
        string soleDiscriminator,
        IReadOnlyList<OrderedRecord> ordered,
        int partitionOffset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(soleDiscriminator);
        ArgumentNullException.ThrowIfNull(ordered);

        Dictionary<EntityId, long> keys = new(ordered.Count);
        List<EntityId> identities = new(ordered.Count);
        foreach (OrderedRecord record in ordered)
        {
            keys[record.Id] = record.PriorityKey;
            identities.Add(record.Id);
        }

        return "## case "
            + caseName
            + ": sole discriminator = "
            + soleDiscriminator
            + "\n"
            + StoreContractAssertions.RenderOrder(
                identities,
                id => keys[id],
                PickupPartitionLabel,
                partitionOffset);
    }

    /// <summary>
    /// The Pickup partition's first slot index, recomputed from the capacity table rather than read
    /// off the allocator.
    /// </summary>
    /// <param name="allocator">The run's allocator, for the per-category capacities.</param>
    /// <remarks>
    /// doc 20 § Authoritative population categories tiles the run's slot space in table order, so the
    /// offset is the running sum of the hard capacities above Pickup. Recomputing it here and
    /// comparing against <c>SlotOffsetFor</c> is what makes the golden's partition base derived rather
    /// than a literal: three of the rows in that sum are doc 22 § Performance and capacity ceilings
    /// and will move.
    /// </remarks>
    internal static int ComputePickupOffsetFromCapacityTable(EntityIdAllocator allocator)
    {
        ArgumentNullException.ThrowIfNull(allocator);

        int offset = 0;
        foreach (PopulationCategory category in StoreCapacities.Categories)
        {
            if (category == PopulationCategory.Pickup)
            {
                return offset;
            }

            offset += allocator.CapacityFor(category).HardCapacity;
        }

        throw new InvalidOperationException(
            "PopulationCategory.Pickup is absent from the canonical category order");
    }

    /// <summary>
    /// Allocates <paramref name="count"/> fresh Pickup slots and returns them in issue order, which is
    /// partition-slot order.
    /// </summary>
    /// <param name="allocator">The case's dedicated allocator.</param>
    /// <param name="count">How many fresh slots to take.</param>
    /// <remarks>
    /// The returned list is indexed by partition-relative slot, which is what the case builders name
    /// their records by. That correspondence is asserted rather than assumed, because it is the one
    /// thing that would silently move a record to a different slot if fresh allocation ever stopped
    /// being sequential.
    /// </remarks>
    private static List<EntityId> AllocateFreshPickups(EntityIdAllocator allocator, int count)
    {
        ArgumentNullException.ThrowIfNull(allocator);

        int partitionOffset = allocator.SlotOffsetFor(PopulationCategory.Pickup);
        List<EntityId> issued = new(count);
        for (int slot = 0; slot < count; slot++)
        {
            Assert.That(
                allocator.TryAllocate(PopulationCategory.Pickup, out EntityId id),
                Is.True,
                "the Pickup partition must have capacity for slot "
                    + slot.ToString(CultureInfo.InvariantCulture));
            Assert.That(
                id.Index,
                Is.EqualTo(partitionOffset + slot),
                "fresh Pickup allocation must be sequential from the partition base, or the case "
                    + "builders name slots that are not the ones they got");
            Assert.That(
                id.Generation,
                Is.EqualTo(EntityId.FirstGeneration),
                "a fresh slot must carry the first generation");
            issued.Add(id);
        }

        return issued;
    }

    /// <summary>Frees a slot and takes it again, returning the next generation's identity.</summary>
    /// <param name="allocator">The case's dedicated allocator.</param>
    /// <param name="held">The identity currently occupying the slot.</param>
    /// <remarks>
    /// The free list is taken last-in-first-out, so freeing exactly one slot and immediately allocating
    /// returns that same slot. Both facts are asserted, because the whole point of building these
    /// fixtures through the allocator is that the identities are ones the allocator really issued.
    /// </remarks>
    private static EntityId RecycleOnce(EntityIdAllocator allocator, EntityId held)
    {
        ArgumentNullException.ThrowIfNull(allocator);

        Assert.That(allocator.TryFree(held), Is.True, "the held identity must name a live slot");
        Assert.That(
            allocator.TryAllocate(PopulationCategory.Pickup, out EntityId recycled),
            Is.True,
            "the just-freed slot must be reusable");
        Assert.That(
            recycled.Index,
            Is.EqualTo(held.Index),
            "the freed slot is the only one on the free list, so it must be the one handed back");
        Assert.That(
            recycled.Generation,
            Is.EqualTo(held.Generation + 1),
            "doc 20 § Entity identity: reusing a slot increments its generation");
        return recycled;
    }

    /// <summary>Recycles one slot until it reaches a target generation.</summary>
    /// <param name="allocator">The case's dedicated allocator.</param>
    /// <param name="held">The identity currently occupying the slot.</param>
    /// <param name="targetGeneration">The generation to reach. Must be at least the current one.</param>
    private static EntityId AdvanceToGeneration(
        EntityIdAllocator allocator,
        EntityId held,
        uint targetGeneration)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(targetGeneration, held.Generation);

        EntityId current = held;
        while (current.Generation < targetGeneration)
        {
            current = RecycleOnce(allocator, current);
        }

        return current;
    }

    private static int CompareDocumented(OrderedRecord left, OrderedRecord right)
    {
        int byPriority = left.PriorityKey.CompareTo(right.PriorityKey);
        return byPriority != 0 ? byPriority : EntityId.Compare(left.Id, right.Id);
    }

    private static int CompareDegraded(OrderedRecord left, OrderedRecord right, Degradation degradation)
    {
        switch (degradation)
        {
            // Every degradation below drops or displaces exactly one component and keeps the rest in
            // documented order, run session included. Dropping the session as a side effect of dropping
            // something else would make the matrix report one loss as several.
            case Degradation.WithoutStorageIndex:
                {
                    int byPriority = left.PriorityKey.CompareTo(right.PriorityKey);
                    if (byPriority != 0)
                    {
                        return byPriority;
                    }

                    int bySession = left.Id.RunSession.CompareTo(right.Id.RunSession);
                    return bySession != 0
                        ? bySession
                        : left.Id.Generation.CompareTo(right.Id.Generation);
                }

            case Degradation.WithoutGeneration:
                {
                    int byPriority = left.PriorityKey.CompareTo(right.PriorityKey);
                    if (byPriority != 0)
                    {
                        return byPriority;
                    }

                    int bySession = left.Id.RunSession.CompareTo(right.Id.RunSession);
                    return bySession != 0 ? bySession : left.Id.Index.CompareTo(right.Id.Index);
                }

            case Degradation.WithoutPriorityKey:
                return EntityId.Compare(left.Id, right.Id);

            case Degradation.GenerationBeforeStorageIndex:
                {
                    int byPriority = left.PriorityKey.CompareTo(right.PriorityKey);
                    if (byPriority != 0)
                    {
                        return byPriority;
                    }

                    int bySession = left.Id.RunSession.CompareTo(right.Id.RunSession);
                    if (bySession != 0)
                    {
                        return bySession;
                    }

                    int byGeneration = left.Id.Generation.CompareTo(right.Id.Generation);
                    return byGeneration != 0 ? byGeneration : left.Id.Index.CompareTo(right.Id.Index);
                }

            case Degradation.WithoutRunSession:
                {
                    int byPriority = left.PriorityKey.CompareTo(right.PriorityKey);
                    if (byPriority != 0)
                    {
                        return byPriority;
                    }

                    int byIndex = left.Id.Index.CompareTo(right.Id.Index);
                    return byIndex != 0 ? byIndex : left.Id.Generation.CompareTo(right.Id.Generation);
                }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(degradation),
                    degradation,
                    "not one of the declared degradations");
        }
    }

    /// <summary>
    /// An explicit stable insertion sort, so a comparator that ties leaves the input order visible.
    /// </summary>
    /// <remarks>
    /// Stability is what makes the negative control decidable rather than a coin toss. Under a
    /// comparator missing a live component two records compare equal, and a stable sort then returns
    /// them in arrival order - so feeding two permutations of the same set produces two different
    /// renderings, and that difference is the evidence that the dropped component was doing work. A
    /// library sort whose tie behaviour is unspecified would make the same control flaky rather than
    /// conclusive.
    /// </remarks>
    private static List<OrderedRecord> StableSort(
        IReadOnlyList<OrderedRecord> records,
        Comparison<OrderedRecord> comparison)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(comparison);

        List<OrderedRecord> sorted = new(records);
        for (int index = 1; index < sorted.Count; index++)
        {
            OrderedRecord current = sorted[index];
            int position = index - 1;
            while (position >= 0 && comparison(sorted[position], current) > 0)
            {
                sorted[position + 1] = sorted[position];
                position--;
            }

            sorted[position + 1] = current;
        }

        return sorted;
    }
}
