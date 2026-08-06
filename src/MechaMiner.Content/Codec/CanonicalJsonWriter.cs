using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MechaMiner.Content.Codec;

/// <summary>
/// Writes a canonical UTF-8 JSON payload: the exact bytes that are hashed.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline states <b>three separate ordering rules</b>, and this writer implements
/// them as three separate operations on purpose:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Schema-declared field order</b> - <see cref="BeginObject"/> plus the
/// <c>Write*</c> field overloads. The order comes from a reviewed
/// <see cref="SchemaFieldOrder"/>; a field the schema does not declare, or a field
/// written out of declared order, is a programming error and throws.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Lexically sorted dictionary keys</b> - <see cref="WriteSortedDictionary{TValue}"/>.
/// Sorted by <em>key</em>, ordinally. "Lexically" here means ordinal, not culture-
/// aware: a culture-aware sort would make the payload, and therefore the hash, depend
/// on the machine's locale, which doc 40 explicitly forbids.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Authored order preserved</b> - <see cref="WriteOrderedArray{TItem}"/>. A
/// semantically ordered array is data whose order carries meaning; sorting one would
/// change what the definition says. This operation deliberately does no sorting at
/// all.
/// </description>
/// </item>
/// </list>
/// <para>
/// <see cref="WriteIdSet"/> is a fourth, distinct case: doc 40 requires "stable-ID
/// sets in canonical ID order". A set has no keys to sort by and no authored order to
/// preserve, so it is ordered by the ID token itself. At DAT-001 canonical ID order is
/// ordinal order of the token. <c>DAT-006</c> owns the compiled bundle, which doc 40
/// § Compilation pipeline orders "by category and stable ID"; when that lands it
/// extends canonical ID order with category precedence, and this is the one place that
/// changes.
/// </para>
/// <para>
/// The writer never indents. Doc 40: "Human-readable pretty JSON is a separate derived
/// view and is never hashed or loaded as canonical state."
/// </para>
/// </remarks>
public sealed class CanonicalJsonWriter
{
    private readonly Utf8JsonWriter _writer;
    private readonly List<Scope> _scopes = new();

    /// <summary>Wraps a writer. The caller owns <paramref name="writer"/>'s lifetime.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
    public CanonicalJsonWriter(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    /// <summary>Opens the root object under <paramref name="order"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="order"/> is null.</exception>
    public void BeginObject(SchemaFieldOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _writer.WriteStartObject();
        _scopes.Add(new Scope(order));
    }

    /// <summary>Opens a nested object as the value of <paramref name="field"/>.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public void BeginObjectField(string field, SchemaFieldOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        WriteFieldName(field);
        _writer.WriteStartObject();
        _scopes.Add(new Scope(order));
    }

