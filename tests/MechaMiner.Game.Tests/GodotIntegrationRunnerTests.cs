using System;
using System.IO;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Game.Tests;

/// <summary>
/// The Godot engine integration tier: proves the runner's process exit and report
/// contract across all four required fixtures.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-FND-003-007</c> (pass), <c>VER-FND-003-008</c> (fail),
/// <c>VER-FND-003-009</c> (timeout), <c>VER-FND-003-010</c> (artifact). FND-003's
/// completion gate in doc 110 is "headless pass/fail/timeout/artifact fixtures".
/// </para>
/// <para>
/// The <c>fail</c> case is asserted here as an expected failure, which is the only way
/// a host test can prove that a broken engine case really fails without failing this
/// suite. <c>build/verify-godot-runner.sh</c> proves the same thing from the outside,
/// by driving the runner directly and requiring a nonzero exit and a
/// <c>failed</c> report.
/// </para>
/// <para>
/// Every assertion is against the emitted report. The Godot process exit code is
/// recorded and cross-checked, never trusted alone: a headless launch exits <c>0</c>
/// even when the C# script fails to load.
/// </para>
/// </remarks>
[TestFixture]
[Category("GodotIntegration")]
internal sealed class GodotIntegrationRunnerTests
{
    [OneTimeSetUp]
    public void PrepareEngineTier()
    {
        EngineRunner.EnsurePrepared();
    }

    [Test]
    public void PassingCaseIsReportedAsPassed()
    {
        EngineRunResult result = EngineRunner.Run("pass");
        WriteEvidence(result);

        Assert.That(
            result.Outcome,
            Is.EqualTo(EngineRunOutcome.Passed),
            () => "the pass case must produce a passing report:\n" + result.Output);

        EngineRunnerReport report = result.Report!;
        Expect.Multiple(() =>
        {
            Assert.That(report.Schema, Is.EqualTo("MMG-RUNNER-REPORT"));
            Assert.That(report.SchemaVersion, Is.EqualTo(1));
            Assert.That(report.Case, Is.EqualTo("pass"));
            Assert.That(report.Outcome, Is.EqualTo("passed"));
            Assert.That(report.RequestedExitCode, Is.Zero);
            Assert.That(report.Assertions, Is.Not.Empty, "a report with no assertion proves nothing");
            Assert.That(report.Engine.Version, Does.StartWith("4.7.1"), "the pinned engine ran");
            Assert.That(report.Engine.RenderingMethod, Is.EqualTo("mobile"));
            Assert.That(report.Engine.Headless, Is.True);
        });

        foreach (EngineRunnerAssertion assertion in report.Assertions)
        {
            Assert.That(assertion.Passed, Is.True, () => assertion.Name + ": " + assertion.Detail);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                result.Output,
                Does.Contain(EngineRunner.StartupLine),
                "managed code must have executed, not merely been loaded");
            Assert.That(
                result.ProcessExitCode,
                Is.EqualTo(report.RequestedExitCode),
                "the exit code is cross-checked against the report, never trusted alone");
        });
    }

    [Test]
    public void FailingCaseIsReportedAsFailedAndNamesItsFailingAssertion()
    {
        EngineRunResult result = EngineRunner.Run("fail");
        WriteEvidence(result);

        Assert.That(
            result.Outcome,
            Is.EqualTo(EngineRunOutcome.Failed),
            () => "the fail case must produce a failing report, not a passing one:\n" + result.Output);

        EngineRunnerReport report = result.Report!;
        EngineRunnerAssertion? failing = null;
        foreach (EngineRunnerAssertion assertion in report.Assertions)
        {
            if (!assertion.Passed)
            {
                failing = assertion;
                break;
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(report.Case, Is.EqualTo("fail"));
            Assert.That(report.Outcome, Is.EqualTo("failed"));
            Assert.That(failing, Is.Not.Null, "a failing report must name the assertion that failed");
            Assert.That(failing!.Name, Is.EqualTo("deliberate-failure"));
            Assert.That(
                report.RequestedExitCode,
                Is.EqualTo(4),
                "a failing engine case requests doc 100's validation exit class");
            Assert.That(
                result.ProcessExitCode,
                Is.EqualTo(4),
                "the engine propagated the requested nonzero exit code");
        });
    }

    [Test]
    public void HangingCaseIsTerminatedAtItsBoundedTimeoutAndLeavesNoReport()
    {
        EngineRunResult result = EngineRunner.Run("hang", EngineRunner.HangTimeout);
        WriteEvidence(result);

        Expect.Multiple(() =>
        {
            Assert.That(
                result.Outcome,
                Is.EqualTo(EngineRunOutcome.TimedOut),
                "a case that never quits must be classified as timed out, never as passed");
            Assert.That(result.Report, Is.Null, "a timed-out case must leave no report");
            Assert.That(
                File.Exists(result.ReportPath),
                Is.False,
                "the runner writes its report atomically, so a killed process leaves nothing behind");
            Assert.That(
                result.DurationMs,
                Is.GreaterThanOrEqualTo((long)EngineRunner.HangTimeout.TotalMilliseconds - 1500),
                "the case must have run until its bound, not exited early");
            Assert.That(
                result.DurationMs,
                Is.LessThan((long)EngineRunner.HangTimeout.TotalMilliseconds + 30_000),
                "the timeout must actually terminate the process");
            Assert.That(
                result.Output,
                Does.Contain("will never quit"),
                "the hanging case is the deliberate one, not an accidental stall");
        });
    }

    [Test]
    public void ArtifactCaseWritesAndReferencesItsArtifact()
    {
        EngineRunResult result = EngineRunner.Run("artifact");
        WriteEvidence(result);

        Assert.That(
            result.Outcome,
            Is.EqualTo(EngineRunOutcome.Passed),
            () => "the artifact case must pass:\n" + result.Output);

        EngineRunnerReport report = result.Report!;
        Assert.That(report.Artifacts, Is.Not.Empty, "the artifact case must reference what it wrote");

        string referenced = report.Artifacts[0];
        Assert.That(File.Exists(referenced), () => "referenced artifact is missing: " + referenced);

        string content = File.ReadAllText(referenced);
        TestContext.Progress.WriteLine("captured engine artifact " + TestArtifacts.Relative(referenced));
        TestContext.Progress.WriteLine(content);

        Expect.Multiple(() =>
        {
            Assert.That(
                referenced,
                Does.StartWith(result.ArtifactDirectory),
                "a case writes only inside the artifact directory it was given");
            Assert.That(content, Does.Contain("engine_version\t4.7.1"));
            Assert.That(content, Does.Contain("rendering_method\tmobile"));
            Assert.That(content, Does.Contain("headless\ttrue"));
        });
    }

    [Test]
    public void AnUnknownCaseIsRejectedRatherThanReportedAsEitherOutcome()
    {
        EngineRunResult result = EngineRunner.Run("definitely-not-a-case", TimeSpan.FromSeconds(60));
        WriteEvidence(result);

        Expect.Multiple(() =>
        {
            Assert.That(
                result.Outcome,
                Is.EqualTo(EngineRunOutcome.NoReport),
                "an unknown case is a broken invocation, not a failing case, so it writes no report");
            Assert.That(
                result.ProcessExitCode,
                Is.EqualTo(2),
                "the runner returns doc 100's invalid-invocation exit class");
        });
    }

    private static void WriteEvidence(EngineRunResult result)
    {
        TestContext.Progress.WriteLine(
            "outcome=" + result.Outcome
            + " process-exit=" + result.ProcessExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " duration-ms=" + result.DurationMs.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " report=" + TestArtifacts.Relative(result.ReportPath));
    }
}
