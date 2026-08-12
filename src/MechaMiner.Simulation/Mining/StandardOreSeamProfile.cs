using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Mining;

/// <summary>
/// The standard ore seam: the one mining-point class this slice carries, and the only one of the four
/// whose reward is not withheld until completion.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/40-mining-and-extraction.md</c> § Standard ore seams:62, verbatim: "A standard ore seam
/// awards 10 common ore whenever the player completes a 1.5-second extraction installment. It contains
/// ten installments and therefore takes 15 seconds of uninterrupted forward extraction to deplete,
/// paying 100 ore in total." :66 confirms the whole set as the baseline: "The count, 10-ore payout,
/// 1.5-second cadence, ten-installment capacity, and 15-second total depletion time are the initial
/// playtest baseline."
/// </para>
/// <para>
/// <b>Why this class and not one of the other three.</b> doc 40:131 names four accepted classes.
/// Rich seams differ only in two numbers, so they would add no behaviour. Material geodes pay only at
/// completion and each one projects a resonance field that modifies enemies inside it (doc 40:98-115),
/// which is <c>MIN-002</c>. Hyper Gold sites pay only at completion and act as progress-threshold
/// threat beacons (doc 40:123), which is <c>MIN-002</c> and <c>ENC-009</c>. The standard seam is
/// therefore the only class whose whole rule set is inside this slice, and it is also the only one that
/// pays incrementally - which is what makes a rising number legible in a transcript before the run
/// ends.
/// </para>
/// <para>
/// <b>The extraction-zone radius is not here, because no document on this ref states one.</b> See
/// <see cref="MiningSiteState"/> and <c>GrayboxExtraction.ZoneRadiusMeters</c>.
/// </para>
/// </remarks>
public static class StandardOreSeamProfile
{
    /// <summary>The class's identifier, used as the source content ID on emitted events.</summary>
    /// <remarks>
    /// Matches the source name the authored content on <c>master</c> uses for this row -
    /// <c>content/resources/common-ore.json</c>'s <c>sources[].source</c> is "standard ore seam" - so
    /// that when <c>DAT-002</c> loads that file the identifier does not have to be translated. That
    /// file is not in this branch's ancestry and is cited here as a naming precedent only, not as a
    /// source for any number.
    /// </remarks>
    public const string ContentId = "standard-ore-seam";

    /// <summary>The resource a completed installment pays.</summary>
    /// <remarks><c>docs/40-mining-and-extraction.md</c>:62 - "common ore".</remarks>
    public const string ResourceContentId = "common-ore";

    /// <summary>Common ore awarded per completed installment: <c>10</c>.</summary>
    /// <remarks><c>docs/40-mining-and-extraction.md</c>:62; DEC-083 sets the unit to ten.</remarks>
    public const int OrePerInstallment = 10;

    /// <summary>An installment's length in seconds of uninterrupted forward extraction: <c>1.5</c>.</summary>
    /// <remarks><c>docs/40-mining-and-extraction.md</c>:62.</remarks>
    public const double InstallmentSeconds = 1.5;

    /// <summary>Installments before the seam is depleted: <c>10</c>.</summary>
    /// <remarks><c>docs/40-mining-and-extraction.md</c>:62.</remarks>
    public const int InstallmentCount = 10;

    /// <summary>An installment's length in ticks: <c>90</c>.</summary>
    public const int InstallmentTicks = (int)(InstallmentSeconds * TickRate.TicksPerSecond);

    /// <summary>The seam's total payout: <c>100</c> common ore.</summary>
    /// <remarks>
    /// Derived, not restated. doc 40:62 gives 100 as the product of the two figures above, so a stored
    /// third number could disagree with its own factors; <c>MiningTests</c> asserts the product equals
    /// the 100 the document states.
    /// </remarks>
    public const int TotalOre = OrePerInstallment * InstallmentCount;

    /// <summary>The seam's total depletion time in ticks: <c>900</c>, or 15 seconds.</summary>
    public const int DepletionTicks = InstallmentTicks * InstallmentCount;
}
