using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Entities;

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
/// <b>Run session is held constant everywhere.</b> doc 20 § Entity identity treats the run session as
/// the scope within which IDs are unique - "IDs are unique only within one run session" - rather than
/// as something to sort by, so no comparison in this file crosses sessions.
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
    }

    /// <summary>Every degradation, in declaration order, for the control matrix.</summary>
    internal static IReadOnlyList<Degradation> AllDegradations { get; } =
    [
        Degradation.WithoutStorageIndex,
        Degradation.WithoutGeneration,
        Degradation.WithoutPriorityKey,
        Degradation.GenerationBeforeStorageIndex,
    ];

    /// <summary>
    /// Case 2 - <c>retained-recycled-slot</c>. Generation is the sole discriminator.
    /// </summary>
    /// <param name="runSession">The run session every record is scoped to.</param>
    /// <param name="partitionOffset">The Pickup partition's first slot index.</param>
    /// <remarks>
    /// Slot 4 carries three generations at one priority key, so generation is the only component that
    /// can order them. Slot 2 carries generation 7, higher than any of the three, and still sorts
    /// first: that is the direct proof that storage index precedes generation rather than the reverse.
    /// </remarks>
    internal static List<OrderedRecord> RetainedRecycledSlot(ulong runSession, int partitionOffset)
    {
        return
        [
            new OrderedRecord(10L, EntityId.Create(runSession, partitionOffset + 4, 1)),
            new OrderedRecord(10L, EntityId.Create(runSession, partitionOffset + 4, 3)),
            new OrderedRecord(10L, EntityId.Create(runSession, partitionOffset + 4, 2)),
            new OrderedRecord(10L, EntityId.Create(runSession, partitionOffset + 2, 7)),
        ];
    }

    /// <summary>
    /// Case 3 - <c>retained-tied-priority-keys</c>. Storage index is the sole discriminator, and the
    /// priority key is shown to lead.
    /// </summary>
    /// <param name="runSession">The run session every record is scoped to.</param>
    /// <param name="partitionOffset">The Pickup partition's first slot index.</param>
    /// <remarks>
    /// All three share generation 4, so generation can decide nothing. Two share priority key 5 and
    /// differ only in storage index. The third has priority key 42 and storage index 3, which falls
    /// between the other two, so it sorts last despite its middle index - which is what makes the
    /// priority key's precedence over the identity observable rather than assumed.
    /// </remarks>
    internal static List<OrderedRecord> RetainedTiedPriorityKeys(ulong runSession, int partitionOffset)
    {
        return
        [
            new OrderedRecord(5L, EntityId.Create(runSession, partitionOffset + 9, 4)),
            new OrderedRecord(5L, EntityId.Create(runSession, partitionOffset + 2, 4)),
            new OrderedRecord(42L, EntityId.Create(runSession, partitionOffset + 3, 4)),
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

    private static int CompareDocumented(OrderedRecord left, OrderedRecord right)
    {
        int byPriority = left.PriorityKey.CompareTo(right.PriorityKey);
        return byPriority != 0 ? byPriority : EntityId.Compare(left.Id, right.Id);
    }

    private static int CompareDegraded(OrderedRecord left, OrderedRecord right, Degradation degradation)
    {
        switch (degradation)
        {
            case Degradation.WithoutStorageIndex:
                {
                    int byPriority = left.PriorityKey.CompareTo(right.PriorityKey);
                    return byPriority != 0
                        ? byPriority
                        : left.Id.Generation.CompareTo(right.Id.Generation);
                }

            case Degradation.WithoutGeneration:
                {
                    int byPriority = left.PriorityKey.CompareTo(right.PriorityKey);
                    return byPriority != 0 ? byPriority : left.Id.Index.CompareTo(right.Id.Index);
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

                    int byGeneration = left.Id.Generation.CompareTo(right.Id.Generation);
                    return byGeneration != 0 ? byGeneration : left.Id.Index.CompareTo(right.Id.Index);
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
