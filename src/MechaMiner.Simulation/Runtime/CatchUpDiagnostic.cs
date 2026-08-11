using System;
using System.Globalization;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Runtime;

/// <summary>
/// One occurrence of the catch-up bound being reached: the performance diagnostic doc 10
/// § Clock domains requires.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains: "A bounded catch-up
/// limit prevents an unresponsive spiral after a stall; reaching that bound produces a
/// performance diagnostic."
/// <c>docs/technical/90-performance-diagnostics-and-observability.md</c> § Frame metrics
/// names the metric it must carry: "frame/tick catch-up count and accumulator debt". Both
/// are fields here, so the diagnostic is a record rather than a log line.
/// </para>
/// <para>
/// One record per occurrence, not one per discarded tick: <c>VER-SIM-001-007</c> asserts
/// exactly that, because a per-tick diagnostic would report a worse stall the longer the
/// stall was and so could not be counted.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>MechaMiner.Tools</c> renders
/// these into the <c>PERF-04</c> benchmark report that <c>VER-SIM-001-013</c> gates the
/// provisional catch-up baseline on, and <c>CMP-PRS-001</c> in <c>game/</c> surfaces them in
/// the development overlay. Hence <c>public</c>.
/// </para>
/// </remarks>
public readonly struct CatchUpDiagnostic : IEquatable<CatchUpDiagnostic>
{
    /// <summary>Records one occurrence.</summary>
    /// <param name="tick">The tick the run stood at when the bound was reached.</param>
    /// <param name="executedTickCount">Ticks the bound permitted this step.</param>
    /// <param name="discardedTickCount">Whole ticks of debt discarded.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either count is out of range.</exception>
    public CatchUpDiagnostic(SimulationTick tick, int executedTickCount, int discardedTickCount)
    {
        if (executedTickCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(executedTickCount),
                executedTickCount,
                "a catch-up diagnostic records the ticks the bound permitted, which is never negative");
        }

        if (discardedTickCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discardedTickCount),
                discardedTickCount,
                "the bound is only diagnosed as reached when whole ticks of debt were actually discarded");
        }

        Tick = tick;
        ExecutedTickCount = executedTickCount;
        DiscardedTickCount = discardedTickCount;
    }

    /// <summary>The tick the run stood at when the bound was reached.</summary>
    public SimulationTick Tick { get; }

    /// <summary>
    /// The catch-up count: ticks this step ran, which equals
    /// <see cref="CatchUpPolicy.MaximumTicksPerStep"/> whenever the bound was reached.
    /// </summary>
    public int ExecutedTickCount { get; }

    /// <summary>Whole ticks of accumulator debt that were discarded rather than queued.</summary>
    public int DiscardedTickCount { get; }

    /// <summary>
    /// The accumulator debt in seconds: the discarded whole ticks converted by one division.
    /// </summary>
    public double DebtSeconds => TickRate.SecondsForTicks(DiscardedTickCount);

    /// <inheritdoc />
    public bool Equals(CatchUpDiagnostic other)
    {
        return Tick.Equals(other.Tick)
            && ExecutedTickCount == other.ExecutedTickCount
            && DiscardedTickCount == other.DiscardedTickCount;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CatchUpDiagnostic other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Tick, ExecutedTickCount, DiscardedTickCount);
    }

    /// <summary>Renders the diagnostic as one canonical line.</summary>
    /// <remarks>Invariant culture, so a diagnostic or golden line never depends on locale.</remarks>
    public override string ToString()
    {
        return string.Concat(
            "catch-up-bound-reached tick=",
            Tick.ToString(),
            " executedTicks=",
            ExecutedTickCount.ToString(CultureInfo.InvariantCulture),
            " discardedTicks=",
            DiscardedTickCount.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>Compares two diagnostics for equality in every field.</summary>
    public static bool operator ==(CatchUpDiagnostic left, CatchUpDiagnostic right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two diagnostics for inequality in any field.</summary>
    public static bool operator !=(CatchUpDiagnostic left, CatchUpDiagnostic right)
    {
        return !left.Equals(right);
    }
}
