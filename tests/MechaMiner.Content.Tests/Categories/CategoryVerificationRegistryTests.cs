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
/// This matters more than usual on this repository, which has no CI - every gate is a
/// local run, so the written description of what a gate covers is doing work an
/// automated re-run would otherwise do.
/// </para>
/// <para>
/// This walk named DAT-002 and DAT-003 in source, and
/// <see cref="Fixtures.VerificationRegistryTests"/> named DAT-001, which between them left
/// eighteen of the twenty-one registries on disk walked by nobody. Both now take their set
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
        if (summary.Length < RouteContractMinimumLength)
        {
            return "too short to state a route (" + summary.Length.ToString(
                CultureInfo.InvariantCulture) + " characters)";
        }

        if (NamesNoRoute(summary))
        {
            return "names no route to its subject";
        }

        return null;
    }

    /// <summary>
    /// True when a summary names none of the four route keywords.
    /// </summary>
    /// <remarks>
    /// Split out of <see cref="RouteContractFailure"/> so the clause can be counted on its own
    /// without a second copy of the keyword list. Two copies would let the count and the
    /// contract drift, and the count exists precisely to say which clause the contract's cost
    /// is a cost of.
    /// </remarks>
    private static bool NamesNoRoute(string summary)
    {
        return !summary.Contains("route", StringComparison.Ordinal)
            && !summary.Contains("matched", StringComparison.Ordinal)
            && !summary.Contains("recomputed", StringComparison.Ordinal)
            && !summary.Contains("compared", StringComparison.Ordinal);
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

        int shorterThanTheThreshold = 0;
        int namingNoRoute = 0;
        int failingOnLengthAlone = 0;

        foreach (JsonElement entry in Entries(registry))
        {
            entries++;
            string summary = entry.GetProperty("summary").GetString()!;

            bool tooShort = summary.Length < RouteContractMinimumLength;
            bool noRoute = NamesNoRoute(summary);

            if (tooShort)
            {
                shorterThanTheThreshold++;
            }

            if (noRoute)
            {
                namingNoRoute++;
            }

            if (tooShort && !noRoute)
            {
                failingOnLengthAlone++;
            }

            if (RouteContractFailure(summary) is not null)
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
                namingNoRoute,
                Is.EqualTo(DatOneEntriesFailingTheRouteContract),
                "the whole of the failing count is the keyword clause. Asserted rather than "
                    + "described because the two clauses are not distinguishable from the "
                    + "total, and a reader taking the 40 for a route-naming figure when it had "
                    + "become partly a length figure would be reading it wrong");
            Assert.That(
                failingOnLengthAlone,
                Is.EqualTo(DatOneEntriesFailingOnLengthAlone),
                "DAT-001 entries that fail ONLY the length clause, which is what would make the "
                    + "length threshold load-bearing. It is 0 today - all "
                    + shorterThanTheThreshold.ToString(CultureInfo.InvariantCulture)
                    + " summaries shorter than the threshold also name no route - so "
                    + nameof(RouteContractMinimumLength) + " decides nothing the keyword clause "
                    + "does not already decide, and setting it to 1 leaves the cost figure at "
                    + "40. This assertion is what makes that a checked property rather than a "
                    + "claim: a move off 0 means the threshold has started deciding cases and "
                    + "the number beside it has stopped being a pure count of keyword failures");
            Assert.That(
                shorterThanTheThreshold,
                Is.EqualTo(DatOneEntriesShorterThanTheThreshold),
                "DAT-001 summaries shorter than the length threshold. Counted separately from "
                    + "the failures because the two coincide today and nothing said so; a count "
                    + "that only ever appears inside a total cannot be checked against it");
            Assert.That(
                RegistriesUnderTheRouteContract,
                Is.SubsetOf(VerificationRegistry.Packages),
                "every registry the contract is declared to cover must be a registry that "
                    + "exists, or the contract silently covers nothing");
            Assert.That(
                RegistriesUnderTheRouteContract.Length,
                Is.EqualTo(RegistriesUnderTheRouteContractCount),
                "how many registries the route-naming contract covers, and the reason it is "
                    + "asserted next to the subset check is that the subset check cannot say "
                    + "it: Is.SubsetOf is satisfied by the empty set, so the one input on which "
                    + "the contract covers literally nothing is the input it passes most "
                    + "readily. THE HAZARD IS A CASE COUNT FALLING TO ZERO, NOT AN ASSERTION "
                    + "FAILING. Emptying this array deletes the two "
                    + nameof(EverySummaryStatesTheRouteAndNotOnlyTheRule) + " cases, which are "
                    + "TestCaseSource-driven: they cease to exist rather than fail. Measured at "
                    + "f9e616a - the empty array gives 0 failed and exit 0 with the total "
                    + "falling 1498 to 1496, dropping DAT-003 alone gives 0 failed and 1497, "
                    + "and NUnit warns about neither. No summary reporting only failures, or "
                    + "only totals, can tell a vanished case from a case that never existed, "
                    + "and the verb host's own non-vacuity check on a run is Total greater than "
                    + "zero, which 1496 satisfies - so test-fast cannot see it either. Note "
                    + "which operand the subset check does protect: naming a registry that does "
                    + "not exist, [DAT-002, DAT-999], gives 2 failed. It guards the wrong "
                    + "operand for the failure mode its own message names");
        });
    }

    /// <summary>DAT-001's entry count.</summary>
    private const int DatOneEntries = 49;

    /// <summary>
    /// The shortest a summary may be and still be capable of stating a route.
    /// </summary>
    /// <remarks>
    /// Named rather than inline so the clause it belongs to can be counted separately from the
    /// keyword clause - see <see cref="DatOneEntriesFailingOnLengthAlone"/>, which records that
    /// on today's corpus this threshold decides nothing on its own.
    /// </remarks>
    private const int RouteContractMinimumLength = 160;

    /// <summary>
    /// How many registries the route-naming contract covers, written out.
    /// </summary>
    /// <remarks>
    /// A literal for the reason the schema-fixture count is one: every other assertion about
    /// <see cref="RegistriesUnderTheRouteContract"/> is derived from the array, and a figure
    /// derived from the array agrees with the array on every input - including an emptied one,
    /// where the walks over it do not fail but simply stop existing. Moving this number is a
    /// deliberate statement that the contract's reach changed.
    /// </remarks>
    private const int RegistriesUnderTheRouteContractCount = 2;

    /// <summary>
    /// DAT-001 entries that fail the route-naming contract - the cost of widening it there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is a count of keyword failures, and only of those.</b>
    /// <see cref="RouteContractFailure"/> has two clauses and this number is insensitive to one
    /// of them: all <see cref="DatOneEntriesShorterThanTheThreshold"/> summaries below
    /// <see cref="RouteContractMinimumLength"/> also name no route, so
    /// <see cref="DatOneEntriesFailingOnLengthAlone"/> is 0 and setting the threshold to 1
    /// leaves this figure at 40. Measured at f9e616a over DAT-001's 49 entries.
    /// </para>
    /// <para>
    /// The threshold is kept rather than deleted, because a rule that is subsumed on today's
    /// corpus is not the same as a rule that is wrong - it still states that a summary naming a
    /// route has to be long enough to name one. What is not kept is the impression that the 40
    /// prices both clauses. Whoever answers the owed decision is pricing the keyword clause.
    /// </para>
    /// </remarks>
    private const int DatOneEntriesFailingTheRouteContract = 40;

    /// <summary>
    /// DAT-001 entries that fail the length clause and not the keyword clause.
    /// </summary>
    /// <remarks>
    /// 0 at f9e616a, and 0 is the informative value: it is what says
    /// <see cref="RouteContractMinimumLength"/> is deciding no case on its own. A move off 0
    /// means the threshold has become load-bearing and
    /// <see cref="DatOneEntriesFailingTheRouteContract"/> has stopped being a pure count of
    /// keyword failures - both facts a reader of that number needs.
    /// </remarks>
    private const int DatOneEntriesFailingOnLengthAlone = 0;

    /// <summary>
    /// DAT-001 summaries shorter than <see cref="RouteContractMinimumLength"/>.
    /// </summary>
    /// <remarks>
    /// 8 at f9e616a, the shortest of them 99 characters. Every one of the 8 also names no
    /// route, which is why the threshold changes nothing on this corpus.
    /// </remarks>
    private const int DatOneEntriesShorterThanTheThreshold = 8;

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
    /// guarantee than it is, which on a repository with no CI is the whole of what a
    /// reader has to go on.
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
