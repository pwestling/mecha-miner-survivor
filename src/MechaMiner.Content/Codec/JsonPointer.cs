using System;
using System.Globalization;
using System.Text;

namespace MechaMiner.Content.Codec;

/// <summary>
/// An RFC 6901 JSON Pointer identifying one location inside a JSON document.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Compilation pipeline
/// requires every diagnostic to carry the "exact source field". A field is named by
/// a repo-relative file path plus a pointer into that document; a pointer is the
/// only field reference that survives reformatting, which is the same property that
/// § <c>source_refs</c> element grammar demands of a source reference.
/// </para>
/// <para>
/// The escaping order is normative and not interchangeable: RFC 6901 § 4 requires
/// <c>~</c> to become <c>~0</c> <em>before</em> <c>/</c> becomes <c>~1</c>, otherwise
/// a literal <c>~1</c> in a property name and an escaped <c>/</c> become the same
/// text and the pointer stops being reversible.
/// </para>
/// <para>
/// This type is deliberately free of any content-specific concept. Doc 40 § JSON
/// codec and schema baseline states the codec policy is "reused by content, saves,
/// recovery, manifests, diagnostics, and task evidence"; nothing in
/// <c>MechaMiner.Content.Codec</c> may name a content type.
/// </para>
/// </remarks>
public readonly struct JsonPointer : IEquatable<JsonPointer>
{
    private readonly string? _value;

    private JsonPointer(string value)
    {
        _value = value;
    }

    /// <summary>
    /// The pointer to the whole document, which RFC 6901 § 5 writes as the empty
    /// string.
    /// </summary>
    public static JsonPointer Root => default;

    /// <summary>The pointer text, always either empty or beginning with <c>/</c>.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>True when this pointer addresses the whole document.</summary>
    public bool IsRoot => string.IsNullOrEmpty(_value);

    /// <summary>Returns the pointer to <paramref name="propertyName"/> inside this object.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null.</exception>
    public JsonPointer AppendProperty(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        return new JsonPointer(Value + "/" + EscapeToken(propertyName));
    }

    /// <summary>Returns the pointer to element <paramref name="index"/> inside this array.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public JsonPointer AppendIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        // RFC 6901 § 4: an array index token is an unpadded decimal, so "01" is not
        // the same token as "1" and would not resolve.
        return new JsonPointer(Value + "/" + index.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Escapes one reference token per RFC 6901 § 4: <c>~</c> first, then <c>/</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null.</exception>
    public static string EscapeToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.IndexOf('~') < 0 && token.IndexOf('/') < 0)
        {
            return token;
        }

        StringBuilder escaped = new(token.Length + 4);
        foreach (char character in token)
        {
            switch (character)
            {
                case '~':
                    escaped.Append("~0");
                    break;
                case '/':
                    escaped.Append("~1");
                    break;
                default:
                    escaped.Append(character);
                    break;
            }
        }

        return escaped.ToString();
    }

    /// <summary>
    /// Reverses <see cref="EscapeToken"/>. RFC 6901 § 4 requires <c>~1</c> to be
    /// replaced before <c>~0</c>, the mirror of the escaping order.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null.</exception>
    public static string UnescapeToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token.Replace("~1", "/", StringComparison.Ordinal)
            .Replace("~0", "~", StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public bool Equals(JsonPointer other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is JsonPointer other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Ordinal equality.</summary>
    public static bool operator ==(JsonPointer left, JsonPointer right)
    {
        return left.Equals(right);
    }

    /// <summary>Ordinal inequality.</summary>
    public static bool operator !=(JsonPointer left, JsonPointer right)
    {
        return !left.Equals(right);
    }
}
