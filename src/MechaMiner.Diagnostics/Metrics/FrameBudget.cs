using System;
using System.Collections.Immutable;
using System.Globalization;

namespace MechaMiner.Diagnostics.Metrics;

/// <summary>One row of the CPU frame budget allocation.</summary>
internal sealed class FrameBudgetArea
{
    internal FrameBudgetArea(string id, string area, double p95Milliseconds)
    {
        Id = id;
        Area = area;
        P95Milliseconds = p95Milliseconds;
    }

    /// <summary>The stable ID this area is referenced by in metrics and reports.</summary>
    internal string Id { get; }

    /// <summary>The area name, quoted from doc 90's allocation table.</summary>
    internal string Area { get; }

    /// <summary>The p95 target in milliseconds.</summary>
    internal double P95Milliseconds { get; }
}

/// <summary>
/// Doc 90's CPU frame budget allocation, GPU targets, and memory targets, as data.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-008</c> (<c>TASK-FND-008-001</c>). Authority:
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Target-device
/// frame budget, and <c>TDR-003</c>, which fixes the 16.67 ms frame. Requirements:
/// <c>TR-OBS-001</c>, <c>TR-FND-004</c>.
/// </para>
/// <para>
/// One source, so no later <c>PERF-*</c> work re-derives a number from prose. Doc 90 says
/// agents "treat these allocations as failure thresholds from the first measurable
/// implementation", which only works if there is exactly one place the thresholds live.
/// </para>
/// <para>
/// The invariant that the parts sum to the whole is asserted by a test rather than trusted,
/// because a transcription error in a budget table is silent: every individual number looks
/// plausible and only the total gives it away.
/// </para>
/// <para>
/// <c>FND-008</c> supplies the allocation and the report format. It deliberately does not
/// implement <c>PERF-01</c> through <c>PERF-08</c>; those scenarios and their runner are
/// <c>QUA-005</c>'s, and the <c>benchmark</c> verb stays registered to that package.
/// </para>
/// </remarks>
internal static class FrameBudget
{
    /// <summary>The 60 FPS frame budget <c>TDR-003</c> requires, in milliseconds.</summary>
    internal const double FrameMilliseconds = 16.67;

    /// <summary>
    /// The measured p95 safety margin doc 90 requires before an agent may rebalance
    /// sub-budgets without human input.
    /// </summary>
    internal const double RebalanceMarginMilliseconds = 2.0;

    /// <summary>Stable ID of the input, application flow, and command admission area.</summary>
    internal const string InputAndCommands = "BDG-CPU-001";

    /// <summary>Stable ID of the complete authoritative simulation area.</summary>
    internal const string Simulation = "BDG-CPU-002";

    /// <summary>Stable ID of the snapshot publication and presentation synchronization area.</summary>
    internal const string SnapshotAndSync = "BDG-CPU-003";

    /// <summary>Stable ID of the crowd, actor, and VFX presentation update area.</summary>
    internal const string CrowdActorVfx = "BDG-CPU-004";

    /// <summary>Stable ID of the HUD and UI update and drawing preparation area.</summary>
    internal const string HudAndUi = "BDG-CPU-005";

    /// <summary>Stable ID of the audio and haptics event processing area.</summary>
    internal const string AudioAndHaptics = "BDG-CPU-006";

    /// <summary>Stable ID of the engine and render submission area outside measured presentation.</summary>
    internal const string EngineAndRender = "BDG-CPU-007";

    /// <summary>Stable ID of the unallocated safety margin.</summary>
    internal const string UnallocatedMargin = "BDG-CPU-008";

    private static readonly ImmutableArray<FrameBudgetArea> Areas = ImmutableArray.Create(
        new FrameBudgetArea(InputAndCommands, "Input, application flow, command admission", 0.40),
        new FrameBudgetArea(Simulation, "Complete authoritative simulation", 5.00),
        new FrameBudgetArea(SnapshotAndSync, "Snapshot publication and presentation synchronization", 1.00),
        new FrameBudgetArea(CrowdActorVfx, "Crowd/actor/VFX presentation updates", 2.00),
        new FrameBudgetArea(HudAndUi, "HUD/UI update and drawing preparation", 1.00),
        new FrameBudgetArea(AudioAndHaptics, "Audio/haptics event processing", 0.40),
        new FrameBudgetArea(
            EngineAndRender,
            "Godot engine/render submission outside measured presentation",
            3.00),
        new FrameBudgetArea(UnallocatedMargin, "Unallocated safety margin", 3.87));

