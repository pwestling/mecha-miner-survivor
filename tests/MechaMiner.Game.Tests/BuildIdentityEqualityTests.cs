using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MechaMiner.Diagnostics.Identity;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Game.Tests;

/// <summary>
/// The <c>FND-004</c> completion gate: "identity visible in tool/game test and
/// diagnostics", written as one equality comparison rather than three separate reads.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-004</c> (<c>TASK-FND-004-001</c>). Verification:
/// <c>VER-FND-004-004</c>. Requirements: <c>TR-BLD-001</c>, <c>TR-BLD-004</c>,
/// <c>TR-RUN-009</c>.
/// </para>
/// <para>
/// Three <b>separate processes</b> report their build identity and all three lines must
/// be equal:
/// </para>
/// <list type="number">
///   <item><description>
///     the <b>tool</b>: the workflow host writes <c>build_identity</c> into its
///     structured verb result under <c>artifacts/verbs/</c>;
///   </description></item>
///   <item><description>
///     the <b>game</b>: the Godot process writes <c>build_identity</c> into the engine
///     runner report;
///   </description></item>
///   <item><description>
///     <b>diagnostics</b>: <c>CMP-OBS-001</c> in this host process, plus the
///     <c>SCH-BLD-001</c> manifest it emits at <c>generated/build-manifest.json</c>.
///   </description></item>
/// </list>
/// <para>
/// The comparison is what makes this a gate. Three independent reads that each merely
/// produced <i>some</i> identity would pass while the surfaces disagreed, and the
/// failures this catches are exactly disagreements: a Godot
/// <c>AssemblyLoadContext</c> resolving a stale <c>MechaMiner.Diagnostics.dll</c>, a
/// surface re-deriving identity from its own environment instead of reading the one
/// owner, or a manifest left behind by an earlier build.
/// </para>
/// <para>
/// A different build configuration legitimately produces a different identity, since
/// configuration is part of it. All three surfaces here are the <c>Debug</c> build the
/// workflow wrapper and the Godot editor both produce, which is why they are
/// comparable at all; the test asserts that agreement instead of assuming it.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class BuildIdentityEqualityTests
{
    private const string ToolVerb = "doctor";

    /// <summary>The tool, the game, and diagnostics report one identical build identity.</summary>
    [Test]
    public void IdentityIsEqualAcrossToolGameAndDiagnostics()
    {
        string diagnosticsIdentity = BuildIdentity.IdentityLine;
        string manifestIdentity = ReadManifestIdentity();
        string toolIdentity = ReadToolIdentity();
        string gameIdentity = ReadGameIdentity();

        TestContext.Progress.WriteLine("diagnostics (CMP-OBS-001, this process): " + diagnosticsIdentity);
        TestContext.Progress.WriteLine("diagnostics (SCH-BLD-001 manifest):      " + manifestIdentity);
        TestContext.Progress.WriteLine("tool (MechaMiner.Tools process):         " + toolIdentity);
        TestContext.Progress.WriteLine("game (Godot process):                    " + gameIdentity);

        Expect.Multiple(() =>
        {
            Assert.That(
                toolIdentity,
                Is.EqualTo(diagnosticsIdentity),
                "the workflow host and the diagnostics owner must report one identity");
            Assert.That(
                gameIdentity,
                Is.EqualTo(diagnosticsIdentity),
                "the Godot process and the diagnostics owner must report one identity");
            Assert.That(
                manifestIdentity,
                Is.EqualTo(diagnosticsIdentity),
                "the emitted SCH-BLD-001 manifest must match the compiled identity");
        });
    }

    /// <summary>
    /// The manifest on disk is current with respect to the assembly that emitted it,
    /// classified with a cause rather than a bare boolean.
    /// </summary>
    [Test]
    public void TheGeneratedManifestIsCurrent()
    {
        string absolute = Path.Combine(
            TestArtifacts.RepositoryRoot,
            BuildManifestFile.RepositoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        EnsureManifestExists(absolute);

        BuildManifestComparison comparison = BuildManifestFile.Compare(
            absolute,
            BuildManifestFile.RepositoryRelativePath);

        Assert.That(
            comparison.Status,
            Is.EqualTo(BuildManifestComparison.CurrentStatus),
            () => comparison.Detail + " " + string.Join("; ", comparison.Differences));
    }

    private static string ReadManifestIdentity()
    {
        string absolute = Path.Combine(
            TestArtifacts.RepositoryRoot,
            BuildManifestFile.RepositoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        EnsureManifestExists(absolute);

        // ReadIdentityLine recomputes the line from the document's own fields and rejects a
        // manifest whose stored line disagrees with its contents, so that check is part of the
        // read rather than something this fixture has to remember to repeat.
        return BuildManifestFile.ReadIdentityLine(absolute);
    }

    /// <summary>
    /// Emits the manifest when it is absent, because it is a build output that is
    /// deliberately not committed: a manifest naming its own source commit could never
    /// be current at the commit that contained it.
    /// </summary>
    private static void EnsureManifestExists(string absolute)
    {
        if (!File.Exists(absolute))
        {
            BuildManifestFile.Write(absolute);
        }
    }

    /// <summary>
    /// Runs the workflow host as a separate process and reads the identity out of its
    /// structured verb result.
    /// </summary>
    /// <remarks>
    /// The host assembly is launched directly rather than through <c>./build.sh</c> so
    /// the fixture does not rebuild the host it is measuring. The verb's exit class is
    /// deliberately not asserted: <c>doctor</c> returns class 3 in an environment
    /// missing a pinned tool, and this fixture is about identity, not about the
    /// toolchain. What it does require is that the result document exists and carries
    /// the identity field, which is the tool's diagnostic header.
    /// </remarks>
    private static string ReadToolIdentity()
    {
        string hostAssembly = Path.Combine(
            TestArtifacts.RepositoryRoot,
            "src",
            "MechaMiner.Tools",
            "bin",
            "Debug",
            "net8.0",
            "MechaMiner.Tools.dll");
        if (!File.Exists(hostAssembly))
        {
            EngineRunner.CommandOutcome hostBuild = EngineRunner.RunProcess(
                "dotnet",
                new[]
                {
                    "build",
                    Path.Combine(TestArtifacts.RepositoryRoot, "src", "MechaMiner.Tools", "MechaMiner.Tools.csproj"),
                    "--nologo",
                    "-v",
                    "quiet",
                },
                TestArtifacts.RepositoryRoot,
                TimeSpan.FromMinutes(10),
                environment: null);
            Assert.That(
                hostBuild.ExitCode,
                Is.Zero,
                () => "the workflow host must build before its identity can be read:\n" + hostBuild.Output);
        }

        string resultPath = Path.Combine(
            TestArtifacts.RepositoryRoot,
            "artifacts",
            "verbs",
            ToolVerb,
            "latest-result.json");
        if (File.Exists(resultPath))
        {
            File.Delete(resultPath);
        }

        EngineRunner.CommandOutcome outcome = EngineRunner.RunProcess(
            "dotnet",
            new[] { hostAssembly, TestArtifacts.RepositoryRoot, ToolVerb },
            TestArtifacts.RepositoryRoot,
            TimeSpan.FromMinutes(5),
            environment: null);

        Assert.That(
            File.Exists(resultPath),
            Is.True,
            () => "the workflow host must write " + TestArtifacts.Relative(resultPath)
                + "; it exited " + outcome.ExitCode + " with:\n" + outcome.Output);

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(resultPath));
        Assert.That(
            document.RootElement.TryGetProperty("build_identity", out JsonElement identity),
            Is.True,
            "the tool's structured result must carry build_identity (doc 100 § Version and build identity)");
        return identity.GetString() ?? string.Empty;
    }

    /// <summary>Runs the Godot process and reads the identity out of the engine runner report.</summary>
    private static string ReadGameIdentity()
    {
        EngineRunResult result = EngineRunner.Run("pass");
        Assert.That(
            result.Outcome,
            Is.EqualTo(EngineRunOutcome.Passed),
            () => "the engine pass case must succeed before its identity can be trusted:\n" + result.Output);

        EngineRunnerReport report = result.Report!;
        List<string> failedAssertions = new();
        foreach (EngineRunnerAssertion assertion in report.Assertions)
        {
            if (!assertion.Passed)
            {
                failedAssertions.Add(assertion.Name + ": " + assertion.Detail);
            }
        }

        Assert.That(failedAssertions, Is.Empty);
        return report.BuildIdentity;
    }
}
