using System;
using System.Globalization;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// A stable event-kind identity, declared by the system that emits it.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "Events
/// carry tick, sequence, stable event kind, relevant entity/content IDs, position, and typed
/// payload", and "Event schemas are versioned when written to diagnostic artifacts."
/// </para>
/// <para>
/// <b>A value type rather than an enumeration, deliberately.</b> doc 20 § Domain and
/// presentation events lists examples - "entity defeated, boss defeated, resource awarded,
/// item installed, threshold crossed, run terminal, and similar outcomes" - not a closed set,
/// and each later authoritative system registers the kinds it emits. An enumeration here would
/// force every later gameplay package to edit a file in this one, which is exactly the
/// shared-contract churn <c>docs/technical/delivery-waves.md</c> is structured to avoid.
/// </para>
/// <para>
/// <b>There is no mutable global registry.</b> doc 20 § Scope and invariants forbids "mutable
/// global services", so a kind is not registered into a process-wide table. Each emitting
/// system declares its kinds as static readonly members on its own type, which makes the
/// declaration reviewable in the package that owns the behaviour and keeps
/// <see cref="StableId"/> uniqueness a content-validation concern rather than a runtime race.
/// </para>
/// <para>
/// Public because <c>CTR-SIM-001</c> and <c>CTR-SIM-002</c> deliver batches to consumers
/// outside this assembly - "other simulation owners and <c>CMP-OBS-001</c>" and
/// "presentation/UI/audio" respectively, per doc 115 § Cross-boundary contract registry.
/// </para>
/// </remarks>
public readonly struct EventKind : IEquatable<EventKind>
{
    private readonly int _stableId;
    private readonly string? _name;

    private EventKind(int stableId, string name)
    {
        _stableId = stableId;
        _name = name;
    }

    /// <summary>
    /// The stable numeric identity, written to diagnostic artifacts and never repurposed.
    /// </summary>
    /// <remarks>
    /// Numeric rather than the name because doc 20 § Domain and presentation events requires
    /// versioned schemas for diagnostic artifacts, and a rename must not invalidate a
    /// recorded artifact.
    /// </remarks>
    public int StableId => _stableId;

    /// <summary>The reviewable name, for logs, goldens, and failure messages.</summary>
    public string Name => _name ?? string.Empty;

    /// <summary>True when this kind was declared rather than defaulted.</summary>
    /// <remarks>
    /// <c>default(EventKind)</c> is never a usable kind, so an event cannot carry a defaulted
    /// kind and be mistaken for a declared one.
    /// </remarks>
    public bool IsDeclared => _stableId > 0 && Name.Length > 0;

    /// <summary>Declares a kind.</summary>
    /// <param name="stableId">The stable numeric identity. Must be positive.</param>
    /// <param name="name">The reviewable name. Must not be blank.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="stableId"/> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public static EventKind Declare(int stableId, string name)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stableId, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new EventKind(stableId, name);
    }

    /// <summary>Orders two kinds by stable identity, then by ordinal name.</summary>
    /// <remarks>
    /// Only a tiebreak of last resort; the batch order is
    /// <c>EventOrdering</c>'s, which never reaches the kind.
    /// </remarks>
    public static int Compare(EventKind left, EventKind right)
    {
        int byId = left._stableId.CompareTo(right._stableId);
        return byId != 0 ? byId : string.CompareOrdinal(left.Name, right.Name);
    }

    /// <summary>Compares two kinds for equality of identity and name.</summary>
    public static bool operator ==(EventKind left, EventKind right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two kinds for inequality.</summary>
    public static bool operator !=(EventKind left, EventKind right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Both components take part, so two declarations that share a stable identity but
    /// disagree on the name are unequal rather than silently aliased.
    /// </remarks>
    public bool Equals(EventKind other)
    {
        return _stableId == other._stableId
            && string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EventKind other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_stableId, StringComparer.Ordinal.GetHashCode(Name));
    }

    /// <summary>Renders the kind as canonical invariant text.</summary>
    public override string ToString()
    {
        return IsDeclared
            ? Name + "#" + _stableId.ToString(CultureInfo.InvariantCulture)
            : "kind:undeclared";
    }
}
