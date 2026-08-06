using System.Collections.Generic;

namespace MechaMiner.Diagnostics.Metrics;

/// <summary>
/// <c>SCH-OBS-002</c>, the canonical performance report.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-008</c> (<c>TASK-FND-008-001</c>). Required identity,
/// quoted from <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Schema
/// registry: "scenario/device/settings, distributions, subsystem counters, budgets". The
/// contents follow <c>docs/technical/90-performance-diagnostics-and-observability.md</c>
/// § Frame metrics and § Performance regression policy. Requirements: <c>TR-OBS-001</c>,
/// <c>TR-FND-004</c>.
/// </para>
/// <para>
/// Field order is declaration order, because source-generated <c>System.Text.Json</c> writes
/// members in the order they are declared. That is what makes two reports of the same run
/// byte-identical and therefore diffable, which doc 90 § Performance regression policy needs
/// in order to compare a build against a stored baseline at all.
/// </para>
/// <para>
/// The report carries build identity so a stored baseline can never be compared against a
/// different build without that being visible, which is doc 90's first response-ladder step:
/// "Confirm the correct build, device, warm-up, scenario, content hash, settings, and
/// instrumentation overhead."
/// </para>
/// <para>
/// <c>FND-008</c> owns this format. The <c>PERF-01</c> to <c>PERF-08</c> scenarios that fill
/// it, and the <c>benchmark</c> verb that runs them, are <c>QUA-005</c>'s.
/// </para>
/// </remarks>
internal sealed class BenchmarkReport
{
    /// <summary>Stable schema identity. Always <c>SCH-OBS-002</c>.</summary>
    public string Schema { get; set; } = "SCH-OBS-002";

    /// <summary>Version of this document's shape.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>The accepted scenario ID, for example <c>PERF-04</c> or <c>WB-03</c>.</summary>
    public string Scenario { get; set; } = string.Empty;

    /// <summary>The canonical <c>SCH-BLD-001</c> identity line of the build under test.</summary>
    public string BuildIdentity { get; set; } = string.Empty;

    /// <summary>The content bundle hash, or the declared unavailable status.</summary>
    public string ContentIdentity { get; set; } = string.Empty;

    /// <summary>Device, driver, and operating-system metadata.</summary>
    public BenchmarkDevice Device { get; set; } = new();

    /// <summary>Settings the scenario pinned.</summary>
    public BenchmarkSettings Settings { get; set; } = new();

    /// <summary>How the capture was bounded.</summary>
    public BenchmarkSampling Sampling { get; set; } = new();

    /// <summary>Duration distributions, one per registered duration metric, in registry order.</summary>
    public List<BenchmarkDistribution> Distributions { get; set; } = new();

    /// <summary>Population and queue counters, in registry order.</summary>
    public List<BenchmarkCounter> Counters { get; set; } = new();

    /// <summary>Allocation and memory figures, in registry order.</summary>
    public List<BenchmarkAllocation> Allocations { get; set; } = new();

    /// <summary>Each frame budget area with its allocation, its measured p95, and a verdict.</summary>
    public List<BenchmarkBudgetLine> Budgets { get; set; } = new();

    /// <summary>The worst frame observed, with the markers active during it.</summary>
    public BenchmarkWorstFrame WorstFrame { get; set; } = new();

    /// <summary><c>pass</c>, <c>fail</c>, or <c>incomplete</c>.</summary>
    public string Verdict { get; set; } = string.Empty;

    /// <summary>Every budget area whose measured p95 exceeded its allocation, in report order.</summary>
    public List<string> Exceeded { get; set; } = new();
}

/// <summary>Device, driver, and operating-system metadata.</summary>
internal sealed class BenchmarkDevice
{
    /// <summary>The device class, for example <c>steam-deck-retail</c> or <c>ci-software-raster</c>.</summary>
    public string DeviceClass { get; set; } = string.Empty;

    /// <summary>The build-time platform identifier of the binary under test.</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>The graphics adapter name as the renderer reports it.</summary>
    public string Adapter { get; set; } = string.Empty;

    /// <summary>The graphics driver or API version.</summary>
    public string Driver { get; set; } = string.Empty;

    /// <summary>The operating-system description.</summary>
    public string OperatingSystem { get; set; } = string.Empty;
}

/// <summary>Settings the scenario pinned.</summary>
internal sealed class BenchmarkSettings
{
    /// <summary>The rendering method, which must be the pinned Mobile renderer for a device capture.</summary>
    public string RenderingMethod { get; set; } = string.Empty;

