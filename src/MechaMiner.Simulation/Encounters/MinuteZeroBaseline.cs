namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// The one authored schedule row this slice executes: minute 0 of the standard 35-minute wave schedule.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/32-standard-wave-and-beacon-schedule.md</c> § Complete 35-minute schedule:56, verbatim
/// row: <c>| 0 | Skitterling 100% | 8 | 2 / 1.50s | Survey orientation; no formation |</c>. Read
/// against § Director vocabulary:18-21, that is: composition entirely <c>EN-01</c>; a desired
/// baseline of 8 ordinary enemies active around the player; batches of 2 at 1.50-second intervals
/// while the population is below that minimum; and no authored formation.
/// </para>
/// <para>
/// <b>This row is held for the whole run, and that is a graybox decision rather than a reading of
/// doc 32.</b> The document gives 35 rows and minute 1 is already a different composition with a
/// higher minimum. Compiling the rows, interpolating between them, and layering authored formations
/// is <c>ENC-002</c> ("Director schedule compiler, pulses, weighted residual composition,
/// ceilings/queues", doc 110:263), whose close evidence is "all 35 rows exact". Executing row 0 for
/// 35 minutes therefore under-pressures every minute after the first by construction: at 35:00 a
/// real run should be holding 420 bodies against this one's 8. That is stated here so the gap is a
/// recorded scope boundary rather than a balance bug someone later measures.
/// </para>
/// <para>
/// <b>Measured consequence, worth writing down because it surprised the author of this file.</b> At
/// this row's pressure the run is very nearly unlosable. Eight Skitterlings are 160 Hull between them
/// and <c>W-BC</c> Pulse Repeater deals 32 damage per second (doc 71:57), so the weapon clears the whole
/// desired population in about five seconds while replenishment adds 1.33 bodies per second - and a
/// pursuer crossing the 8 m targeting range at 1.26 m/s spends 6.3 seconds under fire before it can
/// touch anything. A measured 35-minute run kiting a 12 m circle ended with 95 of 100 Hull and 1,516
/// kills. That is not a defect: doc 32 § Phase-level pressure curve:43 says minute 0 is exactly this -
/// "A few fragile bodies demonstrate automatic combat without contesting survey reading". It does mean
/// the loss condition cannot be demonstrated from an authored row, which is why
/// <see cref="HarnessStressRows"/> exists and why it is labelled the way it is.
/// </para>
/// </remarks>
public static class MinuteZeroBaseline
{
    /// <summary>The desired baseline number of ordinary enemies active around the player: <c>8</c>.</summary>
    /// <remarks><c>docs/32-standard-wave-and-beacon-schedule.md</c>:56, Minimum column.</remarks>
    public const int DesiredMinimumPopulation = 8;

    /// <summary>The pulse batch size: <c>2</c>.</summary>
    /// <remarks><c>docs/32-standard-wave-and-beacon-schedule.md</c>:56, Pulse column, "2 / 1.50s".</remarks>
    public const int PulseBatchSize = 2;

    /// <summary>The pulse interval in active-simulation seconds: <c>1.50</c>.</summary>
    /// <remarks>
    /// <c>docs/32-standard-wave-and-beacon-schedule.md</c>:56, Pulse column, and § Director
    /// vocabulary:20, which calls it an "active-simulation interval" - so it advances with committed
    /// ticks and not with wall time, which is what expressing the row in ticks makes structural.
    /// </remarks>
    public const double PulseIntervalSeconds = 1.50;

    /// <summary>The row, as the value the world's phase 3 consumes.</summary>
    public static BaselineReplenishmentRow Row => BaselineReplenishmentRow.Create(
        "docs/32:56 minute-0 authored row",
        DesiredMinimumPopulation,
        PulseBatchSize,
        PulseIntervalSeconds);
}
