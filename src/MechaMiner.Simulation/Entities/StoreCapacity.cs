using System;
using System.Globalization;

namespace MechaMiner.Simulation.Entities;

/// <summary>
/// One store's soft target, documented margin, computed hard capacity, overflow
/// behaviour, and the authority its numbers came from.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Capacity and overload behavior: "Every
/// dynamic store has a documented soft target, hard capacity, and overflow behavior",
/// and "Initial capacities are derived from the encounter schedule plus a documented
/// margin ... rather than selected as arbitrary powers of two."
/// </para>
/// <para>
/// <see cref="HardCapacity"/> is <em>computed</em> from <see cref="SoftTarget"/> plus
/// <see cref="Margin"/> and is not storable. A struct holding two independent numbers
/// cannot be tested against its own derivation - a hand-edited hard capacity would pass
/// every assertion - whereas a computed one makes the derivation the only way to obtain
/// the number.
/// </para>
/// <para>
/// The margin rule for the authored-enemy stores: hard capacity equals the soft target
/// plus one largest authored single materialization. The director already caps
/// materialization at "the lesser of its batch size, current deficit, available ceiling,
/// and valid positions"
/// (<c>docs/technical/23-encounter-director-and-enemy-runtime.md</c> § Population
/// classes), so the store is never the limiter in correct operation. The margin exists so
/// that a director accounting bug trips the invariant with the offending batch resident
/// and inspectable, which is what doc 20 § Capacity and overload behavior means by "a
/// failed invariant caught by content validation or stress testing, not a runtime
/// balancing tool".
/// </para>
/// </remarks>
public readonly struct StoreCapacity : IEquatable<StoreCapacity>
{
    private readonly int _softTarget;
    private readonly int _margin;
    private readonly OverflowBehaviour _overflow;
    private readonly CapacityAuthority _authority;
    private readonly string? _marginBasis;
    private readonly string? _derivation;
    private readonly string? _weakSourceReason;

    private StoreCapacity(
        int softTarget,
        int margin,
        OverflowBehaviour overflow,
        CapacityAuthority authority,
        string marginBasis,
        string derivation,
        string weakSourceReason)
    {
        _softTarget = softTarget;
        _margin = margin;
        _overflow = overflow;
        _authority = authority;
        _marginBasis = marginBasis;
        _derivation = derivation;
        _weakSourceReason = weakSourceReason;
    }

    /// <summary>
    /// The population the store is expected to hold in correct operation. At or above it
    /// the store reports pressure.
    /// </summary>
    public int SoftTarget => _softTarget;

    /// <summary>
    /// The headroom above the soft target, in records, with its basis named by
    /// <see cref="MarginBasis"/>.
    /// </summary>
    public int Margin => _margin;

    /// <summary>
    /// The hard capacity: <see cref="SoftTarget"/> plus <see cref="Margin"/>. Computed,
    /// never stored.
    /// </summary>
    public int HardCapacity => _softTarget + _margin;

    /// <summary>What the store does at <see cref="HardCapacity"/>.</summary>
    public OverflowBehaviour Overflow => _overflow;

    /// <summary>Where the numbers came from, and therefore what makes them stale.</summary>
    public CapacityAuthority Authority => _authority;

    /// <summary>
    /// The single authored quantity the margin equals, named so a reader can check it
    /// against its source.
    /// </summary>
    /// <remarks>Empty when <see cref="Margin"/> is zero, which the factories require to be deliberate.</remarks>
    public string MarginBasis => _marginBasis ?? string.Empty;

    /// <summary>The cited derivation of the soft target.</summary>
    public string Derivation => _derivation ?? string.Empty;

    /// <summary>
    /// Why this row rests on an assumption rather than a stated figure, or empty when it
    /// does not.
    /// </summary>
    public string WeakSourceReason => _weakSourceReason ?? string.Empty;

    /// <summary>
    /// True when a document input this row needs does not exist yet, so the number is the
    /// smallest defensible basis rather than a stated bound.
    /// </summary>
    /// <remarks>
    /// <c>docs/technical/conventions.md</c> § Certainty: a provisional baseline states
    /// that it is provisional and names what would settle it.
    /// </remarks>
    public bool IsWeaklySourced => WeakSourceReason.Length > 0;

    /// <summary>True when this store is sized exactly from the validated map manifest.</summary>
    public bool IsManifestSized => _authority == CapacityAuthority.MapManifest;

    /// <summary>
    /// A capacity with a soft target and a nonzero margin whose basis is one authored
    /// quantity.
    /// </summary>
    /// <param name="softTarget">The expected population in correct operation.</param>
    /// <param name="margin">The headroom above it. Must be positive.</param>
    /// <param name="overflow">What happens at hard capacity.</param>
    /// <param name="authority">Where the numbers came from.</param>
    /// <param name="marginBasis">The authored quantity <paramref name="margin"/> equals.</param>
    /// <param name="derivation">The cited derivation of <paramref name="softTarget"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A number is outside its domain.</exception>
    /// <exception cref="ArgumentException">A citation is missing.</exception>
    public static StoreCapacity WithMargin(
        int softTarget,
        int margin,
        OverflowBehaviour overflow,
        CapacityAuthority authority,
        string marginBasis,
        string derivation)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(softTarget, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(margin, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(marginBasis);
        ArgumentException.ThrowIfNullOrWhiteSpace(derivation);
        return new StoreCapacity(
            softTarget,
            margin,
            overflow,
            authority,
            marginBasis,
            derivation,
            string.Empty);
    }

    /// <summary>
    /// A capacity over a closed authored set or a stated ceiling, where no margin is
    /// defensible.
    /// </summary>
    /// <param name="hardCapacity">The stated capacity; the soft target equals it.</param>
    /// <param name="overflow">What happens at hard capacity.</param>
    /// <param name="authority">Where the number came from.</param>
    /// <param name="derivation">The cited derivation or citation.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="hardCapacity"/> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="derivation"/> is missing.</exception>
    /// <remarks>
    /// A closed authored set admits no margin: doc 32 § Complete 35-minute schedule says
    /// outright that there is no fifth boss, and a stated safety ceiling in doc 22 §
    /// Performance and capacity is already "intentionally above current gameplay maxima",
    /// so adding headroom to it would invent a number doc 22 does not state.
    /// </remarks>
    public static StoreCapacity WithoutMargin(
        int hardCapacity,
        OverflowBehaviour overflow,
        CapacityAuthority authority,
        string derivation)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(hardCapacity, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(derivation);
        return new StoreCapacity(
            hardCapacity,
            0,
            overflow,
            authority,
            string.Empty,
            derivation,
            string.Empty);
    }

    /// <summary>
    /// A capacity sized exactly from the validated map manifest, which cannot change
    /// during a run.
    /// </summary>
    /// <param name="manifestCount">The count the manifest declares.</param>
    /// <param name="derivation">The cited manifest rule.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="manifestCount"/> is negative.</exception>
    /// <exception cref="ArgumentException"><paramref name="derivation"/> is missing.</exception>
    /// <remarks>
    /// A count fixed at generation needs no margin: nothing can materialize a
    /// thirteenth relic cache mid-run, so headroom would only hide a manifest defect.
    /// </remarks>
    public static StoreCapacity FromManifest(int manifestCount, string derivation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(manifestCount);
        ArgumentException.ThrowIfNullOrWhiteSpace(derivation);
        return new StoreCapacity(
            manifestCount,
            0,
            OverflowBehaviour.FailInvariant,
            CapacityAuthority.MapManifest,
            string.Empty,
            derivation,
            string.Empty);
    }

    /// <summary>
    /// Returns this capacity marked as resting on an assumption, with the missing input
    /// named.
    /// </summary>
    /// <param name="reason">Which document input is missing, and what would settle it.</param>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is missing.</exception>
    public StoreCapacity AsWeaklySourced(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new StoreCapacity(
            _softTarget,
            _margin,
            _overflow,
            _authority,
            MarginBasis,
            Derivation,
            reason);
    }

    /// <summary>Compares two capacities for equality of every declared component.</summary>
    public static bool operator ==(StoreCapacity left, StoreCapacity right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two capacities for inequality.</summary>
    public static bool operator !=(StoreCapacity left, StoreCapacity right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(StoreCapacity other)
    {
        return _softTarget == other._softTarget
            && _margin == other._margin
            && _overflow == other._overflow
            && _authority == other._authority
            && string.Equals(MarginBasis, other.MarginBasis, StringComparison.Ordinal)
            && string.Equals(Derivation, other.Derivation, StringComparison.Ordinal)
            && string.Equals(WeakSourceReason, other.WeakSourceReason, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is StoreCapacity other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _softTarget,
            _margin,
            _overflow,
            _authority,
            StringComparer.Ordinal.GetHashCode(MarginBasis),
            StringComparer.Ordinal.GetHashCode(Derivation),
            StringComparer.Ordinal.GetHashCode(WeakSourceReason));
    }

    /// <summary>Renders the capacity as canonical invariant text for evidence and goldens.</summary>
    public override string ToString()
    {
        return "soft="
            + _softTarget.ToString(CultureInfo.InvariantCulture)
            + " margin="
            + _margin.ToString(CultureInfo.InvariantCulture)
            + " hard="
            + HardCapacity.ToString(CultureInfo.InvariantCulture)
            + " overflow="
            + _overflow.ToString()
            + " authority="
            + _authority.ToString()
            + (IsWeaklySourced ? " weakly-sourced" : string.Empty);
    }
}
