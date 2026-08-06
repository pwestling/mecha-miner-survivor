using System.Collections.Generic;
using System.Text.Json;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// Reports every position in a schema document that holds the JSON <c>null</c> value.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every position, at every depth, whatever the key.</b> The walk descends through
/// objects and arrays without knowing a single schema keyword, so a <c>null</c> under
/// <c>default</c>, inside an <c>enum</c>, inside a <c>const</c>, inside an
/// <c>examples</c> array, or under a key nobody has invented yet is one finding. A scan
/// that enumerated the keywords a <c>null</c> is likely to appear under would be a list
/// of the places somebody already thought of, which is the shape of the hole rather than
/// the check for it.
/// </para>
/// <para>
/// <b>It matches the JSON null value and never the string <c>"null"</c>.</b> Those are
/// two different documents: <c>{"default": null}</c> authors absence as a value, and
/// <c>{"default": "null"}</c> authors a four-character string, which is legal and may
/// well be an enum token. A text search over the file cannot tell them apart, and a check
/// that reported the second would be a check people turn off.
/// </para>
/// <para>
/// Why any of this is a rule: absence is expressed by omitting a key and letting the
/// compiler materialize a documented default, so <c>null</c> is the one value the codec
/// rejects outright. A schema authoring it is authoring the rejected value in the very
/// document that says what is accepted. A whole-tree scan over <c>content/</c> enumerates
/// every file rather than only the definition directories and admits no exception list,
/// so a <c>null</c> here reddens an assertion belonging to somebody else, run somewhere
/// else, naming a file nobody thinks of as content. The point of scanning here as well is
/// that it fails in the change that wrote it.
/// </para>
/// <para>
/// <see cref="Result.NodesVisited"/> and <see cref="Result.DocumentsSeen"/> exist for the
/// same reason the bound walk's counters do: "no nulls found" and "nothing was read" are
/// one sentence to an emptiness assertion, and only one of them means the scan ran.
/// </para>
/// </remarks>
internal static class SchemaNullScan
{
    /// <summary>Scans one document's bytes.</summary>
    internal static Result Of(byte[] documentBytes)
    {
        Result result = new();
        Scan(documentBytes, name: null, result);
        return result;
    }

    /// <summary>Scans several named documents and accumulates one result.</summary>
    /// <param name="documents">
    /// Each document's name - the caller's choice, and a repository-relative path in the
    /// project corpus - paired with its bytes.
    /// </param>
    internal static Result OfAll(IEnumerable<KeyValuePair<string, byte[]>> documents)
    {
        Result total = new();
        foreach (KeyValuePair<string, byte[]> document in documents)
        {
            Scan(document.Value, document.Key, total);
        }

        return total;
    }

    private static void Scan(byte[] documentBytes, string? name, Result result)
    {
        using JsonDocument document = JsonDocument.Parse(documentBytes);
        Walk(document.RootElement, JsonPointer.Root, name, result);
        result.DocumentsSeen++;
    }

    private static void Walk(JsonElement element, JsonPointer at, string? name, Result result)
    {
        result.NodesVisited++;

        // JsonValueKind.Null is the JSON null value. A string whose content happens to be
        // "null" arrives here as JsonValueKind.String and is not a finding.
        if (element.ValueKind == JsonValueKind.Null)
        {
            result.Nulls.Add(name is null ? at.Value : name + "#" + at.Value);
            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                Walk(property.Value, at.AppendProperty(property.Name), name, result);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                Walk(item, at.AppendIndex(index), name, result);
                index++;
            }
        }
    }

    /// <summary>What one scan saw.</summary>
    internal sealed class Result
    {
        /// <summary>
        /// Every position holding a JSON null, as a JSON Pointer - prefixed by the
        /// document's name and a <c>#</c> when the caller named the document.
        /// </summary>
        internal List<string> Nulls { get; } = new();

        /// <summary>How many JSON values the scan looked at, of any kind.</summary>
        internal int NodesVisited { get; set; }

        /// <summary>How many documents the scan read.</summary>
        internal int DocumentsSeen { get; set; }
    }
}
