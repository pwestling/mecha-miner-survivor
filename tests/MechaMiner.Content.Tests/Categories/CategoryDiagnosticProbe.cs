using System;
using System.Collections.Generic;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Relational;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// Every diagnostic code the DAT-002 and DAT-003 suites provoke, collected so the
/// declared-code gate can compare the two sets.
/// </summary>
/// <remarks>
/// <para>
/// <c>ContentDiagnosticCodesTests</c> asserts that the set of codes the suite provokes
/// equals the set declared. Both directions matter: an undeclared code a validator can
/// emit is unenumerable, and a declared code nothing provokes is untested and will
/// first fire in a build nobody expected it in. This type is how the category and
/// relational stages contribute their half of the "provoked" set.
/// </para>
/// <para>
/// It re-runs the same corpora and negative catalogs the category tests use rather than
/// listing codes by hand: a list would let a code be claimed as provoked by a test that
/// no longer provokes it.
/// </para>
/// </remarks>
internal static class CategoryDiagnosticProbe
{
    /// <summary>Every code the category, catalog, and relational suites provoke.</summary>
    internal static IReadOnlyCollection<string> Provoked()
    {
        HashSet<string> provoked = new(StringComparer.Ordinal);

        foreach (CategoryFixture fixture in CategoryFixtureCorpus.Invalid)
        {
            DefinitionReadResult result = CategoryFixtureCorpus.ReadDefinition(fixture);
            foreach (ContentDiagnostic diagnostic in result.Diagnostics)
            {
                provoked.Add(diagnostic.Code);
            }
        }

        foreach (string code in FromCatalogChecks())
        {
            provoked.Add(code);
        }

        foreach (string code in FromRelationalChecks())
        {
            provoked.Add(code);
        }

        return provoked;
    }

    private static IEnumerable<string> FromCatalogChecks()
    {
        DiagnosticBag bag = new();

        // Every catalog check run against a catalog that violates it. These are the same
        // negative cases CategorySemanticRuleTests asserts on individually; running them
        // here as well is what keeps the declared-code set honest when a check is added.
        List<ResourceDefinition> lettered = new()
        {
            Resource("A"),
            Resource("A"),
        };
        CatalogChecks.SixMaterialSetIsExactlyCovered(lettered, "content/", bag);

        CatalogChecks.FourSiteClassesAreExactlyCovered(
            Array.Empty<MiningSiteDefinition>(), "content/", bag);

        CatalogChecks.FifteenDistinctRecipePairs(
            Array.Empty<WeaponDefinition>(), "content/", bag);

        CatalogChecks.RecipeLettersSpellTheWeaponId(
            new[]
            {
                FixtureDocument
                    .Load("weapons/catalog-recipe-letters-mismatch.json")
                    .Read<WeaponDefinition>(DefinitionKind.Weapon, "W-AB"),
            },
            new[] { Resource("A"), Resource("C") },
            "content/",
            bag);

        CatalogChecks.EachWeaponHasOneBranchOfEachClass(
            new[]
            {
                FixtureDocument
                    .Load("branches/valid-branch.json")
                    .Read<BranchDefinition>(DefinitionKind.Branch, "W-AB-unbounded-bore"),
            },
            "content/",
            bag);

        CatalogChecks.UtilityPoolDistributionIsAccepted(
            Array.Empty<UtilityDefinition>(), "content/", bag);

        UtilityDefinition utility = FixtureDocument
            .Load("utilities/valid-utility.json")
            .Read<UtilityDefinition>(DefinitionKind.Utility, "UTL-D2");
        CatalogChecks.NoDuplicateInstalledUtilityIdentity(
            new[] { utility, utility }, "content/", bag);

        CatalogChecks.PowerUpCatalogTotalIsAccepted(
            Array.Empty<PowerUpDefinition>(), "content/", bag);
        CatalogChecks.UnlockCatalogTotalIsAccepted(
            Array.Empty<UnlockDefinition>(), "content/", bag);

        MechDefinition mech = FixtureDocument
            .Load("mechs/valid-mech.json")
            .Read<MechDefinition>(DefinitionKind.Mech, "MCH-01");
        CatalogChecks.MechSelectionOrdersAreUnique(new[] { mech, mech }, "content/", bag);

        return bag.Codes();
    }

    private static IEnumerable<string> FromRelationalChecks()
    {
        DiagnosticBag bag = new();
        UtilityDefinition utility = FixtureDocument
            .Load("utilities/valid-utility.json")
            .Read<UtilityDefinition>(DefinitionKind.Utility, "UTL-D2");

        // An empty catalog cannot resolve an operand; a geode whose field only exceeds
        // the base zone violates the relation. Between them the two relational codes are
        // both reached.
        RelationalConstraints.Evaluate(RelationalCatalog.Empty, "content/", bag);

        List<ContentDefinition> tooTight = new()
        {
            FixtureDocument
                .Load("mining-sites/valid-geode.json")
                .WithIn(
                    "resonance_field",
                    "radius_m",
                    System.Text.Json.Nodes.JsonValue.Create(3.5))
                .Read<MiningSiteDefinition>(DefinitionKind.MiningSite, "SITE-04"),
            FixtureDocument
                .Load("maps/valid-map-contract.json")
                .Read<MapGenerationDefinition>(DefinitionKind.MapGenerationContract, "MGC-01"),
            FixtureDocument
                .Load("player/valid-player-baseline.json")
                .Read<PlayerBaselineDefinition>(DefinitionKind.PlayerBaseline, "PLAYER-01"),

            // The relation sums the catalog's own extraction-zone modifiers, so the
            // catalog has to contain them for the expanded zone to exceed 3.5 M.
            utility,
            FixtureDocument
                .Load("powerups/valid-powerup.json")
                .With("affected_statistic", "extraction-zone-radius")
                .Read<PowerUpDefinition>(DefinitionKind.PowerUp, "PU-E02"),
        };

        RelationalConstraints.Evaluate(new RelationalCatalog(tooTight), "content/", bag);
        return bag.Codes();
    }

    private static ResourceDefinition Resource(string letter)
    {
        return FixtureDocument
            .Load("resources/valid-specialized-material.json")
            .With("canonical_letter", letter)
            .Read<ResourceDefinition>(DefinitionKind.Resource, "RSC-" + letter);
    }
}
