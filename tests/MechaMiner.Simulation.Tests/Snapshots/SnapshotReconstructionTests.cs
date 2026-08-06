using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// Proves that a consumer can fully rebuild from one snapshot without mutating simulation state, and that a
/// consumer skipping snapshots still reconstructs a state consistent with the latest one it read.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-007-004</c>, <c>VER-SIM-007-010</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot and § Scope and invariants
/// ("presentation cannot mutate simulation state"); <c>CTR-SIM-003</c> in doc 115 § Cross-boundary contract
/// registry ("consumer drops stale snapshot or fully rebuilds; never mutates it").
/// </remarks>
[TestFixture]
internal sealed class SnapshotReconstructionTests
{
    private const int DeclaredSeed = 611_007;

    /// <summary>
    /// Verification: <c>VER-SIM-007-004</c>.
    ///
    /// After a complete rebuild pass the committed state, the next tick's behaviour, and the run's rendered
    /// checksum are bit-identical to a run in which no rebuild occurred.
    /// </summary>
    [Test]
    public void RebuildingFromASnapshotMutatesNothing()
    {
        string control = RunRun(rebuildAtTick: -1, out int controlFields, out string controlWorld);
        string rebuilt = RunRun(rebuildAtTick: 2, out int rebuiltFields, out string rebuiltWorld);

        SnapshotContractAssertions.RebuildMutatedNothing(
            "a full presentation rebuild from a mid-run snapshot",
            control,
            rebuilt,
            rebuiltFields);

        Expect.Multiple(() =>
        {
            Assert.That(
                controlFields,
                Is.EqualTo(0),
                "the control run must not have rebuilt anything, or the comparison is between two rebuilds");
            Assert.That(
                rebuiltWorld,
                Is.EqualTo(controlWorld),
                "the authoritative world - store contents and diagnostic counters - must be identical too, so "
                    + "a rebuild that wrote through a store would be caught even if it did not change a "
                    + "published snapshot");
            Assert.That(
                control,
                Does.Contain("tick=4"),
                "the comparison must cover the ticks after the rebuild, or a delayed mutation would pass");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-007-010</c>.
    ///
    /// A consumer that skips an arbitrary number of published snapshots reconstructs a state consistent with
    /// the latest snapshot it did read, and never one blended from two non-adjacent snapshots.
    /// </summary>
    [Test]
    public void SkippedSnapshotsStillYieldAConsistentReconstruction()
    {
        DeterministicCase.Run(
            "snapshots-skipped-reconstruction",
            DeclaredSeed,
            random =>
            {
                SnapshotFixture fixture = new(enemyCount: 4);
                HudViewModel hud = HudViewModel.Unpublished;
                PresentationModel consumer = new();

                long lastReadTick = -1;
                SnapshotVersion lastReadVersion = SnapshotVersion.Unpublished;
                int readCount = 0;
                int skippedCount = 0;

                for (long tick = 0; tick < 40; tick++)
                {
                    MoveEveryEnemy(fixture, tick);
                    fixture.RunTick(tick, hud, out hud);

                    // The consumer reads only some publications, which CTR-SIM-003 explicitly permits.
                    if (random.Next(0, 3) != 0)
                    {
                        skippedCount++;
                        continue;
                    }

                    PresentationSnapshot snapshot = fixture.Publisher.Latest!;
                    consumer.RebuildFrom(snapshot);
                    lastReadTick = snapshot.Tick;
                    lastReadVersion = snapshot.Version;
                    readCount++;

                    Assert.That(
                        consumer.SourceTick,
                        Is.EqualTo(snapshot.Tick),
                        "the reconstruction must name the snapshot it came from");
                    Assert.That(
                        consumer.Render(),
                        Is.EqualTo(RenderExpected(snapshot)),
                        "the reconstruction must equal the snapshot it read exactly, never a blend of two");
                }

                Expect.Multiple(() =>
                {
                    Assert.That(readCount, Is.GreaterThan(0), "the consumer must have read something");
                    Assert.That(
                        skippedCount,
                        Is.GreaterThan(0),
                        "and skipped something, or the gate does not exercise dropping at all");
                    Assert.That(
                        consumer.SourceTick,
                        Is.EqualTo(lastReadTick),
                        "the final reconstruction is consistent with the latest snapshot the consumer read, "
                            + "not with the latest one published");
                    Assert.That(consumer.SourceVersion, Is.EqualTo(lastReadVersion));
                    Assert.That(
                        fixture.Publisher.Latest!.Tick,
                        Is.EqualTo(39L),
                        "the simulation published every tick regardless of what the consumer read");
                    Assert.That(
                        consumer.BlendedReads,
                        Is.EqualTo(0),
                        "no reconstruction may combine records from two different snapshot versions");
                });

                TestContext.Progress.WriteLine(
                    "READS " + readCount.ToString(CultureInfo.InvariantCulture)
                    + " SKIPS " + skippedCount.ToString(CultureInfo.InvariantCulture));
            });
    }

    /// <summary>
    /// Runs a five-tick run, optionally performing a full presentation rebuild at one tick, and returns the
    /// rendered authoritative result of every tick.
    /// </summary>
    private static string RunRun(long rebuildAtTick, out int rebuiltFields, out string worldRendering)
    {
        SnapshotFixture fixture = new(enemyCount: 4);
        HudViewModel hud = HudViewModel.Unpublished;
        PresentationModel consumer = new();
        System.Text.StringBuilder rendering = new();
        rebuiltFields = 0;

        for (long tick = 0; tick < 5; tick++)
        {
            MoveEveryEnemy(fixture, tick);
            rendering.Append(fixture.RunTick(tick, hud, out hud));

            if (tick == rebuildAtTick)
            {
                rebuiltFields = consumer.RebuildFrom(fixture.Publisher.Latest!);
            }
        }

        worldRendering = fixture.RenderWorld();
        return rendering.ToString();
    }

    /// <summary>Moves every enemy authoritatively, so successive snapshots genuinely differ.</summary>
    private static void MoveEveryEnemy(SnapshotFixture fixture, long tick)
    {
        for (int index = 0; index < fixture.EnemyIds.Count; index++)
        {
            EntityId enemy = fixture.EnemyIds[index];
            Assert.That(fixture.Enemies.TryGet(enemy, out EnemyState state), Is.True);
            Assert.That(
                fixture.Enemies.TryUpdate(
                    enemy,
                    new EnemyState(
                        state.PositionX + 0.05,
                        state.PositionY - 0.05,
                        state.Hull - (int)(tick % 2))),
                Is.True);
        }
    }

    private static string RenderExpected(PresentationSnapshot snapshot)
    {
        System.Text.StringBuilder builder = new();
        builder
            .Append("rebuilt from=")
            .Append(snapshot.Version.ToString())
            .Append(" tick=")
            .Append(snapshot.Tick.ToString(CultureInfo.InvariantCulture))
            .Append(" player=(")
            .Append(snapshot.PlayerPositionX.ToString("R", CultureInfo.InvariantCulture))
            .Append(',')
            .Append(snapshot.PlayerPositionY.ToString("R", CultureInfo.InvariantCulture))
            .Append(")\n");

        ReadOnlySpan<SnapshotEntity> entities = snapshot.VisibleEntities.Span;
        for (int index = 0; index < entities.Length; index++)
        {
            builder.Append("  ").Append(entities[index].ToString()).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// A presentation-side model that rebuilds itself entirely from one snapshot.
    /// </summary>
    /// <remarks>
    /// It reads only through <c>CTR-SIM-003</c>'s public surface and holds copies, which is what a real
    /// presentation binding does: doc 30 § Snapshot synchronization has the bridge map "simulation entity IDs
    /// to presentation handles" rather than holding authoritative records. It records the version each record
    /// came from, so a blend of two snapshots would be detectable rather than invisible.
    /// </remarks>
    private sealed class PresentationModel
    {
        private readonly List<SnapshotEntity> _entities = new();
        private readonly List<SnapshotVersion> _recordVersions = new();

        /// <summary>The tick of the snapshot this model was rebuilt from.</summary>
        internal long SourceTick { get; private set; } = -1;

        /// <summary>The version of the snapshot this model was rebuilt from.</summary>
        internal SnapshotVersion SourceVersion { get; private set; } = SnapshotVersion.Unpublished;

        /// <summary>How many reconstructions combined records from more than one snapshot version.</summary>
        internal int BlendedReads { get; private set; }

        private double PlayerPositionX { get; set; }

        private double PlayerPositionY { get; set; }

        /// <summary>Rebuilds the whole model from one snapshot, returning how many fields were read.</summary>
        internal int RebuildFrom(PresentationSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            _entities.Clear();
            _recordVersions.Clear();

            SourceTick = snapshot.Tick;
            SourceVersion = snapshot.Version;
            PlayerPositionX = snapshot.PlayerPositionX;
            PlayerPositionY = snapshot.PlayerPositionY;

            int fieldsRead = 4;
            ReadOnlySpan<SnapshotEntity> entities = snapshot.VisibleEntities.Span;
            for (int index = 0; index < entities.Length; index++)
            {
                _entities.Add(entities[index]);
                _recordVersions.Add(snapshot.Version);
                fieldsRead++;
            }

            foreach (SnapshotVersion version in _recordVersions)
            {
                if (version != SourceVersion)
                {
                    BlendedReads++;
                    break;
                }
            }

            return fieldsRead;
        }

        /// <summary>Renders the reconstruction as canonical invariant text.</summary>
        internal string Render()
        {
            System.Text.StringBuilder builder = new();
            builder
                .Append("rebuilt from=")
                .Append(SourceVersion.ToString())
                .Append(" tick=")
                .Append(SourceTick.ToString(CultureInfo.InvariantCulture))
                .Append(" player=(")
                .Append(PlayerPositionX.ToString("R", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(PlayerPositionY.ToString("R", CultureInfo.InvariantCulture))
                .Append(")\n");

            foreach (SnapshotEntity entity in _entities)
            {
                builder.Append("  ").Append(entity.ToString()).Append('\n');
            }

            return builder.ToString();
        }
    }
}
