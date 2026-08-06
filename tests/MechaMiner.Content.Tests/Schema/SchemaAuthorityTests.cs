using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Schema;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// Every numeric bound in a project schema records where its number came from.
/// </summary>
/// <remarks>
/// <para>
/// The gate exists so that "which schema bounds need re-deriving now that a document's
/// capacity section changed" is a query rather than a recollection. Without it the
/// annotation rots into decoration: a few bounds carry it, new ones do not, and nothing
/// notices.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-025</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SchemaAuthorityTests
{
    private static IEnumerable<string> ProjectSchemas
    {
        get
        {
            string directory = Path.Combine(TestArtifacts.RepositoryRoot, "content", "schemas");
            return Directory.GetFiles(directory, "*.schema.json", SearchOption.AllDirectories);
        }
    }

    [TestCaseSource(nameof(ProjectSchemas))]
    public void EveryNumericBoundInAProjectSchemaCarriesAnAdjacentAuthority(string path)
    {
        IReadOnlyList<string> unattributed = FindUnattributedBounds(File.ReadAllBytes(path));

        Assert.That(
            unattributed,
            Is.Empty,
            () => TestArtifacts.Relative(path) + " has numeric bounds with no adjacent '"
                + SchemaAuthority.Keyword + "': " + string.Join(", ", unattributed));
    }

    /// <summary>
    /// The source says where a number came from; the derivation says why it is that
    /// number. They go stale independently, so a sourced or derived bound needs both.
    /// </summary>
    [TestCaseSource(nameof(ProjectSchemas))]
    public void EverySourcedOrDerivedBoundStatesItsDerivation(string path)
    {
        IReadOnlyList<string> missing = FindMissingDerivations(File.ReadAllBytes(path));

        Assert.That(
            missing,
            Is.Empty,
            () => TestArtifacts.Relative(path) + " has sourced or derived bounds with no "
                + "'derivation': " + string.Join(", ", missing));
    }

    /// <summary>
    /// The negative control. Without it, the gate above would pass just as happily if
    /// <see cref="FindUnattributedBounds"/> never found anything.
    /// </summary>
    [Test]
    public void TheGateFailsOnABareBound()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "unattributed-bound.schema.json");

        IReadOnlyList<string> unattributed = FindUnattributedBounds(File.ReadAllBytes(path));

        Expect.Multiple(() =>
        {
            Assert.That(unattributed, Is.Not.Empty, "the gate must catch a bare bound");
            Assert.That(unattributed[0], Does.Contain("maximum"));
        });
    }

    [Test]
    public void TheDerivationGateFailsOnASourcedBoundWithNoDerivation()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "undelivered-derivation.schema.json");

        IReadOnlyList<string> missing = FindMissingDerivations(File.ReadAllBytes(path));

        Expect.Multiple(() =>
        {
            Assert.That(missing, Is.Not.Empty, "the gate must catch a bound with no derivation");
            Assert.That(missing[0], Does.Contain("maximum"));
        });
    }

    [Test]
    public void TheLoaderAlsoRejectsASourcedBoundWithNoDerivation()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "undelivered-derivation.schema.json");

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            File.ReadAllBytes(path), "tests/fixtures/schema/undelivered-derivation.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(load.IsValid, Is.False);
            Assert.That(load.Diagnostics[0].ExpectedConstraint, Does.Contain("derivation"));
        });
    }

    /// <summary>The loader rejects the same file, so the gate holds from both directions.</summary>
    [Test]
    public void TheLoaderAlsoRejectsABareBound()
    {
        string path = Path.Combine(
            FixtureCorpus.Root, "schema", "unattributed-bound.schema.json");

        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            File.ReadAllBytes(path), "tests/fixtures/schema/unattributed-bound.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(load.IsValid, Is.False);
            Assert.That(load.Diagnostics[0].Code, Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
            Assert.That(load.Diagnostics[0].ExpectedConstraint, Does.Contain(SchemaAuthority.Keyword));
        });
    }

    [Test]
    public void AProjectSchemaStillLoadsWithItsAuthorities()
    {
        foreach (string path in ProjectSchemas)
        {
            JsonSchemaLoadResult load = JsonSchemaLoader.Load(
                File.ReadAllBytes(path), TestArtifacts.Relative(path));
            Assert.That(
                load.IsValid,
                Is.True,
                () => TestArtifacts.Relative(path) + ": " + string.Join("; ", load.Diagnostics));
        }
    }

    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"Performance and capacity\",\"derivation\":\"worst case ~1010; x2 headroom; rounded to a power of two\"}}", true)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"derived\",\"source\":\"GDD-MINING\",\"section\":\"Sites\",\"derivation\":\"four site classes times the per-class ceiling\"}}", true)]
    // A sourced bound with no derivation says where the number came from but not why it
    // is that number.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"S\"}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"S\",\"derivation\":\"  \"}}", false)]
    // A structural bound has no derivation; its rationale is in description.
    [TestCase("{\"maximum\":1,\"description\":\"d\",\"x-authority\":{\"kind\":\"structural\",\"derivation\":\"x\"}}", false)]
    [TestCase("{\"maximum\":1,\"description\":\"why\",\"x-authority\":{\"kind\":\"structural\"}}", true)]
    // A structural bound with no rationale: the limit is unjustified.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"structural\"}}", false)]
    // A structural bound must not claim an authority it does not have.
    [TestCase("{\"maximum\":1,\"description\":\"d\",\"x-authority\":{\"kind\":\"structural\",\"source\":\"TDD-COMBAT\"}}", false)]
    // A sourced bound needs both the document and the section.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"sourced\",\"section\":\"S\",\"derivation\":\"d\"}}", false)]
    // The source uses the source_refs vocabulary, validated by the same parser.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"sourced\",\"source\":\"docs/22.md\",\"section\":\"S\",\"derivation\":\"d\"}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"sourced\",\"source\":\"TDD_COMBAT\",\"section\":\"S\",\"derivation\":\"d\"}}", false)]
    // A section is a heading, so an anchor slug belongs in the section field, not the ID.
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT#capacity\",\"section\":\"S\",\"derivation\":\"d\"}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"invented\",\"source\":\"TDD-COMBAT\",\"section\":\"S\",\"derivation\":\"d\"}}", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"kind\":\"sourced\",\"source\":\"TDD-COMBAT\",\"section\":\"S\",\"derivation\":\"d\",\"extra\":1}}", false)]
    // An authority with nothing to annotate is misplaced.
    [TestCase("{\"type\":\"integer\",\"x-authority\":{\"kind\":\"structural\"}}", false)]
    public void TheAuthorityShapeIsValidated(string schemaText, bool expected)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(schemaText), "inline.schema.json");

        Assert.That(
            load.IsValid,
            Is.EqualTo(expected),
            () => schemaText + " -> " + string.Join("; ", load.Diagnostics));
    }

    /// <summary>
    /// Attributing a bound outside the mandatory set must be permitted. Requiring
    /// attribution and allowing it are different questions.
    /// </summary>
    [Test]
    public void ABoundOutsideTheMandatorySetMayStillBeAttributed()
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(
                "{\"minLength\":1,\"description\":\"why\",\"x-authority\":{\"kind\":\"structural\"}}"),
            "inline.schema.json");

        Assert.That(load.IsValid, Is.True, () => string.Join("; ", load.Diagnostics));
    }

    /// <summary>
    /// Walks a schema document and returns the JSON Pointer of every mandatory bound that
    /// has no <c>x-authority</c> beside it.
    /// </summary>
    private static IReadOnlyList<string> FindUnattributedBounds(byte[] schemaBytes)
    {
        List<string> unattributed = new();
        using JsonDocument document = JsonDocument.Parse(schemaBytes);
        Walk(document.RootElement, JsonPointer.Root, unattributed);
        return unattributed;
    }

    /// <summary>
    /// Walks a schema document and returns the JSON Pointer of every sourced or derived
    /// bound whose authority states no derivation.
    /// </summary>
    private static IReadOnlyList<string> FindMissingDerivations(byte[] schemaBytes)
    {
        List<string> missing = new();
        using JsonDocument document = JsonDocument.Parse(schemaBytes);
        WalkDerivations(document.RootElement, JsonPointer.Root, missing);
        return missing;
    }

    private static void WalkDerivations(JsonElement element, JsonPointer at, List<string> missing)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(SchemaAuthority.Keyword, out JsonElement authority)
                && authority.ValueKind == JsonValueKind.Object
                && authority.TryGetProperty("kind", out JsonElement kind)
                && kind.ValueKind == JsonValueKind.String
                && kind.GetString() is "sourced" or "derived")
            {
                bool stated = authority.TryGetProperty("derivation", out JsonElement derivation)
                    && derivation.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(derivation.GetString());

                if (!stated)
                {
                    foreach (string keyword in SchemaAuthority.AttributableKeywords())
                    {
                        if (element.TryGetProperty(keyword, out _))
                        {
                            missing.Add(at.AppendProperty(keyword).Value);
                            break;
                        }
                    }
                }
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                WalkDerivations(property.Value, at.AppendProperty(property.Name), missing);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                WalkDerivations(item, at.AppendIndex(index), missing);
                index++;
            }
        }
    }

    private static void Walk(JsonElement element, JsonPointer at, List<string> unattributed)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            bool hasAuthority = element.TryGetProperty(SchemaAuthority.Keyword, out _);
            foreach (string keyword in SchemaAuthority.BoundKeywords())
            {
                if (element.TryGetProperty(keyword, out _) && !hasAuthority)
                {
                    unattributed.Add(at.AppendProperty(keyword).Value);
                }
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                Walk(property.Value, at.AppendProperty(property.Name), unattributed);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                Walk(item, at.AppendIndex(index), unattributed);
                index++;
            }
        }
    }
}
