using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// Every <c>tests/verification/*.json</c> registry cites only things that exist.
/// </summary>
/// <remarks>
/// <para>
/// This exists because four gameplay-source paths in DAT-001 were guessed rather than
/// resolved, and three of the four named files that do not exist. A citation to a
/// file or heading that is not there is worse than no citation: it sends the next reader
/// to a document that does not say what they were told it says.
/// </para>
/// <para>
/// It used to read DAT-001 and nothing else, while
/// <see cref="Categories.CategoryVerificationRegistryTests"/> read DAT-002 and DAT-003 and
/// nothing else, so every registry on disk other than those three was validated by nobody
/// and no test said so - all but three of <see cref="VerificationRegistry.RegistriesOnDisk"/>,
/// the count <see cref="EveryRegistryOnDiskIsDiscoveredAndWalked"/> asserts, which is where
/// that arithmetic belongs rather than in a sentence nothing can redden. The set now comes from
/// <see cref="VerificationRegistry.Packages"/>, which is the directory listing - a
/// hand-maintained list of registries fails the same way the hand-maintained pair did.
/// </para>
/// <para>
/// The <em>structural</em> validator for every <c>tests/verification/*.json</c> is owned by
/// <c>TASK-FND-009-002</c>; this checks only that citations resolve, that IDs are well
/// formed, and that selectors name something. It is not a competing implementation of that
/// validator and should be folded into it when it lands - as should this fixture and
/// <see cref="Categories.CategoryVerificationRegistryTests"/>, which now differ only in the
/// one check the other does not have.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class VerificationRegistryTests
{
    private static IEnumerable<string> Packages => VerificationRegistry.Packages;

    /// <summary>
    /// What an emptied <c>entries</c> array must be reported as, wherever it is found.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A walk over nothing reports nothing wrong. Emptying an <c>entries</c> array - by hand, by
    /// a bad merge, or by a generator that wrote an empty document - satisfied every walk in this
    /// fixture at once, which was the one edit to a registry no test could see. This message is
    /// what the five per-package walks report instead: each counts what it visited, in the same
    /// shape as <c>SchemaNullScan.DocumentsSeen</c> and
    /// <c>SchemaFixturePartition.FilesChecked</c> elsewhere in this suite, and the counters are
    /// counts rather than constants because emptying the array turns every one of them red.
    /// </para>
    /// <para>
    /// What is no longer true is that every test here is such a walk, and the difference is worth
    /// stating rather than leaving as a sentence that was accurate once. Of the eleven test
    /// methods in this fixture, counted at <c>46366ea</c>: five are the per-package walks this
    /// message serves; three are censuses -
    /// <see cref="EverySelectorKindIsOneSomeWalkResolves"/>,
    /// <see cref="TheSelectorCensusIsWhatIsDeclared"/> and
    /// <see cref="TheFixtureReferenceCensusIsWhatIsDeclared"/> - which would now fail against
    /// their own committed literals rather than pass over an empty sequence;
    /// <see cref="EveryRegistryOnDiskIsDiscoveredAndWalked"/> reads the directory and not the
    /// entries; and two are resolver negative controls that walk no registry at all. So an
    /// emptied array is loud in two independent ways now, and this message is only the first.
    /// </para>
    /// </remarks>
    private static string NoEntries(string package)
    {
        return "tests/verification/" + package + ".json holds no entries, so this walk visited "
            + "nothing and every assertion in it passed over an empty sequence";
    }

    private static JsonDocument Registry(string package)
    {
        return VerificationRegistry.Open(package);
    }

    private static IEnumerable<JsonElement> Entries(JsonDocument registry)
    {
        return VerificationRegistry.Entries(registry);
    }

    /// <summary>
    /// The set of registries walked is the set on disk, and there are as many as stated.
    /// </summary>
    /// <remarks>
    /// The walks below are all <c>[TestCaseSource(nameof(Packages))]</c>, so a registry that
    /// stops being discovered stops being walked and nothing turns red - the suite quietly
    /// covers less. This is the one assertion that notices, which is why the number it
    /// compares against is a committed literal and not another enumeration of the directory.
    /// </remarks>
    [Test]
    public void EveryRegistryOnDiskIsDiscoveredAndWalked()
    {
        string[] onDisk = Directory
            .GetFiles(VerificationRegistry.DirectoryPath, "*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Expect.Multiple(() =>
        {
            Assert.That(
                VerificationRegistry.Packages,
                Is.EqualTo(onDisk),
                "the walked set must be exactly the registries in tests/verification/");
            Assert.That(
                VerificationRegistry.Packages.Count,
                Is.EqualTo(VerificationRegistry.RegistriesOnDisk),
                "tests/verification/ holds "
                    + VerificationRegistry.Packages.Count.ToString(CultureInfo.InvariantCulture)
                    + " registries and "
                    + nameof(VerificationRegistry.RegistriesOnDisk) + " says "
                    + VerificationRegistry.RegistriesOnDisk.ToString(CultureInfo.InvariantCulture)
                    + ". If a registry was added, raise the literal deliberately; if one stopped "
                    + "being discovered, this is the fixture telling you the suite now covers "
                    + "less than it did");
        });
    }

    /// <summary>
    /// Every cited file exists and, where a citation names a heading anchor, that heading
    /// exists in the cited file.
    /// </summary>
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
                package + " holds entries but not one citation was resolved, so this "
                    + "walk proved nothing about any document");
            Assert.That(
                unresolved,
                Is.Empty,
                () => package + " has unresolved citations:" + Environment.NewLine
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
                Does.Contain("json-codec-and-schema-baseline"),
                "a real heading must resolve");
            Assert.That(
                HeadingAnchors(realDocument),
                Does.Not.Contain("no-such-heading-anywhere"),
                "a missing heading must not resolve");
        });
    }

    [TestCaseSource(nameof(Packages))]
    public void EntryIdsAreWellFormedUniqueAndNeverRenumbered(string package)
    {
        using JsonDocument registry = Registry(package);
        Regex entryId = new(
            @"\AVER-" + Regex.Escape(package) + @"-[0-9]{3}\z",
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        HashSet<string> seen = new(StringComparer.Ordinal);
        int previous = 0;

        Expect.Multiple(() =>
        {
            foreach (JsonElement entry in Entries(registry))
            {
                string id = entry.GetProperty("id").GetString()!;

                Assert.That(entryId.IsMatch(id), Is.True, id + " must be VER-" + package + "-###");
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
                    entry.GetProperty("summary").GetString(),
                    Is.Not.Empty,
                    id + " needs a summary");
                Assert.That(
                    entry.GetProperty("requirements").GetArrayLength(),
                    Is.GreaterThan(0),
                    id + " must cite at least one requirement");
            }

            Assert.That(entriesChecked, Is.GreaterThan(0), NoEntries(package));
        });
    }

    /// <summary>
    /// A test selector must name a fixture class, or a test method on one, that exists - or
    /// the registry is promising evidence nothing produces.
    /// </summary>
    /// <remarks>
    /// Resolution goes through <see cref="RegistrySelectorTypes"/> rather than
    /// <c>Assembly.GetType</c> against this assembly alone, because many of the selectors on
    /// disk name fixtures in the three sibling test projects this project does not reference
    /// and could never have resolved. See that class for what the route proves, and
    /// <see cref="TheSelectorCensusIsWhatIsDeclared"/> for how many get which answer - a
    /// literal rather than a number restated here.
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

        // No "at least one nunit selector" assertion here, deliberately. Seven of the twenty-two
        // registries declare none: DAT-007, FND-001, FND-002 and FND-005, which are script and
        // command gates, and PRE-001, PRE-002 and UI-002, which are engine-scene gates. Not "the
        // FND registries" - FND-003 is one and declares six - and the seven are pinned as
        // RegistriesWithNoNunitSelector rather than restated here, because an enumeration in a
        // comment is what went stale. The non-vacuity guarantee this walk needs is held one level
        // up, over the whole set, by EverySelectorKindIsOneSomeWalkResolves.
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

    /// <summary>
    /// Across the whole registry set, every declared selector <c>kind</c> is one some walk
    /// knows how to resolve, and the nunit walk really did resolve some.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The per-registry selector walk skips any entry whose kind is not <c>nunit</c>, so a
    /// registry declaring no <c>nunit</c> selector passes it while having no selector checked at
    /// all. Seven of the twenty-two are in that state: DAT-007, FND-001, FND-002 and FND-005,
    /// which are script and command gates, and PRE-001, PRE-002 and UI-002, which are engine-scene
    /// gates. FND-003 is an FND registry and is <em>not</em> among them - it declares six nunit
    /// selectors - which is why the exposure is stated as a list of registries rather than as a
    /// family. That is a real gap and this assertion is deliberately <em>not</em> a fix for it: it
    /// records which kinds exist and which are resolvable, so the gap is stated by a test instead
    /// of living in a report. The count of exposed registries is pinned as
    /// <see cref="RegistriesWithNoNunitSelector"/> by
    /// <see cref="TheSelectorCensusIsWhatIsDeclared"/>, because this paragraph's whole job is to
    /// state exposure and it named four when there were seven. Teaching the walk to resolve
    /// <c>script</c>, <c>command</c> and <c>engine-scene</c> selectors is separate work.
    /// </para>
    /// <para>
    /// <see cref="KindsSomeWalkResolves"/> is a literal for the usual reason: derived from the
    /// registries it would agree with any set of kinds, including a typo'd one.
    /// </para>
    /// </remarks>
    [Test]
    public void EverySelectorKindIsOneSomeWalkResolves()
    {
        Dictionary<string, int> kinds = new(StringComparer.Ordinal);
        int nunitResolved = 0;

        foreach (string package in VerificationRegistry.Packages)
        {
            using JsonDocument registry = Registry(package);
            foreach (JsonElement entry in Entries(registry))
            {
                string kind = entry.GetProperty("selector").GetProperty("kind").GetString()!;
                kinds[kind] = kinds.TryGetValue(kind, out int count) ? count + 1 : 1;
                if (string.Equals(kind, "nunit", StringComparison.Ordinal))
                {
                    nunitResolved++;
                }
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                nunitResolved,
                Is.GreaterThan(0),
                "not one nunit selector was found across every registry, so the selector walk "
                    + "resolved no type at all. The registries' whole claim is that their "
                    + "selectors point at evidence that exists");
            Assert.That(
                kinds.Keys.OrderBy(kind => kind, StringComparer.Ordinal),
                Is.EqualTo(KnownSelectorKinds),
                () => "the selector kinds on disk are " + string.Join(
                        ", ",
                        kinds.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                            .Select(pair => pair.Key + "=" + pair.Value.ToString(
                                CultureInfo.InvariantCulture)))
                    + ". A kind not in " + nameof(KnownSelectorKinds)
                    + " is silently skipped by every walk in this suite, so an entry declaring "
                    + "it promises evidence nothing checks");
            Assert.That(
                KindsSomeWalkResolves,
                Is.EquivalentTo(new[] { "nunit" }),
                "nunit is still the only selector kind any walk resolves. Raise this the day "
                    + "script, command or engine-scene selectors are actually resolved, not "
                    + "before");
        });
    }

    /// <summary>Every selector kind any registry declares.</summary>
    private static readonly string[] KnownSelectorKinds =
        ["command", "engine-scene", "nunit", "script"];

    /// <summary>The subset of <see cref="KnownSelectorKinds"/> some walk can resolve.</summary>
    private static readonly string[] KindsSomeWalkResolves = ["nunit"];

    /// <summary>
    /// How many entries there are, how many each selector kind accounts for, and which of the
    /// two resolution routes answers each <c>nunit</c> selector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> <see cref="EverySelectorKindIsOneSomeWalkResolves"/> pins the
    /// <em>set</em> of kinds and <see cref="VerificationRegistry.RegistriesOnDisk"/> pins how
    /// many files are walked, but nothing pinned how much of either is there. Until this test the
    /// only statement of how many nunit selectors are on disk was a sentence in a doc comment on
    /// <see cref="RegistrySelectorTypes"/> - no literal, no assertion - so entries could be
    /// deleted, or a gate downgraded from an <c>nunit</c> selector to a <c>script</c> one nothing
    /// resolves, and that sentence would simply become false with nothing turning red. That is
    /// the same staleness <see cref="ProseReferences"/> and
    /// <see cref="VerificationRegistry.RegistriesOnDisk"/> exist to catch, and the selector
    /// census had been left out of it.
    /// </para>
    /// <para>
    /// The numbers are literals for the usual reason: a census derived from the registries agrees
    /// with itself on every input, including an input that lost a gate. Moving one is a
    /// deliberate statement about what this suite now covers.
    /// </para>
    /// <para>
    /// <b>Which of the sums are load-bearing, and which are arithmetic.</b> Two of the assertions
    /// below compare a sum of committed literals against another committed literal:
    /// <c>NunitSelectors + ScriptSelectors + CommandSelectors + EngineSceneSelectors</c> against
    /// <c>RegistryEntries</c>, and <c>NunitSelectorsReflected + NunitSelectorsSourceDeclared</c>
    /// against <c>NunitSelectors</c>. Every operand is a <c>const</c> in this file, so no state of
    /// <c>tests/verification/</c> can redden either one. They were written as a defence against a
    /// drift in one kind compensated by a drift in another, and they are not that: flip one
    /// DAT-002 entry from <c>nunit</c> to <c>script</c>, edit both literals, and both sums still
    /// hold - as they should, because the per-kind equalities above them are what catch that flip,
    /// individually and without needing a sum. What the two sums actually have is narrower and
    /// still worth keeping: a kind literal cannot be moved without the entry literal moving with
    /// it, so re-measuring the census and transcribing three of four literals is caught here
    /// rather than left to whichever per-kind assertion happens to run.
    /// </para>
    /// <para>
    /// The assertions that do depend on the registries are the ones stated against measured
    /// values: each per-kind equality; <c>namedKinds == entries</c>, which is not
    /// <c>kinds.Values.Sum() == entries</c> - that holds by construction whatever the kinds are -
    /// but the claim that the four <em>named</em> kinds account for every entry measured, so an
    /// entry declaring a fifth kind fails it while all four of the others are right; the route
    /// sum against <c>Kind("nunit")</c>; and
    /// <see cref="RegistriesWithNoNunitSelector"/>, which is the one number in this census that no
    /// other assertion here implies. That last one exists because the exposure it counts - a
    /// registry the per-registry selector walk checks nothing in - had been enumerated in three
    /// comments and was wrong in all three.
    /// </para>
    /// <para>
    /// <b>What these numbers do NOT prove.</b> "Resolves" is two claims of two different
    /// strengths, which is why the split is counted rather than the total. For the
    /// <see cref="NunitSelectorsReflected"/> selectors naming a type in this assembly, it is the
    /// runtime loader's answer: the type was loaded and, for a method-granular selector,
    /// reflection found a member of that name declared on it. For the
    /// <see cref="NunitSelectorsSourceDeclared"/> naming a type in a sibling test project this
    /// project cannot reference, it means only that a member of that name is declared somewhere
    /// in the repository's test sources - not that the declaration compiles into that assembly,
    /// not that it carries <c>[Test]</c>, not that it is reachable, not that it runs. And neither
    /// number, at either strength, says that a test a selector names passes, or that it tests
    /// what its entry's <c>summary</c> claims it tests. This pins how many selectors exist and
    /// which strength of answer each one got; it does not upgrade the weaker answer.
    /// </para>
    /// </remarks>
    [Test]
    public void TheSelectorCensusIsWhatIsDeclared()
    {
        Dictionary<string, int> kinds = new(StringComparer.Ordinal);
        Dictionary<RegistrySelectorTypes.Route, int> routes = new();
        HashSet<string> distinctNunitSelectors = new(StringComparer.Ordinal);
        List<string> registriesWithNoNunitSelector = new();
        int entries = 0;

        foreach (string package in VerificationRegistry.Packages)
        {
            using JsonDocument registry = Registry(package);
            int nunitInPackage = 0;

            foreach (JsonElement entry in Entries(registry))
            {
                entries++;
                JsonElement selector = entry.GetProperty("selector");
                string kind = selector.GetProperty("kind").GetString()!;
                kinds[kind] = kinds.TryGetValue(kind, out int count) ? count + 1 : 1;

                if (!string.Equals(kind, "nunit", StringComparison.Ordinal))
                {
                    continue;
                }

                nunitInPackage++;

                string value = selector.GetProperty("value").GetString()!;
                distinctNunitSelectors.Add(value);
                RegistrySelectorTypes.Route route = RegistrySelectorTypes.RouteOf(value);
                routes[route] = routes.TryGetValue(route, out int answered) ? answered + 1 : 1;
            }

            if (nunitInPackage == 0)
            {
                registriesWithNoNunitSelector.Add(package);
            }
        }

        int Kind(string kind)
        {
            return kinds.TryGetValue(kind, out int count) ? count : 0;
        }

        int Answered(RegistrySelectorTypes.Route route)
        {
            return routes.TryGetValue(route, out int count) ? count : 0;
        }

        int namedKinds = Kind("nunit") + Kind("script") + Kind("command") + Kind("engine-scene");

        TestContext.Out.WriteLine(
            "selector census over " + VerificationRegistry.Packages.Count.ToString(
                CultureInfo.InvariantCulture)
            + " registries and " + entries.ToString(CultureInfo.InvariantCulture) + " entries: "
            + Kind("nunit").ToString(CultureInfo.InvariantCulture) + " nunit, "
            + Kind("script").ToString(CultureInfo.InvariantCulture) + " script, "
            + Kind("command").ToString(CultureInfo.InvariantCulture) + " command, "
            + Kind("engine-scene").ToString(CultureInfo.InvariantCulture) + " engine-scene. Of the "
            + Kind("nunit").ToString(CultureInfo.InvariantCulture) + " nunit selectors, "
            + distinctNunitSelectors.Count.ToString(CultureInfo.InvariantCulture)
            + " are distinct values, "
            + Answered(RegistrySelectorTypes.Route.Reflection).ToString(CultureInfo.InvariantCulture)
            + " are answered by reflection into this assembly and "
            + Answered(RegistrySelectorTypes.Route.SourceIndex).ToString(
                CultureInfo.InvariantCulture)
            + " only by the source index. THOSE TWO ANSWERS ARE NOT THE SAME STRENGTH: the first "
            + "loaded the member, the second found a declaration of that name under tests/ and "
            + "proves nothing about whether it compiles, carries [Test] or runs.");

        Expect.Multiple(() =>
        {
            Assert.That(
                entries,
                Is.GreaterThan(0),
                "no entry was visited, so this census counted nothing and every number below is "
                    + "zero agreeing with zero");
            Assert.That(entries, Is.EqualTo(RegistryEntries), "entries across every registry");
            Assert.That(Kind("nunit"), Is.EqualTo(NunitSelectors), "entries with an nunit selector");
            Assert.That(
                Kind("script"), Is.EqualTo(ScriptSelectors), "entries with a script selector");
            Assert.That(
                Kind("command"), Is.EqualTo(CommandSelectors), "entries with a command selector");
            Assert.That(
                Kind("engine-scene"),
                Is.EqualTo(EngineSceneSelectors),
                "entries with an engine-scene selector");
            Assert.That(
                NunitSelectors + ScriptSelectors + CommandSelectors + EngineSceneSelectors,
                Is.EqualTo(RegistryEntries),
                "the committed kind counts must sum to the committed entry total. Every operand "
                    + "here is a const in this file, so this is arithmetic on the literals and no "
                    + "state of tests/verification/ can redden it: what it catches is a census "
                    + "re-measured and transcribed into three of the four kind literals. A drift "
                    + "in one kind compensated by a drift in another is caught by the per-kind "
                    + "equalities above, not by this");
            Assert.That(
                namedKinds,
                Is.EqualTo(entries),
                () => "the four named kinds must account for every entry on disk, and they "
                    + "accounted for " + namedKinds.ToString(CultureInfo.InvariantCulture)
                    + " of " + entries.ToString(CultureInfo.InvariantCulture)
                    + ". The kinds found were " + string.Join(
                        ", ",
                        kinds.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                            .Select(pair => pair.Key + "=" + pair.Value.ToString(
                                CultureInfo.InvariantCulture)))
                    + ". Summing the measured counts against each other would pass on any input; "
                    + "this is the form that notices a fifth kind");
            Assert.That(
                Answered(RegistrySelectorTypes.Route.Reflection),
                Is.EqualTo(NunitSelectorsReflected),
                "nunit selectors reflection resolves inside this assembly");
            Assert.That(
                Answered(RegistrySelectorTypes.Route.SourceIndex),
                Is.EqualTo(NunitSelectorsSourceDeclared),
                "nunit selectors only the source index resolves - the weaker answer, and raising "
                    + "this says one more gate is now attested by a declaration rather than by "
                    + "the loader");
            Assert.That(
                NunitSelectorsReflected + NunitSelectorsSourceDeclared,
                Is.EqualTo(NunitSelectors),
                "the committed split must sum to the committed nunit total. Const arithmetic, like "
                    + "the kind sum above and with the same narrow value: the assertion that a "
                    + "selector really did change route is the pair of measured equalities above, "
                    + "and the one that notices a selector answered by neither route is the "
                    + "measured route sum below");
            Assert.That(
                Answered(RegistrySelectorTypes.Route.Reflection)
                    + Answered(RegistrySelectorTypes.Route.SourceIndex),
                Is.EqualTo(Kind("nunit")),
                "every nunit selector must be answered by one route or the other; a shortfall "
                    + "here is selectors resolved by neither, which "
                    + nameof(EveryNunitSelectorNamesSomethingThatExists) + " reports in detail");
            Assert.That(
                distinctNunitSelectors.Count,
                Is.EqualTo(DistinctNunitSelectors),
                "distinct nunit selector values - fewer than the entries, because one fixture is "
                    + "legitimately the evidence for several entries");
            Assert.That(
                registriesWithNoNunitSelector.Count,
                Is.EqualTo(RegistriesWithNoNunitSelector),
                () => "registries declaring no nunit selector at all, which "
                    + nameof(EveryNunitSelectorNamesSomethingThatExists)
                    + " therefore checks no selector in. They are "
                    + string.Join(", ", registriesWithNoNunitSelector)
                    + ". This is the number the exposure paragraphs in this fixture and in "
                    + "CategoryVerificationRegistryTests state in prose, and they stated four when "
                    + "it was seven. Raising it says one more registry's gates are now checked by "
                    + "no selector walk");
        });
    }

    /// <summary>
    /// Every fixture reference a registry entry names must resolve, so an entry cannot cite
    /// evidence that was renamed or never written.
    /// </summary>
    /// <remarks>
    /// Resolution goes through <see cref="RegistryFixtureReferences"/>, which knows the four
    /// conventions this field actually carries; see that class for what each is and why prose
    /// is counted rather than passed. There is no "at least one reference" assertion here: six
    /// registries declare no fixture evidence at all, so it would be false for them. What must
    /// hold across the set is
    /// <see cref="TheFixtureReferenceCensusIsWhatIsDeclared"/>.
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

    /// <summary>
    /// How many fixture references follow each convention, and how many are unverifiable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the assertion that keeps the previous check's blind spot from coming back.</b>
    /// <see cref="EveryNamedFixtureReferenceResolves"/> returns "resolved" for a
    /// <see cref="RegistryFixtureReferences.Form.Prose"/> reference because there is nothing in
    /// it to resolve, and that is a fail-open unless something states how many references got
    /// that answer. This does: each form's count is compared against a committed literal and
    /// the census is written to the run's output, so the references that no test can verify are
    /// visible as their own asserted <see cref="ProseReferences"/> count rather than invisible
    /// in a total. Neither number is restated here on purpose - the total is that literal plus
    /// <see cref="RepositoryPathReferences"/>, <see cref="PathAndSectionReferences"/> and
    /// <see cref="PathAndCaseReferences"/>, each of which this test asserts, and a figure
    /// transcribed into this sentence would be asserted by nothing.
    /// </para>
    /// <para>
    /// The literals are literals for the usual reason - a census derived from the registries
    /// agrees with itself on any input, including an input where a checkable reference was
    /// quietly reworded into prose. Raising <see cref="ProseReferences"/> is a deliberate
    /// statement that one more piece of evidence is now unverifiable.
    /// </para>
    /// </remarks>
    [Test]
    public void TheFixtureReferenceCensusIsWhatIsDeclared()
    {
        Dictionary<RegistryFixtureReferences.Form, int> census = new();
        int registriesWithNoFixtureReference = 0;
        int entriesWithNoFixtureReference = 0;
        int entries = 0;

        foreach (string package in VerificationRegistry.Packages)
        {
            using JsonDocument registry = Registry(package);
            int referencesInPackage = 0;

            foreach (JsonElement entry in Entries(registry))
            {
                entries++;
                int referencesInEntry = 0;
                foreach (JsonElement fixture in entry.GetProperty("fixtures").EnumerateArray())
                {
                    referencesInEntry++;
                    referencesInPackage++;
                    RegistryFixtureReferences.Form form =
                        RegistryFixtureReferences.FormOf(fixture.GetString()!);
                    census[form] = census.TryGetValue(form, out int count) ? count + 1 : 1;
                }

                if (referencesInEntry == 0)
                {
                    entriesWithNoFixtureReference++;
                }
            }

            if (referencesInPackage == 0)
            {
                registriesWithNoFixtureReference++;
            }
        }

        int Count(RegistryFixtureReferences.Form form)
        {
            return census.TryGetValue(form, out int count) ? count : 0;
        }

        TestContext.Out.WriteLine(
            "fixture-reference census over " + VerificationRegistry.Packages.Count.ToString(
                CultureInfo.InvariantCulture)
            + " registries and " + entries.ToString(CultureInfo.InvariantCulture) + " entries: "
            + Count(RegistryFixtureReferences.Form.RepositoryPath).ToString(
                CultureInfo.InvariantCulture) + " repository-path, "
            + Count(RegistryFixtureReferences.Form.PathAndSection).ToString(
                CultureInfo.InvariantCulture) + " path-and-section, "
            + Count(RegistryFixtureReferences.Form.PathAndCase).ToString(
                CultureInfo.InvariantCulture) + " path-and-case, "
            + Count(RegistryFixtureReferences.Form.Prose).ToString(CultureInfo.InvariantCulture)
            + " prose. THE PROSE REFERENCES ARE NOT VERIFIED BY ANYTHING: they describe manual "
            + "perturbations, and no assertion in this suite can tell a performed one from an "
            + "imagined one. "
            + registriesWithNoFixtureReference.ToString(CultureInfo.InvariantCulture)
            + " registries and "
            + entriesWithNoFixtureReference.ToString(CultureInfo.InvariantCulture)
            + " entries name no fixture evidence at all.");

        Expect.Multiple(() =>
        {
            Assert.That(
                Count(RegistryFixtureReferences.Form.RepositoryPath),
                Is.EqualTo(RepositoryPathReferences),
                "repository-path fixture references");
            Assert.That(
                Count(RegistryFixtureReferences.Form.PathAndSection),
                Is.EqualTo(PathAndSectionReferences),
                "path-and-section fixture references");
            Assert.That(
                Count(RegistryFixtureReferences.Form.PathAndCase),
                Is.EqualTo(PathAndCaseReferences),
                "path-and-case fixture references");
            Assert.That(
                Count(RegistryFixtureReferences.Form.Prose),
                Is.EqualTo(ProseReferences),
                "prose fixture references - references no test in this suite can verify. If this "
                    + "rose, a piece of evidence that was checkable is now a description, and "
                    + "raising the literal says so deliberately");
            Assert.That(
                registriesWithNoFixtureReference,
                Is.EqualTo(RegistriesNamingNoFixture),
                "registries naming no fixture evidence at all");
            Assert.That(
                entriesWithNoFixtureReference,
                Is.EqualTo(EntriesNamingNoFixture),
                "entries naming no fixture evidence at all");
        });
    }

    /// <summary>
    /// The negative control for the fixture-reference resolver: it must classify by shape and
    /// must be able to fail.
    /// </summary>
    /// <remarks>
    /// The census above is only worth its literals if the resolver behind it can refuse
    /// something. Two failure modes are specifically guarded: a section that is not a heading in
    /// a file that does exist - the case the old <c>File.Exists</c> walk would have passed had
    /// it split the string at all - and the temptation to classify by existence, which would
    /// turn a deleted fixture into unverifiable prose instead of a reported absence.
    /// </remarks>
    [Test]
    public void TheFixtureReferenceResolverRefusesWhatIsNotThere()
    {
        const string realDocument = "tests/verification/SIM-negative-controls.md";
        const string realSection = "The tick rate is changed to 50 Hz";

        Expect.Multiple(() =>
        {
            Assert.That(
                RegistryFixtureReferences.Unresolved(realDocument + " § " + realSection),
                Is.Null,
                "a real file and a real heading in it must resolve");
            Assert.That(
                RegistryFixtureReferences.Unresolved(
                    realDocument + " § No such heading anywhere in this document"),
                Is.Not.Null,
                "a real file with a heading it does not have must not resolve - this is the case "
                    + "the old walk could never even reach");
            Assert.That(
                RegistryFixtureReferences.Unresolved("docs/does-not-exist.md § Anything"),
                Is.Not.Null,
                "a missing file must not resolve through the section form");
            Assert.That(
                RegistryFixtureReferences.Unresolved("content/does-not-exist.json"),
                Is.Not.Null,
                "a missing repository path must not resolve");
            Assert.That(
                RegistryFixtureReferences.Unresolved("content/does-not-exist.tscn case=fail"),
                Is.Not.Null,
                "a missing file must not resolve through the case form");
            Assert.That(
                RegistryFixtureReferences.FormOf("content/does-not-exist.json"),
                Is.EqualTo(RegistryFixtureReferences.Form.RepositoryPath),
                "a path that does not exist is still a path. Classifying by existence would make "
                    + "deleting a cited fixture reclassify its reference as prose, and the check "
                    + "would stop looking for the file rather than report it gone");
            Assert.That(
                RegistryFixtureReferences.FormOf(
                    "working-tree injection: content/relics/REL-11.json, a copy of REL-01.json"),
                Is.EqualTo(RegistryFixtureReferences.Form.Prose),
                "an English description naming a real path inside it is still prose");
            Assert.That(
                RegistryFixtureReferences.IsVerifiable(RegistryFixtureReferences.Form.Prose),
                Is.False,
                "prose must declare itself unverifiable rather than reporting success");
        });
    }

    /// <summary>Fixture references that are a bare repository-relative path.</summary>
    /// <remarks>The 319 at <c>715ef53</c> plus the nine <c>DAT-006.json</c> names at
    /// <c>59a4986</c>: the divergence golden four times, two schemas, and three resource
    /// definitions.</remarks>
    /// <remarks>
    /// 329 now. VER-DAT-006-005 names docs/technical/40-content-data-and-validation.md as the one
    /// file its ref walk reads, so the delta is +1 and the reference is the document the entry is
    /// about.
    /// </remarks>
    /// <remarks>
    /// 331 now. VER-DAT-006-006 names two corpus files as the evidence its declared-pointer
    /// re-derivation turns on - content/mining-sites/specialized-material-geodes.json and
    /// content/resources/A.json - so the delta is +2 and both are files the entry quotes values from.
    /// </remarks>
    /// <remarks>
    /// 333 now. VER-DAT-006-007, which withdraws direction 2 of the vocabulary census as never
    /// measured, names two - content/localization/en.json, the file its out-of-scope-by-kind
    /// disposition is about, and src/MechaMiner.Content/Categories/UtilityDefinition.cs, which
    /// carries the count-of-names remark it cross-references - so the delta is +2. It names its
    /// instrument in prose rather than as a fixture reference on purpose: the script is not
    /// committed yet, blocked on an INVENTORY classification in <c>build/verify-gate-wiring.sh</c>
    /// that another stream owns, and a fixture reference to a path that does not exist is exactly
    /// what <see cref="EveryNamedFixtureReferenceResolves"/> exists to catch.
    /// </remarks>
    /// <remarks>
    /// STILL 333, and it is the previous remark's reason that has changed under it rather than the
    /// count. VER-DAT-006-007's instrument IS now committed, at
    /// <c>tests/verification/instruments/dat-006-derive.md</c> - a markdown document whose header
    /// records the instrument's 54,840 bytes and SHA-256 and whose one fenced code block holds the
    /// program itself, byte for byte. It is a document rather than a script because nothing
    /// executes it and <c>build/verify-gate-wiring.sh</c> has no kind for a program that decides
    /// nothing and that no verb invokes; it is deliberately not a <c>.py</c> with its shebang
    /// stripped, which that gate refuses by name. The delta is 0 because this census counts the
    /// <c>fixtures</c> array and the entry names the document in its summary prose instead: the
    /// document is the instrument that produced the entry's figures, not a fixture any test in this
    /// suite reads. Naming it in <c>fixtures</c> would be a deliberate +1 here, and since that path
    /// now exists it would be a choice rather than the blocker the previous remark records.
    /// </remarks>
    private const int RepositoryPathReferences = 333;

    /// <summary>Fixture references of the form <c>path § heading</c>.</summary>
    private const int PathAndSectionReferences = 108;

    /// <summary>Fixture references of the form <c>path case=variant</c>.</summary>
    private const int PathAndCaseReferences = 5;

    /// <summary>Fixture references that are prose, and so verified by nothing.</summary>
    private const int ProseReferences = 22;

    /// <summary>Registries whose entries name no fixture evidence at all.</summary>
    private const int RegistriesNamingNoFixture = 6;

    /// <summary>Entries naming no fixture evidence at all.</summary>
    private const int EntriesNamingNoFixture = 73;

    /// <summary>Entries across every registry in <c>tests/verification/</c>.</summary>
    /// <remarks>The 294 at <c>715ef53</c> plus <c>DAT-006.json</c>'s four, at
    /// <c>59a4986</c>.</remarks>
    /// <remarks>
    /// 299 now. VER-DAT-006-005, which records the ref-scope invariant that two commits on this
    /// branch stated as a per-clone blob count, is the one entry added since this was last pinned.
    /// </remarks>
    /// <remarks>
    /// 300 now. VER-DAT-006-006, the declared-pointer re-derivation of the vocabulary census, is the
    /// one entry added since this was last pinned.
    /// </remarks>
    /// <remarks>
    /// 301 now. VER-DAT-006-007, which withdraws direction 2 of that census as never measured and
    /// returns its two recoverable figures to direction 1, is the one entry added since this was
    /// last pinned.
    /// </remarks>
    private const int RegistryEntries = 301;

    /// <summary>Entries whose selector <c>kind</c> is <c>nunit</c>.</summary>
    /// <remarks>The 236 at <c>715ef53</c> plus <c>DAT-006.json</c>'s four, at
    /// <c>59a4986</c>.</remarks>
    private const int NunitSelectors = 240;

    /// <summary>Entries whose selector <c>kind</c> is <c>script</c>.</summary>
    private const int ScriptSelectors = 39;

    /// <summary>Entries whose selector <c>kind</c> is <c>command</c>.</summary>
    /// <remarks>
    /// 14 now. VER-DAT-006-005 carries a command selector rather than an nunit one because what it
    /// records is a measurement over git refs, not a property a test asserts - so it moves this
    /// count and leaves <see cref="NunitSelectors"/> alone, which is the distinction worth being
    /// able to see in the census.
    /// </remarks>
    /// <remarks>
    /// 15 now. VER-DAT-006-006 carries a command selector for the same reason VER-DAT-006-005 does:
    /// what it records is a measurement, not a property a test asserts, so it moves this count and
    /// leaves <see cref="NunitSelectors"/> alone.
    /// </remarks>
    /// <remarks>
    /// 16 now. VER-DAT-006-007 carries a command selector for the same reason the two before it do,
    /// and for one more: what it records is a withdrawal plus a re-derivation, neither of which is a
    /// property any test in this suite asserts. The selector names the instrument's command so the
    /// figures can be re-run rather than trusted.
    /// </remarks>
    private const int CommandSelectors = 16;

    /// <summary>Entries whose selector <c>kind</c> is <c>engine-scene</c>.</summary>
    private const int EngineSceneSelectors = 6;

    /// <summary>
    /// Nunit selectors the runtime loader answers, because they name a type in this assembly.
    /// The stronger of the two answers; see
    /// <see cref="TheSelectorCensusIsWhatIsDeclared"/> for what neither proves.
    /// </summary>
    /// <remarks>The 126 at <c>715ef53</c> plus <c>DAT-006.json</c>'s four, which name a
    /// fixture and three methods in this assembly, at <c>59a4986</c>.</remarks>
    private const int NunitSelectorsReflected = 130;

    /// <summary>
    /// Nunit selectors only the source index answers, because they name a type in a sibling test
    /// project this one cannot reference. The weaker answer.
    /// </summary>
    private const int NunitSelectorsSourceDeclared = 110;

    /// <summary>
    /// Distinct nunit selector values, which is fewer than
    /// <see cref="NunitSelectors"/> because a fixture can be the evidence for several entries.
    /// </summary>
    /// <remarks>The 150 at <c>715ef53</c> plus <c>DAT-006.json</c>'s four, which are four
    /// distinct values, at <c>59a4986</c>.</remarks>
    private const int DistinctNunitSelectors = 154;

    /// <summary>
    /// Registries declaring no <c>nunit</c> selector at all, and so having no selector checked by
    /// <see cref="EveryNunitSelectorNamesSomethingThatExists"/>. At <c>46366ea</c> they are
    /// DAT-007, FND-001, FND-002, FND-005, PRE-001, PRE-002 and UI-002. No other assertion in this
    /// census implies this number, which is why it is here: it is the measure of the gap the
    /// exposure paragraphs describe, and those paragraphs had it wrong.
    /// </summary>
    private const int RegistriesWithNoNunitSelector = 7;

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
