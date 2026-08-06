using System.Collections.Generic;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Envelope;

/// <summary>The outcome of reading one source definition's envelope.</summary>
/// <remarks>
/// The result carries no exit class and no build verdict. Exit classes are a
/// build-tool contract owned by <c>docs/technical/100</c> § Standard command surface;
/// a pure library that knew about them would make <c>MechaMiner.Content</c> depend on
/// the CLI's vocabulary. A verb maps <see cref="ContentDiagnosticSeverity"/> onto an
/// exit class at the boundary.
/// </remarks>
public sealed class EnvelopeReadResult
{
    internal EnvelopeReadResult(
        DefinitionEnvelope? envelope,
        ContentId? id,
        IReadOnlyList<ContentDiagnostic> diagnostics,
        JsonStructure structure)
    {
        Envelope = envelope;
        Id = id;
        Diagnostics = diagnostics;
        Structure = structure;
    }

    /// <summary>The validated envelope, or null when any error was reported.</summary>
    public DefinitionEnvelope? Envelope { get; }

    /// <summary>
    /// The definition's stable ID, whether or not an envelope was produced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is deliberately <em>not</em> <c>Envelope?.Id</c>. Doc 40 § Compilation
    /// pipeline requires the content ID on every diagnostic, and
    /// <see cref="ContentDiagnostic.ContentId"/> documents null as meaning the document
    /// was too broken for its ID to be read. <see cref="Envelope"/> is null whenever
    /// <em>any</em> envelope-stage error was reported, including faults that say nothing
    /// about the ID - an unknown root field, a tag outside the vocabulary, a malformed
    /// source ref. Deriving the reported ID from the envelope therefore silently makes
    /// every unrelated error claim the ID is unreadable.
    /// </para>
    /// <para>
    /// The two are separate because they answer separate questions. The envelope is a
    /// model the rest of the compiler consumes, so it exists only for a document that
    /// validated. The ID is diagnostic metadata, and its readability is a property of
    /// the <c>id</c> field alone. Keeping them independent by construction is what stops
    /// a later envelope-stage check from taking the ID away again as a side effect: no
    /// list of "ID-affecting" codes has to be maintained, because there is no list.
    /// </para>
    /// <para>
    /// It is null when the codec could not scan the document, when the envelope's own
    /// shape is unsound so the typed read cannot run at all, or when <c>id</c> is
    /// absent, malformed for its category, or retired. Each of those is a case where no
    /// usable stable ID exists to report.
    /// </para>
    /// </remarks>
    public ContentId? Id { get; }

    /// <summary>Every diagnostic produced, in the order the stages produced them.</summary>
    public IReadOnlyList<ContentDiagnostic> Diagnostics { get; }

    /// <summary>The scanned document shape, for a caller that needs to resolve pointers.</summary>
    public JsonStructure Structure { get; }

    /// <summary>True when the definition validated and an envelope was produced.</summary>
    public bool IsValid => Envelope is not null;
}
