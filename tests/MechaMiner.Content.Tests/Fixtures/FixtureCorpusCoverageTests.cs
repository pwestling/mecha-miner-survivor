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
/// Verification: <c>VER-DAT-001-028</c>, <c>VER-DAT-001-037</c>.
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

    /// <summary>
    /// How <c>Fixtures/schema/</c> is named, in the vocabulary its claims use.
    /// </summary>
    /// <remarks>
    /// The verification registry cites repository-relative paths, so the partition below
    /// speaks in those rather than in file names. That is also the right key on its own
    /// terms: a bare name is not an identity in a tree, and one name standing for two files
    /// is the defect this sweep has been closing everywhere else.
    /// </remarks>
    private const string SchemaFixturePrefix = "tests/MechaMiner.Content.Tests/Fixtures/schema/";

    private static string SchemaFixtureDirectory =>
        Path.Combine(FixtureCorpus.Root, "schema");

    /// <summary>
    /// The schema fixtures that no verification-registry entry claims, on purpose.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Empty is the honest state today, and it is not the same as absent.</b> Every one
    /// of the 34 fixtures in <c>Fixtures/schema/</c> is cited by an entry in
    /// <c>tests/verification/DAT-001.json</c>. The list exists so that the next fixture
    /// which genuinely should not be cited has somewhere to go that is a line in a diff,
    /// rather than being waved through by a check with a hole in it.
    /// </para>
    /// <para>
    /// It is a partition and not an exemption list, and the difference is what happens when
    /// somebody wants the check to stop complaining. Under an orphan scan, adding the file
    /// here is the fix, and nothing afterwards can tell "deliberately uncited" from
    /// "quietened". Under a partition, adding it here is a claim in the other direction that
    /// <see cref="AFixtureBothClaimedAndDeclaredUnclaimedIsReported"/> checks and that moves
    /// <see cref="TheSchemaFixtureCountTheRegistryClaims"/>, so the file cannot both be
    /// evidence for something and be declared to be evidence for nothing.
    /// </para>
    /// </remarks>
    private static readonly string[] SchemaFixturesDeliberatelyUnclaimed = [];

    /// <summary>
    /// How many fixtures in <c>Fixtures/schema/</c> a registry entry claims, written out.
    /// </summary>
    /// <remarks>
    /// <b>A literal, and it has to be.</b> Every other finding in the partition is "this
    /// list is empty", and a fixture deleted together with its registry citation satisfies
    /// all of them: the directory and the registry still agree, the corpus is simply one
    /// proof smaller and nothing says so. A count derived from either side agrees with
    /// itself on that input - see
    /// <see cref="AFixtureAndItsClaimDeletedTogetherAreCaughtOnlyByTheCount"/> - so the
    /// expected number is the one part of this that must not be computed. Changing it is a
    /// deliberate change to what the corpus proves.
    /// </remarks>
    private const int TheSchemaFixtureCountTheRegistryClaims = 34;

    /// <summary>
    /// Every file in <c>Fixtures/schema/</c> is either claimed by a verification-registry
    /// entry or declared deliberately unclaimed, and the number claimed is the number
    /// stated.
    /// </summary>
    /// <remarks>
    /// The mirror of <see cref="EveryFileInTheInvalidDirectoryIsClaimedByExactlyOneTableEntry"/>
    /// for the other fixture directory, which had no such check: a <c>.schema.json</c>
    /// dropped into <c>Fixtures/schema/</c> that no test reads and no entry cites runs
    /// nothing, while a reader counting the directory believes the corpus is one proof
    /// larger than it is.
    /// </remarks>
    [Test]
    public void EverySchemaFixtureIsClaimedByTheRegistryOrDeclaredUnclaimed()
    {
        SchemaFixturePartition.Result partition = SchemaFixturePartition.Of(
            SchemaFixtureNames(),
            VerificationRegistry.CitedFixturesUnder(SchemaFixturePrefix),
            SchemaFixturesDeliberatelyUnclaimed);

        Expect.Multiple(() =>
        {
            Assert.That(
                partition.FilesChecked,
                Is.GreaterThan(0),
                "a partition over zero files satisfies every emptiness assertion below, which "
                    + "is the fail-open the whole fixture exists for");
            Assert.That(
                partition.Unclassified,
                Is.Empty,
                () => "these schema fixtures are in neither class - no entry in "
                    + "tests/verification/DAT-001.json cites them and "
                    + nameof(SchemaFixturesDeliberatelyUnclaimed)
                    + " does not name them - so they prove nothing while looking like corpus: "
                    + string.Join(", ", partition.Unclassified)
                    + ". Cite the fixture from the entry whose claim it is evidence for, or "
                    + "declare it unclaimed and say why");
            Assert.That(
                partition.ClaimedYetDeclaredUnclaimed,
                Is.Empty,
                () => "these schema fixtures are cited by a registry entry and also declared "
                    + "unclaimed: " + string.Join(", ", partition.ClaimedYetDeclaredUnclaimed)
                    + ". The two classes are exclusive; a fixture in both has a classification "
                    + "no reader can settle, and the likeliest way to arrive here is silencing "
                    + "the check on a file that was evidence all along");
            Assert.That(
                partition.StaleUnclaimedDeclarations,
                Is.Empty,
                () => "these names in " + nameof(SchemaFixturesDeliberatelyUnclaimed)
                    + " match no file under Fixtures/schema/: "
                    + string.Join(", ", partition.StaleUnclaimedDeclarations)
                    + ". A declaration for a file that is not there classifies nothing today "
                    + "and pre-classifies whatever file takes that name tomorrow");
            Assert.That(
                partition.Claimed.Count,
                Is.EqualTo(TheSchemaFixtureCountTheRegistryClaims),
                () => "the registry claims " + partition.Claimed.Count + " schema fixtures and "
                    + nameof(TheSchemaFixtureCountTheRegistryClaims) + " says "
                    + TheSchemaFixtureCountTheRegistryClaims
                    + ". A fixture deleted along with its citation leaves every other "
                    + "assertion here green, because the directory and the registry still "
                    + "agree with each other; this literal is the only reader that does not "
                    + "shrink with the corpus. If the change was deliberate, move the number");
        });
    }

    /// <summary>
    /// The positive control: a directory whose files are all classified, one of each way,
    /// reports nothing and counts only the claimed.
    /// </summary>
    /// <remarks>
    /// Without this the four controls below would be satisfied by a partition that reports
    /// everything.
    /// </remarks>
    [Test]
    public void AFullyClassifiedSchemaFixtureDirectoryIsAccepted()
    {
        SchemaFixturePartition.Result partition = SchemaFixturePartition.Of(
            ThreeFixtures,
            new[] { ThreeFixtures[0], ThreeFixtures[1] },
            new[] { ThreeFixtures[2] });

        Expect.Multiple(() =>
        {
            Assert.That(partition.FilesChecked, Is.EqualTo(3));
            Assert.That(
                partition.Claimed,
                Is.EqualTo(new[] { ThreeFixtures[0], ThreeFixtures[1] }),
                () => "the declared-unclaimed file is not claimed evidence: "
                    + string.Join(", ", partition.Claimed));
            Assert.That(partition.Unclassified, Is.Empty);
            Assert.That(partition.ClaimedYetDeclaredUnclaimed, Is.Empty);
            Assert.That(partition.StaleUnclaimedDeclarations, Is.Empty);
        });
    }

    /// <summary>
    /// The orphan: a fixture in neither class is reported by name.
    /// </summary>
    /// <remarks>
    /// The finding the partition exists to force. It looks like corpus, it is counted as
    /// corpus by anyone reading the directory, and it asserts nothing.
    /// </remarks>
    [Test]
    public void AFixtureNoRegistryEntryClaimsIsReported()
    {
        SchemaFixturePartition.Result partition = SchemaFixturePartition.Of(
            ThreeFixtures,
            new[] { ThreeFixtures[0], ThreeFixtures[1] },
            System.Array.Empty<string>());

        Expect.Multiple(() =>
        {
            Assert.That(
                partition.Unclassified,
                Is.EqualTo(new[] { ThreeFixtures[2] }),
                () => "the uncited fixture must be reported, and it must be the only one: "
                    + string.Join(", ", partition.Unclassified));
            Assert.That(
                partition.Claimed.Count,
                Is.EqualTo(2),
                "an unclassified file is not claimed evidence and must not be counted as any");
        });
    }

    /// <summary>
    /// A fixture that is both cited and declared unclaimed is reported.
    /// </summary>
    /// <remarks>
    /// This is the control for the silencer. Under a plain orphan scan, adding a file to the
    /// list is how the report is made to go away, and the file goes on being evidence for a
    /// registry claim while the list says it is evidence for nothing. Here the contradiction
    /// fails, and the claimed count moves as well, so the silencing is visible twice.
    /// </remarks>
    [Test]
    public void AFixtureBothClaimedAndDeclaredUnclaimedIsReported()
    {
        SchemaFixturePartition.Result partition = SchemaFixturePartition.Of(
            ThreeFixtures,
            ThreeFixtures,
            new[] { ThreeFixtures[2] });

        Expect.Multiple(() =>
        {
            Assert.That(
                partition.ClaimedYetDeclaredUnclaimed,
                Is.EqualTo(new[] { ThreeFixtures[2] }),
                () => "the contradicted fixture must be reported, and the two consistent ones "
                    + "must not: " + string.Join(", ", partition.ClaimedYetDeclaredUnclaimed));
            Assert.That(
                partition.Claimed.Count,
                Is.EqualTo(2),
                "a fixture whose classification is contradicted cannot answer for the claimed "
                    + "count either, so the literal count catches this too");
            Assert.That(
                partition.Unclassified,
                Is.Empty,
                () => "a contradiction is not also an orphan: "
                    + string.Join(", ", partition.Unclassified));
        });
    }

    /// <summary>
    /// A declaration naming no file in the directory is reported.
    /// </summary>
    /// <remarks>
    /// Otherwise the list rots into declarations about files that no longer exist, and each
    /// one is a classification already granted to whatever file next takes that name.
    /// </remarks>
    [Test]
    public void AnUnclaimedDeclarationNamingNoFixtureIsReported()
    {
        SchemaFixturePartition.Result partition = SchemaFixturePartition.Of(
            ThreeFixtures,
            ThreeFixtures,
            new[] { SchemaFixturePrefix + "deleted-long-ago.schema.json" });

        Expect.Multiple(() =>
        {
            Assert.That(
                partition.StaleUnclaimedDeclarations,
                Is.EqualTo(new[] { SchemaFixturePrefix + "deleted-long-ago.schema.json" }),
                () => "the declaration naming no file must be reported: "
                    + string.Join(", ", partition.StaleUnclaimedDeclarations));
            Assert.That(
                partition.Claimed.Count,
                Is.EqualTo(3),
                "a stale declaration classifies none of the files that are there");
        });
    }

    /// <summary>
    /// A fixture deleted while its registry citation stays behind is caught by the count.
    /// </summary>
    /// <remarks>
    /// The citation outliving its file is also caught by
    /// <c>VerificationRegistryTests.EveryNamedFixturePathExists</c>, which is why this is the
    /// milder of the two deletion controls; the point here is that the partition's own
    /// emptiness assertions see nothing, because nothing on disk is misclassified.
    /// </remarks>
    [Test]
    public void AFixtureDeletedWhileItsClaimRemainsIsCaughtOnlyByTheCount()
    {
        SchemaFixturePartition.Result partition = SchemaFixturePartition.Of(
            new[] { ThreeFixtures[0], ThreeFixtures[1] },
            ThreeFixtures,
            System.Array.Empty<string>());

        Expect.Multiple(() =>
        {
            Assert.That(partition.Unclassified, Is.Empty);
            Assert.That(partition.ClaimedYetDeclaredUnclaimed, Is.Empty);
            Assert.That(partition.StaleUnclaimedDeclarations, Is.Empty);
            Assert.That(
                partition.Claimed.Count,
                Is.EqualTo(2),
                "every emptiness assertion passes on this input; only the count against a "
                    + "stated literal notices that a fixture left");
        });
    }

    /// <summary>
    /// A fixture and its registry citation deleted in the same change are caught by nothing
    /// except the count.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The correlated deletion, and the reason
    /// <see cref="TheSchemaFixtureCountTheRegistryClaims"/> is a literal rather than
    /// something derived. The directory and the registry agree with each other perfectly on
    /// this input: no orphan, no contradiction, no stale declaration, and no citation
    /// pointing at a missing file for <c>EveryNamedFixturePathExists</c> to find. The corpus
    /// is simply one proof smaller.
    /// </para>
    /// <para>
    /// The second assertion is the whole argument: a count taken from what is present equals
    /// what is present, so any expected value computed from either side of the partition
    /// would have agreed and passed.
    /// </para>
    /// </remarks>
    [Test]
    public void AFixtureAndItsClaimDeletedTogetherAreCaughtOnlyByTheCount()
    {
        string[] survivingFixtures = { ThreeFixtures[0], ThreeFixtures[1] };
        string[] survivingClaims = { ThreeFixtures[0], ThreeFixtures[1] };

        SchemaFixturePartition.Result partition = SchemaFixturePartition.Of(
            survivingFixtures, survivingClaims, System.Array.Empty<string>());

        Expect.Multiple(() =>
        {
            Assert.That(partition.Unclassified, Is.Empty);
            Assert.That(partition.ClaimedYetDeclaredUnclaimed, Is.Empty);
            Assert.That(partition.StaleUnclaimedDeclarations, Is.Empty);
            Assert.That(
                partition.Claimed.Count,
                Is.EqualTo(survivingClaims.Length),
                "the file set and the claim set agree with each other, which is exactly why "
                    + "neither can be the expected count");
            Assert.That(
                partition.Claimed.Count,
                Is.EqualTo(2),
                "and the corpus is one proof smaller than it was, which only a literal stated "
                    + "elsewhere can say");
        });
    }

    /// <summary>
    /// A three-file schema-fixture directory for the partition controls: enough for one
    /// file to be reported while two must not be.
    /// </summary>
    private static readonly string[] ThreeFixtures =
    {
        SchemaFixturePrefix + "reach-root.schema.json",
        SchemaFixturePrefix + "reach-defs.schema.json",
        SchemaFixturePrefix + "no-bounds.schema.json",
    };

    /// <summary>
    /// Every file under <c>Fixtures/schema/</c>, named as the registry names it.
    /// </summary>
    private static IReadOnlyList<string> SchemaFixtureNames()
    {
        List<string> names = new();
        foreach (string absolute in
                 Directory.GetFiles(SchemaFixtureDirectory, "*", SearchOption.AllDirectories))
        {
            names.Add(TestArtifacts.Relative(absolute));
        }

        return names;
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
