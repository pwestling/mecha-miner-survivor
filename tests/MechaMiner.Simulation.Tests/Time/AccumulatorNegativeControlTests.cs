using System;
using System.Collections.Immutable;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Time;

/// <summary>
/// The negative control for the two accumulator gates: each assertion is shown failing against
/// a subject that breaks exactly the rule it checks.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-001-008</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence requires a gate to be
/// falsifiable. Without this control, <c>VER-SIM-001-002</c> and <c>VER-SIM-001-005</c> could
/// both be green while asserting nothing - the classic failure where a helper's assertions were
/// deleted, weakened, or never reached. Here the identical helpers
/// (<see cref="AccumulatorContract"/>) that those two gates call are pointed at subjects that
/// are wrong in exactly one way each, and the control passes only if they fail.
/// </para>
/// <para>
/// The subjects are deliberately wrong <i>stubs</i>, not deliberately invalid fixtures: they are
/// valid C# that compiles inside this project, which is what
/// <c>docs/technical/delivery-waves.md</c> requires - an uncompilable fixture may never be
/// committed inside a compiled project.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class AccumulatorNegativeControlTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-001-008</c>.
    ///
    /// A subject that emits a fractional tick fails the whole-tick assertion of
    /// <c>VER-SIM-001-002</c>; a subject that ignores the catch-up bound fails the bound
    /// assertion of <c>VER-SIM-001-005</c>; and each broken subject still passes the assertion it
    /// does not break, so neither control is merely a stub that fails everything.
    /// </summary>
    [Test]
    public void WholeTickAndCatchUpAssertionsFailAgainstDeliberatelyBrokenStubs()
    {
        ImmutableArray<double> stream = FrameDeltaStreams.ShortIrregular();
        long expectedTicks = ExpectedTicks(stream);

        AssertionException fractionalFailure = Expect.Throws<AssertionException>(
            () => AccumulatorContract.AssertOnlyWholeTicksAreYielded(
                new FractionalTickAccumulatorSubject(),
                stream,
                expectedTicks));

        AssertionException unboundedFailure = Expect.Throws<AssertionException>(
            () => AccumulatorContract.AssertCatchUpBoundIsRespected(
                new UnboundedCatchUpAccumulatorSubject()));

        Expect.Multiple(() =>
        {
            Assert.That(
                fractionalFailure.Message,
                Does.Contain("fractional tick"),
                "the whole-tick assertion must fail for the reason it exists, not incidentally");
            Assert.That(
                unboundedFailure.Message,
                Does.Contain("must run exactly the bound"),
                "the bound assertion must fail for the reason it exists");
        });

        // Each broken subject is wrong in exactly one way: it still passes the other gate. If a
        // stub failed both, this control would not distinguish which assertion is load-bearing.
        Expect.DoesNotThrow(
            () => AccumulatorContract.AssertOnlyWholeTicksAreYielded(
                new UnboundedCatchUpAccumulatorSubject(),
                stream,
                expectedTicks));

        // And the real accumulator passes both, which is the positive half of the control.
        Expect.DoesNotThrow(
            () => AccumulatorContract.AssertOnlyWholeTicksAreYielded(
                new FixedStepAccumulatorSubject(),
                stream,
                expectedTicks));
        Expect.DoesNotThrow(
            () => AccumulatorContract.AssertCatchUpBoundIsRespected(new FixedStepAccumulatorSubject()));
    }

    /// <summary>The whole ticks a stream's exact total elapsed time covers.</summary>
    private static long ExpectedTicks(ImmutableArray<double> elapsedSecondsPerStep)
    {
        double total = 0.0;
        foreach (double elapsed in elapsedSecondsPerStep)
        {
            total += elapsed;
        }

        return (long)Math.Floor(total * TickRate.TicksPerSecond);
    }
}
