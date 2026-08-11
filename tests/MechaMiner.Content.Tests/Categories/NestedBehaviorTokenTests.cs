using System.Collections.Generic;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// The three behavior-token positions that are not a root <c>behavior_kind</c> are each
/// reached, and each reports the malformed token at its own pointer.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ContentDiagnosticCodes.BehaviorTokenMalformed"/> was already provoked by
/// two fixtures, both of them a root <c>behavior_kind</c>. That is enough to prove the
/// code exists and enough to keep the declared-code gate green, and it is not enough to
/// prove the grammar is applied anywhere else: <c>enemy.specialist_attack.kind</c>,
/// <c>mech.inherent_trait.behavior_kind</c> and
/// <c>resource.resonance_behavior.behavior_kind</c> each have their own call site, and
/// deleting any one of the three left the whole suite green before this fixture existed.
/// </para>
/// <para>
/// <b>Why the pointer and not only the code.</b> Asserting the code alone would be
/// satisfied by a document that fails at the root position it already had, which is the
/// exact confusion these three fixtures exist to remove - each one carries a well-formed
/// token everywhere except the nested field. So the assertion is that the malformed
/// token is reported <em>at the nested pointer</em>, and, for the enemy, that the root
/// <c>behavior_kind</c> in the same document is not the thing being reported.
/// </para>
/// <para>
/// <b>Two of the three are optional fields.</b> <c>MechSchema.InherentTrait</c> declares
/// <c>behavior_kind</c> as <c>OptionalText</c>, and
/// <c>content/schemas/resource.schema.json</c> says of its <c>behavior_kind</c> that it
/// "becomes required in the change that mints the vocabulary". Neither is required here,
/// so what is covered is the malformed-token case and not a missing-field case; the
/// clean-when-absent half is asserted below so that "optional" keeps meaning optional.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-002-034</c>, <c>VER-DAT-002-035</c>,
/// <c>VER-DAT-002-036</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class NestedBehaviorTokenTests
{
    /// <summary>Each nested token position, with the fixture that malforms only it.</summary>
    private static IEnumerable<TestCaseData> Positions => new[]
    {
        new TestCaseData(
                "enemies/invalid-specialist-attack-kind-prose.json",
                DefinitionKind.Enemy,
                "/specialist_attack/kind")
            .SetName("EnemySpecialistAttackKind"),
        new TestCaseData(
                "mechs/invalid-trait-behavior-kind-prose.json",
                DefinitionKind.Mech,
                "/inherent_trait/behavior_kind")
            .SetName("MechInherentTraitBehaviorKind"),
        new TestCaseData(
                "resources/invalid-resonance-behavior-kind-prose.json",
                DefinitionKind.Resource,
                "/resonance_behavior/behavior_kind")
            .SetName("ResourceResonanceBehaviorBehaviorKind"),
    };

    /// <summary>Each valid fixture whose nested token position is absent.</summary>
    /// <remarks>
    /// The over-strictness control for the two optional positions, plus the enemy's own
    /// well-formed <c>specialist_attack.kind</c>. Without it, a call site that reported
    /// every value including no value would satisfy the first check.
    /// </remarks>
    private static IEnumerable<TestCaseData> CleanSiblings => new[]
    {
        new TestCaseData("enemies/valid-enemy.json", DefinitionKind.Enemy)
            .SetName("EnemyWithAWellFormedSpecialistAttackKind"),
        new TestCaseData("mechs/valid-mech.json", DefinitionKind.Mech)
            .SetName("MechWithNoTraitBehaviorKind"),
        new TestCaseData("resources/valid-specialized-material.json", DefinitionKind.Resource)
            .SetName("ResourceWithNoResonanceBehaviorKind"),
    };

    [TestCaseSource(nameof(Positions))]
    public void AProseValueInANestedTokenPositionIsReportedAtThatPosition(
        string path, DefinitionKind kind, string pointer)
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(path, kind);

        List<string> atPointer = new();
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Location.Value == pointer)
            {
                atPointer.Add(diagnostic.Code);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                diagnostics,
                Is.Not.Empty,
                path + " must be rejected, or this check proves nothing about " + pointer);
            Assert.That(
                atPointer,
                Does.Contain(ContentDiagnosticCodes.BehaviorTokenMalformed),
                () => path + " must report " + ContentDiagnosticCodes.BehaviorTokenMalformed
                    + " at " + pointer + " specifically; a malformed token reported at some "
                    + "other pointer would leave this call site unproven. Produced: "
                    + Describe(diagnostics));
            Assert.That(
                Codes(diagnostics),
                Has.Exactly(1).EqualTo(ContentDiagnosticCodes.BehaviorTokenMalformed),
                () => path + " malforms exactly one token, so exactly one grammar failure may "
                    + "be reported; more than one means the fixture is not isolating "
                    + pointer + ". Produced: " + Describe(diagnostics));
        });
    }

    [TestCaseSource(nameof(CleanSiblings))]
    public void ANestedTokenPositionThatIsWellFormedOrAbsentReportsNothing(
        string path, DefinitionKind kind)
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(path, kind);

        Assert.That(
            diagnostics,
            Is.Empty,
            () => path + " carries no malformed nested token, so nothing may be reported "
                + "against it: " + Describe(diagnostics));
    }

    /// <summary>
    /// The pointers this fixture asserts are the pointers the validators build, rather
    /// than three string literals that happen to look right.
    /// </summary>
    /// <remarks>
    /// A literal typo would make the first check fail loudly, so this is not a second
    /// copy of the same assertion: it is what makes the expected values readable as the
    /// route each diagnostic takes - root, then the object, then the field - instead of
    /// as pasted output.
    /// </remarks>
    [Test]
    public void TheExpectedPointersAreTheOnesTheValidatorsBuild()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                JsonPointer.Root.AppendProperty("specialist_attack").AppendProperty("kind").Value,
                Is.EqualTo("/specialist_attack/kind"));
            Assert.That(
                JsonPointer.Root.AppendProperty("inherent_trait")
                    .AppendProperty("behavior_kind").Value,
                Is.EqualTo("/inherent_trait/behavior_kind"));
            Assert.That(
                JsonPointer.Root.AppendProperty("resonance_behavior")
                    .AppendProperty("behavior_kind").Value,
                Is.EqualTo("/resonance_behavior/behavior_kind"));
        });
    }

    private static IReadOnlyList<ContentDiagnostic> Read(string path, DefinitionKind kind)
    {
        return CategorySchemas.Read(
            CategoryFixtureCorpus.Read(path),
            new CategoryReadContext(CategoryFixtureCorpus.SourcePathOf(path), kind)).Diagnostics;
    }

    private static IReadOnlyList<string> Codes(IReadOnlyList<ContentDiagnostic> diagnostics)
    {
        List<string> codes = new(diagnostics.Count);
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            codes.Add(diagnostic.Code);
        }

        return codes;
    }

    private static string Describe(IReadOnlyList<ContentDiagnostic> diagnostics)
    {
        return string.Join("; ", diagnostics);
    }
}
