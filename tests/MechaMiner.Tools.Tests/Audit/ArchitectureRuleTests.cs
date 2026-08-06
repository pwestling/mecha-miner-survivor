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
/// The accepted project boundary, enforced as tests with one negative control per
/// forbidden edge.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-001</c>). Verification:
/// <c>VER-FND-009-001</c> through <c>VER-FND-009-006</c>. Requirements:
/// <c>TR-CTR-001</c>, <c>TR-BLD-006</c>, <c>TR-FND-001</c>, <c>TR-FND-002</c>.
/// </para>
/// <para>
/// <c>TASK-FND-009-001</c>'s completion gate is "each forbidden synthetic edge fails",
/// so <see cref="EveryForbiddenReferenceEdgeIsRejected"/> enumerates the complete
/// ordered-pair matrix of accepted projects, skips the pairs doc 115 permits, and for
/// each remaining pair builds a graph that is otherwise fully compliant, injects that
/// one edge, and requires exactly the finding for that edge. One sampled edge would
/// leave every other pair unproved, and a rule that only reported "some violation" would
/// pass while detecting the wrong thing.
/// </para>
/// <para>
/// The positive direction is asserted too: the real repository must produce zero
/// findings. Negative controls alone would pass against a rule that rejected
/// everything.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class ArchitectureRuleTests
{
    /// <summary>The real repository satisfies the accepted boundary with no findings.</summary>
    [Test]
    public void TheRepositorySatisfiesTheAcceptedProjectBoundary()
    {
        ProjectGraph graph = ProjectGraph.ReadFromDisk(TestArtifacts.RepositoryRoot);
        ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(graph);

        Assert.That(
            Render(findings),
            Is.Empty,
            "the repository violates the accepted project boundary");
    }

    /// <summary>
    /// Every ordered pair of accepted projects that doc 115 does not permit is rejected,
    /// one negative control per pair.
    /// </summary>
    [Test]
    public void EveryForbiddenReferenceEdgeIsRejected()
    {
        List<string> unproved = new();
        List<string> evidence = new()
        {
            "# One negative control per forbidden project-reference edge (VER-FND-009-002).",
            "# Each row injected exactly that edge into an otherwise fully compliant graph and",
            "# recorded the finding the rules produced. Canonical, ordered, reviewable text",
            "# (doc 91 § Determinism and fixture policy).",
            "#",
            "# injected edge\texpected rule\tfinding produced",
        };
        int controls = 0;

        foreach (AcceptedProject from in AcceptedArchitecture.Projects)
        {
            foreach (AcceptedProject to in AcceptedArchitecture.Projects)
            {
                if (string.Equals(from.Name, to.Name, StringComparison.Ordinal))
                {
                    continue;
                }

                bool permitted = from.PermittedReferences.Contains(to.Name);
                bool isGodotProject = string.Equals(
                    to.Name,
                    AcceptedArchitecture.GodotProject,
                    StringComparison.Ordinal);
                if (permitted && !isGodotProject)
                {
                    continue;
                }

                controls++;
                ArchitectureRule expected = isGodotProject
                    ? ArchitectureRule.ReverseGodotEdge
                    : ArchitectureRule.ForbiddenReference;
                string edge = from.Name + " -> " + to.Name;

                ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(
                    ProjectGraph.FromAcceptedBoundary().WithReference(from.Name, to.Name));

                evidence.Add(edge + "\t" + expected + "\t" + FindingFor(findings, expected, edge));

                if (!Contains(findings, expected, edge))
                {
                    unproved.Add(edge + " expected " + expected + ", got: " + Render(findings));
                }
            }
        }

        evidence.Add(
            "# " + controls.ToString(CultureInfo.InvariantCulture)
            + " forbidden edges, each individually controlled.");
        string artifact = WriteEvidence("architecture-forbidden-edges.txt", evidence);

        Expect.Multiple(() =>
        {
            Assert.That(
                controls,
                Is.GreaterThanOrEqualTo(100),
                "the forbidden-edge matrix must cover every accepted project pair, not a sample");
            Assert.That(unproved, Is.Empty);
        });

        TestContext.Progress.WriteLine(
            controls.ToString(CultureInfo.InvariantCulture)
            + " forbidden synthetic edges were each injected into an otherwise compliant graph "
            + "and each produced its own finding; evidence at " + artifact);
    }

    /// <summary>
    /// A permitted-but-undeclared edge is reported separately from a forbidden one, so
    /// legal dependency drift that no reviewer was told about is still caught.
    /// </summary>
    [Test]
    public void APermittedButUndeclaredEdgeIsReportedAsDrift()
    {
        // doc 115 permits MechaMiner.Persistence to use "narrow immutable types from
        // MechaMiner.Simulation"; the repository does not declare that edge yet, because
        // no durable type crosses the boundary until PST-005.
        ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(
            ProjectGraph.FromAcceptedBoundary()
                .WithReference("MechaMiner.Persistence", "MechaMiner.Simulation"));

        Expect.Multiple(() =>
        {
            Assert.That(
                Contains(findings, ArchitectureRule.UndeclaredReference,
                    "MechaMiner.Persistence -> MechaMiner.Simulation"),
                Is.True,
                Render(findings));
            Assert.That(
                Contains(findings, ArchitectureRule.ForbiddenReference,
                    "MechaMiner.Persistence -> MechaMiner.Simulation"),
                Is.False,
                "a permitted edge must not be reported as forbidden");
        });
    }

    /// <summary>A declared edge that disappears from a project file is reported.</summary>
    [Test]
    public void ARemovedDeclaredEdgeIsRejected()
    {
        ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(
            ProjectGraph.FromAcceptedBoundary()
                .WithoutReference("MechaMiner.Simulation", "MechaMiner.Content"));

        Assert.That(
            Contains(findings, ArchitectureRule.MissingReference, "MechaMiner.Simulation -> MechaMiner.Content"),
            Is.True,
            Render(findings));
    }

    /// <summary>
    /// A Godot dependency in any project that is not <c>game/</c> is rejected, one
    /// negative control per project and per kind of evidence.
    /// </summary>
    [Test]
    public void EveryGodotDependencyOutsideTheGodotProjectIsRejected()
    {
        string[] evidenceKinds =
        {
            "Sdk=Godot.NET.Sdk/4.7.1",
            "PackageReference=GodotSharp",
            "lock:GodotSharp",
        };

        List<string> unproved = new();
        foreach (AcceptedProject project in AcceptedArchitecture.Projects)
        {
            if (project.GodotAllowed)
            {
                continue;
            }

            foreach (string evidence in evidenceKinds)
            {
                ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(
                    ProjectGraph.FromAcceptedBoundary().WithGodotEvidence(project.Name, evidence));
                if (!Contains(findings, ArchitectureRule.ForbiddenGodotDependency, project.Name))
                {
                    unproved.Add(project.Name + " with " + evidence + ": " + Render(findings));
                }
            }
        }

        Assert.That(unproved, Is.Empty);
    }

    /// <summary>The Godot project losing its engine dependency is also a violation.</summary>
    [Test]
    public void TheGodotProjectWithoutAGodotDependencyIsRejected()
    {
        ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(
            ProjectGraph.FromAcceptedBoundary().WithoutGodotEvidence(AcceptedArchitecture.GodotProject));

        Assert.That(
            Contains(findings, ArchitectureRule.MissingGodotDependency, AcceptedArchitecture.GodotProject),
            Is.True,
            Render(findings));
    }

    /// <summary>
    /// A Godot import outside <c>game/</c> is rejected. This controls the
    /// <em>aggregation</em> step only: the import is handed to the rules as a recorded
    /// value, so the scan that decides what counts as an import never runs. See
    /// <see cref="TheGodotImportRuleCatchesEveryWayOfNamingTheNamespace"/> for the
    /// control over the scan itself.
    /// </summary>
    [Test]
    public void AGodotImportOutsideTheGodotProjectIsRejected()
    {
        ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(
            ProjectGraph.FromAcceptedBoundary()
                .WithGodotImportOutsideGame("src/MechaMiner.Simulation/Smuggled.cs"));

        Assert.That(
            Contains(findings, ArchitectureRule.GodotTypeOutsideGame, "src/MechaMiner.Simulation/Smuggled.cs"),
            Is.True,
            Render(findings));
    }

    /// <summary>
    /// A GDScript file is rejected (<c>TR-FND-002</c>). Aggregation only, as above; see
    /// <see cref="TheGdScriptRuleFiresOnARealFileOnDisk"/> for the glob.
    /// </summary>
    [Test]
    public void AGdScriptFileIsRejected()
    {
        ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(
            ProjectGraph.FromAcceptedBoundary().WithGdScript("game/scenes/Boot.gd"));

        Assert.That(
            Contains(findings, ArchitectureRule.GdScriptPresent, "game/scenes/Boot.gd"),
            Is.True,
            Render(findings));
    }

    /// <summary>
    /// Every way C# offers of naming the <c>Godot</c> namespace, written into a real
    /// file that <see cref="ProjectGraph.ReadFromDisk"/> then discovers and scans.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the control the rule did not have. <see cref="GodotNamingForms"/> is the
    /// list of forms, and the test is parameterised over it, so a form added to the list
    /// is controlled by construction rather than by someone remembering to write a
    /// seventh test. When it was added, five of the six entries below failed: the scan
    /// tested for <c>using Godot;</c> only, while the rule it feeds is called
    /// <see cref="ArchitectureRule.GodotTypeOutsideGame"/>.
    /// </para>
    /// <para>
    /// The file is written to disk on purpose. Every other control for this rule injects
    /// a recorded path with <see cref="ProjectGraph.WithGodotImportOutsideGame"/>, which
    /// exercises the aggregation and skips the enumeration entirely — so the regex that
    /// decides what gets recorded had no control at all, and could have been anything.
    /// </para>
    /// </remarks>
    [TestCaseSource(nameof(GodotNamingForms))]
    public void TheGodotImportRuleCatchesEveryWayOfNamingTheNamespace(string form, string source)
    {
        string root = CreateScratchTree();
        try
        {
            string relative = "src/MechaMiner.Probe/Probe.cs";
            WriteScratchFile(root, relative, source);

            ImmutableArray<ArchitectureFinding> findings =
                ArchitectureRules.Evaluate(ProjectGraph.ReadFromDisk(root));

            Assert.That(
                Contains(findings, ArchitectureRule.GodotTypeOutsideGame, relative),
                Is.True,
                "the naming form '" + form + "' evaded the scan:\n" + source);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// Spellings that look like the namespace and are not it must not fire, so the
    /// six-form control above is measuring the token rather than a scan that flags any
    /// file containing the letters.
    /// </summary>
    [TestCaseSource(nameof(GodotLookalikeForms))]
    public void TheGodotImportRuleDoesNotFireOnSpellingsThatAreNotTheNamespace(string form, string source)
    {
        string root = CreateScratchTree();
        try
        {
            string relative = "src/MechaMiner.Probe/Probe.cs";
            WriteScratchFile(root, relative, source);

            ImmutableArray<ArchitectureFinding> findings =
                ArchitectureRules.Evaluate(ProjectGraph.ReadFromDisk(root));

            Assert.That(
                Contains(findings, ArchitectureRule.GodotTypeOutsideGame, relative),
                Is.False,
                "'" + form + "' is not the Godot namespace but the scan flagged it:\n" + source);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// The <c>*.gd</c> glob fires on a real file on disk, not only on a path handed to
    /// the rules as a value.
    /// </summary>
    /// <remarks>
    /// Same defect shape as the Godot import control: <c>WithGdScript</c> records the
    /// path directly, so the glob that finds GDScript had no control. Parameterised over
    /// the placements that matter, because a glob rooted at the wrong directory or an
    /// exclusion list that grew one entry too many would pass a single-case test.
    /// </remarks>
    [TestCase("game/scenes/Boot.gd")]
    [TestCase("src/MechaMiner.Probe/Smuggled.gd")]
    [TestCase("tools/Helper.gd")]
    [TestCase("Root.gd")]
    public void TheGdScriptRuleFiresOnARealFileOnDisk(string relative)
    {
        string root = CreateScratchTree();
        try
        {
            WriteScratchFile(root, relative, "extends Node\n\nfunc _ready():\n    pass\n");

            ImmutableArray<ArchitectureFinding> findings =
                ArchitectureRules.Evaluate(ProjectGraph.ReadFromDisk(root));

            Assert.That(
                Contains(findings, ArchitectureRule.GdScriptPresent, relative),
                Is.True,
                "a real .gd file at " + relative + " was not found by the glob");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// A scratch tree with no <c>.gd</c> file and no Godot-naming C# file produces
    /// neither finding, so the two controls above measure what they wrote rather than a
    /// scan that fires on any tree.
    /// </summary>
    [Test]
    public void AScratchTreeWithNoGodotEvidenceProducesNeitherScanFinding()
    {
        string root = CreateScratchTree();
        try
        {
            WriteScratchFile(
                root,
                "src/MechaMiner.Probe/Probe.cs",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    internal static int Run() => 1;\n}\n");

            ImmutableArray<ArchitectureFinding> findings =
                ArchitectureRules.Evaluate(ProjectGraph.ReadFromDisk(root));

            Expect.Multiple(() =>
            {
                Assert.That(
                    Contains(findings, ArchitectureRule.GodotTypeOutsideGame, "src/MechaMiner.Probe/Probe.cs"),
                    Is.False,
                    Render(findings));
                foreach (ArchitectureFinding finding in findings)
                {
                    Assert.That(
                        finding.Rule,
                        Is.Not.EqualTo(ArchitectureRule.GdScriptPresent),
                        "a tree with no .gd file reported GDScript: " + finding.ToLine());
                }
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Every prescribed layout path is individually load-bearing.</summary>
    [Test]
    public void EveryMissingPrescribedLayoutPathIsRejected()
    {
        List<string> unproved = new();
        foreach (string path in AcceptedArchitecture.RequiredPaths)
        {
            ImmutableArray<ArchitectureFinding> findings = ArchitectureRules.Evaluate(
                ProjectGraph.FromAcceptedBoundary().WithMissingPath(path));
            if (!Contains(findings, ArchitectureRule.MissingLayoutPath, path))
            {
                unproved.Add(path);
            }
        }

        Assert.That(unproved, Is.Empty);
    }

    /// <summary>
    /// The solution must contain exactly the accepted projects: an omission and an
    /// addition are each rejected, and each is a distinct rule.
    /// </summary>
    [Test]
    public void SolutionMembershipMustMatchTheAcceptedDecomposition()
    {
        ImmutableArray<ArchitectureFinding> omitted = ArchitectureRules.Evaluate(
            ProjectGraph.FromAcceptedBoundary()
                .WithoutSolutionEntry("src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj"));

        ImmutableArray<ArchitectureFinding> added = ArchitectureRules.Evaluate(
            ProjectGraph.FromAcceptedBoundary()
                .WithSolutionEntry("src/MechaMiner.Rogue/MechaMiner.Rogue.csproj"));

        Expect.Multiple(() =>
        {
            Assert.That(
                Contains(omitted, ArchitectureRule.ProjectMissingFromSolution,
                    "src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj"),
                Is.True,
                Render(omitted));
            Assert.That(
                Contains(added, ArchitectureRule.UnexpectedProjectInSolution,
                    "src/MechaMiner.Rogue/MechaMiner.Rogue.csproj"),
                Is.True,
                Render(added));
        });
    }

    /// <summary>
    /// A compliant graph produces no findings, so the negative controls above are
    /// measuring the injected violation rather than a rule that always fires.
    /// </summary>
    [Test]
    public void TheAcceptedBoundaryItselfProducesNoFindings()
    {
        Assert.That(Render(ArchitectureRules.Evaluate(ProjectGraph.FromAcceptedBoundary())), Is.Empty);
    }

    /// <summary>
    /// Every way C# offers of naming a namespace, each of which names <c>Godot</c>.
    /// </summary>
    /// <remarks>
    /// Add a form here and
    /// <see cref="TheGodotImportRuleCatchesEveryWayOfNamingTheNamespace"/> covers it
    /// without a new test being written. The same six forms are the control set in
    /// <c>build/verify-architecture.sh</c> § 6, so the two readers of this rule are
    /// measured against one list.
    /// </remarks>
    private static IEnumerable<TestCaseData> GodotNamingForms
    {
        get
        {
            yield return Form("using Godot;", "using Godot;");
            yield return Form("global using Godot;", "global using Godot;");
            yield return Form("using static Godot.GD;", "using static Godot.GD;");
            yield return Form("using GD = Godot.GD;", "using GD = Godot.GD;");
            yield return Form("using GodotAlias = Godot;", "using GodotAlias = Godot;");
            yield return new TestCaseData(
                "fully qualified, no using",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    internal static void Run() => Godot.GD.Print(\"x\");\n}\n");
        }
    }

    /// <summary>
    /// Spellings a token-anywhere scan gets wrong if its boundaries are loose, plus the
    /// two contexts that separate a namespace reference from an identifier that happens
    /// to be spelled <c>Godot</c>.
    /// </summary>
    private static IEnumerable<TestCaseData> GodotLookalikeForms
    {
        get
        {
            yield return Form("MechaMiner.GodotLike (qualified lookalike)", "using MechaMiner.GodotLike;");
            yield return new TestCaseData(
                "NotGodotish (identifier with the token embedded)",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    private const string Name = \"NotGodotish\";\n\n"
                + "    internal static string Run() => Name;\n}\n");
            yield return new TestCaseData(
                "bare GodotLike (unqualified lookalike)",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    internal static void Run() => GodotLike.Do();\n}\n");
            yield return new TestCaseData(
                "a member named Godot, which is not a namespace reference",
                "namespace MechaMiner.Probe;\n\ninternal sealed class Pins\n{\n"
                + "    public int Godot { get; set; }\n}\n");
            yield return new TestCaseData(
                "the word Godot in a comment and in a diagnostic string",
                "namespace MechaMiner.Probe;\n\n"
                + "// Only game/ may reference Godot, which is why this project does not.\n"
                + "internal static class Probe\n{\n"
                + "    internal static string Run() => \"the pure tier launched no Godot process\";\n}\n");
        }
    }

    /// <summary>A <c>using</c>-directive probe: the directive plus a body that uses it.</summary>
    private static TestCaseData Form(string name, string directive)
    {
        return new TestCaseData(
            name,
            directive + "\n\nnamespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
            + "    internal static int Run() => 1;\n}\n");
    }

    /// <summary>
    /// An empty scratch repository root, so a scan can be run against real files without
    /// writing anything into the repository under test.
    /// </summary>
    private static string CreateScratchTree()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "mecha-architecture-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>Writes one file into a scratch tree, creating its directories.</summary>
    private static void WriteScratchFile(string root, string relative, string content)
    {
        string absolute = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, content);
    }

    /// <summary>
    /// Writes the per-control evidence as canonical ordered text, so the control matrix
    /// is reviewable rather than only counted.
    /// </summary>
    private static string WriteEvidence(string fileName, IReadOnlyList<string> lines)
    {
        string directory = System.IO.Path.Combine(
            TestArtifacts.RepositoryRoot,
            "artifacts",
            "architecture");
        System.IO.Directory.CreateDirectory(directory);
        string absolute = System.IO.Path.Combine(directory, fileName);
        System.IO.File.WriteAllText(absolute, string.Join("\n", lines) + "\n");
        return TestArtifacts.Relative(absolute);
    }

    private static string FindingFor(
        ImmutableArray<ArchitectureFinding> findings,
        ArchitectureRule rule,
        string subject)
    {
        foreach (ArchitectureFinding finding in findings)
        {
            if (finding.Rule == rule && string.Equals(finding.Subject, subject, StringComparison.Ordinal))
            {
                return finding.Rule.ToString() + " on " + finding.Subject;
            }
        }

        return "NO FINDING";
    }

    private static bool Contains(
        ImmutableArray<ArchitectureFinding> findings,
        ArchitectureRule rule,
        string subject)
    {
        foreach (ArchitectureFinding finding in findings)
        {
            if (finding.Rule == rule && string.Equals(finding.Subject, subject, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string Render(ImmutableArray<ArchitectureFinding> findings)
    {
        if (findings.IsEmpty)
        {
            return string.Empty;
        }

        List<string> lines = new();
        foreach (ArchitectureFinding finding in findings)
        {
            lines.Add(finding.ToLine());
        }

        return string.Join("\n", lines);
    }
}
