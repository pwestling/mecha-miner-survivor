using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Diagnostics;

/// <summary>
/// One content diagnostic, with every element doc 40 requires modelled as a field.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Compilation pipeline:
/// "Every stage emits stable diagnostic codes, exact source path/field, content ID,
/// expected constraint, and relevant related IDs."
/// </para>
/// <para>
/// That is five required elements and they are five properties, not one formatted
/// sentence. A formatted sentence cannot be grouped by code, filtered by content ID,
/// or joined against a related definition, so a report built from one degrades to
/// text search - and the compiler is required to produce traceability reports
/// (doc 40 § Compilation pipeline, "Bundle --&gt; Reports").
/// </para>
/// <list type="table">
/// <item><term>stable diagnostic code</term><description><see cref="Code"/></description></item>
/// <item><term>exact source path/field</term><description><see cref="SourcePath"/> plus <see cref="Location"/></description></item>
/// <item><term>content ID</term><description><see cref="ContentId"/></description></item>
/// <item><term>expected constraint</term><description><see cref="ExpectedConstraint"/></description></item>
/// <item><term>relevant related IDs</term><description><see cref="RelatedIds"/></description></item>
/// </list>
/// </remarks>
public sealed class ContentDiagnostic
{
    private static readonly IReadOnlyList<string> NoRelatedIds = Array.Empty<string>();

    private ContentDiagnostic(
        string code,
        ContentDiagnosticSeverity severity,
        string sourcePath,
        JsonPointer location,
        string? contentId,
        string expectedConstraint,
        IReadOnlyList<string> relatedIds,
        WarningPolicy? warning)
    {
        Code = code;
        Severity = severity;
        SourcePath = sourcePath;
        Location = location;
        ContentId = contentId;
        ExpectedConstraint = expectedConstraint;
        RelatedIds = relatedIds;
        Warning = warning;
    }

    /// <summary>The stable code, declared in <see cref="ContentDiagnosticCodes"/>.</summary>
    public string Code { get; }

    /// <summary>Whether the build fails on this diagnostic.</summary>
    public ContentDiagnosticSeverity Severity { get; }

    /// <summary>The repository-relative source path, with forward slashes.</summary>
    public string SourcePath { get; }

    /// <summary>The exact field, as an RFC 6901 pointer into the source document.</summary>
    public JsonPointer Location { get; }

    /// <summary>
    /// The stable ID of the definition at fault, or null when the document is too
    /// broken for its ID to be read - which is itself information, and better than
    /// inventing a placeholder.
    /// </summary>
    public string? ContentId { get; }

    /// <summary>The constraint the content was expected to satisfy.</summary>
    public string ExpectedConstraint { get; }

    /// <summary>
    /// Other stable IDs a reader needs in order to act: the referenced definition, the
    /// tombstone's replacement, the sibling that already claims a value.
    /// </summary>
    public IReadOnlyList<string> RelatedIds { get; }

    /// <summary>
    /// The owner and expiration, present exactly when
    /// <see cref="Severity"/> is <see cref="ContentDiagnosticSeverity.Warning"/>.
    /// </summary>
    public WarningPolicy? Warning { get; }

    /// <summary>Creates an error.</summary>
    /// <exception cref="ArgumentException">
    /// The code is not declared, or the source path or expected constraint is blank.
    /// </exception>
    public static ContentDiagnostic CreateError(
        string code,
        string sourcePath,
        JsonPointer location,
        string? contentId,
        string expectedConstraint,
        IReadOnlyList<string>? relatedIds = null)
    {
        return Create(
            code,
            ContentDiagnosticSeverity.Error,
            sourcePath,
            location,
            contentId,
            expectedConstraint,
            relatedIds,
            warning: null);
    }

    /// <summary>Creates a warning, which cannot exist without an owner and an expiry.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="warning"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// The code is not declared, or the source path or expected constraint is blank.
    /// </exception>
    public static ContentDiagnostic CreateWarning(
        string code,
        string sourcePath,
        JsonPointer location,
        string? contentId,
        string expectedConstraint,
        WarningPolicy warning,
        IReadOnlyList<string>? relatedIds = null)
    {
        ArgumentNullException.ThrowIfNull(warning);
        return Create(
            code,
            ContentDiagnosticSeverity.Warning,
            sourcePath,
            location,
            contentId,
            expectedConstraint,
            relatedIds,
            warning);
    }

    /// <summary>Renders every required element in one reviewable line.</summary>
    public override string ToString()
    {
        string severity = Severity == ContentDiagnosticSeverity.Error ? "error" : "warning";
        string related = RelatedIds.Count == 0
            ? string.Empty
            : " related=[" + string.Join(", ", RelatedIds) + "]";
        string owner = Warning is null
            ? string.Empty
            : " owner=" + Warning.Owner + " expires=" + Warning.ExpiresOn.ToString(
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture);

        return Code + " " + severity + " " + SourcePath + "#" + Location.Value
            + " id=" + (ContentId ?? "<unreadable>")
            + " expected: " + ExpectedConstraint
            + related
            + owner;
    }

    private static ContentDiagnostic Create(
        string code,
        ContentDiagnosticSeverity severity,
        string sourcePath,
        JsonPointer location,
        string? contentId,
        string expectedConstraint,
        IReadOnlyList<string>? relatedIds,
        WarningPolicy? warning)
    {
        ArgumentNullException.ThrowIfNull(code);

        // Declaring the code is the whole point of the registry: a diagnostic built
        // from an undeclared code would be unenumerable and untestable.
        if (!ContentDiagnosticCodes.IsDeclared(code))
        {
            throw new ArgumentException(
                "diagnostic code '" + code + "' is not declared in ContentDiagnosticCodes",
                nameof(code));
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException(
                "a diagnostic names its exact source path (doc 40 § Compilation pipeline)",
                nameof(sourcePath));
        }

        if (string.IsNullOrWhiteSpace(expectedConstraint))
        {
            throw new ArgumentException(
                "a diagnostic states the expected constraint (doc 40 § Compilation pipeline)",
                nameof(expectedConstraint));
        }

        IReadOnlyList<string> related = relatedIds is null || relatedIds.Count == 0
            ? NoRelatedIds
            : new ReadOnlyCollection<string>(new List<string>(relatedIds));

        return new ContentDiagnostic(
            code,
            severity,
            sourcePath,
            location,
            contentId,
            expectedConstraint,
            related,
            warning);
    }
}
