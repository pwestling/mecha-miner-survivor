using System;
using System.Globalization;

namespace MechaMiner.Simulation.Entities;

/// <summary>
/// An authoritative entity identity: a run-session fence, a reusable storage index,
/// and a generation.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Entity identity: "An entity ID
/// contains a reusable storage index and a generation. Reusing a slot increments its
/// generation, making stale references invalid", "IDs are unique only within one run
/// session", "The player has a stable reserved ID", "Cross-system references store
/// entity IDs, never direct mutable object references", and "Invalid, expired, or
/// generation-mismatched references fail closed and produce a diagnostic counter".
/// </para>
/// <para>
/// The run-session fence travels inside the identity rather than living only on the
/// allocator. It has to: two runs legitimately reuse the same
/// <see cref="Index"/>/<see cref="Generation"/> pair, so a reference that leaked
/// across a run boundary is indistinguishable from a live one unless the fence is
/// carried with it. The stability doc 20 promises the player is therefore stability of
/// the reserved <em>slot</em> - <see cref="ReservedPlayerIndex"/> and
/// <see cref="FirstGeneration"/> are the same values in every run - not of the fenced
/// value, which must differ per run for the fence to mean anything.
/// </para>
/// <para>
/// This type is part of the public contract surface because
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Cross-boundary
/// contract registry names <c>CMP-PRE-001</c>, <c>CMP-UI-001</c>, and
/// <c>CMP-AUD-001</c> as consumers of <c>CTR-SIM-003</c>, and those live in
/// <c>game/</c>. A snapshot or event batch that carries entity identities cannot be
/// consumed from another assembly if the identity type is internal.
/// </para>
/// <para>
/// This is not a content ID and is never persisted between runs (doc 20 § Entity
/// identity).
/// </para>
/// </remarks>
public readonly struct EntityId : IEquatable<EntityId>
{
    /// <summary>
    /// The storage index that means "explicitly no entity", as distinct from an unset
    /// default.
    /// </summary>
    /// <remarks>
    /// doc 20 § Numeric and unit conventions makes the analogous rule for direction -
    /// "zero direction is explicit" - and § Scope and invariants requires an entity
    /// without a valid position to "carry an explicit non-interacting transitional
    /// state". An event whose subject is the run rather than one entity therefore says
    /// so through <see cref="NoEntityIn"/> instead of through a defaulted field.
    /// </remarks>
    public const int NoEntityIndex = -1;

    /// <summary>The generation a freshly issued slot carries. Zero is never issued.</summary>
    /// <remarks>
    /// Starting at one is what makes <c>default(EntityId)</c> structurally invalid
    /// rather than merely unlikely, which doc 20 § Entity identity requires when it
    /// says invalid references "fail closed".
    /// </remarks>
    public const uint FirstGeneration = 1;

    /// <summary>
    /// The storage index reserved for the player in every run.
    /// </summary>
    /// <remarks>
    /// doc 20 § Entity identity: "The player has a stable reserved ID". doc 20 § Scope
    /// and invariants: "exactly one player entity exists until terminal resolution".
    /// <see cref="PopulationCategory.Player"/> is the first category and has a hard
    /// capacity of one, so its partition is the single slot zero.
    /// </remarks>
    public const int ReservedPlayerIndex = 0;

    private readonly ulong _runSession;
    private readonly int _index;
    private readonly uint _generation;

    private EntityId(ulong runSession, int index, uint generation)
    {
        _runSession = runSession;
        _index = index;
        _generation = generation;
    }

    /// <summary>
    /// The run session this identity belongs to. Zero means no run, so
    /// <c>default(EntityId)</c> can never resolve anywhere.
    /// </summary>
    /// <remarks>doc 20 § Entity identity: "IDs are unique only within one run session."</remarks>
    public ulong RunSession => _runSession;

    /// <summary>The reusable storage index, or <see cref="NoEntityIndex"/>.</summary>
    /// <remarks>doc 20 § Entity identity: "a reusable storage index and a generation".</remarks>
    public int Index => _index;

    /// <summary>The generation of the slot at the moment this identity was issued.</summary>
    /// <remarks>
    /// doc 20 § Entity identity: "Reusing a slot increments its generation, making
    /// stale references invalid."
    /// </remarks>
    public uint Generation => _generation;

    /// <summary>The unusable default: no run, no slot, no generation.</summary>
    /// <remarks>
    /// Returned by every failed allocation so a caller that ignores the boolean result
    /// still holds something that fails closed rather than something that resolves to
    /// slot zero (doc 20 § Entity identity).
    /// </remarks>
    public static EntityId Unset => default;

    /// <summary>True when this identity names no run and can never resolve.</summary>
    public bool IsUnset => _runSession == 0;

    /// <summary>
    /// True when this identity explicitly names no entity within a real run, which is
    /// different from being unset.
    /// </summary>
    public bool IsNoEntity => _runSession != 0 && _index == NoEntityIndex;

    /// <summary>True when this identity was issued by an allocator for a real slot.</summary>
    public bool IsIssued => _runSession != 0 && _index >= 0 && _generation >= FirstGeneration;

    /// <summary>True when this identity is the run's reserved player identity.</summary>
    /// <remarks>doc 20 § Entity identity: "The player has a stable reserved ID."</remarks>
    public bool IsReservedPlayer =>
        IsIssued && _index == ReservedPlayerIndex && _generation == FirstGeneration;

    /// <summary>Creates an identity for a real slot in a real run.</summary>
    /// <param name="runSession">The run session fence. Must not be zero.</param>
    /// <param name="index">The storage index. Must not be negative.</param>
    /// <param name="generation">The slot generation. Must be at least <see cref="FirstGeneration"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Any argument is outside its documented domain.</exception>
    /// <remarks>
    /// Validation is not defensive politeness: doc 20 § Entity identity requires
    /// invalid references to fail closed, and a constructor that accepts a zero
    /// generation would let a defaulted field masquerade as an identity.
    /// </remarks>
    public static EntityId Create(ulong runSession, int index, uint generation)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run', so it can never fence an identity");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfLessThan(generation, FirstGeneration);
        return new EntityId(runSession, index, generation);
    }

    /// <summary>
    /// Creates the explicit "no entity in this run" identity, for a record whose
    /// subject is the run rather than one entity.
    /// </summary>
    /// <param name="runSession">The run session fence. Must not be zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="runSession"/> is zero.</exception>
    public static EntityId NoEntityIn(ulong runSession)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run'");
        }

        return new EntityId(runSession, NoEntityIndex, 0);
    }

    /// <summary>Creates the reserved player identity for one run.</summary>
    /// <param name="runSession">The run session fence. Must not be zero.</param>
    /// <remarks>
    /// The reserved slot and generation are the same in every run; only the fence
    /// differs. doc 20 § Entity identity requires both facts at once.
    /// </remarks>
    public static EntityId ReservedPlayerIn(ulong runSession)
    {
        return Create(runSession, ReservedPlayerIndex, FirstGeneration);
    }

    /// <summary>
    /// The total order used wherever simultaneous outcomes must not depend on
    /// collection or thread timing.
    /// </summary>
    /// <remarks>
    /// doc 20 § Entity identity: "Stable ordering uses the full entity ID after a
    /// system's authored priority keys." "Full" is why all three components take part:
    /// index alone would tie two generations of one recycled slot.
    /// doc 10 § System phase ordering: "Simultaneous outcomes use documented stable
    /// ordering rather than collection or thread timing."
    /// </remarks>
    public static int Compare(EntityId left, EntityId right)
    {
        int bySession = left._runSession.CompareTo(right._runSession);
        if (bySession != 0)
        {
            return bySession;
        }

        int byIndex = left._index.CompareTo(right._index);
        return byIndex != 0 ? byIndex : left._generation.CompareTo(right._generation);
    }

    /// <summary>Compares two identities for exact equality of all three components.</summary>
    public static bool operator ==(EntityId left, EntityId right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two identities for inequality.</summary>
    public static bool operator !=(EntityId left, EntityId right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(EntityId other)
    {
        return _runSession == other._runSession
            && _index == other._index
            && _generation == other._generation;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EntityId other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(_runSession, _index, _generation);
    }

    /// <summary>
    /// Renders the identity as canonical invariant text for goldens and diagnostics.
    /// </summary>
    /// <remarks>
    /// <see cref="CultureInfo.InvariantCulture"/> throughout: doc 91 § Determinism and
    /// fixture policy requires golden output to be canonical, which a culture-sensitive
    /// format is not.
    /// </remarks>
    public override string ToString()
    {
        if (IsUnset)
        {
            return "entity:unset";
        }

        if (IsNoEntity)
        {
            return "entity:none@run"
                + _runSession.ToString(CultureInfo.InvariantCulture);
        }

        return "entity:"
            + _index.ToString(CultureInfo.InvariantCulture)
            + "/g"
            + _generation.ToString(CultureInfo.InvariantCulture)
            + "@run"
            + _runSession.ToString(CultureInfo.InvariantCulture);
    }
}
