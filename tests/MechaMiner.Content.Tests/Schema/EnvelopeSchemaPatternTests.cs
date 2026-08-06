using System.Collections.Generic;
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

    [Test]
    public void TheStatusEnumIsExactlyTheAcceptedTokens()
    {
        using JsonDocument schema = Schema();

        List<string> fromSchema = new();
        foreach (JsonElement token in schema.RootElement
                     .GetProperty("$defs").GetProperty("status").GetProperty("enum")
                     .EnumerateArray())
        {
            fromSchema.Add(token.GetString()!);
        }

        Assert.That(fromSchema, Is.EqualTo(DefinitionStatuses.Tokens));
    }

    /// <summary>
    /// The vocabulary is closed and empty in both places, and must stay in step: a term
    /// added to one and not the other is a rule that holds in the mirror but not the gate.
    /// </summary>
    [Test]
    public void TheTagEnumMatchesTheClosedVocabulary()
    {
        using JsonDocument schema = Schema();

        List<string> fromSchema = new();
        foreach (JsonElement term in schema.RootElement
                     .GetProperty("$defs").GetProperty("tag").GetProperty("enum")
                     .EnumerateArray())
        {
            fromSchema.Add(term.GetString()!);
        }

        Assert.That(fromSchema, Is.EquivalentTo(TagVocabulary.Terms));
    }

    [Test]
    public void TheRequiredListIsExactlyTheSixRequiredEnvelopeFields()
    {
        using JsonDocument schema = Schema();

        List<string> required = new();
        foreach (JsonElement field in schema.RootElement.GetProperty("required").EnumerateArray())
        {
            required.Add(field.GetString()!);
        }

        Assert.That(required, Is.EquivalentTo(EnvelopeSchema.Required));
    }

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

        Assert.That(properties, Is.EquivalentTo(EnvelopeSchema.Fields));
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
