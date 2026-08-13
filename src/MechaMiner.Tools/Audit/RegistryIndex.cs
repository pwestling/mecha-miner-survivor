using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MechaMiner.Tools.Audit;

/// <summary>Where one identifier or link occurrence was found.</summary>
internal sealed class SourceLocation
{
    internal SourceLocation(string path, int line)
    {
        Path = path;
        Line = line;
    }

    /// <summary>Repository-relative path with forward slashes.</summary>
    internal string Path { get; }

    /// <summary>One-based line number.</summary>
    internal int Line { get; }

    /// <summary>The <c>file:line</c> form a reviewer can paste into an editor.</summary>
    internal string ToFileLine()
    {
        return Path + ":" + Line.ToString(CultureInfo.InvariantCulture);
    }
}

/// <summary>One identifier occurrence.</summary>
internal sealed class IdentifierOccurrence
{
    internal IdentifierOccurrence(string identifier, IdentifierFamily family, SourceLocation location)
    {
        Identifier = identifier;
        Family = family;
        Location = location;
    }

    /// <summary>The identifier text.</summary>
    internal string Identifier { get; }

    /// <summary>The family it belongs to.</summary>
    internal IdentifierFamily Family { get; }

    /// <summary>Where it was found.</summary>
    internal SourceLocation Location { get; }
}

/// <summary>One markdown link occurrence.</summary>
internal sealed class LinkOccurrence
{
    internal LinkOccurrence(string target, SourceLocation location)
    {
        Target = target;
        Location = location;
    }

    /// <summary>The raw link target.</summary>
    internal string Target { get; }

    /// <summary>Where it was found.</summary>
    internal SourceLocation Location { get; }
}

/// <summary>
/// The identifier, heading, and link index built from a <see cref="RegistrySources"/>.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-002</c>).
/// </para>
/// <para>
/// A definition is an identifier in the <b>first cell of a Markdown table row</b> inside
/// the document that owns its family, because that is how every registry in this
/// specification is actually written. Backticks are stripped first: doc 115 and the
/// document 110 task queue wrap the identifier in code spans while doc 112 does not, and
/// both are definitions.
/// </para>
/// <para>
/// <c>TDR-###</c> is defined by the existence of
/// <c>docs/technical/decisions/TDR-###-*.md</c>, and <c>VER-*</c> by an entry in a
/// <c>tests/verification/*.json</c> registry, so neither comes from a table row.
/// </para>
/// </remarks>
internal sealed class RegistryIndex
{
    private static readonly Regex TableRowFirstCell = new(
        @"^\|\s*(?<cell>[^|]*?)\s*\|",
        RegexOptions.Compiled,
        IdentifierFamilies.MatchTimeout);

    private static readonly Regex MarkdownLink = new(
        @"\]\(\s*(?<target>[^)\s]+)",
        RegexOptions.Compiled,
        IdentifierFamilies.MatchTimeout);

    private static readonly Regex Heading = new(
        @"^(?<hashes>#{1,6})\s+(?<text>.+?)\s*$",
        RegexOptions.Compiled,
        IdentifierFamilies.MatchTimeout);

    private static readonly Regex DecisionFileName = new(
        @"^TDR-(?<number>[0-9]{3})-[a-z0-9-]+\.md$",
        RegexOptions.Compiled,
        IdentifierFamilies.MatchTimeout);

    private readonly Dictionary<string, List<IdentifierOccurrence>> _definitions = new(StringComparer.Ordinal);
    private readonly List<IdentifierOccurrence> _references = new();
    private readonly List<IdentifierOccurrence> _malformed = new();
    private readonly Dictionary<string, HashSet<string>> _anchors = new(StringComparer.Ordinal);
    private readonly List<LinkOccurrence> _links = new();

    /// <summary>Every identifier definition, keyed by identifier. A list, so duplicates are visible.</summary>
    internal IReadOnlyDictionary<string, List<IdentifierOccurrence>> Definitions => _definitions;

    /// <summary>Every identifier reference, including the ones that are also definitions.</summary>
    internal IReadOnlyList<IdentifierOccurrence> References => _references;

    /// <summary>Identifier-shaped tokens that violate the grammar.</summary>
    internal IReadOnlyList<IdentifierOccurrence> Malformed => _malformed;

    /// <summary>Heading anchors per document path.</summary>
    internal IReadOnlyDictionary<string, HashSet<string>> Anchors => _anchors;

    /// <summary>Every markdown link found in the indexed documents.</summary>
    internal IReadOnlyList<LinkOccurrence> Links => _links;

    /// <summary>Builds the index.</summary>
    internal static RegistryIndex Build(RegistrySources sources, IEnumerable<VerificationRegistryDocument> registries)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(registries);

        RegistryIndex index = new();

