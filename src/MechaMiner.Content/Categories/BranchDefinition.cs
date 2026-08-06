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
/// <c>SCH-CNT-002-branch</c>: the field table of one weapon branch.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Branches: "Fields include
/// parent weapon, transformation class, two-unit material cost, behavior modifier
/// kind/parameters, affected snapshot/live properties, exclusions/recursion flags,
/// summary/detail keys, and compatibility notes. A branch cannot register against
/// multiple weapons or add an unrecognized fourth stat."
/// </para>
/// <para>
/// <b>The field is <c>branch_class</c>, never <c>class</c>.</b> Thirty of the
/// forty-five files used the shorter spelling and fifteen the longer. The longer one
/// wins on two independent grounds: it says what the value classifies, and
/// <c>class</c> is a C# keyword, so a typed model reading it would need an escape or an
/// attribute on every branch. A document carrying <c>class</c> fails as an unknown
/// field, which is what stops the two spellings coexisting.
/// </para>
/// <para>
/// <b>One <c>expected_effect</c>, not a three-way split.</b> Two field names carried
/// the same concept - a qualitative scene description plus an approximate magnitude -
/// and seventeen branches carried neither. <c>qualitative</c> is required and
/// <c>magnitude</c> is omitted where the effect is unquantified, which is the shape that
/// makes "unquantified" expressible without a second field name.
/// </para>
/// </remarks>
public static class BranchSchema
{
    /// <summary>How many units of its assigned material a branch costs.</summary>
    /// <remarks>
    /// Every accepted branch costs two, but the count is a field rather than a constant:
    /// a future branch could cost a different number, and the current agreement is worth
    /// asserting from the data rather than assuming from the schema.
    /// </remarks>
    public const int MutuallyExclusiveBranchCount = 2;

    /// <summary>The expected-effect sub-shape.</summary>
    public static DefinitionShape ExpectedEffect { get; } = DefinitionShape.Of(
        "a branch's expected effect",
        DefinitionField.Text("qualitative"),
        DefinitionField.OptionalParameterMap("magnitude"));

    /// <summary>The exclusivity sub-shape.</summary>
    /// <remarks>
    /// <c>irreversible_within_run</c> rather than <c>irreversible</c>: the rule sentence
    /// the field travelled with scopes the commitment to one run, and a relic or branch
    /// is not irreversible across runs. The longer name is the one that cannot be
    /// misread, and after the rename the field states the rule the sentence was stating.
    /// </remarks>
    public static DefinitionShape Exclusivity { get; } = DefinitionShape.Of(
        "a branch's exclusivity",
        DefinitionField.Flag("irreversible_within_run"),
        DefinitionField.ArrayOf(
            "mutually_exclusive_with", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The branch field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a branch definition",
        DefinitionField.Text("weapon_id"),
        DefinitionField.Text("branch_class"),
        DefinitionField.Text("cost_material_id"),
        DefinitionField.Integer("cost_units"),
        DefinitionField.Text("behavior_kind"),
        DefinitionField.ParameterMap("effects"),
        DefinitionField.Object("exclusivity", Exclusivity),
        DefinitionField.OptionalObject(
            "global_attack_rate_mapping", WeaponSchema.GlobalAttackRateMapping),
        DefinitionField.OptionalObject("expected_effect", ExpectedEffect),
        DefinitionField.ArrayOf("rules", DefinitionField.ElementOf(FieldShape.Text)));

    /// <summary>The values the compiler derives for a branch.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.At(
            "weapon_name",
            "the display name of the weapon named by weapon_id, which the string catalog holds",
            "/weapon_id"),
        DerivedField.At(
            "availability",
            "both availability sentences are catalog-wide: every branch appears immediately after "
                + "its weapon is equipped, and the purchase cost is cost_material_id and "
                + "cost_units",
            "/cost_material_id", "/cost_units"),
        DerivedField.At(
            "prerequisites",
            "no branch has a prerequisite and no document describes one; an empty array on every "
                + "file is the absence of prerequisites"),
        DerivedField.At(
            "favorable_scene_effect",
            "the superseded spelling of expected_effect; two field names for one concept is two "
                + "writers on it",
            "/expected_effect"),
    });
}

