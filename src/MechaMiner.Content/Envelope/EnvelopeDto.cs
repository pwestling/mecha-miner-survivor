using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// The wire shape of the nine envelope fields, for source-generated deserialization.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline: "Use the built-in <c>System.Text.Json</c> reader/writer with explicit
/// typed DTOs and source-generated serialization metadata". This is that explicit typed
/// DTO; <see cref="EnvelopeJsonContext"/> is the generated metadata.
/// </para>
/// <para>
/// The type is <c>internal</c> deliberately. It is a transport shape with settable
/// properties and nullable everything, which is the opposite of what a consumer should
/// hold; <see cref="DefinitionEnvelope"/> is the public model and is immutable and
/// fully validated. Nothing outside this namespace ever sees a half-checked value.
/// </para>
/// <para>
/// <b>Why the versions are <c>double?</c> and not <c>int?</c>.</b> An <c>int?</c>
/// property turns <c>"schema_version": 1.5</c> into a deserialization exception, which
/// has no JSON Pointer and no diagnostic code. Read as a JSON number and checked for
/// integrality by the validator, the same input produces
/// <c>MMC-2005</c> pointing at <c>/schema_version</c>. The rule is general: the DTO
/// accepts everything the JSON type system allows and the validator decides, so that
/// every rejection is a diagnostic rather than an exception.
/// </para>
/// <para>
/// Unmapped members are left at the default handling rather than
/// <c>JsonUnmappedMemberHandling.Disallow</c>, for the same reason: the validator
/// detects an unknown field from the scanned structure and reports
/// <c>MMC-2001</c> with its exact pointer, which a thrown exception could not.
/// </para>
/// </remarks>
internal sealed class EnvelopeDto
{
    [JsonPropertyName(EnvelopeSchema.Id)]
    public string? Id { get; set; }

    [JsonPropertyName(EnvelopeSchema.SchemaVersion)]
    public double? SchemaVersion { get; set; }

    [JsonPropertyName(EnvelopeSchema.ContentVersion)]
    public double? ContentVersion { get; set; }

    [JsonPropertyName(EnvelopeSchema.Status)]
    public string? Status { get; set; }

    [JsonPropertyName(EnvelopeSchema.NameKey)]
    public string? NameKey { get; set; }

    [JsonPropertyName(EnvelopeSchema.SummaryKey)]
    public string? SummaryKey { get; set; }

    [JsonPropertyName(EnvelopeSchema.Tags)]
    public List<string>? Tags { get; set; }

    [JsonPropertyName(EnvelopeSchema.SourceRefs)]
    public List<string>? SourceRefs { get; set; }

    [JsonPropertyName(EnvelopeSchema.PresentationId)]
    public string? PresentationId { get; set; }
}
