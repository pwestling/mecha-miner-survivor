using System;
using System.Collections.Generic;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Categories;

/// <summary>
/// Emits a declared array through the one canonical operation its declared
/// <see cref="ArrayOrder"/> names.
/// </summary>
/// <remarks>
/// <para>
/// <b>This type exists so that no canonical writer chooses an array operation.</b>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema baseline
/// grants two array treatments, and <see cref="CanonicalJsonWriter"/> implements them as two
/// methods. A writer that picked between those two methods per field would be a second
/// statement of which class each field has, sitting beside the field table's own statement
/// and free to disagree with it - and a disagreement between them is invisible, because both
/// spellings emit well-formed JSON of the declared kind and differ only in the bytes, so it
/// would surface as a wrong hash rather than as an error. Every array in a canonical payload
/// therefore comes through here, and the class comes from the declaration.
/// </para>
/// <para>
/// The two entry points are split by element type rather than by convenience.
/// <see cref="WriteStrings"/> serves both classes;
/// <see cref="WriteItems{TItem}"/> serves only
/// <see cref="ArrayOrder.OrderedArray"/> and refuses <see cref="ArrayOrder.IdSet"/>, because
/// doc 40's set treatment is "stable-ID sets in canonical ID order" and an object or a number
/// has no ID to order by. That refusal is why a field declared
/// <see cref="ArrayOrder.IdSet"/> over non-string elements fails loudly at the first write
/// instead of being sorted by some incidental property of its serialized form.
/// </para>
/// </remarks>
public static class ArrayOrderEmitter
{
    /// <summary>
    /// Writes an array of strings under <paramref name="order"/>: canonical ID order for
    /// <see cref="ArrayOrder.IdSet"/>, authored order unchanged for
    /// <see cref="ArrayOrder.OrderedArray"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="order"/> is <see cref="ArrayOrder.Unspecified"/>, which is reserved and
    /// names no treatment, or an <see cref="ArrayOrder.IdSet"/> repeats an element.
    /// </exception>
    public static void WriteStrings(
        CanonicalJsonWriter writer,
        string field,
        ArrayOrder order,
        IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(values);

        switch (order)
        {
            case ArrayOrder.IdSet:
                writer.WriteIdSet(field, values);
                return;

            case ArrayOrder.OrderedArray:
                writer.WriteOrderedArray(
                    field, values, static (target, value) => target.WriteStringValue(value));
                return;

            default:
                throw Reserved(field, order);
        }
    }

    /// <summary>
    /// Writes an array whose elements are not strings, in its authored order unchanged.
    /// </summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="order"/> is not <see cref="ArrayOrder.OrderedArray"/>. Canonical ID
    /// order is defined over ID tokens, so a set of non-string elements has no canonical
    /// order to be emitted in.
    /// </exception>
    public static void WriteItems<TItem>(
        CanonicalJsonWriter writer,
        string field,
        ArrayOrder order,
        IEnumerable<TItem> items,
        Action<CanonicalJsonWriter, TItem> writeItem)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(writeItem);

        switch (order)
        {
            case ArrayOrder.OrderedArray:
                writer.WriteOrderedArray(field, items, writeItem);
                return;

            case ArrayOrder.IdSet:
                throw new ArgumentException(
                    "array field '" + field + "' is declared " + nameof(ArrayOrder) + "."
                        + nameof(ArrayOrder.IdSet) + " and its elements are not strings; doc 40's "
                        + "set treatment is \"stable-ID sets in canonical ID order\", and an "
                        + "element with no ID token has no place in that order",
                    nameof(order));

            default:
                throw Reserved(field, order);
        }
    }

    private static ArgumentException Reserved(string field, ArrayOrder order)
    {
        return new ArgumentException(
            "array field '" + field + "' was emitted with order " + order + "; "
                + nameof(ArrayOrder) + "." + nameof(ArrayOrder.Unspecified)
                + " is reserved and names neither of doc 40's two array treatments",
            nameof(order));
    }
}
