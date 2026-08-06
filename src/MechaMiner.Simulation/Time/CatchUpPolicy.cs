using System;

namespace MechaMiner.Simulation.Time;

/// <summary>
/// The bounded catch-up limit, carried together with the derivation it comes from so a
/// test can assert the derivation instead of a bare literal.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "A bounded catch-up
/// limit prevents an unresponsive spiral after a stall; reaching that bound produces a
/// performance diagnostic." Doc 10 states that the bound exists but not what it is, and
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> bounds a frame
/// rather than a recovery burst, so the value is derived here.
/// </para>
/// <para>
/// <b>Certainty: provisional baseline.</b> The limit changes observable behaviour under
/// load, so <c>docs/technical/conventions.md</c> § Certainty requires it to name a
/// validation gate: <c>VER-SIM-001-013</c>, <c>./build.sh benchmark PERF-04</c> on
/// <c>linux-x64</c>. A warmed ten-minute capture must report zero catch-up-bound hits and
/// a bounded accumulator debt. If <c>PERF-04</c> reports bound hits the baseline is wrong
/// and is revised through that gate, not by editing this constant.
/// <c>VER-SIM-001-006</c> pins the derivation below as a unit test so the number cannot
/// drift silently before <c>PERF-04</c> can run.
/// </para>
/// <para>
/// <b>Derivation of the default bound, four whole ticks per host step.</b>
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <c>docs/technical/decisions/TDR-003-require-sixty-fps-on-steam-deck.md</c>
/// § Performance contract: "No repeatable active-play stall may exceed 50 milliseconds."
/// 50 ms is therefore the largest stall the accepted performance contract tolerates; a
/// longer one is already a defect by TDR-003, independently of the accumulator.
/// </description>
/// </item>
/// <item>
/// <description>
/// At 60 Hz, 50 ms of accumulated debt is <c>50 * 60 / 1000 = 3</c> ticks exactly. The
/// bound must be at least three, or the accumulator would discard gameplay time during a
/// stall the performance contract explicitly permits.
/// </description>
/// </item>
/// <item>
/// <description>
/// 50 ms lands exactly on the three-tick boundary, so a frame measured <i>at</i> the
/// tolerance can leave a fractional remainder on either side of three. One tick of
/// headroom is added so a conforming frame cannot trip the diagnostic through a rounding
/// remainder: <b>four</b>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Upper-bound check, so the recovery step is not itself a super-tolerance stall. doc 90
/// § Target device frame budget gives a 16.67 ms frame with a 5.00 ms simulation
/// allocation, so the non-simulation portion is 11.67 ms and a four-tick step costs
/// <c>11.67 + 4 * 5.00 = 31.67 ms</c>, below the 50 ms tolerance. Five to seven would also
/// be admissible; eight would not (<c>51.67 ms</c>).
/// </description>
/// </item>
/// <item>
/// <description>
/// Four is the smallest admissible value, which is the right choice: the bound is then
/// reached only when the stall was already a TDR-003 defect, so the performance
/// diagnostic means "something is wrong" rather than "load is high".
/// </description>
/// </item>
/// </list>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-PRS-001</c> in
/// <c>game/</c> constructs the run session and therefore its policy, and
/// <c>MechaMiner.Tools</c> reads the derivation into the <c>PERF-04</c> report. Hence
/// <c>public</c>.
/// </para>
/// </remarks>
public readonly struct CatchUpPolicy : IEquatable<CatchUpPolicy>
{
    /// <summary>
    /// The largest repeatable active-play stall TDR-003 tolerates, in whole milliseconds.
    /// </summary>
    /// <remarks>
    /// TDR-003 § Performance contract states it as "50 milliseconds", a whole number of
    /// milliseconds, so it is held as an integer and converted with integer arithmetic.
    /// Holding it as <c>0.050</c> seconds and multiplying by the rate would make the
    /// three-tick step of the derivation depend on binary rounding.
    /// </remarks>
    public const int ToleratedStallMillisecondsDefault = 50;

    /// <summary>
    /// The one tick of headroom added so a frame at the stall tolerance cannot trip the
    /// bound on a fractional remainder.
    /// </summary>
    public const int HeadroomTicksDefault = 1;

    /// <summary>Milliseconds in one second, used only to convert the stated stall tolerance.</summary>
    private const int MillisecondsPerSecond = 1000;

    private CatchUpPolicy(int toleratedStallMilliseconds, int headroomTicks)
    {
        ToleratedStallMilliseconds = toleratedStallMilliseconds;
        HeadroomTicks = headroomTicks;
    }

    /// <summary>
    /// The accepted provisional baseline: the TDR-003 stall tolerance plus one tick of
    /// headroom, which is four whole ticks per host step at 60 Hz.
    /// </summary>
    public static CatchUpPolicy Default => FromStallTolerance(
        ToleratedStallMillisecondsDefault,
        HeadroomTicksDefault);

    /// <summary>The stall tolerance this policy was derived from, in whole milliseconds.</summary>
    public int ToleratedStallMilliseconds { get; }

    /// <summary>The ticks of headroom added on top of the tolerated stall.</summary>
    public int HeadroomTicks { get; }

    /// <summary>
    /// The tolerated stall expressed in whole ticks, by exact integer arithmetic against
    /// the rational tick rate.
    /// </summary>
    public int ToleratedStallTicks =>
        ToleratedStallMilliseconds * TickRate.TicksPerSecondNumerator
        / (MillisecondsPerSecond * TickRate.TicksPerSecondDenominator);

    /// <summary>
    /// The catch-up bound: the most whole ticks one host step may execute before the
    /// surplus is discarded and diagnosed.
    /// </summary>
    public int MaximumTicksPerStep => ToleratedStallTicks + HeadroomTicks;

    /// <summary>
    /// The tolerated stall in seconds, for a diagnostic or a report. Never used to derive
    /// <see cref="ToleratedStallTicks"/>.
    /// </summary>
    public double ToleratedStallSeconds => (double)ToleratedStallMilliseconds / MillisecondsPerSecond;

    /// <summary>
    /// Derives a policy from a stall tolerance and an explicit headroom, so the bound is
    /// always a stated derivation rather than a chosen number.
    /// </summary>
    /// <param name="toleratedStallMilliseconds">
    /// The largest tolerated repeatable stall, in whole milliseconds. Must be positive and
    /// must be at least one whole tick.
    /// </param>
    /// <param name="headroomTicks">Ticks of headroom; must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The tolerance is not positive, is shorter than one tick, or the headroom is
    /// negative.
    /// </exception>
    public static CatchUpPolicy FromStallTolerance(int toleratedStallMilliseconds, int headroomTicks)
    {
        if (toleratedStallMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(toleratedStallMilliseconds),
                toleratedStallMilliseconds,
                "a stall tolerance is a positive duration; TDR-003 § Performance contract states 50 "
                + "milliseconds");
        }

        if (headroomTicks < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(headroomTicks),
                headroomTicks,
                "headroom is added to the tolerated stall and is never negative");
        }

        int toleratedTicks = toleratedStallMilliseconds * TickRate.TicksPerSecondNumerator
            / (MillisecondsPerSecond * TickRate.TicksPerSecondDenominator);
        if (toleratedTicks + headroomTicks < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(toleratedStallMilliseconds),
                toleratedStallMilliseconds,
                "the derived bound must admit at least one tick per host step, or no run could ever "
                + "advance");
        }

        return new CatchUpPolicy(toleratedStallMilliseconds, headroomTicks);
    }

    /// <inheritdoc />
    public bool Equals(CatchUpPolicy other)
    {
        return ToleratedStallMilliseconds == other.ToleratedStallMilliseconds
            && HeadroomTicks == other.HeadroomTicks;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CatchUpPolicy other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(ToleratedStallMilliseconds, HeadroomTicks);
    }

    /// <summary>Compares two policies for equal derivation inputs.</summary>
    public static bool operator ==(CatchUpPolicy left, CatchUpPolicy right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two policies for differing derivation inputs.</summary>
    public static bool operator !=(CatchUpPolicy left, CatchUpPolicy right)
    {
        return !left.Equals(right);
    }
}
