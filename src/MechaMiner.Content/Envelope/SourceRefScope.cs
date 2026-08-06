using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// The optional scope prefix of a <c>source_refs</c> element: a selector into the
/// definition's own JSON saying which part of it the reference accounts for.
/// </summary>
/// <remarks>
/// <para>
/// A scope is what turns traceability from per-file into per-field. Without it,
/// <c>content/unlocks/UNL-01.json</c> says only "these fourteen documents were
/// consulted"; with it, <c>cost_hyper_gold: DEC-121#decision</c> says which document
/// decided that one number.
/// </para>
/// <para>
/// <b>A scope is validated against the definition it annotates.</b>
/// <see cref="ResolvesIn"/> is the point of the whole mechanism: a scope that names a
/// field the definition does not have is a stale annotation, and a stale annotation is
/// worse than none because a reader believes it. This is a rule the JSON Schema cannot
/// express - the schema can pin the scope's <em>syntax</em>, but only a validator
/// holding the document can tell whether the path exists in it.
/// </para>
/// </remarks>
public sealed class SourceRefScope
{
    /// <summary>
    /// The widest span a range step may select. A scope is an annotation on a part of a
    /// definition; one that named hundreds of array elements would be annotating the
    /// array, and should say so with <c>[]</c>.
    /// </summary>
    public const int MaximumRangeSpan = 256;

    internal SourceRefScope(string text, IReadOnlyList<SourceRefScopeStep> steps)
    {
        Text = text;
        Steps = new ReadOnlyCollection<SourceRefScopeStep>(new List<SourceRefScopeStep>(steps));
    }

    /// <summary>The scope exactly as authored, without the trailing <c>": "</c>.</summary>
    public string Text { get; }

    /// <summary>The parsed steps, in order.</summary>
    public IReadOnlyList<SourceRefScopeStep> Steps { get; }

    /// <summary>
    /// True when at least one concrete path this scope selects exists in
    /// <paramref name="structure"/>.
    /// </summary>
    /// <remarks>
    /// "At least one" is the right test for the wildcard and range forms:
    /// <c>rules[]</c> annotates the rules that exist, and an array of three is fully
    /// covered by it. A scope selecting nothing at all is the failure.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="structure"/> is null.</exception>
    public bool ResolvesIn(JsonStructure structure)
    {
        ArgumentNullException.ThrowIfNull(structure);

        List<JsonPointer> candidates = new() { JsonPointer.Root };

        foreach (SourceRefScopeStep step in Steps)
        {
            List<JsonPointer> next = new();
            foreach (JsonPointer candidate in candidates)
            {
                AppendStep(structure, candidate, step, next);
            }

            if (next.Count == 0)
            {
                return false;
            }

            candidates = next;
        }

        // Every surviving candidate is a pointer that exists, because AppendStep only
        // keeps pointers the structure contains.
        return candidates.Count > 0;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Text;
    }

    private static void AppendStep(
        JsonStructure structure,
        JsonPointer parent,
        SourceRefScopeStep step,
        List<JsonPointer> into)
    {
        switch (step.Kind)
        {
            case SourceRefScopeStepKind.Member:
                JsonPointer member = parent.AppendProperty(step.Name!);
                if (structure.Contains(member))
                {
                    into.Add(member);
                }

                break;

            case SourceRefScopeStepKind.AnyIndex:
                for (int index = 0; ; index++)
                {
                    JsonPointer element = parent.AppendIndex(index);
                    if (!structure.Contains(element))
                    {
                        break;
                    }

                    into.Add(element);
                }

                break;

            default:
                for (int index = step.LowIndex; index <= step.HighIndex; index++)
                {
                    JsonPointer element = parent.AppendIndex(index);
                    if (structure.Contains(element))
                    {
                        into.Add(element);
                    }
                }

                break;
        }
    }

    /// <summary>Renders the concrete paths this scope would select, for a diagnostic.</summary>
    public string DescribeSelection()
    {
        List<string> parts = new(Steps.Count);
        foreach (SourceRefScopeStep step in Steps)
        {
            parts.Add(step.Kind switch
            {
                SourceRefScopeStepKind.Member => "/" + JsonPointer.EscapeToken(step.Name!),
                SourceRefScopeStepKind.AnyIndex => "/<any index>",
                SourceRefScopeStepKind.Index =>
                    "/" + step.LowIndex.ToString(CultureInfo.InvariantCulture),
                _ => "/" + step.LowIndex.ToString(CultureInfo.InvariantCulture) + ".."
                    + step.HighIndex.ToString(CultureInfo.InvariantCulture),
            });
        }

        return string.Concat(parts);
    }
}
