using System;
using MechaMiner.Simulation.Geometry;

namespace MechaMiner.Simulation.Mining;

/// <summary>
/// The extraction rule: one tick of automatic proximity mining, decay, and installment payout for one
/// site.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of <c>docs/40-mining-and-extraction.md</c> § Automatic proximity activation and
/// § Progress decay for the standard-ore-seam class, as a pure function. Phase 11 of the tick calls it
/// once per site; nothing else does.
/// </para>
/// <para>
/// <b>Occupancy is the mech's centre inside the zone circle, and that is a decision rather than a
/// citation.</b> doc 40:46 says mining begins "When the player enters a mining point's clearly visible
/// circular zone" and continues "for as long as the player remains inside", which describes a point
/// crossing a boundary. The alternative reading is footprint overlap, and
/// <c>docs/72-player-survivability-and-damage-baseline.md</c>:49 is the sentence that would license it -
/// "The collision circle is used for blocking terrain, enemy contact, pickups, and damage zones unless
/// an explicit attack presents a different player-facing boundary." Its list is closed and mining zones
/// are not on it, while pickups and damage zones are. So the centre test is chosen on the grounds that
/// the one document listing what the collision circle governs deliberately omits this. The practical
/// difference is 0.5 m of effective radius, which matters at a 2.0 m zone; it is recorded in the pull
/// request as an ambiguity decision rather than left for someone to infer from the code.
/// </para>
/// <para>
/// <b>Progress is counted in ticks, not seconds.</b> Forward progress is one tick of credit per
/// committed tick and decay is four, so every quantity in this rule is an integer and
/// "Progress decays to zero" (doc 40's state diagram) is a state that is actually reachable. A
/// floating fraction decayed by four ninetieths would asymptote instead.
/// </para>
/// </remarks>
public static class MiningAdvance
{
    /// <summary>
    /// Advances one site by one tick.
    /// </summary>
    /// <param name="state">The site's committed state.</param>
    /// <param name="mechCentre">The mech's authoritative ground-plane centre.</param>
    /// <returns>The site's new state and any ore the tick secured.</returns>
    /// <remarks>
    /// <para>
    /// The order inside a tick is: decide occupancy, then move progress, then pay. Paying last is what
    /// makes doc 40:175's open edge case - "An ore seam is interrupted at the exact instant an
    /// installment completes" - resolve in the player's favour: a tick spent inside the zone that
    /// reaches 90 ticks of credit pays, and whether the mech leaves on the following tick cannot
    /// retract it, because doc 40:64 makes a completed installment "a permanent checkpoint for that
    /// run".
    /// </para>
    /// <para>
    /// A depleted seam is returned unchanged, including its outside counter. doc 40:90 - a depleted
    /// point "remains as a non-interactive mapped landmark for the rest of the run and cannot be
    /// reactivated" - so there is nothing for occupancy to mean and nothing left to decay.
    /// </para>
    /// </remarks>
    public static MiningTickOutcome Advance(MiningSiteState state, PlanarVector mechCentre)
    {
        if (state.IsDepleted)
        {
            return new MiningTickOutcome(state, 0);
        }

        bool inside = state.Zone.Contains(mechCentre);
        if (inside)
        {
            int advanced = state.InstallmentProgressTicks + 1;
            if (advanced >= StandardOreSeamProfile.InstallmentTicks)
            {
                return new MiningTickOutcome(
                    state.With(0, state.InstallmentsPaid + 1, 0),
                    StandardOreSeamProfile.OrePerInstallment);
            }

            return new MiningTickOutcome(state.With(advanced, state.InstallmentsPaid, 0), 0);
        }

        int outside = state.TicksOutside + 1;
        if (outside <= GrayboxExtraction.ExitGraceTicks)
        {
            // doc 40:52: "Unfinished progress holds steady during that grace period."
            return new MiningTickOutcome(
                state.With(state.InstallmentProgressTicks, state.InstallmentsPaid, outside),
                0);
        }

        int decayed = Math.Max(
            0,
            state.InstallmentProgressTicks - GrayboxExtraction.DecayRateMultiple);
        return new MiningTickOutcome(state.With(decayed, state.InstallmentsPaid, outside), 0);
    }
}
