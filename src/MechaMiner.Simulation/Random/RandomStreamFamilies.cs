using System;
using System.Collections.Generic;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// The closed registry of the 23 authoritative stream families of doc 20 § Authoritative
/// random-number contract.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// key-registration rule: "New authoritative randomness receives a unique registered family key
/// in this table; keys are never repurposed. A category retry or an added visual draw cannot
/// consume another family's sequence."
/// </para>
/// <para>
/// The registry is <em>closed</em> — an unregistered key is rejected rather than derived, which
/// <c>VER-SIM-005-010</c> asserts. An open <c>ushort</c> parameter would let a caller invent a
/// family key and silently consume a sequence no golden pins.
/// </para>
/// <para>
/// <see cref="All"/> is ordered by ascending family key, which is the canonical order
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-stream-independence.txt</c> pins, so a
/// collision, a missing family, or an off-by-one in this table is visible as a duplicated or
/// shifted row.
/// </para>
/// </remarks>
public static class RandomStreamFamilies
{
    /// <summary>Resource-profile selection, <c>0x0100</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily ResourceProfileSelection =
        new(0x0100, "resource-profile selection", InstanceKeyRule.Zero, true);

    /// <summary>Major topology, <c>0x0200</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily MajorTopology =
        new(0x0200, "major topology", InstanceKeyRule.Zero, true);

    /// <summary>Spatial embedding, <c>0x0201</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily SpatialEmbedding =
        new(0x0201, "spatial embedding", InstanceKeyRule.Zero, true);

    /// <summary>Region recipes, <c>0x0202</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily RegionRecipes =
        new(0x0202, "region recipes", InstanceKeyRule.RegionId, true);

    /// <summary>Landmarks, <c>0x0203</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily Landmarks =
        new(0x0203, "landmarks", InstanceKeyRule.RegionId, true);

    /// <summary>Obstacle and dressing placement, <c>0x0204</c> (doc 20 § Authoritative
    /// random-number contract).</summary>
    public static readonly RandomStreamFamily ObstacleAndDressingPlacement =
        new(0x0204, "obstacle/dressing placement", InstanceKeyRule.RegionId, true);

    /// <summary>Deployment selection, <c>0x0205</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily DeploymentSelection =
        new(0x0205, "deployment selection", InstanceKeyRule.Zero, true);

    /// <summary>Standard-seam placement, <c>0x0210</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily StandardSeamPlacement =
        new(0x0210, "standard-seam placement", InstanceKeyRule.Zero, true);

    /// <summary>Rich-seam placement, <c>0x0211</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily RichSeamPlacement =
        new(0x0211, "rich-seam placement", InstanceKeyRule.Zero, true);

    /// <summary>Material-geode placement, <c>0x0220</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily MaterialGeodePlacement =
        new(0x0220, "material-geode placement", InstanceKeyRule.MaterialOrdinal, true);

    /// <summary>Hyper Gold placement, <c>0x0230</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily HyperGoldPlacement =
        new(0x0230, "Hyper Gold placement", InstanceKeyRule.Zero, true);

    /// <summary>Relic-cache placement, <c>0x0240</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily RelicCachePlacement =
        new(0x0240, "relic-cache placement", InstanceKeyRule.Zero, true);

    /// <summary>Relic assignment, <c>0x0241</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily RelicAssignment =
        new(0x0241, "relic assignment", InstanceKeyRule.Zero, true);

    /// <summary>Dynamic rocks and drop rolls, <c>0x0250</c> (doc 20 § Authoritative
    /// random-number contract).</summary>
    public static readonly RandomStreamFamily DynamicRocksAndDropRolls =
        new(0x0250, "dynamic rocks/drop rolls", InstanceKeyRule.RockId, true);

    /// <summary>Release fallback-manifest selection, <c>0x0260</c> (doc 20 § Authoritative
    /// random-number contract).</summary>
    public static readonly RandomStreamFamily ReleaseFallbackManifestSelection =
        new(0x0260, "release fallback-manifest selection", InstanceKeyRule.ProfileAndRegionCountOrdinal, true);

    /// <summary>Baseline encounter sectors and composition, <c>0x0300</c> (doc 20 §
    /// Authoritative random-number contract).</summary>
    public static readonly RandomStreamFamily BaselineEncounterSectorsAndComposition =
        new(0x0300, "baseline encounter sectors/composition", InstanceKeyRule.Zero, true);