    /// <summary>
    /// Opens an object as an element of an array or as a dictionary entry's value,
    /// where there is no field name to write first.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="order"/> is null.</exception>
    public void BeginObjectValue(SchemaFieldOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);
        _writer.WriteStartObject();
        _scopes.Add(new Scope(order));
    }

    /// <summary>Closes the innermost object.</summary>
    /// <exception cref="InvalidOperationException">No object is open.</exception>
    public void EndObject()
    {
        if (_scopes.Count == 0)
        {
            throw new InvalidOperationException("EndObject was called with no object open");
        }

        _scopes.RemoveAt(_scopes.Count - 1);
        _writer.WriteEndObject();
    }

    /// <summary>Writes a string field.</summary>
    public void WriteString(string field, string value)
    {
        WriteFieldName(field);
        WriteStringValue(value);
    }

    /// <summary>Writes an integer field with no padding.</summary>
    public void WriteInteger(string field, long value)
    {
        WriteFieldName(field);
        WriteIntegerValue(value);
    }

    /// <summary>Writes a finite floating-point field in round-trip form.</summary>
    public void WriteNumber(string field, double value)
    {
        WriteFieldName(field);
        WriteNumberValue(value);
    }

    /// <summary>Writes a boolean field.</summary>
    public void WriteBoolean(string field, bool value)
    {
        WriteFieldName(field);
        _writer.WriteBooleanValue(value);
    }

    /// <summary>
    /// Writes a dictionary field with its entries in ordinal key order (doc 40:
    /// "dictionaries as lexically sorted key entries").
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">Two entries share a key.</exception>
    public void WriteSortedDictionary<TValue>(
        string field,
        IEnumerable<KeyValuePair<string, TValue>> entries,
        Action<CanonicalJsonWriter, TValue> writeValue)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(writeValue);

        List<KeyValuePair<string, TValue>> sorted = new(entries);
        sorted.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));

        for (int index = 1; index < sorted.Count; index++)
        {
            if (string.Equals(sorted[index - 1].Key, sorted[index].Key, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "dictionary key '" + sorted[index].Key + "' occurs twice in field '" + field
                        + "'; a canonical payload cannot represent a duplicate key",
                    nameof(entries));
            }
        }

        WriteFieldName(field);
        _writer.WriteStartObject();
        foreach (KeyValuePair<string, TValue> entry in sorted)
        {
            _writer.WritePropertyName(entry.Key);
            writeValue(this, entry.Value);
        }

        _writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a set of stable IDs in canonical ID order (doc 40: "stable-ID sets in
    /// canonical ID order").
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">An ID occurs twice, which makes it not a set.</exception>
    public void WriteIdSet(string field, IEnumerable<string> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        List<string> sorted = new(ids);
        sorted.Sort(static (left, right) => string.CompareOrdinal(left, right));

        for (int index = 1; index < sorted.Count; index++)
        {
            if (string.Equals(sorted[index - 1], sorted[index], StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "stable ID '" + sorted[index] + "' occurs twice in set field '" + field + "'",
                    nameof(ids));
            }
        }

        WriteFieldName(field);
        _writer.WriteStartArray();
        foreach (string id in sorted)
        {
            _writer.WriteStringValue(id);
        }

        _writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a semantically ordered array in its authored order, unchanged (doc 40:
    /// "semantically ordered arrays in their authored/explicit order").
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public void WriteOrderedArray<TItem>(
        string field,
        IEnumerable<TItem> items,
        Action<CanonicalJsonWriter, TItem> writeItem)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(writeItem);

        WriteFieldName(field);
        _writer.WriteStartArray();
        foreach (TItem item in items)
        {
            writeItem(this, item);
        }

        _writer.WriteEndArray();
    }

    /// <summary>Writes a bare string value, for use inside an array or dictionary.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public void WriteStringValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _writer.WriteStringValue(value);
    }

    /// <summary>Writes a bare integer value, for use inside an array or dictionary.</summary>
    public void WriteIntegerValue(long value)
    {
        _writer.WriteRawValue(CanonicalNumber.Format(value), skipInputValidation: false);
    }

    /// <summary>Writes a bare finite floating-point value, for use inside an array or dictionary.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not finite.</exception>
    public void WriteNumberValue(double value)
    {
        _writer.WriteRawValue(CanonicalNumber.Format(value), skipInputValidation: false);
    }

    /// <summary>Writes a bare boolean value, for use inside an array or dictionary.</summary>
    public void WriteBooleanValue(bool value)
    {
        _writer.WriteBooleanValue(value);
    }

    private void WriteFieldName(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (_scopes.Count == 0)
        {
            throw new InvalidOperationException(
                "field '" + field + "' was written with no object open");
        }

        Scope scope = _scopes[^1];
        int position = scope.Order.PositionOf(field);
        if (position < 0)
        {
            throw new InvalidOperationException(
                "field '" + field + "' is not declared by schema field order '" + scope.Order.Name
                    + "'; the canonical writer emits fields in schema-declared order, so an "
                    + "undeclared field has no canonical position");
        }

        if (position <= scope.LastPosition)
        {
            throw new InvalidOperationException(
                "field '" + field + "' was written out of the order declared by '"
                    + scope.Order.Name + "'; declared order is "
                    + string.Join(", ", scope.Order.Fields));
        }

        scope.LastPosition = position;
        _writer.WritePropertyName(field);
    }

    private sealed class Scope
    {
        internal Scope(SchemaFieldOrder order)
        {
            Order = order;
            LastPosition = -1;
        }

        internal SchemaFieldOrder Order { get; }

        internal int LastPosition { get; set; }
    }
}