        foreach (RegistryDocument document in sources.Documents)
        {
            index.IndexDocument(document);
        }

        foreach (RegistryDocument document in sources.VerificationRegistries)
        {
            index.IndexPlainText(document);
        }

        index.IndexDecisionRecords(sources);
        index.IndexVerificationEntries(sources, registries);
        return index;
    }

    /// <summary>Whether <paramref name="identifier"/> has at least one definition.</summary>
    internal bool IsDefined(string identifier)
    {
        return _definitions.ContainsKey(identifier);
    }

    /// <summary>The GitHub-style anchor slug of a heading.</summary>
    /// <remarks>
    /// <para>
    /// Lowercase, drop everything that is not a letter, digit, hyphen, or underscore, and
    /// turn each whitespace character into one hyphen. That is the rule the existing
    /// cross-links in this specification already assume — <c>### C# and domain defaults</c>
    /// is linked as <c>#c-and-domain-defaults</c>, and
    /// <c>## OQ-010 — What are the progression layers?</c> as
    /// <c>#oq-010--what-are-the-progression-layers</c>.
    /// </para>
    /// <para>
    /// Runs of hyphens are not collapsed and the result is not trimmed. Both would be
    /// wrong: a dropped dash between two spaces legitimately produces a double hyphen, and
    /// collapsing it made this validator report hundreds of correct cross-links as
    /// dangling on its first run against the real tree.
    /// </para>
    /// </remarks>
    internal static string Slug(string headingText)
    {
        ArgumentNullException.ThrowIfNull(headingText);

        // Code spans, emphasis, and links contribute their text, not their punctuation.
        string text = headingText
            .Replace("`", string.Empty, StringComparison.Ordinal)
            .Replace("**", string.Empty, StringComparison.Ordinal);
        text = Regex.Replace(
            text,
            @"\[(?<label>[^\]]*)\]\([^)]*\)",
            "${label}",
            RegexOptions.None,
            IdentifierFamilies.MatchTimeout);

        StringBuilder builder = new(text.Length);
        foreach (char character in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character))
            {
                builder.Append('-');
            }
        }

        // Deliberately no collapsing of hyphen runs and no trimming. A heading such as
        // "OQ-010 - What are the progression layers?" written with an em dash slugs to
        // "oq-010--what-are-the-progression-layers", because the dash is dropped and both
        // surrounding spaces still become hyphens. Collapsing the pair would make this
        // validator report every such cross-link in the specification as dangling when the
        // links are correct and the algorithm was wrong.
        return builder.ToString();
    }

    private void IndexDocument(RegistryDocument document)
    {
        HashSet<string> anchors = new(StringComparer.Ordinal);
        string[] lines = document.Text.Split('\n');
        bool inFencedBlock = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].TrimEnd('\r');
            SourceLocation location = new(document.Path, index + 1);

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inFencedBlock = !inFencedBlock;
                continue;
            }

            if (!inFencedBlock)
            {
                Match heading = Heading.Match(line);
                if (heading.Success)
                {
                    anchors.Add(Slug(heading.Groups["text"].Value));
                }

                foreach (Match link in MarkdownLink.Matches(line))
                {
                    _links.Add(new LinkOccurrence(link.Groups["target"].Value, location));
                }

                IndexTableDefinition(document, line, location);
            }

            IndexIdentifiers(line, location);
        }

        _anchors[document.Path] = anchors;
    }

    private void IndexPlainText(RegistryDocument document)
    {
        string[] lines = document.Text.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            IndexIdentifiers(lines[index].TrimEnd('\r'), new SourceLocation(document.Path, index + 1));
        }
    }

    private void IndexTableDefinition(RegistryDocument document, string line, SourceLocation location)
    {
        Match row = TableRowFirstCell.Match(line);
        if (!row.Success)
        {
            return;
        }

        string cell = row.Groups["cell"].Value.Replace("`", string.Empty, StringComparison.Ordinal).Trim();
        if (cell.Length == 0)
        {
            return;
        }

        Match identifier = IdentifierFamilies.Scanner.Match(cell);
        if (!identifier.Success || identifier.Index != 0 || identifier.Length != cell.Length)
        {
            return;
        }

        IdentifierFamily family = IdentifierFamilies.FamilyOf(identifier);
        if (!IdentifierFamilies.DefiningDocumentPrefix.TryGetValue(family, out string? prefix))
        {
            return;
        }

        if (!document.FileName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        if (family == IdentifierFamily.WorkPackage
            && IdentifierFamilies.IsIgnoredPrefix(IdentifierFamilies.PrefixOf(cell)))
        {
            return;
        }

        AddDefinition(new IdentifierOccurrence(cell, family, location));
    }

    private void IndexIdentifiers(string line, SourceLocation location)
    {
        foreach (Match match in IdentifierFamilies.Scanner.Matches(line))
        {
            string identifier = match.Value;
            IdentifierFamily family = IdentifierFamilies.FamilyOf(match);
            if (family == IdentifierFamily.WorkPackage
                && IdentifierFamilies.IsIgnoredPrefix(IdentifierFamilies.PrefixOf(identifier)))
            {
                continue;
            }

            _references.Add(new IdentifierOccurrence(identifier, family, location));
        }

        foreach (Match match in IdentifierFamilies.LooseScanner.Matches(line))
        {
            string token = match.Groups["token"].Value;

            // A token with no digits is a reference to a whole family, not a broken
            // identifier: document 110's authority-routing table legitimately writes
            // TR-FND, CMP-OBS, and SCH-CNT to name the family rather than one member.
            // Only a token that is reaching for a specific member and getting the shape
            // wrong is malformed.
            if (!HasDigit(token))
            {
                continue;
            }

            // A token the document wrote as a WILDCARD - `VER-FND-009-*` - is also a
            // reference to a family rather than to one member, and the digits it carries
            // narrow the family instead of naming a member. The scanner stops at the `-`
            // and hands over `VER-FND-009-`, which has digits, so the no-digit rule above
            // does not catch it and it was reported as malformed. doc 91 line 118 writes
            // exactly that form, correctly: "which rules are live is recorded by the
            // `VER-FND-009-*` entries and their statuses". The test for whether a token is
            // reaching for a specific member is what FOLLOWS it in the source line, so the
            // check is on the line and not on the token.
            if (match.Index + match.Length < line.Length
                && line[match.Index + match.Length] == '*'
                && token.EndsWith('-'))
            {
                continue;
            }

            if (!IdentifierFamilies.IsWellFormed(token))
            {
                _malformed.Add(new IdentifierOccurrence(
                    token,
                    FamilyFromPrefix(IdentifierFamilies.PrefixOf(token)),
                    location));
            }
        }
    }

    private void IndexDecisionRecords(RegistrySources sources)
    {
        foreach (string path in sources.ExistingPaths)
        {
            if (!path.StartsWith("docs/technical/decisions/", StringComparison.Ordinal))
            {
                continue;
            }

            string fileName = path[(path.LastIndexOf('/') + 1)..];
            Match match = DecisionFileName.Match(fileName);
            if (match.Success)
            {
                AddDefinition(new IdentifierOccurrence(
                    "TDR-" + match.Groups["number"].Value,
                    IdentifierFamily.Decision,
                    new SourceLocation(path, 1)));
            }
        }
    }

    private void IndexVerificationEntries(
        RegistrySources sources,
        IEnumerable<VerificationRegistryDocument> registries)
    {
        List<RegistryDocument> files = new(sources.VerificationRegistries);
        int fileIndex = 0;
        foreach (VerificationRegistryDocument registry in registries)
        {
            string path = fileIndex < files.Count ? files[fileIndex].Path : "tests/verification/unknown.json";
            string text = fileIndex < files.Count ? files[fileIndex].Text : string.Empty;
            fileIndex++;

            foreach (VerificationEntry entry in registry.Entries)
            {
                if (string.IsNullOrEmpty(entry.Id))
                {
                    continue;
                }

                AddDefinition(new IdentifierOccurrence(
                    entry.Id,
                    IdentifierFamily.Verification,
                    new SourceLocation(path, LineOf(text, entry.Id))));
            }
        }
    }

    private static int LineOf(string text, string needle)
    {
        string[] lines = text.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            if (lines[index].Contains(needle, StringComparison.Ordinal))
            {
                return index + 1;
            }
        }

        return 1;
    }

    private static bool HasDigit(string token)
    {
        foreach (char character in token)
        {
            if (char.IsAsciiDigit(character))
            {
                return true;
            }
        }

        return false;
    }

    private static IdentifierFamily FamilyFromPrefix(string prefix)
    {
        return prefix switch
        {
            "TASK" => IdentifierFamily.Task,
            "VER" => IdentifierFamily.Verification,
            "TR" => IdentifierFamily.Requirement,
            "CMP" => IdentifierFamily.Component,
            "CTR" => IdentifierFamily.Contract,
            "SCH" => IdentifierFamily.Schema,
            "TDR" => IdentifierFamily.Decision,
            _ => IdentifierFamily.WorkPackage,
        };
    }

    private void AddDefinition(IdentifierOccurrence occurrence)
    {
        if (!_definitions.TryGetValue(occurrence.Identifier, out List<IdentifierOccurrence>? existing))
        {
            existing = new List<IdentifierOccurrence>();
            _definitions[occurrence.Identifier] = existing;
        }

        existing.Add(occurrence);
    }
}
