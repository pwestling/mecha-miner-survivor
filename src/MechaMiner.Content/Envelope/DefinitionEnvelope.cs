using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// A validated <c>SCH-CNT-001</c> envelope: the nine fields every independently
/// addressable definition carries.
/// </summary>
/// <remarks>
/// <para>
/// An instance exists only if every field passed validation, so a consumer never has to
/// re-check one. <c>docs/technical/115-component-contract-and-schema-registry.md</c>
/// § Schema registry: "Runtime code consumes typed validated values rather than generic
/// JSON trees."
/// </para>
/// <para>
/// A declared-optional field is <c>null</c> here and only here: <c>null</c> in the C#
/// model means "the author omitted the key", which is the one thing a JSON
/// <c>null</c> is never allowed to mean in a source file.
/// <see cref="WriteCanonical"/> materializes
/// <see cref="EnvelopeSchema.AbsentOptionalDefault"/> in its place, so the canonical
/// payload has no absent fields at all.
/// </para>
/// </remarks>
public sealed class DefinitionEnvelope
{
    internal DefinitionEnvelope(
        ContentId id,
        int schemaVersion,
        int contentVersion,
        DefinitionStatus status,
        LocalizationKey? nameKey,
        LocalizationKey? summaryKey,
        IReadOnlyList<string> tags,
        IReadOnlyList<SourceRef> sourceRefs,
        string? presentationId)
    {
        Id = id;
        SchemaVersion = schemaVersion;
        ContentVersion = contentVersion;
        Status = status;
        NameKey = nameKey;
        SummaryKey = summaryKey;
        Tags = tags;
        SourceRefs = sourceRefs;
        PresentationId = presentationId;
    }

    /// <summary>The stable category-valid ID.</summary>
    public ContentId Id { get; }

    /// <summary>The integer version of this definition's schema.</summary>
    public int SchemaVersion { get; }

    /// <summary>The monotonic revision of this definition.</summary>
    public int ContentVersion { get; }

    /// <summary>The lifecycle state.</summary>
    public DefinitionStatus Status { get; }

    /// <summary>The name key, or null when the definition has no player-facing name.</summary>
    public LocalizationKey? NameKey { get; }

    /// <summary>The summary key, or null when no concise summary is relevant.</summary>
    public LocalizationKey? SummaryKey { get; }

    /// <summary>The tags, from the closed vocabulary. Empty for every current definition.</summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>The parsed source references, in authored order.</summary>
    public IReadOnlyList<SourceRef> SourceRefs { get; }

    /// <summary>
    /// The logical presentation entry, or null when the definition never appears
    /// in-world.
    /// </summary>
    public string? PresentationId { get; }

    /// <summary>
    /// True when a release bundle excludes this definition unless configured otherwise
    /// (doc 40 § Common definition envelope).
    /// </summary>
    public bool IsExcludedFromReleaseByDefault =>
        Status is DefinitionStatus.Development or DefinitionStatus.Disabled;

    /// <summary>
    /// Writes the envelope's canonical form: fields in schema-declared order, tags as a
    /// canonical ID set, source refs in their authored order, and every
    /// declared-optional field materialized.
    /// </summary>
    /// <remarks>
    /// The three orderings here are not interchangeable and are chosen per field.
    /// <c>tags</c> is an unordered set of stable terms, so it is emitted in canonical
    /// order and a duplicate is a write failure. <c>source_refs</c> is
    /// <em>semantically ordered</em>: an author lists the whole-definition sources first
    /// and then the per-field ones, and sorting would destroy that reading order, so it
    /// is emitted exactly as authored.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is null.</exception>
    public void WriteCanonical(CanonicalJsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.BeginObject(EnvelopeSchema.Order);
        writer.WriteString(EnvelopeSchema.Id, Id.Value);
        writer.WriteInteger(EnvelopeSchema.SchemaVersion, SchemaVersion);
        writer.WriteInteger(EnvelopeSchema.ContentVersion, ContentVersion);
        writer.WriteString(EnvelopeSchema.Status, DefinitionStatuses.ToToken(Status));
        writer.WriteString(
            EnvelopeSchema.NameKey,
            NameKey?.Value ?? EnvelopeSchema.AbsentOptionalDefault);
        writer.WriteString(
            EnvelopeSchema.SummaryKey,
            SummaryKey?.Value ?? EnvelopeSchema.AbsentOptionalDefault);
        writer.WriteIdSet(EnvelopeSchema.Tags, Tags);
        writer.WriteOrderedArray(
            EnvelopeSchema.SourceRefs,
            SourceRefs,
            static (target, sourceRef) => target.WriteStringValue(sourceRef.Text));
        writer.WriteString(
            EnvelopeSchema.PresentationId,
            PresentationId ?? EnvelopeSchema.AbsentOptionalDefault);
        writer.EndObject();
    }

    /// <summary>The canonical payload bytes of this envelope.</summary>
    public byte[] ToCanonicalUtf8()
    {
        return CanonicalJson.Serialize(WriteCanonical);
    }

    /// <summary>The SHA-256 hex digest of this envelope's canonical payload.</summary>
    public string CanonicalSha256Hex()
    {
        return CanonicalHash.Sha256Hex(ToCanonicalUtf8());
    }

    /// <summary>Wraps a list so the envelope's collections cannot be mutated by a consumer.</summary>
    internal static IReadOnlyList<T> Freeze<T>(List<T> values)
    {
        return new ReadOnlyCollection<T>(values);
    }
}
