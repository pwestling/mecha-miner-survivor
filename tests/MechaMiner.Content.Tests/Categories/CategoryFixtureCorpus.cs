using System.Collections.Generic;
using System.IO;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// The DAT-002 and DAT-003 fixture corpus: which file proves which diagnostic.
/// </summary>
/// <remarks>
/// <para>
/// Three buckets, and the difference between them is what a fixture can decide on
/// its own.
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Valid"/> must produce zero diagnostics. Without this bucket the invalid one could be satisfied by a validator that rejects everything, and a field table could quietly require a field no document asks for.</description></item>
/// <item><description><see cref="Invalid"/> must produce the one diagnostic code it is named for. A fixture that fails with a different code fails the suite, because a gate that passes on the wrong error is not a gate.</description></item>
/// <item><description><see cref="CatalogOnly"/> is individually valid and makes its <em>catalog</em> invalid. These are named <c>catalog-</c> rather than <c>invalid-</c> precisely so that asserting they produce zero per-file diagnostics does not read as a contradiction. They are exercised by <see cref="CategorySemanticRuleTests"/> against a second definition.</description></item>
/// </list>
/// <para>
/// Every expected code is the <see cref="ContentDiagnosticCodes"/> constant itself, so
/// renaming a constant is a compile error rather than a corpus that silently stops
/// asserting anything.
/// </para>
/// </remarks>
internal static class CategoryFixtureCorpus
{
    /// <summary>The absolute path of the category fixture directory.</summary>
    internal static string Root { get; } = Path.Combine(
        TestArtifacts.TestProjectDirectory, "Fixtures", "categories");

    /// <summary>Every fixture that must validate cleanly.</summary>
    internal static IReadOnlyList<CategoryFixture> Valid { get; } = new[]
    {
        Good("resources/valid-specialized-material.json", DefinitionKind.Resource),
        Good("resources/valid-currency-hyper-gold.json", DefinitionKind.Resource),
        Good("mechs/valid-mech.json", DefinitionKind.Mech),
        Good("enemies/valid-enemy.json", DefinitionKind.Enemy),
        Good("enemies/valid-elite-modifiers.json", DefinitionKind.EliteModifiers),
        Good("bosses/valid-boss.json", DefinitionKind.Boss),
        Good("mining-sites/valid-standard-ore-seam.json", DefinitionKind.MiningSite),
        Good("mining-sites/valid-geode.json", DefinitionKind.MiningSite),
        Good("encounters/valid-schedule.json", DefinitionKind.EncounterSchedule),
        Good("maps/valid-map-contract.json", DefinitionKind.MapGenerationContract),
        Good("player/valid-player-baseline.json", DefinitionKind.PlayerBaseline),
        Good("weapons/valid-weapon.json", DefinitionKind.Weapon),
        Good("weapons/valid-price-formula.json", DefinitionKind.WeaponStatPriceFormula),
        Good("branches/valid-branch.json", DefinitionKind.Branch),
        Good("utilities/valid-utility.json", DefinitionKind.Utility),
        Good("utilities/valid-utility-radar.json", DefinitionKind.Utility),
        Good("relics/valid-relic-fresh.json", DefinitionKind.Relic),
        Good("relics/valid-relic-unlocked.json", DefinitionKind.Relic),
        Good("powerups/valid-powerup.json", DefinitionKind.PowerUp),
        Good("unlocks/valid-unlock.json", DefinitionKind.Unlock),
    };

