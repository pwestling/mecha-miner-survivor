using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The 35:00 boundary ordering: the terminal boundary is evaluated before any event scheduled at or
/// after 35:00 can begin.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-012</c>.
/// </para>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Boundary and tie ordering: "Active ticks cover times
/// strictly before 35:00. After the tick covering the final pre-boundary interval commits, the clock
/// reaches 35:00 and successful extraction is evaluated before any attack, spawn, hazard, or other
/// event scheduled for 35:00 or later can begin."
/// </para>
/// <para>
/// <c>tests/verification/SIM-001.json</c> scopes this deliberately: "VER-SIM-001-012 asserts the
/// ordering contract of the 35:00 boundary against a stub tick target, not real extraction ... The
/// full final-tick death-versus-extraction golden is owned by the packages that own damage and
/// extraction."
/// </para>
/// </remarks>
[TestFixture]
internal sealed class FinalBoundaryOrderingTests
{
    private const string GoldenName = "time-final-boundary-ordering.txt";
    private const string EarlyWaveId = "SCH-0100-WAVE";
    private const string FinalPulseId = "SCH-3459-PULSE";
    private const string BoundaryEventId = "SCH-3500-EXTRACTION";
    private const string AfterBoundaryEventId = "SCH-3500-LATER";

    /// <summary>
    /// Verification: <c>VER-SIM-001-012</c>.
    ///
    /// Active ticks cover only times strictly before 35:00; the boundary is evaluated exactly once,
    /// immediately after the final pre-boundary tick commits; and no event scheduled at or after 35:00
    /// is ever admitted - so the boundary evaluation necessarily precedes every one of them. The whole
    /// ordered call sequence is compared against a committed golden.
    /// </summary>
    [Test]
    public void ExtractionBoundaryIsEvaluatedBeforeAnyEventAtOrAfterThirtyFiveMinutes()
    {
        long boundaryIndex = RunClock.FinalBoundaryTick.Index;
        long finalActiveTickIndex = boundaryIndex - 1;

        RecordingWorld world = new();
        SimulationHost host = new(world);

        // Before the run starts: an ordinary early event is admitted, and both 35:00-or-later events
        // are refused already - long before the boundary is anywhere near.
        Admit(host, world, new SimulationTick(60), EarlyWaveId);
        Admit(host, world, RunClock.FinalBoundaryTick, BoundaryEventId);
        Admit(host, world, new SimulationTick(boundaryIndex + 1), AfterBoundaryEventId);

        // Run to four ticks short of the boundary, in steps of exactly the catch-up bound so no step
        // ever discards anything.
        int bound = host.CatchUpPolicy.MaximumTicksPerStep;
        double stepSeconds = TickRate.SecondsForTicks(bound);
        while (host.Clock.CommittedTickCount < boundaryIndex - bound)
        {
            HostStepResult result = host.Step(stepSeconds);
            if (result.TickCount != bound)
            {
                Assert.Fail(
                    "a step of exactly the catch-up bound must run exactly that many ticks; ran "
                    + result.TickCount.ToString(CultureInfo.InvariantCulture));
            }
        }

        // Immediately before the final step: the last pre-boundary tick still admits an event, and the
        // boundary tick still does not.
        Admit(host, world, new SimulationTick(finalActiveTickIndex), FinalPulseId);
        Admit(host, world, RunClock.FinalBoundaryTick, BoundaryEventId);

        // The final step runs the last four active ticks and then evaluates the boundary.
        HostStepResult finalStep = host.Step(stepSeconds);

        // After the boundary: nothing is admitted at all, at or after 35:00 or before it.
        Admit(host, world, new SimulationTick(finalActiveTickIndex), FinalPulseId);
        Admit(host, world, RunClock.FinalBoundaryTick, BoundaryEventId);

        // A further step cannot run another tick: the terminal transition is blocking.
        HostStepResult afterBoundary = host.Step(stepSeconds);

        world.Append("committed-ticks\t"
            + host.Clock.CommittedTickCount.ToString(CultureInfo.InvariantCulture));
        world.Append("blocking-reasons\t" + host.Clock.BlockingReasons.ToString());

        ImmutableArray<long> advanced = world.AdvancedTicks;

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                boundaryIndex,
                host.Clock.CommittedTickCount,
                "the run commits exactly the ticks strictly before 35:00, so the clock reaches 126,000");
            NumericAssert.AreExactlyEqual(
                boundaryIndex,
                advanced.Length,
                "and the tick target ran once for each of them");
            NumericAssert.AreExactlyEqual(
                finalActiveTickIndex,
                advanced[^1],
                "the last active tick is 125,999: active ticks cover times strictly before 35:00");
            Assert.That(
                advanced,
                Has.None.GreaterThanOrEqualTo(boundaryIndex),
                "no tick at or after the boundary is ever executed");

