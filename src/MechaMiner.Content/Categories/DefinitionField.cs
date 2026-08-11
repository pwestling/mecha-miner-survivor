using System;
using System.Text.Json;

namespace MechaMiner.Content.Categories;

/// <summary>
/// One declared field in a category's field table: its name, shape, and whether it
/// must be present.
/// </summary>
/// <remarks>
/// <para>
/// A field table is declared once and read by three consumers: the structural pass
/// that rejects unknown and missing fields, the canonical writer that emits fields in
/// schema-declared order, and the test that proves the draft 2020-12 schema declares
/// the same properties in the same order. Declaring it three times is how those three
/// drift apart.
/// </para>
/// <para>
/// <b>Optional does not mean nullable.</b> Doc 40 § Declared-optional envelope fields:
/// absence is expressed by omitting the key, and "a JSON <c>null</c> is never legal
/// anywhere in a source definition". There is therefore no nullability flag here; a
/// field is either present with a value of its declared shape, or absent.
/// </para>
/// </remarks>
public sealed class DefinitionField
{
    private DefinitionField(
        string name,
        FieldShape shape,
        bool isRequired,
        DefinitionShape? nested,
        DefinitionField? element,
        ArrayOrder order)
    {
        Name = name;
        Shape = shape;
        IsRequired = isRequired;
        Nested = nested;
        Element = element;
        Order = order;
    }

    /// <summary>The <c>snake_case</c> property name.</summary>
    public string Name { get; }

    /// <summary>The declared shape.</summary>
    public FieldShape Shape { get; }

    /// <summary>True when the field must be present.</summary>
    public bool IsRequired { get; }

    /// <summary>
    /// The nested field table, present exactly when <see cref="Shape"/> is
    /// <see cref="FieldShape.Object"/>.
    /// </summary>
    public DefinitionShape? Nested { get; }

    /// <summary>
    /// The element declaration, present exactly when <see cref="Shape"/> is
    /// <see cref="FieldShape.Array"/>. Its <see cref="Name"/> is the empty string,
    /// because an array element is addressed by index and has none.
    /// </summary>
    public DefinitionField? Element { get; }

    /// <summary>
    /// Which of doc 40's two array treatments this field's elements get, a real class
    /// exactly when <see cref="Shape"/> is <see cref="FieldShape.Array"/> and
    /// <see cref="ArrayOrder.Unspecified"/> otherwise.
    /// </summary>
    /// <remarks>
    /// The class is declared here rather than decided at the writer because doc 40 gives
    /// the discriminator and no per-field answers: only the field table knows whether an
    /// array of strings is a set of stable IDs or a sequence whose order is its meaning.
    /// Two consumers read it - the canonical writer, which picks the emitting operation,
    /// and the ordering gate, which asserts every declared array is covered by one class
    /// or the other.
    /// </remarks>
    public ArrayOrder Order { get; }

    /// <summary>The JSON value kind a value of this shape must have.</summary>
    /// <remarks>
    /// <see cref="FieldShape.Flag"/> maps to <see cref="JsonValueKind.True"/> by
    /// convention and is compared through <see cref="Accepts"/>, which treats both
    /// boolean kinds as one; returning a single kind keeps the accessor total.
    /// </remarks>
    public JsonValueKind ExpectedKind => Shape switch
    {
        FieldShape.Text => JsonValueKind.String,
        FieldShape.Integer or FieldShape.Number => JsonValueKind.Number,
        FieldShape.Flag => JsonValueKind.True,
        FieldShape.Object or FieldShape.ParameterMap => JsonValueKind.Object,
        FieldShape.Array => JsonValueKind.Array,
        _ => JsonValueKind.Undefined,
    };

    /// <summary>True when <paramref name="kind"/> satisfies this field's shape.</summary>
    public bool Accepts(JsonValueKind kind)
    {
        return Shape == FieldShape.Flag
            ? kind is JsonValueKind.True or JsonValueKind.False
            : kind == ExpectedKind;
    }

    /// <summary>Names the accepted kind for a diagnostic.</summary>
    public string DescribeExpectedKind()
    {
        return Shape switch
        {
            FieldShape.Text => "string",
            FieldShape.Integer => "integer-valued number",
            FieldShape.Number => "number",
            FieldShape.Flag => "boolean",
            FieldShape.Object => "object",
            FieldShape.ParameterMap => "object whose keys a registered descriptor owns",
            FieldShape.Array => "array",
            _ => "value",
        };
    }

    /// <summary>Declares a required string field.</summary>
    public static DefinitionField Text(string name)
    {
        return Scalar(name, FieldShape.Text, isRequired: true);
    }

    /// <summary>Declares an optional string field.</summary>
    public static DefinitionField OptionalText(string name)
    {
        return Scalar(name, FieldShape.Text, isRequired: false);
    }

    /// <summary>Declares a required integer-valued field.</summary>
    public static DefinitionField Integer(string name)
    {
        return Scalar(name, FieldShape.Integer, isRequired: true);
    }

    /// <summary>Declares an optional integer-valued field.</summary>
    public static DefinitionField OptionalInteger(string name)
    {
        return Scalar(name, FieldShape.Integer, isRequired: false);
    }

    /// <summary>Declares a required number field.</summary>
    public static DefinitionField Number(string name)
    {
        return Scalar(name, FieldShape.Number, isRequired: true);
    }

    /// <summary>Declares an optional number field.</summary>
    public static DefinitionField OptionalNumber(string name)
    {
        return Scalar(name, FieldShape.Number, isRequired: false);
    }

    /// <summary>Declares a required boolean field.</summary>
    public static DefinitionField Flag(string name)
    {
        return Scalar(name, FieldShape.Flag, isRequired: true);
    }

