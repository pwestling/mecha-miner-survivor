using System;
using System.Globalization;
using MechaMiner.Tools.Cli;

namespace MechaMiner.Tools.Verbs;

/// <summary>
/// <c>build</c>: "locked restore, analyzers, warnings-as-errors compilation"
/// (<c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Standard command surface).
/// </summary>
/// <remarks>
/// The verb also asserts the accepted project boundary through
/// <c>build/verify-architecture.sh</c>. Doc 100 § Repository structure makes the
/// ownership layout and reference direction part of what a build must not silently
/// violate, and <c>TR-CTR-001</c> requires the direction to be architecture-tested.
/// <c>TASK-FND-009-001</c> replaces that script with real architecture tests inside
/// the pure test projects; the verb keeps being the way in.
/// </remarks>
internal static class BuildVerb
{
    /// <summary>Runs a locked restore, a warnings-as-errors build, and the boundary assertion.</summary>
    internal static VerbOutcome Execute(VerbContext context)
    {
        WorkflowConfiguration configuration = context.Configuration();

        context.Section("stage 1: locked restore (" + configuration.WorkflowName + " -> MSBuild "
            + configuration.MsbuildName + ")");
        CommandResult restore = context.Runner.Run(
            "dotnet-restore-locked",
            "dotnet",
            // Restore is deliberately not given a configuration. Godot.NET.Sdk
            // references GodotSharpEditor only under Debug, so restoring under
            // ExportRelease would rewrite game/packages.lock.json and every locked
            // restore afterwards would fail. Restoring once at the default
            // configuration produces the superset graph that all three
            // configurations build against, so one committed lock file per project
            // stays correct.
            new[]
            {
                "restore",
                context.Layout.Solution,
                "--locked-mode",
                "--nologo",
            },
            context.Layout.Root,
            TimeSpan.FromMinutes(10));
        if (!restore.Succeeded)
        {
            return VerbOutcome.Build(
                "locked restore failed. CI restores in locked mode and fails if lock files would change "
                + "(doc 100 § Dependency policy); update Directory.Packages.props and the lock files together.");
        }

        context.Section("stage 2: compile with analyzers and warnings as errors");
        CommandResult build = context.Runner.Run(
            "dotnet-build",
            "dotnet",
            new[]
            {
                "build",
                context.Layout.Solution,
                "--no-restore",
                "--nologo",
                "-warnaserror",
                "-c",
                configuration.MsbuildName,
                "-v",
                "minimal",
            },
            context.Layout.Root,
            TimeSpan.FromMinutes(20));

        int warnings = CountReported(build.Output, "Warning(s)");
        int errors = CountReported(build.Output, "Error(s)");
        context.Runner.RecordAssertion(
            "zero-warnings",
            build.Succeeded && warnings == 0 && errors == 0,
            "reported " + warnings.ToString(CultureInfo.InvariantCulture) + " warning(s) and "
                + errors.ToString(CultureInfo.InvariantCulture) + " error(s)");

        if (!build.Succeeded || errors > 0)
        {
            return VerbOutcome.Build(
                "compilation failed with " + errors.ToString(CultureInfo.InvariantCulture)
                + " error(s); see the step log");
        }

        if (warnings > 0)
        {
            return VerbOutcome.Build(
                "compilation reported " + warnings.ToString(CultureInfo.InvariantCulture)
                + " warning(s); the repository treats every warning as an error");
        }

        context.Section("stage 3: assert the accepted project boundary");
        CommandResult architecture = context.RunRepositoryScript(
            "verify-architecture",
            "build/verify-architecture.sh",
            scriptArguments: null,
            timeout: TimeSpan.FromMinutes(10));
        if (!architecture.Succeeded)
        {
            return VerbOutcome.Validation(
                "the accepted project boundary or repository layout is violated; see the step log");
        }

        return VerbOutcome.Success(
            "build " + configuration.WorkflowName + " (MSBuild " + configuration.MsbuildName
            + ") succeeded with 0 warnings, 0 errors, and an intact project boundary");
    }

    /// <summary>
    /// Reads MSBuild's own "<c>N Warning(s)</c>" / "<c>N Error(s)</c>" summary lines.
    /// Returns the largest reported count so a multi-project summary cannot hide one.
    /// </summary>
    private static int CountReported(string output, string suffix)
    {
        int highest = 0;
        foreach (string line in output.Split('\n'))
        {
            string trimmed = line.Trim();
            if (!trimmed.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            string number = trimmed[..^suffix.Length].Trim();
            if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                && value > highest)
            {
                highest = value;
            }
        }

        return highest;
    }
}
