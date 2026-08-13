using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// Every call site of <see cref="SemanticCheck.BehaviorToken"/> is reached by a fixture
/// that malforms only that site's field, and the population of call sites is counted from
/// the source rather than asserted from memory.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this fixture is for, and it is not a second copy of
/// <see cref="NestedBehaviorTokenTests"/>.</b> That fixture reaches the three nested
/// positions. This one states the whole population and reaches all of it. The distinction
/// matters because the defect being closed was never "one field is unchecked" - it was
/// that nothing anywhere said how many fields there are, so a call site could be added,
/// or left uncovered, with every assertion in the suite green.
/// </para>
/// <para>
/// <b>The measurement that produced this table.</b> Each call site was neutralised
/// individually - the value argument replaced by a well-formed literal, so the check still
/// runs and cannot fire - and the whole Content tier was run per site. At
/// <c>1b83ebd</c> that put 12 call sites on the table, of which 5 were reached by some
/// fixture and <b>7 were deletable with the tier green</b>:
/// <c>WeaponPriceFormulaDefinition</c>, <c>BossDefinition</c>, <c>WeaponDefinition</c>
/// three times, <c>UtilityDefinition</c> and <c>BranchDefinition</c>. Two of the five were
/// reached before <c>NestedBehaviorTokenTests</c> existed, not one: the enemy root
/// <c>behavior_kind</c> and the relic root <c>behavior_kind</c>.
/// </para>
/// <para>
/// <b>Why the count is taken from disk.</b> A table of sites is exactly as good as the
/// claim that it is complete, and a table cannot check its own completeness - every walk
/// over it agrees with it by construction. So
/// <see cref="TheCallSitesOnDiskAreExactlyTheOnesThisTableCovers"/> counts the call sites
/// in the category sources and compares them per file. An added call site with no fixture
/// is then a red test naming the file, which is the state this fixture exists to make
/// impossible.
/// </para>
/// <para>
/// <b>Pointers are built, not pasted.</b> Each row constructs its expected pointer with
/// <see cref="JsonPointer"/>, the same way the validator under test constructs the one it
/// reports. A literal would need a second test asserting the literal and the builder agree,
/// which is a weaker arrangement than not having two things to reconcile.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-002-037</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class BehaviorTokenCallSiteTests
{
    /// <summary>The category sources that hold a behavior-token call site.</summary>
    private static string CategorySourceDirectory => Path.Combine(
        TestArtifacts.RepositoryRoot, "src", "MechaMiner.Content", "Categories");

    /// <summary>The call being counted, spelled as it appears in the source.</summary>
    private const string CallSite = "SemanticCheck.BehaviorToken(";

    /// <summary>
    /// One row per call site: the source file that holds it, the fixture that malforms
    /// only that field, and the pointer the diagnostic must land on.
    /// </summary>
    /// <remarks>
    /// Three rows name <c>WeaponDefinition.cs</c> because it holds three call sites, and
    /// each has its own fixture. One fixture malforming all three would satisfy a
    /// contains-the-code assertion while leaving any two of the three deletable, which is
    /// the confusion the per-pointer assertion below exists to remove.
    /// </remarks>
    private static IEnumerable<TestCaseData> CallSites => new[]
    {
        Site(
            "WeaponPriceFormulaDefinition.cs",
            "weapons/invalid-price-formula-kind-prose.json",
            DefinitionKind.WeaponStatPriceFormula,
            JsonPointer.Root.AppendProperty("formula_kind"),
            "WeaponPriceFormulaFormulaKind"),
        Site(
            "BossDefinition.cs",
            "bosses/invalid-boss-behavior-kind-prose.json",
            DefinitionKind.Boss,
            JsonPointer.Root.AppendProperty("behavior_kind"),
            "BossBehaviorKind"),
        Site(
            "WeaponDefinition.cs",
            "weapons/invalid-weapon-behavior-kind-prose.json",
            DefinitionKind.Weapon,
            JsonPointer.Root.AppendProperty("behavior_kind"),
            "WeaponBehaviorKind"),
        Site(
            "WeaponDefinition.cs",
            "weapons/invalid-targeting-policy-prose.json",
            DefinitionKind.Weapon,
            JsonPointer.Root.AppendProperty("targeting_policy"),
            "WeaponTargetingPolicy"),
        Site(
            "WeaponDefinition.cs",
            "weapons/invalid-rock-targeting-behavior-prose.json",
            DefinitionKind.Weapon,
            JsonPointer.Root.AppendProperty("rock_targeting_behavior"),
            "WeaponRockTargetingBehavior"),
        Site(
            "UtilityDefinition.cs",
            "utilities/invalid-utility-behavior-kind-prose.json",
            DefinitionKind.Utility,
            JsonPointer.Root.AppendProperty("behavior_kind"),
            "UtilityBehaviorKind"),
        Site(
            "BranchDefinition.cs",
            "branches/invalid-branch-behavior-kind-prose.json",
            DefinitionKind.Branch,
            JsonPointer.Root.AppendProperty("behavior_kind"),
            "BranchBehaviorKind"),
        Site(
            "RelicDefinition.cs",
            "relics/invalid-relic-behavior-kind-prose.json",
            DefinitionKind.Relic,
            JsonPointer.Root.AppendProperty("behavior_kind"),
            "RelicBehaviorKind"),
        Site(
            "EnemyDefinition.cs",
            "enemies/invalid-behavior-kind-prose.json",
            DefinitionKind.Enemy,
            JsonPointer.Root.AppendProperty("behavior_kind"),
            "EnemyBehaviorKind"),
        Site(
            "EnemyDefinition.cs",
            "enemies/invalid-specialist-attack-kind-prose.json",
            DefinitionKind.Enemy,
            JsonPointer.Root.AppendProperty("specialist_attack").AppendProperty("kind"),
            "EnemySpecialistAttackKind"),
        Site(
            "MechDefinition.cs",
            "mechs/invalid-trait-behavior-kind-prose.json",
            DefinitionKind.Mech,
            JsonPointer.Root.AppendProperty("inherent_trait").AppendProperty("behavior_kind"),
            "MechInherentTraitBehaviorKind"),
        Site(
            "ResourceDefinition.cs",
            "resources/invalid-resonance-behavior-kind-prose.json",
            DefinitionKind.Resource,
            JsonPointer.Root.AppendProperty("resonance_behavior").AppendProperty("behavior_kind"),
            "ResourceResonanceBehaviorKind"),
    };

    private static TestCaseData Site(
        string sourceFile,
        string fixturePath,
        DefinitionKind kind,
        JsonPointer pointer,
        string name)
    {
        return new TestCaseData(sourceFile, fixturePath, kind, pointer.Value).SetName(name);
    }

    /// <summary>
    /// Each call site's fixture reports the malformed token at that site's own pointer,
    /// and reports exactly one grammar failure.
    /// </summary>
    /// <remarks>
    /// The code alone would be satisfied by a document failing at some other position, and
    /// for <c>WeaponDefinition</c> the other position is two lines away in the same method.
    /// The exactly-one clause is what keeps each fixture isolating its own field: a fixture
    /// that starts malforming a second token stops proving anything about the first.
    /// </remarks>
    [TestCaseSource(nameof(CallSites))]
    public void EachCallSiteReportsItsOwnFieldAtItsOwnPointer(
        string sourceFile, string fixturePath, DefinitionKind kind, string pointer)
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(fixturePath, kind);

        List<string> atPointer = new();
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            if (string.Equals(diagnostic.Location.Value, pointer, StringComparison.Ordinal))
            {
                atPointer.Add(diagnostic.Code);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                diagnostics,
                Is.Not.Empty,
                () => fixturePath + " must be rejected, or the " + CallSite + " in " + sourceFile
                    + " that reads " + pointer + " proves nothing");
            Assert.That(
                atPointer,
                Does.Contain(ContentDiagnosticCodes.BehaviorTokenMalformed),
                () => fixturePath + " must report "
                    + ContentDiagnosticCodes.BehaviorTokenMalformed + " at " + pointer
                    + " specifically; a malformed token reported at some other pointer would "
                    + "leave the call site in " + sourceFile + " unproven, and deleting that "
                    + "call site would stay green. Produced: " + Describe(diagnostics));
            Assert.That(
                Codes(diagnostics),
                Has.Exactly(1).EqualTo(ContentDiagnosticCodes.BehaviorTokenMalformed),
                () => fixturePath + " malforms exactly one token, so exactly one grammar "
                    + "failure may be reported; more than one means the fixture is not "
                    + "isolating " + pointer + ". Produced: " + Describe(diagnostics));
        });
    }

    /// <summary>
    /// The behavior-token call sites in the category sources are exactly the ones this
    /// table covers, counted per file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the assertion the table cannot make about itself.</b> Every other walk
    /// here iterates <see cref="CallSites"/>, so all of them agree with it by construction:
    /// a thirteenth call site added tomorrow with no fixture leaves each of them green and
    /// the suite proves one field less than it did. The count comes off disk instead.
    /// </para>
    /// <para>
    /// <b>Per file rather than in total.</b> A total would be satisfied by a call site
    /// added in one file while another lost one, which is exactly the shape a refactor
    /// takes. The comparison is a map from file name to number of calls, so a call that
    /// moves between files is reported as both a gain and a loss with the files named.
    /// </para>
    /// <para>
    /// The grep is deliberately textual, and its own weakness is stated rather than left to
    /// be discovered: a call written through an alias, or reformatted so the method name and
    /// its open paren fall on different lines, is not counted. Both would make the count
    /// disagree with the table and fail here rather than pass quietly - the failure names a
    /// file, and a reader who finds the call reformatted has been told where to look.
    /// </para>
    /// </remarks>
    [Test]
    public void TheCallSitesOnDiskAreExactlyTheOnesThisTableCovers()
    {
        Dictionary<string, int> onDisk = new(StringComparer.Ordinal);
        int filesRead = 0;

        foreach (string path in Directory.GetFiles(
                     CategorySourceDirectory, "*.cs", SearchOption.AllDirectories))
        {
            filesRead++;
            int calls = 0;
            foreach (string line in File.ReadAllLines(path))
            {
                if (line.Contains(CallSite, StringComparison.Ordinal))
                {
                    calls++;
                }
            }

            if (calls > 0)
            {
                onDisk[Path.GetFileName(path)] = calls;
            }
        }

        Dictionary<string, int> covered = new(StringComparer.Ordinal);
        int rows = 0;
        foreach (TestCaseData row in CallSites)
        {
            rows++;
            string sourceFile = (string)row.Arguments[0]!;
            covered.TryGetValue(sourceFile, out int count);
            covered[sourceFile] = count + 1;
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                filesRead,
                Is.GreaterThan(0),
                CategorySourceDirectory + " held no C# source, so this count is a walk over "
                    + "nothing and every comparison below passes for the wrong reason");
            Assert.That(
                onDisk,
                Is.Not.Empty,
                "no " + CallSite + " was found in the category sources at all. Either the "
                    + "check was renamed - in which case this whole fixture is asserting "
                    + "nothing and the name in " + nameof(CallSite) + " must move with it - or "
                    + "the calls are gone and every fixture below is testing an absence");
            Assert.That(
                onDisk,
                Is.EquivalentTo(covered),
                () => "the " + CallSite + " calls on disk are not the ones this table covers. "
                    + "On disk: " + Render(onDisk) + ". Covered by this table: " + Render(covered)
                    + ". A call site with no row is a field whose grammar nothing checks, and "
                    + "deleting it would leave the tier green - which is the defect this "
                    + "fixture exists to keep closed. A row with no call site is a fixture "
                    + "asserting against a check that has moved. Add the fixture, or move the "
                    + "row, in the same change as the call");
            Assert.That(
                rows,
                Is.EqualTo(TheCallSiteCount),
                "the number of call sites this table covers. " + nameof(TheCallSiteCount)
                    + " is written out rather than counted from the table because a count "
                    + "taken from the table equals the table for any table; changing it is a "
                    + "deliberate statement that the population moved");
        });
    }

    /// <summary>
    /// How many behavior-token call sites there are, written out.
    /// </summary>
    /// <remarks>
    /// Measured at <c>1b83ebd</c> by neutralising each call site individually and running
    /// the whole Content tier per site: 12 sites, 5 of them reached by a fixture and 7
    /// deletable with the tier green. This literal is the one reader of that measurement
    /// that does not move when the table does.
    /// </remarks>
    private const int TheCallSiteCount = 12;

    private static string Render(Dictionary<string, int> counts)
    {
        List<string> parts = new(counts.Count);
        foreach (KeyValuePair<string, int> pair in counts)
        {
            parts.Add(pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture));
        }

        parts.Sort(StringComparer.Ordinal);
        return string.Join(", ", parts);
    }

    private static IReadOnlyList<ContentDiagnostic> Read(string path, DefinitionKind kind)
    {
        return CategorySchemas.Read(
            CategoryFixtureCorpus.Read(path),
            new CategoryReadContext(CategoryFixtureCorpus.SourcePathOf(path), kind)).Diagnostics;
    }

    private static IReadOnlyList<string> Codes(IReadOnlyList<ContentDiagnostic> diagnostics)
    {
        List<string> codes = new(diagnostics.Count);
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            codes.Add(diagnostic.Code);
        }

        return codes;
    }

    private static string Describe(IReadOnlyList<ContentDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics);
    }
}
