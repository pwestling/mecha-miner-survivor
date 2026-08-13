using System;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Ids;

/// <summary>
/// Tombstones and retirement. Verification: <c>VER-DAT-001-015</c>.
/// </summary>
/// <remarks>
/// Doc 40 § Stable ID policy: "Removing shipped content retires its ID and leaves a
/// migration/tombstone entry; IDs are never reassigned."
/// </remarks>
[TestFixture]
internal sealed class RetiredIdRegistryTests
{
    private static RetiredIdRegistry RegistryWithRetiredWeapon()
    {
        return new RetiredIdRegistry(new[]
        {
            new RetiredId("W-EF", ContentCategory.Weapon, 4, "W-AB", "superseded by W-AB"),
        });
    }

    /// <summary>
    /// The shipped registry is empty because nothing has shipped. That is the correct
    /// state, and asserting it stops the mechanism being quietly removed as "unused".
    /// </summary>
    [Test]
    public void TheShippedRegistryIsEmptyBecauseNothingHasShippedYet()
    {
        Assert.That(RetiredIdRegistry.Shipped.IsEmpty, Is.True);
    }

    [Test]
    public void ARetiredIdIsRecognisedAsRetired()
    {
        RetiredIdRegistry registry = RegistryWithRetiredWeapon();

        Assert.That(
            registry.IsRetired(ContentId.Create("W-EF", ContentCategory.Weapon)),
            Is.True);
    }

    [Test]
    public void ATombstoneCarriesItsRetiringVersionReplacementAndRationale()
    {
        RetiredIdRegistry registry = RegistryWithRetiredWeapon();

        Assert.That(
            registry.TryGetTombstone(
                ContentId.Create("W-EF", ContentCategory.Weapon), out RetiredId? tombstone),
            Is.True);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(4, tombstone!.RetiredInContentVersion, "retiring version");
            NumericAssert.AreExactlyEqual("W-AB", tombstone.ReplacedBy!, "migration target");
            Assert.That(tombstone.Rationale, Is.Not.Empty);
        });
    }

    [Test]
    public void AnUnretiredIdIsNotRetired()
    {
        Assert.That(
            RegistryWithRetiredWeapon().IsRetired(ContentId.Create("W-AB", ContentCategory.Weapon)),
            Is.False);
    }

    /// <summary>
    /// The grammars do not overlap today but are not required to stay disjoint, so
    /// retiring an ID in one category must not retire a same-spelled ID in another.
    /// </summary>
    [Test]
    public void RetirementIsScopedToItsCategory()
    {
        RetiredIdRegistry registry = new(new[]
        {
            new RetiredId("REL-01", ContentCategory.Relic, 2, null, "removed"),
        });

        Expect.Multiple(() =>
        {
            Assert.That(
                registry.IsRetired(ContentId.Create("REL-01", ContentCategory.Relic)), Is.True);
            Assert.That(
                registry.IsRetired(ContentId.Create("UNL-01", ContentCategory.Unlock)), Is.False);
        });
    }

    [Test]
    public void ATombstoneMayRecordThatThereIsNoSuccessor()
    {
        RetiredId tombstone = new("REL-01", ContentCategory.Relic, 2, null, "cut, no replacement");

        Assert.That(tombstone.ReplacedBy, Is.Null);
    }

    [Test]
    public void ATombstoneMustRecordAnIdThatWasOnceValid()
    {
        Expect.Throws<ArgumentException>(
            () => new RetiredId("not-an-id!", ContentCategory.Weapon, 1, null, "why"));
    }

    [Test]
    public void ATombstoneMustStateWhyTheContentWasRemoved()
    {
        Expect.Throws<ArgumentException>(
            () => new RetiredId("W-EF", ContentCategory.Weapon, 1, null, "   "));
    }

    [Test]
    public void ATombstoneIsRecordedOnceAndNotAmendedInPlace()
    {
        Expect.Throws<ArgumentException>(() => new RetiredIdRegistry(new[]
        {
            new RetiredId("W-EF", ContentCategory.Weapon, 4, "W-AB", "first"),
            new RetiredId("W-EF", ContentCategory.Weapon, 5, "W-AC", "second"),
        }));
    }
}
