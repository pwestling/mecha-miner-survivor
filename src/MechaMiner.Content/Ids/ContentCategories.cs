using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Ids;

/// <summary>
/// The registry of content categories, their directories, and their ID grammars.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every prefix below now comes from an accepted document.</b>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Stable ID policy requires
/// reusing accepted gameplay IDs "exactly", and the list below says which document covers
/// which prefix, because "derived from an accepted document" and "minted by this file" are
/// not the same standing and a reader has to be able to tell them apart without opening
/// every catalog. Three prefixes were this file's own claim until recently; the paragraph
/// on them below records that they are not any more.
/// </para>
/// <para>
/// <b>From an accepted document.</b> The gameplay catalogs mint <c>MCH-</c>,
/// <c>EN-</c>, <c>BOSS-</c>, <c>W-</c>, <c>REL-</c>, <c>PU-</c>, <c>UNL-</c>, and
/// <c>UTL-</c>; doc 40 § Utilities mints <c>UTL-R1</c> by name; doc 40 § Encounter
/// schedule and § Map generation mint <c>WAV-01</c> and <c>MGC-01</c>.
/// </para>
/// <para>
/// <b><c>RSC-</c> and <c>FORMULA-</c> are now minted too.</b> Doc 40 § Minted content-ID
/// grammars states "every prefix in the table below is minted by this document" and gives
/// both a row: <c>RSC-</c> as <c>^RSC-[0-9]{2}$</c> over <c>content/resources/</c>, and
/// <c>FORMULA-</c> as <c>^FORMULA-[0-9]{2}$</c> over <c>content/weapons/</c>. The prose
/// beside the table says why - "<c>RSC-</c> identifies ordinary embodied content and omits
/// neither field" and "<c>FORMULA-01</c> is a shared definition players read the effect
/// of". Neither is this file's own claim any more, and the grammars above restate the
/// document rather than standing in for it.
/// </para>
/// <para>
/// <b><c>SITE-</c>, <c>ELT-</c> and <c>PLAYER-</c> were minted by this implementation, and
/// are now minted by doc 40 too.</b> They were minted by decision of the integration owner
/// because the definitions exist and every schema references other definitions by stable
/// ID, and for as long as no document in this tree minted them they stayed this file's own
/// claim. Doc 40 § Minted content-ID grammars now gives each one a row - <c>SITE-</c> as
/// <c>^SITE-[0-9]{2}$</c> over <c>content/maps/</c>, <c>ELT-</c> as
/// <c>^ELT-[0-9]{2}$</c> over <c>content/enemies/</c>, and <c>PLAYER-</c> as
/// <c>^PLAYER-[0-9]{2}$</c> over <c>content/player/</c>, all three minted under § Map
/// generation - so for these three the grammars above restate the document rather than
/// standing in for it, and no prefix here is an implementation-only mint today.
/// </para>
/// <para>
/// <b>What doc 40 still does not mint, and legitimately.</b> The accepted gameplay
/// register it accounts for as prefixes "reused from the accepted gameplay register":
/// § Stable ID policy names it as <c>MCH-01</c>, <c>EN-01</c>, <c>BOSS-01</c>,
/// <c>W-AB</c>, <c>REL-01</c> "and equivalent utility/PowerUp/unlock IDs". Those are
/// deliberately absent from the table, and they are the only prefixes here that are. A
/// prefix here is still not evidence that a document mints it - that is asserted, below.
/// </para>
/// <para>
/// Which of these states a prefix is in is asserted, not just described:
/// <c>DocumentGrammarAgreementTests</c> reads doc 40's table and holds this file, the
/// category schemas, and the document to one grammar per prefix, with the grammars no
/// document mints recorded by name - eight of them, the reuse register alone, since
/// <c>SITE-</c>, <c>ELT-</c> and <c>PLAYER-</c> left that list for doc 40's table.
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
        // curve, FORMULA-01, which doc 40 § Minted content-ID grammars mints and
        // places in content/weapons/ under its rule that "an aggregate lives in the
        // catalog directory it serves". Placement is not exclusion: the same section
        // requires content/weapons/ to exclude FORMULA-01 from its 15-recipe
        // population assertion *by name*, and that is DAT-002's rule to enforce, not
        // this grammar's.
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
