using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// The schema's patterns are the same patterns the typed validator enforces.
/// </summary>
/// <remarks>
/// <para>
/// A string field carrying a mini-language drifts unless its shape is pinned
/// structurally, and two patterns that merely resemble each other are worse than one:
/// the agreement corpus would report a disagreement without saying which side is wrong.
/// These tests compare the schema's pattern text against the constant the parser is
/// built from, so a change to one without the other is a compile-time or test-time
/// failure rather than a slow divergence.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-023</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class EnvelopeSchemaPatternTests
{
    /// <summary>
    /// The nine envelope field names, written out here rather than read from either side.
    /// </summary>
    /// <remarks>
    /// <b>This list is a third anchor and changing it is a deliberate change.</b>
    /// <c>content/schemas/envelope.schema.json</c> and <see cref="EnvelopeSchema.Fields"/>
    /// both derive from doc 40 § Common definition envelope, so holding them equal to each
    /// other is blind to a field deleted from both in one edit - doc 91 § Negative control
    /// adequacy: "an invariant asserting that two sets match is blind to a correlated
    /// deletion from both sides ... such an invariant needs a third anchor". The count is
    /// advertised outside this file - <c>content/schemas/README.md</c> calls the envelope
    /// "the nine-field common definition envelope", the schema's own <c>description</c>
    /// says "the nine fields every ... definition carries", and
    /// <c>VER-DAT-001-016</c> states "the SCH-CNT-001 envelope has exactly nine fields" -
    /// so the number is a promise this test has to keep, not a label on it.
    /// </remarks>
    private static readonly string[] TheNineEnvelopeFields =
    {
        "id",
        "schema_version",
        "content_version",
        "status",
        "name_key",
        "summary_key",
        "tags",
        "source_refs",
        "presentation_id",
    };

    /// <summary>
    /// The six fields doc 40 requires, written out here rather than read from either side.
    /// </summary>
    /// <remarks>
    /// Same third-anchor role as <see cref="TheNineEnvelopeFields"/>. <c>VER-DAT-001-016</c>
    /// states "the six required ones are errors when missing" and
    /// <see cref="EnvelopeSchema"/>'s remarks head a paragraph "Six required, two
    /// declared-optional, one required absent", so the six is advertised elsewhere and
    /// cannot be quietly reduced to five here.
    /// </remarks>
    private static readonly string[] TheSixRequiredEnvelopeFields =
    {
        "id",
        "schema_version",
        "content_version",
        "status",
        "tags",
        "source_refs",
    };

    /// <summary>
    /// The four <c>status</c> tokens, written out here rather than read from either side.
    /// </summary>
    /// <remarks>
    /// Doc 40 § Common definition envelope's <c>status</c> row states them as prose -
    /// "development, enabled, disabled, or retired" - and both the schema's <c>enum</c> and
    /// <see cref="DefinitionStatuses"/> restate it. Two restatements of one sentence are
    /// not two witnesses: dropping <c>retired</c> from the enum and from the token table in
    /// one edit left the old two-set assertion green. This roster is the third party.
    /// </remarks>
    private static readonly string[] TheFourStatusTokens =
    {
        "development",
        "enabled",
        "disabled",
        "retired",
    };

    /// <summary>
    /// Every term the closed <c>tags</c> vocabulary accepts: deliberately none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Doc 40 § <c>tags</c> vocabulary: "The closed vocabulary starts <b>empty</b> and
    /// gains a term only when a concrete query or tooling need requires it; the term is
    /// added to the vocabulary in the same change that first uses it."
    /// <c>VER-DAT-001-017</c> advertises the same claim.
    /// </para>
    /// <para>
    /// <b>An empty roster is a real anchor only if the reader is a reader.</b> Both sides
    /// of this comparison are empty today, so every set assertion over them passes without
    /// reading anything - including one whose extraction silently found no <c>enum</c> at
    /// all. <see cref="TheTagEnumReaderIsACounterAndNotAConstantZero"/> is the control for
    /// that: it runs the same extraction over a schema carrying three terms and requires
    /// three back. Adding a term to the schema and to
    /// <see cref="TagVocabulary"/> together still fails here, because this array is
    /// neither.
    /// </para>
    /// </remarks>
    private static readonly string[] TheClosedTagVocabulary = System.Array.Empty<string>();

    private static JsonDocument Schema()
    {
        string path = Path.Combine(
            TestArtifacts.RepositoryRoot, "content", "schemas", "envelope.schema.json");
        return JsonDocument.Parse(File.ReadAllBytes(path));
    }

    private static string PatternOf(JsonDocument schema, string definition)
    {
        return schema.RootElement
            .GetProperty("$defs").GetProperty(definition).GetProperty("pattern").GetString()!;
    }

    [Test]
    public void TheSourceRefPatternIsExactlyTheConstantTheParserUses()
    {
        using JsonDocument schema = Schema();

        NumericAssert.AreExactlyEqual(
            SourceRefGrammar.ElementPattern,
            PatternOf(schema, "source_ref"),
            "the schema mirrors SourceRefGrammar.ElementPattern verbatim");
    }

    [Test]
    public void TheLocalizationKeyPatternsAreExactlyTheConstantsTheParserUses()
    {
        using JsonDocument schema = Schema();

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                LocalizationKey.NamePattern, PatternOf(schema, "name_key"), "name_key pattern");
            NumericAssert.AreExactlyEqual(
                LocalizationKey.SummaryPattern,
                PatternOf(schema, "summary_key"),
                "summary_key pattern");
        });
    }

    [Test]
    public void TheSnakeCaseFieldPatternMatchesTheCodecRule()
    {
        using JsonDocument schema = Schema();

        NumericAssert.AreExactlyEqual(
            "^[a-z][a-z0-9_]*$",
            PatternOf(schema, "snake_case_field"),
            "the schema states the same field-name rule StrictJsonReader.IsSnakeCase enforces");
    }

    /// <summary>
    /// Every category's grammar appears in the schema and nothing else does, so a new
    /// category cannot be added to the code without the schema noticing.
    /// </summary>
    /// <remarks>
    /// This compares two sets that both derive from doc 40 and is therefore blind on its
    /// own to a grammar deleted from both sides, which doc 91 § Negative control adequacy
    /// forbids leaving unanchored. The third anchor is
    /// <see cref="DocumentGrammarAgreementTests"/>, which names every grammar
    /// independently of either side: the five doc 40 mints and implements, and the eleven
    /// it does not mint.
    /// </remarks>
    [Test]
    public void TheStableIdAlternativesAreExactlyTheDeclaredCategoryGrammars()
    {
        using JsonDocument schema = Schema();

        List<string> fromSchema = new();
        foreach (JsonElement alternative in schema.RootElement
                     .GetProperty("$defs").GetProperty("stable_id").GetProperty("anyOf")
                     .EnumerateArray())
        {
            fromSchema.Add(alternative.GetProperty("pattern").GetString()!);
        }

        List<string> fromCode = new();
        foreach (ContentCategoryDescriptor descriptor in ContentCategories.All)
        {
            fromCode.AddRange(descriptor.IdPatterns);
        }

        Assert.That(
            fromSchema,
            Is.EquivalentTo(fromCode),
            "the schema's ID alternatives and ContentCategories must declare the same grammars");
    }

    /// <summary>
    /// The schema's <c>status</c> enum, <see cref="DefinitionStatuses.Tokens"/>, and the
    /// four tokens doc 40 states all name the same set.
    /// </summary>
    /// <remarks>
    /// One assertion per token, so a deletion fails a message naming the token that went,
    /// plus set equality in both directions, so an addition fails too. The third anchor is
    /// <see cref="TheFourStatusTokens"/>: without it this was a two-set comparison and a
    /// token removed from the schema and the token table together passed.
    /// </remarks>
    [Test]
    public void TheStatusEnumIsExactlyTheAcceptedTokens()
    {
        using JsonDocument schema = Schema();
        List<string> fromSchema = EnumTermsOf(schema, "status");

        Expect.Multiple(() =>
        {
            foreach (string token in TheFourStatusTokens)
            {
                Assert.That(
                    fromSchema,
                    Does.Contain(token),
                    "the envelope schema's $defs/status enum no longer accepts '" + token
                        + "'. Doc 40 § Common definition envelope states four tokens and this "
                        + "test states them independently of the schema and of "
                        + nameof(DefinitionStatuses)
                        + ", so removing one from both sides at once fails here by name");
                Assert.That(
                    DefinitionStatuses.Tokens,
                    Does.Contain(token),
                    nameof(DefinitionStatuses) + " no longer declares the token '" + token
                        + "' that doc 40's status row states");
            }

            Assert.That(
                fromSchema,
                Is.EqualTo(DefinitionStatuses.Tokens),
                "the schema's enum and " + nameof(DefinitionStatuses)
                    + " must agree in lifecycle order, not merely as sets");
            Assert.That(
                fromSchema,
                Is.EquivalentTo(TheFourStatusTokens),
                "a status token added to the schema must be added to "
                    + nameof(TheFourStatusTokens) + " in the same change, so the enum and its "
                    + "independent statement cannot drift");
            Assert.That(
                fromSchema,
                Has.Count.EqualTo(TheFourStatusTokens.Length),
                "the accepted tokens are exactly four");
        });
    }

    /// <summary>
    /// The schema's <c>tag</c> enum and <see cref="TagVocabulary.Terms"/> are both exactly
    /// the closed vocabulary, which is currently empty.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This comparison used to hold two sets equal that are <em>both empty</em>, so it
    /// passed without reading either and was blind to a term added to one side and the
    /// other in one edit as well as to the enum disappearing entirely.
    /// <see cref="TheClosedTagVocabulary"/> is the third anchor, and
    /// <see cref="TheTagEnumReaderIsACounterAndNotAConstantZero"/> is the control that the
    /// extraction behind the counts is a reader rather than a constant.
    /// </para>
    /// </remarks>
    [Test]
    public void TheTagEnumMatchesTheClosedVocabulary()
    {
        using JsonDocument schema = Schema();

        Assert.That(
            schema.RootElement.GetProperty("$defs").GetProperty("tag")
                .TryGetProperty("enum", out JsonElement declared),
            Is.True,
            "content/schemas/envelope.schema.json $defs/tag declares no enum at all, so the "
                + "vocabulary is no longer closed by the schema. An absent enum and an empty "
                + "one read the same to a set comparison and mean opposite things");
        Assert.That(
            declared.ValueKind,
            Is.EqualTo(JsonValueKind.Array),
            "$defs/tag/enum must be an array of terms");

        List<string> fromSchema = EnumTermsOf(schema, "tag");

        Expect.Multiple(() =>
        {
            foreach (string term in TheClosedTagVocabulary)
            {
                Assert.That(
                    fromSchema,
                    Does.Contain(term),
                    "the schema's tag enum no longer accepts '" + term + "'");
                Assert.That(
                    TagVocabulary.Terms,
                    Does.Contain(term),
                    nameof(TagVocabulary) + " no longer declares the term '" + term + "'");
            }

            Assert.That(
                fromSchema,
                Is.EquivalentTo(TagVocabulary.Terms),
                "the schema's tag enum and " + nameof(TagVocabulary) + " must name the same "
                    + "terms");
            Assert.That(
                fromSchema,
                Is.EquivalentTo(TheClosedTagVocabulary),
                "a term added to the schema's tag enum must be added to "
                    + nameof(TheClosedTagVocabulary) + " in the same change; adding it to the "
                    + "schema and to " + nameof(TagVocabulary) + " together is exactly the "
                    + "correlated change the two-set comparison could not see");
            Assert.That(
                TagVocabulary.Terms,
                Is.EquivalentTo(TheClosedTagVocabulary),
                "a term added to " + nameof(TagVocabulary) + " must be added to "
                    + nameof(TheClosedTagVocabulary) + " in the same change");
            Assert.That(
                fromSchema,
                Has.Count.EqualTo(TheClosedTagVocabulary.Length),
                "the closed vocabulary has "
                    + TheClosedTagVocabulary.Length.ToString(CultureInfo.InvariantCulture)
                    + " terms; doc 40 § tags vocabulary says it starts empty and grows one "
                    + "term at a time in the change that first uses it");
            Assert.That(
                TagVocabulary.IsEmpty,
                Is.EqualTo(TheClosedTagVocabulary.Length == 0),
                nameof(TagVocabulary) + "." + nameof(TagVocabulary.IsEmpty)
                    + " must agree with the roster, so the emptiness the diagnostic text "
                    + "promises is the emptiness this test measured");
        });
    }

    /// <summary>
    /// The control for the counts in <see cref="TheTagEnumMatchesTheClosedVocabulary"/>:
    /// the extraction those counts are taken over reports what is there.
    /// </summary>
    /// <remarks>
    /// Every count in that test is currently zero, and a reader that always returned zero -
    /// a wrong <c>$defs</c> key, a renamed <c>enum</c>, an extraction that never entered its
    /// loop - would satisfy all of them. This runs <see cref="EnumTermsOf"/>, the same
    /// method, over a schema shaped like the real one and carrying three terms, and requires
    /// the three terms back. That is what makes the zero a measurement.
    /// </remarks>
    [Test]
    public void TheTagEnumReaderIsACounterAndNotAConstantZero()
    {
        using JsonDocument populated = JsonDocument.Parse(
            "{\"$defs\":{\"tag\":{\"enum\":[\"alpha\",\"beta\",\"gamma\"]}}}");

        List<string> terms = EnumTermsOf(populated, "tag");

        Expect.Multiple(() =>
        {
            Assert.That(
                terms,
                Is.EqualTo(new[] { "alpha", "beta", "gamma" }),
                "the same extraction the closed-vocabulary test counts over must return the "
                    + "terms a populated enum holds");
            Assert.That(
                terms,
                Has.Count.EqualTo(3),
                "and its count must be a count; if this reads zero, so does every assertion "
                    + "in " + nameof(TheTagEnumMatchesTheClosedVocabulary)
                    + " no matter what the schema says");
        });
    }

    /// <summary>
    /// The schema's <c>required</c> list, <see cref="EnvelopeSchema.Required"/>, and the six
    /// fields doc 40 requires all name the same set.
    /// </summary>
    /// <remarks>
    /// One assertion per field so a deletion names what went, set equality so an addition
    /// fails, and a count so the "six" in this test's name is asserted rather than asserted
    /// about. <see cref="TheSixRequiredEnvelopeFields"/> is the third anchor that survives a
    /// field deleted from the schema and from <see cref="EnvelopeSchema"/> together.
    /// </remarks>
    [Test]
    public void TheRequiredListIsExactlyTheSixRequiredEnvelopeFields()
    {
        using JsonDocument schema = Schema();

        List<string> required = new();
        foreach (JsonElement field in schema.RootElement.GetProperty("required").EnumerateArray())
        {
            required.Add(field.GetString()!);
        }

        Expect.Multiple(() =>
        {
            foreach (string field in TheSixRequiredEnvelopeFields)
            {
                Assert.That(
                    required,
                    Does.Contain(field),
                    "content/schemas/envelope.schema.json no longer requires '" + field
                        + "'. Deleting it here and from " + nameof(EnvelopeSchema)
                        + ".Required in one edit leaves the two sides agreeing with each "
                        + "other, which is why this test names the field independently");
                Assert.That(
                    EnvelopeSchema.Required,
                    Does.Contain(field),
                    nameof(EnvelopeSchema) + ".Required no longer requires '" + field + "'");
            }

            Assert.That(
                required,
                Is.EquivalentTo(EnvelopeSchema.Required),
                "the schema's required list and " + nameof(EnvelopeSchema)
                    + ".Required must name the same fields");
            Assert.That(
                required,
                Is.EquivalentTo(TheSixRequiredEnvelopeFields),
                "a field made required must be added to "
                    + nameof(TheSixRequiredEnvelopeFields) + " in the same change");
            Assert.That(
                required,
                Has.Count.EqualTo(TheSixRequiredEnvelopeFields.Length),
                "doc 40 § Common definition envelope requires exactly "
                    + TheSixRequiredEnvelopeFields.Length.ToString(CultureInfo.InvariantCulture)
                    + " of the nine fields");
        });
    }

    /// <summary>
    /// The schema's <c>properties</c>, <see cref="EnvelopeSchema.Fields"/>, and the nine
    /// fields doc 40 tabulates all name the same set.
    /// </summary>
    /// <remarks>
    /// The name said "the nine" and the body compared two sets that both derive from doc 40,
    /// asserting no count: a field deleted from the schema and from
    /// <see cref="EnvelopeSchema"/> together kept it green.
    /// <see cref="TheNineEnvelopeFields"/> is the third anchor, one assertion per field so
    /// the failure names the field, plus set equality so an addition fails too.
    /// </remarks>
    [Test]
    public void ThePropertiesAreExactlyTheNineEnvelopeFields()
    {
        using JsonDocument schema = Schema();

        List<string> properties = new();
        foreach (JsonProperty property in
                 schema.RootElement.GetProperty("properties").EnumerateObject())
        {
            properties.Add(property.Name);
        }

        Expect.Multiple(() =>
        {
            foreach (string field in TheNineEnvelopeFields)
            {
                Assert.That(
                    properties,
                    Does.Contain(field),
                    "content/schemas/envelope.schema.json no longer declares the property '"
                        + field + "'. Deleting it here and from " + nameof(EnvelopeSchema)
                        + ".Fields in one edit leaves both sides agreeing, so this test names "
                        + "the nine fields itself");
                Assert.That(
                    EnvelopeSchema.Fields,
                    Does.Contain(field),
                    nameof(EnvelopeSchema) + ".Fields no longer declares '" + field + "'");
            }

            Assert.That(
                properties,
                Is.EquivalentTo(EnvelopeSchema.Fields),
                "the schema's properties and " + nameof(EnvelopeSchema)
                    + ".Fields must name the same fields");
            Assert.That(
                properties,
                Is.EquivalentTo(TheNineEnvelopeFields),
                "a tenth envelope field must be added to " + nameof(TheNineEnvelopeFields)
                    + " in the same change");
            Assert.That(
                properties,
                Has.Count.EqualTo(TheNineEnvelopeFields.Length),
                "doc 40 § Common definition envelope tabulates exactly "
                    + TheNineEnvelopeFields.Length.ToString(CultureInfo.InvariantCulture)
                    + " fields");
        });
    }

    /// <summary>Reads every term of a <c>$defs</c> definition's <c>enum</c>.</summary>
    private static List<string> EnumTermsOf(JsonDocument schema, string definition)
    {
        List<string> terms = new();
        foreach (JsonElement term in schema.RootElement
                     .GetProperty("$defs").GetProperty(definition).GetProperty("enum")
                     .EnumerateArray())
        {
            terms.Add(term.GetString()!);
        }

        return terms;
    }

    /// <summary>
    /// The negative control for VER-DAT-001-023: the pattern alone, with the typed parser
    /// removed from the path, must reject a malformed element and a path:line pair.
    /// </summary>
    [TestCase("GDD-WEAPON-CATALOG#accepted-base-catalog-assignment", true)]
    [TestCase("DEC-120#decision", true)]
    [TestCase("rules[2..3]: GDD-MINING#x", true)]
    [TestCase("GDD_WEAPON_CATALOG", false)]
    [TestCase("docs/technical/40-content-data-and-validation.md:224", false)]
    [TestCase("40-content.md:12", false)]
    public void TheSchemaPatternAloneAcceptsAndRejectsTheRightElements(
        string element, bool expected)
    {
        using JsonDocument schema = Schema();
        string pattern = PatternOf(schema, "source_ref");

        Assert.That(
            AnchoredPattern.Compile(pattern).IsMatch(element),
            Is.EqualTo(expected),
            () => "the committed schema pattern against " + element);
    }
}
