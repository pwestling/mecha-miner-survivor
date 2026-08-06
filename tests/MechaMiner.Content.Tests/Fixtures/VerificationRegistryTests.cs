using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// <c>tests/verification/DAT-001.json</c> cites only things that exist.
/// </summary>
/// <remarks>
/// <para>
/// This exists because four gameplay-source paths in this registry were guessed rather
/// than resolved, and three of the four named files that do not exist. A citation to a
/// file or heading that is not there is worse than no citation: it sends the next reader
/// to a document that does not say what they were told it says.
/// </para>
/// <para>
/// Scope is deliberately narrow. The <em>structural</em> validator for every
/// <c>tests/verification/*.json</c> is owned by <c>TASK-FND-009-002</c>; this checks only
/// DAT-001's own file, and only that its citations resolve and its IDs are well formed.
/// It is not a competing implementation of that validator and should be folded into it
/// when it lands.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class VerificationRegistryTests
{
    private static readonly Regex EntryId = new(
        @"\AVER-DAT-001-[0-9]{3}\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static JsonDocument Registry()
    {
        return JsonDocument.Parse(File.ReadAllBytes(
            Path.Combine(TestArtifacts.RepositoryRoot, "tests", "verification", "DAT-001.json")));
    }

    private static IEnumerable<JsonElement> Entries(JsonDocument registry)
    {
        foreach (JsonElement entry in registry.RootElement.GetProperty("entries").EnumerateArray())
        {
            yield return entry;
        }
    }

    /// <summary>
    /// Every cited file exists and, where a citation names a heading anchor, that heading
    /// exists in the cited file.
    /// </summary>
    [Test]
    public void EveryCitedSourceResolvesToARealFileAndHeading()
    {
        using JsonDocument registry = Registry();
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
            () => "unresolved citations:" + Environment.NewLine
                + string.Join(Environment.NewLine, unresolved));
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
                Does.Contain("json-codec-and-schema-baseline"),
                "a real heading must resolve");
            Assert.That(
                HeadingAnchors(realDocument),
                Does.Not.Contain("no-such-heading-anywhere"),
                "a missing heading must not resolve");
        });
    }

    [Test]
    public void EntryIdsAreWellFormedUniqueAndNeverRenumbered()
    {
        using JsonDocument registry = Registry();
        HashSet<string> seen = new(StringComparer.Ordinal);
        int previous = 0;

        Expect.Multiple(() =>
        {
            foreach (JsonElement entry in Entries(registry))
            {
                string id = entry.GetProperty("id").GetString()!;

                Assert.That(EntryId.IsMatch(id), Is.True, id + " must be VER-DAT-001-###");
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

    [Test]
    public void EveryEntryCarriesTheFieldsDocNinetyOneRequires()
    {
        using JsonDocument registry = Registry();

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
                    entry.GetProperty("summary").GetString(),
                    Is.Not.Empty,
                    id + " needs a summary");
                Assert.That(
                    entry.GetProperty("requirements").GetArrayLength(),
                    Is.GreaterThan(0),
                    id + " must cite at least one requirement");
            }
        });
    }

    /// <summary>
    /// A test selector must name a fixture class that exists, or the registry is
    /// promising evidence nothing produces.
    /// </summary>
    [Test]
    public void EveryNunitSelectorNamesATypeInThisAssembly()
    {
        using JsonDocument registry = Registry();

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
                    typeof(VerificationRegistryTests).Assembly.GetType(typeName),
                    Is.Not.Null,
                    entry.GetProperty("id").GetString() + " names '" + typeName
                        + "', which is not a type in this assembly");
            }
        });
    }

    /// <summary>
    /// Every fixture path a registry entry names must exist, so an entry cannot cite
    /// evidence that was renamed or never written.
    /// </summary>
    [Test]
    public void EveryNamedFixturePathExists()
    {
        using JsonDocument registry = Registry();
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
            () => "fixtures named but absent:" + Environment.NewLine
                + string.Join(Environment.NewLine, missing));
    }

    /// <summary>
    /// The GitHub-style anchors of every heading in a Markdown file: lowercased, inline
    /// code stripped, punctuation removed, spaces to hyphens.
    /// </summary>
    private static HashSet<string> HeadingAnchors(string path)
    {
        HashSet<string> anchors = new(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(path))
        {
            if (!line.StartsWith('#'))
            {
                continue;
            }

            string text = line.TrimStart('#').Trim().Replace("`", string.Empty, StringComparison.Ordinal);
            System.Text.StringBuilder anchor = new(text.Length);
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
