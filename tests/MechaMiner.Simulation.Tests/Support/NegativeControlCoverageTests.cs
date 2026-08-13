using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Support;

/// <summary>
/// Asserts that the negative-control record and the seven SIM verification registries partition
/// the registry between controlled entries and a committed uncovered list.
/// </summary>
/// <remarks>
/// <para>
/// This fixture has no <c>VER-*</c> entry and must not be given one. It verifies no
/// <c>TR-*</c> requirement: it verifies <c>tests/verification/SIM-negative-controls.md</c>
/// against <c>tests/verification/SIM-00*.json</c>, which is the structural-validator work
/// <c>TASK-FND-009-002</c> owns. Registering it under a <c>SIM-*</c> package would put a claim
/// about the verification record inside the record it is checking, which is the same category
/// error <c>docs/technical/91-verification-strategy.md</c> § What a kind may name rules out for a
/// technique in <c>evidenceKinds</c>. § The uncovered list, and what makes the two lists a
/// partition, in the document itself, says the same thing and names this class.
/// </para>
/// <para>
/// <b>The problem it exists for.</b> The document recorded its own coverage in prose: so many
/// entries controlled, so many with no control. Both numbers were accurate when written and both
/// rotted the moment anyone added a registry entry, because nothing forced a new entry onto
/// either list. A document that enumerates its own gaps is a curated list, and a curated list of
/// gaps goes stale silently, since the omission is invisible in a green run. Asserting the
/// partition is what makes adding an entry without a control fail closed, and it makes the
/// uncovered list a ratchet: leaving it needs a control, joining it needs someone to write an ID
/// into a committed file.
/// </para>
/// <para>
/// <b>Why coverage is read from <c>fixtures</c> and not from the document's index table.</b> The
/// table's third column is prose, and one row proves prose cannot be read mechanically:
/// <c>VER-SIM-003-003</c>'s cell names a section it is explicitly <em>not</em> credited with, so
/// any rule keyed on "this cell mentions a section" credits it wrongly. The registry's own
/// pointers are the structured statement of coverage and this document's headings are the
/// structured statement of what exists, so the covered set is the intersection of two things that
/// are already machine-readable. The table keeps its job, the human-readable reason, and
/// <see cref="EveryUncoveredEntryHasItsReasonInTheIndexTable"/> requires it to still hold one.
/// </para>
/// <para>
/// <b>Three failure classes, deliberately distinguishable.</b> A coverage failure is an
/// <c>Assert</c> failure naming the entries in neither set or in both. A malformed document is an
/// <see cref="InvalidDataException"/> naming what could not be parsed, and it aborts before any
/// partition arithmetic, so an unparseable block is never silently read as an empty one. Failing
/// to read a file at all is a third class, an <see cref="IOException"/>-caused
/// <see cref="InvalidOperationException"/> carrying the operating system's or the parser's own
/// error as its inner exception. Three gates in this repository have collapsed the second and
/// third into one verdict, and a gate that fails identically for a missing file and an unreadable
/// one reads as flaky, which teaches a reader to re-run it until it passes. See
/// <see cref="ReadOrThrow"/>.
/// </para>
/// <para>
/// <b>A renamed heading fails loudly.</b> Coverage requires every section an entry names to
/// resolve to a real heading in the document. Without that, renaming a heading would drop its
/// entries from the covered set, they would then look uncovered, and the partition would fail for
/// a reason that points at the entries rather than at the rename, or, worse, would still pass if
/// they happened to be on the uncovered list too. <see cref="EverySectionNamedByAnEntryExists"/>
/// is checked before the partition and names the entry, the section, and the closest headings the
/// document does have.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class NegativeControlCoverageTests
{
    private const string DocumentRelativePath = "tests/verification/SIM-negative-controls.md";
    private const string RegistryDirectory = "tests/verification";
    private const string RegistryGlob = "SIM-0*.json";

    private const string PointerPrefix = DocumentRelativePath + " § ";
    private const string BeginMarker = "<!-- SIM-UNCOVERED-BEGIN -->";
    private const string EndMarker = "<!-- SIM-UNCOVERED-END -->";

    /// <summary>Matches an entry ID of the form <c>VER-SIM-000-000</c> and nothing else.</summary>
    private static readonly Regex EntryIdShape = new(
        @"^VER-SIM-[0-9]{3}-[0-9]{3}$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Matches a Markdown ATX heading of level 2 to 6 and captures its text. Level 1 is the
    /// document title and is deliberately not a section a <c>fixtures</c> pointer may name.
    /// </summary>
    private static readonly Regex HeadingShape = new(
        @"^\#{2,6}[ \t]+(?<title>\S.*?)[ \t]*$",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    /// <summary>
    /// Matches the paragraph that states the document's own totals. The wording is load-bearing:
    /// a reword is reported as a malformed document rather than ignored, because a totals sentence
    /// nothing checks is the rot this fixture exists to stop.
    /// </summary>
    private static readonly Regex TotalsShape = new(
        @"(?<covered>[0-9]+) entries name this file\. "
            + @"(?<uncovered>[0-9]+) are recorded above as having no control transcript anywhere "
            + @"and name it nowhere\. (?<retired>One|[0-9]+) (?:is|are) retired and points? at "
            + @"nothing\. That accounts for all (?<total>[0-9]+) entries across the seven files\.",
        RegexOptions.CultureInvariant);

    [Test]
    public void EverySectionNamedByAnEntryExists()
    {
        Document document = Document.Load();
        RegistryCorpus corpus = RegistryCorpus.Load();

        List<string> unresolved = new();
        foreach (RegistryEntry entry in corpus.Entries)
        {
            foreach (string section in entry.NamedSections)
            {
                if (document.Headings.Contains(section))
                {
                    continue;
                }

                unresolved.Add(
                    entry.Id
                        + " names § "
                        + section
                        + ", and "
                        + DocumentRelativePath
                        + " has no such heading. Closest headings it does have: "
                        + string.Join(
                            " | ",
                            document.Headings
                                .OrderByDescending(heading => SharedPrefixLength(heading, section))
                                .Take(3)));
            }
        }

        Assert.That(
            unresolved,
            Is.Empty,
            "every section a fixtures pointer names must resolve to a heading in "
                + DocumentRelativePath
                + ". A pointer that does not resolve is not a missing control, it is a renamed or "
                + "deleted heading, and it is reported here by name rather than by quietly "
                + "dropping the entry from the covered set:\n  "
                + string.Join("\n  ", unresolved));
    }

    [Test]
    public void TheUncoveredBlockIsWellFormed()
    {
        Document document = Document.Load();

        Expect.Multiple(() =>
        {
            Assert.That(
                document.UncoveredIds,
                Is.Ordered.Using<string>(StringComparer.Ordinal),
                "the uncovered block must be in ascending order, so that a diff of it reads as an "
                    + "addition or a removal rather than as a reshuffle");
            Assert.That(
                document.UncoveredIds,
                Is.Unique,
                "a duplicate in the uncovered block would make its declared count disagree with "
                    + "the set it describes");
            Assert.That(
                document.UncoveredIds,
                Is.Not.Empty,
                "an empty uncovered block is legal in principle and would mean every entry has a "
                    + "control; if that is ever true, delete this assertion deliberately rather "
                    + "than discovering it as a pass");
        });
    }

    [Test]
    public void TheStatedTotalsAgreeWithTheRegistry()
    {
        Document document = Document.Load();
        RegistryCorpus corpus = RegistryCorpus.Load();

        int covered = corpus.Entries.Count(entry => !entry.IsRetired && entry.NamedSections.Count > 0);
        int retired = corpus.Entries.Count(entry => entry.IsRetired);

        Expect.Multiple(() =>
        {
            Assert.That(
                document.StatedCovered,
                Is.EqualTo(covered),
                "the document states how many entries name it; that number is now checked against "
                    + "the registries rather than maintained by hand");
            Assert.That(
                document.StatedUncovered,
                Is.EqualTo(document.UncoveredIds.Count),
                "the totals paragraph and the uncovered block are two statements of the same "
                    + "number and must agree");
            Assert.That(
                document.StatedRetired,
                Is.EqualTo(retired),
                "the totals paragraph accounts for retired entries separately, because a retired "
                    + "entry owes a successor rather than a control");
            Assert.That(
                document.StatedTotal,
                Is.EqualTo(corpus.Entries.Count),
                "the three stated groups must account for every entry in the seven files");
            Assert.That(
                covered + document.UncoveredIds.Count + retired,
                Is.EqualTo(corpus.Entries.Count),
                "controlled, uncovered and retired must add up, which is the arithmetic half of "
                    + "the partition");
        });
    }

    [Test]
    public void EveryNonRetiredEntryIsControlledOrUncoveredAndNeverBoth()
    {
        Document document = Document.Load();
        RegistryCorpus corpus = RegistryCorpus.Load();

        HashSet<string> uncovered = new(document.UncoveredIds, StringComparer.Ordinal);
        HashSet<string> knownIds = new(corpus.Entries.Select(entry => entry.Id), StringComparer.Ordinal);

        List<string> inNeither = new();
        List<string> inBoth = new();
        List<string> retiredButListed = new();

        foreach (RegistryEntry entry in corpus.Entries)
        {
            bool controlled = entry.NamedSections.Count > 0;
            bool listed = uncovered.Contains(entry.Id);

            if (entry.IsRetired)
            {
                if (listed)
                {
                    retiredButListed.Add(entry.Id);
                }

                continue;
            }

            if (controlled && listed)
            {
                inBoth.Add(entry.Id + " (names " + entry.NamedSections.Count + " section(s))");
            }
            else if (!controlled && !listed)
            {
                inNeither.Add(entry.Id + " in " + entry.SourceFile);
            }
        }

        List<string> unknownInList = uncovered
            .Where(id => !knownIds.Contains(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        Expect.Multiple(() =>
        {
            Assert.That(
                inNeither,
                Is.Empty,
                "every non-retired VER-SIM-* entry must either name a section of "
                    + DocumentRelativePath
                    + " in its fixtures or appear in that document's uncovered block. An entry in "
                    + "neither is a gap nothing records, which is what this gate exists to make "
                    + "impossible to add:\n  "
                    + string.Join("\n  ", inNeither));
            Assert.That(
                inBoth,
                Is.Empty,
                "an entry that both names a control section and appears on the uncovered list "
                    + "makes the document contradict itself, and makes the two counts above "
                    + "double-count it:\n  "
                    + string.Join("\n  ", inBoth));
            Assert.That(
                retiredButListed,
                Is.Empty,
                "a retired entry is outside the partition: it owes a successor rather than a "
                    + "control, so listing it as uncovered claims an obligation it does not "
                    + "have:\n  "
                    + string.Join("\n  ", retiredButListed));
            Assert.That(
                unknownInList,
                Is.Empty,
                "the uncovered block names an entry that exists in no registry file, so it is a "
                    + "stale row that would keep the counts balanced while covering nothing:\n  "
                    + string.Join("\n  ", unknownInList));
        });
    }

    [Test]
    public void EveryUncoveredEntryHasItsReasonInTheIndexTable()
    {
        Document document = Document.Load();

        List<string> missing = document.UncoveredIds
            .Where(id => !document.IndexTableIds.Contains(id))
            .ToList();

        Assert.That(
            missing,
            Is.Empty,
            "the uncovered block holds IDs and no prose, so the reason an entry has no control "
                + "lives in § Which entry each section controls. Every listed ID must appear "
                + "there, or the gap becomes a bare identifier with no recorded reason:\n  "
                + string.Join("\n  ", missing));
    }

    private static int SharedPrefixLength(string left, string right)
    {
        int limit = Math.Min(left.Length, right.Length);
        int shared = 0;
        while (shared < limit && left[shared] == right[shared])
        {
            shared++;
        }

        return shared;
    }

    /// <summary>
    /// Reads a repository file, turning an unreadable one into a failure that cannot be mistaken
    /// for a coverage finding.
    /// </summary>
    /// <remarks>
    /// The distinction is the point. "This entry has no control" is a fact about the record and is
    /// reported as an assertion failure. "This file could not be read" is a fact about the
    /// environment and is reported as a thrown
    /// <see cref="InvalidOperationException"/> whose inner exception is the operating system's or
    /// the parser's own, so the message names the path, the operation, and the underlying error.
    /// Collapsing the two is what makes a gate look intermittent.
    /// </remarks>
    private static string ReadOrThrow(string relativePath)
    {
        string absolute = Path.Combine(TestArtifacts.RepositoryRoot, relativePath);
        try
        {
            return File.ReadAllText(absolute);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                "could not read " + relativePath + " at " + absolute
                    + ". This is a read failure and not a coverage finding: nothing here says any "
                    + "entry lacks a control, only that the record could not be opened. "
                    + failure.GetType().Name + ": " + failure.Message,
                failure);
        }
    }

    /// <summary>The parsed negative-control document.</summary>
    private sealed class Document
    {
        private Document(
            HashSet<string> headings,
            List<string> uncoveredIds,
            HashSet<string> indexTableIds,
            int statedCovered,
            int statedUncovered,
            int statedRetired,
            int statedTotal)
        {
            Headings = headings;
            UncoveredIds = uncoveredIds;
            IndexTableIds = indexTableIds;
            StatedCovered = statedCovered;
            StatedUncovered = statedUncovered;
            StatedRetired = statedRetired;
            StatedTotal = statedTotal;
        }

        internal HashSet<string> Headings { get; }

        internal List<string> UncoveredIds { get; }

        internal HashSet<string> IndexTableIds { get; }

        internal int StatedCovered { get; }

        internal int StatedUncovered { get; }

        internal int StatedRetired { get; }

        internal int StatedTotal { get; }

        internal static Document Load()
        {
            string text = ReadOrThrow(DocumentRelativePath);

            HashSet<string> headings = new(StringComparer.Ordinal);
            List<string> duplicates = new();
            foreach (Match match in HeadingShape.Matches(text))
            {
                string title = match.Groups["title"].Value;
                if (!headings.Add(title))
                {
                    duplicates.Add(title);
                }
            }

            if (duplicates.Count > 0)
            {
                throw Malformed(
                    "two or more headings share a title, so a fixtures pointer naming one is "
                        + "ambiguous about which section it credits: "
                        + string.Join(" | ", duplicates));
            }

            return new Document(
                headings,
                ParseUncoveredBlock(text),
                ParseIndexTableIds(text),
                statedCovered: ParseTotals(text, "covered"),
                statedUncovered: ParseTotals(text, "uncovered"),
                statedRetired: ParseTotals(text, "retired"),
                statedTotal: ParseTotals(text, "total"));
        }

        /// <remarks>
        /// A marker counts only when it is the whole of its own line, trimmed. That rule is not
        /// cosmetic and was found the hard way: § Proving this gate can fail quotes both markers
        /// inside its transcripts, so a substring search found the begin marker three times and
        /// reported the block as undecidable. The document has to be able to describe its own
        /// format, which means the format cannot be "this text appears somewhere". Requiring the
        /// marker to own its line lets prose quote it and still leaves exactly one place it can be
        /// a delimiter.
        /// </remarks>
        private static List<string> ParseUncoveredBlock(string text)
        {
            string[] allLines = text.Split('\n');
            List<int> begins = new();
            List<int> ends = new();
            for (int index = 0; index < allLines.Length; index++)
            {
                string trimmed = allLines[index].Trim('\r', ' ', '\t');
                if (trimmed == BeginMarker)
                {
                    begins.Add(index);
                }
                else if (trimmed == EndMarker)
                {
                    ends.Add(index);
                }
            }

            if (begins.Count == 0)
            {
                throw Malformed(
                    "no line consists solely of the marker " + BeginMarker + ", so the uncovered "
                        + "list cannot be located. An absent block is not an empty one: a document "
                        + "with no block makes no statement about its gaps, and reading it as "
                        + "'nothing is uncovered' would turn a lost list into a passing gate");
            }

            if (begins.Count > 1)
            {
                throw Malformed(
                    "the marker " + BeginMarker + " owns more than one line (lines "
                        + string.Join(", ", begins.Select(line => line + 1))
                        + "), so which block is the uncovered list is undecidable");
            }

            int begin = begins[0];
            int end = ends.FirstOrDefault(line => line > begin, -1);
            if (end < 0)
            {
                throw Malformed(
                    "no line after the begin marker consists solely of " + EndMarker
                        + ", so the block has no end and its extent is undecidable");
            }

            List<string> lines = allLines[(begin + 1)..end]
                .Select(line => line.Trim('\r', ' ', '\t'))
                .Where(line => line.Length > 0)
                .ToList();

            if (lines.Count < 2 || lines[0] != "```text" || lines[^1] != "```")
            {
                throw Malformed(
                    "the block between the markers must be a single ```text fence and nothing "
                        + "else; found "
                        + lines.Count
                        + " non-blank line(s) starting "
                        + (lines.Count > 0 ? "\"" + lines[0] + "\"" : "(none)"));
            }

            List<string> rows = lines[1..^1];
            if (rows.Count == 0)
            {
                throw Malformed("the fence is empty; the declared count line is required");
            }

            const string countPrefix = "uncovered-count: ";
            if (!rows[0].StartsWith(countPrefix, StringComparison.Ordinal))
            {
                throw Malformed(
                    "the first line inside the fence must be \"" + countPrefix
                        + "<n>\"; found \"" + rows[0] + "\"");
            }

            if (!int.TryParse(
                    rows[0][countPrefix.Length..],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int declared))
            {
                throw Malformed(
                    "the declared count is not a non-negative integer: \"" + rows[0] + "\"");
            }

            List<string> ids = rows[1..];
            foreach (string id in ids)
            {
                if (!EntryIdShape.IsMatch(id))
                {
                    throw Malformed(
                        "\"" + id + "\" is not a VER-SIM-000-000 shaped entry ID. The block holds "
                            + "IDs and nothing else, so a comment, a reason, or a trailing note is "
                            + "malformed rather than ignored");
                }
            }

            if (ids.Count != declared)
            {
                throw Malformed(
                    "the block declares uncovered-count: " + declared + " and holds " + ids.Count
                        + " ID(s). The count is stated so that a row lost to a bad merge is a "
                        + "failure rather than a smaller list");
            }

            return ids;
        }

        private static HashSet<string> ParseIndexTableIds(string text)
        {
            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (Match match in Regex.Matches(
                text,
                @"^\|\s*`(?<id>VER-SIM-[0-9]{3}-[0-9]{3})`\s*\|",
                RegexOptions.CultureInvariant | RegexOptions.Multiline))
            {
                ids.Add(match.Groups["id"].Value);
            }

            if (ids.Count == 0)
            {
                throw Malformed(
                    "§ Which entry each section controls has no rows of the expected "
                        + "\"| `VER-SIM-000-000` |\" shape, so the human-readable reasons cannot be "
                        + "located. The table's shape is load-bearing for that check alone");
            }

            return ids;
        }

        private static int ParseTotals(string text, string group)
        {
            Match match = TotalsShape.Match(Regex.Replace(text, @"\s+", " "));
            if (!match.Success)
            {
                throw Malformed(
                    "the totals paragraph does not match the shape this gate reads. Its wording is "
                        + "checked on purpose, because an unchecked totals sentence is exactly the "
                        + "rot this fixture exists to stop; if the paragraph is being reworded, "
                        + "update TotalsShape in the same commit");
            }

            string value = match.Groups[group].Value;
            return string.Equals(value, "One", StringComparison.Ordinal)
                ? 1
                : int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
        }

        private static InvalidDataException Malformed(string detail)
        {
            return new InvalidDataException(
                "malformed " + DocumentRelativePath + ": " + detail
                    + ". This is a parse failure, not a coverage finding: no partition verdict was "
                    + "computed, so nothing here says any entry is covered or uncovered.");
        }
    }

    /// <summary>One registry entry, reduced to what the partition needs.</summary>
    private sealed record RegistryEntry(
        string Id,
        string SourceFile,
        bool IsRetired,
        IReadOnlyList<string> NamedSections);

    /// <summary>The seven SIM registry files.</summary>
    private sealed class RegistryCorpus
    {
        private RegistryCorpus(List<RegistryEntry> entries)
        {
            Entries = entries;
        }

        internal List<RegistryEntry> Entries { get; }

        internal static RegistryCorpus Load()
        {
            string directory = Path.Combine(TestArtifacts.RepositoryRoot, RegistryDirectory);
            string[] paths;
            try
            {
                paths = Directory.GetFiles(directory, RegistryGlob);
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
            {
                throw new InvalidOperationException(
                    "could not enumerate " + RegistryDirectory + " for " + RegistryGlob
                        + ". This is a read failure and not a coverage finding. "
                        + failure.GetType().Name + ": " + failure.Message,
                    failure);
            }

            Array.Sort(paths, StringComparer.Ordinal);
            if (paths.Length == 0)
            {
                throw new InvalidDataException(
                    "no file matching " + RegistryGlob + " under " + RegistryDirectory
                        + ". An empty corpus would satisfy the partition trivially, so it is a "
                        + "failure rather than a pass.");
            }

            List<RegistryEntry> entries = new();
            foreach (string path in paths)
            {
                string relative = RegistryDirectory + "/" + Path.GetFileName(path);
                string text = ReadOrThrow(relative);
                JsonDocument parsed;
                try
                {
                    parsed = JsonDocument.Parse(text);
                }
                catch (JsonException failure)
                {
                    throw new InvalidOperationException(
                        "could not decode " + relative
                            + " as JSON. This is a read failure and not a coverage finding: the "
                            + "entries in this file were never examined, so nothing here says any "
                            + "of them lacks a control. JsonException: " + failure.Message,
                        failure);
                }

                using (parsed)
                {
                    entries.AddRange(ReadEntries(parsed, relative));
                }
            }

            return new RegistryCorpus(entries);
        }

        private static IEnumerable<RegistryEntry> ReadEntries(JsonDocument parsed, string relative)
        {
            if (!parsed.RootElement.TryGetProperty("entries", out JsonElement entries)
                || entries.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException(
                    "malformed " + relative + ": no 'entries' array. A registry file whose entries "
                        + "cannot be found contributes nothing to the partition, which would let a "
                        + "whole package's gaps pass unnoticed.");
            }

            List<RegistryEntry> result = new();
            foreach (JsonElement entry in entries.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out JsonElement id)
                    || id.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidDataException(
                        "malformed " + relative + ": an entry has no string 'id'.");
                }

                if (!entry.TryGetProperty("status", out JsonElement status)
                    || status.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidDataException(
                        "malformed " + relative + ": entry " + id.GetString()
                            + " has no string 'status', so it cannot be classified as retired or "
                            + "not, and the partition does not apply to retired entries.");
                }

                if (!entry.TryGetProperty("fixtures", out JsonElement fixtures)
                    || fixtures.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidDataException(
                        "malformed " + relative + ": entry " + id.GetString()
                            + " has no 'fixtures' array. An absent array is not an empty one here: "
                            + "reading it as empty would silently move the entry to the uncovered "
                            + "side.");
                }

                List<string> sections = new();
                foreach (JsonElement fixture in fixtures.EnumerateArray())
                {
                    if (fixture.ValueKind != JsonValueKind.String)
                    {
                        throw new InvalidDataException(
                            "malformed " + relative + ": entry " + id.GetString()
                                + " has a non-string fixtures element.");
                    }

                    string value = fixture.GetString()!;
                    if (value.StartsWith(PointerPrefix, StringComparison.Ordinal))
                    {
                        sections.Add(value[PointerPrefix.Length..]);
                    }
                    else if (value.Contains(
                        Path.GetFileName(DocumentRelativePath),
                        StringComparison.Ordinal))
                    {
                        throw new InvalidDataException(
                            "malformed " + relative + ": entry " + id.GetString()
                                + " names the negative-control document as \"" + value
                                + "\", which is not the \"" + PointerPrefix
                                + "<section>\" form this gate reads. An unqualified pointer would "
                                + "be read as no pointer at all, which is the silent drop this "
                                + "check refuses to perform.");
                    }
                }

                result.Add(
                    new RegistryEntry(
                        id.GetString()!,
                        relative,
                        string.Equals(status.GetString(), "retired", StringComparison.Ordinal),
                        sections));
            }

            return result;
        }
    }
}
