using System;
using System.Text.RegularExpressions;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// A parsed localization key of the form
/// <c>&lt;category&gt;.&lt;stable_id&gt;.&lt;role&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Common definition
/// envelope requires <c>name_key</c> to be a "localization key; never literal
/// player-facing text", and § Source catalog format and key pattern fixes the shape:
/// "The key pattern is <c>&lt;category&gt;.&lt;stable_id&gt;.&lt;role&gt;</c>. The
/// category is <c>snake_case</c>. The stable ID appears <b>verbatim</b>, in its own
/// case, so <c>weapon.W-AB.name</c> and not <c>weapon.w_ab.name</c>."
/// </para>
/// <para>
/// <b>How this catches literal text.</b> There is no way to ask a string whether a
/// human wrote it, so the check is structural: a key must have exactly three
/// dot-separated parts, a <c>snake_case</c> category, an ASCII stable ID, and a role
/// from the accepted set. "Fracture Lance" has a space, no dots, and no role, so it
/// fails - and so does every other phrase a person would type into the field by
/// mistake.
/// </para>
/// <para>
/// <b>What is deliberately not checked here.</b> Whether the stable ID part equals the
/// definition's own <c>id</c>, and whether the key exists in
/// <c>content/localization/en.json</c>. Both are relational rules - doc 40
/// § Relational lists "asset and localization existence" - and belong to the
/// cross-reference validator owned by <c>DAT-005</c>. Doing them here would mean the
/// envelope validator needed the whole catalog in hand to check one definition.
/// </para>
/// </remarks>
public sealed class LocalizationKey
{
    private const string CategoryPart = "[a-z][a-z0-9_]*";

    /// <remarks>
    /// The stable ID appears verbatim in its own case, so this part is deliberately
    /// case-permissive where the category part is not: <c>weapon.W-AB.name</c> is
    /// correct and <c>weapon.w_ab.name</c> is not, because a key that transforms an ID
    /// is no longer traceable to it.
    /// </remarks>
    private const string StableIdPart = "[A-Za-z0-9][A-Za-z0-9_-]*";

    private const string Prefix = "^" + CategoryPart + "\\." + StableIdPart + "\\.";

    /// <summary>The pattern for a key in any role.</summary>
    public const string Pattern = Prefix + "(name|summary)$";

    /// <summary>
    /// The pattern for a <c>name_key</c>, mirrored verbatim in
    /// <c>content/schemas/envelope.schema.json</c>.
    /// </summary>
    /// <remarks>
    /// The per-role patterns exist so the schema can enforce the role/field match that
    /// the typed validator reports as <c>MMC-2008</c>. A single shared pattern would
    /// let the schema accept <c>"name_key": "weapon.W-AB.summary"</c> while the typed
    /// validator rejected it, and the fixture corpus would then be proving a
    /// disagreement rather than an agreement.
    /// </remarks>
    public const string NamePattern = Prefix + "name$";

    /// <summary>The pattern for a <c>summary_key</c>, mirrored verbatim in the schema.</summary>
    public const string SummaryPattern = Prefix + "summary$";

    /// <summary>The pattern a key in <paramref name="role"/> must match.</summary>
    public static string PatternFor(LocalizationRole role)
    {
        return role switch
        {
            LocalizationRole.Name => NamePattern,
            LocalizationRole.Summary => SummaryPattern,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "unknown role"),
        };
    }

    private static readonly Regex KeyPattern = AnchoredPattern.Compile(Pattern);

    private LocalizationKey(string value, string category, string stableId, LocalizationRole role)
    {
        Value = value;
        Category = category;
        StableId = stableId;
        Role = role;
    }

    /// <summary>The whole key, as authored.</summary>
    public string Value { get; }

    /// <summary>The <c>snake_case</c> category part.</summary>
    public string Category { get; }

    /// <summary>The stable ID part, verbatim in its own case.</summary>
    public string StableId { get; }

    /// <summary>The role part.</summary>
    public LocalizationRole Role { get; }

    /// <summary>Parses a localization key.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static bool TryParse(string value, out LocalizationKey? key)
    {
        ArgumentNullException.ThrowIfNull(value);

        key = null;
        if (!KeyPattern.IsMatch(value))
        {
            return false;
        }

        int lastDot = value.LastIndexOf('.');
        int firstDot = value.IndexOf('.', StringComparison.Ordinal);

        string category = value[..firstDot];
        string stableId = value[(firstDot + 1)..lastDot];
        string roleToken = value[(lastDot + 1)..];

        // The pattern already restricted the role to one of two tokens, so this switch
        // is total and its default is unreachable rather than tolerant.
        LocalizationRole role = roleToken switch
        {
            "name" => LocalizationRole.Name,
            "summary" => LocalizationRole.Summary,
            _ => throw new InvalidOperationException(
                "role token '" + roleToken + "' matched the key pattern but has no role"),
        };

        key = new LocalizationKey(value, category, stableId, role);
        return true;
    }

    /// <summary>The role a given envelope field's key must carry.</summary>
    public static LocalizationRole RoleForField(string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        return fieldName switch
        {
            EnvelopeSchema.NameKey => LocalizationRole.Name,
            EnvelopeSchema.SummaryKey => LocalizationRole.Summary,
            _ => throw new ArgumentException(
                "'" + fieldName + "' is not a localization key field",
                nameof(fieldName)),
        };
    }

    /// <summary>The token for <paramref name="role"/>, as it appears in a key.</summary>
    public static string ToToken(LocalizationRole role)
    {
        return role switch
        {
            LocalizationRole.Name => "name",
            LocalizationRole.Summary => "summary",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "unknown role"),
        };
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Value;
    }
}