    /// <summary>Authored event formations, <c>0x0301</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily AuthoredEventFormations =
        new(0x0301, "authored event formations", InstanceKeyRule.ScheduleRowIndex, true);

    /// <summary>Beacon response selection, <c>0x0302</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily BeaconResponseSelection =
        new(0x0302, "beacon response selection", InstanceKeyRule.SiteId, true);

    /// <summary>Boss entry and ability randomness, <c>0x0303</c> (doc 20 § Authoritative
    /// random-number contract).</summary>
    public static readonly RandomStreamFamily BossEntryAndAbilityRandomness =
        new(0x0303, "boss entry/ability randomness", InstanceKeyRule.BossIndex, true);

    /// <summary>Player weapon combat randomness, <c>0x0400</c> (doc 20 § Authoritative
    /// random-number contract).</summary>
    public static readonly RandomStreamFamily PlayerWeaponCombatRandomness =
        new(0x0400, "player weapon combat randomness", InstanceKeyRule.WeaponSlotOrdinal, true);

    /// <summary>Enemy combat randomness, <c>0x0410</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomStreamFamily EnemyCombatRandomness =
        new(0x0410, "enemy combat randomness", InstanceKeyRule.SourceAndGeneration, true);

    /// <summary>Boss and other authorized loot, <c>0x0500</c> (doc 20 § Authoritative
    /// random-number contract).</summary>
    public static readonly RandomStreamFamily BossAndOtherAuthorizedLoot =
        new(0x0500, "boss/other authorized loot", InstanceKeyRule.RewardSourceId, true);

    /// <summary>
    /// Presentation-only variation, <c>0xF000</c> (doc 20 § Authoritative random-number
    /// contract). The one non-authoritative family: "never serialized into authoritative
    /// state".
    /// </summary>
    public static readonly RandomStreamFamily PresentationOnlyVariation =
        new(0xF000, "presentation-only variation", InstanceKeyRule.PresentationBinding, false);

    private static readonly RandomStreamFamily[] Registry =
    {
        ResourceProfileSelection,
        MajorTopology,
        SpatialEmbedding,
        RegionRecipes,
        Landmarks,
        ObstacleAndDressingPlacement,
        DeploymentSelection,
        StandardSeamPlacement,
        RichSeamPlacement,
        MaterialGeodePlacement,
        HyperGoldPlacement,
        RelicCachePlacement,
        RelicAssignment,
        DynamicRocksAndDropRolls,
        ReleaseFallbackManifestSelection,
        BaselineEncounterSectorsAndComposition,
        AuthoredEventFormations,
        BeaconResponseSelection,
        BossEntryAndAbilityRandomness,
        PlayerWeaponCombatRandomness,
        EnemyCombatRandomness,
        BossAndOtherAuthorizedLoot,
        PresentationOnlyVariation,
    };

    /// <summary>
    /// Every registered family, in ascending family-key order (the canonical order the
    /// independence golden pins).
    /// </summary>
    public static IReadOnlyList<RandomStreamFamily> All => Registry;

    /// <summary>Looks up a registered family by its key.</summary>
    /// <param name="familyKey">The candidate family key.</param>
    /// <param name="family">The registered family, or the default when unregistered.</param>
    /// <returns><see langword="true"/> when <paramref name="familyKey"/> is
    /// registered.</returns>
    public static bool TryGet(ushort familyKey, out RandomStreamFamily family)
    {
        for (int index = 0; index < Registry.Length; index++)
        {
            if (Registry[index].Key == familyKey)
            {
                family = Registry[index];
                return true;
            }
        }

        family = default;
        return false;
    }

    /// <summary>Looks up a registered family by its key, refusing an unregistered
    /// one.</summary>
    /// <param name="familyKey">The candidate family key.</param>
    /// <returns>The registered family.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="familyKey"/> is not one of the 23 keys the family table of doc 20 §
    /// Authoritative random-number contract registers.
    /// </exception>
    public static RandomStreamFamily Get(ushort familyKey)
    {
        if (TryGet(familyKey, out RandomStreamFamily family))
        {
            return family;
        }

        throw new ArgumentOutOfRangeException(
            nameof(familyKey),
            familyKey,
            "0x" + familyKey.ToString("X4", CultureInfo.InvariantCulture)
                + " is not a registered stream family. doc 20 § Authoritative random-number contract: new authoritative randomness receives a "
                + "unique registered family key in doc 20's table; it is never invented at a call site");
    }
}
