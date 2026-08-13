namespace MechaMiner.Content.Vocabulary;

/// <summary>
/// The closed token vocabularies more than one category draws on.
/// </summary>
/// <remarks>
/// <para>
/// A vocabulary lives here when two or more categories reference it, and in its own
/// category's field table otherwise. The distinction matters: a shared vocabulary is
/// the thing that makes two categories talk about the same quantity, and copying it
/// into both is how three spellings of "weapon damage" got into one tree.
/// </para>
/// <para>
/// None of these is a behavior registry entry. Doc 40 § Behavior registries lists
/// seven registered categories - <c>behavior_kind</c>, targeting policy, formula,
/// modifier hook, formation, effect, presentation recipe - and a named statistic is
/// none of them: it selects no implementation, it names a slot in the modifier graph.
/// A vocabulary that selected an implementation would belong in the registry; these
/// are validated structurally and referenced by registered descriptors as the type of
/// a parameter.
/// </para>
/// </remarks>
public static class ContentVocabularies
{
    /// <summary>
    /// The named statistics every mech trait, utility, PowerUp, and relic modifies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>docs/35-playable-mechs.md</c> § Signature and trait declares that a shared
    /// stat vocabulary exists - "a concise, positive, always-on modifier using the
    /// shared stat vocabulary" - and <c>docs/70-combat-and-economy-balance-framework.md</c>
    /// makes it load-bearing by requiring additive percentage modifiers "to the same
    /// named stat" to stack. Neither document enumerates it in full, so this list is
    /// assembled from every statistic the accepted catalogs actually modify.
    /// </para>
    /// <para>
    /// <c>forward-extraction-rate</c> is one token, not two. The gameplay documents use
    /// "mining extraction rate" and "forward extraction rate" for one quantity;
    /// <c>docs/40-mining-and-extraction.md</c> § Progress decay uses the second in the
    /// normative decay rule, so the second is the spelling kept. That the documents
    /// disagree is a document defect and is reported as one; the vocabulary does not
    /// carry both spellings, because two tokens for one quantity would make the
    /// additive-stacking rule silently not apply between them.
    /// </para>
    /// </remarks>
    public static ClosedVocabulary NamedStatistics { get; } = new(
        "a named statistic",
        "GDD-PLAYABLE-MECHS",
        "weapon-damage",
        "weapon-attack-rate",
        "weapon-area",
        "weapon-duration",
        "weapon-damage-to-elites-and-bosses",
        "maximum-hull-integrity",
        "armor",
        "recovery",
        "revival-charges",
        "movement-speed",
        "discovery-radius",
        "forward-extraction-rate",
        "extraction-zone-radius",
        "mined-common-ore",
        "recharge-time",
        "stored-charges",
        "control-resistance");

    /// <summary>The four accepted mining-site classes.</summary>
    /// <remarks>
    /// <c>docs/technical/40-content-data-and-validation.md</c> § Mining sites: "Standard
    /// mode validates exactly four accepted classes and their totals." The classes
    /// themselves are <c>docs/40-mining-and-extraction.md</c> § Resource payout
    /// profiles. They select no behavior implementation - they classify definitions
    /// whose behavior is their own fields - so this is a schema enum and not a registry
    /// entry.
    /// </remarks>
    public static ClosedVocabulary SiteClasses { get; } = new(
        "a mining-site class",
        "GDD-MINING",
        "standard-ore-seam",
        "rich-ore-seam",
        "specialized-material-geode",
        "hyper-gold-site");

    /// <summary>
    /// How a modifier's value combines with others naming the same statistic.
    /// </summary>
    /// <remarks>
    /// <c>docs/68-utility-catalog.md</c> § Modifier and timing rules: "Percentage
    /// modifiers to the same named statistic add together, and the displayed final
    /// result is authoritative." The classification is a token rather than the sentence
    /// because the sentence is catalog-wide and was being copied into eleven files,
    /// each copy carrying a repository path inside its value.
    /// </remarks>
    public static ClosedVocabulary StackingClassifications { get; } = new(
        "a stacking classification",
        "GDD-UTILITY-CATALOG",
        "additive-percent-same-statistic",
        "flat-before-percent",
        "additive-hull-per-second");

    /// <summary>The three branch transformation classes.</summary>
    /// <remarks>
    /// <c>docs/65-weapon-stat-and-branch-upgrades.md</c> § Weapon branches. Every
    /// weapon has exactly one branch of each, which is a relational rule over the
    /// branches catalog rather than a property of the token.
    /// </remarks>
    public static ClosedVocabulary BranchClasses { get; } = new(
        "a branch transformation class",
        "GDD-WEAPON-UPGRADES",
        "amplification",
        "functional",
        "conversion");
}
