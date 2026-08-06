using System;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// The two intervals that turn a sustained overlap into a bounded stream of damage instances: the
/// same-enemy repeat interval and the mech's global contact grace.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/31-initial-alien-roster.md</c> § Shared ordinary-enemy rules:27-28, verbatim: "An
/// overlapping enemy deals its listed contact damage immediately when eligible and then once every
/// 0.75 seconds while that same overlap continues. After receiving contact damage, the mech has a
/// 0.20-second global contact-damage grace period. Other enemies cannot deal another contact
/// instance during that grace, but enemy projectiles and explicit hazards remain independently
/// eligible." <c>docs/72-player-survivability-and-damage-baseline.md</c>:42-43 states the same two
/// numbers as baseline properties.
/// </para>
/// <para>
/// <b>Two intervals, not one, and they are not interchangeable.</b> The 0.75-second interval is per
/// enemy and per overlap; the 0.20-second grace is per mech and applies across enemies. Together
/// they cap sustained damage at one instance every 0.20 seconds no matter how large the crowd, while
/// still letting a crowd out-damage a single pursuer by a factor of 3.75. Implementing only the
/// repeat interval would let a hundred overlapping Skitterlings deal 500 damage in one tick;
/// implementing only the grace would let one Skitterling deal 5 damage every 0.20 seconds forever,
/// which is 25 Hull per second from one of the roster's most fragile bodies.
/// </para>
/// <para>
/// <b>Both intervals are whole numbers of ticks at 60 Hz, and that is checked rather than assumed.</b>
/// 0.75 s is 45 ticks and 0.20 s is 12 ticks. A duration that did not divide evenly would have to
/// round, and the rounding direction would be an invented rule; <see cref="RepeatIntervalTicks"/>
/// and <see cref="GraceTicks"/> are therefore derived by multiplication and
/// <c>ContactDamageCadenceTests</c> asserts the products are exact.
/// </para>
/// </remarks>
public static class ContactDamageCadence
{
    /// <summary>The same-enemy contact repeat interval in seconds: <c>0.75</c>.</summary>
    /// <remarks>
    /// <c>docs/72-player-survivability-and-damage-baseline.md</c>:42 - "Same-enemy contact repeat
    /// interval | 0.75 s". <c>docs/31-initial-alien-roster.md</c>:27 states the same interval as
    /// behaviour.
    /// </remarks>
    public const double RepeatIntervalSeconds = 0.75;

    /// <summary>The mech's global contact grace in seconds: <c>0.20</c>.</summary>
    /// <remarks>
    /// <c>docs/72-player-survivability-and-damage-baseline.md</c>:43 - "Global contact grace after a
    /// resolved contact | 0.20 s". <c>docs/31-initial-alien-roster.md</c>:28 states the same grace as
    /// behaviour.
    /// </remarks>
    public const double GraceSeconds = 0.20;

    /// <summary>The repeat interval as a whole number of ticks: <c>45</c>.</summary>
    public const int RepeatIntervalTicks = (int)(RepeatIntervalSeconds * TickRate.TicksPerSecond);

    /// <summary>The grace as a whole number of ticks: <c>12</c>.</summary>
    public const int GraceTicks = (int)(GraceSeconds * TickRate.TicksPerSecond);

    /// <summary>
    /// The tick value meaning the mech has not yet received a contact instance this run.
    /// </summary>
    public const long NeverContacted = EnemyState.NeverContacted;

    /// <summary>
    /// Whether a contact instance may resolve on <paramref name="tick"/>.
    /// </summary>
    /// <param name="tick">The tick being resolved.</param>
    /// <param name="lastGlobalContactTick">
    /// The tick the mech last received any contact instance on, or <see cref="NeverContacted"/>.
    /// </param>
    /// <param name="lastContactFromThisEnemyTick">
    /// The tick this particular enemy last landed a contact instance on, or
    /// <see cref="NeverContacted"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="tick"/> is negative.</exception>
    /// <remarks>
    /// <para>
    /// Both tests are <c>&gt;=</c> on the elapsed interval, so an instance lands on exactly the
    /// forty-fifth tick after the previous one rather than the forty-sixth. doc 31:27 says "once
    /// every 0.75 seconds", and 45 ticks after tick <c>t</c> is 0.75 seconds after <c>t</c> exactly;
    /// requiring a further tick would make the real interval 0.7667 seconds, which compounds to a
    /// 2.2% damage shortfall over a sustained overlap.
    /// </para>
    /// <para>
    /// "Immediately when eligible" is the <see cref="NeverContacted"/> case: a first overlap is
    /// eligible on the tick it begins, with no wind-up.
    /// </para>
    /// </remarks>
    public static bool IsEligible(
        long tick,
        long lastGlobalContactTick,
        long lastContactFromThisEnemyTick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        if (lastGlobalContactTick >= 0L && tick - lastGlobalContactTick < GraceTicks)
        {
            return false;
        }

        if (lastContactFromThisEnemyTick >= 0L
            && tick - lastContactFromThisEnemyTick < RepeatIntervalTicks)
        {
            return false;
        }

        return true;
    }
}
