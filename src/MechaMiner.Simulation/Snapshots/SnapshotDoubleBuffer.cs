using System;
using System.Globalization;

namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// The two pages of the presentation double buffer, so the two most recent complete snapshots are
/// simultaneously readable and a held snapshot is never rewritten under its holder.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot: "Snapshots do not expose mutable
/// stores. Double buffering or immutable pooled pages avoids copying untouched static map state.
/// Presentation interpolates transforms between the two most recent complete snapshots".
/// <c>docs/technical/30-presentation-and-rendering.md</c> § Snapshot synchronization: "Presentation
/// consumes the two most recent committed simulation snapshots."
/// </para>
/// <para>
/// <b>Two pages exactly, and why that is a contract rather than a size.</b> A publication writes the page
/// that is currently the older of the two and then flips, so the page a consumer holds as
/// <see cref="Latest"/> becomes <see cref="Previous"/> and is untouched; only the publication after that
/// reclaims it. Presentation therefore has exactly one tick of grace, which is what doc 30 § Snapshot
/// synchronization's interpolation between the two most recent snapshots needs, and no more - a consumer
/// holding a snapshot across two further publications is holding a page that has been reclaimed, and
/// <c>CTR-SIM-003</c>'s own failure behaviour is that a consumer "drops stale snapshot or fully rebuilds".
/// </para>
/// <para>
/// <b>This type owns the arrays; the payload types own none.</b> Each page has its own entity array, and
/// the page's <c>PresentationSnapshot</c> holds only a <see cref="ReadOnlyMemory{T}"/> over it. That is
/// what lets every field of every payload type have an immutable type while publication still allocates
/// nothing.
/// </para>
/// <para>
/// The write path is internal because doc 115 § Mutable-state ownership matrix gives <c>CMP-SIM-003</c>
/// sole ownership of the double buffers, and <c>SnapshotPublisher</c> is that component. The read surface
/// is public because <c>CTR-SIM-003</c>'s consumers are outside this assembly.
/// </para>
/// </remarks>
public sealed class SnapshotDoubleBuffer
{
    /// <summary>How many complete snapshots are simultaneously readable.</summary>
    /// <remarks>Two, per doc 20 § Presentation snapshot and doc 30 § Snapshot synchronization.</remarks>
    public const int PageCount = 2;

    private readonly ulong _runSession;
    private readonly PresentationSnapshot[] _pages;
    private readonly SnapshotEntity[][] _pageEntities;
    private int _frontPage;
    private SnapshotVersion _latestVersion;
    private int _publishedCount;

    /// <summary>Creates the two pages, each sized for the largest publishable entity population.</summary>
    /// <param name="runSession">The run session every page is fenced to. Must not be zero.</param>
    /// <param name="visibleEntityCapacity">
    /// The largest number of visible entities a publication may carry. Preallocated once per page, so a
    /// publication never grows an array.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The run session is zero, or the capacity is negative.</exception>
    public SnapshotDoubleBuffer(ulong runSession, int visibleEntityCapacity)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run'");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(visibleEntityCapacity);

        _runSession = runSession;
        _pages = new PresentationSnapshot[PageCount];
        _pageEntities = new SnapshotEntity[PageCount][];
        for (int page = 0; page < PageCount; page++)
        {
            _pages[page] = new PresentationSnapshot();
            _pageEntities[page] = new SnapshotEntity[visibleEntityCapacity];
        }

