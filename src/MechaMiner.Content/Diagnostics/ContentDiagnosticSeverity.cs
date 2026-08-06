namespace MechaMiner.Content.Diagnostics;

/// <summary>
/// How a content diagnostic affects a build.
/// </summary>
/// <remarks>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Compilation pipeline:
/// "CI fails on errors. Warnings have an owner and expiration; release builds treat
/// unresolved content warnings as errors unless allowlisted with rationale." There
/// are therefore exactly two severities and no informational level, because an
/// informational content diagnostic has no owner, no expiry, and nothing that ever
/// makes it go away.
/// </remarks>
public enum ContentDiagnosticSeverity
{
    /// <summary>
    /// The content is invalid. CI fails. A compiler never materializes a default to
    /// paper over one (doc 115 <c>CMP-CNT-001</c> forbidden responsibility: "silently
    /// defaulting invalid content").
    /// </summary>
    Error = 0,

    /// <summary>
    /// The content is accepted for now under a named owner and a date by which it must
    /// be resolved.
    /// </summary>
    Warning = 1,
}
