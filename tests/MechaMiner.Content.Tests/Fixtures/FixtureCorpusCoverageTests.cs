using System;
using System.Collections.Generic;
using System.IO;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// What the invalid corpus covers, asserted independently of the corpus table.
/// </summary>
/// <remarks>
/// <para>
/// Every gate over <see cref="FixtureCorpus.Invalid"/> iterates the table, so the table
/// is the only thing that decides which files are tested. Two failures follow from that,
/// and neither is visible to any test that iterates it.
/// </para>
/// <para>
/// <b>The orphan.</b> A <c>.json</c> file dropped into <c>Fixtures/invalid/</c> with no
/// row in the table runs no test at all. It looks like corpus, it is counted as corpus by
/// anyone reading the directory, and it asserts nothing. The reverse - a row naming a
/// file that has since been deleted - is loud rather than silent, but it fails as an
/// unhandled <see cref="FileNotFoundException"/> from three separate test cases rather
/// than as one sentence naming the path, so it is asserted here too.
/// </para>
/// <para>
/// <b>The shrinking code.</b> Three codes are provoked by two fixtures each. Deleting one
/// of a pair leaves the code still provoked by its sibling, so
/// <c>ContentDiagnosticCodesTests.TheCodesTheSuiteProvokesAreExactlyTheCodesDeclared</c>
/// still holds and nothing reports that the coverage halved.
/// <see cref="TheFixturesProvokingEachCodeAreExactlyTheRosterStatedHere"/> states the
/// roster here, so a deletion fails a test that names the code and the file.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-028</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class FixtureCorpusCoverageTests
{
    /// <summary>
    /// Every invalid fixture on disk, with the code it proves, written out here rather
    /// than read from <see cref="FixtureCorpus.Invalid"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Changing this roster is a deliberate change to the corpus.</b> The point of
    /// stating it twice is that the two statements are edited in separate places: a
    /// fixture removed from the table without being removed here fails with the file
    /// named, which a comparison derived from the table on both sides cannot do.
    /// </para>
    /// <para>
    /// The counts are the part with teeth. <c>MMC-1004</c>, <c>MMC-1005</c> and
    /// <c>MMC-2005</c> are each proved by two files, and a code proved by two files is one
    /// deletion away from being proved by one while every existing assertion stays green.
    /// </para>
    /// </remarks>
    private static readonly CodeCoverage[] TheFixtureRoster =
    {
        // Codec, band 1xxx.
        new("MMC-1001", "invalid/codec-comment.json"),
        new("MMC-1002", "invalid/codec-trailing-comma.json"),
        new("MMC-1003", "invalid/codec-duplicate-property.json"),
        new(
            "MMC-1004",
            "invalid/codec-nonfinite-nan.json",
            "invalid/codec-nonfinite-overflow.json"),
        new("MMC-1005", "invalid/codec-null-value.json", "invalid/codec-null-nested.json"),
        new("MMC-1006", "invalid/codec-camel-case-property.json"),
        new("MMC-1007", "invalid/codec-malformed.json"),
        new("MMC-1009", "invalid/limit-document-bytes.json"),
        new("MMC-1010", "invalid/limit-depth.json"),
        new("MMC-1011", "invalid/limit-object-properties.json"),
        new("MMC-1012", "invalid/limit-array-elements.json"),
        new("MMC-1013", "invalid/limit-node-count.json"),
        new("MMC-1014", "invalid/limit-string-length.json"),
        new("MMC-1015", "invalid/codec-root-not-object.json"),

        // Structural, band 2xxx.
        new("MMC-2001", "invalid/structural-unknown-field.json"),
        new("MMC-2002", "invalid/structural-missing-required.json"),
        new("MMC-2003", "invalid/structural-wrong-type.json"),
        new("MMC-2004", "invalid/structural-unknown-status.json"),
        new("MMC-2005", "invalid/version-non-integer.json", "invalid/version-nonpositive.json"),
        new("MMC-2006", "invalid/structural-tag-outside-vocabulary.json"),
        new("MMC-2007", "invalid/structural-name-key-literal-text.json"),
        new("MMC-2008", "invalid/structural-name-key-role-mismatch.json"),
        new("MMC-2009", "invalid/structural-empty-optional.json"),

        // Identity, band 3xxx.
        new("MMC-3001", "invalid/identity-bad-id-for-category.json"),
        new("MMC-3002", "invalid/identity-retired-id-reused.json"),

        // Traceability, band 4xxx.
        new("MMC-4001", "invalid/traceability-source-ref-malformed.json"),
        new("MMC-4002", "invalid/traceability-source-ref-path-line.json"),
        new("MMC-4003", "invalid/traceability-scope-unresolved.json"),
    };

    private static string InvalidDirectory => Path.Combine(FixtureCorpus.Root, "invalid");

    /// <summary>
    /// Every file in <c>Fixtures/invalid/</c> is claimed by exactly one table row.
    /// </summary>
    /// <remarks>
    /// An unclaimed file is the quiet failure: nothing reads it, nothing asserts on it,
    /// and the directory listing says the corpus is one fixture larger than it is. A file
    /// claimed twice is the other shape of the same confusion - one of the two rows is
    /// asserting something nobody meant.
    /// </remarks>
    [Test]
    public void EveryFileInTheInvalidDirectoryIsClaimedByExactlyOneTableEntry()
    {
        Dictionary<string, int> claims = new(StringComparer.Ordinal);
        foreach (FixtureCorpus.InvalidFixture fixture in FixtureCorpus.Invalid)
        {
            claims.TryGetValue(fixture.Path, out int count);
            claims[fixture.Path] = count + 1;
        }

        List<string> orphaned = new();
        List<string> claimedTwice = new();

        foreach (string absolute in
                 Directory.GetFiles(InvalidDirectory, "*", SearchOption.AllDirectories))
        {
            string relative = "invalid/"
                + Path.GetRelativePath(InvalidDirectory, absolute).Replace(Path.DirectorySeparatorChar, '/');

            if (!claims.TryGetValue(relative, out int count))
            {
                orphaned.Add(TestArtifacts.Relative(absolute));
            }
            else if (count > 1)
            {
                claimedTwice.Add(TestArtifacts.Relative(absolute) + " (" + count + " rows)");
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                orphaned,
                Is.Empty,
                () => "these files sit in the invalid corpus with no row in "
                    + "FixtureCorpus.Invalid, so no test reads them and no test would notice "
                    + "if they stopped being invalid: " + string.Join(", ", orphaned));
            Assert.That(
                claimedTwice,
                Is.Empty,
                () => "these files are claimed by more than one row, so one of the rows is "
                    + "asserting a code the file was not written for: "
                    + string.Join(", ", claimedTwice));
        });
    }

    /// <summary>
    /// Every table row names a file that is actually there.
    /// </summary>
    /// <remarks>
    /// Without this the failure is three <see cref="FileNotFoundException"/>s out of
    /// <c>InvalidFixtureCorpusTests</c> with the corpus root spliced into a stack trace.
    /// One assertion naming the row is cheaper to read and cannot be mistaken for a
    /// harness fault.
    /// </remarks>
    [Test]
    public void EveryTableEntryNamesAFileThatExists()
    {
        List<string> missing = new();

        foreach (FixtureCorpus.InvalidFixture fixture in FixtureCorpus.Invalid)
        {
            string absolute = Path.Combine(
                FixtureCorpus.Root, fixture.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute))
            {
                missing.Add(fixture.Path + " -> " + fixture.ExpectedCode);
            }
        }

        Assert.That(
            missing,
            Is.Empty,
            () => "FixtureCorpus.Invalid names these files, which are not on disk. A row "
                + "outliving its fixture proves nothing and cannot be run: "
                + string.Join(", ", missing));
    }

    /// <summary>
    /// The fixtures proving each code are exactly the ones this roster names.
    /// </summary>
    /// <remarks>
    /// <c>ContentDiagnosticCodesTests.TheCodesTheSuiteProvokesAreExactlyTheCodesDeclared</c>
    /// compares a set of codes, so it cannot see a code losing one of its two fixtures:
    /// the surviving sibling keeps the code in the provoked set. This compares files, and
    /// it takes its expected side from a roster written here rather than from the table
    /// under test.
    /// </remarks>
    [Test]
    public void TheFixturesProvokingEachCodeAreExactlyTheRosterStatedHere()
    {
        Dictionary<string, List<string>> actual = new(StringComparer.Ordinal);
        foreach (FixtureCorpus.InvalidFixture fixture in FixtureCorpus.Invalid)
        {
            if (!actual.TryGetValue(fixture.ExpectedCode, out List<string>? paths))
            {
                paths = new List<string>();
                actual[fixture.ExpectedCode] = paths;
            }

            paths.Add(fixture.Path);
        }

        Expect.Multiple(() =>
        {
            List<string> rosteredCodes = new();

            foreach (CodeCoverage expected in TheFixtureRoster)
            {
                rosteredCodes.Add(expected.Code);

                Assert.That(
                    ContentDiagnosticCodes.IsDeclared(expected.Code),
                    Is.True,
                    expected.Code + " is named by this roster but is not a declared code");

                actual.TryGetValue(expected.Code, out List<string>? provoking);
                List<string> found = provoking ?? new List<string>();

                foreach (string path in expected.Fixtures)
                {
                    Assert.That(
                        found,
                        Does.Contain(path),
                        path + " no longer provokes " + expected.Code + ", which this roster "
                            + "says is proved by " + expected.Fixtures.Count + " fixture"
                            + (expected.Fixtures.Count == 1 ? string.Empty : "s")
                            + (expected.Fixtures.Count == 1
                                ? ". "
                                : ". Dropping one of several leaves the code still provoked by "
                                    + "its siblings, so the provoked-equals-declared check "
                                    + "stays green with less coverage. ")
                            + "This names what went");
                }

                Assert.That(
                    found,
                    Is.EquivalentTo(expected.Fixtures),
                    "the fixtures provoking " + expected.Code + " changed. A fixture added "
                        + "for a code must be added to this roster in the same change, so that "
                        + "the corpus and its independent statement cannot drift");
            }

            Assert.That(
                actual.Keys,
                Is.EquivalentTo(rosteredCodes),
                "every code the invalid corpus provokes must appear in this roster, and the "
                    + "roster must name no code the corpus does not provoke");
        });
    }

    /// <summary>One diagnostic code and every invalid fixture that proves it.</summary>
    private sealed class CodeCoverage
    {
        internal CodeCoverage(string code, params string[] fixtures)
        {
            Code = code;
            Fixtures = fixtures;
        }

        internal string Code { get; }

        internal IReadOnlyList<string> Fixtures { get; }
    }
}
