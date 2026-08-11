using System;
using MechaMiner.Content.Categories;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// A declared array states one of doc 40's two array treatments, and the reserved value
/// is not one of them.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline: the canonical writer emits "stable-ID sets in canonical ID order, and
/// semantically ordered arrays in their authored/explicit order". Which of the two a
/// field gets is a property of the field, so it is declared with the field.
/// </para>
/// <para>
/// The <em>compile-time</em> half of that forcing cannot be asserted from a test: a call
/// that omits the argument does not build, so there is no test binary to run it in. What
/// this fixture holds is the runtime half - that
/// <see cref="ArrayOrder.Unspecified"/> is zero, and that an array declaration naming it
/// is rejected rather than quietly treated as one of the two classes.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class ArrayOrderDeclarationTests
{
    /// <summary>
    /// The same property <see cref="FieldShape.Unspecified"/> has, restated for the new
    /// enum: reordering the members so a real class sits at zero would make a
    /// default-initialised order silently mean something.
    /// </summary>
    [Test]
    public void TheReservedOrderIsZero()
    {
        Expect.Multiple(() =>
        {
            Assert.That((int)ArrayOrder.Unspecified, Is.Zero, "the reserved member is zero");
            Assert.That(
                default(ArrayOrder),
                Is.EqualTo(ArrayOrder.Unspecified),
                "a default-initialised order is the reserved one and never a real class");
        });
    }

    [Test]
    public void ARequiredArrayCannotDeclareTheReservedOrder()
    {
        ArgumentException error = Expect.Throws<ArgumentException>(() =>
            DefinitionField.ArrayOf(
                "enemy_ids",
                DefinitionField.ElementOf(FieldShape.Text),
                ArrayOrder.Unspecified));

        Assert.That(error.Message, Does.Contain("enemy_ids"));
    }

    [Test]
    public void AnOptionalArrayCannotDeclareTheReservedOrder()
    {
        ArgumentException error = Expect.Throws<ArgumentException>(() =>
            DefinitionField.OptionalArrayOf(
                "beacon_rules",
                DefinitionField.ElementOf(FieldShape.Text),
                ArrayOrder.Unspecified));

        Assert.That(error.Message, Does.Contain("beacon_rules"));
    }

    [TestCase(ArrayOrder.IdSet)]
    [TestCase(ArrayOrder.OrderedArray)]
    public void ADeclaredArrayCarriesTheOrderClassItStated(ArrayOrder order)
    {
        DefinitionField required = DefinitionField.ArrayOf(
            "field", DefinitionField.ElementOf(FieldShape.Text), order);
        DefinitionField optional = DefinitionField.OptionalArrayOf(
            "field", DefinitionField.ElementOf(FieldShape.Text), order);

        Expect.Multiple(() =>
        {
            Assert.That(required.Order, Is.EqualTo(order));
            Assert.That(optional.Order, Is.EqualTo(order));
        });
    }

    /// <summary>
    /// Array order does not apply to a field that is not an array, so the reserved value
    /// is what such a field carries. A consumer therefore tests
    /// <see cref="FieldShape.Array"/> before reading
    /// <see cref="DefinitionField.Order"/>, and the pair
    /// (<see cref="FieldShape.Array"/>, <see cref="ArrayOrder.Unspecified"/>) is
    /// unreachable through the factories.
    /// </summary>
    [Test]
    public void AFieldThatIsNotAnArrayCarriesTheReservedOrder()
    {
        Expect.Multiple(() =>
        {
            Assert.That(DefinitionField.Text("name_key").Order, Is.EqualTo(ArrayOrder.Unspecified));
            Assert.That(
                DefinitionField.Integer("schema_version").Order,
                Is.EqualTo(ArrayOrder.Unspecified));
            Assert.That(
                DefinitionField.ElementOf(FieldShape.Text).Order,
                Is.EqualTo(ArrayOrder.Unspecified));
        });
    }
}
