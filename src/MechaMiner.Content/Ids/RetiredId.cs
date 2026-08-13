using System;

namespace MechaMiner.Content.Ids;

/// <summary>
/// One tombstone: an ID that shipped, was removed, and can never be used again.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Stable ID policy:
/// "Removing shipped content retires its ID and leaves a migration/tombstone entry;
/// IDs are never reassigned."
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Contract
/// change rules says the same thing from the other side: "Stable ID rename/removal -
/// preserve alias/tombstone/migration; never silently reuse".
/// </para>
/// <para>
/// Every field here exists because a save file or a diagnostic seed can outlive the
/// content it names. <see cref="RetiredInContentVersion"/> tells a migration which
/// saves can contain the ID; <see cref="ReplacedBy"/> tells it what to map the ID onto,
/// or states that there is nothing to map to; <see cref="Rationale"/> is what stops the
/// entry from becoming an unexplained line nobody dares delete.
/// </para>
/// </remarks>
public sealed class RetiredId
{
    /// <summary>Records a retirement.</summary>
    /// <exception cref="ArgumentException">
    /// The ID is blank or not valid for its category, the rationale is blank, the
    /// content version is not positive, or a stated replacement is not a valid ID of a
    /// declared category.
    /// </exception>
    public RetiredId(
        string value,
        ContentCategory category,
        int retiredInContentVersion,
        string? replacedBy,
        string rationale)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!ContentId.TryCreate(value, category, out ContentId? id))
        {
            throw new ArgumentException(
                "a tombstone records an ID that was once valid, so '" + value
                    + "' must satisfy its category's grammar: "
                    + ContentCategories.Describe(category).DescribeAcceptedGrammar(),
                nameof(value));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retiredInContentVersion);

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException(
                "a tombstone states why the content was removed; without it the entry becomes a "
                    + "line nobody can safely delete",
                nameof(rationale));
        }

        Id = id!;
        RetiredInContentVersion = retiredInContentVersion;
        ReplacedBy = replacedBy;
        Rationale = rationale;
    }

    /// <summary>The retired ID.</summary>
    public ContentId Id { get; }

    /// <summary>The <c>content_version</c> at which the definition was removed.</summary>
    public int RetiredInContentVersion { get; }

    /// <summary>
    /// The ID a migration maps this one onto, or null when the content was removed with
    /// no successor.
    /// </summary>
    public string? ReplacedBy { get; }

    /// <summary>Why the content was removed.</summary>
    public string Rationale { get; }
}