        _frontPage = 0;
    }

    /// <summary>The run session every page is fenced to.</summary>
    public ulong RunSession => _runSession;

    /// <summary>The largest visible-entity population a publication may carry.</summary>
    public int VisibleEntityCapacity => _pageEntities[0].Length;

    /// <summary>The version of the most recent publication, or unpublished.</summary>
    public SnapshotVersion LatestVersion => _latestVersion;

    /// <summary>How many snapshots have been published in this run.</summary>
    public int PublishedCount => _publishedCount;

    /// <summary>The most recent complete snapshot, or <see langword="null"/> before the first publication.</summary>
    public PresentationSnapshot? Latest => _publishedCount >= 1 ? _pages[_frontPage] : null;

    /// <summary>
    /// The snapshot published immediately before <see cref="Latest"/>, or <see langword="null"/> before the
    /// second publication.
    /// </summary>
    /// <remarks>
    /// Together with <see cref="Latest"/> this is the pair doc 30 § Snapshot synchronization interpolates
    /// between: "Render interpolation uses the render-frame fraction between their tick anchors."
    /// </remarks>
    public PresentationSnapshot? Previous => _publishedCount >= 2 ? _pages[1 - _frontPage] : null;

    /// <summary>
    /// Writes the back page and flips, so the newly written page becomes <see cref="Latest"/>, and
    /// returns that page.
    /// </summary>
    /// <returns>
    /// The page just written, which is also <see cref="Latest"/>. Its <c>Version</c> is the version this
    /// publication minted.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Allocation-free: both pages and both entity arrays exist from construction, the entity span is
    /// copied into the back page's own array, and the snapshot's view is a
    /// <see cref="ReadOnlyMemory{T}"/> struct over it.
    /// </para>
    /// <para>
    /// <b>The page, not the version.</b> Returning the written page rather than the minted
    /// <see cref="SnapshotVersion"/> is what lets <c>SnapshotPublisher.Publish</c> take the published
    /// snapshot and its version from one value. When this returned the version, the publisher had to read
    /// <see cref="Latest"/> back and reconcile the two - a check that could only run <em>after</em> the
    /// flip, which is the one point in a tick at which
    /// <c>SnapshotPublisher.InvalidateTick</c> is no longer available. One value cannot disagree with
    /// itself, so there is nothing left to reconcile and nothing left to throw there.
    /// </para>
    /// <para>
    /// <b>GEO-001:</b> the <paramref name="playerPositionX"/> and <paramref name="playerPositionY"/>
    /// parameters must be replaced by the single planar position type that <c>W2-GEO</c> owns once
    /// <c>GEO-001</c> lands. The change is: delete both <c>double</c> parameters, replace them with one
    /// planar-position parameter, and forward that one value to
    /// <c>PresentationSnapshot.WritePage</c> instead of the pair. Do not introduce a planar vector type in
    /// this package. Double rather than single precision because doc 20 § Numeric and unit conventions
    /// permits single for planar transforms only "after tests confirm the accepted map scale remains safely
    /// within precision bounds", and that confirmation does not exist.
    /// </para>
    /// </remarks>
    internal PresentationSnapshot Publish(
        long tick,
        double playerPositionX,
        double playerPositionY,
        double playerFacingRadians,
        bool isTerminal,
        in HudViewModel hud,
        ReadOnlySpan<SnapshotEntity> visibleEntities)
    {
        int backPage = 1 - _frontPage;
        if (visibleEntities.Length > _pageEntities[backPage].Length)
        {
            throw new InvalidOperationException(
                "a publication carries "
                    + visibleEntities.Length.ToString(CultureInfo.InvariantCulture)
                    + " visible entities but a page holds "
                    + _pageEntities[backPage].Length.ToString(CultureInfo.InvariantCulture)
                    + "; growing here would allocate inside a committed tick, so the capacity is a "
                    + "failed invariant rather than a soft limit");
        }

        visibleEntities.CopyTo(_pageEntities[backPage]);
        SnapshotVersion version = _latestVersion.Next();

        _pages[backPage].WritePage(
            _runSession,
            tick,
            version,
            playerPositionX,
            playerPositionY,
            playerFacingRadians,
            isTerminal,
            hud,
            new ReadOnlyMemory<SnapshotEntity>(_pageEntities[backPage], 0, visibleEntities.Length));

        _frontPage = backPage;
        _latestVersion = version;
        if (_publishedCount < PageCount)
        {
            _publishedCount++;
        }

        return _pages[backPage];
    }
}
