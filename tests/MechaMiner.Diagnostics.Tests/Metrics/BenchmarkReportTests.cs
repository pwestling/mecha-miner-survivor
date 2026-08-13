using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaMiner.Diagnostics.Identity;
using MechaMiner.Diagnostics.Metrics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Diagnostics.Tests.Metrics;

/// <summary>
/// The metric registry, the CPU frame budget allocation, and the canonical
/// <c>SCH-OBS-002</c> report.
/// </summary>
/// <remarks>
/// Owner: <c>FND-008</c> (<c>TASK-FND-008-001</c>). Verification:
/// <c>VER-FND-008-001</c> through <c>VER-FND-008-007</c>. Requirements:
/// <c>TR-OBS-001</c>, <c>TR-FND-004</c>.
/// </remarks>
[TestFixture]
internal sealed class BenchmarkReportTests
{
    /// <summary>
    /// The CPU frame budget allocation is exactly doc 90's table, and its parts sum to the
    /// 16.67 ms frame TDR-003 requires.
    /// </summary>
    /// <remarks>
    /// The total is the assertion that matters. A transcription error in a budget table is
    /// silent: every individual number looks plausible, and only the sum gives it away.
    /// </remarks>
    [Test]
    public void TheFrameBudgetIsDocNinetysTableAndItsPartsSumToTheFrame()
    {
        Dictionary<string, double> expected = new(StringComparer.Ordinal)
        {
            [FrameBudget.InputAndCommands] = 0.40,
            [FrameBudget.Simulation] = 5.00,
            [FrameBudget.SnapshotAndSync] = 1.00,
            [FrameBudget.CrowdActorVfx] = 2.00,
            [FrameBudget.HudAndUi] = 1.00,
            [FrameBudget.AudioAndHaptics] = 0.40,
            [FrameBudget.EngineAndRender] = 3.00,
            [FrameBudget.UnallocatedMargin] = 3.87,
        };

        Tolerance cent = Tolerance.Named(
            "frame-budget-hundredth-millisecond",
            0.005,
            "doc 90 states every allocation to two decimal places, so a comparison only needs to "
            + "distinguish hundredths of a millisecond; a tighter bound would fail on binary "
            + "double representation alone");

        Expect.Multiple(() =>
        {
            Assert.That(FrameBudget.All, Has.Length.EqualTo(expected.Count));
            foreach (FrameBudgetArea area in FrameBudget.All)
            {
                Assert.That(expected.ContainsKey(area.Id), Is.True, "unexpected budget area " + area.Id);
                NumericAssert.AreEqualWithin(expected[area.Id], area.P95Milliseconds, cent, area.Id);
            }

            NumericAssert.AreEqualWithin(
                FrameBudget.FrameMilliseconds,
                FrameBudget.TotalMilliseconds(),
                cent,
                "the sum of every allocation row");
            NumericAssert.AreEqualWithin(
                12.80,
                FrameBudget.AllocatedMilliseconds(),
                cent,
                "the sum of every row except the unallocated margin");
            Assert.That(
                FrameBudget.Require(FrameBudget.UnallocatedMargin).P95Milliseconds,
                Is.GreaterThan(FrameBudget.RebalanceMarginMilliseconds),
                "doc 90 lets an agent rebalance sub-budgets only while at least 2.0 ms of measured "
                + "p95 margin remains, so the unallocated margin must start above it");
        });
    }

    /// <summary>The GPU and memory targets are doc 90's numbers.</summary>
    [Test]
    public void TheGpuAndMemoryTargetsAreDocNinetysNumbers()
    {
        Expect.Multiple(() =>
        {
            Assert.That(FrameBudget.GpuP95Milliseconds, Is.EqualTo(14.0));
            Assert.That(FrameBudget.GpuP99Milliseconds, Is.EqualTo(18.0));
            Assert.That(FrameBudget.WorkingSetBytes, Is.EqualTo(2560L * 1024 * 1024), "2.5 GiB");
            Assert.That(FrameBudget.ManagedHeapBytes, Is.EqualTo(256L * 1024 * 1024), "256 MiB");
            Assert.That(FrameBudget.AllocationBytesPerFrame, Is.EqualTo(1024), "1 KiB per frame");
            Assert.That(FrameBudget.TransitionPeakBytes, Is.EqualTo(3584L * 1024 * 1024), "3.5 GiB");
            Assert.That(FrameBudget.RecoveryArtifactBytes, Is.EqualTo(16L * 1024 * 1024), "16 MiB");
        });
    }

    /// <summary>
    /// Every registered metric has a unique ID, a stable prefix, a unit that matches its kind,
    /// an owning work package, and a budget area that resolves when it declares one.
    /// </summary>
    [Test]
    public void TheMetricRegistryIsConsistent()
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<string> problems = new();

