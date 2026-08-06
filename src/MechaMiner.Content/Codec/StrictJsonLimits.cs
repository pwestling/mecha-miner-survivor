using System;

namespace MechaMiner.Content.Codec;

/// <summary>
/// The size, depth, and count ceilings the strict codec enforces on one document.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Schema
/// registry: "Schemas reject unknown fields, have structural and semantic
/// validators, enforce size/count/depth limits". A limit exists so that a malformed
/// or hostile document fails with a named diagnostic instead of consuming
/// unbounded time or memory inside the parser.
/// </para>
/// <para>
/// Every default below is stated as a multiple of the largest value observed in the
/// accepted catalog at the time it was chosen, so that the limit is a genuine
/// backstop rather than a number the current data happens to sit under. The
/// observed maxima were measured across all 138 definitions under <c>content/</c>;
/// the largest single definition is
/// <c>content/encounters/standard-encounter-schedule.json</c>.
/// </para>
/// <para>
/// Limits are an instance, not a global, so that a boundary test stays a small
/// reviewable fixture instead of a megabyte of generated JSON. Production callers
/// use <see cref="Default"/>.
/// </para>
/// </remarks>
public sealed class StrictJsonLimits
{
    /// <summary>
    /// Largest accepted source document, in UTF-8 bytes. Observed maximum 41,600
    /// bytes, so this is roughly 25x headroom: an aggregate schedule may grow
    /// substantially without a limit change, while a runaway generated file still
    /// fails fast.
    /// </summary>
    public const int DefaultMaximumDocumentBytes = 1_048_576;

    /// <summary>
    /// Deepest permitted nesting, counting the root object as depth 1. Observed
    /// maximum 7. Thirty-two is deep enough that no plausible authored shape reaches
    /// it and shallow enough that a recursive consumer cannot exhaust its stack.
    /// </summary>
    public const int DefaultMaximumDepth = 32;

    /// <summary>
    /// Most properties in any one object. Observed maximum 30, on
    /// <c>content/enemies/EN-01.json</c>. An object that needs more than 256 fields
    /// is a table that should have been an array of records.
    /// </summary>
    public const int DefaultMaximumObjectProperties = 256;

    /// <summary>
    /// Most elements in any one array. Observed maximum 35, the 35 contiguous
    /// minute rows of the standard encounter schedule. A run mode four times longer
    /// than the accepted one still fits.
    /// </summary>
    public const int DefaultMaximumArrayElements = 1_024;

    /// <summary>
    /// Most JSON values in the whole document, counting every object, array, string,
    /// number, and boolean. Observed maximum 1,253. This is the limit that bounds
    /// total validation work when depth and per-container counts are each
    /// individually legal.
    /// </summary>
    public const int DefaultMaximumNodeCount = 32_768;

    /// <summary>
    /// Longest permitted string value, in UTF-16 characters after unescaping.
    /// Observed maximum 429, a prose rule in the map generation contract. Player-
    /// facing prose belongs in the localization catalog, so a 4,096-character string
    /// in a definition is a smell before it is a resource concern.
    /// </summary>
    public const int DefaultMaximumStringLength = 4_096;

    /// <summary>Creates a limit set, rejecting any nonpositive ceiling.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Any argument is not positive.</exception>
    public StrictJsonLimits(
        int maximumDocumentBytes,
        int maximumDepth,
        int maximumObjectProperties,
        int maximumArrayElements,
        int maximumNodeCount,
        int maximumStringLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDocumentBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumDepth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumObjectProperties);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumArrayElements);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumNodeCount);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumStringLength);

        MaximumDocumentBytes = maximumDocumentBytes;
        MaximumDepth = maximumDepth;
        MaximumObjectProperties = maximumObjectProperties;
        MaximumArrayElements = maximumArrayElements;
        MaximumNodeCount = maximumNodeCount;
        MaximumStringLength = maximumStringLength;
    }

    /// <summary>The limits every production read path uses.</summary>
    public static StrictJsonLimits Default { get; } = new(
        DefaultMaximumDocumentBytes,
        DefaultMaximumDepth,
        DefaultMaximumObjectProperties,
        DefaultMaximumArrayElements,
        DefaultMaximumNodeCount,
        DefaultMaximumStringLength);

    /// <summary>Largest accepted document in UTF-8 bytes.</summary>
    public int MaximumDocumentBytes { get; }

    /// <summary>Deepest accepted nesting, with the root value at depth 1.</summary>
    public int MaximumDepth { get; }

    /// <summary>Most properties accepted in one object.</summary>
    public int MaximumObjectProperties { get; }

    /// <summary>Most elements accepted in one array.</summary>
    public int MaximumArrayElements { get; }

    /// <summary>Most JSON values accepted in the whole document.</summary>
    public int MaximumNodeCount { get; }

    /// <summary>Longest accepted string value in UTF-16 characters.</summary>
    public int MaximumStringLength { get; }
}
