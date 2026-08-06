using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace MechaMiner.Game.EngineTesting;

/// <summary>One assertion an engine case made.</summary>
internal sealed class RunnerAssertion
{
    /// <summary>Stable assertion name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether it held.</summary>
    public bool Passed { get; set; }

    /// <summary>What was expected and what was observed.</summary>
    public string Detail { get; set; } = string.Empty;
}

/// <summary>Engine identity the runner observed, so a report is self-describing.</summary>
internal sealed class RunnerEngineIdentity
{
    /// <summary>The engine version string.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>The configured rendering method.</summary>
    public string RenderingMethod { get; set; } = string.Empty;

    /// <summary>The rendering driver actually in use.</summary>
    public string RenderingDriver { get; set; } = string.Empty;

    /// <summary>Whether the process is headless.</summary>
    public bool Headless { get; set; }
}

/// <summary>The report contract between the engine runner and its host.</summary>
/// <remarks>
/// This document, not the process exit code, is the gate. A headless Godot launch
/// exits 0 even when the C# script on a node fails to load - it logs
/// "Cannot instantiate C# script" and carries on - which FND-001 established
/// empirically. The host therefore requires a complete report with the expected case
/// ID and outcome, and treats a missing report as a failure regardless of exit status.
/// </remarks>
internal sealed class RunnerReport
{
    /// <summary>Stable schema identity.</summary>
    public string Schema { get; set; } = "MMG-RUNNER-REPORT";

    /// <summary>Version of this document's shape.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>The case that ran.</summary>
    public string Case { get; set; } = string.Empty;

    /// <summary><c>passed</c> or <c>failed</c>.</summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>The engine identity observed at run time.</summary>
    public RunnerEngineIdentity Engine { get; set; } = new();

    /// <summary>UTC start time in round-trip format.</summary>
    public string StartedUtc { get; set; } = string.Empty;

    /// <summary>Wall-clock duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>The exit code the runner asked the engine for.</summary>
    public int RequestedExitCode { get; set; }

    /// <summary>Every assertion the case made, in order.</summary>
    public List<RunnerAssertion> Assertions { get; set; } = new();

    /// <summary>Repository-relative paths of artifacts this case wrote.</summary>
    public List<string> Artifacts { get; set; } = new();
}

/// <summary>Source-generated JSON metadata for the report.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(RunnerReport))]
internal sealed partial class RunnerJsonContext : JsonSerializerContext
{
}

/// <summary>
/// The minimal Godot integration test runner.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-003</c> (<c>TASK-FND-003-002</c>). Verification:
/// <c>VER-FND-003-007</c> through <c>VER-FND-003-010</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Test project separation:
/// "Engine integration uses a dedicated minimal Godot test runner or scenes, not
/// production front-end navigation unless that is the subject", and doc 100 § C#
/// project standards: "Use a small project-owned Godot integration harness rather
/// than adopting a large engine test plugin before the foundation spike proves a
/// need." This is that harness: one node, one scene, four cases, a JSON report.
/// </para>
/// <para>
/// Development scaffolding, unmistakably marked and excluded from Release: the whole
/// <c>game/tests/</c> tree is removed from compilation under the <c>ExportRelease</c>
/// configuration by <c>game/MechaMiner.Game.csproj</c>, which is doc 100's Release
/// filtering of "test fixtures, debug scenes, diagnostics commands" applied at the
/// compiler rather than trusted to an export filter. <c>FND-006</c> adds the matching
/// export-preset exclusion for the scene file.
/// </para>
/// <para>
/// The report is written atomically - to a temporary file, then moved - so a case
/// that hangs or is killed leaves no report at all rather than a truncated one. That
/// is what lets the host distinguish a timeout from a failure.
/// </para>
/// </remarks>
public partial class GodotTestRunner : Node
{
    /// <summary>The stable line a host asserts the runner reached managed code.</summary>
    internal const string StartupLine = "MechaMiner: engine test runner ready";

    /// <summary>The stable line that precedes the report path.</summary>
    internal const string ReportLinePrefix = "MechaMiner: engine test report ";

    /// <summary>Environment variable naming the case to run.</summary>
    internal const string CaseVariable = "MECHAMINER_TEST_CASE";

    /// <summary>Environment variable naming where the report is written.</summary>
    internal const string ReportVariable = "MECHAMINER_TEST_REPORT";

    /// <summary>Environment variable naming where case artifacts are written.</summary>
    internal const string ArtifactDirectoryVariable = "MECHAMINER_TEST_ARTIFACTS";

    /// <summary>The case that asserts engine invariants and passes.</summary>
    internal const string PassCase = "pass";

    /// <summary>The case with a deliberately false assertion.</summary>
    internal const string FailCase = "fail";

    /// <summary>The case that never quits, so the host must time it out.</summary>
    internal const string HangCase = "hang";

    /// <summary>The case that writes an artifact and references it from the report.</summary>
    internal const string ArtifactCase = "artifact";

    private const string PinnedEngineVersionPrefix = "4.7.1";
    private const string RequiredRenderingMethod = "mobile";

