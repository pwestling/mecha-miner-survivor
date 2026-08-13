using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaMiner.Diagnostics.Identity;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Diagnostics.Tests.Identity;

/// <summary>
/// Proves the <c>SCH-BLD-001</c> build identity contract.
/// </summary>
/// <remarks>
/// Owner: <c>FND-004</c> (<c>TASK-FND-004-001</c>). Verification:
/// <c>VER-FND-004-001</c>, <c>VER-FND-004-002</c>, <c>VER-FND-004-003</c>,
/// <c>VER-FND-004-005</c>, <c>VER-FND-004-006</c>.
/// </remarks>
[TestFixture]
internal sealed class BuildIdentityTests
{
    /// <summary>
    /// Every field doc 100 § Version and build identity requires is present and
    /// nonempty. The content bundle hash is the one field allowed to be empty, and only
    /// while its status says so and names the owning work package.
    /// </summary>
    [Test]
    public void EveryRequiredIdentityFieldIsPopulated()
    {
        BuildManifest manifest = BuildIdentity.Current;

        Expect.Multiple(() =>
        {
            Assert.That(manifest.Schema, Is.EqualTo("SCH-BLD-001"));
            Assert.That(manifest.SchemaVersion, Is.EqualTo(1));

            // product version and build number
            Assert.That(manifest.Product.Version, Does.Match(@"^\d+\.\d+\.\d+$"));
            Assert.That(manifest.Product.BuildNumber, Is.GreaterThanOrEqualTo(0));
            Assert.That(manifest.Product.BuildNumberSource, Is.AnyOf("local", "ci"));

            // source commit and dirty flag
            Assert.That(
                manifest.Source.Commit,
                Does.Match("^([0-9a-f]{40}|unavailable)$"),
                "the commit is a full git object name or the explicit unavailable marker");
            Assert.That(manifest.Source.Dirty, Is.AnyOf("true", "false", "unknown"));
            Assert.That(manifest.Source.CommitShort, Is.EqualTo(manifest.Source.Commit[..12]));

            // Godot and .NET versions
            Assert.That(manifest.Toolchain.GodotVersion, Is.EqualTo("4.7.1"));
            Assert.That(manifest.Toolchain.DotnetSdkVersion, Is.Not.Empty);
            Assert.That(manifest.Toolchain.TargetFramework, Is.EqualTo("net8.0"));

            // content bundle hash
            Assert.That(manifest.Content.Status, Is.AnyOf("available", "unavailable"));
            if (string.Equals(manifest.Content.Status, "unavailable", StringComparison.Ordinal))
            {
                Assert.That(
                    manifest.Content.OwningWorkPackage,
                    Is.Not.Empty,
                    "an unavailable content hash must name the work package that supplies it");
            }
            else
            {
                Assert.That(manifest.Content.BundleSha256, Does.Match("^[0-9a-f]{64}$"));
            }

            // schema/map/random/save versions
            Assert.That(manifest.DataVersions.Schema, Is.GreaterThanOrEqualTo(1));
            Assert.That(manifest.DataVersions.Map, Is.GreaterThanOrEqualTo(1));
            Assert.That(
                manifest.DataVersions.Random,
                Is.EqualTo(1),
                "doc 20 § Authoritative random number contract: 'The current random schema version is 1.'");
            Assert.That(manifest.DataVersions.Save, Is.GreaterThanOrEqualTo(1));

            // build configuration and platform
            Assert.That(manifest.Target.WorkflowConfiguration, Is.AnyOf("Debug", "Development", "Release"));
            Assert.That(
                manifest.Target.MsbuildConfiguration,
                Is.AnyOf("Debug", "ExportDebug", "ExportRelease"));
            Assert.That(manifest.Target.Platform, Is.Not.Empty);
        });
    }

