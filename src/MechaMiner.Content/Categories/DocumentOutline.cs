using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Categories;

/// <summary>
/// The parent-to-child index of a scanned document, so a structural walk can ask
/// "which properties does the object at this pointer have" at any depth.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="JsonStructure"/> answers "what kind is the value at this pointer" and
/// exposes property names for the root only, which is all the envelope needed. A
/// per-category field table is nested, so an unknown-field check has to be able to
/// enumerate an inner object's properties too. This derives that from the node list
/// the codec already produced rather than re-reading the bytes: a second parse would
/// be a second reader with its own escaping rules, and the two would eventually
/// disagree about a property name containing a slash.
/// </para>
/// <para>
/// Child names are recovered by splitting each node's pointer on its final unescaped
/// separator and un-escaping the last token, which is the exact inverse of
/// <see cref="JsonPointer.AppendProperty"/>. Array indices come back as their decimal
/// text and are exposed through <see cref="ElementCount"/> rather than as names,
/// because an index is a position and not a field.
/// </para>
/// </remarks>
public sealed class DocumentOutline
{
    private static readonly IReadOnlyList<string> NoChildren = Array.Empty<string>();

    private readonly Dictionary<JsonPointer, List<string>> _childrenByParent;
    private readonly Dictionary<JsonPointer, JsonValueKind> _kinds;

    private DocumentOutline(
        Dictionary<JsonPointer, List<string>> childrenByParent,
        Dictionary<JsonPointer, JsonValueKind> kinds)
    {
        _childrenByParent = childrenByParent;
        _kinds = kinds;
    }

    /// <summary>Builds the outline of one scanned document.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="structure"/> is null.</exception>
    public static DocumentOutline Of(JsonStructure structure)
    {
        ArgumentNullException.ThrowIfNull(structure);

        Dictionary<JsonPointer, List<string>> children = new();
        Dictionary<JsonPointer, JsonValueKind> kinds = new(structure.Nodes.Count);

        foreach (JsonNodeInfo node in structure.Nodes)
        {
            kinds.TryAdd(node.Location, node.Kind);

            string pointer = node.Location.Value;
            if (pointer.Length == 0)
            {
                continue;
            }

            int separator = pointer.LastIndexOf('/');
            JsonPointer parent = separator <= 0
                ? JsonPointer.Root
                : RebuildParent(pointer[..separator]);
            string token = JsonPointer.UnescapeToken(pointer[(separator + 1)..]);

            if (!children.TryGetValue(parent, out List<string>? names))
            {
                names = new List<string>();
                children[parent] = names;
            }

            names.Add(token);
        }

        return new DocumentOutline(children, kinds);
    }

    /// <summary>
    /// The property names of the object at <paramref name="pointer"/>, in authored
    /// order, or an empty list when the pointer addresses no object.
    /// </summary>
    public IReadOnlyList<string> PropertyNamesAt(JsonPointer pointer)
    {
        return _childrenByParent.TryGetValue(pointer, out List<string>? names)
            ? names
            : NoChildren;
    }

    /// <summary>The number of elements in the array at <paramref name="pointer"/>.</summary>
    /// <remarks>
    /// Counted by probing successive indices rather than by taking the child-list
    /// length, so a sparse or misordered node list cannot report a count that includes
    /// a gap.
    /// </remarks>
    public int ElementCount(JsonPointer pointer)
    {
        int count = 0;
        while (_kinds.ContainsKey(pointer.AppendIndex(count)))
        {
            count++;
        }

        return count;
    }

    /// <summary>Looks up the kind at <paramref name="pointer"/>.</summary>
    public bool TryGetKind(JsonPointer pointer, out JsonValueKind kind)
    {
        return _kinds.TryGetValue(pointer, out kind);
    }

    /// <summary>True when <paramref name="pointer"/> addresses a value in the document.</summary>
    public bool Contains(JsonPointer pointer)
    {
        return _kinds.ContainsKey(pointer);
    }

    private static JsonPointer RebuildParent(string parentText)
    {
        JsonPointer pointer = JsonPointer.Root;
        foreach (string token in parentText.Split('/', StringSplitOptions.None))
        {
            if (token.Length == 0)
            {
                continue;
            }

            if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int index)
                && token == index.ToString(CultureInfo.InvariantCulture))
            {
                pointer = pointer.AppendIndex(index);
                continue;
            }

            pointer = pointer.AppendProperty(JsonPointer.UnescapeToken(token));
        }

        return pointer;
    }
}
