using System.Collections.Generic;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// One layer owns a root-level unknown field, and which layer it is depends on which
/// layer holds the document's field table.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EnvelopeReader"/> and <see cref="DefinitionShapeValidator"/> both walk the
/// root, and both used to report an unknown property there. Two diagnostics with the
/// same code and the same pointer are not two findings: a report grouped by
/// <c>(file, code, pointer)</c> counts one fault twice, and the count of what is left to
/// fix is overstated by exactly the number of unknown root fields in the catalog.
/// </para>
/// <para>
/// The envelope's wording was also the wrong one to keep. Handed the category's field
/// names so it does not reject them, it reported anything else in envelope vocabulary -
/// "the envelope declares exactly these fields: id, schema_version, ..." for
/// <c>/movement_speed</c> on a boss, which sends an author to look at the envelope for a
/// field the envelope has nothing to do with. The shape validator names the kind.
/// </para>
/// <para>
/// The second test is what keeps this from being "delete the check". An envelope read
/// with no category behind it - which is what an envelope fixture is - has no other
/// field table, so the envelope is still the authority there and must still report.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-003-030</c>, <c>VER-DAT-003-031</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RootUnknownFieldOwnershipTests
{
    [Test]
    public void AnUnknownRootFieldOnACategoryDocumentIsReportedOnceAndNamesTheKind()
    {
        IReadOnlyList<ContentDiagnostic> reported = At(
            "/movement_speed",
            Read(FixtureDocument
                .Load("bosses/valid-boss.json")
                .With("movement_speed", JsonValue.Create(3.5)), DefinitionKind.Boss));

        Expect.Multiple(() =>
        {
            Assert.That(
                reported,
                Has.Count.EqualTo(1),
                () => "an unknown root field is one fault: " + string.Join("; ", reported));

            foreach (ContentDiagnostic diagnostic in reported)
            {
                Assert.That(
                    diagnostic.Code,
                    Is.EqualTo(ContentDiagnosticCodes.UnknownField),
                    () => "the fault is an unknown field: " + diagnostic);
                Assert.That(
                    diagnostic.ExpectedConstraint,
                    Does.StartWith("a boss definition accepts"),
                    () => "the constraint must name the kind whose table rejected the field, "
                        + "not the envelope: " + diagnostic);
            }
        });
    }

    /// <summary>
    /// The rule holds for more than one unknown field and more than one kind, so it
    /// cannot be satisfied by a fix keyed to one document's shape.
    /// </summary>
    [Test]
    public void SeveralUnknownRootFieldsAreEachReportedExactlyOnce()
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(FixtureDocument
            .Load("utilities/valid-utility.json")
            .With("movement_speed", JsonValue.Create(3.5))
            .With("design_role", JsonValue.Create("a note"))
            .With("hit_rule", JsonValue.Create("a rule")), DefinitionKind.Utility);

        Expect.Multiple(() =>
        {
            foreach (string pointer in new[] { "/movement_speed", "/design_role", "/hit_rule" })
            {
                Assert.That(
                    At(pointer, diagnostics),
                    Has.Count.EqualTo(1),
                    () => pointer + " must be reported once: "
                        + string.Join("; ", At(pointer, diagnostics)));
            }
        });
    }

    /// <summary>
    /// The over-suppression control. Removing the envelope's report where a category
    /// field table exists must not remove it where one does not: an envelope read with
    /// no domain fields is the whole field table for the document it reads.
    /// </summary>
    [Test]
    public void AnEnvelopeReadWithNoCategoryTableStillReportsAnUnknownRootField()
    {
        EnvelopeReadContext context = new(
            "tests/generated/envelope-unknown-root-field.json", ContentCategory.PowerUp);

        byte[] utf8 = FixtureDocument
            .Load("powerups/valid-powerup.json")
            .ToUtf8();

        EnvelopeReadResult result = EnvelopeReader.Read(utf8, context);

        Expect.Multiple(() =>
        {
            Assert.That(
                At("/ui_grouping", result.Diagnostics),
                Has.Count.EqualTo(1),
                () => "with no category table behind it the envelope is the field table and "
                    + "must reject a field it does not declare: "
                    + string.Join("; ", result.Diagnostics));

            foreach (ContentDiagnostic diagnostic in At("/ui_grouping", result.Diagnostics))
            {
                Assert.That(
                    diagnostic.ExpectedConstraint,
                    Does.StartWith("the envelope declares"),
                    () => "here the envelope is the authority and says so: " + diagnostic);
            }
        });
    }

    /// <summary>
    /// A field the category declares is still not the envelope's to reject, and a nested
    /// unknown field is still reported. Both are the surrounding behaviour this change
    /// must leave alone.
    /// </summary>
    [Test]
    public void DeclaredAndNestedFieldsAreUnaffected()
    {
        IReadOnlyList<ContentDiagnostic> clean = Read(
            FixtureDocument.Load("powerups/valid-powerup.json"), DefinitionKind.PowerUp);

        IReadOnlyList<ContentDiagnostic> nested = Read(
            FixtureDocument
                .Load("powerups/valid-powerup.json")
                .WithIn("active_rank_policy", "movement_speed", JsonValue.Create(3.5)),
            DefinitionKind.PowerUp);

        Expect.Multiple(() =>
        {
            Assert.That(
                clean,
                Is.Empty,
                () => "a valid PowerUp declares ui_grouping and the envelope must not reject "
                    + "it: " + string.Join("; ", clean));
            Assert.That(
                At("/active_rank_policy/movement_speed", nested),
                Has.Count.EqualTo(1),
                () => "a nested unknown field is still one report: "
                    + string.Join("; ", nested));
        });
    }

    private static IReadOnlyList<ContentDiagnostic> Read(
        FixtureDocument document,
        DefinitionKind kind)
    {
        CategoryReadContext context = new(
            "tests/generated/root-unknown-field.json", kind);
        return CategorySchemas.Read(document.ToUtf8(), context).Diagnostics;
    }

    private static IReadOnlyList<ContentDiagnostic> At(
        string pointer,
        IReadOnlyList<ContentDiagnostic> diagnostics)
    {
        List<ContentDiagnostic> matched = new();
        foreach (ContentDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic.Code == ContentDiagnosticCodes.UnknownField
                && diagnostic.Location.Value == pointer)
            {
                matched.Add(diagnostic);
            }
        }

        return matched;
    }
}
