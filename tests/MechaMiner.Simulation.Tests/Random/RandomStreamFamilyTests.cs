using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// The registered stream-family table of doc 20 § Authoritative random-number contract: all 23
/// keys, unique, with their documented instance-key rules, and closed against invention.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-010</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract (doc 20 § Authoritative random-number contract) and
/// <c>docs/technical/10-runtime-architecture.md</c> § Randomness and reproducibility. Fixture:
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-stream-independence.txt</c>, which
/// records all 23 families in ascending key order so a collision or an off-by-one is visible as
/// a duplicated or shifted row.
/// </para>
/// <para>
/// The expected table below is transcribed from the family table of doc 20 § Authoritative
/// random-number contract here, in the test, rather than read from the registry it checks.
/// Reading the registry would assert only that the registry equals itself.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class RandomStreamFamilyTests
{
    /// <summary>The the family table of doc 20 § Authoritative random-number contract table:
    /// family key, registered name, instance-key rule.</summary>
    private static readonly (ushort Key, string Name, InstanceKeyRule Rule)[] DocumentedFamilies =
    {
        (0x0100, "resource-profile selection", InstanceKeyRule.Zero),
        (0x0200, "major topology", InstanceKeyRule.Zero),
        (0x0201, "spatial embedding", InstanceKeyRule.Zero),
        (0x0202, "region recipes", InstanceKeyRule.RegionId),
        (0x0203, "landmarks", InstanceKeyRule.RegionId),
        (0x0204, "obstacle/dressing placement", InstanceKeyRule.RegionId),
        (0x0205, "deployment selection", InstanceKeyRule.Zero),
        (0x0210, "standard-seam placement", InstanceKeyRule.Zero),
        (0x0211, "rich-seam placement", InstanceKeyRule.Zero),
        (0x0220, "material-geode placement", InstanceKeyRule.MaterialOrdinal),
        (0x0230, "Hyper Gold placement", InstanceKeyRule.Zero),
        (0x0240, "relic-cache placement", InstanceKeyRule.Zero),
        (0x0241, "relic assignment", InstanceKeyRule.Zero),
        (0x0250, "dynamic rocks/drop rolls", InstanceKeyRule.RockId),
        (0x0260, "release fallback-manifest selection", InstanceKeyRule.ProfileAndRegionCountOrdinal),
        (0x0300, "baseline encounter sectors/composition", InstanceKeyRule.Zero),
        (0x0301, "authored event formations", InstanceKeyRule.ScheduleRowIndex),
        (0x0302, "beacon response selection", InstanceKeyRule.SiteId),
        (0x0303, "boss entry/ability randomness", InstanceKeyRule.BossIndex),
        (0x0400, "player weapon combat randomness", InstanceKeyRule.WeaponSlotOrdinal),
        (0x0410, "enemy combat randomness", InstanceKeyRule.SourceAndGeneration),
        (0x0500, "boss/other authorized loot", InstanceKeyRule.RewardSourceId),
        (0xF000, "presentation-only variation", InstanceKeyRule.PresentationBinding),
    };

    /// <summary>
    /// Verification: <c>VER-SIM-005-010</c>. All 23 documented families are registered with
    /// their exact keys and rules, every key is unique, and an unregistered key is refused.
    /// </summary>
    [Test]
    public void AllRegisteredFamilyKeysArePresentUniqueAndClosed()
    {
        GoldenText.Matches(
            RandomVectorRendering.StreamIndependenceGolden,
            RandomGoldenHeaders.StreamIndependence
                + RandomVectorRendering.StreamIndependenceBody(ProductionVectorEngine.Instance));

        IReadOnlyList<RandomStreamFamily> registered = RandomStreamFamilies.All;

        Expect.Multiple(() =>
        {
            Assert.That(
                registered,
                Has.Count.EqualTo(DocumentedFamilies.Length),
                "doc 20 § Authoritative random-number contract registers exactly 23 stream families");
            Assert.That(DocumentedFamilies, Has.Length.EqualTo(23));

            HashSet<ushort> seen = new();
            for (int index = 0; index < DocumentedFamilies.Length; index++)
            {
                (ushort key, string name, InstanceKeyRule rule) = DocumentedFamilies[index];
                string label = "0x" + key.ToString("X4", CultureInfo.InvariantCulture);

                Assert.That(seen.Add(key), Is.True, label + " must appear once; keys are never reused");
                Assert.That(
                    registered[index].Key,
                    Is.EqualTo(key),
                    label + " must be at position "
                        + index.ToString(CultureInfo.InvariantCulture)
                        + " of ascending key order");
                Assert.That(registered[index].Name, Is.EqualTo(name), label + ": registered name");
                Assert.That(
                    registered[index].InstanceKeyRule,
                    Is.EqualTo(rule),
                    label + ": documented instance-key rule");
                Assert.That(
                    registered[index].IsRegistered,
                    Is.True,
                    label + ": registry entries are not default values");
                Assert.That(
                    RandomStreamFamilies.Get(key),
                    Is.EqualTo(registered[index]),
                    label + ": lookup by key returns the same family");
            }

            // The presentation family is the only non-authoritative one (doc 20 § Authoritative random-number contract).
            foreach (RandomStreamFamily family in registered)
            {
                Assert.That(
                    family.IsAuthoritative,
                    Is.EqualTo(family.Key != 0xF000),
                    family.ToString() + ": only 0xF000 is presentation-only");
            }

            // Closed: an unregistered key is refused everywhere, rather than silently deriving a
            // stream no golden vector pins (doc 20 § Authoritative random-number contract).
            ushort[] unregistered = { 0x0000, 0x0001, 0x0102, 0x0206, 0x0304, 0x0501, 0xF001, 0xFFFF };
            foreach (ushort key in unregistered)
            {
                string label = "0x" + key.ToString("X4", CultureInfo.InvariantCulture);
                Assert.That(
                    RandomStreamFamilies.TryGet(key, out RandomStreamFamily absent),
                    Is.False,
                    label + " is not registered");
                Assert.That(absent.IsRegistered, Is.False);
                Expect.Throws<ArgumentOutOfRangeException>(() => RandomStreamFamilies.Get(key));
                Expect.Throws<ArgumentOutOfRangeException>(() => RandomStreamKey.Create(key, 0UL));
            }

            // A default family value can never become a stream key either.
            Expect.Throws<ArgumentOutOfRangeException>(
                () => RandomStreamKey.Create(default(RandomStreamFamily), 0UL));
        });
    }
}
