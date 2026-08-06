using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;

namespace MechaMiner.Tools.Audit;

/// <summary>The rule a registry finding violates.</summary>
/// <remarks>
/// A closed enumeration, so a negative control can require the exact rule it injected.
/// The four rules <c>TASK-FND-009-002</c>'s gate names are
/// <see cref="UndefinedIdentifier"/> (missing), <see cref="DuplicateIdentifier"/>
/// (duplicate), <see cref="BrokenLink"/> plus <see cref="BrokenAnchor"/> (dangling), and
/// <see cref="MalformedIdentifier"/> (malformed).
/// </remarks>
internal enum RegistryRule
{
    /// <summary>An identifier is referenced but never defined.</summary>
    UndefinedIdentifier,

    /// <summary>An identifier is defined more than once.</summary>
    DuplicateIdentifier,

    /// <summary>An identifier-shaped token violates the conventions grammar.</summary>
    MalformedIdentifier,

    /// <summary>A relative document link points at a path that does not exist.</summary>
    BrokenLink,

    /// <summary>A link's anchor names a heading that does not exist in the target document.</summary>
    BrokenAnchor,

    /// <summary>A verification registry is not readable as <c>SCH-QUA-001</c>.</summary>
    UnreadableRegistry,

    /// <summary>A verification entry is missing a field <c>SCH-QUA-001</c> requires.</summary>
    IncompleteVerificationEntry,

    /// <summary>A verification entry's field carries a value outside its accepted set.</summary>
    InvalidVerificationValue,

    /// <summary>A registry file's identity does not match its own file name or work package.</summary>
    RegistryIdentityMismatch,

    /// <summary>Entry ordinals are not unique, ascending, and gapless.</summary>
    RegistryNumbering,

    /// <summary>An implementation task has no non-compilation verification.</summary>
    TaskWithoutVerification,

    /// <summary>A registry file escapes a character it is required to encode as UTF-8.</summary>
    NonCanonicalEncoding,
}

/// <summary>How much a finding matters.</summary>
internal enum RegistrySeverity
{
    /// <summary>The structure or grammar this validator owns is violated.</summary>
    Error,

    /// <summary>
    /// A specification-content defect that predates this validator: a citation to an
    /// identifier or a document section that does not exist.
    /// </summary>
    /// <remarks>
    /// Separated from <see cref="Error"/> by rule, not by convenience. A pre-existing
    /// documentation defect is a real defect and is reported in full with
    /// <c>file:line</c>, but it is owned by the document it appears in, and silently
    /// mixing it into the structural class would make an unrelated task responsible for
    /// prose it did not write. The verb returns a distinct diagnostic code for this class
    /// so it stays visible instead of being downgraded to a warning nobody reads.
    /// </remarks>
    SpecificationDefect,
}

/// <summary>One registry violation.</summary>
internal sealed class RegistryFinding
{
    internal RegistryFinding(
        RegistryRule rule,
        RegistrySeverity severity,
        string subject,
        string location,
        string detail)
    {
        Rule = rule;
        Severity = severity;
        Subject = subject;
        Location = location;
        Detail = detail;
    }

    /// <summary>The rule violated.</summary>
    internal RegistryRule Rule { get; }

    /// <summary>How much it matters.</summary>
    internal RegistrySeverity Severity { get; }

    /// <summary>The identifier, path, or entry the finding is about.</summary>
    internal string Subject { get; }

    /// <summary>The <c>file:line</c> a reviewer can open.</summary>
    internal string Location { get; }

    /// <summary>What was expected and what was observed.</summary>
    internal string Detail { get; }

    /// <summary>One canonical reviewable tab-separated line.</summary>
    internal string ToLine()
    {
        return Severity.ToString() + "\t" + Rule.ToString() + "\t" + Location + "\t" + Subject + "\t" + Detail;
    }
}