    /// <summary>
    /// Fixtures that are individually valid and invalidate their catalog. They belong
    /// to the zero-diagnostic bucket for the per-file pass and to the semantic-rule
    /// tests for the catalog pass.
    /// </summary>
    internal static IReadOnlyList<CategoryFixture> CatalogOnly { get; } = new[]
    {
        Good("resources/catalog-duplicate-canonical-letter.json", DefinitionKind.Resource),
        Good("mechs/catalog-duplicate-selection-order.json", DefinitionKind.Mech),
        Good("mining-sites/catalog-duplicate-site-class.json", DefinitionKind.MiningSite),
        Good("weapons/catalog-duplicate-recipe-pair.json", DefinitionKind.Weapon),
        Good("weapons/catalog-recipe-letters-mismatch.json", DefinitionKind.Weapon),
        Good("weapons/catalog-recipe-pair-reversed.json", DefinitionKind.Weapon),
        Good("branches/catalog-duplicate-branch-class.json", DefinitionKind.Branch),
        Good("utilities/catalog-duplicate-installed-identity.json", DefinitionKind.Utility),
        Good("utilities/catalog-shifted-pool-availability.json", DefinitionKind.Utility),
    };

    /// <summary>Every fixture that must fail with exactly the code it is named for.</summary>
    internal static IReadOnlyList<CategoryFixture> Invalid { get; } = new[]
    {
        // --- resources ------------------------------------------------------
        Bad("resources/invalid-unknown-field.json", DefinitionKind.Resource,
            ContentDiagnosticCodes.UnknownField),
        Bad("resources/invalid-canonical-letter-on-currency.json", DefinitionKind.Resource,
            ContentDiagnosticCodes.ConditionalFieldForbidden),
        Bad("resources/invalid-canonical-letter-missing.json", DefinitionKind.Resource,
            ContentDiagnosticCodes.ConditionalFieldMissing),
        Bad("resources/invalid-resource-class-token.json", DefinitionKind.Resource,
            ContentDiagnosticCodes.TokenOutsideVocabulary),

        // --- mechs ----------------------------------------------------------
        Bad("mechs/invalid-signature-weapon-display-name.json", DefinitionKind.Mech,
            ContentDiagnosticCodes.ReferenceGrammarMismatch),
        Bad("mechs/invalid-trait-modifier-kind.json", DefinitionKind.Mech,
            ContentDiagnosticCodes.TokenOutsideVocabulary),

        // --- enemies --------------------------------------------------------
        Bad("enemies/invalid-armor-declared.json", DefinitionKind.Enemy,
            ContentDiagnosticCodes.UnknownField),
        Bad("enemies/invalid-derived-contact-diameter.json", DefinitionKind.Enemy,
            ContentDiagnosticCodes.DerivedValueAuthored),
        Bad("enemies/invalid-derived-center-distance.json", DefinitionKind.Enemy,
            ContentDiagnosticCodes.DerivedValueAuthored),
        Bad("enemies/invalid-derived-world-speed.json", DefinitionKind.Enemy,
            ContentDiagnosticCodes.DerivedValueAuthored),
        Bad("enemies/invalid-behavior-kind-prose.json", DefinitionKind.Enemy,
            ContentDiagnosticCodes.BehaviorTokenMalformed),
        Bad("enemies/invalid-elite-field-on-enemy.json", DefinitionKind.Enemy,
            ContentDiagnosticCodes.UnknownField),
        Bad("enemies/invalid-enemy-field-on-elite.json", DefinitionKind.EliteModifiers,
            ContentDiagnosticCodes.UnknownField),

        // --- bosses ---------------------------------------------------------
        Bad("bosses/invalid-armor-declared.json", DefinitionKind.Boss,
            ContentDiagnosticCodes.UnknownField),
        Bad("bosses/invalid-derived-center-distance.json", DefinitionKind.Boss,
            ContentDiagnosticCodes.DerivedValueAuthored),
        Bad("bosses/invalid-ability-arm-mismatch.json", DefinitionKind.Boss,
            ContentDiagnosticCodes.DiscriminatorArmMismatch),
        Bad("bosses/invalid-null-nested.json", DefinitionKind.Boss,
            ContentDiagnosticCodes.NullValue),

        // --- mining sites ---------------------------------------------------
        Bad("mining-sites/invalid-site-class-token.json", DefinitionKind.MiningSite,
            ContentDiagnosticCodes.TokenOutsideVocabulary),
        Bad("mining-sites/invalid-geode-missing-resonance-radius.json",
            DefinitionKind.MiningSite, ContentDiagnosticCodes.RequiredFieldMissing),

        // --- encounters -----------------------------------------------------
        Bad("encounters/invalid-minute-rows-gap.json", DefinitionKind.EncounterSchedule,
            ContentDiagnosticCodes.SequenceNotContiguous),
        Bad("encounters/invalid-minute-rows-count.json", DefinitionKind.EncounterSchedule,
            ContentDiagnosticCodes.ArrayCardinalityWrong),
        Bad("encounters/invalid-composition-share-sum.json", DefinitionKind.EncounterSchedule,
            ContentDiagnosticCodes.SumMismatch),
        Bad("encounters/invalid-formation-token.json", DefinitionKind.EncounterSchedule,
            ContentDiagnosticCodes.TokenOutsideVocabulary),
        Bad("encounters/invalid-enemy-id-grammar.json", DefinitionKind.EncounterSchedule,
            ContentDiagnosticCodes.ReferenceGrammarMismatch),

        // --- maps -----------------------------------------------------------
        Bad("maps/invalid-range-inverted.json", DefinitionKind.MapGenerationContract,
            ContentDiagnosticCodes.RangeInfeasible),
        Bad("maps/invalid-target-outside-range.json", DefinitionKind.MapGenerationContract,
            ContentDiagnosticCodes.RangeInfeasible),

        // --- player ---------------------------------------------------------
        Bad("player/invalid-unknown-field.json", DefinitionKind.PlayerBaseline,
            ContentDiagnosticCodes.UnknownField),

        // --- weapons --------------------------------------------------------
        Bad("weapons/invalid-recipe-display-name.json", DefinitionKind.Weapon,
            ContentDiagnosticCodes.ReferenceGrammarMismatch),
        Bad("weapons/invalid-recipe-count.json", DefinitionKind.Weapon,
            ContentDiagnosticCodes.ArrayCardinalityWrong),
        Bad("weapons/invalid-stat-track-count.json", DefinitionKind.Weapon,
            ContentDiagnosticCodes.ArrayCardinalityWrong),
        Bad("weapons/invalid-stat-track-duplicate-name.json", DefinitionKind.Weapon,
            ContentDiagnosticCodes.DuplicateValueInDefinition),
        Bad("weapons/invalid-stat-unit-token.json", DefinitionKind.Weapon,
            ContentDiagnosticCodes.TokenOutsideVocabulary),
        Bad("weapons/invalid-branch-id-foreign-weapon.json", DefinitionKind.Weapon,
            ContentDiagnosticCodes.CrossReferenceContradictsOwnId),
        Bad("weapons/invalid-branch-id-count.json", DefinitionKind.Weapon,
            ContentDiagnosticCodes.ArrayCardinalityWrong),
        Bad("weapons/invalid-price-formula-script-string.json",
            DefinitionKind.WeaponStatPriceFormula, ContentDiagnosticCodes.UnknownField),

        // --- branches -------------------------------------------------------
        Bad("branches/invalid-branch-class-token.json", DefinitionKind.Branch,
            ContentDiagnosticCodes.TokenOutsideVocabulary),
        Bad("branches/invalid-class-instead-of-branch-class.json", DefinitionKind.Branch,
            ContentDiagnosticCodes.UnknownField),
        Bad("branches/invalid-weapon-id-mismatch.json", DefinitionKind.Branch,
            ContentDiagnosticCodes.CrossReferenceContradictsOwnId),
        Bad("branches/invalid-cost-material-display-name.json", DefinitionKind.Branch,
            ContentDiagnosticCodes.ReferenceGrammarMismatch),
        Bad("branches/invalid-cost-units-nonpositive.json", DefinitionKind.Branch,
            ContentDiagnosticCodes.ValueOutOfRange),
        Bad("branches/invalid-fourth-stat-track.json", DefinitionKind.Branch,
            ContentDiagnosticCodes.UnknownField),
        Bad("branches/invalid-favorable-scene-effect.json", DefinitionKind.Branch,
            ContentDiagnosticCodes.UnknownField),
        Bad("branches/invalid-expected-effect-no-qualitative.json", DefinitionKind.Branch,
            ContentDiagnosticCodes.RequiredFieldMissing),

        // --- utilities ------------------------------------------------------
        Bad("utilities/invalid-rank-count-mismatch.json", DefinitionKind.Utility,
            ContentDiagnosticCodes.ArrayCardinalityWrong),
        Bad("utilities/invalid-rank-values-noncontiguous.json", DefinitionKind.Utility,
            ContentDiagnosticCodes.SequenceNotContiguous),
        Bad("utilities/invalid-radar-with-material.json", DefinitionKind.Utility,
            ContentDiagnosticCodes.ConditionalFieldForbidden),
        Bad("utilities/invalid-material-utility-without-material.json", DefinitionKind.Utility,
            ContentDiagnosticCodes.ConditionalFieldMissing),

        // --- relics ---------------------------------------------------------
        Bad("relics/invalid-unlock-id-on-fresh-relic.json", DefinitionKind.Relic,
            ContentDiagnosticCodes.ConditionalFieldForbidden),
        Bad("relics/invalid-unlocked-relic-without-unlock-id.json", DefinitionKind.Relic,
            ContentDiagnosticCodes.ConditionalFieldMissing),
        Bad("relics/invalid-relic-behavior-kind-prose.json", DefinitionKind.Relic,
            ContentDiagnosticCodes.BehaviorTokenMalformed),

        // --- powerups -------------------------------------------------------
        Bad("powerups/invalid-effect-kind-token.json", DefinitionKind.PowerUp,
            ContentDiagnosticCodes.TokenOutsideVocabulary),
        Bad("powerups/invalid-parallel-value-key.json", DefinitionKind.PowerUp,
            ContentDiagnosticCodes.UnknownField),
        Bad("powerups/invalid-rank-cap-mismatch.json", DefinitionKind.PowerUp,
            ContentDiagnosticCodes.ArrayCardinalityWrong),
        Bad("powerups/invalid-rank-price-nonpositive.json", DefinitionKind.PowerUp,
            ContentDiagnosticCodes.ValueOutOfRange),

        // --- unlocks --------------------------------------------------------
        Bad("unlocks/invalid-unlock-kind-camel-case.json", DefinitionKind.Unlock,
            ContentDiagnosticCodes.TokenOutsideVocabulary),
        Bad("unlocks/invalid-granted-id-display-name.json", DefinitionKind.Unlock,
            ContentDiagnosticCodes.ReferenceGrammarMismatch),
    };

    /// <summary>Reads a fixture's bytes.</summary>
    internal static byte[] Read(string relativePath)
    {
        return File.ReadAllBytes(Absolute(relativePath));
    }

    /// <summary>The absolute path of a fixture.</summary>
    internal static string Absolute(string relativePath)
    {
        return Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>The repository-relative path a diagnostic reports for a fixture.</summary>
    internal static string SourcePathOf(string relativePath)
    {
        return TestArtifacts.Relative(Absolute(relativePath));
    }

    /// <summary>Reads and validates one fixture.</summary>
    internal static DefinitionReadResult ReadDefinition(CategoryFixture fixture)
    {
        return CategorySchemas.Read(Read(fixture.Path), fixture.Context());
    }

    private static CategoryFixture Good(string path, DefinitionKind kind)
    {
        return new CategoryFixture(path, kind, expectedCode: null);
    }

    private static CategoryFixture Bad(string path, DefinitionKind kind, string expectedCode)
    {
        return new CategoryFixture(path, kind, expectedCode);
    }
}
