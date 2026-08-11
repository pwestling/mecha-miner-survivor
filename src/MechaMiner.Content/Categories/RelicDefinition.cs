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
/// <c>SCH-CNT-002-relic</c>: the field table of one mech relic.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Relics: "Fields include
/// pool availability/unlock, discovery sentence key, sale value, behavior registration,
/// benefit/tradeoff parameters, hook points, affected weapon categories, live-state
/// meter, and presentation. Validation requires one sentence summary, explicit
/// tradeoff, compatibility results for all weapons, and no hidden unsupported
/// behavior."
/// </para>
/// <para>
/// <b>The transformation and the tradeoff are localization keys, not literals.</b> Doc
/// 40 requires an explicit tradeoff, so the field is required and its absence is an
/// error - but the sentence itself is player-facing text, which the string catalog
/// holds. A key satisfies the requirement and keeps the literal out of the definition;
/// deleting the field as redundant with <c>summary_key</c> would have deleted the thing
/// the requirement checks.
/// </para>
/// <para>
/// <b><c>hook</c> is unmapped prose, and this field table does not represent it.</b>
/// The corpus carries a <c>behavior_registration.hook</c> sentence per relic, and this
/// schema used to read those ten sentences as each naming one distinct hook point with
/// no overlap between them - which would have made <c>behavior_kind</c> a mapping of
/// them. Measured against the corpus it is false. There are <b>nineteen</b> hook points
/// across the ten strings: nine of the ten join two noun phrases with different head
/// nouns by "and", and both conjuncts are independently realised in the same file's
/// <c>effects</c> block - <c>REL-01</c>'s "activation-rate transformation and opposite
/// geometry" carries <c>primary_activation_frequency_multiplier: 3</c> for the first and
/// <c>mirrors_directional_and_targeted_geometry: true</c> for the second. Only
/// <c>REL-04</c> names a single hook point, and three of the strings overlap each other
/// on rate/cadence transformation. A relic-to-hook mapping would need nineteen invented
/// names and a document that mints them, so there is none: <c>behavior_kind</c> is a
/// token the behavior registry resolves, and it asserts neither a hook vocabulary nor
/// one hook per relic.
/// </para>
/// <para>
/// <b>No vocabulary is asserted for <c>affected_scope</c>, by either artifact, and that is a
/// decision rather than an omission.</b> <c>relic.schema.json</c>'s element enum was struck at
/// <c>b6c9f86</c> on the document owner's ruling that the constraint is not enforceable as
/// written: <c>docs/67</c> states three mutually incompatible scope rules - five members plus
/// an open tail at <c>:24</c>, "only a tagged subset of systems" at <c>:55</c>, a closed
/// three-way partition at <c>:67</c> - so there is no single documented set for an enum to
/// match, and the member the partition names that the enum dropped, "only a clearly defined
/// weapon family", is ungroundable because no document defines a weapon-family taxonomy and
/// <c>docs/30:50</c> denies one exists. <b>That ruling was about the constraint, not about the
/// file it happened to live in.</b> This class held the same constraint in a different
/// artifact - a three-token <c>ClosedVocabulary</c> with one consuming call site in
/// <c>RelicReader</c> - so leaving it would have implemented the ruling in the mirror and not
/// in the thing that gates, which is backwards: <c>content/schemas/README.md</c> says the
/// schemas are a mirror and the typed validator is authoritative. It was also, after
/// <c>b6c9f86</c>, the <em>only</em> remaining assertion of the three tokens, ungrounded on
/// exactly the argument that struck the schema's: it cited
/// <c>GDD-INITIAL-RELIC-CATALOG</c>, which resolves - <c>docs/69-initial-relic-catalog.md</c>
/// carries that <c>doc_id</c> - while enumerating no scope set at all, which is doc 40
/// § Minted value vocabularies' "a resolving citation is necessary and not sufficient" in its
/// purest form.
/// </para>
/// <para>
/// <b>Why leaving it would have been worse than never having ruled at all.</b> Not firing over
/// today's corpus is not the same as not gating: the path is live, five fixtures reach it, and
/// any definition authoring <c>affected_scope</c> as an array with an out-of-vocabulary token is
/// rejected by it. So content authored with a weapon-family scope would still have been
/// rejected - by this class - with no schema declaration left to point at, and the author of
/// that content could not have found the rule that stopped them. <b>A live rule whose
/// documentation has been removed is harder to deal with than a documented rule you disagree
/// with.</b>
/// </para>
/// <para>
/// <b>The general form, which matters more than this instance.</b> When one constraint is
/// carried by both a mirror and a gate, striking it from the mirror alone inverts which
/// artifact is authoritative and leaves the enforcement running without its documentation. The
/// gate is therefore the artifact that must go first, and a decision to strike a constraint has
/// to name every artifact that carries it. This one was carried by two and the ruling named
/// one, which is how the mirror came to be corrected a commit ahead of the thing that gates.
/// </para>
/// <para>
/// <b>The other option is rejected here so it is not reopened.</b> The check had no negative
/// control: five relic fixtures pass an in-vocabulary token through it and nothing anywhere
/// contradicts it, so its rejecting path had never been observed. Writing that missing control
/// would have been the conscientious-looking move and it is the wrong direction - it would mint
/// a control for a vocabulary the ruling says is not enforceable, entrenching an ungrounded
/// constraint and granting it more authority than it ever had. A check earns a control by being
/// enforceable, not by existing.
/// </para>
/// <para>
/// <b>What is lost, stated because "the check is gone" and "nothing states what scopes exist"
/// are different facts.</b> After the strike no artifact in the repository asserts a scope
/// vocabulary: the schema and this class agree that none is asserted, and <c>affected_scope</c>
/// is checked only for what needs no vocabulary - the elements are distinct and there is at
/// least one. <c>docs/67</c>'s three incompatible sentences are now a documentation question
/// for the relic document's owner rather than a constraint anybody enforces, and the field will
/// accept any string until that document states one set. Nothing about the authored corpus
/// changed: all ten relics author <c>affected_scope</c> as a string rather than an array, so
/// this loop never executed over authored content in the first place.
/// </para>
/// <para>
/// <b>And what the strike does not fix, so the field cannot read as resolved.</b>
/// <c>affected_scope</c> remains a <em>type</em> disagreement in both artifacts: the schema
/// declares an array, all ten relics author a bare string, and that is also why the enum was
/// unreachable - a type failure fires before <c>items</c> is applied. Measured at
/// <c>7a28cc2</c>, that is 10 of <c>relic.schema.json</c>'s 245 pinned divergence errors, one
/// per relic. It is <em>not</em> the relic row's 67 type errors: the other 57 are at
/// <c>/rules/N</c>, where the elements are objects and the schema declares strings, which is a
/// different field's disagreement. So this field has gone from wrongly constrained and
/// unreachable to <b>unconstrained and still type-mismatched</b>, which is not part of this
/// change and is not urgent, but is what a reader who sees a constraint removed would otherwise
/// assume was cleaned up.
/// </para>
/// <para>
/// <b>Undoing this is deliberately two edits.</b> A token list and one call site: restore the
/// <c>ClosedVocabulary</c> with its three tokens, and restore the <c>SemanticCheck.Token</c>
/// loop over the elements in <c>Validate</c>. It is kept that cheap on purpose, because the
/// ruling chain behind it has reversed once already and the next reader should be able to see
/// how to reverse it again.
/// </para>
/// <para>
/// <b><c>effects</c> is the strongest case in the tree for an open parameter map.</b>
/// Seventy-four keys across ten relics, with <em>none</em> shared between any two.
/// There is literally no common structure to factor, so the per-kind parameter schema
/// lives with the registered descriptor and the content schema declares the map.
/// </para>
/// </remarks>
public static class RelicSchema
{
    /// <summary>Where a relic comes from.</summary>
    public static ClosedVocabulary PoolAvailabilities { get; } = new(
        "a relic pool availability",
        "GDD-INITIAL-RELIC-CATALOG",
        "fresh-profile",
        "hyper-gold-unlock");

