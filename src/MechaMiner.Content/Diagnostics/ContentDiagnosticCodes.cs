using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Diagnostics;

/// <summary>
/// Every stable diagnostic code the content pipeline can emit, declared once.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Compilation pipeline:
/// "Every stage emits stable diagnostic codes, exact source path/field, content ID,
/// expected constraint, and relevant related IDs."
/// </para>
/// <para>
/// <b>The convention, and why it is this one.</b>
/// <c>src/MechaMiner.Tools/Cli/DiagnosticCodes.cs</c> already established
/// <c>MMT-####</c> for the verb host - <c>MM</c> for the product, <c>T</c> for the
/// owning domain, four digits whose leading digit classifies the code. Content follows
/// the same grammar with its own domain letter: <b><c>MMC-####</c></b>.
/// </para>
/// <para>
/// The one thing that deliberately does <em>not</em> carry over is what the leading
/// digit means. In <c>MMT</c> it is the process exit class, which is a build-tool
/// contract; <c>MechaMiner.Content</c> is a pure library that must not know about
/// exit classes at all, and a verb maps severities onto them at the boundary. Here the
/// leading digit is the <see cref="ContentValidationStage"/> instead, taken from doc 40
/// § Validation layers. Bands <c>6xxx</c> (semantic), <c>7xxx</c> (relational),
/// <c>8xxx</c> (analytical), and <c>9xxx</c> (internal fault) are reserved for the
/// later DAT packages and are not declared until a validator emits them.
/// </para>
/// <para>
/// Codes are never reused and never renumbered
/// (<c>docs/technical/conventions.md</c> § Stable identifiers). A code that stops being
/// emitted is retired in place, not recycled.
/// </para>
/// </remarks>
public static class ContentDiagnosticCodes
{
    // --- Codec, band 1xxx ---------------------------------------------------

    /// <summary>A comment appears in a source document.</summary>
    public const string Comment = "MMC-1001";

    /// <summary>A comma precedes a closing brace or bracket.</summary>
    public const string TrailingComma = "MMC-1002";

    /// <summary>The same property name appears twice in one object.</summary>
    public const string DuplicateProperty = "MMC-1003";

    /// <summary>A number is NaN, an infinity, or a literal with no finite double value.</summary>
    public const string NonfiniteNumber = "MMC-1004";

    /// <summary>A JSON null appears, where absence must be expressed by omitting the key.</summary>
    public const string NullValue = "MMC-1005";

    /// <summary>A property name is not snake_case.</summary>
    public const string PropertyNameNotSnakeCase = "MMC-1006";

    /// <summary>The bytes are not well-formed JSON.</summary>
    public const string MalformedJson = "MMC-1007";

    /// <summary>The bytes are not valid UTF-8.</summary>
    public const string InvalidUtf8 = "MMC-1008";

    /// <summary>The document exceeds the byte ceiling.</summary>
    public const string DocumentTooLarge = "MMC-1009";

    /// <summary>Nesting exceeds the depth ceiling.</summary>
    public const string DepthLimitExceeded = "MMC-1010";

    /// <summary>An object exceeds the property-count ceiling.</summary>
    public const string ObjectPropertyLimitExceeded = "MMC-1011";

    /// <summary>An array exceeds the element-count ceiling.</summary>
    public const string ArrayElementLimitExceeded = "MMC-1012";

    /// <summary>The document exceeds the total node-count ceiling.</summary>
    public const string NodeCountLimitExceeded = "MMC-1013";

    /// <summary>A string exceeds the length ceiling.</summary>
    public const string StringTooLong = "MMC-1014";

    /// <summary>The root value is not a JSON object.</summary>
    public const string RootNotObject = "MMC-1015";

    // --- Structural, band 2xxx ----------------------------------------------

    /// <summary>A property the schema does not declare appears in the document.</summary>
    public const string UnknownField = "MMC-2001";

    /// <summary>A required envelope field is absent.</summary>
    public const string RequiredFieldMissing = "MMC-2002";

    /// <summary>A field's JSON value kind is not the one its schema declares.</summary>
    public const string FieldTypeMismatch = "MMC-2003";

    /// <summary><c>status</c> is not one of the four accepted lifecycle values.</summary>
    public const string UnknownStatus = "MMC-2004";

    /// <summary><c>schema_version</c> or <c>content_version</c> is not a positive integer.</summary>
    public const string VersionNotPositiveInteger = "MMC-2005";

    /// <summary>A tag is not in the closed vocabulary.</summary>
    public const string TagOutsideVocabulary = "MMC-2006";

    /// <summary>
    /// A localization key is not of the form <c>&lt;category&gt;.&lt;stable_id&gt;.&lt;role&gt;</c>,
    /// which is what literal player-facing text in <c>name_key</c> looks like.
    /// </summary>
    public const string LocalizationKeyMalformed = "MMC-2007";

