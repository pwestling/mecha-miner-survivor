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
    /// <para>
    /// EVERY FIGURE BELOW IS A COUNT OF ENTRIES IN ONE NAMED FILE, MEASURED ON THE MERGE OF
    /// <c>18b847b9</c> WITH <c>aecd36aa</c>. Twenty-five rows summing to 344 entries.
    /// Adding an entry, retiring one, or registering a new work package means editing the
    /// matching row here and the two scalars below in the same change; that is the intended
    /// cost, and it is what makes a deletion that nobody meant to make fail.
    /// </para>
    /// <para>
    /// SUPERSEDED, RECORDED RATHER THAN OVERWRITTEN: this census read 8 rows summing to 95
    /// entries, measured at <c>18b847b9</c>, where <c>tests/verification/</c> held eight
    /// registries. Three things moved it, and they are separable:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     The merge with <c>aecd36aa</c> brought SEVENTEEN further registries - the
    ///     <c>DAT-*</c>, <c>GEO-*</c>, <c>PLY-*</c>, <c>PRE-*</c>, <c>SIM-*</c> and
    ///     <c>UI-002</c> rows below. Eight plus seventeen is twenty-five because exactly
    ///     four paths are common to both sides (<c>FND-001</c>, <c>FND-002</c>,
    ///     <c>FND-003</c>, <c>FND-005</c>).
    ///   </description></item>
    ///   <item><description>
    ///     <c>FND-005</c> went from 12 entries to 13. That registry exists on both sides and
    ///     the base side carries the thirteenth entry, so this is a changed count on a row
    ///     the census already held rather than a new row. It is called out because a row that
    ///     merely moves is the one a re-pin absorbs without noticing.
    ///   </description></item>
    ///   <item><description>
    ///     Four registries were UNREADABLE at the moment of the merge and are counted here
    ///     for the first time: <c>PRE-001</c> (7), <c>SIM-003</c> (12), <c>SIM-007</c> (12)
    ///     and <c>UI-002</c> (7), 38 entries between them. They carry a <c>deferredTo</c>
    ///     field that <see cref="VerificationEntry"/> had no member for, and under
    ///     <c>UnmappedMemberHandling.Disallow</c> an unmodelled key is a hard
    ///     deserialization error, so the whole file failed rather than one field going
    ///     unread. Modelling that field is what made them countable.
    ///   </description></item>
    /// </list>
    /// <para>
    /// The intermediate figure is recorded too, because it is the one a reader reproduces if
    /// they check out the merge without the <c>deferredTo</c> fix: 25 files and an entry
    /// total of 302, in which each of those four files contributed the -1 that
    /// <see cref="Cell"/> renders as <c>unreadable</c>. 302 is therefore not 344 minus 38;
    /// it is 344 minus those 38 real entries, minus a further four for the sentinels.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// LINEAGE, BECAUSE THIS TABLE IS DEMONSTRABLY PORTABLE TO THE WRONG PLACE. These 25 rows
    /// are true of the merge of <c>18b847b9</c> with <c>aecd36aa</c> and false of either parent
    /// and of sibling branches: <c>claude/hearth-thread-3aamx2</c> reads different counts for
    /// <c>DAT-001</c>, <c>DAT-002</c> and <c>DAT-003</c> and carries a <c>DAT-006.json</c> this
    /// tree has no row for at all. A table without its lineage is the same defect as a figure
    /// without its corpus, and copying this one to a branch where it is wrong would produce a
    /// confident green over the wrong population.
    /// </remarks>
    private static readonly ImmutableArray<(string Path, int Entries)> ExpectedRegistryCensus =
        ImmutableArray.Create(
            ("tests/verification/DAT-001.json", 49),
            ("tests/verification/DAT-002.json", 37),
            ("tests/verification/DAT-003.json", 40),
            ("tests/verification/DAT-007.json", 1),
            ("tests/verification/FND-001.json", 13),
            ("tests/verification/FND-002.json", 18),
            ("tests/verification/FND-003.json", 12),
            ("tests/verification/FND-004.json", 8),
            // FND-005's registry arrived with the base-branch merge. The census is a
            // literal precisely so a registry appearing or disappearing is a mismatch
            // rather than a smaller agreement with itself, and it fired on this one: the
            // three-place edit below (this row, the file count, the entry count) is the
            // edit tax working as designed, not an inconvenience. It fired a second time on
            // the merge that widened this census, when the count went 12 to 13.
            // 13, and this row is a RESOLUTION rather than a count: FND-005 is the only
            // registry both merge parents touched. Verified by blob identity, not arithmetic -
            // the merge base (1a771d3) and 18b847b9 share the older 12-entry blob, aecd36aa
            // carries 13, and 2311269's blob is byte-identical to aecd36aa's, so three-way
            // resolution took that side.
            ("tests/verification/FND-005.json", 13),
            ("tests/verification/FND-007.json", 11),
            ("tests/verification/FND-008.json", 7),
            ("tests/verification/FND-009.json", 14),
            ("tests/verification/GEO-001.json", 4),
            ("tests/verification/GEO-003.json", 3),
            ("tests/verification/PLY-001.json", 10),
            // PRE-001, SIM-003, SIM-007 and UI-002 are the four that were unreadable until
            // deferredTo was modelled. Their counts are first measurements, not re-pins.
            ("tests/verification/PRE-001.json", 7),
            ("tests/verification/PRE-002.json", 1),
            ("tests/verification/SIM-001.json", 14),
            ("tests/verification/SIM-002.json", 10),
            ("tests/verification/SIM-003.json", 12),
            ("tests/verification/SIM-004.json", 13),
            ("tests/verification/SIM-005.json", 16),
            ("tests/verification/SIM-006.json", 12),
            ("tests/verification/SIM-007.json", 12),
            ("tests/verification/UI-002.json", 7));

    /// <summary>
    /// How many registries <c>tests/verification/</c> holds, measured on the merge of
    /// <c>18b847b9</c> with <c>aecd36aa</c>. Declared independently of
    /// <see cref="ExpectedRegistryCensus"/> so deleting a registry and its census row
    /// together is still a failure. Was 8 at <c>18b847b9</c>.
    /// </summary>
    private const int ExpectedRegistryFileCount = 25;

    /// <summary>
    /// How many verification entries the registries hold in total, measured on the merge of
    /// <c>18b847b9</c> with <c>aecd36aa</c>. Declared independently of
    /// <see cref="ExpectedRegistryCensus"/> so deleting an entry and decrementing its census
    /// row together is still a failure. Was 95 at <c>18b847b9</c>.
    /// </summary>
    private const int ExpectedRegistryEntryCount = 344;

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
        // Measured over the WHOLE repository, then each finding is attributed to the registry
        // that produced it and the ones belonging to an unaudited registry are separated out.
        //
        // Attribution rather than a restricted corpus, deliberately. Validating a source set
        // with the seventeen registries removed was tried and is wrong: docs/ cites VER-* IDs
        // that those registries define, so removing them makes five citations that DO resolve
        // on this tree report as undefined. That manufactures findings about the very files
        // being declared unaudited, and it would leave the inventory listing non-defects.
        // Attribution keeps every measurement against the real tree.
        var audited = new SortedSet<string>(AuditedDefectRegistries, StringComparer.Ordinal);
        List<string> unaudited = new();
        foreach ((string path, int _) in ExpectedRegistryCensus)
        {
            if (!audited.Contains(path))
            {
                unaudited.Add(path);
            }
        }

        var unauditedSet = new HashSet<string>(unaudited, StringComparer.Ordinal);
        List<RegistryFinding> auditedFindings = new();
        List<RegistryFinding> unauditedFindings = new();
        foreach (RegistryFinding finding in ValidateRepository())
        {
            if (finding.Severity != RegistrySeverity.SpecificationDefect)
            {
                continue;
            }

            string? owner = OwningRegistry(finding, ExpectedRegistryCensus);
            if (owner is not null && unauditedSet.Contains(owner))
            {
                unauditedFindings.Add(finding);
            }
            else
            {
                auditedFindings.Add(finding);
            }
        }

        string report = RenderLines(auditedFindings);
        int count = auditedFindings.Count;

        List<string> evidence = new()
        {
            "# Specification-content defects found by the FND-009 registry validator",
            "# (VER-FND-009-007, VER-FND-009-009). Canonical ordered reviewable text.",
            "#",
            "# SCOPE. This inventory covers docs/ plus the "
            + AuditedDefectRegistries.Length.ToString(CultureInfo.InvariantCulture)
            + " audited registry/registries listed below, out of the "
            + ExpectedRegistryCensus.Length.ToString(CultureInfo.InvariantCulture)
            + " that tests/verification/ holds. The remaining "
            + unaudited.Count.ToString(CultureInfo.InvariantCulture)
            + " are NOT covered by the count below and are UNAUDITED, which is not the same",
            "# as clean: nothing here has examined them, and a defect in one of them does not",
            "# appear in this total. Extending the audit to them is a deliberate act for their",
            "# owners, not a side effect of a merge.",
            "#",
            "# severity\trule\tfile:line\tsubject\tdetail",
            report.Length == 0 ? "# none" : report,
            // "# total: <n>" verbatim, and n must equal the number of non-comment rows above.
            // build/verify-registry.sh's check_defect_inventory requires exactly one line of
            // this shape and cross-checks it against the row count, so the scope is described
            // in the comment lines around it rather than by qualifying this line. Everything
            // below is comment-prefixed for the same reason: a bare row here would be counted
            // as part of this inventory.
            "# total: " + count.ToString(CultureInfo.InvariantCulture),
            "#",
            "# audited registries (" + audited.Count.ToString(CultureInfo.InvariantCulture) + "):",
        };
        foreach (string path in audited)
        {
            evidence.Add("#   " + path);
        }

        evidence.Add(
            "# UNAUDITED registries ("
            + unaudited.Count.ToString(CultureInfo.InvariantCulture)
            + ") - present, counted by the population census, NOT audited for defects:");
        foreach (string path in unaudited)
        {
            evidence.Add("#   " + path);
        }

        // The unaudited findings are written out in full rather than summarised. They are not
        // this count's business, but they are the reason the scope is what it is, and a reader
        // deciding whether to extend the audit needs to see them without re-running anything.
        evidence.Add("#");
        evidence.Add(
            "# Defects attributed to the UNAUDITED registries above ("
            + unauditedFindings.Count.ToString(CultureInfo.InvariantCulture)
            + "). NOT part of the total declared above and NOT asserted by this test. Listed");
        evidence.Add(
            "# so the cost of leaving them unaudited is visible rather than merely stated. Each");
        evidence.Add(
            "# is comment-prefixed so it is not counted as a row of the inventory above:");
        if (unauditedFindings.Count == 0)
        {
            evidence.Add("#   none");
        }
        else
        {
            foreach (RegistryFinding finding in unauditedFindings)
            {
                evidence.Add("#   " + finding.ToLine());
            }
        }

        string artifact = WriteEvidence("registry-specification-defects.txt", evidence);

        // Printed on a GREEN run as well as a red one. A scope this narrow has to be visible
        // in the output of a passing suite, or "8 of 25 audited" silently reads as "25 clean".
        TestContext.Progress.WriteLine(
            count.ToString(CultureInfo.InvariantCulture)
            + " specification-content defect(s) over an audited scope of docs/ plus "
            + audited.Count.ToString(CultureInfo.InvariantCulture)
            + " of " + ExpectedRegistryCensus.Length.ToString(CultureInfo.InvariantCulture)
            + " registries; " + unaudited.Count.ToString(CultureInfo.InvariantCulture)
            + " registry/registries UNAUDITED (present but never examined, which is not clean,"
            + " and carrying " + unauditedFindings.Count.ToString(CultureInfo.InvariantCulture)
            + " defect(s) this total excludes); inventory at " + artifact);

        Expect.Multiple(() =>
        {
            // The scope against itself, before the count. These stop the audited set from
            // quietly shrinking: dropping a path from AuditedDefectRegistries without
            // lowering the declared size fails here, and so does naming a registry that
            // tests/verification/ does not hold, which would otherwise reduce the corpus to
            // whatever happened to resolve.
            Assert.That(
                AuditedDefectRegistries.Length,
                Is.EqualTo(AuditedDefectRegistryCount),
                "the audited registry list is a different size than the declared count");

            // Every audited registry must be one the population census holds. A path here
            // that tests/verification/ does not carry would silently shrink the audit to
            // whatever resolved, and would also make the unaudited remainder below wrong.
            List<string> auditedNotInCensus = new();
            foreach (string path in audited)
            {
                bool present = false;
                foreach ((string censusPath, int _) in ExpectedRegistryCensus)
                {
                    if (string.Equals(censusPath, path, StringComparison.Ordinal))
                    {
                        present = true;
                        break;
                    }
                }

                if (!present)
                {
                    auditedNotInCensus.Add(path);
                }
            }

            Assert.That(
                auditedNotInCensus,
                Is.Empty,
                () => "an audited registry is not in the population census, so the audit claims "
                    + "to cover a file that tests/verification/ does not hold. Inventory:\n"
                    + string.Join("\n", evidence));

            // The partition must be total: the two buckets together must account for every
            // specification defect the validator reported. This is what stops the attribution
            // above from being a hole - a finding that fell into neither bucket would leave
            // the total silently short.
            Assert.That(
                auditedFindings.Count + unauditedFindings.Count,
                Is.EqualTo(RegistryValidator.Count(
                    ValidateRepository(),
                    RegistrySeverity.SpecificationDefect)),
                () => "the audited and unaudited buckets do not account for every "
                    + "specification defect the validator reported, so attribution dropped "
                    + "one. Inventory:\n" + string.Join("\n", evidence));

            // The unaudited remainder is asserted, not merely printed, so registries can
            // neither arrive nor be audited without this figure being edited on purpose.
            Assert.That(
                unaudited.Count,
                Is.EqualTo(UnauditedDefectRegistryCount),
                () => "the number of registries outside the defect audit changed. They are "
                    + "present and counted by the population census but NOT audited, and that "
                    + "gap must move only by a deliberate edit. Inventory:\n"
                    + string.Join("\n", evidence));

            Assert.That(
                count,
                Is.EqualTo(ExpectedSpecificationDefects),
                () => "the specification-defect inventory changed over the AUDITED scope of "
                    + "docs/ plus "
                    + audited.Count.ToString(CultureInfo.InvariantCulture)
                    + " registry/registries. This total says nothing about the "
                    + unaudited.Count.ToString(CultureInfo.InvariantCulture)
                    + " unaudited registry/registries. Full inventory:\n"
                    + string.Join("\n", evidence));
        });
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
    private const int ExpectedSpecificationDefects = 0;

    /// <summary>
    /// The registries the specification-defect count above is measured over. Everything in
    /// <see cref="ExpectedRegistryCensus"/> and not in this list is UNAUDITED.
    /// </summary>
    /// <remarks>
    /// <para>
    /// WHY THIS LIST EXISTS, AND WHY IT IS NOT THE CENSUS. The population census counts all
    /// twenty-five registries, because counting is cheap and a missing file must be a
    /// failure. The defect audit covers eight, because eight is what was examined. Those are
    /// different claims and conflating them is what this split prevents.
    /// </para>
    /// <para>
    /// These eight are the registries <c>tests/verification/</c> held at <c>18b847b9</c>,
    /// the tree this validator landed on, and they are the only ones any figure in this file
    /// has ever been measured against for defects. The merge with <c>aecd36aa</c> brought
    /// seventeen more. Those seventeen are NOT audited by this count and are not asserted to
    /// be clean; the count simply does not reach them.
    /// </para>
    /// <para>
    /// WHY THE NUMBER WAS NOT RAISED TO COVER THEM. Over all twenty-five the validator
    /// reports 54 specification defects, every one an <c>UndefinedIdentifier</c> naming a
    /// <c>TASK-*</c> or <c>VER-*</c> identifier that doc 110 does not register, and every one
    /// arising from a registry in those seventeen. Raising this constant to 54 would convert
    /// "never examined" into "examined and tolerated", which is a worse claim than either,
    /// and it would do so as a side effect of a merge rather than as anybody's decision. The
    /// ratchet keeps its full strength over the corpus it measured, and extending it is a
    /// deliberate act for the owners of those registries.
    /// </para>
    /// <para>
    /// SUPERSEDED FIGURE, AND WHY IT MOVED TO ZERO. This constant read 4 at <c>18b847b9</c>,
    /// and all four were forward references to identifiers whose owning registry had not
    /// merged: <c>VER-SIM-005-001</c> illustrating the grammar in
    /// <c>docs/technical/conventions.md</c>, <c>VER-SIM-001-013</c> and
    /// <c>VER-SIM-001-006</c> recorded as proof gates in
    /// <c>docs/technical/delivery-waves.md</c>, and <c>VER-SIM-006-003</c> cited as a worked
    /// retirement example in <c>docs/technical/91-verification-strategy.md</c>. That comment
    /// said each "resolves by itself when tests/verification/SIM-001.json, SIM-005.json and
    /// SIM-006.json merge". Those three registries arrived with <c>aecd36aa</c> and all four
    /// resolved, exactly as predicted. The figure is 0 because the defects were repaired by
    /// the merge, not because the scope narrowed around them: none of the four was in a
    /// registry, all four were in <c>docs/</c>, and <c>docs/</c> is inside the audited scope.
    /// </para>
    /// <para>
    /// The constant remains a ratchet in both directions over that scope. A new citation to a
    /// nonexistent identifier or anchor in <c>docs/</c>, or a defect in one of the eight
    /// audited registries, fails this test with its own <c>file:line</c>, and it now fails
    /// against zero rather than against a tolerance.
    /// </para>
    /// </remarks>
    private static readonly ImmutableArray<string> AuditedDefectRegistries =
        ImmutableArray.Create(
            "tests/verification/FND-001.json",
            "tests/verification/FND-002.json",
            "tests/verification/FND-003.json",
            "tests/verification/FND-004.json",
            "tests/verification/FND-005.json",
            "tests/verification/FND-007.json",
            "tests/verification/FND-008.json",
            "tests/verification/FND-009.json");

    /// <summary>
    /// How many registries the defect audit covers. Declared independently of
    /// <see cref="AuditedDefectRegistries"/> so removing a path and its coverage together is
    /// still a failure.
    /// </summary>
    private const int AuditedDefectRegistryCount = 8;

    /// <summary>
    /// How many registries are present and counted by <see cref="ExpectedRegistryCensus"/>
    /// but outside the defect audit. Asserted rather than merely printed, so a registry can
    /// neither arrive nor be brought into the audit without this figure being edited on
    /// purpose. Twenty-five minus eight.
    /// </summary>
    private const int UnauditedDefectRegistryCount = 17;

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
    /// The section-sign rule fires on the escape and stays silent on the literal, as a
    /// discriminating pair over one document that differs by exactly that spelling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// THE POINT IS THE PAIR, NOT EITHER LEG. The <c>malformed</c> fixture already showed the
    /// escape being rejected, and the <c>compliant</c> fixture already happened to contain a
    /// literal section sign while producing no findings. What nothing asserted was that the
    /// conforming document contains the character AT ALL, and an encoding rule that has only
    /// ever been shown conforming input it does not have to look at is indistinguishable from
    /// a rule that matches nothing. The count below is what makes the quiet leg load-bearing:
    /// delete the section sign from the fixture and this test fails rather than silently
    /// becoming vacuous.
    /// </para>
    /// <para>
    /// Both documents are built here from the same bytes so the only difference between the
    /// red run and the green run is the six characters of the escape. A pair assembled from
    /// two different fixture directories could differ in some other way and neither leg would
    /// notice.
    /// </para>
    /// </remarks>
    [Test]
    public void TheSectionSignRuleSeparatesTheLiteralFromTheEscape()
    {
        string literal = File.ReadAllText(
            Path.Combine(FixtureDirectory("compliant"), "FIX-001.json"));
        string escaped = literal.Replace(
            "§",
            RegistryValidator.ForbiddenSectionSignEscape,
            StringComparison.Ordinal);

        Expect.Multiple(() =>
        {
            // The conforming leg is only evidence if the character is actually there.
            Assert.That(
                Occurrences(literal, "§"),
                Is.GreaterThan(0),
                "the compliant fixture carries no literal section sign any more, so the "
                + "'a literal is accepted' leg below would hold vacuously and this rule would "
                + "be indistinguishable from one that matches nothing");

            // And the red leg is only evidence if the substitution actually happened.
            Assert.That(
                Occurrences(escaped, RegistryValidator.ForbiddenSectionSignEscape),
                Is.EqualTo(Occurrences(literal, "§")),
                "every literal section sign must have become an escape, or the red leg below "
                + "is testing a document that was never perturbed");
            Assert.That(
                Occurrences(escaped, "§"),
                Is.EqualTo(0),
                "the perturbed document still holds a literal section sign");

            Assert.That(
                Contains(ValidateComposedRegistry(literal), RegistryRule.NonCanonicalEncoding),
                Is.False,
                "a registry spelling the section sign as literal UTF-8 was reported as "
                + "non-canonical, so the rule rejects the spelling it is meant to require");
            Assert.That(
                Contains(ValidateComposedRegistry(escaped), RegistryRule.NonCanonicalEncoding),
                Is.True,
                "a registry escaping the section sign was NOT reported, so the rule matches "
                + "nothing and the seven registries it flagged were flagged by accident");
        });
    }

    /// <summary>
    /// <c>engine-scene</c> is an accepted selector kind and an unauthorised kind is still
    /// rejected, so extending the vocabulary did not open it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A VOCABULARY THAT PERMITS EVERYTHING AND ONE THAT IS CORRECTLY EXTENDED LOOK IDENTICAL
    /// AGAINST A CORPUS THAT ONLY USES LEGAL VALUES. Every selector kind in
    /// <c>tests/verification/</c> is now accepted, so nothing in the repository can tell those
    /// two apart, and adding a fifth member to the list is exactly the moment that stops being
    /// a theoretical distinction. The second leg is the one that carries the argument.
    /// </para>
    /// <para>
    /// <c>engine-scenery</c> is chosen as the unauthorised value because it contains the
    /// accepted value as a prefix. A membership test written as a substring or prefix match
    /// would accept it, and this asserts that the check is exact.
    /// </para>
    /// <para>
    /// <c>manual</c> IS EXERCISED HERE BECAUSE NOTHING ELSE EXERCISES IT. It is the one member
    /// of the vocabulary with zero users in <c>tests/verification/</c> - a fact established as
    /// a closed partition, since <c>nunit</c> 274 + <c>script</c> 51 + <c>command</c> 13 +
    /// <c>engine-scene</c> 6 = 344 is exactly the entry count - so no registry drives its
    /// accept branch and a misspelling in the list, <c>manaul</c> for <c>manual</c>, would be
    /// invisible to every other test. A reserved value with no control is a comment; with this
    /// leg it is a property.
    /// </para>
    /// </remarks>
    [Test]
    public void TheSelectorKindVocabularyAdmitsEngineSceneAndStillRejectsAnythingElse()
    {
        const string authored = "\"kind\": \"nunit\"";
        string literal = File.ReadAllText(
            Path.Combine(FixtureDirectory("compliant"), "FIX-001.json"));
        Assert.That(
            Occurrences(literal, authored),
            Is.EqualTo(1),
            "the compliant fixture's selector is no longer spelled " + authored
            + ", so every leg below would substitute nothing");

        ImmutableArray<RegistryFinding> accepted = ValidateComposedRegistry(
            literal.Replace(authored, "\"kind\": \"engine-scene\"", StringComparison.Ordinal));
        ImmutableArray<RegistryFinding> reserved = ValidateComposedRegistry(
            literal.Replace(authored, "\"kind\": \"manual\"", StringComparison.Ordinal));
        ImmutableArray<RegistryFinding> rejected = ValidateComposedRegistry(
            literal.Replace(authored, "\"kind\": \"engine-scenery\"", StringComparison.Ordinal));

        Expect.Multiple(() =>
        {
            Assert.That(
                Contains(reserved, RegistryRule.InvalidVerificationValue),
                Is.False,
                () => "manual was rejected as a selector kind. It has zero users in "
                    + "tests/verification/, so this is the only thing that drives its accept "
                    + "branch and a misspelling in the vocabulary would otherwise be "
                    + "invisible. Findings:\n" + RegistryValidator.Render(reserved));
            Assert.That(
                Contains(accepted, RegistryRule.InvalidVerificationValue),
                Is.False,
                () => "engine-scene was rejected as a selector kind, but six committed entries "
                    + "carry it and build/verify-run-slice.sh derives its section roster from "
                    + "exactly those entries. Findings:\n" + RegistryValidator.Render(accepted));
            Assert.That(
                Contains(rejected, RegistryRule.InvalidVerificationValue),
                Is.True,
                () => "engine-scenery was ACCEPTED as a selector kind. Extending the vocabulary "
                    + "opened it instead of completing it, so the accepted set no longer "
                    + "constrains anything. Findings:\n" + RegistryValidator.Render(rejected));
        });
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

    /// <summary>
    /// How many times <paramref name="needle"/> occurs in <paramref name="text"/>.
    /// </summary>
    /// <remarks>
    /// Occurrences, not matching lines. The distinction is load-bearing wherever a count of
    /// escapes is compared against a count of literals: one line of registry prose routinely
    /// carries several section signs, so a line-based count understates the corpus and would
    /// make a correct conversion look like a partial one.
    /// </remarks>
    private static int Occurrences(string text, string needle)
    {
        int count = 0;
        int at = text.IndexOf(needle, StringComparison.Ordinal);
        while (at >= 0)
        {
            count++;
            at = text.IndexOf(needle, at + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

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

    /// <summary>
    /// Renders findings that a caller has already selected, so a caller holding a filtered
    /// subset does not have to re-filter it by severity to print it.
    /// </summary>
    private static string RenderLines(IReadOnlyList<RegistryFinding> findings)
    {
        List<string> lines = new();
        foreach (RegistryFinding finding in findings)
        {
            lines.Add(finding.ToLine());
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// The <c>tests/verification/*.json</c> registry a finding arose from, or null when it
    /// arose from <c>docs/</c> or from no registry at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two cases, because this validator reports a location two different ways. Most findings
    /// carry a <c>file:line</c> naming the registry directly. Task-coverage findings carry the
    /// literal <c>tests/verification:1</c> - the directory rather than the file - because they
    /// are raised while walking entries grouped by task, after the file they came from has
    /// been lost. For those the work package in the subject is what identifies the registry:
    /// <c>TASK-DAT-002-001</c> belongs to <c>DAT-002</c>, whose registry is
    /// <c>tests/verification/DAT-002.json</c>.
    /// </para>
    /// <para>
    /// A subject-derived path is only accepted when <paramref name="census"/> holds it. An
    /// identifier naming a work package with no registry at all is not attributable to any
    /// registry, so it stays in the audited scope and is counted, which is the safe direction:
    /// an unattributable finding must never be able to fall out of the total.
    /// </para>
    /// <para>
    /// SUBJECT DERIVATION IS RESTRICTED TO THE DIRECTORY-FORM LOCATION, AND THAT RESTRICTION IS
    /// THE WHOLE CORRECTNESS ARGUMENT. An earlier revision of this method fell through to the
    /// subject for ANY location that was not a <c>tests/verification/*.json</c> path, which
    /// silently included every finding in <c>docs/</c>. Because a docs/ finding's subject is
    /// the identifier the prose cites, a broken citation in <c>docs/</c> was attributed to
    /// whichever registry owns the identifier it names, so a citation naming an UNAUDITED
    /// package fell out of the audited total altogether. That was demonstrated rather than
    /// reasoned about: rewriting <c>VER-SIM-005-001</c> to <c>VER-SIM-005-999</c> in
    /// <c>docs/technical/conventions.md</c> left this test GREEN, while the identical edit to
    /// <c>VER-FND-002-999</c> - same file, same line, same defect, an audited package - failed
    /// it. The scope is meant to say "these eight registries are audited and docs/ is audited";
    /// the fall-through made it say "and docs/ is audited except where it happens to mention a
    /// registry we did not audit", which is a hole in the ratchet rather than a scope.
    /// </para>
    /// <para>
    /// So a location that names a real file is now authoritative for that file. Only the literal
    /// directory <c>tests/verification</c>, which is not a file and carries no other information,
    /// defers to the subject.
    /// </para>
    /// </remarks>
    private static string? OwningRegistry(
        RegistryFinding finding,
        ImmutableArray<(string Path, int Entries)> census)
    {
        string location = finding.Location;
        int colon = location.LastIndexOf(':');
        string path = colon < 0 ? location : location[..colon];
        if (path.StartsWith("tests/verification/", StringComparison.Ordinal)
            && path.EndsWith(".json", StringComparison.Ordinal))
        {
            return path;
        }

        // Any other location naming a real file - every docs/ finding, in particular - is
        // attributable to that file and to nothing else. Returning null keeps it inside the
        // audited scope, where docs/ belongs. Only the directory form defers to the subject.
        if (!string.Equals(path, "tests/verification", StringComparison.Ordinal))
        {
            return null;
        }

        // TASK-<STREAM>-<NNN>-<NNN> and VER-<STREAM>-<NNN>-<NNN>: the work package is the two
        // segments before the final ordinal.
        string subject = finding.Subject;
        int lastDash = subject.LastIndexOf('-');
        if (lastDash <= 0)
        {
            return null;
        }

        int firstDash = subject.IndexOf('-', StringComparison.Ordinal);
        if (firstDash <= 0 || firstDash >= lastDash)
        {
            return null;
        }

        string workPackage = subject[(firstDash + 1)..lastDash];
        string candidate = "tests/verification/" + workPackage + ".json";
        foreach ((string censusPath, int _) in census)
        {
            if (string.Equals(censusPath, candidate, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
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
