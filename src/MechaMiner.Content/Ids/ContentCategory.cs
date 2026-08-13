namespace MechaMiner.Content.Ids;

/// <summary>
/// The content categories, one per authoring directory under <c>content/</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Accepted content
/// repository layout fixes the directory set, and § Stable ID policy requires an ID to
/// match "a schema pattern" - which pattern depends on which category the definition
/// belongs to, so the category is a first-class value rather than a directory string.
/// </para>
/// <para>
/// <c>content/schemas/</c> and <c>content/localization/</c> are directories but not
/// categories: neither holds definitions carrying the <c>SCH-CNT-001</c> envelope. A
/// schema is infrastructure and a locale catalog is "a flat object of key to string"
/// (§ Source catalog format and key pattern). They are named here as the two
/// deliberate exclusions so that a later reader does not add them by symmetry.
/// </para>
/// <para>
/// <c>content/presentation/</c> appears in doc 40's layout but is deliberately absent
/// from this enum. It has no directory yet and, more importantly, no minted ID
/// grammar; adding a member would require inventing a prefix, and prefix minting is
/// an integration-owner decision, not an implementation one. The package that lands
/// <c>SCH-CNT-003</c> adds it with the grammar it is granted.
/// </para>
/// </remarks>
public enum ContentCategory
{
    /// <summary>Reserved so a default-initialised value is never a real category.</summary>
    Unspecified = 0,

    /// <summary><c>content/resources/</c>. The six specialized materials plus common ore and Hyper Gold.</summary>
    Resource,

    /// <summary><c>content/mechs/</c>.</summary>
    Mech,

    /// <summary><c>content/enemies/</c>.</summary>
    Enemy,

    /// <summary><c>content/bosses/</c>.</summary>
    Boss,

    /// <summary><c>content/weapons/</c>.</summary>
    Weapon,

    /// <summary><c>content/branches/</c>.</summary>
    Branch,

    /// <summary><c>content/utilities/</c>.</summary>
    Utility,

    /// <summary><c>content/relics/</c>.</summary>
    Relic,

    /// <summary><c>content/powerups/</c>.</summary>
    PowerUp,

    /// <summary><c>content/unlocks/</c>.</summary>
    Unlock,

    /// <summary><c>content/mining-sites/</c>.</summary>
    MiningSite,

    /// <summary><c>content/encounters/</c>.</summary>
    Encounter,

    /// <summary><c>content/maps/</c>.</summary>
    Map,

    /// <summary>
    /// <c>content/player/</c>. The player baseline. Declared here so the
    /// <c>PLAYER-</c> prefix is reserved before another category claims it; the
    /// directory does not exist yet.
    /// </summary>
    Player,
}
