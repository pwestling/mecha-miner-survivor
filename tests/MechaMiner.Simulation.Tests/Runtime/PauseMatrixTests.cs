using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Tests.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The pause matrix: no tick while any reason is present, resumption only when all are cleared, the
/// UI clock still advancing, and a pause consuming no gameplay time at all.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-002-002</c>, <c>VER-SIM-002-003</c>, <c>VER-SIM-002-007</c>,
/// <c>VER-SIM-002-009</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract and § Clock domains.
/// </remarks>
[TestFixture]
internal sealed class PauseMatrixTests
{
    private const string GoldenName = "runtime-pause-boundary-tick-sequence.txt";

    /// <summary>
    /// Verification: <c>VER-SIM-002-002</c>.
    ///
    /// For each of the seven reasons alone, an arbitrarily long host step runs zero ticks and leaves
    /// the tick index and run clock unchanged.
    /// </summary>
    [Test]
    public void NoTickExecutesWhileAnySingleReasonIsPresent()
    {
        // The same assertion the negative control VER-SIM-002-010 proves can fail.
        PauseContract.AssertNoTickExecutesWhileAnySingleReasonIsPresent(() => new HostPausableRun());

        // Plus the facts the seam does not expose: the run clock and the committed state.
        Expect.Multiple(() =>
        {
            foreach (PauseReason reason in PauseReasonSet.AllReasons)
            {
                RecordingWorld world = new();
                SimulationHost host = new(world);
                host.Clock.Raise(reason);

                HostStepResult result = host.Step(10.0);

                Assert.That(
                    result.TickCount,
                    Is.EqualTo(0),
                    "no tick executes while " + reason.ToString() + " is present");
                Assert.That(
                    result.WasBlocked,
                    "the step reports itself blocked, so the caller can tell a pause from a short frame");
                Assert.That(
                    world.AdvanceTickCallCount,
                    Is.EqualTo(0),
                    "the tick target is never called while " + reason.ToString() + " is present");
                Assert.That(
                    BitConverter.DoubleToInt64Bits(host.Clock.RunSeconds),
                    Is.EqualTo(BitConverter.DoubleToInt64Bits(0.0)),
                    "run time is unchanged; doc 10 § Pause contract: \"Run time ... remain[s] unchanged\"");
                NumericAssert.AreExactlyEqual(
                    0L,
                    host.Clock.CommittedTickCount,
                    "and so is the tick index");
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-003</c>.
    ///
    /// Exhaustively over all 128 subsets of the seven reasons, in both entry and exit orders: the run
    /// is blocking if and only if the set is non-empty, clearing a proper subset leaves it blocking,
    /// and it resumes only when the last clearable reason is cleared.
    /// </summary>
    [Test]
    public void ResumesOnlyWhenEveryOverlappingReasonIsCleared()
    {
        // The same assertion the negative control VER-SIM-002-010 proves can fail.
        PauseContract.AssertResumesOnlyWhenEveryReasonIsCleared(() => new HostPausableRun());
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-007</c>.
    ///
    /// The UI clock continues while the run is blocked: a zero-tick step still reports its elapsed UI
    /// seconds, and the host's UI clock total advances while no gameplay clock does.
    /// </summary>
    [Test]
    public void UiClockAdvancesWhileNoGameplayClockDoes()
    {
        RecordingWorld world = new();
        SimulationHost host = new(world);
        host.Clock.Raise(PauseReason.GeneralPause);

        double[] blockedSteps = { 0.25, 0.125, 1.0 };
        List<double> reportedUiSeconds = new();
        foreach (double elapsed in blockedSteps)
        {
            HostStepResult result = host.Step(elapsed);
            reportedUiSeconds.Add(result.ElapsedUiSeconds);
            Assert.That(result.TickCount, Is.EqualTo(0), "no gameplay clock advances while blocked");
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                reportedUiSeconds,
                Is.EqualTo(blockedSteps).AsCollection,
                "every blocked step reports its own elapsed UI seconds, so pause presentation can "
                    + "animate (doc 10 § Pause contract)");
            Assert.That(
                BitConverter.DoubleToInt64Bits(host.UiClockSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(0.25 + 0.125 + 1.0)),
                "the UI clock total is the sum of what the caller reported");
            NumericAssert.AreExactlyEqual(0L, host.Clock.CommittedTickCount, "the tick index is unchanged");
            Assert.That(
                BitConverter.DoubleToInt64Bits(host.Clock.RunSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(0.0)),
                "and so is run time: the two are different clock domains (doc 10 § Clock domains)");
            Assert.That(
                world.AdvanceTickCallCount,
                Is.EqualTo(0),
                "and the simulation was never advanced");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-009</c>.
    ///
    /// A pause consumes no gameplay time: a run paused and resumed mid-stream produces exactly the
    /// same tick sequence, tick count, and run clock as an unpaused run fed the same unpaused elapsed
    /// seconds, compared as canonical ordered text against a committed golden.
    /// </summary>
    [Test]
    public void PauseBoundaryConsumesNoGameplayTime()
    {
        ImmutableArray<double> deltas = FrameDeltaStreams.ShortIrregular();

        RunTranscript unpaused = RunUnpaused(deltas);
        RunTranscript paused = RunWithAPauseAfterTheEighthStep(deltas);

        Expect.Multiple(() =>
        {
            Assert.That(
                paused.CanonicalText,
                Is.EqualTo(unpaused.CanonicalText).Using(StringComparer.Ordinal),
                "a pause consumes no gameplay time, so the tick sequence is identical");
            NumericAssert.AreExactlyEqual(
                unpaused.CommittedTicks,
                paused.CommittedTicks,
                "and so is the committed tick count");
            Assert.That(
                BitConverter.DoubleToInt64Bits(paused.RunSeconds),
                Is.EqualTo(BitConverter.DoubleToInt64Bits(unpaused.RunSeconds)),
                "and so is the run clock, bit for bit");
            Assert.That(
                paused.AdvancedTicks,
                Is.EqualTo(unpaused.AdvancedTicks).AsCollection,
                "and so is the exact sequence of tick indices the tick target saw");
            Assert.That(
                paused.UiClockSeconds,
                Is.GreaterThan(unpaused.UiClockSeconds),
                "while the paused run's UI clock did advance further, because the UI clock runs during a "
                    + "pause (doc 10 § Pause contract)");
        });

        GoldenText.Matches(GoldenName, unpaused.CanonicalText);
        GoldenText.Matches(GoldenName, paused.CanonicalText);
    }

    private static RunTranscript RunUnpaused(ImmutableArray<double> deltas)
    {
        RecordingWorld world = new();
        SimulationHost host = new(world);
        List<HostStepResult> steps = new();
        foreach (double elapsed in deltas)
        {
            steps.Add(host.Step(elapsed));
        }

        return RunTranscript.From(host, world, steps);
    }

    private static RunTranscript RunWithAPauseAfterTheEighthStep(ImmutableArray<double> deltas)
    {
        const int stepsBeforeThePause = 8;

        // The blocked interval deliberately does not add up to a whole number of ticks: 11/128 s is
        // 5.15625 ticks. A whole-tick blocked interval would hide the very defect this gate exists to
        // catch - an implementation that banks paused wall time into the accumulator would then have
        // its debt cancelled exactly by the whole ticks it drew back out, and the tick sequence would
        // be identical anyway. With a fractional total, banking shifts the retained remainder and the
        // sequence diverges. (Confirmed by the negative control for this gate.)
        double[] blockedSteps =
        {
            1.0 * FrameDeltaStreams.DyadicGranularitySeconds,
            3.0 * FrameDeltaStreams.DyadicGranularitySeconds,
            2.0 * FrameDeltaStreams.DyadicGranularitySeconds,
            5.0 * FrameDeltaStreams.DyadicGranularitySeconds,
        };

        RecordingWorld world = new();
        SimulationHost host = new(world);
        List<HostStepResult> steps = new();

        for (int index = 0; index < stepsBeforeThePause; index++)
        {
            steps.Add(host.Step(deltas[index]));
        }

        host.Clock.Raise(PauseReason.GeneralPause);
        foreach (double blocked in blockedSteps)
        {
            HostStepResult result = host.Step(blocked);
            steps.Add(result);
            Assert.That(result.TickCount, Is.EqualTo(0), "a blocked step runs no tick");
        }

        host.Clock.Clear(PauseReason.GeneralPause);

        for (int index = stepsBeforeThePause; index < deltas.Length; index++)
        {
            steps.Add(host.Step(deltas[index]));
        }

        return RunTranscript.From(host, world, steps);
    }

    /// <summary>What one run of the fixture produced, rendered as canonical ordered text.</summary>
    private sealed class RunTranscript
    {
        private RunTranscript(
            string canonicalText,
            long committedTicks,
            double runSeconds,
            double uiClockSeconds,
            ImmutableArray<long> advancedTicks)
        {
            CanonicalText = canonicalText;
            CommittedTicks = committedTicks;
            RunSeconds = runSeconds;
            UiClockSeconds = uiClockSeconds;
            AdvancedTicks = advancedTicks;
        }

        internal string CanonicalText { get; }

        internal long CommittedTicks { get; }

        internal double RunSeconds { get; }

        internal double UiClockSeconds { get; }

        internal ImmutableArray<long> AdvancedTicks { get; }

        /// <summary>
        /// Renders one run. Only steps that executed at least one tick appear, so a zero-tick step -
        /// whether zero because the interval was short or because the run was blocked - cannot change
        /// the text. That is what makes the paused and unpaused runs comparable at all.
        /// </summary>
        internal static RunTranscript From(
            SimulationHost host,
            RecordingWorld world,
            IReadOnlyList<HostStepResult> steps)
        {
            List<string> batches = new();
            foreach (HostStepResult step in steps)
            {
                if (step.TickCount == 0)
                {
                    continue;
                }

                batches.Add(string.Concat(
                    (batches.Count + 1).ToString(CultureInfo.InvariantCulture),
                    "\t",
                    step.TickCount.ToString(CultureInfo.InvariantCulture),
                    "\t",
                    step.FirstTick.ToString(),
                    "\t",
                    step.LastTick.ToString()));
            }

            StringBuilder rendered = new();
            rendered.Append("# authority: docs/technical/10-runtime-architecture.md § Pause contract\n");
            rendered.Append("#   \"The simulation executes no ticks while any blocking reason is present.\"\n");
            rendered.Append("#   \"Run time, AI, movement, spawning, projectiles, attacks, cooldowns, status effects,\n");
            rendered.Append("#   mining progress and decay, hazards, pickups, and gameplay physics remain unchanged.\"\n");
            rendered.Append("#   docs/technical/20-simulation-core.md § Verification.\n");
            rendered.Append("#\n");
            rendered.Append("# derived by: an independent Python reference implementation of the fixed-step\n");
            rendered.Append("#   accumulator, written from doc 10 § Clock domains, not by the C# under test.\n");
            rendered.Append("#\n");
            rendered.Append("# A pause consumes no gameplay time. Two runs are fed the same 24 frame deltas:\n");
            rendered.Append("# one unpaused throughout, one that raises GeneralPause after the 8th delta, is fed four\n");
            rendered.Append("# further steps totalling 11/128 s while blocked, and then clears the pause and continues. Both\n");
            rendered.Append("# must render exactly this sequence: the paused steps produce no batch at all, so they\n");
            rendered.Append("# cannot appear here, and the blocked wall time is never banked into a later step.\n");
            rendered.Append("#\n");
            rendered.Append("# Frame deltas are exact binary fractions (multiples of 1/128 s) so that the sequence is\n");
            rendered.Append("# a property of the accumulator rather than of decimal rounding in the fixture.\n");
            rendered.Append("# The blocked interval is deliberately not a whole number of ticks (11/128 s = 5.15625), so\n");
            rendered.Append("# an implementation that banked paused wall time could not have its debt cancelled exactly.\n");
            rendered.Append("#\n");
            rendered.Append("# Only steps that executed at least one tick appear, so a zero-tick step - whether it is\n");
            rendered.Append("# zero because the interval was short or because the run was blocked - cannot change the\n");
            rendered.Append("# text.\n");
            rendered.Append("#\n");
            rendered.Append("# columns (tab separated): batchOrdinal, ticksExecuted, firstTick, lastTick\n");
            rendered.Append("# entries=").Append(batches.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (string batch in batches)
            {
                rendered.Append(batch).Append('\n');
            }

            rendered
                .Append("committed-ticks\t")
                .Append(host.Clock.CommittedTickCount.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
            rendered
                .Append("run-seconds-ieee754\t0x")
                .Append(BitConverter.DoubleToUInt64Bits(host.Clock.RunSeconds)
                    .ToString("X16", CultureInfo.InvariantCulture))
                .Append('\n');

            return new RunTranscript(
                rendered.ToString(),
                host.Clock.CommittedTickCount,
                host.Clock.RunSeconds,
                host.UiClockSeconds,
                world.AdvancedTicks);
        }
    }
}
