using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Content.Vocabulary;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-unlock</c>: the field table of one permanent option unlock.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § PowerUps and option
/// unlocks: "Unlocks include exact Hyper Gold cost, nonrefundable flag, owned content
/// additions, and whether ownership may be disabled."
/// </para>
/// <para>
/// <b>An unlock is a cost, a kind, and the IDs it grants.</b> The authored shape
/// carried the kind three times - as <c>category</c>, as <c>kind</c>, and as four
/// booleans that were constant per kind - plus a display name beside every granted ID,
/// plus a count that was the length of the ID list. Those four booleans in particular
/// are the semantics of the kind, and putting them in the definition duplicates the
/// kind's registered descriptor.
/// </para>
/// </remarks>
public static class UnlockSchema
{
    /// <summary>What the whole option-unlock catalog costs, in Hyper Gold.</summary>
    /// <remarks>
    /// <c>docs/63-permanent-option-unlock-catalog.md</c> § Catalog overview. The
    /// maximum-account envelope is this plus the PowerUp catalog total, and it is
    /// deliberately not asserted anywhere: no accepted document states it, and
    /// comparing a derived number against itself proves nothing.
    /// </remarks>
    public const int CatalogTotalHyperGold = 2150;

    /// <summary>What an unlock grants.</summary>
    /// <remarks>
    /// The two authored tokens were the tree's only <c>camelCase</c> values. The
    /// convention everywhere else is kebab case, and one shape everywhere is what lets a
    /// reader tell a token from a sentence.
    /// </remarks>
    public static ClosedVocabulary UnlockKinds { get; } = new(
        "an unlock kind",
        "GDD-OPTION-UNLOCK-CATALOG",
        "relic-cache-pool-entry",
        "utility-blueprints");

    /// <summary>The unlock field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "an option unlock definition",
        DefinitionField.Integer("cost_hyper_gold"),
        DefinitionField.Text("unlock_kind"),
        DefinitionField.ArrayOf("granted_ids", DefinitionField.ElementOf(FieldShape.Text)),
        DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The values the compiler derives for an unlock.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "category",
            "a third encoding of unlock_kind, one-to-one with it and differing only in plurality",
            "/unlock_kind"),
        DerivedField.At(
            "effect",
            "a one-line restatement of the kind and the granted IDs",
            "/unlock_kind", "/granted_ids"),
        DerivedField.At(
            "non_radar_utilities_per_four_material_profile",
            "the before and after counts are the utility catalog's fresh-profile and unlocked "
                + "sizes multiplied by the materials present in a run",
            "/granted_ids", "UTL-R1"),
    });
}

/// <summary>One validated permanent option unlock definition.</summary>
public sealed class UnlockDefinition : ContentDefinition
{
    internal UnlockDefinition(
        DefinitionEnvelope envelope,
        long costHyperGold,
        string unlockKind,
        IReadOnlyList<string> grantedIds)
        : base(envelope, DefinitionKind.Unlock)
    {
        CostHyperGold = costHyperGold;
        UnlockKind = unlockKind;
        GrantedIds = grantedIds;
    }

    /// <summary>The exact Hyper Gold cost.</summary>
    public long CostHyperGold { get; }

    /// <summary>What kind of content the unlock grants.</summary>
    public string UnlockKind { get; }

    /// <summary>The stable IDs of the granted content.</summary>
    public IReadOnlyList<string> GrantedIds { get; }
}

/// <summary>The wire shape of an unlock definition's domain fields.</summary>
internal sealed class UnlockDto
{
    [JsonPropertyName("cost_hyper_gold")]
    public double? CostHyperGold { get; set; }

    [JsonPropertyName("unlock_kind")]
    public string? UnlockKind { get; set; }

    [JsonPropertyName("granted_ids")]
    public List<string>? GrantedIds { get; set; }
}

/// <summary>Source-generated metadata for <see cref="UnlockDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(UnlockDto))]
internal sealed partial class UnlockJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one permanent option unlock definition.</summary>
public static class UnlockReader
{
    /// <summary>Reads one unlock.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public static DefinitionReadResult Read(ReadOnlySpan<byte> utf8, CategoryReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DiagnosticBag bag = new();
        if (!CategoryPrelude.Run(
                utf8, context, bag, out DefinitionEnvelope? envelope,
                out DocumentOutline _, out JsonStructure structure))
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        UnlockDto? dto = JsonSerializer.Deserialize(utf8, UnlockJsonContext.Default.UnlockDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        string? id = envelope?.Id.Value;
        Validate(dto, context, id, bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        UnlockDefinition definition = new(
            envelope,
            (long)dto.CostHyperGold!.Value,
            dto.UnlockKind!,
            new ReadOnlyCollection<string>(new List<string>(dto.GrantedIds!)));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        UnlockDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        bool kindIsKnown = SemanticCheck.Token(
            dto.UnlockKind, UnlockSchema.UnlockKinds, root.AppendProperty("unlock_kind"), context,
            id, bag);

        SemanticCheck.Integer(
            dto.CostHyperGold, root.AppendProperty("cost_hyper_gold"), context, id, bag,
            "cost_hyper_gold");
        SemanticCheck.AtLeast(
            dto.CostHyperGold, 1, root.AppendProperty("cost_hyper_gold"), context, id, bag,
            "cost_hyper_gold is at least one; a free unlock is content the profile already owns");

        List<string> granted = dto.GrantedIds ?? new();
        JsonPointer pointer = root.AppendProperty("granted_ids");

        if (granted.Count == 0)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.ArrayCardinalityWrong,
                context.SourcePath,
                pointer,
                id,
                "an unlock grants at least one piece of content; an unlock that grants nothing is "
                    + "a purchase with no effect"));
        }

        ContentCategory granted_category = string.Equals(
            dto.UnlockKind, "utility-blueprints", StringComparison.Ordinal)
            ? ContentCategory.Utility
            : ContentCategory.Relic;

        for (int index = 0; index < granted.Count; index++)
        {
            if (!kindIsKnown)
            {
                break;
            }

            SemanticCheck.ReferenceGrammar(
                granted[index], granted_category, pointer.AppendIndex(index), context, id, bag);
        }

        SemanticCheck.Distinct(granted, pointer, context, id, bag, "an unlock's granted IDs");
    }
}
