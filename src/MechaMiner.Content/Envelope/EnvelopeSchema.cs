using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// <c>SCH-CNT-001</c>: the nine-field common definition envelope, declared once.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Common definition envelope
/// tabulates exactly nine fields, and
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Schema
/// registry registers them as <c>SCH-CNT-001</c> owned by <c>CMP-CNT-001</c>.
/// </para>
/// <para>
/// <b>Declared order is doc 40's table order</b>, not alphabetical and not the order
/// any file happens to use. That is what makes <see cref="Order"/> reviewable: a
/// reader can put the table and this list side by side.
/// </para>
/// <para>
/// <b>Six required, two declared-optional, one required absent.</b> § Declared-optional
/// envelope fields names <c>presentation_id</c> and <c>name_key</c>, and states that
/// <c>summary_key</c> "follows the same rule its row already states". Absence is
/// expressed by omitting the key and never by <c>null</c>, which the codec rejects
/// outright.
/// </para>
/// <para>
/// <b>Why <c>presentation_id</c> is not among the declared-optional two.</b> No accepted
/// document says what a presentation definition contains, so no ID grammar has been
/// minted for one, and minting a shape for a category with zero members would be
/// inventing structure ahead of need. Declared-optional with a non-empty-string
/// constraint is worse than either: it accepts any string an author invents and
/// validates nothing about it. The field is therefore <see cref="RequiredAbsent"/> -
/// still declared, so it is not an unknown field, and rejected on presence with
/// <c>MMC-2010</c>, so the first definition to carry one fails loudly instead of
/// carrying an unauthorized value. The declaration becomes a grammar when a document
/// mints one.
/// </para>
/// </remarks>
public static class EnvelopeSchema
{
    /// <summary>The <c>id</c> field: a stable category-valid ID.</summary>
    public const string Id = "id";

    /// <summary>The <c>schema_version</c> field: the integer version of the definition's schema.</summary>
    public const string SchemaVersion = "schema_version";

    /// <summary>The <c>content_version</c> field: the monotonic revision of the definition.</summary>
    public const string ContentVersion = "content_version";

    /// <summary>The <c>status</c> field: the lifecycle state.</summary>
    public const string Status = "status";

    /// <summary>The <c>name_key</c> field: a localization key, never literal text.</summary>
    public const string NameKey = "name_key";

    /// <summary>The <c>summary_key</c> field: a localization key for a concise summary.</summary>
    public const string SummaryKey = "summary_key";

    /// <summary>The <c>tags</c> field: terms from the closed vocabulary.</summary>
    public const string Tags = "tags";

    /// <summary>The <c>source_refs</c> field: the sources this definition implements.</summary>
    public const string SourceRefs = "source_refs";

    /// <summary>
    /// The <c>presentation_id</c> field: where the content appears in-world. Declared by
    /// doc 40's table and required absent until a document mints its grammar.
    /// </summary>
    public const string PresentationId = "presentation_id";

    /// <summary>
    /// The value a declared-optional field materializes to in the canonical bundle when
    /// the author omitted it.
    /// </summary>
    /// <remarks>
    /// Doc 40 § Common definition envelope: "Optional fields have explicit defaults
    /// materialized into the canonical bundle so runtime never guesses." The empty
    /// string is the materialized default for the two declared-optional keys, and it is
    /// unambiguous rather than merely convenient: a localization key must have three
    /// dot-separated parts, so no present value can ever be the empty string. Runtime
    /// therefore reads a value, and the value says "there is none". It is also the only
    /// value <c>presentation_id</c> ever carries in the canonical bundle, because that
    /// field is required absent in source and so is never anything else.
    /// </remarks>
    public const string AbsentOptionalDefault = "";

    /// <summary>The initial <c>schema_version</c> of a first-authored definition.</summary>
    /// <remarks>Doc 40 § Initial versions.</remarks>
    public const int InitialSchemaVersion = 1;

    /// <summary>The initial <c>content_version</c> of a first-authored definition.</summary>
    /// <remarks>Doc 40 § Initial versions.</remarks>
    public const int InitialContentVersion = 1;

    private static readonly string[] DeclaredOrder =
    {
        // Exactly doc 40 § Common definition envelope's table order.
        Id,
        SchemaVersion,
        ContentVersion,
        Status,
        NameKey,
        SummaryKey,
        Tags,
        SourceRefs,
        PresentationId,
    };

    private static readonly string[] RequiredFields =
    {
        Id,
        SchemaVersion,
        ContentVersion,
        Status,
        Tags,
        SourceRefs,
    };

    private static readonly string[] DeclaredOptionalFields =
    {
        NameKey,
        SummaryKey,
    };

    private static readonly string[] RequiredAbsentFields =
    {
        PresentationId,
    };

    private static readonly Dictionary<string, JsonValueKind> Kinds = new(StringComparer.Ordinal)
    {
        [Id] = JsonValueKind.String,
        [SchemaVersion] = JsonValueKind.Number,
        [ContentVersion] = JsonValueKind.Number,
        [Status] = JsonValueKind.String,
        [NameKey] = JsonValueKind.String,
        [SummaryKey] = JsonValueKind.String,
        [Tags] = JsonValueKind.Array,
        [SourceRefs] = JsonValueKind.Array,

        // Declared so that presentation_id is a known field rather than an unknown one,
        // and so that a reader of this map sees all nine of doc 40's rows. The kind is
        // never checked: a required-absent field is rejected on presence before anything
        // asks what shape the value has.
        [PresentationId] = JsonValueKind.String,
    };

    /// <summary>The canonical emission order of the envelope's fields.</summary>
    public static SchemaFieldOrder Order { get; } = new("SCH-CNT-001 envelope", DeclaredOrder);

    /// <summary>The nine field names, in declared order.</summary>
    public static IReadOnlyList<string> Fields { get; } =
        new ReadOnlyCollection<string>(new List<string>(DeclaredOrder));

    /// <summary>The six fields that must be present.</summary>
    public static IReadOnlyList<string> Required { get; } =
        new ReadOnlyCollection<string>(new List<string>(RequiredFields));

    /// <summary>The two fields whose absence is expressed by omitting the key.</summary>
    public static IReadOnlyList<string> DeclaredOptional { get; } =
        new ReadOnlyCollection<string>(new List<string>(DeclaredOptionalFields));

    /// <summary>
    /// The fields that are declared and must not be present, because no accepted document
    /// grants them a value yet.
    /// </summary>
    public static IReadOnlyList<string> RequiredAbsent { get; } =
        new ReadOnlyCollection<string>(new List<string>(RequiredAbsentFields));

    /// <summary>True when <paramref name="field"/> is one of the nine.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is null.</exception>
    public static bool Declares(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return Kinds.ContainsKey(field);
    }

    /// <summary>True when <paramref name="field"/> must be present.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is null.</exception>
    public static bool IsRequired(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return Array.IndexOf(RequiredFields, field) >= 0;
    }

    /// <summary>True when <paramref name="field"/> is declared and must not be present.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is null.</exception>
    public static bool IsRequiredAbsent(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return Array.IndexOf(RequiredAbsentFields, field) >= 0;
    }

    /// <summary>The JSON value kind <paramref name="field"/> must have.</summary>
    /// <exception cref="ArgumentException"><paramref name="field"/> is not declared.</exception>
    public static JsonValueKind KindOf(string field)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (!Kinds.TryGetValue(field, out JsonValueKind kind))
        {
            throw new ArgumentException(
                "'" + field + "' is not a declared envelope field",
                nameof(field));
        }

        return kind;
    }
}
