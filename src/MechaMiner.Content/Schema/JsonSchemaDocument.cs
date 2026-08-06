using System;
using System.Collections.Generic;

namespace MechaMiner.Content.Schema;

/// <summary>A loaded draft 2020-12 schema, with its <c>$defs</c> resolved.</summary>
/// <remarks>
/// Only two <c>$ref</c> forms are supported: <c>#</c> for the root schema and
/// <c>#/$defs/&lt;name&gt;</c> for a named definition. Anything else - a remote
/// reference, a JSON Pointer into an arbitrary subschema, an anchor - is a load failure
/// rather than a resolution attempt. A project schema that needed one would be a schema
/// no reviewer could follow by reading it.
/// </remarks>
public sealed class JsonSchemaDocument
{
    private readonly Dictionary<string, JsonSchemaNode> _definitions;

    internal JsonSchemaDocument(
        string sourcePath,
        JsonSchemaNode root,
        Dictionary<string, JsonSchemaNode> definitions)
    {
        SourcePath = sourcePath;
        Root = root;
        _definitions = definitions;
    }

    /// <summary>Where the schema was loaded from, for diagnostics.</summary>
    public string SourcePath { get; }

    /// <summary>The names declared under <c>$defs</c>.</summary>
    public IReadOnlyCollection<string> DefinitionNames => _definitions.Keys;

    internal JsonSchemaNode Root { get; }

    internal bool TryResolve(string reference, out JsonSchemaNode? node)
    {
        if (string.Equals(reference, "#", StringComparison.Ordinal))
        {
            node = Root;
            return true;
        }

        const string defsPrefix = "#/$defs/";
        if (reference.StartsWith(defsPrefix, StringComparison.Ordinal))
        {
            string name = reference[defsPrefix.Length..];
            return _definitions.TryGetValue(name, out node);
        }

        node = null;
        return false;
    }
}
