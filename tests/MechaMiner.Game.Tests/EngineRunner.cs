using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Game.Tests;

/// <summary>How one engine case ended, as classified by the host.</summary>
internal enum EngineRunOutcome
{
    /// <summary>A complete report says the case passed.</summary>
    Passed,

    /// <summary>A complete report says the case failed.</summary>
    Failed,

    /// <summary>The bounded timeout elapsed and the process was terminated.</summary>
    TimedOut,

    /// <summary>The process ended without leaving a readable report.</summary>
    NoReport,
}

/// <summary>The host-side result of running one engine case.</summary>
internal sealed class EngineRunResult
{
    internal EngineRunResult(
        EngineRunOutcome outcome,
        int processExitCode,
        long durationMs,
        string output,
        EngineRunnerReport? report,
        string reportPath,
        string artifactDirectory)
    {
        Outcome = outcome;
        ProcessExitCode = processExitCode;
        DurationMs = durationMs;
        Output = output;
        Report = report;
        ReportPath = reportPath;
        ArtifactDirectory = artifactDirectory;
    }

    /// <summary>The classified outcome, decided from the report and not from the exit code.</summary>
    internal EngineRunOutcome Outcome { get; }

    /// <summary>The raw Godot process exit code, or <c>-1</c> after a timeout kill.</summary>
    internal int ProcessExitCode { get; }

    /// <summary>Wall-clock duration in milliseconds.</summary>
    internal long DurationMs { get; }

    /// <summary>Interleaved engine standard output and standard error.</summary>
    internal string Output { get; }

    /// <summary>The parsed report, or null when none was written or it could not be read.</summary>
    internal EngineRunnerReport? Report { get; }

    /// <summary>Where the report was expected.</summary>
    internal string ReportPath { get; }

    /// <summary>Where the case was told to write artifacts.</summary>
    internal string ArtifactDirectory { get; }
}

