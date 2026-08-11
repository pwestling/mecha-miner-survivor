namespace MechaMiner.Simulation.Time;

/// <summary>
/// The one place the authoritative simulation frequency is stated.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "The simulation
/// frequency is <b>60 ticks per second</b>. It is constant within a run. Game time is
/// derived from integer tick count, not accumulated floating-point frame deltas."
/// Changing it "is architectural ... it requires measured evidence and a TDR rather
/// than a local optimization", so the rate is exposed as compile-time constants and
/// there is deliberately no setter, no constructor parameter, and no instance of this
/// type anywhere: no API can change the rate after a run has started because no API
/// can change it at all.
/// </para>
/// <para>
/// The rate is published as an <b>exact rational</b>
/// (<see cref="TicksPerSecondNumerator"/> over <see cref="TicksPerSecondDenominator"/>)
/// as well as a <see cref="double"/>. The double <c>1.0 / 60.0</c> is not exactly one
/// sixtieth, so anything that must be exact - schedule boundaries, the 35:00 terminal
/// boundary, the catch-up derivation - uses the rational and integer arithmetic. The
/// double exists only for accumulation and presentation.
/// </para>
/// <para>
/// Cross-boundary consumer (<c>docs/technical/115-component-contract-and-schema-registry.md</c>
/// § Component registry): <c>CMP-RUN-001</c> run session lives in this assembly, but
/// <c>CMP-PRS-001</c> presentation in <c>game/</c>, <c>MechaMiner.Tools</c>, and
/// <c>MechaMiner.Game.Tests</c> all need the tick rate to convert a tick index into
/// seconds for interpolation, scenario reports, and integration assertions. That is why
/// this type is <c>public</c> rather than <c>internal</c>.
/// </para>
/// </remarks>
public static class TickRate
{
    /// <summary>The numerator of the exact rational tick rate, in ticks per second.</summary>
    /// <remarks>doc 10 § Clock domains: "The simulation frequency is 60 ticks per second."</remarks>
    public const int TicksPerSecondNumerator = 60;

    /// <summary>The denominator of the exact rational tick rate, in seconds.</summary>
    /// <remarks>
    /// One, because the rate is a whole number of ticks per second. It is stated
    /// explicitly so that consumers do integer arithmetic against a rational rather than
    /// hard-coding the assumption that the rate is an integer.
    /// </remarks>
    public const int TicksPerSecondDenominator = 1;

    /// <summary>The whole number of authoritative ticks in one second.</summary>
    /// <remarks>
    /// Exact by construction: <see cref="TicksPerSecondDenominator"/> is one, so the
    /// rational reduces to an integer. Used by every integer boundary computation.
    /// </remarks>
    public const int TicksPerSecond = TicksPerSecondNumerator / TicksPerSecondDenominator;

    /// <summary>
    /// The number of ticks in one minute, as an exact integer.
    /// </summary>
    /// <remarks>
    /// Present so that a schedule boundary stated in minutes - most importantly the
    /// 35:00 terminal boundary of <c>docs/technical/20-simulation-core.md</c> § Boundary
    /// and tie ordering - is converted by integer multiplication rather than by
    /// multiplying a <see cref="double"/> seconds value.
    /// </remarks>
    public const int TicksPerMinute = TicksPerSecond * 60;

    /// <summary>
    /// The duration of one tick in seconds, as the single rounded quotient of the exact
    /// rational.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is <b>not</b> exactly one sixtieth of a second and must never be
    /// accumulated to produce game time: doc 10 § Clock domains requires game time to be
    /// "derived from integer tick count, not accumulated floating-point frame deltas".
    /// See <see cref="SimulationTick.Seconds"/>, which divides once instead.
    /// </para>
    /// <para>
    /// It is written as a division of the rational's two terms rather than as the literal
    /// <c>0.0166666666666666666</c> so that the double is provably the correctly rounded
    /// quotient of the stated rate and cannot drift from it by a transcription error.
    /// </para>
    /// </remarks>
    public const double SecondsPerTick = (double)TicksPerSecondDenominator / TicksPerSecondNumerator;

    /// <summary>
    /// Converts a whole tick count to seconds with exactly one division, never by
    /// accumulating <see cref="SecondsPerTick"/>.
    /// </summary>
    /// <param name="tickCount">A whole number of ticks. May be zero or negative.</param>
    /// <returns>
    /// The correctly rounded <see cref="double"/> nearest to
    /// <paramref name="tickCount"/> divided by the exact rational rate.
    /// </returns>
    /// <remarks>
    /// One division means one rounding, so the result depends only on
    /// <paramref name="tickCount"/> and never on the path taken to reach it. That is the
    /// property <c>VER-SIM-001-004</c> compares bit for bit between an irregular and a
    /// uniform frame-delta stream.
    /// </remarks>
    public static double SecondsForTicks(long tickCount)
    {
        return (double)(tickCount * TicksPerSecondDenominator) / TicksPerSecondNumerator;
    }
}
