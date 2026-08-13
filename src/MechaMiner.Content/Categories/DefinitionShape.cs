using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Categories;

/// <summary>
/// One object's declared field table, in schema-declared order.
/// </summary>
/// <remarks>
/// <para>
/// Order is part of the contract. <c>docs/technical/40-content-data-and-validation.md</c>
/// § JSON codec and schema baseline: "The canonical writer emits fields in
/// schema-declared order". A field table that carried an unordered set would leave the
/// writer to pick an order, and a picked order is one that can change without anyone
/// deciding it should.
/// </para>
/// <para>
/// Every table is closed: a property the table does not declare is an error at this
/// level, which is <c>additionalProperties: false</c> at every object depth rather than
/// only at the root. The one exception is <see cref="FieldShape.ParameterMap"/>, whose
/// keys are declared by a registered descriptor instead, and which is a distinct shape
/// precisely so that "open here" is a decision a reader can see rather than the absence
/// of one.
/// </para>
/// </remarks>
public sealed class DefinitionShape
{
    private readonly Dictionary<string, DefinitionField> _byName;

    private DefinitionShape(string subject, IReadOnlyList<DefinitionField> fields)
    {
        Subject = subject;
        Fields = new ReadOnlyCollection<DefinitionField>(new List<DefinitionField>(fields));

        _byName = new Dictionary<string, DefinitionField>(fields.Count, StringComparer.Ordinal);
        foreach (DefinitionField field in fields)
        {
            if (!_byName.TryAdd(field.Name, field))
            {
                throw new InvalidOperationException(
                    subject + " declares the field '" + field.Name + "' twice");
            }
        }
    }

    /// <summary>What this table describes, for a diagnostic.</summary>
    public string Subject { get; }

    /// <summary>The declared fields, in schema-declared order.</summary>
    public IReadOnlyList<DefinitionField> Fields { get; }

    /// <summary>Declares a field table.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is null.</exception>
    /// <exception cref="InvalidOperationException">A field name is declared twice.</exception>
    public static DefinitionShape Of(string subject, params DefinitionField[] fields)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(fields);
        return new DefinitionShape(subject, fields);
    }

    /// <summary>Looks up a declared field.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public bool TryGet(string name, out DefinitionField? field)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _byName.TryGetValue(name, out field);
    }

    /// <summary>The declared field names, in schema-declared order.</summary>
    public IReadOnlyList<string> FieldNames()
    {
        List<string> names = new(Fields.Count);
        foreach (DefinitionField field in Fields)
        {
            names.Add(field.Name);
        }

        return new ReadOnlyCollection<string>(names);
    }

    /// <summary>The canonical emission order of this table's fields.</summary>
    public SchemaFieldOrder Order()
    {
        return new SchemaFieldOrder(Subject, FieldNames());
    }

    /// <summary>Renders the declared names for a diagnostic.</summary>
    public string DescribeDeclaredFields()
    {
        return Subject + " declares exactly these fields: " + string.Join(", ", FieldNames());
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Subject;
    }
}