    /// <summary>The live-state meter sub-shape.</summary>
    public static DefinitionShape LiveStateMeter { get; } = DefinitionShape.Of(
        "a relic's live-state meter",
        DefinitionField.Text("meter"),
        DefinitionField.Text("hud_treatment"));

    /// <summary>One cross-document rule with its typed parameters.</summary>
    /// <remarks>
    /// The one rule array in the tree that stays object-shaped: its entries carry typed
    /// siblings beside the sentence, so unwrapping them to plain strings would lose the
    /// numbers. Every other rule array is <c>string[]</c>, because every other rule
    /// object held exactly one key.
    /// </remarks>
    public static DefinitionShape CrossDocumentRule { get; } = DefinitionShape.Of(
        "a cross-document rule",
        DefinitionField.Text("text"),
        DefinitionField.OptionalParameterMap("parameters"));

    /// <summary>The relic field table, in schema-declared order.</summary>
    public static DefinitionShape Shape { get; } = DefinitionShape.Of(
        "a relic definition",
        DefinitionField.Text("transformation_key"),
        DefinitionField.Text("tradeoff_key"),
        // Scope tokens, not stable IDs, so the set clause does not apply. No artifact
        // enumerates the tokens any more: relic.schema.json's element enum was struck at
        // b6c9f86 and this class's AffectedScopes vocabulary at the commit after it - see the
        // remarks above for why the second followed from the first ruling rather than from a
        // new one. Interim either way, and unobservable today: all ten relics author this
        // field as a string rather than an array.
        DefinitionField.ArrayOf(
            "affected_scope", DefinitionField.ElementOf(FieldShape.Text), ArrayOrder.OrderedArray),
        DefinitionField.Text("behavior_kind"),
        DefinitionField.Text("trigger_condition"),
        DefinitionField.ParameterMap("effects"),
        DefinitionField.Text("pool_availability"),
        DefinitionField.OptionalText("unlock_id"),
        // A set: relic.schema.json declares uniqueItems here, and the field table's own name
        // for its elements is a stable relic ID. It no longer declares ^REL-[0-9]{2}$ - that
        // element pattern was struck at b6c9f86 by ruling of the document owner - so this
        // class is now the only side stating that the elements are IDs rather than prose, and
        // it states it as a declaration rather than as a check: nothing validates an element
        // of this field against an ID grammar. The authored corpus does not agree and the
        // authored corpus stands: all ten relics author the field nested under
        // stacking_and_exclusivity and every element names a mechanic, REL-06's being
        // "weapon-authored knockback direction". That belongs to the catalog stream's rename,
        // not here.
        DefinitionField.ArrayOf(
            "overrides_or_replaces",
            DefinitionField.ElementOf(FieldShape.Text),
            ArrayOrder.IdSet),
        DefinitionField.Integer("sale_value_common_ore"),
        DefinitionField.OptionalObject("live_state_meter", LiveStateMeter),
        DefinitionField.ArrayOf(
            "rules", DefinitionField.ElementOf(FieldShape.Text), ArrayOrder.OrderedArray),
        DefinitionField.ArrayOf(
            "cross_document_rules",
            DefinitionField.ElementObject(CrossDocumentRule),
            ArrayOrder.OrderedArray));

