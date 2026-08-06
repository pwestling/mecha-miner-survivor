using System.Collections.Generic;
using System.Collections.Immutable;

namespace MechaMiner.Tools.Audit;

/// <summary>One row of the accepted project boundary.</summary>
/// <remarks>
/// <para>
/// Two reference sets, not one, because
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Accepted
/// project boundary states a <i>maximum</i> while the repository has an exact current
/// set. <see cref="PermittedReferences"/> is doc 115's maximum;
/// <see cref="DeclaredReferences"/> is what the repository actually declares today.
/// </para>
/// <para>
/// Keeping them apart makes two different mistakes detectable. An edge outside
/// <see cref="PermittedReferences"/> is a boundary violation and can never be
/// legitimate. An edge inside the permitted set but outside the declared set is
/// undeclared drift: legal in principle, but it appeared without the task that
/// introduced it updating this table, so no reviewer was told the dependency graph
/// changed. Collapsing the two would either let the second case pass silently or
/// report the first as ordinary drift.
/// </para>
/// </remarks>
internal sealed class AcceptedProject
{
    internal AcceptedProject(
        string projectPath,
        IEnumerable<string> declaredReferences,
        IEnumerable<string> permittedReferences,
        bool godotAllowed)
    {
        ProjectPath = projectPath;
        DeclaredReferences = ImmutableArray.CreateRange(declaredReferences);
        PermittedReferences = ImmutableArray.CreateRange(permittedReferences);
        GodotAllowed = godotAllowed;
    }

    /// <summary>Repository-relative path of the project file, with forward slashes.</summary>
    internal string ProjectPath { get; }

    /// <summary>The project's assembly-and-file name, without the extension.</summary>
    internal string Name => AcceptedArchitecture.ProjectName(ProjectPath);

    /// <summary>The exact project references this project declares today, sorted.</summary>
    internal ImmutableArray<string> DeclaredReferences { get; }

    /// <summary>Every project reference doc 115 § Accepted project boundary permits, sorted.</summary>
    internal ImmutableArray<string> PermittedReferences { get; }

    /// <summary>Whether this project may depend on Godot at all.</summary>
    internal bool GodotAllowed { get; }
}

/// <summary>
/// The accepted repository layout and project boundary, as data.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-001</c>). Authority:
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Accepted
/// project boundary, <c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Repository structure, <c>docs/technical/00-technical-foundation.md</c> § Language
/// boundary. Requirements: <c>TR-CTR-001</c>, <c>TR-BLD-006</c>, <c>TR-FND-001</c>,
/// <c>TR-FND-002</c>.
/// </para>
/// <para>
/// The boundary is data and the rules that read it are pure, which is what makes a
/// negative control possible: a test can hand <see cref="ArchitectureRules"/> a
/// synthetic graph carrying one forbidden edge and require that exact finding. A rule
/// that could only read the real repository could never be shown to fail, so it would
/// not be evidence of anything.
/// </para>
/// <para>
/// <c>build/verify-architecture.sh</c> asserts the same boundary from outside the
/// solution, reading MSBuild's own evaluation of every project so that an
/// SDK-injected package reference is included. Both remain: the script is what CI and
/// the <c>build</c> verb call, and the tests are what <c>test-fast</c> runs. They are
/// deliberately not one mechanism reading the other's output, because then a defect in
/// the shared reader would hide from both.
/// </para>
/// </remarks>
internal static class AcceptedArchitecture
{
    /// <summary>The Godot project, which is the only project allowed an engine dependency.</summary>
    internal const string GodotProject = "MechaMiner.Game";

    /// <summary>The single solution file that must reference exactly the accepted projects.</summary>
    internal const string SolutionPath = "MechaMiner.sln";

    private static readonly string[] PureProjects =
    {
        "MechaMiner.Content",
        "MechaMiner.Diagnostics",
        "MechaMiner.Persistence",
        "MechaMiner.Simulation",
    };

