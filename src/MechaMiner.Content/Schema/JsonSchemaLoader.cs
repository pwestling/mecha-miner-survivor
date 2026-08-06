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

            if (!ApplyKeyword(node, property, at, sourcePath, bag))
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
    private static void ValidateAuthorityPlacement(
        JsonSchemaNode node,
        JsonElement element,
        JsonPointer pointer,
        string sourcePath,
        DiagnosticBag bag)
    {
        bool declaresBound = false;
        foreach (string keyword in SchemaAuthority.BoundKeywords())
        {
            if (element.TryGetProperty(keyword, out _))
            {
                declaresBound = true;
                break;
            }
        }

        if (declaresBound && node.Authority is null)
        {
            bag.Add(Malformed(
                sourcePath,
                pointer,
                "a numeric bound carries an adjacent '" + SchemaAuthority.Keyword
                    + "' recording where the number came from; without one, "
                    + "\"which bounds need re-deriving now that a document changed\" is "
                    + "answerable only from memory"));
        }

        if (node.Authority is null)
        {
            return;
        }

        if (!declaresBound)
        {
            bag.Add(Malformed(
                sourcePath,
                pointer.AppendProperty(SchemaAuthority.Keyword),
                "'" + SchemaAuthority.Keyword + "' annotates a numeric bound, so it belongs "
                    + "next to one of "
                    + string.Join(", ", SchemaAuthority.BoundKeywords())));
            return;
        }

        // A structural bound has no citation to go stale, but it still has to be
        // justified, and description is where a reader looks for the justification.
        if (node.Authority.Kind == SchemaAuthorityKind.Structural
            && !element.TryGetProperty("description", out _))
        {
            bag.Add(Malformed(
                sourcePath,
                pointer.AppendProperty(SchemaAuthority.Keyword),
                "a structural bound states its rationale in 'description'; a limit nobody can "
                    + "justify is indistinguishable from one chosen to make something pass"));
        }
    }

    private static bool ApplyKeyword(
        JsonSchemaNode node,
        JsonProperty property,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag)
    {
        JsonElement value = property.Value;
        switch (property.Name)
        {
            case "$schema":
            case "$id":
            case "$defs":
            case "title":
            case "description":
            case "$comment":
                // Identity and documentation: no assertion. $defs is parsed separately at
                // the root and ignored on a subschema.
                return true;

            case SchemaAuthority.Keyword:
                node.Authority = ReadAuthority(value, at, sourcePath, bag);
                return node.Authority is not null;

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
    private static SchemaAuthority? ReadAuthority(
        JsonElement value,
        JsonPointer at,
        string sourcePath,
        DiagnosticBag bag)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            bag.Add(Malformed(sourcePath, at, "x-authority is an object"));
            return null;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.NameEquals("source")
                || property.NameEquals("section")
                || property.NameEquals("kind")
                || property.NameEquals("derivation"))
            {
                continue;
            }

            bag.Add(Malformed(
                sourcePath,
                at.AppendProperty(property.Name),
                "x-authority declares only source, section, kind, and derivation"));
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

        string? source = value.TryGetProperty("source", out JsonElement sourceElement)
            && sourceElement.ValueKind == JsonValueKind.String
                ? sourceElement.GetString()
                : null;
        string? section = value.TryGetProperty("section", out JsonElement sectionElement)
            && sectionElement.ValueKind == JsonValueKind.String
                ? sectionElement.GetString()
                : null;
        string? derivation = value.TryGetProperty("derivation", out JsonElement derivationElement)
            && derivationElement.ValueKind == JsonValueKind.String
                ? derivationElement.GetString()
                : null;

        if (kind == SchemaAuthorityKind.Structural)
        {
            if (source is not null || section is not null || derivation is not null)
            {
                bag.Add(Malformed(
                    sourcePath,
                    at,
                    "a structural bound has no external authority, so it declares no source, "
                        + "section, or derivation; its rationale lives in 'description'. Use "
                        + "kind 'sourced' if the number does come from a document"));
                return null;
            }

            return new SchemaAuthority(kind, null, null, null);
        }

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(section))
        {
            bag.Add(Malformed(
                sourcePath,
                at,
                "a " + kindElement.GetString() + " bound names the source document and the "
                    + "section within it"));
            return null;
        }

        if (string.IsNullOrWhiteSpace(derivation))
        {
            bag.Add(Malformed(
                sourcePath,
                at,
                "a " + kindElement.GetString() + " bound states its 'derivation': how the number "
                    + "follows from its source. The source says where the number came from; the "
                    + "derivation says why it is that number, and the two go stale independently"));
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

        return new SchemaAuthority(kind, source, section, derivation);
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
