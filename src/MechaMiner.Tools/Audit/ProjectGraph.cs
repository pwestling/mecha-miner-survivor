using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MechaMiner.Tools.Audit;

/// <summary>One project as the repository actually declares it.</summary>
internal sealed class ObservedProject
{
    /// <summary>Repository-relative project path with forward slashes.</summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>The project's name without the extension.</summary>
    public string Name => AcceptedArchitecture.ProjectName(ProjectPath);

    /// <summary>Project references declared in the project file, by name, sorted.</summary>
    public List<string> ProjectReferences { get; } = new();

    /// <summary>Godot dependency evidence: SDK attribute, package references, and lock entries.</summary>
    public List<string> GodotEvidence { get; } = new();
}

/// <summary>
/// The dependency graph as observed on disk, or as constructed by a test.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-001</c>).
/// </para>
/// <para>
/// The graph is a plain value so a test can build a synthetic one. That is the whole
/// point: <c>TASK-FND-009-001</c>'s completion gate is "each forbidden synthetic edge
/// fails", and an assertion that can only read the real repository can never be shown
/// to fail on any edge, so it would prove nothing about the rule.
/// </para>
/// <para>
/// <see cref="ReadFromDisk"/> reads the committed project files and lock files
/// textually rather than invoking MSBuild. That is a deliberate split of duty:
/// <c>build/verify-architecture.sh</c> asks MSBuild to evaluate every project, which
/// catches an SDK-injected package reference that no project file mentions, and it is
/// slow enough that <c>test-fast</c> should not pay for it on every run. Both gates
/// stay, and neither consumes the other's output, so one reader's defect cannot hide
/// from both.
/// </para>
/// </remarks>
internal sealed class ProjectGraph
{
    private readonly List<ObservedProject> _projects = new();
    private readonly List<string> _solutionProjectPaths = new();
    private readonly List<string> _missingPaths = new();
    private readonly List<string> _godotImportsOutsideGame = new();
    private readonly List<string> _gdScriptFiles = new();

    /// <summary>Every project the graph knows about.</summary>
    internal IReadOnlyList<ObservedProject> Projects => _projects;

    /// <summary>Project paths the solution file references, with forward slashes, sorted.</summary>
    internal IReadOnlyList<string> SolutionProjectPaths => _solutionProjectPaths;

    /// <summary>Prescribed layout paths that do not exist.</summary>
    internal IReadOnlyList<string> MissingPaths => _missingPaths;

    /// <summary>Files outside <c>game/</c> that import the <c>Godot</c> namespace.</summary>
    internal IReadOnlyList<string> GodotImportsOutsideGame => _godotImportsOutsideGame;

    /// <summary>Tracked GDScript files, which are forbidden outright.</summary>
    internal IReadOnlyList<string> GdScriptFiles => _gdScriptFiles;

    /// <summary>Builds an empty graph for a test to populate.</summary>
    internal static ProjectGraph Empty()
    {
        return new ProjectGraph();
    }

    /// <summary>
    /// Builds the graph the accepted boundary describes, so a test can start from a
    /// compliant graph and inject exactly one violation.
    /// </summary>
    internal static ProjectGraph FromAcceptedBoundary()
    {
        ProjectGraph graph = new();
        foreach (AcceptedProject accepted in AcceptedArchitecture.Projects)
        {
            ObservedProject observed = new() { ProjectPath = accepted.ProjectPath };
            observed.ProjectReferences.AddRange(accepted.DeclaredReferences);
            if (accepted.GodotAllowed)
            {
                observed.GodotEvidence.Add("Sdk=Godot.NET.Sdk/4.7.1");
                observed.GodotEvidence.Add("lock:GodotSharp");
            }

            graph._projects.Add(observed);
            graph._solutionProjectPaths.Add(accepted.ProjectPath);
        }

        graph._solutionProjectPaths.Sort(StringComparer.Ordinal);
        return graph;
    }

