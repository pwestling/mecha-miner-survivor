using System;
using System.Globalization;
using MechaMiner.Diagnostics.Identity;
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

        int warnings = CountReported(build.Output, "Warning(s)", out bool sawWarningSummary);
        int errors = CountReported(build.Output, "Error(s)", out bool sawErrorSummary);

        // The counts are read out of MSBuild's own localized summary lines, so "no
        // summary line was found" and "the summary line said zero" are different facts
        // and must not both read as clean. Absence would otherwise make the
        // warnings-as-errors claim vacuous the moment MSBuild changed its output or ran
        // in another language - which is exactly why the wrappers pin
        // DOTNET_CLI_UI_LANGUAGE=en-US. An empty candidate set never satisfies a gate.
        bool summaryFound = sawWarningSummary && sawErrorSummary;
        context.Runner.RecordAssertion(
            "build-summary-present",
            summaryFound,
            summaryFound
                ? "MSBuild reported both its Warning(s) and Error(s) summary lines"
                : "MSBuild printed no "
                    + (sawWarningSummary ? "Error(s)" : sawErrorSummary ? "Warning(s)" : "Warning(s)/Error(s)")
                    + " summary line, so the reported counts below are not evidence of anything");

        context.Runner.RecordAssertion(
            "zero-warnings",
            summaryFound && build.Succeeded && warnings == 0 && errors == 0,
            summaryFound
                ? "reported " + warnings.ToString(CultureInfo.InvariantCulture) + " warning(s) and "
                    + errors.ToString(CultureInfo.InvariantCulture) + " error(s)"
                : "not evaluated: MSBuild's warning/error summary was absent from the build output");

        if (!summaryFound)
        {
            return VerbOutcome.Build(
                "the build produced no MSBuild warning/error summary, so a zero-warning tree"
                + " cannot be asserted; see the dotnet-build step log");
        }

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

        context.Section("stage 4: emit the SCH-BLD-001 build manifest");
        VerbOutcome? manifestFailure = EmitBuildManifest(context, out string manifestPath);
        if (manifestFailure is not null)
        {
            return manifestFailure;
        }

        return VerbOutcome.Success(
            "build " + configuration.WorkflowName + " (MSBuild " + configuration.MsbuildName
            + ") succeeded with 0 warnings, 0 errors, an intact project boundary, and a current "
            + "SCH-BLD-001 manifest")
            .WithArtifact(manifestPath);
    }

    /// <summary>
    /// Writes <c>generated/build-manifest.json</c> from the one build-identity owner and
    /// then reads it back to prove the file on disk is current.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The manifest describes the assembly that carries the identity — the workflow host
    /// this process is — not the configuration named by <c>--configuration</c>. That is
    /// deliberate and is why the <c>target</c> block records the host's own configuration
    /// and platform. Per-artifact release manifests, one for each packaged
    /// platform/configuration pair with its checksums, are doc 100 § Artifacts material
    /// and belong to <c>OPS-002</c>; inventing them here would mean writing a manifest
    /// whose <c>target</c> block was copied from an argument rather than read from the
    /// binary it describes.
    /// </para>
    /// <para>
    /// The file is not committed. It names the source commit of the build that produced
    /// it, so a committed copy could never be current at the commit that contains it.
    /// The relation a reviewer needs is "does the manifest match the assembly that was
    /// just built", and that is what the read-back asserts.
    /// </para>
    /// </remarks>
    private static VerbOutcome? EmitBuildManifest(VerbContext context, out string manifestPath)
    {
        manifestPath = BuildManifestFile.RepositoryRelativePath;
        string absolute = context.Layout.Absolute(manifestPath);
        BuildManifestFile.Write(absolute);

        BuildManifestComparison comparison = BuildManifestFile.Compare(absolute, manifestPath);
        context.Runner.RecordAssertion(
            "build-manifest-current",
            comparison.IsCurrent,
            comparison.Detail
                + (comparison.Differences.Count > 0 ? " [" + string.Join("; ", comparison.Differences) + "]" : string.Empty));

        context.Console.WriteLine("      " + manifestPath + ": " + comparison.Status);
        context.Console.WriteLine("      identity: " + Diagnostics.Identity.BuildIdentity.IdentityLine);

        if (!comparison.IsCurrent)
        {
            return VerbOutcome.Validation(
                "the SCH-BLD-001 manifest just written does not read back as current: " + comparison.Detail);
        }

        return null;
    }

    /// <summary>
    /// Reads MSBuild's own "<c>N Warning(s)</c>" / "<c>N Error(s)</c>" summary lines.
    /// Returns the largest reported count so a multi-project summary cannot hide one.
    /// </summary>
    /// <param name="output">The captured build output.</param>
    /// <param name="suffix">The summary-line suffix to read.</param>
    /// <param name="found">
    /// Set when at least one parseable summary line was present. The caller must check
    /// this: a return of <c>0</c> means "the summary said zero" only when
    /// <paramref name="found"/> is true, and otherwise means "no summary was printed",
    /// which is not evidence of a clean build.
    /// </param>
    private static int CountReported(string output, string suffix, out bool found)
    {
        int highest = 0;
        found = false;
        foreach (string line in output.Split('\n'))
        {
            string trimmed = line.Trim();
            if (!trimmed.EndsWith(suffix, StringComparison.Ordinal))
            {
                continue;
            }

            string number = trimmed[..^suffix.Length].Trim();
            if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                found = true;
                if (value > highest)
                {
                    highest = value;
                }
            }
        }

        return highest;
    }
}
