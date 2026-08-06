using System;
using System.Collections.Immutable;

namespace MechaMiner.Diagnostics.Metrics;

/// <summary>What a metric measures.</summary>
/// <remarks>
/// Doc 90 § Frame metrics requires per-frame captures of durations, populations and
/// high-water marks, and allocations and garbage collection. The kind is explicit because a
/// report aggregates each differently: a duration reports percentiles, a count reports a
/// high-water mark, and an allocation reports a per-frame rate against a ceiling.
/// </remarks>
internal enum MetricKind
{
    /// <summary>A wall or CPU duration, reported as a percentile distribution.</summary>
    Duration,

    /// <summary>A population or queue depth, reported with its high-water mark.</summary>
    Count,

    /// <summary>Bytes allocated, reported per frame against a ceiling.</summary>
    Allocation,

    /// <summary>Bytes resident, reported as a peak against a ceiling.</summary>
    Memory,
}

/// <summary>One registered metric or profiler marker.</summary>
internal sealed class MetricDescriptor
{
    internal MetricDescriptor(
        string id,
        string name,
        MetricKind kind,
        string unit,
        string budgetArea,
        string owningWorkPackage)
    {
        Id = id;
        Name = name;
        Kind = kind;
        Unit = unit;
        BudgetArea = budgetArea;
        OwningWorkPackage = owningWorkPackage;
    }

    /// <summary>The stable metric ID, never reused or renumbered.</summary>
    internal string Id { get; }

    /// <summary>The profiler marker name, which is also the report column name.</summary>
    internal string Name { get; }

    /// <summary>What the metric measures.</summary>
    internal MetricKind Kind { get; }

    /// <summary>The unit: <c>ms</c>, <c>count</c>, or <c>bytes</c>.</summary>
    internal string Unit { get; }

    /// <summary>
    /// The <see cref="FrameBudget"/> area this metric rolls up to, or empty when it is a
    /// count or a memory figure with no CPU allocation.
    /// </summary>
    internal string BudgetArea { get; }

    /// <summary>The work package that will produce values for this metric.</summary>
    /// <remarks>
    /// Recorded so an empty column in a report is attributable rather than mysterious. At
    /// <c>FND-008</c> almost every metric has no producer yet, and naming the owner is the
    /// difference between "not implemented" and "measured zero".
    /// </remarks>
    internal string OwningWorkPackage { get; }
}

/// <summary>
/// The explicit registry of profiler markers and metrics.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-008</c> (<c>TASK-FND-008-001</c>). Authority:
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Frame metrics
/// and § Target-device frame budget. Requirements: <c>TR-OBS-001</c>.
/// </para>
/// <para>
/// A literal array, like every other registry here: doc 100 requires explicit registries and
/// doc 114 forbids reflection-based discovery. Every duration metric names the frame budget
/// area it rolls up to, so a report can compare a measured subsystem against its allocation
/// without a second mapping table that could disagree.
/// </para>
/// <para>
/// The set registered here is the one the frame budget itself implies plus the frame-level
/// captures doc 90 names first. Each later package appends the metrics it produces; the
/// registry is the contract that stops two packages from measuring the same thing under two
/// names.
/// </para>
/// </remarks>
internal static class MetricRegistry
{
    /// <summary>The prefix every metric ID carries.</summary>
    internal const string IdPrefix = "MET-";

