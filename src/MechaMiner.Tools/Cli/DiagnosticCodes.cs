namespace MechaMiner.Tools.Cli;

/// <summary>
/// Stable diagnostic codes emitted in structured output.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Standard command surface fixes eight exit classes and then says: "More
/// detailed stable diagnostic codes live in structured output". These are those
/// codes. The numeric part of each code is the exit class it reports under, so a
/// reader can map a code to a class without a table.
/// </para>
/// <para>
/// Codes are never reused or renumbered
/// (<c>docs/technical/conventions.md</c> § Stable identifiers).
/// </para>
/// </remarks>
internal static class DiagnosticCodes
{
    /// <summary>The verb completed successfully.</summary>
    internal const string Success = "MMT-0000";

    /// <summary>The requested verb is not a registered verb.</summary>
    internal const string UnknownVerb = "MMT-2001";

    /// <summary>
    /// The verb is registered and its argument contract is stable, but the work
    /// package that owns its behavior has not landed yet.
    /// </summary>
    internal const string VerbOwnerUnavailable = "MMT-2002";

    /// <summary>An argument is missing, unknown, or outside its allowed value set.</summary>
    internal const string InvalidArgument = "MMT-2003";

    /// <summary>A pinned tool or version is missing or mismatched.</summary>
    internal const string EnvironmentMismatch = "MMT-3001";

    /// <summary>A validation or test gate reported a failure.</summary>
    internal const string ValidationFailed = "MMT-4001";

    /// <summary>
    /// A specification-content defect: a citation to an identifier or a document anchor
    /// that does not exist.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="ValidationFailed"/> under the same exit class, because the
    /// document that contains the prose owns the defect and an unrelated task must not
    /// inherit it. Doc 100 § Standard command surface closes the exit-class set at eight
    /// members and assigns finer distinctions to diagnostic codes, so this is a code rather
    /// than a ninth class. Used by <c>build/verify-registry.sh</c> (<c>FND-009</c>).
    /// </remarks>
    internal const string SpecificationDefect = "MMT-4002";

    /// <summary>A build, import, export, or package step reported a failure.</summary>
    internal const string BuildFailed = "MMT-5001";

    /// <summary>The verb host itself failed in a way it could not classify.</summary>
    internal const string UnexpectedFailure = "MMT-8001";
}
