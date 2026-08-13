using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The shape of one scanned document: which pointers exist, what kind each value is,
/// and the root property names in the order they were authored.
/// </summary>
/// <remarks>
/// <para>
/// A structure is produced as a by-product of <see cref="StrictJsonReader"/>'s single
/// pass, so a caller never re-parses to answer a shape question. It carries no
/// values, which is what keeps it on the right side of doc 40's ban on dynamic JSON
/// in production paths: the typed DTO supplies values, the structure supplies
/// locations.
/// </para>
/// <para>
/// Root property order is retained because it is the only place authored order still
/// exists after the scan, and a diagnostic that reports fields in authored order is
/// easier to reconcile with the file than one that reports them sorted. Canonical
/// output never uses it; that is <see cref="SchemaFieldOrder"/>'s job.
/// </para>
/// </remarks>
public sealed class JsonStructure
{
    private static readonly IReadOnlyList<JsonNodeInfo> NoNodes = Array.Empty<JsonNodeInfo>();
    private static readonly IReadOnlyList<string> NoNames = Array.Empty<string>();

    private readonly Dictionary<JsonPointer, JsonValueKind> _kindsByPointer;

    internal JsonStructure(
        IReadOnlyList<JsonNodeInfo> nodes,
        IReadOnlyList<string> rootPropertyNames)
    {
        Nodes = nodes;
        RootPropertyNames = rootPropertyNames;

        _kindsByPointer = new Dictionary<JsonPointer, JsonValueKind>(nodes.Count);
        foreach (JsonNodeInfo node in nodes)
        {
            // A duplicate pointer can only arise from a duplicate property, which is
            // already a violation; the first occurrence wins so the map stays total.
            _kindsByPointer.TryAdd(node.Location, node.Kind);
        }
    }

    /// <summary>The structure of a document that could not be scanned at all.</summary>
    public static JsonStructure Empty { get; } = new(NoNodes, NoNames);

    /// <summary>Every value in the document, in document order.</summary>
    public IReadOnlyList<JsonNodeInfo> Nodes { get; }

    /// <summary>The root object's property names, in authored order.</summary>
    public IReadOnlyList<string> RootPropertyNames { get; }

    /// <summary>True when <paramref name="pointer"/> addresses a value in the document.</summary>
    public bool Contains(JsonPointer pointer)
    {
        return _kindsByPointer.ContainsKey(pointer);
    }

    /// <summary>Looks up the kind of the value at <paramref name="pointer"/>.</summary>
    public bool TryGetKind(JsonPointer pointer, out JsonValueKind kind)
    {
        return _kindsByPointer.TryGetValue(pointer, out kind);
    }
}
