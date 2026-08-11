using System;
using System.Collections.Generic;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Envelope;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// Every declared array in the tree states an order class, through whichever of the two
/// declaration mechanisms declares it.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema baseline
/// gives arrays two treatments and no third one, so an array nobody classified has no
/// defined canonical form. Category field tables are held to that by the compiler:
/// <see cref="DefinitionField.ArrayOf(string, DefinitionField, ArrayOrder)"/> takes the class
/// as a required parameter, so a declaration that omits it does not build.
/// </para>
/// <para>
/// <b>The envelope's two arrays are outside that forcing</b>, which is what this fixture is
/// for. <c>tags</c> and <c>source_refs</c> are declared as entries in
/// <see cref="EnvelopeSchema"/>'s field-to-kind map rather than through the factories, so no
/// compiler diagnostic would notice their class going missing - and they are on every
/// definition of every category, so they are in every canonical payload and every hash. The
/// forcing cannot reach them; a test can.
/// </para>
/// <para>
/// Both walks carry a census, because a coverage walk that silently visits fewer fields
/// passes for the wrong reason: dropping <c>source_refs</c> from the kind map would leave a
/// coverage-only assertion green over the one field that remained.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class DeclaredArrayOrderCoverageTests
{
    /// <summary>
    /// Array fields the envelope declares. Two, and doc 40 § Common definition envelope's
    /// nine-row table is why it is a literal: the envelope is a closed table, so a third array
    /// appearing here is a change to <c>SCH-CNT-001</c> and should have to be written down.
    /// Measured at <c>674376c</c>.
    /// </summary>
    private const int EnvelopeArrayFields = 2;

    /// <summary>
    /// Array fields reachable from the sixteen category field tables, counted once per
    /// category tree they appear in. Measured at <c>674376c</c>.
    /// </summary>
    /// <remarks>
    /// Three more than the 59 <c>ArrayOf</c> and <c>OptionalArrayOf</c> call sites in
    /// <c>src/MechaMiner.Content/Categories/</c>, because a sub-shape shared by two categories
    /// is declared once and walked twice, once per category tree that embeds it:
    /// <c>CombatShapes.Projectile</c>'s <c>snapshot_at_creation</c> under both enemy and boss
    /// (+1), and <c>WeaponSchema.GlobalAttackRateMapping</c>'s two timing arrays under both
    /// weapon and branch (+2).
    /// </remarks>
    private const int CategoryArrayFields = 62;

    [Test]
    public void EveryEnvelopeArrayFieldStatesAnOrderClass()
    {
        IReadOnlyList<string> fields = EnvelopeSchema.ArrayFields;

        Expect.Multiple(() =>
        {
            foreach (string field in EnvelopeSchema.Fields)
            {
                Assert.That(
                    EnvelopeSchema.Declares(field),
                    Is.True,
                    () => "'" + field + "' is in the envelope's declared order and has no declared "
                        + "kind, so nothing knows whether it is an array");
            }

            Assert.That(
                fields,
                Has.Count.EqualTo(EnvelopeArrayFields),
                () => "the envelope declares " + EnvelopeArrayFields
                    + " array fields; a walk over fewer of them would cover the missing one by "
                    + "not looking at it. Walked: " + string.Join(", ", fields));

            foreach (string field in fields)
            {
                Assert.That(
                    EnvelopeSchema.ArrayOrderOf(field),
                    Is.Not.EqualTo(ArrayOrder.Unspecified),
                    () => "envelope field '" + field + "' is an array and must state one of doc "
                        + "40's two array treatments");
            }
        });
    }

    [Test]
    public void EveryCategoryArrayFieldStatesAnOrderClass()
    {
        List<string> walked = new();
        List<string> unclassified = new();

        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            Walk(descriptor.Kind.ToString(), descriptor.Shape, walked, unclassified);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                walked,
                Has.Count.EqualTo(CategoryArrayFields),
                () => "the sixteen field tables declare " + CategoryArrayFields
                    + " array fields between them. If this moved, say in the message why a "
                    + "category gained or lost one, because a shrinking walk covers a field by "
                    + "not visiting it. Walked " + walked.Count + ": "
                    + string.Join(", ", walked));

            Assert.That(
                unclassified,
                Is.Empty,
                () => "these declared arrays state no order class: "
                    + string.Join(", ", unclassified));
        });
    }

    /// <summary>
    /// The envelope's classification is a total function over its array fields and refuses
    /// anything else, so a caller cannot read a class for a field that has none.
    /// </summary>
    [Test]
    public void AFieldThatIsNotAnEnvelopeArrayHasNoOrderClass()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentException>(
                    () => EnvelopeSchema.ArrayOrderOf(EnvelopeSchema.Id)).Message,
                Does.Contain(EnvelopeSchema.Id));
            Assert.That(
                Expect.Throws<ArgumentException>(
                    () => EnvelopeSchema.ArrayOrderOf("not_a_field")).Message,
                Does.Contain("not_a_field"));
        });
    }

    private static void Walk(
        string path,
        DefinitionShape shape,
        List<string> walked,
        List<string> unclassified)
    {
        foreach (DefinitionField field in shape.Fields)
        {
            Visit(path + "." + field.Name, field, walked, unclassified);
        }
    }

    /// <summary>
    /// One declared field, and then whatever it declares beneath it: a nested field table, or
    /// an element declaration, which may itself be an object with a table of its own.
    /// </summary>
    private static void Visit(
        string path,
        DefinitionField field,
        List<string> walked,
        List<string> unclassified)
    {
        if (field.Shape == FieldShape.Array)
        {
            walked.Add(path);
            if (field.Order == ArrayOrder.Unspecified)
            {
                unclassified.Add(path);
            }
        }

        if (field.Nested is not null)
        {
            Walk(path, field.Nested, walked, unclassified);
        }

        if (field.Element is not null)
        {
            Visit(path + "[]", field.Element, walked, unclassified);
        }
    }
}
