using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Schema;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>One position at which a schema and the corpus disagree, and how often.</summary>
/// <remarks>
/// <para>
/// The normal form is <b>(corpus file, JSON pointer, keyword)</b> plus an occurrence
/// count, and it deliberately excludes the evaluator's human-readable message. A message
/// is prose owned by <c>JsonSchemaEvaluator</c>: rewording one would redden every line it
/// touches for a reason that has nothing to do with the content, and a golden that
/// reddens for editorial reasons is a golden that gets regenerated without being read.
/// </para>
/// <para>
/// <b>What the count is for.</b> Several keywords report more than once at one pointer -
/// <c>required</c> emits one error per missing property, all of them located at the object
/// that lacks them - so a set of bare triples would collapse them and a file gaining a
/// tenth missing field where it had nine would leave the set unchanged. The count keeps
/// that visible. What it cannot see is a swap <em>within</em> one triple: two missing
/// required properties trading places at the same object is the same triple with the same
/// count. That blindness is not left as a caveat -
/// <c>SchemaCorpusDivergenceTests.TheBaselineIsBlindToASwapWithinOnePosition</c> performs
/// exactly that swap and asserts the measurement does not move, so the gap is a committed
/// observation rather than a sentence somebody has to trust. Closing it needs a
/// discriminator per missing property, which today exists only inside the message.
/// </para>
/// </remarks>
internal sealed record DivergencePosition(
    string SchemaFileName,
    string CorpusFile,
    string Pointer,
    string Keyword,
    int Occurrences)
{
    /// <summary>The identity of the position, without the count.</summary>
    internal string Key => SchemaFileName + " | " + CorpusFile + " | " + Pointer + " | " + Keyword;

    /// <summary>The pinned line for this position.</summary>
    internal string Line =>
        Key + " | " + Occurrences.ToString(CultureInfo.InvariantCulture);
}

/// <summary>One schema's row in the report.</summary>
internal sealed record DivergenceRow(
    string SchemaFileName,
    string CorpusSelection,
    int FileCount,
    int ErrorCount,
    int CleanFileCount,
    IReadOnlyList<KeyValuePair<string, int>> Keywords);

