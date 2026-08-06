using System.IO;
using System.Text;
using System.Text.Json;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Schema;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// The draft 2020-12 evaluator, including the ways it refuses to be a silent no-op.
/// </summary>
/// <remarks>
/// Verification: <c>VER-DAT-001-020</c>, <c>VER-DAT-001-033</c>.
/// </remarks>
[TestFixture]
internal sealed class JsonSchemaEvaluatorTests
{
    /// <summary>
    /// The rule that keeps the schema a gate. A keyword the evaluator does not implement
    /// must fail the load; ignoring it would silently drop a constraint and still report
    /// "valid".
    /// </summary>
    [Test]
    public void AnUnimplementedKeywordFailsTheLoad()
    {
        JsonSchemaLoadResult result = LoadFixture("unsupported-keyword.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(ContentDiagnosticCodes.SchemaKeywordUnsupported));
            Assert.That(
                result.Diagnostics[0].ExpectedConstraint,
                Does.Contain("patternProperties"),
                "the diagnostic names the offending keyword");
        });
    }

    [Test]
    public void AnUnresolvableReferenceFailsTheLoad()
    {
        JsonSchemaLoadResult result = LoadFixture("unresolvable-ref.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(ContentDiagnosticCodes.SchemaReferenceUnresolved));
        });
    }

    [Test]
    public void AMalformedSchemaFailsTheLoad()
    {
        JsonSchemaLoadResult result = LoadFixture("malformed.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Diagnostics[0].Code, Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
        });
    }

    [Test]
    public void SchemaBytesThatAreNotJsonFailTheLoad()
    {
        JsonSchemaLoadResult result =
            JsonSchemaLoader.Load(Encoding.UTF8.GetBytes("{ not json"), "x.json");

        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
    }

    [TestCase("{\"type\":\"string\"}", "\"a\"", true)]
    [TestCase("{\"type\":\"string\"}", "1", false)]
    [TestCase("{\"type\":\"integer\"}", "1", true)]
    [TestCase("{\"type\":\"integer\"}", "1.0", true)]
    [TestCase("{\"type\":\"integer\"}", "1.5", false)]
    [TestCase("{\"type\":[\"string\",\"null\"]}", "null", true)]
    [TestCase("{\"enum\":[\"a\",\"b\"]}", "\"a\"", true)]
    [TestCase("{\"enum\":[\"a\",\"b\"]}", "\"c\"", false)]
    [TestCase("{\"const\":5}", "5", true)]
    [TestCase("{\"const\":5}", "5.0", true)]
    [TestCase("{\"const\":5}", "6", false)]
    [TestCase("{\"pattern\":\"^a+$\"}", "\"aaa\"", true)]
    [TestCase("{\"pattern\":\"^a+$\"}", "\"ab\"", false)]
    [TestCase("{\"minLength\":2,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "\"ab\"", true)]
    [TestCase("{\"minLength\":2,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "\"a\"", false)]
    [TestCase("{\"maxLength\":2,\"x-authority\":{\"maxLength\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "\"abc\"", false)]
    [TestCase("{\"minimum\":1,\"x-authority\":{\"minimum\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "1", true)]
    [TestCase("{\"minimum\":1,\"x-authority\":{\"minimum\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "0", false)]
    [TestCase("{\"exclusiveMinimum\":1,\"x-authority\":{\"exclusiveMinimum\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "1", false)]
    [TestCase("{\"maximum\":1,\"x-authority\":{\"maximum\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "2", false)]
    [TestCase("{\"exclusiveMaximum\":1,\"x-authority\":{\"exclusiveMaximum\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "1", false)]
    [TestCase("{\"multipleOf\":3,\"x-authority\":{\"multipleOf\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "9", true)]
    [TestCase("{\"multipleOf\":3,\"x-authority\":{\"multipleOf\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "10", false)]
    [TestCase("{\"minItems\":2,\"x-authority\":{\"minItems\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "[1]", false)]
    [TestCase("{\"maxItems\":1,\"x-authority\":{\"maxItems\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}", "[1,2]", false)]
    [TestCase("{\"uniqueItems\":true}", "[1,2]", true)]
    [TestCase("{\"uniqueItems\":true}", "[1,1]", false)]
    [TestCase("{\"uniqueItems\":true}", "[\"a\",\"a\"]", false)]
    [TestCase("{\"required\":[\"a\"]}", "{\"a\":1}", true)]
    [TestCase("{\"required\":[\"a\"]}", "{\"b\":1}", false)]
    [TestCase("{\"properties\":{\"a\":{\"type\":\"string\"}},\"additionalProperties\":false}", "{\"a\":\"x\"}", true)]
    [TestCase("{\"properties\":{\"a\":{\"type\":\"string\"}},\"additionalProperties\":false}", "{\"b\":1}", false)]
    [TestCase("{\"propertyNames\":{\"pattern\":\"^[a-z_]+$\"}}", "{\"a_b\":1}", true)]
    [TestCase("{\"propertyNames\":{\"pattern\":\"^[a-z_]+$\"}}", "{\"aB\":1}", false)]
    [TestCase("{\"items\":{\"type\":\"string\"}}", "[\"a\"]", true)]
    [TestCase("{\"items\":{\"type\":\"string\"}}", "[1]", false)]
    [TestCase("{\"prefixItems\":[{\"type\":\"string\"}]}", "[\"a\",1]", true)]
    [TestCase("{\"prefixItems\":[{\"type\":\"string\"}]}", "[1]", false)]
    [TestCase("{\"allOf\":[{\"type\":\"string\"},{\"minLength\":2,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}]}", "\"ab\"", true)]
    [TestCase("{\"allOf\":[{\"type\":\"string\"},{\"minLength\":2,\"x-authority\":{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}]}", "\"a\"", false)]
    [TestCase("{\"anyOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}", "1", true)]
    [TestCase("{\"anyOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}", "true", false)]
    [TestCase("{\"oneOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}", "1", true)]
    [TestCase("{\"oneOf\":[{\"minimum\":1,\"x-authority\":{\"minimum\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}},{\"minimum\":2,\"x-authority\":{\"minimum\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}]}", "3", false)]
    [TestCase("{\"not\":{\"type\":\"string\"}}", "1", true)]
    [TestCase("{\"not\":{\"type\":\"string\"}}", "\"a\"", false)]
    [TestCase("true", "1", true)]
    [TestCase("false", "1", false)]
    public void TheKeywordAssertsWhatTheSpecificationSays(
        string schemaText, string instanceText, bool expected)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(schemaText), "inline.schema.json");
        Assert.That(
            load.IsValid, Is.True, () => "schema failed to load: " + Describe(load));

        using JsonDocument instance = JsonDocument.Parse(instanceText);
        JsonSchemaEvaluationResult result =
            JsonSchemaEvaluator.Evaluate(load.Schema!, instance.RootElement);

        Assert.That(
            result.IsValid,
            Is.EqualTo(expected),
            () => schemaText + " against " + instanceText + ": " + result);
    }

    /// <summary>
    /// A composite <c>enum</c> value is a load failure rather than a weaker comparison,
    /// on the same principle as an unimplemented keyword.
    /// </summary>
    [TestCase("{\"enum\":[{\"a\":1}]}")]
    [TestCase("{\"const\":[1,2]}")]
    public void ACompositeEnumOrConstValueFailsTheLoadRatherThanComparingLoosely(string schemaText)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(schemaText), "inline.schema.json");

        Expect.Multiple(() =>
        {
            Assert.That(load.IsValid, Is.False);
            Assert.That(load.Diagnostics[0].Code, Is.EqualTo(ContentDiagnosticCodes.SchemaMalformed));
        });
    }

    /// <summary>
    /// <c>propertyNames</c> evaluates each property name as a string instance, including
    /// names that have to be escaped to be written as JSON at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The path lifts a <see cref="string"/> back into a <see cref="JsonElement"/>, which
    /// means quoting it. That was done with the <c>JsonSerializer.Serialize</c> overload
    /// that takes no <c>JsonTypeInfo</c>, and so went through the reflection-based contract
    /// resolver. <c>JsonSerializerIsReflectionEnabledByDefault</c> is a
    /// <em>per-application</em> runtimeconfig property rather than a per-assembly one, so
    /// the same assembly that passed under this test host threw
    /// <c>InvalidOperationException: Reflection-based serialization has been disabled</c>
    /// under the host that runs the content-compile verb. Doc 40 forbids runtime contract
    /// reflection outright, so this was not merely a portability hazard.
    /// </para>
    /// <para>
    /// The cases below are the ones where the quoting has to be right rather than merely
    /// present: a name containing a quote, a backslash, a control character, and non-ASCII
    /// text. A hand-rolled replacement that concatenated quotes would pass a test using
    /// <c>a_b</c> and corrupt every one of these.
    /// </para>
    /// <para>
    /// <b>What this does not prove.</b> This test project does not set
    /// <c>JsonSerializerIsReflectionEnabledByDefault</c>, so the suite still cannot see a
    /// reflection-based call of this class; it would pass on the old code too. It proves
    /// this call site is fixed and its behaviour unchanged, not that the class is closed.
    /// Closing it means setting that property on the library and test projects so the suite
    /// runs under the consumer's constraint.
    /// </para>
    /// </remarks>
    [TestCase("a_b", true)]
    [TestCase("aB", false)]
    [TestCase("say \"what\"", false)]
    [TestCase("back\\slash", false)]
    [TestCase("tab\there", false)]
    [TestCase("naïve", false)]
    [TestCase("", false)]
    public void PropertyNamesEvaluatesEachNameAsAStringInstance(string name, bool expected)
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes("{\"propertyNames\":{\"pattern\":\"^[a-z_]+$\"}}"),
            "inline.schema.json");
        Assert.That(load.IsValid, Is.True, () => Describe(load));

        using JsonDocument instance = BuildObjectWithOneProperty(name);
        JsonSchemaEvaluationResult result =
            JsonSchemaEvaluator.Evaluate(load.Schema!, instance.RootElement);

        Assert.That(
            result.IsValid,
            Is.EqualTo(expected),
            () => "property name <" + name + "> against ^[a-z_]+$: " + result);
    }

    /// <summary>
    /// The same path reports the offending name back, so a rejection is actionable.
    /// </summary>
    [Test]
    public void PropertyNamesReportsTheNameItRejected()
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes("{\"propertyNames\":{\"minLength\":4,\"x-authority\":"
                + "{\"minLength\":{\"kind\":\"structural\",\"rationale\":\"bound under test\"}}}}"),
            "inline.schema.json");
        Assert.That(load.IsValid, Is.True, () => Describe(load));

        using JsonDocument instance = BuildObjectWithOneProperty("ab");
        JsonSchemaEvaluationResult result =
            JsonSchemaEvaluator.Evaluate(load.Schema!, instance.RootElement);

        Expect.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.ToString(),
                Does.Contain("ab"),
                "the error must name the property whose name failed");
        });
    }

    /// <summary>
    /// Builds <c>{"&lt;name&gt;": 1}</c> without going through a reflection-based
    /// serializer, so the test does not depend on the very thing under test.
    /// </summary>
    private static JsonDocument BuildObjectWithOneProperty(string name)
    {
        System.Buffers.ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber(name, 1);
            writer.WriteEndObject();
        }

        return JsonDocument.Parse(buffer.WrittenMemory);
    }

    [Test]
    public void AnnotationKeywordsAreAcceptedAndAssertNothing()
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(
                "{\"title\":\"t\",\"description\":\"d\",\"$comment\":\"c\","
                + "\"type\":\"integer\"}"),
            "inline.schema.json");

        Assert.That(load.IsValid, Is.True, () => Describe(load));
    }

    [Test]
    public void AReferenceIntoDefsResolves()
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes(
                "{\"$defs\":{\"s\":{\"type\":\"string\"}},\"properties\":{\"a\":{\"$ref\":\"#/$defs/s\"}}}"),
            "inline.schema.json");
        Assert.That(load.IsValid, Is.True, () => Describe(load));

        using JsonDocument good = JsonDocument.Parse("{\"a\":\"x\"}");
        using JsonDocument bad = JsonDocument.Parse("{\"a\":1}");

        Expect.Multiple(() =>
        {
            Assert.That(JsonSchemaEvaluator.Evaluate(load.Schema!, good.RootElement).IsValid, Is.True);
            Assert.That(JsonSchemaEvaluator.Evaluate(load.Schema!, bad.RootElement).IsValid, Is.False);
        });
    }

    [Test]
    public void AnErrorNamesTheInstanceLocationAndTheKeyword()
    {
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(
            Encoding.UTF8.GetBytes("{\"properties\":{\"a\":{\"type\":\"string\"}}}"),
            "inline.schema.json");
        using JsonDocument instance = JsonDocument.Parse("{\"a\":1}");

        JsonSchemaError error =
            JsonSchemaEvaluator.Evaluate(load.Schema!, instance.RootElement).Errors[0];

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual("/a", error.InstanceLocation.Value, "instance location");
            NumericAssert.AreExactlyEqual("type", error.Keyword, "the failing keyword");
        });
    }

    private static JsonSchemaLoadResult LoadFixture(string name)
    {
        string path = Path.Combine(FixtureCorpus.Root, "schema", name);
        return JsonSchemaLoader.Load(File.ReadAllBytes(path), "tests/fixtures/schema/" + name);
    }

    private static string Describe(JsonSchemaLoadResult result)
    {
        return string.Join("; ", result.Diagnostics);
    }
}