/// <summary>
/// Validates every stable identifier and every document cross-link in the specification.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-002</c>). Authority:
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Verification,
/// <c>docs/technical/91-verification-strategy.md</c> § Verification registry,
/// <c>docs/technical/conventions.md</c> § Stable identifiers. Requirements:
/// <c>TR-CTR-006</c>, <c>TR-QUA-004</c>, <c>TR-AGT-003</c>.
/// </para>
/// <para>
/// <see cref="Validate"/> is a pure function from <see cref="RegistrySources"/> to
/// findings. Nothing is read from the filesystem here, which is what lets a fixture prove
/// each failure class.
/// </para>
/// <para>
/// Findings carry a severity, and the split is by rule rather than by whether the
/// repository currently passes. Structural violations of the shapes this task owns are
/// errors. A citation to an identifier or a document anchor that does not exist is a
/// specification defect: real, reported in full with <c>file:line</c>, and owned by the
/// document that contains it. Nothing is downgraded to a warning to make a build green.
/// </para>
/// </remarks>
internal static class RegistryValidator
{
    private static readonly ImmutableArray<string> SelectorKinds =
        ImmutableArray.Create("command", "script", "nunit", "manual");

    private static readonly ImmutableArray<string> Tiers =
        ImmutableArray.Create("fast", "main", "nightly", "device");

    private static readonly ImmutableArray<string> Statuses =
        ImmutableArray.Create("registered", "implemented", "retired");

    private static readonly ImmutableArray<string> Platforms =
        ImmutableArray.Create("linux-x64", "windows-x64", "osx-arm64");

    private static readonly ImmutableArray<string> TestAssemblies = ImmutableArray.Create(
        "MechaMiner.Simulation.Tests",
        "MechaMiner.Content.Tests",
        "MechaMiner.Diagnostics.Tests",
        "MechaMiner.Persistence.Tests",
        "MechaMiner.Tools.Tests",
        "MechaMiner.Game.Tests");

    /// <summary>The escape a registry file must not use for the section sign.</summary>
    /// <remarks>
    /// The repository fixes <c>charset = utf-8</c> for every file in
    /// <c>.editorconfig</c>, so escaping a character the file is already required to
    /// encode only hides it from textual review and makes sibling registries disagree
    /// about how the same character is spelled. Literal UTF-8 is the normal form
    /// (<c>VER-FND-009-011</c>).
    /// </remarks>
    internal const string ForbiddenSectionSignEscape = "\\u00a7";

    /// <summary>Validates every rule and returns the findings in canonical order.</summary>
    internal static ImmutableArray<RegistryFinding> Validate(RegistrySources sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        List<RegistryFinding> findings = new();
        List<VerificationRegistryDocument> registries = ReadRegistries(sources, findings);
        RegistryIndex index = RegistryIndex.Build(sources, registries);

        ValidateIdentifierUniqueness(index, findings);
        ValidateIdentifierResolution(index, findings);
        ValidateMalformedIdentifiers(index, findings);
        ValidateLinks(sources, index, findings);
        ValidateRegistries(sources, registries, index, findings);

        findings.Sort(static (left, right) =>
        {
            int bySeverity = left.Severity.CompareTo(right.Severity);
            if (bySeverity != 0)
            {
                return bySeverity;
            }

            int byRule = left.Rule.CompareTo(right.Rule);
            if (byRule != 0)
            {
                return byRule;
            }

            int byLocation = string.CompareOrdinal(left.Location, right.Location);
            return byLocation != 0 ? byLocation : string.CompareOrdinal(left.Subject, right.Subject);
        });
        return ImmutableArray.CreateRange(findings);
    }

    /// <summary>Renders findings as canonical ordered reviewable text.</summary>
    internal static string Render(ImmutableArray<RegistryFinding> findings)
    {
        if (findings.IsEmpty)
        {
            return string.Empty;
        }

        List<string> lines = new();
        foreach (RegistryFinding finding in findings)
        {
            lines.Add(finding.ToLine());
        }

        return string.Join("\n", lines);
    }

    /// <summary>Counts findings of one severity.</summary>
    internal static int Count(ImmutableArray<RegistryFinding> findings, RegistrySeverity severity)
    {
        int count = 0;
        foreach (RegistryFinding finding in findings)
        {
            if (finding.Severity == severity)
            {
                count++;
            }
        }

        return count;
    }

