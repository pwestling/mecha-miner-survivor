using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Schema;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// The mechanism doc 40 asks for by name: "a fixture corpus proves the schema and typed
/// validator accept/reject the same structural cases."
/// </summary>
/// <remarks>
/// <para>
/// The word doing the work in that sentence is <b>structural</b>. Several rules the
/// typed validator enforces are not expressible in the JSON Schema data model at all,
/// and pretending otherwise would make this test assert a falsehood. Those cases are
/// enumerated in <see cref="NotExpressibleInJsonSchema"/>, each with the reason, and
/// <see cref="EveryExclusionIsStillRejectedByTheTypedValidator"/> proves that excluding
/// them from the comparison does not excuse them from being caught.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-021</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SchemaAgreementTests
{
    /// <summary>
    /// Diagnostic codes whose rule the draft 2020-12 data model cannot express, with the
    /// reason each one is out of reach.
    /// </summary>
    private static readonly Dictionary<string, string> NotExpressibleInJsonSchema =
        new(StringComparer.Ordinal)
        {
            [ContentDiagnosticCodes.DuplicateProperty] =
                "the JSON data model has no duplicate keys; a parser has already collapsed them "
                + "before a schema sees the instance",
            [ContentDiagnosticCodes.NonfiniteNumber] =
                "1e400 is syntactically valid JSON and no keyword asserts finiteness",
            [ContentDiagnosticCodes.DocumentTooLarge] =
                "a byte ceiling is a codec policy, not a per-field schema constraint",
            [ContentDiagnosticCodes.DepthLimitExceeded] =
                "there is no depth keyword",
            [ContentDiagnosticCodes.ObjectPropertyLimitExceeded] =
                "the envelope's maxProperties is implied by additionalProperties:false, and the "
                + "codec ceiling is a policy applied to every document",
            [ContentDiagnosticCodes.ArrayElementLimitExceeded] =
                "the codec ceiling applies to every array, not to one declared field",
            [ContentDiagnosticCodes.NodeCountLimitExceeded] =
                "there is no whole-document node budget keyword",
            [ContentDiagnosticCodes.StringTooLong] =
                "the codec ceiling applies to every string, not to one declared field",
            [ContentDiagnosticCodes.RetiredIdReused] =
                "a tombstone is state outside the document",
            [ContentDiagnosticCodes.SourceRefScopeUnresolved] =
                "resolving a scope requires the document the scope points into, which is a "
                + "relational check rather than a structural one",
        };

    private static IEnumerable<FixtureCorpus.ValidFixture> ValidCases => FixtureCorpus.Valid;

    private static IEnumerable<FixtureCorpus.InvalidFixture> ComparableInvalidCases
    {
        get
        {
            foreach (FixtureCorpus.InvalidFixture fixture in FixtureCorpus.Invalid)
            {
                if (!NotExpressibleInJsonSchema.ContainsKey(fixture.ExpectedCode))
                {
                    yield return fixture;
                }
            }
        }
    }

    [Test]
    public void TheEnvelopeSchemaLoads()
    {
        JsonSchemaLoadResult load = LoadEnvelopeSchema();

        Assert.That(
            load.IsValid,
            Is.True,
            () => "content/schemas/envelope.schema.json must load: "
                + string.Join("; ", load.Diagnostics));
    }

    [TestCaseSource(nameof(ValidCases))]
    public void BothAcceptAValidFixture(FixtureCorpus.ValidFixture fixture)
    {
        AssertAgreement(fixture.Path, fixture.Context(), expectedValid: true);
    }

    [TestCaseSource(nameof(ComparableInvalidCases))]
    public void BothRejectAnInvalidFixture(FixtureCorpus.InvalidFixture fixture)
    {
        AssertAgreement(fixture.Path, fixture.Context(), expectedValid: false);
    }

    /// <summary>
    /// An exclusion says "the schema cannot see this", never "nobody checks this". Each
    /// excluded code must still be caught by the authoritative typed validator.
    /// </summary>
    [Test]
    public void EveryExclusionIsStillRejectedByTheTypedValidator()
    {
        Expect.Multiple(() =>
        {
            foreach (FixtureCorpus.InvalidFixture fixture in FixtureCorpus.Invalid)
            {
                if (!NotExpressibleInJsonSchema.ContainsKey(fixture.ExpectedCode))
                {
                    continue;
                }

                EnvelopeReadResult typed = EnvelopeReader.Read(
                    FixtureCorpus.Read(fixture.Path), fixture.Context());

                Assert.That(
                    typed.IsValid,
                    Is.False,
                    fixture.Path + " is excluded from the schema comparison because "
                        + NotExpressibleInJsonSchema[fixture.ExpectedCode]
                        + ", so the typed validator must still reject it");
            }
        });
    }

    /// <summary>
    /// An exclusion must be justified against a code that actually exists, so the list
    /// cannot rot into a set of stale strings that silently exclude nothing.
    /// </summary>
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
                Assert.That(exclusion.Value, Is.Not.Empty, exclusion.Key + " must state why");
            }
        });
    }

    private static void AssertAgreement(
        string path,
        EnvelopeReadContext context,
        bool expectedValid)
    {
        JsonSchemaLoadResult load = LoadEnvelopeSchema();
        Assert.That(load.IsValid, Is.True, "the envelope schema must load");

        byte[] bytes = FixtureCorpus.Read(path);

        EnvelopeReadResult typed = EnvelopeReader.Read(bytes, context);

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
            // A document the parser rejects is one the schema rejects: a schema only
            // ever sees a parsed instance.
            schemaAccepts = false;
            schemaDetail = "not parseable as JSON: " + exception.Message;
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                typed.IsValid,
                Is.EqualTo(expectedValid),
                () => path + ": the typed validator disagreed with the corpus. "
                    + string.Join("; ", typed.Diagnostics));
            Assert.That(
                schemaAccepts,
                Is.EqualTo(expectedValid),
                () => path + ": the draft 2020-12 schema disagreed with the corpus. "
                    + schemaDetail);
            Assert.That(
                schemaAccepts,
                Is.EqualTo(typed.IsValid),
                () => path + ": the schema and the typed validator reached different verdicts. "
                    + "typed=" + typed.IsValid + " (" + string.Join("; ", typed.Diagnostics)
                    + "), schema=" + schemaAccepts + " (" + schemaDetail + ")");
        });
    }

    private static JsonSchemaLoadResult LoadEnvelopeSchema()
    {
        string path = Path.Combine(
            TestArtifacts.RepositoryRoot, "content", "schemas", "envelope.schema.json");
        return JsonSchemaLoader.Load(File.ReadAllBytes(path), "content/schemas/envelope.schema.json");
    }
}
