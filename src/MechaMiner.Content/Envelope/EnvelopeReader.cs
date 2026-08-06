using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// Reads and validates the <c>SCH-CNT-001</c> envelope of one source definition.
/// </summary>
/// <remarks>
/// <para>
/// This is the project-owned typed structural validator that
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline calls "authoritative". <c>content/schemas/envelope.schema.json</c> is the
/// draft 2020-12 mirror kept for editor and tool interoperability, and a fixture
/// corpus proves the two reach the same verdict on every structural case.
/// </para>
/// <para>
/// The stages run in the order doc 40 § Compilation pipeline draws them, and each
/// stage stops the pipeline only when continuing would produce noise rather than
/// information:
/// </para>
/// <list type="number">
/// <item><description>Strict codec scan. Anything here stops the read: a document with a duplicate property has no single well-defined value for that property, so every later check would be guessing which one to validate.</description></item>
/// <item><description>Structural checks over the scanned shape - unknown fields, missing required fields, value kinds. A kind mismatch stops the read, because the typed DTO cannot deserialize past one.</description></item>
/// <item><description>Typed deserialization, then value checks - ID grammar and retirement, versions, status, localization keys, tags, source refs and their scopes. These all run, so an author sees every fault in one pass.</description></item>
/// </list>
/// </remarks>
public static class EnvelopeReader
{
    /// <summary>Reads one definition's envelope.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public static EnvelopeReadResult Read(ReadOnlySpan<byte> utf8, EnvelopeReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DiagnosticBag bag = new();

        StrictJsonScanResult scan = StrictJsonReader.Scan(utf8, context.Policy);
        if (!scan.IsValid)
        {
            foreach (StrictJsonViolation violation in scan.Violations)
            {
                bag.Add(StrictJsonDiagnostics.ToDiagnostic(violation, context.SourcePath, null));
            }

            return new EnvelopeReadResult(null, bag.Diagnostics, scan.Structure);
        }

        if (!ValidateShape(scan.Structure, context, bag))
        {
            return new EnvelopeReadResult(null, bag.Diagnostics, scan.Structure);
        }

