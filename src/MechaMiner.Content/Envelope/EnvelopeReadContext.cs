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
    private static readonly IReadOnlyCollection<string> NoDomainFields = Array.Empty<string>();

    private readonly IReadOnlyCollection<string> _domainFields;

    /// <summary>Creates a read context.</summary>
    /// <param name="sourcePath">The repository-relative path every diagnostic reports.</param>
    /// <param name="category">The category whose ID grammar the definition must satisfy.</param>
    /// <param name="policy">The strict codec policy, defaulting to the definition policy.</param>
    /// <param name="retiredIds">The tombstones an ID may not collide with.</param>
    /// <param name="domainFields">
    /// The root-level field names a category's own field table declares. Supplying them
    /// says a second field table exists and will walk this document's root, which is
    /// what <see cref="OwnsRootFieldTable"/> reads. Omitted, the envelope is the whole
    /// accepted field set, which is what an envelope fixture wants.
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
            : domainFields;
    }

    /// <summary>
    /// True when the envelope is the whole accepted field set for this document, and so
    /// is the layer that owns an unknown property at the root.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A caller that supplied a category's field names has a field table of its own, and
    /// that table walks the same root immediately afterwards, at every object depth and
    /// with the kind's vocabulary in hand. Two layers reporting the same property at the
    /// same pointer is one fault counted twice, and the envelope's wording is the wrong
    /// one of the two to keep: handed a category's names it can only describe a miss in
    /// envelope vocabulary, which sends an author to the envelope for a field the
    /// envelope has nothing to do with.
    /// </para>
    /// <para>
    /// A caller that supplied none is reading an envelope on its own - an envelope
    /// fixture, or a document with no category behind it. Nothing else will walk the
    /// root, so the envelope reports it or nobody does. The condition is derived from
    /// the field table rather than carried as a separate flag, because a flag and a
    /// table can disagree and this cannot.
    /// </para>
    /// </remarks>
    public bool OwnsRootFieldTable => _domainFields.Count == 0;

    /// <summary>The repository-relative path every diagnostic reports.</summary>
    public string SourcePath { get; }

    /// <summary>The category whose ID grammar the definition must satisfy.</summary>
    public ContentCategory Category { get; }

    /// <summary>The strict codec policy, defaulting to the definition policy.</summary>
    public StrictJsonPolicy Policy { get; }

    /// <summary>The tombstones an ID may not collide with.</summary>
    public RetiredIdRegistry RetiredIds { get; }
}
