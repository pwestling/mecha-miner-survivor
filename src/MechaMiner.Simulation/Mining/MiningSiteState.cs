using System;
using System.Globalization;
using MechaMiner.Simulation.Geometry;

namespace MechaMiner.Simulation.Mining;

/// <summary>
/// One standard ore seam's live state: where its zone is, how far into the current installment it has
/// got, how many installments it has already paid, and how long the mech has been outside.
/// </summary>
/// <remarks>
/// <para>
/// The four fields are exactly the four states of <c>docs/40-mining-and-extraction.md</c> § Mining
/// state flow:30-40, and which one this record is in is derived rather than stored - see
/// <see cref="Phase"/>. Storing the phase as a fifth field would let it disagree with the numbers it
/// is a function of, which is the failure the diagram's own <c>Decaying --&gt; Available</c> edge
/// depends on not happening.
/// </para>
/// <para>
/// <b>Paid installments and unfinished progress are separate fields on purpose.</b> doc 40:64 - "Ore
/// from completed intervals remains secured in the run inventory. Leaving only threatens progress
/// toward the current unfinished interval ... it never removes previously awarded ore." One combined
/// progress figure would make that impossible to honour: decay would eat into a paid installment, and
/// the seam would refund ore the run had already banked.
/// </para>
/// </remarks>
public readonly struct MiningSiteState : IEquatable<MiningSiteState>
{
    private readonly PlanarVector _centre;
    private readonly int _installmentProgressTicks;
    private readonly int _installmentsPaid;
    private readonly int _ticksOutside;

    private MiningSiteState(
        PlanarVector centre,
        int installmentProgressTicks,
        int installmentsPaid,
        int ticksOutside)
    {
        _centre = centre;
        _installmentProgressTicks = installmentProgressTicks;
        _installmentsPaid = installmentsPaid;
        _ticksOutside = ticksOutside;
    }

    /// <summary>The zone's centre in gameplay meters.</summary>
    public PlanarVector Centre => _centre;

    /// <summary>Ticks of forward progress into the current, unpaid installment.</summary>
    public int InstallmentProgressTicks => _installmentProgressTicks;

    /// <summary>How many installments this seam has paid out.</summary>
    public int InstallmentsPaid => _installmentsPaid;

    /// <summary>
    /// How many consecutive ticks the mech has been outside the zone, or zero while it is inside.
    /// </summary>
    public int TicksOutside => _ticksOutside;

    /// <summary>Whether all ten installments have been paid and the seam is spent.</summary>
    /// <remarks>
    /// doc 40:90 makes a depleted point "a non-interactive mapped landmark for the rest of the run"
    /// that "cannot be reactivated", so this is a terminal condition for the site rather than a pause.
    /// </remarks>
    public bool IsDepleted => _installmentsPaid >= StandardOreSeamProfile.InstallmentCount;

    /// <summary>The state-flow phase this record is in.</summary>
    public MiningPhase Phase
    {
        get
        {
            if (IsDepleted)
            {
                return MiningPhase.Complete;
            }

            if (_ticksOutside == 0)
            {
                return _installmentProgressTicks > 0
                    ? MiningPhase.Extracting
                    : MiningPhase.Available;
            }

            if (_installmentProgressTicks == 0)
            {
                return MiningPhase.Available;
            }

            return _ticksOutside > GrayboxExtraction.ExitGraceTicks
                ? MiningPhase.Decaying
                : MiningPhase.Extracting;
        }
    }

    /// <summary>The unfinished installment's progress as a fraction of one installment, in [0, 1].</summary>
    public double InstallmentFraction =>
        (double)_installmentProgressTicks / StandardOreSeamProfile.InstallmentTicks;

    /// <summary>The zone circle the occupancy test uses.</summary>
    public PlanarCircle Zone =>
        PlanarCircle.FromCentreAndRadius(_centre, GrayboxExtraction.ZoneRadiusMeters);

    /// <summary>An untouched seam at <paramref name="centre"/>.</summary>
    /// <param name="centre">The zone's centre.</param>
    public static MiningSiteState Available(PlanarVector centre)
    {
        return new MiningSiteState(centre, 0, 0, 0);
    }

    /// <summary>
    /// Rebuilds a state from its four components, for tests that need to start mid-extraction.
    /// </summary>
    /// <param name="centre">The zone's centre.</param>
    /// <param name="installmentProgressTicks">Progress into the current installment.</param>
    /// <param name="installmentsPaid">Installments already paid.</param>
    /// <param name="ticksOutside">Consecutive ticks the mech has been outside.</param>
    /// <exception cref="ArgumentOutOfRangeException">A component is outside its domain.</exception>
    public static MiningSiteState Create(
        PlanarVector centre,
        int installmentProgressTicks,
        int installmentsPaid,
        int ticksOutside)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(installmentProgressTicks);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(
            installmentProgressTicks,
            StandardOreSeamProfile.InstallmentTicks);
        ArgumentOutOfRangeException.ThrowIfNegative(installmentsPaid);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            installmentsPaid,
            StandardOreSeamProfile.InstallmentCount);
        ArgumentOutOfRangeException.ThrowIfNegative(ticksOutside);

        return new MiningSiteState(centre, installmentProgressTicks, installmentsPaid, ticksOutside);
    }

    /// <summary>Returns this state with the given progress, paid count, and outside count.</summary>
    /// <param name="installmentProgressTicks">Progress into the current installment.</param>
    /// <param name="installmentsPaid">Installments already paid.</param>
    /// <param name="ticksOutside">Consecutive ticks the mech has been outside.</param>
    internal MiningSiteState With(int installmentProgressTicks, int installmentsPaid, int ticksOutside)
    {
        return new MiningSiteState(_centre, installmentProgressTicks, installmentsPaid, ticksOutside);
    }

    /// <inheritdoc/>
    public static bool operator ==(MiningSiteState left, MiningSiteState right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(MiningSiteState left, MiningSiteState right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(MiningSiteState other)
    {
        return _centre.Equals(other._centre)
            && _installmentProgressTicks == other._installmentProgressTicks
            && _installmentsPaid == other._installmentsPaid
            && _ticksOutside == other._ticksOutside;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is MiningSiteState other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _centre,
            _installmentProgressTicks,
            _installmentsPaid,
            _ticksOutside);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return "seam(at="
            + _centre.ToString()
            + ","
            + Phase.ToString()
            + ",progress="
            + _installmentProgressTicks.ToString(CultureInfo.InvariantCulture)
            + "/"
            + StandardOreSeamProfile.InstallmentTicks.ToString(CultureInfo.InvariantCulture)
            + ",paid="
            + _installmentsPaid.ToString(CultureInfo.InvariantCulture)
            + "/"
            + StandardOreSeamProfile.InstallmentCount.ToString(CultureInfo.InvariantCulture)
            + ",outside="
            + _ticksOutside.ToString(CultureInfo.InvariantCulture)
            + ")";
    }
}
