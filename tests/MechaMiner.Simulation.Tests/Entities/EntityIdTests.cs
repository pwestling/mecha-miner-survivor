using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// Proves that a reference which no longer names what it used to fails closed, whether it
/// went stale through slot recycling or through belonging to another run.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-003-002</c>, <c>VER-SIM-003-003</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Entity identity: "IDs are unique only
/// within one run session" and "Invalid, expired, or generation-mismatched references fail
/// closed and produce a diagnostic counter". § Scope and invariants: "every live entity ID
/// resolves to exactly one live record of the matching generation".
/// </remarks>
[TestFixture]
internal sealed class EntityIdTests
{
    private const ulong RunSession = 0x51A0_0001UL;
    private const ulong OtherRunSession = 0x51A0_0002UL;
    private const int MiningSiteManifestCount = 6;
    private const int StaticWorldObjectManifestCount = 4;

    /// <summary>
    /// Verification: <c>VER-SIM-003-002</c>.
    ///
    /// A stale generation resolves to nothing rather than to the live record that now
    /// occupies the slot, and the diagnostic counter rises by exactly one.
    /// </summary>
    [Test]
    public void StaleGenerationFailsClosedAndCountsADiagnostic()
    {
        EntityIdAllocator allocator = NewAllocator(RunSession);
        PackedEntityStore<long> store = new(PopulationCategory.MiningSite, allocator);

        Assert.That(
            store.TryAdmit(0L, 1_000L, out EntityId original),
            Is.True,
            "the store must admit the first record");
        Assert.That(store.TryRemove(original), Is.True, "the record must be removable");
        Assert.That(
            store.TryAdmit(0L, 2_000L, out EntityId recycled),
            Is.True,
            "the freed slot must be reusable");

        StoreContractAssertions.GenerationMismatchFailsClosed(
            "the packed mining-site store",
            original,
            recycled,
            id => store.TryGet(id, out long _),
            () => store.Diagnostics.StaleReferenceResolutions);

        // The record now occupying the slot must be the new one, so "fails closed" is not
        // merely "returns false for everything".
        Assert.That(store.TryGet(recycled, out long resident), Is.True);
        Assert.That(resident, Is.EqualTo(2_000L), "the live record must be the recycled one");

        // A freed identity whose generation still matches the slot must also fail: the slot
        // is not live, and doc 20 § Scope and invariants requires resolution to a *live*
        // record.
        Assert.That(store.TryRemove(recycled), Is.True);
        long beforeFreedProbe = store.Diagnostics.StaleReferenceResolutions;
        Expect.Multiple(() =>
        {
            Assert.That(
                store.TryGet(recycled, out long _),
                Is.False,
                "a freed identity must fail closed even before its slot is reused");
            Assert.That(
                store.Diagnostics.StaleReferenceResolutions - beforeFreedProbe,
                Is.EqualTo(1L),
                "one diagnostic for the freed-identity resolution too");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-003-003</c>.
    ///
    /// An identity issued by another run's allocator never resolves, even when a live record
    /// occupies the very same slot index in this run.
    /// </summary>
    [Test]
    public void AnIdFromAnotherRunSessionNeverResolves()
    {
        EntityIdAllocator thisRun = NewAllocator(RunSession);
        EntityIdAllocator otherRun = NewAllocator(OtherRunSession);
        PackedEntityStore<long> store = new(PopulationCategory.MiningSite, thisRun);

        Assert.That(store.TryAdmit(0L, 7L, out EntityId local), Is.True);
        Assert.That(
            otherRun.TryAllocate(PopulationCategory.MiningSite, out EntityId foreign),
            Is.True,
            "the other run must issue an identity for the same category");

        long before = store.Diagnostics.StaleReferenceResolutions;
        bool foreignResolved = store.TryGet(foreign, out long _);
        long after = store.Diagnostics.StaleReferenceResolutions;

        Expect.Multiple(() =>
        {
            Assert.That(
                foreign.Index,
                Is.EqualTo(local.Index),
                "the foreign identity must collide on slot index, or nothing is being proved: "
                    + "doc 20 § Entity identity says IDs are unique only within one run session");
            Assert.That(
                foreign.Generation,
                Is.EqualTo(local.Generation),
                "and on generation too, so only the run fence distinguishes them");
            Assert.That(
                foreign,
                Is.Not.EqualTo(local),
                "the run fence must make the two identities unequal");
            Assert.That(
                foreignResolved,
                Is.False,
                "a leaked cross-run reference must not alias the live record in this run");
            Assert.That(
                after - before,
                Is.EqualTo(1L),
                "one diagnostic for the refused cross-run resolution");
            Assert.That(
                store.TryGet(local, out long _),
                Is.True,
                "this run's own identity must still resolve");
        });
    }

    /// <summary>
    /// Verification: supports <c>VER-SIM-003-003</c>.
    ///
    /// The default identity names no run and therefore can never resolve anywhere, so a
    /// defaulted field cannot masquerade as slot zero.
    /// </summary>
    [Test]
    public void TheDefaultIdentityNamesNoRunAndNeverResolves()
    {
        EntityIdAllocator allocator = NewAllocator(RunSession);
        PackedEntityStore<long> store = new(PopulationCategory.MiningSite, allocator);
        Assert.That(store.TryAdmit(0L, 1L, out EntityId _), Is.True);

        Expect.Multiple(() =>
        {
            Assert.That(EntityId.Unset.IsUnset, Is.True);
            Assert.That(EntityId.Unset.IsIssued, Is.False);
            Assert.That(default(EntityId).RunSession, Is.EqualTo(0UL));
            Assert.That(
                store.TryGet(EntityId.Unset, out long _),
                Is.False,
                "the default identity must fail closed rather than resolving to slot zero");
            Assert.That(
                EntityId.NoEntityIn(RunSession).IsNoEntity,
                Is.True,
                "'no entity in this run' must be explicit and distinguishable from unset");
            Assert.That(EntityId.NoEntityIn(RunSession).IsUnset, Is.False);
        });
    }

    private static EntityIdAllocator NewAllocator(ulong runSession)
    {
        return new EntityIdAllocator(
            runSession,
            MiningSiteManifestCount,
            StaticWorldObjectManifestCount);
    }
}
