using System.Collections.Generic;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Schema;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// Walks a schema document's raw JSON and reports every numeric bound it saw, which of
/// them carry no adjacent <c>x-authority</c>, and which sourced or derived authorities
/// state no derivation.
/// </summary>
/// <remarks>
/// <para>
/// The walk is deliberately structure-blind: it recurses through every object and array
/// without knowing which keys are applicators. That is the opposite choice from
/// <c>JsonSchemaLoader</c>, which recurses only through the keywords it implements, and
/// the two are kept as separate implementations on purpose. A structure-aware walk can
/// only reach the positions its author enumerated; a structure-blind one reaches a
/// position the loader has not learned about yet. Running both means a bound has to
/// evade two walkers with different blind spots rather than one.
/// </para>
/// <para>
/// <see cref="Result.BoundsSeen"/> and <see cref="Result.ObjectsSeen"/> exist so that
/// "the walk found nothing wrong" can be told apart from "the walk found nothing". A
/// gate that reports success over zero documents, or over documents with zero bounds, is
/// reporting that it did not run.
/// </para>
/// </remarks>
internal static class SchemaBoundWalk
{
    /// <summary>Walks one schema document's bytes.</summary>
    internal static Result Of(byte[] schemaBytes)
    {
        Result result = new();
        using JsonDocument document = JsonDocument.Parse(schemaBytes);
        Walk(document.RootElement, JsonPointer.Root, result);
        return result;
    }

    /// <summary>Walks several documents and accumulates one total.</summary>
    internal static Result OfAll(IEnumerable<byte[]> documents)
    {
        Result total = new();
        foreach (byte[] bytes in documents)
        {
            using JsonDocument document = JsonDocument.Parse(bytes);
            Walk(document.RootElement, JsonPointer.Root, total);
            total.DocumentsSeen++;
        }

        return total;
    }

    private static void Walk(JsonElement element, JsonPointer at, Result result)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            result.ObjectsSeen++;
            bool hasAuthority = element.TryGetProperty(SchemaAuthority.Keyword, out JsonElement authority);
            bool statesDerivation = hasAuthority && StatesDerivation(authority);
            bool citesASource = hasAuthority && CitesASource(authority);

            foreach (string keyword in SchemaAuthority.BoundKeywords())
            {
                if (!element.TryGetProperty(keyword, out _))
                {
                    continue;
                }

                result.BoundsSeen++;
                if (!hasAuthority)
                {
                    result.Unattributed.Add(at.AppendProperty(keyword).Value);
                }
                else if (citesASource && !statesDerivation)
                {
                    result.MissingDerivations.Add(at.AppendProperty(keyword).Value);
                }
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                Walk(property.Value, at.AppendProperty(property.Name), result);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                Walk(item, at.AppendIndex(index), result);
                index++;
            }
        }
    }

    private static bool CitesASource(JsonElement authority)
    {
        return authority.ValueKind == JsonValueKind.Object
            && authority.TryGetProperty("kind", out JsonElement kind)
            && kind.ValueKind == JsonValueKind.String
            && kind.GetString() is "sourced" or "derived";
    }

    private static bool StatesDerivation(JsonElement authority)
    {
        return authority.ValueKind == JsonValueKind.Object
            && authority.TryGetProperty("derivation", out JsonElement derivation)
            && derivation.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(derivation.GetString());
    }

    /// <summary>What one walk saw.</summary>
    internal sealed class Result
    {
        /// <summary>The pointer of every bound with no adjacent authority.</summary>
        internal List<string> Unattributed { get; } = new();

        /// <summary>The pointer of every sourced or derived bound stating no derivation.</summary>
        internal List<string> MissingDerivations { get; } = new();

        /// <summary>How many bound keywords the walk passed, attributed or not.</summary>
        internal int BoundsSeen { get; set; }

        /// <summary>How many JSON objects the walk descended into.</summary>
        internal int ObjectsSeen { get; set; }

        /// <summary>How many documents the walk covered.</summary>
        internal int DocumentsSeen { get; set; }
    }
}
