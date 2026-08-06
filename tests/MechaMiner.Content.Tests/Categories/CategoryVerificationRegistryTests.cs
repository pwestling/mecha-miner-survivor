using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// <c>tests/verification/DAT-002.json</c> and <c>DAT-003.json</c> cite only things that
/// exist.
/// </summary>
/// <remarks>
/// <para>
/// A citation to a file or heading that is not there is worse than no citation: it
/// sends the next reader to a document that does not say what they were told it says.
/// This matters more than usual on this repository, which has no CI - every gate is a
/// local run, so the written description of what a gate covers is doing work an
/// automated re-run would otherwise do.
/// </para>
/// <para>
/// Scope is deliberately narrow, and the same as the DAT-001 registry test's: the
/// structural validator for every <c>tests/verification/*.json</c> is owned by
/// <c>TASK-FND-009-002</c>, and both of these should fold into it when it lands.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CategoryVerificationRegistryTests
{
    private static IEnumerable<string> Packages => new[] { "DAT-002", "DAT-003" };

    [TestCaseSource(nameof(Packages))]
    public void EveryCitedSourceResolvesToARealFileAndHeading(string package)
    {
        using JsonDocument registry = Registry(package);
        List<string> unresolved = new();

        foreach (JsonElement entry in Entries(registry))
        {
            string id = entry.GetProperty("id").GetString()!;
            foreach (string property in new[] { "technicalSources", "gameplaySources" })
            {
                foreach (JsonElement citation in entry.GetProperty(property).EnumerateArray())
                {
                    string value = citation.GetString()!;
                    int hash = value.IndexOf('#', StringComparison.Ordinal);
                    string path = hash < 0 ? value : value[..hash];
                    string? anchor = hash < 0 ? null : value[(hash + 1)..];

                    string absolute = Path.Combine(TestArtifacts.RepositoryRoot, path);
                    if (!File.Exists(absolute))
                    {
                        unresolved.Add(id + ": " + value + " (file missing)");
                        continue;
                    }

                    if (anchor is not null && !HeadingAnchors(absolute).Contains(anchor))
                    {
                        unresolved.Add(id + ": " + value + " (heading missing)");
                    }
                }
            }
        }

        Assert.That(
            unresolved,
            Is.Empty,
            () => package + " has unresolved citations:" + Environment.NewLine
                + string.Join(Environment.NewLine, unresolved));
    }

    [TestCaseSource(nameof(Packages))]
    public void EntryIdsAreWellFormedUniqueAndNeverRenumbered(string package)
    {
        using JsonDocument registry = Registry(package);
        Regex pattern = new(
            @"\AVER-" + package + "-[0-9]{3}\\z",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        HashSet<string> seen = new(StringComparer.Ordinal);
        int previous = 0;

        Expect.Multiple(() =>
        {
            foreach (JsonElement entry in Entries(registry))
            {
                string id = entry.GetProperty("id").GetString()!;

                Assert.That(pattern.IsMatch(id), Is.True, id + " must be VER-" + package + "-###");
                Assert.That(seen.Add(id), Is.True, id + " is declared twice");

                int number = int.Parse(
                    id.AsSpan(id.Length - 3), NumberStyles.None, CultureInfo.InvariantCulture);
                Assert.That(
                    number,
                    Is.EqualTo(previous + 1),
                    id + " must continue the sequence; entries are never renumbered");
                previous = number;
            }
        });
    }

    [TestCaseSource(nameof(Packages))]
    public void EveryEntryCarriesTheFieldsDocNinetyOneRequires(string package)
    {
        using JsonDocument registry = Registry(package);

        Expect.Multiple(() =>
        {
            foreach (JsonElement entry in Entries(registry))
            {
                string id = entry.GetProperty("id").GetString()!;

                foreach (string required in new[]
                         {
                             "id", "summary", "task", "requirements", "technicalSources",
                             "gameplaySources", "selector", "fixtures", "scenarios",
                             "evidenceKinds", "platforms", "tier", "status",
                         })
                {
                    Assert.That(
                        entry.TryGetProperty(required, out _),
                        Is.True,
                        id + " is missing '" + required + "'");
                }

                Assert.That(
                    entry.GetProperty("requirements").GetArrayLength(),
                    Is.GreaterThan(0),
                    id + " must cite at least one requirement");
            }
        });
    }

    /// <summary>
    /// A summary states the route the check takes to its subject, not only the property
    /// it forbids. A description broader than the thing it describes reads as a stronger
    /// guarantee than it is, which on a repository with no CI is the whole of what a
    /// reader has to go on.
    /// </summary>
    [TestCaseSource(nameof(Packages))]
    public void EverySummaryStatesTheRouteAndNotOnlyTheRule(string package)
    {
        using JsonDocument registry = Registry(package);
        List<string> thin = new();

        foreach (JsonElement entry in Entries(registry))
        {
            string id = entry.GetProperty("id").GetString()!;
            string summary = entry.GetProperty("summary").GetString()!;

            if (summary.Length < 160)
            {
                thin.Add(id + ": too short to state a route (" + summary.Length + " characters)");
                continue;
            }

            if (!summary.Contains("route", StringComparison.Ordinal)
                && !summary.Contains("matched", StringComparison.Ordinal)
                && !summary.Contains("recomputed", StringComparison.Ordinal)
                && !summary.Contains("compared", StringComparison.Ordinal))
            {
                thin.Add(id + ": names no route to its subject");
            }
        }

        Assert.That(
            thin,
            Is.Empty,
            () => package + " entries that describe a rule without its route:"
                + Environment.NewLine + string.Join(Environment.NewLine, thin));
    }

    [TestCaseSource(nameof(Packages))]
    public void EveryNunitSelectorNamesATypeInThisAssembly(string package)
    {
        using JsonDocument registry = Registry(package);

        Expect.Multiple(() =>
        {
            foreach (JsonElement entry in Entries(registry))
            {
                JsonElement selector = entry.GetProperty("selector");
                if (selector.GetProperty("kind").GetString() != "nunit")
                {
                    continue;
                }

                string value = selector.GetProperty("value").GetString()!;
                string typeName = value.Contains('+', StringComparison.Ordinal)
                    ? value[..value.IndexOf('+', StringComparison.Ordinal)]
                    : value;

                Assert.That(
                    typeof(CategoryVerificationRegistryTests).Assembly.GetType(typeName),
                    Is.Not.Null,
                    entry.GetProperty("id").GetString() + " names '" + typeName
                        + "', which is not a type in this assembly");
            }
        });
    }

    [TestCaseSource(nameof(Packages))]
    public void EveryNamedFixturePathExists(string package)
    {
        using JsonDocument registry = Registry(package);
        List<string> missing = new();

        foreach (JsonElement entry in Entries(registry))
        {
            string id = entry.GetProperty("id").GetString()!;
            foreach (JsonElement fixture in entry.GetProperty("fixtures").EnumerateArray())
            {
                string path = fixture.GetString()!;
                string absolute = Path.Combine(TestArtifacts.RepositoryRoot, path);
                if (!File.Exists(absolute) && !Directory.Exists(absolute))
                {
                    missing.Add(id + ": " + path);
                }
            }
        }

        Assert.That(
            missing,
            Is.Empty,
            () => package + " names fixtures that are absent:" + Environment.NewLine
                + string.Join(Environment.NewLine, missing));
    }

    /// <summary>The negative control: the resolver must be able to fail.</summary>
    [Test]
    public void TheResolverRejectsAMissingFileAndAMissingHeading()
    {
        string realDocument = Path.Combine(
            TestArtifacts.RepositoryRoot,
            "docs", "technical", "40-content-data-and-validation.md");

        Expect.Multiple(() =>
        {
            Assert.That(
                File.Exists(Path.Combine(TestArtifacts.RepositoryRoot, "docs/does-not-exist.md")),
                Is.False,
                "a missing file must be detectable");
            Assert.That(
                HeadingAnchors(realDocument),
                Does.Contain("mining-sites"),
                "a real heading must resolve");
            Assert.That(
                HeadingAnchors(realDocument),
                Does.Not.Contain("no-such-heading-anywhere"),
                "a missing heading must not resolve");
        });
    }

    private static JsonDocument Registry(string package)
    {
        return JsonDocument.Parse(File.ReadAllBytes(Path.Combine(
            TestArtifacts.RepositoryRoot, "tests", "verification", package + ".json")));
    }

    private static IEnumerable<JsonElement> Entries(JsonDocument registry)
    {
        foreach (JsonElement entry in registry.RootElement.GetProperty("entries").EnumerateArray())
        {
            yield return entry;
        }
    }

    private static HashSet<string> HeadingAnchors(string path)
    {
        HashSet<string> anchors = new(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(path))
        {
            if (!line.StartsWith('#'))
            {
                continue;
            }

            string text = line.TrimStart('#').Trim()
                .Replace("`", string.Empty, StringComparison.Ordinal);
            StringBuilder anchor = new(text.Length);
            foreach (char character in text.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character) || character is '_' or '-')
                {
                    anchor.Append(character);
                }
                else if (character == ' ')
                {
                    anchor.Append('-');
                }
            }

            anchors.Add(anchor.ToString());
        }

        return anchors;
    }
}
