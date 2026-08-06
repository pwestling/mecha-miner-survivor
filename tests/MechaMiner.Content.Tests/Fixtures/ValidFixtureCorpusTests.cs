using System.Collections.Generic;
using MechaMiner.Content.Envelope;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// The over-strict control: a valid fixture produces zero diagnostics of any severity.
/// </summary>
/// <remarks>
/// <para>
/// Without this, the invalid corpus could be satisfied by a validator that rejected
/// everything. Doc 40 § Verification asks for "Invalid-fixture suites"; a suite of only
/// invalid fixtures measures one direction of a two-directional property.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-016</c>, <c>VER-DAT-001-022</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class ValidFixtureCorpusTests
{
    private static IEnumerable<FixtureCorpus.ValidFixture> Cases => FixtureCorpus.Valid;

    [TestCaseSource(nameof(Cases))]
    public void TheFixtureValidatesWithNoDiagnosticsAtAll(FixtureCorpus.ValidFixture fixture)
    {
        EnvelopeReadResult result = EnvelopeReader.Read(
            FixtureCorpus.Read(fixture.Path),
            fixture.Context());

        Expect.Multiple(() =>
        {
            Assert.That(
                result.Diagnostics,
                Is.Empty,
                () => fixture.Path + " must produce no diagnostics, but produced: "
                    + string.Join("; ", result.Diagnostics));
            Assert.That(
                result.IsValid,
                Is.True,
                () => fixture.Path + " must yield an envelope");
        });
    }

    [Test]
    public void TheMinimalFixtureOmitsEveryDeclaredOptionalFieldAndStillValidates()
    {
        EnvelopeReadResult result = Read("valid/envelope-minimal.json");

        Assert.That(result.Envelope, Is.Not.Null);
        DefinitionEnvelope envelope = result.Envelope!;

        Expect.Multiple(() =>
        {
            Assert.That(envelope.NameKey, Is.Null, "an omitted name_key reads as absent");
            Assert.That(envelope.SummaryKey, Is.Null, "an omitted summary_key reads as absent");
            NumericAssert.AreExactlyEqual(
                EnvelopeSchema.InitialSchemaVersion,
                envelope.SchemaVersion,
                "the initial schema_version of a first-authored definition");
            NumericAssert.AreExactlyEqual(
                EnvelopeSchema.InitialContentVersion,
                envelope.ContentVersion,
                "the initial content_version of a first-authored definition");
            Assert.That(envelope.Tags, Is.Empty, "[] is the expected value of tags");
        });
    }

    /// <summary>
    /// The maximal fixture carries every field a definition may author - the six required
    /// and both declared-optional keys - and exercises all five reference forms.
    /// </summary>
    /// <remarks>
    /// It carried nine until <c>presentation_id</c> became required absent. A maximal
    /// fixture is maximal among <em>legal</em> documents, so the ninth row is now proved by
    /// <c>invalid/structural-presentation-id-authored.json</c> instead; carrying it here
    /// would make the over-strict control assert that an unauthorized value is clean.
    /// </remarks>
    [Test]
    public void TheMaximalFixtureCarriesEveryAuthorableFieldAndParsesEveryReferenceForm()
    {
        EnvelopeReadResult result = Read("valid/envelope-maximal.json");
        Assert.That(result.Envelope, Is.Not.Null, () => string.Join("; ", result.Diagnostics));
        DefinitionEnvelope envelope = result.Envelope!;

        HashSet<SourceRefKind> kinds = new();
        foreach (SourceRef reference in envelope.SourceRefs)
        {
            kinds.Add(reference.Kind);
        }

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual("W-AB", envelope.Id.Value, "the maximal fixture's ID");
            Assert.That(envelope.NameKey, Is.Not.Null);
            Assert.That(envelope.SummaryKey, Is.Not.Null);
            Assert.That(
                kinds,
                Is.EquivalentTo(new[]
                {
                    SourceRefKind.GameplayDocument,
                    SourceRefKind.TechnicalDocument,
                    SourceRefKind.GameplayDecision,
                    SourceRefKind.TechnicalDecision,
                    SourceRefKind.TechnicalRequirement,
                }),
                "the maximal fixture exercises all five reference kinds");
        });
    }

    /// <summary>
    /// An aggregate omits <c>name_key</c>, which doc 40 prescribes for it specifically.
    /// </summary>
    /// <remarks>
    /// It omits <c>presentation_id</c> too, but that is no longer a fact about aggregates:
    /// every definition omits it now, and the rule is asserted where it belongs, in
    /// <c>EnvelopeValidatorTests</c> and the two invalid fixtures.
    /// </remarks>
    [Test]
    public void AnAggregateOmitsNameKeyAsDocFortyPrescribes()
    {
        // doc 40 § Encounter schedule: WAV-01 "is not embodied in the world and players
        // never read its name, so it omits presentation_id and name_key".
        EnvelopeReadResult result = Read("valid/envelope-aggregate.json");
        Assert.That(result.Envelope, Is.Not.Null, () => string.Join("; ", result.Diagnostics));

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual("WAV-01", result.Envelope!.Id.Value, "the aggregate ID");
            Assert.That(result.Envelope.NameKey, Is.Null);
        });
    }

    private static EnvelopeReadResult Read(string path)
    {
        foreach (FixtureCorpus.ValidFixture fixture in FixtureCorpus.Valid)
        {
            if (string.Equals(fixture.Path, path, System.StringComparison.Ordinal))
            {
                return EnvelopeReader.Read(FixtureCorpus.Read(path), fixture.Context());
            }
        }

        throw new System.InvalidOperationException("fixture '" + path + "' is not in the corpus");
    }
}