/// <summary>One validated weapon branch definition.</summary>
public sealed class BranchDefinition : ContentDefinition
{
    internal BranchDefinition(
        DefinitionEnvelope envelope,
        string weaponId,
        string branchClass,
        string costMaterialId,
        long costUnits,
        string behaviorKind,
        IReadOnlyList<string> mutuallyExclusiveWith)
        : base(envelope, DefinitionKind.Branch)
    {
        WeaponId = weaponId;
        BranchClass = branchClass;
        CostMaterialId = costMaterialId;
        CostUnits = costUnits;
        BehaviorKind = behaviorKind;
        MutuallyExclusiveWith = mutuallyExclusiveWith;
    }

    /// <summary>The one weapon this branch registers against.</summary>
    public string WeaponId { get; }

    /// <summary>Which of the three transformation classes this branch is.</summary>
    public string BranchClass { get; }

    /// <summary>The resource this branch is fabricated from.</summary>
    public string CostMaterialId { get; }

    /// <summary>How many units of that resource it costs.</summary>
    public long CostUnits { get; }

    /// <summary>The registered behavior modifier token.</summary>
    public string BehaviorKind { get; }

    /// <summary>The sibling branches installing this one rules out.</summary>
    public IReadOnlyList<string> MutuallyExclusiveWith { get; }
}

/// <summary>The wire shape of a branch definition's domain fields.</summary>
internal sealed class BranchDto
{
    [JsonPropertyName("weapon_id")]
    public string? WeaponId { get; set; }

    [JsonPropertyName("branch_class")]
    public string? BranchClass { get; set; }

    [JsonPropertyName("cost_material_id")]
    public string? CostMaterialId { get; set; }

    [JsonPropertyName("cost_units")]
    public double? CostUnits { get; set; }

    [JsonPropertyName("behavior_kind")]
    public string? BehaviorKind { get; set; }

    [JsonPropertyName("exclusivity")]
    public ExclusivityDto? Exclusivity { get; set; }

    internal sealed class ExclusivityDto
    {
        [JsonPropertyName("irreversible_within_run")]
        public bool? IrreversibleWithinRun { get; set; }

        [JsonPropertyName("mutually_exclusive_with")]
        public List<string>? MutuallyExclusiveWith { get; set; }
    }
}

