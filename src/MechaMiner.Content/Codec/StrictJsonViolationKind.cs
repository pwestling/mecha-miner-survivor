namespace MechaMiner.Content.Codec;

/// <summary>
/// The codec-level faults <see cref="StrictJsonReader"/> can report.
/// </summary>
/// <remarks>
/// <para>
/// These are the failures named in
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline, plus the size/count/depth ceilings
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Schema
/// registry requires.
/// </para>
/// <para>
/// A kind is not a diagnostic code. The codec is domain-neutral and must not know
/// the content diagnostic vocabulary; <c>MechaMiner.Content.Diagnostics</c> maps a
/// kind onto a stable code, and another domain reusing this codec maps it onto its
/// own.
/// </para>
/// </remarks>
public enum StrictJsonViolationKind
{
    /// <summary>Reserved so that a default-initialised value is never a real fault.</summary>
    None = 0,

    /// <summary>A <c>//</c> or <c>/* */</c> comment appears in the document.</summary>
    Comment,

    /// <summary>A comma precedes a closing brace or bracket.</summary>
    TrailingComma,

    /// <summary>The same property name appears twice in one object.</summary>
    DuplicateProperty,

    /// <summary>
    /// A number token is <c>NaN</c>, <c>Infinity</c>, <c>-Infinity</c>, or a
    /// syntactically valid literal whose value is not a finite double.
    /// </summary>
    NonfiniteNumber,

    /// <summary>
    /// A JSON <c>null</c> appears. Doc 40 § Declared-optional envelope fields: "A
    /// JSON <c>null</c> is never legal anywhere in a source definition"; absence is
    /// expressed by omitting the key.
    /// </summary>
    NullValue,

    /// <summary>A property name is not <c>snake_case</c>.</summary>
    PropertyNameNotSnakeCase,

    /// <summary>The bytes are not well-formed JSON for a reason with no more specific kind.</summary>
    MalformedJson,

    /// <summary>The bytes are not valid UTF-8.</summary>
    InvalidUtf8,

    /// <summary>The document exceeds <see cref="StrictJsonLimits.MaximumDocumentBytes"/>.</summary>
    DocumentTooLarge,

    /// <summary>Nesting exceeds <see cref="StrictJsonLimits.MaximumDepth"/>.</summary>
    DepthLimitExceeded,

    /// <summary>An object exceeds <see cref="StrictJsonLimits.MaximumObjectProperties"/>.</summary>
    ObjectPropertyLimitExceeded,

    /// <summary>An array exceeds <see cref="StrictJsonLimits.MaximumArrayElements"/>.</summary>
    ArrayElementLimitExceeded,

    /// <summary>The document exceeds <see cref="StrictJsonLimits.MaximumNodeCount"/>.</summary>
    NodeCountLimitExceeded,

    /// <summary>A string exceeds <see cref="StrictJsonLimits.MaximumStringLength"/>.</summary>
    StringTooLong,

    /// <summary>The root value is not a JSON object, which every definition must be.</summary>
    RootNotObject,
}
