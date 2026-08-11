using System;
using System.Globalization;

namespace MechaMiner.Simulation.Time;

/// <summary>
/// The authoritative 64-bit run time: an integer index of complete fixed ticks, with
/// seconds derived from that index rather than accumulated.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Numeric and unit conventions gives run
/// time as a "64-bit integer simulation tick plus derived seconds", and
/// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains requires game time to
/// be "derived from integer tick count, not accumulated floating-point frame deltas".
/// The index is therefore the authority and <see cref="Seconds"/> is a pure function of
/// it.
/// </para>
/// <para>
/// A tick with index <c>i</c> covers the half-open interval
/// <c>[i / 60, (i + 1) / 60)</c> seconds of run time. The index is the tick's own start
/// time, which is what makes "active ticks cover only times strictly before 35:00"
/// (doc 20 § Boundary and tie ordering) a comparison on integers.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): <c>CMP-PRS-001</c>
/// presentation in <c>game/</c> interpolates between the two most recent snapshots and
/// so must read their tick identity; <c>MechaMiner.Tools</c> stamps scenario and
/// benchmark reports with it; <c>MechaMiner.Game.Tests</c> asserts on it. Hence
/// <c>public</c>.
/// </para>
/// </remarks>
public readonly struct SimulationTick : IEquatable<SimulationTick>, IComparable<SimulationTick>
{
    /// <summary>Creates a tick from its authoritative index.</summary>
    /// <param name="index">The whole number of complete ticks since the run began.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index"/> is negative. Run time never precedes the start of the
    /// run, so a negative index is a defect rather than a representable value.
    /// </exception>
    public SimulationTick(long index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "a simulation tick index is the count of complete ticks since the run began and is "
                + "never negative");
        }

        Index = index;
    }

    /// <summary>The first tick of a run.</summary>
    public static SimulationTick Zero => default;

    /// <summary>The authoritative tick index: the count of complete ticks since the run began.</summary>
    public long Index { get; }

    /// <summary>
    /// The tick's start time in seconds of run time, derived from
    /// <see cref="Index"/> by a single division.
    /// </summary>
    /// <remarks>
    /// Exactly one division, never an accumulation of
    /// <see cref="TickRate.SecondsPerTick"/>: adding that double 126,000 times drifts,
    /// and doc 10 § Clock domains forbids deriving game time from accumulated deltas.
    /// Because the result is a pure function of <see cref="Index"/>, two runs that reach
    /// the same tick through different frame pacing report bit-identical seconds, which
    /// is what <c>VER-SIM-001-004</c> asserts.
    /// </remarks>
    public double Seconds => TickRate.SecondsForTicks(Index);

    /// <summary>Returns the tick immediately after this one.</summary>
    public SimulationTick Next()
    {
        return new SimulationTick(checked(Index + 1));
    }

    /// <summary>Returns the tick <paramref name="tickCount"/> ticks after this one.</summary>
    /// <param name="tickCount">A whole number of ticks to advance by; must not be negative.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tickCount"/> is negative.</exception>
    public SimulationTick Advance(long tickCount)
    {
        if (tickCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tickCount),
                tickCount,
                "run time advances only forwards; doc 20 § Scope and invariants: \"simulation time "
                + "advances only by complete fixed ticks\"");
        }

        return new SimulationTick(checked(Index + tickCount));
    }

    /// <inheritdoc />
    public bool Equals(SimulationTick other)
    {
        return Index == other.Index;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SimulationTick other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    /// <inheritdoc />
    public int CompareTo(SimulationTick other)
    {
        return Index.CompareTo(other.Index);
    }

    /// <summary>Renders the tick index for a diagnostic or golden line.</summary>
    /// <remarks>
    /// Invariant culture, because doc 91 § Determinism and fixture policy requires golden
    /// text to be canonical and a culture-dependent rendering is not.
    /// </remarks>
    public override string ToString()
    {
        return Index.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Compares two ticks for equal index.</summary>
    public static bool operator ==(SimulationTick left, SimulationTick right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two ticks for differing index.</summary>
    public static bool operator !=(SimulationTick left, SimulationTick right)
    {
        return !left.Equals(right);
    }

    /// <summary>Returns whether <paramref name="left"/> precedes <paramref name="right"/>.</summary>
    public static bool operator <(SimulationTick left, SimulationTick right)
    {
        return left.Index < right.Index;
    }

    /// <summary>Returns whether <paramref name="left"/> precedes or equals <paramref name="right"/>.</summary>
    public static bool operator <=(SimulationTick left, SimulationTick right)
    {
        return left.Index <= right.Index;
    }

    /// <summary>Returns whether <paramref name="left"/> follows <paramref name="right"/>.</summary>
    public static bool operator >(SimulationTick left, SimulationTick right)
    {
        return left.Index > right.Index;
    }

    /// <summary>Returns whether <paramref name="left"/> follows or equals <paramref name="right"/>.</summary>
    public static bool operator >=(SimulationTick left, SimulationTick right)
    {
        return left.Index >= right.Index;
    }
}
