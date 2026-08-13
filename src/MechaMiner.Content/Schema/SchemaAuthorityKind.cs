namespace MechaMiner.Content.Schema;

/// <summary>Where a numeric bound in a schema comes from.</summary>
/// <remarks>
/// The three kinds answer different maintenance questions, which is why they are
/// distinguished rather than collapsed into "has a citation" and "does not".
/// </remarks>
public enum SchemaAuthorityKind
{
    /// <summary>
    /// The number comes from a document and must be re-derived when that document
    /// changes. This is the kind a staleness check follows.
    /// </summary>
    Sourced = 0,

    /// <summary>
    /// The number follows from other content and moves when its operands move. It is
    /// not independently authored, so editing it directly creates a second source of
    /// truth.
    /// </summary>
    Derived = 1,

    /// <summary>
    /// An implementation limit with no external authority - a depth cap, an element
    /// ceiling. It still needs a rationale in <c>description</c>, because a limit nobody
    /// can justify is indistinguishable from one chosen to make something pass.
    /// </summary>
    Structural = 2,
}
