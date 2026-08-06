using System;
using System.Globalization;
using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// Proves that every store declares a soft target, a hard capacity derived from its
/// documented margin, and an overflow behaviour, and that the diagnostic counters reconcile
/// with the operations actually performed.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-003-008</c>, <c>VER-SIM-003-009</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Capacity and overload behavior;
/// <c>docs/technical/23-encounter-director-and-enemy-runtime.md</c> § Population classes;
/// <c>docs/technical/22-combat-and-weapon-runtime.md</c> § Performance and capacity;
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Frame metrics.
/// </remarks>
[TestFixture]
internal sealed class StoreCapacityTests
{
    private const ulong RunSession = 0xC0DE_0001UL;
    private const int MiningSiteManifestCount = 63;
    private const int StaticWorldObjectManifestCount = 40;
    private const int DiagnosticsSeed = 20260806;

    /// <summary>
    /// Verification: <c>VER-SIM-003-008</c>.
    ///
    /// Each store's hard capacity is its soft target plus its documented margin; at the soft
    /// target it reports pressure; at hard capacity an authored enemy queues rather than being
    /// cancelled or converted, and a store whose breach is an invariant failure says so
    /// instead of dropping a record.
    /// </summary>
    [Test]
    public void SoftTargetHardCapacityAndOverflowBehaviourAreEnforcedPerStore()
    {
        // The derivation is the only way to obtain the hard capacity, for every row.
        Expect.Multiple(() =>
        {
            foreach (PopulationCategory category in StoreCapacities.Categories)
            {
                StoreCapacity capacity = StoreCapacities.For(
                    category,
                    MiningSiteManifestCount,
                    StaticWorldObjectManifestCount);

                Assert.That(
                    capacity.HardCapacity,
                    Is.EqualTo(capacity.SoftTarget + capacity.Margin),
                    category.ToString() + ": the hard capacity must be the soft target plus "
                        + "the documented margin, never a hand-edited number");
                Assert.That(
                    capacity.Derivation,
                    Is.Not.Empty,
                    category.ToString() + ": every capacity must cite its derivation");
                Assert.That(
                    capacity.Overflow,
                    Is.Not.EqualTo(OverflowBehaviour.DegradePresentation),
                    category.ToString() + ": no authoritative population may degrade; doc 20 "
                        + "§ Capacity and overload behavior confines degradation to visual-only pools");

                if (capacity.Margin > 0)
                {
                    Assert.That(
                        capacity.MarginBasis,
                        Is.Not.Empty,
                        category.ToString() + ": a nonzero margin must name the single authored "
                            + "quantity it equals");
                }
            }
        });

        // The documented numbers themselves, so a silent edit is a red test rather than a
        // reviewer's memory.
        Expect.Multiple(() =>
        {
            AssertRow(PopulationCategory.Player, 1, 0, OverflowBehaviour.FailInvariant, CapacityAuthority.SimulationInvariant);
            AssertRow(PopulationCategory.OrdinaryEnemy, 700, 30, OverflowBehaviour.QueueAuthored, CapacityAuthority.EncounterSchedule);
            AssertRow(PopulationCategory.Elite, 13, 2, OverflowBehaviour.QueueAuthored, CapacityAuthority.EncounterSchedule);
            AssertRow(PopulationCategory.Boss, 4, 0, OverflowBehaviour.QueueAuthored, CapacityAuthority.EncounterSchedule);
            AssertRow(PopulationCategory.EnemyProjectile, 512, 0, OverflowBehaviour.FailInvariant, CapacityAuthority.CombatRuntimeCeiling);
            AssertRow(PopulationCategory.WeaponActor, 2048, 0, OverflowBehaviour.FailInvariant, CapacityAuthority.CombatRuntimeCeiling);
            AssertRow(PopulationCategory.DamageZone, 512, 0, OverflowBehaviour.FailInvariant, CapacityAuthority.CombatRuntimeCeiling);
            AssertRow(PopulationCategory.Pickup, 75, 12, OverflowBehaviour.FailInvariant, CapacityAuthority.DerivedFromGameplayRates);
            AssertRow(PopulationCategory.DestructibleRock, 16, 0, OverflowBehaviour.FailInvariant, CapacityAuthority.EncounterSchedule);
            AssertRow(PopulationCategory.RelicCache, 3, 0, OverflowBehaviour.FailInvariant, CapacityAuthority.EncounterSchedule);
            AssertRow(PopulationCategory.MiningSite, MiningSiteManifestCount, 0, OverflowBehaviour.FailInvariant, CapacityAuthority.MapManifest);
            AssertRow(PopulationCategory.StaticWorldObject, StaticWorldObjectManifestCount, 0, OverflowBehaviour.FailInvariant, CapacityAuthority.MapManifest);
        });

        AssertDocTwentyTwoSourcedRows();
        AssertWeaklySourcedRows();
        AssertAuthoredEnemyQueuesAtHardCapacity();
        AssertInvariantStoreRefusesRatherThanDropping();
    }

