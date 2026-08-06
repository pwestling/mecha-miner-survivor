using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// Proves the storage half of <c>SIM-003</c>: one purpose-built store per authoritative
/// category, iteration ordered by the documented comparison, and an allocation-free churn
/// cycle.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-003-007</c>, <c>VER-SIM-003-010</c>, <c>VER-SIM-003-011</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative population categories and
/// § Entity identity; <c>docs/technical/10-runtime-architecture.md</c> § System phase
/// ordering.
/// </remarks>
[TestFixture]
internal sealed class PackedEntityStoreTests
{
    private const ulong RunSession = 0x9F0C_0001UL;
    private const int MiningSiteManifestCount = 63;
    private const int StaticWorldObjectManifestCount = 40;

    /// <summary>How many churn cycles the storage invariant is checked across.</summary>
    private const int ChurnCycles = 64;

    /// <summary>
    /// The provenance header committed with <c>entities-store-ordering.txt</c>, so the golden
    /// is reviewable against its authoritative rule rather than merely diffable.
    /// </summary>
    /// <remarks>
    /// doc 91 § Determinism and fixture policy: "Golden outputs are canonical, ordered, and
    /// reviewable text". A reviewer needs the rule and the fixture to judge the file, not just
    /// the bytes.
    /// </remarks>
    private const string GoldenHeader =
        "# entities-store-ordering\n"
        + "#\n"
        + "# Rule under test: doc 20 § Entity identity - \"Stable ordering uses the full\n"
        + "# entity ID after a system's authored priority keys.\" Ordering is therefore\n"
        + "# authored priority key ascending, then storage index, then generation, within\n"
        + "# one run session.\n"
        + "#\n"
        + "# Storage index precedes generation because it is the only component that\n"
        + "# discriminates among simultaneously-live entities; generation exists to order\n"
        + "# records that share a recycled slot. Doc 20 § Entity identity gives the ID two\n"
        + "# components - \"a reusable storage index and a generation\" - and treats the run\n"
        + "# session as the uniqueness scope: \"IDs are unique only within one run session\".\n"
        + "# Every case below holds the session constant, so no comparison here crosses\n"
        + "# sessions.\n"
        + "#\n"
        + "# Storage indices are rendered partition-relative as pickup+N, where N is the\n"
        + "# offset from the first slot of the Pickup partition. The partition offset itself\n"
        + "# is computed by the fixture from the doc 20 § Authoritative population\n"
        + "# categories capacity table, never written down as a literal. Three rows above\n"
        + "# Pickup - enemy projectile, weapon actor, damage zone - are doc 22 §\n"
        + "# Performance and capacity ceilings and move whenever doc 22 moves, so an\n"
        + "# absolute index here would make this ordering golden fail for a capacity reason\n"
        + "# that has nothing to do with ordering.\n"
        + "#\n"
        + "# Three cases, so that each component is the sole discriminator in at least one\n"
        + "# of them and no component is dead:\n"
        + "#\n"
        + "# 1. live-store-tied-priority-keys - sole discriminator: storage index.\n"
        + "#    PopulationCategory.Pickup, run session 0x9F0C0001, map manifest of 63 mining\n"
        + "#    sites and 40 static world objects. Eight records are admitted with priority\n"
        + "#    keys 30,10,20,10,30,10,20,20 in that order, then the third-admitted record\n"
        + "#    is removed. The removal is a swap-remove, so the dense storage order\n"
        + "#    afterwards is neither admission order nor key order. Every survivor is\n"
        + "#    simultaneously live, so every survivor has a distinct storage index and all\n"
        + "#    share generation 1: within a tied priority band the storage index is the\n"
        + "#    only component that can decide anything. This case is therefore blind to a\n"
        + "#    comparator that orders generation before storage index - case 2 exists to\n"
        + "#    catch that, and the negative control asserts this blindness rather than\n"
        + "#    leaving it implied.\n"
        + "#\n"
        + "# 2. retained-recycled-slot - sole discriminator: generation.\n"
        + "#    A retained record set, not a live store: a live store cannot produce this\n"
        + "#    shape, because two simultaneously-live entities have distinct storage\n"
        + "#    indices by construction. The record sets that legitimately hold a slot's\n"
        + "#    earlier occupant alongside the occupant that recycled it are recovery\n"
        + "#    snapshots, persisted history, and retained diagnostic or statistics\n"
        + "#    records. Slot 4 carries three generations at one priority key, so generation\n"
        + "#    is the only component that can order them. Slot 2 carries generation 7,\n"
        + "#    higher than any of them, and still sorts first: that is the direct proof\n"
        + "#    that storage index precedes generation rather than the other way round.\n"
        + "#\n"
        + "# 3. retained-tied-priority-keys - sole discriminator: storage index, with the\n"
        + "#    priority key shown to lead. All three records share generation 4, so\n"
        + "#    generation can decide nothing. Two share priority key 5 and differ only in\n"
        + "#    storage index. The third has priority key 42 and storage index 3, which\n"
        + "#    falls between the other two, so it sorts last despite its middle index.\n"
        + "#\n"
        + "# Derived by: the documented rule read off doc 20, computed in an independent\n"
        + "# Python reference before any C# ran, and cross-checked against an independent\n"
        + "# list sort in PackedEntityStoreTests. Not by accepting whatever the store\n"
        + "# emitted.\n"
        + "#\n";

