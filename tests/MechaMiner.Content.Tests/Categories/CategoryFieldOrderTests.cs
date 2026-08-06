using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Schema;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// Each category schema declares the same properties, in the same order, as the typed
/// field table the validator walks.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline: "The canonical writer emits fields in schema-declared order." Two
/// declarations of that order means the bundle's field order depends on which one the
/// writer consulted, and a disagreement would be invisible until two builds produced
/// different bytes for the same input.
/// </para>
/// <para>
/// The comparison is element by element over the domain fields only: the envelope's own
/// nine come first and are declared by <see cref="EnvelopeSchema"/>, not by a category.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-002-029</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CategoryFieldOrderTests
{
    private static IEnumerable<CategoryDescriptor> Descriptors => CategorySchemas.All;

    [TestCaseSource(nameof(Descriptors))]
    public void TheSchemaDeclaresTheFieldTablesPropertiesInItsOrder(CategoryDescriptor descriptor)
    {
        IReadOnlyList<string> schemaFields = DomainPropertiesOf(descriptor);
        IReadOnlyList<string> tableFields = descriptor.Shape.FieldNames();

        Assert.That(
            schemaFields,
            Is.EqualTo(tableFields),
            () => descriptor.SchemaPath + " and " + descriptor.Shape.Subject
                + " must declare the same domain fields in the same order."
                + Environment.NewLine + "schema: " + string.Join(", ", schemaFields)
                + Environment.NewLine + "table:  " + string.Join(", ", tableFields));
    }

    /// <summary>
    /// A required field in one declaration and not the other would let the schema and
    /// the typed validator disagree about whether a document is complete.
    /// </summary>
    [TestCaseSource(nameof(Descriptors))]
    public void TheSchemaRequiresTheSameDomainFieldsTheTableDoes(CategoryDescriptor descriptor)
    {
        HashSet<string> schemaRequired = RequiredDomainFieldsOf(descriptor);
        List<string> tableRequired = new();
        foreach (DefinitionField field in descriptor.Shape.Fields)
        {
            if (field.IsRequired)
            {
                tableRequired.Add(field.Name);
            }
        }

        Assert.That(
            schemaRequired,
            Is.EquivalentTo(tableRequired),
            () => descriptor.SchemaPath + " and " + descriptor.Shape.Subject
                + " must agree on which domain fields are required."
                + Environment.NewLine + "schema: " + string.Join(", ", schemaRequired)
                + Environment.NewLine + "table:  " + string.Join(", ", tableRequired));
    }

    /// <summary>
    /// The negative control: the comparison must be able to fail. A table with one field
    /// removed must not match its schema.
    /// </summary>
    [Test]
    public void TheComparisonFailsWhenATableAndItsSchemaDisagree()
    {
        CategoryDescriptor descriptor = CategorySchemas.Describe(DefinitionKind.Unlock);
        IReadOnlyList<string> schemaFields = DomainPropertiesOf(descriptor);

        List<string> shortened = new(descriptor.Shape.FieldNames());
        shortened.RemoveAt(0);

        Assert.That(
            schemaFields,
            Is.Not.EqualTo(shortened),
            "a table missing a field the schema declares must not compare equal");
    }

    private static IReadOnlyList<string> DomainPropertiesOf(CategoryDescriptor descriptor)
    {
        List<string> domain = new();
        using JsonDocument schema = Read(descriptor);

        foreach (JsonProperty property in
                 schema.RootElement.GetProperty("properties").EnumerateObject())
        {
            if (!EnvelopeSchema.Declares(property.Name))
            {
                domain.Add(property.Name);
            }
        }

        return domain;
    }

    private static HashSet<string> RequiredDomainFieldsOf(CategoryDescriptor descriptor)
    {
        HashSet<string> required = new(StringComparer.Ordinal);
        using JsonDocument schema = Read(descriptor);

        foreach (JsonElement name in schema.RootElement.GetProperty("required").EnumerateArray())
        {
            string value = name.GetString()!;
            if (!EnvelopeSchema.Declares(value))
            {
                required.Add(value);
            }
        }

        return required;
    }

    private static JsonDocument Read(CategoryDescriptor descriptor)
    {
        string path = Path.Combine(
            TestArtifacts.RepositoryRoot, "content", "schemas", descriptor.SchemaFileName);
        return JsonDocument.Parse(File.ReadAllBytes(path));
    }
}

