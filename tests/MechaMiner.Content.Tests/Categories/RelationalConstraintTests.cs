using System.Collections.Generic;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Relational;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// The two declared cross-definition relations: their operands, their stage, and the
/// proof that each rejects an inverted pair.
/// </summary>
/// <remarks>
/// <para>
/// Both constraints are relations rather than range checks, and the test that matters
/// is not that the accepted numbers pass - it is that plausible numbers with the
/// relation inverted <em>fail</em>. Each of these pairs satisfies any reasonable range
/// check on its own in either order, which is the signature of a constraint that has to
/// be written as a relation.
/// </para>
/// <para>
/// Evaluating them over a compiled content tree is <c>DAT-005</c>'s work. What lands
/// here is the constraint shape, its operands, its stage marker, and the negative
/// controls.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-002-023</c>, <c>VER-DAT-002-024</c>,
/// <c>VER-DAT-002-025</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RelationalConstraintTests
{
    private const string CatalogPath = "content/";

    private static readonly Tolerance Geometry = Tolerance.Named(
        "mech-collision-diameter",
        1e-9,
        "geometry here is a product of authored decimals with no accumulated error; the "
            + "tolerance exists because the values are doubles and not because the arithmetic "
            + "is approximate");

    [Test]
    public void TheAcceptedCatalogSatisfiesBothRelations()
    {
        DiagnosticBag bag = new();
        RelationalConstraints.Evaluate(AcceptedCatalog(), CatalogPath, bag);

        Assert.That(
            bag.Diagnostics,
            Is.Empty,
            () => "the accepted geometry must satisfy both relations: "
                + string.Join("; ", bag.Diagnostics));
    }

    /// <summary>
    /// The negative control that matters for RC-01: a resonance field larger than the
    /// <em>base</em> zone but smaller than the maximum expanded one. The naive check
    /// would pass this; the relation must not.
    /// </summary>
    [Test]
    public void AFieldThatOnlyExceedsTheBaseZoneFailsResonanceRelation()
    {
        RelationalCatalog catalog = Catalog(resonanceFieldRadius: 3.5);

        DiagnosticBag bag = new();
        RelationalConstraints.Evaluate(
            RelationalConstraints.ResonanceFieldExceedsExpandedZone, catalog, CatalogPath, bag);

        Assert.That(
            bag.Codes(),
            Does.Contain(ContentDiagnosticCodes.RelationViolated),
            () => "3.5 M exceeds the 3.0 M base zone and not the 4.2 M expanded one, so the "
                + "naive comparison would pass it and the relation must not: "
                + string.Join("; ", bag.Diagnostics));
    }

    /// <summary>
    /// The expanded zone is computed from the catalog's own modifiers, so adding one
    /// shrinks the headroom rather than leaving the constraint checking a stale figure.
    /// </summary>
    [Test]
    public void TheExpandedZoneIsSummedFromTheCatalogsOwnModifiers()
    {
        RelationalCatalog accepted = AcceptedCatalog();

        // Raising the PowerUp's per-rank contribution to 15 makes the catalog's
        // extraction-zone modifiers sum to 100 percentage points, which doubles the
        // zone to 6.0 M and leaves the 6.0 M field no longer strictly larger.
        RelationalCatalog widened = Catalog(powerUpPercentPerRank: 15);

        DiagnosticBag stillHolds = new();
        RelationalConstraints.Evaluate(
            RelationalConstraints.ResonanceFieldExceedsExpandedZone, accepted, CatalogPath,
            stillHolds);

        DiagnosticBag broken = new();
        RelationalConstraints.Evaluate(
            RelationalConstraints.ResonanceFieldExceedsExpandedZone, widened, CatalogPath,
            broken);

        Expect.Multiple(() =>
        {
            Assert.That(stillHolds.Diagnostics, Is.Empty, "40 percentage points leaves headroom");
            Assert.That(
                broken.Codes(),
                Does.Contain(ContentDiagnosticCodes.RelationViolated),
                () => "a third extraction-zone modifier must consume the headroom and fail, "
                    + "without anyone editing the constraint: "
                    + string.Join("; ", broken.Diagnostics));
            Assert.That(
                RelationalConstraints.DescribeHeadroom(accepted),
                Does.Contain("RC-01 headroom"),
                "the balance report needs the margin, not only the verdict");
        });
    }

    /// <summary>
    /// RC-02 accepts equality, because the requirement is that deployment is not
    /// tighter than an ordinary mining point.
    /// </summary>
    [Test]
    public void DeploymentClearanceAcceptsEqualityAndRejectsATighterRadius()
    {
        // A clearance of 0.6667 zone diameters resolves to exactly 4.0 M against a
        // 3.0 M zone, which equals the cleared radius.
        RelationalCatalog equal = Catalog(obstacleFreeRadiusInZoneDiameters: 4.0 / 6.0);
        RelationalCatalog tighter = Catalog(obstacleFreeRadiusInZoneDiameters: 0.5);

        DiagnosticBag atEquality = new();
        RelationalConstraints.Evaluate(
            RelationalConstraints.DeploymentClearanceCoversMiningPoint, equal, CatalogPath,
            atEquality);

        DiagnosticBag belowIt = new();
        RelationalConstraints.Evaluate(
            RelationalConstraints.DeploymentClearanceCoversMiningPoint, tighter, CatalogPath,
            belowIt);

        Expect.Multiple(() =>
        {
            Assert.That(
                atEquality.Diagnostics,
                Is.Empty,
                () => "equality satisfies 'not tighter': " + string.Join("; ",
                    atEquality.Diagnostics));
            Assert.That(
                belowIt.Codes(),
                Does.Contain(ContentDiagnosticCodes.RelationViolated),
                () => "3.0 M of clearance is tighter than the 4.0 M cleared around an ordinary "
                    + "mining point: " + string.Join("; ", belowIt.Diagnostics));
        });
    }

    /// <summary>
    /// The comparison is against the largest cleared radius over every site class, so a
    /// site with a bigger zone than the geode's is what the relation is measured
    /// against.
    /// </summary>
    [Test]
    public void DeploymentClearanceIsComparedAgainstTheLargestSiteZone()
    {
        RelationalCatalog catalog = Catalog(largestOtherSiteZoneRadius: 6.0);

        DiagnosticBag bag = new();
        RelationalConstraints.Evaluate(
            RelationalConstraints.DeploymentClearanceCoversMiningPoint, catalog, CatalogPath,
            bag);

        Assert.That(
            bag.Codes(),
            Does.Contain(ContentDiagnosticCodes.RelationViolated),
            () => "a site class with a 6.0 M zone clears 7.0 M, which the 6.0 M deployment "
                + "clearance does not cover; comparing against one named site would have "
                + "missed it: " + string.Join("; ", bag.Diagnostics));
    }

    /// <summary>
    /// A constraint asked to evaluate before its operands are loaded reports the gap. A
    /// skipped relational check and a passing one are indistinguishable from outside,
    /// which is why skipping is not an option.
    /// </summary>
    [Test]
    public void AnUnloadedOperandIsReportedRatherThanSkipped()
    {
        DiagnosticBag bag = new();
        RelationalConstraints.Evaluate(RelationalCatalog.Empty, CatalogPath, bag);

        IReadOnlyList<string> codes = bag.Codes();
        Expect.Multiple(() =>
        {
            Assert.That(
                codes,
                Does.Contain(ContentDiagnosticCodes.RelationOperandMissing),
                () => "an empty catalog must produce a missing-operand diagnostic, not silence: "
                    + string.Join("; ", bag.Diagnostics));
            Assert.That(
                codes,
                Does.Not.Contain(ContentDiagnosticCodes.RelationViolated),
                "an unevaluable relation is not a violated one");
        });
    }

    /// <summary>
    /// Every declared constraint names the definitions it needs, so the stage knows what
    /// to wait for rather than discovering it by failing.
    /// </summary>
    [Test]
    public void EveryConstraintDeclaresItsRequiredDefinitionsAndItsAuthority()
    {
        Expect.Multiple(() =>
        {
            foreach (RelationalConstraint constraint in RelationalConstraints.All)
            {
                Assert.That(
                    constraint.RequiredDefinitionIds,
                    Is.Not.Empty,
                    constraint.Id + " must name the definitions it needs loaded");
                Assert.That(
                    constraint.Requirement, Is.Not.Empty, constraint.Id + " must state its rule");
                Assert.That(
                    constraint.Authority,
                    Is.Not.Empty,
                    constraint.Id + " must cite the documents that grant the relation");
            }
        });
    }

    /// <summary>The accepted geometry, spelled out so the margins are visible.</summary>
    [Test]
    public void TheAcceptedOperandsAreTheOnesTheDocumentsState()
    {
        RelationalCatalog catalog = AcceptedCatalog();
        MiningSiteDefinition geode = catalog.Find<MiningSiteDefinition>("SITE-04")!;
        PlayerBaselineDefinition player = catalog.Find<PlayerBaselineDefinition>("PLAYER-01")!;

        Expect.Multiple(() =>
        {
            NumericAssert.AreEqualWithin(
                3.0, geode.ExtractionZoneRadiusMetres, Geometry, "the base extraction zone");
            NumericAssert.AreEqualWithin(
                6.0, geode.ResonanceFieldRadiusMetres!.Value, Geometry, "the resonance field");
            NumericAssert.AreEqualWithin(
                1.0, player.CollisionDiameterMetres, Geometry, "one mech width");
        });
    }

    private static RelationalCatalog AcceptedCatalog()
    {
        return Catalog();
    }

    private static RelationalCatalog Catalog(
        double resonanceFieldRadius = 6.0,
        double obstacleFreeRadiusInZoneDiameters = 1.0,
        double? largestOtherSiteZoneRadius = null,
        double? powerUpPercentPerRank = null)
    {
        List<ContentDefinition> definitions = new()
        {
            FixtureDocument
                .Load("mining-sites/valid-geode.json")
                .WithIn("resonance_field", "radius_m", JsonValue.Create(resonanceFieldRadius))
                .Read<MiningSiteDefinition>(DefinitionKind.MiningSite, "SITE-04"),

            FixtureDocument
                .Load("maps/valid-map-contract.json")
                .WithIn(
                    "deployment_and_opening_fairness",
                    "obstacle_free_radius_in_mining_zone_diameters",
                    JsonValue.Create(obstacleFreeRadiusInZoneDiameters))
                .Read<MapGenerationDefinition>(
                    DefinitionKind.MapGenerationContract, "MGC-01"),

            Load<PlayerBaselineDefinition>(
                "player/valid-player-baseline.json", DefinitionKind.PlayerBaseline),

            // The extraction-zone modifiers the relation sums over: the rank-3 utility
            // at +25 percentage points and the capped PowerUp at its per-rank value
            // times its cap.
            Load<UtilityDefinition>("utilities/valid-utility.json", DefinitionKind.Utility),

            FixtureDocument
                .Load("powerups/valid-powerup.json")
                .With("affected_statistic", "extraction-zone-radius")
                .With(
                    "per_rank_value",
                    JsonValue.Create(powerUpPercentPerRank ?? 3.0))
                .Read<PowerUpDefinition>(DefinitionKind.PowerUp, "PU-E02"),
        };

        if (largestOtherSiteZoneRadius is not null)
        {
            definitions.Add(FixtureDocument
                .Load("mining-sites/valid-standard-ore-seam.json")
                .With(
                    "extraction_zone_radius_m",
                    JsonValue.Create(largestOtherSiteZoneRadius.Value))
                .Read<MiningSiteDefinition>(DefinitionKind.MiningSite, "SITE-01"));
        }

        return new RelationalCatalog(definitions);
    }

    private static TDefinition Load<TDefinition>(string path, DefinitionKind kind)
        where TDefinition : ContentDefinition
    {
        CategoryFixture fixture = new(path, kind, expectedCode: null);
        DefinitionReadResult result = CategoryFixtureCorpus.ReadDefinition(fixture);

        Assert.That(
            result.IsValid,
            Is.True,
            () => path + " must load before a relation can read an operand out of it: "
                + string.Join("; ", result.Diagnostics));

        return (TDefinition)result.Definition!;
    }
}
