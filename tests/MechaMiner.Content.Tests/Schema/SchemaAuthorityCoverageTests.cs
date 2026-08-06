using System.Collections.Generic;
using System.IO;
using MechaMiner.Content.Schema;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// What the attribution gate covers, asserted independently of the gate.
/// </summary>
/// <remarks>
/// <para>
/// Two failure modes are closed here, and neither is caught by the gate itself.
/// </para>
/// <para>
/// <b>Deletion.</b> The negative controls in <see cref="SchemaAuthorityTests"/> are
/// parameterised off <see cref="SchemaAuthority.BoundKeywords"/>, so a tenth keyword
/// arrives with its control already written. The reverse move is the dangerous one:
/// removing a keyword from that list removes the rule <em>and</em> every test case that
/// proved the rule, and the suite goes green with one fewer keyword covered and nothing
/// naming what left. An exemption written as a deletion leaves no exemption list.
/// <see cref="TheBoundKeywordListIsExactlyTheNineStatedHere"/> states the nine here, so a
/// deletion breaks a test that names the keyword deleted.
/// </para>
/// <para>
/// <b>The empty set.</b> Every assertion in the gate is of the form "the list of
/// violations is empty", and that is equally true of a corpus with no documents, a glob
/// that matches nothing, and a schema with no bounds. A gate that reports success
/// without having looked at anything is the most comfortable kind of broken.
/// </para>
/// <para>
/// <b>The aggregate.</b> A count summed over the corpus is satisfied by one document. One
/// schema with a bound answered for every schema, so a document could lose every bound it
/// had and the total would stay positive. That count is now taken per document, against
/// <see cref="DocumentsDeclaredBoundFree"/>.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-027</c>, <c>VER-DAT-001-030</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SchemaAuthorityCoverageTests
{
    private static string SchemaDirectory =>
        Path.Combine(TestArtifacts.RepositoryRoot, "content", "schemas");

    /// <summary>
    /// The nine bound keywords, written out here rather than read from the rule.
    /// </summary>
    /// <remarks>
    /// <b>Changing this list is a deliberate change to the rule, not a test fix.</b> No
    /// keyword may be removed without a recorded reason: state it in the XML
    /// documentation on <see cref="SchemaAuthority.BoundKeywords"/>, where the next
    /// reader will find it, and only then delete it from both places. A keyword silently
    /// dropped from the rule is an exemption nobody voted for and nobody can enumerate.
    /// </remarks>
    private static readonly string[] TheNineBoundKeywords =
    {
        "minimum",
        "maximum",
        "exclusiveMinimum",
        "exclusiveMaximum",
        "minItems",
        "maxItems",
        "minLength",
        "maxLength",
        "multipleOf",
    };

    [Test]
    public void TheBoundKeywordListIsExactlyTheNineStatedHere()
    {
        IReadOnlyList<string> declared = SchemaAuthority.BoundKeywords();

        Expect.Multiple(() =>
        {
            foreach (string keyword in TheNineBoundKeywords)
            {
                Assert.That(
                    declared,
                    Does.Contain(keyword),
                    "'" + keyword + "' was removed from SchemaAuthority.BoundKeywords(). That "
                        + "removes the rule and every negative control for it in one edit, and "
                        + "leaves no exemption list for a reviewer to find. If the removal is "
                        + "deliberate, record why on BoundKeywords and delete it here too");
            }

            Assert.That(
                declared,
                Is.EquivalentTo(TheNineBoundKeywords),
                "SchemaAuthority.BoundKeywords() no longer matches the list stated in this "
                    + "test. A keyword added to the rule must be added here as well, so that "
                    + "the rule and its independent statement cannot drift apart");

            Assert.That(
                declared,
                Is.Unique,
                "a keyword listed twice would run its control twice and prove nothing extra");
        });
    }

    /// <summary>
    /// Every bound keyword is a keyword the evaluator actually implements.
    /// </summary>
    /// <remarks>
    /// A bound keyword the loader refuses would make its negative control pass for the
    /// wrong reason: the document would fail on the unsupported keyword rather than on
    /// the missing attribution, and the attribution rule would be untested for it.
    /// </remarks>
    [TestCaseSource(typeof(SchemaAuthority), nameof(SchemaAuthority.BoundKeywords))]
    public void EveryBoundKeywordIsOneTheEvaluatorImplements(string keyword)
    {
        Assert.That(
            JsonSchemaKeywords.IsRecognised(keyword),
            Is.True,
            keyword + " is required to carry an authority but is not implemented, so its "
                + "negative control would pass on the wrong diagnostic");
    }

    /// <summary>
    /// The corpus the gate walks is not empty.
    /// </summary>
    [Test]
    public void TheProjectSchemaCorpusIsNotEmpty()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                Directory.Exists(SchemaDirectory),
                Is.True,
                "content/schemas must exist; a gate over a directory that is not there "
                    + "reports success having read nothing");
            Assert.That(
                Glob(),
                Is.Not.Empty,
                "content/schemas/**/*.schema.json matched no file. Every assertion in the "
                    + "attribution gate is 'the violations are empty', which is exactly what "
                    + "an empty corpus produces");
        });
    }

    /// <summary>
    /// The glob the gate uses matches every schema document that is actually there.
    /// </summary>
    /// <remarks>
    /// <c>*.schema.json</c> is a naming convention, and a convention is only a gate while
    /// something enforces it. A file added as <c>weapon.json</c> would be walked by
    /// nothing and reported by nothing, which is the same fail-open as an empty
    /// directory, only harder to notice.
    /// </remarks>
    [Test]
    public void TheGlobMatchesEveryJsonFileUnderContentSchemas()
    {
        HashSet<string> matched = new(Glob(), System.StringComparer.Ordinal);
        List<string> skipped = new();

        foreach (string path in Directory.GetFiles(SchemaDirectory, "*.json", SearchOption.AllDirectories))
        {
            if (!matched.Contains(path))
            {
                skipped.Add(TestArtifacts.Relative(path));
            }
        }

        Assert.That(
            skipped,
            Is.Empty,
            () => "these JSON files under content/schemas are invisible to the attribution "
                + "gate's *.schema.json glob: " + string.Join(", ", skipped));
    }

    /// <summary>
    /// The walk over the project corpus visited a nonzero number of documents and a
    /// nonzero number of bounds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "No unattributed bounds" and "no bounds" are the same sentence to an emptiness
    /// assertion, and only one of them means the gate ran.
    /// </para>
    /// <para>
    /// The bound count here is the aggregate, and it is kept only because it is free: it
    /// is satisfied by any one document in the corpus. The assertion that actually holds
    /// the corpus is per document, in
    /// <see cref="EverySchemaDocumentCarriesABoundOrIsDeclaredBoundFree"/>.
    /// </para>
    /// </remarks>
    [Test]
    public void TheWalkOverTheProjectCorpusVisitsDocumentsAndBounds()
    {
        SchemaBoundWalk.Result walk = WalkTheCorpus();

        Expect.Multiple(() =>
        {
            Assert.That(
                walk.DocumentsSeen,
                Is.GreaterThan(0),
                "the gate must have walked at least one schema document");
            Assert.That(
                walk.ObjectsSeen,
                Is.GreaterThan(0),
                "the gate must have descended into at least one schema object");
            Assert.That(
                walk.BoundsSeen,
                Is.GreaterThan(0),
                "the gate must have passed at least one numeric bound. This is the aggregate, "
                    + "so it is satisfied by any single document; "
                    + nameof(EverySchemaDocumentCarriesABoundOrIsDeclaredBoundFree)
                    + " is the assertion that answers for each of them");
            Assert.That(
                walk.Unattributed,
                Is.Empty,
                () => "unattributed bounds: " + string.Join(", ", walk.Unattributed));
            Assert.That(
                walk.MissingDerivations,
                Is.Empty,
                () => "sourced or derived bounds with no derivation: "
                    + string.Join(", ", walk.MissingDerivations));
            Assert.That(
                walk.MissingRationales,
                Is.Empty,
                () => "structural bounds with no rationale of their own: "
                    + string.Join(", ", walk.MissingRationales));
        });
    }

    /// <summary>
    /// Every document under <c>content/schemas/</c> that declares no numeric bound, named
    /// one by one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is a list of file names. It is not a pattern, a prefix, or a predicate.</b>
    /// A schema that loses every bound it had must fail until somebody writes its name
    /// here, because writing a name here is a line in a diff that a reviewer can argue
    /// with. A rule that matched names instead would grant the same waiver to files nobody
    /// has read yet.
    /// </para>
    /// <para>
    /// <b>Why an exemption list is right here, when the attribution gate deliberately has
    /// none.</b> This reads as a contradiction of
    /// <see cref="TheBoundKeywordListIsExactlyTheNineStatedHere"/>, which exists to say
    /// that the <c>x-authority</c> rule has no exemptions and that removing a keyword from
    /// it is not an exemption but a deletion. The two are exemptions over different things.
    /// An exemption there would be over <em>keyword kinds</em>: it would say "bounds of
    /// this sort need no authority", which is a claim about the rule, and it widens the
    /// rule's blind spot everywhere at once, in every document, including the ones written
    /// after it. Nobody can check such a claim by reading, because what it exempts has not
    /// been written yet. An exemption here is over <em>documents</em>: it says "this file,
    /// today, declares no bound", which is a claim about the corpus and is settled by
    /// opening the file. Its blind spot is exactly one file wide, and
    /// <see cref="AnExemptionForADocumentThatHasBoundsIsReported"/> and
    /// <see cref="AnExemptionForADocumentNotInTheCorpusIsReported"/> shut it again the
    /// moment the claim stops being true.
    /// </para>
    /// <para>
    /// <b>The list makes the assertion stronger, not weaker.</b> What it replaced was
    /// <c>BoundsSeen &gt; 0</c> over the aggregate corpus, which says only "some schema
    /// somewhere had a bound" - one document vouching for all of them. What stands here
    /// says "every schema either has a bound or is declared not to", which is a statement
    /// about each document by name. The stronger assertion cannot be written at all
    /// without somewhere to put the documents that are legitimately bound-free; the list
    /// is not the exception to the gate, it is the thing that lets the gate exist.
    /// </para>
    /// </remarks>
    private static readonly string[] DocumentsDeclaredBoundFree = [];

    /// <summary>
    /// Every schema document in the project corpus either carries a numeric bound or is
    /// named in <see cref="DocumentsDeclaredBoundFree"/>.
    /// </summary>
    /// <remarks>
    /// The list is checked in both directions at once: an entry naming no document, and an
    /// entry naming a document that does have bounds, both fail here. An exemption is a
    /// claim about the corpus, and a claim that has quietly stopped being true is the one
    /// thing worse than no claim, because it reads as though somebody checked.
    /// </remarks>
    [Test]
    public void EverySchemaDocumentCarriesABoundOrIsDeclaredBoundFree()
    {
        SchemaBoundCoverage.Result coverage =
            SchemaBoundCoverage.Of(TheCorpusDocuments(), DocumentsDeclaredBoundFree);

        Expect.Multiple(() =>
        {
            Assert.That(
                coverage.DocumentsChecked,
                Is.GreaterThan(0),
                "a per-document check over zero documents passes every one of its "
                    + "assertions, which is the fail-open this whole fixture exists for");
            Assert.That(
                coverage.UndeclaredBoundFree,
                Is.Empty,
                () => "these schema documents under content/schemas declare no numeric bound "
                    + "and are named by no exemption: "
                    + string.Join(", ", coverage.UndeclaredBoundFree)
                    + ". Either the document lost its bounds, which is the accident this "
                    + "gate exists to catch, or it genuinely has none and its file name "
                    + "belongs in " + nameof(DocumentsDeclaredBoundFree)
                    + " as a deliberate, reviewable line");
            Assert.That(
                coverage.StaleExemptions,
                Is.Empty,
                () => "these names in " + nameof(DocumentsDeclaredBoundFree)
                    + " match no document under content/schemas: "
                    + string.Join(", ", coverage.StaleExemptions)
                    + ". An exemption for a file that is not there exempts nothing today and "
                    + "silently exempts whatever file takes that name tomorrow");
            Assert.That(
                coverage.UnnecessaryExemptions,
                Is.Empty,
                () => "these names in " + nameof(DocumentsDeclaredBoundFree)
                    + " are declared bound-free but their documents do carry bounds: "
                    + string.Join(", ", coverage.UnnecessaryExemptions)
                    + ". The list is a factual claim about the corpus; an exemption nobody "
                    + "needs is a false one, and it would absorb that document losing every "
                    + "bound it has");
        });
    }

    /// <summary>
    /// The positive control: a corpus of one bounded and one bound-free document, with the
    /// bound-free one declared, is clean on all three findings.
    /// </summary>
    /// <remarks>
    /// Without this the three negative controls below would be satisfied by a check that
    /// simply reports everything.
    /// </remarks>
    [Test]
    public void ADeclaredBoundFreeDocumentIsAccepted()
    {
        SchemaBoundCoverage.Result coverage = SchemaBoundCoverage.Of(
            TheTwoDocumentFixtureCorpus(),
            new[] { "no-bounds.schema.json" });

        Expect.Multiple(() =>
        {
            Assert.That(coverage.DocumentsChecked, Is.EqualTo(2));
            Assert.That(
                coverage.UndeclaredBoundFree,
                Is.Empty,
                () => "the bound-free document is declared, and the other has bounds: "
                    + string.Join(", ", coverage.UndeclaredBoundFree));
            Assert.That(
                coverage.StaleExemptions,
                Is.Empty,
                () => "the exempted name is in the corpus: "
                    + string.Join(", ", coverage.StaleExemptions));
            Assert.That(
                coverage.UnnecessaryExemptions,
                Is.Empty,
                () => "the exempted document really does declare no bound: "
                    + string.Join(", ", coverage.UnnecessaryExemptions));
        });
    }

    /// <summary>
    /// The negative control the gate exists for: a bound-free document nobody declared is
    /// reported, by name.
    /// </summary>
    /// <remarks>
    /// This is the case the aggregate count could not see. Both documents together report
    /// a positive total, so a corpus-wide <c>BoundsSeen &gt; 0</c> passes on exactly this
    /// input, and the assertion here has to name the offending file rather than merely
    /// fail.
    /// </remarks>
    [Test]
    public void AnUndeclaredBoundFreeDocumentIsReportedByName()
    {
        IReadOnlyList<SchemaBoundCoverage.Document> corpus = TheTwoDocumentFixtureCorpus();

        SchemaBoundCoverage.Result coverage =
            SchemaBoundCoverage.Of(corpus, System.Array.Empty<string>());

        Expect.Multiple(() =>
        {
            Assert.That(
                coverage.UndeclaredBoundFree,
                Is.EqualTo(new[] { "no-bounds.schema.json" }),
                () => "the bound-free document must be reported by name, and it must be the "
                    + "only one reported: " + string.Join(", ", coverage.UndeclaredBoundFree));
            Assert.That(
                Aggregate(corpus).BoundsSeen,
                Is.GreaterThan(0),
                "the aggregate count is positive on this very corpus, which is why it cannot "
                    + "be the gate");
        });
    }

    /// <summary>
    /// A name on the exemption list matching no document in the corpus is reported.
    /// </summary>
    /// <remarks>
    /// A stale exemption is an exemption for nothing, and it does not stay harmless: it is
    /// a waiver already granted to whichever file is next given that name, decided by
    /// somebody who never saw it.
    /// </remarks>
    [Test]
    public void AnExemptionForADocumentNotInTheCorpusIsReported()
    {
        SchemaBoundCoverage.Result coverage = SchemaBoundCoverage.Of(
            TheTwoDocumentFixtureCorpus(),
            new[] { "no-bounds.schema.json", "deleted-long-ago.schema.json" });

        Expect.Multiple(() =>
        {
            Assert.That(
                coverage.StaleExemptions,
                Is.EqualTo(new[] { "deleted-long-ago.schema.json" }),
                () => "the exemption naming no document must be reported, and the live one "
                    + "must not be: " + string.Join(", ", coverage.StaleExemptions));
            Assert.That(
                coverage.UndeclaredBoundFree,
                Is.Empty,
                () => "a stale exemption must not be reported as a second, unrelated finding: "
                    + string.Join(", ", coverage.UndeclaredBoundFree));
        });
    }

    /// <summary>
    /// A name on the exemption list whose document does carry bounds is reported.
    /// </summary>
    /// <remarks>
    /// An exemption that is not needed is a false statement about the corpus, and it is
    /// pre-positioned to hide the exact accident the gate is for: the day that document
    /// loses every bound it has, the waiver is already in place and nothing says so.
    /// </remarks>
    [Test]
    public void AnExemptionForADocumentThatHasBoundsIsReported()
    {
        SchemaBoundCoverage.Result coverage = SchemaBoundCoverage.Of(
            TheTwoDocumentFixtureCorpus(),
            new[] { "no-bounds.schema.json", "attributed-bound.schema.json" });

        Expect.Multiple(() =>
        {
            Assert.That(
                coverage.UnnecessaryExemptions,
                Is.EqualTo(new[] { "attributed-bound.schema.json" }),
                () => "the exemption for a bounded document must be reported, and the needed "
                    + "one must not be: " + string.Join(", ", coverage.UnnecessaryExemptions));
            Assert.That(
                coverage.StaleExemptions,
                Is.Empty,
                () => "both names are in the corpus, so neither is stale: "
                    + string.Join(", ", coverage.StaleExemptions));
        });
    }

    /// <summary>
    /// The negative control for the counts: they are counts, not constants.
    /// </summary>
    /// <remarks>
    /// A schema with no bound produces an empty violation list - the gate's success
    /// condition - while reporting zero bounds seen. That is the case the assertion above
    /// exists to fail on, so it has to be demonstrated rather than assumed.
    /// </remarks>
    [Test]
    public void ABoundFreeSchemaPassesTheViolationCheckAndReportsZeroBounds()
    {
        byte[] bytes = File.ReadAllBytes(
            Path.Combine(FixtureCorpus.Root, "schema", "no-bounds.schema.json"));

        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(bytes);
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(bytes, "no-bounds.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(
                walk.Unattributed,
                Is.Empty,
                "a bound-free schema has no violations, which is why 'no violations' alone "
                    + "cannot be the gate's success condition");
            Assert.That(
                walk.BoundsSeen,
                Is.Zero,
                "the bound counter must report zero here, or it is not counting anything");
            Assert.That(
                walk.ObjectsSeen,
                Is.GreaterThan(0),
                "the walk did descend into the document; it simply found no bound");
            Assert.That(load.IsValid, Is.True, () => string.Join("; ", load.Diagnostics));
        });
    }

    /// <summary>
    /// The negative control for the corpus count: an empty corpus reports zero documents
    /// and zero violations together.
    /// </summary>
    [Test]
    public void AnEmptyCorpusReportsZeroDocumentsRatherThanSuccess()
    {
        SchemaBoundWalk.Result walk = SchemaBoundWalk.OfAll(new List<byte[]>());

        Expect.Multiple(() =>
        {
            Assert.That(
                walk.Unattributed,
                Is.Empty,
                "an empty corpus produces an empty violation list, indistinguishable from a "
                    + "clean one unless the document count is asserted");
            Assert.That(walk.DocumentsSeen, Is.Zero);
            Assert.That(walk.BoundsSeen, Is.Zero);
        });
    }

    /// <summary>
    /// Every document the gate walks, named as <see cref="DocumentsDeclaredBoundFree"/>
    /// names them: by file name, so that an exemption reads as the file a reviewer would
    /// open.
    /// </summary>
    private static IReadOnlyList<SchemaBoundCoverage.Document> TheCorpusDocuments()
    {
        List<SchemaBoundCoverage.Document> documents = new();
        foreach (string path in Glob())
        {
            documents.Add(new SchemaBoundCoverage.Document(
                Path.GetFileName(path), File.ReadAllBytes(path)));
        }

        return documents;
    }

    /// <summary>
    /// The corpus the three exemption controls run against: one document with a bound and
    /// one with none, so every finding has a document that must be reported and a document
    /// that must not.
    /// </summary>
    private static IReadOnlyList<SchemaBoundCoverage.Document> TheTwoDocumentFixtureCorpus()
    {
        return new[]
        {
            FixtureDocument("attributed-bound.schema.json"),
            FixtureDocument("no-bounds.schema.json"),
        };
    }

    private static SchemaBoundCoverage.Document FixtureDocument(string name)
    {
        return new SchemaBoundCoverage.Document(
            name, File.ReadAllBytes(Path.Combine(FixtureCorpus.Root, "schema", name)));
    }

    private static SchemaBoundWalk.Result Aggregate(
        IEnumerable<SchemaBoundCoverage.Document> documents)
    {
        List<byte[]> bytes = new();
        foreach (SchemaBoundCoverage.Document document in documents)
        {
            bytes.Add(document.Bytes);
        }

        return SchemaBoundWalk.OfAll(bytes);
    }

    private static SchemaBoundWalk.Result WalkTheCorpus()
    {
        List<byte[]> documents = new();
        foreach (string path in Glob())
        {
            documents.Add(File.ReadAllBytes(path));
        }

        return SchemaBoundWalk.OfAll(documents);
    }

    private static string[] Glob()
    {
        return Directory.GetFiles(SchemaDirectory, "*.schema.json", SearchOption.AllDirectories);
    }
}
