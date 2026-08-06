using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace MechaMiner.Content.Ids;

/// <summary>
/// One content category: its authoring directory and the ID grammars it accepts.
/// </summary>
/// <remarks>
/// <para>
/// A category accepts a <em>list</em> of patterns rather than one, because doc 40
/// § Accepted content repository layout groups definitions "by stable item or the
/// smallest cohesive aggregate": a directory can therefore hold both per-item
/// definitions and an aggregate that is not one of those items. <c>content/enemies/</c>
/// is the live example - ten <c>EN-##</c> enemies and one <c>ELT-01</c> shared elite
/// modifier set, which is an aggregate over all of them and is not itself an enemy.
/// </para>
/// <para>
/// A pattern is a <b>grammar, not a census</b>. It says what an ID of this category
/// looks like; it does not say how many exist. Cardinality - "exactly 15 unordered
/// material-pair recipes", "exactly four accepted classes" - is a semantic rule that
/// doc 40 § Semantic assigns to the per-catalog validators owned by <c>DAT-002</c> and
/// <c>DAT-003</c>. Encoding a count in the pattern would put the same rule in two
/// places and make adding a reviewed definition a grammar change.
/// </para>
/// </remarks>
public sealed class ContentCategoryDescriptor
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private readonly Regex[] _patterns;

    internal ContentCategoryDescriptor(
        ContentCategory category,
        string directoryName,
        IReadOnlyList<string> idPatterns)
    {
        Category = category;
        DirectoryName = directoryName;
        IdPatterns = new ReadOnlyCollection<string>(new List<string>(idPatterns));

        _patterns = new Regex[idPatterns.Count];
        for (int index = 0; index < idPatterns.Count; index++)
        {
            _patterns[index] = new Regex(
                idPatterns[index],
                RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
                MatchTimeout);
        }
    }

    /// <summary>The category.</summary>
    public ContentCategory Category { get; }

    /// <summary>The directory name beneath <c>content/</c>.</summary>
    public string DirectoryName { get; }

    /// <summary>The anchored ID patterns this category accepts, as written.</summary>
    public IReadOnlyList<string> IdPatterns { get; }

    /// <summary>True when <paramref name="id"/> matches one of this category's grammars.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is null.</exception>
    public bool Accepts(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        // Doc 40 § Stable ID policy: "IDs are case-sensitive ASCII tokens". Every
        // pattern below is written with ASCII-only character classes, so a non-ASCII
        // ID could not match anyway; the check is here so the failure says "not ASCII"
        // instead of "did not match a pattern", which is what an author who pasted a
        // Cyrillic homoglyph needs to be told.
        foreach (char character in id)
        {
            if (!char.IsAscii(character))
            {
                return false;
            }
        }

        foreach (Regex pattern in _patterns)
        {
            if (pattern.IsMatch(id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Renders the accepted grammars for a diagnostic's expected constraint.</summary>
    public string DescribeAcceptedGrammar()
    {
        return "an ID of category " + Category + " matches " + string.Join(" or ", IdPatterns);
    }
}
