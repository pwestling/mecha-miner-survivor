using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MechaMiner.Content.Schema;

/// <summary>One parsed subschema.</summary>
/// <remarks>
/// A null property means the keyword was absent, which in JSON Schema means the
/// assertion does not apply. That is why every constraint is nullable rather than
/// defaulted: a defaulted <c>minItems</c> of 0 and an absent <c>minItems</c> behave the
/// same for arrays but not for the error message, and the message is the whole value of
/// a gate.
/// </remarks>
internal sealed class JsonSchemaNode
{
    /// <summary>
    /// A boolean schema: <c>true</c> accepts everything, <c>false</c> rejects
    /// everything. Draft 2020-12 § 4.3.2.
    /// </summary>
    internal bool? BooleanSchema { get; set; }

    internal string? Reference { get; set; }

    internal IReadOnlyList<string>? Types { get; set; }

    internal IReadOnlyList<string>? Required { get; set; }

    internal Dictionary<string, JsonSchemaNode>? Properties { get; set; }

    internal JsonSchemaNode? AdditionalProperties { get; set; }

    internal JsonSchemaNode? PropertyNames { get; set; }

    internal IReadOnlyList<JsonSchemaScalar>? Enumeration { get; set; }

    internal JsonSchemaScalar? Constant { get; set; }

    internal Regex? Pattern { get; set; }

    internal string? PatternText { get; set; }

    internal int? MinLength { get; set; }

    internal int? MaxLength { get; set; }

    internal double? Minimum { get; set; }

    internal double? Maximum { get; set; }

    internal double? ExclusiveMinimum { get; set; }

    internal double? ExclusiveMaximum { get; set; }

    internal double? MultipleOf { get; set; }

    internal JsonSchemaNode? Items { get; set; }

    internal IReadOnlyList<JsonSchemaNode>? PrefixItems { get; set; }

    internal int? MinItems { get; set; }

    internal int? MaxItems { get; set; }

    internal bool UniqueItems { get; set; }

    internal IReadOnlyList<JsonSchemaNode>? AllOf { get; set; }

    internal IReadOnlyList<JsonSchemaNode>? AnyOf { get; set; }

    internal IReadOnlyList<JsonSchemaNode>? OneOf { get; set; }

    internal JsonSchemaNode? Not { get; set; }

    /// <summary>
    /// The provenance of each of this subschema's numeric bounds, keyed by the bound
    /// keyword it explains. Null when the subschema declares no <c>x-authority</c> at all,
    /// which is a different thing from declaring an empty one.
    /// </summary>
    /// <remarks>
    /// Keyed rather than single because a subschema may declare several bounds and each
    /// number has its own provenance. One authority standing for the whole subschema meant
    /// that attributing <c>minLength</c> silently licensed an unattributed <c>maxLength</c>
    /// beside it.
    /// </remarks>
    internal IReadOnlyDictionary<string, SchemaAuthority>? Authorities { get; set; }
}
