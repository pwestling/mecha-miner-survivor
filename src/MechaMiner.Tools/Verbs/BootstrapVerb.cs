using System;
using System.Collections.Generic;
using System.IO;
using MechaMiner.Tools.Cli;
using MechaMiner.Tools.Toolchain;

namespace MechaMiner.Tools.Verbs;

/// <summary>
/// <c>bootstrap</c>: "restore/download allowed repository-local tools, then run
/// <c>doctor</c>"
/// (<c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Standard command surface).
/// </summary>
/// <remarks>
/// <para>
/// This verb is the only entry point to environment setup. <c>AGENTS.md</c>
/// forbids competing workflow entrypoints, so <c>build/bootstrap-linux.sh</c> is
/// now the platform installer this verb invokes rather than a second entry point a
/// developer is expected to find.
/// </para>
/// <para>
/// Doc 100 § Toolchain pinning: a bootstrap command "never mutates global developer
/// configuration silently". This verb therefore does three separable things and
/// says which is which: it always performs the repository-local locked restore; it
/// invokes the platform installer only when a required system tool is actually
/// missing and the process can write the pinned install directories; and otherwise
/// it prints the exact manual command instead of attempting a privileged action.
/// </para>
/// </remarks>
internal static class BootstrapVerb
{
    /// <summary>Restores repository-local tools, installs missing pinned system tools, then runs doctor.</summary>
    internal static VerbOutcome Execute(VerbContext context)
    {
        ToolchainInspector inspector = new(context.Layout, context.Runner);
        ToolchainPins pins = inspector.LoadPins();

        context.Section("stage 1: probe the pinned system toolchain");
        List<ToolProbe> before = inspector.Probe(pins);
        List<ToolProbe> missing = new();
        foreach (ToolProbe probe in before)
        {
            if (probe.IsBlocking)
            {
                missing.Add(probe);
            }
        }

        List<string> warnings = new();

        if (missing.Count > 0)
        {
            context.Section("stage 2: install the missing pinned system tools");
            VerbOutcome? installFailure = InstallSystemTools(context, missing, warnings);
            if (installFailure is not null)
            {
                return installFailure;
            }
        }
        else
        {
            context.Section("stage 2: no pinned system tool is missing (idempotent no-op)");
            context.Runner.RecordAssertion(
                "system-tools",
                succeeded: true,
                "every required pinned tool is already present; nothing installed");
        }

        context.Section("stage 3: restore repository-local packages in locked mode");
        CommandResult restore = context.Runner.Run(
            "dotnet-restore-locked",
            "dotnet",
            new[] { "restore", context.Layout.Solution, "--locked-mode", "--nologo" },
            context.Layout.Root,
            TimeSpan.FromMinutes(10));
        if (!restore.Succeeded)
        {
            return VerbOutcome
                .Environment(
                    "locked restore failed; the committed lock files and Directory.Packages.props disagree "
                    + "with what restore would produce. See the step log.")
                .WithWarnings(warnings);
        }

        context.Section("stage 4: run doctor");
        List<ToolProbe> after = inspector.Probe(pins);
        VerbOutcome doctorOutcome = DoctorVerb.Report(context, pins, after);
        if (doctorOutcome.ExitClass != ExitClass.Success)
        {
            return doctorOutcome.WithWarnings(warnings);
        }

        VerbOutcome bootstrapped = VerbOutcome
            .Success("bootstrap complete: repository-local packages restored in locked mode, then "
                + doctorOutcome.FinalResult)
            .WithWarnings(warnings)
            .WithWarnings(doctorOutcome.Warnings);
        foreach (string artifact in doctorOutcome.Artifacts)
        {
            bootstrapped.WithArtifact(artifact);
        }

        return bootstrapped;
    }

    private static VerbOutcome? InstallSystemTools(
        VerbContext context,
        List<ToolProbe> missing,
        List<string> warnings)
    {
        string script = context.Layout.Absolute("build", "bootstrap-linux.sh");
        bool canInstall = OperatingSystem.IsLinux() && File.Exists(script) && HasWriteAccessToSystemPaths();

        List<string> names = new();
        foreach (ToolProbe probe in missing)
        {
            names.Add(probe.Tool);
        }

        if (!canInstall)
        {
            string reason = !OperatingSystem.IsLinux()
                ? "there is no platform installer for " + ToolchainInspector.PlatformKey() + " yet"
                : "this process cannot write the pinned system install directories";

            context.Runner.RecordAssertion(
                "system-tools",
                succeeded: false,
                reason + "; printing the exact manual installation command instead of attempting it");

            string instructions = string.Join(
                "\n",
                "Missing pinned tools: " + string.Join(", ", names),
                string.Empty,
                "Run exactly this, from the repository root, as a user who may write",
                "/usr/share/dotnet and /opt/godot:",
                string.Empty,
                "    sudo build/bootstrap-linux.sh",
                string.Empty,
                "then re-run:",
                string.Empty,
                "    ./build.sh bootstrap",
                string.Empty,
                "The installer is idempotent: it revalidates and skips what is already correct.",
                "It writes only /usr/share/dotnet, /opt/godot, /usr/local/bin symlinks, and apt",
                "packages, and it never edits a developer's shell profile or global NuGet",
                "configuration (docs/technical/100 § Toolchain pinning).");

            string path = context.WriteArtifact("manual-installation.txt", instructions + "\n");
            context.Console.WriteLine();
            context.Console.WriteLine(instructions);

            return VerbOutcome
                .Environment("missing pinned tools: " + string.Join(", ", names)
                    + ". Exact manual installation instructions written to " + path)
                .WithWarnings(warnings)
                .WithArtifact(path);
        }

        CommandResult install = context.Runner.Run(
            "bootstrap-platform-installer",
            "bash",
            new[] { script },
            context.Layout.Root,
            TimeSpan.FromMinutes(20));

        if (!install.Succeeded)
        {
            return VerbOutcome
                .Environment("build/bootstrap-linux.sh failed to install " + string.Join(", ", names)
                    + "; see the step log")
                .WithWarnings(warnings);
        }

        warnings.Add("bootstrap installed pinned system tools: " + string.Join(", ", names));
        return null;
    }

    private static bool HasWriteAccessToSystemPaths()
    {
        // The installer writes /usr/share/dotnet, /opt/godot, and /usr/local/bin.
        // Probing writability is more honest than assuming an effective user id.
        foreach (string directory in new[] { "/usr/local/bin", "/opt" })
        {
            try
            {
                string probe = Path.Combine(directory, ".mechaminer-bootstrap-probe");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        return true;
    }
}
