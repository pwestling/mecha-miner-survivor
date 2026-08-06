using System.Collections.Generic;

namespace MechaMiner.Content.Codec;

/// <summary>The outcome of one <see cref="StrictJsonReader"/> pass.</summary>
public sealed class StrictJsonScanResult
{
    internal StrictJsonScanResult(
        IReadOnlyList<StrictJsonViolation> violations,
        JsonStructure structure)
    {
        Violations = violations;
        Structure = structure;
    }

    /// <summary>Every codec-level fault found, in document order.</summary>
    public IReadOnlyList<StrictJsonViolation> Violations { get; }

    /// <summary>
    /// The document's shape. Partial when the scan stopped early, which happens only
    /// on a fault that makes the remainder meaningless.
    /// </summary>
    public JsonStructure Structure { get; }

    /// <summary>True when the document satisfies the whole strict-codec policy.</summary>
    public bool IsValid => Violations.Count == 0;
}