    /// <summary>A localization key's role does not match the field carrying it.</summary>
    public const string LocalizationKeyRoleMismatch = "MMC-2008";

    /// <summary>
    /// A declared-optional field is present but empty. The empty string is what the
    /// compiler materializes for an omitted field, so an authored empty string would be
    /// a second way to say "absent" - the ambiguity doc 40 removes by banning
    /// <c>null</c>.
    /// </summary>
    public const string EmptyOptionalField = "MMC-2009";

    // --- Identity, band 3xxx ------------------------------------------------

    /// <summary>An ID does not match the pattern its content category declares.</summary>
    public const string IdMalformedForCategory = "MMC-3001";

    /// <summary>An ID that was retired by a tombstone entry has been used again.</summary>
    public const string RetiredIdReused = "MMC-3002";

    // --- Traceability, band 4xxx --------------------------------------------

    /// <summary>A <c>source_refs</c> element does not match the element grammar.</summary>
    public const string SourceRefMalformed = "MMC-4001";

    /// <summary>
    /// A <c>source_refs</c> element is a file path or a <c>path:line</c> pair, which doc
    /// 40 rejects by name because it decays silently whenever the document is edited.
    /// </summary>
    public const string SourceRefPathLine = "MMC-4002";

    /// <summary>
    /// A <c>source_refs</c> scope prefix names a path that does not exist in the
    /// definition it annotates.
    /// </summary>
    public const string SourceRefScopeUnresolved = "MMC-4003";

    // --- Schema infrastructure, band 5xxx -----------------------------------

    /// <summary>A schema uses a keyword the evaluator does not implement.</summary>
    public const string SchemaKeywordUnsupported = "MMC-5001";

    /// <summary>A schema <c>$ref</c> does not resolve.</summary>
    public const string SchemaReferenceUnresolved = "MMC-5002";

    /// <summary>A schema document cannot be read as a draft 2020-12 schema.</summary>
    public const string SchemaMalformed = "MMC-5003";

