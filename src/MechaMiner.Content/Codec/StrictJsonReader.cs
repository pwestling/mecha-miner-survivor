using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The strict UTF-8 JSON scanner every source document passes through before any
/// typed model sees it.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline: "Comments, trailing commas, duplicate object properties, nonfinite
/// numbers, and unknown fields are errors." <c>System.Text.Json</c> rejects the
/// first two through <see cref="JsonReaderOptions"/> but reports them as one
/// undifferentiated <see cref="JsonException"/>, and it <em>accepts</em> duplicate
/// properties silently, keeping the last one. Neither behaviour is adequate for a
/// gate, so this scanner detects each fault itself and names it.
/// </para>
/// <para>
/// Unknown fields are not detected here. An unknown field is only unknown relative
/// to a schema, and the codec is schema-neutral; the typed structural validator
/// answers that question using the <see cref="JsonStructure"/> this scan produces.
/// </para>
/// <para>
/// <b>Stages are sequential and short-circuit.</b> Size, then UTF-8 validity, then a
/// lexical pre-scan, then the structural pass. Each stage returns as soon as it finds
/// anything, because a fault in an earlier stage makes every later observation about
/// the same bytes untrustworthy - a comment, for instance, would otherwise also
/// surface as a malformed-JSON fault at a different offset, and an author would have
/// to work out which of the two was the real one.
/// </para>
/// <para>
/// The lexical pre-scan exists because the three faults it finds all occur
/// <em>between</em> values, where <see cref="Utf8JsonReader"/> can only say "invalid
/// start of a value". Classifying them from the exception message would couple this
/// gate to <c>System.Text.Json</c>'s wording; scanning the bytes is deterministic and
/// gives an exact offset.
/// </para>
/// </remarks>
public static class StrictJsonReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    /// <summary>
    /// Scans <paramref name="utf8"/> under <paramref name="policy"/> and reports every
    /// codec-level fault together with the document's shape.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> is null.</exception>
    public static StrictJsonScanResult Scan(ReadOnlySpan<byte> utf8, StrictJsonPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        List<StrictJsonViolation> violations = new();

        if (utf8.Length > policy.Limits.MaximumDocumentBytes)
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.DocumentTooLarge,
                JsonPointer.Root,
                0,
                "the document must be at most "
                    + policy.Limits.MaximumDocumentBytes.ToString(Invariant)
                    + " UTF-8 bytes"));
            return new StrictJsonScanResult(violations, JsonStructure.Empty);
        }

        if (!IsValidUtf8(utf8))
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.InvalidUtf8,
                JsonPointer.Root,
                0,
                "source files are UTF-8 (doc 40 § JSON codec and schema baseline)"));
            return new StrictJsonScanResult(violations, JsonStructure.Empty);
        }

        ScanLexically(utf8, violations);
        if (violations.Count > 0)
        {
            return new StrictJsonScanResult(violations, JsonStructure.Empty);
        }

        JsonStructure structure = ScanStructurally(utf8, policy, violations);
        return new StrictJsonScanResult(violations, structure);
    }

    /// <summary>
    /// True when <paramref name="name"/> is <c>snake_case</c>, meaning it matches
    /// <c>^[a-z][a-z0-9_]*$</c>.
    /// </summary>
    /// <remarks>
    /// The expression is stated here in exactly the form
    /// <c>content/schemas/envelope.schema.json</c> uses for <c>propertyNames</c>, so
    /// the typed validator and the JSON Schema cannot drift apart on what a field
    /// name is.
    /// </remarks>
    public static bool IsSnakeCase(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (name[0] is < 'a' or > 'z')
        {
            return false;
        }

        foreach (char character in name)
        {
            bool allowed = character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_';
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    private static System.Globalization.CultureInfo Invariant =>
        System.Globalization.CultureInfo.InvariantCulture;

    private static bool IsValidUtf8(ReadOnlySpan<byte> utf8)
    {
        try
        {
            StrictUtf8.GetCharCount(utf8);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>
    /// Finds the three faults that live between values: comments, trailing commas,
    /// and the nonfinite literals <c>NaN</c>, <c>Infinity</c>, and <c>-Infinity</c>.
    /// </summary>
    private static void ScanLexically(ReadOnlySpan<byte> utf8, List<StrictJsonViolation> violations)
    {
        bool inString = false;
        bool escaped = false;

        for (int index = 0; index < utf8.Length; index++)
        {
            byte current = utf8[index];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == (byte)'\\')
                {
                    escaped = true;
                }
                else if (current == (byte)'"')
                {
                    inString = false;
                }

                continue;
            }

            switch (current)
            {
                case (byte)'"':
                    inString = true;
                    break;

                case (byte)'/':
                    violations.Add(new StrictJsonViolation(
                        StrictJsonViolationKind.Comment,
                        JsonPointer.Root,
                        index,
                        "a source document contains no comments (doc 40 § JSON codec and schema "
                            + "baseline); record rationale in the owning document, not in the data"));
                    return;

                case (byte)',':
                    int next = SkipWhitespace(utf8, index + 1);
                    if (next < utf8.Length && (utf8[next] == (byte)'}' || utf8[next] == (byte)']'))
                    {
                        violations.Add(new StrictJsonViolation(
                            StrictJsonViolationKind.TrailingComma,
                            JsonPointer.Root,
                            index,
                            "a source document contains no trailing commas (doc 40 § JSON codec and "
                                + "schema baseline)"));
                        return;
                    }

                    break;

                case (byte)'N':
                    if (Matches(utf8, index, "NaN"))
                    {
                        violations.Add(NonfiniteLiteral(index, "NaN"));
                        return;
                    }

                    break;

                case (byte)'I':
                    if (Matches(utf8, index, "Infinity"))
                    {
                        violations.Add(NonfiniteLiteral(index, "Infinity"));
                        return;
                    }

                    break;

                default:
                    break;
            }
        }
    }

    private static StrictJsonViolation NonfiniteLiteral(int index, string literal)
    {
        return new StrictJsonViolation(
            StrictJsonViolationKind.NonfiniteNumber,
            JsonPointer.Root,
            index,
            "the literal '" + literal + "' is not a finite JSON number; nonfinite numbers are "
                + "errors (doc 40 § JSON codec and schema baseline)");
    }

    private static int SkipWhitespace(ReadOnlySpan<byte> utf8, int start)
    {
        int index = start;
        while (index < utf8.Length)
        {
            byte current = utf8[index];
            bool whitespace = current is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';
            if (!whitespace)
            {
                break;
            }

            index++;
        }

        return index;
    }

    private static bool Matches(ReadOnlySpan<byte> utf8, int index, string literal)
    {
        if (index + literal.Length > utf8.Length)
        {
            return false;
        }

        for (int offset = 0; offset < literal.Length; offset++)
        {
            if (utf8[index + offset] != (byte)literal[offset])
            {
                return false;
            }
        }

        return true;
    }

    private static JsonStructure ScanStructurally(
        ReadOnlySpan<byte> utf8,
        StrictJsonPolicy policy,
        List<StrictJsonViolation> violations)
    {
        StrictJsonLimits limits = policy.Limits;
        JsonReaderOptions options = new()
        {
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,

            // Headroom over the policy depth so that the codec's own named
            // depth diagnostic fires before System.Text.Json throws an
            // undifferentiated one.
            MaxDepth = limits.MaximumDepth + DepthHeadroom,
        };

        Utf8JsonReader reader = new(utf8, options);
        List<JsonNodeInfo> nodes = new();
        List<string> rootPropertyNames = new();
        List<Frame> stack = new();
        long lastOffset = 0;

        try
        {
            while (reader.Read())
            {
                lastOffset = reader.TokenStartIndex;

                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        if (!HandlePropertyName(
                                ref reader, policy, stack, rootPropertyNames, violations))
                        {
                            return new JsonStructure(nodes, rootPropertyNames);
                        }

                        break;

                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        if (!HandleContainerStart(
                                ref reader, policy, stack, nodes, violations))
                        {
                            return new JsonStructure(nodes, rootPropertyNames);
                        }

                        break;

                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        if (stack.Count > 0)
                        {
                            stack.RemoveAt(stack.Count - 1);
                        }

                        if (!AdvanceAfterValue(stack, reader.TokenStartIndex, limits, violations))
                        {
                            return new JsonStructure(nodes, rootPropertyNames);
                        }

                        break;

                    default:
                        if (!HandleScalar(ref reader, policy, stack, nodes, violations))
                        {
                            return new JsonStructure(nodes, rootPropertyNames);
                        }

                        break;
                }

                if (nodes.Count > limits.MaximumNodeCount)
                {
                    violations.Add(new StrictJsonViolation(
                        StrictJsonViolationKind.NodeCountLimitExceeded,
                        JsonPointer.Root,
                        reader.TokenStartIndex,
                        "the document must contain at most "
                            + limits.MaximumNodeCount.ToString(Invariant) + " JSON values"));
                    return new JsonStructure(nodes, rootPropertyNames);
                }
            }
        }
        catch (JsonException exception)
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.MalformedJson,
                JsonPointer.Root,
                lastOffset,
                "the document must be well-formed JSON: " + exception.Message));
            return new JsonStructure(nodes, rootPropertyNames);
        }

        if (nodes.Count == 0)
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.MalformedJson,
                JsonPointer.Root,
                0,
                "the document must contain a JSON value"));
        }
        else if (policy.RequireObjectRoot && nodes[0].Kind != JsonValueKind.Object)
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.RootNotObject,
                JsonPointer.Root,
                0,
                "the root value must be a JSON object"));
        }

        return new JsonStructure(nodes, rootPropertyNames);
    }

    private const int DepthHeadroom = 8;

    private static bool HandlePropertyName(
        ref Utf8JsonReader reader,
        StrictJsonPolicy policy,
        List<Frame> stack,
        List<string> rootPropertyNames,
        List<StrictJsonViolation> violations)
    {
        string name = reader.GetString() ?? string.Empty;
        Frame frame = stack[^1];
        frame.PendingName = name;

        if (stack.Count == 1)
        {
            rootPropertyNames.Add(name);
        }

        if (!frame.Names.Add(name))
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.DuplicateProperty,
                frame.ContainerPointer.AppendProperty(name),
                reader.TokenStartIndex,
                "each property name occurs at most once in an object; System.Text.Json would "
                    + "otherwise keep the last occurrence and discard the first silently"));
        }

        if (policy.RequireSnakeCasePropertyNames && !IsSnakeCase(name))
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.PropertyNameNotSnakeCase,
                frame.ContainerPointer.AppendProperty(name),
                reader.TokenStartIndex,
                "property names use snake_case, matching ^[a-z][a-z0-9_]*$ "
                    + "(doc 40 § JSON codec and schema baseline)"));
        }

        frame.PropertyCount++;
        if (frame.PropertyCount > policy.Limits.MaximumObjectProperties)
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.ObjectPropertyLimitExceeded,
                frame.ContainerPointer,
                reader.TokenStartIndex,
                "an object must have at most "
                    + policy.Limits.MaximumObjectProperties.ToString(Invariant) + " properties"));
            return false;
        }

        return true;
    }

    private static bool HandleContainerStart(
        ref Utf8JsonReader reader,
        StrictJsonPolicy policy,
        List<Frame> stack,
        List<JsonNodeInfo> nodes,
        List<StrictJsonViolation> violations)
    {
        bool isArray = reader.TokenType == JsonTokenType.StartArray;
        JsonPointer pointer = ValuePointer(stack);
        nodes.Add(new JsonNodeInfo(
            pointer,
            isArray ? JsonValueKind.Array : JsonValueKind.Object));

        if (stack.Count + 1 > policy.Limits.MaximumDepth)
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.DepthLimitExceeded,
                pointer,
                reader.TokenStartIndex,
                "nesting must be at most "
                    + policy.Limits.MaximumDepth.ToString(Invariant)
                    + " levels deep, counting the root value as level 1"));
            return false;
        }

        stack.Add(new Frame(pointer, isArray));
        return true;
    }

    private static bool HandleScalar(
        ref Utf8JsonReader reader,
        StrictJsonPolicy policy,
        List<Frame> stack,
        List<JsonNodeInfo> nodes,
        List<StrictJsonViolation> violations)
    {
        JsonPointer pointer = ValuePointer(stack);

        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                nodes.Add(new JsonNodeInfo(pointer, JsonValueKind.Null));
                violations.Add(new StrictJsonViolation(
                    StrictJsonViolationKind.NullValue,
                    pointer,
                    reader.TokenStartIndex,
                    "a JSON null is never legal in a source definition; express absence by "
                        + "omitting the key (doc 40 § Declared-optional envelope fields)"));
                break;

            case JsonTokenType.String:
                nodes.Add(new JsonNodeInfo(pointer, JsonValueKind.String));
                string value = reader.GetString() ?? string.Empty;
                if (value.Length > policy.Limits.MaximumStringLength)
                {
                    violations.Add(new StrictJsonViolation(
                        StrictJsonViolationKind.StringTooLong,
                        pointer,
                        reader.TokenStartIndex,
                        "a string must be at most "
                            + policy.Limits.MaximumStringLength.ToString(Invariant)
                            + " characters; player-facing prose belongs in the localization "
                            + "catalog (doc 40 § Localization contract)"));
                }

                break;

            case JsonTokenType.Number:
                nodes.Add(new JsonNodeInfo(pointer, JsonValueKind.Number));
                if (!reader.TryGetDouble(out double number) || !double.IsFinite(number))
                {
                    violations.Add(new StrictJsonViolation(
                        StrictJsonViolationKind.NonfiniteNumber,
                        pointer,
                        reader.TokenStartIndex,
                        "a number must have a finite double value; nonfinite numbers are errors "
                            + "(doc 40 § JSON codec and schema baseline)"));
                }

                break;

            case JsonTokenType.True:
                nodes.Add(new JsonNodeInfo(pointer, JsonValueKind.True));
                break;

            case JsonTokenType.False:
                nodes.Add(new JsonNodeInfo(pointer, JsonValueKind.False));
                break;

            default:
                break;
        }

        return AdvanceAfterValue(stack, reader.TokenStartIndex, policy.Limits, violations);
    }

    /// <summary>
    /// Moves the enclosing array's cursor on after a completed value, and enforces the
    /// array element ceiling at the point the ceiling is crossed.
    /// </summary>
    private static bool AdvanceAfterValue(
        List<Frame> stack,
        long byteOffset,
        StrictJsonLimits limits,
        List<StrictJsonViolation> violations)
    {
        if (stack.Count == 0)
        {
            return true;
        }

        Frame frame = stack[^1];
        if (!frame.IsArray)
        {
            return true;
        }

        frame.Index++;
        if (frame.Index > limits.MaximumArrayElements)
        {
            violations.Add(new StrictJsonViolation(
                StrictJsonViolationKind.ArrayElementLimitExceeded,
                frame.ContainerPointer,
                byteOffset,
                "an array must have at most "
                    + limits.MaximumArrayElements.ToString(Invariant) + " elements"));
            return false;
        }

        return true;
    }

    private static JsonPointer ValuePointer(List<Frame> stack)
    {
        if (stack.Count == 0)
        {
            return JsonPointer.Root;
        }

        Frame frame = stack[^1];
        return frame.IsArray
            ? frame.ContainerPointer.AppendIndex(frame.Index)
            : frame.ContainerPointer.AppendProperty(frame.PendingName ?? string.Empty);
    }

    /// <summary>One open container on the scan stack.</summary>
    private sealed class Frame
    {
        internal Frame(JsonPointer containerPointer, bool isArray)
        {
            ContainerPointer = containerPointer;
            IsArray = isArray;
            Names = new HashSet<string>(StringComparer.Ordinal);
        }

        internal JsonPointer ContainerPointer { get; }

        internal bool IsArray { get; }

        /// <summary>
        /// Ordinal, because doc 40 § Stable ID policy makes tokens case-sensitive and
        /// two field names differing only in case are two different fields, not one
        /// duplicate.
        /// </summary>
        internal HashSet<string> Names { get; }

        internal string? PendingName { get; set; }

        internal int Index { get; set; }

        internal int PropertyCount { get; set; }
    }
}
