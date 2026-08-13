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
/// A filesystem tripwire that makes "no pure NUnit test process launched Godot" a
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
/// sentinel file and exits nonzero. If the sentinel exists afterwards, something the
/// tripwire armed tried to launch the engine and the tier fails with the recorded
/// command line. The tripwire never launches the real engine, so a violation is caught
/// rather than performed.
/// </para>
/// <para>
/// WHAT THIS DOES NOT COVER, stated because the assertion used to be printed as
/// though it covered everything the verb did. The shim reaches only the processes this
/// class hands its <see cref="Environment"/> to, which is the pure NUnit test
/// processes. <see cref="VerbContext.RunRepositoryScript"/> passes no environment, so
/// the gate scripts <c>test-fast</c> runs in stages 1 and 2 inherit the verb host's own
/// PATH and never see the shim. Their nested <c>./build.sh</c> invocations do launch the
/// pinned engine - <c>build/verify-verbs.sh</c> probes it through <c>doctor</c> - and an
/// empty sentinel says nothing about them. Reporting "no process in the pure tier tried
/// to launch the engine" therefore asserted a fact over a region this tripwire is blind
/// to, and the recorded assertion no longer says it.
/// </para>
/// <para>
/// Closing that gap means propagating the tripwire environment through
/// <c>RunRepositoryScript</c>, which is FND-003's to decide because the hard part is not
/// the plumbing. A gate script legitimately runs <c>./build.sh doctor</c>, and doctor's
/// job is to probe the pinned engine; arming the shim for gate scripts would either fail
/// <c>doctor</c> or require an exception for it, and an exception is how a tripwire stops
/// being one. Whoever takes it has to answer that question first, not the plumbing.
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
            // Scoped to the processes the shim actually reached. The wider claim - "no
            // process in the pure tier tried to launch the engine" - was false whenever a
            // gate script this verb runs invoked ./build.sh doctor, because
            // RunRepositoryScript passes no environment and those processes never see the
            // shim. Saying less is the fix; see the GodotTripwire remarks.
            context.Runner.RecordAssertion(
                "no-godot-launched",
                true,
                "a '" + shimName + "' shim was first on PATH and MECHAMINER_GODOT pointed at it for every "
                + "pure NUnit test process this verb started, and it was never invoked, so no test process "
                + "tried to launch the engine. This covers the test processes only: the gate scripts run in "
                + "stages 1 and 2 are started without this environment, so their own nested ./build.sh "
                + "invocations are outside what this assertion can observe");
            return;
        }

        string recorded = File.ReadAllText(SentinelPath).Trim();
        context.Runner.RecordAssertion(
            "no-godot-launched",
            false,
            "a pure NUnit test process tried to launch Godot, which doc 91 § Test project separation "
            + "forbids. Recorded invocation(s): " + recorded.Replace('\n', ';'));
        failures.Add("a pure test process launched Godot (see the no-godot-launched step)");
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
        "tests/MechaMiner.Tools.Tests/MechaMiner.Tools.Tests.csproj",
    };

    private const string EngineTestProject = "tests/MechaMiner.Game.Tests/MechaMiner.Game.Tests.csproj";

    /// <summary>
    /// Engine-tier gate scripts the main tier runs: (step name, script, what a
    /// failure means).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only scripts that drive the pinned engine and the SDK directly belong here. An
    /// earlier version of this remark said a gate whose subject is <c>./build.sh</c>
    /// cannot be reached from any verb, because the wrapper rebuilds the verb host
    /// assembly the calling verb is executing from. That does not reproduce. What does
    /// constrain placement is recursion - a gate that invokes <c>./build.sh &lt;verb&gt;</c>
    /// cannot live in that verb - and <c>build/verify-gate-wiring.sh</c>'s exemption
    /// list states per script what was observed.
    /// </para>
    /// <para>
    /// A gate placed here is not automation. No workflow invokes <c>test-main</c>, so a
    /// script in this table runs only when a person types the verb, which is why
    /// <c>build/verify-godot-runner.sh</c> is exempt in
    /// <c>build/verify-gate-wiring.sh</c> rather than counted as wired. OPS-001's
    /// main-branch suite is the workflow that would change that.
    /// </para>
    /// </remarks>
    private static readonly (string Step, string Script, string Claim)[] MainTierContractGates =
    {
        (
            "verify-godot-runner",
            "build/verify-godot-runner.sh",
            "the Godot integration runner's pass/fail/timeout/report contract is broken"),
    };

    /// <summary>
    /// Wrapper-contract gate scripts the fast tier runs: (step name, script, what a
    /// failure means).
    /// </summary>
    /// <remarks>
    /// Three of the four have <c>./build.sh build</c> as their subject, which is the verb
    /// that would otherwise own them, and that is exactly why they are here: each invokes
    /// <c>./build.sh build</c> itself, so from <c>build</c> they recurse without
    /// bound - observed as a 200 s timeout with the wrapper still nesting.
    /// <c>test-fast</c> is the next verb the CI workflow runs and it invokes neither. The
    /// fourth, <c>build/verify-registry.sh</c>, never touches the wrapper and is here
    /// because it needs the test assemblies this verb builds.
    /// They were exempted from <c>build/verify-gate-wiring.sh</c> on a shared reason that
    /// did not reproduce; wiring each one and running this verb end to end is what
    /// retired it.
    /// <para>
    /// A hazard in that placement decision, written down because it is a dependency on a
    /// property of someone else's fixtures. <c>build/verify-verbs.sh</c> wired into
    /// <c>build</c> also exits 0, in 114 s, and it would be the more natural home for a
    /// gate about the wrapper. It survives there only because every nested
    /// <c>./build.sh build</c> it makes is deliberately broken first - § 8 and § 9 write
    /// an uncompilable fixture, § 10 breaks the SDK pin - so each nested build fails at
    /// compile or at the pin probe and never reaches the stage that would call the gate
    /// again. If those fixtures ever stop breaking compilation, that placement becomes
    /// unbounded recursion with no other change. It is here instead, where the recursion
    /// cannot arise at all, and the 114 s measurement is not a reason to move it.
    /// </para>
    /// </remarks>
    private static readonly (string Step, string Script, string Claim)[] FastTierWrapperGates =
    {
        (
            "verify-verbs",
            "build/verify-verbs.sh",
            "the wrapper's verb table, argument validation, or exit classification is broken"),
        (
            "verify-configurations",
            "build/verify-configurations.sh",
            "doc 100's three configuration names no longer map 1:1 onto MSBuild's"),

        // The two gates FND-004 and FND-009 wrote and left unwired. PR #7 recorded that as
        // an open item on the reasoning that wiring a gate is a workflow-contract decision
        // rather than part of writing the validator - which was true when nothing asserted
        // the partition. build/verify-gate-wiring.sh now does, and its exemption list says
        // in its own header that it "is not a place to park a script that could simply be
        // wired". Both of these can be, so both are, and the open item is closed rather
        // than restated.
        //
        // Here rather than in `build` for the same reason as the two above, measured the
        // same way. build/verify-build-identity.sh runs `./build.sh build` at line 73, so
        // from `build` it recurses; test-fast invokes neither `build` nor itself through
        // it. build/verify-registry.sh runs `dotnet build MechaMiner.sln` and
        // `dotnet test` directly and never touches the wrapper, so no placement recurses -
        // it is here beside the other repository-consistency gates rather than in `build`
        // because it needs the test assemblies built and test-fast is where that happens.
        (
            "verify-registry",
            "build/verify-registry.sh",
            "the specification's identifiers, cross-links or verification registries are broken, "
            + "or the retained audit evidence does not match what the tests assert"),
        (
            "verify-build-identity",
            "build/verify-build-identity.sh",
            "the three build-identity surfaces no longer read one baked assembly and agree"),
    };

    /// <summary>Runs the pure tiers and the build-policy fixtures, and launches no Godot process.</summary>
    internal static VerbOutcome RunFastTier(VerbContext context)
    {
        GodotTripwire tripwire = GodotTripwire.Arm(context);
        StageLedger ledger = new(
            context,
            "build-policy fixtures",
            "wrapper-contract gate scripts",
            "pure NUnit tiers (no Godot)",
            "assert no pure NUnit test process launched Godot");

        ledger.Enter(0);
        CommandResult policies = context.RunRepositoryScript(
            "verify-policies",
            "build/verify-policies.sh",
            scriptArguments: null,
            timeout: TimeSpan.FromMinutes(20));
        if (!policies.Succeeded)
        {
            return ledger.Abandon(VerbOutcome.Validation(
                "a build-policy fixture no longer proves its policy; see the step log"));
        }

        ledger.Enter(1);
        foreach ((string step, string script, string claim) in FastTierWrapperGates)
        {
            CommandResult wrapperGate = context.RunRepositoryScript(
                step,
                script,
                scriptArguments: null,
                timeout: TimeSpan.FromMinutes(20));
            if (!wrapperGate.Succeeded)
            {
                return ledger.Abandon(VerbOutcome.Validation(claim + "; see the " + step + " step log"));
            }
        }

        ledger.Enter(2);
        List<TestTally> tallies = new();
        List<string> failures = new();
        foreach (string project in PureTestProjects)
        {
            VerbOutcome? failure = RunTestProject(context, project, tallies, failures, tripwire.Environment);
            if (failure is not null)
            {
                return ledger.Abandon(failure);
            }
        }

        ledger.Enter(3);
        tripwire.Assert(context, failures);

        return Summarize(context, tallies, failures, "test-fast");
    }

    /// <summary>Runs the fast tier, a clean headless import, and the Godot engine tier.</summary>
    internal static VerbOutcome RunMainTier(VerbContext context)
    {
        StageLedger ledger = new(
            context,
            "the complete fast tier",
            "clean headless Godot import",
            "engine-tier gate scripts",
            "Godot engine integration tier");

        ledger.Enter(0);
        VerbOutcome fast = RunFastTier(context);
        if (fast.ExitClass != ExitClass.Success)
        {
            return ledger.Abandon(fast);
        }

        ledger.Enter(1);
        VerbOutcome import = GodotImportVerb.Execute(context);
        if (import.ExitClass != ExitClass.Success)
        {
            return ledger.Abandon(import);
        }

        ledger.Enter(2);

        // verify-godot-runner.sh drives the pinned engine and the SDK directly, which
        // is the definition of this tier, and it was previously invoked by nothing at
        // all: it ran only when a person remembered to type it. It does not touch
        // ./build.sh, so reaching it from a verb is safe.
        foreach ((string step, string script, string claim) in MainTierContractGates)
        {
            CommandResult gate = context.RunRepositoryScript(
                step,
                script,
                scriptArguments: null,
                timeout: TimeSpan.FromMinutes(45));
            if (!gate.Succeeded)
            {
                return ledger.Abandon(VerbOutcome.Validation(claim + "; see the " + step + " step log"));
            }
        }

        ledger.Enter(3);
        List<TestTally> tallies = new();
        List<string> failures = new();
        VerbOutcome? failure = RunTestProject(context, EngineTestProject, tallies, failures);
        if (failure is not null)
        {
            return ledger.Abandon(failure);
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