    /// <summary>The values the compiler derives for a relic.</summary>
    public static DerivedFieldRegister Derived { get; } = new(new[]
    {
        DerivedField.Nested(
            new[] { "rarity_and_weighting", "in_fresh_profile_pool" },
            "pool_availability being fresh-profile; the two were perfectly anti-correlated across "
                + "all ten relics, which is two encodings of one fact",
            "/pool_availability"),
        DerivedField.Nested(
            new[] { "acquisition", "unlock" },
            "the referenced unlock's own cost, currency, permanence, refundability and effect; "
                + "unlock_id references them instead of copying them",
            "/unlock_id"),
        DerivedField.At(
            "primary_transformation",
            "player-facing text belongs in the string catalog; transformation_key references it"),
        DerivedField.At(
            "core_tradeoff",
            "player-facing text belongs in the string catalog; tradeoff_key references it"),
    });
}

/// <summary>One validated relic definition.</summary>
public sealed class RelicDefinition : ContentDefinition
{
    internal RelicDefinition(
        DefinitionEnvelope envelope,
        string transformationKey,
        string tradeoffKey,
        IReadOnlyList<string> affectedScope,
        string behaviorKind,
        string poolAvailability,
        string? unlockId,
        long saleValueCommonOre,
        IReadOnlyList<string> overridesOrReplaces)
        : base(envelope, DefinitionKind.Relic)
    {
        TransformationKey = transformationKey;
        TradeoffKey = tradeoffKey;
        AffectedScope = affectedScope;
        BehaviorKind = behaviorKind;
        PoolAvailability = poolAvailability;
        UnlockId = unlockId;
        SaleValueCommonOre = saleValueCommonOre;
        OverridesOrReplaces = overridesOrReplaces;
    }