    /// <inheritdoc/>
    public override void _Ready()
    {
        GD.Print(StartupLine);

        DateTimeOffset started = DateTimeOffset.UtcNow;
        string caseName = ReadSetting(CaseVariable);
        string reportPath = ReadSetting(ReportVariable);

        if (caseName.Length == 0 || reportPath.Length == 0)
        {
            // An invalid invocation is not a failing case: it is a broken harness, and
            // it must not be reported as either outcome.
            GD.PushError(
                "the engine test runner requires " + CaseVariable + " and " + ReportVariable);
            GetTree().Quit(2);
            return;
        }

        GD.Print("MechaMiner: engine test case " + caseName);

        if (string.Equals(caseName, HangCase, StringComparison.Ordinal))
        {
            // Deliberately never quits and deliberately writes no report. The host's
            // bounded timeout is the only thing that ends this process, which is what
            // VER-FND-003-009 proves.
            GD.Print("MechaMiner: engine test case hang will never quit; the host must time it out");
            return;
        }

        RunnerReport report = new()
        {
            Case = caseName,
            StartedUtc = started.ToString("O", CultureInfo.InvariantCulture),
            Engine = ReadEngineIdentity(),
        };

        switch (caseName)
        {
            case PassCase:
                AssertEngineInvariants(report);
                break;

            case FailCase:
                AssertEngineInvariants(report);
                report.Assertions.Add(new RunnerAssertion
                {
                    Name = "deliberate-failure",
                    Passed = false,
                    Detail = "this assertion is false on purpose so the host can prove that a failing "
                        + "engine case is reported as failed and fails its host test",
                });
                break;

            case ArtifactCase:
                AssertEngineInvariants(report);
                WriteCaseArtifact(report);
                break;

            default:
                GD.PushError("unknown engine test case '" + caseName + "'");
                GetTree().Quit(2);
                return;
        }

        bool passed = true;
        foreach (RunnerAssertion assertion in report.Assertions)
        {
            if (!assertion.Passed)
            {
                passed = false;
            }
        }

        report.Outcome = passed ? "passed" : "failed";
        report.DurationMs = (long)(DateTimeOffset.UtcNow - started).TotalMilliseconds;

        // Exit class 4 for a failing case, matching doc 100 § Standard command surface:
        // a validation or test failure. The host asserts the report, not this number.
        report.RequestedExitCode = passed ? 0 : 4;

        WriteReportAtomically(reportPath, report);
        GD.Print(ReportLinePrefix + reportPath);
        GetTree().Quit(report.RequestedExitCode);
    }

    private static string ReadSetting(string name)
    {
        // Command-line user arguments take precedence, so a case can be run by hand
        // without exporting anything; the host uses the environment.
        string prefix = "--" + name.ToLowerInvariant().Replace('_', '-') + "=";
        foreach (string argument in OS.GetCmdlineUserArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.Ordinal))
            {
                return argument[prefix.Length..];
            }
        }

        return OS.GetEnvironment(name);
    }

    private static RunnerEngineIdentity ReadEngineIdentity()
    {
        Godot.Collections.Dictionary version = Godot.Engine.GetVersionInfo();
        return new RunnerEngineIdentity
        {
            Version = version["string"].AsString(),
            RenderingMethod = ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),
            RenderingDriver = RenderingServer.GetVideoAdapterApiVersion(),
            Headless = DisplayServer.GetName() == "headless",
        };
    }

    private void AssertEngineInvariants(RunnerReport report)
    {
        string version = report.Engine.Version;
        report.Assertions.Add(new RunnerAssertion
        {
            Name = "pinned-engine-version",
            Passed = version.StartsWith(PinnedEngineVersionPrefix, StringComparison.Ordinal),
            Detail = "expected a version starting with " + PinnedEngineVersionPrefix
                + ", observed " + version,
        });

        report.Assertions.Add(new RunnerAssertion
        {
            Name = "mobile-renderer-configured",
            Passed = string.Equals(
                report.Engine.RenderingMethod,
                RequiredRenderingMethod,
                StringComparison.Ordinal),
            Detail = "expected rendering/renderer/rendering_method=" + RequiredRenderingMethod
                + ", observed " + report.Engine.RenderingMethod,
        });

        SceneTree? tree = GetTree();
        report.Assertions.Add(new RunnerAssertion
        {
            Name = "scene-tree-reached-managed-code",
            Passed = tree is not null && tree.Root is not null && IsInsideTree(),
            Detail = "the runner node is inside a live scene tree, so managed code really executed "
                + "rather than the script failing to instantiate",
        });

        report.Assertions.Add(new RunnerAssertion
        {
            Name = "headless-display-server",
            Passed = report.Engine.Headless,
            Detail = "expected the headless display server, observed " + DisplayServer.GetName(),
        });
    }

    private static void WriteCaseArtifact(RunnerReport report)
    {
        string directory = ReadSetting(ArtifactDirectoryVariable);
        if (directory.Length == 0)
        {
            report.Assertions.Add(new RunnerAssertion
            {
                Name = "artifact-directory-provided",
                Passed = false,
                Detail = "the artifact case requires " + ArtifactDirectoryVariable,
            });
            return;
        }

        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "engine-observations.txt");
        string content = string.Join(
            "\n",
            "# Engine observations captured by the MechaMiner engine test runner.",
            "# Canonical, ordered, reviewable text (doc 91 § Determinism and fixture policy).",
            "engine_version\t" + report.Engine.Version,
            "headless\t" + (report.Engine.Headless ? "true" : "false"),
            "rendering_driver\t" + report.Engine.RenderingDriver,
            "rendering_method\t" + report.Engine.RenderingMethod,
            string.Empty);
        File.WriteAllText(path, content);

        report.Artifacts.Add(path);
        report.Assertions.Add(new RunnerAssertion
        {
            Name = "artifact-written",
            Passed = File.Exists(path),
            Detail = "wrote and referenced " + path,
        });
    }

    private static void WriteReportAtomically(string reportPath, RunnerReport report)
    {
        string? directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(report, RunnerJsonContext.Default.RunnerReport) + "\n";
        string temporary = reportPath + ".partial";
        File.WriteAllText(temporary, json);
        File.Move(temporary, reportPath, overwrite: true);
    }
}