    private static List<VerificationRegistryDocument> ReadRegistries(
        RegistrySources sources,
        List<RegistryFinding> findings)
    {
        List<VerificationRegistryDocument> registries = new();
        foreach (RegistryDocument document in sources.VerificationRegistries)
        {
            try
            {
                registries.Add(ToolsJsonContextAccess.DeserializeVerificationRegistry(document.Text));
            }
            catch (JsonException exception)
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.UnreadableRegistry,
                    RegistrySeverity.Error,
                    document.Path,
                    document.Path + ":1",
                    "not a readable SCH-QUA-001 document: " + exception.Message));
                registries.Add(new VerificationRegistryDocument());
            }
        }

        return registries;
    }

    private static void ValidateIdentifierUniqueness(RegistryIndex index, List<RegistryFinding> findings)
    {
        foreach (KeyValuePair<string, List<IdentifierOccurrence>> entry in index.Definitions)
        {
            if (entry.Value.Count <= 1)
            {
                continue;
            }

            List<string> locations = new();
            foreach (IdentifierOccurrence occurrence in entry.Value)
            {
                locations.Add(occurrence.Location.ToFileLine());
            }

            findings.Add(new RegistryFinding(
                RegistryRule.DuplicateIdentifier,
                RegistrySeverity.Error,
                entry.Key,
                locations[0],
                "defined " + entry.Value.Count.ToString(CultureInfo.InvariantCulture)
                + " times, at " + string.Join(", ", locations)
                + ". docs/technical/conventions.md: 'Identifiers are never reused.'"));
        }
    }

    private static void ValidateIdentifierResolution(RegistryIndex index, List<RegistryFinding> findings)
    {
        HashSet<string> reported = new(StringComparer.Ordinal);
        foreach (IdentifierOccurrence reference in index.References)
        {
            if (index.IsDefined(reference.Identifier))
            {
                continue;
            }

            // One finding per identifier per file, so a widely cited missing ID is one
            // reviewable line per document rather than a hundred.
            string key = reference.Identifier + "@" + reference.Location.Path;
            if (!reported.Add(key))
            {
                continue;
            }

            findings.Add(new RegistryFinding(
                RegistryRule.UndefinedIdentifier,
                RegistrySeverity.SpecificationDefect,
                reference.Identifier,
                reference.Location.ToFileLine(),
                reference.Family + " identifier is referenced but never defined in the document that owns "
                + "its family"));
        }
    }

    private static void ValidateMalformedIdentifiers(RegistryIndex index, List<RegistryFinding> findings)
    {
        HashSet<string> reported = new(StringComparer.Ordinal);
        foreach (IdentifierOccurrence occurrence in index.Malformed)
        {
            string key = occurrence.Identifier + "@" + occurrence.Location.ToFileLine();
            if (!reported.Add(key))
            {
                continue;
            }

            findings.Add(new RegistryFinding(
                RegistryRule.MalformedIdentifier,
                RegistrySeverity.Error,
                occurrence.Identifier,
                occurrence.Location.ToFileLine(),
                "does not match the " + occurrence.Family
                + " grammar of docs/technical/conventions.md § Stable identifiers"));
        }
    }

    private static void ValidateLinks(
        RegistrySources sources,
        RegistryIndex index,
        List<RegistryFinding> findings)
    {
        foreach (LinkOccurrence link in index.Links)
        {
            string target = link.Target;
            if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                // External targets are not resolved: doc 114 § External research defaults
                // requires offline-safe behavior, and a gate that needed the network
                // would fail for reasons that are not about the repository.
                continue;
            }

            int hash = target.IndexOf('#', StringComparison.Ordinal);
            string path = hash < 0 ? target : target[..hash];
            string anchor = hash < 0 ? string.Empty : target[(hash + 1)..];

            string resolved = path.Length == 0
                ? link.Location.Path
                : Combine(link.Location.Path, path);

            if (!sources.PathExists(resolved))
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.BrokenLink,
                    RegistrySeverity.SpecificationDefect,
                    target,
                    link.Location.ToFileLine(),
                    "resolves to '" + resolved + "', which does not exist"));
                continue;
            }

            if (anchor.Length == 0)
            {
                continue;
            }

            if (!index.Anchors.TryGetValue(resolved, out HashSet<string>? anchors))
            {
                // The target exists but is not an indexed Markdown document, so its
                // headings are unknown. Reporting a dangling anchor here would be a guess.
                continue;
            }

            if (!anchors.Contains(anchor))
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.BrokenAnchor,
                    RegistrySeverity.SpecificationDefect,
                    target,
                    link.Location.ToFileLine(),
                    "'" + resolved + "' has no heading whose anchor is '" + anchor + "'"));
            }
        }
    }

    private static void ValidateRegistries(
        RegistrySources sources,
        List<VerificationRegistryDocument> registries,
        RegistryIndex index,
        List<RegistryFinding> findings)
    {
        Dictionary<string, List<VerificationEntry>> entriesByTask = new(StringComparer.Ordinal);

        for (int fileIndex = 0; fileIndex < registries.Count; fileIndex++)
        {
            VerificationRegistryDocument registry = registries[fileIndex];
            RegistryDocument document = sources.VerificationRegistries[fileIndex];
            string path = document.Path;

            if (document.Text.Contains(ForbiddenSectionSignEscape, StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.NonCanonicalEncoding,
                    RegistrySeverity.Error,
                    path,
                    path + ":1",
                    "escapes the section sign as " + ForbiddenSectionSignEscape
                    + " instead of writing it as literal UTF-8; .editorconfig already fixes charset = utf-8 "
                    + "for every file, and sibling registries must spell it the same way"));
            }

            string expectedWorkPackage = document.FileName.EndsWith(".json", StringComparison.Ordinal)
                ? document.FileName[..^".json".Length]
                : document.FileName;

            if (!string.Equals(registry.Schema, "SCH-QUA-001", StringComparison.Ordinal))
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.RegistryIdentityMismatch,
                    RegistrySeverity.Error,
                    path,
                    path + ":1",
                    "schema is '" + (registry.Schema ?? "null") + "', expected SCH-QUA-001"));
            }

            if (!string.Equals(registry.WorkPackage, expectedWorkPackage, StringComparison.Ordinal))
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.RegistryIdentityMismatch,
                    RegistrySeverity.Error,
                    path,
                    path + ":1",
                    "workPackage is '" + (registry.WorkPackage ?? "null") + "', but the file name declares '"
                    + expectedWorkPackage + "'; doc 91 gives each work package exactly one registry file"));
            }

            if (registry.SchemaVersion != 1)
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.InvalidVerificationValue,
                    RegistrySeverity.Error,
                    path,
                    path + ":1",
                    "schemaVersion is " + registry.SchemaVersion.ToString(CultureInfo.InvariantCulture)
                    + ", expected 1"));
            }

            ValidateEntries(registry, expectedWorkPackage, path, index, findings, entriesByTask);
        }

        ValidateTaskCoverage(entriesByTask, index, findings);
    }

    private static void ValidateEntries(
        VerificationRegistryDocument registry,
        string expectedWorkPackage,
        string path,
        RegistryIndex index,
        List<RegistryFinding> findings,
        Dictionary<string, List<VerificationEntry>> entriesByTask)
    {
        List<int> ordinals = new();

        foreach (VerificationEntry entry in registry.Entries)
        {
            string subject = entry.Id ?? "(entry without an id)";
            string location = path + ":1";

            void Missing(string field)
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.IncompleteVerificationEntry,
                    RegistrySeverity.Error,
                    subject,
                    location,
                    "is missing the required field '" + field
                    + "' of doc 91 § Verification registry"));
            }

            void Invalid(string field, string observed, ImmutableArray<string> accepted)
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.InvalidVerificationValue,
                    RegistrySeverity.Error,
                    subject,
                    location,
                    field + " is '" + observed + "', which is not one of [" + string.Join(", ", accepted) + "]"));
            }

            if (string.IsNullOrWhiteSpace(entry.Id))
            {
                Missing("id");
            }
            else
            {
                string expectedPrefix = "VER-" + expectedWorkPackage + "-";
                if (!entry.Id.StartsWith(expectedPrefix, StringComparison.Ordinal)
                    || !IdentifierFamilies.IsWellFormed(entry.Id))
                {
                    findings.Add(new RegistryFinding(
                        RegistryRule.RegistryIdentityMismatch,
                        RegistrySeverity.Error,
                        entry.Id,
                        location,
                        "must be '" + expectedPrefix + "###' to belong to this registry file"));
                }
                else if (int.TryParse(
                    entry.Id[expectedPrefix.Length..],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int ordinal))
                {
                    ordinals.Add(ordinal);
                }
            }

            if (string.IsNullOrWhiteSpace(entry.Summary))
            {
                Missing("summary");
            }

            if (string.IsNullOrWhiteSpace(entry.Task))
            {
                Missing("task");
            }
            else
            {
                if (!entriesByTask.TryGetValue(entry.Task, out List<VerificationEntry>? forTask))
                {
                    forTask = new List<VerificationEntry>();
                    entriesByTask[entry.Task] = forTask;
                }

                forTask.Add(entry);
            }

            if (entry.Requirements.Count == 0)
            {
                Missing("requirements");
            }

            foreach (string requirement in entry.Requirements)
            {
                if (!index.IsDefined(requirement))
                {
                    findings.Add(new RegistryFinding(
                        RegistryRule.UndefinedIdentifier,
                        RegistrySeverity.SpecificationDefect,
                        requirement,
                        location,
                        "cited by " + subject + " but not defined in the normative requirement index"));
                }
            }

            if (entry.Selector is null
                || string.IsNullOrWhiteSpace(entry.Selector.Kind)
                || string.IsNullOrWhiteSpace(entry.Selector.Value))
            {
                Missing("selector");
            }
            else
            {
                if (!SelectorKinds.Contains(entry.Selector.Kind))
                {
                    Invalid("selector.kind", entry.Selector.Kind, SelectorKinds);
                }
                else if (string.Equals(entry.Selector.Kind, "nunit", StringComparison.Ordinal)
                    && !NamesAKnownTestAssembly(entry.Selector.Value))
                {
                    findings.Add(new RegistryFinding(
                        RegistryRule.InvalidVerificationValue,
                        RegistrySeverity.Error,
                        subject,
                        location,
                        "selector.value '" + entry.Selector.Value
                        + "' does not begin with the namespace of a test project in the accepted decomposition"));
                }
            }

            if (entry.EvidenceKinds.Count == 0)
            {
                Missing("evidenceKinds");
            }

            if (entry.Platforms.Count == 0)
            {
                Missing("platforms");
            }

            foreach (string platform in entry.Platforms)
            {
                if (!Platforms.Contains(platform))
                {
                    Invalid("platforms", platform, Platforms);
                }
            }

            if (string.IsNullOrWhiteSpace(entry.Tier))
            {
                Missing("tier");
            }
            else if (!Tiers.Contains(entry.Tier))
            {
                Invalid("tier", entry.Tier, Tiers);
            }

            if (string.IsNullOrWhiteSpace(entry.Status))
            {
                Missing("status");
            }
            else if (!Statuses.Contains(entry.Status))
            {
                Invalid("status", entry.Status, Statuses);
            }
            else if (string.Equals(entry.Status, "retired", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(entry.Successor))
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.IncompleteVerificationEntry,
                    RegistrySeverity.Error,
                    subject,
                    location,
                    "is retired without a successor; doc 91: 'retired verification retains a tombstone "
                    + "and successor'"));
            }

            foreach (string source in entry.TechnicalSources)
            {
                ValidateCitedSource(source, subject, location, index, findings);
            }
        }

        ValidateNumbering(ordinals, path, findings);
    }

    private static void ValidateNumbering(List<int> ordinals, string path, List<RegistryFinding> findings)
    {
        for (int index = 1; index < ordinals.Count; index++)
        {
            if (ordinals[index] <= ordinals[index - 1])
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.RegistryNumbering,
                    RegistrySeverity.Error,
                    path,
                    path + ":1",
                    "entry ordinals are not strictly ascending at position "
                    + index.ToString(CultureInfo.InvariantCulture)
                    + " (" + ordinals[index - 1].ToString(CultureInfo.InvariantCulture)
                    + " then " + ordinals[index].ToString(CultureInfo.InvariantCulture)
                    + "); doc 91: entries 'are never renumbered'"));
                return;
            }
        }

        for (int index = 0; index < ordinals.Count; index++)
        {
            if (ordinals[index] != index + 1)
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.RegistryNumbering,
                    RegistrySeverity.Error,
                    path,
                    path + ":1",
                    "entry ordinals have a gap at " + (index + 1).ToString(CultureInfo.InvariantCulture)
                    + "; a removed entry keeps a retired tombstone rather than vanishing"));
                return;
            }
        }
    }

    private static void ValidateTaskCoverage(
        Dictionary<string, List<VerificationEntry>> entriesByTask,
        RegistryIndex index,
        List<RegistryFinding> findings)
    {
        foreach (KeyValuePair<string, List<VerificationEntry>> pair in entriesByTask)
        {
            if (!index.IsDefined(pair.Key))
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.UndefinedIdentifier,
                    RegistrySeverity.SpecificationDefect,
                    pair.Key,
                    "tests/verification:1",
                    "a verification entry names this task but document 110 does not register it"));
            }

            bool hasNonCompilationEvidence = false;
            foreach (VerificationEntry entry in pair.Value)
            {
                foreach (string kind in entry.EvidenceKinds)
                {
                    if (!string.Equals(kind, "compilation", StringComparison.Ordinal))
                    {
                        hasNonCompilationEvidence = true;
                        break;
                    }
                }

                if (hasNonCompilationEvidence)
                {
                    break;
                }
            }

            if (!hasNonCompilationEvidence)
            {
                findings.Add(new RegistryFinding(
                    RegistryRule.TaskWithoutVerification,
                    RegistrySeverity.Error,
                    pair.Key,
                    "tests/verification:1",
                    "has no verification whose evidence is anything but compilation; doc 91: "
                    + "'An agent may not declare completion based solely on compilation'"));
            }
        }
    }

    private static void ValidateCitedSource(
        string source,
        string subject,
        string location,
        RegistryIndex index,
        List<RegistryFinding> findings)
    {
        int hash = source.IndexOf('#', StringComparison.Ordinal);
        string path = hash < 0 ? source : source[..hash];
        string anchor = hash < 0 ? string.Empty : source[(hash + 1)..];

        if (!index.Anchors.TryGetValue(path, out HashSet<string>? anchors))
        {
            findings.Add(new RegistryFinding(
                RegistryRule.BrokenLink,
                RegistrySeverity.SpecificationDefect,
                source,
                location,
                "cited by " + subject + " but '" + path + "' is not an indexed specification document"));
            return;
        }

        if (anchor.Length > 0 && !anchors.Contains(anchor))
        {
            findings.Add(new RegistryFinding(
                RegistryRule.BrokenAnchor,
                RegistrySeverity.SpecificationDefect,
                source,
                location,
                "cited by " + subject + " but '" + path + "' has no heading whose anchor is '" + anchor + "'"));
        }
    }

    private static bool NamesAKnownTestAssembly(string selector)
    {
        foreach (string assembly in TestAssemblies)
        {
            if (selector.StartsWith(assembly + ".", StringComparison.Ordinal)
                || string.Equals(selector, assembly, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Combine(string fromDocumentPath, string relativeTarget)
    {
        int slash = fromDocumentPath.LastIndexOf('/');
        string directory = slash < 0 ? string.Empty : fromDocumentPath[..slash];

        List<string> segments = new();
        if (directory.Length > 0)
        {
            segments.AddRange(directory.Split('/'));
        }

        foreach (string segment in relativeTarget.Split('/'))
        {
            if (segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(segment, "..", StringComparison.Ordinal))
            {
                if (segments.Count > 0)
                {
                    segments.RemoveAt(segments.Count - 1);
                }

                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments);
    }
}