    private static readonly ContentDiagnosticDescriptor[] Declared =
    {
        Describe(Comment, nameof(Comment), ContentValidationStage.Codec,
            "a comment appears in a source document; rationale belongs in the owning document, not in the data"),
        Describe(TrailingComma, nameof(TrailingComma), ContentValidationStage.Codec,
            "a comma precedes a closing brace or bracket"),
        Describe(DuplicateProperty, nameof(DuplicateProperty), ContentValidationStage.Codec,
            "the same property name appears twice in one object, which System.Text.Json would otherwise accept by keeping the last one"),
        Describe(NonfiniteNumber, nameof(NonfiniteNumber), ContentValidationStage.Codec,
            "a number is NaN, an infinity, or a literal whose value is not a finite double"),
        Describe(NullValue, nameof(NullValue), ContentValidationStage.Codec,
            "a JSON null appears; absence in a source definition is expressed by omitting the key"),
        Describe(PropertyNameNotSnakeCase, nameof(PropertyNameNotSnakeCase), ContentValidationStage.Codec,
            "a property name does not match ^[a-z][a-z0-9_]*$"),
        Describe(MalformedJson, nameof(MalformedJson), ContentValidationStage.Codec,
            "the bytes are not well-formed JSON"),
        Describe(InvalidUtf8, nameof(InvalidUtf8), ContentValidationStage.Codec,
            "the bytes are not valid UTF-8"),
        Describe(DocumentTooLarge, nameof(DocumentTooLarge), ContentValidationStage.Codec,
            "the document exceeds the maximum accepted size in UTF-8 bytes"),
        Describe(DepthLimitExceeded, nameof(DepthLimitExceeded), ContentValidationStage.Codec,
            "nesting exceeds the maximum accepted depth"),
        Describe(ObjectPropertyLimitExceeded, nameof(ObjectPropertyLimitExceeded), ContentValidationStage.Codec,
            "an object has more properties than the maximum accepted count"),
        Describe(ArrayElementLimitExceeded, nameof(ArrayElementLimitExceeded), ContentValidationStage.Codec,
            "an array has more elements than the maximum accepted count"),
        Describe(NodeCountLimitExceeded, nameof(NodeCountLimitExceeded), ContentValidationStage.Codec,
            "the document contains more JSON values than the maximum accepted count"),
        Describe(StringTooLong, nameof(StringTooLong), ContentValidationStage.Codec,
            "a string is longer than the maximum accepted length"),
        Describe(RootNotObject, nameof(RootNotObject), ContentValidationStage.Codec,
            "the root value of a definition is not a JSON object"),

        Describe(UnknownField, nameof(UnknownField), ContentValidationStage.Structural,
            "a property the schema does not declare appears in the document; unknown fields are errors rather than silently ignored"),
        Describe(RequiredFieldMissing, nameof(RequiredFieldMissing), ContentValidationStage.Structural,
            "a required envelope field is absent"),
        Describe(FieldTypeMismatch, nameof(FieldTypeMismatch), ContentValidationStage.Structural,
            "a field's JSON value kind is not the one its schema declares"),
        Describe(UnknownStatus, nameof(UnknownStatus), ContentValidationStage.Structural,
            "status is not development, enabled, disabled, or retired"),
        Describe(VersionNotPositiveInteger, nameof(VersionNotPositiveInteger), ContentValidationStage.Structural,
            "schema_version or content_version is not a positive integer"),
        Describe(TagOutsideVocabulary, nameof(TagOutsideVocabulary), ContentValidationStage.Structural,
            "a tag is not in the closed tags vocabulary; a term is added to the vocabulary in the same change that first uses it"),
        Describe(LocalizationKeyMalformed, nameof(LocalizationKeyMalformed), ContentValidationStage.Structural,
            "a localization key is not <category>.<stable_id>.<role>; name_key and summary_key never carry literal player-facing text"),
        Describe(LocalizationKeyRoleMismatch, nameof(LocalizationKeyRoleMismatch), ContentValidationStage.Structural,
            "a localization key's role does not match the envelope field carrying it"),
        Describe(EmptyOptionalField, nameof(EmptyOptionalField), ContentValidationStage.Structural,
            "a declared-optional field is present but empty; the empty string is the value the compiler materializes for an omitted field, so authoring one is a second way to say absent"),

        Describe(IdMalformedForCategory, nameof(IdMalformedForCategory), ContentValidationStage.Identity,
            "an ID does not match the pattern its content category declares"),
        Describe(RetiredIdReused, nameof(RetiredIdReused), ContentValidationStage.Identity,
            "an ID retired by a tombstone entry has been used again; IDs are never reassigned"),

        Describe(SourceRefMalformed, nameof(SourceRefMalformed), ContentValidationStage.Traceability,
            "a source_refs element does not match the element grammar"),
        Describe(SourceRefPathLine, nameof(SourceRefPathLine), ContentValidationStage.Traceability,
            "a source_refs element is a file path or a path:line pair, which decays silently whenever the document is edited"),
        Describe(SourceRefScopeUnresolved, nameof(SourceRefScopeUnresolved), ContentValidationStage.Traceability,
            "a source_refs scope prefix names a path that does not exist in the definition it annotates"),

        Describe(SchemaKeywordUnsupported, nameof(SchemaKeywordUnsupported), ContentValidationStage.SchemaInfrastructure,
            "a schema uses a keyword the evaluator does not implement; ignoring it would make the schema stop being a gate"),
        Describe(SchemaReferenceUnresolved, nameof(SchemaReferenceUnresolved), ContentValidationStage.SchemaInfrastructure,
            "a schema $ref does not resolve"),
        Describe(SchemaMalformed, nameof(SchemaMalformed), ContentValidationStage.SchemaInfrastructure,
            "a schema document cannot be read as a draft 2020-12 schema"),
    };

    private static readonly Dictionary<string, ContentDiagnosticDescriptor> ByCode = BuildIndex();

    /// <summary>Every declared code, in code order.</summary>
    public static IReadOnlyList<ContentDiagnosticDescriptor> All { get; } =
        new ReadOnlyCollection<ContentDiagnosticDescriptor>(Declared);

    /// <summary>True when <paramref name="code"/> is declared here.</summary>
    public static bool IsDeclared(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return ByCode.ContainsKey(code);
    }

    /// <summary>Returns the descriptor for <paramref name="code"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="code"/> is not declared.</exception>
    public static ContentDiagnosticDescriptor Describe(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (!ByCode.TryGetValue(code, out ContentDiagnosticDescriptor? descriptor))
        {
            throw new ArgumentException(
                "diagnostic code '" + code + "' is not declared in ContentDiagnosticCodes; every "
                    + "code a validator emits is declared in exactly one place",
                nameof(code));
        }

        return descriptor;
    }

    private static ContentDiagnosticDescriptor Describe(
        string code,
        string name,
        ContentValidationStage stage,
        string description)
    {
        return new ContentDiagnosticDescriptor(code, name, stage, description);
    }

    private static Dictionary<string, ContentDiagnosticDescriptor> BuildIndex()
    {
        Dictionary<string, ContentDiagnosticDescriptor> index =
            new(Declared.Length, StringComparer.Ordinal);
        foreach (ContentDiagnosticDescriptor descriptor in Declared)
        {
            if (!index.TryAdd(descriptor.Code, descriptor))
            {
                throw new InvalidOperationException(
                    "diagnostic code '" + descriptor.Code + "' is declared twice");
            }
        }

        return index;
    }
}