    /// <summary>Declares an optional boolean field.</summary>
    public static DefinitionField OptionalFlag(string name)
    {
        return Scalar(name, FieldShape.Flag, isRequired: false);
    }

    /// <summary>Declares a required object field with a nested field table.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="nested"/> is null.</exception>
    public static DefinitionField Object(string name, DefinitionShape nested)
    {
        ArgumentNullException.ThrowIfNull(nested);
        return new DefinitionField(
            Require(name), FieldShape.Object, true, nested, null, ArrayOrder.Unspecified);
    }

    /// <summary>Declares an optional object field with a nested field table.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="nested"/> is null.</exception>
    public static DefinitionField OptionalObject(string name, DefinitionShape nested)
    {
        ArgumentNullException.ThrowIfNull(nested);
        return new DefinitionField(
            Require(name), FieldShape.Object, false, nested, null, ArrayOrder.Unspecified);
    }

    /// <summary>
    /// Declares a required array field whose elements have one declared shape and whose
    /// order class <paramref name="order"/> states.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><paramref name="order"/> has no default, and that is this stream's decision
    /// rather than doc 40's.</b> Doc 40 § JSON codec and schema baseline distinguishes the
    /// two array treatments and says nothing about defaulted parameters or compile-time
    /// forcing, so the contract does not require this and no reader should cite it as
    /// contractual.
    /// </para>
    /// <para>
    /// <b>Why forcing rather than a reserved default.</b> The alternative is in this file's
    /// neighbourhood already: <see cref="FieldShape.Unspecified"/> is a reserved zero, so a
    /// declaration that says nothing about its shape still compiles. That is safe for a
    /// shape, because the structural pass rejects the first authored value it sees and the
    /// omission surfaces immediately. An unstated array order class has no such backstop: a
    /// wrongly ordered array is still well-formed JSON of the declared kind, so the
    /// omission surfaces as a wrong hash rather than as an error, and it surfaces in the
    /// bundle rather than at the declaration. Sixty-one declared arrays needed an answer
    /// and at least one of them has a counter-intuitive one - <c>recipe_pair_material_ids</c>
    /// holds two stable IDs and is ordered, not a set - so the parameter is required and
    /// every declaration states its answer where a reviewer reads it.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="order"/> is <see cref="ArrayOrder.Unspecified"/>, which is reserved
    /// and is not one of the two treatments.
    /// </exception>
    public static DefinitionField ArrayOf(string name, DefinitionField element, ArrayOrder order)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new DefinitionField(
            Require(name), FieldShape.Array, true, null, element, RequireOrder(name, order));
    }

    /// <summary>
    /// Declares an optional array field whose elements have one declared shape and whose
    /// order class <paramref name="order"/> states.
    /// </summary>
    /// <remarks>
    /// Optional changes nothing about the order class: a field the author omitted is still
    /// emitted into the canonical bundle, because doc 40 § Common definition envelope
    /// requires optional fields to have "explicit defaults materialized into the canonical
    /// bundle so runtime never guesses", and an emitted array is ordered one way or the
    /// other. See <see cref="ArrayOf(string, DefinitionField, ArrayOrder)"/> for why the
    /// parameter has no default.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="order"/> is <see cref="ArrayOrder.Unspecified"/>, which is reserved
    /// and is not one of the two treatments.
    /// </exception>
    public static DefinitionField OptionalArrayOf(
        string name,
        DefinitionField element,
        ArrayOrder order)
    {
        ArgumentNullException.ThrowIfNull(element);
        return new DefinitionField(
            Require(name), FieldShape.Array, false, null, element, RequireOrder(name, order));
    }

    /// <summary>Declares an array element of a scalar shape.</summary>
    public static DefinitionField ElementOf(FieldShape shape)
    {
        return new DefinitionField(string.Empty, shape, true, null, null, ArrayOrder.Unspecified);
    }

    /// <summary>Declares an array element that is an object with a nested field table.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="nested"/> is null.</exception>
    public static DefinitionField ElementObject(DefinitionShape nested)
    {
        ArgumentNullException.ThrowIfNull(nested);
        return new DefinitionField(
            string.Empty, FieldShape.Object, true, nested, null, ArrayOrder.Unspecified);
    }

    /// <summary>Declares a required registry-owned parameter map.</summary>
    public static DefinitionField ParameterMap(string name)
    {
        return new DefinitionField(
            Require(name), FieldShape.ParameterMap, true, null, null, ArrayOrder.Unspecified);
    }

    /// <summary>Declares an optional registry-owned parameter map.</summary>
    public static DefinitionField OptionalParameterMap(string name)
    {
        return new DefinitionField(
            Require(name), FieldShape.ParameterMap, false, null, null, ArrayOrder.Unspecified);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Name + ": " + DescribeExpectedKind() + (IsRequired ? " (required)" : " (optional)");
    }

    private static DefinitionField Scalar(string name, FieldShape shape, bool isRequired)
    {
        return new DefinitionField(
            Require(name), shape, isRequired, null, null, ArrayOrder.Unspecified);
    }

    private static ArrayOrder RequireOrder(string name, ArrayOrder order)
    {
        if (order == ArrayOrder.Unspecified)
        {
            throw new ArgumentException(
                "array field '" + name + "' must state an order class; "
                    + nameof(ArrayOrder) + "." + nameof(ArrayOrder.Unspecified)
                    + " is reserved so a default-initialised order is never a real one",
                nameof(order));
        }

        return order;
    }

    private static string Require(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0)
        {
            throw new ArgumentException("a declared field has a name", nameof(name));
        }

        return name;
    }
}
