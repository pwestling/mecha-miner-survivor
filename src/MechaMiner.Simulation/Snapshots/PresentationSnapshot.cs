using System;
using System.Globalization;

namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// One complete, immutable presentation snapshot: <c>CTR-SIM-003</c>'s payload.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot: "At the end of each tick the
/// simulation publishes a read-only snapshot optimized for presentation synchronization", including
/// "tick and interpolation anchor", the player transform and meters, "visible or potentially visible
/// entity transforms and presentation-state flags", terminal state, and "versioned HUD view models".
/// "Snapshots do not expose mutable stores. Double buffering or immutable pooled pages avoids copying
/// untouched static map state."
/// </para>
/// <para>
/// <c>CTR-SIM-003</c> in doc 115 § Cross-boundary contract registry: produced by <c>CMP-SIM-003</c>,
/// consumed by <c>CMP-PRE-001</c>, <c>CMP-UI-001</c>, and <c>CMP-AUD-001</c>, delivered as the
/// "immutable latest complete snapshot with run/tick/version; double-buffered", and on failure
/// "consumer drops stale snapshot or fully rebuilds; never mutates it". Those three consumers live
/// outside this assembly, which is why this type and everything it exposes is public.
/// </para>
/// <para>
/// <b>Immutability is structural, not guarded.</b> Every field of this type has an immutable type -
/// scalars, enums, readonly structs, and <see cref="ReadOnlyMemory{T}"/> - so there is no member,
/// public or private, that a consumer could mutate and none that could be handed out and then written
/// through. In particular this type owns no <c>T[]</c> and no <c>List{T}</c>: the page storage the
/// visible-entity view spans lives in <see cref="SnapshotDoubleBuffer"/>, which is not a payload type.
/// A future member of a mutable type therefore fails
/// <c>PresentationSnapshotTests.SnapshotIsImmutableAndExposesNoMutableStore</c>, which reflects over
/// every field of every payload type rather than the members someone remembered to check.
/// </para>
/// <para>
/// <b>Why a class whose fields are rewritten.</b> It is one of the two pages of a double buffer. Writing
/// the back page while the front page is held is what makes publication allocation-free
/// (<c>VER-SIM-007-009</c>) and what makes a held snapshot immune to the next publication
/// (<c>VER-SIM-007-003</c>). The writer is internal to <see cref="SnapshotDoubleBuffer"/>, which doc 115
/// § Mutable-state ownership matrix requires: one writer, and it is <c>CMP-SIM-003</c>.
/// </para>
/// </remarks>
public sealed class PresentationSnapshot
{
    private ulong _runSession;
    private long _tick;
    private SnapshotVersion _version;
    private double _playerPositionX;
    private double _playerPositionY;
    private double _playerFacingRadians;
    private bool _isTerminal;
    private HudViewModel _hud;
    private ReadOnlyMemory<SnapshotEntity> _visibleEntities;

    internal PresentationSnapshot()
    {
    }

    /// <summary>The run session this snapshot belongs to.</summary>
    /// <remarks>
    /// <c>CTR-SIM-003</c> requires run identity on the payload so a consumer can tell whether a snapshot
    /// belongs to its run at all; doc 30 § Snapshot synchronization has "run identity fences all handles".
    /// </remarks>
    public ulong RunSession => _runSession;

    /// <summary>The authoritative tick this snapshot was published for.</summary>
    /// <remarks>doc 20 § Presentation snapshot: the snapshot carries the "tick and interpolation anchor".</remarks>
    public long Tick => _tick;

    /// <summary>The strictly increasing publication version.</summary>
    public SnapshotVersion Version => _version;

    /// <summary>
    /// The planar X component of the player's transform, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> this component and <see cref="PlayerPositionY"/> must be replaced by the single
    /// planar position type that <c>W2-GEO</c> owns once <c>GEO-001</c> lands. The change is: delete both
    /// <c>double</c> fields and properties, replace them with one planar-position field and property,
    /// update <c>SnapshotDoubleBuffer.WritePage</c> and <c>SnapshotPublisher.StagePlayer</c> to take that
    /// type, and update <see cref="Render"/>. Do not introduce a planar vector type in this package.
    /// </remarks>
    public double PlayerPositionX => _playerPositionX;

