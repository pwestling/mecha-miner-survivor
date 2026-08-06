using System;
using System.Collections.Generic;
using System.IO;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Schema;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// The attribution gate reaches every position in a schema document where a numeric
/// bound can sit, not only the positions its author thought of.
/// </summary>
/// <remarks>
/// <para>
/// A bound is not only found under <c>properties</c>. Draft 2020-12 lets one sit at the
/// root, in <c>$defs</c>, in any of the boolean applicators, in the array applicators, in
/// the object applicators, behind a <c>$ref</c>, and any number of those nested inside
/// each other. A gate that walks the routes someone enumerated by hand is not a gate on
/// the schema; it is a gate on the author's memory of the specification.
/// </para>
/// <para>
/// One fixture per position, each a bare bound with no <c>x-authority</c>, each asserted
/// to fail with a named diagnostic. The positions split in two:
/// </para>
/// <list type="bullet">
///   <item>
///     positions the evaluator implements, where the bound must be rejected for having
///     no authority (<see cref="ContentDiagnosticCodes.SchemaMalformed"/>); and
///   </item>
///   <item>
///     positions whose keyword the evaluator does not implement at all, where the whole
///     document is rejected for the unsupported keyword
///     (<see cref="ContentDiagnosticCodes.SchemaKeywordUnsupported"/>) before the bound
///     inside is ever considered. That is unreachable-by-refusal rather than
///     unreachable-by-oversight, and the difference is only worth anything if it is
///     proved: <see cref="ThePositionIsRefusedRatherThanSilentlyAccepted"/> asserts the
///     document fails, fails for that reason, and does not quietly load.
///   </item>
/// </list>
/// <para>
/// Verification: <c>VER-DAT-001-026</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class SchemaAuthorityReachTests
{
    /// <summary>
    /// Every position the evaluator implements, with the bound keyword the fixture hides
    /// there.
    /// </summary>
    private static IEnumerable<Position> ImplementedPositions
    {
        get
        {
            yield return new Position(
                "reach-root.schema.json", "maximum", "the root schema object itself");
            yield return new Position(
                "reach-defs.schema.json", "maximum", "a root $defs entry");
            yield return new Position(
                "reach-nested-defs.schema.json",
                "maximum",
                "a $defs declared on a subschema, which no $ref of this evaluator reaches");
            yield return new Position(
                "reach-ref.schema.json", "maximum", "a subschema reached through a $ref");
            yield return new Position("reach-all-of.schema.json", "maximum", "an allOf branch");
            yield return new Position("reach-any-of.schema.json", "maximum", "an anyOf branch");
            yield return new Position("reach-one-of.schema.json", "maximum", "a oneOf branch");
            yield return new Position("reach-not.schema.json", "maximum", "a not subschema");
            yield return new Position("reach-items.schema.json", "maximum", "an items subschema");
            yield return new Position(
                "reach-prefix-items.schema.json", "maximum", "a prefixItems element");
            yield return new Position(
                "reach-property-names.schema.json", "maxLength", "a propertyNames subschema");
            yield return new Position(
                "reach-additional-properties.schema.json",
                "maximum",
                "an additionalProperties subschema object");
            yield return new Position(
                "reach-deeply-nested.schema.json",
                "maximum",
                "properties -> allOf -> oneOf -> items -> properties -> not");
        }
    }

    /// <summary>
    /// Every position whose keyword this evaluator refuses outright, with the keyword
    /// that gets refused.
    /// </summary>
    /// <remarks>
    /// These are not exemptions. <c>JsonSchemaKeywords</c> makes an unimplemented keyword
    /// a load failure rather than a no-op, so a bound in one of these positions cannot
    /// reach an evaluated schema at all. The day one of them is implemented, the fixture
    /// stops failing with <see cref="ContentDiagnosticCodes.SchemaKeywordUnsupported"/>
    /// and this test says so, which is the point of keeping the fixtures.
    /// </remarks>
    private static IEnumerable<Position> RefusedPositions
    {
        get
        {
            yield return new Position("reach-if.schema.json", "if", "an if subschema");
            yield return new Position("reach-then.schema.json", "then", "a then subschema");
            yield return new Position("reach-else.schema.json", "else", "an else subschema");
            yield return new Position(
                "reach-contains.schema.json", "contains", "a contains subschema");
            yield return new Position(
                "reach-pattern-properties.schema.json",
                "patternProperties",
                "a patternProperties subschema");
            yield return new Position(
                "reach-dependent-schemas.schema.json",
                "dependentSchemas",
                "a dependentSchemas subschema");
        }
    }

    /// <summary>
    /// The loader's structure-aware walk descends into the position and rejects the bare
    /// bound there for the stated reason.
    /// </summary>
    [TestCaseSource(nameof(ImplementedPositions))]
    public void TheLoaderRejectsABareBoundInThisPosition(Position position)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(position.Bytes(), position.SourcePath);

        Expect.Multiple(() =>
        {
            Assert.That(
                load.IsValid,
                Is.False,
                () => "a bare bound in " + position.Where + " must be rejected, but "
                    + position.Fixture + " loaded cleanly");
            Assert.That(
                CodesOf(load),
                Does.Contain(ContentDiagnosticCodes.SchemaMalformed),
                () => "a bare bound in " + position.Where + " must fail for the stated reason: "
                    + Describe(load));
            Assert.That(
                ConstraintsOf(load),
                Has.Some.Contains(SchemaAuthority.Keyword),
                () => "the diagnostic must name " + SchemaAuthority.Keyword + ", not merely fail: "
                    + Describe(load));
        });
    }

    /// <summary>
    /// The structure-blind corpus walk finds the same bound, and names the position it
    /// found it in.
    /// </summary>
    /// <remarks>
    /// The two walks have different blind spots by construction, so agreeing on every
    /// position is worth asserting: the loader knows the applicator keywords and nothing
    /// else, and this one knows the JSON and nothing else.
    /// </remarks>
    [TestCaseSource(nameof(ImplementedPositions))]
    public void TheCorpusWalkAlsoFindsABareBoundInThisPosition(Position position)
    {
        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(position.Bytes());

        Expect.Multiple(() =>
        {
            Assert.That(
                walk.Unattributed,
                Is.Not.Empty,
                () => "the corpus walk must catch a bare bound in " + position.Where);
            Assert.That(
                walk.Unattributed,
                Has.Some.Contains(position.Keyword),
                () => "the reported pointer must name the bound: "
                    + string.Join(", ", walk.Unattributed));
            Assert.That(
                walk.BoundsSeen,
                Is.GreaterThan(0),
                "the walk must record that it passed a bound at all");
        });
    }

    /// <summary>
    /// A position built on a keyword the evaluator does not implement is refused, and
    /// refused for that reason rather than passing silently.
    /// </summary>
    [TestCaseSource(nameof(RefusedPositions))]
    public void ThePositionIsRefusedRatherThanSilentlyAccepted(Position position)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(position.Bytes(), position.SourcePath);

        Expect.Multiple(() =>
        {
            Assert.That(
                JsonSchemaKeywords.IsRecognised(position.Keyword),
                Is.False,
                position.Keyword + " is implemented now, so " + position.Fixture
                    + " belongs in ImplementedPositions and the loader must walk into it");
            Assert.That(
                load.IsValid,
                Is.False,
                () => position.Fixture + " must not load: a bound in " + position.Where
                    + " that nothing checks is the same fail-open as one nothing walks");
            Assert.That(
                CodesOf(load),
                Is.EquivalentTo(new[] { ContentDiagnosticCodes.SchemaKeywordUnsupported }),
                () => position.Where + " must be refused for the unsupported keyword and for "
                    + "nothing else: " + Describe(load));
            Assert.That(
                ConstraintsOf(load),
                Has.Some.Contains("'" + position.Keyword + "'"),
                () => "the refusal must name " + position.Keyword + ": " + Describe(load));
        });
    }

    /// <summary>
    /// The corpus walk reaches the refused positions even though the loader stops short
    /// of them, so the gate over <c>content/schemas/**</c> holds there today and would
    /// keep holding the day the keyword is implemented.
    /// </summary>
    [TestCaseSource(nameof(RefusedPositions))]
    public void TheCorpusWalkStillFindsTheBoundInARefusedPosition(Position position)
    {
        SchemaBoundWalk.Result walk = SchemaBoundWalk.Of(position.Bytes());

        Assert.That(
            walk.Unattributed,
            Is.Not.Empty,
            () => "the structure-blind walk must reach " + position.Where
                + " regardless of what the evaluator implements");
    }

    /// <summary>
    /// The set of keywords the evaluator recognises, stated here independently.
    /// </summary>
    /// <remarks>
    /// This is the tripwire that keeps the two lists above honest. A keyword added to
    /// <c>JsonSchemaKeywords</c> without a reach fixture would silently create a new
    /// position for a bound to hide in; failing here forces whoever adds it to say which
    /// list the new position belongs in.
    /// </remarks>
    [Test]
    public void TheRecognisedKeywordSetIsExactlyTheOneStatedHere()
    {
        string[] expected =
        {
            "$schema", "$id", "$ref", "$defs", "type", "required", "properties",
            "additionalProperties", "propertyNames", "enum", "const", "pattern",
            "minLength", "maxLength", "minimum", "maximum", "exclusiveMinimum",
            "exclusiveMaximum", "multipleOf", "items", "prefixItems", "minItems",
            "maxItems", "uniqueItems", "allOf", "anyOf", "oneOf", "not",
            "title", "description", "$comment", SchemaAuthority.Keyword,
        };

        List<string> actual = new(JsonSchemaKeywords.Assertions);
        actual.AddRange(JsonSchemaKeywords.Annotations);

        Assert.That(
            actual,
            Is.EquivalentTo(expected),
            "the evaluator's keyword set changed. A new subschema applicator is a new "
                + "position a bound can occupy, so it needs a reach fixture in this fixture "
                + "class; a new numeric keyword needs adding to SchemaAuthority.BoundKeywords()");
    }

    /// <summary>
    /// The draft 2020-12 keywords that carry a number but are <em>not</em> in
    /// <see cref="SchemaAuthority.BoundKeywords"/>, each asserted to be unimplemented.
    /// </summary>
    /// <remarks>
    /// This is the hole the nine-keyword list would otherwise have. <c>minProperties</c>,
    /// <c>maxProperties</c>, <c>minContains</c>, and <c>maxContains</c> are every bit as
    /// much "a number someone chose" as <c>maxItems</c> is, and they are absent from the
    /// bound list only because the evaluator refuses them outright. If one is ever
    /// implemented and this test is not updated, it becomes an unattributed number the
    /// gate has no opinion about. Failing here is how that gets noticed.
    /// </remarks>
    [TestCase("minProperties")]
    [TestCase("maxProperties")]
    [TestCase("minContains")]
    [TestCase("maxContains")]
    public void ANumericKeywordOutsideTheBoundListIsNotImplemented(string keyword)
    {
        Assert.That(
            JsonSchemaKeywords.IsRecognised(keyword),
            Is.False,
            keyword + " asserts a number, so implementing it means adding it to "
                + "SchemaAuthority.BoundKeywords() in the same change. Until then it must be "
                + "a load failure rather than a keyword the attribution gate ignores");
    }

    private static IReadOnlyList<string> CodesOf(JsonSchemaLoadResult load)
    {
        List<string> codes = new();
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (ContentDiagnostic diagnostic in load.Diagnostics)
        {
            if (seen.Add(diagnostic.Code))
            {
                codes.Add(diagnostic.Code);
            }
        }

        return codes;
    }

    private static IReadOnlyList<string> ConstraintsOf(JsonSchemaLoadResult load)
    {
        List<string> constraints = new();
        foreach (ContentDiagnostic diagnostic in load.Diagnostics)
        {
            constraints.Add(diagnostic.ExpectedConstraint);
        }

        return constraints;
    }

    private static string Describe(JsonSchemaLoadResult load)
    {
        return load.Diagnostics.Count == 0
            ? "(no diagnostics)"
            : string.Join("; ", load.Diagnostics);
    }

    /// <summary>One place inside a schema document where a bound can sit.</summary>
    internal sealed class Position
    {
        internal Position(string fixture, string keyword, string where)
        {
            Fixture = fixture;
            Keyword = keyword;
            Where = where;
        }

        /// <summary>The fixture file under <c>Fixtures/schema/</c>.</summary>
        internal string Fixture { get; }

        /// <summary>
        /// The bound keyword the fixture hides, or the applicator keyword the evaluator
        /// refuses.
        /// </summary>
        internal string Keyword { get; }

        /// <summary>The position, in prose, for the failure message.</summary>
        internal string Where { get; }

        /// <summary>The repository-relative path a diagnostic reports.</summary>
        internal string SourcePath => "tests/MechaMiner.Content.Tests/Fixtures/schema/" + Fixture;

        internal byte[] Bytes()
        {
            return File.ReadAllBytes(Path.Combine(FixtureCorpus.Root, "schema", Fixture));
        }

        public override string ToString()
        {
            return Where + " (" + Fixture + ")";
        }
    }
}
