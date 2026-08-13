using System;
using System.IO;
using NUnit.Framework;

namespace MechaMiner.Tests.Support;

/// <summary>
/// Resolves the repository paths a test writes evidence into.
/// </summary>
/// <remarks>
/// Failure evidence goes beneath <c>artifacts/</c>, which
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Repository structure defines as "ignored local outputs". Golden files are read
/// from and written to the test project's own source directory, because a golden is
/// a reviewable committed artifact, not build output.
/// </remarks>
internal static class TestArtifacts
{
    /// <summary>
    /// The absolute repository root, found by walking up from the test assembly's
    /// directory to the directory holding <c>MechaMiner.sln</c>.
    /// </summary>
    internal static string RepositoryRoot { get; } = FindUpwards(
        TestContext.CurrentContext.TestDirectory,
        directory => File.Exists(Path.Combine(directory, "MechaMiner.sln")),
        "MechaMiner.sln");

    /// <summary>
    /// The absolute source directory of the running test project, found by walking
    /// up from the test assembly's directory to the directory holding its
    /// <c>.csproj</c>.
    /// </summary>
    internal static string TestProjectDirectory { get; } = FindUpwards(
        TestContext.CurrentContext.TestDirectory,
        directory => Directory.GetFiles(directory, "*.csproj").Length > 0,
        "a .csproj file");

    /// <summary>
    /// Creates and returns the directory a failing case preserves its minimized
    /// input and reproduction description in.
    /// </summary>
    internal static string FailureDirectory(string caseName, int seed)
    {
        string directory = Path.Combine(
            RepositoryRoot,
            "artifacts",
            "test-failures",
            Sanitize(caseName),
            "seed-" + seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Creates and returns the directory a golden mismatch writes its diff into.</summary>
    internal static string GoldenDiffDirectory(string goldenName)
    {
        string directory = Path.Combine(RepositoryRoot, "artifacts", "goldens", Sanitize(goldenName));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Returns <paramref name="absolutePath"/> relative to the repository root, with forward slashes.</summary>
    internal static string Relative(string absolutePath)
    {
        return Path.GetRelativePath(RepositoryRoot, absolutePath).Replace('\\', '/');
    }

    private static string FindUpwards(string start, Func<string, bool> predicate, string what)
    {
        DirectoryInfo? current = new(start);
        while (current is not null)
        {
            if (predicate(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "could not find " + what + " above " + start
            + ". The test harness resolves repository paths by walking up from the test assembly "
            + "directory; running tests from a copied output directory is not supported.");
    }

    private static string Sanitize(string value)
    {
        char[] characters = value.ToCharArray();
        for (int index = 0; index < characters.Length; index++)
        {
            if (!char.IsLetterOrDigit(characters[index]) && characters[index] != '-')
            {
                characters[index] = '-';
            }
        }

        return new string(characters);
    }
}
