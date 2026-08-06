using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// Proves the allocation half of the identity contract: generations advance on reuse, the
/// player's slot is reserved, and an exhausted generation retires its slot instead of
/// wrapping.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-003-001</c>, <c>VER-SIM-003-004</c>, <c>VER-SIM-003-006</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Entity identity and § Scope and invariants.
/// </remarks>
[TestFixture]
internal sealed class EntityIdAllocatorTests
{
    private const ulong RunSession = 0x4A11_0001UL;
    private const ulong SecondRunSession = 0x4A11_0002UL;
    private const int MiningSiteManifestCount = 4;
    private const int StaticWorldObjectManifestCount = 4;

    /// <summary>
    /// Verification: <c>VER-SIM-003-001</c>.
    ///
    /// Freeing an identity and allocating again returns the same storage index with a
    /// different generation, so the freed identity is never equal to the new one.
    /// </summary>
    [Test]
    public void SlotReuseIncrementsGeneration()
    {
        EntityIdAllocator allocator = NewAllocator(RunSession);

        Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId first), Is.True);
        Assert.That(allocator.TryFree(first), Is.True);
        Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId second), Is.True);
        Assert.That(allocator.TryFree(second), Is.True);
        Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId third), Is.True);

        Expect.Multiple(() =>
        {
            Assert.That(
                second.Index,
                Is.EqualTo(first.Index),
                "the storage index must be reused; that is what makes it reusable storage");
            Assert.That(third.Index, Is.EqualTo(first.Index));
            Assert.That(
                second.Generation,
                Is.EqualTo(first.Generation + 1),
                "reuse must increment the generation by exactly one");
            Assert.That(third.Generation, Is.EqualTo(first.Generation + 2));
            Assert.That(
                second,
                Is.Not.EqualTo(first),
                "the freed identity must never equal the identity that replaced it");
            Assert.That(third, Is.Not.EqualTo(first));
            Assert.That(third, Is.Not.EqualTo(second));
            Assert.That(
                allocator.IsLive(first),
                Is.False,
                "the stale identity must not be live");
            Assert.That(allocator.IsLive(second), Is.False);
            Assert.That(allocator.IsLive(third), Is.True);
            Assert.That(
                allocator.DiagnosticsFor(PopulationCategory.Pickup).ReuseCount,
                Is.EqualTo(2L),
                "two of the three allocations reused a freed slot");
            Assert.That(
                first.Generation,
                Is.EqualTo(EntityId.FirstGeneration),
                "a fresh slot starts at the first generation, never at zero");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-003-004</c>.
    ///
    /// The reserved player identity is the same slot and generation in every run, ordinary
    /// allocation never returns it, and its slot is never recycled while the run is active.
    /// </summary>
    /// <remarks>
    /// The registry summary's "it is the same value in every run" is asserted as the
    /// stability of the reserved <em>slot</em>: index and generation are identical across
    /// runs, and only the run fence differs. Both facts are required at once - doc 20 §
    /// Entity identity says the player ID is stable <em>and</em> that "IDs are unique only
    /// within one run session", and <c>VER-SIM-003-003</c> requires the fence to travel
    /// inside the identity, so a fenced value cannot also be byte-identical across runs.
    /// </remarks>
    [Test]
    public void ThePlayerIdIsReservedStableAndNeverRecycled()
    {
        EntityIdAllocator firstRun = NewAllocator(RunSession);
        EntityIdAllocator secondRun = NewAllocator(SecondRunSession);

        EntityId firstPlayer = firstRun.PlayerId;
        EntityId secondPlayer = secondRun.PlayerId;

        Expect.Multiple(() =>
        {
            Assert.That(
                secondPlayer.Index,
                Is.EqualTo(firstPlayer.Index),
                "the reserved slot is the same in every run");
            Assert.That(
                secondPlayer.Generation,
                Is.EqualTo(firstPlayer.Generation),
                "the reserved generation is the same in every run");
            Assert.That(
                firstPlayer.Index,
                Is.EqualTo(EntityId.ReservedPlayerIndex),
                "the reserved slot is the documented constant, not whatever was allocated first");
            Assert.That(firstPlayer.Generation, Is.EqualTo(EntityId.FirstGeneration));
            Assert.That(firstPlayer.IsReservedPlayer, Is.True);
            Assert.That(
                firstPlayer.RunSession,
                Is.Not.EqualTo(secondPlayer.RunSession),
                "only the run fence differs, and it must, or a leaked player reference would "
                    + "resolve in the wrong run");
            Assert.That(firstRun.IsLive(firstPlayer), Is.True, "the player exists from the start of the run");
            Assert.That(
                firstRun.LiveCount(PopulationCategory.Player),
                Is.EqualTo(1),
                "doc 20 § Scope and invariants: exactly one player entity");
        });

        // Ordinary allocation never returns the reserved identity: the Player partition is
        // full because the player occupies it, and every other category has a different
        // slot range.
        Expect.Multiple(() =>
        {
            Assert.That(
                firstRun.TryAllocate(PopulationCategory.Player, out EntityId extraPlayer),
                Is.False,
                "a second player must not be allocatable");
            Assert.That(extraPlayer.IsUnset, Is.True, "a refused allocation yields something that fails closed");
        });

        for (int attempt = 0; attempt < 32; attempt++)
        {
            PopulationCategory category = StoreCapacities.Categories[1 + (attempt % 11)];
            if (!firstRun.TryAllocate(category, out EntityId issued))
            {
                continue;
            }

            Assert.That(
                issued,
                Is.Not.EqualTo(firstPlayer),
                "no ordinary allocation may return the reserved player identity");
            Assert.That(
                issued.Index,
                Is.Not.EqualTo(EntityId.ReservedPlayerIndex),
                "no ordinary allocation may land on the reserved slot");
            Assert.That(firstRun.TryFree(issued), Is.True);
        }

        // The slot cannot re-enter the free list, so it can never be handed out again.
        Expect.Multiple(() =>
        {
            Assert.That(
                firstRun.TryFree(firstPlayer),
                Is.False,
                "freeing the reserved player must be refused, not silently accepted");
            Assert.That(firstRun.IsLive(firstPlayer), Is.True, "and the player must still be live afterwards");
            Assert.That(firstRun.LiveCount(PopulationCategory.Player), Is.EqualTo(1));
            Assert.That(
                firstRun.TryAllocate(PopulationCategory.Player, out EntityId _),
                Is.False,
                "the reserved slot never becomes allocatable");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-003-006</c>.
    ///
    /// When a slot's generation counter reaches its representable maximum the slot is retired
    /// rather than wrapped, so a still-held reference can never match a live entity again.
    /// </summary>
    /// <remarks>
    /// The ceiling is supplied to the allocator rather than hard-coded at
    /// <see cref="uint.MaxValue"/>, because at that ceiling the retirement branch is
    /// reachable only after 4.29 billion recycles of one slot and would be an untested path.
    /// The behaviour asserted is the production behaviour; only the ceiling is small.
    /// </remarks>
    [Test]
    public void GenerationExhaustionRetiresTheSlotRatherThanAliasing()
    {
        const uint maximumGeneration = 3;
        EntityIdAllocator allocator = new(
            RunSession,
            miningSiteManifestCount: 1,
            staticWorldObjectManifestCount: 1,
            maximumGeneration: maximumGeneration);

        EntityDiagnostics diagnostics = allocator.DiagnosticsFor(PopulationCategory.MiningSite);
        EntityId firstEverIssued = EntityId.Unset;
        EntityId lastIssued = EntityId.Unset;

        for (uint generation = EntityId.FirstGeneration; generation <= maximumGeneration; generation++)
        {
            Assert.That(
                allocator.TryAllocate(PopulationCategory.MiningSite, out EntityId issued),
                Is.True,
                "the single mining-site slot must still be issuable at generation "
                    + generation.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.That(issued.Generation, Is.EqualTo(generation));

            if (generation == EntityId.FirstGeneration)
            {
                firstEverIssued = issued;
            }

            lastIssued = issued;
            Assert.That(allocator.TryFree(issued), Is.True);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                allocator.IsRetired(lastIssued),
                Is.True,
                "the slot must be retired once its generation is exhausted");
            Assert.That(
                diagnostics.RetiredSlotCount,
                Is.EqualTo(1),
                "retirement must be a counted diagnostic, not a silent state");
            Assert.That(
                allocator.TryAllocate(PopulationCategory.MiningSite, out EntityId afterRetirement),
                Is.False,
                "a retired slot must not be re-issued; the partition is now exhausted");
            Assert.That(afterRetirement.IsUnset, Is.True);
            Assert.That(
                diagnostics.RejectedRequests,
                Is.EqualTo(1L),
                "the refused allocation must be counted rather than served by wrapping");
            Assert.That(
                allocator.IsLive(firstEverIssued),
                Is.False,
                "the generation-one reference held since the beginning must never become live "
                    + "again; wrapping to generation one is exactly the aliasing this prevents");
            Assert.That(allocator.IsLive(lastIssued), Is.False);
            Assert.That(allocator.LiveCount(PopulationCategory.MiningSite), Is.EqualTo(0));
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
