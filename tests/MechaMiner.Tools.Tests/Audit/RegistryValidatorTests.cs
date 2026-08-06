using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MechaMiner.Tests.Support;
using MechaMiner.Tools.Audit;
using NUnit.Framework;

namespace MechaMiner.Tools.Tests.Audit;

/// <summary>
/// The identifier and cross-link registry validator, with one fixture per failure class.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-002</c>). Verification:
/// <c>VER-FND-009-007</c> through <c>VER-FND-009-011</c>, <c>VER-FND-009-012</c>,
/// <c>VER-FND-009-014</c>.
/// Requirements: <c>TR-CTR-006</c>, <c>TR-QUA-004</c>, <c>TR-AGT-003</c>.
/// </para>
/// <para>
/// <c>TASK-FND-009-002</c>'s completion gate is "missing, duplicate, dangling, and
/// malformed fixtures fail". Each class has its own fixture under
/// <c>build/policy-fixtures/registry/</c>, outside the solution, and each is required to
/// produce its own rule rather than merely some failure.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RegistryValidatorTests
{
    private const string FixtureRoot = "build/policy-fixtures/registry";

    /// <summary>
    /// The real repository has no structural identifier or registry errors. Findings that
    /// are specification-content defects are reported separately, by
    /// <see cref="TheSpecificationDefectInventoryIsRecorded"/>, so a documentation defect
    /// this task did not introduce cannot be silently absorbed and cannot silently grow.
    /// </summary>
    [Test]
    public void TheRepositoryHasNoIdentifierErrors()
    {
        ImmutableArray<RegistryFinding> findings = ValidateRepository();
        Assert.That(
            Render(findings, RegistrySeverity.Error),
            Is.Empty,
            "structural identifier or registry errors are owned by this task and must be zero");
    }

    /// <summary>
    /// The registry population, against a literal census written down in this test.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every other assertion in this fixture is a property of whatever registries happen to
    /// be on disk. None of them notices a registry that is gone. Measured, before this test
    /// existed: deleting the highest-ordinal entry from <c>FND-004</c>, <c>FND-007</c> and
    /// <c>FND-008</c> left the whole suite green, because the remaining ordinals were still
    /// gapless and nothing said how many entries there should be. Emptying every registry to
    /// <c>"entries": []</c> was green. Renaming <c>tests/verification/</c> away entirely was
    /// green, because <see cref="RegistrySources.ReadFromDisk"/> guards the scan with
    /// <c>Directory.Exists</c> and selector resolution returns early on an empty selector
    /// set - a satisfied registry and an absent one are the same observation.
    /// </para>
    /// <para>
    /// So the expected population cannot be derived from the directory. A count taken from
    /// the directory agrees with the directory by construction and can never report a
    /// deletion. The census below is therefore three independent literals:
    /// <see cref="ExpectedRegistryCensus"/> (one path and one entry count per file),
    /// <see cref="ExpectedRegistryFileCount"/>, and
    /// <see cref="ExpectedRegistryEntryCount"/>. The two scalars are not computed from the
    /// census; they are asserted against it, so removing a census row to accommodate a
    /// deleted file still fails, and decrementing a census row to accommodate a deleted entry
    /// still fails. Legitimately adding or retiring a registry entry means editing the
    /// literals in the same change, which is a visible edit in the diff rather than silent
    /// drift - the same ratchet <see cref="ExpectedSpecificationDefects"/> uses.
    /// </para>
    /// <para>
    /// Doc 91 § Verification registry: entries "are never renumbered", and doc 91 requires a
    /// verification entry to exist before its implementation. Both properties are about
    /// entries continuing to exist, which is exactly what a floor on the population asserts
    /// and what no rule over the present contents can.
    /// </para>
    /// <para>
    /// The ceiling on what this proves, stated so the next reader does not over-trust it:
    /// the census is a three-place edit tax that makes an accidental deletion loud, not
    /// evidence that an entry exists. Three consistent literal edits in this one file - the
    /// census row, <see cref="ExpectedRegistryFileCount"/> and
    /// <see cref="ExpectedRegistryEntryCount"/> - delete an entry with the suite green.
    /// That was ruled acceptable rather than fixed: nothing cheaper does better, and the
    /// alternative considered and rejected was a committed per-entry baseline, which is a
    /// second registry to keep in step with the first.
    /// </para>
    /// </remarks>
    [Test]
    public void TheRegistryPopulationMatchesItsLiteralCensus()
    {
        var observed = new SortedDictionary<string, int>(StringComparer.Ordinal);
        List<string> unreadable = new();
        foreach (RegistryDocument document in RepositorySources.Value.VerificationRegistries)
        {
            try
            {
                observed[document.Path] =
                    ToolsJsonContextAccess.DeserializeVerificationRegistry(document.Text).Entries.Count;
            }
            catch (JsonException error)
            {
                observed[document.Path] = -1;
                unreadable.Add(document.Path + ": " + error.Message);
            }
        }

        var expected = new SortedDictionary<string, int>(StringComparer.Ordinal);
        int censusEntries = 0;
        foreach ((string path, int entries) in ExpectedRegistryCensus)
        {
            expected[path] = entries;
            censusEntries += entries;
        }

        int observedEntries = 0;
        foreach (int entries in observed.Values)
        {
            observedEntries += entries;
        }

        List<string> evidence = new()
        {
            "# Registry population against the literal census in",
            "# RegistryValidatorTests.TheRegistryPopulationMatchesItsLiteralCensus",
            "# (doc 91 § Verification registry). The expected column is a literal in the test,",
            "# not a value read from tests/verification/, so a deleted registry or a deleted",
            "# entry is a mismatch rather than a smaller agreement with itself.",
            string.Empty,
            "# registry\texpected entries\tobserved entries",
        };
        foreach (string path in Union(expected.Keys, observed.Keys))
        {
            evidence.Add(
                path + "\t"
                + Cell(expected, path) + "\t"
                + Cell(observed, path));
        }

        evidence.Add(string.Empty);
        evidence.Add(
            "# files: expected " + ExpectedRegistryFileCount.ToString(CultureInfo.InvariantCulture)
            + ", observed " + observed.Count.ToString(CultureInfo.InvariantCulture));
        evidence.Add(
            "# entries: expected " + ExpectedRegistryEntryCount.ToString(CultureInfo.InvariantCulture)
            + ", observed " + observedEntries.ToString(CultureInfo.InvariantCulture));
        string artifact = WriteEvidence("registry-population.txt", evidence);
        TestContext.Progress.WriteLine(
            "registry population: " + observed.Count.ToString(CultureInfo.InvariantCulture)
            + " file(s), " + observedEntries.ToString(CultureInfo.InvariantCulture)
            + " entry/entries; census at " + artifact);

        Expect.Multiple(() =>
        {
            // The census against itself first. These two hold no matter what is on disk, and
            // they are what stops a deletion from being accommodated by editing one literal:
            // dropping a census row without lowering the file count fails here, and lowering
            // a census row without lowering the entry count fails here.
            Assert.That(
                ExpectedRegistryCensus.Length,
                Is.EqualTo(ExpectedRegistryFileCount),
                "the census lists a different number of registries than the declared file count");
            Assert.That(
                censusEntries,
                Is.EqualTo(ExpectedRegistryEntryCount),
                "the census's per-file counts do not sum to the declared entry count");

            // Then the disk against the census.
            Assert.That(unreadable, Is.Empty, "a registry in the census could not be read");
            Assert.That(
                observed.Keys,
                Is.EqualTo(expected.Keys).AsCollection,
                () => "the set of tests/verification/*.json registries is not the census's. Census:\n"
                    + string.Join("\n", evidence));
            Assert.That(
                observed.Count,
                Is.EqualTo(ExpectedRegistryFileCount),
                () => "tests/verification/ holds a different number of registries than the census. "
                    + "Zero means the directory is absent or empty, which the validator otherwise "
                    + "reports as nothing at all. Census:\n" + string.Join("\n", evidence));
            Assert.That(
                observed,
                Is.EqualTo(expected),
                () => "a registry holds a different number of entries than the census. Census:\n"
                    + string.Join("\n", evidence));
            Assert.That(
                observedEntries,
                Is.EqualTo(ExpectedRegistryEntryCount),
                () => "the total number of verification entries is not the census total. Census:\n"
                    + string.Join("\n", evidence));
        });
    }

    /// <summary>
    /// One row per <c>tests/verification/*.json</c> registry and the number of entries it
    /// holds, as literals rather than as a count taken from the directory.
    /// </summary>
    /// <remarks>
    /// Measured on the tree that landed this test. Adding an entry, retiring one, or
    /// registering a new work package means editing the matching row here and the two scalars
    /// below in the same change; that is the intended cost, and it is what makes a deletion
    /// that nobody meant to make fail.
    /// </remarks>
    private static readonly ImmutableArray<(string Path, int Entries)> ExpectedRegistryCensus =
        ImmutableArray.Create(
            ("tests/verification/FND-001.json", 13),
            ("tests/verification/FND-002.json", 18),
            ("tests/verification/FND-003.json", 12),
            ("tests/verification/FND-004.json", 8),
            // FND-005's registry arrived with the base-branch merge. The census is a
            // literal precisely so a registry appearing or disappearing is a mismatch
            // rather than a smaller agreement with itself, and it fired on this one: the
            // three-place edit below (this row, the file count, the entry count) is the
            // edit tax working as designed, not an inconvenience.
            ("tests/verification/FND-005.json", 12),
            ("tests/verification/FND-007.json", 11),
            ("tests/verification/FND-008.json", 7),
            ("tests/verification/FND-009.json", 14));

    /// <summary>
    /// How many registries <c>tests/verification/</c> holds. Declared independently of
    /// <see cref="ExpectedRegistryCensus"/> so deleting a registry and its census row
    /// together is still a failure.
    /// </summary>
    private const int ExpectedRegistryFileCount = 8;

    /// <summary>
    /// How many verification entries the registries hold in total. Declared independently of
    /// <see cref="ExpectedRegistryCensus"/> so deleting an entry and decrementing its census
    /// row together is still a failure.
    /// </summary>
    private const int ExpectedRegistryEntryCount = 95;

    private static string Cell(SortedDictionary<string, int> counts, string path)
    {
        if (!counts.TryGetValue(path, out int value))
        {
            return "absent";
        }

        return value < 0 ? "unreadable" : value.ToString(CultureInfo.InvariantCulture);
    }

    private static List<string> Union(IEnumerable<string> left, IEnumerable<string> right)
    {
        SortedSet<string> all = new(StringComparer.Ordinal);
        foreach (string value in left)
        {
            all.Add(value);
        }

        foreach (string value in right)
        {
            all.Add(value);
        }

        return new List<string>(all);
    }

    /// <summary>
    /// Records the complete inventory of specification-content defects with
    /// <c>file:line</c>, and holds the count at the number measured when this validator
    /// landed so the inventory cannot grow unnoticed.
    /// </summary>
    /// <remarks>
    /// Doc 114 § Failure and retry policy forbids masking a failure. This is the opposite
    /// of masking: every defect is written out in full, the artifact is retained, and the
    /// count is a ratchet. Repairing a defect requires lowering the number in the same
    /// change, which is a deliberate edit rather than a silent drift.
    /// </remarks>
    [Test]
    public void TheSpecificationDefectInventoryIsRecorded()
    {
        ImmutableArray<RegistryFinding> findings = ValidateRepository();
        string report = Render(findings, RegistrySeverity.SpecificationDefect);
        int count = RegistryValidator.Count(findings, RegistrySeverity.SpecificationDefect);

        string artifact = WriteEvidence(
            "registry-specification-defects.txt",
            new[]
            {
                "# Specification-content defects found by the FND-009 registry validator",
                "# (VER-FND-009-007, VER-FND-009-009). Canonical ordered reviewable text.",
                "# severity\trule\tfile:line\tsubject\tdetail",
                report.Length == 0 ? "# none" : report,
                "# total: " + count.ToString(CultureInfo.InvariantCulture),
            });

        TestContext.Progress.WriteLine(
            count.ToString(CultureInfo.InvariantCulture)
            + " specification-content defect(s); inventory at " + artifact);

        Assert.That(
            count,
            Is.EqualTo(ExpectedSpecificationDefects),
            () => "the specification-defect inventory changed. Full inventory:\n" + report);
    }

    /// <summary>
    /// The count of specification-content defects measured when this validator landed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four, each named here rather than tolerated anonymously. All four are forward
    /// references to identifiers whose owning registry has not merged yet, and none is a
    /// broken link or a typo:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>docs/technical/conventions.md</c> § Stable identifiers illustrates the
    ///     verification-ID grammar with "for example <c>VER-SIM-005-001</c>", and no
    ///     <c>SIM-005</c> registry exists yet.
    ///   </description></item>
    ///   <item><description>
    ///     <c>docs/technical/delivery-waves.md</c> records the integration owner's proof
    ///     gate for the tick catch-up limit as <c>VER-SIM-001-013</c> and its unit-test pin
    ///     as <c>VER-SIM-001-006</c>. Doc 91 requires a verification entry to exist before
    ///     its implementation, so those two are exactly the entries the simulation stream is
    ///     registering on its own branch; naming them is the point of recording the ruling.
    ///   </description></item>
    ///   <item><description>
    ///     <c>docs/technical/91-verification-strategy.md</c> cites <c>VER-SIM-006-003</c> as
    ///     a worked example of a <c>retired</c> entry naming a <c>VER-*</c> successor. It
    ///     arrived with the base-branch merge, and the sentence citing it says in its own
    ///     text that the entry exists at a sibling ref and not at this one, which is the
    ///     historical-figure form that same rule permits. It resolves when
    ///     <c>tests/verification/SIM-006.json</c> merges. The count went 3 to 4 here, which
    ///     is the ratchet working: the merge added a citation and this test named its
    ///     <c>file:line</c> rather than absorbing it.
    ///   </description></item>
    /// </list>
    /// <para>
    /// None is repaired here. Rewriting an authoritative document's illustrative example, or
    /// removing a decided proof-gate ID, so a validator turns green is editing the
    /// specification to fit the tool. Each finding is reported with its <c>file:line</c> in
    /// the retained inventory and in this task's handoff, and each resolves by itself when
    /// <c>tests/verification/SIM-001.json</c>, <c>SIM-005.json</c> and
    /// <c>SIM-006.json</c> merge.
    /// </para>
    /// <para>
    /// The constant is a ratchet in both directions. A new citation to a nonexistent
    /// identifier or anchor fails this test with its own <c>file:line</c>, and repairing one
    /// requires lowering the number in the same change, which is a deliberate edit rather
    /// than silent drift. A broken anchor this task introduced in doc 40 was caught by
    /// exactly that mechanism and fixed rather than absorbed.
    /// </para>
    /// </remarks>
    private const int ExpectedSpecificationDefects = 4;

    /// <summary>Every relative document link and anchor resolves.</summary>
    [Test]
    public void TheRepositoryHasNoBrokenDocumentLinks()
    {
        ImmutableArray<RegistryFinding> findings = ValidateRepository();
        List<string> broken = new();
        foreach (RegistryFinding finding in findings)
        {
            if (finding.Rule is RegistryRule.BrokenLink or RegistryRule.BrokenAnchor)
            {
                broken.Add(finding.ToLine());
            }
        }

        Assert.That(broken, Is.Empty);
    }

    /// <summary>
    /// Each of the failure classes fails under its own rule, one fixture per class.
    /// </summary>
    /// <remarks>
    /// The class-to-rule pairing is read from <c>build/audit-expectations.env</c> rather
    /// than written here, because <c>build/verify-registry.sh</c> stage 2 has to check the
    /// headings this test writes and a second hardcoded list in the script is the
    /// two-literal defect that let it accept
    /// <c>## malformed (expects Whatever)</c>. One owner, two readers.
    /// </remarks>
    [Test]
    public void EachFixtureClassFailsUnderItsOwnRule()
    {
        ImmutableArray<(string Fixture, RegistryRule Rule)> classes =
            AuditExpectations.RegistryFixtureClasses;

        List<string> evidence = new()
        {
            "# One fixture per registry failure class (VER-FND-009-008).",
            "# Each fixture is a deliberately invalid mini-specification under",
            "# build/policy-fixtures/registry/, outside the solution.",
        };
        List<string> unproved = new();

        foreach ((string fixture, RegistryRule rule) in classes)
        {
            ImmutableArray<RegistryFinding> findings = ValidateFixture(fixture);
            evidence.Add(string.Empty);
            evidence.Add("## " + fixture + " (expects " + rule + ")");
            evidence.Add(RegistryValidator.Render(findings));

            if (!Contains(findings, rule))
            {
                unproved.Add(fixture + " produced no " + rule + " finding: "
                    + RegistryValidator.Render(findings));
            }
        }

        string artifact = WriteEvidence("registry-fixture-classes.txt", evidence);
        TestContext.Progress.WriteLine("fixture-class evidence at " + artifact);

        Assert.That(unproved, Is.Empty);
    }

    /// <summary>The dangling fixture also proves a dangling anchor, not only a missing file.</summary>
    [Test]
    public void TheDanglingFixtureProvesABrokenAnchorAsWell()
    {
        ImmutableArray<RegistryFinding> findings = ValidateFixture("dangling");
        Assert.That(
            Contains(findings, RegistryRule.BrokenAnchor),
            Is.True,
            RegistryValidator.Render(findings));
    }

    /// <summary>
    /// The malformed fixture also proves the registry-shape rules: an unaccepted tier, a
    /// missing required field, and an escaped section sign.
    /// </summary>
    [Test]
    public void TheMalformedFixtureProvesTheRegistryShapeRules()
    {
        ImmutableArray<RegistryFinding> findings = ValidateFixture("malformed");

        Expect.Multiple(() =>
        {
            Assert.That(
                Contains(findings, RegistryRule.InvalidVerificationValue),
                Is.True,
                RegistryValidator.Render(findings));
            Assert.That(
                Contains(findings, RegistryRule.IncompleteVerificationEntry),
                Is.True,
                RegistryValidator.Render(findings));
            Assert.That(
                Contains(findings, RegistryRule.NonCanonicalEncoding),
                Is.True,
                RegistryValidator.Render(findings));
        });
    }

    /// <summary>
    /// A compliant source set produces no findings, so the fixture controls above measure
    /// the injected defect rather than a validator that rejects everything.
    /// </summary>
    [Test]
    public void TheCompliantFixtureProducesNoFindings()
    {
        Assert.That(RegistryValidator.Render(ValidateFixture("compliant")), Is.Empty);
    }

    /// <summary>
    /// The composed registry these controls start from is itself clean, so each control
    /// below is measuring its own injected violation.
    /// </summary>
    /// <remarks>
    /// Without this, seven controls that all pass by producing a finding would also pass
    /// against a validator that had broken into rejecting every registry. The composed
    /// document is not the on-disk fixture: it carries two entries, because a
    /// single-entry registry cannot exhibit an ordinal gap or a non-ascending pair, and
    /// a numbering rule controlled only against a document that structurally cannot
    /// violate it is not controlled.
    /// </remarks>
    [Test]
    public void TheComposedTwoEntryRegistryProducesNoFindings()
    {
        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(
            ComposedRegistry(
                FixtureEntry("VER-FIX-001-001"),
                FixtureEntry("VER-FIX-001-002")));

        Assert.That(RegistryValidator.Render(findings), Is.Empty);
    }

    /// <summary>
    /// A registry file that is not parseable JSON is its own failure, not a registry
    /// that happens to declare nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The violation is a trailing comma after the last entry, which is what a real hand
    /// edit leaves behind when an entry is deleted: JSON forbids it, every editor accepts
    /// it, and the file still looks right. A file truncated mid-token would also fail,
    /// but for reasons a reader could dismiss as "obviously broken"; this one is the
    /// state an ordinary mistake produces.
    /// </para>
    /// <para>
    /// This rule had no control. Deleting its findings from <c>Validate</c> left the
    /// suite 23/23 green, which means nothing distinguished a validator that reports an
    /// unreadable registry from one that silently substitutes an empty document — and an
    /// empty document is exactly what <c>ReadRegistries</c> substitutes to keep going.
    /// </para>
    /// </remarks>
    [Test]
    public void AnUnparseableRegistryFileIsRejected()
    {
        string valid = ComposedRegistry(FixtureEntry("VER-FIX-001-001"));
        string withTrailingComma = valid.Replace("    }\n  ]", "    },\n  ]", StringComparison.Ordinal);
        Assert.That(
            withTrailingComma,
            Is.Not.EqualTo(valid),
            "the trailing comma was not injected, so this control changes nothing");

        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(withTrailingComma);

        Assert.That(
            Contains(findings, RegistryRule.UnreadableRegistry),
            Is.True,
            RegistryValidator.Render(findings));
    }

    /// <summary>
    /// Each of the three ways a registry's declared identity can disagree with the file
    /// it is in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All three are the state a real edit leaves. A wrong <c>schema</c> is what copying
    /// a sibling registry produces; a wrong <c>workPackage</c> is what copying the file
    /// and renaming it produces; an entry ID from another package is what moving an entry
    /// between registries produces. Each is coherent JSON of the right shape, so what
    /// fails is the identity rule and not the parser.
    /// </para>
    /// <para>
    /// <c>RegistryIdentityMismatch</c> had no control at all: deleting its findings left
    /// the suite 23/23 green. Three controls rather than one because the rule has three
    /// independent call sites and a single control would leave two of them unproved.
    /// </para>
    /// </remarks>
    [TestCase("wrong schema", "\"schema\": \"SCH-QUA-001\"", "\"schema\": \"SCH-QUA-002\"")]
    [TestCase("wrong workPackage", "\"workPackage\": \"FIX-001\"", "\"workPackage\": \"FIX-002\"")]
    [TestCase("entry ID from another package", "\"id\": \"VER-FIX-001-001\"", "\"id\": \"VER-FIX-002-001\"")]
    public void ARegistryWhoseDeclaredIdentityDisagreesWithItsFileIsRejected(
        string description,
        string original,
        string replacement)
    {
        string json = ComposedRegistry(FixtureEntry("VER-FIX-001-001"));
        Assert.That(
            json.Contains(original, StringComparison.Ordinal),
            Is.True,
            "the composed registry no longer contains '" + original + "', so '" + description
            + "' would substitute nothing");

        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(
            json.Replace(original, replacement, StringComparison.Ordinal));

        Assert.That(
            Contains(findings, RegistryRule.RegistryIdentityMismatch),
            Is.True,
            description + ":\n" + RegistryValidator.Render(findings));
    }

    /// <summary>
    /// Both ways entry ordinals can stop being the sequence doc 91 requires: a pair that
    /// does not ascend, and a gap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Doc 91 says entries "are never renumbered". The two violations are the two edits
    /// that break that: reordering two entries so their ordinals descend, and deleting a
    /// middle entry outright instead of leaving a retired tombstone. Both are coherent
    /// registries — correct schema, correct package, resolvable selectors, complete
    /// entries — so the only thing wrong with each is its numbering.
    /// </para>
    /// <para>
    /// <c>RegistryNumbering</c> had no control. Deleting its findings left the suite
    /// 23/23 green, while the PR body cited it as a rule that "was always real" and
    /// pasted output showing it firing that no assertion required to exist. Two controls
    /// because <c>ValidateNumbering</c> has two independent loops and returns after the
    /// first finding, so a control for one branch leaves the other unproved.
    /// </para>
    /// </remarks>
    [TestCase("descending pair", "VER-FIX-001-002", "VER-FIX-001-001")]
    [TestCase("gap at 2", "VER-FIX-001-001", "VER-FIX-001-003")]
    public void EntryOrdinalsThatAreNotTheDeclaredSequenceAreRejected(
        string description,
        string firstId,
        string secondId)
    {
        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(
            ComposedRegistry(FixtureEntry(firstId), FixtureEntry(secondId)));

        Assert.That(
            Contains(findings, RegistryRule.RegistryNumbering),
            Is.True,
            description + ":\n" + RegistryValidator.Render(findings));
    }

    /// <summary>
    /// A task whose only verification evidence is that the code compiles is rejected.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Doc 91: "An agent may not declare completion based solely on compilation." The
    /// violation is a complete, well-formed, resolvable entry whose
    /// <c>evidenceKinds</c> is exactly <c>["compilation"]</c> — which is the shape a real
    /// entry takes when the work was done and the tests were not written. Nothing else
    /// about the registry is wrong, so a validator that had stopped enforcing this would
    /// report nothing.
    /// </para>
    /// <para>
    /// This rule had no control: deleting its findings left the suite 23/23 green.
    /// </para>
    /// </remarks>
    [Test]
    public void ATaskWhoseOnlyEvidenceIsCompilationIsRejected()
    {
        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(
            ComposedRegistry(FixtureEntry("VER-FIX-001-001", evidenceKinds: "\"compilation\"")));

        Assert.That(
            Contains(findings, RegistryRule.TaskWithoutVerification),
            Is.True,
            RegistryValidator.Render(findings));
    }

    /// <summary>
    /// Each of the three values <c>status</c> may take is accepted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The vocabulary is closed and the validator does enforce it — an injected
    /// <c>"regsitered"</c> in a real registry file turns the suite red — but nothing kept
    /// it enforced: deleting the <c>Statuses</c> comparison from <c>ValidateEntry</c> left
    /// the suite 47/47 green, because the only assertion that could see it is the real-tree
    /// one, and the real tree is correct. So the enum needs both halves of a control: every
    /// legal value accepted, and a near-miss rejected.
    /// </para>
    /// <para>
    /// The "accepted" half is not decoration. An enum control that only proves a bad value
    /// is rejected would also pass against a comparison that had narrowed to accept one
    /// value, or none, and every registry in the repository carries the same status today
    /// (<c>implemented</c>, all 83 entries), so <c>registered</c> and <c>retired</c> have
    /// no real-tree witness at all.
    /// </para>
    /// </remarks>
    [TestCase("registered")]
    [TestCase("implemented")]
    public void EveryLegalEntryStatusIsAccepted(string status)
    {
        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(
            ComposedRegistry(FixtureEntry("VER-FIX-001-001", status: status)));

        Assert.That(RegistryValidator.Render(findings), Is.Empty);
    }

    /// <summary>
    /// The third legal status, <c>retired</c>, is accepted when it carries the successor
    /// doc 91 requires.
    /// </summary>
    /// <remarks>
    /// Separate from the two above because <c>retired</c> is the one status with a
    /// companion obligation, so "accepted" for it means accepted *with* a successor.
    /// </remarks>
    [Test]
    public void ARetiredEntryWithASuccessorIsAccepted()
    {
        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(
            ComposedRegistry(
                FixtureEntry("VER-FIX-001-001", status: "retired", successor: "VER-FIX-001-002"),
                FixtureEntry("VER-FIX-001-002")));

        Assert.That(RegistryValidator.Render(findings), Is.Empty);
    }

    /// <summary>
    /// A <c>status</c> outside the closed vocabulary is rejected, including a near-miss
    /// misspelling and a word from a different vocabulary in the same specification.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>regsitered</c> is the case the field is most exposed to: a transposition that
    /// reads correctly at a glance and, without this rule, would make an entry's status
    /// mean nothing while looking like it meant <c>registered</c>.
    /// </para>
    /// <para>
    /// <c>Done</c> and <c>Active</c> come from doc 114 § Work states and integration,
    /// which defines Draft / Ready / Active / Evidence review / Done / Blocked. That is a
    /// vocabulary for the state of a <em>task</em>, not for the state of a verification
    /// entry, and it shares no word with this field's three. They are included as controls
    /// precisely because they are the plausible wrong answer: a reader who knows one
    /// vocabulary and not the other would write them.
    /// </para>
    /// </remarks>
    [TestCase("regsitered", TestName = "a transposed registered")]
    [TestCase("Registered", TestName = "the right word, wrong case")]
    [TestCase("planned", TestName = "a word from no vocabulary in the specification")]
    [TestCase("Done", TestName = "doc 114's task vocabulary, which is a different axis")]
    [TestCase("Active", TestName = "doc 114's task vocabulary, which is a different axis")]
    public void AnEntryStatusOutsideTheClosedVocabularyIsRejected(string status)
    {
        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(
            ComposedRegistry(FixtureEntry("VER-FIX-001-001", status: status)));

        Assert.That(
            Contains(findings, RegistryRule.InvalidVerificationValue),
            Is.True,
            "status '" + status + "' was accepted:\n" + RegistryValidator.Render(findings));
    }

    /// <summary>
    /// A <c>retired</c> entry with no successor is rejected.
    /// </summary>
    /// <remarks>
    /// Doc 91: "retired verification retains a tombstone and successor." This rule was in
    /// the same state as the status enum — live and uncontrolled: deleting it left the
    /// suite 47/47 green, because no registry in the repository carries a retired entry.
    /// The violation is a coherent one: a complete, well-formed entry whose only defect is
    /// that it was retired without naming what replaced it, which is the state retiring an
    /// entry by editing one field leaves behind.
    /// </remarks>
    [Test]
    public void ARetiredEntryWithoutASuccessorIsRejected()
    {
        ImmutableArray<RegistryFinding> findings = ValidateComposedRegistry(
            ComposedRegistry(FixtureEntry("VER-FIX-001-001", status: "retired")));

        Assert.That(
            Contains(findings, RegistryRule.IncompleteVerificationEntry),
            Is.True,
            RegistryValidator.Render(findings));
    }

    /// <summary>
    /// Every <c>nunit</c> selector in every <c>tests/verification/*.json</c> names a test the
    /// NUnit harness actually discovers, and the harness discovered a nonzero number of them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An entry whose selector names no discovered test claims coverage nothing runs. Two
    /// entries did exactly that - <c>VER-FND-009-010</c> and <c>VER-FND-009-011</c> cited a
    /// <c>VerificationRegistryTests</c> fixture that was never written - and the previous
    /// check could not see it, because it compared only the selector's namespace prefix
    /// against the list of test projects and accepted any class or method name after it.
    /// </para>
    /// <para>
    /// The discovered-count assertion is not decoration. Zero discovered tests is the state a
    /// build failure, a moved project, or a changed <c>--list-tests</c> output format all
    /// produce, and every selector "resolves" against an empty list in the sense that no
    /// selector is contradicted by it. Asserting the count is what stops that from reading as
    /// a pass;
    /// <see cref="TheEmptyInventoryGuardCountsDiscoveredTestsRatherThanAssertingAConstant"/>
    /// proves the assertion is a count and not a constant that happens to be true.
    /// </para>
    /// </remarks>
    [Test]
    public void EveryNUnitSelectorNamesATestTheHarnessActuallyDiscovers()
    {
        TestInventory tests = RepositorySources.Value.Tests;
        List<string> unresolved = new();
        foreach (RegistryFinding finding in ValidateRepository())
        {
            if (finding.Rule is RegistryRule.UnresolvedTestSelector or RegistryRule.EmptyTestInventory)
            {
                unresolved.Add(finding.ToLine());
            }
        }

        List<string> evidence = new()
        {
            "# nunit selector resolution for every tests/verification/*.json (VER-FND-009-014).",
            "# Selectors are resolved against the tests the NUnit harness itself reports, not",
            "# against a list maintained by hand.",
            string.Empty,
            "## discovery",
            tests.DiscoveryReport,
            string.Empty,
            "## unresolved selectors",
            unresolved.Count == 0 ? "# none" : string.Join("\n", unresolved),
        };
        string artifact = WriteEvidence("registry-test-selectors.txt", evidence);
        TestContext.Progress.WriteLine(
            "resolved nunit selectors against "
            + tests.Count.ToString(CultureInfo.InvariantCulture)
            + " discovered test(s); evidence at " + artifact);

        Expect.Multiple(() =>
        {
            Assert.That(
                tests.Count,
                Is.GreaterThan(0),
                () => "test discovery found nothing, so no selector could be contradicted:\n"
                    + tests.DiscoveryReport);
            Assert.That(unresolved, Is.Empty);
        });
    }

    /// <summary>
    /// A selector naming a test class that was never written is rejected, even though its
    /// namespace is a real test project.
    /// </summary>
    [Test]
    public void ASelectorNamingATestClassThatDoesNotExistIsRejected()
    {
        ImmutableArray<RegistryFinding> findings = ValidateCompliantFixtureWith(
            "MechaMiner.Tools.Tests.Audit.ThisFixtureWasNeverWrittenTests",
            TestInventory.Of(ARealDiscoveredTest));

        Assert.That(
            Contains(findings, RegistryRule.UnresolvedTestSelector),
            Is.True,
            RegistryValidator.Render(findings));
    }

    /// <summary>
    /// A selector naming a real test class but a method that does not exist is rejected.
    /// </summary>
    /// <remarks>
    /// The exact shape of the defect this check was added for. A prefix comparison generous
    /// enough to accept the class would accept this too, so it gets its own control rather
    /// than being assumed to follow from the class case.
    /// </remarks>
    [Test]
    public void ASelectorNamingATestMethodThatDoesNotExistIsRejected()
    {
        ImmutableArray<RegistryFinding> findings = ValidateCompliantFixtureWith(
            CompliantFixtureSelector + ".ThisMethodWasNeverWritten",
            TestInventory.Of(ARealDiscoveredTest));

        Assert.That(
            Contains(findings, RegistryRule.UnresolvedTestSelector),
            Is.True,
            RegistryValidator.Render(findings));
    }

    /// <summary>
    /// Discovery returning nothing fails, rather than passing with nothing to compare against.
    /// </summary>
    /// <remarks>
    /// The selector here is correct. The only defect is that the inventory is empty, which is
    /// what a build failure or a changed CLI output format looks like from inside the
    /// validator. If that were treated as "no selector was contradicted", the whole check
    /// would silently stop being a check.
    /// </remarks>
    [Test]
    public void AnEmptyTestInventoryFailsRatherThanPassingWithNothingToCompare()
    {
        ImmutableArray<RegistryFinding> findings = ValidateCompliantFixtureWith(
            CompliantFixtureSelector,
            TestInventory.Nothing("a control: discovery was never run"));

        Assert.That(
            Contains(findings, RegistryRule.EmptyTestInventory),
            Is.True,
            RegistryValidator.Render(findings));
    }

    /// <summary>
    /// The empty-inventory guard reads the discovered-test count, not a constant.
    /// </summary>
    /// <remarks>
    /// Three inventories against the same correct selector. Empty fails as an empty
    /// inventory. One unrelated test does not - it fails as an unresolved selector instead,
    /// which is only possible if the guard distinguishes "nothing was discovered" from
    /// "something was discovered and it did not match". One matching test fails neither. An
    /// assertion hard-coded to true, or one that fired on any resolution failure, would break
    /// the middle case.
    /// </remarks>
    [Test]
    public void TheEmptyInventoryGuardCountsDiscoveredTestsRatherThanAssertingAConstant()
    {
        ImmutableArray<RegistryFinding> empty = ValidateCompliantFixtureWith(
            CompliantFixtureSelector,
            TestInventory.Nothing("a control: discovery was never run"));
        ImmutableArray<RegistryFinding> oneUnrelated = ValidateCompliantFixtureWith(
            CompliantFixtureSelector,
            TestInventory.Of("MechaMiner.Simulation.Tests.Support.DeterministicCaseTests.AnyOtherTest"));
        ImmutableArray<RegistryFinding> oneMatching = ValidateCompliantFixtureWith(
            CompliantFixtureSelector,
            TestInventory.Of(ARealDiscoveredTest));

        Expect.Multiple(() =>
        {
            Assert.That(
                Contains(empty, RegistryRule.EmptyTestInventory),
                Is.True,
                "0 discovered tests must be reported as an empty inventory: "
                + RegistryValidator.Render(empty));
            Assert.That(
                Contains(oneUnrelated, RegistryRule.EmptyTestInventory),
                Is.False,
                "1 discovered test is not an empty inventory: " + RegistryValidator.Render(oneUnrelated));
            Assert.That(
                Contains(oneUnrelated, RegistryRule.UnresolvedTestSelector),
                Is.True,
                "1 discovered test that does not match must fail resolution instead: "
                + RegistryValidator.Render(oneUnrelated));
            Assert.That(
                RegistryValidator.Render(oneMatching),
                Is.Empty,
                "a matching inventory must produce no finding at all");
        });
    }

    /// <summary>
    /// The real source set, read once. Reading it asks the NUnit harness which tests exist,
    /// which costs a process per test project, and the answer cannot change while this
    /// process runs. Memoizing a pure function of an unchanging tree is not a cache with a
    /// staleness problem; re-reading it four times would only make the suite slower.
    /// </summary>
    private static readonly Lazy<RegistrySources> RepositorySources =
        new(() => RegistrySources.ReadFromDisk(TestArtifacts.RepositoryRoot));

    private static readonly Lazy<ImmutableArray<RegistryFinding>> RepositoryFindings =
        new(() => RegistryValidator.Validate(RepositorySources.Value));

    private static ImmutableArray<RegistryFinding> ValidateRepository()
    {
        return RepositoryFindings.Value;
    }

    private static string FixtureDirectory(string fixtureName)
    {
        string directory = Path.Combine(
            TestArtifacts.RepositoryRoot,
            FixtureRoot.Replace('/', Path.DirectorySeparatorChar),
            fixtureName);
        Assert.That(Directory.Exists(directory), Is.True, "missing fixture: " + directory);
        return directory;
    }

    private static ImmutableArray<RegistryFinding> ValidateFixture(string fixtureName)
    {
        return RegistryValidator.Validate(RegistrySources.ReadFixture(FixtureDirectory(fixtureName)));
    }

    /// <summary>
    /// The compliant fixture with one substitution applied to its <c>nunit</c> selector and
    /// an explicitly declared test inventory.
    /// </summary>
    /// <remarks>
    /// Built from the fixture that is known to produce zero findings, so any finding these
    /// selector controls report is the substitution and not background noise. The inventory
    /// is a parameter rather than the fixture's <c>discovered-tests.txt</c>, because what is
    /// under test is exactly how the validator behaves as that inventory changes.
    /// </remarks>
    private static ImmutableArray<RegistryFinding> ValidateCompliantFixtureWith(
        string selectorValue,
        TestInventory tests)
    {
        string directory = FixtureDirectory("compliant");
        RegistrySources sources = RegistrySources.Empty();
        foreach (string document in Directory.EnumerateFiles(directory, "*.md"))
        {
            sources.WithDocument(
                "docs/technical/" + Path.GetFileName(document),
                File.ReadAllText(document));
        }

        string json = File.ReadAllText(Path.Combine(directory, "FIX-001.json"))
            .Replace(CompliantFixtureSelector, selectorValue, StringComparison.Ordinal);
        Assert.That(
            json.Contains(selectorValue, StringComparison.Ordinal),
            Is.True,
            "the compliant fixture's selector is no longer " + CompliantFixtureSelector
            + ", so these controls would substitute nothing");

        return RegistryValidator.Validate(
            sources
                .WithVerificationRegistry("tests/verification/FIX-001.json", json)
                .WithTests(tests));
    }

    /// <summary>
    /// One well-formed <c>SCH-QUA-001</c> entry, with the fields a control needs to vary
    /// exposed as parameters and everything else fixed at a value the validator accepts.
    /// </summary>
    /// <remarks>
    /// Composed here rather than added to <c>build/policy-fixtures/registry/</c> as more
    /// on-disk fixture directories, because each control differs from the compliant
    /// document by one field and a directory per field would make the difference the
    /// reader has to find rather than the thing the test states.
    /// </remarks>
    private static string FixtureEntry(
        string id,
        string evidenceKinds = "\"test-counts\"",
        string status = "implemented",
        string? successor = null)
    {
        return "    {\n"
            + "      \"id\": \"" + id + "\",\n"
            + "      \"summary\": \"The fixture verification proves the fixture requirement.\",\n"
            + "      \"task\": \"TASK-FIX-001-001\",\n"
            + "      \"requirements\": [\"TR-FIX-001\"],\n"
            + "      \"technicalSources\": [\"" + FixtureTechnicalSource + "\"],\n"
            + "      \"gameplaySources\": [],\n"
            + "      \"selector\": { \"kind\": \"nunit\", \"value\": \"" + CompliantFixtureSelector + "\" },\n"
            + "      \"fixtures\": [],\n"
            + "      \"scenarios\": [],\n"
            + "      \"evidenceKinds\": [" + evidenceKinds + "],\n"
            + "      \"platforms\": [\"linux-x64\"],\n"
            + "      \"tier\": \"fast\",\n"
            + "      \"status\": \"" + status + "\""
            + (successor is null ? string.Empty : ",\n      \"successor\": \"" + successor + "\"") + "\n"
            + "    }";
    }

    /// <summary>A well-formed <c>FIX-001</c> registry document around the given entries.</summary>
    private static string ComposedRegistry(params string[] entries)
    {
        return "{\n"
            + "  \"schema\": \"SCH-QUA-001\",\n"
            + "  \"schemaVersion\": 1,\n"
            + "  \"workPackage\": \"FIX-001\",\n"
            + "  \"workPackageTitle\": \"Fixture work package\",\n"
            + "  \"notes\": [\n"
            + "    \"Composed by RegistryValidatorTests as the clean document its controls mutate.\"\n"
            + "  ],\n"
            + "  \"entries\": [\n"
            + string.Join(",\n", entries) + "\n"
            + "  ]\n"
            + "}\n";
    }

    /// <summary>
    /// Validates a composed registry document against the compliant fixture's
    /// specification prose and declared test inventory.
    /// </summary>
    /// <remarks>
    /// The prose comes from the fixture directory rather than from the real repository so
    /// that a control proving one injected defect cannot also fail because a real document
    /// was edited, which is the same reason <c>discovered-tests.txt</c> exists.
    /// </remarks>
    private static ImmutableArray<RegistryFinding> ValidateComposedRegistry(string json)
    {
        string directory = FixtureDirectory("compliant");
        RegistrySources sources = RegistrySources.Empty();
        foreach (string document in Directory.EnumerateFiles(directory, "*.md"))
        {
            sources.WithDocument(
                "docs/technical/" + Path.GetFileName(document),
                File.ReadAllText(document));
        }

        return RegistryValidator.Validate(
            sources
                .WithVerificationRegistry("tests/verification/FIX-001.json", json)
                .WithTests(TestInventory.Of(ARealDiscoveredTest)));
    }

    /// <summary>The one technical source the fixture's requirement index actually defines.</summary>
    private const string FixtureTechnicalSource =
        "docs/technical/112-normative-requirement-index.md#fixture-requirements";

    /// <summary>The selector the compliant fixture carries, and the anchor these controls substitute.</summary>
    private const string CompliantFixtureSelector = "MechaMiner.Tools.Tests.Audit.RegistryValidatorTests";

    /// <summary>One real discovered test name, enough for a nonempty inventory.</summary>
    private const string ARealDiscoveredTest =
        "MechaMiner.Tools.Tests.Audit.RegistryValidatorTests.TheCompliantFixtureProducesNoFindings";

    private static bool Contains(ImmutableArray<RegistryFinding> findings, RegistryRule rule)
    {
        foreach (RegistryFinding finding in findings)
        {
            if (finding.Rule == rule)
            {
                return true;
            }
        }

        return false;
    }

    private static string Render(ImmutableArray<RegistryFinding> findings, RegistrySeverity severity)
    {
        List<string> lines = new();
        foreach (RegistryFinding finding in findings)
        {
            if (finding.Severity == severity)
            {
                lines.Add(finding.ToLine());
            }
        }

        return string.Join("\n", lines);
    }

    private static string WriteEvidence(string fileName, IReadOnlyList<string> lines)
    {
        string directory = Path.Combine(TestArtifacts.RepositoryRoot, "artifacts", "registry");
        Directory.CreateDirectory(directory);
        string absolute = Path.Combine(directory, fileName);
        File.WriteAllText(absolute, string.Join("\n", lines) + "\n");
        return TestArtifacts.Relative(absolute);
    }
}
