namespace MechaMiner.Content.Envelope;

/// <summary>
/// The role part of a localization key.
/// </summary>
/// <remarks>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Source catalog format and
/// key pattern: "The role comes from a small set, beginning with <c>name</c> and
/// <c>summary</c>, matching the <c>name_key</c> and <c>summary_key</c> envelope fields.
/// The set grows with the same discipline as the <c>tags</c> vocabulary: a role is
/// added when a definition or a UI surface needs it, not in advance."
/// </remarks>
public enum LocalizationRole
{
    /// <summary>A definition's player-facing name. Carried by <c>name_key</c>.</summary>
    Name = 0,

    /// <summary>A definition's concise player-facing summary. Carried by <c>summary_key</c>.</summary>
    Summary = 1,

    /// <summary>
    /// A relic's one-phrase statement of what it transforms. Carried by
    /// <c>transformation_key</c>.
    /// </summary>
    /// <remarks>
    /// Added because a definition needs it, which is the discipline the section above
    /// sets. <c>docs/technical/40-content-data-and-validation.md</c> § Relics requires a
    /// relic to state an explicit tradeoff and a one-sentence summary; those are two
    /// strings a player reads on the relic-cache screen, and the string catalog holds
    /// strings players read. Without a role for each, the relic would have to carry the
    /// sentences as literals in the definition.
    /// </remarks>
    Transformation = 2,

    /// <summary>
    /// A relic's explicit tradeoff. Carried by <c>tradeoff_key</c>.
    /// </summary>
    /// <remarks>
    /// A separate role from <see cref="Transformation"/> rather than a second summary,
    /// because doc 40 § Relics validates the tradeoff's presence specifically: folding
    /// it into the summary would remove the thing the validator checks.
    /// </remarks>
    Tradeoff = 3,
}
