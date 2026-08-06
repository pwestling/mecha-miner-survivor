namespace MechaMiner.Content.Categories;

/// <summary>
/// The definition kinds that have their own field table and schema document.
/// </summary>
/// <remarks>
/// <para>
/// This is finer than <see cref="Ids.ContentCategory"/> and deliberately so. A
/// category is an authoring directory; a kind is a shape. Three directories hold two
/// shapes each, because doc 40 § Accepted content repository layout groups definitions
/// "by stable item or the smallest cohesive aggregate" and an aggregate is not one of
/// the items it aggregates:
/// </para>
/// <list type="bullet">
/// <item><description><c>content/enemies/</c> holds ten enemies and <c>ELT-01</c>, a shared elite modifier profile that is not an enemy.</description></item>
/// <item><description><c>content/weapons/</c> holds fifteen weapons and <c>FORMULA-01</c>, the stat price curve, which is a rule about weapons rather than a weapon.</description></item>
/// </list>
/// <para>
/// Merging either pair into one schema would mean a field table in which most fields
/// are optional and the real requirement is "these eleven together or those nine
/// together", which is a union wearing a single shape's clothes.
/// </para>
/// </remarks>
public enum DefinitionKind
{
    /// <summary>Reserved so a default-initialised value is never a real kind.</summary>
    Unspecified = 0,

    /// <summary><c>RSC-01</c>..<c>RSC-08</c>: the six specialized materials plus common ore and Hyper Gold.</summary>
    Resource,

    /// <summary><c>MCH-01</c>..<c>MCH-06</c>.</summary>
    Mech,

    /// <summary><c>EN-01</c>..<c>EN-10</c>.</summary>
    Enemy,

    /// <summary><c>ELT-01</c>: the shared elite modifier profile.</summary>
    EliteModifiers,

    /// <summary><c>BOSS-01</c>..<c>BOSS-04</c>.</summary>
    Boss,

    /// <summary><c>SITE-01</c>..<c>SITE-04</c>.</summary>
    MiningSite,

    /// <summary><c>WAV-01</c>: the standard encounter schedule.</summary>
    EncounterSchedule,

    /// <summary><c>MGC-01</c>: the standard map generation contract.</summary>
    MapGenerationContract,

    /// <summary><c>PLAYER-01</c>: the player baseline.</summary>
    PlayerBaseline,

    /// <summary><c>W-AB</c>..<c>W-EF</c>: the fifteen unordered material-pair weapons.</summary>
    Weapon,

    /// <summary><c>FORMULA-01</c>: the weapon common-ore stat upgrade price curve.</summary>
    WeaponStatPriceFormula,

    /// <summary><c>W-xy-&lt;name&gt;</c>: the forty-five weapon branches.</summary>
    Branch,

    /// <summary><c>UTL-A1</c>..<c>UTL-F2</c> plus <c>UTL-R1</c>.</summary>
    Utility,

    /// <summary><c>REL-01</c>..<c>REL-10</c>.</summary>
    Relic,

    /// <summary><c>PU-&lt;group&gt;&lt;nn&gt;</c>: the thirteen permanent PowerUps.</summary>
    PowerUp,

    /// <summary><c>UNL-01</c>..<c>UNL-06</c>: the six permanent option unlocks.</summary>
    Unlock,
}
