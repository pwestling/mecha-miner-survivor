using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MechaMiner.Diagnostics.Logging;

/// <summary>Log severity.</summary>
/// <remarks>
/// Doc 90 § Structured logging: "Player-facing expected failures such as unaffordable
/// purchase are not error logs; unexpected rejection/invariant divergence is." The
/// severity of a record is therefore fixed by its code in
/// <see cref="DiagnosticCatalog"/> rather than chosen at each call site, so the same
/// event cannot be an error in one caller and information in another.
/// </remarks>
internal enum DiagnosticSeverity
{
    /// <summary>Development-only detail; absent from Release logs by policy.</summary>
    Debug,

    /// <summary>Normal operation, including an expected player-facing rejection.</summary>
    Information,

    /// <summary>Degraded but handled: a fallback was used or a bound was reached.</summary>
    Warning,

    /// <summary>An unexpected rejection, invariant divergence, or failed operation.</summary>
    Error,

    /// <summary>The process cannot continue coherently.</summary>
    Fatal,
}

/// <summary>
/// The log categories of doc 90 § Structured logging, in that document's order.
/// </summary>
/// <remarks>
/// The list is closed and matches the document's ten initial categories exactly. A new
/// category is a documentation change first, which is why this is an enumeration rather
/// than a free string.
/// </remarks>
internal enum DiagnosticCategory
{
    /// <summary>bootstrap/build/platform.</summary>
    Bootstrap,

    /// <summary>content/import/asset.</summary>
    Content,

    /// <summary>persistence/cloud/migration.</summary>
    Persistence,

    /// <summary>generation/validation.</summary>
    Generation,

    /// <summary>simulation invariant/command/transaction.</summary>
    Simulation,

    /// <summary>director/spawn/capacity.</summary>
    Encounter,

    /// <summary>presentation/resource fallback.</summary>
    Presentation,

    /// <summary>UI/input/accessibility.</summary>
    Interface,

    /// <summary>performance/benchmark.</summary>
    Performance,

    /// <summary>crash/shutdown.</summary>
    Shutdown,
}

/// <summary>One registered diagnostic event code.</summary>
internal sealed class DiagnosticCode
{
    internal DiagnosticCode(
        string code,
        DiagnosticCategory category,
        DiagnosticSeverity severity,
        int burst,
        string meaning)
    {
        Code = code;
        Category = category;
        Severity = severity;
        Burst = burst;
        Meaning = meaning;
    }

    /// <summary>The stable code, never reused or renumbered.</summary>
    internal string Code { get; }

    /// <summary>The category the code belongs to.</summary>
    internal DiagnosticCategory Category { get; }

    /// <summary>The severity every record with this code carries.</summary>
    internal DiagnosticSeverity Severity { get; }

    /// <summary>
    /// How many records with this code are emitted per rate-limit window before the
    /// remainder is replaced by one summary count. Zero means never rate-limited.
    /// </summary>
    /// <remarks>
    /// Doc 90: "Rate-limit repetitive diagnostics and emit a summary count." A code that
    /// reports a unique, non-repeating event is not rate-limited, because dropping it
    /// would lose information no summary could reconstruct.
    /// </remarks>
    internal int Burst { get; }

    /// <summary>What the code means, for a reader of a log file.</summary>
    internal string Meaning { get; }

    /// <summary>Whether records with this code are rate-limited.</summary>
    internal bool IsRateLimited => Burst > 0;
}

/// <summary>
/// The explicit registry of stable diagnostic event codes.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-007</c> (<c>TASK-FND-007-001</c>). Authority:
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Structured
/// logging, <c>CTR-OBS-001</c>. Requirements: <c>TR-OBS-001</c>, <c>TR-BLD-004</c>.
/// </para>
/// <para>
/// A literal array, not discovery. Doc 100 § C# project standards requires that
/// "generated/explicit registries make missing behavior a build error", and doc 114
/// forbids a reflection-based registry. Codes are never reused or renumbered
/// (<c>docs/technical/conventions.md</c> § Stable identifiers).
/// </para>
/// <para>
/// The codes registered here are the ones <c>FND-007</c> itself emits, plus one per
/// category so that every category doc 90 lists has at least one registered code and the
/// category-coverage test is a real assertion rather than a placeholder. Each later
/// package appends the codes it emits; the owning category and severity are declared once,
/// here, so a call site cannot promote an expected player-facing rejection into an error.
/// </para>
/// </remarks>
internal static class DiagnosticCatalog
{
    /// <summary>The prefix every diagnostics code carries.</summary>
    /// <remarks>
    /// Distinct from the tool host's <c>MMT-</c> and the engine runner's <c>MMG-</c> so a
    /// code in a mixed log names its emitter without a lookup.
    /// </remarks>
    internal const string CodePrefix = "MMD-";

