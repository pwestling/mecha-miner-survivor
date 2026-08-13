using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MechaMiner.Tests.Support;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// Reads the verification registries under <c>tests/verification/</c>.
/// </summary>
/// <remarks>
/// <para>
/// One reader, because there are several questions asked of these files - whether their
/// citations resolve (<see cref="VerificationRegistryTests"/> and
/// <see cref="Categories.CategoryVerificationRegistryTests"/>) and whether the fixture
/// corpus is claimed by them (<see cref="FixtureCorpusCoverageTests"/>) - and two copies
/// of "where a registry is and what shape it has" would drift the day one moves.
/// </para>
/// <para>
/// <see cref="Packages"/> is discovered from the directory rather than listed here. A
/// hand-maintained list of registries has the same defect as a hand-maintained list of
/// checks: the registry added next is walked by nobody, and nothing says so. That was the
/// actual state of this suite - two walks between them named DAT-001, DAT-002 and DAT-003,
/// and every other registry on disk was validated by nothing. How many that is follows from
/// <see cref="RegistriesOnDisk"/>, which
/// <see cref="VerificationRegistryTests.EveryRegistryOnDiskIsDiscoveredAndWalked"/> asserts,
/// and is deliberately not transcribed here: a count written into prose is gated by nothing
/// and goes stale the next time a registry is added.
/// </para>
/// </remarks>
internal static class VerificationRegistry
{
    /// <summary>
    /// How many registries are on disk, written out.
    /// </summary>
    /// <remarks>
    /// <b>A literal, and it has to be.</b> Every walk below is driven by
    /// <see cref="Packages"/>, and a walk over a set that silently shrank reports nothing
    /// wrong - a registry that stops being discovered (renamed to <c>.json.bak</c>, moved
    /// one directory up, dropped by a bad merge) simply stops being checked and every
    /// assertion still passes. A count derived from the same enumeration agrees with itself
    /// on that input, so the expected number is the one part of this that must not be
    /// computed. Changing it is a deliberate change to what this suite covers.
    /// </remarks>
    /// <remarks>
    /// The twenty-one at <c>715ef53</c> plus <c>DAT-006.json</c>, added at <c>59a4986</c>
    /// with the schema-versus-corpus divergence gate it records.
    /// </remarks>
    internal const int RegistriesOnDisk = 22;

    /// <summary>The directory holding every verification registry.</summary>
    internal static string DirectoryPath { get; } = Path.Combine(
        TestArtifacts.RepositoryRoot, "tests", "verification");

    /// <summary>
    /// Every registry package name on disk - the file name without its extension -
    /// discovered from <see cref="DirectoryPath"/> and ordered so a walk is reproducible.
    /// </summary>
    internal static IReadOnlyList<string> Packages { get; } =
        Directory.GetFiles(DirectoryPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>The absolute path of one registry.</summary>
    internal static string PathOf(string package)
    {
        return Path.Combine(DirectoryPath, package + ".json");
    }

    /// <summary>Opens one registry. The caller disposes it.</summary>
    internal static JsonDocument Open(string package)
    {
        return JsonDocument.Parse(File.ReadAllBytes(PathOf(package)));
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
    /// Every repository-relative fixture path a <b>DAT-001</b> entry cites that begins with
    /// <paramref name="prefix"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Duplicates are kept. Two entries citing one fixture is not a fault - a fixture can
    /// be evidence for two claims - and a caller that cares can deduplicate.
    /// </para>
    /// <para>
    /// DAT-001 alone, deliberately, and unlike the walks this is not extended to
    /// <see cref="Packages"/>. Its one caller counts the result against a committed literal
    /// (<c>FixtureCorpusCoverageTests.TheSchemaFixtureCountTheRegistryClaims</c>) which is a
    /// statement about what DAT-001 claims; widening the source would change that number's
    /// meaning without changing the number, which is the failure mode that literal exists to
    /// prevent. Whether the schema corpus should be claimable from any registry is a separate
    /// question from whether every registry is walked.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<string> CitedFixturesUnder(string prefix)
    {
        using JsonDocument registry = Open("DAT-001");
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
