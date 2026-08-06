using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaMiner.Tools.Cli;

namespace MechaMiner.Tools.Verbs;

/// <summary>The counted outcome of one test project run.</summary>
internal sealed class TestTally
{
    /// <summary>The project that was run.</summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>Tests that passed.</summary>
    public int Passed { get; set; }

    /// <summary>Tests that failed.</summary>
    public int Failed { get; set; }

    /// <summary>Tests that were skipped. A skipped required test is a defect (doc 91 § Flake policy).</summary>
    public int Skipped { get; set; }

    /// <summary>Tests discovered and executed.</summary>
    public int Total { get; set; }
}

/// <summary>
/// A filesystem tripwire that makes "the pure tier launched no Godot process" a
/// falsifiable assertion.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Test project separation:
/// "Pure simulation/content/persistence tests do not launch Godot." Proving that
/// negative needs an observation that would actually differ if it were violated.
/// The import cache is not such an observation: after any earlier
/// <c>godot-import</c> the cache already exists, so "the cache did not appear" is
/// true no matter what the tier did.
/// </para>
/// <para>
/// Instead an executable named <c>godot</c> is written into a private directory,
/// that directory is placed first on <c>PATH</c> for every pure test process, and
/// <c>MECHAMINER_GODOT</c> is pointed at it as well, so both discovery routes the
/// repository uses lead to it. The shim appends its own argument vector to a
/// sentinel file and exits nonzero. If the sentinel exists afterwards, something in
/// the pure tier tried to launch the engine and the tier fails with the recorded
/// command line; if it does not, nothing did. The tripwire never launches the real
/// engine, so a violation is caught rather than performed.
/// </para>
/// </remarks>
internal sealed class GodotTripwire
{
    private const int ShimExitCode = 97;

    private GodotTripwire(string shimPath, string sentinelPath, IReadOnlyDictionary<string, string> environment)
    {
        ShimPath = shimPath;
        SentinelPath = sentinelPath;
        Environment = environment;
    }

    /// <summary>The environment every pure test process runs with.</summary>
    internal IReadOnlyDictionary<string, string> Environment { get; }

    /// <summary>The shim that stands in for the engine.</summary>
    internal string ShimPath { get; }

    /// <summary>The file the shim appends to when it is invoked.</summary>
    internal string SentinelPath { get; }

    /// <summary>Writes the shim and returns the environment that arms it.</summary>
    internal static GodotTripwire Arm(VerbContext context)
    {
        string directory = Path.Combine(context.ArtifactDirectory, "godot-tripwire");
        Directory.CreateDirectory(directory);
        string sentinel = Path.Combine(directory, "launched.log");
        if (File.Exists(sentinel))
        {
            File.Delete(sentinel);
        }

        string shim = Path.Combine(directory, OperatingSystem.IsWindows() ? "godot.cmd" : "godot");
        File.WriteAllText(shim, OperatingSystem.IsWindows() ? WindowsShim(sentinel) : UnixShim(sentinel));
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                shim,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        string existingPath = System.Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        Dictionary<string, string> environment = new(StringComparer.Ordinal)
        {
            ["PATH"] = directory + Path.PathSeparator + existingPath,
            ["MECHAMINER_GODOT"] = shim,
        };

        return new GodotTripwire(shim, sentinel, environment);
    }

    /// <summary>
    /// Records the assertion, and appends to <paramref name="failures"/> when the
    /// tripwire was hit so the verb returns a validation class rather than success.
    /// </summary>
    internal void Assert(VerbContext context, List<string> failures)
    {
        bool tripped = File.Exists(SentinelPath);
        string shimName = Path.GetFileName(ShimPath);
        if (!tripped)
        {
            context.Runner.RecordAssertion(
                "no-godot-launched",
                true,
                "a '" + shimName + "' shim was first on PATH and MECHAMINER_GODOT pointed at it for every "
                + "pure test process; it was never invoked, so no process in the pure tier tried to launch "
                + "the engine");
            return;
        }

        string recorded = File.ReadAllText(SentinelPath).Trim();
        context.Runner.RecordAssertion(
            "no-godot-launched",
            false,
            "the pure tier tried to launch Godot, which doc 91 § Test project separation forbids. "
            + "Recorded invocation(s): " + recorded.Replace('\n', ';'));
        failures.Add("the pure tier launched Godot (see the no-godot-launched step)");
    }

    private static string UnixShim(string sentinel)
    {
        return "#!/usr/bin/env bash\n"
            + "# Written by MechaMiner.Tools GodotTripwire. Not the engine.\n"
            + "printf '%s\\n' \"godot-shim $*\" >>" + Quote(sentinel) + "\n"
            + "echo 'the pure test tier must not launch Godot (doc 91 s Test project separation)' >&2\n"
            + "exit " + ShimExitCode.ToString(CultureInfo.InvariantCulture) + "\n";
    }