    /// <summary>Every allocation row, in doc 90's table order.</summary>
    internal static ImmutableArray<FrameBudgetArea> All => Areas;

    /// <summary>GPU p95 target in milliseconds.</summary>
    internal const double GpuP95Milliseconds = 14.0;

    /// <summary>GPU p99 target in milliseconds.</summary>
    internal const double GpuP99Milliseconds = 18.0;

    /// <summary>Process working set target in bytes during an active standard run.</summary>
    internal const long WorkingSetBytes = 2560L * 1024 * 1024;

    /// <summary>Managed heap target in bytes after warm-up.</summary>
    internal const long ManagedHeapBytes = 256L * 1024 * 1024;

    /// <summary>Aggregate steady active-play managed allocation target in bytes per frame.</summary>
    /// <remarks>
    /// Doc 90: the aggregate target "accommodates engine/UI behavior; project-controlled hot
    /// paths target zero", so this is a ceiling on the whole frame and not a licence for a
    /// simulation system to allocate.
    /// </remarks>
    internal const long AllocationBytesPerFrame = 1024;

    /// <summary>Run transition peak working set target in bytes.</summary>
    internal const long TransitionPeakBytes = 3584L * 1024 * 1024;

    /// <summary>Recovery artifact size target in compressed bytes.</summary>
    internal const long RecoveryArtifactBytes = 16L * 1024 * 1024;

    /// <summary>Looks up an allocation row by stable ID.</summary>
    internal static FrameBudgetArea Require(string id)
    {
        FrameBudgetArea? found = Find(id);
        return found ?? throw new InvalidOperationException(
            "frame budget area '" + id + "' is not registered in FrameBudget. Doc 90 § Target-device frame "
            + "budget fixes the allocation, and every metric must roll up to one of its rows.");
    }

    /// <summary>Looks up an allocation row by stable ID, or null.</summary>
    internal static FrameBudgetArea? Find(string id)
    {
        foreach (FrameBudgetArea area in Areas)
        {
            if (string.Equals(area.Id, id, StringComparison.Ordinal))
            {
                return area;
            }
        }

        return null;
    }

    /// <summary>The sum of every allocation row, including the unallocated margin.</summary>
    internal static double TotalMilliseconds()
    {
        double total = 0;
        foreach (FrameBudgetArea area in Areas)
        {
            total += area.P95Milliseconds;
        }

        return total;
    }

    /// <summary>The sum of every allocated row, excluding the unallocated margin.</summary>
    internal static double AllocatedMilliseconds()
    {
        return TotalMilliseconds() - Require(UnallocatedMargin).P95Milliseconds;
    }

    /// <summary>Renders the allocation as canonical ordered reviewable text.</summary>
    internal static string ToTable()
    {
        System.Text.StringBuilder builder = new();
        builder.Append("# CPU frame budget allocation, from doc 90 § Target-device frame budget.\n");
        builder.Append("# TDR-003 requires a ")
            .Append(FrameMilliseconds.ToString("0.00", CultureInfo.InvariantCulture))
            .Append(" ms frame at 60 FPS.\n");
        builder.Append("id\tp95_ms\tarea\n");
        foreach (FrameBudgetArea area in Areas)
        {
            builder.Append(area.Id).Append('\t')
                .Append(area.P95Milliseconds.ToString("0.00", CultureInfo.InvariantCulture)).Append('\t')
                .Append(area.Area).Append('\n');
        }

        builder.Append("total\t")
            .Append(TotalMilliseconds().ToString("0.00", CultureInfo.InvariantCulture))
            .Append("\tsum of every row, which must equal the frame budget\n");
        return builder.ToString();
    }
}
