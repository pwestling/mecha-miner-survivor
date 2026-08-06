using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The tick-rate contract: 60 Hz, exact as a rational, stated in one place, and unchangeable.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-001-001</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "The simulation frequency
/// is <b>60 ticks per second</b>. It is constant within a run. ... Changing it is architectural
/// because it changes schedules, numeric fixtures, recovery, and performance; it requires
/// measured evidence and a TDR rather than a local optimization."
/// </remarks>
[TestFixture]
internal sealed class TickRateTests
{
    /// <summary>
    /// Member names that would each be a second statement of the tick rate. Every one of them,
    /// anywhere in the simulation assembly, must be declared on <see cref="TickRate"/>.
    /// </summary>
    private static readonly string[] RateBearingMemberNames =
    {
        "TicksPerSecond",
        "TicksPerSecondNumerator",
        "TicksPerSecondDenominator",
        "TicksPerMinute",
        "SecondsPerTick",
        "TickFrequency",
        "TickHertz",
        "Hertz",
    };

    /// <summary>
    /// Verification: <c>VER-SIM-001-001</c>.
    ///
    /// Four separate claims: the rate is 60 ticks per second; the rational is exact, which the
    /// <see cref="double"/> alone is not; the rate is stated in exactly one place in the
    /// assembly; and nothing can change it, before or after a run starts.
    /// </summary>
    [Test]
    public void FrequencyIsSixtyHertzAndConstantWithinARun()
    {
        Expect.Multiple(() =>
        {
            // 1. The rate is sixty ticks per second, as an exact rational.
            NumericAssert.AreExactlyEqual(60L, TickRate.TicksPerSecondNumerator, "tick rate numerator");
            NumericAssert.AreExactlyEqual(1L, TickRate.TicksPerSecondDenominator, "tick rate denominator");
            NumericAssert.AreExactlyEqual(60L, TickRate.TicksPerSecond, "ticks per second");
            NumericAssert.AreExactlyEqual(3_600L, TickRate.TicksPerMinute, "ticks per minute");
            NumericAssert.AreExactlyEqual(
                TickRate.TicksPerSecondNumerator,
                TickRate.TicksPerSecond * TickRate.TicksPerSecondDenominator,
                "the rational must reduce to the stated integer rate");

            // 2. The seconds-per-tick double is the single correctly rounded quotient of that
            //    rational - compared as bits, because two doubles that print identically are not
            //    necessarily equal.
            Assert.That(
                BitConverter.DoubleToInt64Bits(TickRate.SecondsPerTick),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(1.0 / 60.0)),
                "SecondsPerTick must be the single rounded quotient of the rational rate");

            // 3. The rational is exact where the double is not: sixty ticks is exactly one
            //    second, and n * 60 ticks is exactly n seconds, for every n tested. Accumulating
            //    SecondsPerTick sixty times is not exactly one second, which is why the rational
            //    exists at all.
            for (int seconds = 1; seconds <= 1_000; seconds++)
            {
                Assert.That(
                    BitConverter.DoubleToInt64Bits(TickRate.SecondsForTicks(seconds * 60L)),
                    Is.EqualTo(BitConverter.DoubleToInt64Bits(seconds)),
                    "the exact rational makes "
                        + seconds.ToString(CultureInfo.InvariantCulture)
                        + " * 60 ticks exactly "
                        + seconds.ToString(CultureInfo.InvariantCulture)
                        + " second(s)");
            }

            double accumulated = 0.0;
            for (int tick = 0; tick < 60; tick++)
            {
                accumulated += TickRate.SecondsPerTick;
            }

            Assert.That(
                BitConverter.DoubleToInt64Bits(accumulated),
                Is.Not.EqualTo(BitConverter.DoubleToInt64Bits(1.0)),
                "accumulating SecondsPerTick is not exact, which is the reason doc 10 § Clock domains "
                    + "requires game time to come from the integer tick count instead");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-001</c>.
    ///
    /// The rate is exposed from exactly one place: no other type in the simulation assembly
    /// declares a member that restates it. A second statement is how a rate silently forks.
    /// </summary>
    [Test]
    public void TheRateIsDeclaredInExactlyOnePlace()
    {
        List<string> elsewhere = new();
        foreach (Type type in typeof(TickRate).Assembly.GetTypes())
        {
            if (type == typeof(TickRate))
            {
                continue;
            }

            foreach (MemberInfo member in type.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (member is not FieldInfo && member is not PropertyInfo)
                {
                    continue;
                }

                foreach (string reserved in RateBearingMemberNames)
                {
                    if (string.Equals(member.Name, reserved, StringComparison.Ordinal))
                    {
                        elsewhere.Add(type.FullName + "." + member.Name);
                    }
                }
            }
        }

        Assert.That(
            elsewhere,
            Is.Empty,
            "doc 10 § Clock domains makes the tick rate architectural; it is stated once, on TickRate, "
                + "so a second declaration cannot drift from the first");
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-001</c>.
    ///
    /// No API can change the rate after a run has started, because no API can change it at all:
    /// the type is static, has no instance state, and every member is a constant or read-only.
    /// </summary>
    [Test]
    public void NoApiCanChangeTheRate()
    {
        Type rate = typeof(TickRate);
        List<string> mutable = new();
        foreach (MemberInfo member in rate.GetMembers(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (member is FieldInfo field && !field.IsLiteral && !field.IsInitOnly)
            {
                mutable.Add(field.Name);
            }

            if (member is PropertyInfo property && property.CanWrite)
            {
                mutable.Add(property.Name);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                rate.IsAbstract && rate.IsSealed,
                "TickRate is a static class, so there is no instance that could carry a different rate");
            Assert.That(
                rate.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                Is.Empty,
                "a constructor taking a rate is how a per-run rate override gets introduced");
            Assert.That(
                mutable,
                Is.Empty,
                "every tick-rate member is a constant or read-only, so the rate cannot change within a "
                    + "run (doc 10 § Clock domains: \"It is constant within a run\")");
        });
    }
}
