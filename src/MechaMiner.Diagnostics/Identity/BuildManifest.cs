using System.Collections.Generic;

namespace MechaMiner.Diagnostics.Identity;

/// <summary>
/// <c>SCH-BLD-001</c>, the build/release manifest.
/// </summary>
/// <remarks>
/// <para>
/// Required identity, quoted from
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Schema
/// registry: "version/commit/tool/content hashes, target/configuration,
/// artifacts/checksums". The field list is fixed by
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c> § Version
/// and build identity, which requires every executable, about screen, diagnostic
/// header, and result manifest to include product version and build number, source
/// commit and dirty flag, Godot and .NET versions, content bundle hash,
/// schema/map/random/save versions, and build configuration/platform.
/// </para>
/// <para>
/// Field order is declaration order, because source-generated
/// <c>System.Text.Json</c> writes members in the order they are declared. That makes
/// the serialized document canonical and diffable, which
/// <c>docs/technical/91-verification-strategy.md</c> § Determinism and fixture policy
/// requires of every reviewable artifact.
/// </para>
/// <para>
/// <c>artifacts</c> and their checksums are the packaging half of this schema. They
/// are produced by <c>OPS-002</c> (release packaging) and are an empty list until
/// then; the field exists now so the schema does not change shape when that package
/// lands, and its emptiness is explicit rather than absent.
/// </para>
/// </remarks>
internal sealed class BuildManifest
{
    /// <summary>Stable schema identity. Always <c>SCH-BLD-001</c>.</summary>
    public string Schema { get; set; } = BuildIdentity.SchemaId;

    /// <summary>Version of this document's shape.</summary>
    public int SchemaVersion { get; set; } = BuildIdentity.SchemaVersion;

    /// <summary>
    /// One canonical line carrying the whole identity, for a log header, an about
    /// screen, or a one-line equality comparison.
    /// </summary>
    public string IdentityLine { get; set; } = string.Empty;

    /// <summary>Product version and CI build number.</summary>
    public ProductIdentity Product { get; set; } = new();

    /// <summary>Source commit and dirty flag.</summary>
    public SourceIdentity Source { get; set; } = new();

    /// <summary>Godot and .NET versions.</summary>
    public ToolchainIdentity Toolchain { get; set; } = new();

    /// <summary>Content bundle hash.</summary>
    public ContentIdentity Content { get; set; } = new();

    /// <summary>Schema, map, random, and save versions.</summary>
    public DataVersionIdentity DataVersions { get; set; } = new();

    /// <summary>Build configuration and platform.</summary>
    public BuildTargetIdentity Target { get; set; } = new();

    /// <summary>
    /// Packaged artifacts and their SHA-256 checksums. Empty until <c>OPS-002</c>.
    /// </summary>
    public List<ManifestArtifact> Artifacts { get; set; } = new();
}

/// <summary>Product version and CI build number.</summary>
internal sealed class ProductIdentity
{
    /// <summary>Semantic product version, for example <c>0.1.0</c>.</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>Monotonically increasing CI build number; <c>0</c> for a local build.</summary>
    public int BuildNumber { get; set; }

    /// <summary><c>local</c> or <c>ci</c>, so <c>0</c> is never mistaken for a CI build.</summary>
    public string BuildNumberSource { get; set; } = string.Empty;
}

/// <summary>Source commit and dirty flag.</summary>
internal sealed class SourceIdentity
{
    /// <summary>
    /// Full 40-character commit hash, or <c>unavailable</c> when the build tree is
    /// not a git working tree.
    /// </summary>
    public string Commit { get; set; } = string.Empty;

    /// <summary>First twelve characters of <see cref="Commit"/>, for human-readable output.</summary>
    public string CommitShort { get; set; } = string.Empty;

    /// <summary>
    /// <c>true</c>, <c>false</c>, or <c>unknown</c>. Tracked as text because the third
    /// state is real: a source archive has no working tree to compare against, and a
    /// boolean would have to report that as clean.
    /// </summary>
    public string Dirty { get; set; } = string.Empty;
}

/// <summary>Pinned Godot and .NET versions.</summary>
internal sealed class ToolchainIdentity
{
    /// <summary>The pinned Godot editor version.</summary>
    public string GodotVersion { get; set; } = string.Empty;

    /// <summary>The .NET SDK version that compiled this assembly.</summary>
    public string DotnetSdkVersion { get; set; } = string.Empty;

    /// <summary>The compiled target framework moniker.</summary>
    public string TargetFramework { get; set; } = string.Empty;
}

/// <summary>The canonical compiled content bundle hash.</summary>
internal sealed class ContentIdentity
{
    /// <summary>SHA-256 of the canonical compiled bundle, or empty while unavailable.</summary>
    public string BundleSha256 { get; set; } = string.Empty;

    /// <summary><c>available</c> or <c>unavailable</c>.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>The work package that supplies the hash while the status is unavailable.</summary>
    public string OwningWorkPackage { get; set; } = string.Empty;
}

/// <summary>Schema, map, random, and save versions.</summary>
internal sealed class DataVersionIdentity
{
    /// <summary>Content/definition schema version (doc 40 § Definition envelope).</summary>
    public int Schema { get; set; }

    /// <summary>Map generator version (doc 50 § Generated manifest).</summary>
    public int Map { get; set; }

    /// <summary>Authoritative random schema version (doc 20 § Authoritative random number contract).</summary>
    public int Random { get; set; }

    /// <summary>Save schema version (doc 70 § Save envelope).</summary>
    public int Save { get; set; }
}

/// <summary>Build configuration and platform.</summary>
internal sealed class BuildTargetIdentity
{
    /// <summary>doc 100's workflow configuration name: <c>Debug</c>, <c>Development</c>, or <c>Release</c>.</summary>
    public string WorkflowConfiguration { get; set; } = string.Empty;

    /// <summary>The MSBuild identity: <c>Debug</c>, <c>ExportDebug</c>, or <c>ExportRelease</c>.</summary>
    public string MsbuildConfiguration { get; set; } = string.Empty;

    /// <summary>The build-time platform identifier, for example <c>linux-x64</c>.</summary>
    public string Platform { get; set; } = string.Empty;
}

/// <summary>One packaged artifact and its checksum.</summary>
internal sealed class ManifestArtifact
{
    /// <summary>Repository-relative or package-relative path.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Lowercase hexadecimal SHA-256 of the artifact bytes.</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>Byte length of the artifact.</summary>
    public long SizeBytes { get; set; }
}
