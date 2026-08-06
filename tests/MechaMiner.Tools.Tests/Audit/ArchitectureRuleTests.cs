using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
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

    /// <summary>A Godot import outside <c>game/</c> is rejected.</summary>
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

    /// <summary>A GDScript file is rejected (<c>TR-FND-002</c>).</summary>
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
