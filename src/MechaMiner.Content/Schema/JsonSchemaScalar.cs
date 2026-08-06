using System;
using System.Globalization;
using System.Text.Json;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Schema;

/// <summary>
/// A scalar JSON value in a normalized form that two instances can be compared by.
/// </summary>
/// <remarks>
/// <para>
/// <c>enum</c> and <c>const</c> compare by JSON structural equality. This evaluator
/// implements that for scalars only, and a composite <c>enum</c> or <c>const</c> value
/// is a <em>load failure</em> rather than a silently weaker comparison - the same rule
/// as an unrecognised keyword, for the same reason.
/// </para>
/// <para>
/// Numbers normalize through <see cref="CanonicalNumber"/> so that <c>1</c>, <c>1.0</c>,
/// and <c>1e0</c> are one value, which is what JSON structural equality requires and
/// what raw-text comparison would get wrong.
/// </para>
/// </remarks>
internal readonly struct JsonSchemaScalar : IEquatable<JsonSchemaScalar>
{
    private JsonSchemaScalar(JsonValueKind kind, string text)
    {
        Kind = kind;
        Text = text;
    }

    internal JsonValueKind Kind { get; }

    internal string Text { get; }

    /// <summary>Normalizes <paramref name="element"/>, or fails for a composite value.</summary>
    internal static bool TryFrom(JsonElement element, out JsonSchemaScalar scalar)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                scalar = new JsonSchemaScalar(JsonValueKind.String, element.GetString() ?? string.Empty);
                return true;

            case JsonValueKind.Number:
                double number = element.GetDouble();
                scalar = new JsonSchemaScalar(
                    JsonValueKind.Number,
                    double.IsFinite(number)
                        ? CanonicalNumber.Format(number)
                        : number.ToString("R", CultureInfo.InvariantCulture));
                return true;

            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                scalar = new JsonSchemaScalar(element.ValueKind, string.Empty);
                return true;

            default:
                scalar = default;
                return false;
        }
    }

    public bool Equals(JsonSchemaScalar other)
    {
        return Kind == other.Kind && string.Equals(Text, other.Text, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is JsonSchemaScalar other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Kind, StringComparer.Ordinal.GetHashCode(Text));
    }

    public override string ToString()
    {
        return Kind switch
        {
            JsonValueKind.String => "\"" + Text + "\"",
            JsonValueKind.Number => Text,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => "null",
        };
    }

    public static bool operator ==(JsonSchemaScalar left, JsonSchemaScalar right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JsonSchemaScalar left, JsonSchemaScalar right)
    {
        return !left.Equals(right);
    }
}