/// <summary>
/// Runs every schema under <c>content/schemas/</c> over the definitions it governs under
/// <c>content/</c>, and reports the disagreement as a pinnable artifact.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> <c>content/schemas/README.md</c> presents the schemas as a
/// mirror of the authoritative typed validator, "kept for editors, external tooling, and
/// review", and what makes the mirror trustworthy is the agreement corpus. The agreement
/// corpus is written by hand under <c>tests/</c>. So until this file, no test anywhere
/// loaded a definition from <c>content/&lt;category&gt;/</c> and ran a schema over it, and
/// the mirror was green for the same reason an unrun check is green. The project had
/// already written the consequence down about one schema -
/// <c>tests/verification/DAT-001.json</c>, <c>VER-DAT-001-039</c>: "those two committed
/// artifacts contradict each other today and nothing is red only because no check reads
/// the corpus".
/// </para>
/// <para>
/// <b>Why a pinned baseline rather than a passing gate, recorded as forced rather than
/// chosen.</b> <b>Zero of the 138 authored definitions satisfy the schema that governs
/// them.</b> That holds on this branch's working tree at <c>59a4986</c>, measured through
/// this class, and on <c>origin/master</c>'s content at <c>e17b8b6</c>, measured through
/// <c>jsonschema</c> 4.26.0 out of this clone's object store: 138 of 138 files carry at
/// least one error on both, the least any single file carries is three here and two there,
/// and the most is 297 on both. There is therefore no green baseline anywhere to switch a
/// schema on from - turning any one of the sixteen into a gate today reddens its entire
/// category - so a pinned divergence is the only construction that can be committed green
/// at this sha. That is a different status from being the most elegant of several options,
/// and it is written here as forced because a design recorded as a preference gets
/// simplified away by a later reader while a design recorded as forced gets its premise
/// re-measured first.
/// </para>
/// <para>
/// <b>What this gate covers, and what it structurally cannot.</b> It covers
/// <em>realised</em> divergence: a disagreement between a schema and a definition that
/// exists today, in the tree the suite can read. It is blind by construction to every
/// latent defect - a constraint no authored value exercises produces no error to pin, so
/// it contributes no row and its absence from the report is not evidence of its absence
/// from the schemas.
/// </para>
/// <para>
/// <b>The worked example is an intended constraint, not a defect, which is what makes it the
/// clean case.</b> All seventeen schemas declare <c>"enum": []</c> inline for a tag value -
/// sixteen at <c>$defs/tags/items</c> and <c>envelope.schema.json</c> at <c>$defs/tag</c>,
/// which <c>tags.items</c> reaches by a <c>$ref</c> hop upstream of the declaration. An empty
/// <c>enum</c> matches nothing, and that is the mechanism rather than an oversight: doc 40
/// § <c>tags</c> vocabulary says "the closed vocabulary starts empty and gains a term only
/// when a concrete query or tooling need requires it; the term is added to the vocabulary in
/// the same change that first uses it", the schema's own <c>description</c> states the same
/// protocol, and <c>TagVocabulary</c> mirrors it in typed code with an empty
/// <c>Declared</c>. Every one of the 138 authored files authors <c>tags: []</c> on both this
/// branch's corpus and <c>origin/master</c>'s, which is unanimous compliance with that
/// protocol, so <c>items</c> never applies and the constraint yields <b>zero</b> rows here -
/// <c>{"tags": []}</c> produces no error and <c>{"tags": ["x"]}</c> produces one. That is the
/// point: a designed, working, currently unexercised constraint is invisible to a baseline
/// over observed failures, exactly as an unnoticed one would be, and this gate cannot tell
/// the two apart. <b>If this pin ever reads zero, the correct reading is "no disagreement is
/// realised", never "the schemas and the content agree."</b> Whether a constraint that is
/// unexercised is intended is found by reading the schema and the document, not by counting
/// rows, and this class cannot be extended to answer it. One open question is recorded and
/// not taken here: the empty <c>enum</c> is reported to be incompatible with at least one
/// mainstream validator, and the choice between keeping the in-tree evaluator as the
/// enforcing instrument - already the ruling - and expressing "no value admitted yet" in a
/// form every implementation compiles while preserving the documented protocol belongs to
/// the schema's owner. Three things do tell an unexercised constraint from an abandoned one,
/// and none of them is this class: the document, the schema's own description, and
/// <c>EnvelopeSchemaPatternTests</c>, which asserts that <c>$defs/tag</c> declares
/// <c>enum</c> and that it is an empty array - "an absent enum and an empty one read the same
/// to a set comparison and mean opposite things" - with deleting the keyword recorded as a
/// failing injected control in <c>tests/verification/DAT-001.json</c>. So the tag vocabulary
/// could not have been quietly struck: the tier would have gone red.
/// </para>
/// <para>
/// <b>The instrument, and why the instrument is part of the finding.</b> The evaluator is
/// this repository's own <c>JsonSchemaEvaluator</c>, which is the only JSON Schema
/// implementation on any ref in this repository - measured over
/// <c>Directory.Packages.props</c> across every ref, which declares NUnit, the test SDK,
/// the NUnit adapter and analyzers and nothing else, and central package management means
/// that file is the whole answer. Instruments disagree in ways that matter: this one emits
/// one <c>additionalProperties</c> error per undeclared property, located at that
/// property, while <c>jsonschema</c> 4.26.0 emits one per object listing the names in its
/// message, so the totals differ and the per-property pointers here are strictly more
/// locatable. Worse, an instrument can fail before it reaches the content at all: Ajv
/// 8.20.0 is reported to refuse to compile all seventeen of these schemas over the empty tag
/// enum - a second-hand figure, not reproducible from this tree, which has no JSON Schema
/// library to reproduce it with - and a validator that will not load can never report
/// anything about a definition, so the class of defect it stops on is the class no
/// corpus-running gate can find. So a divergence figure is only meaningful with its
/// instrument named beside it.
/// </para>
/// <para>
/// <b>And with its row set and traversal depth named beside it, in the same sentence as the
/// number.</b> One library over one corpus yields a different total for every choice of the
/// two, and the spread is not small. This measurement is <b>sixteen category rows, one row
/// per definition against the single schema that governs it, the envelope excluded, and the
/// evaluator's flat error list</b> - failed <c>oneOf</c> and <c>anyOf</c> branches are
/// reported as one error at the failing location and their sub-errors are not expanded,
/// while <c>additionalProperties</c> is reported once per undeclared property. Under that
/// shape the figure is <b>3,109 errors at 2,560 positions over 138 files at
/// <c>59a4986</c></b>. <b>No ranking of the schemas travels without its traversal:</b>
/// under this shape <c>branch.schema.json</c> is worst at 748 and <c>relic.schema.json</c>
/// is sixth at 245, while under the per-object shallow shape relic is fifth.
/// <c>branch</c> is worst under every shape measured.
/// </para>
/// <para>
/// <b>The instrument gap, measured rather than reconciled, and asymmetrically because the
/// two directions are not equally dangerous.</b> Over-reporting inflates the pin, which is
/// noisy and safe; under-reporting makes it blind, which is worse than no pin at all because
/// it certifies coverage it does not have. So the under-reporting direction is enumerated
/// exhaustively rather than sampled. The same corpus and row set under <c>jsonschema</c>
/// 4.26.0 - a second opinion with no stake, since nothing will ever run it here - gives
/// <b>1,927</b> where this gives <b>3,109</b>, and the 1,182 difference is <b>entirely one
/// keyword's arity</b>: the two agree exactly on <c>required</c> 955, <c>type</c> 247,
/// <c>enum</c> 101, <c>oneOf</c> 48 and <c>pattern</c> 23, and the 825 positions at those
/// five keywords are the <em>same set</em>, position for position, with no position reported
/// fewer times here. The whole difference is <c>additionalProperties</c>: 553 positions
/// there, 1,735 here. <b>Of the 553 positions <c>jsonschema</c> reports that this evaluator
/// does not, all 553 are <c>additionalProperties</c> at an object, and every one of them is
/// covered by at least one row here at a direct child of that object - zero uncovered - and
/// all 1,735 rows only this evaluator reports are exactly those children.</b> So the
/// under-reporting set is empty: nothing <c>jsonschema</c> finds is unrepresented here, and
/// the gap is a relocation of one keyword's report from the object to the property. This
/// evaluator <b>splits</b> rather than misses: <c>content/relics/REL-01.json</c> carries
/// seven undeclared root fields, and <c>jsonschema</c> reports one error at the root whose
/// <em>message</em> lists "'acquisition', 'behavior_registration', 'core_tradeoff',
/// 'primary_transformation', 'rarity_and_weighting', 'stacking_and_exclusivity', 'trigger'
/// were unexpected", while this evaluator reports seven, each located at the property -
/// <c>/acquisition</c>, <c>/behavior_registration</c>, and so on. Neither loses a finding.
/// But for a normal form that excludes the message the difference is decisive rather than
/// cosmetic: under the per-object arity those seven fields collapse into one position with
/// a count of one, and renaming one undeclared field to another undeclared field would move
/// nothing - the substitution blindness this whole design exists to remove, one keyword
/// over. The instrument that puts the property name in the pointer is the one the pin needs,
/// which is a reason to prefer it beyond its being the one the suite can run.
/// </para>
/// <para>
/// <b>Why a correction to the evaluator is never in the same commit as a baseline it
/// measured, with the reason and not just the rule</b>, because a rule whose reason is not
/// written down gets collapsed by whoever cannot see the reason. An instrument and the
/// measurement it produced cannot change together: afterwards nobody can tell whether a row
/// moved because the content changed or because the instrument did, and the baseline's
/// provenance is gone rather than merely unclear. It is the same failure as a golden
/// regenerated by the change it was meant to check. So an evaluator defect found while
/// measuring is recorded with its rows and left alone, and the fix lands separately with the
/// baseline deliberately re-measured and the change named. The three sentences above that
/// look like ceremony - this one, the realised-divergence-only scope, and forced-rather-than-
/// chosen - are each load-bearing for the same reason.
/// </para>
/// <para>
/// This class asserts nothing about whether an authored order is correct. It reads
/// arrays only in the sense that a keyword failure inside one carries the element's
/// pointer; it does not compare element sequences, and it must not grow a check that
/// does.
/// </para>
/// </remarks>
internal sealed class SchemaCorpusDivergence
{
    private SchemaCorpusDivergence(
        IReadOnlyList<DivergenceRow> rows,
        IReadOnlyList<DivergencePosition> positions,
        IReadOnlyList<string> noCorpus,
        IReadOnlyList<string> excluded,
        int fileCount,
        int errorCount,
        int cleanFileCount)
    {
        Rows = rows;
        Positions = positions;
        NoCorpus = noCorpus;
        Excluded = excluded;
        FileCount = fileCount;
        ErrorCount = errorCount;
        CleanFileCount = cleanFileCount;
    }

