using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Schema;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// For every category, the draft 2020-12 schema and the authoritative typed validator
/// reach the same accept/reject verdict.
/// </summary>
/// <remarks>
/// <para>
/// This is doc 40 § JSON codec and schema baseline's mechanism, extended from the
/// envelope to every category: "a fixture corpus proves the schema and typed validator
/// accept/reject the same structural cases."
/// </para>
/// <para>
/// <b>What the comparison is on.</b> The verdict, not the diagnostic. The two
/// validators legitimately report the same rejection differently - a boss ability
/// parameter from another arm is a <c>oneOf</c> failure to the schema and an arm
/// mismatch to the typed validator - and requiring identical codes would be requiring
/// the schema to be a second implementation of the typed one rather than a mirror of it.
/// </para>
/// <para>
/// Rules the JSON Schema data model cannot express are excluded by diagnostic code with
/// a written reason, and each exclusion is separately proven still to be rejected by
/// the typed validator, so an exclusion never becomes an amnesty.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-002-001</c>, <c>VER-DAT-002-026</c>,
/// <c>VER-DAT-003-025</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CategorySchemaAgreementTests
{
    /// <summary>
    /// Diagnostic codes whose rule the draft 2020-12 data model cannot express, with the
    /// reason each one is out of reach and what a reviewer should check instead.
    /// </summary>
    private static readonly Dictionary<string, string> NotExpressibleInJsonSchema =
        new(StringComparer.Ordinal)
        {
            [ContentDiagnosticCodes.DerivedValueAuthored] =
                "the field is undeclared, so the schema does reject the document - but it "
                + "rejects it as an unknown field, which is a different claim. The typed "
                + "register exists to say the value is derived and name its operands, and a "
                + "reviewer comparing verdicts would see agreement while the two are reporting "
                + "different things. Checked instead by the corpus asserting the "
                + "derived-value code specifically.",
            [ContentDiagnosticCodes.SumMismatch] =
                "summing an array's members and comparing the total to a constant needs "
                + "arithmetic over instance values, which no keyword performs",
            [ContentDiagnosticCodes.SequenceNotContiguous] =
                "comparing each element's authored ordinal against its own array index needs "
                + "the index as a value, which the data model does not expose",
            [ContentDiagnosticCodes.ArrayCardinalityWrong] =
                "the cardinality that fails here is checked against another field in the same "
                + "document - a rank array against its cap, minute rows against a duration - "
                + "and minItems takes a constant, not a sibling",
            [ContentDiagnosticCodes.RangeInfeasible] =
                "comparing two sibling numbers to each other is a relation between instance "
                + "values; minimum and maximum each take a constant",
            [ContentDiagnosticCodes.CrossReferenceContradictsOwnId] =
                "the reference is well formed, so the schema's pattern accepts it; what makes it "
                + "wrong is that it disagrees with the definition's own id, and comparing two "
                + "instance values to each other is not something a pattern or an enum can do. "
                + "Checked instead by the corpus asserting this code specifically.",
            [ContentDiagnosticCodes.DuplicateValueInDefinition] =
                "uniqueItems compares whole elements, and what must be unique here is one "
                + "property of each element",
        };

    private static IEnumerable<CategoryFixture> ValidCases => CategoryFixtureCorpus.Valid;

    private static IEnumerable<CategoryFixture> CatalogOnlyCases =>
        CategoryFixtureCorpus.CatalogOnly;

    private static IEnumerable<CategoryFixture> ComparableInvalidCases
    {
        get
        {
            foreach (CategoryFixture fixture in CategoryFixtureCorpus.Invalid)
            {
                if (!NotExpressibleInJsonSchema.ContainsKey(fixture.ExpectedCode!))
                {
                    yield return fixture;
                }
            }
        }
    }

    private static IEnumerable<CategoryDescriptor> Descriptors => CategorySchemas.All;

    /// <summary>
    /// The declared kinds are exactly the members of <see cref="DefinitionKind"/>, and
    /// exactly the category schemas on disk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything else that ranges over <see cref="CategorySchemas.All"/> - the schema
    /// loads here, the field-order comparison, and both halves of the provenance-gate
    /// coverage - is a <c>foreach</c> or a <c>TestCaseSource</c> over it. The table
    /// therefore decides how much all of them prove, and nothing derived from the table
    /// can see it shrink. Deleting a kind deletes its own coverage and leaves every
    /// remaining assertion green.
    /// </para>
    /// <para>
    /// <b>Two anchors, neither of them a number written next to the table.</b> The
    /// <see cref="DefinitionKind"/> enum is one: it lives in a different file, exists for
    /// a different reason - it is what every caller passes to
    /// <see cref="CategorySchemas.Read"/> - and dropping a member from it is a breaking
    /// API change rather than a tidy-up. The schema directory is the other, and it is the
    /// one <c>content/schemas/README.md</c> advertises outside this codebase: "the other
    /// sixteen <c>*.schema.json</c>" and "one schema per definition <em>kind</em> -
    /// sixteen of them". A promise made in a README to readers who will never run this
    /// suite is a promise worth asserting.
    /// </para>
    /// <para>
    /// A count literal would be neither. It would be a third copy of a fact already
    /// stated by the table below it, and the single edit that retires a kind takes the
    /// copy with it.
    /// </para>
    /// </remarks>
    [Test]
    public void TheDeclaredKindsAreExactlyTheKindEnumAndTheSchemasOnDisk()
    {
        List<DefinitionKind> declaredKinds = new();
        List<string> declaredFiles = new();
        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            declaredKinds.Add(descriptor.Kind);
            declaredFiles.Add(descriptor.SchemaFileName);
        }

        List<string> onDisk = new();
        foreach (string path in Directory.GetFiles(
                     Path.Combine(TestArtifacts.RepositoryRoot, "content", "schemas"),
                     "*.schema.json",
                     SearchOption.AllDirectories))
        {
            string name = Path.GetFileName(path);
            if (name != "envelope.schema.json")
            {
                onDisk.Add(name);
            }
        }

        // Unspecified is the zero sentinel - "no kind was chosen" - and declaring it would
        // give an unset field a schema and a reader. It is excluded from the expectation
        // rather than from the enum so the exclusion is one named value here rather than
        // a rule that could quietly widen.
        List<DefinitionKind> expectedKinds = new();
        foreach (DefinitionKind kind in Enum.GetValues<DefinitionKind>())
        {
            if (kind != DefinitionKind.Unspecified)
            {
                expectedKinds.Add(kind);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                declaredKinds,
                Is.EquivalentTo(expectedKinds),
                nameof(CategorySchemas) + " declares a different set of kinds than "
                    + nameof(DefinitionKind) + " names. A kind leaves this table only when "
                    + "it leaves the enum");
            Assert.That(
                declaredKinds,
                Is.Unique,
                "a kind declared twice would be two field tables for one identity");
            Assert.That(
                declaredKinds,
                Does.Not.Contain(DefinitionKind.Unspecified),
                "the zero sentinel must not carry a schema or a reader");
            Assert.That(
                declaredFiles,
                Is.EquivalentTo(onDisk),
                "the declared category schemas are not the category schemas under "
                    + "content/schemas/. content/schemas/README.md tells a reader there is "
                    + "one per definition kind beside the envelope, so a schema with no "
                    + "kind, or a kind with no schema, contradicts a published claim");
            Assert.That(
                onDisk,
                Is.Not.Empty,
                "content/schemas/ holds no category schema, so every walk over it proves "
                    + "nothing");
        });
    }

    [TestCaseSource(nameof(Descriptors))]
    public void EveryCategorySchemaLoads(CategoryDescriptor descriptor)
    {
        JsonSchemaLoadResult load = Load(descriptor);

        Assert.That(
            load.IsValid,
            Is.True,
            () => descriptor.SchemaPath + " must load: " + string.Join("; ", load.Diagnostics));
    }

    [TestCaseSource(nameof(ValidCases))]
    public void BothAcceptAValidFixture(CategoryFixture fixture)
    {
        AssertAgreement(fixture, expectedValid: true);
    }

    [TestCaseSource(nameof(CatalogOnlyCases))]
    public void BothAcceptACatalogOnlyFixture(CategoryFixture fixture)
    {
        AssertAgreement(fixture, expectedValid: true);
    }

    [TestCaseSource(nameof(ComparableInvalidCases))]
    public void BothRejectAnInvalidFixture(CategoryFixture fixture)
    {
        AssertAgreement(fixture, expectedValid: false);
    }

    /// <summary>
    /// An exclusion says "the schema cannot see this", never "nobody checks this".
    /// </summary>
    [Test]
    public void EveryExclusionIsStillRejectedByTheTypedValidator()
    {
        Expect.Multiple(() =>
        {
            foreach (CategoryFixture fixture in CategoryFixtureCorpus.Invalid)
            {
                if (!NotExpressibleInJsonSchema.ContainsKey(fixture.ExpectedCode!))
                {
                    continue;
                }

                DefinitionReadResult typed = CategoryFixtureCorpus.ReadDefinition(fixture);

                Assert.That(
                    typed.IsValid,
                    Is.False,
                    fixture.Path + " is excluded from the schema comparison because "
                        + NotExpressibleInJsonSchema[fixture.ExpectedCode!]
                        + " The typed validator must still reject it.");
            }
        });
    }

    [Test]
    public void EveryExclusionNamesADeclaredCodeAndGivesAReason()
    {
        Expect.Multiple(() =>
        {
            foreach (KeyValuePair<string, string> exclusion in NotExpressibleInJsonSchema)
            {
                Assert.That(
                    ContentDiagnosticCodes.IsDeclared(exclusion.Key),
                    Is.True,
                    exclusion.Key + " must be a declared diagnostic code");
                Assert.That(
                    exclusion.Value.Length,
                    Is.GreaterThan(40),
                    exclusion.Key + " must state a reason a reviewer can act on, not a label");
            }
        });
    }

    /// <summary>
    /// An exclusion that no fixture uses is a stale string that excludes nothing, which
    /// is how the list rots into decoration.
    /// </summary>
    [Test]
    public void EveryExclusionIsUsedByAtLeastOneFixture()
    {
        HashSet<string> used = new(StringComparer.Ordinal);
        foreach (CategoryFixture fixture in CategoryFixtureCorpus.Invalid)
        {
            used.Add(fixture.ExpectedCode!);
        }

        List<string> unused = new();
        foreach (string code in NotExpressibleInJsonSchema.Keys)
        {
            if (!used.Contains(code))
            {
                unused.Add(code);
            }
        }

        Assert.That(
            unused,
            Is.Empty,
            () => "exclusions that no fixture exercises: " + string.Join(", ", unused));
    }

    private static void AssertAgreement(CategoryFixture fixture, bool expectedValid)
    {
        CategoryDescriptor descriptor = CategorySchemas.Describe(fixture.Kind);
        JsonSchemaLoadResult load = Load(descriptor);
        Assert.That(
            load.IsValid,
            Is.True,
            () => descriptor.SchemaPath + " must load: " + string.Join("; ", load.Diagnostics));

        byte[] bytes = CategoryFixtureCorpus.Read(fixture.Path);
        DefinitionReadResult typed = CategoryFixtureCorpus.ReadDefinition(fixture);

        bool schemaAccepts;
        string schemaDetail;
        try
        {
            using JsonDocument instance = JsonDocument.Parse(
                bytes,
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Disallow,
                    AllowTrailingCommas = false,
                });

            JsonSchemaEvaluationResult evaluation =
                JsonSchemaEvaluator.Evaluate(load.Schema!, instance.RootElement);
            schemaAccepts = evaluation.IsValid;
            schemaDetail = evaluation.ToString();
        }
        catch (JsonException exception)
        {
            schemaAccepts = false;
            schemaDetail = "not parseable as JSON: " + exception.Message;
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                typed.IsValid,
                Is.EqualTo(expectedValid),
                () => fixture.Path + ": the typed validator disagreed with the corpus. "
                    + string.Join("; ", typed.Diagnostics));
            Assert.That(
                schemaAccepts,
                Is.EqualTo(expectedValid),
                () => fixture.Path + ": " + descriptor.SchemaPath + " disagreed with the corpus. "
                    + schemaDetail);
            Assert.That(
                schemaAccepts,
                Is.EqualTo(typed.IsValid),
                () => fixture.Path + ": the schema and the typed validator reached different "
                    + "verdicts. typed=" + typed.IsValid + " ("
                    + string.Join("; ", typed.Diagnostics) + "), schema=" + schemaAccepts + " ("
                    + schemaDetail + ")");
        });
    }

    private static JsonSchemaLoadResult Load(CategoryDescriptor descriptor)
    {
        string path = Path.Combine(
            TestArtifacts.RepositoryRoot, "content", "schemas", descriptor.SchemaFileName);
        return JsonSchemaLoader.Load(File.ReadAllBytes(path), descriptor.SchemaPath);
    }
}
