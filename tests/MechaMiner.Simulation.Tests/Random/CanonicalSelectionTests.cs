using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Canonical selection: order first, then draw an index, and consume no draw when the outcome
/// is already determined.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-008</c>, <c>VER-SIM-005-009</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract (doc 20 § Authoritative random-number contract) and
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering. Fixture:
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-degenerate-selection.txt</c>, which the
/// selection rules of doc 20 § Authoritative random-number contract names ("this convention is
/// fixture-pinned").
/// </para>
/// </remarks>
[TestFixture]
internal sealed class CanonicalSelectionTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-005-008</c>. An empty or singleton selection leaves state,
    /// increment, and consumed-draw count bit-identical.
    /// </summary>
    /// <remarks>
    /// The multi-candidate cases in the same fixture are the control: they do advance the
    /// state, which is what makes the zero-draw claim meaningful rather than vacuous.
    /// </remarks>
    [Test]
    public void EmptyAndSingletonSelectionConsumeNoDraw()
    {
        GoldenText.Matches(
            RandomVectorRendering.DegenerateSelectionGolden,
            RandomGoldenHeaders.DegenerateSelection
                + RandomVectorRendering.DegenerateSelectionBody(ProductionVectorEngine.Instance));

        RandomStreamSet set = new(RandomSchemaVersion.Current, RandomVectorRendering.FixtureMasterSeed);
        RandomStreamKey key = RandomStreamKey.Create(0x0205, 0UL);
        IRandomSource source = set.Source(key);

        ulong stateBefore = set.StateOf(key);
        ulong incrementBefore = set.IncrementOf(key);
        ulong drawsBefore = set.DrawCountOf(key);

        bool emptySelected = CanonicalSelection.TrySelect(
            source,
            Array.Empty<string>(),
            StringComparer.Ordinal,
            out string? fromEmpty);
        bool singletonSelected = CanonicalSelection.TrySelect(
            source,
            new[] { "MCH-01" },
            StringComparer.Ordinal,
            out string? fromSingleton);
        bool orderedSingletonSelected = CanonicalSelection.TrySelectFromCanonicalOrder(
            source,
            new[] { "MCH-01" },
            out string? fromOrderedSingleton);

        Expect.Multiple(() =>
        {
            Assert.That(emptySelected, Is.False, "an empty selection has no outcome");
            Assert.That(fromEmpty, Is.Null);
            Assert.That(singletonSelected, Is.True);
            Assert.That(fromSingleton, Is.EqualTo("MCH-01"), "the singleton is the outcome");
            Assert.That(orderedSingletonSelected, Is.True);
            Assert.That(fromOrderedSingleton, Is.EqualTo("MCH-01"));

            Assert.That(set.StateOf(key), Is.EqualTo(stateBefore), "state is bit-identical (doc 20 § Authoritative random-number contract)");
            Assert.That(set.IncrementOf(key), Is.EqualTo(incrementBefore), "increment is unchanged");
            Assert.That(set.DrawCountOf(key), Is.EqualTo(drawsBefore), "no draw was consumed");
            Assert.That(source.DrawCount, Is.Zero, "and the source agrees");

            // The control: two candidates do consume a draw and do advance the state. Note the
            // deliberate split doc 20 draws - selection short-circuits, but the bounded primitive
            // with a bound of one does not, because doc 20 § Authoritative random-number contract scopes the rule to selection.
            bool pairSelected = CanonicalSelection.TrySelect(
                source,
                new[] { "W-BD", "W-AB" },
                StringComparer.Ordinal,
                out string? fromPair);
            Assert.That(pairSelected, Is.True);
            Assert.That(fromPair, Is.EqualTo("W-AB"), "pinned by random-degenerate-selection.txt");
            Assert.That(set.DrawCountOf(key), Is.EqualTo(1UL), "a real choice costs one draw");
            Assert.That(set.StateOf(key), Is.Not.EqualTo(stateBefore));

            RandomStreamSet boundedSet = new(
                RandomSchemaVersion.Current,
                RandomVectorRendering.FixtureMasterSeed);
            RandomStreamKey boundedKey = RandomStreamKey.Create(0x0205, 0UL);
            _ = boundedSet.NextBounded(boundedKey, 1U);
            Assert.That(
                boundedSet.DrawCountOf(boundedKey),
                Is.EqualTo(1UL),
                "bounded(1) is the canonical PCG primitive and consumes one draw; the no-draw rule "
                    + "of doc 20 § Authoritative random-number contract is a property of selection, "
                    + "not of this primitive");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-009</c>. Canonical order is established before the index is
    /// drawn, so authored or enumeration order never reaches the generator.
    /// </summary>
    [Test]
    public void CanonicalOrderIsEstablishedBeforeTheIndexIsDrawn()
    {
        string[] authored = { "REL-01", "BOSS-01", "EN-01" };
        string[] reversed = { "EN-01", "BOSS-01", "REL-01" };
        string[] rotated = { "BOSS-01", "REL-01", "EN-01" };

        Expect.Multiple(() =>
        {
            string?[] selections = new string?[3];
            string[][] permutations = { authored, reversed, rotated };
            for (int index = 0; index < permutations.Length; index++)
            {
                RandomStreamSet set = new(
                    RandomSchemaVersion.Current,
                    RandomVectorRendering.FixtureMasterSeed);
                RandomStreamKey key = RandomStreamKey.Create(0x0205, 0UL);
                _ = CanonicalSelection.TrySelect(
                    set.Source(key),
                    permutations[index],
                    StringComparer.Ordinal,
                    out selections[index]);
                Assert.That(
                    set.DrawCountOf(key),
                    Is.EqualTo(1UL),
                    "every permutation costs the same single draw");
            }

            Assert.That(
                selections[1],
                Is.EqualTo(selections[0]),
                "the same members in a different insertion order select the same member");
            Assert.That(selections[2], Is.EqualTo(selections[0]));
            Assert.That(
                selections[0],
                Is.EqualTo("EN-01"),
                "pinned by random-degenerate-selection.txt: ordinal order is "
                    + "[BOSS-01, EN-01, REL-01] and the drawn index is 1");

            // Ordering is applied, not assumed: the canonical order of a permuted input is the
            // canonical order of the same members.
            Assert.That(
                CanonicalSelection.Order(reversed, StringComparer.Ordinal),
                Is.EqualTo(new[] { "BOSS-01", "EN-01", "REL-01" }));
            Assert.That(
                CanonicalSelection.Order(rotated, StringComparer.Ordinal),
                Is.EqualTo(CanonicalSelection.Order(authored, StringComparer.Ordinal)));

            // A comparer that is not a strict total order is refused rather than silently
            // resolved by authored position, which is the failure doc 20 § Authoritative random-number contract forbids.
            InvalidOperationException tie = Expect.Throws<InvalidOperationException>(
                () => CanonicalSelection.Order(authored, new AllEqualComparer()));
            Assert.That(tie.Message, Does.Contain("strict total order"));
        });
    }

    /// <summary>A comparer that reports every pair equal, so no canonical order
    /// exists.</summary>
    private sealed class AllEqualComparer : IComparer<string>
    {
        public int Compare(string? left, string? right)
        {
            return 0;
        }
    }
}
