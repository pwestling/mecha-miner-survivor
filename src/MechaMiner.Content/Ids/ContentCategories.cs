using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Ids;

/// <summary>
/// The registry of content categories, their directories, and their ID grammars.
/// </summary>
/// <remarks>
/// <para>
/// Every grammar below is derived from an ID that an accepted document already mints.
/// <c>docs/technical/40-content-data-and-validation.md</c> § Stable ID policy requires
/// reusing accepted gameplay IDs "exactly", so nothing here invents a prefix: the
/// gameplay catalogs mint <c>MCH-</c>, <c>EN-</c>, <c>BOSS-</c>, <c>W-</c>,
/// <c>REL-</c>, <c>PU-</c>, <c>UNL-</c>, and <c>UTL-</c>; doc 40 § Utilities mints
/// <c>UTL-R1</c>; doc 40 § Encounter schedule and § Map generation mint <c>WAV-01</c>
/// and <c>MGC-01</c>; and the integration owner minted <c>RSC-</c>, <c>SITE-</c>,
/// <c>ELT-</c>, and <c>PLAYER-</c>.
/// </para>
/// <para>
/// Two things this registry deliberately does not do. It does not bound a grammar to
/// the accepted <em>count</em> of definitions - that is a semantic rule owned by
/// <c>DAT-002</c> and <c>DAT-003</c>. And it does not mint a grammar for a category
/// that has not been granted one; see <see cref="ContentCategory"/> for why
/// <c>presentation</c> is absent.
/// </para>
/// </remarks>
public static class ContentCategories
{
    private static readonly ContentCategoryDescriptor[] Declared =
    {
        // Eight resources: the six specialized materials plus common ore and Hyper
        // Gold. The A-F letters are a separate `canonical_letter` field, not the ID:
        // doc 40 § Resources enumerates "ID, canonical letter, ..." as two fields.
        Declare(ContentCategory.Resource, "resources", "^RSC-[0-9]{2}$"),
        Declare(ContentCategory.Mech, "mechs", "^MCH-[0-9]{2}$"),

        // Two grammars: the per-enemy definitions and the shared elite modifier
        // aggregate, which is not itself an enemy.
        Declare(ContentCategory.Enemy, "enemies", "^EN-[0-9]{2}$", "^ELT-[0-9]{2}$"),
        Declare(ContentCategory.Boss, "bosses", "^BOSS-[0-9]{2}$"),

        // A weapon ID is its unordered material pair, so the two letters are the ID.
        // The second grammar is the weapons catalog's own aggregate: the stat price
        // curve, FORMULA-01. Its authored stem matched no grammar at all, and it needs
        // one for the same reason WAV-01 and MGC-01 do - every schema references other
        // definitions by stable ID. It stays in content/weapons/ because the price
        // curve is a shared rule *within* the weapon domain, which is the reasoning
        // that already keeps ELT-01 in content/enemies/ rather than in a constants
        // directory.
        Declare(ContentCategory.Weapon, "weapons", "^W-[A-F]{2}$", "^FORMULA-[0-9]{2}$"),

        // A branch ID is its parent weapon plus a kebab-case name, which is what makes
        // a branch reference unambiguous about which weapon it belongs to.
        Declare(ContentCategory.Branch, "branches", "^W-[A-F]{2}(-[a-z0-9]+)+$"),

        // UTL-<material letter><variant>, plus the ore-only resource radar UTL-R1 that
        // doc 40 § Utilities mints by name.
        Declare(ContentCategory.Utility, "utilities", "^UTL-[A-FR][1-9]$"),
        Declare(ContentCategory.Relic, "relics", "^REL-[0-9]{2}$"),

        // PU-<group><number>, where the group letter is the UI grouping.
        Declare(ContentCategory.PowerUp, "powerups", "^PU-[A-Z][0-9]{2}$"),
        Declare(ContentCategory.Unlock, "unlocks", "^UNL-[0-9]{2}$"),
        Declare(ContentCategory.MiningSite, "mining-sites", "^SITE-[0-9]{2}$"),
        Declare(ContentCategory.Encounter, "encounters", "^WAV-[0-9]{2}$"),
        Declare(ContentCategory.Map, "maps", "^MGC-[0-9]{2}$"),
        Declare(ContentCategory.Player, "player", "^PLAYER-[0-9]{2}$"),
    };

    private static readonly Dictionary<ContentCategory, ContentCategoryDescriptor> ByCategory =
        BuildCategoryIndex();

    private static readonly Dictionary<string, ContentCategoryDescriptor> ByDirectory =
        BuildDirectoryIndex();

    /// <summary>Every declared category.</summary>
    public static IReadOnlyList<ContentCategoryDescriptor> All { get; } =
        new ReadOnlyCollection<ContentCategoryDescriptor>(Declared);

    /// <summary>Returns the descriptor for <paramref name="category"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is not declared.</exception>
    public static ContentCategoryDescriptor Describe(ContentCategory category)
    {
        if (!ByCategory.TryGetValue(category, out ContentCategoryDescriptor? descriptor))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "no ID grammar is declared for this content category");
        }

        return descriptor;
    }

    /// <summary>
    /// Resolves the category owning <paramref name="directoryName"/>, the directory
    /// name beneath <c>content/</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="directoryName"/> is null.</exception>
    public static bool TryResolveDirectory(
        string directoryName,
        out ContentCategoryDescriptor? descriptor)
    {
        ArgumentNullException.ThrowIfNull(directoryName);
        return ByDirectory.TryGetValue(directoryName, out descriptor);
    }

    private static ContentCategoryDescriptor Declare(
        ContentCategory category,
        string directoryName,
        params string[] idPatterns)
    {
        return new ContentCategoryDescriptor(category, directoryName, idPatterns);
    }

    private static Dictionary<ContentCategory, ContentCategoryDescriptor> BuildCategoryIndex()
    {
        Dictionary<ContentCategory, ContentCategoryDescriptor> index = new(Declared.Length);
        foreach (ContentCategoryDescriptor descriptor in Declared)
        {
            if (!index.TryAdd(descriptor.Category, descriptor))
            {
                throw new InvalidOperationException(
                    "content category " + descriptor.Category + " is declared twice");
            }
        }

        return index;
    }

    private static Dictionary<string, ContentCategoryDescriptor> BuildDirectoryIndex()
    {
        Dictionary<string, ContentCategoryDescriptor> index =
            new(Declared.Length, StringComparer.Ordinal);
        foreach (ContentCategoryDescriptor descriptor in Declared)
        {
            if (!index.TryAdd(descriptor.DirectoryName, descriptor))
            {
                throw new InvalidOperationException(
                    "content directory '" + descriptor.DirectoryName + "' is declared twice");
            }
        }

        return index;
    }
}