    private static readonly ImmutableArray<MetricDescriptor> Registered = ImmutableArray.Create(
        new MetricDescriptor(
            "MET-0001",
            "frame.wall",
            MetricKind.Duration,
            "ms",
            budgetArea: string.Empty,
            owningWorkPackage: "QUA-005"),
        new MetricDescriptor("MET-0002", "frame.cpu", MetricKind.Duration, "ms", string.Empty, "QUA-005"),
        new MetricDescriptor("MET-0003", "frame.gpu", MetricKind.Duration, "ms", string.Empty, "QUA-005"),
        new MetricDescriptor(
            "MET-0004",
            "input.and.commands",
            MetricKind.Duration,
            "ms",
            FrameBudget.InputAndCommands,
            "SIM-004"),
        new MetricDescriptor(
            "MET-0005",
            "simulation.tick",
            MetricKind.Duration,
            "ms",
            FrameBudget.Simulation,
            "SIM-009"),
        new MetricDescriptor(
            "MET-0006",
            "snapshot.publish",
            MetricKind.Duration,
            "ms",
            FrameBudget.SnapshotAndSync,
            "SIM-007"),
        new MetricDescriptor(
            "MET-0007",
            "presentation.crowd",
            MetricKind.Duration,
            "ms",
            FrameBudget.CrowdActorVfx,
            "PRE-004"),
        new MetricDescriptor("MET-0008", "ui.update", MetricKind.Duration, "ms", FrameBudget.HudAndUi, "UI-003"),
        new MetricDescriptor(
            "MET-0009",
            "audio.events",
            MetricKind.Duration,
            "ms",
            FrameBudget.AudioAndHaptics,
            "AUD-001"),
        new MetricDescriptor(
            "MET-0010",
            "engine.render.submit",
            MetricKind.Duration,
            "ms",
            FrameBudget.EngineAndRender,
            "PRE-007"),
        new MetricDescriptor(
            "MET-0011",
            "simulation.catchup.ticks",
            MetricKind.Count,
            "count",
            string.Empty,
            "SIM-001"),
        new MetricDescriptor("MET-0012", "population.enemies", MetricKind.Count, "count", string.Empty, "ENC-002"),
        new MetricDescriptor("MET-0013", "population.projectiles", MetricKind.Count, "count", string.Empty, "COM-002"),
        new MetricDescriptor("MET-0014", "queue.spawn.highwater", MetricKind.Count, "count", string.Empty, "ENC-003"),
        new MetricDescriptor("MET-0015", "render.draw.calls", MetricKind.Count, "count", string.Empty, "PRE-007"),
        new MetricDescriptor(
            "MET-0016",
            "allocation.managed.per.frame",
            MetricKind.Allocation,
            "bytes",
            string.Empty,
            "QUA-005"),
        new MetricDescriptor("MET-0017", "gc.collections", MetricKind.Count, "count", string.Empty, "QUA-005"),
        new MetricDescriptor("MET-0018", "memory.managed.heap", MetricKind.Memory, "bytes", string.Empty, "QUA-005"),
        new MetricDescriptor("MET-0019", "memory.working.set", MetricKind.Memory, "bytes", string.Empty, "QUA-005"),
        new MetricDescriptor("MET-0020", "audio.voices.active", MetricKind.Count, "count", string.Empty, "AUD-001"));

    /// <summary>Every registered metric, in registration order.</summary>
    internal static ImmutableArray<MetricDescriptor> All => Registered;

    /// <summary>Looks up a registered metric.</summary>
    internal static MetricDescriptor Require(string id)
    {
        MetricDescriptor? found = Find(id);
        return found ?? throw new InvalidOperationException(
            "metric '" + id + "' is not registered in MetricRegistry. Doc 90 § Frame metrics fixes what is "
            + "captured, and an unregistered metric would appear in a report with no owner and no unit.");
    }

    /// <summary>Looks up a registered metric, or null.</summary>
    internal static MetricDescriptor? Find(string id)
    {
        foreach (MetricDescriptor descriptor in Registered)
        {
            if (string.Equals(descriptor.Id, id, StringComparison.Ordinal))
            {
                return descriptor;
            }
        }

        return null;
    }

    /// <summary>The stable wire name of a metric kind.</summary>
    internal static string NameOf(MetricKind kind)
    {
        return kind switch
        {
            MetricKind.Duration => "duration",
            MetricKind.Count => "count",
            MetricKind.Allocation => "allocation",
            MetricKind.Memory => "memory",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "unregistered metric kind"),
        };
    }
}
