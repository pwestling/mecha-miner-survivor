using System.Collections.Generic;

namespace MechaMiner.Diagnostics.Logging;

/// <summary>One bounded breadcrumb of a diagnostic run record.</summary>
internal sealed class DiagnosticBreadcrumb
{
    /// <summary>The simulation tick, or <c>-1</c> outside a ticking run.</summary>
    public long Tick { get; set; } = -1;

    /// <summary>The stable event code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The redacted one-line summary.</summary>
    public string Detail { get; set; } = string.Empty;
}

/// <summary>
/// <c>SCH-OBS-001</c>, the diagnostic run record.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>. Authority:
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Diagnostic run
/// record, which fixes the header contents, and
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Schema registry:
/// "run/build/content/map identity, bounded breadcrumbs, outcome". Requirements:
/// <c>TR-RUN-009</c>, <c>TR-OBS-001</c>.
/// </para>
/// <para>
/// <c>FND-007</c> lands the schema, its canonical serialization, and the build-identity half
/// of the header, which is the part that exists before any run does. The run-scoped fields —
/// master seed, generation checksum, selected mech, account-power summary, terminal outcome —
/// are filled by <c>CMP-RUN-001</c> when <c>SIM-009</c> and <c>PRG-006</c> land. They are
/// declared here now, with explicit empty values, so the schema's shape does not change when
/// their owner arrives, and so a reader can tell an unfilled field from an absent one.
/// </para>
/// <para>
/// It is not a replay. Doc 90: "It is not an exact replay." Breadcrumbs are bounded and
/// coalesced by design; reconstructing a run from one is not a supported use.
/// </para>
/// </remarks>
internal sealed class DiagnosticRunRecord
{
    /// <summary>Stable schema identity. Always <c>SCH-OBS-001</c>.</summary>
    public string Schema { get; set; } = "SCH-OBS-001";

    /// <summary>Version of this document's shape.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>The diagnostic ID of this record.</summary>
    public string DiagnosticId { get; set; } = string.Empty;

    /// <summary>The run diagnostic ID, or empty when the record covers no run.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>The canonical <c>SCH-BLD-001</c> identity line.</summary>
    public string BuildIdentity { get; set; } = string.Empty;

    /// <summary>The content bundle hash, or the declared unavailable status.</summary>
    public string ContentIdentity { get; set; } = string.Empty;

    /// <summary>The master seed, as unsigned decimal text; empty until a run exists.</summary>
    public string MasterSeed { get; set; } = string.Empty;

    /// <summary>The generation manifest checksum; empty until <c>MAP-007</c>.</summary>
    public string MapManifestChecksum { get; set; } = string.Empty;

    /// <summary>Schema, map, random, and save versions, taken from build identity.</summary>
    public DiagnosticDataVersions DataVersions { get; set; } = new();

    /// <summary>Platform, renderer, quality, resolution, and input family.</summary>
    public DiagnosticEnvironment Environment { get; set; } = new();

    /// <summary>Selected mech stable ID; empty until a run exists.</summary>
    public string SelectedMech { get; set; } = string.Empty;

    /// <summary>Warning, invariant, and capacity counters.</summary>
    public DiagnosticCounters Counters { get; set; } = new();

    /// <summary>Bounded major-event breadcrumbs, in order.</summary>
    public List<DiagnosticBreadcrumb> Breadcrumbs { get; set; } = new();

    /// <summary>
    /// The terminal outcome, or the last recovery tick. Empty until a run terminates, which
    /// is <c>PRG-006</c>'s to fill.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;
}

/// <summary>Schema, map, random, and save versions carried in a diagnostic header.</summary>
internal sealed class DiagnosticDataVersions
{
    /// <summary>Content/definition schema version.</summary>
    public int Schema { get; set; }

    /// <summary>Map generator version.</summary>
    public int Map { get; set; }

    /// <summary>Authoritative random schema version.</summary>
    public int Random { get; set; }

    /// <summary>Save schema version.</summary>
    public int Save { get; set; }
}

/// <summary>Platform, renderer, quality, resolution, and input family.</summary>
internal sealed class DiagnosticEnvironment
{
    /// <summary>The build-time platform identifier, for example <c>linux-x64</c>.</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>The renderer in use; empty outside the engine process.</summary>
    public string Renderer { get; set; } = string.Empty;

    /// <summary>The quality preset; empty until <c>PRE-007</c>.</summary>
    public string Quality { get; set; } = string.Empty;

    /// <summary>The resolution as <c>WxH</c>; empty outside the engine process.</summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>The input family; empty until <c>UI-002</c>.</summary>
    public string InputFamily { get; set; } = string.Empty;
}

/// <summary>Warning, invariant, and capacity counters.</summary>
internal sealed class DiagnosticCounters
{
    /// <summary>Records with warning severity.</summary>
    public int Warnings { get; set; }

    /// <summary>Records with error severity.</summary>
    public int Errors { get; set; }

    /// <summary>Records suppressed by rate limiting.</summary>
    public int Suppressed { get; set; }

    /// <summary>Records the bounded ring discarded.</summary>
    public int Overflowed { get; set; }

    /// <summary>Records a sink refused to write.</summary>
    public int Dropped { get; set; }
}