    /// <summary>One row per exercised schema, in schema-file-name order.</summary>
    internal IReadOnlyList<DivergenceRow> Rows { get; }

    /// <summary>Every disagreement position, in ordinal line order.</summary>
    internal IReadOnlyList<DivergencePosition> Positions { get; }

    /// <summary>The schema files exercised against no corpus, as repository-relative paths.</summary>
    internal IReadOnlyList<string> NoCorpus { get; }

    /// <summary>The schema files deliberately outside the pin, as repository-relative paths.</summary>
    internal IReadOnlyList<string> Excluded { get; }

    /// <summary>How many corpus files were evaluated.</summary>
    internal int FileCount { get; }

    /// <summary>How many errors were reported over the whole corpus.</summary>
    internal int ErrorCount { get; }

    /// <summary>How many corpus files satisfied the schema governing them.</summary>
    internal int CleanFileCount { get; }

    /// <summary>The schema file names that were exercised against at least one file.</summary>
    internal IReadOnlyList<string> Exercised
    {
        get
        {
            List<string> names = new();
            foreach (DivergenceRow row in Rows)
            {
                names.Add(row.SchemaFileName);
            }

            return names;
        }
    }

    /// <summary>
    /// Measures every declared kind's schema against its corpus beneath
    /// <paramref name="root"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">A schema does not load, or a corpus file does not parse.</exception>
    internal static SchemaCorpusDivergence Measure(string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);

