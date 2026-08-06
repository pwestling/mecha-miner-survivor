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
}
