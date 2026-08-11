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

    /// <summary>
    /// Verification: supports <c>VER-SIM-003-002</c>.
    ///
    /// A genuine, live identity from a category whose partition lies <em>below</em> this store's fails
    /// closed, is counted once against the store that refused it, and leaves the record it really names
    /// intact.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the protection <see cref="EntityIdAllocator"/>'s own remarks claim for the partitioned slot
    /// space: "a cross-category reference is out of the target store's range and fails closed for free". It
    /// needs no forged identity. <see cref="EntityIdAllocator.PlayerId"/> is issued by the allocator's own
    /// constructor at storage index <see cref="EntityId.ReservedPlayerIndex"/>, and every other category's
    /// partition begins above it, so handing it to a pickup store is a real identity in the wrong store.
    /// </para>
    /// <para>
    /// It is registered as evidence because the alternative reading was that the two slot-range clauses
    /// could not be tested at all - <see cref="EntityId.Create"/> being internal - which mistook "the test
    /// assembly cannot mint an arbitrary identity" for "the test assembly cannot obtain an out-of-partition
    /// one". The allocator issues them across twelve partitions and both constructors are public.
    /// </para>
    /// </remarks>
    [Test]
    public void AnIdentityFromBelowThisStoresPartitionFailsClosed()
    {
        EntityIdAllocator allocator = NewAllocator(RunSession);
        PackedEntityStore<long> playerStore = new(PopulationCategory.Player, allocator);
        PackedEntityStore<long> pickupStore = new(PopulationCategory.Pickup, allocator);

        EntityId player = allocator.PlayerId;
        Assert.That(pickupStore.TryAdmit(0L, 9_000L, out EntityId pickup), Is.True);

        long before = pickupStore.Diagnostics.StaleReferenceResolutions;
        long playerStaleBefore = playerStore.Diagnostics.StaleReferenceResolutions;
        bool resolved = pickupStore.TryGet(player, out long _);
        long after = pickupStore.Diagnostics.StaleReferenceResolutions;

        Expect.Multiple(() =>
        {
            Assert.That(
                player.Index,
                Is.LessThan(allocator.SlotOffsetFor(PopulationCategory.Pickup)),
                "the identity must lie below the pickup partition, or the clause under test is not the one "
                    + "being reached");
            Assert.That(
                allocator.IsLive(player),
                Is.True,
                "and it must be a live identity this run really issued, not a stale or forged one");
            Assert.That(
                allocator.TryGetCategory(player, out PopulationCategory owning),
                Is.True,
                "the allocator must agree that it owns the slot");
            Assert.That(owning, Is.EqualTo(PopulationCategory.Player), "in the player partition");
            Assert.That(
                resolved,
                Is.False,
                "the pickup store must refuse an identity outside its own partition rather than indexing "
                    + "below its arrays");
            Assert.That(
                after - before,
                Is.EqualTo(1L),
                "counted exactly once, against the store that was asked");
            Assert.That(
                playerStore.Diagnostics.StaleReferenceResolutions,
                Is.EqualTo(playerStaleBefore),
                "and not against the category the identity actually belongs to");
            Assert.That(
                pickupStore.TryUpdate(player, 1L),
                Is.False,
                "every resolving entry point must fail closed, not only the read");
            Assert.That(pickupStore.TryRemove(player), Is.False);
            Assert.That(
                playerStore.TryGet(player, out long _),
                Is.True,
                "and the record the identity really names is untouched by the refusals");
            Assert.That(
                pickupStore.TryGet(pickup, out long resident),
                Is.True,
                "while the pickup store still resolves its own identity");
            Assert.That(resident, Is.EqualTo(9_000L));
        });
    }

    /// <summary>
    /// Verification: supports <c>VER-SIM-003-002</c>.
    ///
    /// A genuine, live identity from a category whose partition lies <em>above</em> this store's fails
    /// closed too, so the range refusal holds from both sides rather than only below.
    /// </summary>
    /// <remarks>
    /// The player partition is one slot wide, so any identity from any other category is above it. Both
    /// clauses are asserted separately because each is one comparison, and a test that only crossed the
    /// boundary from one side would leave the other free to be deleted.
    /// </remarks>
    [Test]
    public void AnIdentityFromAboveThisStoresPartitionFailsClosed()
    {
        EntityIdAllocator allocator = NewAllocator(RunSession);
        PackedEntityStore<long> playerStore = new(PopulationCategory.Player, allocator);
        PackedEntityStore<long> enemyStore = new(PopulationCategory.OrdinaryEnemy, allocator);

        Assert.That(enemyStore.TryAdmit(0L, 4_200L, out EntityId enemy), Is.True);

        long before = playerStore.Diagnostics.StaleReferenceResolutions;
        bool resolved = playerStore.TryGet(enemy, out long _);
        long after = playerStore.Diagnostics.StaleReferenceResolutions;

        Expect.Multiple(() =>
        {
            Assert.That(
                playerStore.Capacity.HardCapacity,
                Is.EqualTo(1),
                "the player partition is one slot wide, which is what puts every other identity above it");
            Assert.That(
                enemy.Index,
                Is.GreaterThanOrEqualTo(
                    allocator.SlotOffsetFor(PopulationCategory.Player) + playerStore.Capacity.HardCapacity),
                "so this identity is above the player partition's last slot");
            Assert.That(allocator.IsLive(enemy), Is.True, "and it is a live identity this run issued");
            Assert.That(
                resolved,
                Is.False,
                "the player store must refuse it rather than reading past the end of its own arrays");
            Assert.That(
                after - before,
                Is.EqualTo(1L),
                "counted exactly once");
            Assert.That(
                playerStore.TryGet(allocator.PlayerId, out long _),
                Is.True,
                "while the player store still resolves the one identity it does hold");
            Assert.That(
                enemyStore.TryGet(enemy, out long resident),
                Is.True,
                "and the enemy record is untouched");
            Assert.That(resident, Is.EqualTo(4_200L));
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
