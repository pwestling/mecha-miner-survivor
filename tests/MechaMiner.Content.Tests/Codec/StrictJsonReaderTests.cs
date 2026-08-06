using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Codec;

/// <summary>
/// The strict codec rules, including the ones <c>System.Text.Json</c> does not enforce.
/// </summary>
/// <remarks>
/// Verification: <c>VER-DAT-001-001</c> through <c>VER-DAT-001-006</c>.
/// </remarks>
[TestFixture]
internal sealed class StrictJsonReaderTests
{
    /// <summary>
    /// The case that motivates the whole scanner. <c>System.Text.Json</c> accepts a
    /// duplicate property and keeps the last one, so a definition can silently mean
    /// something other than what its first occurrence says. This asserts both halves:
    /// that the platform really does accept it, and that the scanner does not.
    /// </summary>
    [Test]
    public void ADuplicatePropertyIsRejectedEvenThoughSystemTextJsonAcceptsIt()
    {
        const string json = "{\"a\":1,\"a\":2}";

        using (JsonDocument platform = JsonDocument.Parse(json))
        {
            NumericAssert.AreExactlyEqual(
                2,
                platform.RootElement.GetProperty("a").GetInt32(),
                "System.Text.Json keeps the last duplicate, which is why we scan for it ourselves");
        }

        StrictJsonViolation violation = SingleViolation(json);
        Expect.Multiple(() =>
        {
            Assert.That(violation.Kind, Is.EqualTo(StrictJsonViolationKind.DuplicateProperty));
            NumericAssert.AreExactlyEqual("/a", violation.Location.Value, "the duplicate's pointer");
        });
    }

    [TestCase("{\"a\":1} // trailing", StrictJsonViolationKind.Comment)]
    [TestCase("{/* lead */\"a\":1}", StrictJsonViolationKind.Comment)]
    [TestCase("{\"a\":1,}", StrictJsonViolationKind.TrailingComma)]
    [TestCase("{\"a\":[1,2,]}", StrictJsonViolationKind.TrailingComma)]
    [TestCase("{\"a\":NaN}", StrictJsonViolationKind.NonfiniteNumber)]
    [TestCase("{\"a\":Infinity}", StrictJsonViolationKind.NonfiniteNumber)]
    [TestCase("{\"a\":-Infinity}", StrictJsonViolationKind.NonfiniteNumber)]
    [TestCase("{\"a\":1e400}", StrictJsonViolationKind.NonfiniteNumber)]
    [TestCase("{\"a\":null}", StrictJsonViolationKind.NullValue)]
    [TestCase("{\"a\":{\"b\":null}}", StrictJsonViolationKind.NullValue)]
    [TestCase("{\"a\":[null]}", StrictJsonViolationKind.NullValue)]
    [TestCase("{\"camelCase\":1}", StrictJsonViolationKind.PropertyNameNotSnakeCase)]
    [TestCase("{\"Pascal\":1}", StrictJsonViolationKind.PropertyNameNotSnakeCase)]
    [TestCase("{\"kebab-case\":1}", StrictJsonViolationKind.PropertyNameNotSnakeCase)]
    [TestCase("{\"_leading\":1}", StrictJsonViolationKind.PropertyNameNotSnakeCase)]
    [TestCase("{\"a\":1", StrictJsonViolationKind.MalformedJson)]
    [TestCase("", StrictJsonViolationKind.MalformedJson)]
    [TestCase("[1,2]", StrictJsonViolationKind.RootNotObject)]
    public void TheRuleIsEnforcedWithItsOwnViolationKind(
        string json,
        StrictJsonViolationKind expected)
    {
        Assert.That(
            SingleViolation(json).Kind,
            Is.EqualTo(expected),
            () => "scanning " + json);
    }

    /// <summary>
    /// A comment is reported as a comment and not as "invalid start of a value". The
    /// lexical pre-scan exists precisely so that these three faults, which all occur
    /// between values, get their own names instead of one undifferentiated parse error.
    /// </summary>
    [Test]
    public void ALexicalFaultShortCircuitsSoOnlyTheRealCauseIsReported()
    {
        StrictJsonScanResult result = Scan("{\"a\":1, /* c */ \"b\":2,}");

        Assert.That(
            Kinds(result),
            Is.EquivalentTo(new[] { StrictJsonViolationKind.Comment }),
            "the comment is the first cause; reporting the trailing comma too would make an "
                + "author guess which one to fix");
    }

    [Test]
    public void InvalidUtf8IsRejectedBeforeParsing()
    {
        // 0xFF is not a legal UTF-8 byte in any position.
        byte[] bytes = { (byte)'{', (byte)'"', 0xFF, (byte)'"', (byte)':', (byte)'1', (byte)'}' };

        StrictJsonScanResult result =
            StrictJsonReader.Scan(bytes, StrictJsonPolicy.Definitions);

        Assert.That(
            Kinds(result),
            Is.EquivalentTo(new[] { StrictJsonViolationKind.InvalidUtf8 }),
            "source files are UTF-8");
    }

