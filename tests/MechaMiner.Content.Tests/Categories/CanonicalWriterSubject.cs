using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Envelope;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// One valid category fixture paired with the writer of its kind: the unit the canonical
/// writer tests and the ordering gate both range over.
/// </summary>
/// <remarks>
/// <para>
/// The subject corpus is <see cref="CategoryFixtureCorpus.Valid"/> and nothing else, which is
/// how the gate inherits that corpus's own coverage assertions instead of carrying a second
/// list that could drift or shrink. Every subject's fixture is a document a reader accepted
/// with zero diagnostics, so a canonical payload written from one is a payload of real
/// validated content.
/// </para>
/// <para>
/// <b>The envelope is read once and reused across permutations.</b> Permuting a semantically
/// ordered array frequently makes the document invalid - reversing <c>minute_rows</c> breaks
/// contiguity, reversing <c>ranks</c> breaks the rank sequence - so re-reading a permuted
/// document through <see cref="CategorySchemas.Read"/> would fail for reasons that have nothing
/// to do with ordering, and the gate would be measuring the validator instead of the writer.
/// A permutation only ever moves elements within one array, so the envelope it was read with is
/// still the envelope of the permuted document, and reusing it keeps the subject of the test
/// the writer.
/// </para>
/// </remarks>
internal sealed class CanonicalWriterSubject
{
    private readonly string _authoredJson;

    private CanonicalWriterSubject(
        CategoryFixture fixture,
        DefinitionEnvelope envelope,
        string authoredJson)
    {
        Fixture = fixture;
        Envelope = envelope;
        _authoredJson = authoredJson;
    }

    /// <summary>Every valid category fixture, as a writer subject.</summary>
    internal static IReadOnlyList<CanonicalWriterSubject> All { get; } = Build();

    /// <summary>The fixture this subject was read from.</summary>
    internal CategoryFixture Fixture { get; }

    /// <summary>The validated envelope of the authored document.</summary>
    internal DefinitionEnvelope Envelope { get; }

    /// <summary>The kind whose writer this subject exercises.</summary>
    internal DefinitionKind Kind => Fixture.Kind;

    /// <summary>The writer of this subject's kind.</summary>
    internal DefinitionWriter Writer => CategorySchemas.Describe(Kind).Writer;

    /// <summary>The declared field table of this subject's kind.</summary>
    internal DefinitionShape Shape => CategorySchemas.Describe(Kind).Shape;

    /// <summary>
    /// A fresh mutable copy of the authored root object, so a caller can permute it without
    /// affecting any other test.
    /// </summary>
    internal JsonObject AuthoredRoot()
    {
        JsonNode? root = JsonNode.Parse(_authoredJson);
        Assert.That(root, Is.InstanceOf<JsonObject>(), Fixture.Path + " must be a JSON object");
        return (JsonObject)root!;
    }

    /// <summary>The canonical payload of the authored document, unmodified.</summary>
    internal byte[] Canonical()
    {
        return CanonicalOf(AuthoredRoot());
    }

    /// <summary>The canonical payload of <paramref name="root"/> under this subject's writer.</summary>
    internal byte[] CanonicalOf(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        using JsonDocument document = JsonDocument.Parse(root.ToJsonString());
        return Writer.ToCanonicalUtf8(Envelope, document.RootElement);
    }

    /// <summary>The canonical payload of <paramref name="root"/> as a parsed tree.</summary>
    internal JsonObject CanonicalTreeOf(JsonObject root)
    {
        JsonNode? emitted = JsonNode.Parse(Encoding.UTF8.GetString(CanonicalOf(root)));
        Assert.That(
            emitted,
            Is.InstanceOf<JsonObject>(),
            Fixture.Path + " must canonicalize to a JSON object");
        return (JsonObject)emitted!;
    }

    public override string ToString()
    {
        return Kind + " <- " + Fixture.Path;
    }

    private static IReadOnlyList<CanonicalWriterSubject> Build()
    {
        List<CanonicalWriterSubject> subjects = new(CategoryFixtureCorpus.Valid.Count);
        foreach (CategoryFixture fixture in CategoryFixtureCorpus.Valid)
        {
            DefinitionReadResult result = CategoryFixtureCorpus.ReadDefinition(fixture);
            Assert.That(
                result.IsValid,
                Is.True,
                () => fixture.Path + " is in the valid corpus and must read cleanly: "
                    + string.Join("; ", result.Diagnostics));

            subjects.Add(new CanonicalWriterSubject(
                fixture,
                result.Definition!.Envelope,
                Encoding.UTF8.GetString(CategoryFixtureCorpus.Read(fixture.Path))));
        }

        return subjects;
    }
}
