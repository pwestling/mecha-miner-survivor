using System;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Categories;

/// <summary>Everything a category reader needs besides the bytes.</summary>
/// <remarks>
/// The kind is supplied rather than inferred from the document, for the same reason
/// <see cref="EnvelopeReadContext"/> is told its category: inferring it would turn a
/// mistyped discriminator into a definition of a different shape rather than into an
/// error. Doc 40 § Accepted content repository layout makes catalog directories "the
/// authoring boundary", so the caller that enumerated the directory knows the kind and
/// the document is checked against it.
/// </remarks>
public sealed class CategoryReadContext
{
    /// <summary>Creates a read context.</summary>
    /// <exception cref="ArgumentException"><paramref name="sourcePath"/> is blank.</exception>
    public CategoryReadContext(
        string sourcePath,
        DefinitionKind kind,
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
        Kind = kind;
        Policy = policy ?? StrictJsonPolicy.Definitions;
        RetiredIds = retiredIds ?? RetiredIdRegistry.Shipped;
    }

    /// <summary>The repository-relative path every diagnostic reports.</summary>
    public string SourcePath { get; }

    /// <summary>The definition kind whose field table the document must satisfy.</summary>
    public DefinitionKind Kind { get; }

    /// <summary>The strict codec policy, defaulting to the definition policy.</summary>
    public StrictJsonPolicy Policy { get; }

    /// <summary>The tombstones an ID may not collide with.</summary>
    public RetiredIdRegistry RetiredIds { get; }

    /// <summary>The authoring category this kind belongs to.</summary>
    public ContentCategory Category => CategorySchemas.Describe(Kind).Category;

    /// <summary>Builds the envelope context for this read.</summary>
    public EnvelopeReadContext ForEnvelope()
    {
        CategoryDescriptor descriptor = CategorySchemas.Describe(Kind);
        return new EnvelopeReadContext(
            SourcePath,
            descriptor.Category,
            Policy,
            RetiredIds,
            descriptor.Shape.FieldNames());
    }
}