    [TestCase("{\"snake_case\":1}")]
    [TestCase("{\"a\":1}")]
    [TestCase("{\"a1\":1}")]
    [TestCase("{\"body_scale_multiplier\":1.5}")]
    [TestCase("{\"a\":{\"b\":[1,2,{\"c\":true}]}}")]
    [TestCase("{}")]
    public void AWellFormedDefinitionDocumentScansCleanly(string json)
    {
        StrictJsonScanResult result = Scan(json);

        Assert.That(result.IsValid, Is.True, () => "scanning " + json + ": " + Describe(result));
    }

    /// <summary>
    /// The localization catalog is keyed by <c>&lt;category&gt;.&lt;stable_id&gt;.&lt;role&gt;</c>,
    /// which is not snake_case and is not meant to be: there a property name is data, not
    /// a schema-declared field. The two policies exist so both rules in doc 40 can hold.
    /// </summary>
    [Test]
    public void ACatalogKeyIsDataAndIsNotHeldToTheFieldNamingRule()
    {
        const string catalog = "{\"weapon.W-AB.name\":\"Fracture Lance\"}";

        Expect.Multiple(() =>
        {
            Assert.That(
                Scan(catalog).IsValid,
                Is.False,
                "under the definition policy a property name is a field and must be snake_case");
            Assert.That(
                StrictJsonReader.Scan(
                    Encoding.UTF8.GetBytes(catalog), StrictJsonPolicy.KeyedCatalog).IsValid,
                Is.True,
                "under the catalog policy a property name is data");
        });
    }

    [Test]
    public void TheScanRecordsThePointerAndKindOfEveryValue()
    {
        StrictJsonScanResult result = Scan("{\"a\":{\"b\":[1,\"x\"]},\"c\":true}");

        Expect.Multiple(() =>
        {
            AssertKind(result, "", JsonValueKind.Object);
            AssertKind(result, "/a", JsonValueKind.Object);
            AssertKind(result, "/a/b", JsonValueKind.Array);
            AssertKind(result, "/a/b/0", JsonValueKind.Number);
            AssertKind(result, "/a/b/1", JsonValueKind.String);
            AssertKind(result, "/c", JsonValueKind.True);
            Assert.That(
                result.Structure.Contains(JsonPointer.Root.AppendProperty("missing")),
                Is.False);
        });
    }

    [Test]
    public void RootPropertyNamesAreRetainedInAuthoredOrder()
    {
        StrictJsonScanResult result = Scan("{\"zulu\":1,\"alpha\":2,\"mike\":3}");

        Assert.That(
            result.Structure.RootPropertyNames,
            Is.EqualTo(new[] { "zulu", "alpha", "mike" }),
            "authored order is the order a diagnostic reports fields in");
    }

    /// <summary>
    /// A property name is compared ordinally, so two names differing only in case are two
    /// different fields rather than one duplicate. Doc 40 makes tokens case-sensitive.
    /// </summary>
    [Test]
    public void CaseDifferingPropertyNamesAreNotDuplicates()
    {
        StrictJsonScanResult result =
            StrictJsonReader.Scan(
                Encoding.UTF8.GetBytes("{\"a\":1,\"A\":2}"),
                StrictJsonPolicy.KeyedCatalog);

        Assert.That(
            Kinds(result),
            Does.Not.Contain(StrictJsonViolationKind.DuplicateProperty));
    }

    [TestCase("a", true)]
    [TestCase("a_b", true)]
    [TestCase("a1", true)]
    [TestCase("body_scale_multiplier", true)]
    [TestCase("A", false)]
    [TestCase("aB", false)]
    [TestCase("1a", false)]
    [TestCase("_a", false)]
    [TestCase("a-b", false)]
    [TestCase("", false)]
    public void SnakeCaseMatchesTheStatedExpression(string name, bool expected)
    {
        Assert.That(StrictJsonReader.IsSnakeCase(name), Is.EqualTo(expected));
    }

    private static void AssertKind(
        StrictJsonScanResult result,
        string pointer,
        JsonValueKind expected)
    {
        JsonPointer target = PointerOf(pointer);
        Assert.That(
            result.Structure.TryGetKind(target, out JsonValueKind kind) ? kind : JsonValueKind.Undefined,
            Is.EqualTo(expected),
            "the kind recorded at '" + pointer + "'");
    }

    private static JsonPointer PointerOf(string pointer)
    {
        JsonPointer result = JsonPointer.Root;
        if (pointer.Length == 0)
        {
            return result;
        }

        foreach (string token in pointer.TrimStart('/').Split('/'))
        {
            result = int.TryParse(token, out int index)
                ? result.AppendIndex(index)
                : result.AppendProperty(token);
        }

        return result;
    }

    private static StrictJsonScanResult Scan(string json)
    {
        return StrictJsonReader.Scan(Encoding.UTF8.GetBytes(json), StrictJsonPolicy.Definitions);
    }

    private static StrictJsonViolation SingleViolation(string json)
    {
        StrictJsonScanResult result = Scan(json);
        Assert.That(
            result.Violations,
            Has.Count.EqualTo(1),
            () => "expected exactly one violation from " + json + ", got " + Describe(result));
        return result.Violations[0];
    }

    private static IReadOnlyList<StrictJsonViolationKind> Kinds(StrictJsonScanResult result)
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
        return result.Violations.Count == 0 ? "(none)" : string.Join(", ", Kinds(result));
    }
}
