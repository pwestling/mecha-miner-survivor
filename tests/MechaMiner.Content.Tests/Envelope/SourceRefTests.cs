using MechaMiner.Content.Envelope;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Envelope;

/// <summary>
/// The <c>source_refs</c> element grammar. Verification: <c>VER-DAT-001-018</c>.
/// </summary>
[TestFixture]
internal sealed class SourceRefTests
{
    [TestCase("GDD-COMBAT", SourceRefKind.GameplayDocument, "GDD-COMBAT", null)]
    [TestCase("GDD-COMBAT#contact-damage", SourceRefKind.GameplayDocument, "GDD-COMBAT", "contact-damage")]
    [TestCase("TDD-ENCOUNTERS#elite-construction", SourceRefKind.TechnicalDocument, "TDD-ENCOUNTERS", "elite-construction")]
    [TestCase("DEC-120", SourceRefKind.GameplayDecision, "DEC-120", null)]
    [TestCase("DEC-120#decision", SourceRefKind.GameplayDecision, "DEC-120", "decision")]
    [TestCase("TDR-006", SourceRefKind.TechnicalDecision, "TDR-006", null)]
    [TestCase("TDR-006#consequences", SourceRefKind.TechnicalDecision, "TDR-006", "consequences")]
    [TestCase("TR-DAT-006", SourceRefKind.TechnicalRequirement, "TR-DAT-006", null)]
    [TestCase("TR-DAT-006#codec", SourceRefKind.TechnicalRequirement, "TR-DAT-006", "codec")]
    public void AReferenceParsesIntoItsKindDocumentAndAnchor(
        string element, SourceRefKind kind, string documentId, string? anchor)
    {
        Assert.That(
            SourceRefGrammar.Parse(element, out SourceRef? parsed),
            Is.EqualTo(SourceRefParseOutcome.Parsed),
            () => "parsing " + element);

        Expect.Multiple(() =>
        {
            Assert.That(parsed!.Kind, Is.EqualTo(kind));
            NumericAssert.AreExactlyEqual(documentId, parsed.DocumentId, "document ID");
            Assert.That(parsed.Anchor, anchor is null ? Is.Null : Is.EqualTo(anchor));
            Assert.That(parsed.Scope, Is.Null, "no scope prefix on this element");
        });
    }

    [TestCase("recipe_pair: GDD-WEAPON-CATALOG#accepted-base-catalog-assignment", "recipe_pair")]
    [TestCase("resonance_behavior.short_modifier: GDD-MINING#geode-resonance-fields", "resonance_behavior.short_modifier")]
    [TestCase("rules[]: GDD-MINING#x", "rules[]")]
    [TestCase("rules[2..3]: GDD-MINING#x", "rules[2..3]")]
    [TestCase("minute_rows[33].formation_events[].timestamps_reconstructed: GDD-STANDARD-WAVE-SCHEDULE#x", "minute_rows[33].formation_events[].timestamps_reconstructed")]
    [TestCase("unlocks.utilities[].utility_id: GDD-UTILITY-CATALOG#catalog-overview", "unlocks.utilities[].utility_id")]
    public void AScopePrefixIsParsedSeparatelyFromItsReference(string element, string scope)
    {
        Assert.That(
            SourceRefGrammar.Parse(element, out SourceRef? parsed),
            Is.EqualTo(SourceRefParseOutcome.Parsed),
            () => "parsing " + element);

        NumericAssert.AreExactlyEqual(scope, parsed!.Scope!.Text, "the scope prefix");
    }

    /// <summary>
    /// The ban that matters. Doc 40: "A file path, a line number, or any <c>path:line</c>
    /// pair is <b>not</b> a legal element." Extending the grammar for anchors must not
    /// loosen this.
    /// </summary>
    [TestCase("docs/technical/40-content-data-and-validation.md:224")]
    [TestCase("docs/technical/40-content-data-and-validation.md")]
    [TestCase("40-content.md:12")]
    [TestCase("src\\MechaMiner.Content\\Codec\\JsonPointer.cs:9")]
    [TestCase("./docs/40.md")]
    [TestCase("content/weapons/W-AB.json")]
    public void APathOrPathLinePairIsRejectedUnderItsOwnOutcome(string element)
    {
        Assert.That(
            SourceRefGrammar.Parse(element, out _),
            Is.EqualTo(SourceRefParseOutcome.PathLine),
            () => element + " must be identified as a path, not merely as malformed");
    }

    [TestCase("GDD_COMBAT")]
    [TestCase("gdd-combat")]
    [TestCase("DEC-12")]
    [TestCase("DEC-1200")]
    [TestCase("TR-dat-006")]
    [TestCase("GDD-COMBAT#Contact-Damage")]
    [TestCase("GDD-COMBAT #anchor")]
    [TestCase("Scope: GDD-COMBAT")]
    [TestCase("scope:GDD-COMBAT")]
    [TestCase("rules[3..2]: GDD-MINING#x")]
    [TestCase("")]
    public void AMalformedElementIsRejected(string element)
    {
        Assert.That(
            SourceRefGrammar.Parse(element, out _),
            Is.EqualTo(SourceRefParseOutcome.Malformed),
            () => "parsing " + element);
    }

    /// <summary>
    /// A range wider than the cap is annotating the array, not a part of it, and should
    /// say so with <c>[]</c>.
    /// </summary>
    [Test]
    public void AnImplausiblyWideRangeIsRejected()
    {
        Assert.That(
            SourceRefGrammar.Parse("rules[0..9999]: GDD-MINING#x", out _),
            Is.EqualTo(SourceRefParseOutcome.Malformed));
    }

    /// <summary>
    /// The parser and the constant the schema mirrors must accept exactly the same
    /// strings, or the schema is pinning a different grammar from the one enforced.
    /// </summary>
    [TestCase("GDD-COMBAT#contact-damage", true)]
    [TestCase("DEC-120#decision", true)]
    [TestCase("rules[2..3]: GDD-MINING#x", true)]
    [TestCase("docs/foo.md:12", false)]
    [TestCase("GDD_COMBAT", false)]
    [TestCase("", false)]
    public void TheSchemaPatternAndTheParserAgree(string element, bool expected)
    {
        bool byPattern = System.Text.RegularExpressions.Regex.IsMatch(
            element, AnchoredPatternText());
        bool byParser =
            SourceRefGrammar.Parse(element, out _) == SourceRefParseOutcome.Parsed;

        Expect.Multiple(() =>
        {
            Assert.That(byPattern, Is.EqualTo(expected), "the mirrored pattern");
            Assert.That(byParser, Is.EqualTo(expected), "the parser");
        });
    }

    private static string AnchoredPatternText()
    {
        return MechaMiner.Content.Codec.AnchoredPattern.Translate(SourceRefGrammar.ElementPattern);
    }
}
