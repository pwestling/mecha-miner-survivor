using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Relational;

/// <summary>
/// The catalog-wide checks: cardinality, uniqueness, closed-set coverage, recomputed
/// totals, and the recipe-letter agreement.
/// </summary>
/// <remarks>
/// <para>
/// Every check here reads more than one definition, which is what puts it in doc 40
/// § Relational rather than § Semantic. Each states the route it takes to its subject,
/// because "the catalog holds fifteen weapons" and "the catalog holds fifteen distinct
/// material pairs" are different claims and only the second catches two weapons sharing
/// a recipe.
/// </para>
/// </remarks>
public static class CatalogChecks
{
    /// <summary>
    /// The six canonical letters exactly cover the specialized-material resources.
    /// </summary>
    /// <remarks>
    /// Matched on the parsed <c>canonical_letter</c> value rather than on the file name
    /// or the stable ID, so renaming a file or reassigning an ID cannot change the
    /// verdict. A duplicated letter and a missing one are both reported, and named.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void SixMaterialSetIsExactlyCovered(
        IReadOnlyList<ResourceDefinition> resources,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        Dictionary<string, List<string>> byLetter = new(StringComparer.Ordinal);
        foreach (ResourceDefinition resource in resources)
        {
            if (resource.CanonicalLetter is null)
            {
                continue;
            }

            if (!byLetter.TryGetValue(resource.CanonicalLetter, out List<string>? owners))
            {
                owners = new List<string>();
                byLetter[resource.CanonicalLetter] = owners;
            }

            owners.Add(resource.Id);
        }

