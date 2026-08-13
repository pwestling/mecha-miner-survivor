using System.Collections.Generic;
using System.Text.Json.Nodes;
using MechaMiner.Content.Categories;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Categories;

/// <summary>
/// Every diagnostic from a document whose <c>id</c> reads carries that ID, and no
/// diagnostic from a document whose <c>id</c> does not read carries an invented one.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Compilation pipeline makes
/// the content ID one of the five elements every diagnostic carries, and
/// <see cref="ContentDiagnostic.ContentId"/> documents null as meaning "the document is
/// too broken for its ID to be read". Those two together are a single assertion: the ID
/// is reported whenever it is readable, and the readability of the ID is a property of
/// the <c>id</c> field alone.
/// </para>
/// <para>
/// The pairing is what makes this a test rather than a preference. Reporting the ID
/// everywhere is trivially achieved by inventing one, so the second control - a
/// genuinely malformed ID must report nothing - is what forbids the cheap answer. The
/// first control uses an unknown root field precisely because it is the error that says
/// least about the ID: the ID parsed, passed, and is sitting in the file.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-003-028</c>, <c>VER-DAT-003-029</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class DiagnosticContentIdTests
{
    [Test]
    public void AnUnknownRootFieldDoesNotTakeTheIdOffTheOtherDiagnostics()
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(FixtureDocument
            .Load("powerups/valid-powerup.json")
            .With("movement_speed", JsonValue.Create(3.5)), DefinitionKind.PowerUp);

        Expect.Multiple(() =>
        {
            Assert.That(
                diagnostics,
                Is.Not.Empty,
                "an unknown root field must still be an error, or this control proves nothing");

            foreach (ContentDiagnostic diagnostic in diagnostics)
            {
                Assert.That(
                    diagnostic.ContentId,
                    Is.EqualTo("PU-C01"),
                    () => diagnostic.Code + " at " + diagnostic.Location.Value
                        + " lost an ID that is valid and present in the document: " + diagnostic);
            }
        });
    }

    /// <summary>
    /// A second error at the same stage must not take the ID away either. One unknown
    /// field could be survived by a fix that special-cased a single code; several
    /// unrelated envelope-stage faults at once cannot.
    /// </summary>
    [Test]
    public void SeveralUnrelatedFaultsAtOnceStillLeaveTheIdReadable()
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(FixtureDocument
            .Load("utilities/valid-utility.json")
            .With("movement_speed", JsonValue.Create(3.5))
            .With("tags", FixtureDocument.Strings(new[] { "NOT-A-TAG" }))
            .Without("acquisition"), DefinitionKind.Utility);

        Expect.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Not.Empty, "the variant must be rejected");
            foreach (ContentDiagnostic diagnostic in diagnostics)
            {
                Assert.That(
                    diagnostic.ContentId,
                    Is.EqualTo("UTL-D2"),
                    () => diagnostic.Code + " at " + diagnostic.Location.Value
                        + " lost a readable ID: " + diagnostic);
            }
        });
    }

    /// <summary>
    /// The control that keeps the first two from being satisfied by inventing an ID: a
    /// document whose <c>id</c> does not match its category's grammar has no readable
    /// ID, and every diagnostic from it must say so rather than echo the raw string.
    /// </summary>
    /// <remarks>
    /// <see cref="ContentDiagnosticCodes.IdMalformedForCategory"/> is exempt and is the
    /// only exemption. That diagnostic's subject <em>is</em> the raw string, so quoting
    /// it there is what lets an author find the value being rejected; quoting it on any
    /// other diagnostic would assert that the document has an ID it does not have. The
    /// exemption is named rather than skipped so that the rule stays one code wide.
    /// </remarks>
    [Test]
    public void AMalformedIdIsReportedAsNoIdRatherThanAsABogusOne()
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(FixtureDocument
            .Load("powerups/valid-powerup.json")
            .With("id", JsonValue.Create("powerup one"))
            .With("movement_speed", JsonValue.Create(3.5)), DefinitionKind.PowerUp);

        Expect.Multiple(() =>
        {
            Assert.That(
                Codes(diagnostics),
                Does.Contain(ContentDiagnosticCodes.IdMalformedForCategory),
                () => "the malformed ID must be reported: " + Describe(diagnostics));

            foreach (ContentDiagnostic diagnostic in diagnostics)
            {
                if (diagnostic.Code == ContentDiagnosticCodes.IdMalformedForCategory)
                {
                    continue;
                }

                Assert.That(
                    diagnostic.ContentId,
                    Is.Null,
                    () => diagnostic.Code + " at " + diagnostic.Location.Value
                        + " reported an ID the document does not have: " + diagnostic);
            }
        });
    }

    /// <summary>
    /// An absent <c>id</c> is the other way for the ID to be unreadable, and it must
    /// reach the same verdict as a malformed one.
    /// </summary>
    [Test]
    public void AnAbsentIdIsReportedAsNoId()
    {
        IReadOnlyList<ContentDiagnostic> diagnostics = Read(FixtureDocument
            .Load("powerups/valid-powerup.json")
            .Without("id"), DefinitionKind.PowerUp);

        Expect.Multiple(() =>
        {
            Assert.That(
                Codes(diagnostics),
                Does.Contain(ContentDiagnosticCodes.RequiredFieldMissing),
                () => "the absent ID must be reported: " + Describe(diagnostics));

            foreach (ContentDiagnostic diagnostic in diagnostics)
            {
                Assert.That(
                    diagnostic.ContentId,
                    Is.Null,
                    () => diagnostic.Code + " invented an ID for a document that has none: "
                        + diagnostic);
            }
        });
    }

    private static IReadOnlyList<ContentDiagnostic> Read(FixtureDocument document, DefinitionKind kind)
    {
        CategoryReadContext context = new("tests/generated/diagnostic-content-id.json", kind);
        return CategorySchemas.Read(document.ToUtf8(), context).Diagnostics;
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