    /// <summary>Reads the graph from the repository at <paramref name="repositoryRoot"/>.</summary>
    internal static ProjectGraph ReadFromDisk(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ProjectGraph graph = new();

        graph.ReadSolution(repositoryRoot);
        graph.ReadProjects(repositoryRoot);
        graph.ReadRequiredPaths(repositoryRoot);
        graph.ReadSourceScans(repositoryRoot);
        return graph;
    }

    /// <summary>Finds an observed project by name, or null.</summary>
    internal ObservedProject? Find(string name)
    {
        foreach (ObservedProject project in _projects)
        {
            if (string.Equals(project.Name, name, StringComparison.Ordinal))
            {
                return project;
            }
        }

        return null;
    }

    /// <summary>Adds a synthetic project reference edge, for a negative control.</summary>
    internal ProjectGraph WithReference(string from, string to)
    {
        ObservedProject project = Find(from)
            ?? throw new InvalidOperationException("unknown project in synthetic graph: " + from);
        project.ProjectReferences.Add(to);
        project.ProjectReferences.Sort(StringComparer.Ordinal);
        return this;
    }

    /// <summary>Removes a project reference edge, for a negative control.</summary>
    internal ProjectGraph WithoutReference(string from, string to)
    {
        ObservedProject project = Find(from)
            ?? throw new InvalidOperationException("unknown project in synthetic graph: " + from);
        project.ProjectReferences.Remove(to);
        return this;
    }

    /// <summary>Adds synthetic Godot dependency evidence, for a negative control.</summary>
    internal ProjectGraph WithGodotEvidence(string projectName, string evidence)
    {
        ObservedProject project = Find(projectName)
            ?? throw new InvalidOperationException("unknown project in synthetic graph: " + projectName);
        project.GodotEvidence.Add(evidence);
        return this;
    }

    /// <summary>Removes all Godot dependency evidence, for a negative control.</summary>
    internal ProjectGraph WithoutGodotEvidence(string projectName)
    {
        ObservedProject project = Find(projectName)
            ?? throw new InvalidOperationException("unknown project in synthetic graph: " + projectName);
        project.GodotEvidence.Clear();
        return this;
    }

    /// <summary>Records a synthetic Godot import outside <c>game/</c>, for a negative control.</summary>
    internal ProjectGraph WithGodotImportOutsideGame(string filePath)
    {
        _godotImportsOutsideGame.Add(filePath);
        return this;
    }

    /// <summary>Records a synthetic GDScript file, for a negative control.</summary>
    internal ProjectGraph WithGdScript(string filePath)
    {
        _gdScriptFiles.Add(filePath);
        return this;
    }

    /// <summary>Records a synthetic missing layout path, for a negative control.</summary>
    internal ProjectGraph WithMissingPath(string path)
    {
        _missingPaths.Add(path);
        return this;
    }

    /// <summary>Removes a project from the solution listing, for a negative control.</summary>
    internal ProjectGraph WithoutSolutionEntry(string projectPath)
    {
        _solutionProjectPaths.Remove(projectPath);
        return this;
    }

    /// <summary>Adds an unexpected project to the solution listing, for a negative control.</summary>
    internal ProjectGraph WithSolutionEntry(string projectPath)
    {
        _solutionProjectPaths.Add(projectPath);
        _solutionProjectPaths.Sort(StringComparer.Ordinal);
        return this;
    }

    private void ReadSolution(string repositoryRoot)
    {
        string solutionPath = Path.Combine(repositoryRoot, AcceptedArchitecture.SolutionPath);
        if (!File.Exists(solutionPath))
        {
            return;
        }

        foreach (Match match in Regex.Matches(
            File.ReadAllText(solutionPath),
            "^Project\\(\"\\{[^}]+\\}\"\\)\\s*=\\s*\"[^\"]+\",\\s*\"([^\"]+\\.csproj)\"",
            RegexOptions.Multiline,
            TimeSpan.FromSeconds(10)))
        {
            _solutionProjectPaths.Add(match.Groups[1].Value.Replace('\\', '/'));
        }

        _solutionProjectPaths.Sort(StringComparer.Ordinal);
    }

