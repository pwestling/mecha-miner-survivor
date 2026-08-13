using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace MechaMiner.Diagnostics.Logging;

/// <summary>
/// Removes machine-private values from every log field before it is written.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>CMP-OBS-001</c>, <c>FND-007</c> (<c>TASK-FND-007-001</c>). Authority:
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Structured
/// logging: "Release logs exclude full filesystem paths, usernames, raw Steam identifiers,
/// and uncontrolled content text", and § Diagnostic package export: "The exporter redacts
/// paths and identifiers". Requirements: <c>TR-OBS-002</c>, <c>TR-BLD-003</c>.
/// </para>
/// <para>
/// Redaction runs in every configuration, not only Release. A rule that is exercised only
/// in the configuration nobody develops against is a rule that is discovered to be broken
/// at release time; and a development log is shared in a bug report just as readily.
/// </para>
/// <para>
/// The private values are <b>passed in</b>. Nothing here reads the environment, because a
/// redactor that discovers its own secrets cannot be given a fixture's secrets, and the
/// gate for this class is exactly "a fixture containing machine-private values proves they
/// are absent from the output".
/// </para>
/// <para>
/// The rules run longest-secret-first. Replacing the user name before the home directory
/// would leave <c>/home/&lt;user&gt;</c> behind, which still discloses the layout and would
/// no longer match the home-directory rule.
/// </para>
/// </remarks>
internal sealed class Redaction
{
    /// <summary>The token that replaces the user's home directory.</summary>
    /// <remarks>
    /// Square brackets, not angle brackets. <c>System.Text.Json</c>'s default encoder escapes
    /// <c>&lt;</c> and <c>&gt;</c> for HTML safety, so an angle-bracket token would appear in a
    /// log line as an escape sequence: still correct, but unreadable, and a reviewer grepping
    /// for the token would not find it. Square brackets pass through unescaped, so the
    /// canonical encoder stays untouched and the line stays legible.
    /// </remarks>
    internal const string HomeToken = "[home]";

    /// <summary>The token that replaces the owned user-data root.</summary>
    internal const string UserDataToken = "[user-data]";

    /// <summary>The token that replaces the account name.</summary>
    internal const string UserToken = "[user]";

    /// <summary>The token that replaces any other absolute filesystem path.</summary>
    internal const string PathToken = "[path]";

    /// <summary>The token that replaces a raw Steam identifier.</summary>
    internal const string SteamIdToken = "[steam-id]";

    /// <summary>The token appended when uncontrolled text is truncated.</summary>
    internal const string TruncationToken = "[truncated]";

    /// <summary>The bound on any single field value, before the truncation token.</summary>
    /// <remarks>
    /// Doc 90 requires excluding "uncontrolled content text" and bounding log growth. A
    /// bound on each field is the mechanism: a localized string, an exception message, or a
    /// content author's free text cannot make one record arbitrarily large.
    /// </remarks>
    internal const int MaximumFieldLength = 512;

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(5);

    // A 17-digit Steam ID64 in the individual-account range, which begins 7656119.
    private static readonly Regex SteamId = new(
        @"7656119[0-9]{10}",
        RegexOptions.Compiled,
        MatchTimeout);

    // Absolute paths that survive the explicit home and user-data rules: another user's
    // home, a Windows profile directory, or a macOS user directory.
    private static readonly Regex AbsolutePath = new(
        @"(?:[A-Za-z]:[\\/](?:Users|Documents and Settings)[\\/][^\s""'<>|]*"
        + @"|/(?:home|Users|root)/[^\s""'<>|]*)",
        RegexOptions.Compiled,
        MatchTimeout);

    private readonly ImmutableArray<KeyValuePair<string, string>> _literals;

    private Redaction(ImmutableArray<KeyValuePair<string, string>> literals)
    {
        _literals = literals;
    }

    /// <summary>
    /// Builds a redactor for the given private values. Any of them may be empty, in which
    /// case that rule is simply absent rather than matching everything.
    /// </summary>
    internal static Redaction For(string homeDirectory, string userDataDirectory, string userName)
    {
        List<KeyValuePair<string, string>> literals = new();

        // Longest first. The user-data root is normally inside the home directory, so
        // replacing home first would leave a value the user-data rule can no longer see.
        Add(literals, userDataDirectory, UserDataToken);
        Add(literals, homeDirectory, HomeToken);
        Add(literals, userName, UserToken);

        literals.Sort(static (left, right) => right.Key.Length.CompareTo(left.Key.Length));
        return new Redaction(ImmutableArray.CreateRange(literals));
    }

    /// <summary>A redactor with no private values, for a test that only exercises the patterns.</summary>
    internal static Redaction None()
    {
        return new Redaction(ImmutableArray<KeyValuePair<string, string>>.Empty);
    }

    /// <summary>Redacts one field value or message.</summary>
    internal string Apply(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        string redacted = value;
        foreach (KeyValuePair<string, string> literal in _literals)
        {
            redacted = redacted.Replace(literal.Key, literal.Value, StringComparison.Ordinal);
        }

        redacted = SteamId.Replace(redacted, SteamIdToken);
        redacted = AbsolutePath.Replace(redacted, PathToken);
        redacted = Flatten(redacted);

        if (redacted.Length > MaximumFieldLength)
        {
            redacted = redacted[..MaximumFieldLength] + TruncationToken;
        }

        return redacted;
    }

    /// <summary>
    /// Collapses control characters, so one record is one line and uncontrolled text cannot
    /// forge a second record by embedding a newline.
    /// </summary>
    private static string Flatten(string value)
    {
        bool needsWork = false;
        foreach (char character in value)
        {
            if (char.IsControl(character))
            {
                needsWork = true;
                break;
            }
        }

        if (!needsWork)
        {
            return value;
        }

        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            builder.Append(char.IsControl(character) ? ' ' : character);
        }

        return builder.ToString();
    }

    private static void Add(List<KeyValuePair<string, string>> literals, string secret, string token)
    {
        // A one- or two-character "secret" would redact ordinary prose, which is worse than
        // not redacting: it destroys the log without protecting anything.
        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 3)
        {
            literals.Add(new KeyValuePair<string, string>(secret, token));
        }
    }
}