        List<DivergenceRow> rows = new();
        List<DivergencePosition> positions = new();
        List<string> noCorpus = new();
        int fileCount = 0;
        int errorCount = 0;
        int cleanFileCount = 0;

        foreach (CategoryDescriptor descriptor in CategorySchemas.All)
        {
            IReadOnlyList<string> files = AuthoredCorpus.FilesOf(descriptor.Kind, root);
            if (AuthoredCorpus.NoCorpus.ContainsKey(descriptor.Kind))
            {
                noCorpus.Add(descriptor.SchemaPath);
                continue;
            }

            JsonSchemaDocument schema = Load(root, descriptor);
            Dictionary<string, int> keywords = new(StringComparer.Ordinal);
            int rowErrors = 0;
            int rowClean = 0;

            foreach (string file in files)
            {
                Dictionary<string, DivergencePosition> byKey = new(StringComparer.Ordinal);
                foreach (JsonSchemaError error in Evaluate(schema, root, file))
                {
                    string pointer = error.InstanceLocation.IsRoot
                        ? "(root)"
                        : error.InstanceLocation.Value;
                    DivergencePosition position = new(
                        descriptor.SchemaFileName, file, pointer, error.Keyword, 1);
                    byKey[position.Key] = byKey.TryGetValue(position.Key, out DivergencePosition? seen)
                        ? seen with { Occurrences = seen.Occurrences + 1 }
                        : position;

                    keywords[error.Keyword] = keywords.TryGetValue(error.Keyword, out int count)
                        ? count + 1
                        : 1;
                    rowErrors++;
                }

                if (byKey.Count == 0)
                {
                    rowClean++;
                }

                positions.AddRange(byKey.Values);
            }

            rows.Add(new DivergenceRow(
                descriptor.SchemaFileName,
                AuthoredCorpus.DescribeSelection(descriptor.Kind),
                files.Count,
                rowErrors,
                rowClean,
                SortKeywords(keywords)));

            fileCount += files.Count;
            errorCount += rowErrors;
            cleanFileCount += rowClean;
        }

        rows.Sort(static (left, right) =>
            string.CompareOrdinal(left.SchemaFileName, right.SchemaFileName));
        positions.Sort(static (left, right) => string.CompareOrdinal(left.Key, right.Key));
        noCorpus.Sort(StringComparer.Ordinal);

        List<string> excluded = new();
        foreach (string name in AuthoredCorpus.Excluded.Keys)
        {
            excluded.Add("content/schemas/" + name);
        }

        excluded.Sort(StringComparer.Ordinal);

