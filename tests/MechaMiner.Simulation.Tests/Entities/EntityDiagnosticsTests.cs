using System;
using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// Pins the two properties of the diagnostic counters that nothing else asserts: reading one never resets
/// it, and every field doc 20 enumerates is present in the canonical rendering with the value the run
/// produced.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports <c>VER-SIM-003-008</c>.
/// </para>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Capacity and overload behavior: "Capacity, high-water
/// mark, queue depth, reuse count, and rejected visual requests are diagnostic metrics", plus § Entity
/// identity's failed-resolution counter.
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Frame metrics is what makes
/// non-resetting reads load-bearing: it expects these to be sampled repeatedly during a run, and
/// <c>EntityDiagnostics</c> states the rule outright - a counter a read clears "cannot be reconciled
/// against the operations that produced it".
/// </para>
/// <para>
/// <b>Why a whole-string comparison and not a substring.</b> A read-twice-and-compare over one counter is
/// true of a counter that was deleted, and the same holds for a rendering compared against another
/// rendering of the same object: both sides lose the field together. So the rendering is compared against
/// text this test states, every counter is asserted to be non-zero before it is asserted to be stable, and
/// the <c>retired=</c> field - which had no assertion of any kind and could be deleted outright - is one of
/// the fields that text contains.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class EntityDiagnosticsTests
{
    private const ulong RunSession = 0x0D1A_0001UL;

    /// <summary>
    /// The generation ceiling, low enough that one recycle exhausts a slot and the retirement counter is
    /// reachable. doc 20's behaviour is unchanged by the ceiling; only the number of recycles is.
    /// </summary>
    private const uint MaximumGeneration = 2;

    /// <summary>
    /// Verification: supports <c>VER-SIM-003-008</c>.
    ///
    /// Every counter is driven to a distinct non-zero value, read twice, and rendered twice: no read moves
    /// a counter, and the rendering carries all eleven fields with the values the run produced.
    /// </summary>
    [Test]
    public void ReadingACounterNeverResetsItAndTheRenderingCarriesEveryField()
    {
        EntityIdAllocator allocator = new(
            RunSession,
            miningSiteManifestCount: 1,
            staticWorldObjectManifestCount: 1,
            MaximumGeneration);
        PackedEntityStore<long> store = new(PopulationCategory.Elite, allocator);
        EntityDiagnostics diagnostics = store.Diagnostics;

        int hardCapacity = store.Capacity.HardCapacity;
        string renderedBeforeAnyRetirement = diagnostics.Render();

        for (int index = 0; index < hardCapacity; index++)
        {
            Assert.That(store.TryAdmit(index, index, out EntityId _), Is.True);
        }

        // One more than the queue's preallocated length, so the queue enlarges and says so.
        for (int index = 0; index <= hardCapacity; index++)
        {
            Assert.That(
                store.TryAdmit(1_000 + index, 1_000 + index, out EntityId _),
                Is.False,
                "the store is at its ceiling, so these queue rather than entering");
        }

        Assert.That(store.TryAdmit(0, 0, out EntityId _), Is.False);
        Assert.That(store.CopyOrderedTo(new EntityId[hardCapacity]), Is.EqualTo(hardCapacity));

        EntityId[] resident = new EntityId[hardCapacity];
        store.CopyOrderedTo(resident);
        EntityId firstLife = resident[0];
        Assert.That(store.TryRemove(firstLife), Is.True, "one record leaves, freeing its slot");
        Assert.That(
            store.TryGet(firstLife, out long _),
            Is.False,
            "and resolving it afterwards is the failed resolution the counter is for");
        Assert.That(
            store.TryAdmit(500, 500, out EntityId secondLife),
            Is.True,
            "the freed slot is reused, which is the reuse counter");
        Assert.That(
            secondLife.Generation,
            Is.EqualTo(MaximumGeneration),
            "at the generation ceiling, so removing it retires the slot");
        Assert.That(store.TryRemove(secondLife), Is.True);

        // Two reads of every counter, and two renderings, with nothing in between.
        long[] firstReads =
        [
            diagnostics.LiveCount,
            diagnostics.HighWaterMark,
            diagnostics.QueueDepth,
            diagnostics.ReuseCount,
            diagnostics.RejectedRequests,
            diagnostics.StaleReferenceResolutions,
            diagnostics.RetiredSlotCount,
            diagnostics.StoreGrowthCount,
        ];
        long[] secondReads =
        [
            diagnostics.LiveCount,
            diagnostics.HighWaterMark,
            diagnostics.QueueDepth,
            diagnostics.ReuseCount,
            diagnostics.RejectedRequests,
            diagnostics.StaleReferenceResolutions,
            diagnostics.RetiredSlotCount,
            diagnostics.StoreGrowthCount,
        ];
        string firstRendering = diagnostics.Render();
        string secondRendering = diagnostics.Render();
        long[] readsAfterRendering =
        [
            diagnostics.LiveCount,
            diagnostics.HighWaterMark,
            diagnostics.QueueDepth,
            diagnostics.ReuseCount,
            diagnostics.RejectedRequests,
            diagnostics.StaleReferenceResolutions,
            diagnostics.RetiredSlotCount,
            diagnostics.StoreGrowthCount,
        ];

        string expectedRendering =
            "category=Elite live=14 soft=13 hard=15 high-water=15 queue-depth=17 reuse=1 rejected=17 "
            + "stale=1 retired=1 store-growth=1";

        Expect.Multiple(() =>
        {
            for (int index = 0; index < firstReads.Length; index++)
            {
                Assert.That(
                    firstReads[index],
                    Is.GreaterThan(0L),
                    "counter " + index.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        + " must be non-zero before its stability is asserted, or the stability assertion "
                        + "would also hold for a counter that was never wired up");
            }

            Assert.That(
                secondReads,
                Is.EqualTo(firstReads).AsCollection,
                "reading a counter must not reset it: doc 90 § Frame metrics samples these repeatedly, so a "
                    + "read that cleared one would make the sample disagree with the operations behind it");
            Assert.That(
                readsAfterRendering,
                Is.EqualTo(firstReads).AsCollection,
                "and rendering them must not reset them either, which is the same rule for the member a "
                    + "diagnostics export actually calls");
            Assert.That(
                firstRendering,
                Is.EqualTo(expectedRendering).Using(StringComparer.Ordinal),
                "the rendering carries every field with the value this run produced, compared against text "
                    + "stated here rather than against another rendering of the same object");
            Assert.That(
                secondRendering,
                Is.EqualTo(firstRendering).Using(StringComparer.Ordinal),
                "and it is stable across calls");
            Assert.That(
                renderedBeforeAnyRetirement,
                Does.Contain("retired=0"),
                "before any slot was exhausted the retirement field read zero, so the one above is a value "
                    + "the run moved and not a constant");
        });
    }
}
