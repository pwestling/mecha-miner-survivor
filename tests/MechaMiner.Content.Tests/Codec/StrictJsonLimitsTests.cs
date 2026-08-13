using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MechaMiner.Content.Codec;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Codec;

/// <summary>
/// The size, depth, and count ceilings, tested at the boundary.
/// </summary>
/// <remarks>
/// <para>
/// Each case builds a document that sits exactly <em>on</em> the limit and one that sits
/// exactly one past it. Testing only the failing side would pass on an off-by-one that
/// rejected legal content, which is the more damaging direction: an author cannot work
/// around a ceiling that is secretly one lower than documented.
/// </para>
/// <para>
/// Documents are built in code rather than committed, because a fixture proving the
/// shipped one-megabyte ceiling would be a megabyte of generated JSON. The shipped
/// defaults are asserted separately below.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-007</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class StrictJsonLimitsTests
{
    [Test]
    public void TheShippedDefaultsAreTheDocumentedValues()
    {
        StrictJsonLimits limits = StrictJsonLimits.Default;

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(1_048_576, limits.MaximumDocumentBytes, "document bytes");
            NumericAssert.AreExactlyEqual(32, limits.MaximumDepth, "depth");
            NumericAssert.AreExactlyEqual(256, limits.MaximumObjectProperties, "object properties");
            NumericAssert.AreExactlyEqual(1_024, limits.MaximumArrayElements, "array elements");
            NumericAssert.AreExactlyEqual(32_768, limits.MaximumNodeCount, "node count");
            NumericAssert.AreExactlyEqual(4_096, limits.MaximumStringLength, "string length");
        });
    }

    [Test]
    public void EveryDefaultLeavesHeadroomOverTheLargestObservedCatalogValue()
    {
        // The observed maxima across all 138 definitions under content/, recorded in
        // StrictJsonLimits' rationale comments. A limit that a shipped definition
        // already sits near is a limit that will fire on the next edit.
        Expect.Multiple(() =>
        {
            AssertHeadroom(41_600, StrictJsonLimits.DefaultMaximumDocumentBytes, "document bytes");
            AssertHeadroom(7, StrictJsonLimits.DefaultMaximumDepth, "depth");
            AssertHeadroom(30, StrictJsonLimits.DefaultMaximumObjectProperties, "object properties");
            AssertHeadroom(35, StrictJsonLimits.DefaultMaximumArrayElements, "array elements");
            AssertHeadroom(1_253, StrictJsonLimits.DefaultMaximumNodeCount, "node count");
            AssertHeadroom(429, StrictJsonLimits.DefaultMaximumStringLength, "string length");
        });
    }

    [Test]
    public void DepthAcceptsTheLimitAndRejectsOnePast()
    {
        AssertBoundary(
            atLimit: NestedObject(4),
            past: NestedObject(5),
            limits: Limits(depth: 4),
            expected: StrictJsonViolationKind.DepthLimitExceeded,
            subject: "depth");
    }

    [Test]
    public void ObjectPropertyCountAcceptsTheLimitAndRejectsOnePast()
    {
        AssertBoundary(
            atLimit: ObjectWithProperties(6),
            past: ObjectWithProperties(7),
            limits: Limits(objectProperties: 6),
            expected: StrictJsonViolationKind.ObjectPropertyLimitExceeded,
            subject: "object properties");
    }

    [Test]
    public void ArrayElementCountAcceptsTheLimitAndRejectsOnePast()
    {
        AssertBoundary(
            atLimit: ObjectWithArray(5),
            past: ObjectWithArray(6),
            limits: Limits(arrayElements: 5),
            expected: StrictJsonViolationKind.ArrayElementLimitExceeded,
            subject: "array elements");
    }

    [Test]
    public void StringLengthAcceptsTheLimitAndRejectsOnePast()
    {
        AssertBoundary(
            atLimit: ObjectWithString(10),
            past: ObjectWithString(11),
            limits: Limits(stringLength: 10),
            expected: StrictJsonViolationKind.StringTooLong,
            subject: "string length");
    }

    [Test]
    public void NodeCountAcceptsTheLimitAndRejectsOnePast()
    {
        // ObjectWithProperties(n) contains 1 object plus n string values.
        AssertBoundary(
            atLimit: ObjectWithProperties(4),
            past: ObjectWithProperties(5),
            limits: Limits(nodeCount: 5),
            expected: StrictJsonViolationKind.NodeCountLimitExceeded,
            subject: "node count");
    }

    [Test]
    public void DocumentSizeAcceptsTheLimitAndRejectsOnePast()
    {
        string document = ObjectWithProperties(3);
        int size = Encoding.UTF8.GetByteCount(document);

        Expect.Multiple(() =>
        {
            Assert.That(
                Scan(document, Limits(documentBytes: size)).IsValid,
                Is.True,
                "a document exactly at the byte ceiling is accepted");
            Assert.That(
                KindsOf(Scan(document, Limits(documentBytes: size - 1))),
                Does.Contain(StrictJsonViolationKind.DocumentTooLarge),
                "a document one byte past the ceiling is rejected");
        });
    }

    /// <summary>
    /// The size check runs before parsing, so an oversized document reports its size
    /// rather than whatever the parser would have said about its contents.
    /// </summary>
    [Test]
    public void AnOversizedDocumentIsRejectedBeforeItIsParsed()
    {
        StrictJsonScanResult result = Scan("{ this is not json at all", Limits(documentBytes: 4));

        Assert.That(
            KindsOf(result),
            Is.EquivalentTo(new[] { StrictJsonViolationKind.DocumentTooLarge }),
            "the byte ceiling is checked first, so no parse error is reported alongside it");
    }

    private static void AssertHeadroom(int observed, int limit, string subject)
    {
        Assert.That(
            limit,
            Is.GreaterThanOrEqualTo(observed * 2),
            subject + ": the limit must leave at least 2x headroom over the observed catalog "
                + "maximum of " + observed.ToString(CultureInfo.InvariantCulture));
    }

    private static void AssertBoundary(
        string atLimit,
        string past,
        StrictJsonLimits limits,
        StrictJsonViolationKind expected,
        string subject)
    {
        StrictJsonScanResult accepted = Scan(atLimit, limits);
        StrictJsonScanResult rejected = Scan(past, limits);

        Expect.Multiple(() =>
        {
            Assert.That(
                accepted.IsValid,
                Is.True,
                () => subject + ": a document exactly at the limit must be accepted, but got "
                    + Describe(accepted));
            Assert.That(
                KindsOf(rejected),
                Does.Contain(expected),
                () => subject + ": a document one past the limit must report " + expected
                    + ", but got " + Describe(rejected));
        });
    }

    private static StrictJsonScanResult Scan(string json, StrictJsonLimits limits)
    {
        return StrictJsonReader.Scan(
            Encoding.UTF8.GetBytes(json),
            StrictJsonPolicy.Definitions.WithLimits(limits));
    }

    private static IReadOnlyList<StrictJsonViolationKind> KindsOf(StrictJsonScanResult result)
    {
        List<StrictJsonViolationKind> kinds = new();
        foreach (StrictJsonViolation violation in result.Violations)
        {
            kinds.Add(violation.Kind);
        }

        return kinds;
    }

    private static string Describe(StrictJsonScanResult result)
    {
        if (result.Violations.Count == 0)
        {
            return "(no violations)";
        }

        List<string> parts = new();
        foreach (StrictJsonViolation violation in result.Violations)
        {
            parts.Add(violation.Kind + " at byte "
                + violation.ByteOffset.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join("; ", parts);
    }

    /// <summary>A document whose deepest container sits at <paramref name="depth"/>.</summary>
    private static string NestedObject(int depth)
    {
        StringBuilder builder = new();
        for (int level = 0; level < depth; level++)
        {
            builder.Append("{\"a\":");
        }

        builder.Append('1');
        for (int level = 0; level < depth; level++)
        {
            builder.Append('}');
        }

        return builder.ToString();
    }

    private static string ObjectWithProperties(int count)
    {
        StringBuilder builder = new("{");
        for (int index = 0; index < count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append("\"p").Append(index.ToString(CultureInfo.InvariantCulture))
                .Append("\":\"v\"");
        }

        return builder.Append('}').ToString();
    }

    private static string ObjectWithArray(int elements)
    {
        StringBuilder builder = new("{\"a\":[");
        for (int index = 0; index < elements; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append('1');
        }

        return builder.Append("]}").ToString();
    }

    private static string ObjectWithString(int length)
    {
        return "{\"a\":\"" + new string('x', length) + "\"}";
    }

    private static StrictJsonLimits Limits(
        int? documentBytes = null,
        int? depth = null,
        int? objectProperties = null,
        int? arrayElements = null,
        int? nodeCount = null,
        int? stringLength = null)
    {
        return new StrictJsonLimits(
            documentBytes ?? StrictJsonLimits.DefaultMaximumDocumentBytes,
            depth ?? StrictJsonLimits.DefaultMaximumDepth,
            objectProperties ?? StrictJsonLimits.DefaultMaximumObjectProperties,
            arrayElements ?? StrictJsonLimits.DefaultMaximumArrayElements,
            nodeCount ?? StrictJsonLimits.DefaultMaximumNodeCount,
            stringLength ?? StrictJsonLimits.DefaultMaximumStringLength);
    }
}
