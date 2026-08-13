using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace MechaMiner.Diagnostics.Identity;

/// <summary>
/// The one build identity every surface reports.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>. Authority:
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c> § Version
/// and build identity, and <c>docs/technical/115</c> § Initialization order step 1
/// ("Verify build/tool identity embedded in the executable"). Requirements:
/// <c>TR-BLD-001</c>, <c>TR-BLD-004</c>, <c>TR-RUN-009</c>.
/// </para>
/// <para>
/// The values are baked into this assembly's metadata at compile time by
/// <c>MechaMiner.Diagnostics.csproj</c> and read back here. Nothing is probed from
/// the environment at run time, which is what makes the identity equal across the
/// tool process, the Godot process, and a diagnostic header: all three load this
/// assembly and read the same baked values. A run-time probe would let two processes
/// disagree about platform, configuration, or working-tree state and would make the
/// equality gate meaningless.
/// </para>
/// <para>
/// Reading this assembly's own attributes is not the reflection doc 100 § C# project
/// standards prohibits. That prohibition is about "reflection-based gameplay
/// registration and runtime assembly scanning"; nothing is discovered here, one known
/// attribute set on one known assembly is read once.
/// </para>
/// <para>
/// A missing attribute is a violated build invariant, not an expected rejection, so
/// it throws (doc 114 § C# and domain defaults: "Exceptions are reserved for violated
/// invariants, invalid startup/build data, or unrecoverable infrastructure failure").
/// An assembly compiled without its identity metadata is exactly invalid build data.
/// </para>
/// </remarks>
public static class BuildIdentity
{
    /// <summary>The stable schema ID of the emitted manifest.</summary>
    public const string SchemaId = "SCH-BLD-001";

    /// <summary>The version of the manifest's shape.</summary>
    public const int SchemaVersion = 1;

    /// <summary>The value the source commit carries outside a git working tree.</summary>
    public const string SourceUnavailable = "unavailable";

    /// <summary>The value the dirty flag carries when the working tree cannot be inspected.</summary>
    public const string DirtyUnknown = "unknown";

    private const int ShortCommitLength = 12;

    private static readonly Lazy<BuildManifest> Lazy = new(Compose, isThreadSafe: true);

    /// <summary>
    /// The identity of the build that produced this assembly. Composed once and
    /// immutable thereafter in every practical sense: callers receive the same
    /// instance and must not mutate it, and <see cref="ToManifest"/> hands out an
    /// independent copy for serialization.
    /// </summary>
    internal static BuildManifest Current => Lazy.Value;

    /// <summary>
    /// The single canonical line every diagnostic header, about surface, and report prints.
    /// Ordered, delimiter-separated, and stable, so two surfaces can be compared for equality
    /// as text.
    /// </summary>
    /// <remarks>
    /// Public because this is the cross-project contract: the workflow host, the Godot
    /// process, and every diagnostic header report the same line, and
    /// <c>VER-FND-004-004</c> compares them. The typed manifest behind it stays internal, so
    /// the contract is one string rather than a mutable object graph.
    /// </remarks>
    public static string IdentityLine => Lazy.Value.IdentityLine;

    /// <summary>Returns an independent copy of the identity, safe to serialize or extend.</summary>
    internal static BuildManifest ToManifest()
    {
        BuildManifest source = Lazy.Value;
        return new BuildManifest
        {
            Schema = source.Schema,
            SchemaVersion = source.SchemaVersion,
            IdentityLine = source.IdentityLine,
            Product = new ProductIdentity
            {
                Version = source.Product.Version,
                BuildNumber = source.Product.BuildNumber,
                BuildNumberSource = source.Product.BuildNumberSource,
            },
            Source = new SourceIdentity
            {
                Commit = source.Source.Commit,
                CommitShort = source.Source.CommitShort,
                Dirty = source.Source.Dirty,
            },
            Toolchain = new ToolchainIdentity
            {
                GodotVersion = source.Toolchain.GodotVersion,
                DotnetSdkVersion = source.Toolchain.DotnetSdkVersion,
                TargetFramework = source.Toolchain.TargetFramework,
            },
            Content = new ContentIdentity
            {
                BundleSha256 = source.Content.BundleSha256,
                Status = source.Content.Status,
                OwningWorkPackage = source.Content.OwningWorkPackage,
            },
            DataVersions = new DataVersionIdentity
            {
                Schema = source.DataVersions.Schema,
                Map = source.DataVersions.Map,
                Random = source.DataVersions.Random,
                Save = source.DataVersions.Save,
            },
            Target = new BuildTargetIdentity
            {
                WorkflowConfiguration = source.Target.WorkflowConfiguration,
                MsbuildConfiguration = source.Target.MsbuildConfiguration,
                Platform = source.Target.Platform,
            },
        };
    }

