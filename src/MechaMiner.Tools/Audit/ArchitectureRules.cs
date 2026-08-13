using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MechaMiner.Tools.Audit;

/// <summary>The rule an architecture finding violates.</summary>
/// <remarks>
/// A closed enumeration rather than free text, so a negative control can require the
/// exact rule it injected instead of "some failure happened". A control that only
/// asserted a nonzero finding count would still pass if the rules reported an unrelated
/// problem.
/// </remarks>
internal enum ArchitectureRule
{
    /// <summary>A project reference edge doc 115 § Accepted project boundary does not permit.</summary>
    ForbiddenReference,

    /// <summary>An edge doc 115 permits that the accepted table does not declare.</summary>
    UndeclaredReference,

    /// <summary>A declared edge that the project file does not have.</summary>
    MissingReference,

    /// <summary>A Godot dependency in a project that may not have one.</summary>
    ForbiddenGodotDependency,

    /// <summary>The Godot project has no Godot dependency.</summary>
    MissingGodotDependency,

    /// <summary>Something references <c>MechaMiner.Game</c>, which is always a reverse edge.</summary>
    ReverseGodotEdge,

    /// <summary>A file outside <c>game/</c> imports the <c>Godot</c> namespace.</summary>
    GodotTypeOutsideGame,

    /// <summary>A GDScript file exists.</summary>
    GdScriptPresent,

    /// <summary>A prescribed repository layout path is missing.</summary>
    MissingLayoutPath,

    /// <summary>The solution omits an accepted project.</summary>
    ProjectMissingFromSolution,

    /// <summary>The solution contains a project the accepted decomposition does not.</summary>
    UnexpectedProjectInSolution,

    /// <summary>A project file exists on disk that the accepted decomposition does not name.</summary>
    UnexpectedProjectOnDisk,
}

/// <summary>One violation of the accepted architecture.</summary>
internal sealed class ArchitectureFinding
{
    internal ArchitectureFinding(ArchitectureRule rule, string subject, string detail)
    {
        Rule = rule;
        Subject = subject;
        Detail = detail;
    }

    /// <summary>The rule violated.</summary>
    internal ArchitectureRule Rule { get; }

    /// <summary>The project, path, or edge the finding is about.</summary>
    internal string Subject { get; }

    /// <summary>What was expected and what was observed.</summary>
    internal string Detail { get; }

    /// <summary>A single canonical reviewable line.</summary>
    internal string ToLine()
    {
        return Rule.ToString() + "\t" + Subject + "\t" + Detail;
    }
}

/// <summary>
/// The accepted project boundary as pure rules over a <see cref="ProjectGraph"/>.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-001</c>). Authority:
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Accepted
/// project boundary and § Verification ("Architecture tests enforce project-reference
/// direction and prohibit Godot references in pure projects"),
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c> § Repository
/// structure, <c>docs/technical/00-technical-foundation.md</c> § Language boundary.
/// Requirements: <c>TR-CTR-001</c>, <c>TR-BLD-006</c>, <c>TR-FND-001</c>,
/// <c>TR-FND-002</c>.
/// </para>
/// <para>
/// <see cref="Evaluate"/> is a pure function from a graph to findings and reads nothing
/// from the filesystem. That is what lets a test hand it a graph carrying exactly one
/// forbidden edge and require exactly that finding, which is
/// <c>TASK-FND-009-001</c>'s completion gate: "each forbidden synthetic edge fails".
/// </para>
/// </remarks>
internal static class ArchitectureRules
{
    /// <summary>Evaluates every rule and returns the findings, in rule order then subject order.</summary>
    internal static ImmutableArray<ArchitectureFinding> Evaluate(ProjectGraph graph)
    {
        ArgumentNullException.ThrowIfNull(graph);

        List<ArchitectureFinding> findings = new();
        EvaluateSolutionMembership(graph, findings);
        EvaluateReferences(graph, findings);
        EvaluateGodotDependencies(graph, findings);
        EvaluateSourceScans(graph, findings);
        EvaluateLayout(graph, findings);

        findings.Sort(static (left, right) =>
        {
            int byRule = left.Rule.CompareTo(right.Rule);
            return byRule != 0
                ? byRule
                : string.CompareOrdinal(left.Subject, right.Subject);
        });
        return ImmutableArray.CreateRange(findings);
    }

