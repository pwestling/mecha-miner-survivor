using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// The document is the third anchor: doc 40's minted-grammar table, the schema
/// <c>pattern</c>s, and <see cref="ContentCategories"/> are each asserted against it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b>
/// <see cref="EnvelopeSchemaPatternTests.TheStableIdAlternativesAreExactlyTheDeclaredCategoryGrammars"/>
/// holds the schema's ID alternatives and <see cref="ContentCategories"/> equal to each
/// other. That stops those two drifting apart and does nothing to stop them drifting from
/// the document together, which is not a hypothetical: the resource grammar was carried
/// identically by both while no accepted document minted <c>RSC-</c> at all. Two-party
/// agreement is not verification when both parties derive from a third thing and neither
/// reads it. Doc 91 § Negative control adequacy states the general form - "an invariant
/// asserting that two sets match is blind to a correlated deletion from both sides ...
/// such an invariant needs a third anchor".
/// </para>
/// <para>
/// <b>The table, not the prose - for the grammar.</b> Doc 40 § Minted content-ID
/// grammars states each grammar twice on purpose and says which half a machine reads: the
/// table "is what a check reads to detect that a schema <c>pattern</c> or an
/// implementation category table has drifted from this document". Every grammar
/// comparison here therefore reads the table. A check that scraped the surrounding
/// English for a regex would break on the first editorial rewrite, be marked flaky, and
/// be deleted by someone doing a reasonable cleanup - which leaves the repository worse
/// off than never having had it.
/// </para>
/// <para>
/// <b>And the prose, for one thing only: that the prefix is still in it.</b> The same
/// section makes the two halves agreeing a mandatory rule - "the two <b>must agree</b>
/// ... neither may be deleted in favor of the other" - and nothing asserted that rule, so
/// a prefix could leave one half and stay in the other with every test green. The
/// document would then describe a set of aggregates its own table does not mint.
/// <see cref="EveryPrefixInTheTableIsNamedInTheProseThatMintsIt"/> closes that, and it
/// asks the smallest question that closes it: does the token occur. Containment is not
/// parsing. It has no opinion about what the sentence says, so an editorial rewrite
/// cannot break it, and it fails in exactly one case - a prefix has genuinely left one
/// side.
/// </para>
/// <para>
/// <b>Shape of the assertions.</b> One case per minted prefix, so deleting a row fails a
/// test whose name contains the prefix that was deleted, plus set-equality assertions
/// against lists written out here, so adding a row or an implementation fails too. The
/// written-out lists are the independent anchor: they are not derived from the document,
/// from the schemas, or from the C# table, so a correlated deletion from all three still
/// fails.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-039</c>, <c>VER-DAT-001-047</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class DocumentGrammarAgreementTests
{
    /// <summary>
    /// The document path. Doc 40 is the minting authority; nothing else is consulted.
    /// </summary>
    private const string DocumentPath = "docs/technical/40-content-data-and-validation.md";

    /// <summary>The heading whose table this fixture reads.</summary>
    private const string TableHeading = "### Minted content-ID grammars";

    /// <summary>
    /// Every prefix doc 40 mints, written out here rather than read from the document.
    /// </summary>
    /// <remarks>
    /// <b>This list is the third anchor and changing it is a deliberate change.</b> It
    /// exists so that deleting a row from the document's table - or deleting the table -
    /// fails here instead of quietly reducing what the other assertions range over. A
    /// prefix may only be removed from this list when doc 40 retires it under § Stable ID
    /// policy, which requires a migration/tombstone entry.
    /// </remarks>
    private static readonly string[] TheSixteenMintedPrefixes =
    {
        "RSC-",
        "UTL-",
        "WAV-",
        "MGC-",
        "FORMULA-",
        "FAB-",
        "STACK-",
        "CACHE-",
        "EXCL-",
        "HOOK-",
        "RESPEC-",
        "DEED-",
        "HORDE-",
        "FOOTPRINT-",
        "SIEGE-",
        "BOUNTY-",
    };

    /// <summary>
    /// The minted prefixes that a schema and a category descriptor exist for today, each
    /// paired with the two artifacts that must restate the document's grammar verbatim.
    /// </summary>
    /// <remarks>
    /// The other eleven rows in doc 40's table name aggregates whose definitions have not
    /// been extracted yet; they have no schema and no category, and
    /// <see cref="TheMintedPrefixesWithNoImplementationAreExactlyTheElevenAggregates"/>
    /// states that gap by name rather than letting it pass unremarked.
    /// </remarks>
    private static readonly MintedGrammar[] TheFiveImplementedGrammars =
    {
        new("RSC-", ContentCategory.Resource, "resource.schema.json"),
        new("UTL-", ContentCategory.Utility, "utility.schema.json"),
        new("WAV-", ContentCategory.Encounter, "encounter-schedule.schema.json"),
        new("MGC-", ContentCategory.Map, "map-generation-contract.schema.json"),
        new("FORMULA-", ContentCategory.Weapon, "weapon-stat-price-formula.schema.json"),
    };

    /// <summary>
    /// The grammars <see cref="ContentCategories"/> declares that doc 40 does not mint.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Being on this list is not an exemption; it is a recorded debt.</b> Doc 40
    /// § Minted content-ID grammars says prefixes "reused from the accepted gameplay
    /// register are governed by the reuse bullet above and are deliberately absent here",
    /// and § Stable ID policy names that register: <c>MCH-01</c>, <c>EN-01</c>,
    /// <c>BOSS-01</c>, <c>W-AB</c>, <c>REL-01</c>, "and equivalent utility/PowerUp/unlock
    /// IDs". Those are legitimately absent from the table.
    /// </para>
    /// <para>
    /// <c>SITE-</c>, <c>ELT-</c> and <c>PLAYER-</c> are not. No accepted document in this
    /// tree mints them - not doc 40 and not any gameplay document - so they remain this
    /// implementation's own claim. They are listed here so that the day a document mints
    /// one, this assertion fails and forces the row to be wired up rather than the mint
    /// going unnoticed.
    /// </para>
    /// </remarks>
    private static readonly string[] TheGrammarsNoDocumentMints =
    {
        "^MCH-[0-9]{2}$",
        "^EN-[0-9]{2}$",
        "^ELT-[0-9]{2}$",
        "^BOSS-[0-9]{2}$",
        "^W-[A-F]{2}$",
        "^W-[A-F]{2}(-[a-z0-9]+)+$",
        "^REL-[0-9]{2}$",
        "^PU-[A-Z][0-9]{2}$",
        "^UNL-[0-9]{2}$",
        "^SITE-[0-9]{2}$",
        "^PLAYER-[0-9]{2}$",
    };

    /// <summary>Names the five cases so a deleted row fails a case naming its prefix.</summary>
    private static IEnumerable<TestCaseData> ImplementedGrammarCases()
    {
        foreach (MintedGrammar grammar in TheFiveImplementedGrammars)
        {
            yield return new TestCaseData(grammar).SetName(
                "TheDocumentSchemaAndCodeStateOneGrammarFor_" + grammar.Prefix.TrimEnd('-'));
        }
    }

    /// <summary>
    /// For one minted prefix: the document's grammar, the category schema's
    /// <c>properties/id</c> pattern, the envelope schema's alternative, and the
    /// <see cref="ContentCategories"/> entry are one string, compared as text.
    /// </summary>
    /// <remarks>
    /// Compared as text rather than by matching sample IDs on purpose. Two regexes that
    /// accept the same sample set can still differ, and the report a reader needs names
    /// which artifact disagrees, not that some ID was rejected somewhere.
    /// </remarks>
    [TestCaseSource(nameof(ImplementedGrammarCases))]
    public void TheDocumentSchemaAndCodeStateOneGrammar(MintedGrammar grammar)
    {
        IReadOnlyDictionary<string, string> table = ReadDocumentTable();

        Assert.That(
            table.ContainsKey(grammar.Prefix),
            Is.True,
            () => "doc 40 § Minted content-ID grammars has no row for " + grammar.Prefix
                + "; the grammar it authorizes cannot be read, so the schema and "
                + nameof(ContentCategories) + " are unanchored for this prefix");

        string fromDocument = table[grammar.Prefix];

        Expect.Multiple(() =>
        {
            Assert.That(
                CategorySchemaIdPattern(grammar.SchemaFile),
                Is.EqualTo(fromDocument),
                () => "content/schemas/" + grammar.SchemaFile
                    + " properties/id must restate the grammar doc 40 mints for "
                    + grammar.Prefix);

            Assert.That(
                ContentCategories.Describe(grammar.Category).IdPatterns,
                Contains.Item(fromDocument),
                () => nameof(ContentCategories) + " category " + grammar.Category
                    + " must declare the grammar doc 40 mints for " + grammar.Prefix);

            Assert.That(
                EnvelopeStableIdAlternatives(),
                Contains.Item(fromDocument),
                () => "content/schemas/envelope.schema.json $defs/stable_id must accept the"
                    + " grammar doc 40 mints for " + grammar.Prefix);
        });
    }

    /// <summary>
    /// The document's table is exactly the sixteen prefixes named in this fixture.
    /// </summary>
    /// <remarks>
    /// This is the assertion that catches an addition. Minting a prefix without wiring it
    /// into this fixture leaves a grammar no gate compares, which is the state doc 40's
    /// table was written to end.
    /// </remarks>
    [Test]
    public void TheDocumentTableIsExactlyTheSixteenPrefixesNamedHere()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                ReadDocumentTable().Keys,
                Is.EquivalentTo(TheSixteenMintedPrefixes),
                "doc 40 § Minted content-ID grammars mints a different set of prefixes than "
                    + nameof(TheSixteenMintedPrefixes) + " states");

            // The set assertion above compares the document against this fixture's roster,
            // which is the third anchor - but only against whatever the roster currently
            // holds. Deleting a row from the document and the matching line from
            // TheSixteenMintedPrefixes in one edit shrinks both and leaves it green. The
            // sixteen is advertised outside this file: VER-DAT-001-039's summary records
            // that deleting the SIEGE- row "failed
            // TheDocumentTableIsExactlyTheSixteenPrefixesNamedHere", and doc 40 names the
            // eleven aggregates that make up part of the total. It is a promise, so it is
            // asserted.
            Assert.That(
                TheSixteenMintedPrefixes,
                Has.Length.EqualTo(16),
                nameof(TheSixteenMintedPrefixes) + " no longer states sixteen prefixes. A "
                    + "prefix may only leave this roster when doc 40 retires it under "
                    + "§ Stable ID policy, which requires a migration or tombstone entry");
            Assert.That(
                ReadDocumentTable(),
                Has.Count.EqualTo(16),
                "doc 40 § Minted content-ID grammars no longer mints sixteen prefixes");
        });
    }

    /// <summary>
    /// The eleven minted prefixes that no schema and no category implements, by name.
    /// </summary>
    /// <remarks>
    /// Doc 40 mints these against definitions that have not been extracted yet. Naming
    /// them keeps the gap visible: when one gains a schema this fails, and the fix is to
    /// move it into <see cref="TheFiveImplementedGrammars"/> so it starts being compared.
    /// A gate that silently skipped what it could not represent would have stopped being
    /// one.
    /// </remarks>
    [Test]
    public void TheMintedPrefixesWithNoImplementationAreExactlyTheElevenAggregates()
    {
        List<string> implemented = new();
        foreach (MintedGrammar grammar in TheFiveImplementedGrammars)
        {
            implemented.Add(grammar.Prefix);
        }

        List<string> unimplemented = new();
        foreach (string prefix in ReadDocumentTable().Keys)
        {
            if (!implemented.Contains(prefix))
            {
                unimplemented.Add(prefix);
            }
        }

        string[] theElevenAggregates =
        {
            "FAB-", "STACK-", "CACHE-", "EXCL-", "HOOK-", "RESPEC-",
            "DEED-", "HORDE-", "FOOTPRINT-", "SIEGE-", "BOUNTY-",
        };

        Expect.Multiple(() =>
        {
            Assert.That(
                unimplemented,
                Is.EquivalentTo(theElevenAggregates),
                "the set of minted prefixes awaiting an implementation changed");

            // Doc 40 § Minted content-ID grammars states the eleven by name, and
            // VER-DAT-001-039's summary promises "eleven aggregate prefixes have no
            // definition extracted yet and are asserted as an explicit list". Deleting a
            // row from the document and its line from this literal together satisfies the
            // set assertion; the count is what does not shrink with it.
            Assert.That(
                theElevenAggregates,
                Has.Length.EqualTo(11),
                "doc 40 names eleven aggregates awaiting extraction, and this list must "
                    + "still hold eleven of them");
            Assert.That(
                unimplemented,
                Has.Count.EqualTo(11),
                "eleven minted prefixes have no schema and no category today. If one gained "
                    + "one, move it into " + nameof(TheFiveImplementedGrammars)
                    + " so it starts being compared three ways");
        });
    }

    /// <summary>
    /// Every grammar <see cref="ContentCategories"/> declares is either minted by doc 40
    /// or on the recorded list of grammars no document mints.
    /// </summary>
    /// <remarks>
    /// The reverse direction of the per-prefix cases. Without it, a fourteenth category
    /// could be declared with an invented grammar and no assertion would range over it.
    /// </remarks>
    [Test]
    public void TheDeclaredGrammarsNoDocumentMintsAreExactlyTheElevenNamedHere()
    {
        IReadOnlyDictionary<string, string> table = ReadDocumentTable();
        List<string> minted = new(table.Values);

        List<string> unminted = new();
        foreach (ContentCategoryDescriptor descriptor in ContentCategories.All)
        {
            foreach (string pattern in descriptor.IdPatterns)
            {
                if (!minted.Contains(pattern))
                {
                    unminted.Add(pattern);
                }
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                unminted,
                Is.EquivalentTo(TheGrammarsNoDocumentMints),
                nameof(ContentCategories) + " declares a different set of undocumented "
                    + "grammars than " + nameof(TheGrammarsNoDocumentMints) + " records");

            // The recorded debt is eleven grammars. Dropping one from ContentCategories and
            // its line from TheGrammarsNoDocumentMints in one edit keeps the two sides
            // equal and reduces what this test ranges over, which is how a recorded debt
            // stops being recorded. The count is the part that does not move with it.
            Assert.That(
                TheGrammarsNoDocumentMints,
                Has.Length.EqualTo(11),
                nameof(TheGrammarsNoDocumentMints) + " no longer records eleven grammars. A "
                    + "grammar leaves this list when a document mints it - in which case it "
                    + "must appear in doc 40's table - or when the category is retired, not "
                    + "because the line was tidied away");
            Assert.That(
                unminted,
                Has.Count.EqualTo(11),
                nameof(ContentCategories) + " declares a different number of grammars no "
                    + "accepted document mints");
        });
    }

    /// <summary>
    /// Every prefix the table mints is named somewhere in the prose that mints it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rule this enforces is doc 40's own.</b> § Minted content-ID grammars:
    /// "The table is the <b>machine-readable</b> form of what the prose in this section
    /// and in the sections it cites states in sentences, and the two <b>must agree</b>
    /// ... neither may be deleted in favor of the other". Every other assertion in this
    /// fixture reads the table and nothing reads the prose, so the halves can be brought
    /// out of agreement without any test noticing - and the document then says one thing
    /// in its sentences and another in its rows, which is the failure mode a reader hits
    /// first and a machine never does.
    /// </para>
    /// <para>
    /// <b>Containment, deliberately, and not parsing.</b> The check asks only whether the
    /// prefix token occurs in the prose. It does not try to work out what the prose says
    /// about it, which sentence names it, or whether the description is accurate: a rule
    /// that scraped English would break on the first editorial rewrite, be marked flaky,
    /// and be deleted by someone doing a reasonable cleanup - and doc 40 names that exact
    /// outcome as the reason the table exists at all. Containment survives a rewrite,
    /// survives reordering, survives a paragraph being split in two, and fails in exactly
    /// one situation: a prefix has genuinely left one side.
    /// </para>
    /// <para>
    /// <b>Which prose.</b> The section's own, plus the section each row's "Minted in"
    /// column cites - that column is the document's own statement of where the prefix is
    /// minted, so following it is reading doc 40 rather than guessing. <c>UTL-</c> is the
    /// case that makes the difference: its grammar is minted under § Utilities and the
    /// grammars section never spells it outside the table.
    /// </para>
    /// </remarks>
    [Test]
    public void EveryPrefixInTheTableIsNamedInTheProseThatMintsIt()
    {
        IReadOnlyDictionary<string, string> mintedIn = ReadMintedInColumn();
        string grammarSectionProse = ProseUnder(TableHeading);

        List<string> unnamed = new();
        List<string> unresolved = new();

        foreach (KeyValuePair<string, string> row in mintedIn)
        {
            string prose = grammarSectionProse;

            string? anchor = CitedAnchor(row.Value);
            if (anchor is not null)
            {
                string? cited = ProseUnderAnchor(anchor);
                if (cited is null)
                {
                    unresolved.Add(row.Key + " cites '" + row.Value
                        + "', which resolves to no heading in " + DocumentPath);
                    continue;
                }

                prose += cited;
            }

            if (!prose.Contains(row.Key, StringComparison.Ordinal))
            {
                unnamed.Add(row.Key + " (minted in: " + row.Value + ")");
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                grammarSectionProse,
                Is.Not.Empty,
                DocumentPath + " § Minted content-ID grammars has no prose outside its "
                    + "table, so this check would pass over nothing");

            // The containment test must be able to fail. A prefix no document mints is
            // not in the prose, and if this found one the reader below is matching
            // something other than what it is given.
            Assert.That(
                grammarSectionProse,
                Does.Not.Contain("ZZZ-"),
                "the prose reader is matching text that is not there");

            Assert.That(
                unresolved,
                Is.Empty,
                () => "rows whose 'Minted in' column names a section that does not exist:"
                    + Environment.NewLine + string.Join(Environment.NewLine, unresolved));

            Assert.That(
                unnamed,
                Is.Empty,
                () => "doc 40 § Minted content-ID grammars requires the table and the prose "
                    + "to agree, and these prefixes have a row but are named in no "
                    + "sentence - in this section or in the section the row cites:"
                    + Environment.NewLine + string.Join(Environment.NewLine, unnamed)
                    + Environment.NewLine
                    + "Retiring a prefix means retiring it from both halves under § Stable "
                    + "ID policy, which requires a migration or tombstone entry. Deleting "
                    + "one half leaves the document contradicting itself.");
        });
    }

    /// <summary>Reads the table's prefix to "Minted in" column.</summary>
    private static IReadOnlyDictionary<string, string> ReadMintedInColumn()
    {
        Dictionary<string, string> rows = new(StringComparer.Ordinal);
        foreach (string[] cells in TableRows())
        {
            if (cells.Length < 5)
            {
                throw new InvalidOperationException(
                    DocumentPath + " § Minted content-ID grammars has a row with fewer "
                        + "than five cells; the 'Minted in' column is what says which "
                        + "section's prose must name the prefix");
            }

            rows[cells[0].Trim().Trim('`')] = cells[4].Trim();
        }

        return rows;
    }

    /// <summary>
    /// The heading anchor a "Minted in" cell links to, or null when the cell says the
    /// prefix is minted in the grammars section itself.
    /// </summary>
    private static string? CitedAnchor(string mintedIn)
    {
        int hash = mintedIn.IndexOf("](#", StringComparison.Ordinal);
        if (hash < 0)
        {
            return null;
        }

        int close = mintedIn.IndexOf(')', hash);
        return close < 0 ? null : mintedIn[(hash + 3)..close];
    }

    /// <summary>
    /// The prose beneath the heading whose GitHub anchor is <paramref name="anchor"/>, or
    /// null when no heading has it.
    /// </summary>
    private static string? ProseUnderAnchor(string anchor)
    {
        foreach (string line in DocumentLines())
        {
            if (line.StartsWith("#", StringComparison.Ordinal)
                && HeadingAnchor(line) == anchor)
            {
                return ProseUnder(line);
            }
        }

        return null;
    }

    /// <summary>
    /// Every non-table line beneath <paramref name="heading"/>, up to the next heading.
    /// </summary>
    /// <remarks>
    /// Table rows are dropped rather than included. Including them would make the
    /// containment check compare the table against itself, which is the two-party
    /// agreement this fixture exists to avoid.
    /// </remarks>
    private static string ProseUnder(string heading)
    {
        string[] lines = DocumentLines();
        int start = Array.IndexOf(lines, heading);
        if (start < 0)
        {
            throw new InvalidOperationException(
                DocumentPath + " has no '" + heading + "' heading");
        }

        System.Text.StringBuilder prose = new();
        for (int index = start + 1; index < lines.Length; index++)
        {
            string line = lines[index];
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                break;
            }

            if (line.TrimStart().StartsWith("|", StringComparison.Ordinal))
            {
                continue;
            }

            prose.Append(line).Append('\n');
        }

        return prose.ToString();
    }

    /// <summary>The GitHub-style anchor of a Markdown heading line.</summary>
    private static string HeadingAnchor(string line)
    {
        string text = line.TrimStart('#').Trim()
            .Replace("`", string.Empty, StringComparison.Ordinal);
        System.Text.StringBuilder anchor = new(text.Length);
        foreach (char character in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character) || character is '_' or '-')
            {
                anchor.Append(character);
            }
            else if (character == ' ')
            {
                anchor.Append('-');
            }
        }

        return anchor.ToString();
    }

    /// <summary>Reads doc 40.</summary>
    private static string[] DocumentLines()
    {
        return File.ReadAllLines(Path.Combine(
            TestArtifacts.RepositoryRoot,
            DocumentPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <summary>Every data row of the minted-grammar table, split into cells.</summary>
    private static IEnumerable<string[]> TableRows()
    {
        string[] lines = DocumentLines();
        int start = Array.IndexOf(lines, TableHeading);
        if (start < 0)
        {
            throw new InvalidOperationException(
                DocumentPath + " has no '" + TableHeading + "' section; the document this"
                    + " fixture anchors against is gone, which is a change to doc 40 and not"
                    + " a test fix");
        }

        List<string[]> rows = new();
        for (int index = start + 1; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                break;
            }

            if (!line.StartsWith("|", StringComparison.Ordinal))
            {
                continue;
            }

            string[] cells = line.Trim('|').Split('|');
            if (cells.Length < 2)
            {
                continue;
            }

            string prefix = cells[0].Trim().Trim('`');
            string grammar = cells[1].Trim().Trim('`');
            if (prefix.Length == 0
                || prefix == "Prefix"
                || grammar.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            rows.Add(cells);
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                DocumentPath + " § Minted content-ID grammars has no table rows");
        }

        return rows;
    }

    /// <summary>
    /// Reads doc 40's machine-readable grammar table into prefix to grammar.
    /// </summary>
    /// <remarks>
    /// Reads the table under <see cref="TableHeading"/> and stops at the next heading, so
    /// a later section adding a table of its own is not silently absorbed. Throws rather
    /// than returning empty when the heading or the table is missing: an empty dictionary
    /// would make every "contains" assertion here vacuously reportable as a missing row
    /// instead of as a deleted table.
    /// </remarks>
    private static IReadOnlyDictionary<string, string> ReadDocumentTable()
    {
        Dictionary<string, string> rows = new(StringComparer.Ordinal);
        foreach (string[] cells in TableRows())
        {
            string prefix = cells[0].Trim().Trim('`');
            if (!rows.TryAdd(prefix, cells[1].Trim().Trim('`')))
            {
                throw new InvalidOperationException(
                    DocumentPath + " mints " + prefix + " on two rows; one prefix cannot"
                        + " carry two grammars");
            }
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException(
                DocumentPath + " § Minted content-ID grammars has no table rows");
        }

        return rows;
    }

    /// <summary>Reads <c>properties/id/pattern</c> from a category schema.</summary>
    private static string CategorySchemaIdPattern(string schemaFile)
    {
        string path = Path.Combine(
            TestArtifacts.RepositoryRoot, "content", "schemas", schemaFile);
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllBytes(path));
        return schema.RootElement
            .GetProperty("properties").GetProperty("id").GetProperty("pattern").GetString()!;
    }

    /// <summary>Reads every alternative from the envelope schema's stable ID union.</summary>
    private static List<string> EnvelopeStableIdAlternatives()
    {
        string path = Path.Combine(
            TestArtifacts.RepositoryRoot, "content", "schemas", "envelope.schema.json");
        using JsonDocument schema = JsonDocument.Parse(File.ReadAllBytes(path));

        List<string> alternatives = new();
        foreach (JsonElement alternative in schema.RootElement
                     .GetProperty("$defs").GetProperty("stable_id").GetProperty("anyOf")
                     .EnumerateArray())
        {
            alternatives.Add(alternative.GetProperty("pattern").GetString()!);
        }

        return alternatives;
    }

    /// <summary>One minted prefix and the two artifacts that must restate its grammar.</summary>
    /// <param name="Prefix">The prefix exactly as doc 40's table names it.</param>
    /// <param name="Category">The category whose descriptor declares the grammar.</param>
    /// <param name="SchemaFile">The category schema file beneath <c>content/schemas/</c>.</param>
    public sealed record MintedGrammar(
        string Prefix, ContentCategory Category, string SchemaFile);
}