    /// <summary>Build identity was read and verified at initialization step 1.</summary>
    internal const string BuildIdentityVerified = "MMD-0001";

    /// <summary>Bounded local logging opened at initialization step 2.</summary>
    internal const string LoggingStarted = "MMD-0002";

    /// <summary>A log file reached its size bound and was rotated.</summary>
    internal const string LogRotated = "MMD-0003";

    /// <summary>Retention deleted an aged log file inside the owned directory.</summary>
    internal const string LogRetentionApplied = "MMD-0004";

    /// <summary>A log sink write failed; the record was dropped rather than propagated.</summary>
    internal const string LogSinkFailed = "MMD-0005";

    /// <summary>The bounded in-memory queue was full and the oldest record was discarded.</summary>
    internal const string LogQueueOverflow = "MMD-0006";

    /// <summary>A rate-limited code exceeded its burst; the summary count follows.</summary>
    internal const string LogRateLimitSummary = "MMD-0007";

    /// <summary>A content definition failed validation.</summary>
    internal const string ContentValidationFailed = "MMD-1001";

    /// <summary>A durable write completed atomically.</summary>
    internal const string PersistenceWriteCommitted = "MMD-2001";

    /// <summary>A generation attempt was rejected and will be retried.</summary>
    internal const string GenerationAttemptRejected = "MMD-3001";

    /// <summary>A simulation invariant diverged; the run ends safely.</summary>
    internal const string SimulationInvariantViolated = "MMD-4001";

    /// <summary>A spawn was capacity-blocked and queued rather than cancelled.</summary>
    internal const string EncounterSpawnQueued = "MMD-5001";

    /// <summary>A noncritical presentation resource was missing or saturated; a fallback was used.</summary>
    internal const string PresentationFallbackUsed = "MMD-6001";

    /// <summary>A player-facing action was rejected for an expected reason.</summary>
    internal const string InterfaceActionRejected = "MMD-7001";

    /// <summary>A measured frame or subsystem exceeded its budget.</summary>
    internal const string PerformanceBudgetExceeded = "MMD-8001";

    /// <summary>An unclean shutdown was detected on the following boot.</summary>
    internal const string UncleanShutdownDetected = "MMD-9001";

