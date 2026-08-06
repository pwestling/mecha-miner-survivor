using System.Collections.Generic;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// The structured result document every verb writes beneath <c>artifacts/</c>.
/// </summary>
/// <remarks>
/// <para>
/// Doc 100 § Standard command surface: "Every verb is noninteractive, returns
/// nonzero on failure, writes structured evidence beneath <c>artifacts/</c>, and
/// prints a concise final result plus artifact paths."
/// </para>
/// <para>
/// This is not <c>SCH-OBS-003</c>. The task evidence summary schema, its canonical
/// emitter, and its validator are owned by <c>FND-010</c>
/// (<c>TASK-FND-010-001</c>), and <c>TASK-FND-010-002</c> integrates evidence
/// emission into these verbs. This document is the verb-level record that
/// integration will consume, and its field names deliberately match the evidence
/// bundle vocabulary of doc 114 § Required evidence bundle so the later mapping is
/// mechanical.
/// </para>
/// </remarks>
internal sealed class InvocationRecord
{
    /// <summary>Stable schema identity of this document.</summary>
    public string Schema { get; set; } = "MMT-VERB-RESULT";

    /// <summary>Version of this document's shape.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>The verb name.</summary>
    public string Verb { get; set; } = string.Empty;

    /// <summary>The exact argument vector the wrapper passed, excluding the verb.</summary>
    public List<string> Arguments { get; set; } = new();

    /// <summary>The work package that owns this verb's behavior.</summary>
    public string OwningWorkPackage { get; set; } = string.Empty;

    /// <summary>The invocation identifier, which is also the artifact directory name.</summary>
    public string InvocationId { get; set; } = string.Empty;

    /// <summary>UTC start time in round-trip format.</summary>
    public string StartedUtc { get; set; } = string.Empty;

    /// <summary>Wall-clock duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>The doc 100 exit class returned to the caller.</summary>
    public int ExitClass { get; set; }

    /// <summary>The stable short name of <see cref="ExitClass"/>.</summary>
    public string ExitClassName { get; set; } = string.Empty;

    /// <summary>The stable diagnostic code.</summary>
    public string DiagnosticCode { get; set; } = string.Empty;

    /// <summary>The concise final result line, identical to the one printed.</summary>
    public string FinalResult { get; set; } = string.Empty;

    /// <summary>The workflow configuration the verb operated on, when it takes one.</summary>
    public string? Configuration { get; set; }

    /// <summary>The MSBuild configuration the workflow configuration maps onto.</summary>
    public string? MsbuildConfiguration { get; set; }

    /// <summary>
    /// The canonical <c>SCH-BLD-001</c> build identity of the workflow host that ran
    /// this verb.
    /// </summary>
    /// <remarks>
    /// doc 100 § Version and build identity requires every diagnostic header to carry
    /// build identity, and this document is the tool's diagnostic header. It comes from
    /// the one diagnostics owner (<c>CMP-OBS-001</c>) rather than being re-derived here,
    /// which is what makes it equal to the identity the game and the diagnostics
    /// surfaces report (<c>VER-FND-004-004</c>).
    /// </remarks>
    public string BuildIdentity { get; set; } = string.Empty;

    /// <summary>The source revision, or <c>unknown</c> when git is unavailable.</summary>
    public string SourceRevision { get; set; } = "unknown";

    /// <summary>Whether the working tree had uncommitted changes.</summary>
    public bool SourceTreeDirty { get; set; }

    /// <summary>The ordered steps this verb executed.</summary>
    public List<StepRecord> Steps { get; set; } = new();

    /// <summary>Warnings that did not change the exit class.</summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>Repository-relative artifact paths this verb produced.</summary>
    public List<string> Artifacts { get; set; } = new();
}
