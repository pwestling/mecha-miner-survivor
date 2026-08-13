using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MechaMiner.Content.Tests.Fixtures;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// Every <c>tests/verification/*.json</c> registry cites only things that exist.
/// </summary>
/// <remarks>
/// <para>
/// A citation to a file or heading that is not there is worse than no citation: it
/// sends the next reader to a document that does not say what they were told it says.
/// An automated re-run does not catch that, and this repository has one: the workflow
/// <c>.github/workflows/fast.yml</c> triggers on push and pull_request and invokes
/// <c>./build.sh test-fast</c>, and <c>TestVerb.RunFastTier</c>'s <c>PureTestProjects</c>
/// list in <c>src/MechaMiner.Tools/Verbs/TestVerb.cs</c> names this project, so this
/// fixture runs there. What that run reports is that the gate passed, never what it
/// covered, so the written description of what a gate covers is still doing work no
/// re-run does. An earlier version of this paragraph gave the absence of CI as the
/// reason instead, which was false against the workflow file already committed here;
/// the mechanism is named above rather than the state asserted, so a reader who doubts
/// it has two paths to open instead of a sentence to believe.
/// </para>
/// <para>
/// This walk named DAT-002 and DAT-003 in source, and
/// <see cref="Fixtures.VerificationRegistryTests"/> named DAT-001, which between them left
/// nineteen of the twenty-two registries on disk walked by nobody. Both now take their set
/// from <see cref="VerificationRegistry.Packages"/> - the directory listing - because a
/// hand-maintained list of registries fails exactly the way the hand-maintained pair did.
/// </para>
/// <para>
/// Scope is deliberately narrow, and the same as the other registry walk's: the
/// structural validator for every <c>tests/verification/*.json</c> is owned by
/// <c>TASK-FND-009-002</c>, and both of these should fold into it when it lands. They are
/// now identical but for
/// <see cref="EverySummaryStatesTheRouteAndNotOnlyTheRule"/>, which is the whole of what
/// this fixture still adds and the reason it has not simply been deleted.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CategoryVerificationRegistryTests
{
    private static IEnumerable<string> Packages => VerificationRegistry.Packages;

    /// <summary>
    /// The registries the route-naming contract is declared to cover.
    /// </summary>
    /// <remarks>
    /// DAT-002 and DAT-003, because those two were written to it and satisfy it. Named here
    /// rather than inherited from whichever walk the check happens to live in - see
    /// <see cref="EverySummaryStatesTheRouteAndNotOnlyTheRule"/> for why that distinction is the
    /// whole of this change.
    /// </remarks>
    private static readonly string[] RegistriesUnderTheRouteContract = ["DAT-002", "DAT-003"];

    /// <summary>
    /// Why a summary fails the route-naming contract, or <see langword="null"/> when it passes.
    /// </summary>
    /// <remarks>
    /// One implementation, because two questions are asked of this rule now: whether the
    /// registries under the contract satisfy it, and what it would cost to widen it. A second
    /// copy would let the cost drift away from the rule it is the cost of.
    /// </remarks>
    private static string? RouteContractFailure(string summary)
    {
        if (summary.Length < 160)
        {
            return "too short to state a route (" + summary.Length.ToString(
                CultureInfo.InvariantCulture) + " characters)";
        }

        if (!summary.Contains("route", StringComparison.Ordinal)
            && !summary.Contains("matched", StringComparison.Ordinal)
            && !summary.Contains("recomputed", StringComparison.Ordinal)
            && !summary.Contains("compared", StringComparison.Ordinal))
        {
            return "names no route to its subject";
        }

        return null;
    }

    /// <summary>
    /// <b>OWED DECISION.</b> Whether the route-naming contract should cover every registry, and
    /// what widening it would cost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The question.</b> <see cref="EverySummaryStatesTheRouteAndNotOnlyTheRule"/> is
    /// declared to cover DAT-002 and DAT-003. Extending the walks to the whole directory showed
    /// that no other registry satisfies it - including DAT-001, which fails <b>40 of its 49</b>
    /// entries. Whoever owns the summary contract has to decide whether route-naming is a
    /// corpus-wide requirement (199 of 294 summaries to rewrite, 40 of them in DAT-001 alone),
    /// or a convention that DAT-002 and DAT-003 keep and the rest do not. This test does not
    /// answer it. It exists so the question cannot be met without its price.
    /// </para>
    /// <para>
    /// <b>Why the figure is asserted rather than written in a comment.</b> A number in prose
    /// rots: rewrite twenty DAT-001 summaries and the comment still says 40, and the decision
    /// gets taken against a cost that is no longer real. Asserting it means the figure is either
    /// current or the build is red, and a red build here is the correct outcome - it means the
    /// cost of the open question changed and whoever changed it should say whether the question
    /// is now answered.
    /// </para>
    /// </remarks>
    [Test]
    public void TheCostOfExtendingTheRouteContractIsRecorded()
    {
        using JsonDocument registry = Registry("DAT-001");
        int entries = 0;
        int failing = 0;

        foreach (JsonElement entry in Entries(registry))
        {
            entries++;
            if (RouteContractFailure(entry.GetProperty("summary").GetString()!) is not null)
            {
                failing++;
            }
        }

        TestContext.Out.WriteLine(
            "OWED DECISION - should the route-naming contract cover every registry? It is "
            + "declared to cover " + string.Join(", ", RegistriesUnderTheRouteContract)
            + ". DAT-001 fails " + failing.ToString(CultureInfo.InvariantCulture) + " of its "
            + entries.ToString(CultureInfo.InvariantCulture)
            + " entries against it. The decision is meaningless without that figure, so the "
            + "figure is asserted here rather than described somewhere.");

        Expect.Multiple(() =>
        {
            Assert.That(
                entries,
                Is.EqualTo(DatOneEntries),
                "DAT-001 entry count, which the cost below is a fraction of");
            Assert.That(
                failing,
                Is.EqualTo(DatOneEntriesFailingTheRouteContract),
                "DAT-001 entries failing the route-naming contract. This is the price of "
                    + "widening the contract to DAT-001 and it must stay current, because a "
                    + "stale price is worse than none - see this test's remarks");
            Assert.That(
                RegistriesUnderTheRouteContract,
                Is.SubsetOf(VerificationRegistry.Packages),
                "every registry the contract is declared to cover must be a registry that "
                    + "exists, or the contract silently covers nothing");
        });
    }

    /// <summary>DAT-001's entry count.</summary>
    private const int DatOneEntries = 49;

    /// <summary>
    /// DAT-001 entries that fail the route-naming contract - the cost of widening it there.
    /// </summary>
    private const int DatOneEntriesFailingTheRouteContract = 40;

    /// <summary>
    /// What an emptied <c>entries</c> array must be reported as, wherever it is found.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A walk over nothing reports nothing wrong. Emptying the array satisfied every walk in this
    /// fixture at once - the one edit to a registry no test here could see. Each of the six
    /// per-package walks now counts what it visited, in the same shape as
    /// <c>SchemaNullScan.DocumentsSeen</c> and <c>SchemaFixturePartition.FilesChecked</c>
    /// elsewhere in this suite. What makes each counter a count rather than a constant is that
    /// emptying the array turns every one of them red.
    /// </para>
    /// <para>
    /// "Nothing here asserts a number" has stopped being true, and so has "all five". Of the eight
    /// test methods in this fixture, counted at <c>46366ea</c>: six are the per-package walks this
    /// message serves; <see cref="TheResolverRejectsAMissingFileAndAMissingHeading"/> walks no
    /// registry; and <see cref="TheCostOfExtendingTheRouteContractIsRecorded"/> asserts two
    /// numbers - <see cref="DatOneEntries"/> at 49 and
    /// <see cref="DatOneEntriesFailingTheRouteContract"/> at 40 - so an emptied DAT-001 fails it
    /// on the entry count rather than passing over an empty sequence.
    /// </para>
    /// </remarks>
    private static string NoEntries(string package)
    {
        return "tests/verification/" + package + ".json holds no entries, so this walk "
            + "visited nothing and every assertion in it passed over an empty sequence";
    }

    [TestCaseSource(nameof(Packages))]
    public void EveryCitedSourceResolvesToARealFileAndHeading(string package)
    {
        using JsonDocument registry = Registry(package);
        List<string> unresolved = new();
        int entriesSeen = 0;
        int citationsResolved = 0;

        foreach (JsonElement entry in Entries(registry))
        {
            entriesSeen++;
            string id = entry.GetProperty("id").GetString()!;
            foreach (string property in new[] { "technicalSources", "gameplaySources" })
            {
                foreach (JsonElement citation in entry.GetProperty(property).EnumerateArray())
                {
                    citationsResolved++;
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

        Expect.Multiple(() =>
        {
            Assert.That(entriesSeen, Is.GreaterThan(0), NoEntries(package));
            Assert.That(
                citationsResolved,
                Is.GreaterThan(0),
                package + " holds entries but not one citation was resolved, so this walk "
                    + "proved nothing about any document");
            Assert.That(
                unresolved,
                Is.Empty,
                () => package + " has unresolved citations:" + Environment.NewLine
                    + string.Join(Environment.NewLine, unresolved));
        });
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

            Assert.That(seen, Is.Not.Empty, NoEntries(package));
        });
    }

    [TestCaseSource(nameof(Packages))]
    public void EveryEntryCarriesTheFieldsDocNinetyOneRequires(string package)
    {
        using JsonDocument registry = Registry(package);
        int entriesChecked = 0;

        Expect.Multiple(() =>
        {
            foreach (JsonElement entry in Entries(registry))
            {
                entriesChecked++;
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

            Assert.That(entriesChecked, Is.GreaterThan(0), NoEntries(package));
        });
    }

    /// <summary>
    /// A summary states the route the check takes to its subject, not only the property
    /// it forbids. A description broader than the thing it describes reads as a stronger
    /// guarantee than it is, and no automated re-run narrows it: CI runs this suite by the
    /// workflow and verb path this fixture's own remarks name, and reports that the gate
    /// passed rather than what it covered - so the summary stays the whole of what a
    /// reader has to go on. This gave the absence of CI as that reason until today, which
    /// was false.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Scope is declared, and deliberately narrower than the other walks'.</b> Every other
    /// check in this fixture runs over <see cref="VerificationRegistry.Packages"/> - the whole
    /// directory. This one runs over <see cref="RegistriesUnderTheRouteContract"/>, and the
    /// difference is the point: DAT-002 and DAT-003 were authored under this contract and
    /// satisfy it, and no other registry was. Until today its scope was those same two
    /// registries by pure accident - they were the only ones this fixture read - which is the
    /// worst state for a real rule to be in: a contract whose reach nobody had declared. It is
    /// now a named property of the rule with the reason attached.
    /// </para>
    /// <para>
    /// <b>This is not an exemption list.</b> An exemption list names registries excused from a
    /// rule that applies to everything, and it hides failures. This names the registries the
    /// rule applies to, and the ones outside it are not excused - they are the subject of an
    /// open question that is deliberately unanswered here, measured by
    /// <see cref="TheCostOfExtendingTheRouteContractIsRecorded"/>.
    /// </para>
    /// </remarks>
    [TestCaseSource(nameof(RegistriesUnderTheRouteContract))]
    public void EverySummaryStatesTheRouteAndNotOnlyTheRule(string package)
    {
        using JsonDocument registry = Registry(package);
        List<string> thin = new();
        int summariesRead = 0;

        foreach (JsonElement entry in Entries(registry))
        {
            summariesRead++;
            string id = entry.GetProperty("id").GetString()!;
            string summary = entry.GetProperty("summary").GetString()!;

            if (RouteContractFailure(summary) is string reason)
            {
                thin.Add(id + ": " + reason);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(summariesRead, Is.GreaterThan(0), NoEntries(package));
            Assert.That(
                thin,
                Is.Empty,
                () => package + " entries that describe a rule without its route:"
                    + Environment.NewLine + string.Join(Environment.NewLine, thin));
        });
    }

    /// <remarks>
    /// Resolution goes through <see cref="RegistrySelectorTypes"/> rather than
    /// <c>Assembly.GetType</c> against this assembly alone; see that class for why, and for
    /// what the route proves. The "at least one nunit selector" assertion this walk used to
    /// make is gone: it was true of DAT-002 and DAT-003 and is false of the seven registries that
    /// declare no nunit selector at all - DAT-007, FND-001, FND-002 and FND-005, which are script
    /// and command gates, and PRE-001, PRE-002 and UI-002, which are engine-scene gates. Not "the
    /// FND registries": FND-003 is one and declares six. That seven is pinned as
    /// <c>VerificationRegistryTests.RegistriesWithNoNunitSelector</c>, because this is the third
    /// place the list was written and it was wrong in all three. The non-vacuity guarantee now
    /// sits over the whole set in
    /// <c>VerificationRegistryTests.EverySelectorKindIsOneSomeWalkResolves</c>.
    /// </remarks>
    [TestCaseSource(nameof(Packages))]
    public void EveryNunitSelectorNamesSomethingThatExists(string package)
    {
        using JsonDocument registry = Registry(package);
        List<string> unresolved = new();
        int entriesSeen = 0;

        foreach (JsonElement entry in Entries(registry))
        {
            entriesSeen++;
            JsonElement selector = entry.GetProperty("selector");
            if (selector.GetProperty("kind").GetString() != "nunit")
            {
                continue;
            }

            string value = selector.GetProperty("value").GetString()!;
            if (RegistrySelectorTypes.Unresolved(value) is string reason)
            {
                unresolved.Add(entry.GetProperty("id").GetString() + ": " + reason);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(entriesSeen, Is.GreaterThan(0), NoEntries(package));
            Assert.That(
                unresolved,
                Is.Empty,
                () => package + " has nunit selectors that name nothing:" + Environment.NewLine
                    + string.Join(Environment.NewLine, unresolved));
        });
    }

    /// <remarks>
    /// Resolution goes through <see cref="RegistryFixtureReferences"/>; see that class for the
    /// four conventions this field carries and why prose is counted rather than passed. The
    /// per-form census is asserted once over the whole set by
    /// <c>VerificationRegistryTests.TheFixtureReferenceCensusIsWhatIsDeclared</c>.
    /// </remarks>
    [TestCaseSource(nameof(Packages))]
    public void EveryNamedFixtureReferenceResolves(string package)
    {
        using JsonDocument registry = Registry(package);
        List<string> unresolved = new();
        int entriesSeen = 0;

        foreach (JsonElement entry in Entries(registry))
        {
            entriesSeen++;
            string id = entry.GetProperty("id").GetString()!;
            foreach (JsonElement fixture in entry.GetProperty("fixtures").EnumerateArray())
            {
                string reference = fixture.GetString()!;
                if (RegistryFixtureReferences.Unresolved(reference) is string reason)
                {
                    unresolved.Add(id + ": " + reason);
                }
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(entriesSeen, Is.GreaterThan(0), NoEntries(package));
            Assert.That(
                unresolved,
                Is.Empty,
                () => package + " names fixtures that do not resolve:" + Environment.NewLine
                    + string.Join(Environment.NewLine, unresolved));
        });
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
        return VerificationRegistry.Open(package);
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
