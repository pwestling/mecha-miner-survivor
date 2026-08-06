using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MechaMiner.Tools.Cli;
using MechaMiner.Tools.Toolchain;

namespace MechaMiner.Tools.Verbs;

/// <summary>
/// <c>doctor</c>: "verify exact Godot/.NET/Blender/tool/template availability and
/// hashes without mutating global state"
/// (<c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Standard command surface).
/// </summary>
internal static class DoctorVerb
{
    /// <summary>Runs the read-only toolchain verification.</summary>
    internal static VerbOutcome Execute(VerbContext context)
    {
        context.Section("pinned toolchain (read-only; nothing outside artifacts/ is written)");

        ToolchainInspector inspector = new(context.Layout, context.Runner);
        ToolchainPins pins = inspector.LoadPins();
        List<ToolProbe> probes = inspector.Probe(pins);

        return Report(context, pins, probes);
    }

    /// <summary>
    /// Runs the same verification on behalf of another verb, so <c>bootstrap</c>
    /// ends by running <c>doctor</c> rather than by reimplementing it.
    /// </summary>
    internal static VerbOutcome Report(VerbContext context, ToolchainPins pins, List<ToolProbe> probes)
    {
        StringBuilder report = new();
        report.Append("MechaMiner toolchain report\n");
        report.Append("pin file:  ").Append(context.Layout.Relative(context.Layout.ToolchainPins)).Append('\n');
        report.Append("pin schema: ").Append(pins.Schema).Append(" v")
            .Append(pins.SchemaVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
        report.Append("platform:  ").Append(ToolchainInspector.PlatformKey()).Append('\n');
        report.Append('\n');

        int blocking = 0;
        List<string> warnings = new();

        context.Section("toolchain report");
        foreach (ToolProbe probe in probes)
        {
            string line = probe.ToReportLine();
            report.Append(line).Append('\n');
            context.Console.WriteLine(line);

            if (probe.IsBlocking)
            {
                blocking++;
            }
            else if (probe.Status == ToolStatus.Warning)
            {
                warnings.Add(probe.Tool + ": " + probe.Detail);
            }

            context.Runner.RecordAssertion(
                "toolchain:" + probe.Tool,
                !probe.IsBlocking,
                probe.Status.ToString().ToLowerInvariant() + " (expected " + probe.Expected
                    + ", observed " + probe.Observed + ")",
                quiet: true);
        }

        string reportPath = context.WriteArtifact("toolchain-report.txt", report.ToString());

        if (blocking > 0)
        {
            return VerbOutcome
                .Environment(
                    blocking.ToString(CultureInfo.InvariantCulture)
                    + " pinned tool(s) missing or mismatched; see " + reportPath)
                .WithWarnings(warnings)
                .WithArtifact(reportPath);
        }

        return VerbOutcome
            .Success("pinned toolchain verified; " + probes.Count.ToString(CultureInfo.InvariantCulture)
                + " probes, 0 mismatches")
            .WithWarnings(warnings)
            .WithArtifact(reportPath);
    }
}
