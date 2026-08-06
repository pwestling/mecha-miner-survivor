using MechaMiner.Content.Codec;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Codec;

/// <summary>RFC 6901 conformance. Verification: <c>VER-DAT-001-013</c>.</summary>
[TestFixture]
internal sealed class JsonPointerTests
{
    [Test]
    public void TheRootPointerIsTheEmptyString()
    {
        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(string.Empty, JsonPointer.Root.Value, "the root pointer");
            Assert.That(JsonPointer.Root.IsRoot, Is.True);
        });
    }

    [TestCase("foo", "/foo")]
    [TestCase("", "/")]
    [TestCase("a/b", "/a~1b")]
    [TestCase("m~n", "/m~0n")]
    [TestCase("~1", "/~01")]
    [TestCase("a~/b", "/a~0~1b")]
    public void APropertyTokenIsEscapedPerRfc6901(string propertyName, string expected)
    {
        NumericAssert.AreExactlyEqual(
            expected,
            JsonPointer.Root.AppendProperty(propertyName).Value,
            "the escaped pointer for property '" + propertyName + "'");
    }

    /// <summary>
    /// The escaping order is normative: <c>~</c> becomes <c>~0</c> before <c>/</c>
    /// becomes <c>~1</c>. Reversing it makes a literal <c>~1</c> and an escaped
    /// <c>/</c> the same text, so the pointer stops being reversible. <c>"~1"</c> above
    /// is exactly that case: the correct answer is <c>/~01</c>, and the wrong order
    /// yields <c>/~1</c>, which reads back as <c>/</c>.
    /// </summary>
    [TestCase("~1")]
    [TestCase("a/b")]
    [TestCase("m~n")]
    [TestCase("a~/b")]
    [TestCase("plain")]
    public void EscapingRoundTrips(string propertyName)
    {
        string escaped = JsonPointer.EscapeToken(propertyName);

        NumericAssert.AreExactlyEqual(
            propertyName,
            JsonPointer.UnescapeToken(escaped),
            "unescaping must recover the original token");
    }

    [Test]
    public void AnArrayIndexIsAnUnpaddedDecimal()
    {
        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                "/rules/0",
                JsonPointer.Root.AppendProperty("rules").AppendIndex(0).Value,
                "index zero");
            NumericAssert.AreExactlyEqual(
                "/rules/33",
                JsonPointer.Root.AppendProperty("rules").AppendIndex(33).Value,
                "a two-digit index");
        });
    }

    [Test]
    public void NestedAppendsComposeLeftToRight()
    {
        JsonPointer pointer = JsonPointer.Root
            .AppendProperty("minute_rows")
            .AppendIndex(33)
            .AppendProperty("formation_events")
            .AppendIndex(0);

        NumericAssert.AreExactlyEqual(
            "/minute_rows/33/formation_events/0",
            pointer.Value,
            "a composed pointer");
    }

    [Test]
    public void PointersCompareByValue()
    {
        JsonPointer left = JsonPointer.Root.AppendProperty("id");
        JsonPointer right = JsonPointer.Root.AppendProperty("id");

        Expect.Multiple(() =>
        {
            Assert.That(left, Is.EqualTo(right));
            Assert.That(left == right, Is.True);
            Assert.That(left != JsonPointer.Root, Is.True);
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        });
    }
}
