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
/// nothing else, so eighteen of the twenty-one registries on disk were validated by nobody
/// and no test said so. The set now comes from
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
    /// Every test in this fixture is a walk over <c>entries</c>, and a walk over nothing
    /// reports nothing wrong. Emptying the array - by hand, by a bad merge, or by a
    /// generator that wrote an empty document - satisfied all five of them at once, which
    /// is the one edit to a registry that no test could see. Each walk now counts what
    /// it visited, in the same shape as <c>SchemaNullScan.DocumentsSeen</c> and
    /// <c>SchemaFixturePartition.FilesChecked</c> elsewhere in this suite, and the
    /// counters are counts rather than constants because emptying the array turns every
    /// one of them red.
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
    /// Every cited file exists and, where a citation names a heading anchor, that heading
    /// exists in the cited file.
    /// </summary>
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
    /// <c>Assembly.GetType</c> against this assembly alone, because 110 of the selectors on
    /// disk name fixtures in the three sibling test projects this project does not reference
    /// and could never have resolved. See that class for what the route proves.
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

        // No "at least one nunit selector" assertion here, deliberately. DAT-007 and the FND
        // registries are script and command gates and legitimately declare none, so a
        // per-registry non-zero requirement would be false for them. The non-vacuity guarantee
        // this walk needs is held one level up, over the whole set, by
        // EverySelectorKindIsOneSomeWalkResolves.
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
    /// registry made entirely of <c>script</c> or <c>command</c> selectors passes it while
    /// having no selector checked at all - which is the state DAT-007, FND-001, FND-002 and
    /// FND-005 are in. That is a real gap and this assertion is deliberately <em>not</em> a
    /// fix for it: it records which kinds exist and which are resolvable, so the gap is
    /// stated by a test instead of living in a report. Teaching the walk to resolve
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
    /// the census is written to the run's output, so 22 references that no test can verify are
    /// visible as 22 rather than invisible among 454.
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
    private const int RepositoryPathReferences = 319;

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
