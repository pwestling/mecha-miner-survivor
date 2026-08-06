using System.Collections.Generic;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Ids;

/// <summary>
/// Per-category stable ID grammars. Verification: <c>VER-DAT-001-014</c>.
/// </summary>
[TestFixture]
internal sealed class ContentIdTests
{
    /// <summary>
    /// Real IDs from the accepted catalog, one or more per category. These are the exact
    /// tokens doc 40 § Stable ID policy says to "reuse ... exactly".
    /// </summary>
    private static readonly (string Id, ContentCategory Category)[] AcceptedIds =
    {
        ("RSC-01", ContentCategory.Resource),
        ("RSC-08", ContentCategory.Resource),
        ("MCH-01", ContentCategory.Mech),
        ("MCH-06", ContentCategory.Mech),
        ("EN-01", ContentCategory.Enemy),
        ("EN-10", ContentCategory.Enemy),
        ("ELT-01", ContentCategory.Enemy),
        ("BOSS-01", ContentCategory.Boss),
        ("BOSS-04", ContentCategory.Boss),
        ("W-AB", ContentCategory.Weapon),
        ("W-EF", ContentCategory.Weapon),
        ("W-AC-interdiction-payload", ContentCategory.Branch),
        ("W-EF-spiral-barrage", ContentCategory.Branch),
        ("UTL-A1", ContentCategory.Utility),
        ("UTL-F2", ContentCategory.Utility),
        ("UTL-R1", ContentCategory.Utility),
        ("REL-01", ContentCategory.Relic),
        ("REL-10", ContentCategory.Relic),
        ("PU-C01", ContentCategory.PowerUp),
        ("PU-S04", ContentCategory.PowerUp),
        ("UNL-01", ContentCategory.Unlock),
        ("SITE-01", ContentCategory.MiningSite),
        ("WAV-01", ContentCategory.Encounter),
        ("MGC-01", ContentCategory.Map),
        ("PLAYER-01", ContentCategory.Player),
    };

    private static IEnumerable<(string Id, ContentCategory Category)> Accepted => AcceptedIds;

    [TestCaseSource(nameof(Accepted))]
    public void AnAcceptedCatalogIdMatchesItsOwnCategory((string Id, ContentCategory Category) entry)
    {
        Assert.That(
            ContentId.TryCreate(entry.Id, entry.Category, out ContentId? id),
            Is.True,
            () => entry.Id + " must be valid for " + entry.Category + ": "
                + ContentCategories.Describe(entry.Category).DescribeAcceptedGrammar());
        NumericAssert.AreExactlyEqual(entry.Id, id!.Value, "the ID is preserved verbatim");
    }

    /// <summary>
    /// An ID of one category must not be silently accepted as another. The one deliberate
    /// exception is a branch ID, which begins with its parent weapon's ID by design; the
    /// grammars still separate them because a weapon ID has no suffix.
    /// </summary>
    [TestCase("EN-01", ContentCategory.Boss)]
    [TestCase("MCH-01", ContentCategory.Enemy)]
    [TestCase("W-AB", ContentCategory.Branch)]
    [TestCase("W-AC-interdiction-payload", ContentCategory.Weapon)]
    [TestCase("REL-01", ContentCategory.Unlock)]
    [TestCase("WAV-01", ContentCategory.Map)]
    [TestCase("SITE-01", ContentCategory.Encounter)]
    public void AnIdOfAnotherCategoryIsRejected(string id, ContentCategory category)
    {
        Assert.That(ContentId.TryCreate(id, category, out _), Is.False);
    }

    /// <summary>Doc 40 § Stable ID policy: IDs are case-sensitive ASCII and never localized.</summary>
    [TestCase("w-ab", ContentCategory.Weapon)]
    [TestCase("mch-01", ContentCategory.Mech)]
    [TestCase("W-ab", ContentCategory.Weapon)]
    [TestCase("Boss-01", ContentCategory.Boss)]
    public void ALowercasedOrMixedCaseIdIsRejectedBecauseIdsAreCaseSensitive(
        string id, ContentCategory category)
    {
        Assert.That(ContentId.TryCreate(id, category, out _), Is.False);
    }

    /// <summary>
    /// A Cyrillic 'А' is a different code point from a Latin 'A' and looks identical in a
    /// diff, which is exactly why "ASCII" is stated as a rule rather than assumed.
    /// </summary>
    [TestCase("MCH-А1")]
    [TestCase("MCH-０１")]
    public void ANonAsciiIdIsRejected(string id)
    {
        Assert.That(ContentId.TryCreate(id, ContentCategory.Mech, out _), Is.False);
    }

    [TestCase("")]
    [TestCase("not-an-id!")]
    [TestCase("W-AB ")]
    [TestCase(" W-AB")]
    [TestCase("W-AB\n")]
    [TestCase("W-GH")]
    public void AMalformedIdIsRejected(string id)
    {
        Assert.That(ContentId.TryCreate(id, ContentCategory.Weapon, out _), Is.False);
    }

    [Test]
    public void EveryDeclaredCategoryHasADirectoryAndAtLeastOneGrammar()
    {
        Expect.Multiple(() =>
        {
            foreach (ContentCategoryDescriptor descriptor in ContentCategories.All)
            {
                Assert.That(descriptor.DirectoryName, Is.Not.Empty);
                Assert.That(
                    descriptor.IdPatterns,
                    Is.Not.Empty,
                    descriptor.Category + " must declare at least one ID grammar");
            }
        });
    }

    [Test]
    public void ADirectoryResolvesToItsCategory()
    {
        Expect.Multiple(() =>
        {
            foreach (ContentCategoryDescriptor descriptor in ContentCategories.All)
            {
                Assert.That(
                    ContentCategories.TryResolveDirectory(
                        descriptor.DirectoryName, out ContentCategoryDescriptor? resolved),
                    Is.True);
                Assert.That(resolved!.Category, Is.EqualTo(descriptor.Category));
            }
        });
    }

    [Test]
    public void IdsCompareByValueAndCategory()
    {
        ContentId left = ContentId.Create("W-AB", ContentCategory.Weapon);
        ContentId right = ContentId.Create("W-AB", ContentCategory.Weapon);

        Expect.Multiple(() =>
        {
            Assert.That(left, Is.EqualTo(right));
            Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
            Assert.That(left, Is.Not.EqualTo(ContentId.Create("W-AC", ContentCategory.Weapon)));
        });
    }
}
