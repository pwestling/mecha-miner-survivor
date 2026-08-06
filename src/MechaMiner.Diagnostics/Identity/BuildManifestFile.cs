using System;
using System.Collections.Generic;
using System.IO;

namespace MechaMiner.Diagnostics.Identity;

/// <summary>The outcome of comparing an on-disk manifest with the compiled identity.</summary>
/// <remarks>
/// Expected rejection is typed result data rather than an exception, per doc 114 § C#
/// and domain defaults. A stale or missing manifest is expected: it is exactly what a
/// staleness gate is looking for.
/// </remarks>
public sealed class BuildManifestComparison
{
    private readonly List<string> _differences = new();

    private BuildManifestComparison(string status, string detail)
    {
        Status = status;
        Detail = detail;
    }

    /// <summary>The manifest on disk matches the compiled identity exactly.</summary>
    public const string CurrentStatus = "current";

    /// <summary>No manifest exists on disk.</summary>
    public const string MissingStatus = "missing";

    /// <summary>A manifest exists but does not match the compiled identity.</summary>
    public const string StaleStatus = "stale";

    /// <summary>A manifest exists but is not a readable <c>SCH-BLD-001</c> document.</summary>
    public const string UnreadableStatus = "unreadable";

    /// <summary>One of <see cref="CurrentStatus"/>, <see cref="MissingStatus"/>, <see cref="StaleStatus"/>, or <see cref="UnreadableStatus"/>.</summary>
    public string Status { get; }

    /// <summary>A single human-readable sentence naming the outcome.</summary>
    public string Detail { get; }

    /// <summary>Field-level differences, empty unless <see cref="Status"/> is stale.</summary>
    public IReadOnlyList<string> Differences => _differences;

    /// <summary>Whether the on-disk manifest is current.</summary>
    public bool IsCurrent => string.Equals(Status, CurrentStatus, StringComparison.Ordinal);

    /// <summary>The manifest matches.</summary>
    internal static BuildManifestComparison Current(string path)
    {
        return new BuildManifestComparison(
            CurrentStatus,
            path + " matches the compiled build identity");
    }

    /// <summary>The manifest is absent.</summary>
    internal static BuildManifestComparison Missing(string path)
    {
        return new BuildManifestComparison(
            MissingStatus,
            path + " does not exist; run the build verb to generate it");
    }

    /// <summary>The manifest cannot be read as <c>SCH-BLD-001</c>.</summary>
    internal static BuildManifestComparison Unreadable(string path, string reason)
    {
        return new BuildManifestComparison(
            UnreadableStatus,
            path + " is not a readable SCH-BLD-001 document: " + reason);
    }

    /// <summary>The manifest differs from the compiled identity.</summary>
    internal static BuildManifestComparison Stale(string path, IReadOnlyList<string> differences)
    {
        BuildManifestComparison comparison = new(
            StaleStatus,
            path + " does not match the compiled build identity");
        comparison._differences.AddRange(differences);
        return comparison;
    }
}

/// <summary>
/// Reads and writes the generated <c>SCH-BLD-001</c> manifest file.
/// </summary>
/// <remarks>
/// <para>
/// The manifest is a build output, not a committed source file. It records the source
/// commit of the build that produced it, so a committed copy would be stale the moment
/// the commit that contains it exists: the file cannot name its own commit. It is
/// therefore written into <c>generated/</c> and excluded from version control, and the
/// staleness relation a reviewer actually needs — "does the manifest on disk match the
/// assembly that was just built" — is what <see cref="Compare"/> answers.
/// </para>
/// <para>
/// The path is passed in by the caller. Nothing here derives a location from the
/// current working directory, an environment variable, or a username
/// (<c>TR-PST-006</c>, doc 70 § Local file layout and encoding).
/// </para>
/// </remarks>
public static class BuildManifestFile
{
    /// <summary>The repository-relative path of the generated manifest.</summary>
    public const string RepositoryRelativePath = "generated/build-manifest.json";