    /// <summary>
    /// The planar Y component of the player's transform, in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <b>GEO-001:</b> see <see cref="PlayerPositionX"/>. Double rather than single precision per doc 20 §
    /// Numeric and unit conventions.
    /// </remarks>
    public double PlayerPositionY => _playerPositionY;

    /// <summary>The player's facing, in radians.</summary>
    public double PlayerFacingRadians => _playerFacingRadians;

    /// <summary>Whether the run has reached a terminal result.</summary>
    /// <remarks>doc 20 § Presentation snapshot includes "run clock, schedule phase, pause-independent display state, and terminal state".</remarks>
    public bool IsTerminal => _isTerminal;

    /// <summary>The versioned HUD view model, already rounded by the authoritative rule.</summary>
    public HudViewModel Hud => _hud;

    /// <summary>
    /// The visible or potentially visible entities, as a read-only view over this page's own storage.
    /// </summary>
    /// <remarks>
    /// <see cref="ReadOnlyMemory{T}"/> rather than an array, a list, or a copy: an array would be a
    /// mutable collection crossing a boundary, which doc 115 § Cross-boundary contract registry forbids
    /// outright; a fresh copy each tick would allocate and defeat doc 20 § Presentation snapshot's
    /// requirement that double buffering "avoids copying untouched static map state". The view spans the
    /// page this snapshot <em>is</em>, and the other page is what the next publication writes, so a held
    /// snapshot's view is never rewritten under it.
    /// </remarks>
    public ReadOnlyMemory<SnapshotEntity> VisibleEntities => _visibleEntities;

    /// <summary>How many visible entities this snapshot carries.</summary>
    public int VisibleEntityCount => _visibleEntities.Length;

    /// <summary>True when this page has been written at least once.</summary>
    public bool IsPublished => _version.IsPublished;

    /// <summary>
    /// Renders the whole snapshot as canonical invariant text, for reconstruction comparisons and
    /// evidence.
    /// </summary>
    /// <remarks>
    /// A reconstruction test needs to compare two whole snapshots exactly, and a rendered form makes the
    /// comparison reviewable rather than a reference check.
    /// <see cref="CultureInfo.InvariantCulture"/> and the round-trip <c>R</c> format throughout, so the
    /// text is identical on every platform (doc 91 § Determinism and fixture policy).
    /// </remarks>
    public string Render()
    {
        System.Text.StringBuilder builder = new();
        builder
            .Append("snapshot run=")
            .Append(_runSession.ToString(CultureInfo.InvariantCulture))
            .Append(" tick=")
            .Append(_tick.ToString(CultureInfo.InvariantCulture))
            .Append(' ')
            .Append(_version.ToString())
            .Append(" terminal=")
            .Append(_isTerminal ? "yes" : "no")
            .Append(" player=(")
            .Append(_playerPositionX.ToString("R", CultureInfo.InvariantCulture))
            .Append(',')
            .Append(_playerPositionY.ToString("R", CultureInfo.InvariantCulture))
            .Append(") facing=")
            .Append(_playerFacingRadians.ToString("R", CultureInfo.InvariantCulture))
            .Append(' ')
            .Append(_hud.ToString())
            .Append('\n');

        ReadOnlySpan<SnapshotEntity> entities = _visibleEntities.Span;
        for (int index = 0; index < entities.Length; index++)
        {
            builder
                .Append("  ")
                .Append(index.ToString(CultureInfo.InvariantCulture).PadLeft(3))
                .Append("  ")
                .Append(entities[index].ToString())
                .Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Rewrites this page. Called only by <see cref="SnapshotDoubleBuffer"/>, and only for the back page.
    /// </summary>
    internal void WritePage(
        ulong runSession,
        long tick,
        SnapshotVersion version,
        double playerPositionX,
        double playerPositionY,
        double playerFacingRadians,
        bool isTerminal,
        in HudViewModel hud,
        ReadOnlyMemory<SnapshotEntity> visibleEntities)
    {
        _runSession = runSession;
        _tick = tick;
        _version = version;
        _playerPositionX = playerPositionX;
        _playerPositionY = playerPositionY;
        _playerFacingRadians = playerFacingRadians;
        _isTerminal = isTerminal;
        _hud = hud;
        _visibleEntities = visibleEntities;
    }
}
