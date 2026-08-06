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
        AssertTheRealStorePassesBothGates();
    }

    /// <summary>
    /// A stub that resolves by storage index alone hands back the live record that now occupies
    /// a recycled slot, which is precisely the aliasing generations exist to prevent.
    /// </summary>
    private static void AssertGenerationIgnoringStoreFailsTheStaleReferenceGate()
    {
        GenerationIgnoringStore broken = new();
        EntityId stale = EntityId.Create(RunSession, index: 4, generation: 1);
        EntityId live = EntityId.Create(RunSession, index: 4, generation: 2);
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
        EntityId stale = EntityId.Create(RunSession, index: 4, generation: 1);
        EntityId live = EntityId.Create(RunSession, index: 4, generation: 2);
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

        string insertionRendering = StoreContractAssertions.RenderOrder(insertionOrder, PriorityKeyOf);
        string correctRendering = StoreContractAssertions.RenderOrder(correctOrder, PriorityKeyOf);

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

    private static EntityIdAllocator NewAllocator()
    {
        return new EntityIdAllocator(
            RunSession,
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