    /// <summary>The localization key of the one-phrase transformation summary.</summary>
    public string TransformationKey { get; }

    /// <summary>The localization key of the explicit tradeoff.</summary>
    public string TradeoffKey { get; }

    /// <summary>What the transformation applies to.</summary>
    public IReadOnlyList<string> AffectedScope { get; }

    /// <summary>
    /// The registered behavior token. Not a mapping of the corpus's <c>hook</c> prose -
    /// see <see cref="RelicSchema"/> for why one is not derivable.
    /// </summary>
    public string BehaviorKind { get; }

    /// <summary>Which pool this relic is drawn from.</summary>
    public string PoolAvailability { get; }

    /// <summary>The unlock that adds it to the pool, where one does.</summary>
    public string? UnlockId { get; }

    /// <summary>What selling a displaced copy yields, in common ore.</summary>
    public long SaleValueCommonOre { get; }

    /// <summary>The relics this one overrides or replaces.</summary>
    public IReadOnlyList<string> OverridesOrReplaces { get; }
}

/// <summary>The wire shape of a relic definition's domain fields.</summary>
internal sealed class RelicDto
{
    [JsonPropertyName("transformation_key")]
    public string? TransformationKey { get; set; }

    [JsonPropertyName("tradeoff_key")]
    public string? TradeoffKey { get; set; }

    [JsonPropertyName("affected_scope")]
    public List<string>? AffectedScope { get; set; }

    [JsonPropertyName("behavior_kind")]
    public string? BehaviorKind { get; set; }

    [JsonPropertyName("pool_availability")]
    public string? PoolAvailability { get; set; }

    [JsonPropertyName("unlock_id")]
    public string? UnlockId { get; set; }

    [JsonPropertyName("overrides_or_replaces")]
    public List<string>? OverridesOrReplaces { get; set; }

    [JsonPropertyName("sale_value_common_ore")]
    public double? SaleValueCommonOre { get; set; }
}

/// <summary>Source-generated metadata for <see cref="RelicDto"/>.</summary>
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false,
    NumberHandling = JsonNumberHandling.Strict)]
[JsonSerializable(typeof(RelicDto))]
internal sealed partial class RelicJsonContext : JsonSerializerContext
{
}

/// <summary>Reads and validates one relic definition.</summary>
public static class RelicReader
{
    /// <summary>Reads one relic.</summary>
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

        RelicDto? dto = JsonSerializer.Deserialize(utf8, RelicJsonContext.Default.RelicDto);
        if (dto is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        Validate(dto, context, id, bag);

        if (bag.HasErrors || envelope is null)
        {
            return new DefinitionReadResult(null, bag.Diagnostics, structure);
        }

        RelicDefinition definition = new(
            envelope,
            dto.TransformationKey!,
            dto.TradeoffKey!,
            new ReadOnlyCollection<string>(new List<string>(dto.AffectedScope!)),
            dto.BehaviorKind!,
            dto.PoolAvailability!,
            dto.UnlockId,
            (long)dto.SaleValueCommonOre!.Value,
            new ReadOnlyCollection<string>(new List<string>(dto.OverridesOrReplaces!)));

        return new DefinitionReadResult(definition, bag.Diagnostics, structure);
    }