    /// <summary>
    /// Renders the canonical identity line for an arbitrary manifest, so a manifest
    /// read back from disk can be compared against the compiled identity by exactly
    /// the same rule that produced it.
    /// </summary>
    internal static string RenderIdentityLine(BuildManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return string.Join(
            " ",
            "product=" + manifest.Product.Version,
            "build=" + manifest.Product.BuildNumber.ToString(CultureInfo.InvariantCulture)
                + "(" + manifest.Product.BuildNumberSource + ")",
            "commit=" + manifest.Source.Commit,
            "dirty=" + manifest.Source.Dirty,
            "godot=" + manifest.Toolchain.GodotVersion,
            "dotnet-sdk=" + manifest.Toolchain.DotnetSdkVersion,
            "tfm=" + manifest.Toolchain.TargetFramework,
            "content=" + (manifest.Content.BundleSha256.Length > 0
                ? manifest.Content.BundleSha256
                : manifest.Content.Status + ":" + manifest.Content.OwningWorkPackage),
            "schema=" + manifest.DataVersions.Schema.ToString(CultureInfo.InvariantCulture),
            "map=" + manifest.DataVersions.Map.ToString(CultureInfo.InvariantCulture),
            "random=" + manifest.DataVersions.Random.ToString(CultureInfo.InvariantCulture),
            "save=" + manifest.DataVersions.Save.ToString(CultureInfo.InvariantCulture),
            "configuration=" + manifest.Target.WorkflowConfiguration
                + "(" + manifest.Target.MsbuildConfiguration + ")",
            "platform=" + manifest.Target.Platform);
    }

    /// <summary>
    /// The assembly metadata keys build identity requires, in the order doc 100
    /// § Version and build identity lists the values they carry.
    /// </summary>
    internal static IReadOnlyList<string> RequiredMetadataKeys { get; } = new[]
    {
        "MechaMiner.ProductVersion",
        "MechaMiner.BuildNumber",
        "MechaMiner.BuildNumberSource",
        "MechaMiner.SourceCommit",
        "MechaMiner.SourceDirty",
        "MechaMiner.GodotVersion",
        "MechaMiner.DotnetSdkVersion",
        "MechaMiner.TargetFramework",
        "MechaMiner.ContentBundleStatus",
        "MechaMiner.ContentBundleOwner",
        "MechaMiner.SchemaVersion",
        "MechaMiner.MapVersion",
        "MechaMiner.RandomVersion",
        "MechaMiner.SaveVersion",
        "MechaMiner.WorkflowConfiguration",
        "MechaMiner.MsbuildConfiguration",
        "MechaMiner.Platform",
    };

    /// <summary>
    /// The metadata baked into this assembly, exposed so a test can prove the whole
    /// required set is present rather than only that composition happened to succeed.
    /// </summary>
    internal static IReadOnlyDictionary<string, string> Metadata => ReadMetadata();

    private static BuildManifest Compose()
    {
        return ComposeFrom(ReadMetadata());
    }

