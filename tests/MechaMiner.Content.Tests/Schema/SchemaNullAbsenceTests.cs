using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// No schema under <c>content/schemas/</c> contains a JSON <c>null</c>, anywhere.
/// </summary>
/// <remarks>
/// <para>
/// <c>null</c> is the one value the codec rejects: absence is expressed by omitting a key
/// and letting the compiler materialize a documented default, so a <c>"default": null</c>
/// authors the rejected value in the document that defines what is accepted. The same
/// goes for a <c>null</c> in an <c>enum</c>, in a <c>const</c>, or in an example, and
/// <c>presentation_id</c> has no default to author at all.
/// </para>
/// <para>
/// <b>Why this is asserted here and not left to the whole-tree scan.</b> A separate check
/// asserts that no <c>null</c> appears anywhere under <c>content/</c>, enumerating every
/// file rather than only the definition directories, and its own documentation rules out
/// an exception list. So a schema with a null default does fail something - somebody
/// else's assertion, in somebody else's run, naming a file nobody thinks of as content,
/// with no waiver available. Asserting it in this suite means it fails in the change that
/// wrote it, next to the schema and next to the person who can fix it in one line.
/// </para>
/// <para>
/// Six of these were live: <c>"description": null</c> written where the field was meant
/// to be absent, in six of the sixteen category schemas. Nothing reported them, because
/// the loader read a non-string annotation as no annotation. That is the reason this scan
/// asks about the value and not about a list of keywords a <c>null</c> is likely to be
/// under - none of the six were under a keyword anybody would have listed.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-040</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SchemaNullAbsenceTests
{
    private static string SchemaDirectory =>
        Path.Combine(TestArtifacts.RepositoryRoot, "content", "schemas");

    /// <summary>
    /// Every <c>.json</c> under <c>content/schemas/</c>, recursively, and not only the
    /// <c>*.schema.json</c> the attribution gate globs.
    /// </summary>
    /// <remarks>
    /// The wider glob is deliberate. A file added as <c>weapon.json</c> is still a file
    /// under <c>content/</c> to the whole-tree scan, so exempting it here would leave
    /// exactly the gap this fixture exists to close, and
    /// <c>SchemaAuthorityCoverageTests.TheGlobMatchesEveryJsonFileUnderContentSchemas</c>
    /// already holds that no such file is there.
    /// </remarks>
    private static IReadOnlyList<KeyValuePair<string, byte[]>> TheCorpus()
    {
        List<KeyValuePair<string, byte[]>> documents = new();
        foreach (string path in
                 Directory.GetFiles(SchemaDirectory, "*.json", SearchOption.AllDirectories))
        {
            documents.Add(new KeyValuePair<string, byte[]>(
                TestArtifacts.Relative(path), File.ReadAllBytes(path)));
        }

        return documents;
    }

    [Test]
    public void NoSchemaUnderContentSchemasContainsAJsonNull()
    {
        SchemaNullScan.Result scan = SchemaNullScan.OfAll(TheCorpus());

        Expect.Multiple(() =>
        {
            Assert.That(
                scan.DocumentsSeen,
                Is.GreaterThan(0),
                "a scan over zero documents reports no nulls, which is the same sentence as "
                    + "a clean corpus and the fail-open this counter exists for");
            Assert.That(
                scan.NodesVisited,
                Is.GreaterThan(scan.DocumentsSeen),
                "the scan must have descended below each document's root, or it read nothing "
                    + "but the outermost value");
            Assert.That(
                scan.Nulls,
                Is.Empty,
                () => "these positions under content/schemas hold a JSON null: "
                    + string.Join(", ", scan.Nulls)
                    + ". Absence is written by omitting the key and letting the compiler "
                    + "materialize a documented default; null is the one value the codec "
                    + "rejects, and a schema authoring it authors the rejected value in the "
                    + "document that says what is accepted");
        });
    }

    /// <summary>
    /// The negative control: a schema with a null default is reported, by pointer.
    /// </summary>
    /// <remarks>
    /// The fixture carries two of the guarded thing where only one is a finding - a null
    /// default and a default of the string <c>"null"</c> - because a check that reported
    /// both would be a text search wearing a parser's clothes, and one that reported
    /// neither would pass over the corpus for the wrong reason.
    /// </remarks>
    [Test]
    public void ASchemaWithANullDefaultIsReportedByPointer()
    {
        SchemaNullScan.Result scan = SchemaNullScan.Of(File.ReadAllBytes(
            Path.Combine(FixtureCorpus.Root, "schema", "null-default.schema.json")));

        Expect.Multiple(() =>
        {
            Assert.That(
                scan.Nulls,
                Is.EqualTo(new[]
                {
                    "/properties/capacity/default",
                    "/properties/layers/examples/0/1",
                }),
                () => "both nulls must be reported, each at the pointer that addresses it, "
                    + "and the string \"null\" beside them must not be: "
                    + string.Join(", ", scan.Nulls));
            Assert.That(
                scan.DocumentsSeen,
                Is.EqualTo(1),
                "one document was read");
        });
    }

    /// <summary>
    /// The string <c>"null"</c> is a string, and the scan says so on its own.
    /// </summary>
    /// <remarks>
    /// Stated separately from the fixture control because it is the assertion that fails
    /// if the scan is ever reimplemented as a text search over the bytes, which is the
    /// cheap way to write this and the wrong one.
    /// </remarks>
    [Test]
    public void TheStringNullIsNotAJsonNull()
    {
        SchemaNullScan.Result scan = SchemaNullScan.Of(
            Encoding.UTF8.GetBytes("{\"const\":\"null\",\"enum\":[\"null\"],\"title\":\"null\"}"));

        Expect.Multiple(() =>
        {
            Assert.That(
                scan.Nulls,
                Is.Empty,
                () => "a four-character string is a legal value and may well be an enum "
                    + "token: " + string.Join(", ", scan.Nulls));
            Assert.That(
                scan.NodesVisited,
                Is.GreaterThan(1),
                "the scan did read the document; it simply found no null in it");
        });
    }

    /// <summary>
    /// A null at the document root is reported rather than skipped.
    /// </summary>
    /// <remarks>
    /// The root is the one position a walk written as "recurse into each member" never
    /// tests, and a scan that reported nothing for <c>null</c> would be a scan whose only
    /// clean answer is the one it cannot distinguish from an empty corpus.
    /// </remarks>
    [Test]
    public void ANullAtTheRootIsReported()
    {
        SchemaNullScan.Result scan = SchemaNullScan.Of(Encoding.UTF8.GetBytes("null"));

        Assert.That(
            scan.Nulls,
            Is.EqualTo(new[] { string.Empty }),
            () => "the root pointer is the empty string, and the root is a position: "
                + string.Join(", ", scan.Nulls));
    }

    /// <summary>
    /// A document with no null reports none while still counting what it read.
    /// </summary>
    /// <remarks>
    /// The positive control. Without it every assertion above is satisfied by a scan that
    /// reports nothing whatever it is given.
    /// </remarks>
    [Test]
    public void ANullFreeDocumentReportsNothingAndStillCountsItsNodes()
    {
        SchemaNullScan.Result scan = SchemaNullScan.Of(File.ReadAllBytes(
            Path.Combine(FixtureCorpus.Root, "schema", "no-bounds.schema.json")));

        Expect.Multiple(() =>
        {
            Assert.That(scan.Nulls, Is.Empty);
            Assert.That(
                scan.NodesVisited,
                Is.GreaterThan(1),
                "the scan descended into the document and found no null, which is a different "
                    + "outcome from having read nothing");
        });
    }

    /// <summary>
    /// The accumulating scan names the document each null is in.
    /// </summary>
    /// <remarks>
    /// A pointer alone is not an address across a corpus: <c>/properties/capacity/default</c>
    /// is a position in every one of seventeen files, and a finding a reader cannot open is
    /// a finding they will not act on.
    /// </remarks>
    [Test]
    public void AcrossDocumentsEachNullIsNamedWithItsDocument()
    {
        SchemaNullScan.Result scan = SchemaNullScan.OfAll(new[]
        {
            new KeyValuePair<string, byte[]>(
                "content/schemas/a.schema.json", Encoding.UTF8.GetBytes("{\"default\":null}")),
            new KeyValuePair<string, byte[]>(
                "content/schemas/b.schema.json", Encoding.UTF8.GetBytes("{\"default\":1}")),
        });

        Expect.Multiple(() =>
        {
            Assert.That(
                scan.Nulls,
                Is.EqualTo(new[] { "content/schemas/a.schema.json#/default" }),
                () => "the finding must name the document that holds it, and the clean "
                    + "document must not appear: " + string.Join(", ", scan.Nulls));
            Assert.That(scan.DocumentsSeen, Is.EqualTo(2));
        });
    }
}
