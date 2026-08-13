using System;
using System.Collections.Generic;
using System.IO;

namespace MechaMiner.Tools.Audit;

/// <summary>One text document the validator reads.</summary>
internal sealed class RegistryDocument
{
    internal RegistryDocument(string path, string text)
    {
        Path = path;
        Text = text;
    }

    /// <summary>Repository-relative path with forward slashes.</summary>
    internal string Path { get; }

    /// <summary>The document's full text.</summary>
    internal string Text { get; }

    /// <summary>The file name without directories.</summary>
    internal string FileName
    {
        get
        {
            int slash = Path.LastIndexOf('/');
            return slash < 0 ? Path : Path[(slash + 1)..];
        }
    }
}

/// <summary>
/// Everything the registry validator reads, as data.
/// </summary>
/// <remarks>
/// <para>
/// Owner: <c>FND-009</c> (<c>TASK-FND-009-002</c>).
/// </para>
/// <para>
/// The validator takes its inputs as a value rather than reading the repository itself,
/// for the same reason the architecture rules do: <c>TASK-FND-009-002</c>'s completion
/// gate is "missing, duplicate, dangling, and malformed fixtures fail", and a validator
/// that could only read the real tree could never be shown to fail on any of those four
/// classes. The deliberately invalid fixtures live under <c>build/policy-fixtures/</c>,
/// outside the solution, because the repository policy keeps invalid fixtures out of
/// production projects.
/// </para>
/// <para>
/// <see cref="ExistingPaths"/> exists so link resolution can be answered from the same
/// value. A fixture declares which paths it pretends exist, so a broken-link control does
/// not depend on the fixture tree mirroring the real repository.
/// </para>
/// <para>
/// <see cref="Tests"/> is the same idea for <c>nunit</c> selector resolution. The real
/// source set carries what the NUnit harness actually discovered; a fixture declares its
/// own inventory in <c>discovered-tests.txt</c>. Both are values, so a control can supply
/// an empty inventory and require the failure that produces.
/// </para>
/// </remarks>
internal sealed class RegistrySources
{
    private readonly List<RegistryDocument> _documents = new();
    private readonly List<RegistryDocument> _verificationRegistries = new();
    private readonly HashSet<string> _existingPaths = new(StringComparer.Ordinal);
    private TestInventory _tests = TestInventory.Nothing("no test inventory was supplied to this source set");

    /// <summary>Markdown documents scanned for definitions, references, headings, and links.</summary>
    internal IReadOnlyList<RegistryDocument> Documents => _documents;

    /// <summary><c>tests/verification/*.json</c> registry documents.</summary>
    internal IReadOnlyList<RegistryDocument> VerificationRegistries => _verificationRegistries;

    /// <summary>Repository-relative paths that exist, for link resolution.</summary>
    internal IReadOnlyCollection<string> ExistingPaths => _existingPaths;

    /// <summary>The tests the harness discovered, for <c>nunit</c> selector resolution.</summary>
    internal TestInventory Tests => _tests;

    /// <summary>Builds an empty source set for a test to populate.</summary>
    internal static RegistrySources Empty()
    {
        return new RegistrySources();
    }

    /// <summary>Reads the real repository.</summary>
    internal static RegistrySources ReadFromDisk(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        RegistrySources sources = new();

        // Every tracked path, so a relative link can be resolved without a second walk.
        foreach (string file in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories))
        {
            string relative = Relative(repositoryRoot, file);
            if (IsIgnoredPath(relative))
            {
                continue;
            }

            sources._existingPaths.Add(relative);
        }

        foreach (string directory in Directory.EnumerateDirectories(
            repositoryRoot,
            "*",
            SearchOption.AllDirectories))
        {
            string relative = Relative(repositoryRoot, directory);
            if (!IsIgnoredPath(relative))
            {
                sources._existingPaths.Add(relative);
            }
        }

        // Specification prose: the gameplay root plus every technical document, and the
        // agent instructions, which cite work packages and task IDs directly.
        foreach (string file in Directory.EnumerateFiles(
            Path.Combine(repositoryRoot, "docs"),
            "*.md",
            SearchOption.AllDirectories))
        {
            string relative = Relative(repositoryRoot, file);
            sources._documents.Add(new RegistryDocument(relative, File.ReadAllText(file)));
        }

        string agents = Path.Combine(repositoryRoot, "AGENTS.md");
        if (File.Exists(agents))
        {
            sources._documents.Add(new RegistryDocument("AGENTS.md", File.ReadAllText(agents)));
        }