/// <summary>
/// Launches the pinned Godot editor headlessly, runs one engine case, and classifies
/// the outcome from the emitted report.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-003</c> (<c>TASK-FND-003-002</c>).
/// </para>
/// <para>
/// This type holds no Godot type. <c>docs/technical/115</c> § Accepted project
/// boundary keeps <c>game/</c> as the only Godot-referencing project, so the host
/// drives the pinned executable as an external process - which doc 115 explicitly
/// permits for tool processes - and the JSON report is the whole contract.
/// </para>
/// <para>
/// <b>The exit code is not the gate.</b> A headless Godot launch exits <c>0</c> even
/// when the C# script on a node fails to load: it logs "Cannot instantiate C# script"
/// and carries on. FND-001 hit this empirically. So a run is accepted only when a
/// complete report exists, names the case that was requested, and states an outcome;
/// the exit code is recorded as evidence and asserted for consistency, never trusted
/// alone.
/// </para>
/// <para>
/// Every run carries a bounded timeout and no wall-clock sleep, per doc 91 § Flake
/// policy. A case that never quits is killed and classified
/// <see cref="EngineRunOutcome.TimedOut"/>, which is distinguishable from a failure
/// precisely because the runner writes its report atomically and therefore leaves none.
/// </para>
/// </remarks>
internal static class EngineRunner
{
    /// <summary>The default bound on one engine case.</summary>
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);

    /// <summary>The bound used for the deliberately hanging case, kept short on purpose.</summary>
    internal static readonly TimeSpan HangTimeout = TimeSpan.FromSeconds(15);

    /// <summary>The scene the runner lives in.</summary>
    internal const string RunnerScene = "res://tests/GodotTestRunner.tscn";

    /// <summary>The stable startup line the runner prints when it reaches managed code.</summary>
    internal const string StartupLine = "MechaMiner: engine test runner ready";

    private const string PreparedMarker = "engine-tier-prepared";

    /// <summary>
    /// Builds the game assembly and imports the project once, so a bare
    /// <c>dotnet test</c> of this project works without the wrapper having run first.
    /// Idempotent and bounded.
    /// </summary>
    internal static void EnsurePrepared()
    {
        string gameDirectory = Path.Combine(TestArtifacts.RepositoryRoot, "game");
        string marker = Path.Combine(gameDirectory, ".godot", PreparedMarker);
        if (File.Exists(marker))
        {
            return;
        }

        TestContext.Progress.WriteLine("preparing the engine tier: building MechaMiner.Game");
        CommandOutcome build = RunProcess(
            "dotnet",
            new[]
            {
                "build",
                Path.Combine(gameDirectory, "MechaMiner.Game.csproj"),
                "--nologo",
                "-v",
                "quiet",
            },
            TestArtifacts.RepositoryRoot,
            TimeSpan.FromMinutes(10),
            environment: null);
        Assert.That(
            build.ExitCode,
            Is.Zero,
            () => "the game assembly must build before Godot can load it:\n" + build.Output);

        TestContext.Progress.WriteLine("preparing the engine tier: godot --headless --import");
        CommandOutcome import = RunProcess(
            GodotCommand(),
            new[] { "--headless", "--path", gameDirectory, "--import" },
            TestArtifacts.RepositoryRoot,
            TimeSpan.FromMinutes(10),
            environment: null);
        Assert.That(
            import.ExitCode,
            Is.Zero,
            () => "headless import must succeed before an engine case can run:\n" + import.Output);

        Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
        File.WriteAllText(
            marker,
            "Written by tests/MechaMiner.Game.Tests/EngineRunner.cs so the engine tier prepares once "
            + "per import cache. Deleting game/.godot forces a full rebuild and reimport.\n");
    }

    /// <summary>Runs one engine case and classifies its outcome from the report.</summary>
    internal static EngineRunResult Run(string caseName, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caseName);
        EnsurePrepared();

        string gameDirectory = Path.Combine(TestArtifacts.RepositoryRoot, "game");
        string caseDirectory = Path.Combine(
            TestArtifacts.RepositoryRoot,
            "artifacts",
            "engine-tier",
            caseName);
        Directory.CreateDirectory(caseDirectory);

        string reportPath = Path.Combine(caseDirectory, "report.json");
        string artifactDirectory = Path.Combine(caseDirectory, "case-artifacts");
        File.Delete(reportPath);
        if (Directory.Exists(artifactDirectory))
        {
            Directory.Delete(artifactDirectory, recursive: true);
        }

        Dictionary<string, string> environment = new(StringComparer.Ordinal)
        {
            ["MECHAMINER_TEST_CASE"] = caseName,
            ["MECHAMINER_TEST_REPORT"] = reportPath,
            ["MECHAMINER_TEST_ARTIFACTS"] = artifactDirectory,
        };

        TestContext.Progress.WriteLine(
            "ENGINE CASE " + caseName + " timeout "
            + (timeout ?? DefaultTimeout).TotalSeconds.ToString(CultureInfo.InvariantCulture) + "s");

        CommandOutcome outcome = RunProcess(
            GodotCommand(),
            new[]
            {
                "--headless",
                "--path",
                gameDirectory,
                RunnerScene,
                "--audio-driver",
                "Dummy",
            },
            TestArtifacts.RepositoryRoot,
            timeout ?? DefaultTimeout,
            environment);

        File.WriteAllText(Path.Combine(caseDirectory, "engine.log"), outcome.Output);

        if (outcome.TimedOut)
        {
            return new EngineRunResult(
                EngineRunOutcome.TimedOut,
                outcome.ExitCode,
                outcome.DurationMs,
                outcome.Output,
                report: null,
                reportPath,
                artifactDirectory);
        }

        EngineRunnerReport? report = ReadReport(reportPath, outcome.Output);
        if (report is null)
        {
            return new EngineRunResult(
                EngineRunOutcome.NoReport,
                outcome.ExitCode,
                outcome.DurationMs,
                outcome.Output,
                report: null,
                reportPath,
                artifactDirectory);
        }

        EngineRunOutcome classified = string.Equals(report.Outcome, "passed", StringComparison.Ordinal)
            ? EngineRunOutcome.Passed
            : EngineRunOutcome.Failed;

        return new EngineRunResult(
            classified,
            outcome.ExitCode,
            outcome.DurationMs,
            outcome.Output,
            report,
            reportPath,
            artifactDirectory);
    }

    /// <summary>Resolves the pinned Godot executable the same way <c>doctor</c> does.</summary>
    internal static string GodotCommand()
    {
        string? overridePath = Environment.GetEnvironmentVariable("MECHAMINER_GODOT");
        return string.IsNullOrWhiteSpace(overridePath) ? "godot" : overridePath;
    }

    private static EngineRunnerReport? ReadReport(string reportPath, string output)
    {
        if (!File.Exists(reportPath))
        {
            TestContext.Progress.WriteLine(
                "no report at " + TestArtifacts.Relative(reportPath) + "; engine output was:\n" + output);
            return null;
        }

        try
        {
            return EngineRunnerJsonContext.Deserialize(File.ReadAllText(reportPath));
        }
        catch (System.Text.Json.JsonException exception)
        {
            TestContext.Progress.WriteLine(
                "the report at " + TestArtifacts.Relative(reportPath) + " is not a valid "
                + "MMG-RUNNER-REPORT document: " + exception.Message);
            return null;
        }
    }

    private sealed class CommandOutcome
    {
        internal CommandOutcome(int exitCode, string output, long durationMs, bool timedOut)
        {
            ExitCode = exitCode;
            Output = output;
            DurationMs = durationMs;
            TimedOut = timedOut;
        }

        internal int ExitCode { get; }

        internal string Output { get; }

        internal long DurationMs { get; }

        internal bool TimedOut { get; }
    }

    private static CommandOutcome RunProcess(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        TimeSpan timeout,
        IReadOnlyDictionary<string, string>? environment)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        if (environment is not null)
        {
            foreach (KeyValuePair<string, string> entry in environment)
            {
                startInfo.Environment[entry.Key] = entry.Value;
            }
        }

        StringBuilder captured = new();
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool timedOut = false;
        int exitCode;

        using (Process process = new() { StartInfo = startInfo })
        {
            process.OutputDataReceived += (_, e) => Append(captured, e.Data);
            process.ErrorDataReceived += (_, e) => Append(captured, e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.Close();

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                timedOut = true;
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Exited between the timeout and the kill.
                }
            }

            process.WaitForExit();
            exitCode = timedOut ? -1 : process.ExitCode;
        }

        stopwatch.Stop();
        return new CommandOutcome(exitCode, captured.ToString(), stopwatch.ElapsedMilliseconds, timedOut);
    }

    private static void Append(StringBuilder target, string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (target)
        {
            target.Append(line).Append('\n');
        }
    }
}
