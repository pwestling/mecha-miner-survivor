using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace MechaMiner.Tools.Audit;

/// <summary>The stable identifier families this validator indexes.</summary>
/// <remarks>
/// The families and their shapes come from
/// <c>docs/technical/conventions.md</c> § Stable identifiers. Families that document
/// exists but this validator does not own — <c>TOQ-###</c>, gameplay <c>DEC-###</c>,
/// <c>RSK-###</c>, and the scenario IDs <c>PERF-*</c> and <c>WB-*</c> — are listed in
/// <see cref="IdentifierFamilies.IgnoredPrefixes"/> so a reference to one is skipped
/// deliberately instead of being reported as a dangling work package.
/// </remarks>
internal enum IdentifierFamily
{
    /// <summary><c>TASK-&lt;WORK-PACKAGE&gt;-###</c>.</summary>
    Task,

    /// <summary><c>VER-&lt;WORK-PACKAGE&gt;-###</c>.</summary>
    Verification,

    /// <summary><c>TR-&lt;DOMAIN&gt;-###</c>. The domain is two to four letters: <c>TR-UI-001</c> is real.</summary>
    Requirement,

    /// <summary><c>CMP-&lt;DOMAIN&gt;-###</c>.</summary>
    Component,

    /// <summary><c>CTR-&lt;DOMAIN&gt;-###</c>.</summary>
    Contract,

    /// <summary><c>SCH-&lt;DOMAIN&gt;-###</c>.</summary>
    Schema,

    /// <summary><c>TDR-###</c>.</summary>
    Decision,

    /// <summary><c>&lt;DOMAIN&gt;-###</c> work packages registered in document 110.</summary>
    WorkPackage,
}

/// <summary>
/// The identifier grammar, and where each family is defined.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-002</c>). Authority:
/// <c>docs/technical/conventions.md</c> § Stable identifiers,
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Verification
/// ("Registration tests assert every <c>CMP-*</c>, <c>CTR-*</c>, <c>SCH-*</c>, and
/// <c>VER-*</c> ID is unique, indexed, and resolves its references"),
/// <c>docs/technical/91-verification-strategy.md</c> § Verification registry.
/// Requirements: <c>TR-CTR-006</c>, <c>TR-QUA-004</c>.
/// </para>
/// <para>
/// One combined scanner, with the longest families first, so a position in the text is
/// consumed by the most specific family that matches it. That is what keeps
/// <c>FND-001</c> inside <c>TASK-FND-001-001</c> from being read as a second, separate
/// work-package reference. The lookaround also refuses a match that is glued to
/// surrounding word characters, so <c>MMT-2003</c> is not a work package with a stray
/// digit.
/// </para>
/// </remarks>
internal static class IdentifierFamilies
{
    /// <summary>How long a single regex operation may run before it is abandoned.</summary>
    internal static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Prefixes that look like a work package but belong to a family this validator does
    /// not own. A reference to one is skipped rather than reported.
    /// </summary>
    internal static ImmutableArray<string> IgnoredPrefixes { get; } = ImmutableArray.Create(
        // Gameplay decisions, technical open questions, and the risk register: real
        // registries with their own homes, outside the eight families this task names.
        "DEC",
        "TOQ",
        "RSK",
        // Diagnostic code prefixes of the tool host and the engine runner.
        "MMT",
        "MMG",
        // Accepted scenario IDs. doc 91: "PERF-* and WB-* remain scenario IDs referenced
        // by one or more VER-* entries", so they are not identifiers to resolve here.
        "PERF",
        "WB",
        // Gameplay document identifiers and delivery-wave labels.
        "GDD",
        "TDD",
        // Gameplay open questions (docs/open-questions.md) and research notes
        // (docs/research/). Real registries with their own homes; conventions.md mints
        // TOQ-### for *technical* open questions, and OQ-### is the gameplay register.
        // Neither is in the eight families TASK-FND-009-002 owns.
        "OQ",
        "RES",
        // Not an identifier at all: SHA-256 is the hash algorithm, and the work-package
        // shape would otherwise read it as one.
        "SHA");

