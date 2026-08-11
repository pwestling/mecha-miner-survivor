using System;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// The conversions from raw 32-bit draws to bounded integers, to a <c>[0,1)</c> double, and to
/// an integer-ratio chance.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// bounded-integer rule: "Unbiased bounded integers use rejection sampling rather than modulo
/// reduction"; the conversion rules: "A
/// <c>[0,1)</c> double is built from 53 random bits under one golden-tested conversion; a
/// chance that can be represented as an integer ratio compares integers instead."
/// </para>
/// <para>
/// Doc 20 constrains but does not fully determine two details. Both are fixed in the golden
/// headers rather than here, because four downstream streams depend on them:
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-bounded-conversion.txt</c> pins the
/// rejection threshold as <c>(2^32 - bound) mod bound</c> (canonical
/// <c>pcg32_boundedrand_r</c>), and <c>random-unit-double-conversion.txt</c> pins the 53-bit
/// layout as <c>((hi &lt;&lt; 32) | lo) &gt;&gt; 11</c> scaled by <c>2^-53</c> with the
/// <em>first</em> draw as the high half. Changing either is a golden change under doc 91 §
/// Determinism and fixture policy, not a local choice.
/// </para>
/// <para>
/// There is exactly one <c>[0,1)</c> conversion — <see cref="UnitDouble"/>. Everything else
/// that yields a double delegates to it, so "one golden-tested conversion" is a property of the
/// code and not of a comment (<c>VER-SIM-005-006</c>).
/// </para>
/// </remarks>
public static class BoundedRandom
{
    /// <summary><c>2^53</c>, the divisor of the 53-bit unit-interval conversion.</summary>
    internal const double UnitDoubleScale = 9007199254740992.0;

    /// <summary>The number of low bits the 53-bit conversion discards from its 64-bit
    /// pair.</summary>
    internal const int DiscardedLowBits = 11;

    /// <summary>
    /// The most consecutive rejections <see cref="NextBounded"/> tolerates before it reports a
    /// source that is not advancing, rather than looping forever.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This cannot make a correct run flaky, because <see cref="RejectionThreshold"/> accepts
    /// strictly more than half of the <c>2^32</c> possible draws for every bound. For a bound above
    /// <c>2^31</c> the threshold is exactly <c>2^32 - bound</c>, leaving <c>bound</c> values
    /// accepted, and <c>bound &gt; 2^31</c>. For a bound at or below <c>2^31</c> the threshold is
    /// strictly below the bound and therefore below <c>2^31</c>, leaving more than <c>2^31</c>
    /// accepted. So each draw is accepted with probability above one half, and this many
    /// consecutive rejections from a genuinely advancing stream has probability below
    /// <c>2^-256</c>.
    /// </para>
    /// <para>
    /// The only way to reach the bound is a source whose draws do not change, which is what a
    /// forked <see cref="Pcg32"/> behaves like: a copy replays one value forever. Before this
    /// bound existed that defect was reported by the run never finishing, and a check that fails
    /// identically whether the property was violated or the machine died trains people to re-run
    /// it. <c>VER-SIM-005-011</c> is the gate for the fork itself.
    /// </para>
    /// </remarks>
    internal const int MaxConsecutiveRejections = 256;

    /// <summary>
    /// The rejection threshold of doc 20 § Authoritative random-number contract:
    /// <c>(2^32 - bound) mod bound</c>. A draw strictly below it is rejected.
    /// </summary>
    /// <param name="bound">The exclusive upper bound, at least one.</param>
    /// <returns>The threshold. Zero when <paramref name="bound"/> divides
    /// <c>2^32</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bound"/> is
    /// zero.</exception>
    /// <remarks>
    /// Exactly <c>2^32 - threshold</c> values are accepted and that count is an exact multiple
    /// of <paramref name="bound"/>, so every residue is produced by an equal number of accepted
    /// draws. That is why <c>result = draw mod bound</c> below is unbiased while
    /// <c>draw mod bound</c> on its own is not.
    /// </remarks>
    public static uint RejectionThreshold(uint bound)
    {
        ThrowIfZero(bound, nameof(bound));
        return unchecked(0U - bound) % bound;
    }

