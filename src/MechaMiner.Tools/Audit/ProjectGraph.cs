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

    /// <summary>
    /// The <c>Godot</c> namespace as a token in any position.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exact expression is also spelled out as <c>GODOT_TOKEN</c> in
    /// <c>build/verify-architecture.sh</c> § 6. <b>The two are deliberately two readers
    /// of the same rule, not a redundancy to be collapsed.</b> One is a C# rule over a
    /// <see cref="ProjectGraph"/> that a test can construct, and it runs in
    /// <c>test-fast</c>; the other is a text scan the shell gate runs with no build at
    /// all, so a defect in the C# audit assembly cannot hide the violation from both.
    /// Deleting either leaves the rule with one reader and no cross-check. If this
    /// expression changes, change it in both places, and keep
    /// <c>TheGodotImportRuleCatchesEveryWayOfNamingTheNamespace</c> passing, which is
    /// the control that measures what the expression actually catches.
    /// </para>
    /// <para>
    /// The predecessor was <c>^\s*using\s+Godot\s*(;|\.)</c>, which tested for one
    /// import spelling while the rule it feeds is named
    /// <see cref="ArchitectureRule.GodotTypeOutsideGame"/>. Five of six ways of naming
    /// the namespace evaded it: <c>using static Godot.GD;</c>,
    /// <c>global using Godot;</c>, <c>using GD = Godot.GD;</c>,
    /// <c>using GodotAlias = Godot;</c>, and a fully-qualified <c>Godot.GD.Print</c>
    /// with no <c>using</c> at all. Matching <c>Godot[.]</c> alone is not sufficient
    /// either: the alias form <c>using GodotAlias = Godot;</c> has no dot after the
    /// token. The trailing class is therefore "any non-identifier character or end of
    /// line", and the leading class excludes <c>.</c> and identifier characters so that
    /// <c>MechaMiner.GodotLike</c> and <c>NotGodotish</c> do not match.
    /// </para>
    /// </remarks>
    internal const string GodotNamespaceToken = "(^|[^A-Za-z0-9_.])Godot([^A-Za-z0-9_]|$)";

    /// <summary>A <c>using</c> directive line, including the <c>global using</c> form.</summary>
    private const string UsingDirectiveLine = "^\\s*(global\\s+)?using\\s";

    /// <summary>
    /// <c>Godot</c> in namespace-qualifier position, which is the only place the token
    /// can appear outside a <c>using</c> directive and still name an engine type.
    /// </summary>
    private const string GodotQualifier = "(^|[^A-Za-z0-9_.])Godot\\s*\\.";

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

                if (NamesGodotNamespace(File.ReadAllText(file)))
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

    /// <summary>
    /// Whether a C# source file names the <c>Godot</c> namespace, in any of the ways
    /// C# allows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two contexts, both decided by <see cref="GodotNamespaceToken"/>:
    /// </para>
    /// <list type="number">
    /// <item>
    /// the token on a <c>using</c> directive line, which covers <c>using Godot;</c>,
    /// <c>global using Godot;</c>, <c>using static Godot.GD;</c>,
    /// <c>using GD = Godot.GD;</c> and <c>using GodotAlias = Godot;</c>; and
    /// </item>
    /// <item>
    /// the token in namespace-qualifier position anywhere at all, which covers a
    /// fully-qualified <c>Godot.GD.Print</c> with no import.
    /// </item>
    /// </list>
    /// <para>
    /// The two contexts exist because the token alone, matched against raw file text,
    /// is not a usable rule on this repository: 96 lines under <c>src/</c> and
    /// <c>tests/</c> spell <c>Godot</c> as a bare English word in a comment or a
    /// diagnostic string, and most of them are prose explaining why this very boundary
    /// exists. Rewording those to satisfy a text scan would make the code worse and
    /// prove nothing. Comments and string literals are therefore removed before the
    /// scan, and the one remaining collision — <c>ToolchainPins.Godot</c>, a property
    /// whose name is bound to the <c>godot</c> key of <c>build/toolchain.json</c> and
    /// is not a namespace reference — is excluded by requiring qualifier position
    /// rather than by an allowlist entry, because an allowlist entry is a hole and a
    /// context requirement is a rule.
    /// </para>
    /// <para>
    /// The shell reader in <c>build/verify-architecture.sh</c> § 6 asks the same two
    /// questions with the same token, over a cruder <c>sed</c> stripper. It can lose a
    /// token to an awkwardly quoted line where this reader would not; it cannot invent
    /// one. The C# reader is the precise one, the shell reader is the one that needs no
    /// build, and § 6 carries the same six-form control so a divergence shows up as a
    /// failing control rather than as a silent disagreement.
    /// </para>
    /// </remarks>
    internal static bool NamesGodotNamespace(string sourceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);

        foreach (string line in StripCommentsAndStringLiterals(sourceText)
            .Split('\n'))
        {
            if (Regex.IsMatch(line, GodotQualifier, RegexOptions.None, TimeSpan.FromSeconds(10)))
            {
                return true;
            }

            if (Regex.IsMatch(line, UsingDirectiveLine, RegexOptions.None, TimeSpan.FromSeconds(10))
                && Regex.IsMatch(line, GodotNamespaceToken, RegexOptions.None, TimeSpan.FromSeconds(10)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Replaces comment and string-literal content with spaces, preserving line
    /// structure so the caller can still reason per line.
    /// </summary>
    /// <remarks>
    /// A character scanner rather than a regex, because the cases that matter are
    /// exactly the ones a regex gets wrong: an escaped quote inside a literal
    /// (<c>"\""</c>), a <c>//</c> inside a literal (<c>"https://..."</c>), and a quote
    /// inside a comment. Handles line comments, block comments, ordinary literals,
    /// verbatim literals and raw string literals. Character literals are left alone;
    /// a <c>char</c> cannot hold an identifier.
    /// </remarks>
    private static string StripCommentsAndStringLiterals(string sourceText)
    {
        char[] output = sourceText.ToCharArray();
        int index = 0;

        void Blank(int from, int toExclusive)
        {
            for (int i = from; i < toExclusive && i < output.Length; i++)
            {
                if (output[i] != '\n' && output[i] != '\r')
                {
                    output[i] = ' ';
                }
            }
        }

        while (index < sourceText.Length)
        {
            char current = sourceText[index];

            if (current == '/' && index + 1 < sourceText.Length && sourceText[index + 1] == '/')
            {
                int end = sourceText.IndexOf('\n', index);
                end = end < 0 ? sourceText.Length : end;
                Blank(index, end);
                index = end;
                continue;
            }

            if (current == '/' && index + 1 < sourceText.Length && sourceText[index + 1] == '*')
            {
                int end = sourceText.IndexOf("*/", index + 2, StringComparison.Ordinal);
                end = end < 0 ? sourceText.Length : end + 2;
                Blank(index, end);
                index = end;
                continue;
            }

            if (current == '"')
            {
                index = BlankLiteral(sourceText, index, Blank);
                continue;
            }

            if (current == '@' && index + 1 < sourceText.Length && sourceText[index + 1] == '"')
            {
                index = BlankLiteral(sourceText, index, Blank);
                continue;
            }

            index++;
        }

        return new string(output);
    }

    /// <summary>Blanks one string literal starting at <paramref name="start"/> and returns the index after it.</summary>
    private static int BlankLiteral(string text, int start, Action<int, int> blank)
    {
        int index = start;
        bool verbatim = text[index] == '@';
        if (verbatim)
        {
            index++;
        }

        // A raw string literal opens with three or more quotes and closes with the same
        // count, and backslash is not an escape inside it.
        int quotes = 0;
        while (index < text.Length && text[index] == '"')
        {
            quotes++;
            index++;
        }

        if (quotes >= 3)
        {
            string terminator = new('"', quotes);
            int rawEnd = text.IndexOf(terminator, index, StringComparison.Ordinal);
            rawEnd = rawEnd < 0 ? text.Length : rawEnd + quotes;
            blank(start, rawEnd);
            return rawEnd;
        }

        if (quotes == 2)
        {
            // An empty literal: both quotes were consumed.
            blank(start, index);
            return index;
        }

        while (index < text.Length)
        {
            char current = text[index];
            if (!verbatim && current == '\\' && index + 1 < text.Length)
            {
                index += 2;
                continue;
            }

            if (current == '"')
            {
                if (verbatim && index + 1 < text.Length && text[index + 1] == '"')
                {
                    index += 2;
                    continue;
                }

                index++;
                break;
            }

            if (!verbatim && current == '\n')
            {
                // An unterminated ordinary literal cannot span a line; stop rather than
                // swallowing the rest of the file.
                break;
            }

            index++;
        }

        blank(start, index);
        return index;
    }
}