    /// <summary>
    /// The combined identifier scanner. Named groups are ordered longest-family-first.
    /// </summary>
    internal static Regex Scanner { get; } = new(
        "(?<![A-Za-z0-9-])(?:"
        + "(?<task>TASK-[A-Z]{2,4}-[0-9]{3}-[0-9]{3})"
        + "|(?<ver>VER-[A-Z]{2,4}-[0-9]{3}-[0-9]{3})"
        + "|(?<tr>TR-[A-Z]{2,4}-[0-9]{3})"
        + "|(?<cmp>CMP-[A-Z]{2,4}-[0-9]{3})"
        + "|(?<ctr>CTR-[A-Z]{2,4}-[0-9]{3})"
        + "|(?<sch>SCH-[A-Z]{2,4}-[0-9]{3})"
        + "|(?<tdr>TDR-[0-9]{3})"
        + "|(?<wp>[A-Z]{2,4}-[0-9]{3})"
        + ")(?![0-9A-Za-z-])",
        RegexOptions.Compiled,
        MatchTimeout);

    /// <summary>
    /// A deliberately loose scanner for identifier-shaped tokens, used to catch a
    /// near-miss such as <c>TR-FND-1</c> that the strict scanner simply would not see.
    /// </summary>
    /// <remarks>
    /// It matches uppercase, digits, and hyphens only. That is what keeps a decision
    /// document's own filename slug — <c>TDR-008-use-steamworks-net-...</c> — from being
    /// reported as a malformed identifier: the slug is lowercase, so the token does not
    /// terminate cleanly and no match is produced. A broken link to that filename is the
    /// link checker's business, not the grammar's.
    /// </remarks>
    internal static Regex LooseScanner { get; } = new(
        "(?<![A-Za-z0-9-])(?<token>(?:TASK|VER|TR|TDR|CMP|CTR|SCH)-[A-Z0-9][A-Z0-9-]{0,24})(?![0-9A-Za-z-])",
        RegexOptions.Compiled,
        MatchTimeout);

    /// <summary>Maps a scanner match to its family.</summary>
    internal static IdentifierFamily FamilyOf(Match match)
    {
        ArgumentNullException.ThrowIfNull(match);

        if (match.Groups["task"].Success)
        {
            return IdentifierFamily.Task;
        }

        if (match.Groups["ver"].Success)
        {
            return IdentifierFamily.Verification;
        }

        if (match.Groups["tr"].Success)
        {
            return IdentifierFamily.Requirement;
        }

        if (match.Groups["cmp"].Success)
        {
            return IdentifierFamily.Component;
        }

        if (match.Groups["ctr"].Success)
        {
            return IdentifierFamily.Contract;
        }

        if (match.Groups["sch"].Success)
        {
            return IdentifierFamily.Schema;
        }

        return match.Groups["tdr"].Success ? IdentifierFamily.Decision : IdentifierFamily.WorkPackage;
    }

    /// <summary>Whether <paramref name="identifier"/> is well formed for its family.</summary>
    internal static bool IsWellFormed(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        Match match = Scanner.Match(identifier);
        return match.Success && match.Length == identifier.Length && match.Index == 0;
    }

    /// <summary>The prefix before the first hyphen, used to skip families this task does not own.</summary>
    internal static string PrefixOf(string identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        int hyphen = identifier.IndexOf('-', StringComparison.Ordinal);
        return hyphen < 0 ? identifier : identifier[..hyphen];
    }

    /// <summary>Whether the prefix belongs to a family this validator deliberately skips.</summary>
    internal static bool IsIgnoredPrefix(string prefix)
    {
        foreach (string ignored in IgnoredPrefixes)
        {
            if (string.Equals(prefix, ignored, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The document filename prefix that owns each family's definitions.
    /// </summary>
    /// <remarks>
    /// Recognition is by filename prefix rather than by full path so the same rules run
    /// against a fixture tree that contains only the defining documents under test.
    /// <c>TDR-###</c> and <c>VER-*</c> are absent because they are defined by a file name
    /// and by a registry document respectively, not by a table row.
    /// </remarks>
    internal static IReadOnlyDictionary<IdentifierFamily, string> DefiningDocumentPrefix { get; } =
        new Dictionary<IdentifierFamily, string>
        {
            [IdentifierFamily.Component] = "115-",
            [IdentifierFamily.Contract] = "115-",
            [IdentifierFamily.Schema] = "115-",
            [IdentifierFamily.Requirement] = "112-",
            [IdentifierFamily.WorkPackage] = "110-",
            [IdentifierFamily.Task] = "110-",
        };
}
