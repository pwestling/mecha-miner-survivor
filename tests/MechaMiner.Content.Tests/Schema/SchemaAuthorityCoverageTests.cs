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
/// Verification: <c>VER-DAT-001-027</c>.
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
    /// "No unattributed bounds" and "no bounds" are the same sentence to an emptiness
    /// assertion, and only one of them means the gate ran.
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
                "the gate must have passed at least one numeric bound. If content/schemas "
                    + "genuinely declares no bound anywhere, the attribution gate is asserting "
                    + "nothing and this test is the only thing that would say so");
            Assert.That(
                walk.Unattributed,
                Is.Empty,
                () => "unattributed bounds: " + string.Join(", ", walk.Unattributed));
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
