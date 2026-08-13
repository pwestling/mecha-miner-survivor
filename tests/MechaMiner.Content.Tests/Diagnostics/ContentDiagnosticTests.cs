using System;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Diagnostics;

/// <summary>
/// The five elements doc 40 requires, and the owner/expiration a warning cannot exist
/// without. Verification: <c>VER-DAT-001-011</c>.
/// </summary>
[TestFixture]
internal sealed class ContentDiagnosticTests
{
    [Test]
    public void AnErrorCarriesAllFiveRequiredElementsAsFields()
    {
        ContentDiagnostic diagnostic = ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.RetiredIdReused,
            "content/weapons/W-EF.json",
            JsonPointer.Root.AppendProperty("id"),
            "W-EF",
            "'W-EF' was retired and is never reassigned",
            new[] { "W-AB" });

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual("MMC-3002", diagnostic.Code, "stable diagnostic code");
            NumericAssert.AreExactlyEqual(
                "content/weapons/W-EF.json", diagnostic.SourcePath, "exact source path");
            NumericAssert.AreExactlyEqual("/id", diagnostic.Location.Value, "exact source field");
            NumericAssert.AreExactlyEqual("W-EF", diagnostic.ContentId!, "content ID");
            Assert.That(diagnostic.ExpectedConstraint, Is.Not.Empty, "expected constraint");
            Assert.That(diagnostic.RelatedIds, Is.EqualTo(new[] { "W-AB" }), "related IDs");
            Assert.That(diagnostic.Severity, Is.EqualTo(ContentDiagnosticSeverity.Error));
            Assert.That(diagnostic.Warning, Is.Null, "an error carries no warning policy");
        });
    }

    [Test]
    public void AWarningCannotBeCreatedWithoutAnOwnerAndAnExpiration()
    {
        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentException>(
                () => new WarningPolicy(" ", new DateOnly(2026, 12, 31), "why"));
            Expect.Throws<ArgumentException>(
                () => new WarningPolicy("DAT-002", new DateOnly(2026, 12, 31), " "));
        });
    }

    [Test]
    public void AWarningCarriesItsOwnerAndExpiration()
    {
        WarningPolicy policy = new(
            "DAT-002",
            new DateOnly(2026, 12, 31),
            "the catalog remediation pass lands with DAT-002");

        ContentDiagnostic diagnostic = ContentDiagnostic.CreateWarning(
            ContentDiagnosticCodes.TagOutsideVocabulary,
            "content/weapons/W-AB.json",
            JsonPointer.Root.AppendProperty("tags").AppendIndex(0),
            "W-AB",
            "a tag is in the closed vocabulary",
            policy);

        Expect.Multiple(() =>
        {
            Assert.That(diagnostic.Severity, Is.EqualTo(ContentDiagnosticSeverity.Warning));
            NumericAssert.AreExactlyEqual("DAT-002", diagnostic.Warning!.Owner, "warning owner");
            Assert.That(diagnostic.Warning.IsExpired(new DateOnly(2026, 12, 31)), Is.False);
            Assert.That(diagnostic.Warning.IsExpired(new DateOnly(2027, 1, 1)), Is.True);
        });
    }

    [Test]
    public void AnUndeclaredCodeCannotBecomeADiagnostic()
    {
        // The registry is only useful if nothing can bypass it.
        Expect.Throws<ArgumentException>(() => ContentDiagnostic.CreateError(
            "MMC-9999", "a.json", JsonPointer.Root, null, "something"));
    }

    [Test]
    public void ADiagnosticRequiresASourcePathAndAnExpectedConstraint()
    {
        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentException>(() => ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.UnknownField, " ", JsonPointer.Root, null, "x"));
            Expect.Throws<ArgumentException>(() => ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.UnknownField, "a.json", JsonPointer.Root, null, " "));
        });
    }

    [Test]
    public void RelatedIdsCannotBeMutatedThroughTheDiagnostic()
    {
        string[] related = { "W-AB" };
        ContentDiagnostic diagnostic = ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.RetiredIdReused, "a.json", JsonPointer.Root, "W-EF", "x", related);

        related[0] = "MUTATED";

        NumericAssert.AreExactlyEqual(
            "W-AB", diagnostic.RelatedIds[0], "the diagnostic copies its related IDs");
    }

    [Test]
    public void TheRenderedFormShowsEveryRequiredElement()
    {
        string text = ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.UnknownField,
            "content/weapons/W-AB.json",
            JsonPointer.Root.AppendProperty("behavior_kind"),
            "W-AB",
            "the envelope declares exactly nine fields",
            new[] { "W-CD" }).ToString();

        Expect.Multiple(() =>
        {
            Assert.That(text, Does.Contain("MMC-2001"));
            Assert.That(text, Does.Contain("content/weapons/W-AB.json"));
            Assert.That(text, Does.Contain("/behavior_kind"));
            Assert.That(text, Does.Contain("W-AB"));
            Assert.That(text, Does.Contain("nine fields"));
            Assert.That(text, Does.Contain("W-CD"));
        });
    }

    [Test]
    public void ABagReportsErrorsAndDistinctCodes()
    {
        DiagnosticBag bag = new();
        Assert.That(bag.IsEmpty, Is.True);

        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.UnknownField, "a.json", JsonPointer.Root, null, "x"));
        bag.Add(ContentDiagnostic.CreateError(
            ContentDiagnosticCodes.UnknownField, "a.json", JsonPointer.Root, null, "y"));

        Expect.Multiple(() =>
        {
            Assert.That(bag.HasErrors, Is.True);
            Assert.That(bag.Codes(), Is.EqualTo(new[] { ContentDiagnosticCodes.UnknownField }));
        });
    }
}
