using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using MechaMiner.Tools.Cli;

namespace MechaMiner.Tools.Toolchain;

/// <summary>
/// Reads the pinned toolchain and reports what is actually installed.
/// </summary>
/// <remarks>
/// <para>
/// This type never installs, downloads, upgrades, or writes outside
/// <c>artifacts/</c>. It is the read-only half that <c>doctor</c> uses directly and
/// that <c>bootstrap</c> uses before and after it changes anything.
/// </para>
/// <para>
/// A tool whose owning work package has not landed is reported as
/// <see cref="ToolStatus.Deferred"/>, not as missing. Doc 100 § Toolchain pinning
/// lists Blender and export templates among the pinned tools, but the derivation
/// scripts and export presets that need them are owned by <c>AST-002</c> and
/// <c>FND-006</c>; failing <c>doctor</c> on their absence would make the verb
/// unusable in every environment until those packages land, which would defeat the
/// gate rather than strengthen it.
/// </para>
/// </remarks>
internal sealed class ToolchainInspector
{
    private readonly RepositoryLayout _layout;
    private readonly CommandRunner _runner;

    internal ToolchainInspector(RepositoryLayout layout, CommandRunner runner)
    {
        _layout = layout;
        _runner = runner;
    }

    /// <summary>Loads and validates <c>build/toolchain.json</c>.</summary>
    internal ToolchainPins LoadPins()
    {
        string path = _layout.ToolchainPins;
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("missing pinned toolchain record at " + _layout.Relative(path));
        }

