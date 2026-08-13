using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MechaMiner.Simulation.Random;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// Bounded integers, the single 53-bit unit-interval conversion, and integer-ratio chance.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-005-005</c>, <c>VER-SIM-005-006</c>, <c>VER-SIM-005-007</c>.
/// </para>
/// <para>
/// Authority: <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number
/// contract (doc 20 § Authoritative random-number contract) and § Numeric and unit conventions.
/// Fixture:
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-unit-double-conversion.txt</c>.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class BoundedRandomTests
{
    /// <summary>Bounds that do not divide <c>2^32</c>, so every one has a nonzero
    /// threshold.</summary>
    private static readonly uint[] BiasedWithoutRejection =
    {
        3U, 5U, 6U, 7U, 9U, 10U, 52U, 100U, 1000U, 3221225472U, 4294967295U,
    };

    /// <summary>
    /// Verification: <c>VER-SIM-005-005</c>. Draws below the threshold are rejected and
    /// redrawn, the accepted set is an exact multiple of the bound, and the consumed-draw count
    /// exceeds the sample count.
    /// </summary>
    [Test]
    public void BoundedIntegersUseRejectionSamplingNotModulo()
    {
        Expect.Multiple(() =>
        {
            // 1. The threshold is (2^32 - bound) mod bound, and the accepted range is an exact
            //    multiple of the bound. That equality is the whole reason rejection sampling is
            //    unbiased where plain modulo is not, so it is asserted rather than assumed.
            foreach (uint bound in BiasedWithoutRejection)
            {
                uint threshold = BoundedRandom.RejectionThreshold(bound);
                Assert.That(
                    threshold,
                    Is.EqualTo(RandomVectorRendering.RejectionThreshold(bound)),
                    "bound " + bound.ToString(CultureInfo.InvariantCulture) + ": threshold formula");
                Assert.That(
                    threshold,
                    Is.Not.Zero,
                    "bound " + bound.ToString(CultureInfo.InvariantCulture)
                        + " does not divide 2^32, so plain modulo would be biased");
                Assert.That(
                    (4294967296UL - threshold) % bound,
                    Is.Zero,
                    "bound " + bound.ToString(CultureInfo.InvariantCulture)
                        + ": the accepted count is an exact multiple of the bound");
            }

            // 2. A draw below the threshold is discarded, not reduced. Plain modulo would have
            //    returned 0 for draw 0 and 95 for draw 95; rejection sampling returns 96.
            ScriptedRandomSource rejecting = new(0U, 95U, 96U, 199U);
            uint result = BoundedRandom.NextBounded(rejecting, 100U);
            Assert.That(BoundedRandom.RejectionThreshold(100U), Is.EqualTo(96U));
            Assert.That(result, Is.EqualTo(96U), "the first accepted draw is 96, reduced to 96");
            Assert.That(rejecting.DrawCount, Is.EqualTo(3UL), "two draws were rejected and redrawn");
            Assert.That(rejecting.RemainingCount, Is.EqualTo(1), "no further draw was consumed");

            // 3. Enumerating one whole accepted window gives an exactly uniform residue
            //    distribution: every residue appears the same number of times.
            const uint enumeratedBound = 3U;
            const int windowCount = 500;
            uint enumeratedThreshold = BoundedRandom.RejectionThreshold(enumeratedBound);
            List<uint> window = new();
            for (uint offset = 0; offset < enumeratedBound * windowCount; offset++)
            {
                window.Add(enumeratedThreshold + offset);
            }

            ScriptedRandomSource enumerated = new(window);
            int[] residueCounts = new int[enumeratedBound];
            for (int sample = 0; sample < enumeratedBound * windowCount; sample++)
            {
                residueCounts[BoundedRandom.NextBounded(enumerated, enumeratedBound)]++;
            }

            for (int residue = 0; residue < residueCounts.Length; residue++)
            {
                Assert.That(
                    residueCounts[residue],
                    Is.EqualTo(windowCount),
                    "residue " + residue.ToString(CultureInfo.InvariantCulture)
                        + " must occur exactly as often as every other over an accepted window");
            }

            // 4. Every draw strictly below the threshold is rejected, so a source of nothing but
            //    rejected values is exhausted rather than reduced.
            ScriptedRandomSource allRejected = new(0U, 1U, 2U, 3U);
            InvalidOperationException exhausted = Expect.Throws<InvalidOperationException>(
                () => BoundedRandom.NextBounded(allRejected, 6U));
            Assert.That(BoundedRandom.RejectionThreshold(6U), Is.EqualTo(4U));
            Assert.That(exhausted.Message, Does.Contain("exhausted"));

            // 5. Over the pinned fixture stream, 16 results of bound 0xC0000000 consume more
            //    than 16 draws, so rejection is observable in the committed vector too.
            RandomStreamSet set = new(RandomSchemaVersion.Current, RandomVectorRendering.FixtureMasterSeed);
            RandomStreamKey key = RandomStreamKey.Create(0x0100, 0UL);
            for (int sample = 0; sample < 16; sample++)
            {
                _ = set.NextBounded(key, 0xC0000000U);
            }

            Assert.That(
                set.DrawCountOf(key),
                Is.GreaterThan(16UL),
                "bound 0xC0000000 rejects one draw in four, so 16 results cost more than 16 draws");
            Assert.That(
                set.DrawCountOf(key),
                Is.EqualTo(21UL),
                "and the exact count is pinned by random-bounded-conversion.txt");

            // 6. A zero bound has no representable result.
            ScriptedRandomSource unused = new(1U);
            Assert.That(unused.DrawCount, Is.Zero);
            Expect.Throws<ArgumentOutOfRangeException>(() => BoundedRandom.NextBounded(unused, 0U));
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-006</c>. Exactly one 53-bit conversion exists, it matches
    /// its committed golden, and it returns a value in <c>[0,1)</c> for every input pattern.
    /// </summary>
    [Test]
    public void TheSingleFiftyThreeBitUnitConversionIsGoldenAndInRange()
    {
        GoldenText.Matches(
            RandomVectorRendering.UnitDoubleConversionGolden,
            RandomGoldenHeaders.UnitDoubleConversion
                + RandomVectorRendering.UnitDoubleConversionBody(ProductionVectorEngine.Instance));

        (uint High, uint Low)[] extremes =
        {
            (0x00000000U, 0x00000000U),
            (0x00000000U, 0x000007FFU),
            (0x00000000U, 0xFFFFFFFFU),
            (0x00000001U, 0x00000000U),
            (0x7FFFFFFFU, 0xFFFFFFFFU),
            (0x80000000U, 0x00000000U),
            (0xFFFFFFFFU, 0xFFFFFFF8U),
            (0xFFFFFFFFU, 0xFFFFFFFFU),
        };

        Expect.Multiple(() =>
        {
            foreach ((uint high, uint low) in extremes)
            {
                double value = BoundedRandom.UnitDouble(high, low);
                string label = "0x" + high.ToString("X8", CultureInfo.InvariantCulture)
                    + "/0x" + low.ToString("X8", CultureInfo.InvariantCulture);

                Assert.That(value, Is.GreaterThanOrEqualTo(0.0), label + ": never negative");
                Assert.That(value, Is.LessThan(1.0), label + ": never 1.0, the interval is [0,1)");
                Assert.That(
                    RandomVectorRendering.MantissaOf(value),
                    Is.EqualTo((((ulong)high << 32) | low) >> 11),
                    label + ": the mantissa is the top 53 bits, first draw as the high half");
            }

            Assert.That(BoundedRandom.UnitDouble(0U, 0U), Is.EqualTo(0.0), "all-zero bits give exactly 0");
            Assert.That(
                BoundedRandom.UnitDouble(0U, 0x000007FFU),
                Is.EqualTo(0.0),
                "the low 11 bits are discarded, so they cannot move the value");
            Assert.That(
                BoundedRandom.UnitDouble(0xFFFFFFFFU, 0xFFFFFFFFU),
                Is.EqualTo(9007199254740991.0 / 9007199254740992.0),
                "all-ones bits give the largest representable value below one, (2^53-1)/2^53");

            // The conversion is one function, not one per caller. Reflection over the public
            // surface is the only way to assert "exactly one conversion exists" structurally: a
            // second copy of the arithmetic elsewhere in the namespace would silently drift from
            // the golden that pins this one.
            List<string> doubleReturningMembers = new();
            foreach (Type type in typeof(BoundedRandom).Assembly.GetExportedTypes())
            {
                if (!string.Equals(type.Namespace, typeof(BoundedRandom).Namespace, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static
                    | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.ReturnType == typeof(double))
                    {
                        doubleReturningMembers.Add(type.Name + "." + method.Name);
                    }
                }
            }

            doubleReturningMembers.Sort(StringComparer.Ordinal);
            Assert.That(
                doubleReturningMembers,
                Is.EqualTo(new[]
                {
                    "BoundedRandom.NextUnitDouble",
                    "BoundedRandom.UnitDouble",
                    "RandomStreamSet.NextUnitDouble",
                }),
                "one conversion (UnitDouble) and two drivers that delegate to it; nothing else "
                    + "in MechaMiner.Simulation.Random may produce a double from draws (doc 20 § Authoritative random-number contract)");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-005-007</c>. An integer-ratio chance compares integers,
    /// consumes exactly one draw, and consumes none at all when the outcome is already
    /// determined.
    /// </summary>
    [Test]
    public void IntegerRatioChanceComparesIntegersAndConsumesOneDraw()
    {
        Expect.Multiple(() =>
        {
            // Exactly one draw: the source holds one value, so a conversion that routed through
            // the two-draw [0,1) double would throw instead of answering.
            ScriptedRandomSource single = new(0x3FFFFFFFU);
            Assert.That(BoundedRandom.NextChance(single, 1U, 4U), Is.True);
            Assert.That(single.DrawCount, Is.EqualTo(1UL), "exactly one draw is consumed");
            Assert.That(single.RemainingCount, Is.Zero);

            // The comparison is exact integer arithmetic at the boundary: draw × denominator
            // versus numerator × 2^32. 0x40000000 × 4 is exactly 2^32, which is not below it.
            ScriptedRandomSource atBoundary = new(0x40000000U);
            Assert.That(
                BoundedRandom.NextChance(atBoundary, 1U, 4U),
                Is.False,
                "draw × denominator == numerator × 2^32 is not below it: the comparison is exact");

            ScriptedRandomSource belowBoundary = new(0x3FFFFFFFU);
            Assert.That(BoundedRandom.NextChance(belowBoundary, 1U, 4U), Is.True);

            // A one-in-three chance has no exact binary representation, which is precisely the
            // case doc 20 § Authoritative random-number contract wants compared as integers rather than as a double.
            ScriptedRandomSource thirdBelow = new(0x55555555U);
            ScriptedRandomSource thirdAbove = new(0x55555556U);
            Assert.That(BoundedRandom.NextChance(thirdBelow, 1U, 3U), Is.True);
            Assert.That(BoundedRandom.NextChance(thirdAbove, 1U, 3U), Is.False);

            // Guaranteed outcomes consume no draw, so adding one cannot shift a later sequence.
            ScriptedRandomSource never = new();
            Assert.That(BoundedRandom.NextChance(never, 0U, 7U), Is.False, "ratio zero always fails");
            Assert.That(never.DrawCount, Is.Zero, "ratio zero consumes no draw");

            ScriptedRandomSource always = new();
            Assert.That(BoundedRandom.NextChance(always, 7U, 7U), Is.True, "ratio one always succeeds");
            Assert.That(always.DrawCount, Is.Zero, "ratio one consumes no draw");

            // A stream's state is untouched by a guaranteed chance.
            RandomStreamSet set = new(RandomSchemaVersion.Current, RandomVectorRendering.FixtureMasterSeed);
            RandomStreamKey key = RandomStreamKey.Create(0x0400, 0UL);
            ulong stateBefore = set.StateOf(key);
            Assert.That(set.NextChance(key, 0U, 100U), Is.False);
            Assert.That(set.NextChance(key, 100U, 100U), Is.True);
            Assert.That(set.StateOf(key), Is.EqualTo(stateBefore), "no draw, so no advance");
            Assert.That(set.DrawCountOf(key), Is.Zero);

            // And a real chance consumes exactly one.
            _ = set.NextChance(key, 1U, 5U);
            Assert.That(set.DrawCountOf(key), Is.EqualTo(1UL));

            ScriptedRandomSource invalid = new(1U);
            Expect.Throws<ArgumentOutOfRangeException>(() => BoundedRandom.NextChance(invalid, 2U, 1U));
            Expect.Throws<ArgumentOutOfRangeException>(() => BoundedRandom.NextChance(invalid, 0U, 0U));
        });
    }
}