    private static readonly ImmutableArray<DiagnosticCode> Registered = ImmutableArray.Create(
        new DiagnosticCode(
            BuildIdentityVerified,
            DiagnosticCategory.Bootstrap,
            DiagnosticSeverity.Information,
            burst: 0,
            "build identity was read and verified (doc 115 § Initialization order step 1)"),
        new DiagnosticCode(
            LoggingStarted,
            DiagnosticCategory.Bootstrap,
            DiagnosticSeverity.Information,
            burst: 0,
            "bounded local logging opened (doc 115 § Initialization order step 2)"),
        new DiagnosticCode(
            LogRotated,
            DiagnosticCategory.Bootstrap,
            DiagnosticSeverity.Information,
            burst: 0,
            "a log file reached the 4 MiB bound and was rotated"),
        new DiagnosticCode(
            LogRetentionApplied,
            DiagnosticCategory.Bootstrap,
            DiagnosticSeverity.Information,
            burst: 0,
            "retention deleted an aged log file inside the owned directory"),
        new DiagnosticCode(
            LogSinkFailed,
            DiagnosticCategory.Bootstrap,
            DiagnosticSeverity.Warning,
            burst: 3,
            "a log sink write failed; diagnostics degrade rather than propagating the failure"),
        new DiagnosticCode(
            LogQueueOverflow,
            DiagnosticCategory.Bootstrap,
            DiagnosticSeverity.Warning,
            burst: 1,
            "the bounded queue was full; the oldest record was discarded to keep the write nonblocking"),
        new DiagnosticCode(
            LogRateLimitSummary,
            DiagnosticCategory.Bootstrap,
            DiagnosticSeverity.Information,
            burst: 0,
            "the suppressed-record count for a rate-limited code in the window that just closed"),
        new DiagnosticCode(
            ContentValidationFailed,
            DiagnosticCategory.Content,
            DiagnosticSeverity.Error,
            burst: 0,
            "a content definition failed validation; startup or the build fails"),
        new DiagnosticCode(
            PersistenceWriteCommitted,
            DiagnosticCategory.Persistence,
            DiagnosticSeverity.Information,
            burst: 0,
            "a durable write completed atomically"),
        new DiagnosticCode(
            GenerationAttemptRejected,
            DiagnosticCategory.Generation,
            DiagnosticSeverity.Warning,
            burst: 8,
            "a generation attempt was rejected; attempts are bounded and retried"),
        new DiagnosticCode(
            SimulationInvariantViolated,
            DiagnosticCategory.Simulation,
            DiagnosticSeverity.Error,
            burst: 0,
            "a simulation invariant diverged; the run ends safely rather than continuing"),
        new DiagnosticCode(
            EncounterSpawnQueued,
            DiagnosticCategory.Encounter,
            DiagnosticSeverity.Information,
            burst: 16,
            "an authored spawn was capacity-blocked and queued rather than cancelled"),
        new DiagnosticCode(
            PresentationFallbackUsed,
            DiagnosticCategory.Presentation,
            DiagnosticSeverity.Warning,
            burst: 1,
            "a noncritical presentation resource was missing or saturated; the documented fallback was used"),
        new DiagnosticCode(
            InterfaceActionRejected,
            DiagnosticCategory.Interface,
            // Deliberately Information. Doc 90: "Player-facing expected failures such as
            // unaffordable purchase are not error logs."
            DiagnosticSeverity.Information,
            burst: 4,
            "a player-facing action was rejected for an expected reason, such as an unaffordable purchase"),
        new DiagnosticCode(
            PerformanceBudgetExceeded,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            burst: 4,
            "a measured frame or subsystem exceeded its allocation"),
        new DiagnosticCode(
            UncleanShutdownDetected,
            DiagnosticCategory.Shutdown,
            DiagnosticSeverity.Warning,
            burst: 0,
            "the previous session did not shut down cleanly"));

    /// <summary>Every registered code, in registration order.</summary>
    internal static ImmutableArray<DiagnosticCode> All => Registered;

    /// <summary>Looks up a registered code.</summary>
    /// <remarks>
    /// An unregistered code is a violated build invariant, not an expected rejection: the
    /// registry is explicit precisely so a missing entry is caught rather than logged.
    /// </remarks>
    internal static DiagnosticCode Require(string code)
    {
        DiagnosticCode? found = Find(code);
        return found ?? throw new InvalidOperationException(
            "diagnostic code '" + code + "' is not registered in DiagnosticCatalog. Doc 90 requires a "
            + "stable event code per record, and doc 100 § C# project standards requires an explicit "
            + "registry so a missing one is a build-time rather than a run-time surprise.");
    }

    /// <summary>Looks up a registered code, or null.</summary>
    internal static DiagnosticCode? Find(string code)
    {
        foreach (DiagnosticCode registered in Registered)
        {
            if (string.Equals(registered.Code, code, StringComparison.Ordinal))
            {
                return registered;
            }
        }

        return null;
    }

    /// <summary>The stable wire name of a category, as it appears in a log record.</summary>
    internal static string NameOf(DiagnosticCategory category)
    {
        return category switch
        {
            DiagnosticCategory.Bootstrap => "bootstrap",
            DiagnosticCategory.Content => "content",
            DiagnosticCategory.Persistence => "persistence",
            DiagnosticCategory.Generation => "generation",
            DiagnosticCategory.Simulation => "simulation",
            DiagnosticCategory.Encounter => "encounter",
            DiagnosticCategory.Presentation => "presentation",
            DiagnosticCategory.Interface => "interface",
            DiagnosticCategory.Performance => "performance",
            DiagnosticCategory.Shutdown => "shutdown",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, "unregistered category"),
        };
    }

    /// <summary>The stable wire name of a severity.</summary>
    internal static string NameOf(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Debug => "debug",
            DiagnosticSeverity.Information => "information",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Fatal => "fatal",
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "unregistered severity"),
        };
    }

    /// <summary>Every category that has at least one registered code.</summary>
    internal static IReadOnlySet<DiagnosticCategory> CoveredCategories()
    {
        HashSet<DiagnosticCategory> covered = new();
        foreach (DiagnosticCode code in Registered)
        {
            covered.Add(code.Category);
        }

        return covered;
    }
}
