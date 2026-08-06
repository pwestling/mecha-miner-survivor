using System.Collections.Generic;
using System.Text;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Envelope;

/// <summary>
/// The nine-field envelope contract. Verification: <c>VER-DAT-001-016</c>.
/// </summary>
[TestFixture]
internal sealed class EnvelopeValidatorTests
{
    /// <summary>
    /// Nine declared rows, split six / two / one.
    /// </summary>
    /// <remarks>
    /// <c>presentation_id</c> was the third declared-optional field until the presentation
    /// category was ruled unminted. It is still one of the nine - it is declared, so a
    /// document carrying it is not carrying an unknown field - but it is required absent,
    /// and the split is what says so.
    /// </remarks>
    [Test]
    public void TheEnvelopeHasNineFieldsSixRequiredTwoOptionalAndOneRequiredAbsent()
    {
        Expect.Multiple(() =>
        {
            Assert.That(EnvelopeSchema.Fields, Has.Count.EqualTo(9));
            Assert.That(EnvelopeSchema.Required, Has.Count.EqualTo(6));
            Assert.That(EnvelopeSchema.DeclaredOptional, Has.Count.EqualTo(2));
            Assert.That(
                EnvelopeSchema.DeclaredOptional,
                Is.EquivalentTo(new[] { "name_key", "summary_key" }),
                "doc 40 § Declared-optional envelope fields names presentation_id and name_key, "
                    + "and says summary_key follows the same rule its row already states; "
                    + "presentation_id has since become required absent");
            Assert.That(
                EnvelopeSchema.RequiredAbsent,
                Is.EquivalentTo(new[] { "presentation_id" }),
                "no accepted document says what a presentation definition contains, so no ID "
                    + "grammar has been minted for one and no definition may carry the field");
            Assert.That(
                EnvelopeSchema.Required.Count + EnvelopeSchema.DeclaredOptional.Count
                    + EnvelopeSchema.RequiredAbsent.Count,
                Is.EqualTo(EnvelopeSchema.Fields.Count),
                "the three classes partition the nine; a field in none of them is a field "
                    + "nobody decided about");
        });
    }