        EnvelopeDto? dto = JsonSerializer.Deserialize(utf8, EnvelopeJsonContext.Default.EnvelopeDto);
        if (dto is null)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.MalformedJson,
                context.SourcePath,
                JsonPointer.Root,
                null,
                "the document must deserialize into a definition envelope"));
            return new EnvelopeReadResult(null, bag.Diagnostics, scan.Structure);
        }

        DefinitionEnvelope? envelope = ValidateValues(dto, scan.Structure, context, bag);
        return new EnvelopeReadResult(envelope, bag.Diagnostics, scan.Structure);
    }

    /// <summary>
    /// Checks the document's shape against the nine declared fields. Returns false when
    /// a kind mismatch makes deserialization unsafe.
    /// </summary>
    private static bool ValidateShape(
        JsonStructure structure,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        bool kindsAreSound = true;

        foreach (string name in structure.RootPropertyNames)
        {
            if (EnvelopeSchema.Declares(name))
            {
                continue;
            }

            // A domain field belongs to the owning category's field table, which the
            // category reader walks straight after this. Reporting it here would make
            // every category definition fail with one unknown-field diagnostic per
            // domain field, drowning the real fault.
            if (context.DeclaresDomainField(name))
            {
                continue;
            }

            // doc 40 § Common definition envelope: "Unknown fields are errors rather
            // than silently ignored."
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.UnknownField,
                context.SourcePath,
                JsonPointer.Root.AppendProperty(name),
                null,
                "the envelope declares exactly these fields: "
                    + string.Join(", ", EnvelopeSchema.Fields)));
        }

        foreach (string field in EnvelopeSchema.Fields)
        {
            JsonPointer pointer = JsonPointer.Root.AppendProperty(field);
            if (!structure.TryGetKind(pointer, out JsonValueKind kind))
            {
                if (EnvelopeSchema.IsRequired(field))
                {
                    bag.Add(ContentDiagnostic.CreateError(
                        ContentDiagnosticCodes.RequiredFieldMissing,
                        context.SourcePath,
                        pointer,
                        null,
                        "'" + field + "' is required; the declared-optional fields are "
                            + string.Join(", ", EnvelopeSchema.DeclaredOptional)));
                }

                continue;
            }

            JsonValueKind expected = EnvelopeSchema.KindOf(field);
            if (kind != expected)
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.FieldTypeMismatch,
                    context.SourcePath,
                    pointer,
                    null,
                    "'" + field + "' is a JSON " + Describe(expected) + ", not a "
                        + Describe(kind)));
                kindsAreSound = false;
                continue;
            }

            if (expected == JsonValueKind.Array)
            {
                kindsAreSound &= ValidateStringArrayElements(structure, context, bag, field, pointer);
            }
        }

        return kindsAreSound;
    }

    private static bool ValidateStringArrayElements(
        JsonStructure structure,
        EnvelopeReadContext context,
        DiagnosticBag bag,
        string field,
        JsonPointer arrayPointer)
    {
        bool sound = true;
        for (int index = 0; ; index++)
        {
            JsonPointer element = arrayPointer.AppendIndex(index);
            if (!structure.TryGetKind(element, out JsonValueKind kind))
            {
                break;
            }

            if (kind != JsonValueKind.String)
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.FieldTypeMismatch,
                    context.SourcePath,
                    element,
                    null,
                    "every element of '" + field + "' is a JSON string, not a " + Describe(kind)));
                sound = false;
            }
        }

        return sound;
    }

    private static DefinitionEnvelope? ValidateValues(
        EnvelopeDto dto,
        JsonStructure structure,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        string? rawId = dto.Id;

        ContentId? id = ValidateId(rawId, context, bag);
        int schemaVersion = ValidateVersion(
            dto.SchemaVersion, EnvelopeSchema.SchemaVersion, rawId, context, bag);
        int contentVersion = ValidateVersion(
            dto.ContentVersion, EnvelopeSchema.ContentVersion, rawId, context, bag);
        DefinitionStatus status = ValidateStatus(dto.Status, rawId, context, bag);
        LocalizationKey? nameKey = ValidateLocalizationKey(
            dto.NameKey, EnvelopeSchema.NameKey, rawId, context, bag);
        LocalizationKey? summaryKey = ValidateLocalizationKey(
            dto.SummaryKey, EnvelopeSchema.SummaryKey, rawId, context, bag);
        List<string> tags = ValidateTags(dto.Tags, rawId, context, bag);
        List<SourceRef> sourceRefs = ValidateSourceRefs(
            dto.SourceRefs, structure, rawId, context, bag);
        string? presentationId = ValidatePresentationId(dto.PresentationId, rawId, context, bag);

        if (bag.HasErrors || id is null)
        {
            return null;
        }

        return new DefinitionEnvelope(
            id,
            schemaVersion,
            contentVersion,
            status,
            nameKey,
            summaryKey,
            DefinitionEnvelope.Freeze(tags),
            DefinitionEnvelope.Freeze(sourceRefs),
            presentationId);
    }

    private static ContentId? ValidateId(
        string? rawId,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty(EnvelopeSchema.Id);
        if (rawId is null)
        {
            // Already reported as a missing required field.
            return null;
        }

        ContentCategoryDescriptor descriptor = ContentCategories.Describe(context.Category);
        if (!ContentId.TryCreate(rawId, context.Category, out ContentId? id))
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.IdMalformedForCategory,
                context.SourcePath,
                pointer,
                rawId,
                descriptor.DescribeAcceptedGrammar()));
            return null;
        }

        if (context.RetiredIds.TryGetTombstone(id!, out RetiredId? tombstone))
        {
            List<string> related = new();
            if (tombstone!.ReplacedBy is not null)
            {
                related.Add(tombstone.ReplacedBy);
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.RetiredIdReused,
                context.SourcePath,
                pointer,
                rawId,
                "'" + rawId + "' was retired at content_version "
                    + tombstone.RetiredInContentVersion.ToString(CultureInfo.InvariantCulture)
                    + " and is never reassigned (" + tombstone.Rationale + ")",
                related));
            return null;
        }

        return id;
    }

    private static int ValidateVersion(
        double? value,
        string field,
        string? contentId,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty(field);
        if (value is null)
        {
            return 0;
        }

        double raw = value.Value;
        bool integral = Math.Floor(raw) == raw;
        bool inRange = raw is > 0 and <= int.MaxValue;
        if (!integral || !inRange)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.VersionNotPositiveInteger,
                context.SourcePath,
                pointer,
                contentId,
                "'" + field + "' is a positive integer; the initial value of every "
                    + "first-authored definition is 1 (doc 40 § Initial versions)"));
            return 0;
        }

        return (int)raw;
    }

    private static DefinitionStatus ValidateStatus(
        string? token,
        string? contentId,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        if (token is null)
        {
            return DefinitionStatus.Development;
        }

        if (!DefinitionStatuses.TryParse(token, out DefinitionStatus status))
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.UnknownStatus,
                context.SourcePath,
                JsonPointer.Root.AppendProperty(EnvelopeSchema.Status),
                contentId,
                "'status' is one of the exact case-sensitive tokens "
                    + string.Join(", ", DefinitionStatuses.Tokens)));
            return DefinitionStatus.Development;
        }

        return status;
    }

    private static LocalizationKey? ValidateLocalizationKey(
        string? value,
        string field,
        string? contentId,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        if (value is null)
        {
            return null;
        }

        JsonPointer pointer = JsonPointer.Root.AppendProperty(field);
        if (value.Length == 0)
        {
            bag.Add(EmptyOptional(field, pointer, contentId, context));
            return null;
        }

        if (!LocalizationKey.TryParse(value, out LocalizationKey? key))
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.LocalizationKeyMalformed,
                context.SourcePath,
                pointer,
                contentId,
                "'" + field + "' is a localization key of the form "
                    + "<category>.<stable_id>.<role> matching " + LocalizationKey.Pattern
                    + ", never literal player-facing text"));
            return null;
        }

        LocalizationRole expected = LocalizationKey.RoleForField(field);
        if (key!.Role != expected)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.LocalizationKeyRoleMismatch,
                context.SourcePath,
                pointer,
                contentId,
                "'" + field + "' carries a key whose role is '"
                    + LocalizationKey.ToToken(expected) + "'"));
            return null;
        }

        return key;
    }

    private static List<string> ValidateTags(
        List<string>? tags,
        string? contentId,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        List<string> accepted = new();
        if (tags is null)
        {
            return accepted;
        }

        for (int index = 0; index < tags.Count; index++)
        {
            string tag = tags[index];
            if (TagVocabulary.Accepts(tag))
            {
                accepted.Add(tag);
                continue;
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.TagOutsideVocabulary,
                context.SourcePath,
                JsonPointer.Root.AppendProperty(EnvelopeSchema.Tags).AppendIndex(index),
                contentId,
                TagVocabulary.Describe()));
        }

        return accepted;
    }

    private static List<SourceRef> ValidateSourceRefs(
        List<string>? elements,
        JsonStructure structure,
        string? contentId,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        List<SourceRef> parsed = new();
        if (elements is null)
        {
            return parsed;
        }

        for (int index = 0; index < elements.Count; index++)
        {
            string element = elements[index];
            JsonPointer pointer =
                JsonPointer.Root.AppendProperty(EnvelopeSchema.SourceRefs).AppendIndex(index);

            SourceRefParseOutcome outcome = SourceRefGrammar.Parse(element, out SourceRef? reference);
            switch (outcome)
            {
                case SourceRefParseOutcome.PathLine:
                    bag.Add(ContentDiagnostic.CreateError(
                        ContentDiagnosticCodes.SourceRefPathLine,
                        context.SourcePath,
                        pointer,
                        contentId,
                        "a source reference is a stable ID, never a file path or a path:line "
                            + "pair; paths and line numbers move whenever a document is edited, "
                            + "so a reference built from them decays silently. A source with no "
                            + "stable ID gets one before it can be referenced."));
                    continue;

                case SourceRefParseOutcome.Malformed:
                    bag.Add(ContentDiagnostic.CreateError(
                        ContentDiagnosticCodes.SourceRefMalformed,
                        context.SourcePath,
                        pointer,
                        contentId,
                        "a source_refs element matches " + SourceRefGrammar.ElementPattern));
                    continue;

                default:
                    break;
            }

            SourceRefScope? scope = reference!.Scope;
            if (scope is not null && !scope.ResolvesIn(structure))
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.SourceRefScopeUnresolved,
                    context.SourcePath,
                    pointer,
                    contentId,
                    "the scope '" + scope.Text + "' selects " + scope.DescribeSelection()
                        + ", which does not exist in this definition; a scope attributes one "
                        + "part of the definition to a source, so it must name a part that is "
                        + "there",
                    new[] { reference.DocumentId }));
                continue;
            }

            parsed.Add(reference);
        }

        return parsed;
    }

    private static string? ValidatePresentationId(
        string? value,
        string? contentId,
        EnvelopeReadContext context,
        DiagnosticBag bag)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length == 0)
        {
            bag.Add(EmptyOptional(
                EnvelopeSchema.PresentationId,
                JsonPointer.Root.AppendProperty(EnvelopeSchema.PresentationId),
                contentId,
                context));
            return null;
        }

        // A presentation ID grammar is not minted anywhere yet; doc 40 § Presentation
        // and audio describes the definitions but no accepted document gives their IDs a
        // pattern. Until one is granted, the envelope requires a non-empty string and
        // the cross-reference validator owned by DAT-005 checks that the entry exists.
        return value;
    }

    private static ContentDiagnostic EmptyOptional(
        string field,
        JsonPointer pointer,
        string? contentId,
        EnvelopeReadContext context)
    {
        return ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.EmptyOptionalField,
            context.SourcePath,
            pointer,
            contentId,
            "'" + field + "' is declared optional, so absence is expressed by omitting the key; "
                + "the empty string is the value the compiler materializes for an omitted field "
                + "and authoring one would be a second way to say the same thing");
    }

    private static string Describe(JsonValueKind kind)
    {
        return kind switch
        {
            JsonValueKind.Object => "object",
            JsonValueKind.Array => "array",
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            JsonValueKind.Null => "null",
            _ => "value",
        };
    }
}