    /// <summary>
    /// Composes an identity from an explicit metadata set.
    /// </summary>
    /// <remarks>
    /// The seam exists so a test can feed a deliberately incomplete metadata set and
    /// prove that an assembly compiled without its identity fails loudly instead of
    /// reporting a partial identity. Building a second, deliberately broken assembly
    /// to prove the same thing would put an invalid fixture inside a production
    /// project, which the repository policy places under <c>build/policy-fixtures/</c>
    /// instead.
    /// </remarks>
    internal static BuildManifest ComposeFrom(IReadOnlyDictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        BuildManifest manifest = new()
        {
            Product = new ProductIdentity
            {
                Version = Required(metadata, "MechaMiner.ProductVersion"),
                BuildNumber = RequiredInteger(metadata, "MechaMiner.BuildNumber"),
                BuildNumberSource = Required(metadata, "MechaMiner.BuildNumberSource"),
            },
            Source = new SourceIdentity
            {
                Commit = Required(metadata, "MechaMiner.SourceCommit"),
                Dirty = Required(metadata, "MechaMiner.SourceDirty"),
            },
            Toolchain = new ToolchainIdentity
            {
                GodotVersion = Required(metadata, "MechaMiner.GodotVersion"),
                DotnetSdkVersion = Required(metadata, "MechaMiner.DotnetSdkVersion"),
                TargetFramework = Required(metadata, "MechaMiner.TargetFramework"),
            },
            Content = new ContentIdentity
            {
                // Deliberately allowed to be empty: no bundle exists until DAT-006,
                // and Status plus OwningWorkPackage say so explicitly.
                BundleSha256 = metadata.TryGetValue("MechaMiner.ContentBundleSha256", out string? hash)
                    ? hash
                    : string.Empty,
                Status = Required(metadata, "MechaMiner.ContentBundleStatus"),
                OwningWorkPackage = Required(metadata, "MechaMiner.ContentBundleOwner"),
            },
            DataVersions = new DataVersionIdentity
            {
                Schema = RequiredInteger(metadata, "MechaMiner.SchemaVersion"),
                Map = RequiredInteger(metadata, "MechaMiner.MapVersion"),
                Random = RequiredInteger(metadata, "MechaMiner.RandomVersion"),
                Save = RequiredInteger(metadata, "MechaMiner.SaveVersion"),
            },
            Target = new BuildTargetIdentity
            {
                WorkflowConfiguration = Required(metadata, "MechaMiner.WorkflowConfiguration"),
                MsbuildConfiguration = Required(metadata, "MechaMiner.MsbuildConfiguration"),
                Platform = Required(metadata, "MechaMiner.Platform"),
            },
        };

        manifest.Source.CommitShort = manifest.Source.Commit.Length >= ShortCommitLength
            ? manifest.Source.Commit[..ShortCommitLength]
            : manifest.Source.Commit;
        manifest.IdentityLine = RenderIdentityLine(manifest);
        return manifest;
    }

    private static Dictionary<string, string> ReadMetadata()
    {
        Dictionary<string, string> metadata = new(StringComparer.Ordinal);
        Assembly assembly = typeof(BuildIdentity).Assembly;
        foreach (AssemblyMetadataAttribute attribute in
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attribute.Key.StartsWith("MechaMiner.", StringComparison.Ordinal))
            {
                metadata[attribute.Key] = attribute.Value ?? string.Empty;
            }
        }

        return metadata;
    }

    private static string Required(IReadOnlyDictionary<string, string> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out string? value) || value.Length == 0)
        {
            throw new InvalidOperationException(
                "MechaMiner.Diagnostics was compiled without the build identity attribute '" + key
                + "'. Build identity is baked in by MechaMiner.Diagnostics.csproj's "
                + "MechaMinerResolveSourceRevision target; an assembly without it cannot satisfy "
                + "doc 100 § Version and build identity.");
        }

        return value;
    }

    private static int RequiredInteger(IReadOnlyDictionary<string, string> metadata, string key)
    {
        string text = Required(metadata, key);
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            throw new InvalidOperationException(
                "build identity attribute '" + key + "' is not an integer: '" + text + "'");
        }

        return value;
    }
}
