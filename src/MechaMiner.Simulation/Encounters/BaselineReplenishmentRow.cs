using System;
using System.Globalization;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// One row of the director's baseline replenishment: a desired population, a batch size, and an
/// interval.
/// </summary>
/// <remarks>
/// <para>
/// The three fields are the three columns <c>docs/32-standard-wave-and-beacon-schedule.md</c>
/// § Director vocabulary:19-20 defines - "Minimum: the desired baseline number of ordinary enemies
/// active around the player" and "Pulse: the batch size and active-simulation interval used while the
/// baseline population is below its desired minimum."
/// </para>
/// <para>
/// <b>Why this is a value and not the pair of constants it started as.</b> Two reasons, and only one of
/// them is <c>ENC-002</c>. The first is that a schedule is a sequence of these, so the type that a
/// compiler of doc 32's 35 rows will produce is this one, and having it now means that package changes
/// the source of a row rather than the shape of the world's dependency. The second is measured: at
/// minute 0's authored pressure the run is very nearly unlosable, so a harness that has to show damage
/// accumulating and a run ending in destruction cannot do it from an authored row at all. See
/// <see cref="MinuteZeroBaseline"/> for the measurement and <c>HarnessStressRows</c> for what the
/// harnesses use instead - deliberately labelled, so a row nobody authored can never be mistaken for one
/// doc 32 states.
/// </para>
/// </remarks>
public readonly struct BaselineReplenishmentRow : IEquatable<BaselineReplenishmentRow>
{
    private readonly string? _label;
    private readonly int _desiredMinimumPopulation;
    private readonly int _pulseBatchSize;
    private readonly int _pulseIntervalTicks;

    private BaselineReplenishmentRow(
        string label,
        int desiredMinimumPopulation,
        int pulseBatchSize,
        int pulseIntervalTicks)
    {
        _label = label;
        _desiredMinimumPopulation = desiredMinimumPopulation;
        _pulseBatchSize = pulseBatchSize;
        _pulseIntervalTicks = pulseIntervalTicks;
    }

    /// <summary>
    /// What this row is, and whether a document states it.
    /// </summary>
    /// <remarks>
    /// A required field rather than a convenience. A row's provenance is the difference between a
    /// balance claim and a test fixture, and a transcript that printed only three numbers could not tell
    /// a reader which it was looking at.
    /// </remarks>
    public string Label => _label ?? string.Empty;

    /// <summary>The desired baseline population.</summary>
    public int DesiredMinimumPopulation => _desiredMinimumPopulation;

    /// <summary>The batch admitted per pulse while the population is short.</summary>
    public int PulseBatchSize => _pulseBatchSize;

    /// <summary>The pulse interval in ticks.</summary>
    public int PulseIntervalTicks => _pulseIntervalTicks;

    /// <summary>Whether this row was built rather than defaulted.</summary>
    public bool IsAuthored => Label.Length > 0 && _pulseIntervalTicks > 0;

    /// <summary>
    /// Builds a row from a minimum, a batch size, and an interval in seconds.
    /// </summary>
    /// <param name="label">What this row is and where it came from.</param>
    /// <param name="desiredMinimumPopulation">The desired baseline population. Must be positive.</param>
    /// <param name="pulseBatchSize">The pulse batch size. Must be positive.</param>
    /// <param name="pulseIntervalSeconds">The pulse interval. Must be a positive whole number of ticks.</param>
    /// <exception cref="ArgumentException"><paramref name="label"/> is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A number is outside its domain.</exception>
    /// <remarks>
    /// The interval must land on a whole tick. Every interval doc 32's Pulse column states is a multiple
    /// of 0.01 s and most are not multiples of the 1/60 s tick - 0.16 s is 9.6 ticks - so a compiler of
    /// those rows has to decide what to do about the remainder, and refusing here means it decides
    /// deliberately rather than inheriting a silent truncation from this constructor.
    /// </remarks>
    public static BaselineReplenishmentRow Create(
        string label,
        int desiredMinimumPopulation,
        int pulseBatchSize,
        double pulseIntervalSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentOutOfRangeException.ThrowIfLessThan(desiredMinimumPopulation, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pulseBatchSize, 1);

        double ticks = pulseIntervalSeconds * TickRate.TicksPerSecond;
        int wholeTicks = (int)Math.Round(ticks, MidpointRounding.ToEven);
        if (!double.IsFinite(ticks) || wholeTicks < 1 || Math.Abs(ticks - wholeTicks) > 1e-9)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pulseIntervalSeconds),
                pulseIntervalSeconds,
                "a pulse interval must be a positive whole number of "
                    + TickRate.TicksPerSecond.ToString(CultureInfo.InvariantCulture)
                    + " Hz ticks; "
                    + ticks.ToString("0.####", CultureInfo.InvariantCulture)
                    + " ticks is not, and rounding it here would hide a rate change inside a constructor");
        }

        return new BaselineReplenishmentRow(
            label,
            desiredMinimumPopulation,
            pulseBatchSize,
            wholeTicks);
    }

    /// <summary>
    /// How many enemies this row's pulse admits on <paramref name="tick"/>.
    /// </summary>
    /// <param name="tick">The tick being evaluated.</param>
    /// <param name="livePopulation">The number of ordinary enemies currently live.</param>
    /// <returns>Zero when no pulse is due or the minimum is already met; otherwise the batch size,
    /// reduced to what the shortfall needs.</returns>
    /// <remarks>
    /// <para>
    /// The minimum is a floor, not a target: doc 32:20 - "Once the minimum is met, baseline spawning
    /// waits until attrition creates room." So replenishment is demand-driven and the population is a
    /// closed loop with whatever is killing it, rather than a crowd that grows until it meets a store
    /// ceiling.
    /// </para>
    /// <para>
    /// The batch is trimmed to the shortfall, because doc 32:19 calls the minimum "desired" and
    /// overshooting it by an arbitrary amount would put the population above the number the document
    /// names.
    /// </para>
    /// <para>
    /// Tick zero is a pulse tick. doc 32 § Phase-level pressure curve:43 wants "a few fragile bodies"
    /// demonstrating combat from 0:00, and holding the first pulse back by one interval would leave the
    /// opening empty for no stated reason.
    /// </para>
    /// </remarks>
    public int PulseSize(long tick, int livePopulation)
    {
        if (!IsAuthored || tick < 0L || livePopulation < 0)
        {
            return 0;
        }

        if (tick % _pulseIntervalTicks != 0L)
        {
            return 0;
        }

        int shortfall = _desiredMinimumPopulation - livePopulation;
        if (shortfall <= 0)
        {
            return 0;
        }

        return shortfall < _pulseBatchSize ? shortfall : _pulseBatchSize;
    }

    /// <inheritdoc/>
    public static bool operator ==(BaselineReplenishmentRow left, BaselineReplenishmentRow right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(BaselineReplenishmentRow left, BaselineReplenishmentRow right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(BaselineReplenishmentRow other)
    {
        return string.Equals(Label, other.Label, StringComparison.Ordinal)
            && _desiredMinimumPopulation == other._desiredMinimumPopulation
            && _pulseBatchSize == other._pulseBatchSize
            && _pulseIntervalTicks == other._pulseIntervalTicks;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is BaselineReplenishmentRow other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(Label),
            _desiredMinimumPopulation,
            _pulseBatchSize,
            _pulseIntervalTicks);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Label
            + "(minimum="
            + _desiredMinimumPopulation.ToString(CultureInfo.InvariantCulture)
            + ",pulse="
            + _pulseBatchSize.ToString(CultureInfo.InvariantCulture)
            + "/"
            + _pulseIntervalTicks.ToString(CultureInfo.InvariantCulture)
            + "t)";
    }
}