    /// <summary>
    /// Verification: <c>VER-SIM-003-009</c>.
    ///
    /// Capacity, high-water mark, queue depth, reuse count, rejected requests, and failed
    /// resolutions are observable after randomized churn, reconcile with the operations
    /// performed, and are not reset by a read.
    /// </summary>
    [Test]
    public void CapacityDiagnosticsReconcileWithTheOperationsPerformed()
    {
        DeterministicCase.Run(
            "entities-capacity-diagnostics",
            DiagnosticsSeed,
            random =>
            {
                EntityIdAllocator allocator = NewAllocator();

                // An authored-enemy store, so the queue-depth and rejected-request counters
                // are reachable without the store failing the tick invariant instead.
                PackedEntityStore<long> store = new(PopulationCategory.Elite, allocator);
                EntityDiagnostics diagnostics = store.Diagnostics;
                int hardCapacity = store.Capacity.HardCapacity;

                System.Collections.Generic.List<EntityId> live = new(hardCapacity);
                System.Collections.Generic.List<EntityId> freed = new(64);

                long expectedAdmissions = 0;
                long expectedRejections = 0;
                long expectedStaleResolutions = 0;
                int expectedHighWater = 0;

                // Phase one: randomized churn below the ceiling, so reuse, high-water, and
                // failed-resolution counters accumulate against a population that moves.
                for (int operation = 0; operation < 400; operation++)
                {
                    switch (random.Next(0, 3))
                    {
                        case 0:
                            if (live.Count < hardCapacity)
                            {
                                Assert.That(store.TryAdmit(operation, operation, out EntityId admitted), Is.True);
                                live.Add(admitted);
                                expectedAdmissions++;
                                expectedHighWater = Math.Max(expectedHighWater, live.Count);
                            }

                            break;

                        case 1:
                            if (live.Count > 0)
                            {
                                int victim = random.Next(0, live.Count);
                                Assert.That(store.TryRemove(live[victim]), Is.True);
                                freed.Add(live[victim]);
                                live.RemoveAt(victim);
                            }

                            break;

                        default:
                            if (freed.Count > 0)
                            {
                                EntityId stale = freed[random.Next(0, freed.Count)];
                                Assert.That(
                                    store.TryGet(stale, out long _),
                                    Is.False,
                                    "a freed identity must never resolve");
                                expectedStaleResolutions++;
                            }

                            break;
                    }
                }

                // Phase two: fill to the ceiling.
                while (live.Count < hardCapacity)
                {
                    Assert.That(store.TryAdmit(live.Count, live.Count, out EntityId filled), Is.True);
                    live.Add(filled);
                    expectedAdmissions++;
                    expectedHighWater = Math.Max(expectedHighWater, live.Count);
                }

                // Phase three: three admissions at the ceiling, which are refused and queued
                // rather than cancelled.
                const int overCapacityAdmissions = 3;
                for (int attempt = 0; attempt < overCapacityAdmissions; attempt++)
                {
                    Assert.That(
                        store.TryAdmit(10_000 + attempt, 10_000 + attempt, out EntityId _),
                        Is.False,
                        "the store is at its ceiling, so the record queues instead of entering");
                    expectedRejections++;
                }

                long firstRead = diagnostics.ReuseCount;
                long secondRead = diagnostics.ReuseCount;

                Expect.Multiple(() =>
                {
                    Assert.That(
                        diagnostics.Capacity.HardCapacity,
                        Is.EqualTo(hardCapacity),
                        "capacity is itself one of the diagnostics doc 20 enumerates");
                    Assert.That(
                        diagnostics.LiveCount,
                        Is.EqualTo(live.Count),
                        "the live count must equal the identities the test believes are live");
                    Assert.That(
                        store.Count,
                        Is.EqualTo(live.Count),
                        "the store's residency must agree with the allocator's ledger");
                    Assert.That(
                        diagnostics.HighWaterMark,
                        Is.EqualTo(expectedHighWater),
                        "the high-water mark must equal the largest population reached");
                    Assert.That(
                        diagnostics.HighWaterMark,
                        Is.LessThanOrEqualTo(hardCapacity),
                        "and can never exceed the hard capacity");
                    Assert.That(
                        diagnostics.RejectedRequests,
                        Is.EqualTo(expectedRejections),
                        "every refused admission must be counted exactly once");
                    Assert.That(
                        diagnostics.StaleReferenceResolutions,
                        Is.EqualTo(expectedStaleResolutions),
                        "every failed resolution must be counted exactly once");
                    Assert.That(
                        diagnostics.ReuseCount,
                        Is.EqualTo(expectedAdmissions - Math.Min(expectedAdmissions, hardCapacity)),
                        "every admission beyond the first pass over the partition reused a slot");
                    Assert.That(
                        diagnostics.QueueDepth,
                        Is.EqualTo(overCapacityAdmissions),
                        "every refused authored admission must be queued, not cancelled");
                    Assert.That(
                        diagnostics.RetiredSlotCount,
                        Is.EqualTo(0),
                        "no slot can retire at the production generation ceiling");
                    Assert.That(
                        secondRead,
                        Is.EqualTo(firstRead),
                        "reading a counter must not reset it");
                    Assert.That(
                        expectedStaleResolutions,
                        Is.GreaterThan(0L),
                        "the churn must have exercised at least one failed resolution, or the "
                            + "counter proves nothing");
                    Assert.That(
                        diagnostics.ReuseCount,
                        Is.GreaterThan(0L),
                        "and at least one slot must have been recycled");
                });

                TestContext.Progress.WriteLine("DIAGNOSTICS " + diagnostics.Render());
            });
    }

