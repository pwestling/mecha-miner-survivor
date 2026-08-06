using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The fixed-step accumulator contract: whole ticks only, a retained remainder that is never
/// observable, and no long-run drift.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-001-002</c>, <c>VER-SIM-001-003</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "The host uses an
/// accumulator to execute zero or more complete ticks per rendered frame. It never passes a
/// variable delta to authoritative systems."
/// </remarks>
[TestFixture]
internal sealed class FixedStepAccumulatorTests
{
    /// <summary>
    /// Member-name fragments that would expose a partial delta. A tick target that could read
    /// any of these could interpolate gameplay against a variable frame delta, which is what
    /// doc 10 § Clock domains forbids.
    /// </summary>
    private static readonly string[] PartialDeltaNameFragments =
    {
        "Remainder",
        "Fraction",
        "Partial",
        "Alpha",
        "Interpolat",
        "Retained",
        "Pending",
        "Leftover",
        "SubTick",
        "Accumulated",
    };

    /// <summary>
    /// Types that a tick target can reach: the accumulator, its result, the host, and the host's
    /// result. If a partial delta is observable at all, it is observable on one of these.
    /// </summary>
    private static readonly Type[] TickFacingTypes =
    {
        typeof(FixedStepAccumulator),
        typeof(TickBudget),
        typeof(SimulationHost),
        typeof(HostStepResult),
        typeof(ISimulationWorld),
    };