    /// <summary>
    /// Draws an unbiased integer in <c>[0, bound)</c> by rejection sampling (doc 20 §
    /// Authoritative random-number contract).
    /// </summary>
    /// <param name="source">The stream to draw from.</param>
    /// <param name="bound">The exclusive upper bound, at least one.</param>
    /// <returns>The bounded value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bound"/> is
    /// zero.</exception>
    /// <remarks>
    /// <paramref name="bound"/> of one gives a threshold of zero, so the first draw is always
    /// accepted and exactly one draw is consumed. The no-draw rule of doc 20 § Authoritative
    /// random-number contract is a property of <em>selection</em>, not of this primitive — see
    /// <see cref="CanonicalSelection"/>.
    /// </remarks>
    public static uint NextBounded(IRandomSource source, uint bound)
    {
        ArgumentNullException.ThrowIfNull(source);
        uint threshold = RejectionThreshold(bound);
        uint lastRejected = 0U;
        for (int rejections = 0; rejections < MaxConsecutiveRejections; rejections++)
        {
            uint draw = source.NextUInt32();
            if (draw >= threshold)
            {
                return draw % bound;
            }

            lastRejected = draw;
        }

        throw new InvalidOperationException(
            "rejection sampling rejected "
                + MaxConsecutiveRejections.ToString(CultureInfo.InvariantCulture)
                + " consecutive draws for bound "
                + bound.ToString(CultureInfo.InvariantCulture)
                + " against threshold "
                + threshold.ToString(CultureInfo.InvariantCulture)
                + "; the last rejected draw was 0x"
                + lastRejected.ToString("X8", CultureInfo.InvariantCulture)
                + " after " + source.DrawCount.ToString(CultureInfo.InvariantCulture)
                + " draws from " + source.ToString()
                + ". More than half of all draws are accepted for every bound, so this is a source "
                + "that is not advancing rather than an unlucky run: the stream has forked and every "
                + "draw is a copy of one value (doc 20 § Authoritative random-number contract; "
                + "VER-SIM-005-011).");
    }

    /// <summary>
    /// The single <c>[0,1)</c> conversion of doc 20 § Authoritative random-number contract: the
    /// top 53 bits of the 64-bit pair, scaled by <c>2^-53</c>.
    /// </summary>
    /// <param name="high">The first draw, which forms the high half.</param>
    /// <param name="low">The second draw, which forms the low half.</param>
    /// <returns>A double in <c>[0,1)</c>. Never negative and never <c>1.0</c>.</returns>
    /// <remarks>
    /// The largest possible mantissa is <c>2^53 - 1</c>, so the largest possible result is
    /// <c>(2^53 - 1) / 2^53</c>, which is strictly below one and exactly representable.
    /// </remarks>
    public static double UnitDouble(uint high, uint low)
    {
        ulong bits = ((ulong)high << 32) | low;
        return (bits >> DiscardedLowBits) * (1.0 / UnitDoubleScale);
    }

    /// <summary>
    /// Draws a <c>[0,1)</c> double, consuming exactly two draws (doc 20 § Authoritative
    /// random-number contract).
    /// </summary>
    /// <param name="source">The stream to draw from.</param>
    /// <returns>A double in <c>[0,1)</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static double NextUnitDouble(IRandomSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        uint high = source.NextUInt32();
        uint low = source.NextUInt32();
        return UnitDouble(high, low);
    }

    /// <summary>
    /// Resolves a chance expressed as the integer ratio
    /// <paramref name="numerator"/>/<paramref name="denominator"/> by comparing integers (doc
    /// 20 § Authoritative random-number contract).
    /// </summary>
    /// <param name="source">The stream to draw from.</param>
    /// <param name="numerator">The favourable count, at most <paramref
    /// name="denominator"/>.</param>
    /// <param name="denominator">The total count, at least one.</param>
    /// <returns><see langword="true"/> when the chance succeeds.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="denominator"/> is zero, or <paramref name="numerator"/> exceeds it.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The comparison is <c>draw × denominator &lt; numerator × 2^32</c> in 64-bit integer
    /// arithmetic: no division, no double, and <b>exactly one draw</b>. Routing a chance
    /// through <see cref="UnitDouble"/> would consume two draws and reintroduce the
    /// floating-point comparison the conversion rules of doc 20 § Authoritative random-number
    /// contract replaces, which is why
    /// <c>VER-SIM-005-007</c> scripts a single draw and requires success.
    /// </para>
    /// <para>
    /// A guaranteed outcome consumes no draw: a ratio of zero can only fail and a ratio of one
    /// can only succeed, so drawing would shift every later value in the stream for a decision
    /// that was already determined.
    /// </para>
    /// </remarks>
    public static bool NextChance(IRandomSource source, uint numerator, uint denominator)
    {
        ArgumentNullException.ThrowIfNull(source);
        ThrowIfZero(denominator, nameof(denominator));
        if (numerator > denominator)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numerator),
                numerator,
                "an integer-ratio chance has a numerator no greater than its denominator (doc 20 § Authoritative random-number contract)");
        }

        if (numerator == 0U)
        {
            return false;
        }

        if (numerator == denominator)
        {
            return true;
        }

        ulong draw = source.NextUInt32();
        return draw * denominator < (ulong)numerator << 32;
    }

    private static void ThrowIfZero(uint bound, string parameterName)
    {
        if (bound == 0U)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                bound,
                "a bound of zero has no representable result; doc 20 § Authoritative random-number contract bounds are exclusive upper bounds of at least one");
        }
    }
}