    /// <summary>
    /// Writes the compiled identity to <paramref name="absolutePath"/> and returns the
    /// exact text written.
    /// </summary>
    public static string Write(string absolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        string? directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = DiagnosticsJsonContext.Serialize(BuildIdentity.ToManifest());

        // Written through a temporary file in the same directory and then moved, so a
        // reader never observes a half-written manifest and a failed write leaves the
        // previous one intact (doc 70 § Local file layout and encoding).
        string temporary = absolutePath + ".partial";
        File.WriteAllText(temporary, json);
        File.Move(temporary, absolutePath, overwrite: true);
        return json;
    }

    /// <summary>
    /// Reads only the canonical identity line out of a manifest on disk.
    /// </summary>
    /// <remarks>
    /// The consumer of the equality gate needs one string, not the typed manifest, so this is
    /// the whole of the read surface. The line is recomputed from the document's own fields and
    /// compared with the stored line, so a manifest whose identity line disagrees with its own
    /// contents is rejected here rather than quietly compared.
    /// </remarks>
    public static string ReadIdentityLine(string absolutePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        BuildManifest manifest = DiagnosticsJsonContext.DeserializeManifest(File.ReadAllText(absolutePath));
        string derived = BuildIdentity.RenderIdentityLine(manifest);
        if (!string.Equals(derived, manifest.IdentityLine, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "the manifest at " + absolutePath + " carries an identity line that is not derivable from its "
                + "own fields: stored '" + manifest.IdentityLine + "', derived '" + derived + "'");
        }

        return manifest.IdentityLine;
    }

    /// <summary>Compares the manifest at <paramref name="absolutePath"/> with the compiled identity.</summary>
    public static BuildManifestComparison Compare(string absolutePath, string displayPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

        if (!File.Exists(absolutePath))
        {
            return BuildManifestComparison.Missing(displayPath);
        }

        BuildManifest onDisk;
        try
        {
            onDisk = DiagnosticsJsonContext.DeserializeManifest(File.ReadAllText(absolutePath));
        }
        catch (System.Text.Json.JsonException exception)
        {
            return BuildManifestComparison.Unreadable(displayPath, exception.Message);
        }

        string expected = DiagnosticsJsonContext.Serialize(BuildIdentity.ToManifest());
        string actual = DiagnosticsJsonContext.Serialize(onDisk);
        if (string.Equals(expected, actual, StringComparison.Ordinal))
        {
            return BuildManifestComparison.Current(displayPath);
        }

        return BuildManifestComparison.Stale(displayPath, Differences(BuildIdentity.Current, onDisk));
    }

    private static List<string> Differences(BuildManifest expected, BuildManifest actual)
    {
        List<string> differences = new();
        Compare(differences, "schema", expected.Schema, actual.Schema);
        Compare(differences, "identity_line", expected.IdentityLine, actual.IdentityLine);
        Compare(differences, "product.version", expected.Product.Version, actual.Product.Version);
        Compare(
            differences,
            "product.build_number",
            expected.Product.BuildNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            actual.Product.BuildNumber.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Compare(differences, "source.commit", expected.Source.Commit, actual.Source.Commit);
        Compare(differences, "source.dirty", expected.Source.Dirty, actual.Source.Dirty);
        Compare(differences, "toolchain.godot_version", expected.Toolchain.GodotVersion, actual.Toolchain.GodotVersion);
        Compare(
            differences,
            "toolchain.dotnet_sdk_version",
            expected.Toolchain.DotnetSdkVersion,
            actual.Toolchain.DotnetSdkVersion);
        Compare(differences, "content.bundle_sha256", expected.Content.BundleSha256, actual.Content.BundleSha256);
        Compare(
            differences,
            "target.workflow_configuration",
            expected.Target.WorkflowConfiguration,
            actual.Target.WorkflowConfiguration);
        Compare(differences, "target.platform", expected.Target.Platform, actual.Target.Platform);
        return differences;
    }

    private static void Compare(List<string> differences, string field, string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            differences.Add(field + ": compiled '" + expected + "', on disk '" + actual + "'");
        }
    }
}
