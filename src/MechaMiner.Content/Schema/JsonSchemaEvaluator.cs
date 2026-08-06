using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Schema;

/// <summary>
/// Evaluates a JSON instance against a loaded draft 2020-12 schema.
/// </summary>
/// <remarks>
/// <para>
/// This evaluator exists for one job: to prove that
/// <c>content/schemas/*.schema.json</c> and the project-owned typed validators agree,
/// which <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and
/// schema baseline requires ("a fixture corpus proves the schema and typed validator
/// accept/reject the same structural cases"). It is not a general JSON Schema library
/// and must not become one.
/// </para>
/// <para>
/// The typed validator stays authoritative. If the two ever disagree, the corpus test
/// fails and a human decides which one is wrong; the evaluator never overrides a typed
/// verdict.
/// </para>
/// </remarks>
public static class JsonSchemaEvaluator
{
    /// <summary>
    /// A hard ceiling on <c>$ref</c> following, so a schema that references itself in a
    /// cycle fails instead of running until the stack is gone.
    /// </summary>
    private const int MaximumReferenceDepth = 64;

    /// <summary>Evaluates <paramref name="instance"/> against <paramref name="schema"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="schema"/> is null.</exception>
    public static JsonSchemaEvaluationResult Evaluate(
        JsonSchemaDocument schema,
        JsonElement instance)
    {
        ArgumentNullException.ThrowIfNull(schema);

        List<JsonSchemaError> errors = new();
        EvaluateNode(schema, schema.Root, instance, JsonPointer.Root, errors, 0);
        return new JsonSchemaEvaluationResult(errors);
    }

    private static void EvaluateNode(
        JsonSchemaDocument schema,
        JsonSchemaNode node,
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors,
        int depth)
    {
        if (depth > MaximumReferenceDepth)
        {
            errors.Add(new JsonSchemaError(
                location,
                "$ref",
                "reference depth exceeded " + MaximumReferenceDepth.ToString(
                    CultureInfo.InvariantCulture) + "; the schema is cyclic"));
            return;
        }

        if (node.BooleanSchema is bool accepts)
        {
            if (!accepts)
            {
                errors.Add(new JsonSchemaError(location, "false", "no value is valid here"));
            }

            return;
        }

        if (node.Reference is not null)
        {
            if (!schema.TryResolve(node.Reference, out JsonSchemaNode? target))
            {
                // Load already rejected an unresolvable $ref, so reaching this means the
                // document was constructed without loading. Report rather than assume.
                errors.Add(new JsonSchemaError(
                    location, "$ref", "'" + node.Reference + "' does not resolve"));
                return;
            }

            EvaluateNode(schema, target!, instance, location, errors, depth + 1);
        }

        EvaluateType(node, instance, location, errors);
        EvaluateGeneric(node, instance, location, errors);
        EvaluateString(node, instance, location, errors);
        EvaluateNumber(node, instance, location, errors);
        EvaluateObject(schema, node, instance, location, errors, depth);
        EvaluateArray(schema, node, instance, location, errors, depth);
        EvaluateCombinators(schema, node, instance, location, errors, depth);
    }

    private static void EvaluateType(
        JsonSchemaNode node,
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors)
    {
        if (node.Types is null)
        {
            return;
        }

        foreach (string type in node.Types)
        {
            if (MatchesType(type, instance))
            {
                return;
            }
        }

        errors.Add(new JsonSchemaError(
            location,
            "type",
            "expected " + string.Join(" or ", node.Types) + ", found " + NameOf(instance)));
    }

    private static bool MatchesType(string type, JsonElement instance)
    {
        return type switch
        {
            "object" => instance.ValueKind == JsonValueKind.Object,
            "array" => instance.ValueKind == JsonValueKind.Array,
            "string" => instance.ValueKind == JsonValueKind.String,
            "boolean" => instance.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => instance.ValueKind == JsonValueKind.Null,
            "number" => instance.ValueKind == JsonValueKind.Number,

            // Draft 2020-12 § 6.1.1: "integer" matches a number with zero fractional
            // part, so 1.0 is an integer and 1.5 is not.
            "integer" => instance.ValueKind == JsonValueKind.Number
                && instance.TryGetDouble(out double number)
                && Math.Floor(number) == number,
            _ => false,
        };
    }

