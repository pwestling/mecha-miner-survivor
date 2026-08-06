using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MechaMiner.Tests.Support;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// Reads <c>tests/verification/DAT-001.json</c>.
/// </summary>
/// <remarks>
/// One reader, because there are now two questions asked of this file - whether its
/// citations resolve (<see cref="VerificationRegistryTests"/>) and whether the fixture
/// corpus is claimed by them (<see cref="FixtureCorpusCoverageTests"/>) - and two copies
/// of "where the registry is and what shape it has" would drift the day the registry
/// moves.
/// </remarks>
internal static class VerificationRegistry
{
    /// <summary>The registry's absolute path.</summary>
    internal static string AbsolutePath { get; } = Path.Combine(
        TestArtifacts.RepositoryRoot, "tests", "verification", "DAT-001.json");

    /// <summary>Opens the registry. The caller disposes it.</summary>
    internal static JsonDocument Open()
    {
        return JsonDocument.Parse(File.ReadAllBytes(AbsolutePath));
    }

    /// <summary>Every entry of an opened registry.</summary>
    internal static IEnumerable<JsonElement> Entries(JsonDocument registry)
    {
        foreach (JsonElement entry in registry.RootElement.GetProperty("entries").EnumerateArray())
        {
            yield return entry;
        }
    }

    /// <summary>
    /// Every repository-relative fixture path any entry cites that begins with
    /// <paramref name="prefix"/>.
    /// </summary>
    /// <remarks>
    /// Duplicates are kept. Two entries citing one fixture is not a fault - a fixture can
    /// be evidence for two claims - and a caller that cares can deduplicate.
    /// </remarks>
    internal static IReadOnlyList<string> CitedFixturesUnder(string prefix)
    {
        using JsonDocument registry = Open();
        List<string> cited = new();

        foreach (JsonElement entry in Entries(registry))
        {
            foreach (JsonElement fixture in entry.GetProperty("fixtures").EnumerateArray())
            {
                string path = fixture.GetString()!;
                if (path.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    cited.Add(path);
                }
            }
        }

        return cited;
    }
}
