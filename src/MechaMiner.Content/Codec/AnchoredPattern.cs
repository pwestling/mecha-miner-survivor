using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MechaMiner.Content.Codec;

/// <summary>
/// Compiles a pattern written in JSON Schema's dialect so that .NET agrees with it.
/// </summary>
/// <remarks>
/// <para>
/// JSON Schema <c>pattern</c> is ECMA-262. .NET's <see cref="Regex"/> is close but
/// differs on one point that matters here: <b>.NET's <c>$</c> also matches immediately
/// before a trailing newline, and ECMA-262's does not.</b> So <c>^W-[A-F]{2}$</c>
/// accepts <c>"W-AB\n"</c> in .NET and rejects it in every JSON Schema tool.
/// </para>
/// <para>
/// That difference is not cosmetic. It would let a stable ID, a localization key, or a
/// <c>source_refs</c> element carry a trailing newline past the typed validator, and it
/// would make the typed validator and <c>content/schemas/envelope.schema.json</c>
/// disagree on exactly the fixtures the agreement corpus exists to compare - a
/// disagreement the corpus would report as a schema defect when the defect is really in
/// the regex dialect.
/// </para>
/// <para>
/// Every pattern in this project is therefore authored once in ECMA-262 form, stored as
/// the constant the schema mirrors, and compiled through here. <c>^</c> needs no
/// translation: without the multiline option it means "start of input" in both
/// dialects.
/// </para>
/// </remarks>
public static class AnchoredPattern
{
    /// <summary>
    /// A bound on backtracking, so a pathological pattern fails loudly instead of
    /// hanging a build.
    /// </summary>
    public static TimeSpan MatchTimeout { get; } = TimeSpan.FromSeconds(1);

    /// <summary>Compiles <paramref name="ecmaPattern"/> with ECMA-262 anchor semantics.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="ecmaPattern"/> is null.</exception>
    /// <exception cref="ArgumentException">The pattern is not a valid regular expression.</exception>
    public static Regex Compile(string ecmaPattern)
    {
        return new Regex(
            Translate(ecmaPattern),
            RegexOptions.CultureInvariant,
            MatchTimeout);
    }

    /// <summary>
    /// Rewrites every unescaped <c>$</c> as <c>\z</c>, which is what ECMA-262's <c>$</c>
    /// means when the multiline flag is off.
    /// </summary>
    /// <remarks>
    /// A <c>$</c> inside a character class is left alone only if it is escaped. No
    /// project pattern puts a literal <c>$</c> in a class; one that needed to would write
    /// <c>\$</c>, which this already skips.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="ecmaPattern"/> is null.</exception>
    public static string Translate(string ecmaPattern)
    {
        ArgumentNullException.ThrowIfNull(ecmaPattern);

        StringBuilder translated = new(ecmaPattern.Length + 2);
        bool escaped = false;

        foreach (char character in ecmaPattern)
        {
            if (escaped)
            {
                translated.Append(character);
                escaped = false;
                continue;
            }

            switch (character)
            {
                case '\\':
                    translated.Append(character);
                    escaped = true;
                    break;

                case '$':
                    translated.Append("\\z");
                    break;

                default:
                    translated.Append(character);
                    break;
            }
        }

        return translated.ToString();
    }
}
