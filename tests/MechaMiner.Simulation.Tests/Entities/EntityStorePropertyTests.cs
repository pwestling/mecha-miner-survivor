using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// Compares the packed store against a deliberately simple dictionary reference model over
/// randomized allocate, free, and resolve sequences.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-003-005</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Scope and invariants: "every live entity ID
/// resolves to exactly one live record of the matching generation".
/// <c>docs/technical/91-verification-strategy.md</c> § Reference models.
///
/// The reference model is a <see cref="Dictionary{TKey, TValue}"/> on purpose, and that is
/// not a contradiction of the repository rule that dictionaries never define authoritative
/// order: the model answers only "is this identity live?", never "in what order?". Nothing
/// in this test reads an enumeration order from it.
/// </remarks>
[TestFixture]
internal sealed class EntityStorePropertyTests
{
    private const ulong RunSession = 0x8888_0001UL;
    private const int MiningSiteManifestCount = 6;
    private const int StaticWorldObjectManifestCount = 4;
    private const int DeclaredSeed = 611_003;

    /// <summary>
    /// Verification: <c>VER-SIM-003-005</c>.
    ///
    /// Over randomized operation sequences every live identity resolves to exactly one live
    /// record of the matching generation, and every freed identity fails closed, matching the
    /// reference model at every step.
    /// </summary>
    [Test]
    public void LiveAndFreedIdentitiesMatchTheReferenceModel()
    {
        PropertyCase.ForAll(
            "entities-live-and-freed-identities",
            DeclaredSeed,
            caseCount: 96,
            generate: random =>
            {
                int[] operations = new int[random.Next(0, 60)];
                for (int index = 0; index < operations.Length; index++)
                {
                    operations[index] = random.Next(0, 30);
                }

                return operations;
            },
            shrink: Shrinkers.Int32Array,
            render: operations => "[" + string.Join(",", operations) + "]",
            property: RunOperations);
    }

    /// <summary>
    /// Replays one encoded operation sequence against the store and the reference model,
    /// asserting agreement after every step.
    /// </summary>
    /// <remarks>
    /// The encoding is deliberately crude so that <see cref="Shrinkers.Int32Array"/> can
    /// shrink a failure into something a human reads: each element's remainder modulo three
    /// selects admit, remove, or resolve, and its quotient selects which known identity the
    /// operation touches.
    /// </remarks>
    private static void RunOperations(int[] operations)
    {
        EntityIdAllocator allocator = new(
            RunSession,
            MiningSiteManifestCount,
            StaticWorldObjectManifestCount);
        PackedEntityStore<long> store = new(PopulationCategory.MiningSite, allocator);
        int hardCapacity = store.Capacity.HardCapacity;

        // Reference model: slot -> (generation, live). A dictionary is the simplest thing
        // that can disagree with the implementation, which is the point of a reference model.
        Dictionary<int, ModelSlot> model = new(hardCapacity);
        List<EntityId> issued = new(operations.Length + 1);
        List<EntityId> liveIds = new(hardCapacity);

        for (int step = 0; step < operations.Length; step++)
        {
            int operation = operations[step];
            int selector = operation / 3;

            switch (operation % 3)
            {
                case 0:
                    if (liveIds.Count < hardCapacity)
                    {
                        Assert.That(store.TryAdmit(step, step, out EntityId admitted), Is.True);
                        int slot = admitted.Index;
                        model[slot] = new ModelSlot(admitted.Generation, IsLive: true);
                        issued.Add(admitted);
                        liveIds.Add(admitted);
                    }

                    break;

                case 1:
                    if (liveIds.Count > 0)
                    {
                        EntityId victim = liveIds[selector % liveIds.Count];
                        Assert.That(store.TryRemove(victim), Is.True);
                        model[victim.Index] = new ModelSlot(victim.Generation, IsLive: false);
                        liveIds.Remove(victim);
                    }

                    break;

                default:
                    if (issued.Count > 0)
                    {
                        EntityId probe = issued[selector % issued.Count];
                        bool modelSaysLive = model.TryGetValue(probe.Index, out ModelSlot slotState)
                            && slotState.IsLive
                            && slotState.Generation == probe.Generation;
                        Assert.That(
                            store.TryGet(probe, out long _),
                            Is.EqualTo(modelSaysLive),
                            "step " + step.ToString(CultureInfo.InvariantCulture)
                                + ": resolution must agree with the reference model for "
                                + probe.ToString());
                    }

                    break;
            }

            AssertAgreement(store, model, issued, liveIds, step);
        }
    }

    private static void AssertAgreement(
        PackedEntityStore<long> store,
        Dictionary<int, ModelSlot> model,
        List<EntityId> issued,
        List<EntityId> liveIds,
        int step)
    {
        string where = "step " + step.ToString(CultureInfo.InvariantCulture) + ": ";

        Assert.That(
            store.Count,
            Is.EqualTo(liveIds.Count),
            where + "residency must equal the model's live count");

        foreach (EntityId id in liveIds)
        {
            Assert.That(
                store.TryGet(id, out long _),
                Is.True,
                where + "every live identity must resolve: " + id.ToString());
        }

        foreach (EntityId id in issued)
        {
            bool modelSaysLive = model.TryGetValue(id.Index, out ModelSlot slotState)
                && slotState.IsLive
                && slotState.Generation == id.Generation;
            if (modelSaysLive)
            {
                continue;
            }

            Assert.That(
                store.TryGet(id, out long _),
                Is.False,
                where + "every freed or superseded identity must fail closed: " + id.ToString());
        }

        // "Exactly one live record": no two live identities may share a slot.
        HashSet<int> liveSlots = new(liveIds.Count);
        foreach (EntityId id in liveIds)
        {
            Assert.That(
                liveSlots.Add(id.Index),
                Is.True,
                where + "two live identities share slot "
                    + id.Index.ToString(CultureInfo.InvariantCulture));
        }

        EntityId[] ordered = new EntityId[store.Count];
        int written = store.CopyOrderedTo(ordered);
        Assert.That(written, Is.EqualTo(liveIds.Count), where + "the ordered batch must hold every live record");
        for (int index = 1; index < written; index++)
        {
            Assert.That(
                EntityId.Compare(ordered[index - 1], ordered[index]),
                Is.Not.EqualTo(0),
                where + "the ordered batch must not contain a duplicate identity");
        }
    }

    /// <summary>One slot's state in the reference model.</summary>
    /// <param name="Generation">The generation the slot last issued.</param>
    /// <param name="IsLive">Whether that generation is still live.</param>
    private readonly record struct ModelSlot(uint Generation, bool IsLive);
}