    private static string WindowsShim(string sentinel)
    {
        return "@echo off\r\n"
            + "rem Written by MechaMiner.Tools GodotTripwire. Not the engine.\r\n"
            + "echo godot-shim %*>>\"" + sentinel + "\"\r\n"
            + "echo the pure test tier must not launch Godot (doc 91 s Test project separation) 1>&2\r\n"
            + "exit /b " + ShimExitCode.ToString(CultureInfo.InvariantCulture) + "\r\n";
    }

    private static string Quote(string path)
    {
        return "\"" + path.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
    }
}

/// <summary>
/// <c>test-fast</c> and <c>test-main</c>.
/// </summary>
/// <remarks>
/// <para>
/// Doc 100 § Standard command surface defines <c>test-fast</c> as "pure bounded
/// tests, content validation, representative headless fixtures" and <c>test-main</c>
/// as "fast suite plus Godot integration, package smoke prerequisites, broader
/// matrices".
/// </para>
/// <para>
/// The tiers are separated by project list, not by a test-name filter. Doc 91
/// § Test project separation states that "Pure simulation/content/persistence tests
/// do not launch Godot", so the pure tier simply never runs the project that can,
/// and <c>test-fast</c> proves it with a <see cref="GodotTripwire"/> rather than by
/// inspecting the import cache: a cache that already exists proves nothing, so an
/// assertion built on it can never fail and is not a gate.
/// </para>
/// <para>
/// Content validation and the broader matrices named in doc 100 belong to
/// <c>DAT-006</c> and <c>OPS-001</c>; those steps join this verb when those
/// packages land. The <c>content</c> verb is registered and awaiting <c>DAT-006</c>,
/// so nothing here silently substitutes for it.
/// </para>
/// </remarks>
internal static class TestVerb
{
    private static readonly string[] PureTestProjects =
    {
        "tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj",
        "tests/MechaMiner.Content.Tests/MechaMiner.Content.Tests.csproj",
        "tests/MechaMiner.Diagnostics.Tests/MechaMiner.Diagnostics.Tests.csproj",
        "tests/MechaMiner.Persistence.Tests/MechaMiner.Persistence.Tests.csproj",
    };

    private const string EngineTestProject = "tests/MechaMiner.Game.Tests/MechaMiner.Game.Tests.csproj";

    /// <summary>Runs the pure tiers and the build-policy fixtures, and launches no Godot process.</summary>
    internal static VerbOutcome RunFastTier(VerbContext context)
    {
        GodotTripwire tripwire = GodotTripwire.Arm(context);

        context.Section("stage 1: build-policy fixtures");
        CommandResult policies = context.RunRepositoryScript(
            "verify-policies",
            "build/verify-policies.sh",
            scriptArguments: null,
            timeout: TimeSpan.FromMinutes(20));
        if (!policies.Succeeded)
        {
            return VerbOutcome.Validation("a build-policy fixture no longer proves its policy; see the step log");
        }

        context.Section("stage 2: pure NUnit tiers (no Godot)");
        List<TestTally> tallies = new();
        List<string> failures = new();
        foreach (string project in PureTestProjects)
        {
            VerbOutcome? failure = RunTestProject(context, project, tallies, failures, tripwire.Environment);
            if (failure is not null)
            {
                return failure;
            }
        }

        context.Section("stage 3: assert the pure tier launched no Godot process");
        tripwire.Assert(context, failures);

        return Summarize(context, tallies, failures, "test-fast");
    }

    /// <summary>Runs the fast tier, a clean headless import, and the Godot engine tier.</summary>
    internal static VerbOutcome RunMainTier(VerbContext context)
    {
        context.Section("stage 1: the complete fast tier");
        VerbOutcome fast = RunFastTier(context);
        if (fast.ExitClass != ExitClass.Success)
        {
            return fast;
        }

        context.Section("stage 2: clean headless Godot import");
        VerbOutcome import = GodotImportVerb.Execute(context);
        if (import.ExitClass != ExitClass.Success)
        {
            return import;
        }

        context.Section("stage 3: Godot engine integration tier");
        List<TestTally> tallies = new();
        List<string> failures = new();
        VerbOutcome? failure = RunTestProject(context, EngineTestProject, tallies, failures);
        if (failure is not null)
        {
            return failure;
        }

        return Summarize(context, tallies, failures, "test-main engine tier");
    }

