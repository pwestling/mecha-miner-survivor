using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;

namespace MechaMiner.Content.Relational;

/// <summary>
/// The cross-definition relations this package declares, and their evaluator.
/// </summary>
/// <remarks>
/// <para>
/// Two constraints, both relations rather than range checks, both evaluated after every
/// definition is loaded. Their operands span three and five definitions respectively,
/// and in each case two independent bounds would pass with the relation inverted.
/// </para>
/// </remarks>
public static class RelationalConstraints
{
    /// <summary>
    /// The geode resonance field must exceed the <em>maximum expanded</em> extraction
    /// zone, not the base one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The extraction zone is a modifiable statistic: it grows under every modifier
    /// naming <c>extraction-zone-radius</c>, and those modifiers add rather than
    /// multiply, because <c>docs/68-utility-catalog.md</c> § Modifier and timing rules
    /// makes percentage modifiers to the same named statistic additive. The constraint
    /// that matters is against the largest reachable zone.
    /// </para>
    /// <para>
    /// <b>Why the naive check is the wrong one.</b> Comparing the field against the base
    /// zone passes at any field radius above the base - including radii at which a
    /// player with the extraction-zone utility and PowerUp maxed would be mining a geode
    /// from outside its own resonance field. The accepted statement is about the played
    /// geometry, so the validator has to be too.
    /// </para>
    /// </remarks>
    public static RelationalConstraint ResonanceFieldExceedsExpandedZone { get; } = new(
        "RC-01",
        "SITE-04",
        "the geode resonance field radius strictly exceeds the maximum expanded extraction zone, "
            + "which is the base zone scaled by one plus the additive sum of every modifier "
            + "naming the extraction-zone-radius statistic at its highest rank",
        "GDD-MINING#geode-resonance-fields; GDD-UTILITY-CATALOG#modifier-and-timing-rules",
        new[] { "SITE-04" },
        ReadResonanceOperands,
        operands => operands[0].Value!.Value > ExpandedZone(operands));

    /// <summary>
    /// The deployment obstacle-free radius must be at least the largest mining-point
    /// cleared radius over every site class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Greater-than-or-equal and not strictly greater: the requirement is that
    /// deployment is <em>not tighter</em> than an ordinary mining point, and equality
    /// satisfies that.
    /// </para>
    /// <para>
    /// The comparison is against the maximum over all four site classes rather than
    /// against one named site, so a future class with a larger extraction zone is caught
    /// without anyone remembering to add it here.
    /// </para>
    /// </remarks>
    public static RelationalConstraint DeploymentClearanceCoversMiningPoint { get; } = new(
        "RC-02",
        "MGC-01",
        "the deployment obstacle-free radius is at least the largest mining-point cleared radius "
            + "over every site class, where a mining point's cleared radius is its extraction "
            + "zone radius plus one mech width",
        "GDD-MAP-GENERATION#deployment-and-opening-fairness; GDD-PLAYER-BASELINE",
        new[] { "MGC-01", "PLAYER-01" },
        ReadDeploymentOperands,
        operands => operands[0].Value!.Value >= operands[1].Value!.Value);

    private static readonly RelationalConstraint[] Declared =
    {
        ResonanceFieldExceedsExpandedZone,
        DeploymentClearanceCoversMiningPoint,
    };

    /// <summary>Every declared relation, in declared order.</summary>
    public static IReadOnlyList<RelationalConstraint> All { get; } =
        new ReadOnlyCollection<RelationalConstraint>(Declared);

