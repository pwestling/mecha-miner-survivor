using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Mining;

/// <summary>
/// The extraction rules shared by every mining-point class, plus the one number in this package that
/// no document on this ref states.
/// </summary>
/// <remarks>
/// The grace and decay figures below are authoritative and cited. The zone radius is not, and it is
/// isolated in this type rather than buried in a site profile so that the sourced and the unsourced
/// cannot be mistaken for each other.
/// </remarks>
public static class GrayboxExtraction
{
    /// <summary>
    /// The extraction-zone radius in gameplay meters: <c>2.0</c>. <b>Graybox: no source claimed.</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No document in this branch's ancestry states an extraction-zone radius, and two say outright
    /// that it is open.</b> <c>docs/40-mining-and-extraction.md</c> § Automatic proximity
    /// activation:48 - "Exact radius and any resource-specific size variation remain open."
    /// <c>docs/decisions/DEC-031-circular-mining-zone-and-fast-decay.md</c>:51 - "Exact zone radius,
    /// resource-specific exceptions, fractional ore behavior, and forced-displacement treatment remain
    /// open in OQ-004." <c>docs/10-core-game-loop.md</c>:75 says the same a third time. The documents
    /// that do quantify a radius quantify only <em>modifiers</em> to it: doc 62:48's <c>+3%</c>
    /// per rank and doc 68:44's <c>+10% -&gt; +25%</c> are percentages of a base neither states.
    /// </para>
    /// <para>
    /// <b>So 2.0 is chosen here and cited to nothing.</b> It is four mech collision diameters wide
    /// against the 1.0 m diameter of doc 72:40, which is large enough that a player can move inside the
    /// zone rather than standing still - the "circling or weaving inside the point" DEC-031:42 wants -
    /// and small enough to read as a distinct place on a camera showing 24 m vertically. Both of those
    /// are arguments for a plausible value, not derivations of this one, and calling them a source would
    /// be worse than admitting there is none.
    /// </para>
    /// <para>
    /// <b>It is deliberately not 3.0.</b> A 3.0 m radius is stated on a sibling branch that is not in
    /// this one's ancestry, so citing it here would be citing a document this build does not carry, and
    /// writing 3.0 without the citation would silently reproduce a number whose provenance a reader
    /// could not check. When that decision merges, this constant becomes a one-line change with a real
    /// citation, which is the whole reason it is one constant in one place.
    /// </para>
    /// </remarks>
    public const double ZoneRadiusMeters = 2.0;

    /// <summary>The grace period after leaving a zone, in seconds: <c>0.5</c>.</summary>
    /// <remarks>
    /// <c>docs/40-mining-and-extraction.md</c> § Progress decay:52 - "Leaving the circular zone begins
    /// a 0.5-second grace period. Unfinished progress holds steady during that grace period."
    /// <c>docs/decisions/DEC-031-circular-mining-zone-and-fast-decay.md</c>:14 states the same.
    /// </remarks>
    public const double ExitGraceSeconds = 0.5;

    /// <summary>The grace period as a whole number of ticks: <c>30</c>.</summary>
    public const int ExitGraceTicks = (int)(ExitGraceSeconds * TickRate.TicksPerSecond);

    /// <summary>How much faster unfinished progress decays than it accrues: <c>4</c>.</summary>
    /// <remarks>
    /// <para>
    /// <c>docs/40-mining-and-extraction.md</c>:52 - "If the player remains outside after it expires,
    /// progress decays linearly at four times that point's forward extraction rate." Expressed as a
    /// multiple of the forward rate rather than as a duration, exactly as the document does, so it stays
    /// correct for a site class whose installment is a different length: doc 40:54 states the
    /// consequence the multiple produces - "half of a full extraction's progress disappears in one
    /// eighth of the time needed to complete that extraction from zero".
    /// </para>
    /// <para>
    /// Because forward progress is one tick of credit per tick, decay is four ticks of credit per tick,
    /// in integers. That is why progress is stored in ticks rather than as a fraction: a floating
    /// fraction decayed by <c>4 * (1/90)</c> would leave a residue that never reaches zero, and
    /// "Decaying --&gt; Available: Progress decays to zero" in doc 40's own state diagram would be a
    /// transition that never fires.
    /// </para>
    /// </remarks>
    public const int DecayRateMultiple = 4;
}
