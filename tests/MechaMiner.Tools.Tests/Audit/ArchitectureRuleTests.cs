using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
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
    /// <remarks>
    /// The expected number of pairs is read from <c>build/audit-expectations.env</c> via
    /// <see cref="AuditExpectations.ForbiddenEdgeControls"/> and asserted exactly, not as
    /// a floor. <c>build/verify-registry.sh</c> stage 3 reads the same line and compares
    /// the retained inventory against it, so the two readers of this matrix cannot report
    /// different numbers: the count used to be a literal here and an equal literal there,
    /// with a comment claiming they were the same, and changing this one to 10 left the
    /// script reporting a floor of 100 and attributing it to this test by name.
    /// </remarks>
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
                Is.EqualTo(AuditExpectations.ForbiddenEdgeControls),
                "the forbidden-edge matrix must be every accepted project pair, not a sample;"
                + " if the accepted boundary changed, FORBIDDEN_EDGE_CONTROLS in "
                + AuditExpectations.FilePath + " changes with it in the same commit");
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
    /// Every naming form the shared corpus covers, written into a real file that
    /// <see cref="ProjectGraph.ReadFromDisk"/> then discovers and scans. Deliberately not
    /// "every way C# offers": see <see cref="GodotFormsNeitherReaderCovers"/> for the ways
    /// it does not, which are asserted rather than left implied.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the control the rule did not have. <see cref="GodotNamingForms"/> is the
    /// list of forms, and the test is parameterised over it, so a form added to the list
    /// is controlled by construction rather than by someone remembering to write a
    /// seventh test. When it was added, five of the six entries then present failed: the scan
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
    /// Text that looks like the namespace and is not it must not fire, so the positive
    /// control above is measuring the token in context rather than a scan that flags any
    /// file containing the letters. Four of these - the <c>x*</c> class - are the ones the
    /// shell reader in § 6 accuses and this reader clears.
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
    /// The recorded gap, asserted rather than assumed: each of these is a real reference to
    /// <c>Godot</c> and this reader does not see it.
    /// </summary>
    /// <remarks>
    /// A gap nothing measures is indistinguishable from a gap nobody has yet found, and the
    /// header on <see cref="GodotNamingForms"/> used to claim six forms were "every way C#
    /// offers", which was false. This test is the measurement. It fails when a form here
    /// starts being caught, which is an improvement, and the failure message says to move
    /// the form into <see cref="GodotNamingForms"/> in this file AND into § 6's <c>f*</c>
    /// class in <c>build/verify-architecture.sh</c>, so the corpus stays one list and the
    /// improvement is a visible edit rather than a silently loosened control.
    /// </remarks>
    [TestCaseSource(nameof(GodotFormsNeitherReaderCovers))]
    public void TheRecordedGapInTheGodotImportRuleIsStillExactlyThatGap(string form, string source)
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
                "'" + form + "' is recorded as beyond this reader and was caught. That is an"
                + " improvement: move it from GodotFormsNeitherReaderCovers into"
                + " GodotNamingForms here, move the matching k* probe into the f* class in"
                + " build/verify-architecture.sh, and update the scores in both readers'"
                + " headers.\n" + source);
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
    /// The shared probe corpus: 46 files in four classes, the same list
    /// <c>build/verify-architecture.sh</c> § 6 writes, so a divergence between the two
    /// readers of this rule is a failing control rather than a silent disagreement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GodotNamingForms"/> is the <c>f*</c> class: real references this reader
    /// must catch. <see cref="GodotLookalikeForms"/> is the <c>n*</c> and <c>x*</c>
    /// classes: text that is not a reference and must not fire.
    /// <see cref="GodotFormsNeitherReaderCovers"/> is the <c>k*</c> class: real references
    /// NEITHER reader sees, asserted as missed so the gap is measured instead of
    /// unmeasured. Add a form to a list and the parameterised test covers it without a new
    /// test being written.
    /// </para>
    /// <para>
    /// Every member has been compiled against a Godot shim: each <c>f*</c> and <c>k*</c>
    /// source fails to compile without it, so every positive is a reference that really
    /// resolves to <c>Godot</c>, and each <c>n*</c> and <c>x*</c> source compiles without
    /// it, so no lookalike is secretly a reference. A corpus whose positives were not
    /// verified that way measures a scan against an opinion.
    /// </para>
    /// <para>
    /// WHY THE PROBES ARE NOT ALL ONE SHORT LINE. The original six each occupied a line of
    /// its own, and that is why they were not enough: a stripper defect that swallows the
    /// rest of a line is invisible to a probe whose line holds nothing else. All six stayed
    /// green while the single line <c>char q = '"'; Godot.GD.Print("y");</c> passed both
    /// readers, because the quote held in the character literal opened a string state
    /// neither stripper knew how to leave. Most of the corpus is therefore same-line
    /// probes - decoy first, reference after it on the same line. None is contrived: a URL,
    /// a quote character, or an apostrophe in English prose beside a Godot call is ordinary
    /// code, and English prose in diagnostic strings is the commonest shape here.
    /// </para>
    /// <para>
    /// Two members are production-sized, a few hundred KiB each with the reference near the
    /// top and the bulk after it. For the shell reader they are load-bearing: its scan
    /// carried a SIGPIPE false pass whose miss rate rose with file size, and at ~75-byte
    /// fixture size that defect was structurally unreachable by any control. For this
    /// reader they are a size and position control rather than a pipeline one. They are in
    /// both lists because the lists are one list.
    /// </para>
    /// <para>
    /// The four <c>x*</c> members are lookalikes THIS reader clears and the shell reader
    /// flags; see <see cref="ProjectGraph.NamesGodotNamespace"/> for the measured scores
    /// and for why the pair is one precise reader plus one approximation.
    /// </para>
    /// </remarks>
    private static IEnumerable<TestCaseData> GodotNamingForms
    {
        get
        {
            yield return Form("f01 using Godot;", "using Godot;");
            yield return Form("f02 global using Godot;", "global using Godot;");
            yield return Form("f03 using static Godot.GD;", "using static Godot.GD;");
            yield return Form("f04 using GD = Godot.GD;", "using GD = Godot.GD;");
            yield return Form("f05 using GodotAlias = Godot;", "using GodotAlias = Godot;");
            yield return SameLineForm("f06 fully qualified, no using", "Godot.GD.Print(\"x\");");
            yield return SameLineForm(
                "f07 a quote held in a char literal, then a reference on the same line",
                "char q = '\"'; Godot.GD.Print(q);");
            yield return SameLineForm(
                "f08 a // inside a string, then a reference on the same line",
                "string s = \"// not a comment\"; Godot.GD.Print(s);");
            yield return SameLineForm(
                "f09 a URL inside a string, then a reference on the same line",
                "string u = \"http://example.invalid/x\"; Godot.GD.Print(u);");

            // f10 and f11 are the round-four regression in the shell reader's character
            // literal pass: written as `'([^'\\]|\\.)*'` and run before the string pass,
            // it paired apostrophes belonging to two DIFFERENT string literals and deleted
            // everything between them, so both of these lines lost their reference. This
            // reader always got them right; they are here because the lists are one list
            // and because English prose in a diagnostic string is the commonest shape in
            // this repository.
            yield return SameLineForm(
                "f10 apostrophes in two different string literals, then a reference",
                "string s = \"don't\"; Godot.GD.Print(\"won't\");");
            yield return SameLineForm(
                "f11 an apostrophe in a block comment, then a reference on the same line",
                "/* it's fine */ Godot.GD.Print(\"won't\");");
            yield return SameLineForm(
                "f12 an apostrophe in a line comment, then a reference on the next line",
                "// it's fine\n        Godot.GD.Print(\"won't\");");

            // Caught only because line two still holds `Godot.`; its three siblings in
            // GodotFormsNeitherReaderCovers are not. The pair is the evidence that a
            // per-line scan's boundary is arbitrary rather than principled.
            yield return Form("f13 using static split across two lines", "using static\n    Godot.GD;");

            yield return SameLineForm(
                "f14 an escaped quote inside a string, then a reference",
                "string s = \"a\\\"b\"; Godot.GD.Print(s);");
            yield return SameLineForm(
                "f15 a doubled quote inside a verbatim string, then a reference",
                "string s = @\"a\"\"b\"; Godot.GD.Print(s);");
            yield return SameLineForm(
                "f16 a raw string literal, then a reference",
                "string s = \"\"\"x\"\"\"; Godot.GD.Print(s);");
            yield return SameLineForm(
                "f17 an apostrophe inside an interpolated string, then a reference",
                "int x = 1; string s = $\"it's {x}\"; Godot.GD.Print(s);");
            yield return new TestCaseData(
                "f18 an attribute in qualifier position",
                "namespace MechaMiner.Probe;\n\n[Godot.GlobalClass]\n"
                + "internal sealed class Probe\n{\n    internal int Run() => 1;\n}\n");
            yield return SameLineForm(
                "f19 typeof in qualifier position",
                "System.Type t = typeof(Godot.Node); Godot.GD.Print(t);");
            yield return new TestCaseData(
                "f20 a field whose type is qualified",
                "namespace MechaMiner.Probe;\n\ninternal sealed class Probe\n{\n"
                + "    private Godot.Node? _node;\n\n    internal object? Run() => _node;\n}\n");
            yield return new TestCaseData(
                "f21 a qualified base type",
                "namespace MechaMiner.Probe;\n\ninternal sealed class Probe : Godot.Node\n{\n"
                + "    internal int Run() => 1;\n}\n");
            yield return Form("f22 using Godot.Collections;", "using Godot.Collections;");
            yield return SameLineForm(
                "f23 a qualified generic argument",
                "var l = new System.Collections.Generic.List<Godot.Node>(); Godot.GD.Print(l.Count);");
            yield return Form("f24 using Godot ; with a space before the semicolon", "using Godot ;");
            yield return SameLineForm("f25 whitespace around the qualifier dot", "Godot . GD.Print(\"x\");");
            yield return SameLineForm(
                "f26 a block comment that ends on the reference's own line",
                "/* a comment that\n           spans lines and ends here */ Godot.GD.Print(\"y\");");
            yield return SameLineForm(
                "f27 an escaped apostrophe in a char literal, then a reference",
                "char q = '\\''; Godot.GD.Print(q);");
            yield return SameLineForm(
                "f28 a reference inside a conditional-compilation block",
                "#if DEBUG\n        Godot.GD.Print(\"d\");\n#else\n        Godot.GD.Print(\"r\");\n#endif");
            yield return SameLineForm(
                "f29 a nested namespace's type",
                "var a = new Godot.Collections.Array(); Godot.GD.Print(a.Count);");
            yield return new TestCaseData(
                "f30 production-sized, reference early, qualifier branch",
                ProductionSizedQualifierProbe());
            yield return new TestCaseData(
                "f31 production-sized, reference early, using branch",
                ProductionSizedUsingProbe());
        }
    }

    /// <summary>
    /// Real references NEITHER reader sees, asserted as missed. A recorded gap is
    /// measured; an unrecorded one is a false claim of completeness.
    /// </summary>
    /// <remarks>
    /// Three are one reference split across two physical lines, which both readers miss
    /// because both decide per line - and <c>using static</c> split the same way IS caught
    /// (<c>f13</c>) because its second line still holds <c>Godot.</c>, so the boundary is
    /// arbitrary. Two are identifiers written with a Unicode escape, which the compiler
    /// binds to <c>Godot</c> and no amount of text stripping reveals. Closing either class
    /// needs a parser; see <see cref="ProjectGraph.NamesGodotNamespace"/> for the
    /// escalation. If one of these starts being caught, this test fails and says to move it
    /// into <see cref="GodotNamingForms"/> - a gap closing should be a visible edit in the
    /// diff, the same ratchet the audit-expectations census uses.
    /// </remarks>
    private static IEnumerable<TestCaseData> GodotFormsNeitherReaderCovers
    {
        get
        {
            yield return Form("k1 global using split across two lines", "global using\n    Godot;");
            yield return Form("k2 using split across two lines", "using\n    Godot;");
            yield return SameLineForm(
                "k3 a qualifier split across two lines",
                "Godot\n            .GD.Print(\"y\");");
            yield return Form("k4 a Unicode-escaped identifier in a using", "using \\u0047odot;");
            yield return SameLineForm(
                "k5 a Unicode-escaped identifier in qualifier position",
                "\\u0047odot.GD.Print(\"x\");");
        }
    }

    /// <summary>
    /// Spellings a token-anywhere scan gets wrong if its boundaries are loose, plus the
    /// contexts that separate a namespace reference from an identifier that happens to be
    /// spelled <c>Godot</c>, plus the four the shell reader accuses and this one clears.
    /// </summary>
    private static IEnumerable<TestCaseData> GodotLookalikeForms
    {
        get
        {
            yield return Form("n1 MechaMiner.GodotLike (qualified lookalike)", "using MechaMiner.GodotLike;");
            yield return new TestCaseData(
                "n2 NotGodotish (identifier with the token embedded)",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    private const string Name = \"NotGodotish\";\n\n"
                + "    internal static string Run() => Name;\n}\n");
            yield return new TestCaseData(
                "n3 bare GodotLike (unqualified lookalike)",
                "using MechaMiner.GodotLike;\n\nnamespace MechaMiner.Probe;\n\n"
                + "internal static class Probe\n{\n    internal static void Run() => GodotLike.Do();\n}\n");
            yield return new TestCaseData(
                "n4 a member named Godot, which is not a namespace reference",
                "namespace MechaMiner.Probe;\n\ninternal sealed class Pins\n{\n"
                + "    public int Godot { get; set; }\n}\n");
            yield return new TestCaseData(
                "n5 the word Godot in a comment and in a diagnostic string",
                "namespace MechaMiner.Probe;\n\n"
                + "// Only game/ may reference Godot, which is why this project does not.\n"
                + "internal static class Probe\n{\n"
                + "    internal static string Run() => \"the pure tier launched no Godot process\";\n}\n");

            // The other half of the same-line string probes. Reordering the strippers so
            // literals are removed before comments must not stop them removing literals: a
            // file whose only qualifier-position text sits inside a string is not a
            // reference, and if this case starts firing, the stripper has stopped stripping
            // and the same-line probes are passing for the wrong reason.
            yield return new TestCaseData(
                "n6 a qualifier spelled inside a string literal, which is not a reference",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    internal static string Run() => \"call Godot.GD.Print here\";\n}\n");

            // x1-x4: the shell reader flags all four and this reader clears all four. They
            // are the measured refutation of the claim that a sed stripper's only error
            // direction is a false negative. Its sed strips `//` and never `/* */` in any
            // form, and it has no state across lines. Recorded here as lookalikes because
            // that is what they are; recorded there as the x* class because that reader
            // gets them wrong.
            yield return new TestCaseData(
                "x1 a single-line block comment naming the qualifier",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    /* Godot.GD is engine-only, so nothing here calls it. */\n"
                + "    internal static int Run() => 1;\n}\n");
            yield return new TestCaseData(
                "x2 a multi-line block comment naming the qualifier",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    /*\n     * Godot.GD is engine-only.\n"
                + "     * game/ owns every call to Godot.GD.Print.\n     */\n"
                + "    internal static int Run() => 1;\n}\n");
            yield return new TestCaseData(
                "x3 a multi-line verbatim string containing a call",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    private const string Snippet = @\"\nGodot.GD.Print(x);\n\";\n\n"
                + "    internal static string Run() => Snippet;\n}\n");
            yield return new TestCaseData(
                "x4 a multi-line raw string containing the qualifier",
                "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
                + "    private const string Snippet = \"\"\"\n"
                + "        Godot.GD.Print(x);\n        \"\"\";\n\n"
                + "    internal static string Run() => Snippet;\n}\n");
        }
    }

    /// <summary>
    /// A few hundred KiB with the reference on the second line and the bulk after it,
    /// through the qualifier branch. The size and the position both matter: see
    /// <see cref="GodotNamingForms"/>.
    /// </summary>
    private static string ProductionSizedQualifierProbe()
    {
        StringBuilder builder = new();
        builder.Append("namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n")
            .Append("    internal static void Run()\n    {\n")
            .Append("        Godot.GD.Print(\"early\");\n");
        for (int i = 0; i < 12000; i++)
        {
            builder.Append("        System.GC.KeepAlive(")
                .Append(i.ToString(CultureInfo.InvariantCulture))
                .Append(");\n");
        }

        return builder.Append("    }\n}\n").ToString();
    }

    /// <summary>The same idea through the using-directive branch.</summary>
    private static string ProductionSizedUsingProbe()
    {
        StringBuilder builder = new();
        builder.Append("using Godot;\n");
        for (int i = 0; i < 14000; i++)
        {
            builder.Append("using Filler.N")
                .Append(i.ToString(CultureInfo.InvariantCulture))
                .Append(";\n");
        }

        return builder.Append("\nnamespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n")
            .Append("    internal static int Run() => 1;\n}\n").ToString();
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
    /// A probe whose decoy and whose Godot reference share one line, so a stripper that
    /// loses the rest of a line loses the reference with it.
    /// </summary>
    private static TestCaseData SameLineForm(string name, string statement)
    {
        return new TestCaseData(
            name,
            "namespace MechaMiner.Probe;\n\ninternal static class Probe\n{\n"
            + "    internal static void Run() { " + statement + " }\n}\n");
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
