using System;
using System.Globalization;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// The authoritative record a fixture enemy carries: a planar position and a Hull.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports <c>VER-SIM-007-004</c>, <c>VER-SIM-007-010</c>, <c>VER-SIM-007-011</c>.
/// </para>
/// <para>
/// A stand-in for the record <c>docs/technical/20-simulation-core.md</c> § Authoritative population
/// categories gives an ordinary enemy - "definition, transform, motion, Hull, contact cooldown, control
/// state, spawn tags". The real record belongs to the encounter package; this one carries the minimum a
/// snapshot test needs to detect a mutation.
/// </para>
/// <para>
/// A readonly struct, so <c>PackedEntityStore</c> holds it in one contiguous array and a test that mutates
/// it has to write it back through the store rather than through a reference it happens to hold.
/// </para>
/// </remarks>
internal readonly struct EnemyState : IEquatable<EnemyState>
{
    /// <summary>Creates the record.</summary>
    /// <param name="positionX">The planar X component in gameplay meters. <b>GEO-001</b> in production types; this is a test record.</param>
    /// <param name="positionY">The planar Y component in gameplay meters.</param>
    /// <param name="hull">The Hull, an authoritative integer per doc 20 § Numeric and unit conventions.</param>
    internal EnemyState(double positionX, double positionY, int hull)
    {
        PositionX = positionX;
        PositionY = positionY;
        Hull = hull;
    }

    /// <summary>The planar X component of the record's transform.</summary>
    internal double PositionX { get; }

    /// <summary>The planar Y component of the record's transform.</summary>
    internal double PositionY { get; }

    /// <summary>The record's Hull.</summary>
    internal int Hull { get; }

    /// <inheritdoc/>
    public bool Equals(EnemyState other)
    {
        return PositionX.Equals(other.PositionX)
            && PositionY.Equals(other.PositionY)
            && Hull == other.Hull;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EnemyState other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(PositionX, PositionY, Hull);
    }

    /// <summary>Renders the record as canonical invariant text.</summary>
    public override string ToString()
    {
        return "at=("
            + PositionX.ToString("R", CultureInfo.InvariantCulture)
            + ","
            + PositionY.ToString("R", CultureInfo.InvariantCulture)
            + ") hull="
            + Hull.ToString(CultureInfo.InvariantCulture);
    }
}