        return new SchemaCorpusDivergence(
            rows, positions, noCorpus, excluded, fileCount, errorCount, cleanFileCount);
    }

    /// <summary>Every error one schema reports over one corpus file.</summary>
    internal static IReadOnlyList<JsonSchemaError> Evaluate(
        JsonSchemaDocument schema,
        string root,
        string corpusFile)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentException.ThrowIfNullOrEmpty(corpusFile);

        byte[] bytes = File.ReadAllBytes(
            Path.Combine(root, corpusFile.Replace('/', Path.DirectorySeparatorChar)));
        using JsonDocument instance = JsonDocument.Parse(
            bytes,
            new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Disallow,
                AllowTrailingCommas = false,
            });

        return JsonSchemaEvaluator.Evaluate(schema, instance.RootElement).Errors;
    }

    /// <summary>Loads one category schema from beneath <paramref name="root"/>.</summary>
    /// <exception cref="InvalidOperationException">The schema does not load.</exception>
    internal static JsonSchemaDocument Load(string root, CategoryDescriptor descriptor)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);
        ArgumentNullException.ThrowIfNull(descriptor);

        string path = Path.Combine(
            root, "content", "schemas", descriptor.SchemaFileName);
        JsonSchemaLoadResult load = JsonSchemaLoader.Load(File.ReadAllBytes(path), descriptor.SchemaPath);
        if (!load.IsValid)
        {
            // A schema that does not load reports nothing about any definition, which would
            // read as agreement. Throwing keeps that from being a quiet zero.
            throw new InvalidOperationException(
                descriptor.SchemaPath + " does not load: " + string.Join("; ", load.Diagnostics));
        }

        return load.Schema!;
    }

    /// <summary>The pinned text: the header, the per-schema table, and every position.</summary>
    internal string Render()
    {
        StringBuilder text = new();
        text.Append("# content/schemas/ measured against the definitions under content/.\n");
        text.Append("#\n");
        text.Append("# One line per (corpus file, JSON pointer, keyword) with its occurrence count.\n");
        text.Append("#\n");
        text.Append("# What this baseline may be read as claiming, stated here because a limit\n");
        text.Append("# recorded in a report is a limit the reader never meets:\n");
        text.Append("#  - Realised divergence only. A constraint no authored value exercises produces\n");
        text.Append("#    no row, so zero rows would mean nothing is realised and never that the\n");
        text.Append("#    schemas and the content agree. The worked example is the empty tag enum\n");
        text.Append("#    all seventeen schemas declare: it admits no tag, which is the documented\n");
        text.Append("#    initial state of a vocabulary that grows a term in the same change that\n");
        text.Append("#    first uses one, and all 138 files comply by authoring tags: [] - so a\n");
        text.Append("#    designed and working constraint contributes zero rows here, and this file\n");
        text.Append("#    cannot tell an unexercised constraint from an absent one. Three other\n");
        text.Append("#    things do: the document, the schema description, and\n");
        text.Append("#    EnvelopeSchemaPatternTests, which asserts $defs/tag declares enum and that\n");
        text.Append("#    it is empty, with deleting the keyword recorded as a failing control.\n");
        text.Append("#  - Two relic constraints struck at b6c9f86 were row-neutral for the same\n");
        text.Append("#    reason, and this file is the evidence: the ten /affected_scope type rows\n");
        text.Append("#    below are unchanged, because a type failure fires before items is applied,\n");
        text.Append("#    so the element enum was never reached; and overrides_or_replaces appears\n");
        text.Append("#    nowhere below, because every relic authors it one level down from the\n");
        text.Append("#    declaration, so its element pattern was never reached either. A strike\n");
        text.Append("#    that removes no row is a correction to a dead constraint rather than a\n");
        text.Append("#    reduction of the measured disagreement.\n");
        text.Append("#  - Blind to a swap within one position: two required properties trading\n");
        text.Append("#    places at one object is one line at one count. Asserted by a test, not\n");
        text.Append("#    assumed.\n");
        text.Append("#  - Nothing a second implementation finds is missing here. Of the 553 positions\n");
        text.Append("#    jsonschema 4.26.0 reports over this corpus that this evaluator does not,\n");
        text.Append("#    all 553 are additionalProperties at an object and every one is covered by a\n");
        text.Append("#    row below at a direct child of that object: zero unrepresented, and the 825\n");
        text.Append("#    positions at the other five keywords are the same set position for\n");
        text.Append("#    position. If that number is ever not zero it is a limit on what this file\n");
        text.Append("#    can claim, and it belongs in this header rather than in a report.\n");
        text.Append("#\n");
        text.Append("# Instrument: MechaMiner.Content.Schema.JsonSchemaEvaluator, this repository's\n");
        text.Append("# own strict draft 2020-12 evaluator and the only one on any ref here. Its\n");
        text.Append("# revision is this commit: an in-tree instrument needs no version stamp because\n");
        text.Append("# the tree is the stamp, and a total carrying a corpus and a traversal but no\n");
        text.Append("# instrument is the same defect one axis over. jsonschema 4.26.0 over this same\n");
        text.Append("# corpus and row set reports 1927 rather than 3109; the whole 1182 difference is\n");
        text.Append("# additionalProperties arity - one error per object there, one per undeclared\n");
        text.Append("# property here - and every other keyword total agrees exactly.\n");
        text.Append("#\n");
        text.Append("# Row set and traversal, without which no total below is comparable to another:\n");
        text.Append("# sixteen category rows, one row per definition against the single schema that\n");
        text.Append("# governs it, envelope excluded; every error rather than the first; failed oneOf\n");
        text.Append("# and anyOf branches reported once at the failing location with their sub-errors\n");
        text.Append("# not expanded; additionalProperties reported once per undeclared property.\n");
        text.Append("# A per-schema ranking taken from this file is only true of this traversal.\n");
        text.Append('\n');

        Append(text, "schemas exercised", Exercised.Count);
        Append(text, "corpus files", FileCount);
        Append(text, "corpus files satisfying their schema", CleanFileCount);
        Append(text, "errors", ErrorCount);
        Append(text, "positions", Positions.Count);
        foreach (string schema in NoCorpus)
        {
            text.Append("schema with no authored corpus: ").Append(schema).Append('\n');
        }

        foreach (string schema in Excluded)
        {
            text.Append("schema excluded from the pin: ").Append(schema).Append('\n');
        }

        text.Append('\n');
        text.Append("## per schema: schema | corpus | files | errors | clean | keywords\n");
        foreach (DivergenceRow row in Rows)
        {
            List<string> keywords = new();
            foreach (KeyValuePair<string, int> keyword in row.Keywords)
            {
                keywords.Add(keyword.Key + "=" + keyword.Value.ToString(CultureInfo.InvariantCulture));
            }

            text.Append(row.SchemaFileName)
                .Append(" | ").Append(row.CorpusSelection)
                .Append(" | ").Append(row.FileCount.ToString(CultureInfo.InvariantCulture))
                .Append(" | ").Append(row.ErrorCount.ToString(CultureInfo.InvariantCulture))
                .Append(" | ").Append(row.CleanFileCount.ToString(CultureInfo.InvariantCulture))
                .Append(" | ").Append(string.Join(", ", keywords))
                .Append('\n');
        }

        text.Append("total | | ")
            .Append(FileCount.ToString(CultureInfo.InvariantCulture)).Append(" | ")
            .Append(ErrorCount.ToString(CultureInfo.InvariantCulture)).Append(" | ")
            .Append(CleanFileCount.ToString(CultureInfo.InvariantCulture)).Append(" |\n");

        text.Append('\n');
        text.Append("## positions: schema | corpus file | pointer | keyword | occurrences\n");
        foreach (DivergencePosition position in Positions)
        {
            text.Append(position.Line).Append('\n');
        }

        return text.ToString();
    }

    /// <summary>Every position's line, for a set comparison against another measurement.</summary>
    internal IReadOnlyCollection<string> Lines()
    {
        HashSet<string> lines = new(StringComparer.Ordinal);
        foreach (DivergencePosition position in Positions)
        {
            lines.Add(position.Line);
        }

        return lines;
    }

    private static void Append(StringBuilder text, string label, int value)
    {
        text.Append(label).Append(": ").Append(value.ToString(CultureInfo.InvariantCulture)).Append('\n');
    }

    private static IReadOnlyList<KeyValuePair<string, int>> SortKeywords(Dictionary<string, int> keywords)
    {
        List<KeyValuePair<string, int>> sorted = new(keywords);
        sorted.Sort(static (left, right) => left.Value == right.Value
            ? string.CompareOrdinal(left.Key, right.Key)
            : right.Value.CompareTo(left.Value));
        return sorted;
    }
}
