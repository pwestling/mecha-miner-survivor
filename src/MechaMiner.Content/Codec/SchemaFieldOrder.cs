using System;
using System.Collections.Generic;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The declared field order of one object shape.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline: "The canonical writer emits fields in schema-declared order". Declared
/// order is a property of the schema, fixed once and reviewed when it changes; it is
/// emphatically <em>not</em> alphabetical, and it is not the order the author happened
/// to type. Those are three different things and doc 40 names all three separately.
/// </para>
/// <para>
/// A field order is data, not a comparer, because the writer must be able to reject a
/// field the schema does not declare. A comparer would silently accept one.
/// </para>
/// </remarks>
public sealed class SchemaFieldOrder
{
    private readonly Dictionary<string, int> _positions;

    /// <summary>Declares an order.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is blank, <paramref name="fields"/> is empty, or a
    /// field name repeats.
    /// </exception>
    public SchemaFieldOrder(string name, IEnumerable<string> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("a schema field order must be named", nameof(name));
        }

        List<string> ordered = new(fields);
        if (ordered.Count == 0)
        {
            throw new ArgumentException("a schema field order declares at least one field", nameof(fields));
        }

        _positions = new Dictionary<string, int>(ordered.Count, StringComparer.Ordinal);
        for (int index = 0; index < ordered.Count; index++)
        {
            if (!_positions.TryAdd(ordered[index], index))
            {
                throw new ArgumentException(
                    "field '" + ordered[index] + "' is declared twice in schema field order '"
                        + name + "'",
                    nameof(fields));
            }
        }

        Name = name;
        Fields = ordered;
    }

    /// <summary>The schema shape this order belongs to, used in error messages.</summary>
    public string Name { get; }

    /// <summary>The field names in declared order.</summary>
    public IReadOnlyList<string> Fields { get; }

    /// <summary>The declared position of <paramref name="field"/>, or -1 if undeclared.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is null.</exception>
    public int PositionOf(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return _positions.TryGetValue(field, out int position) ? position : -1;
    }

    /// <summary>True when <paramref name="field"/> is declared by this order.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is null.</exception>
    public bool Declares(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return _positions.ContainsKey(field);
    }
}
