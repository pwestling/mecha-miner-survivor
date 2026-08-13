using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The frame-delta streams the accumulator gates are fed.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-002</c>, <c>VER-SIM-001-003</c>, <c>VER-SIM-001-004</c>,
/// <c>VER-SIM-002-009</c>.
/// </para>
/// <para>
/// <b>Every irregular delta is an exact binary fraction</b> - a multiple of <c>1/128</c> of a
/// second. That is not cosmetic. A stream of decimal-looking deltas such as <c>0.017</c> does
/// not sum to any exact number of ticks, so "126,000 ticks with no drift" could not be
/// asserted exactly and the test would need a tolerance, which doc 91 § Numeric tolerance
/// forbids for tick counts: ticks are one of the quantities it lists as exact. With dyadic
/// deltas every partial sum is exactly representable, the stream's total is exactly 2,100
/// seconds, and the expected tick count is exactly 126,000 - so a failure is always the
/// accumulator's, never the fixture's.
/// </para>
/// <para>
/// Every delta is also below one tick interval times the catch-up bound, so the streams
/// exercise long-run accumulation without ever reaching the bound. The bound has its own gates
/// (<c>VER-SIM-001-005</c>, <c>VER-SIM-001-006</c>).
/// </para>
/// </remarks>
internal static class FrameDeltaStreams
{
    /// <summary>The granularity every irregular delta is a whole multiple of: one 128th of a second.</summary>
    /// <remarks>
    /// A negative power of two, so it and every sum of its multiples up to a few thousand
    /// seconds is exactly representable as a <see cref="double"/>.
    /// </remarks>
    internal const double DyadicGranularitySeconds = 1.0 / 128.0;

    /// <summary>The authored run length in seconds: 35:00, which is 126,000 ticks.</summary>
    internal const double FullRunSeconds = 2100.0;

    /// <summary>The whole ticks a full run contains.</summary>
    internal const long FullRunTicks = 126_000L;

    /// <summary>
    /// One cycle of the irregular pattern, in whole 128ths of a second: 1, 2, 3, 4, 1, 5, 2, 6.
    /// </summary>
    /// <remarks>
    /// Chosen so one cycle mixes intervals shorter than a tick (1/128 s is 0.47 ticks), about
    /// one tick (2/128 s is 0.94), and longer than one tick (6/128 s is 2.81), which is what
    /// produces the zero-, one-, and many-tick steps <c>VER-SIM-001-003</c> requires from a
    /// single stream. The cycle sums to 24/128 s, which is not a whole number of ticks, so the
    /// retained fraction is genuinely carried across cycles rather than resetting.
    /// </remarks>
    private static readonly ImmutableArray<int> IrregularCycleIn128ths =
        ImmutableArray.Create(1, 2, 3, 4, 1, 5, 2, 6);

    /// <summary>
    /// An irregular stream whose total is exactly <see cref="FullRunSeconds"/> and which
    /// therefore must yield exactly <see cref="FullRunTicks"/> ticks.
    /// </summary>
    internal static ImmutableArray<double> FullRunIrregular() => Irregular(FullRunSeconds);

    /// <summary>
    /// A uniform stream of exactly one tick interval per step whose total is
    /// <see cref="FullRunSeconds"/>.
    /// </summary>
    /// <remarks>
    /// The comparison stream for <c>VER-SIM-001-004</c>: the same total elapsed time delivered
    /// as perfectly even frames. It must reach the same tick index, and therefore the same
    /// derived seconds, as the irregular stream.
    /// </remarks>
    internal static ImmutableArray<double> FullRunUniform()
    {
        ImmutableArray<double>.Builder deltas = ImmutableArray.CreateBuilder<double>((int)FullRunTicks);
        for (long tick = 0; tick < FullRunTicks; tick++)
        {
            deltas.Add(TickRate.SecondsPerTick);
        }

        return deltas.ToImmutable();
    }

    /// <summary>
    /// A short irregular stream: three cycles of the pattern, 24 steps totalling 72/128 s.
    /// </summary>
    /// <remarks>
    /// The stream <c>VER-SIM-002-009</c> feeds twice, once unpaused and once with a pause
    /// spliced into the middle. Short enough that the whole tick sequence fits in a reviewable
    /// golden.
    /// </remarks>
    internal static ImmutableArray<double> ShortIrregular()
    {
        return Irregular(3 * CycleSeconds());
    }

    /// <summary>The exact duration of one cycle of the irregular pattern.</summary>
    internal static double CycleSeconds()
    {
        int total = 0;
        foreach (int units in IrregularCycleIn128ths)
        {
            total += units;
        }

        return total * DyadicGranularitySeconds;
    }

    /// <summary>
    /// Builds an irregular stream whose deltas sum to exactly
    /// <paramref name="totalSeconds"/>.
    /// </summary>
    /// <param name="totalSeconds">
    /// A whole multiple of <see cref="DyadicGranularitySeconds"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="totalSeconds"/> is not a whole multiple of the dyadic granularity, which
    /// would make the stream's total inexact and the expected tick count unassertable.
    /// </exception>
    private static ImmutableArray<double> Irregular(double totalSeconds)
    {
        double unitsExact = totalSeconds / DyadicGranularitySeconds;
        if (unitsExact != Math.Floor(unitsExact) || unitsExact <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(totalSeconds),
                totalSeconds,
                "an irregular stream's total must be a positive whole multiple of 1/128 s so that every "
                + "partial sum is exactly representable");
        }

        long remainingUnits = (long)unitsExact;
        List<double> deltas = new();
        int cycleIndex = 0;
        while (remainingUnits > 0)
        {
            int units = IrregularCycleIn128ths[cycleIndex % IrregularCycleIn128ths.Length];
            if (units > remainingUnits)
            {
                units = (int)remainingUnits;
            }

            deltas.Add(units * DyadicGranularitySeconds);
            remainingUnits -= units;
            cycleIndex++;
        }

        return deltas.ToImmutableArray();
    }
}
