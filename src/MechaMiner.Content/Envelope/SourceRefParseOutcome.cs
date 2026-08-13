namespace MechaMiner.Content.Envelope;

/// <summary>The result of parsing one <c>source_refs</c> element.</summary>
public enum SourceRefParseOutcome
{
    /// <summary>The element matched the grammar and was parsed.</summary>
    Parsed = 0,

    /// <summary>The element did not match the grammar.</summary>
    Malformed = 1,

    /// <summary>
    /// The element is a file path or a <c>path:line</c> pair. Doc 40 rejects these by
    /// name and explains why, so they get their own outcome rather than being lumped in
    /// with everything else that failed to parse: an author who wrote one needs to be
    /// told that the reference has to become a stable ID, not that they made a typo.
    /// </summary>
    PathLine = 2,
}
