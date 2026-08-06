using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// A semantic check whose operand the field table has already reported stays silent,
/// and the same check on a document that carries its operands still fires.
/// </summary>
/// <remarks>
/// <para>
/// The two directions are one rule and neither half means anything alone. Suppression
/// on its own is indistinguishable from deleting the check; firing on its own is what
/// the compiler already did, including on operands nobody authored. Every case below
/// therefore appears in both tables, built from the same valid fixture by two different
/// mutations: one that removes the operand, and one that leaves every operand present
/// and breaks the rule.
/// </para>
/// <para>
/// The suppression case also asserts that the structural diagnostic is still there. A
/// suppression that swallowed both would leave the document silently accepted at that
/// pointer, which is worse than the double report it replaces.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-003-032</c>, <c>VER-DAT-003-033</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class DependentSemanticSuppressionTests
{
    /// <summary>
    /// One dependent check: which fixture, which operand, and how to break the rule
    /// without touching the operand.
    /// </summary>
    internal sealed class Case
    {
        internal Case(
            string name,
            string fixturePath,
            DefinitionKind kind,
            string operandPointer,
            string semanticCode,
            string semanticPointer,
            Func<FixtureDocument, FixtureDocument> removeOperand,
            Func<FixtureDocument, FixtureDocument> violate)
        {
            Name = name;
            FixturePath = fixturePath;
            Kind = kind;
            OperandPointer = operandPointer;
            SemanticCode = semanticCode;
            SemanticPointer = semanticPointer;
            RemoveOperand = removeOperand;
            Violate = violate;
        }

        /// <summary>What the check is, for the test name.</summary>
        internal string Name { get; }

        /// <summary>The valid fixture both variants are built from.</summary>
        internal string FixturePath { get; }

        /// <summary>The definition kind it is read as.</summary>
        internal DefinitionKind Kind { get; }

        /// <summary>The pointer the structural stage must report when the operand goes.</summary>
        internal string OperandPointer { get; }

        /// <summary>The code the dependent check emits.</summary>
        internal string SemanticCode { get; }

        /// <summary>Where the dependent check emits it.</summary>
        internal string SemanticPointer { get; }

        /// <summary>Removes the operand the check reads.</summary>
        internal Func<FixtureDocument, FixtureDocument> RemoveOperand { get; }

        /// <summary>Breaks the rule with every operand still present.</summary>
        internal Func<FixtureDocument, FixtureDocument> Violate { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    private static IEnumerable<Case> Cases => new[]
    {
        new Case(
            "a PowerUp's rank ladder length against its cap",
            "powerups/valid-powerup.json", DefinitionKind.PowerUp,
            "/cap",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/ranks",
            d => d.RemoveAt("/cap"),
            d => d.SetAt("/cap", JsonValue.Create(4))),

        new Case(
            "a PowerUp's rank ladder length when the ladder itself is gone",
            "powerups/valid-powerup.json", DefinitionKind.PowerUp,
            "/ranks",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/ranks",
            d => d.RemoveAt("/ranks"),
            d => d.RemoveAt("/ranks/4")),

        new Case(
            "a PowerUp's rank numbers running contiguously from one",
            "powerups/valid-powerup.json", DefinitionKind.PowerUp,
            "/ranks/1/rank",
            ContentDiagnosticCodes.SequenceNotContiguous, "/ranks/1",
            d => d.RemoveAt("/ranks/1/rank"),
            d => d.SetAt("/ranks/1/rank", JsonValue.Create(3))),

        new Case(
            "a utility's rank-price count against its rank_count",
            "utilities/valid-utility.json", DefinitionKind.Utility,
            "/acquisition/rank_count",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/acquisition/rank_ore_costs",
            d => d.RemoveAt("/acquisition/rank_count"),
            d => d.RemoveAt("/acquisition/rank_ore_costs/2")),

        // The operand here is rank_count and not /ranks: a utility's rank array is
        // optional, because the ore-only radar has none, so removing it is a conditional
        // fault rather than a structural one.
        new Case(
            "a utility's rank array length against its rank_count",
            "utilities/valid-utility.json", DefinitionKind.Utility,
            "/acquisition/rank_count",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/ranks",
            d => d.RemoveAt("/acquisition/rank_count"),
            d => d.RemoveAt("/ranks/3")),

        new Case(
            "a utility's ranks running contiguously from zero",
            "utilities/valid-utility.json", DefinitionKind.Utility,
            "/ranks/1/rank",
            ContentDiagnosticCodes.SequenceNotContiguous, "/ranks/1",
            d => d.RemoveFromEvery("/ranks", "rank"),
            d => d.SetAt("/ranks/1/rank", JsonValue.Create(2))),

        // The three ore-only cases are the radar's, and they are the clearest instance
        // of the whole defect: with ore_only_exception unreadable the discriminator
        // defaults to false and the compiler tells the ore-only radar, three times, that
        // it is not the ore-only radar.
        new Case(
            "the ore-only discriminator deciding whether a material is required",
            "utilities/valid-utility-radar.json", DefinitionKind.Utility,
            "/ore_only_exception",
            ContentDiagnosticCodes.ConditionalFieldMissing, "/material_id",
            d => d.RemoveAt("/ore_only_exception"),
            d => d.With("ore_only_exception", JsonValue.Create(false))),

        new Case(
            "the ore-only discriminator deciding whether a common-ore cost is allowed",
            "utilities/valid-utility-radar.json", DefinitionKind.Utility,
            "/ore_only_exception",
            ContentDiagnosticCodes.ConditionalFieldForbidden, "/acquisition/common_ore_cost",
            d => d.RemoveAt("/ore_only_exception"),
            d => d.With("ore_only_exception", JsonValue.Create(false))),

        new Case(
            "the ore-only discriminator deciding whether a rank ladder is required",
            "utilities/valid-utility-radar.json", DefinitionKind.Utility,
            "/ore_only_exception",
            ContentDiagnosticCodes.ConditionalFieldMissing, "/ranks",
            d => d.RemoveAt("/ore_only_exception"),
            d => d.With("ore_only_exception", JsonValue.Create(false))),

        new Case(
            "the pool-availability discriminator deciding whether an unlock is named",
            "utilities/valid-utility.json", DefinitionKind.Utility,
            "/availability/pool_availability",
            ContentDiagnosticCodes.ConditionalFieldForbidden, "/availability/unlock_id",
            d => d.RemoveAt("/availability/pool_availability"),
            d => d.SetAt(
                "/availability/pool_availability", JsonValue.Create("always-available"))),

        new Case(
            "a weapon's recipe pair naming two resources",
            "weapons/valid-weapon.json", DefinitionKind.Weapon,
            "/recipe_pair_material_ids",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/recipe_pair_material_ids",
            d => d.RemoveAt("/recipe_pair_material_ids"),
            d => d.RemoveAt("/recipe_pair_material_ids/1")),

        new Case(
            "a weapon declaring three ore-upgradeable stat tracks",
            "weapons/valid-weapon.json", DefinitionKind.Weapon,
            "/ore_upgradeable_stats",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/ore_upgradeable_stats",
            d => d.RemoveAt("/ore_upgradeable_stats"),
            d => d.RemoveAt("/ore_upgradeable_stats/2")),

        new Case(
            "a weapon's stat track slots running contiguously from one",
            "weapons/valid-weapon.json", DefinitionKind.Weapon,
            "/ore_upgradeable_stats/1/slot",
            ContentDiagnosticCodes.SequenceNotContiguous, "/ore_upgradeable_stats/1",
            d => d.RemoveAt("/ore_upgradeable_stats/1/slot"),
            d => d.SetAt("/ore_upgradeable_stats/1/slot", JsonValue.Create(3))),

        new Case(
            "a weapon having three branches",
            "weapons/valid-weapon.json", DefinitionKind.Weapon,
            "/branch_ids",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/branch_ids",
            d => d.RemoveAt("/branch_ids"),
            d => d.RemoveAt("/branch_ids/2")),

        new Case(
            "an unlock granting at least one thing",
            "unlocks/valid-unlock.json", DefinitionKind.Unlock,
            "/granted_ids",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/granted_ids",
            d => d.RemoveAt("/granted_ids"),
            d => d.With("granted_ids", new JsonArray())),

        new Case(
            "a branch ruling out the other two branches of its weapon",
            "branches/valid-branch.json", DefinitionKind.Branch,
            "/exclusivity/mutually_exclusive_with",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/exclusivity/mutually_exclusive_with",
            d => d.RemoveAt("/exclusivity/mutually_exclusive_with"),
            d => d.RemoveAt("/exclusivity/mutually_exclusive_with/1")),

        new Case(
            "the elite modifier order covering every layer",
            "enemies/valid-elite-modifiers.json", DefinitionKind.EliteModifiers,
            "/modifier_application_order",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/modifier_application_order",
            d => d.RemoveAt("/modifier_application_order"),
            d => d.RemoveAt("/modifier_application_order/3")),

        new Case(
            "the schedule defining every formation exactly once",
            "encounters/valid-schedule.json", DefinitionKind.EncounterSchedule,
            "/spawn_formations",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/spawn_formations",
            d => d.RemoveAt("/spawn_formations"),
            d => d.RemoveAt("/spawn_formations/4")),

        new Case(
            "the schedule's row count against its duration",
            "encounters/valid-schedule.json", DefinitionKind.EncounterSchedule,
            "/minute_rows",
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/minute_rows",
            d => d.RemoveAt("/minute_rows"),
            d => d.SetAt("/duration_minutes", JsonValue.Create(34))),

        new Case(
            "the schedule's minute numbers running contiguously from zero",
            "encounters/valid-schedule.json", DefinitionKind.EncounterSchedule,
            "/minute_rows/1/minute",
            ContentDiagnosticCodes.SequenceNotContiguous, "/minute_rows/1",
            d => d.RemoveFromEvery("/minute_rows", "minute"),
            d => d.SetAt("/minute_rows/1/minute", JsonValue.Create(2))),

        new Case(
            "a minute row's composition shares summing to a hundred",
            "encounters/valid-schedule.json", DefinitionKind.EncounterSchedule,
            "/minute_rows/0/composition/0/share_percent",
            ContentDiagnosticCodes.SumMismatch, "/minute_rows/0/composition",
            d => d.RemoveAt("/minute_rows/0/composition/0/share_percent"),
            d => d.SetAt("/minute_rows/0/composition/0/share_percent", JsonValue.Create(50))),
    };

    /// <summary>
    /// With the operand gone, the structural stage reports it and the dependent check
    /// says nothing. Both halves matter: the second alone would be satisfied by never
    /// reporting anything at that pointer at all.
    /// </summary>
    [TestCaseSource(nameof(Cases))]
    public void AMissingOperandProducesTheStructuralDiagnosticAndNotTheDependentOne(Case testCase)
    {
        IReadOnlyList<ContentDiagnostic> diagnostics =
            Read(testCase, testCase.RemoveOperand);

        Expect.Multiple(() =>
        {
            Assert.That(
                Structural(diagnostics, testCase.OperandPointer),
                Is.Not.Empty,
                () => "removing " + testCase.OperandPointer
                    + " must still be reported by the field table: " + Describe(diagnostics));

            Assert.That(
                At(diagnostics, testCase.SemanticCode, testCase.SemanticPointer),
                Is.Empty,
                () => testCase.SemanticCode + " at " + testCase.SemanticPointer
                    + " reads " + testCase.OperandPointer
                    + ", which is not there, so it is asserting against a defaulted value: "
                    + Describe(diagnostics));
        });
    }

    /// <summary>
    /// The control that keeps the suppression from being a deletion: with every operand
    /// present and the rule genuinely broken, the same check must still fire.
    /// </summary>
    [TestCaseSource(nameof(Cases))]
    public void TheCheckStillFiresWhenItsOperandsArePresent(Case testCase)
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(testCase, testCase.Violate);

        Expect.Multiple(() =>
        {
            Assert.That(
                Structural(diagnostics, testCase.OperandPointer),
                Is.Empty,
                () => "the violating variant must leave " + testCase.OperandPointer
                    + " present, or this control proves nothing: " + Describe(diagnostics));

            Assert.That(
                At(diagnostics, testCase.SemanticCode, testCase.SemanticPointer),
                Is.Not.Empty,
                () => testCase.SemanticCode + " at " + testCase.SemanticPointer
                    + " must still be reported when its operands are all present: "
                    + Describe(diagnostics));
        });
    }

    /// <summary>
    /// The set of cases is not a hand-kept list of names: every fixture it names is a
    /// fixture the corpus already declares valid, so a case cannot quietly be built on a
    /// document that was already failing for another reason.
    /// </summary>
    [Test]
    public void EveryCaseIsBuiltFromAFixtureTheCorpusDeclaresValid()
    {
        HashSet<string> valid = new(StringComparer.Ordinal);
        foreach (CategoryFixture fixture in CategoryFixtureCorpus.Valid)
        {
            valid.Add(fixture.Path);
        }

        List<string> unfounded = new();
        foreach (Case testCase in Cases)
        {
            if (!valid.Contains(testCase.FixturePath))
            {
                unfounded.Add(testCase.Name + " -> " + testCase.FixturePath);
            }
        }

        Assert.That(
            unfounded,
            Is.Empty,
            () => "cases built on a document the corpus does not hold valid: "
                + string.Join("; ", unfounded));
    }

    /// <summary>
    /// <see cref="StructuralReport"/> counts a mistyped field as well as a missing one,
    /// and this records why only the missing half can be provoked today: a kind mismatch
    /// makes the shape unsound, and an unsound shape stops the read before any semantic
    /// check runs. The mistyped half of the rule is therefore a statement of the rule
    /// rather than a live branch, and this test is what will notice if that ordering
    /// changes and the branch starts carrying weight.
    /// </summary>
    [Test]
    public void AMistypedFieldStopsTheReadBeforeAnySemanticCheckRuns()
    {
        FixtureDocument document = FixtureDocument
            .Load("powerups/valid-powerup.json")
            .SetAt("/cap", JsonValue.Create("five"));

        CategoryReadContext context = new(
            "tests/generated/dependent-semantic-mistyped.json", DefinitionKind.PowerUp);
        IReadOnlyList<ContentDiagnostic> diagnostics =
            CategorySchemas.Read(document.ToUtf8(), context).Diagnostics;

        List<string> semantic = new();
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Code.StartsWith("MMC-6", StringComparison.Ordinal))
            {
                semantic.Add(diagnostic.ToString());
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                Structural(diagnostics, "/cap"),
                Is.Not.Empty,
                () => "a mistyped cap must be reported: " + Describe(diagnostics));
            Assert.That(
                semantic,
                Is.Empty,
                () => "the semantic stage must not run past an unsound shape: "
                    + string.Join("; ", semantic));
        });
    }

    private static IReadOnlyList<ContentDiagnostic> Read(
        Case testCase,
        Func<FixtureDocument, FixtureDocument> mutate)
    {
        FixtureDocument document = mutate(FixtureDocument.Load(testCase.FixturePath));
        CategoryReadContext context = new(
            "tests/generated/dependent-semantic.json", testCase.Kind);
        return CategorySchemas.Read(document.ToUtf8(), context).Diagnostics;
    }

    private static IReadOnlyList<ContentDiagnostic> Structural(
        IReadOnlyList<ContentDiagnostic> diagnostics,
        string pointer)
    {
        List<ContentDiagnostic> matched = new();
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Location.Value == pointer
                && diagnostic.Code is ContentDiagnosticCodes.RequiredFieldMissing
                    or ContentDiagnosticCodes.FieldTypeMismatch)
            {
                matched.Add(diagnostic);
            }
        }

        return matched;
    }

    private static IReadOnlyList<ContentDiagnostic> At(
        IReadOnlyList<ContentDiagnostic> diagnostics,
        string code,
        string pointer)
    {
        List<ContentDiagnostic> matched = new();
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Code == code && diagnostic.Location.Value == pointer)
            {
                matched.Add(diagnostic);
            }
        }

        return matched;
    }

    private static string Describe(IReadOnlyList<ContentDiagnostic> diagnostics)
    {
        return diagnostics.Count == 0
            ? "(no diagnostics)"
            : string.Join("; ", diagnostics);
    }
}
