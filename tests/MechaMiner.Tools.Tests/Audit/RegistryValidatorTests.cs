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
/// <c>VER-FND-009-007</c> through <c>VER-FND-009-009</c>, <c>VER-FND-009-012</c>.
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

    private static ImmutableArray<RegistryFinding> ValidateRepository()
    {
        return RegistryValidator.Validate(RegistrySources.ReadFromDisk(TestArtifacts.RepositoryRoot));
    }

    private static ImmutableArray<RegistryFinding> ValidateFixture(string fixtureName)
    {
        string directory = Path.Combine(
            TestArtifacts.RepositoryRoot,
            FixtureRoot.Replace('/', Path.DirectorySeparatorChar),
            fixtureName);
        Assert.That(Directory.Exists(directory), Is.True, "missing fixture: " + directory);
        return RegistryValidator.Validate(RegistrySources.ReadFixture(directory));
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
