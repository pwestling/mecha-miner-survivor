using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Schema;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// The structure-blind corpus walk still knows a map of subschemas from a subschema.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SchemaBoundWalk"/> is structure-blind on purpose, so that a bound has to
/// evade two walkers with different blind spots rather than one. Blind to
/// <em>applicators</em> is the intent; blind to the difference between a schema object
/// and a map keyed by author-chosen names is a defect, because
/// <b>every schema keyword is a legal property name</b>. A schema declaring properties
/// called <c>maximum</c> and <c>x-authority</c> presents the walk with an object that has
/// a bound keyword and an authority keyword side by side, and the walk counts a bound
/// that is not there and attributes it to an authority that is not there.
/// </para>
/// <para>
/// The counted phantom is the fail-open.
/// <c>SchemaAuthorityCoverageTests.TheWalkOverTheProjectCorpusVisitsDocumentsAndBounds</c>
/// asserts <c>BoundsSeen &gt; 0</c> precisely so that "no unattributed bounds" cannot be
/// satisfied by a corpus with no bounds in it - and property names alone were enough to
/// satisfy it. The unsuppressed case is the mirror image: a property called
/// <c>maximum</c> with no <c>x-authority</c> beside it is reported as an unattributed
/// bound at a pointer where no bound exists.
/// </para>
/// <para>
/// The loader's walk is structure-<em>aware</em> and was never affected: it reads
/// <c>properties</c> through <c>ReadSubschemaMap</c> and never interprets an author's
/// property name as a keyword. <see cref="TheLoaderWasNeverConfusedByAKeywordNamedProperty"/>
/// holds it to that.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-029</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SchemaBoundWalkStructureTests
{
    /// <summary>
    /// The keywords whose value is a map from author-chosen name to subschema, written
    /// out here rather than read from the walk.
    /// </summary>
    /// <remarks>
    /// <b>Changing this list is a deliberate change to the walk.</b> Dropping one
    /// re-opens the confusion for that keyword, and the confusion is silent by nature -
    /// it inflates a counter and invents an attribution rather than throwing. A keyword
    /// added to draft 2020-12 support whose value is a name-to-subschema map must be
    /// added to both places in the same change.
    /// </remarks>
    private static readonly string[] TheSubschemaMapKeywords =
    {
        "properties",
        "$defs",
        "patternProperties",
        "dependentSchemas",
    };

    /// <summary>
    /// Every keyword the evaluator recognises, paired with every map keyword, so that a
    /// property named after a keyword is proved harmless for the whole set rather than
    /// for the two names that happened to be reported.
    /// </summary>
    private static IEnumerable<TestCaseData> KeywordNamedProperties
    {
        get
        {
            foreach (string map in TheSubschemaMapKeywords)
            {
                foreach (string keyword in JsonSchemaKeywords.Assertions)
                {
                    yield return new TestCaseData(map, keyword).SetName(
                        "AKeywordNamedPropertyDoesNotConfuseTheWalk(" + map + "/" + keyword + ")");
                }

                foreach (string keyword in JsonSchemaKeywords.Annotations)
                {
                    yield return new TestCaseData(map, keyword).SetName(
                        "AKeywordNamedPropertyDoesNotConfuseTheWalk(" + map + "/" + keyword + ")");
                }
            }
        }
    }

    [Test]
    public void TheSubschemaMapKeywordListIsExactlyTheOneStatedHere()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                SchemaBoundWalk.SubschemaMapKeywords,
                Is.EquivalentTo(TheSubschemaMapKeywords),
                "the walk's list of name-to-subschema maps no longer matches the list stated "
                    + "here. Removing one silently restores the property-name confusion for "
                    + "that keyword; adding one to the walk without stating it here lets the "
                    + "two drift");
            Assert.That(
                SchemaBoundWalk.SubschemaMapKeywords,
                Is.Unique,
                "a keyword listed twice would be handled twice and prove nothing extra");
        });
    }

    /// <summary>
    /// <c>patternProperties</c> and <c>dependentSchemas</c> are in the list even though
    /// the evaluator refuses them.
    /// </summary>
    /// <remarks>
    /// The loader stops at an unimplemented keyword; this walk does not, and
    /// <c>SchemaAuthorityReachTests.TheCorpusWalkStillFindsTheBoundInARefusedPosition</c>
    /// depends on it not stopping. A position the walk reaches is a position it can be
    /// confused in, so refused-by-the-loader is not a reason to leave one out.
    /// </remarks>
    [TestCase("patternProperties")]
    [TestCase("dependentSchemas")]
    public void ARefusedMapKeywordIsStillHandledByTheWalk(string keyword)
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                JsonSchemaKeywords.IsRecognised(keyword),
                Is.False,
                keyword + " is implemented now, so this test's premise has changed and the "
                    + "reach fixtures for it must move to SchemaAuthorityReachTests' "
                    + "implemented positions");
            Assert.That(
                SchemaBoundWalk.SubschemaMapKeywords,
                Does.Contain(keyword),
                "the structure-blind walk reaches " + keyword + " whether or not the loader "
                    + "does, so it must read it as a map of subschemas there too");
        });
    }

    /// <summary>
    /// A property named after a schema keyword changes nothing the walk reports.
    /// </summary>
    /// <remarks>
    /// Two shapes per keyword. With an <c>x-authority</c>-named sibling the phantom bound
    /// is suppressed, so only the inflated <c>BoundsSeen</c> gives it away - which is
    /// exactly the counter that exists to prove the gate ran. Without one, the phantom is
    /// reported as an unattributed bound at a pointer that addresses no bound.
    /// </remarks>
    [TestCaseSource(nameof(KeywordNamedProperties))]
    public void AKeywordNamedPropertyDoesNotConfuseTheWalk(string map, string propertyName)
    {
        string realBound = "/" + map + "/capacity/maximum";

        SchemaBoundWalk.Result suppressed = SchemaBoundWalk.Of(
            Document(map, propertyName, withAuthorityNamedSibling: true));
        SchemaBoundWalk.Result reported = SchemaBoundWalk.Of(
            Document(map, propertyName, withAuthorityNamedSibling: false));

        Expect.Multiple(() =>
        {
            Assert.That(
                suppressed.Unattributed,
                Is.EquivalentTo(new[] { realBound }),
                () => "a property named '" + propertyName + "' under " + map + ", beside one "
                    + "named 'x-authority', must leave the real unattributed bound the only "
                    + "finding: " + string.Join(", ", suppressed.Unattributed));
            NumericAssert.AreExactlyEqual(
                1,
                suppressed.BoundsSeen,
                "the walk must count the one real bound and no phantom read out of the "
                    + "property names; BoundsSeen is what proves the gate looked at anything");
            Assert.That(
                suppressed.MissingDerivations,
                Is.Empty,
                () => "a property named 'x-authority' is a property name, not an authority: "
                    + string.Join(", ", suppressed.MissingDerivations));

            Assert.That(
                reported.Unattributed,
                Is.EquivalentTo(new[] { realBound }),
                () => "a property named '" + propertyName + "' under " + map + " must not be "
                    + "reported as a bound: " + string.Join(", ", reported.Unattributed));
            NumericAssert.AreExactlyEqual(
                1,
                reported.BoundsSeen,
                "the same count, with no authority-named sibling to suppress the phantom");
        });
    }

    /// <summary>
    /// The committed negative control: several keyword-named properties at once, and a
    /// real unattributed bound that must still be reported.
    /// </summary>
    [Test]
    public void TheKeywordNamedPropertyFixtureReportsOnlyItsRealBound()
    {
        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(FixtureBytes());

        Expect.Multiple(() =>
        {
            Assert.That(
                walk.Unattributed,
                Is.EquivalentTo(new[] { "/properties/capacity/maximum" }),
                () => "the only bound in keyword-named-properties.schema.json is capacity's: "
                    + string.Join(", ", walk.Unattributed));
            NumericAssert.AreExactlyEqual(
                1,
                walk.BoundsSeen,
                "five of the six declared properties are named after schema keywords and "
                    + "none of them is a bound");
            Assert.That(walk.MissingDerivations, Is.Empty);
        });
    }

    /// <summary>
    /// The loader reaches the same conclusion, on the same file, having never had the
    /// confusion in the first place.
    /// </summary>
    [Test]
    public void TheLoaderWasNeverConfusedByAKeywordNamedProperty()
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            FixtureBytes(),
            "tests/MechaMiner.Content.Tests/Fixtures/schema/keyword-named-properties.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(
                load.IsValid,
                Is.False,
                () => "capacity's maximum carries no x-authority: "
                    + string.Join("; ", load.Diagnostics));
            NumericAssert.AreExactlyEqual(
                1,
                load.Diagnostics.Count,
                "one bound is unattributed, and the keyword-named properties are not bounds");
            Assert.That(
                load.Diagnostics[0].Code,
                Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
            Assert.That(
                load.Diagnostics[0].ExpectedConstraint,
                Does.Contain(SchemaAuthority.Keyword));
        });
    }

    private static byte[] FixtureBytes()
    {
        return File.ReadAllBytes(
            Path.Combine(FixtureCorpus.Root, "schema", "keyword-named-properties.schema.json"));
    }

    /// <summary>
    /// A document whose <paramref name="map"/> declares a member named
    /// <paramref name="propertyName"/>, optionally one named <c>x-authority</c>, and one
    /// real unattributed bound under <c>capacity</c>.
    /// </summary>
    private static byte[] Document(string map, string propertyName, bool withAuthorityNamedSibling)
    {
        StringBuilder text = new();
        text.Append("{\"type\":\"object\",\"").Append(map).Append("\":{");
        text.Append('"').Append(propertyName).Append("\":{\"type\":\"string\"}");

        if (withAuthorityNamedSibling
            && !string.Equals(propertyName, SchemaAuthority.Keyword, System.StringComparison.Ordinal))
        {
            text.Append(",\"").Append(SchemaAuthority.Keyword).Append("\":{\"type\":\"string\"}");
        }

        text.Append(",\"capacity\":{\"type\":\"integer\",\"maximum\":2048}}}");
        return Encoding.UTF8.GetBytes(text.ToString());
    }
}
