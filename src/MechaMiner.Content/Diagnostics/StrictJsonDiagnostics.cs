using System;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Diagnostics;

/// <summary>
/// Maps a domain-neutral codec violation onto a content diagnostic code.
/// </summary>
/// <remarks>
/// This mapping lives in <c>Diagnostics</c> and not in <c>Codec</c> on purpose. Doc 40
/// § JSON codec and schema baseline says the codec policy is "reused by content, saves,
/// recovery, manifests, diagnostics, and task evidence" and that "codec reuse does not
/// merge domain ownership". A save-domain reader will one day map the same
/// <see cref="StrictJsonViolationKind"/> values onto its own codes; if the codec knew
/// the content codes, that reuse would drag content's vocabulary into persistence.
/// </remarks>
public static class StrictJsonDiagnostics
{
    /// <summary>The content diagnostic code for <paramref name="kind"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="kind"/> has no mapping, which can only mean a kind was added
    /// without a code.
    /// </exception>
    public static string CodeFor(StrictJsonViolationKind kind)
    {
        return kind switch
        {
            StrictJsonViolationKind.Comment => ContentDiagnosticCodes.Comment,
            StrictJsonViolationKind.TrailingComma => ContentDiagnosticCodes.TrailingComma,
            StrictJsonViolationKind.DuplicateProperty => ContentDiagnosticCodes.DuplicateProperty,
            StrictJsonViolationKind.NonfiniteNumber => ContentDiagnosticCodes.NonfiniteNumber,
            StrictJsonViolationKind.NullValue => ContentDiagnosticCodes.NullValue,
            StrictJsonViolationKind.PropertyNameNotSnakeCase =>
                ContentDiagnosticCodes.PropertyNameNotSnakeCase,
            StrictJsonViolationKind.MalformedJson => ContentDiagnosticCodes.MalformedJson,
            StrictJsonViolationKind.InvalidUtf8 => ContentDiagnosticCodes.InvalidUtf8,
            StrictJsonViolationKind.DocumentTooLarge => ContentDiagnosticCodes.DocumentTooLarge,
            StrictJsonViolationKind.DepthLimitExceeded => ContentDiagnosticCodes.DepthLimitExceeded,
            StrictJsonViolationKind.ObjectPropertyLimitExceeded =>
                ContentDiagnosticCodes.ObjectPropertyLimitExceeded,
            StrictJsonViolationKind.ArrayElementLimitExceeded =>
                ContentDiagnosticCodes.ArrayElementLimitExceeded,
            StrictJsonViolationKind.NodeCountLimitExceeded =>
                ContentDiagnosticCodes.NodeCountLimitExceeded,
            StrictJsonViolationKind.StringTooLong => ContentDiagnosticCodes.StringTooLong,
            StrictJsonViolationKind.RootNotObject => ContentDiagnosticCodes.RootNotObject,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "no content diagnostic code is declared for this codec violation kind"),
        };
    }

    /// <summary>Converts a codec violation into a content diagnostic.</summary>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    public static ContentDiagnostic ToDiagnostic(
        StrictJsonViolation violation,
        string sourcePath,
        string? contentId)
    {
        ArgumentNullException.ThrowIfNull(violation);

        return ContentDiagnostic.CreateError(
            CodeFor(violation.Kind),
            sourcePath,
            violation.Location,
            contentId,
            violation.ExpectedConstraint);
    }
}