        try
        {
            return ToolsJsonContext.DeserializePins(File.ReadAllText(path));
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                _layout.Relative(path) + " is not a valid pin document: " + exception.Message,
                exception);
        }
    }

    /// <summary>Reads the pinned SDK version out of <c>global.json</c>.</summary>
    internal string ReadPinnedSdkVersion()
    {
        string path = _layout.GlobalJson;
        using FileStream stream = File.OpenRead(path);
        using JsonDocument document = JsonDocument.Parse(stream);
        if (document.RootElement.TryGetProperty("sdk", out JsonElement sdk)
            && sdk.TryGetProperty("version", out JsonElement version)
            && version.GetString() is string text)
        {
            return text;
        }

        throw new InvalidOperationException(_layout.Relative(path) + " does not declare sdk.version");
    }

    /// <summary>Probes every pinned and deferred tool and returns one report row each.</summary>
    internal List<ToolProbe> Probe(ToolchainPins pins)
    {
        List<ToolProbe> probes = new()
        {
            ProbeDotnetSdk(pins.DotnetSdk),
        };
        probes.AddRange(ProbeGodot(pins.Godot));
        probes.Add(ProbeExportTemplates(pins.GodotExportTemplates));

        foreach (RequiredCommandPin command in pins.RequiredCommands)
        {
            probes.Add(ProbeRequiredCommand(command));
        }

        foreach (OptionalToolPin tool in pins.OptionalTools)
        {
            probes.Add(ProbeOptionalTool(tool));
        }

        return probes;
    }

    /// <summary>Resolves the pinned Godot executable, honoring the documented discovery order.</summary>
    internal static string ResolveGodotCommand()
    {
        string? overridePath = Environment.GetEnvironmentVariable("MECHAMINER_GODOT");
        return string.IsNullOrWhiteSpace(overridePath) ? "godot" : overridePath;
    }

    /// <summary>Returns the platform key used by the pin file's platform table.</summary>
    internal static string PlatformKey()
    {
        string architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.X86 => "x86",
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            System.Runtime.InteropServices.Architecture.Arm => "arm",
            _ => "unknown",
        };

        if (OperatingSystem.IsLinux())
        {
            return "linux-" + architecture;
        }

        if (OperatingSystem.IsWindows())
        {
            return "windows-" + architecture;
        }

        if (OperatingSystem.IsMacOS())
        {
            return "osx-" + architecture;
        }

        return "unknown-" + architecture;
    }

    private ToolProbe ProbeDotnetSdk(DotnetSdkPin pin)
    {
        string pinned = ReadPinnedSdkVersion();
        CommandResult listed = _runner.Run(
            "probe-dotnet-sdks",
            "dotnet",
            new[] { "--list-sdks" },
            quiet: true);
        if (!listed.Succeeded)
        {
            return new ToolProbe(
                "dotnet sdk",
                ToolStatus.Mismatched,
                pinned,
                "dotnet --list-sdks failed",
                "run build/bootstrap-linux.sh (or ./build.sh bootstrap) to install the pinned SDK into "
                    + pin.InstallDirectory,
                pin.RequiredBy);
        }

        bool present = false;
        foreach (string line in listed.Output.Split('\n'))
        {
            if (line.StartsWith(pinned + " ", StringComparison.Ordinal))
            {
                present = true;
                break;
            }
        }

        CommandResult resolved = _runner.Run(
            "probe-dotnet-version",
            "dotnet",
            new[] { "--version" },
            quiet: true);
        string observed = resolved.Output.Trim();

        if (!present)
        {
            return new ToolProbe(
                "dotnet sdk",
                ToolStatus.Mismatched,
                pinned,
                observed.Length == 0 ? "absent" : observed,
                "global.json pins " + pinned + "; install it into " + pin.InstallDirectory
                    + " with ./build.sh bootstrap",
                pin.RequiredBy);
        }

        return new ToolProbe(
            "dotnet sdk",
            ToolStatus.Ok,
            pinned,
            observed,
            "resolved through " + pin.VersionAuthority,
            pin.RequiredBy);
    }

    private List<ToolProbe> ProbeGodot(GodotPin pin)
    {
        List<ToolProbe> probes = new();
        string command = ResolveGodotCommand();
        CommandResult version = _runner.Run(
            "probe-godot-version",
            command,
            new[] { "--headless", "--version" },
            quiet: true);

        string observed = version.Output.Trim();
        if (!version.Succeeded || observed.Length == 0)
        {
            probes.Add(new ToolProbe(
                "godot editor",
                ToolStatus.Mismatched,
                pin.ExpectedVersionPrefix,
                observed.Length == 0 ? "absent" : observed,
                "not runnable as '" + command
                    + "'. Discovery order: " + string.Join(", ", pin.DiscoveryOrder)
                    + ". Install with ./build.sh bootstrap.",
                pin.RequiredBy));
            return probes;
        }

        // A headless Godot process can print engine chatter before the version, so
        // the last nonempty line is the version string.
        string versionLine = LastNonEmptyLine(observed);
        if (!versionLine.StartsWith(pin.ExpectedVersionPrefix, StringComparison.Ordinal))
        {
            probes.Add(new ToolProbe(
                "godot editor",
                ToolStatus.Mismatched,
                pin.ExpectedVersionPrefix + ".*",
                versionLine,
                "the pinned editor is " + pin.Version + " " + pin.Flavor + " " + pin.ReleaseChannel,
                pin.RequiredBy));
            return probes;
        }

        probes.Add(new ToolProbe(
            "godot editor",
            ToolStatus.Ok,
            pin.ExpectedVersionPrefix + ".*",
            versionLine,
            "resolved as '" + command + "'",
            pin.RequiredBy));

        probes.Add(ProbeGodotExecutableHash(pin, command));
        return probes;
    }

    private ToolProbe ProbeGodotExecutableHash(GodotPin pin, string command)
    {
        string platform = PlatformKey();
        if (!pin.Platforms.TryGetValue(platform, out GodotPlatformPin? platformPin))
        {
            return new ToolProbe(
                "godot executable hash",
                ToolStatus.Warning,
                "a recorded sha256 for " + platform,
                "no pin recorded",
                "build/toolchain.json records godot.platforms." + platform
                    + " nowhere; the version check above still applies. " + pin.UnpinnedPlatformPolicy,
                pin.RequiredBy);
        }

        // The hash must be taken over the executable that WOULD ACTUALLY RUN - the same
        // one ProbeGodot just executed for its version check - and nothing else.
        //
        // This previously resolved the pinned install path first and fell back to the
        // command only if that path was absent. On any machine with a pinned install
        // present, that meant pointing MECHAMINER_GODOT at a substituted binary produced
        // "resolved as '<substitute>'" from the version probe and "sha256 of
        // /opt/godot/... matches the pin" from this one, on adjacent lines, and doctor
        // exited 0 with "0 mismatches". The probe reported a pin match for a file it had
        // never opened, which is the one thing a hash probe exists to rule out. doctor is
        // what every other gate trusts, so a probe here that validates an assumed
        // canonical path instead of the resolved artifact is load-bearing dishonesty.
        //
        // FindOnPath applies the same discovery rule the runner uses: a rooted path is
        // taken as given, and a bare name is resolved along PATH in PATH order. A symlink
        // is followed by the hash, so the ordinary install shape - `godot` on PATH
        // pointing into the pinned install root - still matches the pin.
        string? executable = FindOnPath(command);
        if (executable is null || !File.Exists(executable))
        {
            return new ToolProbe(
                "godot executable hash",
                ToolStatus.Mismatched,
                platformPin.ExecutableSha256,
                "executable path not resolvable",
                "'" + command + "' does not resolve to a file, so there is nothing to hash. "
                    + "Discovery order: " + string.Join(", ", pin.DiscoveryOrder)
                    + ". The pinned install is "
                    + Path.Combine(platformPin.InstallRoot, platformPin.ExecutableRelativePath),
                pin.RequiredBy);
        }

        string pinnedInstallPath = Path.Combine(platformPin.InstallRoot, platformPin.ExecutableRelativePath);
        string measured = Sha256OfFile(executable);
        bool matches = string.Equals(measured, platformPin.ExecutableSha256, StringComparison.OrdinalIgnoreCase);

        // Naming the hashed file in both branches is deliberate: the reader can see that
        // the path in this row is the path the version row resolved.
        string substitutionNote =
            string.Equals(Path.GetFullPath(executable), Path.GetFullPath(pinnedInstallPath), StringComparison.Ordinal)
                ? string.Empty
                : " (this is not the pinned install path " + pinnedInstallPath
                    + "; it is what '" + command + "' resolves to, and it is what was hashed)";

        return new ToolProbe(
            "godot executable hash",
            matches ? ToolStatus.Ok : ToolStatus.Mismatched,
            platformPin.ExecutableSha256,
            measured,
            matches
                ? "sha256 of " + executable + " matches the pin recorded "
                    + platformPin.RetrievedUtc + substitutionNote
                : "sha256 of " + executable + " does not match the pin" + substitutionNote
                    + "; reinstall from " + platformPin.ArchiveUrl,
            pin.RequiredBy);
    }

    private ToolProbe ProbeExportTemplates(DeferredArchivePin pin)
    {
        return new ToolProbe(
            "godot export templates",
            ToolStatus.Deferred,
            pin.Version + " (" + (pin.ArchiveSizeBytes / (1024L * 1024L)).ToString(CultureInfo.InvariantCulture)
                + " MiB)",
            "not fetched",
            pin.DeferredReason,
            pin.RequiredBy);
    }

    private ToolProbe ProbeRequiredCommand(RequiredCommandPin pin)
    {
        string? located = FindOnPath(pin.Name);
        if (located is null)
        {
            return new ToolProbe(
                pin.Name,
                ToolStatus.Mismatched,
                "present on PATH",
                "absent",
                pin.Reason + "; install it, then re-run ./build.sh doctor",
                "FND-002");
        }

        return new ToolProbe(
            pin.Name,
            ToolStatus.Ok,
            "present on PATH",
            DescribeVersion(pin.Name, located),
            pin.Reason,
            "FND-002");
    }

    private ToolProbe ProbeOptionalTool(OptionalToolPin pin)
    {
        string? located = FindOnPath(pin.Name);
        if (located is null)
        {
            return new ToolProbe(
                pin.Name,
                ToolStatus.Deferred,
                pin.ExpectedVersion,
                "absent",
                pin.DeferredReason,
                pin.RequiredBy);
        }

        return new ToolProbe(
            pin.Name,
            ToolStatus.Ok,
            pin.ExpectedVersion,
            DescribeVersion(pin.Name, located),
            "present; required by " + pin.RequiredBy,
            pin.RequiredBy);
    }

    /// <summary>
    /// Reports a version line when the tool has a machine-readable one. Absence of a
    /// version flag is not a failure: some pinned helpers, notably
    /// <c>xvfb-run</c>, have no version option at all, and treating that as
    /// "absent" would be a false negative.
    /// </summary>
    private string DescribeVersion(string name, string locatedPath)
    {
        string[] arguments = name switch
        {
            "unzip" => new[] { "-v" },
            _ => new[] { "--version" },
        };

        CommandResult result = _runner.Run("probe-" + name, locatedPath, arguments, quiet: true);
        return result.Succeeded && result.Output.Trim().Length > 0
            ? FirstNonEmptyLine(result.Output)
            : "present at " + locatedPath + " (reports no version)";
    }

    /// <summary>Locates an executable on PATH without executing it.</summary>
    private static string? FindOnPath(string name)
    {
        if (Path.IsPathRooted(name))
        {
            return File.Exists(name) ? name : null;
        }

        string? pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (pathVariable is null)
        {
            return null;
        }

        foreach (string directory in pathVariable.Split(Path.PathSeparator))
        {
            if (directory.Length == 0)
            {
                continue;
            }

            string candidate = Path.Combine(directory, name);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (OperatingSystem.IsWindows())
            {
                foreach (string extension in new[] { ".exe", ".cmd", ".bat", ".ps1" })
                {
                    string windowsCandidate = candidate + extension;
                    if (File.Exists(windowsCandidate))
                    {
                        return windowsCandidate;
                    }
                }
            }
        }

        return null;
    }

    private static string Sha256OfFile(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string FirstNonEmptyLine(string text)
    {
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                return trimmed;
            }
        }

        return "(no output)";
    }

    private static string LastNonEmptyLine(string text)
    {
        string result = "(no output)";
        foreach (string line in text.Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length > 0)
            {
                result = trimmed;
            }
        }

        return result;
    }
}
