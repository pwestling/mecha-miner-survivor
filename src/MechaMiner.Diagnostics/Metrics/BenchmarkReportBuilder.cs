using System;
using System.Collections.Generic;
using MechaMiner.Diagnostics.Identity;

namespace MechaMiner.Diagnostics.Metrics;

/// <summary>
/// Assembles a canonical <c>SCH-OBS-002</c> report from recorded samples.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-008</c> (<c>TASK-FND-008-001</c>).
/// </para>
/// <para>
/// Output order is fixed by <see cref="MetricRegistry"/> and <see cref="FrameBudget"/>, not by
/// the order a caller happened to record samples in. That is what makes the field order
/// stable: two runs that recorded the same samples in different orders still produce the same
/// document, so a diff against a stored baseline shows behaviour rather than bookkeeping.
/// </para>
/// <para>
/// Percentiles use the nearest-rank method on a sorted copy, and the method name is written
/// into the report. Doc 90 § Performance regression policy compares a build against a stored
/// baseline distribution, which is only meaningful if both were computed the same way; a
/// report that omitted the method could be compared across a silent change of it.
/// </para>
/// <para>
/// A metric with no recorded samples is reported as unmeasured and names the work package
/// that will produce it, rather than as a measured zero. At <c>FND-008</c> almost every metric
/// has no producer, and "unmeasured, owned by PRE-004" and "measured 0.00 ms" are completely
/// different claims.
/// </para>
/// </remarks>
internal sealed class BenchmarkReportBuilder
{
    /// <summary>The percentile method name written into every report this builder produces.</summary>
    internal const string PercentileMethod = "nearest-rank";

    private readonly Dictionary<string, List<double>> _samples = new(StringComparer.Ordinal);
    private readonly string _scenario;

