using System;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Envelope;

/// <summary>Everything <see cref="EnvelopeReader"/> needs besides the bytes.</summary>
/// <remarks>
/// The category is supplied rather than inferred from the ID, because inferring it
/// would make a mistyped ID silently become a definition of a different category
/// instead of an error. Doc 40 § Accepted content repository layout makes "Catalog
/// directories are the authoring boundary", so the directory is the authority on what
/// category a file belongs to and the ID is checked against it.
/// </remarks>
public sealed class EnvelopeReadContext
{
    /// <summary>Creates a read context.</summary>
    /// <exception cref="ArgumentException"><paramref name="sourcePath"/> is blank.</exception>
    public EnvelopeReadContext(
        string sourcePath,
        ContentCategory category,
        StrictJsonPolicy? policy = null,
        RetiredIdRegistry? retiredIds = null)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException(
                "a diagnostic names its exact source path, so the reader must be told one",
                nameof(sourcePath));
        }

        SourcePath = sourcePath;
        Category = category;
        Policy = policy ?? StrictJsonPolicy.Definitions;
        RetiredIds = retiredIds ?? RetiredIdRegistry.Shipped;
    }

    /// <summary>The repository-relative path every diagnostic reports.</summary>
    public string SourcePath { get; }

    /// <summary>The category whose ID grammar the definition must satisfy.</summary>
    public ContentCategory Category { get; }

    /// <summary>The strict codec policy, defaulting to the definition policy.</summary>
    public StrictJsonPolicy Policy { get; }

    /// <summary>The tombstones an ID may not collide with.</summary>
    public RetiredIdRegistry RetiredIds { get; }
}