    /// <summary>
    /// A definition carrying <c>presentation_id</c> is rejected, whatever it carries.
    /// </summary>
    /// <remarks>
    /// The empty string is the case with teeth. Under the declared-optional rule it was an
    /// empty optional field (<c>MMC-2009</c>) - a complaint about the shape of a value,
    /// which invites an author to supply a better one. Under the ruling the field itself is
    /// unauthorized, so every value including that one is the same fault and reports the
    /// same code. A number is here because the fault is detected before anything asks what
    /// kind of value the field holds.
    /// </remarks>
    [TestCase("\"weapon-ab-emitter\"")]
    [TestCase("\"\"")]
    [TestCase("3")]
    public void ADefinitionCarryingAPresentationIdIsRejected(string value)
    {
        EnvelopeReadResult result = Read(Envelope("W-AB", presentationId: value));

        Expect.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(ContentDiagnosticCodes.PresentationIdNotMinted),
                () => "the fault is the field's presence: " + string.Join("; ", result.Diagnostics));
            NumericAssert.AreExactlyEqual(
                "/presentation_id",
                result.Diagnostics[0].Location.Value,
                "the pointer of the field at fault");
            Assert.That(
                result.Diagnostics[0].ExpectedConstraint,
                Does.Contain("presentation_id").And.Contain("not yet minted"),
                "the diagnostic names the field and says the presentation category is not "
                    + "minted, rather than reporting a shape the author could correct");
        });
    }

    /// <summary>
    /// The positive control: omitting the field is what every definition does, and it
    /// validates.
    /// </summary>
    /// <remarks>
    /// Without this the rejection above would be satisfied by a validator that rejected
    /// every envelope.
    /// </remarks>
    [Test]
    public void ADefinitionOmittingPresentationIdIsAccepted()
    {
        EnvelopeReadResult result = Read(Envelope("W-AB"));

        Expect.Multiple(() =>
        {
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.IsValid, Is.True);
        });
    }

    /// <summary>
    /// <c>presentation_id</c> is declared, so carrying one is the presentation ruling and
    /// not an unknown field.
    /// </summary>
    /// <remarks>
    /// The distinction is the whole point of leaving the field in the declared nine.
    /// "Unknown field" tells an author the name is wrong; this tells them the name is
    /// right and the category does not exist yet.
    /// </remarks>
    [Test]
    public void APresentationIdIsNotReportedAsAnUnknownField()
    {
        EnvelopeReadResult result = Read(Envelope("W-AB", presentationId: "\"anything\""));

        Expect.Multiple(() =>
        {
            Assert.That(
                Codes(result),
                Does.Not.Contain(ContentDiagnosticCodes.UnknownField),
                "the field is one of doc 40's nine rows");
            Assert.That(
                Codes(result),
                Does.Not.Contain(ContentDiagnosticCodes.EmptyOptionalField),
                "it is not a declared-optional field any more, so it cannot be an empty one");
            Assert.That(Codes(result), Has.Exactly(1).Items);
        });
    }

    /// <summary>
    /// Declared order is doc 40's table order. This is what the canonical writer emits,
    /// so it is not alphabetical and not any file's authored order.
    /// </summary>
    [Test]
    public void DeclaredOrderIsTheOrderOfDocFortysEnvelopeTable()
    {
        Assert.That(
            EnvelopeSchema.Order.Fields,
            Is.EqualTo(new[]
            {
                "id", "schema_version", "content_version", "status",
                "name_key", "summary_key", "tags", "source_refs", "presentation_id",
            }));
    }

    [Test]
    public void TheInitialVersionsAreOne()
    {
        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(1, EnvelopeSchema.InitialSchemaVersion, "initial schema_version");
            NumericAssert.AreExactlyEqual(1, EnvelopeSchema.InitialContentVersion, "initial content_version");
        });
    }

    [TestCase("development", DefinitionStatus.Development)]
    [TestCase("enabled", DefinitionStatus.Enabled)]
    [TestCase("disabled", DefinitionStatus.Disabled)]
    [TestCase("retired", DefinitionStatus.Retired)]
    public void EveryAcceptedStatusTokenRoundTrips(string token, DefinitionStatus status)
    {
        Expect.Multiple(() =>
        {
            Assert.That(DefinitionStatuses.TryParse(token, out DefinitionStatus parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(status));
            NumericAssert.AreExactlyEqual(token, DefinitionStatuses.ToToken(status), "the token");
        });
    }

    [TestCase("Enabled")]
    [TestCase("ENABLED")]
    [TestCase("active")]
    [TestCase("")]
    public void AStatusTokenIsExactAndCaseSensitive(string token)
    {
        Assert.That(DefinitionStatuses.TryParse(token, out _), Is.False);
    }

    [Test]
    public void ReleaseExclusionFollowsStatus()
    {
        Expect.Multiple(() =>
        {
            Assert.That(Read(Envelope("W-AB", status: "development")).Envelope!
                .IsExcludedFromReleaseByDefault, Is.True);
            Assert.That(Read(Envelope("W-AB", status: "disabled")).Envelope!
                .IsExcludedFromReleaseByDefault, Is.True);
            Assert.That(Read(Envelope("W-AB", status: "enabled")).Envelope!
                .IsExcludedFromReleaseByDefault, Is.False);
            Assert.That(Read(Envelope("W-AB", status: "retired")).Envelope!
                .IsExcludedFromReleaseByDefault, Is.False);
        });
    }

    /// <summary>
    /// The declared-optional fields materialize their documented default in the canonical
    /// payload, so runtime reads a value rather than guessing. Doc 40: "Optional fields
    /// have explicit defaults materialized into the canonical bundle so runtime never
    /// guesses."
    /// </summary>
    [Test]
    public void OmittedOptionalFieldsMaterializeTheirDefaultInCanonicalOutput()
    {
        DefinitionEnvelope envelope = Read(Envelope("W-AB")).Envelope!;
        string payload = Encoding.UTF8.GetString(envelope.ToCanonicalUtf8());

        Expect.Multiple(() =>
        {
            Assert.That(payload, Does.Contain("\"name_key\":\"\""));
            Assert.That(payload, Does.Contain("\"summary_key\":\"\""));
            Assert.That(
                payload,
                Does.Contain("\"presentation_id\":\"\""),
                "the required-absent row is emitted too, and the default is the only value "
                    + "it can ever have, so runtime reads a value here as well");
            Assert.That(
                payload,
                Does.Not.Contain("null"),
                "a canonical payload never contains null; absence became a materialized default");
        });
    }

    [Test]
    public void TheCanonicalPayloadEmitsAllNineFieldsInDeclaredOrder()
    {
        DefinitionEnvelope envelope = Read(Envelope("W-AB")).Envelope!;

        NumericAssert.AreExactlyEqual(
            "{\"id\":\"W-AB\",\"schema_version\":1,\"content_version\":1,\"status\":\"enabled\","
                + "\"name_key\":\"\",\"summary_key\":\"\",\"tags\":[],"
                + "\"source_refs\":[\"GDD-WEAPON-CATALOG#accepted-base-catalog-assignment\"],"
                + "\"presentation_id\":\"\"}",
            Encoding.UTF8.GetString(envelope.ToCanonicalUtf8()),
            "the canonical envelope payload");
    }

    /// <summary>
    /// The ID is checked against the category the authoring directory implies, not
    /// against whatever category the ID happens to look like. Inferring it would turn a
    /// mistyped ID into a definition of a different category.
    /// </summary>
    [Test]
    public void TheIdIsCheckedAgainstTheCategoryTheCallerDeclares()
    {
        EnvelopeReadResult result = Read(Envelope("EN-01"), ContentCategory.Weapon);

        Expect.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(ContentDiagnosticCodes.IdMalformedForCategory));
        });
    }

    [Test]
    public void ADiagnosticPointsAtTheExactFieldAtFault()
    {
        EnvelopeReadResult result = Read(Envelope("W-AB", status: "Enabled"));

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                "/status", result.Diagnostics[0].Location.Value, "the pointer of the faulty field");
            NumericAssert.AreExactlyEqual(
                "W-AB", result.Diagnostics[0].ContentId!, "the content ID is still reported");
        });
    }

    /// <summary>
    /// An envelope with the six required fields, and <c>presentation_id</c> spliced in
    /// verbatim as a JSON value when one is asked for.
    /// </summary>
    private static string Envelope(
        string id, string status = "enabled", string? presentationId = null)
    {
        string presentation = presentationId is null
            ? string.Empty
            : ",\"presentation_id\":" + presentationId;

        return "{\"id\":\"" + id + "\",\"schema_version\":1,\"content_version\":1,"
            + "\"status\":\"" + status + "\",\"tags\":[],"
            + "\"source_refs\":[\"GDD-WEAPON-CATALOG#accepted-base-catalog-assignment\"]"
            + presentation + "}";
    }

    private static IReadOnlyList<string> Codes(EnvelopeReadResult result)
    {
        List<string> codes = new();
        foreach (ContentDiagnostic diagnostic in result.Diagnostics)
        {
            codes.Add(diagnostic.Code);
        }

        return codes;
    }

    private static EnvelopeReadResult Read(
        string json, ContentCategory category = ContentCategory.Weapon)
    {
        return EnvelopeReader.Read(
            Encoding.UTF8.GetBytes(json),
            new EnvelopeReadContext("tests/fixture.json", category));
    }
}
