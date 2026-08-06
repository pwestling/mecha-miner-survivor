using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Relational;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// The catalog-level cardinality, coverage, distribution, and total rules, each with a
/// negative control that violates exactly that rule.
/// </summary>
/// <remarks>
/// <para>
/// Every rule here needs more than one definition to decide, which is what puts it in
/// doc 40 § Relational rather than § Semantic. The positive case uses the valid
/// fixtures; the negative case adds one <c>catalog-</c> fixture that is individually
/// valid and makes the catalog wrong. That pairing is what proves the rule is about the
/// catalog and not about the file.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-002-002</c> through <c>VER-DAT-002-007</c>,
/// <c>VER-DAT-002-016</c> through <c>VER-DAT-002-021</c>, and
/// <c>VER-DAT-003-001</c> through <c>VER-DAT-003-025</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CategorySemanticRuleTests
{
    private const string CatalogPath = "content/";

    [Test]
    public void TheSixMaterialSetIsCoveredExactlyOnceEach()
    {
        List<ResourceDefinition> resources = SixMaterials();

        DiagnosticBag bag = new();
        CatalogChecks.SixMaterialSetIsExactlyCovered(resources, CatalogPath, bag);

        Assert.That(
            bag.Diagnostics,
            Is.Empty,
            () => "six lettered resources must cover A through F exactly once: "
                + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void ADuplicatedCanonicalLetterFailsTheSixMaterialSet()
    {
        List<ResourceDefinition> resources = new()
        {
            Load<ResourceDefinition>(
                "resources/valid-specialized-material.json", DefinitionKind.Resource),
            Load<ResourceDefinition>(
                "resources/catalog-duplicate-canonical-letter.json", DefinitionKind.Resource),
        };

        DiagnosticBag bag = new();
        CatalogChecks.SixMaterialSetIsExactlyCovered(resources, CatalogPath, bag);

        Assert.That(
            Codes(bag),
            Does.Contain(ContentDiagnosticCodes.CatalogDuplicateIdentity),
            () => "two resources claiming letter A must collide: "
                + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void AMissingCanonicalLetterFailsTheSixMaterialSet()
    {
        List<ResourceDefinition> resources = SixMaterials();
        resources.RemoveAt(resources.Count - 1);

        DiagnosticBag bag = new();
        CatalogChecks.SixMaterialSetIsExactlyCovered(resources, CatalogPath, bag);

        Assert.That(
            Codes(bag),
            Does.Contain(ContentDiagnosticCodes.CatalogSetNotCoveredExactly),
            () => "a letter with no resource must be reported: "
                + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void MechSelectionOrdersAreUniqueAndADuplicateIsCaught()
    {
        MechDefinition first = Load<MechDefinition>("mechs/valid-mech.json", DefinitionKind.Mech);
        MechDefinition second = Load<MechDefinition>(
            "mechs/catalog-duplicate-selection-order.json", DefinitionKind.Mech);

        DiagnosticBag clean = new();
        CatalogChecks.MechSelectionOrdersAreUnique(new[] { first }, CatalogPath, clean);

        DiagnosticBag collided = new();
        CatalogChecks.MechSelectionOrdersAreUnique(
            new[] { first, second }, CatalogPath, collided);

        Expect.Multiple(() =>
        {
            Assert.That(clean.Diagnostics, Is.Empty, "one mech cannot collide with itself");
            Assert.That(
                Codes(collided),
                Does.Contain(ContentDiagnosticCodes.CatalogDuplicateIdentity),
                () => "two mechs at selection order 1 must collide: "
                    + string.Join("; ", collided.Diagnostics));
        });
    }

    [Test]
    public void TheFourSiteClassesAreCoveredExactlyOnceEach()
    {
        List<MiningSiteDefinition> sites = FourSites();

        DiagnosticBag bag = new();
        CatalogChecks.FourSiteClassesAreExactlyCovered(sites, CatalogPath, bag);

        Assert.That(
            bag.Diagnostics,
            Is.Empty,
            () => "the four accepted classes must be covered exactly once each: "
                + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void ADuplicatedSiteClassFailsTheFourClassSet()
    {
        List<MiningSiteDefinition> sites = FourSites();
        sites.Add(Load<MiningSiteDefinition>(
            "mining-sites/catalog-duplicate-site-class.json", DefinitionKind.MiningSite));

        DiagnosticBag bag = new();
        CatalogChecks.FourSiteClassesAreExactlyCovered(sites, CatalogPath, bag);

        Assert.That(
            Codes(bag),
            Does.Contain(ContentDiagnosticCodes.CatalogDuplicateIdentity),
            () => "two definitions of one class must collide: "
                + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void AMissingSiteClassFailsTheFourClassSet()
    {
        List<MiningSiteDefinition> sites = FourSites();
        sites.RemoveAt(sites.Count - 1);

        DiagnosticBag bag = new();
        CatalogChecks.FourSiteClassesAreExactlyCovered(sites, CatalogPath, bag);

        Assert.That(
            Codes(bag),
            Does.Contain(ContentDiagnosticCodes.CatalogSetNotCoveredExactly),
            () => "an uncovered class must be reported: " + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void TheWeaponsCatalogHoldsFifteenDistinctRecipePairs()
    {
        DiagnosticBag bag = new();
        CatalogChecks.FifteenDistinctRecipePairs(FifteenWeapons(), CatalogPath, bag);

        Assert.That(
            bag.Diagnostics,
            Is.Empty,
            () => "fifteen distinct pairs must pass: " + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void ARecipePairReusedInTheOtherOrderCollides()
    {
        List<WeaponDefinition> weapons = new()
        {
            Load<WeaponDefinition>("weapons/valid-weapon.json", DefinitionKind.Weapon),
            Load<WeaponDefinition>(
                "weapons/catalog-duplicate-recipe-pair.json", DefinitionKind.Weapon),
        };

        DiagnosticBag bag = new();
        CatalogChecks.FifteenDistinctRecipePairs(weapons, CatalogPath, bag);

        Assert.That(
            Codes(bag),
            Does.Contain(ContentDiagnosticCodes.CatalogDuplicateIdentity),
            () => "the same pair in the other order must collide, because a material pair is "
                + "unordered: " + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void AWeaponCountShortOfFifteenFails()
    {
        List<WeaponDefinition> weapons = FifteenWeapons();
        weapons.RemoveAt(weapons.Count - 1);

        DiagnosticBag bag = new();
        CatalogChecks.FifteenDistinctRecipePairs(weapons, CatalogPath, bag);

        Assert.That(
            Codes(bag),
            Does.Contain(ContentDiagnosticCodes.CatalogCardinalityWrong),
            () => "fourteen distinct pairs must fail the count: "
                + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void RecipeLettersSpellTheWeaponIdAndAMismatchIsCaught()
    {
        List<ResourceDefinition> resources = SixMaterials();
        WeaponDefinition good = Load<WeaponDefinition>(
            "weapons/valid-weapon.json", DefinitionKind.Weapon);
        WeaponDefinition bad = Load<WeaponDefinition>(
            "weapons/catalog-recipe-letters-mismatch.json", DefinitionKind.Weapon);

        DiagnosticBag clean = new();
        CatalogChecks.RecipeLettersSpellTheWeaponId(
            new[] { good }, resources, CatalogPath, clean);

        DiagnosticBag mismatched = new();
        CatalogChecks.RecipeLettersSpellTheWeaponId(
            new[] { bad }, resources, CatalogPath, mismatched);

        Expect.Multiple(() =>
        {
            Assert.That(
                clean.Diagnostics,
                Is.Empty,
                () => "W-AB's recipe must resolve to the letters A and B: "
                    + string.Join("; ", clean.Diagnostics));
            Assert.That(
                Codes(mismatched),
                Does.Contain(ContentDiagnosticCodes.RecipeLettersMismatch),
                () => "a recipe resolving to AC under the ID W-AB must fail; this is the check "
                    + "that replaces reading the pair by eye: "
                    + string.Join("; ", mismatched.Diagnostics));
        });
    }

    [Test]
    public void EachWeaponHasOneBranchOfEachClassAndADoubledClassIsCaught()
    {
        List<BranchDefinition> good = ThreeBranches();
        List<BranchDefinition> bad = new()
        {
            Load<BranchDefinition>("branches/valid-branch.json", DefinitionKind.Branch),
            Load<BranchDefinition>(
                "branches/catalog-duplicate-branch-class.json", DefinitionKind.Branch),
            good[2],
        };

        DiagnosticBag clean = new();
        CatalogChecks.EachWeaponHasOneBranchOfEachClass(good, CatalogPath, clean);

        DiagnosticBag doubled = new();
        CatalogChecks.EachWeaponHasOneBranchOfEachClass(bad, CatalogPath, doubled);

        Expect.Multiple(() =>
        {
            Assert.That(
                clean.Diagnostics,
                Is.Empty,
                () => "one of each class must pass: " + string.Join("; ", clean.Diagnostics));
            Assert.That(
                Codes(doubled),
                Does.Contain(ContentDiagnosticCodes.BranchClassDistributionWrong),
                () => "three branches of two classes must fail even though the count is three: "
                    + string.Join("; ", doubled.Diagnostics));
        });
    }

    [Test]
    public void TheUtilityPoolDistributionIsTheAcceptedOne()
    {
        DiagnosticBag bag = new();
        CatalogChecks.UtilityPoolDistributionIsAccepted(ThirteenUtilities(), CatalogPath, bag);

        Assert.That(
            bag.Diagnostics,
            Is.Empty,
            () => "six fresh, six unlocked and one always-available must pass: "
                + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void MovingOneUtilityBetweenPoolsFailsTheDistribution()
    {
        List<UtilityDefinition> utilities = ThirteenUtilities();
        utilities.RemoveAt(utilities.Count - 2);
        utilities.Add(Load<UtilityDefinition>(
            "utilities/catalog-shifted-pool-availability.json", DefinitionKind.Utility));

        DiagnosticBag bag = new();
        CatalogChecks.UtilityPoolDistributionIsAccepted(utilities, CatalogPath, bag);

        Assert.That(
            Codes(bag),
            Does.Contain(ContentDiagnosticCodes.CatalogCardinalityWrong),
            () => "moving one utility from one pool to the other must fail even though every "
                + "file stays individually valid: " + string.Join("; ", bag.Diagnostics));
    }

    [Test]
    public void NoTwoUtilitiesShareAnInstalledIdentity()
    {
        UtilityDefinition first = Load<UtilityDefinition>(
            "utilities/valid-utility.json", DefinitionKind.Utility);
        UtilityDefinition radar = Load<UtilityDefinition>(
            "utilities/valid-utility-radar.json", DefinitionKind.Utility);
        UtilityDefinition duplicate = Load<UtilityDefinition>(
            "utilities/catalog-duplicate-installed-identity.json", DefinitionKind.Utility);

        DiagnosticBag clean = new();
        CatalogChecks.NoDuplicateInstalledUtilityIdentity(
            new[] { first, radar }, CatalogPath, clean);

        DiagnosticBag collided = new();
        CatalogChecks.NoDuplicateInstalledUtilityIdentity(
            new[] { first, duplicate }, CatalogPath, collided);

        Expect.Multiple(() =>
        {
            Assert.That(clean.Diagnostics, Is.Empty, "two distinct identities must pass");
            Assert.That(
                Codes(collided),
                Does.Contain(ContentDiagnosticCodes.CatalogDuplicateIdentity),
                () => "the same material and role under a different ID must still collide: "
                    + string.Join("; ", collided.Diagnostics));
        });
    }

    [Test]
    public void ThePowerUpCatalogTotalIsRecomputedFromTheParts()
    {
        PowerUpDefinition one = Load<PowerUpDefinition>(
            "powerups/valid-powerup.json", DefinitionKind.PowerUp);

        DiagnosticBag shortOfTotal = new();
        CatalogChecks.PowerUpCatalogTotalIsAccepted(new[] { one }, CatalogPath, shortOfTotal);

        List<PowerUpDefinition> whole = new();
        for (int index = 0; index < 9; index++)
        {
            whole.Add(one);
        }

        DiagnosticBag stillShort = new();
        CatalogChecks.PowerUpCatalogTotalIsAccepted(whole, CatalogPath, stillShort);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                1000, one.TotalCostHyperGold, "one PowerUp's rank prices");
            Assert.That(
                Codes(shortOfTotal),
                Does.Contain(ContentDiagnosticCodes.CatalogTotalMismatch),
                () => "one PowerUp alone does not sum to the catalog total, and the check must "
                    + "say so rather than pass: " + string.Join("; ", shortOfTotal.Diagnostics));
            Assert.That(
                Codes(stillShort),
                Does.Contain(ContentDiagnosticCodes.CatalogTotalMismatch),
                "nine thousand is not nine thousand four hundred and fifty");
        });
    }

    [Test]
    public void TheUnlockCatalogTotalIsRecomputedFromTheParts()
    {
        UnlockDefinition one = Load<UnlockDefinition>(
            "unlocks/valid-unlock.json", DefinitionKind.Unlock);

        DiagnosticBag bag = new();
        CatalogChecks.UnlockCatalogTotalIsAccepted(new[] { one }, CatalogPath, bag);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(600, one.CostHyperGold, "one unlock's cost");
            Assert.That(
                Codes(bag),
                Does.Contain(ContentDiagnosticCodes.CatalogTotalMismatch),
                () => "one unlock alone does not sum to the catalog total: "
                    + string.Join("; ", bag.Diagnostics));
        });
    }

    private static List<ResourceDefinition> SixMaterials()
    {
        List<ResourceDefinition> resources = new();
        string[] letters = { "A", "B", "C", "D", "E", "F" };
        for (int index = 0; index < letters.Length; index++)
        {
            string id = "RSC-0" + (index + 1).ToString(CultureInfo.InvariantCulture);
            resources.Add(FixtureDocument
                .Load("resources/valid-specialized-material.json")
                .WithId("RSC-01", id)
                .With("canonical_letter", letters[index])
                .Read<ResourceDefinition>(DefinitionKind.Resource, id));
        }

        return resources;
    }

    private static List<MiningSiteDefinition> FourSites()
    {
        List<MiningSiteDefinition> sites = new();
        string[] classes =
        {
            "standard-ore-seam", "rich-ore-seam", "specialized-material-geode",
            "hyper-gold-site",
        };

        for (int index = 0; index < classes.Length; index++)
        {
            string id = "SITE-0" + (index + 1).ToString(CultureInfo.InvariantCulture);
            string source = classes[index] == "specialized-material-geode"
                ? "mining-sites/valid-geode.json"
                : "mining-sites/valid-standard-ore-seam.json";

            FixtureDocument document = FixtureDocument
                .Load(source)
                .WithId(classes[index] == "specialized-material-geode" ? "SITE-04" : "SITE-01", id)
                .With("site_class", classes[index]);

            if (classes[index] == "hyper-gold-site")
            {
                document = document
                    .With("beacon_thresholds", BeaconThresholds())
                    .With("beacon_rules", FixtureDocument.Strings(
                        new[] { "The beacon fires once per crossing." }));
            }

            sites.Add(document.Read<MiningSiteDefinition>(DefinitionKind.MiningSite, id));
        }

        return sites;
    }

    private static JsonArray BeaconThresholds()
    {
        JsonArray rows = new();
        rows.Add(new JsonObject { ["trigger_kind"] = "activation" });
        foreach (int percent in new[] { 25, 50, 75 })
        {
            rows.Add(new JsonObject
            {
                ["trigger_kind"] = "progress-threshold",
                ["trigger_progress_percent"] = percent,
            });
        }

        return rows;
    }

    private static List<WeaponDefinition> FifteenWeapons()
    {
        List<WeaponDefinition> weapons = new();
        string[] letters = { "A", "B", "C", "D", "E", "F" };

        for (int left = 0; left < letters.Length; left++)
        {
            for (int right = left + 1; right < letters.Length; right++)
            {
                string id = "W-" + letters[left] + letters[right];
                string[] recipe =
                {
                    "RSC-0" + (left + 1).ToString(CultureInfo.InvariantCulture),
                    "RSC-0" + (right + 1).ToString(CultureInfo.InvariantCulture),
                };

                weapons.Add(FixtureDocument
                    .Load("weapons/valid-weapon.json")
                    .WithId("W-AB", id)
                    .With("recipe_pair_material_ids", FixtureDocument.Strings(recipe))
                    .With("branch_ids", FixtureDocument.Strings(
                        new[] { id + "-one", id + "-two", id + "-three" }))
                    .Read<WeaponDefinition>(DefinitionKind.Weapon, id));
            }
        }

        return weapons;
    }

    private static List<BranchDefinition> ThreeBranches()
    {
        List<BranchDefinition> branches = new();
        string[] classes = { "amplification", "functional", "conversion" };

        foreach (string token in classes)
        {
            string id = "W-AB-" + token + "-branch";
            branches.Add(FixtureDocument
                .Load("branches/valid-branch.json")
                .WithId("W-AB-unbounded-bore", id)
                .With("branch_class", token)
                .Read<BranchDefinition>(DefinitionKind.Branch, id));
        }

        return branches;
    }

    private static List<UtilityDefinition> ThirteenUtilities()
    {
        List<UtilityDefinition> utilities = new();
        string[] materials = { "A", "B", "C", "D", "E", "F" };

        for (int index = 0; index < UtilitySchema.FreshProfileCount; index++)
        {
            utilities.Add(Utility(
                "UTL-" + materials[index] + "1", index, "fresh-profile"));
        }

        for (int index = 0; index < UtilitySchema.HyperGoldUnlockCount; index++)
        {
            utilities.Add(Utility(
                "UTL-" + materials[index] + "2", index, "hyper-gold-unlock"));
        }

        utilities.Add(FixtureDocument
            .Load("utilities/valid-utility-radar.json")
            .Read<UtilityDefinition>(DefinitionKind.Utility, "UTL-R1"));

        return utilities;
    }

    private static UtilityDefinition Utility(string id, int materialIndex, string pool)
    {
        JsonObject availability = new() { ["pool_availability"] = pool };
        if (pool == "hyper-gold-unlock")
        {
            availability["unlock_id"] = "UNL-01";
        }

        return FixtureDocument
            .Load("utilities/valid-utility.json")
            .WithId("UTL-D2", id)
            .With("material_id",
                "RSC-0" + (materialIndex + 1).ToString(CultureInfo.InvariantCulture))
            .With("primary_role", id + " role")
            .With("availability", availability)
            .Read<UtilityDefinition>(DefinitionKind.Utility, id);
    }

    private static TDefinition Load<TDefinition>(string path, DefinitionKind kind)
        where TDefinition : ContentDefinition
    {
        CategoryFixture fixture = new(path, kind, expectedCode: null);
        DefinitionReadResult result = CategoryFixtureCorpus.ReadDefinition(fixture);

        Assert.That(
            result.IsValid,
            Is.True,
            () => path + " must load before a catalog rule can be checked against it: "
                + string.Join("; ", result.Diagnostics));

        return (TDefinition)result.Definition!;
    }

    private static IReadOnlyList<string> Codes(DiagnosticBag bag)
    {
        return bag.Codes();
    }
}
