using System;
using System.Globalization;

namespace MechaMiner.Simulation.Combat;

/// <summary>
/// Pulse Repeater's attack schedule: an exact rational cooldown that yields activations at
/// 2.6667 per second without drifting.
/// </summary>
/// <remarks>
/// <para>
/// See <see cref="PulseRepeaterBaseline.CadenceDenominatorTicks"/> for why the schedule is a fraction
/// rather than a tick count. This type is the accumulator: each advanced tick adds
/// <see cref="PulseRepeaterBaseline.CadenceNumeratorPerTick"/> credit and each activation spends
/// <see cref="PulseRepeaterBaseline.CadenceDenominatorTicks"/>.
/// </para>
/// <para>
/// <b>It starts ready.</b> doc 71 § Base Weapon Summary:48 - "<c>Burst 10</c> assumes the weapon
/// begins ready to attack", which is the assumption the catalog's own burst column is computed under.
/// So the first tick of a run can fire, and the burst figure the catalog states is the burst this
/// implementation produces.
/// </para>
/// <para>
/// <b>Credit does not accumulate past one activation while there is no target.</b> Otherwise a mech
/// that walked alone for ten seconds would discharge 26 projectiles in one tick on meeting the first
/// enemy - a burst no document describes, and the exact shape of "stored charge" that doc 71:59
/// treats as a distinguishing property of <c>W-BE</c> Sentry Pod's ramp rather than a general rule.
/// </para>
/// <para>
/// <b>The cap is one activation plus the largest remainder a tick can leave, not one activation.</b>
/// That extra <see cref="PulseRepeaterBaseline.CadenceNumeratorPerTick"/> minus one is what keeps the
/// half-tick alive, and capping at exactly one activation was a measured defect rather than a
/// hypothetical one: it discarded the remainder every time, so the schedule fired on a uniform 23-tick
/// gap - 2.609 activations per second, 2.2% slow - and the exact-fraction accumulator became an
/// elaborate way of rounding up. <c>WeaponScheduleTests</c> asserts that both a 22- and a 23-tick gap
/// occur, which is what tells the two implementations apart; asserting only that gaps are 22 or 23
/// would have passed for the broken one.
/// </para>
/// </remarks>
public readonly struct WeaponSchedule : IEquatable<WeaponSchedule>
{
    private readonly int _credit;

    private WeaponSchedule(int credit)
    {
        _credit = credit;
    }

    /// <summary>A schedule that begins ready to fire.</summary>
    public static WeaponSchedule Ready =>
        new(PulseRepeaterBaseline.CadenceDenominatorTicks);

    /// <summary>The accumulated credit, in units where one activation costs the denominator.</summary>
    public int Credit => _credit;

    /// <summary>Whether an activation is affordable now.</summary>
    public bool IsReady => _credit >= PulseRepeaterBaseline.CadenceDenominatorTicks;

    /// <summary>
    /// The most credit a schedule may hold: one activation plus the largest remainder a tick can leave.
    /// </summary>
    public const int MaximumCredit =
        PulseRepeaterBaseline.CadenceDenominatorTicks
        + PulseRepeaterBaseline.CadenceNumeratorPerTick
        - 1;

    /// <summary>Returns this schedule with one tick's credit added, capped at <see cref="MaximumCredit"/>.</summary>
    public WeaponSchedule Advanced()
    {
        int credited = _credit + PulseRepeaterBaseline.CadenceNumeratorPerTick;
        return new WeaponSchedule(credited > MaximumCredit ? MaximumCredit : credited);
    }

    /// <summary>Returns this schedule with one activation's credit spent.</summary>
    /// <exception cref="InvalidOperationException">The schedule was not ready.</exception>
    public WeaponSchedule Fired()
    {
        if (!IsReady)
        {
            throw new InvalidOperationException(
                "the attack schedule held "
                    + _credit.ToString(CultureInfo.InvariantCulture)
                    + " credit and an activation costs "
                    + PulseRepeaterBaseline.CadenceDenominatorTicks.ToString(CultureInfo.InvariantCulture)
                    + "; firing while not ready would make the weapon's rate a function of how often "
                    + "the caller asked rather than of its attack-rate stat");
        }

        return new WeaponSchedule(_credit - PulseRepeaterBaseline.CadenceDenominatorTicks);
    }

    /// <inheritdoc/>
    public static bool operator ==(WeaponSchedule left, WeaponSchedule right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(WeaponSchedule left, WeaponSchedule right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(WeaponSchedule other)
    {
        return _credit == other._credit;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is WeaponSchedule other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return _credit.GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "schedule(credit="
            + _credit.ToString(CultureInfo.InvariantCulture)
            + "/"
            + PulseRepeaterBaseline.CadenceDenominatorTicks.ToString(CultureInfo.InvariantCulture)
            + ")";
    }
}
