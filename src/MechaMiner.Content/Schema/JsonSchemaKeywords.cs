using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Schema;

/// <summary>
/// The draft 2020-12 keywords this evaluator implements, and the annotation keywords it
/// accepts without asserting anything.
/// </summary>
/// <remarks>
/// <para>
/// The evaluator is deliberately small. It exists to prove that
/// <c>content/schemas/*.schema.json</c> and the project-owned typed validators agree,
/// which is the mechanism
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline asks for by name. It is not a general JSON Schema library and must not grow
/// into one; a keyword is added here when a project schema needs it and a test covers
/// it, and not before.
/// </para>
/// <para>
/// <b>An unrecognised keyword is a load failure, not a no-op.</b> The specification
/// says an implementation should ignore keywords it does not know, which is the right
/// rule for interoperability and exactly the wrong rule for a gate: a schema that
/// silently loses a constraint because the evaluator did not recognise it still reports
/// "valid", and the gate has quietly stopped being one. Failing loudly means a schema
/// author who needs a new keyword is told to implement it.
/// </para>
/// </remarks>
public static class JsonSchemaKeywords
{
    private static readonly string[] AssertionKeywords =
    {
        "$schema",
        "$id",
        "$ref",
        "$defs",
        "type",
        "required",
        "properties",
        "additionalProperties",
        "propertyNames",
        "enum",
        "const",
        "pattern",
        "minLength",
        "maxLength",
        "minimum",
        "maximum",
        "exclusiveMinimum",
        "exclusiveMaximum",
        "multipleOf",
        "items",
        "prefixItems",
        "minItems",
        "maxItems",
        "uniqueItems",
        "allOf",
        "anyOf",
        "oneOf",
        "not",
    };

    /// <summary>
    /// Keywords that carry documentation rather than a constraint. They are accepted
    /// because a schema without them is unreadable, and they are enumerated rather than
    /// pattern-matched so that a misspelled assertion keyword cannot hide among them.
    /// </summary>
    private static readonly string[] AnnotationKeywords =
    {
        "title",
        "description",
        "$comment",
        "examples",
        "default",
        "deprecated",
    };

    private static readonly HashSet<string> Recognised = BuildRecognised();

    /// <summary>Every keyword the evaluator asserts on.</summary>
    public static IReadOnlyList<string> Assertions { get; } =
        new ReadOnlyCollection<string>(new List<string>(AssertionKeywords));

    /// <summary>Every keyword the evaluator accepts but does not assert on.</summary>
    public static IReadOnlyList<string> Annotations { get; } =
        new ReadOnlyCollection<string>(new List<string>(AnnotationKeywords));

    /// <summary>True when <paramref name="keyword"/> is implemented or a known annotation.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="keyword"/> is null.</exception>
    public static bool IsRecognised(string keyword)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        return Recognised.Contains(keyword);
    }

    /// <summary>Renders the implemented keyword set for a diagnostic.</summary>
    public static string DescribeSupported()
    {
        return "this evaluator implements " + string.Join(", ", AssertionKeywords)
            + " and accepts the annotations " + string.Join(", ", AnnotationKeywords)
            + "; a keyword it does not implement is a load failure rather than a no-op, "
            + "because ignoring one makes the schema stop being a gate";
    }

    private static HashSet<string> BuildRecognised()
    {
        HashSet<string> recognised = new(StringComparer.Ordinal);
        foreach (string keyword in AssertionKeywords)
        {
            recognised.Add(keyword);
        }

        foreach (string keyword in AnnotationKeywords)
        {
            recognised.Add(keyword);
        }

        return recognised;
    }
}
