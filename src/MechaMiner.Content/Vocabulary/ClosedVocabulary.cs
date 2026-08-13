using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Vocabulary;

/// <summary>
/// A closed set of value tokens, matched by exact case-sensitive ASCII comparison.
/// </summary>
/// <remarks>
/// <para>
/// Exactness is the point. <c>docs/technical/40-content-data-and-validation.md</c>
/// § JSON codec and schema baseline makes tokens "exact case-sensitive ASCII", so
/// <c>Amplification</c> is not <c>amplification</c> and a case-insensitive comparison
/// would silently accept both spellings of one concept - which is how a field ends up
/// with three casings of the same statistic name, as this catalog's named statistics
/// did before they were closed.
/// </para>
/// <para>
/// A vocabulary carries the document that grants it, so that "where did this list come
/// from" is answerable from the type rather than from a comment near it.
/// </para>
/// </remarks>
public sealed class ClosedVocabulary
{
    private readonly HashSet<string> _tokens;

    /// <summary>Declares a closed vocabulary.</summary>
    /// <param name="subject">What the vocabulary names, for a diagnostic.</param>
    /// <param name="source">The document ID that grants the list.</param>
    /// <param name="tokens">The accepted tokens.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException">A token is not well formed, or is declared twice.</exception>
    public ClosedVocabulary(string subject, string source, params string[] tokens)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(tokens);

        Subject = subject;
        Source = source;
        Tokens = new ReadOnlyCollection<string>(new List<string>(tokens));

        _tokens = new HashSet<string>(tokens.Length, StringComparer.Ordinal);
        foreach (string token in tokens)
        {
            if (!TokenGrammar.IsWellFormed(token))
            {
                throw new ArgumentException(
                    "'" + token + "' is not a well-formed token for " + subject + "; "
                        + TokenGrammar.Describe(),
                    nameof(tokens));
            }

            if (!_tokens.Add(token))
            {
                throw new ArgumentException(
                    subject + " declares the token '" + token + "' twice",
                    nameof(tokens));
            }
        }
    }

    /// <summary>What the vocabulary names.</summary>
    public string Subject { get; }

    /// <summary>The document ID that grants the list.</summary>
    public string Source { get; }

    /// <summary>The accepted tokens, in declared order.</summary>
    public IReadOnlyList<string> Tokens { get; }

    /// <summary>True when <paramref name="token"/> is in the vocabulary.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null.</exception>
    public bool Accepts(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return _tokens.Contains(token);
    }

    /// <summary>Renders the accepted set for a diagnostic.</summary>
    public string Describe()
    {
        return Subject + " is one of the exact case-sensitive tokens "
            + string.Join(", ", Tokens) + " (" + Source + ")";
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Subject;
    }
}
