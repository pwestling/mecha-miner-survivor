using System;
using System.Collections.Generic;
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
    private static readonly HashSet<string> NoDomainFields = new(StringComparer.Ordinal);

    private readonly HashSet<string> _domainFields;

    /// <summary>Creates a read context.</summary>
    /// <param name="sourcePath">The repository-relative path every diagnostic reports.</param>
    /// <param name="category">The category whose ID grammar the definition must satisfy.</param>
    /// <param name="policy">The strict codec policy, defaulting to the definition policy.</param>
    /// <param name="retiredIds">The tombstones an ID may not collide with.</param>
    /// <param name="domainFields">
    /// The root-level field names a category's own field table declares. The envelope
    /// reader accepts them without asserting anything about them, so that reading the
    /// envelope of a full definition does not report every domain field as unknown.
    /// Omitted, the envelope is the whole accepted field set, which is what an envelope
    /// fixture wants.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="sourcePath"/> is blank.</exception>
    public EnvelopeReadContext(
        string sourcePath,
        ContentCategory category,
        StrictJsonPolicy? policy = null,
        RetiredIdRegistry? retiredIds = null,
        IReadOnlyCollection<string>? domainFields = null)
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
        _domainFields = domainFields is null || domainFields.Count == 0
            ? NoDomainFields
            : new HashSet<string>(domainFields, StringComparer.Ordinal);
    }

    /// <summary>
    /// True when <paramref name="field"/> is a domain field the owning category
    /// declares, and so is not the envelope reader's to reject.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is null.</exception>
    public bool DeclaresDomainField(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return _domainFields.Contains(field);
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
