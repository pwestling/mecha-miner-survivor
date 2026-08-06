using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// A fixture loaded as a mutable tree, so a catalog-level test can build the second,
/// third, and fifteenth definition of a catalog from the one on disk.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a variant is built as a document and not as a model.</b> The typed models
/// have internal constructors: a public one would let a consumer hold a model that was
/// never validated, which is the whole thing the reader exists to prevent. Building a
/// variant as JSON and reading it through the real reader keeps that property and has a
/// second benefit - the variant goes through every check a real file does, so a catalog
/// test cannot accidentally assemble a catalog out of definitions that would not have
/// loaded.
/// </para>
/// <para>
/// <c>JsonNode</c> is a dynamic JSON object, which
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline forbids "in production paths". This is a test fixture builder and never
/// ships; nothing in <c>src/</c> references it.
/// </para>
/// </remarks>
internal sealed class FixtureDocument
{
    private readonly JsonObject _root;

    private FixtureDocument(JsonObject root)
    {
        _root = root;
    }

    /// <summary>Loads a fixture as a mutable tree.</summary>
    internal static FixtureDocument Load(string relativePath)
    {
        JsonNode? node = JsonNode.Parse(CategoryFixtureCorpus.Read(relativePath));
        if (node is not JsonObject root)
        {
            throw new InvalidOperationException(relativePath + " is not a JSON object");
        }

        return new FixtureDocument(root);
    }

    /// <summary>Sets a root property.</summary>
    internal FixtureDocument With(string property, JsonNode? value)
    {
        _root[property] = value;
        return this;
    }

    /// <summary>Sets a property of a nested object.</summary>
    internal FixtureDocument WithIn(string container, string property, JsonNode? value)
    {
        JsonObject nested = (JsonObject)_root[container]!;
        nested[property] = value;
        return this;
    }

    /// <summary>Rewrites the stable ID and every localization key that carries it.</summary>
    /// <remarks>
    /// A localization key embeds the stable ID verbatim, so a variant that changed only
    /// the ID would carry keys pointing at a different definition. Rewriting both keeps
    /// the variant a document that could plausibly have been authored.
    /// </remarks>
    internal FixtureDocument WithId(string oldId, string newId)
    {
        _root["id"] = newId;
        foreach (string field in new[] { "name_key", "summary_key" })
        {
            if (_root[field] is JsonValue existing)
            {
                _root[field] = existing.GetValue<string>().Replace(
                    "." + oldId + ".", "." + newId + ".", StringComparison.Ordinal);
            }
        }

        return this;
    }

    /// <summary>Removes a root property.</summary>
    internal FixtureDocument Without(string property)
    {
        _root.Remove(property);
        return this;
    }

    /// <summary>The document's UTF-8 bytes.</summary>
    internal byte[] ToUtf8()
    {
        return Encoding.UTF8.GetBytes(_root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
        }));
    }

    /// <summary>Reads this variant through the real reader and requires it to validate.</summary>
    internal TDefinition Read<TDefinition>(DefinitionKind kind, string subject)
        where TDefinition : ContentDefinition
    {
        CategoryReadContext context = new("tests/generated/" + subject + ".json", kind);
        DefinitionReadResult result = CategorySchemas.Read(ToUtf8(), context);

        Assert.That(
            result.IsValid,
            Is.True,
            () => subject + " must validate before a catalog rule can be checked against it: "
                + string.Join("; ", result.Diagnostics));

        return (TDefinition)result.Definition!;
    }

    /// <summary>Builds a JSON array of strings.</summary>
    internal static JsonArray Strings(IReadOnlyList<string> values)
    {
        JsonArray array = new();
        foreach (string value in values)
        {
            array.Add(JsonValue.Create(value));
        }

        return array;
    }
}