    private static void EvaluateGeneric(
        JsonSchemaNode node,
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors)
    {
        if (node.Constant is JsonSchemaScalar constant)
        {
            if (!JsonSchemaScalar.TryFrom(instance, out JsonSchemaScalar actual)
                || actual != constant)
            {
                errors.Add(new JsonSchemaError(
                    location, "const", "expected " + constant));
            }
        }

        if (node.Enumeration is null)
        {
            return;
        }

        if (JsonSchemaScalar.TryFrom(instance, out JsonSchemaScalar value))
        {
            foreach (JsonSchemaScalar candidate in node.Enumeration)
            {
                if (candidate == value)
                {
                    return;
                }
            }
        }

        errors.Add(new JsonSchemaError(
            location,
            "enum",
            "expected one of " + string.Join(", ", node.Enumeration)));
    }

    private static void EvaluateString(
        JsonSchemaNode node,
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors)
    {
        if (instance.ValueKind != JsonValueKind.String)
        {
            return;
        }

        string text = instance.GetString() ?? string.Empty;

        if (node.Pattern is not null && !node.Pattern.IsMatch(text))
        {
            errors.Add(new JsonSchemaError(
                location, "pattern", "'" + text + "' does not match " + node.PatternText));
        }

        if (node.MinLength is null && node.MaxLength is null)
        {
            return;
        }

        // Draft 2020-12 § 6.3.1: length is counted in Unicode code points, not UTF-16
        // code units, so a surrogate pair is one character.
        int length = CountCodePoints(text);

        if (node.MinLength is int min && length < min)
        {
            errors.Add(new JsonSchemaError(
                location,
                "minLength",
                "expected at least " + min.ToString(CultureInfo.InvariantCulture)
                    + " characters, found " + length.ToString(CultureInfo.InvariantCulture)));
        }

        if (node.MaxLength is int max && length > max)
        {
            errors.Add(new JsonSchemaError(
                location,
                "maxLength",
                "expected at most " + max.ToString(CultureInfo.InvariantCulture)
                    + " characters, found " + length.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static int CountCodePoints(string text)
    {
        int count = 0;
        foreach (Rune _ in text.EnumerateRunes())
        {
            count++;
        }

        return count;
    }

    private static void EvaluateNumber(
        JsonSchemaNode node,
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors)
    {
        if (instance.ValueKind != JsonValueKind.Number || !instance.TryGetDouble(out double value))
        {
            return;
        }

        if (node.Minimum is double minimum && value < minimum)
        {
            errors.Add(Bound(location, "minimum", value, minimum, "at least"));
        }

        if (node.Maximum is double maximum && value > maximum)
        {
            errors.Add(Bound(location, "maximum", value, maximum, "at most"));
        }

        if (node.ExclusiveMinimum is double exclusiveMinimum && value <= exclusiveMinimum)
        {
            errors.Add(Bound(location, "exclusiveMinimum", value, exclusiveMinimum, "greater than"));
        }

        if (node.ExclusiveMaximum is double exclusiveMaximum && value >= exclusiveMaximum)
        {
            errors.Add(Bound(location, "exclusiveMaximum", value, exclusiveMaximum, "less than"));
        }

        if (node.MultipleOf is double multiple)
        {
            double quotient = value / multiple;
            if (Math.Abs(quotient - Math.Round(quotient)) > 1e-9)
            {
                errors.Add(new JsonSchemaError(
                    location,
                    "multipleOf",
                    CanonicalNumber.Format(value) + " is not a multiple of "
                        + CanonicalNumber.Format(multiple)));
            }
        }
    }

    private static JsonSchemaError Bound(
        JsonPointer location,
        string keyword,
        double value,
        double bound,
        string relation)
    {
        return new JsonSchemaError(
            location,
            keyword,
            "expected " + relation + " " + CanonicalNumber.Format(bound) + ", found "
                + CanonicalNumber.Format(value));
    }

    private static void EvaluateObject(
        JsonSchemaDocument schema,
        JsonSchemaNode node,
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors,
        int depth)
    {
        if (instance.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (node.Required is not null)
        {
            foreach (string required in node.Required)
            {
                if (!instance.TryGetProperty(required, out _))
                {
                    errors.Add(new JsonSchemaError(
                        location, "required", "'" + required + "' is required"));
                }
            }
        }

        foreach (JsonProperty property in instance.EnumerateObject())
        {
            JsonPointer at = location.AppendProperty(property.Name);

            if (node.PropertyNames is not null)
            {
                // propertyNames validates each NAME as if it were a string instance.
                List<JsonSchemaError> nameErrors = new();
                using (JsonDocument nameDocument = QuoteAsJsonString(property.Name))
                {
                    EvaluateNode(
                        schema,
                        node.PropertyNames,
                        nameDocument.RootElement,
                        at,
                        nameErrors,
                        depth + 1);
                }

                foreach (JsonSchemaError error in nameErrors)
                {
                    errors.Add(new JsonSchemaError(
                        at,
                        "propertyNames",
                        "property name '" + property.Name + "' " + error.Message));
                }
            }

            if (node.Properties is not null
                && node.Properties.TryGetValue(property.Name, out JsonSchemaNode? declared))
            {
                EvaluateNode(schema, declared, property.Value, at, errors, depth + 1);
                continue;
            }

            if (node.AdditionalProperties is not null)
            {
                List<JsonSchemaError> extra = new();
                EvaluateNode(schema, node.AdditionalProperties, property.Value, at, extra, depth + 1);
                if (extra.Count > 0 && node.AdditionalProperties.BooleanSchema == false)
                {
                    errors.Add(new JsonSchemaError(
                        at, "additionalProperties", "'" + property.Name + "' is not declared"));
                }
                else
                {
                    errors.AddRange(extra);
                }
            }
        }
    }

    private static void EvaluateArray(
        JsonSchemaDocument schema,
        JsonSchemaNode node,
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors,
        int depth)
    {
        if (instance.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        int count = instance.GetArrayLength();

        if (node.MinItems is int min && count < min)
        {
            errors.Add(new JsonSchemaError(
                location,
                "minItems",
                "expected at least " + min.ToString(CultureInfo.InvariantCulture)
                    + " items, found " + count.ToString(CultureInfo.InvariantCulture)));
        }

        if (node.MaxItems is int max && count > max)
        {
            errors.Add(new JsonSchemaError(
                location,
                "maxItems",
                "expected at most " + max.ToString(CultureInfo.InvariantCulture)
                    + " items, found " + count.ToString(CultureInfo.InvariantCulture)));
        }

        int index = 0;
        int prefixCount = node.PrefixItems?.Count ?? 0;
        foreach (JsonElement item in instance.EnumerateArray())
        {
            JsonPointer at = location.AppendIndex(index);

            if (index < prefixCount)
            {
                EvaluateNode(schema, node.PrefixItems![index], item, at, errors, depth + 1);
            }
            else if (node.Items is not null)
            {
                // Draft 2020-12 § 10.3.1.2: items applies to elements after prefixItems.
                EvaluateNode(schema, node.Items, item, at, errors, depth + 1);
            }

            index++;
        }

        if (node.UniqueItems)
        {
            EvaluateUniqueItems(instance, location, errors);
        }
    }

    private static void EvaluateUniqueItems(
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement item in instance.EnumerateArray())
        {
            // A decoded string compares by value so that "a" and "a" are one item,
            // which is what JSON structural equality requires; every other kind falls
            // back to raw text, which is exact for the scalars a project schema uses.
            string key = item.ValueKind == JsonValueKind.String
                ? "s:" + item.GetString()
                : "r:" + item.GetRawText();

            if (!seen.Add(key))
            {
                errors.Add(new JsonSchemaError(
                    location.AppendIndex(index),
                    "uniqueItems",
                    "duplicate item"));
            }

            index++;
        }
    }

    private static void EvaluateCombinators(
        JsonSchemaDocument schema,
        JsonSchemaNode node,
        JsonElement instance,
        JsonPointer location,
        List<JsonSchemaError> errors,
        int depth)
    {
        if (node.AllOf is not null)
        {
            foreach (JsonSchemaNode branch in node.AllOf)
            {
                EvaluateNode(schema, branch, instance, location, errors, depth + 1);
            }
        }

        if (node.AnyOf is not null && !AnyBranchAccepts(schema, node.AnyOf, instance, location, depth))
        {
            errors.Add(new JsonSchemaError(
                location, "anyOf", "no branch accepted the value"));
        }

        if (node.OneOf is not null)
        {
            int accepted = CountAcceptingBranches(schema, node.OneOf, instance, location, depth);
            if (accepted != 1)
            {
                errors.Add(new JsonSchemaError(
                    location,
                    "oneOf",
                    "expected exactly one branch to accept the value, "
                        + accepted.ToString(CultureInfo.InvariantCulture) + " did"));
            }
        }

        if (node.Not is not null)
        {
            List<JsonSchemaError> negated = new();
            EvaluateNode(schema, node.Not, instance, location, negated, depth + 1);
            if (negated.Count == 0)
            {
                errors.Add(new JsonSchemaError(
                    location, "not", "the value matched a schema it must not match"));
            }
        }
    }

    private static bool AnyBranchAccepts(
        JsonSchemaDocument schema,
        IReadOnlyList<JsonSchemaNode> branches,
        JsonElement instance,
        JsonPointer location,
        int depth)
    {
        foreach (JsonSchemaNode branch in branches)
        {
            List<JsonSchemaError> branchErrors = new();
            EvaluateNode(schema, branch, instance, location, branchErrors, depth + 1);
            if (branchErrors.Count == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static int CountAcceptingBranches(
        JsonSchemaDocument schema,
        IReadOnlyList<JsonSchemaNode> branches,
        JsonElement instance,
        JsonPointer location,
        int depth)
    {
        int accepted = 0;
        foreach (JsonSchemaNode branch in branches)
        {
            List<JsonSchemaError> branchErrors = new();
            EvaluateNode(schema, branch, instance, location, branchErrors, depth + 1);
            if (branchErrors.Count == 0)
            {
                accepted++;
            }
        }

        return accepted;
    }

    /// <summary>
    /// Wraps a property name as a one-value JSON document holding that string, so
    /// <c>propertyNames</c> can evaluate the name as an instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written with <see cref="Utf8JsonWriter"/> rather than
    /// <c>JsonSerializer.Serialize(name)</c>. The serializer overload with no
    /// <c>JsonTypeInfo</c> goes through the reflection-based contract resolver, and
    /// <c>JsonSerializerIsReflectionEnabledByDefault</c> is a <em>per-application</em>
    /// runtimeconfig property, not a per-assembly one: the same assembly that passes here
    /// throws <c>InvalidOperationException: Reflection-based serialization has been
    /// disabled</c> the moment it is loaded by a host that sets the property false, which
    /// the content-compile verb's host does. A call that works or does not depending on
    /// who loaded the DLL is not a call this layer may make.
    /// </para>
    /// <para>
    /// The writer escapes exactly as the serializer did, so the quoting is unchanged; what
    /// changes is that nothing on this path resolves a contract at runtime.
    /// </para>
    /// </remarks>
    private static JsonDocument QuoteAsJsonString(string value)
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter writer = new(buffer))
        {
            writer.WriteStringValue(value);
        }

        return JsonDocument.Parse(buffer.WrittenMemory);
    }

    private static string NameOf(JsonElement instance)
    {
        return instance.ValueKind switch
        {
            JsonValueKind.Object => "object",
            JsonValueKind.Array => "array",
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Null => "null",
            _ => "undefined",
        };
    }
}
