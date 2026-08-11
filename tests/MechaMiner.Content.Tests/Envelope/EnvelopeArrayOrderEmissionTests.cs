using System;
using System.Collections.Generic;
using System.Text;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Envelope;

/// <summary>
/// The envelope writer emits each of its two arrays under the class
/// <see cref="EnvelopeSchema.ArrayOrderOf"/> declares for it, and under no other.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema baseline
/// grants two array treatments. Which one a field gets is declared with the field; the writer
/// reads that declaration. Before this fixture existed the writer named an operation per field
/// directly, and the two statements had drifted into opposites - the declaration said
/// <c>source_refs</c> is an ID set and <c>tags</c> is authored-order, and the writer sorted
/// <c>tags</c> and preserved <c>source_refs</c>. Nothing was red, because both spellings emit
/// a well-formed array of strings: the disagreement was only ever visible in the bytes, and so
/// in the hash.
/// </para>
/// <para>
/// <b>The two halves here are deliberately different in kind.</b>
/// <see cref="TheDeclaredClassOfEachEnvelopeArrayIsThePinned"/> pins the two answers as
/// literals, so changing one is a visible edit to a test rather than a quiet change of
/// canonical bytes. <see cref="EachEnvelopeArrayIsEmittedUnderTheClassItDeclares"/> asserts
/// only <em>agreement</em>, computing what to expect from the declaration at run time, so it
/// cannot be satisfied by a writer that hardcodes the operation the declaration currently
/// happens to name. Together they are a pinned decision plus a proof the writer obeys it;
/// either alone permits the drift described above.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class EnvelopeArrayOrderEmissionTests
{
    /// <summary>
    /// Three well-formed <c>source_refs</c> elements whose authored order is not their ordinal
    /// order. The mismatch is asserted rather than assumed: a set-class permutation test over
    /// an already-sorted array cannot tell sorting from doing nothing.
    /// </summary>
    private static readonly string[] ThreeUnsortedRefs =
    {
        "GDD-WEAPON-CATALOG#accepted-base-catalog-assignment",
        "TDD-COMBAT-RUNTIME#weapon-actors",
        "DEC-120#decision",
    };

    [Test]
    public void TheDeclaredClassOfEachEnvelopeArrayIsThePinned()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                EnvelopeSchema.ArrayOrderOf(EnvelopeSchema.SourceRefs),
                Is.EqualTo(ArrayOrder.IdSet),
                "doc 40 § source_refs element grammar: \"source_refs is an array of stable-ID "
                    + "strings\", which is doc 40's set treatment verbatim");
            Assert.That(
                EnvelopeSchema.ArrayOrderOf(EnvelopeSchema.Tags),
                Is.EqualTo(ArrayOrder.OrderedArray),
                "doc 40 § tags vocabulary does not classify tags, and of the two treatments it "
                    + "grants, canonical order is granted to stable-ID sets; a vocabulary term "
                    + "is not a stable ID, so sorting tags would assert that it is");
        });
    }

    /// <summary>
    /// Whatever the declaration says, that is what the payload shows. Expected order is
    /// derived from <see cref="EnvelopeSchema.ArrayOrderOf"/> at run time, so this test tracks
    /// the declaration instead of restating it.
    /// </summary>
    [Test]
    public void EachEnvelopeArrayIsEmittedUnderTheClassItDeclares()
    {
        IReadOnlyList<string> emitted = EmittedSourceRefs(ThreeUnsortedRefs);

        List<string> expected = new(ThreeUnsortedRefs);
        if (EnvelopeSchema.ArrayOrderOf(EnvelopeSchema.SourceRefs) == ArrayOrder.IdSet)
        {
            expected.Sort(static (left, right) => string.CompareOrdinal(left, right));
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                ThreeUnsortedRefs,
                Has.Length.GreaterThanOrEqualTo(2),
                "an array with fewer than two elements has one order, so it cannot show which "
                    + "of the two treatments produced it");
            Assert.That(
                expected,
                Is.Not.EqualTo(ThreeUnsortedRefs),
                "the fixture's authored order must differ from the order the declared class "
                    + "produces, or agreement holds for both classes and this asserts nothing");
            Assert.That(
                emitted,
                Is.EqualTo(expected),
                () => "source_refs is declared "
                    + EnvelopeSchema.ArrayOrderOf(EnvelopeSchema.SourceRefs)
                    + " and must be emitted that way. Emitted: " + string.Join(", ", emitted));
        });
    }

    /// <summary>
    /// The set-class invariant at the envelope level: every ordering of a
    /// <c>source_refs</c> array produces byte-identical canonical output.
    /// </summary>
    /// <remarks>
    /// This is the one place in the tree where the set treatment is exercised over an array of
    /// more than one element on <em>every</em> category, because <c>source_refs</c> is on every
    /// definition and the sixteen category fixtures each author exactly one element. The
    /// per-category gate records that reach limit in its own census.
    /// </remarks>
    [Test]
    public void EveryOrderingOfASourceRefsSetProducesTheSameCanonicalBytes()
    {
        Assert.That(
            EnvelopeSchema.ArrayOrderOf(EnvelopeSchema.SourceRefs),
            Is.EqualTo(ArrayOrder.IdSet),
            "this invariant is the set treatment's; it does not hold, and must not be asserted, "
                + "for an authored-order array");

        IReadOnlyList<string[]> orderings = PermutationsOf(ThreeUnsortedRefs);
        byte[] expected = CanonicalBytes(ThreeUnsortedRefs);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                6,
                orderings.Count,
                "three distinct elements have six orderings; a smaller family would leave an "
                    + "ordering unasserted");

            foreach (string[] ordering in orderings)
            {
                Assert.That(
                    CanonicalBytes(ordering),
                    Is.EqualTo(expected),
                    () => "ordering [" + string.Join(", ", ordering)
                        + "] must canonicalize to the same bytes as the authored one");
            }
        });
    }

    /// <summary>
    /// <c>tags</c> is emitted, and the class it is emitted under is currently unobservable.
    /// </summary>
    /// <remarks>
    /// The tag vocabulary is empty - <see cref="TagVocabulary"/> grants no term - so the only
    /// value <c>tags</c> can carry is the empty array, and the empty array is its own sole
    /// ordering. Both treatments produce <c>[]</c>. Recorded here rather than left out, because
    /// a reader of the census should know that this row is a declaration nothing yet exercises;
    /// the first authored tag makes it observable.
    /// </remarks>
    [Test]
    public void TheTagsArrayIsEmittedAndItsClassIsNotYetObservable()
    {
        string payload = Encoding.UTF8.GetString(
            Read(EnvelopeJson(ThreeUnsortedRefs)).Envelope!.ToCanonicalUtf8());

        Expect.Multiple(() =>
        {
            Assert.That(payload, Does.Contain("\"tags\":[]"));
            Assert.That(
                TagVocabulary.Terms,
                Is.Empty,
                "if a term is ever granted, the tags row stops being unobservable and this "
                    + "fixture owes it a permutation");
        });
    }

    /// <summary>The <c>source_refs</c> array as the canonical payload holds it.</summary>
    private static IReadOnlyList<string> EmittedSourceRefs(IReadOnlyList<string> authored)
    {
        string payload = Encoding.UTF8.GetString(CanonicalBytes(authored));
        const string opening = "\"source_refs\":[";

        int start = payload.IndexOf(opening, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "the payload must carry a source_refs array");
        start += opening.Length;
        int end = payload.IndexOf(']', start);
        Assert.That(end, Is.GreaterThan(start), "the source_refs array must be closed and nonempty");

        List<string> emitted = new();
        foreach (string element in payload[start..end].Split(','))
        {
            emitted.Add(element.Trim('"'));
        }

        return emitted;
    }

    private static byte[] CanonicalBytes(IReadOnlyList<string> sourceRefs)
    {
        EnvelopeReadResult result = Read(EnvelopeJson(sourceRefs));
        Assert.That(
            result.IsValid,
            Is.True,
            () => "the constructed envelope must validate: "
                + string.Join("; ", result.Diagnostics));
        return result.Envelope!.ToCanonicalUtf8();
    }

    private static string EnvelopeJson(IReadOnlyList<string> sourceRefs)
    {
        List<string> quoted = new(sourceRefs.Count);
        foreach (string sourceRef in sourceRefs)
        {
            quoted.Add("\"" + sourceRef + "\"");
        }

        return "{\"id\":\"W-AB\",\"schema_version\":1,\"content_version\":1,"
            + "\"status\":\"enabled\",\"tags\":[],\"source_refs\":["
            + string.Join(",", quoted) + "]}";
    }

    private static EnvelopeReadResult Read(string json)
    {
        return EnvelopeReader.Read(
            Encoding.UTF8.GetBytes(json),
            new EnvelopeReadContext("tests/fixture.json", ContentCategory.Weapon));
    }

    /// <summary>Every ordering of <paramref name="values"/>, by insertion into each position.</summary>
    private static IReadOnlyList<string[]> PermutationsOf(IReadOnlyList<string> values)
    {
        List<string[]> orderings = new() { Array.Empty<string>() };
        foreach (string value in values)
        {
            List<string[]> grown = new();
            foreach (string[] ordering in orderings)
            {
                for (int position = 0; position <= ordering.Length; position++)
                {
                    List<string> next = new(ordering);
                    next.Insert(position, value);
                    grown.Add(next.ToArray());
                }
            }

            orderings = grown;
        }

        return orderings;
    }
}