    /// <summary>
    /// Verification: <c>VER-SIM-001-002</c>.
    ///
    /// Three claims: every step yields a whole tick count; the fractional remainder is retained
    /// across steps rather than delivered or dropped; and no member of the accumulator, the
    /// budget, the host, the step result, or the tick-target interface exposes that remainder.
    /// </summary>
    [Test]
    public void OnlyWholeTicksAreEverYielded()
    {
        // 1. Behaviour, through the same assertion the negative control VER-SIM-001-008 proves
        //    can fail.
        ImmutableArray<double> shortStream = FrameDeltaStreams.ShortIrregular();
        AccumulatorContract.AssertOnlyWholeTicksAreYielded(
            new FixedStepAccumulatorSubject(),
            shortStream,
            ExpectedTicks(shortStream));

        // 2. The remainder is retained, not delivered and not dropped: two steps of just under
        //    half a tick yield nothing, and the third completes the tick they jointly cover.
        FixedStepAccumulator accumulator = new(CatchUpPolicy.Default);
        double thirdOfATick = TickRate.SecondsPerTick / 3.0;
        TickBudget first = accumulator.Advance(thirdOfATick);
        TickBudget second = accumulator.Advance(thirdOfATick);
        TickBudget third = accumulator.Advance(thirdOfATick);

        // 3. Structure: nothing exposes the remainder, and no member of the tick-target
        //    interface takes a duration at all.
        List<string> exposed = FindPartialDeltaMembers();
        List<string> durationParameters = FindDurationParametersOnTheTickTarget();

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(0L, first.TickCount, "a third of a tick completes no tick");
            NumericAssert.AreExactlyEqual(0L, second.TickCount, "two thirds of a tick completes no tick");
            NumericAssert.AreExactlyEqual(1L, third.TickCount, "three thirds of a tick completes one");
            NumericAssert.AreExactlyEqual(
                0L,
                first.DiscardedTickCount + second.DiscardedTickCount + third.DiscardedTickCount,
                "a retained remainder is not a discard");

            Assert.That(
                exposed,
                Is.Empty,
                "no member may expose the retained fraction: doc 10 § Clock domains says the host "
                    + "\"never passes a variable delta to authoritative systems\", which a readable "
                    + "remainder would immediately undo");
            Assert.That(
                durationParameters,
                Is.Empty,
                "no member of the tick target may take a duration; the only thing handed to a tick is "
                    + "its SimulationTick");
            Assert.That(
                typeof(TickBudget).GetProperty(nameof(TickBudget.TickCount))!.PropertyType,
                Is.EqualTo(typeof(int)),
                "the tick count is an integer type, so a fractional tick is not even representable");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-003</c>.
    ///
    /// Zero ticks below one interval, exactly one at the interval, the exact whole count above
    /// it, and a full run's worth of irregular deltas yielding exactly 126,000 ticks with no
    /// accumulated drift.
    /// </summary>
    [Test]
    public void ZeroOneAndManyTicksPerStepWithoutLongRunDrift()
    {
        FixedStepAccumulator accumulator = new(CatchUpPolicy.Default);
        TickBudget belowOne = accumulator.Advance(TickRate.SecondsPerTick * 0.5);
        TickBudget completesOne = accumulator.Advance(TickRate.SecondsPerTick * 0.5);
        TickBudget exactlyOne = accumulator.Advance(TickRate.SecondsPerTick);
        TickBudget three = accumulator.Advance(TickRate.SecondsForTicks(3));
        TickBudget nothing = accumulator.Advance(0.0);

        ImmutableArray<double> irregular = FrameDeltaStreams.FullRunIrregular();
        FixedStepAccumulator longRun = new(CatchUpPolicy.Default);
        long total = 0;
        int zeroTickSteps = 0;
        int oneTickSteps = 0;
        int manyTickSteps = 0;
        int discardedSteps = 0;
        foreach (double elapsed in irregular)
        {
            TickBudget budget = longRun.Advance(elapsed);
            total += budget.TickCount;
            if (budget.TickCount == 0)
            {
                zeroTickSteps++;
            }
            else if (budget.TickCount == 1)
            {
                oneTickSteps++;
            }
            else
            {
                manyTickSteps++;
            }

            if (budget.DiscardReason != AccumulatorDiscardReason.None)
            {
                discardedSteps++;
            }
        }

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(0L, belowOne.TickCount, "half a tick interval completes no tick");
            NumericAssert.AreExactlyEqual(1L, completesOne.TickCount, "the second half completes it");
            NumericAssert.AreExactlyEqual(
                1L,
                exactlyOne.TickCount,
                "exactly one tick interval completes exactly one tick");
            NumericAssert.AreExactlyEqual(3L, three.TickCount, "three tick intervals complete three ticks");
            NumericAssert.AreExactlyEqual(0L, nothing.TickCount, "no elapsed time completes no tick");

            NumericAssert.AreExactlyEqual(
                FrameDeltaStreams.FullRunTicks,
                total,
                "a full run of irregular deltas totalling exactly 2,100 s must yield exactly 126,000 "
                    + "ticks; anything else is accumulated drift (doc 10 § Clock domains)");
            NumericAssert.AreExactlyEqual(0L, discardedSteps, "no step in the stream may reach the bound");
            Assert.That(zeroTickSteps, Is.GreaterThan(0), "the stream must contain zero-tick steps");
            Assert.That(oneTickSteps, Is.GreaterThan(0), "the stream must contain one-tick steps");
            Assert.That(manyTickSteps, Is.GreaterThan(0), "the stream must contain multi-tick steps");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-003</c>.
    ///
    /// The tick-boundary snap is bounded from both sides, so it cannot drift into either failure
    /// mode: too small and an exactly representable stream loses its final tick to double
    /// rounding, too large and it could concede a tick an interval did not cover.
    /// </summary>
    [Test]
    public void TheTickBoundarySnapStaysBetweenItsTwoDerivedBounds()
    {
        const double accumulatorOwnErrorTicks = 1e-11;
        const double monotonicClockResolutionTicks = 1e-6;

        Expect.Multiple(() =>
        {
            Assert.That(
                FixedStepAccumulator.TickBoundarySnapTicks,
                Is.GreaterThan(accumulatorOwnErrorTicks),
                "the snap must exceed the accumulator's own worst-case rounding over a full run "
                    + "(about 6e-12 ticks), or an exactly representable stream loses a tick");
            Assert.That(
                FixedStepAccumulator.TickBoundarySnapTicks,
                Is.LessThan(monotonicClockResolutionTicks),
                "the snap must stay far below the ~100 ns a monotonic frame clock can resolve (about "
                    + "6e-6 ticks), or it could concede a tick the interval did not cover");
        });
    }

    /// <summary>The whole ticks a stream's exact total elapsed time covers.</summary>
    private static long ExpectedTicks(IReadOnlyList<double> elapsedSecondsPerStep)
    {
        double total = 0.0;
        foreach (double elapsed in elapsedSecondsPerStep)
        {
            total += elapsed;
        }

        return (long)Math.Floor(total * TickRate.TicksPerSecond);
    }

    /// <summary>Every member of a tick-facing type whose name suggests a partial delta.</summary>
    private static List<string> FindPartialDeltaMembers()
    {
        List<string> found = new();
        foreach (Type type in TickFacingTypes)
        {
            foreach (MemberInfo member in type.GetMembers(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance
                | BindingFlags.DeclaredOnly))
            {
                foreach (string fragment in PartialDeltaNameFragments)
                {
                    if (member.Name.Contains(fragment, StringComparison.Ordinal))
                    {
                        found.Add(type.Name + "." + member.Name);
                    }
                }
            }
        }

        return found;
    }

    /// <summary>Every parameter of the tick target that is a floating-point duration.</summary>
    private static List<string> FindDurationParametersOnTheTickTarget()
    {
        List<string> found = new();
        foreach (MethodInfo method in typeof(ISimulationWorld).GetMethods())
        {
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.ParameterType == typeof(double)
                    || parameter.ParameterType == typeof(float)
                    || parameter.ParameterType == typeof(decimal)
                    || parameter.ParameterType == typeof(TimeSpan))
                {
                    found.Add(method.Name + "(" + parameter.Name + ": " + parameter.ParameterType.Name + ")");
                }
            }
        }

        return found;
    }
}
