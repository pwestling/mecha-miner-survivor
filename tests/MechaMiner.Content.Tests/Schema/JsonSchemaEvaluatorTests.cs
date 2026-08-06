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
/// <remarks>Verification: <c>VER-DAT-001-020</c>.</remarks>
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
    [TestCase("{\"minLength\":2,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "\"ab\"", true)]
    [TestCase("{\"minLength\":2,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "\"a\"", false)]
    [TestCase("{\"maxLength\":2,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "\"abc\"", false)]
    [TestCase("{\"minimum\":1,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "1", true)]
    [TestCase("{\"minimum\":1,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "0", false)]
    [TestCase("{\"exclusiveMinimum\":1,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "1", false)]
    [TestCase("{\"maximum\":1,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "2", false)]
    [TestCase("{\"exclusiveMaximum\":1,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "1", false)]
    [TestCase("{\"multipleOf\":3,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "9", true)]
    [TestCase("{\"multipleOf\":3,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "10", false)]
    [TestCase("{\"minItems\":2,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "[1]", false)]
    [TestCase("{\"maxItems\":1,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}", "[1,2]", false)]
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
    [TestCase("{\"allOf\":[{\"type\":\"string\"},{\"minLength\":2,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}]}", "\"ab\"", true)]
    [TestCase("{\"allOf\":[{\"type\":\"string\"},{\"minLength\":2,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}]}", "\"a\"", false)]
    [TestCase("{\"anyOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}", "1", true)]
    [TestCase("{\"anyOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}", "true", false)]
    [TestCase("{\"oneOf\":[{\"type\":\"string\"},{\"type\":\"integer\"}]}", "1", true)]
    [TestCase("{\"oneOf\":[{\"minimum\":1,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}},{\"minimum\":2,\"description\":\"bound under test\",\"x-authority\":{\"kind\":\"structural\"}}]}", "3", false)]
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
