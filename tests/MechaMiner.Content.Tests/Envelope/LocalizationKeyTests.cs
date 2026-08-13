using MechaMiner.Content.Envelope;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Envelope;

/// <summary>
/// Localization keys are keys, never literal text. Verification: <c>VER-DAT-001-024</c>.
/// </summary>
[TestFixture]
internal sealed class LocalizationKeyTests
{
    [TestCase("weapon.W-AB.name", "weapon", "W-AB", LocalizationRole.Name)]
    [TestCase("weapon.W-AB.summary", "weapon", "W-AB", LocalizationRole.Summary)]
    [TestCase("mining_site.standard-ore-seams.name", "mining_site", "standard-ore-seams", LocalizationRole.Name)]
    [TestCase("boss.BOSS-01.name", "boss", "BOSS-01", LocalizationRole.Name)]
    [TestCase("resource.A.name", "resource", "A", LocalizationRole.Name)]
    public void AWellFormedKeyParsesIntoItsThreeParts(
        string value, string category, string stableId, LocalizationRole role)
    {
        Assert.That(LocalizationKey.TryParse(value, out LocalizationKey? key), Is.True);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(category, key!.Category, "category part");
            NumericAssert.AreExactlyEqual(stableId, key.StableId, "stable ID part");
            Assert.That(key.Role, Is.EqualTo(role));
        });
    }

    /// <summary>
    /// The stable ID appears verbatim in its own case. Doc 40: "so <c>weapon.W-AB.name</c>
    /// and not <c>weapon.w_ab.name</c>: a localization key that transforms an ID is no
    /// longer traceable to it."
    /// </summary>
    [Test]
    public void TheStableIdPartKeepsItsOwnCase()
    {
        Assert.That(LocalizationKey.TryParse("weapon.W-AB.name", out LocalizationKey? key), Is.True);
        NumericAssert.AreExactlyEqual("W-AB", key!.StableId, "the ID is not lowercased");
    }

    /// <summary>
    /// There is no way to ask a string whether a human wrote it, so the check is
    /// structural. Every one of these is what literal text actually looks like.
    /// </summary>
    [TestCase("Fracture Lance")]
    [TestCase("Fracture Lance: a heavy bore")]
    [TestCase("weapon.W-AB")]
    [TestCase("weapon.W-AB.title")]
    [TestCase("Weapon.W-AB.name")]
    [TestCase("weapon..name")]
    [TestCase(".W-AB.name")]
    [TestCase("")]
    [TestCase("weapon.W-AB.name.extra")]
    public void LiteralTextAndMalformedKeysAreRejected(string value)
    {
        Assert.That(LocalizationKey.TryParse(value, out _), Is.False);
    }

    [Test]
    public void EachFieldDeclaresTheRoleItsKeyMustCarry()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                LocalizationKey.RoleForField(EnvelopeSchema.NameKey),
                Is.EqualTo(LocalizationRole.Name));
            Assert.That(
                LocalizationKey.RoleForField(EnvelopeSchema.SummaryKey),
                Is.EqualTo(LocalizationRole.Summary));
        });
    }

    /// <summary>
    /// The per-role patterns are what the schema mirrors, so they must agree with the
    /// combined pattern the parser uses.
    /// </summary>
    [TestCase("weapon.W-AB.name", LocalizationRole.Name, true)]
    [TestCase("weapon.W-AB.summary", LocalizationRole.Name, false)]
    [TestCase("weapon.W-AB.summary", LocalizationRole.Summary, true)]
    [TestCase("weapon.W-AB.name", LocalizationRole.Summary, false)]
    public void ThePerRolePatternMatchesOnlyItsOwnRole(
        string value, LocalizationRole role, bool expected)
    {
        Assert.That(
            System.Text.RegularExpressions.Regex.IsMatch(value, LocalizationKey.PatternFor(role)),
            Is.EqualTo(expected));
    }
}
