using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Combat;

/// <summary>
/// The rank-zero numbers of <c>W-BC</c> Pulse Repeater, the mech's one automatic weapon in this
/// slice.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a weapon exists here at all.</b> Without one, nothing can kill an enemy, so phase 10 has no
/// death to resolve and phase 12 has no deferred removal to apply - two of the phases this slice owns
/// would be empty for a reason that is not a scope boundary but a missing input. One nearest-target
/// weapon is the smallest thing that closes the loop.
/// </para>
/// <para>
/// <b>Why this weapon.</b> <c>docs/36-initial-mech-catalog.md</c>:47 makes Pulse Repeater the
/// signature weapon of <c>MCH-01</c> Kestrel, which the same document:58 calls "the recommended first
/// deployment". <c>docs/66-weapon-catalog-and-resource-graph.md</c>:46 marks <c>W-BC</c> the "Initial
/// signature". So the first weapon to exist is the one the documents make the first weapon a player
/// meets, rather than whichever was easiest.
/// </para>
/// <para>
/// <b>What is deliberately absent.</b> The <c>0.25M</c> hit push of doc 71:81, the three branches of
/// doc 66:46, the ore-stat ranks, and Kestrel's own <c>+15%</c> Accelerated Feed trait
/// (doc 36:60-62) are all unimplemented. The push needs a displacement system with control-resistance
/// rules (<c>docs/72</c> § control resistance) that does not exist; the rest are <c>COM-*</c> and
/// <c>PRG-*</c> work. Applying the trait would also mean this slice silently assumes a mech
/// selection, and no mech selection flow exists: <c>MCH-01</c> is cited above as the source of the
/// weapon identity, not as a chosen loadout.
/// </para>
/// </remarks>
public static class PulseRepeaterBaseline
{
    /// <summary>The weapon's accepted gameplay ID.</summary>
    /// <remarks><c>docs/66-weapon-catalog-and-resource-graph.md</c>:46.</remarks>
    public const string ContentId = "W-BC";

    /// <summary>Rank-zero damage per projectile: <c>12</c>.</summary>
    /// <remarks>
    /// <c>docs/71-initial-weapon-numeric-catalog.md</c>:81, Stat 1 "Damage | 12 / +1.2"; :57 states
    /// the same as a damage model, "12 every 0.375 s".
    /// </remarks>
    public const int DamagePerProjectile = 12;

    /// <summary>Rank-zero attack rate in activations per second: <c>2.667</c>.</summary>
    /// <remarks>
    /// <para>
    /// <c>docs/71-initial-weapon-numeric-catalog.md</c>:81, Stat 2 "Attack rate | 2.667/s /
    /// +0.267/s". doc 71 § Measurement Conventions:27 - "An attack-rate stat is recorded as
    /// activations per second even if the UI presents the reciprocal cooldown."
    /// </para>
    /// <para>
    /// <b>This is the displayed stat and not the timing source.</b> It is the four-significant-digit
    /// rounding of the exact rate, so its own reciprocal is 0.37495 s rather than the 0.375 s doc 71:57
    /// states, and deriving a tick period from it truncates to 22 ticks - 2.3% fast. The timing source
    /// is <see cref="ActivationPeriodSeconds"/>. <c>PulseRepeaterTests</c> asserts the two agree to
    /// within that rounding, so they cannot silently describe different weapons.
    /// </para>
    /// </remarks>
    public const double AttackRatePerSecond = 2.667;

    /// <summary>The activation period in seconds: <c>0.375</c>.</summary>
    /// <remarks>
    /// <c>docs/71-initial-weapon-numeric-catalog.md</c>:57, rank-zero damage model column - "12 every
    /// <b>0.375 s</b>". The document states the period as a terminating decimal and the rate as a
    /// rounded one, so this is the exact figure of the pair and every derived tick count below comes
    /// from it.
    /// </remarks>
    public const double ActivationPeriodSeconds = 0.375;

    /// <summary>Rank-zero targeting range in gameplay meters: <c>8.0</c>.</summary>
    /// <remarks>
    /// <c>docs/71-initial-weapon-numeric-catalog.md</c>:81, Stat 3 "Range | 8M / +0.8M". <c>M</c> is
    /// one unmodified mech collision diameter, <c>1.0</c> m by
    /// <c>docs/72-player-survivability-and-damage-baseline.md</c>:40 and :47.
    /// </remarks>
    public const double TargetingRangeMeters = 8.0;

    /// <summary>Projectile speed in gameplay meters per second: <c>16.0</c>.</summary>
    /// <remarks>
    /// <c>docs/71-initial-weapon-numeric-catalog.md</c>:81, Fixed properties column - "16M/s
    /// projectile, 0.25M hit push".
    /// </remarks>
    public const double ProjectileSpeedMetersPerSecond = 16.0;

    /// <summary>The cadence numerator: the shot credit one committed tick earns.</summary>
    /// <remarks>See <see cref="CadenceDenominatorTicks"/> for why this is a fraction and not a count.</remarks>
    public const int CadenceNumeratorPerTick = 2;

    /// <summary>
    /// The shot credit one activation costs: <c>45</c>, against <see cref="CadenceNumeratorPerTick"/>
    /// earned per tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The documented cadence is not a whole number of ticks, and rounding it would be an invented
    /// rule.</b> doc 71:57 gives "12 every 0.375 s", which at 60 Hz is 22.5 ticks. Rounding down to 22
    /// makes the weapon fire at 2.727/s, 2.3% fast; rounding up to 23 makes it 2.609/s, 2.2% slow.
    /// Either would be a balance change introduced by an implementation detail, and doc 71:28 is
    /// explicit that "Tick rate is not itself an upgradeable stat and should not change total damage".
    /// </para>
    /// <para>
    /// So the schedule is an exact rational accumulator instead: each committed tick adds
    /// <see cref="CadenceNumeratorPerTick"/> = 2 credit, an activation costs 45, and
    /// <c>45 / 2 = 22.5</c> ticks. The weapon therefore fires on alternating 22- and 23-tick gaps and
    /// its long-run rate is exactly 2.6667 activations per second, in integer arithmetic that cannot
    /// drift.
    /// </para>
    /// </remarks>
    public const int CadenceDenominatorTicks =
        (int)(CadenceNumeratorPerTick * ActivationPeriodSeconds * TickRate.TicksPerSecond);

    /// <summary>Projectile displacement per tick in gameplay meters.</summary>
    public const double ProjectileDisplacementPerTickMeters =
        ProjectileSpeedMetersPerSecond * TickRate.SecondsPerTick;

    /// <summary>
    /// A projectile's lifetime in ticks: how long it takes to travel the targeting range.
    /// </summary>
    /// <remarks>
    /// <para>
    /// doc 71:81 gives the weapon a range and a projectile speed and no separate lifetime, and doc 66:94
    /// says "Projectiles continue on their fired trajectory rather than homing after launch". Range
    /// divided by speed is the only lifetime those three statements jointly admit: 8 m at 16 m/s is
    /// 0.5 s, or 30 ticks. A longer lifetime would let the weapon damage a target outside its stated
    /// range; a shorter one would make the range unreachable.
    /// </para>
    /// <para>
    /// 30 is exact at 60 Hz, which is checked rather than assumed - see <c>PulseRepeaterTests</c>.
    /// </para>
    /// </remarks>
    public const int ProjectileLifetimeTicks =
        (int)((TargetingRangeMeters / ProjectileSpeedMetersPerSecond) * TickRate.TicksPerSecond);
}