    private static VerbOutcome? RunTestProject(
        VerbContext context,
        string projectRelativePath,
        List<TestTally> tallies,
        List<string> failures,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        string resultsDirectory = Path.Combine(context.ArtifactDirectory, "trx");
        Directory.CreateDirectory(resultsDirectory);

        string stepName = "test-" + Path.GetFileNameWithoutExtension(projectRelativePath);
        CommandResult result = context.Runner.Run(
            stepName,
            "dotnet",
            new[]
            {
                "test",
                context.Layout.Absolute(projectRelativePath),
                "--nologo",
                "-v",
                "minimal",
                "--logger",
                "trx",
                "--results-directory",
                resultsDirectory,
            },
            context.Layout.Root,
            TimeSpan.FromMinutes(30),
            environment);

        TestTally tally = ParseTally(projectRelativePath, result.Output);
        tallies.Add(tally);
        context.Console.WriteLine(
            "      " + projectRelativePath + ": total " + tally.Total.ToString(CultureInfo.InvariantCulture)
            + ", passed " + tally.Passed.ToString(CultureInfo.InvariantCulture)
            + ", failed " + tally.Failed.ToString(CultureInfo.InvariantCulture)
            + ", skipped " + tally.Skipped.ToString(CultureInfo.InvariantCulture));

        if (LooksLikeCompilationFailure(result.Output))
        {
            return VerbOutcome.Build(projectRelativePath + " did not compile; see the step log");
        }

        if (!result.Succeeded || tally.Failed > 0)
        {
            failures.Add(projectRelativePath);
        }

        context.Runner.RecordAssertion(
            stepName + ":discovered-tests",
            tally.Total > 0,
            tally.Total > 0
                ? "discovered and executed " + tally.Total.ToString(CultureInfo.InvariantCulture) + " test(s)"
                : "discovered 0 tests, which is a harness failure rather than a passing run");

        if (tally.Total == 0)
        {
            failures.Add(projectRelativePath + " (zero tests discovered)");
        }

        return null;
    }

    private static VerbOutcome Summarize(
        VerbContext context,
        List<TestTally> tallies,
        List<string> failures,
        string tierName)
    {
        int total = 0;
        int passed = 0;
        int failed = 0;
        int skipped = 0;
        foreach (TestTally tally in tallies)
        {
            total += tally.Total;
            passed += tally.Passed;
            failed += tally.Failed;
            skipped += tally.Skipped;
        }

        string summary = tierName + ": total " + total.ToString(CultureInfo.InvariantCulture)
            + ", passed " + passed.ToString(CultureInfo.InvariantCulture)
            + ", failed " + failed.ToString(CultureInfo.InvariantCulture)
            + ", skipped " + skipped.ToString(CultureInfo.InvariantCulture);

        string tallyReport = RenderTallyReport(tallies);
        string tallyPath = context.WriteArtifact("test-tally.txt", tallyReport);

        if (failures.Count > 0)
        {
            return VerbOutcome
                .Validation(summary + ". Failing project(s): " + string.Join(", ", failures))
                .WithArtifact(tallyPath);
        }

        VerbOutcome outcome = VerbOutcome.Success(summary).WithArtifact(tallyPath);
        if (skipped > 0)
        {
            outcome.WithWarning(
                skipped.ToString(CultureInfo.InvariantCulture)
                + " test(s) were skipped. doc 91 § Flake policy: a skipped required test is a defect, "
                + "and quarantine needs an owner, issue, reason, expiration, and equivalent gate.");
        }

        return outcome;
    }

    private static string RenderTallyReport(List<TestTally> tallies)
    {
        System.Text.StringBuilder builder = new();
        builder.Append("project\ttotal\tpassed\tfailed\tskipped\n");
        foreach (TestTally tally in tallies)
        {
            builder.Append(tally.Project).Append('\t')
                .Append(tally.Total.ToString(CultureInfo.InvariantCulture)).Append('\t')
                .Append(tally.Passed.ToString(CultureInfo.InvariantCulture)).Append('\t')
                .Append(tally.Failed.ToString(CultureInfo.InvariantCulture)).Append('\t')
                .Append(tally.Skipped.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        return builder.ToString();
    }

    private static bool LooksLikeCompilationFailure(string output)
    {
        return output.Contains(": error CS", StringComparison.Ordinal)
            || output.Contains("Build FAILED", StringComparison.Ordinal);
    }

    /// <summary>
    /// Reads the counts out of the test runner's own summary line, for example
    /// <c>Failed: 0, Passed: 3, Skipped: 0, Total: 3</c>. Counts accumulate across
    /// summary lines so a multi-assembly run is not undercounted.
    /// </summary>
    private static TestTally ParseTally(string project, string output)
    {
        TestTally tally = new() { Project = project };
        foreach (string line in output.Split('\n'))
        {
            if (!line.Contains("Total:", StringComparison.Ordinal))
            {
                continue;
            }

            tally.Failed += ReadCount(line, "Failed:");
            tally.Passed += ReadCount(line, "Passed:");
            tally.Skipped += ReadCount(line, "Skipped:");
            tally.Total += ReadCount(line, "Total:");
        }

        return tally;
    }

    private static int ReadCount(string line, string label)
    {
        int index = line.IndexOf(label, StringComparison.Ordinal);
        if (index < 0)
        {
            return 0;
        }

        int cursor = index + label.Length;
        while (cursor < line.Length && line[cursor] == ' ')
        {
            cursor++;
        }

        int start = cursor;
        while (cursor < line.Length && char.IsAsciiDigit(line[cursor]))
        {
            cursor++;
        }

        return cursor > start
            && int.TryParse(line[start..cursor], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : 0;
    }
}