        foreach (MetricDescriptor descriptor in MetricRegistry.All)
        {
            if (!seen.Add(descriptor.Id))
            {
                problems.Add(descriptor.Id + ": duplicate metric ID");
            }

            if (!descriptor.Id.StartsWith(MetricRegistry.IdPrefix, StringComparison.Ordinal))
            {
                problems.Add(descriptor.Id + ": ID does not carry the registered prefix");
            }

            string expectedUnit = descriptor.Kind switch
            {
                MetricKind.Duration => "ms",
                MetricKind.Count => "count",
                _ => "bytes",
            };
            if (!string.Equals(descriptor.Unit, expectedUnit, StringComparison.Ordinal))
            {
                problems.Add(descriptor.Id + ": unit '" + descriptor.Unit + "' does not match its kind");
            }

            if (descriptor.OwningWorkPackage.Length == 0)
            {
                problems.Add(descriptor.Id + ": no owning work package, so an empty column would be unattributable");
            }

            if (descriptor.BudgetArea.Length > 0 && FrameBudget.Find(descriptor.BudgetArea) is null)
            {
                problems.Add(descriptor.Id + ": budget area '" + descriptor.BudgetArea + "' does not resolve");
            }
        }

        Assert.That(problems, Is.Empty);
    }

    /// <summary>
    /// Every allocated frame budget area has at least one metric that rolls up to it, so no
    /// allocation is unmeasurable by construction.
    /// </summary>
    [Test]
    public void EveryAllocatedBudgetAreaHasAMetricThatRollsUpToIt()
    {
        List<string> uncovered = new();
        foreach (FrameBudgetArea area in FrameBudget.All)
        {
            if (string.Equals(area.Id, FrameBudget.UnallocatedMargin, StringComparison.Ordinal))
            {
                // The margin is the remainder, not a measured area.
                continue;
            }

            bool covered = false;
            foreach (MetricDescriptor descriptor in MetricRegistry.All)
            {
                if (string.Equals(descriptor.BudgetArea, area.Id, StringComparison.Ordinal))
                {
                    covered = true;
                    break;
                }
            }

            if (!covered)
            {
                uncovered.Add(area.Id + " (" + area.Area + ")");
            }
        }

        Assert.That(uncovered, Is.Empty);
    }

    /// <summary>An unregistered metric or budget area throws rather than reaching a report.</summary>
    [Test]
    public void AnUnregisteredMetricOrBudgetAreaThrows()
    {
        BenchmarkReportBuilder builder = new("PERF-04");

        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<InvalidOperationException>(() => builder.Record("MET-9999", 1.0)).Message,
                Does.Contain("MET-9999"));
            Assert.That(
                Expect.Throws<InvalidOperationException>(() => FrameBudget.Require("BDG-CPU-999")).Message,
                Does.Contain("BDG-CPU-999"));
        });
    }

    /// <summary>Nearest-rank percentiles return values that were actually observed.</summary>
    [Test]
    public void PercentilesUseNearestRankAndReturnObservedValues()
    {
        double[] sorted = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        Expect.Multiple(() =>
        {
            Assert.That(BenchmarkReportBuilder.Percentile(sorted, 50), Is.EqualTo(5));
            Assert.That(BenchmarkReportBuilder.Percentile(sorted, 95), Is.EqualTo(10));
            Assert.That(BenchmarkReportBuilder.Percentile(sorted, 99), Is.EqualTo(10));
            Assert.That(BenchmarkReportBuilder.Percentile(sorted, 0), Is.EqualTo(1));
            Assert.That(BenchmarkReportBuilder.Percentile(sorted, 100), Is.EqualTo(10));
            Assert.That(
                BenchmarkReportBuilder.Percentile(Array.Empty<double>(), 95),
                Is.EqualTo(0),
                "an empty sample has no percentile; the report marks the metric unmeasured instead");
        });
    }

    /// <summary>
    /// A sample CPU, count, and allocation report is produced, and its field order is stable
    /// across two independent builds of the same samples, including when the samples are
    /// recorded in a different order the second time.
    /// </summary>
    /// <remarks>
    /// This is <c>FND-008</c>'s completion gate. The reversed-recording-order half is the part
    /// that makes it a real stability claim: a report whose order came from the caller's
    /// recording sequence would pass a naive two-run comparison and still diff noisily against
    /// a stored baseline produced by a different caller.
    /// </remarks>
    [Test]
    public void ASampleReportIsProducedWithAFieldOrderStableAcrossTwoRuns()
    {
        string first = MechaMiner.Diagnostics.DiagnosticsJsonContext.Serialize(SampleReport(reverseOrder: false));
        string second = MechaMiner.Diagnostics.DiagnosticsJsonContext.Serialize(SampleReport(reverseOrder: true));

        string directory = Path.Combine(TestArtifacts.RepositoryRoot, "artifacts", "benchmark");
        Directory.CreateDirectory(directory);
        string firstPath = Path.Combine(directory, "sample-report.json");
        string secondPath = Path.Combine(directory, "sample-report-second-run.json");
        File.WriteAllText(firstPath, first);
        File.WriteAllText(secondPath, second);
        File.WriteAllText(Path.Combine(directory, "frame-budget.txt"), FrameBudget.ToTable());

        TestContext.Progress.WriteLine("sample report: " + TestArtifacts.Relative(firstPath));
        TestContext.Progress.WriteLine("second run:    " + TestArtifacts.Relative(secondPath));

        BenchmarkReport parsed = MechaMiner.Diagnostics.DiagnosticsJsonContext.DeserializeBenchmarkReport(first);

        Expect.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first), "field order and every value must be identical across runs");
            Assert.That(
                MechaMiner.Diagnostics.DiagnosticsJsonContext.Serialize(parsed),
                Is.EqualTo(first),
                "the report round-trips byte-exactly");

            int schema = first.IndexOf("\"schema\":", StringComparison.Ordinal);
            int scenario = first.IndexOf("\"scenario\":", StringComparison.Ordinal);
            int device = first.IndexOf("\"device\":", StringComparison.Ordinal);
            int sampling = first.IndexOf("\"sampling\":", StringComparison.Ordinal);
            int distributions = first.IndexOf("\"distributions\":", StringComparison.Ordinal);
            int counters = first.IndexOf("\"counters\":", StringComparison.Ordinal);
            int allocations = first.IndexOf("\"allocations\":", StringComparison.Ordinal);
            int budgets = first.IndexOf("\"budgets\":", StringComparison.Ordinal);
            int worstFrame = first.IndexOf("\"worst_frame\":", StringComparison.Ordinal);
            // The last occurrence, not the first: a budget line and an allocation each carry
            // their own verdict, so IndexOf would find a nested one and report the top-level
            // field as appearing before the collection that contains it.
            int verdict = first.LastIndexOf("\"verdict\":", StringComparison.Ordinal);
            Assert.That(
                new[]
                {
                    schema, scenario, device, sampling, distributions, counters, allocations, budgets,
                    worstFrame, verdict,
                },
                Is.Ordered.Ascending,
                "field order is declaration order");

            // The gate names CPU, count, and allocation explicitly.
            Assert.That(parsed.Distributions, Is.Not.Empty, "CPU distributions");
            Assert.That(parsed.Counters, Is.Not.Empty, "counts");
            Assert.That(parsed.Allocations, Is.Not.Empty, "allocations");
            Assert.That(parsed.Budgets, Has.Count.EqualTo(FrameBudget.All.Length));
            Assert.That(parsed.BuildIdentity, Is.EqualTo(BuildIdentity.IdentityLine));
            Assert.That(parsed.Sampling.PercentileMethod, Is.EqualTo(BenchmarkReportBuilder.PercentileMethod));
        });
    }

    /// <summary>
    /// A budget area with no producing metric is reported as unmeasured and names its owner,
    /// never as a measured zero, and the report verdict is not a pass on no measurement.
    /// </summary>
    [Test]
    public void AnUnmeasuredBudgetAreaIsReportedAsUnmeasuredWithItsOwner()
    {
        BenchmarkReport report = new BenchmarkReportBuilder("PERF-01").Build();

        BenchmarkBudgetLine? crowd = null;
        foreach (BenchmarkBudgetLine line in report.Budgets)
        {
            if (string.Equals(line.BudgetArea, FrameBudget.CrowdActorVfx, StringComparison.Ordinal))
            {
                crowd = line;
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(crowd, Is.Not.Null);
            Assert.That(crowd!.Verdict, Is.EqualTo("unmeasured"));
            Assert.That(crowd.MeasuredP95Ms, Is.EqualTo(-1), "an unmeasured area is not a measured zero");
            Assert.That(crowd.OwningWorkPackage, Is.EqualTo("PRE-004"));
            Assert.That(
                report.Verdict,
                Is.EqualTo("incomplete"),
                "a report with no measured frame must not read as a pass");
        });
    }

    /// <summary>
    /// A measured p95 above its allocation fails that budget line, is listed in the exceeded
    /// set, and fails the whole report. A value at the allocation passes.
    /// </summary>
    [Test]
    public void ExceedingABudgetFailsTheLineTheExceededSetAndTheReport()
    {
        BenchmarkReportBuilder over = new("PERF-04");
        BenchmarkReportBuilder at = new("PERF-04");
        for (int frame = 0; frame < 100; frame++)
        {
            over.Record("MET-0001", 16.0);
            over.Record("MET-0005", 5.01);
            at.Record("MET-0001", 16.0);
            at.Record("MET-0005", 5.00);
        }

        BenchmarkReport overReport = over.Build();
        BenchmarkReport atReport = at.Build();

        Expect.Multiple(() =>
        {
            Assert.That(overReport.Verdict, Is.EqualTo("fail"));
            Assert.That(overReport.Exceeded, Does.Contain(FrameBudget.Simulation));
            Assert.That(atReport.Verdict, Is.EqualTo("pass"), "a value exactly at the allocation passes");
            Assert.That(atReport.Exceeded, Is.Empty);
        });
    }

    /// <summary>An allocation above its per-frame ceiling fails, judged on the worst frame.</summary>
    [Test]
    public void AnAllocationAboveItsCeilingFailsOnTheWorstFrame()
    {
        BenchmarkReportBuilder builder = new("PERF-03");
        for (int frame = 0; frame < 100; frame++)
        {
            builder.Record("MET-0001", 16.0);

            // Every frame but one is comfortably inside the ceiling. A mean would hide the
            // outlier; a per-frame ceiling must not.
            builder.Record("MET-0016", frame == 42 ? FrameBudget.AllocationBytesPerFrame + 1 : 64);
        }

        BenchmarkReport report = builder.Build();

        BenchmarkAllocation? allocation = null;
        foreach (BenchmarkAllocation candidate in report.Allocations)
        {
            if (string.Equals(candidate.MetricId, "MET-0016", StringComparison.Ordinal))
            {
                allocation = candidate;
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(allocation, Is.Not.Null);
            Assert.That(allocation!.Verdict, Is.EqualTo("fail"));
            Assert.That(allocation.CeilingBytes, Is.EqualTo(FrameBudget.AllocationBytesPerFrame));
            Assert.That(report.Verdict, Is.EqualTo("fail"));
        });
    }

    /// <summary>The frame budget renders as canonical ordered reviewable text whose total is stated.</summary>
    [Test]
    public void TheFrameBudgetRendersAsCanonicalReviewableText()
    {
        string table = FrameBudget.ToTable();

        Expect.Multiple(() =>
        {
            Assert.That(table, Is.EqualTo(FrameBudget.ToTable()), "rendering is stable");
            Assert.That(table, Does.Contain("BDG-CPU-002\t5.00"));
            Assert.That(
                table,
                Does.Contain(
                    "total\t" + FrameBudget.FrameMilliseconds.ToString("0.00", CultureInfo.InvariantCulture)));
        });
    }

    private static BenchmarkReport SampleReport(bool reverseOrder)
    {
        BenchmarkReportBuilder builder = new("PERF-04")
        {
            WarmupFrames = 600,
        };
        builder.Device.DeviceClass = "ci-software-raster";
        builder.Device.Adapter = "llvmpipe";
        builder.Device.Driver = "Vulkan 1.4 lavapipe";
        builder.Device.OperatingSystem = "linux";
        builder.Settings.RenderingMethod = "mobile";
        builder.Settings.Resolution = "1280x800";
        builder.Settings.Quality = "default";
        builder.Settings.MasterSeed = "0";
        builder.Settings.InputScript = "sample-report-fixture";
        builder.WorstFrame.FrameIndex = 87;
        builder.WorstFrame.WallMs = 15.20;
        builder.WorstFrame.Markers.Add("boss.arrival");
        builder.WorstFrame.Markers.Add("gc.generation.0");

        // Deterministic, non-random samples: doc 91 forbids an unseeded generator, and a fixed
        // pattern makes the emitted document a reviewable artifact rather than noise.
        string[] metricOrder =
        {
            "MET-0001", "MET-0002", "MET-0004", "MET-0005", "MET-0006",
            "MET-0011", "MET-0012", "MET-0015", "MET-0016", "MET-0018",
        };
        if (reverseOrder)
        {
            Array.Reverse(metricOrder);
        }

        for (int frame = 0; frame < 100; frame++)
        {
            foreach (string metric in metricOrder)
            {
                builder.Record(metric, SampleValue(metric, frame));
            }
        }

        return builder.Build();
    }

    private static double SampleValue(string metricId, int frame)
    {
        int wobble = frame % 10;
        return metricId switch
        {
            "MET-0001" => 13.00 + (wobble * 0.10),
            "MET-0002" => 9.00 + (wobble * 0.08),
            "MET-0004" => 0.20 + (wobble * 0.01),
            "MET-0005" => 3.50 + (wobble * 0.05),
            "MET-0006" => 0.60 + (wobble * 0.02),
            "MET-0011" => wobble < 9 ? 1 : 2,
            "MET-0012" => 400 + (wobble * 10),
            "MET-0015" => 900 + (wobble * 5),
            "MET-0016" => 256 + (wobble * 16),
            "MET-0018" => 96L * 1024 * 1024,
            _ => 0,
        };
    }
}
