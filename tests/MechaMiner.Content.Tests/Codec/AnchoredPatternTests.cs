using System.Text.RegularExpressions;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Codec;

/// <summary>
/// ECMA-262 anchor semantics for patterns shared with JSON Schema.
/// </summary>
/// <remarks>
/// <para>
/// This exists because of a real defect. .NET's <c>$</c> also matches immediately before
/// a trailing newline; ECMA-262's does not. Under the original code
/// <c>ContentId.TryCreate("W-AB\n", Weapon)</c> succeeded, so a stable ID could carry a
/// trailing newline past the typed validator while every JSON Schema tool rejected it -
/// which would also have shown up as a phantom schema/typed disagreement.
/// </para>
/// <para>
/// Verification: supports <c>VER-DAT-001-014</c>, <c>VER-DAT-001-018</c>,
/// <c>VER-DAT-001-021</c>, and <c>VER-DAT-001-023</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class AnchoredPatternTests
{
    [Test]
    public void TheDotNetDefaultIsTheBehaviourThisTypeExistsToCorrect()
    {
        // Documents the platform behaviour being corrected, so a future reader does not
        // "simplify" AnchoredPattern away.
        Assert.That(
            Regex.IsMatch("W-AB\n", "^W-[A-F]{2}$"),
            Is.True,
            ".NET's $ matches before a trailing newline, which ECMA-262's does not");
    }

    [TestCase("W-AB")]
    public void AValueWithoutATrailingNewlineStillMatches(string value)
    {
        Assert.That(AnchoredPattern.Compile("^W-[A-F]{2}$").IsMatch(value), Is.True);
    }

    [TestCase("W-AB\n")]
    [TestCase("W-AB\r\n")]
    [TestCase("W-AB\n ")]
    public void ATrailingNewlineNoLongerSlipsPastTheAnchor(string value)
    {
        Assert.That(AnchoredPattern.Compile("^W-[A-F]{2}$").IsMatch(value), Is.False);
    }

    [Test]
    public void EveryProjectPatternRejectsATrailingNewline()
    {
        Expect.Multiple(() =>
        {
            Assert.That(ContentId.TryCreate("W-AB\n", ContentCategory.Weapon, out _), Is.False);
            Assert.That(ContentId.TryCreate("MCH-01\n", ContentCategory.Mech, out _), Is.False);
            Assert.That(LocalizationKey.TryParse("weapon.W-AB.name\n", out _), Is.False);
            Assert.That(
                SourceRefGrammar.MatchesElementPattern("GDD-MINING\n"),
                Is.False);
        });
    }

    [TestCase("^a$", "^a\\z")]
    [TestCase("^a$|^b$", "^a\\z|^b\\z")]
    [TestCase("^a\\$b$", "^a\\$b\\z")]
    [TestCase("^a", "^a")]
    [TestCase("", "")]
    public void TranslationRewritesOnlyUnescapedDollars(string ecma, string expected)
    {
        NumericAssert.AreExactlyEqual(expected, AnchoredPattern.Translate(ecma), "translated pattern");
    }

    [Test]
    public void AnEscapedDollarStillMatchesALiteralDollar()
    {
        Assert.That(AnchoredPattern.Compile("^a\\$$").IsMatch("a$"), Is.True);
    }
}
