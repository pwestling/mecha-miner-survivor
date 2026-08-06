using System;
using System.Globalization;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The canonical textual form of a number in a canonical payload.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline: the canonical writer "writes integers without padding and finite
/// floating-point values with invariant round-trip representation, normalizing
/// negative zero to zero."
/// </para>
/// <para>
/// <b>Why negative zero must be normalized.</b> <c>-0.0</c> and <c>0.0</c> compare
/// equal under <c>==</c> but have different bit patterns and different textual forms.
/// If either could reach the payload, two runs that computed the same value by
/// different arithmetic would produce different bytes and therefore different
/// SHA-256 digests, which is precisely the property doc 40 § Compilation pipeline
/// says the hash must not have.
/// </para>
/// <para>
/// <b>Why "R" and not the default.</b> Since .NET Core 3.0 the default
/// <c>double.ToString(IFormatProvider)</c> is already shortest-round-trippable, so
/// the two agree; <c>"R"</c> is used because it is the specifier that <em>documents</em>
/// the requirement, and a test asserts the two forms agree on hard values so the
/// equivalence is verified rather than assumed.
/// </para>
/// </remarks>
public static class CanonicalNumber
{
    /// <summary>Formats an integer with no padding, no separators, and no sign on zero.</summary>
    public static string Format(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a finite double in invariant shortest round-trip form, with negative
    /// zero normalized to zero.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> is NaN or an infinity. A nonfinite number cannot be
    /// written, because it cannot be read back: doc 40 makes it a codec error on the
    /// way in, so producing one on the way out would create a payload the codec
    /// itself rejects.
    /// </exception>
    public static string Format(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "a canonical payload contains only finite numbers (doc 40 § JSON codec and "
                    + "schema baseline)");
        }

        return NormalizeNegativeZero(value).ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns <paramref name="value"/> with negative zero replaced by positive zero
    /// and every other value unchanged.
    /// </summary>
    public static double NormalizeNegativeZero(double value)
    {
        // -0.0 == 0.0 is true, so this branch catches exactly the two zeros and
        // returns the positive one for both.
        return value == 0.0 ? 0.0 : value;
    }
}