    private static void Validate(
        RelicDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer root = JsonPointer.Root;

        SemanticCheck.BehaviorToken(
            dto.BehaviorKind, root.AppendProperty("behavior_kind"), context, id, bag);
        SemanticCheck.Token(
            dto.PoolAvailability, RelicSchema.PoolAvailabilities,
            root.AppendProperty("pool_availability"), context, id, bag);

        // No token check over the elements. The three-token vocabulary this loop used to
        // enforce was struck at this commit: the ruling that struck relic.schema.json's enum
        // was about the constraint, not about the file it lived in. What is still checked here
        // is what the schema also states and what does not need a vocabulary: the elements are
        // distinct, and there is at least one of them.
        List<string> scope = dto.AffectedScope ?? new();
        JsonPointer scopePointer = root.AppendProperty("affected_scope");

        SemanticCheck.Distinct(scope, scopePointer, context, id, bag, "a relic's affected scopes");
        if (scope.Count == 0)
        {
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.ArrayCardinalityWrong,
                context.SourcePath,
                scopePointer,
                id,
                "a relic names at least one affected scope; a transformation that applies to "
                    + "nothing is not one, and doc 40 § Relics requires compatibility results for "
                    + "all weapons, which needs a scope to compute against"));
        }

        SemanticCheck.Integer(
            dto.SaleValueCommonOre, root.AppendProperty("sale_value_common_ore"), context, id, bag,
            "sale_value_common_ore");
        SemanticCheck.AtLeast(
            dto.SaleValueCommonOre, 0, root.AppendProperty("sale_value_common_ore"), context, id,
            bag,
            "sale_value_common_ore is a nonnegative amount of common ore; the economy report sums "
                + "the column across the catalog, so a relic that sells for nothing records zero");

        ValidateKey(
            dto.TransformationKey, "transformation_key", LocalizationRole.Transformation,
            context, id, bag);
        ValidateKey(dto.TradeoffKey, "tradeoff_key", LocalizationRole.Tradeoff, context, id, bag);
        ValidateUnlock(dto, context, id, bag);
        ValidateOverrides(dto, context, id, bag);
    }

    private static void ValidateKey(
        string? value,
        string field,
        LocalizationRole role,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        if (value is null)
        {
            return;
        }

        if (LocalizationKey.TryParse(value, out LocalizationKey? key) && key!.Role == role)
        {
            return;
        }

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.LocalizationKeyMalformed,
            context.SourcePath,
            JsonPointer.Root.AppendProperty(field),
            id,
            "'" + field + "' is a localization key of the form <category>.<stable_id>."
                + LocalizationKey.ToToken(role) + ", never the sentence itself. Doc 40 § Relics "
                + "requires the tradeoff to be explicit, which the required field satisfies; "
                + "doc 40 § Localization contract keeps the words out of the definition"));
    }

    private static void ValidateUnlock(
        RelicDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        JsonPointer pointer = JsonPointer.Root.AppendProperty("unlock_id");
        bool unlocked = string.Equals(
            dto.PoolAvailability, "hyper-gold-unlock", StringComparison.Ordinal);

        if (unlocked && dto.UnlockId is null)
        {
            SemanticCheck.RequiredBy(
                pointer, context, id, bag,
                "a relic behind an unlock names the unlock that adds it to the cache pool");
        }

        if (!unlocked && dto.UnlockId is not null)
        {
            SemanticCheck.ForbiddenBy(
                pointer, context, id, bag,
                "a fresh-profile relic is in the pool already and names no unlock; the pool token "
                    + "and the unlock reference would otherwise be two answers to one question");
        }

        if (dto.UnlockId is not null)
        {
            SemanticCheck.ReferenceGrammar(
                dto.UnlockId, ContentCategory.Unlock, pointer, context, id, bag);
        }
    }

    private static void ValidateOverrides(
        RelicDto dto,
        CategoryReadContext context,
        string? id,
        DiagnosticBag bag)
    {
        List<string> overrides = dto.OverridesOrReplaces ?? new();
        JsonPointer pointer = JsonPointer.Root.AppendProperty("overrides_or_replaces");

        for (int index = 0; index < overrides.Count; index++)
        {
            JsonPointer element = pointer.AppendIndex(index);
            SemanticCheck.ReferenceGrammar(
                overrides[index], ContentCategory.Relic, element, context, id, bag);

            if (!string.Equals(overrides[index], id, StringComparison.Ordinal))
            {
                continue;
            }

            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.DuplicateValueInDefinition,
                context.SourcePath,
                element,
                id,
                "a relic does not override itself; only one relic is active at a time, so a "
                    + "self-reference would describe a replacement that never happens"));
        }

        SemanticCheck.Distinct(
            overrides, pointer, context, id, bag, "the relics this one overrides");
    }
}