    private static void EvaluateSolutionMembership(ProjectGraph graph, List<ArchitectureFinding> findings)
    {
        HashSet<string> accepted = new(StringComparer.Ordinal);
        foreach (AcceptedProject project in AcceptedArchitecture.Projects)
        {
            accepted.Add(project.ProjectPath);
        }

        HashSet<string> inSolution = new(graph.SolutionProjectPaths, StringComparer.Ordinal);

        foreach (AcceptedProject project in AcceptedArchitecture.Projects)
        {
            if (!inSolution.Contains(project.ProjectPath))
            {
                findings.Add(new ArchitectureFinding(
                    ArchitectureRule.ProjectMissingFromSolution,
                    project.ProjectPath,
                    "the accepted decomposition names this project but MechaMiner.sln does not reference it"));
            }
        }

        foreach (string path in graph.SolutionProjectPaths)
        {
            if (!accepted.Contains(path))
            {
                findings.Add(new ArchitectureFinding(
                    ArchitectureRule.UnexpectedProjectInSolution,
                    path,
                    "MechaMiner.sln references a project the accepted decomposition does not name"));
            }
        }

        foreach (ObservedProject project in graph.Projects)
        {
            if (!accepted.Contains(project.ProjectPath))
            {
                findings.Add(new ArchitectureFinding(
                    ArchitectureRule.UnexpectedProjectOnDisk,
                    project.ProjectPath,
                    "a project file exists that the accepted decomposition does not name; adding one "
                    + "requires updating doc 115 § Accepted project boundary and this table in the same task"));
            }
        }
    }

    private static void EvaluateReferences(ProjectGraph graph, List<ArchitectureFinding> findings)
    {
        foreach (ObservedProject observed in graph.Projects)
        {
            AcceptedProject? accepted = AcceptedArchitecture.Find(observed.Name);
            if (accepted is null)
            {
                // Already reported as UnexpectedProjectOnDisk; a second finding per edge
                // would bury the cause.
                continue;
            }

            foreach (string reference in observed.ProjectReferences)
            {
                if (string.Equals(reference, AcceptedArchitecture.GodotProject, StringComparison.Ordinal))
                {
                    findings.Add(new ArchitectureFinding(
                        ArchitectureRule.ReverseGodotEdge,
                        observed.Name + " -> " + reference,
                        "nothing may reference " + AcceptedArchitecture.GodotProject
                        + "; doc 115 § Contract change rules: 'reverse Godot dependency is forbidden'"));
                    continue;
                }

                if (!accepted.PermittedReferences.Contains(reference))
                {
                    findings.Add(new ArchitectureFinding(
                        ArchitectureRule.ForbiddenReference,
                        observed.Name + " -> " + reference,
                        "doc 115 § Accepted project boundary permits " + observed.Name + " to reference ["
                        + string.Join(", ", accepted.PermittedReferences) + "]"));
                    continue;
                }

                if (!accepted.DeclaredReferences.Contains(reference))
                {
                    findings.Add(new ArchitectureFinding(
                        ArchitectureRule.UndeclaredReference,
                        observed.Name + " -> " + reference,
                        "the edge is permitted by doc 115 but is not in the declared accepted set ["
                        + string.Join(", ", accepted.DeclaredReferences)
                        + "]; the task that adds it updates AcceptedArchitecture in the same change"));
                }
            }

            foreach (string declared in accepted.DeclaredReferences)
            {
                if (!observed.ProjectReferences.Contains(declared))
                {
                    findings.Add(new ArchitectureFinding(
                        ArchitectureRule.MissingReference,
                        observed.Name + " -> " + declared,
                        "the accepted decomposition declares this edge but the project file does not have it"));
                }
            }
        }
    }

    private static void EvaluateGodotDependencies(ProjectGraph graph, List<ArchitectureFinding> findings)
    {
        foreach (ObservedProject observed in graph.Projects)
        {
            AcceptedProject? accepted = AcceptedArchitecture.Find(observed.Name);
            if (accepted is null)
            {
                continue;
            }

            if (accepted.GodotAllowed)
            {
                if (observed.GodotEvidence.Count == 0)
                {
                    findings.Add(new ArchitectureFinding(
                        ArchitectureRule.MissingGodotDependency,
                        observed.Name,
                        "the Godot project must declare and lock a Godot dependency, and none was observed"));
                }

                continue;
            }

            if (observed.GodotEvidence.Count > 0)
            {
                findings.Add(new ArchitectureFinding(
                    ArchitectureRule.ForbiddenGodotDependency,
                    observed.Name,
                    "doc 115 § Accepted project boundary allows no Godot types here; observed ["
                    + string.Join(", ", observed.GodotEvidence) + "]"));
            }
        }
    }

    private static void EvaluateSourceScans(ProjectGraph graph, List<ArchitectureFinding> findings)
    {
        foreach (string file in graph.GodotImportsOutsideGame)
        {
            findings.Add(new ArchitectureFinding(
                ArchitectureRule.GodotTypeOutsideGame,
                file,
                "only game/ may hold Godot types; a pure project that imports Godot has taken an engine "
                + "dependency the project boundary forbids"));
        }

        foreach (string file in graph.GdScriptFiles)
        {
            findings.Add(new ArchitectureFinding(
                ArchitectureRule.GdScriptPresent,
                file,
                "TR-FND-002 forbids GDScript and mixed-language ownership in runtime logic"));
        }
    }

    private static void EvaluateLayout(ProjectGraph graph, List<ArchitectureFinding> findings)
    {
        foreach (string path in graph.MissingPaths)
        {
            findings.Add(new ArchitectureFinding(
                ArchitectureRule.MissingLayoutPath,
                path,
                "doc 100 § Repository structure prescribes this path from FND-001 onward"));
        }
    }
}