    /// <summary>The name of the golden's first case, the one a live store can produce.</summary>
    private const string LiveStoreCaseName = "live-store-tied-priority-keys";

    /// <summary>The twelve categories doc 20 § Authoritative population categories tabulates, in table order.</summary>
    private static readonly string[] DocumentedCategoryNames =
    [
        "Player",
        "OrdinaryEnemy",
        "Elite",
        "Boss",
        "EnemyProjectile",
        "WeaponActor",
        "DamageZone",
        "MiningSite",
        "Pickup",
        "DestructibleRock",
        "RelicCache",
        "StaticWorldObject",
    ];

    /// <summary>
    /// Verification: <c>VER-SIM-003-007</c>.
    ///
    /// Exactly twelve categories exist, each gets one store with its own disjoint slot
    /// partition, and a thirteenth category has no store.
    /// </summary>
    [Test]
    public void OneStoreExistsForEachAuthoritativePopulationCategory()
    {
        EntityIdAllocator allocator = NewAllocator();

        List<PackedEntityStore<long>> stores = new(StoreCapacities.Categories.Count);
        foreach (PopulationCategory category in StoreCapacities.Categories)
        {
            stores.Add(new PackedEntityStore<long>(category, allocator));
        }

        string[] actualNames = new string[StoreCapacities.Categories.Count];
        for (int index = 0; index < StoreCapacities.Categories.Count; index++)
        {
            actualNames[index] = StoreCapacities.Categories[index].ToString();
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                StoreCapacities.Categories,
                Has.Count.EqualTo(12),
                "doc 20 § Authoritative population categories tabulates twelve rows; a "
                    + "thirteenth would be an unregistered category");
            Assert.That(
                Enum.GetValues<PopulationCategory>(),
                Has.Length.EqualTo(12),
                "and the enumeration must not carry a member the table does not");
            Assert.That(
                actualNames,
                Is.EqualTo(DocumentedCategoryNames),
                "the canonical iteration order must be doc 20's table order, because the "
                    + "allocator partitions the run's slot space by it");
            Assert.That(stores, Has.Count.EqualTo(12), "one store per category, no more and no fewer");
        });

        // Purpose-built per category rather than one universal table: each store owns a
        // disjoint slot partition, and together they tile the run's whole slot space.
        int expectedOffset = 0;
        bool[] categorySeen = new bool[12];
        Expect.Multiple(() =>
        {
            foreach (PackedEntityStore<long> store in stores)
            {
                int ordinal = (int)store.Category;
                Assert.That(categorySeen[ordinal], Is.False, "no category may have two stores");
                categorySeen[ordinal] = true;
                Assert.That(
                    allocator.SlotOffsetFor(store.Category),
                    Is.EqualTo(expectedOffset),
                    store.Category.ToString() + " must start where the previous partition ended");
                expectedOffset += store.Capacity.HardCapacity;
            }

            Assert.That(
                expectedOffset,
                Is.EqualTo(allocator.TotalSlotCapacity),
                "the twelve partitions must tile the run's slot space exactly, so no live "
                    + "identity can name two records");
            Assert.That(categorySeen, Is.All.True);
        });

        // An unregistered category has no store and no capacity.
        const PopulationCategory unregistered = (PopulationCategory)12;
        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentOutOfRangeException>(
                () => new PackedEntityStore<long>(unregistered, allocator));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => StoreCapacities.For(unregistered, MiningSiteManifestCount, StaticWorldObjectManifestCount));
            Expect.Throws<ArgumentOutOfRangeException>(() => allocator.CapacityFor(unregistered));
        });

        // Every store is usable, and the Player store already holds the run's one player.
        Expect.Multiple(() =>
        {
            foreach (PackedEntityStore<long> store in stores)
            {
                if (store.Category == PopulationCategory.Player)
                {
                    Assert.That(store.Count, Is.EqualTo(1), "the player exists from the start of the run");
                    Assert.That(store.TryGet(allocator.PlayerId, out long _), Is.True);
                    continue;
                }

                Assert.That(store.Count, Is.EqualTo(0), store.Category.ToString() + " starts empty");
                Assert.That(
                    store.Capacity.HardCapacity,
                    Is.GreaterThan(0),
                    store.Category.ToString() + " must declare a positive hard capacity");
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-003-010</c>.
    ///
    /// Iteration order is authored priority key then full entity ID: two stores holding the
    /// same members inserted in different orders iterate identically, ties on the key break
    /// by the full identity, and the order is not the dense storage order.
    /// </summary>
    /// <remarks>
    /// Permuting admission necessarily attaches different identities to the same records,
    /// because the allocator hands slots out in allocation order. So the cross-permutation
    /// comparison is made over the ordered <em>priority-key sequence</em>, which is what
    /// "the same members" can mean once identity depends on admission; identity's part in
    /// the order is asserted separately by the tie fixture, where equal keys leave the full
    /// entity ID as the only discriminator.
    /// </remarks>
    [Test]
    public void IterationOrderIsPriorityKeysThenFullEntityId()
    {
        // Distinct keys whose ascending order matches neither admission order used below.
        long[] distinctKeys = [30L, 10L, 20L, 40L, 50L, 15L, 25L, 35L];

        StoreContractAssertions.IterationOrderMatchesTheDocumentedComparison(
            "the packed pickup store, distinct priority keys",
            RenderSortedKeyReference(distinctKeys),
            RenderAdmittedKeySequence(distinctKeys, Ascending(distinctKeys.Length)),
            RenderAdmittedKeySequence(distinctKeys, Descending(distinctKeys.Length)));

        // Ties on the priority key leave the full entity ID as the only discriminator, and a
        // swap-remove has already made the dense storage order disagree with both.
        TieFixture fixture = BuildTieFixture();

        StoreContractAssertions.IterationOrderMatchesTheDocumentedComparison(
            "the packed pickup store, tied priority keys",
            fixture.ReferenceRendering,
            fixture.IteratedRendering,
            fixture.IteratedRendering);

        Expect.Multiple(() =>
        {
            Assert.That(
                fixture.IteratedRendering,
                Is.Not.EqualTo(fixture.StorageRendering),
                "iteration order must not be the dense storage order, or the ordering rule "
                    + "is untested: a swap-remove has already permuted storage");
            Assert.That(
                fixture.TiedKeyCount,
                Is.GreaterThan(1),
                "the fixture must actually contain tied priority keys");
        });

        GoldenText.Matches(
            "entities-store-ordering.txt",
            GoldenHeader
                + EntityOrderingCases.RenderCase(
                    LiveStoreCaseName,
                    "storage index",
                    fixture.OrderedRecords,
                    PickupPartitionOffset())
                + RenderRetainedCases());
    }

    /// <summary>
    /// Renders the two golden cases a live store cannot produce, each after checking that its shape
    /// is what makes its component reachable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A live store's records are all simultaneously live, so they have distinct storage indices, and
    /// storage index sorts before generation - so no store fixture of any size reaches the generation
    /// key. These two cases are retained record sets, where a slot's earlier occupant sits alongside
    /// the occupant that recycled it: recovery snapshots, persisted history, and retained diagnostic or
    /// statistics records.
    /// </para>
    /// <para>
    /// Both orders come from the documented comparison over production
    /// <see cref="EntityId.Compare"/>, and both are asserted to be independent of the arrival order
    /// they were built in, so the golden records the rule rather than the order the fixture happened to
    /// list.
    /// </para>
    /// </remarks>
    private static string RenderRetainedCases()
    {
        int partitionOffset = PickupPartitionOffset();

        List<EntityOrderingCases.OrderedRecord> recycled =
            EntityOrderingCases.RetainedRecycledSlot(NewAllocator());
        List<EntityOrderingCases.OrderedRecord> tied =
            EntityOrderingCases.RetainedTiedPriorityKeys(NewAllocator());

        AssertRetainedShape(
            "retained-recycled-slot",
            recycled,
            expectedSharedSlotGenerations: 3,
            requireDistinctGenerationsOnOneSlot: true);
        AssertRetainedShape(
            "retained-tied-priority-keys",
            tied,
            expectedSharedSlotGenerations: 1,
            requireDistinctGenerationsOnOneSlot: false);

        return RenderPermutationIndependentCase("retained-recycled-slot", "generation", recycled, partitionOffset)
            + RenderPermutationIndependentCase("retained-tied-priority-keys", "storage index", tied, partitionOffset);
    }

    /// <summary>
    /// Sorts a retained record set by the documented comparison, asserts a reversed arrival order
    /// produces the same rendering, and returns it.
    /// </summary>
    private static string RenderPermutationIndependentCase(
        string caseName,
        string soleDiscriminator,
        List<EntityOrderingCases.OrderedRecord> records,
        int partitionOffset)
    {
        List<EntityOrderingCases.OrderedRecord> reversed = new(records);
        reversed.Reverse();

        string rendering = EntityOrderingCases.RenderCase(
            caseName,
            soleDiscriminator,
            EntityOrderingCases.DocumentedSort(records),
            partitionOffset);
        string reversedRendering = EntityOrderingCases.RenderCase(
            caseName,
            soleDiscriminator,
            EntityOrderingCases.DocumentedSort(reversed),
            partitionOffset);

        Assert.That(
            reversedRendering,
            Is.EqualTo(rendering),
            caseName + ": the documented comparison must be a total order over this set, so a "
                + "reversed arrival order produces the identical result");

        return rendering;
    }

    /// <summary>
    /// Asserts that a retained record set really has the shape its case depends on, so the case cannot
    /// pass vacuously.
    /// </summary>
    /// <param name="caseName">The case's name, for failure messages.</param>
    /// <param name="records">The record set.</param>
    /// <param name="expectedSharedSlotGenerations">How many generations the most-shared slot must carry.</param>
    /// <param name="requireDistinctGenerationsOnOneSlot">
    /// Whether one slot must carry several generations, which is what makes the generation key
    /// reachable at all.
    /// </param>
    private static void AssertRetainedShape(
        string caseName,
        List<EntityOrderingCases.OrderedRecord> records,
        int expectedSharedSlotGenerations,
        bool requireDistinctGenerationsOnOneSlot)
    {
        Dictionary<int, HashSet<uint>> generationsBySlot = new();
        HashSet<ulong> sessions = new();
        foreach (EntityOrderingCases.OrderedRecord record in records)
        {
            if (!generationsBySlot.TryGetValue(record.Id.Index, out HashSet<uint>? generations))
            {
                generations = new HashSet<uint>();
                generationsBySlot[record.Id.Index] = generations;
            }

            generations.Add(record.Id.Generation);
            sessions.Add(record.Id.RunSession);
        }

        int mostSharedSlot = 0;
        foreach (KeyValuePair<int, HashSet<uint>> entry in generationsBySlot)
        {
            if (entry.Value.Count > mostSharedSlot)
            {
                mostSharedSlot = entry.Value.Count;
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                sessions,
                Has.Count.EqualTo(1),
                caseName + ": every record must share one run session, because doc 20 § Entity "
                    + "identity makes the session the scope IDs are unique within, not something to "
                    + "sort by");
            Assert.That(
                mostSharedSlot,
                Is.EqualTo(expectedSharedSlotGenerations),
                caseName + ": the most-shared storage index must carry exactly this many "
                    + "generations, or the case is not the shape it claims to be");
            if (requireDistinctGenerationsOnOneSlot)
            {
                Assert.That(
                    mostSharedSlot,
                    Is.GreaterThan(1),
                    caseName + ": one storage index must carry several generations, or the "
                        + "generation key is unreachable and this case is as vacuous as a live-store "
                        + "fixture");
            }
        });
    }

    /// <summary>
    /// The Pickup partition's first slot index, cross-checked between the allocator and an independent
    /// walk of the capacity table.
    /// </summary>
    /// <remarks>
    /// The golden renders storage indices against this base, so it must be derived and not written
    /// down: three of the rows summed into it are doc 22 § Performance and capacity ceilings that doc
    /// 22 reserves the right to move.
    /// </remarks>
    private static int PickupPartitionOffset()
    {
        EntityIdAllocator allocator = NewAllocator();
        int computed = EntityOrderingCases.ComputePickupOffsetFromCapacityTable(allocator);

        Assert.That(
            allocator.SlotOffsetFor(PopulationCategory.Pickup),
            Is.EqualTo(computed),
            "the Pickup partition offset must be the running sum of the hard capacities above it in "
                + "doc 20 § Authoritative population categories order");

        return computed;
    }

    /// <summary>
    /// Verification: <c>VER-SIM-003-011</c>.
    ///
    /// A full admit, mutate, resolve, order, and remove cycle allocates zero managed bytes
    /// after warm-up, which index-addressed plain arrays plus a free list satisfy without
    /// unsafe code.
    /// </summary>
    [Test]
    public void ChurnCycleAllocatesNothingAfterWarmUp()
    {
        // Structural half: the dense region is readonly plain arrays sized at construction, so a churn cycle
        // has nothing to allocate. A readonly array field assigned only in the constructor is
        // reference-identical for the object's lifetime, which is what "allocates nothing" means here.
        AssertDenseStorageFieldsAreReadonlyPlainArrays();

        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.Pickup, allocator);
        EntityId[] live = new EntityId[64];
        EntityId[] ordered = new EntityId[64];

        int capacityBefore = store.Capacity.HardCapacity;
        for (int cycle = 0; cycle < ChurnCycles; cycle++)
        {
            RunChurnCycle(store, live, ordered);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                store.Diagnostics.StoreGrowthCount,
                Is.EqualTo(0),
                ChurnCycles.ToString(CultureInfo.InvariantCulture)
                    + " admit-mutate-resolve-order-remove cycles must not enlarge a backing array once");
            Assert.That(
                store.Capacity.HardCapacity,
                Is.EqualTo(capacityBefore),
                "and the declared capacity must not move");
            Assert.That(
                store.QueueDepth,
                Is.EqualTo(0),
                "nothing queued, so the only growable arrays in the store were never touched");
            Assert.That(store.Count, Is.EqualTo(0), "the churn cycle must leave the store empty");
            Assert.That(
                store.Diagnostics.ReuseCount,
                Is.GreaterThan(0L),
                "the cycle must actually recycle slots, or it is not churn and the invariant is vacuous");
            Assert.That(
                store.Diagnostics.HighWaterMark,
                Is.EqualTo(live.Length),
                "the high-water mark must reflect the population the cycle reached");
            Assert.That(
                store.Diagnostics.Render(),
                Does.Contain("store-growth=0"),
                "and the counter must be observable to CMP-OBS-001, not only to this test");
        });

        // The zero above would be worthless if the counter could never be anything else, so prove it can.
        AssertGrowthCounterRisesWhenTheQueueGrows();
    }

    /// <summary>
    /// Proves the growth counter is capable of rising, so asserting zero across a churn cycle is evidence
    /// rather than an unconditional truth.
    /// </summary>
    /// <remarks>
    /// The churn cycle never reaches hard capacity and therefore never enqueues, so its
    /// <c>StoreGrowthCount == 0</c> would hold even if the counter were never wired up. An authored-enemy
    /// store driven past its ceiling by more than one queue's worth does grow, and must say so.
    /// </remarks>
    private static void AssertGrowthCounterRisesWhenTheQueueGrows()
    {
        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.Elite, allocator);
        int hardCapacity = store.Capacity.HardCapacity;

        for (int index = 0; index < hardCapacity; index++)
        {
            Assert.That(store.TryAdmit(index, index, out EntityId _), Is.True);
        }

        // The queue is preallocated to the hard capacity, so queueing more than that must enlarge it.
        for (int index = 0; index < hardCapacity + 1; index++)
        {
            Assert.That(
                store.TryAdmit(1_000 + index, 1_000 + index, out EntityId _),
                Is.False,
                "the store is at its ceiling, so these records queue");
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                store.QueueDepth,
                Is.EqualTo(hardCapacity + 1),
                "every refused authored admission queued, so nothing was cancelled");
            Assert.That(
                store.Diagnostics.StoreGrowthCount,
                Is.GreaterThan(0),
                "queueing past the preallocated queue size must enlarge it and be counted, which is what "
                    + "makes the churn cycle's zero meaningful");
            Assert.That(
                store.Count,
                Is.EqualTo(hardCapacity),
                "and no resident record was displaced by the queueing");
        });
    }

    /// <summary>
    /// Asserts that the dense record region is <see langword="readonly"/> plain arrays, and that the only
    /// non-readonly arrays are the authored-spawn queue, which doc 20 requires to be able to grow.
    /// </summary>
    /// <remarks>
    /// This is what proves the summary's "index-addressed plain arrays rather than a reflection-driven or
    /// pointer-based component table": the field types are arrays of the record type, not dictionaries,
    /// not component bags, and not pointers.
    /// </remarks>
    private static void AssertDenseStorageFieldsAreReadonlyPlainArrays()
    {
        FieldInfo[] fields = typeof(PackedEntityStore<long>).GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        List<string> readonlyArrays = new();
        List<string> growableArrays = new();
        List<string> violations = new();

        foreach (FieldInfo field in fields)
        {
            if (field.FieldType.IsPointer || field.FieldType == typeof(IntPtr))
            {
                violations.Add(field.Name + " is a pointer; the store must be plain arrays");
                continue;
            }

            if (!field.FieldType.IsArray)
            {
                continue;
            }

            if (field.IsInitOnly)
            {
                readonlyArrays.Add(field.Name);
            }
            else
            {
                growableArrays.Add(field.Name);
            }
        }

        readonlyArrays.Sort(StringComparer.Ordinal);
        growableArrays.Sort(StringComparer.Ordinal);

        Expect.Multiple(() =>
        {
            Assert.That(
                readonlyArrays,
                Is.EqualTo(new[] { "_denseIds", "_densePriorityKeys", "_denseStates", "_order", "_slotToDense" }),
                "the dense region and the ordering scratch are readonly arrays, so no operation can replace "
                    + "one; a new array field here is new per-churn storage and needs its own registry entry");
            Assert.That(
                growableArrays,
                Is.EqualTo(new[] { "_queuedPriorityKeys", "_queuedStates" }),
                "the authored-spawn queue is the only growable storage, because doc 20 § Capacity and overload "
                    + "behavior says a queued authored enemy later enters and the queue must never lose it");
            Assert.That(violations, Is.Empty, string.Join("; ", violations));
            Assert.That(
                typeof(PackedEntityStore<long>).GetField("_denseStates", BindingFlags.NonPublic | BindingFlags.Instance)!.FieldType,
                Is.EqualTo(typeof(long[])),
                "the record region is a contiguous array of the record type itself, not a component table");
        });
    }

    private static void RunChurnCycle(PackedEntityStore<long> store, EntityId[] live, EntityId[] ordered)
    {
        for (int index = 0; index < live.Length; index++)
        {
            store.TryAdmit(live.Length - index, index, out live[index]);
        }

        for (int index = 0; index < live.Length; index++)
        {
            store.TryUpdate(live[index], index * 2);
            store.TryGet(live[index], out long _);
        }

        store.CopyOrderedTo(ordered);

        for (int index = 0; index < live.Length; index++)
        {
            store.TryRemove(live[index]);
        }
    }

    private static int[] Ascending(int count)
    {
        int[] order = new int[count];
        for (int index = 0; index < count; index++)
        {
            order[index] = index;
        }

        return order;
    }

    private static int[] Descending(int count)
    {
        int[] order = new int[count];
        for (int index = 0; index < count; index++)
        {
            order[index] = count - 1 - index;
        }

        return order;
    }

    /// <summary>
    /// Admits every member in <paramref name="admissionOrder"/> and renders the ordered
    /// priority-key sequence the store iterates.
    /// </summary>
    private static string RenderAdmittedKeySequence(long[] priorityKeys, int[] admissionOrder)
    {
        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.Pickup, allocator);

        foreach (int position in admissionOrder)
        {
            Assert.That(
                store.TryAdmit(priorityKeys[position], position, out EntityId _),
                Is.True,
                "the pickup store must admit every member of the fixture");
        }

        EntityId[] ordered = new EntityId[store.Count];
        int written = store.CopyOrderedTo(ordered);
        Assert.That(written, Is.EqualTo(priorityKeys.Length));

        System.Text.StringBuilder builder = new();
        for (int index = 0; index < written; index++)
        {
            Assert.That(store.TryGetPriorityKey(ordered[index], out long key), Is.True);
            builder.Append(key.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Renders the ascending key sequence from an independent sort, not from the store.
    /// </summary>
    /// <remarks>doc 91 § Reference models: a deliberately simple model, so agreement is evidence about the rule.</remarks>
    private static string RenderSortedKeyReference(long[] priorityKeys)
    {
        List<long> sorted = new(priorityKeys);
        sorted.Sort();

        System.Text.StringBuilder builder = new();
        foreach (long key in sorted)
        {
            builder.Append(key.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds a store whose priority keys tie, whose dense storage order has been permuted by
    /// a swap-remove, and renders all three orders for comparison.
    /// </summary>
    private static TieFixture BuildTieFixture()
    {
        long[] keysByAdmission = [30L, 10L, 20L, 10L, 30L, 10L, 20L, 20L];

        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.Pickup, allocator);

        List<EntityId> admitted = new(keysByAdmission.Length);
        foreach (long key in keysByAdmission)
        {
            Assert.That(store.TryAdmit(key, admitted.Count, out EntityId issued), Is.True);
            admitted.Add(issued);
        }

        // A swap-remove of a middle record moves the last record into its place, so the dense
        // storage order is now neither admission order nor key order.
        Assert.That(store.TryRemove(admitted[2]), Is.True);
        admitted[2] = admitted[^1];
        admitted.RemoveAt(admitted.Count - 1);

        List<EntityId> storageOrder = new(admitted);

        EntityId[] ordered = new EntityId[store.Count];
        int written = store.CopyOrderedTo(ordered);
        Assert.That(written, Is.EqualTo(admitted.Count));

        List<EntityId> iterated = new(written);
        for (int index = 0; index < written; index++)
        {
            iterated.Add(ordered[index]);
        }

        // Independent reference comparison over the store's own contents.
        List<EntityId> reference = new(admitted);
        reference.Sort((left, right) =>
        {
            Assert.That(store.TryGetPriorityKey(left, out long leftKey), Is.True);
            Assert.That(store.TryGetPriorityKey(right, out long rightKey), Is.True);
            int byPriority = leftKey.CompareTo(rightKey);
            if (byPriority != 0)
            {
                return byPriority;
            }

            int bySession = left.RunSession.CompareTo(right.RunSession);
            if (bySession != 0)
            {
                return bySession;
            }

            int byIndex = left.Index.CompareTo(right.Index);
            return byIndex != 0 ? byIndex : left.Generation.CompareTo(right.Generation);
        });

        long PriorityKeyOf(EntityId id)
        {
            Assert.That(store.TryGetPriorityKey(id, out long key), Is.True);
            return key;
        }

        int tiedKeyCount = 0;
        for (int index = 1; index < iterated.Count; index++)
        {
            if (PriorityKeyOf(iterated[index]) == PriorityKeyOf(iterated[index - 1]))
            {
                tiedKeyCount++;
            }
        }

        int partitionOffset = allocator.SlotOffsetFor(PopulationCategory.Pickup);
        List<EntityOrderingCases.OrderedRecord> orderedRecords = new(iterated.Count);
        foreach (EntityId identity in iterated)
        {
            orderedRecords.Add(new EntityOrderingCases.OrderedRecord(PriorityKeyOf(identity), identity));
        }

        return new TieFixture(
            StoreContractAssertions.RenderOrder(
                iterated, PriorityKeyOf, EntityOrderingCases.PickupPartitionLabel, partitionOffset),
            StoreContractAssertions.RenderOrder(
                reference, PriorityKeyOf, EntityOrderingCases.PickupPartitionLabel, partitionOffset),
            StoreContractAssertions.RenderOrder(
                storageOrder, PriorityKeyOf, EntityOrderingCases.PickupPartitionLabel, partitionOffset),
            tiedKeyCount,
            orderedRecords);
    }

    /// <summary>The three renderings of one tie fixture, plus how many adjacent ties it contains.</summary>
    /// <param name="IteratedRendering">What the store iterated.</param>
    /// <param name="ReferenceRendering">What the independent comparison produced.</param>
    /// <param name="StorageRendering">The dense storage order, which must differ from both.</param>
    /// <param name="TiedKeyCount">How many adjacent pairs in the iterated order share a priority key.</param>
    /// <param name="OrderedRecords">
    /// The iterated order as key-and-identity pairs, so the golden's first case and the negative
    /// control's degradation matrix judge the same records the store produced.
    /// </param>
    private readonly record struct TieFixture(
        string IteratedRendering,
        string ReferenceRendering,
        string StorageRendering,
        int TiedKeyCount,
        List<EntityOrderingCases.OrderedRecord> OrderedRecords);

    private static EntityIdAllocator NewAllocator()
    {
        return new EntityIdAllocator(
            RunSession,
            MiningSiteManifestCount,
            StaticWorldObjectManifestCount);
    }
}
