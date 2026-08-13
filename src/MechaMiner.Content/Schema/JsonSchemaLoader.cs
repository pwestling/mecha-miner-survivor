using System;
using System.Collections.Generic;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Schema;

/// <summary>
/// Parses a draft 2020-12 schema document into an evaluable form, failing loudly on
/// anything it does not implement.
/// </summary>
/// <remarks>
/// Every rejection here happens at <em>load</em> time, before any instance is
/// evaluated. That ordering is the point: a schema whose constraints the evaluator
/// cannot enforce must never be used to declare an instance valid, so the failure has
/// to come before the first verdict rather than as a footnote after it.
/// </remarks>
public static class JsonSchemaLoader
{
    /// <summary>Loads a schema from UTF-8 bytes.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="sourcePath"/> is null.</exception>
    public static JsonSchemaLoadResult Load(ReadOnlySpan<byte> utf8, string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(sourcePath);

        DiagnosticBag bag = new();
        JsonDocument document;
        try
        {
            // A JsonDocument is used here and only here. The brief for this layer is
            // inherently schema-generic - it reads a document whose shape is a schema,
            // not a definition - so there is no typed DTO that would do. Everything a
            // consumer sees is the typed JsonSchemaDocument built below.
            document = JsonDocument.Parse(
                utf8.ToArray(),
                new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Disallow,
                    AllowTrailingCommas = false,
                });
        }
        catch (JsonException exception)
        {
            bag.Add(Malformed(sourcePath, JsonPointer.Root, exception.Message));
            return new JsonSchemaLoadResult(null, bag.Diagnostics);
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            Dictionary<string, JsonSchemaNode> definitions = new(StringComparer.Ordinal);

            // Draft 2020-12 § 4.3.2: a schema is "an object or a boolean", and that
            // applies at the root as much as to a subschema. A boolean root is not
            // useful in a project schema, but rejecting it would be this evaluator
            // disagreeing with the specification rather than implementing a subset of it.
            if (root.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return new JsonSchemaLoadResult(
                    new JsonSchemaDocument(
                        sourcePath,
                        new JsonSchemaNode { BooleanSchema = root.ValueKind == JsonValueKind.True },
                        definitions),
                    bag.Diagnostics);
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                bag.Add(Malformed(
                    sourcePath,
                    JsonPointer.Root,
                    "the root of a schema document is a JSON object or a boolean"));
                return new JsonSchemaLoadResult(null, bag.Diagnostics);
            }

            if (root.TryGetProperty("$defs", out JsonElement defs))
            {
                if (defs.ValueKind != JsonValueKind.Object)
                {
                    bag.Add(Malformed(
                        sourcePath,
                        JsonPointer.Root.AppendProperty("$defs"),
                        "$defs is an object of name to subschema"));
                    return new JsonSchemaLoadResult(null, bag.Diagnostics);
                }

                foreach (JsonProperty definition in defs.EnumerateObject())
                {
                    JsonPointer pointer =
                        JsonPointer.Root.AppendProperty("$defs").AppendProperty(definition.Name);
                    JsonSchemaNode? node = ParseNode(definition.Value, pointer, sourcePath, bag);
                    if (node is not null)
                    {
                        definitions[definition.Name] = node;
                    }
                }
            }

            JsonSchemaNode? rootNode = ParseNode(root, JsonPointer.Root, sourcePath, bag);
            if (rootNode is null || bag.HasErrors)
            {
                return new JsonSchemaLoadResult(null, bag.Diagnostics);
            }

            JsonSchemaDocument schema = new(sourcePath, rootNode, definitions);
            VerifyReferencesResolve(schema, rootNode, JsonPointer.Root, sourcePath, bag);
            foreach (KeyValuePair<string, JsonSchemaNode> definition in definitions)
            {
                VerifyReferencesResolve(
                    schema,
                    definition.Value,
                    JsonPointer.Root.AppendProperty("$defs").AppendProperty(definition.Key),
                    sourcePath,
                    bag);
            }

            return bag.HasErrors
                ? new JsonSchemaLoadResult(null, bag.Diagnostics)
                : new JsonSchemaLoadResult(schema, bag.Diagnostics);
        }
    }

    private static JsonSchemaNode? ParseNode(
        JsonElement element,
        JsonPointer pointer,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return new JsonSchemaNode { BooleanSchema = element.ValueKind == JsonValueKind.True };
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            bag.Add(Malformed(sourcePath, pointer, "a subschema is an object or a boolean"));
            return null;
        }

        JsonSchemaNode node = new();
        bool atRoot = pointer.IsRoot;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            JsonPointer at = pointer.AppendProperty(property.Name);

            if (!JsonSchemaKeywords.IsRecognised(property.Name))
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.SchemaKeywordUnsupported,
                    sourcePath,
                    at,
                    null,
                    "'" + property.Name + "' is not implemented; "
                        + JsonSchemaKeywords.DescribeSupported()));
                continue;
            }

            if (!ApplyKeyword(node, property, at, atRoot, sourcePath, bag))
            {
                return null;
            }
        }

        ValidateAuthorityPlacement(node, element, pointer, sourcePath, bag);
        return node;
    }

    /// <summary>
    /// Enforces the rules that relate <c>x-authority</c> to its siblings, which can only
    /// be checked once the whole subschema has been read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The relation is a bijection: every bound keyword the subschema declares has an
    /// entry in <c>x-authority</c>, and every entry in <c>x-authority</c> names a bound
    /// keyword the subschema declares. Both halves are reported keyword by keyword, so a
    /// subschema with three bounds and two authorities fails naming the third.
    /// </para>
    /// <para>
    /// This used to be a single flag. The subschema was asked only whether it declared
    /// <em>a</em> bound and whether it carried <em>an</em> authority, so one
    /// <c>x-authority</c> licensed every bound beside it: adding an unattributed
    /// <c>maxLength</c> next to an attributed <c>minLength</c> was accepted by the loader
    /// and by the corpus walk together. Provenance is a property of a number, and a
    /// subschema can assert several numbers.
    /// </para>
    /// <para>
    /// Nothing here reads the subschema's <c>description</c>. A structural bound's rationale
    /// used to be checked against it, which is the same arity failure one field over: a
    /// <c>description</c> is per subschema, so one sentence licensed every structural bound
    /// under it. The rationale is now a field of the entry, checked in
    /// <see cref="ReadAuthority"/> where the bound it explains is known by name.
    /// </para>
    /// </remarks>
    private static void ValidateAuthorityPlacement(
        JsonSchemaNode node,
        JsonElement element,
        JsonPointer pointer,
        string sourcePath,
        DiagnosticBag bag)
    {
        List<string> declaredBounds = new();
        foreach (string keyword in SchemaAuthority.BoundKeywords())
        {
            if (element.TryGetProperty(keyword, out _))
            {
                declaredBounds.Add(keyword);
            }
        }

        IReadOnlyDictionary<string, SchemaAuthority> authorities =
            node.Authorities ?? EmptyAuthorities;

        foreach (string keyword in declaredBounds)
        {
            if (authorities.ContainsKey(keyword))
            {
                continue;
            }

            bag.Add(Malformed(
                sourcePath,
                pointer.AppendProperty(keyword),
                "'" + keyword + "' is a numeric bound, so '" + SchemaAuthority.Keyword
                    + "' carries an entry keyed '" + keyword + "' recording where that number "
                    + "came from. An authority on a neighbouring bound does not cover it: "
                    + "without an entry of its own, \"which bounds need re-deriving now that a "
                    + "document changed\" is answerable only from memory"));
        }

        foreach (KeyValuePair<string, SchemaAuthority> attributed in authorities)
        {
            if (!declaredBounds.Contains(attributed.Key))
            {
                bag.Add(Malformed(
                    sourcePath,
                    pointer.AppendProperty(SchemaAuthority.Keyword)
                        .AppendProperty(attributed.Key),
                    "'" + SchemaAuthority.Keyword + "' explains a bound this subschema "
                        + "declares, and this subschema declares no '" + attributed.Key
                        + "'. An authority for a bound that is not there is provenance for "
                        + "nothing, and it would silently cover that bound the day someone "
                        + "adds it"));
            }
        }
    }

    private static readonly IReadOnlyDictionary<string, SchemaAuthority> EmptyAuthorities =
        new Dictionary<string, SchemaAuthority>(StringComparer.Ordinal);

    private static bool ApplyKeyword(
        JsonSchemaNode node,
        JsonProperty property,
        JsonPointer at,
        bool atRoot,
        string sourcePath,
        DiagnosticBag bag)
    {
        JsonElement value = property.Value;
        switch (property.Name)
        {
            case "$schema":
            case "$id":
            case "title":
            case "description":
            case "$comment":
                // Identity and documentation: no assertion, but still a type. An
                // annotation that accepted any JSON value would be a hiding place with a
                // keyword's name on it: {"title":{"if":{"maximum":5}}} loaded clean, and
                // the subschema under it was walked by nothing and evaluated by nothing.
                // That is the same hole a nested $defs was, reached by a shorter route.
                if (value.ValueKind != JsonValueKind.String)
                {
                    bag.Add(Malformed(
                        sourcePath,
                        at,
                        "'" + property.Name + "' is a string; a non-string annotation is a "
                            + "subschema-shaped value that nothing checks"));
                    return false;
                }

                return true;

            case "$defs":
                // The root's $defs is parsed by Load, which needs the resulting map to
                // resolve $ref, so parsing it again here would report every error in it
                // twice. A $defs on a subschema is a different matter: $ref reaches only
                // '#' and '#/$defs/<name>' at the root, so nothing can ever evaluate it -
                // and that is exactly why it has to be walked rather than skipped. An
                // unreachable subschema is the one place an unattributed bound could sit
                // and be seen by nobody: not the evaluator, which never resolves it, and
                // not the reader, who assumes anything under $defs is checked like the
                // rest. Skipping it was a hole in this walker, not a saving.
                if (atRoot)
                {
                    return true;
                }

                node.UnevaluatedDefinitions =
                    ParseUnevaluatedSubschemaMap(value, at, sourcePath, bag);
                return node.UnevaluatedDefinitions is not null;

            case SchemaAuthority.Keyword:
                node.Authorities = ReadAuthorities(value, at, sourcePath, bag);
                return node.Authorities is not null;

            case "$ref":
                if (value.ValueKind != JsonValueKind.String)
                {
                    bag.Add(Malformed(sourcePath, at, "$ref is a string"));
                    return false;
                }

                node.Reference = value.GetString();
                return true;

            case "type":
                node.Types = ReadStringOrStringArray(value, at, sourcePath, bag, "type");
                return node.Types is not null;

            case "required":
                node.Required = ReadStringArray(value, at, sourcePath, bag, "required");
                return node.Required is not null;

            case "properties":
                node.Properties = ReadSubschemaMap(value, at, sourcePath, bag);
                return node.Properties is not null;

            case "additionalProperties":
                node.AdditionalProperties = ParseNode(value, at, sourcePath, bag);
                return node.AdditionalProperties is not null;

            case "propertyNames":
                node.PropertyNames = ParseNode(value, at, sourcePath, bag);
                return node.PropertyNames is not null;

            case "enum":
                node.Enumeration = ReadScalarArray(value, at, sourcePath, bag);
                return node.Enumeration is not null;

            case "const":
                if (!JsonSchemaScalar.TryFrom(value, out JsonSchemaScalar constant))
                {
                    bag.Add(Malformed(
                        sourcePath,
                        at,
                        "const compares by structural equality and this evaluator implements "
                            + "that for scalars only"));
                    return false;
                }

                node.Constant = constant;
                return true;

            case "pattern":
                if (value.ValueKind != JsonValueKind.String)
                {
                    bag.Add(Malformed(sourcePath, at, "pattern is a string"));
                    return false;
                }

                node.PatternText = value.GetString();
                try
                {
                    // Compiled with ECMA-262 anchor semantics so the evaluator and an
                    // external JSON Schema tool agree on what the same pattern accepts.
                    node.Pattern = AnchoredPattern.Compile(node.PatternText!);
                }
                catch (ArgumentException exception)
                {
                    bag.Add(Malformed(
                        sourcePath,
                        at,
                        "pattern is a valid regular expression: " + exception.Message));
                    return false;
                }

                return true;

            case "minLength":
                node.MinLength = ReadNonNegativeInteger(value, at, sourcePath, bag, "minLength");
                return node.MinLength is not null;

            case "maxLength":
                node.MaxLength = ReadNonNegativeInteger(value, at, sourcePath, bag, "maxLength");
                return node.MaxLength is not null;

            case "minItems":
                node.MinItems = ReadNonNegativeInteger(value, at, sourcePath, bag, "minItems");
                return node.MinItems is not null;

            case "maxItems":
                node.MaxItems = ReadNonNegativeInteger(value, at, sourcePath, bag, "maxItems");
                return node.MaxItems is not null;

            case "minimum":
                node.Minimum = ReadNumber(value, at, sourcePath, bag, "minimum");
                return node.Minimum is not null;

            case "maximum":
                node.Maximum = ReadNumber(value, at, sourcePath, bag, "maximum");
                return node.Maximum is not null;

            case "exclusiveMinimum":
                node.ExclusiveMinimum = ReadNumber(value, at, sourcePath, bag, "exclusiveMinimum");
                return node.ExclusiveMinimum is not null;

            case "exclusiveMaximum":
                node.ExclusiveMaximum = ReadNumber(value, at, sourcePath, bag, "exclusiveMaximum");
                return node.ExclusiveMaximum is not null;

            case "multipleOf":
                node.MultipleOf = ReadNumber(value, at, sourcePath, bag, "multipleOf");
                if (node.MultipleOf is not null && node.MultipleOf <= 0)
                {
                    bag.Add(Malformed(sourcePath, at, "multipleOf is greater than zero"));
                    return false;
                }

                return node.MultipleOf is not null;

            case "uniqueItems":
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    bag.Add(Malformed(sourcePath, at, "uniqueItems is a boolean"));
                    return false;
                }

                node.UniqueItems = value.ValueKind == JsonValueKind.True;
                return true;

            case "items":
                node.Items = ParseNode(value, at, sourcePath, bag);
                return node.Items is not null;

            case "prefixItems":
                node.PrefixItems = ReadSubschemaArray(value, at, sourcePath, bag, "prefixItems");
                return node.PrefixItems is not null;

            case "allOf":
                node.AllOf = ReadSubschemaArray(value, at, sourcePath, bag, "allOf");
                return node.AllOf is not null;

            case "anyOf":
                node.AnyOf = ReadSubschemaArray(value, at, sourcePath, bag, "anyOf");
                return node.AnyOf is not null;

            case "oneOf":
                node.OneOf = ReadSubschemaArray(value, at, sourcePath, bag, "oneOf");
                return node.OneOf is not null;

            case "not":
                node.Not = ParseNode(value, at, sourcePath, bag);
                return node.Not is not null;

            default:
                // Unreachable: IsRecognised already gated the name.
                bag.Add(Malformed(sourcePath, at, "keyword '" + property.Name + "' has no parser"));
                return false;
        }
    }

    /// <summary>
    /// Parses every subschema of a <c>$defs</c> declared on a subschema, so that the rules
    /// which do not depend on reachability reach it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The result never becomes evaluable: it goes to
    /// <see cref="JsonSchemaNode.UnevaluatedDefinitions"/> and not to the document's
    /// definition map, so neither of this evaluator's two <c>$ref</c> forms can reach it.
    /// That is the deliberate part, and it stands - a nested <c>$defs</c> is unreachable by
    /// construction. Parsing it anyway is the other deliberate part: a bound nobody
    /// evaluates still has to say where its number came from, because the next person to
    /// make it reachable will not re-derive it.
    /// </para>
    /// <para>
    /// What the nodes may not be is <em>dropped</em>, which is what happened before. The
    /// parse-time rules ran on them and then they were gone, so
    /// <see cref="VerifyReferencesResolve"/> - which runs afterwards over the node graph,
    /// because it needs the finished document to resolve against - never saw them. A
    /// <c>$ref</c> in this position was checked by nobody, which is a hole of a worse kind
    /// than a rule that accepts too much: there was no reader whose rule could have been
    /// wrong.
    /// </para>
    /// </remarks>
    private static Dictionary<string, JsonSchemaNode>? ParseUnevaluatedSubschemaMap(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            bag.Add(Malformed(sourcePath, at, "$defs is an object of name to subschema"));
            return null;
        }

        Dictionary<string, JsonSchemaNode> definitions = new(StringComparer.Ordinal);
        foreach (JsonProperty definition in value.EnumerateObject())
        {
            JsonSchemaNode? node =
                ParseNode(definition.Value, at.AppendProperty(definition.Name), sourcePath, bag);
            if (node is null)
            {
                return null;
            }

            definitions[definition.Name] = node;
        }

        return definitions;
    }

    private static void VerifyReferencesResolve(
        JsonSchemaDocument schema,
        JsonSchemaNode node,
        JsonPointer pointer,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (node.Reference is not null && !schema.TryResolve(node.Reference, out _))
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.SchemaReferenceUnresolved,
                sourcePath,
                pointer.AppendProperty("$ref"),
                null,
                "'" + node.Reference + "' does not resolve; this evaluator supports '#' and "
                    + "'#/$defs/<name>', and the declared definitions are "
                    + (schema.DefinitionNames.Count == 0
                        ? "(none)"
                        : string.Join(", ", schema.DefinitionNames))));
        }

        if (node.Properties is not null)
        {
            foreach (KeyValuePair<string, JsonSchemaNode> property in node.Properties)
            {
                VerifyReferencesResolve(
                    schema,
                    property.Value,
                    pointer.AppendProperty("properties").AppendProperty(property.Key),
                    sourcePath,
                    bag);
            }
        }

        // A $defs on a subschema. Nothing evaluates these, and that is exactly why they are
        // walked here: an unreachable node is where a dangling reference sits unseen, since
        // the reader who would have caught it is the one that only visits what a $ref can
        // reach.
        if (node.UnevaluatedDefinitions is not null)
        {
            foreach (KeyValuePair<string, JsonSchemaNode> definition in node.UnevaluatedDefinitions)
            {
                VerifyReferencesResolve(
                    schema,
                    definition.Value,
                    pointer.AppendProperty("$defs").AppendProperty(definition.Key),
                    sourcePath,
                    bag);
            }
        }

        VerifyChild(schema, node.AdditionalProperties, pointer, "additionalProperties", sourcePath, bag);
        VerifyChild(schema, node.PropertyNames, pointer, "propertyNames", sourcePath, bag);
        VerifyChild(schema, node.Items, pointer, "items", sourcePath, bag);
        VerifyChild(schema, node.Not, pointer, "not", sourcePath, bag);
        VerifyChildren(schema, node.PrefixItems, pointer, "prefixItems", sourcePath, bag);
        VerifyChildren(schema, node.AllOf, pointer, "allOf", sourcePath, bag);
        VerifyChildren(schema, node.AnyOf, pointer, "anyOf", sourcePath, bag);
        VerifyChildren(schema, node.OneOf, pointer, "oneOf", sourcePath, bag);
    }

    private static void VerifyChild(
        JsonSchemaDocument schema,
        JsonSchemaNode? child,
        JsonPointer pointer,
        string keyword,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (child is not null)
        {
            VerifyReferencesResolve(schema, child, pointer.AppendProperty(keyword), sourcePath, bag);
        }
    }

    private static void VerifyChildren(
        JsonSchemaDocument schema,
        IReadOnlyList<JsonSchemaNode>? children,
        JsonPointer pointer,
        string keyword,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (children is null)
        {
            return;
        }

        for (int index = 0; index < children.Count; index++)
        {
            VerifyReferencesResolve(
                schema,
                children[index],
                pointer.AppendProperty(keyword).AppendIndex(index),
                sourcePath,
                bag);
        }
    }

    private static IReadOnlyList<string>? ReadStringOrStringArray(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag,
        string keyword)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return new[] { value.GetString() ?? string.Empty };
        }

        return ReadStringArray(value, at, sourcePath, bag, keyword);
    }

    private static IReadOnlyList<string>? ReadStringArray(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag,
        string keyword)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            bag.Add(Malformed(sourcePath, at, keyword + " is an array of strings"));
            return null;
        }

        List<string> values = new();
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                bag.Add(Malformed(sourcePath, at, keyword + " contains only strings"));
                return null;
            }

            values.Add(item.GetString() ?? string.Empty);
        }

        return values;
    }

    private static Dictionary<string, JsonSchemaNode>? ReadSubschemaMap(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            bag.Add(Malformed(sourcePath, at, "properties is an object of name to subschema"));
            return null;
        }

        Dictionary<string, JsonSchemaNode> map = new(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            JsonSchemaNode? child =
                ParseNode(property.Value, at.AppendProperty(property.Name), sourcePath, bag);
            if (child is null)
            {
                return null;
            }

            map[property.Name] = child;
        }

        return map;
    }

    private static IReadOnlyList<JsonSchemaNode>? ReadSubschemaArray(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag,
        string keyword)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            bag.Add(Malformed(sourcePath, at, keyword + " is an array of subschemas"));
            return null;
        }

        List<JsonSchemaNode> nodes = new();
        int index = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            JsonSchemaNode? child = ParseNode(item, at.AppendIndex(index), sourcePath, bag);
            if (child is null)
            {
                return null;
            }

            nodes.Add(child);
            index++;
        }

        return nodes;
    }

    private static IReadOnlyList<JsonSchemaScalar>? ReadScalarArray(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            bag.Add(Malformed(sourcePath, at, "enum is an array"));
            return null;
        }

        List<JsonSchemaScalar> values = new();
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (!JsonSchemaScalar.TryFrom(item, out JsonSchemaScalar scalar))
            {
                bag.Add(Malformed(
                    sourcePath,
                    at,
                    "enum compares by structural equality and this evaluator implements that "
                        + "for scalars only"));
                return null;
            }

            values.Add(scalar);
        }

        return values;
    }

    private static int? ReadNonNegativeInteger(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag,
        string keyword)
    {
        if (value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out int number)
            || number < 0)
        {
            bag.Add(Malformed(sourcePath, at, keyword + " is a non-negative integer"));
            return null;
        }

        return number;
    }

    private static double? ReadNumber(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag,
        string keyword)
    {
        if (value.ValueKind != JsonValueKind.Number)
        {
            bag.Add(Malformed(sourcePath, at, keyword + " is a number"));
            return null;
        }

        return value.GetDouble();
    }

    /// <summary>
    /// Reads an <c>x-authority</c> annotation.
    /// </summary>
    /// <remarks>
    /// <c>source</c> is validated with <see cref="SourceRefGrammar"/>, the same parser
    /// <c>source_refs</c> uses. A second, separately maintained document-ID grammar would
    /// drift from the first, and then a citation legal in one place would be illegal in
    /// the other.
    /// </remarks>
    /// <summary>
    /// Reads <c>x-authority</c> as a map from bound keyword to the authority for that one
    /// bound.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The key is the identity of what is being attributed, so a subschema asserting three
    /// numbers writes three authorities and no number can borrow its neighbour's. The
    /// alternative shapes were a flat authority carrying a <c>bound</c> field, and an array
    /// of those: both allow the same keyword to be attributed twice, with two different
    /// derivations and no rule saying which one is the provenance. A map cannot express
    /// that.
    /// </para>
    /// <para>
    /// The map's keys are the closed bound-keyword list, so the earlier flat shape fails
    /// here naming <c>kind</c> rather than loading as an authority for a bound called
    /// "kind". A shape change that failed silently would be worse than the defect it fixes.
    /// </para>
    /// <para>
    /// Every entry is read even after one of them faults, so a subschema with two
    /// unjustified bounds is reported twice and names both. Stopping at the first fault made
    /// the diagnostic per <em>annotation</em> where the guarded thing is a <em>bound</em>,
    /// and the two bounds sharing one flaw is exactly the case a reviewer needs to see whole:
    /// repairing the named half and finding the other still broken reads as a second, new
    /// defect rather than the rest of the first.
    /// </para>
    /// </remarks>
    private static IReadOnlyDictionary<string, SchemaAuthority>? ReadAuthorities(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            bag.Add(Malformed(
                sourcePath,
                at,
                "x-authority is an object keyed by the bound keyword each authority explains"));
            return null;
        }

        Dictionary<string, SchemaAuthority> authorities = new(StringComparer.Ordinal);
        bool faulted = false;
        foreach (JsonProperty entry in value.EnumerateObject())
        {
            if (Array.IndexOf(SchemaAuthority.BoundKeywords(), entry.Name) < 0)
            {
                bag.Add(Malformed(
                    sourcePath,
                    at.AppendProperty(entry.Name),
                    "'" + entry.Name + "' is not a bound keyword. x-authority is keyed by the "
                        + "bound each authority explains, one of "
                        + string.Join(", ", SchemaAuthority.BoundKeywords())
                        + ", because a subschema may assert several numbers and each has its "
                        + "own provenance"));
                faulted = true;
                continue;
            }

            SchemaAuthority? authority = ReadAuthority(
                entry.Value, at.AppendProperty(entry.Name), entry.Name, sourcePath, bag);
            if (authority is null)
            {
                faulted = true;
                continue;
            }

            authorities[entry.Name] = authority;
        }

        if (faulted)
        {
            return null;
        }

        if (authorities.Count == 0)
        {
            bag.Add(Malformed(
                sourcePath,
                at,
                "x-authority attributes at least one bound; an empty one records nothing and "
                    + "reads as though a bound had been attributed"));
            return null;
        }

        return authorities;
    }

    /// <summary>Reads the one authority explaining <paramref name="keyword"/>.</summary>
    /// <remarks>
    /// <paramref name="keyword"/> is carried in so that every diagnostic below names the
    /// bound it is about. The pointer alone would say it, but a pointer is the thing a
    /// reader skims past, and the message that has to survive being read in a build log is
    /// the one naming which of two numbers went unjustified.
    /// </remarks>
    private static SchemaAuthority? ReadAuthority(
        JsonElement value,
        JsonPointer at,
        string keyword,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            bag.Add(Malformed(
                sourcePath,
                at,
                "the x-authority entry for '" + keyword + "' is an object"));
            return null;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.NameEquals("source")
                || property.NameEquals("section")
                || property.NameEquals("kind")
                || property.NameEquals("derivation")
                || property.NameEquals("rationale"))
            {
                continue;
            }

            bag.Add(Malformed(
                sourcePath,
                at.AppendProperty(property.Name),
                "x-authority declares only source, section, kind, derivation, and rationale"));
            return null;
        }

        if (!value.TryGetProperty("kind", out JsonElement kindElement)
            || kindElement.ValueKind != JsonValueKind.String)
        {
            bag.Add(Malformed(
                sourcePath, at, "x-authority.kind is sourced, derived, or structural"));
            return null;
        }

        SchemaAuthorityKind kind;
        switch (kindElement.GetString())
        {
            case "sourced":
                kind = SchemaAuthorityKind.Sourced;
                break;
            case "derived":
                kind = SchemaAuthorityKind.Derived;
                break;
            case "structural":
                kind = SchemaAuthorityKind.Structural;
                break;
            default:
                bag.Add(Malformed(
                    sourcePath,
                    at.AppendProperty("kind"),
                    "x-authority.kind is sourced, derived, or structural"));
                return null;
        }

        if (!TryReadAuthorityText(value, at, "source", sourcePath, bag, out string? source)
            || !TryReadAuthorityText(value, at, "section", sourcePath, bag, out string? section)
            || !TryReadAuthorityText(
                value, at, "derivation", sourcePath, bag, out string? derivation)
            || !TryReadAuthorityText(
                value, at, "rationale", sourcePath, bag, out string? rationale))
        {
            return null;
        }

        if (kind == SchemaAuthorityKind.Structural)
        {
            if (source is not null || section is not null || derivation is not null)
            {
                bag.Add(Malformed(
                    sourcePath,
                    at,
                    "'" + keyword + "' is structural, so it has no external authority and "
                        + "declares no source, section, or derivation; it states a 'rationale' "
                        + "instead. Use kind 'sourced' if the number does come from a document"));
                return null;
            }

            // A structural bound has no citation to go stale, and a limit nobody can justify
            // is indistinguishable from one chosen to make something pass. The rationale sits
            // in this entry rather than in the subschema's description because a description
            // is per subschema: one sentence licensed every structural bound under it, and
            // nothing could check which clause covered which number.
            if (string.IsNullOrWhiteSpace(rationale))
            {
                bag.Add(Malformed(
                    sourcePath,
                    at.AppendProperty("rationale"),
                    "'" + keyword + "' is structural, so its own x-authority entry states a "
                        + "'rationale': a string with something in it saying why this number. "
                        + "The subschema's 'description' does not answer for it - a description "
                        + "is per subschema, so one sentence would license every structural "
                        + "bound beside this one"));
                return null;
            }

            return new SchemaAuthority(kind, null, null, null, rationale);
        }

        if (rationale is not null)
        {
            bag.Add(Malformed(
                sourcePath,
                at.AppendProperty("rationale"),
                "'" + keyword + "' is " + kindElement.GetString() + ", so it states a "
                    + "'derivation' and not a 'rationale'. Both would ask why the number is "
                    + "that number, and two fields asking one question mean neither is the one "
                    + "to read; the redundant one is the one that fills with filler"));
            return null;
        }

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(section))
        {
            bag.Add(Malformed(
                sourcePath,
                at,
                "'" + keyword + "' is " + kindElement.GetString() + ", so it names the source "
                    + "document and the section within it"));
            return null;
        }

        if (string.IsNullOrWhiteSpace(derivation))
        {
            bag.Add(Malformed(
                sourcePath,
                at,
                "'" + keyword + "' is " + kindElement.GetString() + ", so it states its "
                    + "'derivation': how the number follows from its source. The source says "
                    + "where the number came from; the derivation says why it is that number, "
                    + "and the two go stale independently"));
            return null;
        }

        if (SourceRefGrammar.Parse(source!, out SourceRef? reference)
                != SourceRefParseOutcome.Parsed
            || reference!.Scope is not null
            || reference.Anchor is not null)
        {
            bag.Add(Malformed(
                sourcePath,
                at.AppendProperty("source"),
                "x-authority.source is a bare document ID in the same vocabulary source_refs "
                    + "uses, with no scope prefix and no anchor; the section is named separately "
                    + "so it stays a heading rather than becoming a slug"));
            return null;
        }

        return new SchemaAuthority(kind, source, section, derivation, null);
    }

    /// <summary>
    /// Reads one optional text field of an <c>x-authority</c> entry, requiring it to be a
    /// string when it is present at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type check is the point. These four fields used to be read as "a string if it is
    /// a string, otherwise absent", which quietly made every one of them a hiding place with
    /// a recognised field's name on it - the same hole <c>title</c> and <c>description</c>
    /// were, one level further in. <c>{"kind":"structural","source":{"if":{"maximum":5}}}</c>
    /// read as a structural entry declaring no source, so the loader raised nothing, and the
    /// corpus walk steps over <c>x-authority</c> wholesale precisely so that the annotation's
    /// own keys are not mistaken for bounds. Between them, the subschema parked under
    /// <c>source</c> was walked by nobody.
    /// </para>
    /// <para>
    /// <c>rationale</c> is the field that made this urgent: it is a new string one level
    /// inside the annotation, which is exactly the position the structure-blind walk has been
    /// fooled in twice. It is checked here with the other three rather than on its own,
    /// because a rule applied to the newest field and not its neighbours is how the hole gets
    /// reopened next time.
    /// </para>
    /// </remarks>
    private static bool TryReadAuthorityText(
        JsonElement value,
        JsonPointer at,
        string field,
        string sourcePath,
        DiagnosticBag bag,
        out string? text)
    {
        text = null;
        if (!value.TryGetProperty(field, out JsonElement element))
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            bag.Add(Malformed(
                sourcePath,
                at.AppendProperty(field),
                "'" + field + "' is a string; a non-string field inside x-authority is a "
                    + "subschema-shaped value that nothing walks, because the loader never "
                    + "parses the annotation as a schema and the corpus walk steps over it"));
            return false;
        }

        text = element.GetString();
        return true;
    }

    private static ContentDiagnostic Malformed(
        string sourcePath,
        JsonPointer pointer,
        string expected)
    {
        return ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.SchemaMalformed,
            sourcePath,
            pointer,
            null,
            expected);
    }
}
