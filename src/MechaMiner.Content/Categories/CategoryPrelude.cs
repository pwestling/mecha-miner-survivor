using System;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Categories;

/// <summary>
/// The checks every category definition passes before its own typed value pass runs:
/// strict codec, envelope, derived-value register, and the declared field table.
/// </summary>
/// <remarks>
/// <para>
/// The order is not interchangeable and each step stops the read only where continuing
/// would produce invented faults rather than information.
/// </para>
/// <list type="number">
/// <item><description><b>Strict codec and envelope.</b> A document with a duplicate property has no single well-defined value for that property, so every later check would be guessing which one to validate.</description></item>
/// <item><description><b>Derived-value register.</b> Runs <em>before</em> the field table so that authoring a derived value reports the derivation rather than "this field is not declared". Both are true; only one tells the author where the value went.</description></item>
/// <item><description><b>Field table.</b> Unknown, missing, and mistyped fields at every object depth. A kind mismatch stops the read, because the typed DTO cannot deserialize past one.</description></item>
/// </list>
/// <para>
/// Nothing here is relational. Every check reads one document, which is what keeps its
/// verdict independent of the order the catalog was enumerated in.
/// </para>
/// </remarks>
public static class CategoryPrelude
{
    /// <summary>
    /// Runs the common prelude. Returns true when the typed value pass may proceed.
    /// </summary>
    /// <param name="utf8">The document's bytes.</param>
    /// <param name="context">What category the document is read against.</param>
    /// <param name="bag">Where every diagnostic is collected.</param>
    /// <param name="envelope">The validated envelope, or null when anything was reported.</param>
    /// <param name="contentId">
    /// The stable ID every diagnostic from this document names, or null when the ID
    /// itself could not be read. This is not <c>envelope?.Id</c>: see
    /// <see cref="EnvelopeReadResult.Id"/> for why the two are separate.
    /// </param>
    /// <param name="outline">The scanned shape, indexed for the field-table walk.</param>
    /// <param name="structure">The scanned shape.</param>
    /// <exception cref="ArgumentNullException">Any reference argument is null.</exception>
    public static bool Run(
        ReadOnlySpan<byte> utf8,
        CategoryReadContext context,
        DiagnosticBag bag,
        out DefinitionEnvelope? envelope,
        out string? contentId,
        out DocumentOutline outline,
        out JsonStructure structure)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        CategoryDescriptor descriptor = CategorySchemas.Describe(context.Kind);

        EnvelopeReadResult envelopeResult = EnvelopeReader.Read(utf8, context.ForEnvelope());
        foreach (ContentDiagnostic diagnostic in envelopeResult.Diagnostics)
        {
            bag.Add(diagnostic);
        }

        envelope = envelopeResult.Envelope;
        structure = envelopeResult.Structure;
        outline = DocumentOutline.Of(structure);

        // Read from the envelope result rather than from the envelope. The envelope is
        // null whenever any envelope-stage error was reported, so deriving the ID from
        // it would strip the ID off every downstream diagnostic of a document whose only
        // fault is an unknown field. EnvelopeReadResult.Id says why the two are separate.
        contentId = envelopeResult.Id?.Value;

        if (structure.Nodes.Count == 0)
        {
            // The codec could not scan the bytes at all, so there is no shape to walk
            // and every field-table diagnostic would be a restatement of that.
            return false;
        }

        descriptor.Derived.Check(outline, context, contentId, bag);

        bool shapeIsSound = DefinitionShapeValidator.Validate(
            outline, descriptor.Shape, context, contentId, bag);

        return shapeIsSound;
    }
}