    /// <summary>
    /// The assembly really carries the whole required metadata set. Composition
    /// succeeding is weaker evidence: an optional-looking field could be quietly
    /// absent and still produce a manifest.
    /// </summary>
    [Test]
    public void TheAssemblyCarriesEveryRequiredIdentityMetadataKey()
    {
        IReadOnlyDictionary<string, string> metadata = BuildIdentity.Metadata;
        List<string> missing = new();
        foreach (string key in BuildIdentity.RequiredMetadataKeys)
        {
            if (!metadata.ContainsKey(key))
            {
                missing.Add(key);
            }
        }

        Assert.That(missing, Is.Empty, "MechaMiner.Diagnostics.csproj must bake every identity key");
    }

    /// <summary>
    /// An assembly compiled without its identity metadata fails loudly instead of
    /// reporting a partial identity. Proved per required key, not with one sample.
    /// </summary>
    [Test]
    public void MissingIdentityMetadataThrowsPerRequiredKey()
    {
        Dictionary<string, string> complete = new(BuildIdentity.Metadata, StringComparer.Ordinal);

        Expect.Multiple(() =>
        {
            foreach (string key in BuildIdentity.RequiredMetadataKeys)
            {
                Dictionary<string, string> incomplete = new(complete, StringComparer.Ordinal);
                incomplete.Remove(key);

                InvalidOperationException failure =
                    Expect.Throws<InvalidOperationException>(() => BuildIdentity.ComposeFrom(incomplete));
                Assert.That(
                    failure.Message,
                    Does.Contain(key),
                    "removing " + key + " must fail composition and name the key");
            }
        });
    }

    /// <summary>
    /// The pinned Godot version carried in build identity is the same number
    /// <c>build/toolchain.json</c> pins, so the two records cannot drift apart
    /// silently.
    /// </summary>
    [Test]
    public void TheGodotVersionInIdentityEqualsTheToolchainPin()
    {
        string pinsPath = Path.Combine(TestArtifacts.RepositoryRoot, "build", "toolchain.json");
        string pins = File.ReadAllText(pinsPath);

        string expected = BuildIdentity.Current.Toolchain.GodotVersion;
        Assert.That(
            pins,
            Does.Contain("\"version\": \"" + expected + "\""),
            "build/version-identity.props pins Godot " + expected
            + "; build/toolchain.json must pin the same version");
    }