    /// <summary>Starts a report for one accepted scenario ID.</summary>
    internal BenchmarkReportBuilder(string scenario)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);
        _scenario = scenario;
    }

    /// <summary>Device metadata to record.</summary>
    internal BenchmarkDevice Device { get; } = new();

    /// <summary>Settings to record.</summary>
    internal BenchmarkSettings Settings { get; } = new();

    /// <summary>Frames discarded before measurement began.</summary>
    internal int WarmupFrames { get; set; }

    /// <summary>The worst frame observed.</summary>
    internal BenchmarkWorstFrame WorstFrame { get; } = new();

    /// <summary>Records one sample for a registered metric.</summary>
    internal void Record(string metricId, double value)
    {
        MetricDescriptor descriptor = MetricRegistry.Require(metricId);
        if (!_samples.TryGetValue(descriptor.Id, out List<double>? values))
        {
            values = new List<double>();
            _samples[descriptor.Id] = values;
        }

        values.Add(value);
    }

    /// <summary>Builds the report.</summary>
    internal BenchmarkReport Build()
    {
        BuildManifest identity = BuildIdentity.Current;
        BenchmarkReport report = new()
        {
            Scenario = _scenario,
            BuildIdentity = identity.IdentityLine,
            ContentIdentity = identity.Content.BundleSha256.Length > 0
                ? identity.Content.BundleSha256
                : identity.Content.Status + ":" + identity.Content.OwningWorkPackage,
            Device = new BenchmarkDevice
            {
                DeviceClass = Device.DeviceClass,
                Platform = Device.Platform.Length > 0 ? Device.Platform : identity.Target.Platform,
                Adapter = Device.Adapter,
                Driver = Device.Driver,
                OperatingSystem = Device.OperatingSystem,
            },
            Settings = new BenchmarkSettings
            {
                RenderingMethod = Settings.RenderingMethod,
                Resolution = Settings.Resolution,
                Quality = Settings.Quality,
                MasterSeed = Settings.MasterSeed,
                InputScript = Settings.InputScript,
            },
            Sampling = new BenchmarkSampling
            {
                WarmupFrames = WarmupFrames,
                MeasuredFrames = SampleCount(MetricRegistry.Require("MET-0001").Id),
                PercentileMethod = PercentileMethod,
            },
            WorstFrame = new BenchmarkWorstFrame
            {
                FrameIndex = WorstFrame.FrameIndex,
                WallMs = Round(WorstFrame.WallMs),
            },
        };
        report.WorstFrame.Markers.AddRange(WorstFrame.Markers);

        Dictionary<string, double> measuredByArea = new(StringComparer.Ordinal);

        // Registry order, not recording order. That is the stability guarantee.
        foreach (MetricDescriptor descriptor in MetricRegistry.All)
        {
            List<double>? values = _samples.TryGetValue(descriptor.Id, out List<double>? found) ? found : null;
            switch (descriptor.Kind)
            {
                case MetricKind.Duration:
                    BenchmarkDistribution distribution = Distribution(descriptor, values);
                    report.Distributions.Add(distribution);
                    if (descriptor.BudgetArea.Length > 0 && distribution.Samples > 0)
                    {
                        measuredByArea[descriptor.BudgetArea] = distribution.P95;
                    }

                    break;

                case MetricKind.Count:
                    report.Counters.Add(Counter(descriptor, values));
                    break;

                case MetricKind.Allocation:
                case MetricKind.Memory:
                    report.Allocations.Add(Allocation(descriptor, values));
                    break;

                default:
                    throw new InvalidOperationException("unhandled metric kind " + descriptor.Kind);
            }
        }

        bool anyFailure = false;
        foreach (FrameBudgetArea area in FrameBudget.All)
        {
            BenchmarkBudgetLine line = new()
            {
                BudgetArea = area.Id,
                Area = area.Area,
                AllocatedP95Ms = area.P95Milliseconds,
            };

            if (measuredByArea.TryGetValue(area.Id, out double measured))
            {
                line.MeasuredP95Ms = Round(measured);
                line.Verdict = measured <= area.P95Milliseconds ? "pass" : "fail";
                if (measured > area.P95Milliseconds)
                {
                    anyFailure = true;
                    report.Exceeded.Add(area.Id);
                }
            }
            else
            {
                line.MeasuredP95Ms = -1;
                line.Verdict = "unmeasured";
                line.OwningWorkPackage = OwnerOf(area.Id);
            }

            report.Budgets.Add(line);
        }

        foreach (BenchmarkAllocation allocation in report.Allocations)
        {
            if (string.Equals(allocation.Verdict, "fail", StringComparison.Ordinal))
            {
                anyFailure = true;
            }
        }

        bool anyMeasurement = report.Sampling.MeasuredFrames > 0;
        report.Verdict = !anyMeasurement ? "incomplete" : anyFailure ? "fail" : "pass";
        return report;
    }

    /// <summary>
    /// The nearest-rank percentile of a sorted sample.
    /// </summary>
    /// <remarks>
    /// Nearest-rank rather than an interpolating method because every value it returns is a
    /// value that was actually observed. An interpolated p95 can be a duration no frame took,
    /// which makes a worst-frame investigation chase a number nothing produced.
    /// </remarks>
    internal static double Percentile(IReadOnlyList<double> sorted, double percentile)
    {
        ArgumentNullException.ThrowIfNull(sorted);
        if (sorted.Count == 0)
        {
            return 0;
        }

        if (percentile <= 0)
        {
            return sorted[0];
        }

        if (percentile >= 100)
        {
            return sorted[^1];
        }

        int rank = (int)Math.Ceiling(percentile / 100.0 * sorted.Count);
        int index = Math.Clamp(rank - 1, 0, sorted.Count - 1);
        return sorted[index];
    }

    private int SampleCount(string metricId)
    {
        return _samples.TryGetValue(metricId, out List<double>? values) ? values.Count : 0;
    }

    private static BenchmarkDistribution Distribution(MetricDescriptor descriptor, List<double>? values)
    {
        BenchmarkDistribution distribution = new()
        {
            MetricId = descriptor.Id,
            Name = descriptor.Name,
            Unit = descriptor.Unit,
            BudgetArea = descriptor.BudgetArea,
            Samples = values?.Count ?? 0,
        };

        if (values is null || values.Count == 0)
        {
            return distribution;
        }

        List<double> sorted = new(values);
        sorted.Sort();
        distribution.P50 = Round(Percentile(sorted, 50));
        distribution.P95 = Round(Percentile(sorted, 95));
        distribution.P99 = Round(Percentile(sorted, 99));
        distribution.Max = Round(sorted[^1]);
        return distribution;
    }

    private static BenchmarkCounter Counter(MetricDescriptor descriptor, List<double>? values)
    {
        BenchmarkCounter counter = new()
        {
            MetricId = descriptor.Id,
            Name = descriptor.Name,
            Samples = values?.Count ?? 0,
        };

        if (values is null || values.Count == 0)
        {
            return counter;
        }

        double total = 0;
        double highest = double.MinValue;
        foreach (double value in values)
        {
            total += value;
            if (value > highest)
            {
                highest = value;
            }
        }

        counter.Mean = Round(total / values.Count);
        counter.HighWaterMark = (long)Math.Round(highest, MidpointRounding.AwayFromZero);
        return counter;
    }

    private static BenchmarkAllocation Allocation(MetricDescriptor descriptor, List<double>? values)
    {
        long ceiling = CeilingFor(descriptor);
        BenchmarkAllocation allocation = new()
        {
            MetricId = descriptor.Id,
            Name = descriptor.Name,
            CeilingBytes = ceiling,
        };

        if (values is null || values.Count == 0)
        {
            allocation.Bytes = -1;
            allocation.Verdict = "unmeasured";
            return allocation;
        }

        // An allocation ceiling is per frame, so the figure judged is the worst frame rather
        // than the mean: doc 90 states the target as a per-frame aggregate.
        double highest = double.MinValue;
        foreach (double value in values)
        {
            if (value > highest)
            {
                highest = value;
            }
        }

        allocation.Bytes = (long)Math.Round(highest, MidpointRounding.AwayFromZero);
        allocation.Verdict = allocation.Bytes <= ceiling ? "pass" : "fail";
        return allocation;
    }

    private static long CeilingFor(MetricDescriptor descriptor)
    {
        return descriptor.Id switch
        {
            "MET-0016" => FrameBudget.AllocationBytesPerFrame,
            "MET-0018" => FrameBudget.ManagedHeapBytes,
            "MET-0019" => FrameBudget.WorkingSetBytes,
            _ => 0,
        };
    }

    private static string OwnerOf(string budgetAreaId)
    {
        foreach (MetricDescriptor descriptor in MetricRegistry.All)
        {
            if (string.Equals(descriptor.BudgetArea, budgetAreaId, StringComparison.Ordinal))
            {
                return descriptor.OwningWorkPackage;
            }
        }

        // The unallocated margin has no producing metric by design: it is what is left over.
        return string.Equals(budgetAreaId, FrameBudget.UnallocatedMargin, StringComparison.Ordinal)
            ? "not applicable: the margin is the remainder, not a measured area"
            : string.Empty;
    }

    /// <summary>
    /// Rounds to two decimals so a report is stable text.
    /// </summary>
    /// <remarks>
    /// Without it, a distribution value would serialize with full binary-double precision and
    /// two runs of the same capture could differ in the last digit, which would make a diff
    /// against a stored baseline noisy for no behavioural reason. Two decimals is finer than
    /// any budget in doc 90's table, which is stated to two.
    /// </remarks>
    private static double Round(double value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
