using System;
using System.Collections.Generic;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Schema;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// Walks a schema document's raw JSON and reports every numeric bound it saw, which of
/// them carry no adjacent <c>x-authority</c>, which sourced or derived authorities state no
/// derivation, and which structural authorities state no rationale.
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
/// It knows exactly one thing about schema structure, and only because it must:
/// <see cref="SubschemaMapKeywords"/>. The value of <c>properties</c> is a map keyed by
/// names the author chose, and every schema keyword is a legal name, so reading that map
/// as a schema object invents bounds and authorities out of property names. Knowing which
/// keys hold such a map costs the walk none of its reach - it still descends into every
/// value - and is the difference between blind to applicators, which is the intent, and
/// blind to what is a keyword at all, which is a defect.
/// </para>
/// <para>
/// Attribution is asked per bound keyword, against the <c>x-authority</c> map's entry for
/// that keyword. It was once asked per subschema - does this object declare a bound, does
/// this object carry an authority - and the two questions are not the same one: a
/// subschema asserting <c>minLength</c> and <c>maxLength</c> satisfied it with a single
/// authority, so the second number was attributed to the first number's provenance.
/// </para>
/// <para>
/// The rationale for a structural bound is asked the same way, of the entry rather than of
/// the subschema. It was once the subschema's <c>description</c> - the identical arity
/// failure one field over, and one this walk could not even have reported per bound, since
/// a <c>description</c> has no bound to point at.
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
    /// <summary>
    /// The keywords whose value is a map from an author-chosen name to a subschema,
    /// rather than a subschema itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Structure-blind means blind to which keys are <em>applicators</em>. It cannot mean
    /// blind to the difference between a schema object and a map keyed by names the
    /// author chose, because every schema keyword is a legal property name: a schema
    /// declaring properties called <c>maximum</c> and <c>x-authority</c> hands the walk an
    /// object with a bound keyword and an authority keyword side by side, and the walk
    /// counts a bound that does not exist and attributes it to an authority that does not
    /// exist. The counted phantom is the fail-open - <c>BoundsSeen</c> is what proves the
    /// gate looked at anything, and property names alone were enough to satisfy it.
    /// </para>
    /// <para>
    /// <c>patternProperties</c> and <c>dependentSchemas</c> are here although the
    /// evaluator refuses them outright. The loader stops at an unimplemented keyword and
    /// this walk deliberately does not, so those positions are reachable here, and a
    /// position the walk reaches is a position it can be confused in.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<string> SubschemaMapKeywords { get; } = new[]
    {
        "properties",
        "$defs",
        "patternProperties",
        "dependentSchemas",
    };

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
            element.TryGetProperty(SchemaAuthority.Keyword, out JsonElement authorities);

            foreach (string keyword in SchemaAuthority.BoundKeywords())
            {
                if (!element.TryGetProperty(keyword, out _))
                {
                    continue;
                }

                result.BoundsSeen++;

                // Per keyword, not per subschema. Asking only whether the subschema
                // carries an authority let one x-authority licence every bound beside it,
                // so an unattributed maxLength added next to an attributed minLength was
                // reported by nothing.
                if (!TryAuthorityFor(authorities, keyword, out JsonElement authority))
                {
                    result.Unattributed.Add(at.AppendProperty(keyword).Value);
                    continue;
                }

                if (CitesASource(authority))
                {
                    if (!StatesDerivation(authority))
                    {
                        result.MissingDerivations.Add(at.AppendProperty(keyword).Value);
                    }
                }
                else if (IsStructural(authority) && !StatesRationale(authority))
                {
                    // Asked of the entry, not of the enclosing subschema. The rationale for
                    // a structural bound used to be the subschema's description, so one
                    // sentence answered for every structural bound beside it and this walk
                    // could not have told which number it was about.
                    result.MissingRationales.Add(at.AppendProperty(keyword).Value);
                }
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                // x-authority's value is a map keyed by bound keyword, so descending into
                // it as a subschema reads "minimum" as a bound the schema asserts. That is
                // the SubschemaMapKeywords confusion again, arriving through the very
                // annotation that answers it: the walk would count a phantom bound and
                // report it unattributed at a pointer inside the annotation.
                if (string.Equals(property.Name, SchemaAuthority.Keyword, StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsSubschemaMap(property))
                {
                    WalkSubschemaMap(property.Value, at.AppendProperty(property.Name), result);
                    continue;
                }

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

    /// <summary>
    /// Whether <paramref name="property"/> holds a map from author-chosen name to
    /// subschema.
    /// </summary>
    /// <remarks>
    /// A non-object value is not a map and is walked normally, so a malformed
    /// <c>"properties": 3</c> or <c>"properties": [...]</c> keeps whatever reach the
    /// blind walk had over it rather than being quietly skipped. Rejecting that shape is
    /// the loader's job.
    /// </remarks>
    private static bool IsSubschemaMap(JsonProperty property)
    {
        if (property.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (string keyword in SubschemaMapKeywords)
        {
            if (string.Equals(property.Name, keyword, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Walks each member of a name-to-subschema map as a subschema, without ever reading
    /// the map's own keys as keywords.
    /// </summary>
    private static void WalkSubschemaMap(JsonElement map, JsonPointer at, Result result)
    {
        foreach (JsonProperty member in map.EnumerateObject())
        {
            Walk(member.Value, at.AppendProperty(member.Name), result);
        }
    }

    /// <summary>
    /// The authority for one bound keyword, out of the <c>x-authority</c> map.
    /// </summary>
    /// <remarks>
    /// A malformed <c>x-authority</c> - absent, a non-object, or an object with no entry
    /// for this keyword - yields no authority, so the bound is reported unattributed. The
    /// walk's job is to notice a bound nobody vouched for; saying precisely how the
    /// annotation is malformed is the loader's.
    /// </remarks>
    private static bool TryAuthorityFor(
        JsonElement authorities,
        string keyword,
        out JsonElement authority)
    {
        if (authorities.ValueKind != JsonValueKind.Object)
        {
            authority = default;
            return false;
        }

        return authorities.TryGetProperty(keyword, out authority);
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

    private static bool IsStructural(JsonElement authority)
    {
        return authority.ValueKind == JsonValueKind.Object
            && authority.TryGetProperty("kind", out JsonElement kind)
            && kind.ValueKind == JsonValueKind.String
            && kind.GetString() == "structural";
    }

    /// <summary>
    /// Whether a structural entry states a rationale of its own that says something.
    /// </summary>
    /// <remarks>
    /// The same shape as <see cref="StatesDerivation"/>, and deliberately so: the two are
    /// the same question asked of the two kinds, and the field a reader must find is decided
    /// by the entry's <c>kind</c> rather than by what happens to be present.
    /// </remarks>
    private static bool StatesRationale(JsonElement authority)
    {
        return authority.TryGetProperty("rationale", out JsonElement rationale)
            && rationale.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(rationale.GetString());
    }

    /// <summary>What one walk saw.</summary>
    internal sealed class Result
    {
        /// <summary>The pointer of every bound with no adjacent authority.</summary>
        internal List<string> Unattributed { get; } = new();

        /// <summary>The pointer of every sourced or derived bound stating no derivation.</summary>
        internal List<string> MissingDerivations { get; } = new();

        /// <summary>The pointer of every structural bound stating no rationale.</summary>
        internal List<string> MissingRationales { get; } = new();

        /// <summary>How many bound keywords the walk passed, attributed or not.</summary>
        internal int BoundsSeen { get; set; }

        /// <summary>How many JSON objects the walk descended into.</summary>
        internal int ObjectsSeen { get; set; }

        /// <summary>How many documents the walk covered.</summary>
        internal int DocumentsSeen { get; set; }
    }
}
