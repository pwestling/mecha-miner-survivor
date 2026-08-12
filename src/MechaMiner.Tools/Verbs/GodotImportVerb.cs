using System;
using MechaMiner.Tools.Cli;

namespace MechaMiner.Tools.Verbs;

/// <summary>
/// <c>godot-import</c>: "clean headless import/check with captured warnings"
/// (<c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Standard command surface).
/// </summary>
/// <remarks>
/// <para>
/// The verb routes to <c>build/verify-godot.sh</c>, which is the only place the
/// cold-cache ordering is encoded: <c>Godot.NET.Sdk</c> puts both <c>obj/</c> and
/// <c>bin/</c> for <c>MechaMiner.Game</c> inside <c>game/.godot/mono/temp/</c> and
/// <c>.godot</c> is ignored, so a clean checkout must restore, build, import, and
/// only then launch.
/// </para>
/// <para>
/// The gate is the captured report, not the process exit code. A headless Godot
/// launch exits <c>0</c> even when the C# script on the boot node fails to load: it
/// logs <c>Cannot instantiate C# script</c> and carries on. FND-001 hit this
/// empirically, which is why the script asserts the stable startup line and the
/// absence of engine <c>ERROR</c> and <c>WARNING</c> lines in addition to the exit
/// code.
/// </para>
/// </remarks>
internal static class GodotImportVerb
{
    /// <summary>Performs a cold-cache headless import and asserts the captured report.</summary>
    internal static VerbOutcome Execute(VerbContext context)
    {
        context.Section("cold-cache headless import and launch assertions");
        CommandResult result = context.RunRepositoryScript(
            "verify-godot",
            "build/verify-godot.sh",
            scriptArguments: null,
            timeout: TimeSpan.FromMinutes(20));

        string logNote = "the captured import and launch logs are in the step log; "
            + "Godot exits 0 even for engine ERROR lines, so the assertions above are the gate";
        context.Runner.RecordAssertion("report-is-the-gate", result.Succeeded, logNote);

        if (result.TimedOut)
        {
            return VerbOutcome.Build("headless import did not finish inside its bounded timeout");
        }

        if (result.ExitCode != 0)
        {
            return result.ExitCode switch
            {
                4 => VerbOutcome.Validation(
                    "headless import or launch produced an unexpected report; see the step log"),
                5 => VerbOutcome.Build("the game assembly did not build, so Godot could not load it"),
                _ => VerbOutcome.Build(
                    "build/verify-godot.sh returned an unclassified exit code; treating it as a build failure"),
            };
        }

        return RunSliceEvidence(context);
    }

    /// <summary>
    /// Asserts the run slice evidence harness ran during this invocation
    /// (<c>VER-PRE-001-003</c> through <c>VER-PRE-001-007</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ordered after <c>build/verify-godot.sh</c> on purpose and not merely by habit: that
    /// script is the only place the cold-cache ordering is encoded, and the harness cannot
    /// reach managed code until it has restored, built and imported. So
    /// <c>build/verify-run-slice.sh</c> deliberately repeats none of that and defines no
    /// class 5.
    /// </para>
    /// <para>
    /// The harness's own class is forwarded UNCHANGED, per
    /// <c>docs/technical/100-build-dependencies-and-release-operations.md</c> § Standard
    /// command surface ("wrappers preserve the owning tool's class rather than returning
    /// success after partial work"). <c>2</c> is the harness's missing-output-directory
    /// refusal and <c>4</c> its assertion failure; <c>3</c> is the gate's own class for an
    /// absent or unpinned Godot, and this is the repository's first live consumer of it.
    /// The one place pass-through does not hold is a timeout: <see cref="CommandResult"/>
    /// carries <c>-1</c> then, and the branch below returns class 5 before any code is read.
    /// </para>
    /// </remarks>
    private static VerbOutcome RunSliceEvidence(VerbContext context)
    {
        context.Section("run slice evidence harness assertions");
        CommandResult result = context.RunRepositoryScript(
            "verify-run-slice",
            "build/verify-run-slice.sh",
            scriptArguments: null,
            timeout: TimeSpan.FromMinutes(20));

        string harnessNote = "the harness transcript is retained under "
            + "artifacts/engine-tier/verify-run-slice/; a headless Godot launch exits 0 even "
            + "when a script fails to attach, so the transcript and the startup line are the gate";
        context.Runner.RecordAssertion("harness-ran-this-invocation", result.Succeeded, harnessNote);

        if (result.TimedOut)
        {
            return VerbOutcome.Build(
                "the run slice evidence harness did not finish inside its bounded timeout");
        }

        return result.ExitCode switch
        {
            0 => VerbOutcome.Success(
                "headless import passed and the run slice evidence harness ran and passed this invocation"),
            2 => VerbOutcome.InvalidInvocation(
                DiagnosticCodes.InvalidArgument,
                "the run slice evidence harness was given no output directory, so it evaluated nothing"),
            3 => VerbOutcome.Environment(
                "the pinned Godot is absent or does not match its pin; repair the machine, not the repository"),
            4 => VerbOutcome.Validation(
                "the run slice evidence harness did not run, or an assertion inside it failed; see the step log"),
            _ => VerbOutcome.Build(
                "build/verify-run-slice.sh returned an unclassified exit code; treating it as a build failure"),
        };
    }
}