    /// <summary>
    /// Evaluates every declared relation over a loaded catalog.
    /// </summary>
    /// <remarks>
    /// The whole catalog is supplied at once and no constraint triggers a load, so the
    /// stage's verdict does not depend on the order the source tree was enumerated in.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void Evaluate(
        RelationalCatalog catalog,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        foreach (RelationalConstraint constraint in Declared)
        {
            Evaluate(constraint, catalog, sourcePath, bag);
        }
    }

    /// <summary>Evaluates one relation over a loaded catalog.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void Evaluate(
        RelationalConstraint constraint,
        RelationalCatalog catalog,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(constraint);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        IReadOnlyList<RelationalOperand> operands = constraint.Operands(catalog);

        List<string> unresolved = new();
        foreach (RelationalOperand operand in operands)
        {
            if (!operand.IsResolved)
            {
                unresolved.Add(operand.DefinitionId + "#" + operand.Pointer);
            }
        }

        if (unresolved.Count > 0)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.RelationOperandMissing,
                sourcePath,
                JsonPointer.Root,
                constraint.Subject,
                constraint.Id + " needs every one of its operands before it can be evaluated, and "
                    + string.Join(", ", unresolved) + " could not be read. A relational check runs "
                    + "after the whole catalog is loaded; one that skipped a missing operand would "
                    + "be indistinguishable from one that passed",
                constraint.RequiredDefinitionIds));
            return;
        }

        if (constraint.Holds(operands))
        {
            return;
        }

        List<string> related = new(constraint.RequiredDefinitionIds);
        foreach (RelationalOperand operand in operands)
        {
            if (!related.Contains(operand.DefinitionId))
            {
                related.Add(operand.DefinitionId);
            }
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.RelationViolated,
            sourcePath,
            JsonPointer.Root,
            constraint.Subject,
            constraint.Id + ": " + constraint.Requirement + " (" + constraint.Authority
                + "). Operands: " + string.Join("; ", Describe(operands))
                + ". The fix could be to any of them, which is why every one is named",
            related));
    }

    private static IEnumerable<string> Describe(IReadOnlyList<RelationalOperand> operands)
    {
        foreach (RelationalOperand operand in operands)
        {
            yield return operand.ToString();
        }
    }

    private static IReadOnlyList<RelationalOperand> ReadResonanceOperands(
        RelationalCatalog catalog)
    {
        MiningSiteDefinition? geode = FindGeode(catalog);

        double? fieldRadius = geode?.ResonanceFieldRadiusMetres;
        double? zoneRadius = geode?.ExtractionZoneRadiusMetres;
        double expansionPercent = SumExtractionZoneModifierPercent(catalog);

        return new ReadOnlyCollection<RelationalOperand>(new List<RelationalOperand>
        {
            new("SITE-04", "/resonance_field/radius_m", fieldRadius),
            new("SITE-04", "/extraction_zone_radius_m", zoneRadius),
            new(
                "catalog",
                "/sum of every extraction-zone-radius modifier's highest percent",
                expansionPercent),
        });
    }

    private static double ExpandedZone(IReadOnlyList<RelationalOperand> operands)
    {
        double baseRadius = operands[1].Value!.Value;
        double expansionPercent = operands[2].Value!.Value;
        return baseRadius * (1.0 + (expansionPercent / 100.0));
    }

    /// <summary>
    /// The additive sum, in percentage points, of every modifier naming the
    /// extraction-zone-radius statistic at its highest rank.
    /// </summary>
    /// <remarks>
    /// Summed over the catalog rather than hardcoded, so that adding a third modifier
    /// shrinks the headroom instead of leaving the constraint checking a stale figure.
    /// A mech trait naming the statistic would be included here too; none does today.
    /// </remarks>
    private static double SumExtractionZoneModifierPercent(RelationalCatalog catalog)
    {
        const string statistic = "extraction-zone-radius";
        double total = 0;

        foreach (UtilityDefinition utility in catalog.OfKind<UtilityDefinition>())
        {
            if (!Names(utility.AffectedStatNames, statistic)
                || !string.Equals(utility.EffectKind, "additive-percent", StringComparison.Ordinal))
            {
                continue;
            }

            double highest = 0;
            foreach (UtilityDefinition.UtilityRank rank in utility.Ranks)
            {
                if (rank.Value > highest)
                {
                    highest = rank.Value;
                }
            }

            total += highest;
        }

        foreach (PowerUpDefinition powerUp in catalog.OfKind<PowerUpDefinition>())
        {
            if (!string.Equals(powerUp.AffectedStatistic, statistic, StringComparison.Ordinal)
                || !string.Equals(powerUp.EffectKind, "additive-percent", StringComparison.Ordinal))
            {
                continue;
            }

            total += powerUp.PerRankValue * powerUp.Cap;
        }

        return total;
    }

    private static bool Names(IReadOnlyList<string> statistics, string statistic)
    {
        foreach (string candidate in statistics)
        {
            if (string.Equals(candidate, statistic, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<RelationalOperand> ReadDeploymentOperands(
        RelationalCatalog catalog)
    {
        MapGenerationDefinition? map = catalog.Find<MapGenerationDefinition>("MGC-01");
        PlayerBaselineDefinition? player = catalog.Find<PlayerBaselineDefinition>("PLAYER-01");
        MiningSiteDefinition? geode = FindGeode(catalog);

        // The clearance is authored in mining-zone diameters, so it resolves against a
        // zone's own radius: one diameter is twice the radius.
        double? clearance = map is null || geode is null
            ? null
            : map.ObstacleFreeRadiusInMiningZoneDiameters * geode.ExtractionZoneRadiusMetres * 2.0;

        double? clearedRadius = null;
        if (player is not null)
        {
            foreach (MiningSiteDefinition site in catalog.OfKind<MiningSiteDefinition>())
            {
                double candidate =
                    site.ExtractionZoneRadiusMetres + player.CollisionDiameterMetres;
                if (clearedRadius is null || candidate > clearedRadius.Value)
                {
                    clearedRadius = candidate;
                }
            }
        }

        return new ReadOnlyCollection<RelationalOperand>(new List<RelationalOperand>
        {
            new(
                "MGC-01",
                "/deployment_and_opening_fairness/obstacle_free_radius_in_mining_zone_diameters"
                    + " resolved to mech collision diameters",
                clearance),
            new(
                "SITE-01..SITE-04",
                "/extraction_zone_radius_m + PLAYER-01#/collision_diameter_m, maximised",
                clearedRadius),
        });
    }

    private static MiningSiteDefinition? FindGeode(RelationalCatalog catalog)
    {
        foreach (MiningSiteDefinition site in catalog.OfKind<MiningSiteDefinition>())
        {
            if (string.Equals(
                    site.SiteClass, MiningSiteReader.GeodeClass, StringComparison.Ordinal))
            {
                return site;
            }
        }

        return null;
    }

    /// <summary>Renders a headroom figure for the balance report.</summary>
    /// <remarks>
    /// The analytical layer should report the margin rather than only the verdict, so
    /// that adding a third extraction-zone modifier shows up as headroom shrinking
    /// instead of as a sudden failure one change later.
    /// </remarks>
    public static string DescribeHeadroom(RelationalCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        IReadOnlyList<RelationalOperand> operands = ReadResonanceOperands(catalog);
        if (!operands[0].IsResolved || !operands[1].IsResolved)
        {
            return "RC-01 headroom is not computable: an operand was not loaded";
        }

        double expanded = ExpandedZone(operands);
        double headroom = operands[0].Value!.Value - expanded;
        return "RC-01 headroom "
            + headroom.ToString("R", CultureInfo.InvariantCulture)
            + " M above a maximum expanded zone of "
            + expanded.ToString("R", CultureInfo.InvariantCulture) + " M";
    }
}