    /// <summary>Resolution as <c>WxH</c>.</summary>
    public string Resolution { get; set; } = string.Empty;

    /// <summary>The quality preset.</summary>
    public string Quality { get; set; } = string.Empty;

    /// <summary>The master seed as unsigned decimal text.</summary>
    public string MasterSeed { get; set; } = string.Empty;

    /// <summary>The input script identity the scenario replayed.</summary>
    public string InputScript { get; set; } = string.Empty;
}

/// <summary>How the capture was bounded.</summary>
internal sealed class BenchmarkSampling
{
    /// <summary>Frames discarded before measurement began.</summary>
    public int WarmupFrames { get; set; }

    /// <summary>Frames included in the distributions.</summary>
    public int MeasuredFrames { get; set; }

    /// <summary>
    /// The percentile method, recorded so a stored baseline cannot be compared across a change
    /// of method without that being visible.
    /// </summary>
    public string PercentileMethod { get; set; } = string.Empty;
}

/// <summary>One duration metric's distribution.</summary>
internal sealed class BenchmarkDistribution
{
    /// <summary>The registered metric ID.</summary>
    public string MetricId { get; set; } = string.Empty;

    /// <summary>The profiler marker name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The unit, always <c>ms</c> for a duration.</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>The frame budget area this metric rolls up to, or empty.</summary>
    public string BudgetArea { get; set; } = string.Empty;

    /// <summary>Number of samples.</summary>
    public int Samples { get; set; }

    /// <summary>Median.</summary>
    public double P50 { get; set; }

    /// <summary>95th percentile, which is the value every budget is stated against.</summary>
    public double P95 { get; set; }

    /// <summary>99th percentile.</summary>
    public double P99 { get; set; }

    /// <summary>Maximum observed.</summary>
    public double Max { get; set; }
}

/// <summary>One population or queue counter.</summary>
internal sealed class BenchmarkCounter
{
    /// <summary>The registered metric ID.</summary>
    public string MetricId { get; set; } = string.Empty;

    /// <summary>The profiler marker name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Number of samples.</summary>
    public int Samples { get; set; }

    /// <summary>Mean, rounded to two places so the value is stable text.</summary>
    public double Mean { get; set; }

    /// <summary>The high-water mark, which is what a capacity contract is judged against.</summary>
    public long HighWaterMark { get; set; }
}

/// <summary>One allocation or memory figure.</summary>
internal sealed class BenchmarkAllocation
{
    /// <summary>The registered metric ID.</summary>
    public string MetricId { get; set; } = string.Empty;

    /// <summary>The profiler marker name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The measured value in bytes: per frame for an allocation, peak for a memory figure.</summary>
    public long Bytes { get; set; }

    /// <summary>The doc 90 ceiling in bytes.</summary>
    public long CeilingBytes { get; set; }

    /// <summary><c>pass</c> or <c>fail</c>.</summary>
    public string Verdict { get; set; } = string.Empty;
}

/// <summary>One frame budget area with its allocation, its measured p95, and a verdict.</summary>
internal sealed class BenchmarkBudgetLine
{
    /// <summary>The frame budget area ID.</summary>
    public string BudgetArea { get; set; } = string.Empty;

    /// <summary>The area name from doc 90's table.</summary>
    public string Area { get; set; } = string.Empty;

    /// <summary>The allocated p95 in milliseconds.</summary>
    public double AllocatedP95Ms { get; set; }

    /// <summary>The measured p95 in milliseconds, or <c>-1</c> when nothing produced the metric yet.</summary>
    public double MeasuredP95Ms { get; set; } = -1;

    /// <summary><c>pass</c>, <c>fail</c>, or <c>unmeasured</c>.</summary>
    public string Verdict { get; set; } = string.Empty;

    /// <summary>The work package that will produce the measurement, when it is unmeasured.</summary>
    public string OwningWorkPackage { get; set; } = string.Empty;
}

/// <summary>The worst frame observed and the markers active during it.</summary>
internal sealed class BenchmarkWorstFrame
{
    /// <summary>The frame index within the measured window, or <c>-1</c> when nothing was measured.</summary>
    public int FrameIndex { get; set; } = -1;

    /// <summary>The frame's wall duration in milliseconds.</summary>
    public double WallMs { get; set; }

    /// <summary>Markers doc 90 requires on a worst-frame timeline: boss, spawn, save, UI, GC, shader.</summary>
    public List<string> Markers { get; set; } = new();
}
