using System.Collections.Generic;

namespace MechaMiner.Tools.Toolchain;

/// <summary>Typed view of <c>build/toolchain.json</c>.</summary>
/// <remarks>
/// Doc 40 § JSON codec and schema baseline applies to tool configuration as well as
/// content: explicit typed DTOs, source-generated metadata, <c>snake_case</c>
/// property names, and unknown fields are errors rather than silently ignored. The
/// deserializer is configured with
/// <c>JsonUnmappedMemberHandling.Disallow</c> so a stale pin file fails loudly.
/// </remarks>
internal sealed class ToolchainPins
{
    /// <summary>Stable schema identity.</summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>Version of the pin file's shape.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>Editorial purpose and authority notes.</summary>
    public List<string> Purpose { get; set; } = new();

    /// <summary>The .NET SDK pin.</summary>
    public DotnetSdkPin DotnetSdk { get; set; } = new();

    /// <summary>The Godot editor pin.</summary>
    public GodotPin Godot { get; set; } = new();

    /// <summary>The Godot export template pin.</summary>
    public DeferredArchivePin GodotExportTemplates { get; set; } = new();

    /// <summary>Tools whose owning work package has not landed, or which are platform-specific.</summary>
    public List<OptionalToolPin> OptionalTools { get; set; } = new();

    /// <summary>Commands that must exist for the workflow to function at all.</summary>
    public List<RequiredCommandPin> RequiredCommands { get; set; } = new();
}

/// <summary>The pinned .NET SDK.</summary>
internal sealed class DotnetSdkPin
{
    /// <summary>The file that owns the version number.</summary>
    public string VersionAuthority { get; set; } = string.Empty;

    /// <summary>The directory the SDK must be installed into.</summary>
    public string InstallDirectory { get; set; } = string.Empty;

    /// <summary>Why the install directory is not free choice.</summary>
    public string InstallDirectoryReason { get; set; } = string.Empty;

    /// <summary>The official install script bootstrap downloads.</summary>
    public string InstallScriptUrl { get; set; } = string.Empty;

    /// <summary>Whether doctor fails when this tool is absent.</summary>
    public bool Required { get; set; }

    /// <summary>The work package that requires it.</summary>
    public string RequiredBy { get; set; } = string.Empty;
}

/// <summary>The pinned Godot editor.</summary>
internal sealed class GodotPin
{
    /// <summary>The exact editor version.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Release channel; never a preview or nightly (doc 114 § External research defaults).</summary>
    public string ReleaseChannel { get; set; } = string.Empty;

    /// <summary>Distribution flavor; the .NET builds identify as <c>mono</c>.</summary>
    public string Flavor { get; set; } = string.Empty;

    /// <summary>The prefix <c>godot --version</c> output must start with.</summary>
    public string ExpectedVersionPrefix { get; set; } = string.Empty;

    /// <summary>Whether doctor fails when the editor is absent.</summary>
    public bool Required { get; set; }

    /// <summary>The work package that requires it.</summary>
    public string RequiredBy { get; set; } = string.Empty;

    /// <summary>Documented discovery order, reported by doctor so a mismatch is diagnosable.</summary>
    public List<string> DiscoveryOrder { get; set; } = new();

    /// <summary>Per-platform download and hash pins.</summary>
    public Dictionary<string, GodotPlatformPin> Platforms { get; set; } = new();

    /// <summary>What doctor does on a platform with no recorded hash.</summary>
    public string UnpinnedPlatformPolicy { get; set; } = string.Empty;
}

/// <summary>The download and hash pin for one platform.</summary>
internal sealed class GodotPlatformPin
{
    /// <summary>Release-asset URL of the editor archive.</summary>
    public string ArchiveUrl { get; set; } = string.Empty;

    /// <summary>SHA-256 of the archive bytes.</summary>
    public string ArchiveSha256 { get; set; } = string.Empty;

    /// <summary>Exact archive size in bytes.</summary>
    public long ArchiveSizeBytes { get; set; }

    /// <summary>Directory the archive is extracted into.</summary>
    public string InstallRoot { get; set; } = string.Empty;

    /// <summary>Editor executable path relative to the install root.</summary>
    public string ExecutableRelativePath { get; set; } = string.Empty;

    /// <summary>SHA-256 of the extracted editor executable.</summary>
    public string ExecutableSha256 { get; set; } = string.Empty;

    /// <summary>The date the pin was verified against the official download.</summary>
    public string RetrievedUtc { get; set; } = string.Empty;
}

/// <summary>An archive that is pinned but deliberately not fetched yet.</summary>
internal sealed class DeferredArchivePin
{
    /// <summary>The pinned version.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Release-asset URL.</summary>
    public string ArchiveUrl { get; set; } = string.Empty;

    /// <summary>Exact archive size in bytes.</summary>
    public long ArchiveSizeBytes { get; set; }

    /// <summary>Whether doctor fails when it is absent.</summary>
    public bool Required { get; set; }

    /// <summary>The work package that will require it.</summary>
    public string RequiredBy { get; set; } = string.Empty;

    /// <summary>Why it is not fetched yet.</summary>
    public string DeferredReason { get; set; } = string.Empty;
}

/// <summary>A tool doctor reports but does not require yet.</summary>
internal sealed class OptionalToolPin
{
    /// <summary>The command name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The version expectation, or <c>unpinned</c> until its owner pins it.</summary>
    public string ExpectedVersion { get; set; } = string.Empty;

    /// <summary>Whether doctor fails when it is absent.</summary>
    public bool Required { get; set; }

    /// <summary>The work package that will require it.</summary>
    public string RequiredBy { get; set; } = string.Empty;

    /// <summary>Why its absence is not a failure yet.</summary>
    public string DeferredReason { get; set; } = string.Empty;
}

/// <summary>A command the workflow cannot function without.</summary>
internal sealed class RequiredCommandPin
{
    /// <summary>The command name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Why the workflow needs it.</summary>
    public string Reason { get; set; } = string.Empty;
}