    private static readonly ImmutableArray<AcceptedProject> ProjectRows = ImmutableArray.Create(
        // "MechaMiner.Content ... .NET base libraries only ... No"
        new AcceptedProject(
            "src/MechaMiner.Content/MechaMiner.Content.csproj",
            declaredReferences: System.Array.Empty<string>(),
            permittedReferences: System.Array.Empty<string>(),
            godotAllowed: false),

        // "MechaMiner.Diagnostics ... .NET base libraries only ... No". A dependency
        // leaf on purpose, so every consumer can reference it without a cycle.
        new AcceptedProject(
            "src/MechaMiner.Diagnostics/MechaMiner.Diagnostics.csproj",
            declaredReferences: System.Array.Empty<string>(),
            permittedReferences: System.Array.Empty<string>(),
            godotAllowed: false),

        // "MechaMiner.Simulation ... MechaMiner.Content ... No"
        new AcceptedProject(
            "src/MechaMiner.Simulation/MechaMiner.Simulation.csproj",
            declaredReferences: new[] { "MechaMiner.Content" },
            permittedReferences: new[] { "MechaMiner.Content" },
            godotAllowed: false),

        // "MechaMiner.Persistence ... MechaMiner.Content, narrow immutable types from
        // MechaMiner.Simulation ... No". The Simulation edge is permitted but not yet
        // declared: no durable type crosses that boundary until PST-005 captures a run
        // recovery snapshot. That task adds the edge here in the same change.
        new AcceptedProject(
            "src/MechaMiner.Persistence/MechaMiner.Persistence.csproj",
            declaredReferences: new[] { "MechaMiner.Content" },
            permittedReferences: new[] { "MechaMiner.Content", "MechaMiner.Simulation" },
            godotAllowed: false),

        // "MechaMiner.Tools ... all pure projects ... No; it may launch the pinned Godot
        // executable as an external process"
        new AcceptedProject(
            "src/MechaMiner.Tools/MechaMiner.Tools.csproj",
            declaredReferences: PureProjects,
            permittedReferences: PureProjects,
            godotAllowed: false),

        // "MechaMiner.Game ... all pure projects plus Godot APIs ... Yes"
        new AcceptedProject(
            "game/MechaMiner.Game.csproj",
            declaredReferences: PureProjects,
            permittedReferences: PureProjects,
            godotAllowed: true),

        // "Tests mirror those projects." Each pure test project references its subject
        // only, so a test cannot reach a sibling domain through its own test assembly.
        new AcceptedProject(
            "tests/MechaMiner.Content.Tests/MechaMiner.Content.Tests.csproj",
            declaredReferences: new[] { "MechaMiner.Content" },
            permittedReferences: new[] { "MechaMiner.Content" },
            godotAllowed: false),
        new AcceptedProject(
            "tests/MechaMiner.Diagnostics.Tests/MechaMiner.Diagnostics.Tests.csproj",
            declaredReferences: new[] { "MechaMiner.Diagnostics" },
            permittedReferences: new[] { "MechaMiner.Diagnostics" },
            godotAllowed: false),
        new AcceptedProject(
            "tests/MechaMiner.Simulation.Tests/MechaMiner.Simulation.Tests.csproj",
            declaredReferences: new[] { "MechaMiner.Simulation" },
            permittedReferences: new[] { "MechaMiner.Simulation" },
            godotAllowed: false),
        new AcceptedProject(
            "tests/MechaMiner.Persistence.Tests/MechaMiner.Persistence.Tests.csproj",
            declaredReferences: new[] { "MechaMiner.Persistence" },
            permittedReferences: new[] { "MechaMiner.Persistence" },
            godotAllowed: false),

        // The tool's own audits and validators are tested here. It may reference the
        // tool host, which is not a pure project, because a test project is not
        // production code; doc 115 forbids the reverse, "Production projects never
        // depend on test projects".
        new AcceptedProject(
            "tests/MechaMiner.Tools.Tests/MechaMiner.Tools.Tests.csproj",
            declaredReferences: new[] { "MechaMiner.Tools" },
            permittedReferences: new[] { "MechaMiner.Tools" },
            godotAllowed: false),

        // "MechaMiner.Game.Tests ... engine integration". It drives the pinned Godot
        // executable as an external process and holds no Godot type, so game/ stays the
        // only Godot-referencing project.
        new AcceptedProject(
            "tests/MechaMiner.Game.Tests/MechaMiner.Game.Tests.csproj",
            declaredReferences: PureProjects,
            permittedReferences: PureProjects,
            godotAllowed: false));

    /// <summary>Every accepted project, in declaration order.</summary>
    internal static ImmutableArray<AcceptedProject> Projects => ProjectRows;

    /// <summary>
    /// The prescribed top-level ownership paths of doc 100 § Repository structure.
    /// </summary>
    /// <remarks>
    /// Doc 100: "Changing these top-level ownership directories requires updating the
    /// Component, Contract, and Schema Registry and architecture tests in the same
    /// task." This list is the architecture-test half of that sentence.
    /// </remarks>
    internal static ImmutableArray<string> RequiredPaths { get; } = ImmutableArray.Create(
        "MechaMiner.sln",
        "global.json",
        "Directory.Build.props",
        "Directory.Packages.props",
        "build.sh",
        "build.ps1",
        "game/project.godot",
        "game/MechaMiner.Game.csproj",
        "game/scenes",
        "game/shaders",
        "game/presentation",
        "src/MechaMiner.Content",
        "src/MechaMiner.Diagnostics",
        "src/MechaMiner.Persistence",
        "src/MechaMiner.Simulation",
        "src/MechaMiner.Tools",
        "tests/MechaMiner.Content.Tests",
        "tests/MechaMiner.Diagnostics.Tests",
        "tests/MechaMiner.Game.Tests",
        "tests/MechaMiner.Persistence.Tests",
        "tests/MechaMiner.Simulation.Tests",
        "tests/MechaMiner.Tools.Tests",
        "tests/verification",
        "content",
        // doc 40 § Accepted content repository layout. Both directories carry a .gitkeep,
        // the way FND-001 seeded every other empty accepted directory.
        "content/schemas",
        "content/player",
        "assets-source",
        "assets-runtime",
        "assets-manifest",
        "generated",
        "docs",
        "build");

    /// <summary>Finds an accepted project row by name, or null when the name is not accepted.</summary>
    internal static AcceptedProject? Find(string name)
    {
        foreach (AcceptedProject project in ProjectRows)
        {
            if (string.Equals(project.Name, name, System.StringComparison.Ordinal))
            {
                return project;
            }
        }

        return null;
    }

    /// <summary>Extracts the project name from a project path.</summary>
    internal static string ProjectName(string projectPath)
    {
        int lastSlash = projectPath.LastIndexOfAny(new[] { '/', '\\' });
        string fileName = lastSlash >= 0 ? projectPath[(lastSlash + 1)..] : projectPath;
        return fileName.EndsWith(".csproj", System.StringComparison.Ordinal)
            ? fileName[..^".csproj".Length]
            : fileName;
    }
}
