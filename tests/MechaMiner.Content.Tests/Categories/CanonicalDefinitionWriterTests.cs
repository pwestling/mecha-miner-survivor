using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Envelope;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// Every declared category has a canonical writer, and each one emits its kind's fields in the
/// order that kind's schema declares.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema baseline:
/// "The canonical writer emits fields in schema-declared order". Doc 40 does not say how many
/// writers there are - it names catalog sections, and
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Schema registry lumps
/// thirteen names into one <c>SCH-CNT-002</c> row - so sixteen is derived, and
/// <see cref="TheSixteenWritersAreSixteenByThreeIndependentCounts"/> derives it three ways that
/// would have to fail together.
/// </para>
/// <para>
/// This fixture owns the field-order half of the ordering contract;
/// <c>CanonicalArrayOrderGateTests</c> owns the array-order half.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CanonicalDefinitionWriterTests
{
    /// <summary>
    /// The number of canonical writers, which is the number of declared definition kinds.
    /// </summary>
    /// <remarks>
    /// A literal, because all three counts below are derived from things in this repository and
    /// a change that moved all three together - adding a kind with a schema and a field table -
    /// should still have to be written down. Measured at <c>8e45064</c>.
    /// </remarks>
    private const int DeclaredWriters = 16;

    /// <summary>
    /// Sixteen, counted three independent ways: the rows of the category registry, the members
    /// of <see cref="DefinitionKind"/> excluding the reserved zero, and the schema documents
    /// under <c>content/schemas/</c> excluding the envelope's.
    /// </summary>
    /// <remarks>
    /// Three counts and not one, because each is a different kind of evidence and each can go
    /// wrong on its own: a kind can be declared in the enum and never registered, a schema file
    /// can be added with no field table behind it, and a registry row can be deleted while both
    /// the enum member and the schema file stay. Agreement between the three is the claim; the
    /// literal is what makes the agreement have to be at sixteen rather than at whatever they
    /// happen to agree on.
    /// </remarks>
    [Test]
    public void TheSixteenWritersAreSixteenByThreeIndependentCounts()
    {
        int registryRows = CategorySchemas.All.Count;

        List<DefinitionKind> realKinds = new();
        foreach (DefinitionKind kind in Enum.GetValues<DefinitionKind>())
        {
            if (kind != DefinitionKind.Unspecified)
            {
                realKinds.Add(kind);
            }
        }

        List<string> categorySchemaFiles = new();
        foreach (string path in Directory.GetFiles(SchemaDirectory(), "*.schema.json"))
        {
            string name = Path.GetFileName(path);
            if (!string.Equals(name, "envelope.schema.json", StringComparison.Ordinal))
            {
                categorySchemaFiles.Add(name);
            }
        }

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                DeclaredWriters, registryRows, "rows in CategorySchemas.All");
            NumericAssert.AreExactlyEqual(
                DeclaredWriters,
                realKinds.Count,
                "DefinitionKind members excluding the reserved Unspecified");
            NumericAssert.AreExactlyEqual(
                DeclaredWriters,
                categorySchemaFiles.Count,
                "content/schemas/*.schema.json excluding the envelope's, found: "
                    + string.Join(", ", categorySchemaFiles));
        });
    }

    /// <summary>
    /// Each declared kind has a writer of its own, bound to its own field table.
    /// </summary>
    /// <remarks>
    /// The distinctness assertion is the one with teeth. Sixteen non-null writers could all be
    /// the same instance over one shape, and every ordering assertion in the suite would still
    /// pass for whichever kind that shape belonged to while proving nothing about the other
    /// fifteen.
    /// </remarks>
    [Test]
    public void EveryDeclaredKindHasItsOwnWriterBoundToItsOwnFieldTable()
    {
        HashSet<DefinitionWriter> distinct = new();

        Expect.Multiple(() =>
        {
            foreach (CategoryDescriptor descriptor in CategorySchemas.All)
            {
                DefinitionWriter writer = descriptor.Writer;
                distinct.Add(writer);

                Assert.That(
                    writer.Kind,
                    Is.EqualTo(descriptor.Kind),
                    () => descriptor.Kind + "'s writer must report that kind");
                Assert.That(
                    writer.Shape,
                    Is.SameAs(descriptor.Shape),
                    () => descriptor.Kind + "'s writer must be bound to the same field table the "
                        + "registry declares, not to a copy of it");
            }

            NumericAssert.AreExactlyEqual(
                DeclaredWriters,
                distinct.Count,
                "distinct writer instances; sixteen references to one writer would leave "
                    + "fifteen kinds unexercised by every test that ranges over the registry");
        });
    }

    /// <summary>
    /// A definition's canonical field order is the envelope's nine followed by the kind's own
    /// domain fields, each in its declared order.
    /// </summary>
    [Test]
    public void EachWritersRootOrderIsTheEnvelopeThenTheDomainFields()
    {
        Expect.Multiple(() =>
        {
            foreach (CategoryDescriptor descriptor in CategorySchemas.All)
            {
                List<string> expected = new(EnvelopeSchema.Fields);
                expected.AddRange(descriptor.Shape.FieldNames());

                Assert.That(
                    descriptor.Writer.RootOrder.Fields,
                    Is.EqualTo(expected),
                    () => descriptor.Kind + "'s root order must be doc 40's nine envelope rows "
                        + "then " + descriptor.Shape.Subject + "'s fields");
            }
        });
    }

    /// <summary>
    /// Every valid fixture writes a canonical payload whose properties appear in the kind's
    /// declared order.
    /// </summary>
    /// <remarks>
    /// The comparison is against the declared order <em>filtered to the properties the payload
    /// carries</em>, not against the whole declared order: an absent optional domain field is
    /// omitted rather than defaulted, which
    /// <see cref="DefinitionWriter"/> documents and this test therefore has to allow. The
    /// assertion that survives is the one doc 40 states - relative order - and a payload with a
    /// property out of declared order could not have been written at all, because the writer
    /// throws on one.
    /// </remarks>
    [Test]
    public void EveryValidFixtureIsEmittedInItsKindsDeclaredFieldOrder()
    {
        Assert.That(
            CanonicalWriterSubject.All,
            Is.Not.Empty,
            "an empty subject corpus makes every loop over it prove nothing");

        Expect.Multiple(() =>
        {
            foreach (CanonicalWriterSubject subject in CanonicalWriterSubject.All)
            {
                JsonObject emitted = subject.CanonicalTreeOf(subject.AuthoredRoot());

                List<string> emittedNames = new();
                foreach (KeyValuePair<string, JsonNode?> property in emitted)
                {
                    emittedNames.Add(property.Key);
                }

                List<string> declaredAndPresent = new();
                foreach (string name in subject.Writer.RootOrder.Fields)
                {
                    if (emittedNames.Contains(name))
                    {
                        declaredAndPresent.Add(name);
                    }
                }

                Assert.That(
                    emittedNames,
                    Is.EqualTo(declaredAndPresent),
                    () => subject + " must emit its properties in declared order. Emitted: "
                        + string.Join(", ", emittedNames));
                Assert.That(
                    emittedNames,
                    Has.Count.GreaterThan(EnvelopeSchema.Fields.Count),
                    () => subject + " must emit domain fields as well as the envelope's nine, "
                        + "or this fixture is asserting order over the envelope alone");
            }
        });
    }

    /// <summary>
    /// Every valid fixture's canonical payload contains no <c>null</c> and hashes to a digest.
    /// </summary>
    /// <remarks>
    /// Doc 40 § JSON codec and schema baseline makes a JSON <c>null</c> a codec error, so a
    /// canonical payload containing one would be a payload the codec itself rejects. The digest
    /// assertion is the smoke test that the bytes are hashable at all, which is what the bundle
    /// will do with them.
    /// </remarks>
    [Test]
    public void EveryValidFixtureCanonicalizesToHashableBytesWithNoNulls()
    {
        Expect.Multiple(() =>
        {
            foreach (CanonicalWriterSubject subject in CanonicalWriterSubject.All)
            {
                string payload = Encoding.UTF8.GetString(subject.Canonical());

                Assert.That(
                    payload,
                    Does.Not.Contain("null"),
                    () => subject + "'s canonical payload must contain no null");
                Assert.That(
                    subject.Writer.CanonicalSha256Hex(
                        subject.Envelope,
                        JsonDocument.Parse(payload).RootElement),
                    Does.Match("^[0-9a-f]{64}$"),
                    () => subject + "'s canonical payload must hash");
            }
        });
    }

    /// <summary>
    /// No category field table redeclares an envelope field, so no property has two canonical
    /// positions in a definition document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The root order is the envelope's names concatenated with a kind's own, so a kind that
    /// declared <c>tags</c> itself would put that name in the list twice.
    /// <see cref="SchemaFieldOrder"/> refuses a repeated name, which means such a kind cannot
    /// be registered at all - the failure is a <see cref="TypeInitializationException"/> out of
    /// <see cref="CategorySchemas"/> naming nothing useful. This test states the property up
    /// front so the diagnosis is a sentence rather than an archaeology exercise.
    /// </para>
    /// <para>
    /// The second half is the negative control on the mechanism: the same construction with a
    /// name repeated really does throw, so the protection above is a protection and not an
    /// assumption.
    /// </para>
    /// </remarks>
    [Test]
    public void NoCategoryFieldTableRedeclaresAnEnvelopeField()
    {
        Expect.Multiple(() =>
        {
            foreach (CategoryDescriptor descriptor in CategorySchemas.All)
            {
                foreach (string name in descriptor.Shape.FieldNames())
                {
                    Assert.That(
                        EnvelopeSchema.Declares(name),
                        Is.False,
                        () => descriptor.Kind + " declares '" + name + "', which the envelope "
                            + "already declares; the property would have two canonical "
                            + "positions in one document");
                }
            }

            List<string> repeated = new(EnvelopeSchema.Fields) { EnvelopeSchema.Tags };
            ArgumentException error = Expect.Throws<ArgumentException>(
                () => new SchemaFieldOrder("a repeated name", repeated));
            Assert.That(
                error.Message,
                Does.Contain(EnvelopeSchema.Tags),
                "the mechanism that would catch a redeclaration must be able to fire");
        });
    }

    private static string SchemaDirectory()
    {
        return Path.Combine(TestArtifacts.RepositoryRoot, "content", "schemas");
    }
}
