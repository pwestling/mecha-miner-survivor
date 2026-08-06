using System;
using System.IO;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// The accepted repository paths this process needs, resolved once from the
/// repository root the wrapper passed in.
/// </summary>
/// <remarks>
/// Paths come from
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Repository structure. Nothing here searches upward or probes an alternate
/// location: <c>TR-BLD-006</c> requires that clean builds never depend on an
/// alternate search path.
/// </remarks>
internal sealed class RepositoryLayout
{
    private RepositoryLayout(string root)
    {
        Root = root;
    }

    /// <summary>The absolute repository root.</summary>
    internal string Root { get; }

    /// <summary>The one solution that references every C# project.</summary>
    internal string Solution => Path.Combine(Root, "MechaMiner.sln");

    /// <summary>The pinned .NET SDK declaration.</summary>
    internal string GlobalJson => Path.Combine(Root, "global.json");

    /// <summary>The Godot project directory, the only Godot-dependent tree.</summary>
    internal string GameDirectory => Path.Combine(Root, "game");

    /// <summary>Scripts and configuration; never build output.</summary>
    internal string BuildDirectory => Path.Combine(Root, "build");

    /// <summary>The machine-readable toolchain pins read by doctor and bootstrap.</summary>
    internal string ToolchainPins => Path.Combine(BuildDirectory, "toolchain.json");

    /// <summary>Ignored local outputs. Every verb writes its evidence beneath this directory.</summary>
    internal string ArtifactsDirectory => Path.Combine(Root, "artifacts");

    /// <summary>Resolves the layout for an absolute repository root.</summary>
    internal static RepositoryLayout ForRoot(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        string full = Path.GetFullPath(root);
        if (!File.Exists(Path.Combine(full, "MechaMiner.sln")))
        {
            throw new InvalidOperationException(
                "the repository root passed by the wrapper does not contain MechaMiner.sln: " + full);
        }

        return new RepositoryLayout(full);
    }

    /// <summary>Returns <paramref name="absolutePath"/> relative to the repository root, with forward slashes.</summary>
    internal string Relative(string absolutePath)
    {
        string relative = Path.GetRelativePath(Root, absolutePath);
        return relative.Replace('\\', '/');
    }

    /// <summary>Combines repository-relative segments into an absolute path.</summary>
    internal string Absolute(params string[] segments)
    {
        string combined = Root;
        foreach (string segment in segments)
        {
            combined = Path.Combine(combined, segment);
        }

        return combined;
    }
}
