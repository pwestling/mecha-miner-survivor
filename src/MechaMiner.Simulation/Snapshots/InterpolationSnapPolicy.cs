using System;
using System.Globalization;

namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// The snap triggers and the derived distance threshold above which interpolation would be misleading,
/// carrying the derivation rather than a chosen constant.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot: presentation "interpolates
/// transforms between the two most recent complete snapshots but snaps on spawn, teleport, re-entry,
/// terminal transition, or a distance threshold that would make interpolation misleading".
/// </para>
/// <para>
/// <b>The threshold is 0.18 gameplay meters, and it is derived, not tuned.</b>
/// </para>
/// <list type="number">
///   <item><description>
///     The fastest documented authoritative continuous movement in the game is Riftjaw's charge at
///     <b>5.40 M/s</b> (<c>docs/72-player-survivability-and-damage-baseline.md</c> § Movement and Speed
///     Modifiers: "it moves at 180% of base mech speed, or 5.40M/s, for 1.5 seconds"). The fastest
///     player speed is 4.05 M/s and the fastest ordinary enemy is 2.55 M/s, both in the same section, so
///     nothing legal moves faster.
///   </description></item>
///   <item><description>
///     At the 60 Hz authoritative tick rate (<c>docs/technical/10-runtime-architecture.md</c> § Clock
///     domains) the largest legal single-tick displacement is <c>5.40 / 60 = 0.090</c> M.
///   </description></item>
///   <item><description>
///     Presentation must tolerate <b>one dropped snapshot</b> without falsely snapping, because
///     <c>CTR-SIM-003</c> explicitly permits it: "consumer drops stale snapshot or fully rebuilds". Two
///     intervals of the fastest legal movement is <c>0.090 x 2 = 0.180</c> M.
///   </description></item>
///   <item><description>
///     A displacement <em>above</em> that cannot be legal continuous movement across at most two
///     snapshot intervals, so interpolating it would draw motion that never happened - exactly doc 20's
///     "a distance threshold that would make interpolation misleading".
///   </description></item>
/// </list>
/// <para>
/// <b>Cross-checks that the number is not merely arithmetic.</b> The gameplay camera shows 24 gameplay
/// meters vertically (<c>docs/technical/30-presentation-and-rendering.md</c> § Camera), so 0.18 M is
/// 0.75% of camera height - about six pixels at 800 px vertical - and a snap at the threshold is
/// imperceptible, meaning the threshold does not itself cause a visible pop. In the other direction,
/// 0.18 M is 18% of the mech collision diameter, so a real teleport, spawn, or boss re-entry exceeds it
/// by orders of magnitude, and those are separately enumerated in <see cref="InterpolationSnapReason"/>
/// anyway.
/// </para>
/// <para>
/// The comparison is strictly greater than the threshold, so movement at exactly the fastest legal rate
/// across exactly the tolerated number of intervals does not snap. That is what makes the derivation
/// assertable rather than approximately true.
/// </para>
/// </remarks>
public readonly struct InterpolationSnapPolicy : IEquatable<InterpolationSnapPolicy>
{
    /// <summary>
    /// The fastest documented authoritative continuous movement, in gameplay meters per second.
    /// </summary>
    /// <remarks>
    /// Riftjaw's charge, <c>docs/72-player-survivability-and-damage-baseline.md</c> § Movement and Speed
    /// Modifiers. If a later document introduces a faster authoritative movement, this constant moves and
    /// the threshold follows; the threshold is never edited on its own.
    /// </remarks>
    public const double FastestAuthoritativeMetresPerSecond = 5.40;

    /// <summary>The authoritative tick rate, in ticks per second.</summary>
    /// <remarks>
    /// <c>docs/technical/10-runtime-architecture.md</c> § Clock domains. Stated here as the divisor of
    /// this derivation rather than taken from <c>SIM-001</c>'s <c>TickRate</c>, so that this package does
    /// not depend on that package to state its own derivation.
    /// </remarks>
    public const int AuthoritativeTicksPerSecond = 60;

    /// <summary>
    /// How many snapshot intervals of legal movement the policy tolerates before snapping.
    /// </summary>
    /// <remarks>
    /// Two: one for the ordinary interval between the two most recent complete snapshots, and one more
    /// because <c>CTR-SIM-003</c> permits a consumer to drop a stale snapshot.
    /// </remarks>
    public const int ToleratedSnapshotIntervals = 2;

    private readonly bool _isConfigured;

    private InterpolationSnapPolicy(bool isConfigured)
    {
        _isConfigured = isConfigured;
    }

    /// <summary>The policy as derived from the accepted documents. There is no other.</summary>
    /// <remarks>
    /// A single value rather than a constructor with parameters, because every input is derived and none
    /// is a knob. If a document changes, the constants above change and this value follows.
    /// </remarks>
    public static InterpolationSnapPolicy Documented => new(true);

    /// <summary>The largest displacement legal in one tick, in gameplay meters.</summary>
    public static double LargestLegalSingleTickDisplacementMetres =>
        FastestAuthoritativeMetresPerSecond / AuthoritativeTicksPerSecond;

    /// <summary>
    /// The distance threshold, in gameplay meters: the largest legal single-tick displacement across the
    /// tolerated number of intervals.
    /// </summary>
    public static double DistanceThresholdMetres =>
        LargestLegalSingleTickDisplacementMetres * ToleratedSnapshotIntervals;

    /// <summary>True when this value came from <see cref="Documented"/> rather than being defaulted.</summary>
    public bool IsConfigured => _isConfigured;

    /// <summary>
    /// Decides whether presentation snaps, and why.
    /// </summary>
    /// <param name="spawnedSinceOlderSnapshot">The entity did not exist in the older snapshot.</param>
    /// <param name="teleported">An authoritative rule moved the entity discontinuously.</param>
    /// <param name="bossReEntered">A boss re-entered after leaving.</param>
    /// <param name="terminalTransition">The run reached death or extraction.</param>
    /// <param name="displacementMetres">
    /// The straight-line displacement between the two snapshots, in gameplay meters. Must be finite and
    /// not negative. <b>GEO-001:</b> once <c>GEO-001</c> lands this becomes the two planar positions and
    /// the distance is computed by the geometry package, rather than a scalar the caller computed.
    /// </param>
    /// <returns>The highest-precedence reason to snap, or <see cref="InterpolationSnapReason.None"/>.</returns>
    /// <exception cref="InvalidOperationException">This value was defaulted rather than taken from <see cref="Documented"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="displacementMetres"/> is negative or not finite.</exception>
    public InterpolationSnapReason Evaluate(
        bool spawnedSinceOlderSnapshot,
        bool teleported,
        bool bossReEntered,
        bool terminalTransition,
        double displacementMetres)
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException(
                "use InterpolationSnapPolicy.Documented; a defaulted policy has no derivation behind it");
        }

        if (!double.IsFinite(displacementMetres) || displacementMetres < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(displacementMetres),
                displacementMetres,
                "a displacement must be a finite, nonnegative distance in gameplay meters");
        }

        if (spawnedSinceOlderSnapshot)
        {
            return InterpolationSnapReason.Spawn;
        }

        if (teleported)
        {
            return InterpolationSnapReason.Teleport;
        }

        if (bossReEntered)
        {
            return InterpolationSnapReason.BossReEntry;
        }

        if (terminalTransition)
        {
            return InterpolationSnapReason.TerminalTransition;
        }

        return displacementMetres > DistanceThresholdMetres
            ? InterpolationSnapReason.DistanceThresholdExceeded
            : InterpolationSnapReason.None;
    }

    /// <summary>Compares two policies.</summary>
    public static bool operator ==(InterpolationSnapPolicy left, InterpolationSnapPolicy right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two policies for inequality.</summary>
    public static bool operator !=(InterpolationSnapPolicy left, InterpolationSnapPolicy right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(InterpolationSnapPolicy other)
    {
        return _isConfigured == other._isConfigured;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is InterpolationSnapPolicy other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _isConfigured.GetHashCode();
    }

    /// <summary>Renders the policy and its derived threshold as canonical invariant text.</summary>
    public override string ToString()
    {
        return "interpolation-snap threshold="
            + DistanceThresholdMetres.ToString("R", CultureInfo.InvariantCulture)
            + "M from "
            + FastestAuthoritativeMetresPerSecond.ToString("R", CultureInfo.InvariantCulture)
            + "M/s over "
            + ToleratedSnapshotIntervals.ToString(CultureInfo.InvariantCulture)
            + " tick interval(s)";
    }
}
