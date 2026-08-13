using System;
using System.Globalization;

namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// A strictly increasing publication version, so a consumer can order two snapshots without
/// comparing their contents.
/// </summary>
/// <remarks>
/// <para>
/// <c>CTR-SIM-003</c> in
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Cross-boundary contract
/// registry: "immutable latest complete snapshot with run/tick/version; double-buffered", and on
/// failure "consumer drops stale snapshot or fully rebuilds".
/// </para>
/// <para>
/// <b>Separate from the tick, deliberately.</b> A paused transaction publishes a replacement
/// snapshot between ticks (doc 20 § Commands and paused transactions; doc 60 § Fabrication and
/// transactional UI: "After commit, the simulation publishes a new snapshot before the pause reason
/// clears"), so two snapshots can share a tick and still need ordering. A consumer comparing ticks
/// alone would treat the replacement as stale.
/// </para>
/// <para>
/// Public because <c>CTR-SIM-003</c>'s consumers - <c>CMP-PRE-001</c>, <c>CMP-UI-001</c>,
/// <c>CMP-AUD-001</c> - are outside this assembly and the ordering decision is theirs to make.
/// </para>
/// </remarks>
public readonly struct SnapshotVersion : IEquatable<SnapshotVersion>, IComparable<SnapshotVersion>
{
    private readonly long _value;

    private SnapshotVersion(long value)
    {
        _value = value;
    }

    /// <summary>The version no snapshot carries, meaning nothing has been published.</summary>
    public static SnapshotVersion Unpublished => default;

    /// <summary>The version of the first publication.</summary>
    public static SnapshotVersion First => new(1);

    /// <summary>The numeric version.</summary>
    public long Value => _value;

    /// <summary>True when this version belongs to a real publication.</summary>
    public bool IsPublished => _value > 0;

    /// <summary>The next version. Strictly greater, always.</summary>
    /// <exception cref="InvalidOperationException">The version space is exhausted.</exception>
    /// <remarks>
    /// Overflow throws rather than wrapping, for the same reason a generation is retired rather than
    /// wrapped: a wrapped version would make an old snapshot compare as newer, which is precisely
    /// the judgement a consumer relies on this type for. At 60 Hz the space lasts about 4.9 billion
    /// years, so the branch documents intent rather than guarding a plausible event.
    /// </remarks>
    public SnapshotVersion Next()
    {
        if (_value == long.MaxValue)
        {
            throw new InvalidOperationException(
                "the snapshot version space is exhausted; wrapping would make an older snapshot "
                    + "compare as newer, which is the one thing this type exists to prevent");
        }

        return new SnapshotVersion(_value + 1);
    }

    /// <summary>Whether the left version is older than the right.</summary>
    public static bool operator <(SnapshotVersion left, SnapshotVersion right)
    {
        return left._value < right._value;
    }

    /// <summary>Whether the left version is older than or the same as the right.</summary>
    public static bool operator <=(SnapshotVersion left, SnapshotVersion right)
    {
        return left._value <= right._value;
    }

    /// <summary>Whether the left version is newer than the right.</summary>
    public static bool operator >(SnapshotVersion left, SnapshotVersion right)
    {
        return left._value > right._value;
    }

    /// <summary>Whether the left version is newer than or the same as the right.</summary>
    public static bool operator >=(SnapshotVersion left, SnapshotVersion right)
    {
        return left._value >= right._value;
    }

    /// <summary>Whether two versions are the same.</summary>
    public static bool operator ==(SnapshotVersion left, SnapshotVersion right)
    {
        return left._value == right._value;
    }

    /// <summary>Whether two versions differ.</summary>
    public static bool operator !=(SnapshotVersion left, SnapshotVersion right)
    {
        return left._value != right._value;
    }

    /// <inheritdoc/>
    public int CompareTo(SnapshotVersion other)
    {
        return _value.CompareTo(other._value);
    }

    /// <inheritdoc/>
    public bool Equals(SnapshotVersion other)
    {
        return _value == other._value;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is SnapshotVersion other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    /// <summary>Renders the version as canonical invariant text.</summary>
    public override string ToString()
    {
        return IsPublished
            ? "v" + _value.ToString(CultureInfo.InvariantCulture)
            : "v:unpublished";
    }
}
