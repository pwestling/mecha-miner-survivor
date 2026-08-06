using System;
using System.Collections.Generic;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Categories;

/// <summary>
/// Walks a scanned document against a category's field table at every object depth.
/// </summary>
/// <remarks>
/// <para>
/// This is the generalization of the envelope's own shape pass in
/// <c>EnvelopeReader</c>, which checks nine fields at the root. A category definition
/// nests, so the same three questions - is this property declared, is a required one
/// missing, is the kind the declared one - have to be asked at every level. Asking
/// them only at the root is how <c>additionalProperties: false</c> becomes decoration:
/// the root looks closed while a misspelled key three levels down is silently ignored.
/// </para>
/// <para>
/// The envelope's own nine fields are declared by <see cref="EnvelopeSchema"/> and are
/// accepted at the root without being restated in a category's table, so the two
/// declarations cannot disagree about whether <c>tags</c> is a field.
/// </para>
/// <para>
/// Returns whether the shape is sound enough for the typed value pass to run. A kind
/// mismatch makes it unsound, because deserialization past one produces either an
/// exception with no pointer or a default value that later checks would report as a
/// second, invented fault.
/// </para>
/// </remarks>
public static class DefinitionShapeValidator
{
    /// <summary>Validates one document's domain fields against <paramref name="shape"/>.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static bool Validate(
        DocumentOutline outline,
        DefinitionShape shape,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        return ValidateObject(outline, shape, JsonPointer.Root, context, contentId, bag, isRoot: true);
    }

    private static bool ValidateObject(
        DocumentOutline outline,
        DefinitionShape shape,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag,
        bool isRoot)
    {
        bool sound = true;

        foreach (string name in outline.PropertyNamesAt(pointer))
        {
            if (shape.TryGet(name, out _))
            {
                continue;
            }

            if (isRoot && EnvelopeSchema.Declares(name))
            {
                continue;
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.UnknownField,
                context.SourcePath,
                pointer.AppendProperty(name),
                contentId,
                Describe(shape, isRoot)));
        }

        foreach (DefinitionField field in shape.Fields)
        {
            JsonPointer child = pointer.AppendProperty(field.Name);
            if (!outline.TryGetKind(child, out JsonValueKind kind))
            {
                if (field.IsRequired)
                {
                    bag.Add(ContentDiagnostic.CreateError(
                        ContentDiagnosticCodes.RequiredFieldMissing,
                        context.SourcePath,
                        child,
                        contentId,
                        "'" + field.Name + "' is required by " + shape.Subject
                            + "; absence of an optional field is expressed by omitting the key, so a "
                            + "required field has no absent form"));
                }

                continue;
            }

            sound &= ValidateValue(outline, field, child, context, contentId, bag);
        }

        return sound;
    }

    private static bool ValidateValue(
        DocumentOutline outline,
        DefinitionField field,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag)
    {
        if (!outline.TryGetKind(pointer, out JsonValueKind kind))
        {
            return true;
        }

        if (!field.Accepts(kind))
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.FieldTypeMismatch,
                context.SourcePath,
                pointer,
                contentId,
                "a value here is a JSON " + field.DescribeExpectedKind() + ", not a "
                    + DescribeKind(kind)));
            return false;
        }

        switch (field.Shape)
        {
            case FieldShape.Object:
                return ValidateObject(
                    outline, field.Nested!, pointer, context, contentId, bag, isRoot: false);

            case FieldShape.Array:
                return ValidateArray(outline, field, pointer, context, contentId, bag);

            case FieldShape.ParameterMap:
                // The key vocabulary belongs to a registered descriptor. The codec has
                // already asserted that every key is snake_case and that no value is
                // null, so what is deferred is the vocabulary and nothing else.
                return true;

            default:
                return true;
        }
    }

    private static bool ValidateArray(
        DocumentOutline outline,
        DefinitionField field,
        JsonPointer pointer,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag)
    {
        bool sound = true;
        int count = outline.ElementCount(pointer);
        for (int index = 0; index < count; index++)
        {
            sound &= ValidateValue(
                outline, field.Element!, pointer.AppendIndex(index), context, contentId, bag);
        }

        return sound;
    }

    private static string Describe(DefinitionShape shape, bool isRoot)
    {
        if (!isRoot)
        {
            return shape.DescribeDeclaredFields()
                + "; unknown fields are errors rather than silently ignored, at every object depth "
                + "and not only at the root";
        }

        List<string> names = new(EnvelopeSchema.Fields);
        names.AddRange(shape.FieldNames());
        return shape.Subject + " accepts the nine envelope fields plus its own declared fields: "
            + string.Join(", ", names);
    }

    private static string DescribeKind(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.Object => "object",
            JsonValueKind.Array => "array",
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Null => "null",
            _ => "value",
        };
    }
}
