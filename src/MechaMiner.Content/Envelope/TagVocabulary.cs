using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// The closed vocabulary the <c>tags</c> envelope field draws from.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § <c>tags</c> vocabulary:
/// "<c>tags</c> accepts an empty array, and an empty array is the expected value for
/// most definitions. The closed vocabulary starts <b>empty</b> and gains a term only
/// when a concrete query or tooling need requires it; the term is added to the
/// vocabulary in the same change that first uses it. A tag never carries behavior,
/// never selects an implementation, and never gates a rule."
/// </para>
/// <para>
/// <b>The vocabulary is empty and every tag is therefore currently rejected.</b> All
/// 138 definitions in the accepted catalog author <c>"tags": []</c>, so nothing is
/// blocked by this. The empty set is the whole mechanism: an author who needs a term
/// adds one line to <see cref="Declared"/> in the same commit that first uses it, and
/// a reviewer sees the vocabulary grow rather than discovering a tag in a data file.
/// </para>
/// <para>
/// <b>How to add a term.</b> Add it to <see cref="Declared"/> with a comment naming
/// the query or tool that needs it. Do not add a term "for later": a tag with no
/// consumer is indistinguishable from a tag that carries behavior, which is what the
/// doc forbids.
/// </para>
/// </remarks>
public static class TagVocabulary
{
    /// <summary>
    /// The accepted terms. Deliberately empty; see the type remarks for how a term is
    /// added.
    /// </summary>
    private static readonly string[] Declared = Array.Empty<string>();

    private static readonly HashSet<string> Accepted = new(Declared, StringComparer.Ordinal);

    /// <summary>Every accepted term.</summary>
    public static IReadOnlyList<string> Terms { get; } =
        new ReadOnlyCollection<string>(new List<string>(Declared));

    /// <summary>True when the vocabulary has no terms.</summary>
    public static bool IsEmpty => Accepted.Count == 0;

    /// <summary>True when <paramref name="tag"/> is in the vocabulary.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="tag"/> is null.</exception>
    public static bool Accepts(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        return Accepted.Contains(tag);
    }

    /// <summary>Renders the vocabulary for a diagnostic's expected constraint.</summary>
    public static string Describe()
    {
        return Accepted.Count == 0
            ? "the tags vocabulary is closed and currently empty, so [] is the only accepted "
                + "value; a term is added to the vocabulary in the same change that first uses it"
            : "a tag is one of: " + string.Join(", ", Terms);
    }
}