    private void ReadProjects(string repositoryRoot)
    {
        foreach (string relative in EnumerateProjectFiles(repositoryRoot))
        {
            string absolute = Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            string text = File.ReadAllText(absolute);
            ObservedProject project = new() { ProjectPath = relative };

            foreach (Match match in Regex.Matches(
                text,
                "<ProjectReference\\s+Include=\"([^\"]+)\"",
                RegexOptions.None,
                TimeSpan.FromSeconds(10)))
            {
                project.ProjectReferences.Add(AcceptedArchitecture.ProjectName(match.Groups[1].Value));
            }

            project.ProjectReferences.Sort(StringComparer.Ordinal);

            Match sdk = Regex.Match(
                text,
                "Sdk=\"([^\"]*Godot[^\"]*)\"",
                RegexOptions.None,
                TimeSpan.FromSeconds(10));
            if (sdk.Success)
            {
                project.GodotEvidence.Add("Sdk=" + sdk.Groups[1].Value);
            }

            foreach (Match match in Regex.Matches(
                text,
                "<PackageReference\\s+Include=\"(Godot[^\"]*)\"",
                RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(10)))
            {
                project.GodotEvidence.Add("PackageReference=" + match.Groups[1].Value);
            }

            string lockFile = Path.Combine(Path.GetDirectoryName(absolute)!, "packages.lock.json");
            if (File.Exists(lockFile))
            {
                foreach (Match match in Regex.Matches(
                    File.ReadAllText(lockFile),
                    "\"(Godot[A-Za-z.]*)\"\\s*:",
                    RegexOptions.None,
                    TimeSpan.FromSeconds(10)))
                {
                    string entry = "lock:" + match.Groups[1].Value;
                    if (!project.GodotEvidence.Contains(entry))
                    {
                        project.GodotEvidence.Add(entry);
                    }
                }
            }

            project.GodotEvidence.Sort(StringComparer.Ordinal);
            _projects.Add(project);
        }
    }

    private static List<string> EnumerateProjectFiles(string repositoryRoot)
    {
        List<string> found = new();
        foreach (string directory in new[] { "src", "tests", "game" })
        {
            string absolute = Path.Combine(repositoryRoot, directory);
            if (!Directory.Exists(absolute))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(absolute, "*.csproj", SearchOption.AllDirectories))
            {
                found.Add(Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/'));
            }
        }

        found.Sort(StringComparer.Ordinal);
        return found;
    }

    private void ReadRequiredPaths(string repositoryRoot)
    {
        foreach (string path in AcceptedArchitecture.RequiredPaths)
        {
            string absolute = Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute) && !Directory.Exists(absolute))
            {
                _missingPaths.Add(path);
            }
        }
    }

    private void ReadSourceScans(string repositoryRoot)
    {
        foreach (string directory in new[] { "src", "tests" })
        {
            string absolute = Path.Combine(repositoryRoot, directory);
            if (!Directory.Exists(absolute))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(absolute, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');

                // Compiler intermediates and build output are not source. bin/ and obj/
                // are gitignored, and a linked Godot type would appear in the real
                // source file that produced them anyway.
                if (relative.Contains("/obj/", StringComparison.Ordinal)
                    || relative.Contains("/bin/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (Regex.IsMatch(
                    File.ReadAllText(file),
                    "^\\s*using\\s+Godot\\s*(;|\\.)",
                    RegexOptions.Multiline,
                    TimeSpan.FromSeconds(10)))
                {
                    _godotImportsOutsideGame.Add(relative);
                }
            }
        }

        foreach (string file in Directory.EnumerateFiles(repositoryRoot, "*.gd", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
            if (relative.StartsWith(".git/", StringComparison.Ordinal)
                || relative.Contains("/.godot/", StringComparison.Ordinal)
                || relative.StartsWith("artifacts/", StringComparison.Ordinal))
            {
                continue;
            }

            _gdScriptFiles.Add(relative);
        }

        _godotImportsOutsideGame.Sort(StringComparer.Ordinal);
        _gdScriptFiles.Sort(StringComparer.Ordinal);
    }
}