            NumericAssert.AreExactlyEqual(
                1L,
                world.TerminalBoundaryCallCount,
                "the terminal boundary is evaluated exactly once (doc 20 § Scope and invariants: a run "
                    + "terminal result is assigned once)");
            Assert.That(
                finalStep.TerminalBoundaryEvaluated,
                "and the step that reached it says so");
            Assert.That(
                host.Clock.TerminalBoundaryEvaluated,
                "and the run clock records it");
            Assert.That(
                host.Clock.BlockingReasons,
                Is.EqualTo(PauseReasonSet.Of(PauseReason.TerminalTransition)),
                "reaching the boundary raises the terminal transition, so no later step can run a tick");
            NumericAssert.AreExactlyEqual(0L, afterBoundary.TickCount, "as the following step shows");

            NumericAssert.AreExactlyEqual(
                2L,
                world.ScheduledEventCallCount,
                "exactly the two pre-boundary events were begun; every 35:00-or-later event was refused, "
                    + "so the boundary evaluation precedes all of them");
        });

        GoldenText.Matches(GoldenName, Render(world.Lines));
    }

    /// <summary>
    /// Asks the host to begin a scheduled event, recording a refusal in the same ordered log the world
    /// writes into.
    /// </summary>
    /// <remarks>
    /// An admission needs no line of its own: the world's own
    /// <c>begin-scheduled-event</c> line is the admission, and it is written by the host's call rather
    /// than by this test, which makes it the stronger evidence of the two.
    /// </remarks>
    private static void Admit(
        SimulationHost host,
        RecordingWorld world,
        SimulationTick scheduledTick,
        string scheduleEventId)
    {
        if (host.TryBeginScheduledEvent(scheduledTick, scheduleEventId))
        {
            return;
        }

        world.Append("admission-refused\t" + scheduledTick.ToString() + "\t" + scheduleEventId);
    }

    /// <summary>Renders the ordered call log with the golden's header.</summary>
    private static string Render(ImmutableArray<string> lines)
    {
        StringBuilder rendered = new();
        rendered.Append("# authority: docs/technical/20-simulation-core.md § Boundary and tie ordering\n");
        rendered.Append("#   \"Active ticks cover times strictly before 35:00. After the tick covering the final\n");
        rendered.Append("#   pre-boundary interval commits, the clock reaches 35:00 and successful extraction is\n");
        rendered.Append("#   evaluated before any attack, spawn, hazard, or other event scheduled for 35:00 or\n");
        rendered.Append("#   later can begin.\"\n");
        rendered.Append("#   docs/technical/10-runtime-architecture.md § System phase ordering, phase 2: \"the 35:00\n");
        rendered.Append("#   terminal boundary is handled before another tick can begin.\"\n");
        rendered.Append("#\n");
        rendered.Append("# derived by: reading the two rules above, not by the C# implementation under test. Every\n");
        rendered.Append("#   line below is what those rules require, in the order they require it.\n");
        rendered.Append("#\n");
        rendered.Append("# The 35:00 boundary is tick 35 * 60 * 60 = 126000 by exact integer arithmetic against\n");
        rendered.Append("# the rational 60 Hz rate. A tick with index i covers [i/60, (i+1)/60), so 125999 is the\n");
        rendered.Append("# final pre-boundary tick and 126000 is never executed.\n");
        rendered.Append("#\n");
        rendered.Append("# An event scheduled at or after 126000 is refused at every point in the run, before the\n");
        rendered.Append("# boundary as well as after it. Refusing it only once the boundary had been reached would\n");
        rendered.Append("# leave a window during the final pre-boundary tick in which a 35:00 event could begin,\n");
        rendered.Append("# and doc 20 requires extraction to be evaluated before any of them.\n");
        rendered.Append("#\n");
        rendered.Append("# Consecutive advance-tick calls are collapsed into one range line so the file stays\n");
        rendered.Append("# reviewable; the count column makes the collapse lossless.\n");
        rendered.Append("#\n");
        rendered.Append("# An admitted event appears as the begin-scheduled-event call the host actually made; a\n");
        rendered.Append("# refused one appears as an admission-refused line, because there is no call to show.\n");
        rendered.Append("#\n");
        rendered.Append("# line kinds: begin-scheduled-event | admission-refused |\n");
        rendered.Append("#   advance-tick-range (first, last, count) | evaluate-terminal-boundary\n");
        rendered.Append("#\n");
        rendered.Append("# columns (tab separated): kind, then that kind's fields\n");
        foreach (string line in lines)
        {
            rendered.Append(line).Append('\n');
        }

        return rendered.ToString();
    }
}
