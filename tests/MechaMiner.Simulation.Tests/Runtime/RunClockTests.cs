using System;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The run clock: it advances only on committed authoritative ticks, and its seconds are the tick
/// index over the exact rational rate.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-001-011</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: the simulation tick advances only
/// when the "Run [is] active and unpaused", and game time is "derived from integer tick count".
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Mutable-state ownership
/// matrix makes the run session the sole writer of run clock state, which is why the clock has no
/// method that advances it by a duration.
/// </remarks>
[TestFixture]
internal sealed class RunClockTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-001-011</c>.
    ///
    /// Run time advances only on committed ticks and equals the tick index divided by 60 exactly for
    /// every tick count tested; any amount of elapsed wall time that produces no tick leaves the run
    /// clock unchanged.
    /// </summary>
    [Test]
    public void RunTimeAdvancesOnlyOnCommittedTicks()
    {
        RecordingWorld world = new();
        SimulationHost host = new(world);

        // Sub-tick steps: elapsed wall time that produces no tick must not move the clock.
        double subTickStep = TickRate.SecondsPerTick / 4.0;
        double[] runSecondsAfterSubTickSteps = new double[3];
        for (int index = 0; index < runSecondsAfterSubTickSteps.Length; index++)
        {
            HostStepResult result = host.Step(subTickStep);
            runSecondsAfterSubTickSteps[index] = host.Clock.RunSeconds;
            Assert.That(
                result.TickCount,
                Is.EqualTo(0),
                "a quarter of a tick interval completes no tick");
        }

        // The fourth quarter completes the first tick, and the clock moves by exactly one interval.
        HostStepResult completesTheFirstTick = host.Step(subTickStep);

        Expect.Multiple(() =>
        {
            foreach (double runSeconds in runSecondsAfterSubTickSteps)
            {
                Assert.That(
                    BitConverter.DoubleToInt64Bits(runSeconds),
                    Is.EqualTo(BitConverter.DoubleToInt64Bits(0.0)),
                    "elapsed wall time that produces no tick leaves the run clock exactly unchanged");
            }

            NumericAssert.AreExactlyEqual(1L, completesTheFirstTick.TickCount, "the fourth quarter completes it");
            Assert.That(
                BitConverter.DoubleToInt64Bits(host.Clock.RunSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(TickRate.SecondsForTicks(1))),
                "and the clock is exactly one tick interval in");
        });

        // Run time equals the tick index over the rational rate exactly, at every tick count tested.
        RunClock clock = new();
        Expect.Multiple(() =>
        {
            for (long committed = 1; committed <= 5_000; committed++)
            {
                clock.CommitTick();
                NumericAssert.AreExactlyEqual(committed, clock.CommittedTickCount, "committed tick count");
                Assert.That(
                    BitConverter.DoubleToInt64Bits(clock.RunSeconds),
                    Is.EqualTo(BitConverter.DoubleToInt64Bits(TickRate.SecondsForTicks(committed))),
                    "run time is the tick index over the exact rational rate, at tick "
                        + clock.CurrentTick.ToString());
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-011</c>.
    ///
    /// The clock has no way to advance except by committing a whole tick, and it refuses to commit one
    /// while the run is blocked or after the terminal boundary.
    /// </summary>
    [Test]
    public void TheClockAdvancesOnlyByWholeTicksAndOnlyWhileUnblocked()
    {
        RunClock blocked = new();
        blocked.Raise(PauseReason.Fabrication);

        Expect.Multiple(() =>
        {
            Expect.Throws<InvalidOperationException>(() => blocked.CommitTick());
            NumericAssert.AreExactlyEqual(0L, blocked.CommittedTickCount, "and nothing was committed");

            foreach (System.Reflection.MethodInfo method in typeof(RunClock).GetMethods(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.DeclaredOnly))
            {
                foreach (System.Reflection.ParameterInfo parameter in method.GetParameters())
                {
                    Assert.That(
                        parameter.ParameterType,
                        Is.Not.EqualTo(typeof(double)),
                        "no run-clock method takes a duration: doc 20 § Scope and invariants says "
                            + "\"simulation time advances only by complete fixed ticks\" - "
                            + method.Name);
                }
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-001-011</c>.
    ///
    /// The 35:00 terminal boundary is an exact integer tick derived from whole minutes, so the boundary
    /// comparison is never a floating-point one.
    /// </summary>
    [Test]
    public void TheTerminalBoundaryIsAnExactIntegerTick()
    {
        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(35L, RunClock.FinalBoundaryMinutes, "the run is 35 minutes long");
            NumericAssert.AreExactlyEqual(
                126_000L,
                RunClock.FinalBoundaryTick.Index,
                "35 * 60 * 60 = 126,000, by exact integer arithmetic against the rational rate");
            Assert.That(
                BitConverter.DoubleToInt64Bits(RunClock.FinalBoundarySeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(2_100.0)),
                "which is exactly 2,100 seconds");
            Assert.That(
                new RunClock().HasReachedFinalBoundary,
                Is.False,
                "a fresh run has not reached it");
        });
    }
}