        string verificationDirectory = Path.Combine(repositoryRoot, "tests", "verification");
        if (Directory.Exists(verificationDirectory))
        {
            foreach (string file in Directory.EnumerateFiles(verificationDirectory, "*.json"))
            {
                string relative = Relative(repositoryRoot, file);
                sources._verificationRegistries.Add(new RegistryDocument(relative, File.ReadAllText(file)));
            }
        }

        sources._tests = TestInventory.Discover(repositoryRoot);
        sources.Sort();
        return sources;
    }

    /// <summary>
    /// Reads one fixture directory. <c>.md</c> files become documents, <c>.json</c> files
    /// become verification registries, an optional <c>existing-paths.txt</c> declares
    /// which repository paths the fixture pretends exist, and an optional
    /// <c>discovered-tests.txt</c> declares which tests the fixture pretends the harness
    /// discovered.
    /// </summary>
    internal static RegistrySources ReadFixture(string fixtureDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureDirectory);
        RegistrySources sources = new();

        foreach (string file in Directory.EnumerateFiles(fixtureDirectory, "*.md"))
        {
            string name = Path.GetFileName(file);
            sources._documents.Add(new RegistryDocument("docs/technical/" + name, File.ReadAllText(file)));
            sources._existingPaths.Add("docs/technical/" + name);
        }

        foreach (string file in Directory.EnumerateFiles(fixtureDirectory, "*.json"))
        {
            string name = Path.GetFileName(file);
            sources._verificationRegistries.Add(
                new RegistryDocument("tests/verification/" + name, File.ReadAllText(file)));
            sources._existingPaths.Add("tests/verification/" + name);
        }

        string declared = Path.Combine(fixtureDirectory, "existing-paths.txt");
        if (File.Exists(declared))
        {
            foreach (string line in File.ReadAllLines(declared))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0 && !trimmed.StartsWith('#'))
                {
                    sources._existingPaths.Add(trimmed);
                }
            }
        }

        string discovered = Path.Combine(fixtureDirectory, "discovered-tests.txt");
        if (File.Exists(discovered))
        {
            List<string> names = new();
            foreach (string line in File.ReadAllLines(discovered))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0 && !trimmed.StartsWith('#'))
                {
                    names.Add(trimmed);
                }
            }

            sources._tests = TestInventory.Of(names);
        }

        sources.Sort();
        return sources;
    }

    /// <summary>Adds a document, for a test that constructs sources inline.</summary>
    internal RegistrySources WithDocument(string path, string text)
    {
        _documents.Add(new RegistryDocument(path, text));
        _existingPaths.Add(path);
        return this;
    }

    /// <summary>Adds a verification registry document, for a test.</summary>
    internal RegistrySources WithVerificationRegistry(string path, string json)
    {
        _verificationRegistries.Add(new RegistryDocument(path, json));
        _existingPaths.Add(path);
        return this;
    }

    /// <summary>
    /// Declares the test inventory <c>nunit</c> selectors resolve against, for a test that
    /// constructs sources inline.
    /// </summary>
    internal RegistrySources WithTests(TestInventory tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        _tests = tests;
        return this;
    }

    /// <summary>Whether <paramref name="repositoryRelativePath"/> exists in this source set.</summary>
    internal bool PathExists(string repositoryRelativePath)
    {
        return _existingPaths.Contains(repositoryRelativePath);
    }

    /// <summary>Finds a document by repository-relative path, or null.</summary>
    internal RegistryDocument? FindDocument(string repositoryRelativePath)
    {
        foreach (RegistryDocument document in _documents)
        {
            if (string.Equals(document.Path, repositoryRelativePath, StringComparison.Ordinal))
            {
                return document;
            }
        }

        return null;
    }

    private void Sort()
    {
        _documents.Sort(static (left, right) => string.CompareOrdinal(left.Path, right.Path));
        _verificationRegistries.Sort(static (left, right) => string.CompareOrdinal(left.Path, right.Path));
    }

    private static string Relative(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('\\', '/');
    }

    private static bool IsIgnoredPath(string relative)
    {
        return relative.StartsWith(".git/", StringComparison.Ordinal)
            || relative.StartsWith("artifacts/", StringComparison.Ordinal)
            || relative.Contains("/obj/", StringComparison.Ordinal)
            || relative.Contains("/bin/", StringComparison.Ordinal)
            || relative.Contains("/.godot/", StringComparison.Ordinal);
    }
}
