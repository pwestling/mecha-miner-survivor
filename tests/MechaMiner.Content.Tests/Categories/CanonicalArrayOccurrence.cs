using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// One array a definition document actually carries, paired with the declaration that says
/// which of doc 40's two treatments it gets.
/// </summary>
/// <remarks>
/// <para>
/// A <em>declared</em> array field and an <em>occurrence</em> of one are different things, and
/// the difference is what the ordering gate has to range over. <c>enemy_ids</c> is one
/// declaration and fifteen occurrences in <c>content/encounters/</c>, one per formation event,
/// each with its own elements and its own arity. A gate that ranged over declarations would
/// permute one of the fifteen and report the field covered.
/// </para>
/// <para>
/// The node is a live reference into the tree it was walked from, so a caller permutes the
/// array in place and re-canonicalizes the same root. That is why <see cref="Of"/> takes a
/// mutable <see cref="JsonObject"/> rather than a <c>JsonElement</c>.
/// </para>
/// </remarks>
internal sealed class CanonicalArrayOccurrence
{
    private CanonicalArrayOccurrence(string path, DefinitionField field, JsonArray node)
    {
        Path = path;
        Field = field;
        Node = node;
    }

    /// <summary>Where in the document this array is, as a slash-separated path.</summary>
    internal string Path { get; }

    /// <summary>The field table entry that declares this array, including its order class.</summary>
    internal DefinitionField Field { get; }

    /// <summary>The array itself, live in the tree it was walked from.</summary>
    internal JsonArray Node { get; }

    /// <summary>Which of doc 40's two treatments this array's declaration states.</summary>
    internal ArrayOrder Order => Field.Order;

    /// <summary>The number of elements the document authored here.</summary>
    internal int Arity => Node.Count;

    /// <summary>
    /// True when reversing this array produces a genuinely different array, which is the
    /// cardinality condition every "reordering changes the output" assertion depends on.
    /// </summary>
    /// <remarks>
    /// Arity two is necessary and not sufficient: <c>["a", "a"]</c> has two elements and one
    /// ordering, so a reversal of it is the identity and any assertion that the output changed
    /// would be false rather than vacuous. Naming the condition this way rather than as
    /// <c>Arity >= 2</c> is doc 91 § Reach and arity applied literally - the guarded thing is
    /// two elements that <em>differ</em>.
    /// </remarks>
    internal bool IsReorderable
    {
        get
        {
            if (Node.Count < 2)
            {
                return false;
            }

            List<string> texts = ElementTexts();
            for (int index = 0; index < texts.Count; index++)
            {
                if (!string.Equals(
                        texts[index],
                        texts[texts.Count - 1 - index],
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Every array in <paramref name="root"/> that <paramref name="shape"/> declares, at every
    /// depth, in declared order.
    /// </summary>
    /// <remarks>
    /// A parameter map's contents are deliberately not walked: its keys belong to a registered
    /// descriptor rather than to a field table, so an array inside one is not a declared array
    /// field and has no declared order class to hold it to. <c>DefinitionWriter</c> records what
    /// it does with those instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    internal static IReadOnlyList<CanonicalArrayOccurrence> Of(
        DefinitionShape shape,
        JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(shape);
        ArgumentNullException.ThrowIfNull(root);

        List<CanonicalArrayOccurrence> found = new();
        WalkObject(string.Empty, shape, root, found);
        return found;
    }

    /// <summary>The JSON text of each element, in the order they currently appear.</summary>
    internal List<string> ElementTexts()
    {
        List<string> texts = new(Node.Count);
        foreach (JsonNode? element in Node)
        {
            texts.Add(element?.ToJsonString() ?? "null");
        }

        return texts;
    }

    /// <summary>Reverses this array's elements in place, reusing the very same nodes.</summary>
    /// <remarks>
    /// The elements are detached and re-added rather than cloned, deliberately. An occurrence
    /// list may contain occurrences nested <em>inside</em> this array, and those hold live
    /// references to the element nodes; cloning would leave them pointing at detached copies and
    /// every later permutation in the same document would silently be applied to a tree nobody
    /// canonicalizes. Reversing twice therefore also restores the exact original tree, which is
    /// what lets one document serve every occurrence in it.
    /// </remarks>
    internal void Reverse()
    {
        List<JsonNode?> reversed = new(Node.Count);
        for (int index = Node.Count - 1; index >= 0; index--)
        {
            JsonNode? element = Node[index];
            Node.RemoveAt(index);
            reversed.Add(element);
        }

        foreach (JsonNode? element in reversed)
        {
            Node.Add(element);
        }
    }

    public override string ToString()
    {
        return Path + " (" + Order + ", arity " + Arity + ")";
    }

    private static void WalkObject(
        string path,
        DefinitionShape shape,
        JsonObject value,
        List<CanonicalArrayOccurrence> found)
    {
        foreach (DefinitionField field in shape.Fields)
        {
            if (value.TryGetPropertyValue(field.Name, out JsonNode? property)
                && property is not null)
            {
                Visit(path + "/" + field.Name, field, property, found);
            }
        }
    }

    private static void Visit(
        string path,
        DefinitionField field,
        JsonNode node,
        List<CanonicalArrayOccurrence> found)
    {
        if (field.Shape == FieldShape.Array && node is JsonArray array)
        {
            found.Add(new CanonicalArrayOccurrence(path, field, array));

            DefinitionShape? elementShape = field.Element?.Nested;
            if (elementShape is not null)
            {
                for (int index = 0; index < array.Count; index++)
                {
                    if (array[index] is JsonObject element)
                    {
                        WalkObject(path + "/" + index, elementShape, element, found);
                    }
                }
            }

            return;
        }

        if (field.Shape == FieldShape.Object && field.Nested is not null && node is JsonObject nested)
        {
            WalkObject(path, field.Nested, nested, found);
        }
    }
}
