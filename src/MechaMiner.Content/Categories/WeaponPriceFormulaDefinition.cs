using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Categories;

/// <summary>
/// <c>SCH-CNT-002-weapon-stat-price-formula</c>: the field table of <c>FORMULA-01</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Unit and numeric policy:
/// "Formulas allowed to players, such as weapon upgrade price, are represented by a
/// registered formula kind plus parameters, not arbitrary script strings." The authored
/// definition carried the string <c>5n(n + 1)</c>, a variable description, the first
/// ten prices, and four cumulative checkpoints - an expression plus four tables of its
/// own outputs.
/// </para>
/// <para>
/// It stays in <c>content/weapons/</c> because the price curve is a shared rule within
/// the weapon domain, which is the same reasoning that keeps the shared elite modifier
/// profile in <c>content/enemies/</c>. It is an aggregate, so it omits
/// <c>name_key</c>: the authored catalog even had a localization string for it, which
/// put an internal title into a catalog scoped to strings players read.
/// </para>
/// </remarks>
public static class WeaponPriceFormulaSchema
{
    /// <summary>The formula field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "the weapon stat price formula",
        DefinitionField.Text("formula_kind"),
        DefinitionField.Text("currency_resource_id"),
        DefinitionField.ParameterMap("parameters"),
        DefinitionField.Text("applies_to"));

    /// <summary>The values the compiler derives for the formula.</summary>
    /// <remarks>
    /// Everything the authored file held besides the kind and its parameters was an
    /// output of the formula. The price-curve report recomputes them, which is doc 40
    /// § Analytical's "recalculate ... price curves" - and recomputing them against an
    /// authored copy of themselves would prove nothing.
    /// </remarks>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "formula",
            "an expression string is not a representation doc 40 accepts for a player-facing "
                + "formula; the registered kind plus its parameters is",
            "/formula_kind", "/parameters"),
        DerivedField.At(
            "first_ten_prices",
            "the formula evaluated at the first ten ranks",
            "/formula_kind", "/parameters"),
        DerivedField.At(
            "cumulative_cost_checkpoints",
            "the running sum of the formula's outputs at the checkpoint ranks",
            "/formula_kind", "/parameters"),
        DerivedField.At(
            "equivalent_by_depth",
            "a restatement of the same formula in terms of purchase depth",
            "/formula_kind", "/parameters"),
    });
}

/// <summary>The validated weapon stat price formula.</summary>
public sealed class WeaponPriceFormulaDefinition : ContentDefinition
{
    internal WeaponPriceFormulaDefinition(
        DefinitionEnvelope envelope,
        string formulaKind,
        string currencyResourceId,
        string appliesTo)
        : base(envelope, DefinitionKind.WeaponStatPriceFormula)
    {
        FormulaKind = formulaKind;
        CurrencyResourceId = currencyResourceId;
        AppliesTo = appliesTo;
    }

    /// <summary>The registered formula kind.</summary>
    public string FormulaKind { get; }

    /// <summary>The resource the price is paid in.</summary>
    public string CurrencyResourceId { get; }

    /// <summary>What the curve prices.</summary>
    public string AppliesTo { get; }
}

/// <summary>The wire shape of the price formula's domain fields.</summary>
internal sealed class WeaponPriceFormulaDto
{
    [JsonPropertyName("formula_kind")]
    public string? FormulaKind { get; set; }

    [JsonPropertyName("currency_resource_id")]
    public string? CurrencyResourceId { get; set; }

    [JsonPropertyName("applies_to")]
    public string? AppliesTo { get; set; }
}

/// <summary>Source-generated metadata for <see cref="WeaponPriceFormulaDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(WeaponPriceFormulaDto))]
internal sealed partial class WeaponPriceFormulaJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates the weapon stat price formula.</summary>
public static class WeaponPriceFormulaReader
{
    /// <summary>Reads the price formula.</summary>
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

        WeaponPriceFormulaDto? dto = JsonSerializer.Deserialize(
            utf8, WeaponPriceFormulaJsonContext.Default.WeaponPriceFormulaDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        string? id = envelope?.Id.Value;
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.BehaviorToken(
            dto.FormulaKind, root.AppendProperty("formula_kind"), context, id, bag);
        SemanticCheck.ReferenceGrammar(
            dto.CurrencyResourceId, ContentCategory.Resource,
            root.AppendProperty("currency_resource_id"), context, id, bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        WeaponPriceFormulaDefinition definition = new(
            envelope, dto.FormulaKind!, dto.CurrencyResourceId!, dto.AppliesTo!);

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }
}