        foreach (string letter in new[] { "A", "B", "C", "D", "E", "F" })
        {
            if (!byLetter.TryGetValue(letter, out List<string>? owners))
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.CatalogSetNotCoveredExactly,
                    sourcePath,
                    JsonPointer.Root,
                    null,
                    "the six-material set is covered exactly once each by canonical_letter A "
                        + "through F, and no resource carries '" + letter + "'. The set is "
                        + "matched on the parsed field value rather than on the file name, so a "
                        + "renamed file is not the cause"));
                continue;
            }

            if (owners.Count > 1)
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.CatalogDuplicateIdentity,
                    sourcePath,
                    JsonPointer.Root,
                    owners[1],
                    "canonical_letter '" + letter + "' is claimed by more than one resource ("
                        + string.Join(", ", owners) + "). The letter is what a weapon recipe "
                        + "resolves through, so two claimants make that resolution ambiguous",
                    owners));
            }
        }
    }

    /// <summary>
    /// The four accepted mining-site classes are each defined exactly once.
    /// </summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void FourSiteClassesAreExactlyCovered(
        IReadOnlyList<MiningSiteDefinition> sites,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(sites);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        Dictionary<string, List<string>> byClass = new(StringComparer.Ordinal);
        foreach (MiningSiteDefinition site in sites)
        {
            if (!byClass.TryGetValue(site.SiteClass, out List<string>? owners))
            {
                owners = new List<string>();
                byClass[site.SiteClass] = owners;
            }

            owners.Add(site.Id);
        }

        foreach (string token in ContentVocabularies.SiteClasses.Tokens)
        {
            if (!byClass.TryGetValue(token, out List<string>? owners))
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.CatalogSetNotCoveredExactly,
                    sourcePath,
                    JsonPointer.Root,
                    null,
                    "standard mode accepts exactly four mining-site classes and no definition "
                        + "carries site_class '" + token + "'. The check is over the parsed "
                        + "tokens, so a fifth class, a duplicate, and a missing one are three "
                        + "distinct faults"));
                continue;
            }

            if (owners.Count > 1)
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.CatalogDuplicateIdentity,
                    sourcePath,
                    JsonPointer.Root,
                    owners[1],
                    "site_class '" + token + "' is claimed by more than one definition ("
                        + string.Join(", ", owners) + ")",
                    owners));
            }
        }
    }

    /// <summary>
    /// The weapons catalog holds exactly fifteen distinct unordered material pairs.
    /// </summary>
    /// <remarks>
    /// The route sorts each weapon's two resource IDs and matches on the sorted pair, so
    /// two weapons whose recipes differ only in order collide as they should. The count
    /// is of distinct pairs and not of files: counting files alone would let two weapons
    /// share one recipe and still make fifteen.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void FifteenDistinctRecipePairs(
        IReadOnlyList<WeaponDefinition> weapons,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(weapons);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        Dictionary<string, string> byPair = new(StringComparer.Ordinal);
        foreach (WeaponDefinition weapon in weapons)
        {
            List<string> pair = new(weapon.RecipePairMaterialIds);
            pair.Sort(StringComparer.Ordinal);
            string key = string.Join("+", pair);

            if (byPair.TryGetValue(key, out string? owner))
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.CatalogDuplicateIdentity,
                    sourcePath,
                    JsonPointer.Root.AppendProperty("recipe_pair_material_ids"),
                    weapon.Id,
                    "a material pair is unordered and belongs to exactly one weapon; the pair "
                        + key + " is already claimed by " + owner
                        + ". The match is on the sorted pair, so reversing the two resources does "
                        + "not make a second recipe",
                    new[] { owner }));
                continue;
            }

            byPair[key] = weapon.Id;
        }

        if (byPair.Count == WeaponSchema.AcceptedRecipeCount)
        {
            return;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.CatalogCardinalityWrong,
            sourcePath,
            JsonPointer.Root,
            null,
            "the weapons catalog holds exactly "
                + WeaponSchema.AcceptedRecipeCount.ToString(CultureInfo.InvariantCulture)
                + " distinct unordered material-pair recipes; "
                + byPair.Count.ToString(CultureInfo.InvariantCulture)
                + " distinct pairs were found across "
                + weapons.Count.ToString(CultureInfo.InvariantCulture)
                + " definitions. The count is of distinct pairs rather than of files, so two "
                + "weapons sharing a recipe do not make up the number"));
    }

    /// <summary>
    /// Each weapon's recipe resources, resolved to canonical letters in authored order,
    /// concatenate to the weapon ID's own two-letter suffix.
    /// </summary>
    /// <remarks>
    /// This is the compensation for a recipe that stopped being human-legible. Once a
    /// recipe holds resource IDs, a mis-assigned pair is invisible to a reader;
    /// resolving each ID to its letter and comparing the concatenation restores the
    /// check a human used to perform by eye.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void RecipeLettersSpellTheWeaponId(
        IReadOnlyList<WeaponDefinition> weapons,
        IReadOnlyList<ResourceDefinition> resources,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(weapons);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        Dictionary<string, string> letters = new(StringComparer.Ordinal);
        foreach (ResourceDefinition resource in resources)
        {
            if (resource.CanonicalLetter is not null)
            {
                letters[resource.Id] = resource.CanonicalLetter;
            }
        }

        foreach (WeaponDefinition weapon in weapons)
        {
            JsonPointer pointer = JsonPointer.Root.AppendProperty("recipe_pair_material_ids");
            List<string> resolved = new(weapon.RecipePairMaterialIds.Count);
            List<string> unresolved = new();

            foreach (string resourceId in weapon.RecipePairMaterialIds)
            {
                if (letters.TryGetValue(resourceId, out string? letter))
                {
                    resolved.Add(letter);
                    continue;
                }

                unresolved.Add(resourceId);
            }

            if (unresolved.Count > 0)
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.RecipeLettersMismatch,
                    sourcePath,
                    pointer,
                    weapon.Id,
                    "every recipe resource resolves to a canonical letter, and "
                        + string.Join(", ", unresolved) + " does not. A recipe naming a resource "
                        + "with no letter cannot be checked against the weapon ID at all, which "
                        + "is the whole of what this check exists for",
                    unresolved));
                continue;
            }

            string spelled = string.Concat(resolved);
            string suffix = weapon.Id.Length > 2 ? weapon.Id[2..] : weapon.Id;
            if (string.Equals(spelled, suffix, StringComparison.Ordinal))
            {
                continue;
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.RecipeLettersMismatch,
                sourcePath,
                pointer,
                weapon.Id,
                "the recipe resources spell '" + spelled + "' and the weapon ID's suffix is '"
                    + suffix + "'. A weapon ID is its material pair, so the two must agree; "
                    + "resolving the IDs to their letters is what keeps that checkable now that "
                    + "the recipe holds IDs rather than letters",
                new List<string>(weapon.RecipePairMaterialIds)));
        }
    }

    /// <summary>
    /// Each weapon's branches are exactly one amplification, one functional, and one
    /// conversion.
    /// </summary>
    /// <remarks>
    /// Grouped by the branch's own <c>weapon_id</c> rather than by the weapon's
    /// <c>branch_ids</c>, so a branch that names a weapon the weapon does not name back
    /// still lands in a group and is counted.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void EachWeaponHasOneBranchOfEachClass(
        IReadOnlyList<BranchDefinition> branches,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(branches);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        Dictionary<string, Dictionary<string, List<string>>> byWeapon =
            new(StringComparer.Ordinal);

        foreach (BranchDefinition branch in branches)
        {
            if (!byWeapon.TryGetValue(branch.WeaponId, out Dictionary<string, List<string>>? classes))
            {
                classes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                byWeapon[branch.WeaponId] = classes;
            }

            if (!classes.TryGetValue(branch.BranchClass, out List<string>? owners))
            {
                owners = new List<string>();
                classes[branch.BranchClass] = owners;
            }

            owners.Add(branch.Id);
        }

        foreach (KeyValuePair<string, Dictionary<string, List<string>>> weapon in byWeapon)
        {
            foreach (string token in ContentVocabularies.BranchClasses.Tokens)
            {
                weapon.Value.TryGetValue(token, out List<string>? owners);
                int count = owners?.Count ?? 0;
                if (count == 1)
                {
                    continue;
                }

                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.BranchClassDistributionWrong,
                    sourcePath,
                    JsonPointer.Root.AppendProperty("branch_class"),
                    weapon.Key,
                    "each weapon has exactly one branch of each transformation class; "
                        + weapon.Key + " has "
                        + count.ToString(CultureInfo.InvariantCulture) + " of class '" + token
                        + "'. Grouped by the branch's own weapon_id, so a weapon with three "
                        + "branches of two classes fails even though its branch count is right",
                    owners ?? new List<string>()));
            }
        }
    }

    /// <summary>
    /// The utility catalog's fresh-versus-unlocked distribution is the accepted one.
    /// </summary>
    /// <remarks>
    /// Counted over parsed <c>pool_availability</c> tokens, so moving one utility from
    /// one pool to the other fails even though every file stays individually valid.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void UtilityPoolDistributionIsAccepted(
        IReadOnlyList<UtilityDefinition> utilities,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(utilities);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        foreach (UtilityDefinition utility in utilities)
        {
            counts.TryGetValue(utility.PoolAvailability, out int seen);
            counts[utility.PoolAvailability] = seen + 1;
        }

        Expect(counts, "fresh-profile", UtilitySchema.FreshProfileCount, sourcePath, bag);
        Expect(counts, "hyper-gold-unlock", UtilitySchema.HyperGoldUnlockCount, sourcePath, bag);
        Expect(counts, "always-available", UtilitySchema.AlwaysAvailableCount, sourcePath, bag);
    }

    /// <summary>No two utilities share an installed identity.</summary>
    /// <remarks>
    /// The identity is the pair of assigned material and primary role, matched on parsed
    /// field values and not on the stable ID - precisely so that minting a new ID for a
    /// duplicate does not evade the check.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void NoDuplicateInstalledUtilityIdentity(
        IReadOnlyList<UtilityDefinition> utilities,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(utilities);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        Dictionary<string, string> byIdentity = new(StringComparer.Ordinal);
        foreach (UtilityDefinition utility in utilities)
        {
            string identity = (utility.MaterialId ?? "<ore-only>") + "/"
                + (utility.PrimaryRole ?? "<unstated>");

            if (byIdentity.TryGetValue(identity, out string? owner))
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.CatalogDuplicateIdentity,
                    sourcePath,
                    JsonPointer.Root.AppendProperty("material_id"),
                    utility.Id,
                    "no two utilities share an installed identity, which is the pair of assigned "
                        + "material and primary role; " + owner + " already claims '" + identity
                        + "'. The match is on parsed values rather than on the stable ID, so "
                        + "minting a new ID for a duplicate does not evade it",
                    new[] { owner }));
                continue;
            }

            byIdentity[identity] = utility.Id;
        }
    }

    /// <summary>The PowerUp catalog's rank prices sum to the accepted total.</summary>
    /// <remarks>
    /// Summed over every rank price in every definition. There is no authored total left
    /// to compare against, which is the point: a total compared against a copy of itself
    /// proves nothing.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void PowerUpCatalogTotalIsAccepted(
        IReadOnlyList<PowerUpDefinition> powerUps,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(powerUps);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        long total = 0;
        foreach (PowerUpDefinition powerUp in powerUps)
        {
            total += powerUp.TotalCostHyperGold;
        }

        if (total == PowerUpSchema.CatalogTotalHyperGold)
        {
            return;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.CatalogTotalMismatch,
            sourcePath,
            JsonPointer.Root,
            null,
            "the PowerUp catalog's rank prices sum to "
                + PowerUpSchema.CatalogTotalHyperGold.ToString(CultureInfo.InvariantCulture)
                + " Hyper Gold; recomputing them from every ranks[].price_hyper_gold in every "
                + "definition gives " + total.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>The option-unlock catalog's costs sum to the accepted total.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void UnlockCatalogTotalIsAccepted(
        IReadOnlyList<UnlockDefinition> unlocks,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(unlocks);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        long total = 0;
        foreach (UnlockDefinition unlock in unlocks)
        {
            total += unlock.CostHyperGold;
        }

        if (total == UnlockSchema.CatalogTotalHyperGold)
        {
            return;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.CatalogTotalMismatch,
            sourcePath,
            JsonPointer.Root,
            null,
            "the option-unlock catalog's costs sum to "
                + UnlockSchema.CatalogTotalHyperGold.ToString(CultureInfo.InvariantCulture)
                + " Hyper Gold; recomputing them from every cost_hyper_gold gives "
                + total.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Mech selection orders are unique across the catalog.</summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public static void MechSelectionOrdersAreUnique(
        IReadOnlyList<MechDefinition> mechs,
        string sourcePath,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(mechs);
        ArgumentNullException.ThrowIfNull(sourcePath);
        ArgumentNullException.ThrowIfNull(bag);

        Dictionary<long, string> byOrder = new();
        foreach (MechDefinition mech in mechs)
        {
            if (byOrder.TryGetValue(mech.SelectionOrder, out string? owner))
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.CatalogDuplicateIdentity,
                    sourcePath,
                    JsonPointer.Root.AppendProperty("selection_order"),
                    mech.Id,
                    "selection_order is a position in one ordered list, so no two mechs share "
                        + "one; " + owner + " already claims position "
                        + mech.SelectionOrder.ToString(CultureInfo.InvariantCulture)
                        + ". Two mechs at one position leave the selection screen's order "
                        + "undefined, which would make it depend on enumeration order",
                    new[] { owner }));
                continue;
            }

            byOrder[mech.SelectionOrder] = mech.Id;
        }
    }

    private static void Expect(
        Dictionary<string, int> counts,
        string token,
        int expected,
        string sourcePath,
        DiagnosticBag bag)
    {
        counts.TryGetValue(token, out int actual);
        if (actual == expected)
        {
            return;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.CatalogCardinalityWrong,
            sourcePath,
            JsonPointer.Root.AppendProperty("availability").AppendProperty("pool_availability"),
            null,
            "the accepted utility distribution has "
                + expected.ToString(CultureInfo.InvariantCulture) + " definitions with "
                + "pool_availability '" + token + "'; "
                + actual.ToString(CultureInfo.InvariantCulture) + " were found. Counted over the "
                + "parsed tokens, so moving one utility from one pool to the other fails even "
                + "though every file stays individually valid"));
    }
}