/// <summary>
/// Every schema this package adds is inside the directory the DAT-001 provenance gate
/// globs, so a new schema is covered the moment its file exists.
/// </summary>
/// <remarks>
/// <para>
/// The <c>x-authority</c> gate in <c>SchemaAuthorityTests</c> enumerates
/// <c>content/schemas/**/*.schema.json</c> rather than a registered list, which is what
/// makes it impossible to add a schema that quietly escapes it. This test asserts that
/// property rather than re-implementing the gate: it checks that every declared
/// category's schema file is where the glob looks, and that the glob finds no schema no
/// category claims.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-002-028</c>, <c>VER-DAT-003-027</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CategorySchemaAuthorityCoverageTests
{
    private static string SchemaDirectory => Path.Combine(
        TestArtifacts.RepositoryRoot, "content", "schemas");

    /// <summary>
    /// What an emptied <see cref="CategorySchemas.All"/> must be reported as.
    /// </summary>
    /// <remarks>
    /// Both walks in this fixture range over the declared table, so the table decides how
    /// much they prove and neither can see it shrink. The cardinality of the table is
    /// anchored, against two independent things, by
    /// <c>CategorySchemaAgreementTests.TheDeclaredKindsAreExactlyTheKindEnumAndTheSchemasOnDisk</c>;
    /// these guards are what stops a walk reporting success over nothing while that
    /// anchor is the only test failing.
    /// </remarks>
    private const string NoDeclaredCategories =
        "no category is declared, so this walk visited nothing; see "
            + "CategorySchemaAgreementTests"
            + ".TheDeclaredKindsAreExactlyTheKindEnumAndTheSchemasOnDisk";

    [Test]
    public void EveryDeclaredCategoryHasASchemaWhereTheProvenanceGateLooks()
    {
        List<string> missing = new();
        int checkedSchemas = 0;
        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            checkedSchemas++;
            if (!File.Exists(Path.Combine(SchemaDirectory, descriptor.SchemaFileName)))
            {
                missing.Add(descriptor.SchemaPath);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(checkedSchemas, Is.GreaterThan(0), NoDeclaredCategories);
            Assert.That(
                missing,
                Is.Empty,
                () => "declared categories with no schema document: "
                    + string.Join(", ", missing));
        });
    }

    [Test]
    public void EverySchemaOnDiskIsClaimedByACategoryOrIsTheEnvelope()
    {
        HashSet<string> claimed = new(StringComparer.Ordinal) { "envelope.schema.json" };
        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            claimed.Add(descriptor.SchemaFileName);
        }

        List<string> orphans = new();
        foreach (string path in Directory.GetFiles(
                     SchemaDirectory, "*.schema.json", SearchOption.AllDirectories))
        {
            if (!claimed.Contains(Path.GetFileName(path)))
            {
                orphans.Add(Path.GetFileName(path));
            }
        }

        Assert.That(
            orphans,
            Is.Empty,
            () => "schemas no category claims: " + string.Join(", ", orphans));
    }

    /// <summary>
    /// Every bound in every category schema is attributed, checked here against the
    /// keyword set <see cref="SchemaAuthority"/> declares rather than a copy of it.
    /// </summary>
    [Test]
    public void EveryCategorySchemaLoadsUnderTheProvenanceEnforcingLoader()
    {
        int loaded = 0;

        Expect.Multiple(() =>
        {
            foreach (CategoryDescriptor descriptor in CategorySchemas.All)
            {
                loaded++;
                string path = Path.Combine(SchemaDirectory, descriptor.SchemaFileName);
                JsonSchemaLoadResult load = JsonSchemaLoader.Load(
                    File.ReadAllBytes(path), descriptor.SchemaPath);

                Assert.That(
                    load.IsValid,
                    Is.True,
                    descriptor.SchemaPath + " must load under the loader that rejects an "
                        + "unattributed bound and a sourced bound with no derivation: "
                        + string.Join("; ", load.Diagnostics));
            }

            Assert.That(loaded, Is.GreaterThan(0), NoDeclaredCategories);
        });
    }
}
