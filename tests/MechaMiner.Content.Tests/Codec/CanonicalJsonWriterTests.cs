using System;
using System.Collections.Generic;
using System.Text;
using MechaMiner.Content.Codec;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Codec;

/// <summary>
/// Doc 40's three ordering rules, proved to be three distinct mechanisms.
/// </summary>
/// <remarks>
/// <para>
/// The point of these tests is that the three rules are <em>not</em> interchangeable.
/// A writer that sorted everything would pass a dictionary test and silently corrupt a
/// semantically ordered array; one that preserved authored order everywhere would make
/// the hash depend on source property order. Each test therefore pins a rule against
/// the behaviour of the other two.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-008</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CanonicalJsonWriterTests
{
    private static readonly SchemaFieldOrder Order =
        new("test", new[] { "zulu", "alpha", "mike" });

    /// <summary>
    /// Rule 1: fields come out in schema-declared order, which here is deliberately not
    /// alphabetical, so a writer that sorted field names would fail.
    /// </summary>
    [Test]
    public void FieldsAreEmittedInSchemaDeclaredOrderNotAlphabeticalOrder()
    {
        byte[] payload = CanonicalJson.SerializeObject(Order, writer =>
        {
            writer.WriteString("zulu", "z");
            writer.WriteString("alpha", "a");
            writer.WriteString("mike", "m");
        });

        NumericAssert.AreExactlyEqual(
            "{\"zulu\":\"z\",\"alpha\":\"a\",\"mike\":\"m\"}",
            Encoding.UTF8.GetString(payload),
            "schema-declared order");
    }

    [Test]
    public void AFieldWrittenOutOfDeclaredOrderIsAProgrammingError()
    {
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => CanonicalJson.SerializeObject(Order, writer =>
            {
                writer.WriteString("alpha", "a");
                writer.WriteString("zulu", "z");
            }));

        Assert.That(failure.Message, Does.Contain("out of the order declared"));
    }

    [Test]
    public void AFieldTheSchemaDoesNotDeclareIsAProgrammingError()
    {
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => CanonicalJson.SerializeObject(Order, writer => writer.WriteString("nope", "x")));

        Assert.That(failure.Message, Does.Contain("is not declared by schema field order"));
    }

    /// <summary>
    /// Rule 2: dictionary entries sort by key, ordinally. Ordinal and not culture-aware:
    /// the two disagree on case ordering, and a culture-aware sort would make the payload
    /// depend on the machine's locale, which doc 40 forbids by name.
    /// </summary>
    [Test]
    public void DictionaryKeysAreSortedOrdinally()
    {
        byte[] payload = CanonicalJson.SerializeObject(
            new SchemaFieldOrder("d", new[] { "map" }),
            writer => writer.WriteSortedDictionary(
                "map",
                new[]
                {
                    new KeyValuePair<string, long>("b", 2),
                    new KeyValuePair<string, long>("A", 1),
                    new KeyValuePair<string, long>("a", 3),
                    new KeyValuePair<string, long>("B", 4),
                },
                static (target, value) => target.WriteIntegerValue(value)));

        // Ordinal puts every uppercase letter before every lowercase one. A
        // culture-aware sort would interleave them as A, a, B, b.
        NumericAssert.AreExactlyEqual(
            "{\"map\":{\"A\":1,\"B\":4,\"a\":3,\"b\":2}}",
            Encoding.UTF8.GetString(payload),
            "ordinal key order");
    }

    [Test]
    public void DictionaryOrderDoesNotDependOnInsertionOrder()
    {
        static byte[] Write(IEnumerable<KeyValuePair<string, long>> entries)
        {
            return CanonicalJson.SerializeObject(
                new SchemaFieldOrder("d", new[] { "map" }),
                writer => writer.WriteSortedDictionary(
                    "map", entries, static (t, v) => t.WriteIntegerValue(v)));
        }

        KeyValuePair<string, long>[] forward =
        {
            new("alpha", 1), new("bravo", 2), new("charlie", 3),
        };
        KeyValuePair<string, long>[] reversed =
        {
            new("charlie", 3), new("bravo", 2), new("alpha", 1),
        };

        Assert.That(Write(reversed), Is.EqualTo(Write(forward)));
    }

    /// <summary>
    /// Rule 3: a semantically ordered array keeps its authored order. This is the rule
    /// that a "sort everything" writer would break, and breaking it changes what the
    /// definition says rather than merely how it is written.
    /// </summary>
    [Test]
    public void SemanticallyOrderedArraysKeepTheirAuthoredOrder()
    {
        byte[] payload = CanonicalJson.SerializeObject(
            new SchemaFieldOrder("a", new[] { "rules" }),
            writer => writer.WriteOrderedArray(
                "rules",
                new[] { "zebra", "apple", "mango" },
                static (target, value) => target.WriteStringValue(value)));

        NumericAssert.AreExactlyEqual(
            "{\"rules\":[\"zebra\",\"apple\",\"mango\"]}",
            Encoding.UTF8.GetString(payload),
            "authored array order is preserved, not sorted");
    }

    /// <summary>
    /// The fourth case: a stable-ID set has no keys to sort by and no authored order to
    /// preserve, so it is ordered by the ID token itself.
    /// </summary>
    [Test]
    public void StableIdSetsAreEmittedInCanonicalIdOrder()
    {
        byte[] payload = CanonicalJson.SerializeObject(
            new SchemaFieldOrder("s", new[] { "ids" }),
            writer => writer.WriteIdSet("ids", new[] { "W-BD", "MCH-01", "EN-01", "BOSS-01" }));

        NumericAssert.AreExactlyEqual(
            "{\"ids\":[\"BOSS-01\",\"EN-01\",\"MCH-01\",\"W-BD\"]}",
            Encoding.UTF8.GetString(payload),
            "canonical ID order");
    }

    [Test]
    public void ADuplicateInAnIdSetIsAWriteFailureBecauseASetCannotHoldOne()
    {
        Expect.Throws<ArgumentException>(() => CanonicalJson.SerializeObject(
            new SchemaFieldOrder("s", new[] { "ids" }),
            writer => writer.WriteIdSet("ids", new[] { "W-AB", "W-AB" })));
    }

    [Test]
    public void ADuplicateDictionaryKeyIsAWriteFailure()
    {
        Expect.Throws<ArgumentException>(() => CanonicalJson.SerializeObject(
            new SchemaFieldOrder("d", new[] { "map" }),
            writer => writer.WriteSortedDictionary(
                "map",
                new[] { new KeyValuePair<string, long>("a", 1), new KeyValuePair<string, long>("a", 2) },
                static (t, v) => t.WriteIntegerValue(v))));
    }

    [Test]
    public void TheCanonicalPayloadIsNeverIndented()
    {
        byte[] payload = CanonicalJson.SerializeObject(Order, writer =>
        {
            writer.WriteString("zulu", "z");
            writer.WriteString("alpha", "a");
        });

        string text = Encoding.UTF8.GetString(payload);
        Expect.Multiple(() =>
        {
            Assert.That(text, Does.Not.Contain("\n"), "canonical bytes carry no line breaks");
            Assert.That(text, Does.Not.Contain("  "), "canonical bytes carry no indentation");
        });
    }

    [Test]
    public void NumbersInAPayloadUseCanonicalFormatting()
    {
        byte[] payload = CanonicalJson.SerializeObject(
            new SchemaFieldOrder("n", new[] { "count", "ratio", "zero" }),
            writer =>
            {
                writer.WriteInteger("count", 42);
                writer.WriteNumber("ratio", 0.1);
                writer.WriteNumber("zero", -0.0);
            });

        NumericAssert.AreExactlyEqual(
            "{\"count\":42,\"ratio\":0.1,\"zero\":0}",
            Encoding.UTF8.GetString(payload),
            "canonical numeric forms inside a payload");
    }

    [Test]
    public void SerializeAndHashAgreeOnTheSameBytes()
    {
        void Write(CanonicalJsonWriter writer)
        {
            writer.BeginObject(Order);
            writer.WriteString("zulu", "z");
            writer.EndObject();
        }

        NumericAssert.AreExactlyEqual(
            CanonicalHash.Sha256Hex(CanonicalJson.Serialize(Write)),
            CanonicalJson.Sha256HexOf(Write),
            "the hash helper must hash exactly the serialized bytes");
    }
}
