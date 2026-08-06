using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// What the five dependent semantic codes actually say, held as text.
/// </summary>
/// <remarks>
/// <para>
/// <b>The failure this exists to stop.</b> Swapping <c>expected</c> and <c>actual</c> in
/// the <c>MMC-6006</c> message - <c>SemanticCheck.ExactCount</c> - makes the compiler
/// report the reverse of the truth at the same code and the same pointer: a five-row
/// ladder under a cap of four is told it "holds exactly 5 elements; found 4". Every
/// assertion the suite had over that diagnostic is a count, a key, or a code, and all
/// three are preserved by the swap. The suite stayed green. An author reading it goes
/// and adds a row to a ladder that already has one too many.
/// </para>
/// <para>
/// This is not a property that can be checked structurally. There is no second copy of
/// the sentence to compare against and no oracle for "is this English true of that
/// document" - the only thing that distinguishes a correct message from its inverse is
/// somebody having read it. So the sentence is written down, and changing it is a
/// deliberate edit to a line a reviewer sees.
/// </para>
/// <para>
/// <b>Written out here rather than held as a golden file.</b> Both were available, and
/// the text is short enough that either would work. What decides it is where the change
/// shows up: a golden file moves the sentence out of the diff of the commit that alters
/// it and into a fixture whose regeneration is a one-line command, and the exact review
/// step this is here to force - a human reading the new sentence and deciding whether it
/// is true - is the one a regenerate-and-commit loop skips. Inline is also what
/// <see cref="RootUnknownFieldOwnershipTests"/> already does for <c>MMC-2001</c>.
/// </para>
/// <para>
/// <b>Both directions of every asymmetric pair.</b> <c>MMC-6006</c> appears three times
/// with expected above actual and expected below it, and <c>MMC-6007</c> carries an
/// ordinal and the index it was required to equal. A swap that happened to be invisible
/// in one row is visible in the next.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-003-038</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class DependentSemanticConstraintTextTests
{
    /// <summary>One provoked diagnostic and the sentence it must carry.</summary>
    internal sealed class Case
    {
        internal Case(
            string name,
            string fixturePath,
            DefinitionKind kind,
            Func<FixtureDocument, FixtureDocument> provoke,
            string code,
            string pointer,
            string constraint)
        {
            Name = name;
            FixturePath = fixturePath;
            Kind = kind;
            Provoke = provoke;
            Code = code;
            Pointer = pointer;
            Constraint = constraint;
        }

        /// <summary>What is being provoked, for the test name.</summary>
        internal string Name { get; }

        /// <summary>The valid fixture the variant is built from.</summary>
        internal string FixturePath { get; }

        /// <summary>The definition kind it is read as.</summary>
        internal DefinitionKind Kind { get; }

        /// <summary>Breaks exactly the rule under test.</summary>
        internal Func<FixtureDocument, FixtureDocument> Provoke { get; }

        /// <summary>The code the diagnostic must carry.</summary>
        internal string Code { get; }

        /// <summary>Where it must be reported.</summary>
        internal string Pointer { get; }

        /// <summary>The constraint text it must state, in full.</summary>
        internal string Constraint { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    private static IEnumerable<Case> Cases => new[]
    {
        // MMC-6006, expected below actual: a cap of four over a five-row ladder. This is
        // the row the expected/actual swap inverts most legibly.
        new Case(
            "MMC-6006 a rank ladder longer than its cap",
            "powerups/valid-powerup.json", DefinitionKind.PowerUp,
            d => d.SetAt("/cap", JsonValue.Create(4)),
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/ranks",
            "the rank ladder holds one row per purchasable rank, so its length equals the "
                + "cap. The two are checked against each other rather than both against a "
                + "constant, so a cap raised without adding a row fails and so does the "
                + "reverse holds exactly 4 elements; found 5"),

        // MMC-6006, expected above actual: the other direction of the same pair.
        new Case(
            "MMC-6006 a rank-price list shorter than its rank_count",
            "utilities/valid-utility.json", DefinitionKind.Utility,
            d => d.RemoveAt("/acquisition/rank_ore_costs/2"),
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/acquisition/rank_ore_costs",
            "rank_ore_costs holds one price per rank above Installed, so its length equals "
                + "rank_count; the two are checked against each other rather than both "
                + "against a constant holds exactly 3 elements; found 2"),

        // MMC-6006 against a constant rather than a sibling, so the swap is checked on
        // both shapes of the same check.
        new Case(
            "MMC-6006 a weapon with two branches",
            "weapons/valid-weapon.json", DefinitionKind.Weapon,
            d => d.RemoveAt("/branch_ids/2"),
            ContentDiagnosticCodes.ArrayCardinalityWrong, "/branch_ids",
            "a weapon has exactly three branches. That they are one amplification, one "
                + "functional, and one conversion is asserted from the branches catalog, "
                + "because branch_class lives on the branch holds exactly 3 elements; "
                + "found 2"),

        new Case(
            "MMC-6007 a rank ladder numbered out of sequence",
            "powerups/valid-powerup.json", DefinitionKind.PowerUp,
            d => d.SetAt("/ranks/1/rank", JsonValue.Create(3)),
            ContentDiagnosticCodes.SequenceNotContiguous, "/ranks/1",
            "a PowerUp's rank numbers runs as the contiguous integers from 1 in array "
                + "order; element 1 is 3 where 2 was required. The comparison is against "
                + "the array index, so a correct element count with a repeated or "
                + "reordered ordinal still fails"),

        new Case(
            "MMC-6007 a schedule numbered out of sequence from zero",
            "encounters/valid-schedule.json", DefinitionKind.EncounterSchedule,
            d => d.SetAt("/minute_rows/1/minute", JsonValue.Create(2)),
            ContentDiagnosticCodes.SequenceNotContiguous, "/minute_rows/1",
            "the schedule's minute numbers runs as the contiguous integers from 0 in array "
                + "order; element 1 is 2 where 1 was required. The comparison is against "
                + "the array index, so a correct element count with a repeated or "
                + "reordered ordinal still fails"),

        new Case(
            "MMC-6010 composition shares that do not sum to a hundred",
            "encounters/valid-schedule.json", DefinitionKind.EncounterSchedule,
            d => d.SetAt(
                "/minute_rows/0/composition/0/share_percent", JsonValue.Create(50)),
            ContentDiagnosticCodes.SumMismatch, "/minute_rows/0/composition",
            "a minute row's composition shares. The sum is over this row only and uses "
                + "integer arithmetic, because shares are authored as whole percentage "
                + "points and no tolerance is involved sums to 100; the parts sum to 90. "
                + "The total is recomputed from the parts rather than compared against an "
                + "authored copy of itself, which would prove nothing"),

        new Case(
            "MMC-6004 a material utility with no material",
            "utilities/valid-utility-radar.json", DefinitionKind.Utility,
            d => d.With("ore_only_exception", JsonValue.Create(false)),
            ContentDiagnosticCodes.ConditionalFieldMissing, "/material_id",
            "a utility that is not the ore-only exception is assigned to a material; doc 40 "
                + "§ Utilities makes the two alternatives, so a utility with neither is "
                + "neither kind"),

        new Case(
            "MMC-6004 a material utility with no rank ladder",
            "utilities/valid-utility-radar.json", DefinitionKind.Utility,
            d => d.With("ore_only_exception", JsonValue.Create(false)),
            ContentDiagnosticCodes.ConditionalFieldMissing, "/ranks",
            "a material utility has a rank ladder: Installed plus its rank_count ranks"),

        new Case(
            "MMC-6005 a material utility bought with common ore",
            "utilities/valid-utility-radar.json", DefinitionKind.Utility,
            d => d.With("ore_only_exception", JsonValue.Create(false)),
            ContentDiagnosticCodes.ConditionalFieldForbidden, "/acquisition/common_ore_cost",
            "only the ore-only radar is bought with common ore; a material utility's "
                + "fabrication cost is its material_unit_cost"),
    };

    /// <summary>
    /// The provoked diagnostic is reported exactly once at the stated pointer and states
    /// exactly the stated constraint.
    /// </summary>
    [TestCaseSource(nameof(Cases))]
    public void TheDiagnosticStatesTheConstraintItIsWrittenToState(Case testCase)
    {
        CategoryReadContext context = new(
            "tests/generated/dependent-semantic-constraint.json", testCase.Kind);
        IReadOnlyList<ContentDiagnostic> diagnostics = CategorySchemas
            .Read(testCase.Provoke(FixtureDocument.Load(testCase.FixturePath)).ToUtf8(), context)
            .Diagnostics;

        List<ContentDiagnostic> matched = new();
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Code == testCase.Code
                && diagnostic.Location.Value == testCase.Pointer)
            {
                matched.Add(diagnostic);
            }
        }

        Assert.That(
            matched,
            Has.Count.EqualTo(1),
            () => testCase.Code + " at " + testCase.Pointer + " must be reported exactly "
                + "once: " + Describe(diagnostics));

        Assert.That(
            matched[0].ExpectedConstraint,
            Is.EqualTo(testCase.Constraint),
            () => "the sentence " + testCase.Code + " states at " + testCase.Pointer
                + " changed. A message can be rewritten - but read the new one first and "
                + "check it is true of the document, because an expected/actual swap "
                + "preserves the code, the pointer, and every value in the sentence while "
                + "reversing what it claims.");
    }

    /// <summary>
    /// The five dependent codes are all represented, so a code cannot lose its only row
    /// and leave the fixture looking complete.
    /// </summary>
    /// <remarks>
    /// The five are the codes <c>StructuralReport</c> suppresses, and they are the ones
    /// whose text nothing else pins: four of the five appear zero times across the
    /// accepted catalog, so a message that reversed itself would not show up in any real
    /// output either.
    /// </remarks>
    [Test]
    public void EveryDependentSemanticCodeHasItsTextPinned()
    {
        string[] theFiveDependentCodes =
        {
            ContentDiagnosticCodes.ConditionalFieldMissing,
            ContentDiagnosticCodes.ConditionalFieldForbidden,
            ContentDiagnosticCodes.ArrayCardinalityWrong,
            ContentDiagnosticCodes.SequenceNotContiguous,
            ContentDiagnosticCodes.SumMismatch,
        };

        HashSet<string> covered = new(StringComparer.Ordinal);
        foreach (Case testCase in Cases)
        {
            covered.Add(testCase.Code);
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                covered,
                Is.EquivalentTo(theFiveDependentCodes),
                "the codes with pinned text are not the five dependent codes");
            Assert.That(
                theFiveDependentCodes,
                Has.Length.EqualTo(5),
                "StructuralReport suppresses five dependent codes; a code leaves this list "
                    + "when the check that emits it is retired, not when a row is tidied "
                    + "away");
        });
    }

    /// <summary>
    /// Every case is built on a document the corpus already declares valid, so a pinned
    /// sentence cannot be the output of a fixture that was failing for another reason.
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

    private static string Describe(IReadOnlyList<ContentDiagnostic> diagnostics)
    {
        return diagnostics.Count == 0
            ? "(no diagnostics)"
            : string.Join("; ", diagnostics);
    }
}
