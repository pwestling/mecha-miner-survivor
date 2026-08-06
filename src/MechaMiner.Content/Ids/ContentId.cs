using System;

namespace MechaMiner.Content.Ids;

/// <summary>
/// A stable content ID that has been checked against its category's grammar.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Stable ID policy: "IDs
/// are case-sensitive ASCII tokens matching a schema pattern and never localized."
/// </para>
/// <para>
/// The ID carries its category. Doc 40's last bullet in that section requires
/// "Cross-references contain IDs plus schema-validated expected category where
/// ambiguity is possible", and a bare string cannot satisfy that: <c>W-AB</c> as a
/// weapon and <c>W-AB</c> as the prefix of a branch are different references, and only
/// the category distinguishes them. Comparisons are ordinal <em>and</em>
/// category-aware for the same reason.
/// </para>
/// </remarks>
public sealed class ContentId : IEquatable<ContentId>
{
    private ContentId(string value, ContentCategory category)
    {
        Value = value;
        Category = category;
    }

    /// <summary>The ID token, exactly as authored.</summary>
    public string Value { get; }

    /// <summary>The category whose grammar accepted the token.</summary>
    public ContentCategory Category { get; }

    /// <summary>
    /// Creates an ID when <paramref name="value"/> matches the grammar of
    /// <paramref name="category"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not declared.</exception>
    public static bool TryCreate(string value, ContentCategory category, out ContentId? id)
    {
        ArgumentNullException.ThrowIfNull(value);

        ContentCategoryDescriptor descriptor = ContentCategories.Describe(category);
        if (!descriptor.Accepts(value))
        {
            id = null;
            return false;
        }

        id = new ContentId(value, category);
        return true;
    }

    /// <summary>Creates an ID or throws.</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> does not match the grammar.</exception>
    public static ContentId Create(string value, ContentCategory category)
    {
        if (!TryCreate(value, category, out ContentId? id))
        {
            throw new ArgumentException(
                "'" + value + "' is not a valid ID: "
                    + ContentCategories.Describe(category).DescribeAcceptedGrammar(),
                nameof(value));
        }

        return id!;
    }

    /// <inheritdoc/>
    public bool Equals(ContentId? other)
    {
        return other is not null
            && Category == other.Category
            && string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ContentId);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(StringComparer.Ordinal.GetHashCode(Value), Category);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Value;
    }
}
