using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
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
    /// Three, each named here rather than tolerated anonymously. All three are forward
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
    /// </list>
    /// <para>
    /// None is repaired here. Rewriting an authoritative document's illustrative example, or
    /// removing a decided proof-gate ID, so a validator turns green is editing the
    /// specification to fit the tool. Each finding is reported with its <c>file:line</c> in
    /// the retained inventory and in this task's handoff, and each resolves by itself when
    /// <c>tests/verification/SIM-001.json</c> and <c>SIM-005.json</c> merge.
    /// </para>
    /// <para>
    /// The constant is a ratchet in both directions. A new citation to a nonexistent
    /// identifier or anchor fails this test with its own <c>file:line</c>, and repairing one
    /// requires lowering the number in the same change, which is a deliberate edit rather
    /// than silent drift. A broken anchor this task introduced in doc 40 was caught by
    /// exactly that mechanism and fixed rather than absorbed.
    /// </para>
    /// </remarks>
    private const int ExpectedSpecificationDefects = 3;

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
    /// Each of the four failure classes fails under its own rule, one fixture per class.
    /// </summary>
    [Test]
    public void EachFixtureClassFailsUnderItsOwnRule()
    {
        (string Fixture, RegistryRule Rule)[] classes =
        {
            ("missing", RegistryRule.UndefinedIdentifier),
            ("duplicate", RegistryRule.DuplicateIdentifier),
            ("dangling", RegistryRule.BrokenLink),
            ("malformed", RegistryRule.MalformedIdentifier),
        };

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
