namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// Replenishment rows that <b>no document states</b>, used only to make the damage and loss paths
/// observable.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing that ships composes a run from these.</b> <c>RunComposition.CreateGraybox</c> uses
/// <see cref="MinuteZeroBaseline.Row"/>, the authored one. These exist because of a measurement
/// recorded on <see cref="MinuteZeroBaseline"/>: at the pressure doc 32:56 authors for minute 0, a
/// 35-minute run ends with 95 of 100 Hull, so contact damage accumulating and a run ending in
/// destruction are outcomes an authored row will not produce inside a capture a person can watch. A
/// gate or a harness that claimed to show them from that row would be claiming something it had not
/// arranged to see.
/// </para>
/// <para>
/// <b>The alternative would have been to use an authored high-pressure row, and it is not available.</b>
/// doc 32's later minutes do reach 420 bodies, but every row after minute 0 composes identities this
/// slice has no profile for - minute 1 is already "Skitterling 80%, Ripper 20%" - and inventing a
/// Skitterling-only version of minute 34 would be putting a made-up row under a real row's name, which
/// is worse than putting it under an obviously made-up one.
/// </para>
/// <para>
/// So each row below carries a label that says so, and the label travels into
/// <c>BaselineReplenishmentRow.ToString</c> and from there into every transcript that prints the row.
/// A reader of an artifact cannot see one of these numbers without also seeing that nothing authored it.
/// </para>
/// </remarks>
public static class HarnessStressRows
{
    /// <summary>
    /// Enough simultaneous pressure that a stationary mech is destroyed in a few seconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 64 bodies in batches of 16 every 0.25 seconds. The arithmetic behind those three numbers, so a
    /// reader can check that this row does what this type claims rather than taking it on trust: with
    /// many pursuers overlapping at once, the mech's damage intake is capped by the 0.20-second global
    /// contact grace of doc 72:43 at one 5-damage instance every 12 ticks, which is 25 Hull per second,
    /// so 100 Hull is four seconds. The batch has to be large enough and the interval short enough that
    /// the overlap is continuous against a weapon clearing 32 Hull per second, and 16 bodies per 0.25
    /// seconds is four times the weapon's throughput.
    /// </para>
    /// <para>
    /// <b>Not a balance proposal.</b> It is a load that makes the cadence rule, the death path, phase
    /// 13's settlement, and the terminal outcome all observable inside a few hundred ticks.
    /// </para>
    /// </remarks>
    public static BaselineReplenishmentRow LethalSwarm => BaselineReplenishmentRow.Create(
        "harness stress row, NOT from any document",
        desiredMinimumPopulation: 64,
        pulseBatchSize: 16,
        pulseIntervalSeconds: 0.25);
}