    private static void AssertRow(
        PopulationCategory category,
        int expectedSoftTarget,
        int expectedMargin,
        OverflowBehaviour expectedOverflow,
        CapacityAuthority expectedAuthority)
    {
        StoreCapacity capacity = StoreCapacities.For(
            category,
            MiningSiteManifestCount,
            StaticWorldObjectManifestCount);

        Assert.That(capacity.SoftTarget, Is.EqualTo(expectedSoftTarget), category.ToString() + " soft target");
        Assert.That(capacity.Margin, Is.EqualTo(expectedMargin), category.ToString() + " margin");
        Assert.That(
            capacity.HardCapacity,
            Is.EqualTo(expectedSoftTarget + expectedMargin),
            category.ToString() + " hard capacity");
        Assert.That(capacity.Overflow, Is.EqualTo(expectedOverflow), category.ToString() + " overflow behaviour");
        Assert.That(capacity.Authority, Is.EqualTo(expectedAuthority), category.ToString() + " capacity authority");
    }

    /// <summary>
    /// Pins exactly which rows are doc 22's, so a revision of doc 22 § Performance and
    /// capacity has a checklist rather than a memory.
    /// </summary>
    /// <remarks>
    /// This assertion exists because a stale figure in one of these rows is otherwise
    /// catchable only by accident: nothing else in the codebase records that these three
    /// numbers are re-read rather than re-derived.
    /// </remarks>
    private static void AssertDocTwentyTwoSourcedRows()
    {
        System.Collections.Generic.List<PopulationCategory> docTwentyTwoRows = new(4);
        foreach (PopulationCategory category in StoreCapacities.Categories)
        {
            StoreCapacity capacity = StoreCapacities.For(
                category,
                MiningSiteManifestCount,
                StaticWorldObjectManifestCount);
            if (capacity.Authority == CapacityAuthority.CombatRuntimeCeiling)
            {
                docTwentyTwoRows.Add(category);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                docTwentyTwoRows,
                Is.EqualTo(new[]
                {
                    PopulationCategory.EnemyProjectile,
                    PopulationCategory.WeaponActor,
                    PopulationCategory.DamageZone,
                }),
                "exactly these three rows are taken verbatim from doc 22 § Performance and "
                    + "capacity and must be re-read whenever that section changes; the other "
                    + "nine do not depend on doc 22 at all");
            Assert.That(
                StoreCapacities.EnemyProjectile.Derivation,
                Does.Contain("512 enemy projectiles"),
                "the row must quote the figure it took, so a doc 22 diff is a text match");
            Assert.That(
                StoreCapacities.WeaponActor.Derivation,
                Does.Contain("2,048 player weapon projectiles/actors combined"));
            Assert.That(
                StoreCapacities.DamageZone.Derivation,
                Does.Contain("512 persistent damage zones/trail segments"));
        });
    }

    /// <summary>
    /// Pins exactly which rows rest on an assumption rather than a stated figure, and that
    /// each names its missing input.
    /// </summary>
    private static void AssertWeaklySourcedRows()
    {
        System.Collections.Generic.List<PopulationCategory> weakRows = new(4);
        foreach (PopulationCategory category in StoreCapacities.Categories)
        {
            StoreCapacity capacity = StoreCapacities.For(
                category,
                MiningSiteManifestCount,
                StaticWorldObjectManifestCount);
            if (capacity.IsWeaklySourced)
            {
                weakRows.Add(category);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                weakRows,
                Is.EqualTo(new[] { PopulationCategory.Pickup, PopulationCategory.StaticWorldObject }),
                "exactly two rows are weakly sourced");
            Assert.That(
                StoreCapacities.Pickup.WeakSourceReason,
                Does.Contain("Boss death and physical loot"),
                "the pickup row must name the document that leaves the pickup-entity count open");
            Assert.That(
                StoreCapacities.StaticWorldObjects(StaticWorldObjectManifestCount).WeakSourceReason,
                Does.Contain("no document states a total"),
                "the static-world-object row must say that no total exists");
            Assert.That(
                StoreCapacities.MiningSites(MiningSiteManifestCount).IsWeaklySourced,
                Is.False,
                "the mining-site row is manifest-sized and bounded, so it is not weakly sourced");
        });
    }

    /// <summary>
    /// At the soft target the store reports pressure; at hard capacity an authored enemy
    /// queues and later enters, and nothing resident is evicted to make room.
    /// </summary>
    private static void AssertAuthoredEnemyQueuesAtHardCapacity()
    {
        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.Elite, allocator);
        StoreCapacity capacity = store.Capacity;

        EntityId[] resident = new EntityId[capacity.HardCapacity];
        for (int index = 0; index < capacity.HardCapacity; index++)
        {
            bool pressureBefore = store.IsUnderPressure;
            Assert.That(store.TryAdmit(index, index, out resident[index]), Is.True);
            Assert.That(
                pressureBefore,
                Is.EqualTo(index >= capacity.SoftTarget),
                "pressure must be reported from the soft target onwards and not before");
        }

        Assert.That(store.Count, Is.EqualTo(capacity.HardCapacity));
        Assert.That(store.IsUnderPressure, Is.True);

        bool admittedAtCapacity = store.TryAdmit(999L, 999L, out EntityId queuedId);

        Expect.Multiple(() =>
        {
            Assert.That(admittedAtCapacity, Is.False, "the store is full, so the record does not enter now");
            Assert.That(queuedId.IsUnset, Is.True, "and no identity is issued for a record that is not resident");
            Assert.That(
                store.QueueDepth,
                Is.EqualTo(1),
                "doc 20 § Capacity and overload behavior: an authored enemy at the ceiling "
                    + "queues; it is not silently canceled or converted");
            Assert.That(store.Diagnostics.QueueDepth, Is.EqualTo(1), "and the queue depth is a diagnostic");
            Assert.That(
                store.Count,
                Is.EqualTo(capacity.HardCapacity),
                "no resident record may be dropped to make room for another");
            for (int index = 0; index < resident.Length; index++)
            {
                Assert.That(
                    store.TryGet(resident[index], out long _),
                    Is.True,
                    "every record resident before the refused admission must still be resident");
            }
        });

        // The queued record later enters, unchanged and unconverted.
        Assert.That(store.TryRemove(resident[0]), Is.True);
        int admitted = store.AdmitQueued();

        Expect.Multiple(() =>
        {
            Assert.That(admitted, Is.EqualTo(1), "the queued record must enter once capacity exists");
            Assert.That(store.QueueDepth, Is.EqualTo(0));
            Assert.That(store.Count, Is.EqualTo(capacity.HardCapacity));
        });
    }

    /// <summary>
    /// A store whose declared behaviour is a failed invariant says so at hard capacity rather
    /// than dropping the record or evicting a resident one.
    /// </summary>
    private static void AssertInvariantStoreRefusesRatherThanDropping()
    {
        EntityIdAllocator allocator = NewAllocator();
        PackedEntityStore<long> store = new(PopulationCategory.RelicCache, allocator);
        StoreCapacity capacity = store.Capacity;
        Assert.That(capacity.Overflow, Is.EqualTo(OverflowBehaviour.FailInvariant));

        EntityId[] resident = new EntityId[capacity.HardCapacity];
        for (int index = 0; index < capacity.HardCapacity; index++)
        {
            Assert.That(store.TryAdmit(index, index, out resident[index]), Is.True);
        }

        InvalidOperationException breach = Expect.Throws<InvalidOperationException>(
            () => store.TryAdmit(999L, 999L, out EntityId _));

        Expect.Multiple(() =>
        {
            Assert.That(
                breach.Message,
                Does.Contain("hard authoritative capacity breach"),
                "the failure must name what it is, so a stress run's log is diagnosable");
            Assert.That(
                breach.Message,
                Does.Contain(capacity.HardCapacity.ToString(CultureInfo.InvariantCulture)),
                "and must name the capacity it breached");
            Assert.That(
                store.Count,
                Is.EqualTo(capacity.HardCapacity),
                "the offending batch must still be resident when the invariant fires, which is "
                    + "what makes the breach inspectable rather than masked");
            Assert.That(store.QueueDepth, Is.EqualTo(0), "a fail-invariant store never queues");
            for (int index = 0; index < resident.Length; index++)
            {
                Assert.That(store.TryGet(resident[index], out long _), Is.True);
            }
        });
    }

    private static EntityIdAllocator NewAllocator()
    {
        return new EntityIdAllocator(
            RunSession,
            MiningSiteManifestCount,
            StaticWorldObjectManifestCount);
    }
}