    /// <summary>
    /// The serialized manifest is canonical: the same identity serializes to the same
    /// bytes, the field order is the declaration order doc 91 requires of a reviewable
    /// artifact, and a round trip preserves every value.
    /// </summary>
    [Test]
    public void TheSerializedManifestIsCanonicalAndRoundTrips()
    {
        string first = DiagnosticsJsonContext.Serialize(BuildIdentity.ToManifest());
        string second = DiagnosticsJsonContext.Serialize(BuildIdentity.ToManifest());

        Expect.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first), "two serializations of one identity must be byte-identical");
            Assert.That(first, Does.EndWith("\n"), "a text artifact ends with a newline");

            int schema = first.IndexOf("\"schema\":", StringComparison.Ordinal);
            int identity = first.IndexOf("\"identity_line\":", StringComparison.Ordinal);
            int product = first.IndexOf("\"product\":", StringComparison.Ordinal);
            int source = first.IndexOf("\"source\":", StringComparison.Ordinal);
            int toolchain = first.IndexOf("\"toolchain\":", StringComparison.Ordinal);
            int content = first.IndexOf("\"content\":", StringComparison.Ordinal);
            int dataVersions = first.IndexOf("\"data_versions\":", StringComparison.Ordinal);
            int target = first.IndexOf("\"target\":", StringComparison.Ordinal);
            int artifacts = first.IndexOf("\"artifacts\":", StringComparison.Ordinal);
            Assert.That(
                new[] { schema, identity, product, source, toolchain, content, dataVersions, target, artifacts },
                Is.Ordered.Ascending,
                "field order is declaration order");

            BuildManifest roundTripped = DiagnosticsJsonContext.DeserializeManifest(first);
            Assert.That(DiagnosticsJsonContext.Serialize(roundTripped), Is.EqualTo(first));
        });
    }

    /// <summary>Unknown fields are rejected rather than ignored (doc 40 § JSON codec and schema baseline).</summary>
    [Test]
    public void AnUnknownManifestFieldIsRejected()
    {
        string json = DiagnosticsJsonContext.Serialize(BuildIdentity.ToManifest())
            .Replace("\"schema\":", "\"unexpected_field\": 1,\n  \"schema\":", StringComparison.Ordinal);

        Expect.Throws<System.Text.Json.JsonException>(
            () => DiagnosticsJsonContext.DeserializeManifest(json));
    }

    /// <summary>
    /// The identity line is derived from the manifest by one rule, so a line read back
    /// from a report can be recomputed and compared rather than trusted.
    /// </summary>
    [Test]
    public void TheIdentityLineIsDerivedFromTheManifestFields()
    {
        BuildManifest manifest = BuildIdentity.ToManifest();

        Expect.Multiple(() =>
        {
            Assert.That(BuildIdentity.RenderIdentityLine(manifest), Is.EqualTo(manifest.IdentityLine));
            Assert.That(manifest.IdentityLine, Does.Contain("product=" + manifest.Product.Version));
            Assert.That(manifest.IdentityLine, Does.Contain("commit=" + manifest.Source.Commit));
            Assert.That(manifest.IdentityLine, Does.Contain("dirty=" + manifest.Source.Dirty));
            Assert.That(manifest.IdentityLine, Does.Contain("godot=" + manifest.Toolchain.GodotVersion));
            Assert.That(
                manifest.IdentityLine,
                Does.Contain(
                    "random=" + manifest.DataVersions.Random.ToString(CultureInfo.InvariantCulture)));
            Assert.That(manifest.IdentityLine, Does.Contain("platform=" + manifest.Target.Platform));
        });
    }

    /// <summary>
    /// The manifest file comparison classifies missing, unreadable, stale, and current
    /// separately, which is what a staleness gate needs to report a cause rather than a
    /// bare failure.
    /// </summary>
    [Test]
    public void ManifestComparisonClassifiesMissingUnreadableStaleAndCurrent()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "mechaminer-build-manifest-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "build-manifest.json");

            Assert.That(
                BuildManifestFile.Compare(path, "build-manifest.json").Status,
                Is.EqualTo(BuildManifestComparison.MissingStatus));

            File.WriteAllText(path, "{ not json");
            Assert.That(
                BuildManifestFile.Compare(path, "build-manifest.json").Status,
                Is.EqualTo(BuildManifestComparison.UnreadableStatus));

            BuildManifest tampered = BuildIdentity.ToManifest();
            tampered.Source.Commit = new string('0', 40);
            tampered.IdentityLine = BuildIdentity.RenderIdentityLine(tampered);
            File.WriteAllText(path, DiagnosticsJsonContext.Serialize(tampered));
            BuildManifestComparison stale = BuildManifestFile.Compare(path, "build-manifest.json");
            Expect.Multiple(() =>
            {
                Assert.That(stale.Status, Is.EqualTo(BuildManifestComparison.StaleStatus));
                Assert.That(stale.Differences, Is.Not.Empty);
                Assert.That(string.Join("; ", stale.Differences), Does.Contain("source.commit"));
            });

            BuildManifestFile.Write(path);
            BuildManifestComparison current = BuildManifestFile.Compare(path, "build-manifest.json");
            Expect.Multiple(() =>
            {
                Assert.That(current.Status, Is.EqualTo(BuildManifestComparison.CurrentStatus));
                Assert.That(current.IsCurrent, Is.True);
                Assert.That(current.Differences, Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>The manifest is written through a temporary file, so no partial document is left behind.</summary>
    [Test]
    public void WritingTheManifestLeavesNoPartialFile()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "mechaminer-build-manifest-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        try
        {
            string path = Path.Combine(directory, "nested", "build-manifest.json");
            string written = BuildManifestFile.Write(path);

            Expect.Multiple(() =>
            {
                Assert.That(File.ReadAllText(path), Is.EqualTo(written));
                Assert.That(File.Exists(path + ".partial"), Is.False);
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
