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
    [Test]
    public void TheEnvelopeHasExactlyNineFieldsSixRequiredAndThreeDeclaredOptional()
    {
        Expect.Multiple(() =>
        {
            Assert.That(EnvelopeSchema.Fields, Has.Count.EqualTo(9));
            Assert.That(EnvelopeSchema.Required, Has.Count.EqualTo(6));
            Assert.That(EnvelopeSchema.DeclaredOptional, Has.Count.EqualTo(3));
            Assert.That(
                EnvelopeSchema.DeclaredOptional,
                Is.EquivalentTo(new[] { "name_key", "summary_key", "presentation_id" }),
                "doc 40 § Declared-optional envelope fields names presentation_id and name_key, "
                    + "and says summary_key follows the same rule its row already states");
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
            Assert.That(payload, Does.Contain("\"presentation_id\":\"\""));
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

    private static string Envelope(string id, string status = "enabled")
    {
        return "{\"id\":\"" + id + "\",\"schema_version\":1,\"content_version\":1,"
            + "\"status\":\"" + status + "\",\"tags\":[],"
            + "\"source_refs\":[\"GDD-WEAPON-CATALOG#accepted-base-catalog-assignment\"]}";
    }

    private static EnvelopeReadResult Read(
        string json, ContentCategory category = ContentCategory.Weapon)
    {
        return EnvelopeReader.Read(
            Encoding.UTF8.GetBytes(json),
            new EnvelopeReadContext("tests/fixture.json", category));
    }
}
