using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// Proves the identity and ordering gates can fail, by running the same assertions the real
/// gates run against stubs that are deliberately wrong.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-003-012</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence requires evidence
/// that a gate can fail, not only that it currently passes. The stubs here are ordinary valid
/// C# that behaves incorrectly - not a deliberately invalid fixture, which
/// <c>docs/technical/delivery-waves.md</c> forbids inside a compiled project.
/// </para>
/// <para>
/// The assertions come from <see cref="StoreContractAssertions"/>, the same code
/// <see cref="EntityIdTests"/> and <see cref="PackedEntityStoreTests"/> use. That is what
/// makes this a control rather than a second opinion: weakening the shared assertion turns
/// both the real gate and this control red at once.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class EntityStoreNegativeControlTests
{
    private const ulong RunSession = 0xDEAD_0001UL;
    private const ulong OtherRunSession = 0xDEAD_0002UL;
    private const int MiningSiteManifestCount = 8;
    private const int StaticWorldObjectManifestCount = 4;

    /// <summary>
    /// Verification: <c>VER-SIM-003-012</c>.
    ///
    /// A store that ignores the generation field fails the stale-reference assertion; a store
    /// that iterates in insertion order fails the ordering assertion; and the correct store
    /// passes both.
    /// </summary>
    [Test]
    public void IdentityAndOrderingAssertionsFailAgainstDeliberatelyBrokenStubs()
    {
        AssertGenerationIgnoringStoreFailsTheStaleReferenceGate();
        AssertUncountedResolutionFailsTheDiagnosticGate();
        AssertInsertionOrderedStoreFailsTheOrderingGate();
        AssertEachGoldenCaseDetectsTheComponentItIsThereFor();
        AssertTheRealStorePassesBothGates();
    }

    /// <summary>
    /// Proves each case of <c>entities-store-ordering.txt</c> is not vacuous, by removing one
    /// documented component from the comparator at a time and asserting exactly which cases notice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// doc 91 § Acceptance evidence wants evidence a gate can fail. For an ordering golden the sharp
    /// question is not whether the file matches but whether its inputs reach every component of the
    /// comparator - a case whose records cannot distinguish two comparators proves nothing about the
    /// difference between them, and an ordering golden built only from a live store is exactly that:
    /// its records are all simultaneously live, so they hold distinct storage indices and one shared
    /// generation.
    /// </para>
    /// <para>
    /// So the blindness is asserted, not only the detection. The live-store case must be blind to a
    /// comparator that orders generation before storage index, because that is the fact which makes the
    /// two retained cases necessary; if it ever stops being blind, the reasoning behind those cases has
    /// changed and should be re-read rather than silently outlived.
    /// </para>
    /// </remarks>
    private static void AssertEachGoldenCaseDetectsTheComponentItIsThereFor()
    {
        EntityIdAllocator allocator = NewAllocator();
        int partitionOffset = EntityOrderingCases.ComputePickupOffsetFromCapacityTable(allocator);

        AssertCaseDetection(
            "live-store-tied-priority-keys",
            BuildLiveStoreRecords(),
            partitionOffset,
            expectedDetections:
            [
                EntityOrderingCases.Degradation.WithoutStorageIndex,
                EntityOrderingCases.Degradation.WithoutPriorityKey,
            ]);

        AssertCaseDetection(
            "retained-recycled-slot",
            EntityOrderingCases.RetainedRecycledSlot(NewAllocator()),
            partitionOffset,
            expectedDetections:
            [
                EntityOrderingCases.Degradation.WithoutStorageIndex,
                EntityOrderingCases.Degradation.WithoutGeneration,
                EntityOrderingCases.Degradation.GenerationBeforeStorageIndex,
            ]);

        AssertCaseDetection(
            "retained-tied-priority-keys",
            EntityOrderingCases.RetainedTiedPriorityKeys(NewAllocator()),
            partitionOffset,
            expectedDetections:
            [
                EntityOrderingCases.Degradation.WithoutStorageIndex,
                EntityOrderingCases.Degradation.WithoutPriorityKey,
            ]);

        // The case the golden had no equivalent of. Every case above holds one run session, so all three are
        // blind to WithoutRunSession - which is asserted above by its absence from their expected lists, and
        // is what let the leading key be deleted with the suite staying green.
        AssertCaseDetection(
            "retained-cross-session-records",
            EntityOrderingCases.RetainedCrossSessionRecords(NewAllocator(), NewOtherSessionAllocator()),
            partitionOffset,
            expectedDetections:
            [
                EntityOrderingCases.Degradation.WithoutRunSession,
                EntityOrderingCases.Degradation.WithoutStorageIndex,
            ]);
    }

    /// <summary>
    /// Asserts that exactly <paramref name="expectedDetections"/> of the degradations change one case's
    /// rendering, and that the others leave it byte-identical.
    /// </summary>
    /// <param name="caseName">The case's name, for failure messages.</param>
    /// <param name="records">The case's record set, in arrival order.</param>
    /// <param name="partitionOffset">The Pickup partition's first slot index.</param>
    /// <param name="expectedDetections">The degradations this case is expected to notice.</param>
    /// <remarks>
    /// A degradation is detected when it fails to reproduce the documented rendering for at least one
    /// arrival order. Two arrival orders are tried, because a dropped component shows up as a tie, and
    /// a tie under a stable sort is only visible as a disagreement between permutations.
    /// </remarks>
    private static void AssertCaseDetection(
        string caseName,
        List<EntityOrderingCases.OrderedRecord> records,
        int partitionOffset,
        EntityOrderingCases.Degradation[] expectedDetections)
    {
        List<EntityOrderingCases.OrderedRecord> reversed = new(records);
        reversed.Reverse();

        string expected = Render(caseName, EntityOrderingCases.DocumentedSort(records), partitionOffset);

        List<string> detected = new();
        List<string> blind = new();
        foreach (EntityOrderingCases.Degradation degradation in EntityOrderingCases.AllDegradations)
        {
            string ascending = Render(
                caseName, EntityOrderingCases.DegradedSort(records, degradation), partitionOffset);
            string descending = Render(
                caseName, EntityOrderingCases.DegradedSort(reversed, degradation), partitionOffset);

            bool notices = !string.Equals(ascending, expected, StringComparison.Ordinal)
                || !string.Equals(descending, expected, StringComparison.Ordinal);
            (notices ? detected : blind).Add(degradation.ToString());
        }

        List<string> expectedNames = new(expectedDetections.Length);
        foreach (EntityOrderingCases.Degradation degradation in expectedDetections)
        {
            expectedNames.Add(degradation.ToString());
        }

        detected.Sort(StringComparer.Ordinal);
        expectedNames.Sort(StringComparer.Ordinal);

        Assert.That(
            detected,
            Is.EqualTo(expectedNames),
            caseName + ": these are the degradations the case must notice, and the ones it must not. "
                + "A case that notices fewer is vacuous for the component it was added for; a case "
                + "that notices more means the fixture shape changed and the golden's header no "
                + "longer describes it. Blind here: " + string.Join(", ", blind));
    }

    private static string Render(
        string caseName,
        List<EntityOrderingCases.OrderedRecord> ordered,
        int partitionOffset)
    {
        return EntityOrderingCases.RenderCase(caseName, "control", ordered, partitionOffset);
    }

    /// <summary>
    /// Rebuilds the live-store case's records: eight admissions at tied priority keys with the
    /// third-admitted record swap-removed.
    /// </summary>
    /// <remarks>
    /// Built through the real store, so the control judges the same records the golden's first case
    /// contains rather than a hand-written imitation of them.
    /// </remarks>
    private static List<EntityOrderingCases.OrderedRecord> BuildLiveStoreRecords()
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

        Assert.That(store.TryRemove(admitted[2]), Is.True);
        admitted[2] = admitted[^1];
        admitted.RemoveAt(admitted.Count - 1);

        List<EntityOrderingCases.OrderedRecord> records = new(admitted.Count);
        foreach (EntityId identity in admitted)
        {
            Assert.That(store.TryGetPriorityKey(identity, out long key), Is.True);
            records.Add(new EntityOrderingCases.OrderedRecord(key, identity));
        }

        return records;
    }

    /// <summary>
    /// A stub that resolves by storage index alone hands back the live record that now occupies
    /// a recycled slot, which is precisely the aliasing generations exist to prevent.
    /// </summary>
    private static void AssertGenerationIgnoringStoreFailsTheStaleReferenceGate()
    {
        GenerationIgnoringStore broken = new();
        IssueStaleAndLivePair(out EntityId stale, out EntityId live);
        broken.Place(live);

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => StoreContractAssertions.GenerationMismatchFailsClosed(
                "a stub store that ignores the generation field",
                stale,
                live,
                broken.Resolve,
                () => broken.StaleReferenceResolutions));

        Assert.That(
            failure.Message,
            Does.Contain("must fail closed"),
            "the stale-reference gate must be the assertion that failed, not some other one");
    }

    /// <summary>
    /// A stub that fails closed but does not count the failure still breaks the gate: doc 20 §
    /// Entity identity requires the diagnostic counter, not only the refusal.
    /// </summary>
    private static void AssertUncountedResolutionFailsTheDiagnosticGate()
    {
        GenerationIgnoringStore broken = new(honourGenerations: true, countStaleReferences: false);
        IssueStaleAndLivePair(out EntityId stale, out EntityId live);
        broken.Place(live);

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => StoreContractAssertions.GenerationMismatchFailsClosed(
                "a stub store that fails closed without counting",
                stale,
                live,
                broken.Resolve,
                () => broken.StaleReferenceResolutions));

        Assert.That(
            failure.Message,
            Does.Contain("exactly one diagnostic counter increment"),
            "the diagnostic half of the gate must be what failed");
    }

    /// <summary>
    /// A stub that iterates in insertion order agrees with itself but disagrees with the
    /// documented comparison, so the ordering gate must reject it.
    /// </summary>
    private static void AssertInsertionOrderedStoreFailsTheOrderingGate()
    {
        long[] keysByAdmission = [30L, 10L, 20L, 10L];
        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.Pickup, allocator);

        List<EntityId> insertionOrder = new(keysByAdmission.Length);
        foreach (long key in keysByAdmission)
        {
            Assert.That(store.TryAdmit(key, insertionOrder.Count, out EntityId issued), Is.True);
            insertionOrder.Add(issued);
        }

        EntityId[] ordered = new EntityId[store.Count];
        int written = store.CopyOrderedTo(ordered);
        List<EntityId> correctOrder = new(written);
        for (int index = 0; index < written; index++)
        {
            correctOrder.Add(ordered[index]);
        }

        long PriorityKeyOf(EntityId id)
        {
            Assert.That(store.TryGetPriorityKey(id, out long key), Is.True);
            return key;
        }

        int partitionOffset = allocator.SlotOffsetFor(PopulationCategory.Pickup);
        string insertionRendering = StoreContractAssertions.RenderOrder(
            insertionOrder, PriorityKeyOf, EntityOrderingCases.PickupPartitionLabel, partitionOffset);
        string correctRendering = StoreContractAssertions.RenderOrder(
            correctOrder, PriorityKeyOf, EntityOrderingCases.PickupPartitionLabel, partitionOffset);

        Assert.That(
            insertionRendering,
            Is.Not.EqualTo(correctRendering),
            "the fixture must be one where insertion order and key order genuinely differ, or "
                + "the control proves nothing");

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => StoreContractAssertions.IterationOrderMatchesTheDocumentedComparison(
                "a stub store that iterates in insertion order",
                correctRendering,
                insertionRendering,
                insertionRendering));

        Assert.That(
            failure.Message,
            Does.Contain("authored priority key ascending"),
            "the ordering gate must be the assertion that failed");
    }

    /// <summary>The same two assertions must pass against the real store, or the control is vacuous.</summary>
    private static void AssertTheRealStorePassesBothGates()
    {
        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.Pickup, allocator);

        Assert.That(store.TryAdmit(0L, 1L, out EntityId original), Is.True);
        Assert.That(store.TryRemove(original), Is.True);
        Assert.That(store.TryAdmit(0L, 2L, out EntityId recycled), Is.True);

        Expect.DoesNotThrow(() => StoreContractAssertions.GenerationMismatchFailsClosed(
            "the real packed store",
            original,
            recycled,
            id => store.TryGet(id, out long _),
            () => store.Diagnostics.StaleReferenceResolutions));
    }

    /// <summary>
    /// Issues a stale identity and the live identity that recycled its slot, by admit, remove,
    /// admit on a real store.
    /// </summary>
    /// <param name="stale">The identity whose slot was recycled underneath it.</param>
    /// <param name="live">The identity that now occupies that slot.</param>
    /// <remarks>
    /// The pair comes from the allocator through the store, the same route
    /// <see cref="AssertTheRealStorePassesBothGates"/> uses, rather than from a factory. That is not
    /// a stylistic preference: the gate under test is about an identity a caller could genuinely be
    /// holding after a slot was recycled, and a pair whose index and generation were chosen by hand
    /// would demonstrate only that the assertion reads two fields. It is also why
    /// <c>EntityId.Create</c> can be internal to the simulation assembly: nothing outside it needs
    /// to mint an identity, so nothing outside it should be able to.
    /// </remarks>
    private static void IssueStaleAndLivePair(out EntityId stale, out EntityId live)
    {
        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.Pickup, allocator);

        Assert.That(store.TryAdmit(0L, 1L, out stale), Is.True, "the store must admit the first record");
        Assert.That(store.TryRemove(stale), Is.True, "the record must be removable");
        Assert.That(store.TryAdmit(0L, 2L, out live), Is.True, "the freed slot must be reusable");

        Assert.That(
            live.Index,
            Is.EqualTo(stale.Index),
            "the pair must share a storage index, or the generation is not what distinguishes them");
        Assert.That(
            live.Generation,
            Is.Not.EqualTo(stale.Generation),
            "and it must differ in generation, or there is no stale reference to fail closed on");
    }

    private static EntityIdAllocator NewAllocator()
    {
        return new EntityIdAllocator(
            RunSession,
            MiningSiteManifestCount,
            StaticWorldObjectManifestCount);
    }

    /// <summary>An allocator for a second, higher run session, for the cross-session ordering case.</summary>
    private static EntityIdAllocator NewOtherSessionAllocator()
    {
        return new EntityIdAllocator(
            OtherRunSession,
            MiningSiteManifestCount,
            StaticWorldObjectManifestCount);
    }

    /// <summary>
    /// A deliberately broken store: it resolves by storage index and, optionally, forgets to
    /// count a failed resolution.
    /// </summary>
    /// <remarks>
    /// Valid code that behaves incorrectly, which is what a negative control needs. It never
    /// runs as production behaviour and nothing depends on it.
    /// </remarks>
    private sealed class GenerationIgnoringStore
    {
        private readonly bool _honourGenerations;
        private readonly bool _countStaleReferences;
        private readonly Dictionary<int, EntityId> _bySlot = new();

        internal GenerationIgnoringStore()
            : this(honourGenerations: false, countStaleReferences: true)
        {
        }

        internal GenerationIgnoringStore(bool honourGenerations, bool countStaleReferences)
        {
            _honourGenerations = honourGenerations;
            _countStaleReferences = countStaleReferences;
        }

        /// <summary>How many failed resolutions this stub admits to.</summary>
        internal long StaleReferenceResolutions { get; private set; }

        /// <summary>Makes an identity the live occupant of its slot.</summary>
        internal void Place(EntityId id)
        {
            _bySlot[id.Index] = id;
        }

        /// <summary>Resolves an identity, ignoring the generation unless configured not to.</summary>
        internal bool Resolve(EntityId id)
        {
            if (!_bySlot.TryGetValue(id.Index, out EntityId occupant))
            {
                Count();
                return false;
            }

            if (_honourGenerations && occupant != id)
            {
                Count();
                return false;
            }

            return true;
        }

        private void Count()
        {
            if (_countStaleReferences)
            {
                StaleReferenceResolutions++;
            }
        }
    }
}
