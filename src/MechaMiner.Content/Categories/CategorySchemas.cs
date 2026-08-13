using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Categories;

/// <summary>
/// The registry of definition kinds: which category each belongs to, which schema
/// document mirrors it, what its field table is, and what the compiler derives for it.
/// </summary>
/// <remarks>
/// <para>
/// One registry rather than a lookup per consumer. The field-order test, the schema
/// agreement test, the reader, and the canonical writer all resolve a kind through
/// here, so a kind that is added without a schema document or without a field table
/// fails to compile rather than silently validating nothing.
/// </para>
/// </remarks>
public static class CategorySchemas
{
    private static readonly CategoryDescriptor[] Declared =
    {
        Declare(
            DefinitionKind.Resource, ContentCategory.Resource, "resource.schema.json",
            ResourceSchema.Shape, ResourceSchema.Derived),
        Declare(
            DefinitionKind.Mech, ContentCategory.Mech, "mech.schema.json",
            MechSchema.Shape, MechSchema.Derived),
        Declare(
            DefinitionKind.Enemy, ContentCategory.Enemy, "enemy.schema.json",
            EnemySchema.Shape, EnemySchema.Derived),
        Declare(
            DefinitionKind.EliteModifiers, ContentCategory.Enemy, "elite-modifiers.schema.json",
            EliteModifierSchema.Shape, EliteModifierSchema.Derived, omitsNameKey: true),
        Declare(
            DefinitionKind.Boss, ContentCategory.Boss, "boss.schema.json",
            BossSchema.Shape, BossSchema.Derived),
        Declare(
            DefinitionKind.MiningSite, ContentCategory.MiningSite, "mining-site.schema.json",
            MiningSiteSchema.Shape, MiningSiteSchema.Derived),
        Declare(
            DefinitionKind.EncounterSchedule, ContentCategory.Encounter,
            "encounter-schedule.schema.json",
            EncounterScheduleSchema.Shape, EncounterScheduleSchema.Derived, omitsNameKey: true),
        Declare(
            DefinitionKind.MapGenerationContract, ContentCategory.Map,
            "map-generation-contract.schema.json",
            MapGenerationSchema.Shape, MapGenerationSchema.Derived, omitsNameKey: true),
        Declare(
            DefinitionKind.PlayerBaseline, ContentCategory.Player, "player-baseline.schema.json",
            PlayerBaselineSchema.Shape, PlayerBaselineSchema.Derived, omitsNameKey: true),
        Declare(
            DefinitionKind.Weapon, ContentCategory.Weapon, "weapon.schema.json",
            WeaponSchema.Shape, WeaponSchema.Derived),
        Declare(
            DefinitionKind.WeaponStatPriceFormula, ContentCategory.Weapon,
            "weapon-stat-price-formula.schema.json",
            WeaponPriceFormulaSchema.Shape, WeaponPriceFormulaSchema.Derived, omitsNameKey: true),
        Declare(
            DefinitionKind.Branch, ContentCategory.Branch, "branch.schema.json",
            BranchSchema.Shape, BranchSchema.Derived),
        Declare(
            DefinitionKind.Utility, ContentCategory.Utility, "utility.schema.json",
            UtilitySchema.Shape, UtilitySchema.Derived),
        Declare(
            DefinitionKind.Relic, ContentCategory.Relic, "relic.schema.json",
            RelicSchema.Shape, RelicSchema.Derived),
        Declare(
            DefinitionKind.PowerUp, ContentCategory.PowerUp, "powerup.schema.json",
            PowerUpSchema.Shape, PowerUpSchema.Derived),
        Declare(
            DefinitionKind.Unlock, ContentCategory.Unlock, "unlock.schema.json",
            UnlockSchema.Shape, UnlockSchema.Derived),
    };

    private static readonly Dictionary<DefinitionKind, CategoryDescriptor> ByKind = BuildIndex();

    /// <summary>Every declared kind, in declared order.</summary>
    public static IReadOnlyList<CategoryDescriptor> All { get; } =
        new ReadOnlyCollection<CategoryDescriptor>(Declared);

    /// <summary>Returns the descriptor for <paramref name="kind"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">No field table is declared for the kind.</exception>
    public static CategoryDescriptor Describe(DefinitionKind kind)
    {
        return ByKind.TryGetValue(kind, out CategoryDescriptor? descriptor)
            ? descriptor
            : CategoryDescriptor.Undeclared(kind);
    }

    /// <summary>Reads one definition of <paramref name="context"/>'s kind.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">No reader is declared for the kind.</exception>
    public static DefinitionReadResult Read(
        ReadOnlySpan<byte> utf8,
        CategoryReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Kind switch
        {
            DefinitionKind.Resource => ResourceReader.Read(utf8, context),
            DefinitionKind.Mech => MechReader.Read(utf8, context),
            DefinitionKind.Enemy => EnemyReader.Read(utf8, context),
            DefinitionKind.EliteModifiers => EliteModifierReader.Read(utf8, context),
            DefinitionKind.Boss => BossReader.Read(utf8, context),
            DefinitionKind.MiningSite => MiningSiteReader.Read(utf8, context),
            DefinitionKind.EncounterSchedule => EncounterScheduleReader.Read(utf8, context),
            DefinitionKind.MapGenerationContract => MapGenerationReader.Read(utf8, context),
            DefinitionKind.PlayerBaseline => PlayerBaselineReader.Read(utf8, context),
            DefinitionKind.Weapon => WeaponReader.Read(utf8, context),
            DefinitionKind.WeaponStatPriceFormula => WeaponPriceFormulaReader.Read(utf8, context),
            DefinitionKind.Branch => BranchReader.Read(utf8, context),
            DefinitionKind.Utility => UtilityReader.Read(utf8, context),
            DefinitionKind.Relic => RelicReader.Read(utf8, context),
            DefinitionKind.PowerUp => PowerUpReader.Read(utf8, context),
            DefinitionKind.Unlock => UnlockReader.Read(utf8, context),
            _ => throw new ArgumentOutOfRangeException(
                nameof(context), context.Kind, "no reader is declared for this definition kind"),
        };
    }

    private static CategoryDescriptor Declare(
        DefinitionKind kind,
        ContentCategory category,
        string schemaFileName,
        DefinitionShape shape,
        DerivedFieldRegister derived,
        bool omitsNameKey = false)
    {
        return new CategoryDescriptor(kind, category, schemaFileName, shape, derived, omitsNameKey);
    }

    private static Dictionary<DefinitionKind, CategoryDescriptor> BuildIndex()
    {
        Dictionary<DefinitionKind, CategoryDescriptor> index = new(Declared.Length);
        foreach (CategoryDescriptor descriptor in Declared)
        {
            if (!index.TryAdd(descriptor.Kind, descriptor))
            {
                throw new InvalidOperationException(
                    "definition kind " + descriptor.Kind + " is declared twice");
            }
        }

        return index;
    }
}
