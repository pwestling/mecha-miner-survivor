using System;

namespace MechaMiner.Content.Codec;

/// <summary>
/// One codec-level fault, located precisely enough for a caller to build a
/// diagnostic from it without re-reading the document.
/// </summary>
/// <remarks>
/// The violation carries a JSON Pointer where one exists and a byte offset always.
/// A lexical fault such as a comment or a trailing comma occurs between values and
/// therefore has no meaningful pointer; the offset is the only exact location, so
/// both are recorded rather than one.
/// </remarks>
public sealed class StrictJsonViolation
{
    /// <summary>Creates a violation.</summary>
    /// <exception cref="ArgumentException"><paramref name="expectedConstraint"/> is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteOffset"/> is negative.</exception>
    public StrictJsonViolation(
        StrictJsonViolationKind kind,
        JsonPointer location,
        long byteOffset,
        string expectedConstraint)
    {
        if (string.IsNullOrWhiteSpace(expectedConstraint))
        {
            throw new ArgumentException(
                "a violation must state the constraint that was expected; doc 40 § Compilation "
                + "pipeline requires the expected constraint in every diagnostic",
                nameof(expectedConstraint));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(byteOffset);

        Kind = kind;
        Location = location;
        ByteOffset = byteOffset;
        ExpectedConstraint = expectedConstraint;
    }

    /// <summary>What went wrong.</summary>
    public StrictJsonViolationKind Kind { get; }

    /// <summary>
    /// Where in the document, as an RFC 6901 pointer. <see cref="JsonPointer.Root"/>
    /// for a fault that is not attributable to one value.
    /// </summary>
    public JsonPointer Location { get; }

    /// <summary>The zero-based byte offset the fault begins at.</summary>
    public long ByteOffset { get; }

    /// <summary>The constraint the document was expected to satisfy.</summary>
    public string ExpectedConstraint { get; }
}
