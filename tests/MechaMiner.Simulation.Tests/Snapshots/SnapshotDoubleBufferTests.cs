using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// Proves that publishing does not mutate a held snapshot, that the two most recent complete snapshots are
/// simultaneously readable, and that a churn-free publication reuses its pages rather than allocating new
/// ones.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-007-003</c>, <c>VER-SIM-007-009</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot;
/// <c>docs/technical/30-presentation-and-rendering.md</c> § Snapshot synchronization;
/// <c>docs/technical/10-runtime-architecture.md</c> § Performance posture.
/// </remarks>
[TestFixture]
internal sealed class SnapshotDoubleBufferTests
{
    /// <summary>How many publications the identity invariant is checked across.</summary>
    private const int PublicationCount = 64;

    /// <summary>
    /// Verification: <c>VER-SIM-007-003</c>.
    ///
    /// Publishing for tick N+1 does not mutate a snapshot a consumer already holds for tick N, and both are
    /// readable at once so presentation can interpolate between them.
    /// </summary>
    [Test]
    public void PublishingDoesNotMutateAHeldSnapshot()
    {
        SnapshotFixture fixture = new(enemyCount: 3);
        HudViewModel hud = HudViewModel.Unpublished;

        fixture.RunTick(0, hud, out hud);
        PresentationSnapshot held = fixture.Publisher.Latest!;
        string heldRendering = held.Render();
        SnapshotVersion heldVersion = held.Version;

        fixture.RunTick(1, hud, out hud);

        Expect.Multiple(() =>
        {
            Assert.That(
                held.Render(),
                Is.EqualTo(heldRendering),
                "the snapshot held for tick 0 must be byte-identical after tick 1 is published");
            Assert.That(held.Version, Is.EqualTo(heldVersion), "including its version");
            Assert.That(held.Tick, Is.EqualTo(0L), "and its tick");
            Assert.That(
                fixture.Publisher.Latest,
                Is.Not.SameAs(held),
                "the next publication must have written the other page, not the held one");
            Assert.That(
                fixture.Publisher.Previous,
                Is.SameAs(held),
                "and the held snapshot is now the previous of the pair presentation interpolates between");
            Assert.That(fixture.Publisher.Latest!.Tick, Is.EqualTo(1L));
            Assert.That(
                fixture.Publisher.Latest!.Version,
                Is.GreaterThan(held.Version),
                "so a consumer can tell which of the two readable snapshots is newer");
            Assert.That(
                SnapshotDoubleBuffer.PageCount,
                Is.EqualTo(2),
                "doc 20 § Presentation snapshot and doc 30 § Snapshot synchronization both name exactly two");
        });

        // The entity views of the two live pages must be different storage, or one publication would rewrite
        // the other's records.
        ReadOnlyMemory<SnapshotEntity> latestView = fixture.Publisher.Latest!.VisibleEntities;
        ReadOnlyMemory<SnapshotEntity> previousView = fixture.Publisher.Previous!.VisibleEntities;

        Expect.Multiple(() =>
        {
            Assert.That(
                latestView.Equals(previousView),
                Is.False,
                "the two live pages must span different storage; ReadOnlyMemory equality compares the "
                    + "underlying array reference, so equality here would mean one page rewrites the other");
            Assert.That(
                latestView.Span[0],
                Is.Not.EqualTo(previousView.Span[0]),
                "and the fixture must move something between ticks, or shared storage would be undetectable");
            Assert.That(latestView.Length, Is.EqualTo(previousView.Length));
        });

        // A third publication reclaims the page the first used. That is the documented depth, and CTR-SIM-003
        // already says a consumer either drops a stale snapshot or fully rebuilds.
        fixture.RunTick(2, hud, out hud);
        Expect.Multiple(() =>
        {
            Assert.That(fixture.Publisher.Latest!.Tick, Is.EqualTo(2L), "the newest snapshot is tick 2");
            Assert.That(
                fixture.Publisher.Previous!.Tick,
                Is.EqualTo(1L),
                "and the pair is ticks 1 and 2; the tick-0 page has been reclaimed, which is the two-page "
                    + "depth doc 30 § Snapshot synchronization asks for and no more");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-007-009</c>.
    ///
    /// Publishing a churn-free tick allocates nothing: across many publications the buffer cycles through
    /// exactly two <c>PresentationSnapshot</c> instances, never creates a third, and each page keeps the same
    /// backing storage throughout - so untouched map state is shared rather than copied per tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Object identity rather than a byte measurement, deliberately.</b> An earlier version of this gate
    /// asserted <c>GC.GetAllocatedBytesForCurrentThread()</c> was exactly zero across a measured window. That
    /// proved unreliable in a way no warm-up could fix: the residual tracked codegen of the measuring
    /// scaffold rather than any statement in the publisher - replacing <em>any</em> statement in
    /// <c>SnapshotPublisher.Publish</c> with a stub made it pass, and instrumenting 20,000 iterations found
    /// one nonzero delta in 20,000 followed later by 64 in 64. doc 91 § Flake policy makes a flaky required
    /// test a defect, and a gate that flakes rarely is worse than one that flakes often because nobody
    /// believes the failure.
    /// </para>
    /// <para>
    /// What "does not allocate" <em>means</em> for this design is that the pages and their arrays are created
    /// once and reused, and that is decidable rather than measurable: two page instances, no third, and
    /// stable backing storage per page. <see cref="ReadOnlyMemory{T}"/> equality compares the underlying
    /// array reference together with the offset and length, so it is exactly the reference-identity check
    /// needed, with no accessor that hands the array out.
    /// </para>
    /// <para>
    /// A real per-frame allocation budget in bytes belongs to a benchmark scenario in the main tier, not to
    /// the fast tier, and <c>QUA-005</c> owns that gate. The entry records that deferral in its summary and
    /// not in <c>successor</c>: doc 91 § What 'successor' means makes the field a property of retirement
    /// whose target is a <c>VER-*</c> entry, so a work-package ID on an implemented entry was neither, and
    /// no <c>VER-*</c> ID is invented for a scenario that does not exist yet.
    /// </para>
    /// </remarks>
    [Test]
    public void PublishingAChurnFreeTickAllocatesNothing()
    {
        // Structural half: no publication can replace a page or a page's array, because every field holding
        // one is readonly. A readonly array field assigned only in the constructor is reference-identical for
        // the object's lifetime, which is the claim, established by construction rather than by sampling.
        AssertPageStorageFieldsAreReadonlyArrays();

        SnapshotFixture fixture = new(enemyCount: 8);
        HudViewModel hud = HudViewModel.Unpublished;

        List<PresentationSnapshot> distinctPages = new(4);
        List<int> pageOrder = new(PublicationCount);
        List<ReadOnlyMemory<SnapshotEntity>> firstViewOfPage = new(4);
        List<string> storageDrift = new();

        for (long tick = 0; tick < PublicationCount; tick++)
        {
            PublishChurnFreeTick(fixture, tick, ref hud);
            PresentationSnapshot latest = fixture.Publisher.Latest!;

            int pageIndex = IndexOfSameInstance(distinctPages, latest);
            if (pageIndex < 0)
            {
                distinctPages.Add(latest);
                firstViewOfPage.Add(latest.VisibleEntities);
                pageIndex = distinctPages.Count - 1;
            }
            else if (!latest.VisibleEntities.Equals(firstViewOfPage[pageIndex]))
            {
                storageDrift.Add(
                    "page " + pageIndex.ToString(CultureInfo.InvariantCulture)
                    + " changed backing storage at tick " + tick.ToString(CultureInfo.InvariantCulture));
            }

            pageOrder.Add(pageIndex);
        }

        int alternationBreaks = 0;
        for (int index = 1; index < pageOrder.Count; index++)
        {
            if (pageOrder[index] == pageOrder[index - 1])
            {
                alternationBreaks++;
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                distinctPages,
                Has.Count.EqualTo(SnapshotDoubleBuffer.PageCount),
                PublicationCount.ToString(CultureInfo.InvariantCulture)
                    + " publications must cycle through exactly "
                    + SnapshotDoubleBuffer.PageCount.ToString(CultureInfo.InvariantCulture)
                    + " snapshot instances; a third instance means a snapshot was allocated per tick rather "
                    + "than a page reused");
            Assert.That(
                storageDrift,
                Is.Empty,
                "each page must keep the same backing storage for every publication that writes it: "
                    + string.Join("; ", storageDrift));
            Assert.That(
                alternationBreaks,
                Is.EqualTo(0),
                "publications must alternate strictly between the two pages, so the page a consumer holds is "
                    + "never the page being written");
            Assert.That(
                fixture.Publisher.Buffer.PublishedCount,
                Is.EqualTo(SnapshotDoubleBuffer.PageCount),
                "the buffer still reports exactly two live pages after "
                    + PublicationCount.ToString(CultureInfo.InvariantCulture)
                    + " publications, so nothing accumulated");
            Assert.That(
                fixture.Publisher.Latest!.VisibleEntityCount,
                Is.EqualTo(8),
                "and every publication carried the whole visible population, so the pages were genuinely "
                    + "rewritten rather than left stale");
            Assert.That(
                fixture.Publisher.Latest!.Tick,
                Is.EqualTo((long)(PublicationCount - 1)));
            Assert.That(
                fixture.Publisher.LatestVersion.Value,
                Is.EqualTo((long)PublicationCount),
                "one version per publication, so no publication was skipped");
        });
    }

    /// <summary>
    /// Asserts that every field of <see cref="SnapshotDoubleBuffer"/> that holds a page or page storage is a
    /// <see langword="readonly"/> plain array, so no publication can replace one.
    /// </summary>
    private static void AssertPageStorageFieldsAreReadonlyArrays()
    {
        FieldInfo[] fields = typeof(SnapshotDoubleBuffer).GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        List<string> arrayFieldNames = new();
        List<string> violations = new();
        foreach (FieldInfo field in fields)
        {
            if (!field.FieldType.IsArray)
            {
                continue;
            }

            arrayFieldNames.Add(field.Name);
            if (!field.IsInitOnly)
            {
                violations.Add(field.Name + " is not readonly, so a publication could replace it");
            }
        }

        arrayFieldNames.Sort(StringComparer.Ordinal);

        Expect.Multiple(() =>
        {
            Assert.That(
                arrayFieldNames,
                Is.EqualTo(new[] { "_pageEntities", "_pages" }),
                "SnapshotDoubleBuffer's array fields are the two pages and their entity storage; a new array "
                    + "field here is new per-tick storage and needs its own registry entry");
            Assert.That(violations, Is.Empty, string.Join("; ", violations));
        });
    }

    private static int IndexOfSameInstance(List<PresentationSnapshot> pages, PresentationSnapshot candidate)
    {
        for (int index = 0; index < pages.Count; index++)
        {
            if (ReferenceEquals(pages[index], candidate))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>Publishes one tick with no entity churn.</summary>
    /// <returns>The published batch sizes, so the caller consumes the tick result rather than discarding it.</returns>
    private static int PublishChurnFreeTick(SnapshotFixture fixture, long tick, ref HudViewModel hud)
    {
        fixture.Publisher.BeginTick(tick);
        fixture.DomainEvents.BeginTick(tick);
        fixture.PresentationEvents.BeginTick(tick);

        fixture.Publisher.StagePlayer(tick * 0.01, tick * -0.01, 0.0);
        hud = HudViewModel.Next(hud, 100.0, 5.0, 300, 25, tick / 60.0, 0.0);
        fixture.Publisher.StageHud(hud);
        fixture.Publisher.StageTerminalState(false);

        for (int index = 0; index < fixture.EnemyIds.Count; index++)
        {
            fixture.Publisher.StageVisibleEntity(SnapshotEntity.Create(
                fixture.EnemyIds[index],
                MechaMiner.Simulation.Entities.PopulationCategory.OrdinaryEnemy,
                index * 2.0,
                (index * -1.5) + tick,
                0.0,
                presentationFlags: 1));
        }

        TickPublication publication = fixture.Publisher.Publish(
            fixture.DomainEvents,
            fixture.PresentationEvents,
            MechaMiner.Simulation.Events.PresentationCoalescingPolicy.Verbatim);
        int observed = publication.DomainEventCount + publication.PresentationEventCount;
        fixture.Publisher.ReleaseTick(fixture.DomainEvents, fixture.PresentationEvents);
        return observed;
    }
}
