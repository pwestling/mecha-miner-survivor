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

        return result.ExitCode switch
        {
            0 => VerbOutcome.Success("headless import and launch assertions passed from a cold import cache"),
            4 => VerbOutcome.Validation(
                "headless import or launch produced an unexpected report; see the step log"),
            5 => VerbOutcome.Build("the game assembly did not build, so Godot could not load it"),
            _ => VerbOutcome.Build(
                "build/verify-godot.sh returned an unclassified exit code; treating it as a build failure"),
        };
    }
}
