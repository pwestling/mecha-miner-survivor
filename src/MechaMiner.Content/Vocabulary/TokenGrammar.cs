using System;
using System.Text.RegularExpressions;

namespace MechaMiner.Content.Vocabulary;

/// <summary>
/// The shape a value token takes: <c>lower-kebab-case</c>, exact and case-sensitive.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline requires only that "stable enum/kind/ID tokens remain exact case-sensitive
/// ASCII"; it does not pick a shape. The tree picks one by weight of usage -
/// <c>hyper-gold-site</c>, <c>fresh-profile</c>, <c>additive-percent</c> against eight
/// <c>camelCase</c> stragglers - and one shape everywhere is what lets a reader tell a
/// token from a sentence at a glance.
/// </para>
/// <para>
/// <b>The first character is a letter.</b> A token beginning with a digit reads as a
/// number wherever a token appears without quotes - a log line, a build diagnostic, a
/// report column - and the reader who has to tell <c>3-shot-burst</c> from a quantity is
/// doing work the grammar can do once. Digits are otherwise unrestricted, so
/// <c>burst-3</c> and <c>tier-2-armor</c> are tokens.
/// </para>
/// <para>
/// <b>There is no length bound, deliberately.</b> No accepted document states one, and a
/// number nobody can derive is worse than no number: it looks considered, it is the first
/// thing a genuine long token trips over, and under
/// <c>content/schemas/README.md</c> § <c>x-authority</c> every bound in a schema here has
/// to state where it came from. There is nowhere for an invented ceiling to come from.
/// </para>
/// <para>
/// <b>This is a grammar check and never a registry check.</b> Whether a token has
/// exactly one registered descriptor with a compatible parameter schema is the
/// behavior registry's question, owned by <c>DAT-004</c>. Conflating the two here
/// would mean a well-formed token failing with "malformed" when the real fault is
/// "unregistered", and would make this package's gates depend on a registry that does
/// not exist yet.
/// </para>
/// </remarks>
public static class TokenGrammar
{
    /// <summary>The accepted token pattern, as written.</summary>
    public const string Pattern = "^[a-z][a-z0-9]*(-[a-z0-9]+)*$";

    private static readonly Regex Token = new(
        Pattern,
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        TimeSpan.FromSeconds(1));

    /// <summary>True when <paramref name="value"/> is a well-formed token.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static bool IsWellFormed(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        foreach (char character in value)
        {
            if (!char.IsAscii(character))
            {
                return false;
            }
        }

        return Token.IsMatch(value);
    }

    /// <summary>Renders the grammar for a diagnostic.</summary>
    public static string Describe()
    {
        return "a token matches " + Pattern + ": lower-kebab-case, exact and case-sensitive, "
            + "beginning with a letter so that a token is never read as a number. "
            + "A prose sentence, a camelCase token, and a display name are all rejected. Whether "
            + "the token resolves to a registered behavior descriptor is a separate check owned by "
            + "the behavior registry and is deliberately not asserted here";
    }
}
