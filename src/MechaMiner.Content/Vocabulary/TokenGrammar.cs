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
    public const string Pattern = "^[a-z0-9]+(-[a-z0-9]+)*$";

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
        return "a token matches " + Pattern + ": lower-kebab-case, exact and case-sensitive. "
            + "A prose sentence, a camelCase token, and a display name are all rejected. Whether "
            + "the token resolves to a registered behavior descriptor is a separate check owned by "
            + "the behavior registry and is deliberately not asserted here";
    }
}