/// <summary>Source-generated metadata for <see cref="BranchDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(BranchDto))]
internal sealed partial class BranchJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one weapon branch definition.</summary>
public static class BranchReader
{
    /// <summary>Reads one branch.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public static DefinitionReadResult Read(ReadOnlySpan<byte> utf8, CategoryReadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        DiagnosticBag bag = new();
        if (!CategoryPrelude.Run(
                utf8, context, bag, out DefinitionEnvelope? envelope, out string? id,
                out DocumentOutline _, out JsonStructure structure))
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        BranchDto? dto = JsonSerializer.Deserialize(utf8, BranchJsonContext.Default.BranchDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        Validate(dto, context, id, StructuralReport.Of(bag), bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        List<string> exclusions = dto.Exclusivity?.MutuallyExclusiveWith ?? new List<string>();
        BranchDefinition definition = new(
            envelope,
            dto.WeaponId!,
            dto.BranchClass!,
            dto.CostMaterialId!,
            (long)dto.CostUnits!.Value,
            dto.BehaviorKind!,
            new ReadOnlyCollection<string>(new List<string>(exclusions)));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        BranchDto dto,
        CategoryReadContext context,
        string? id,
        StructuralReport structural,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.Token(
            dto.BranchClass, ContentVocabularies.BranchClasses,
            root.AppendProperty("branch_class"), context, id, bag);
        SemanticCheck.BehaviorToken(
            dto.BehaviorKind, root.AppendProperty("behavior_kind"), context, id, bag);
        SemanticCheck.ReferenceGrammar(
            dto.CostMaterialId, ContentCategory.Resource, root.AppendProperty("cost_material_id"),
            context, id, bag);

        SemanticCheck.Integer(
            dto.CostUnits, root.AppendProperty("cost_units"), context, id, bag, "cost_units");
        SemanticCheck.AtLeast(
            dto.CostUnits, 1, root.AppendProperty("cost_units"), context, id, bag,
            "cost_units is a count of specialized material units and is at least one; a branch "
                + "that costs nothing is not a fabrication choice");

        ValidateParentWeapon(dto, context, id, bag);
        ValidateExclusivity(dto, context, id, structural, bag);
    }

    private static void ValidateParentWeapon(
        BranchDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty("weapon_id");
        if (!SemanticCheck.ReferenceGrammar(
                dto.WeaponId, ContentCategory.Weapon, pointer, context, id, bag))
        {
            return;
        }

        if (id is null || id.StartsWith(dto.WeaponId + "-", StringComparison.Ordinal))
        {
            return;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.CrossReferenceContradictsOwnId,
            context.SourcePath,
            pointer,
            id,
            "a branch registers against exactly one weapon, and that weapon is the one its own ID "
                + "names: a branch ID is its parent weapon's ID plus a hyphen and a kebab-case "
                + "name. '" + id + "' does not begin '" + dto.WeaponId + "-', so the ID and the "
                + "field disagree about which weapon owns this branch. Exactly-one is structural "
                + "here rather than a count, because weapon_id is a scalar and an array of two "
                + "would be a type error",
            new[] { dto.WeaponId! }));
    }

    private static void ValidateExclusivity(
        BranchDto dto,
        CategoryReadContext context,
        string? id,
        StructuralReport structural,
        DiagnosticBag bag)
    {
        if (dto.Exclusivity is null)
        {
            return;
        }

        JsonPointer pointer =
            JsonPointer.Root.AppendProperty("exclusivity").AppendProperty("mutually_exclusive_with");
        List<string> exclusions = dto.Exclusivity.MutuallyExclusiveWith ?? new List<string>();

        if (!structural.Reported(pointer))
        {
            SemanticCheck.ExactCount(
                exclusions.Count, BranchSchema.MutuallyExclusiveBranchCount, pointer, context, id,
                bag,
                "a branch rules out exactly the other two branches of its own weapon, because a "
                    + "weapon has three and installing one commits to it for the run");
        }

        for (int index = 0; index < exclusions.Count; index++)
        {
            JsonPointer element = pointer.AppendIndex(index);
            if (!SemanticCheck.ReferenceGrammar(
                    exclusions[index], ContentCategory.Branch, element, context, id, bag))
            {
                continue;
            }

            if (string.Equals(exclusions[index], id, StringComparison.Ordinal))
            {
                bag.Add(ContentDiagnostic.CreateError(
                    ContentDiagnosticCodes.DuplicateValueInDefinition,
                    context.SourcePath,
                    element,
                    id,
                    "a branch does not exclude itself; the list names the sibling branches "
                        + "installing this one rules out"));
                continue;
            }

            if (dto.WeaponId is null
                || exclusions[index].StartsWith(dto.WeaponId + "-", StringComparison.Ordinal))
            {
                continue;
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.CrossReferenceContradictsOwnId,
                context.SourcePath,
                element,
                id,
                "a branch is mutually exclusive only with siblings of the same weapon, so every "
                    + "entry begins '" + dto.WeaponId + "-'. A branch of another weapon is not a "
                    + "sibling and installing it does not compete for this weapon's commitment",
                new[] { exclusions[index] }));
        }

        SemanticCheck.Distinct(
            exclusions, pointer, context, id, bag, "a branch's mutually exclusive siblings");
    }
}
