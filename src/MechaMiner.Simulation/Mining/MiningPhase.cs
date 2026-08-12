namespace MechaMiner.Simulation.Mining;

/// <summary>
/// The four states of a mining point, exactly as <c>docs/40-mining-and-extraction.md</c> § Mining
/// state flow:30-40 names them.
/// </summary>
/// <remarks>
/// The names are the document's own node labels rather than paraphrases, so a reader comparing the two
/// is comparing identical words. The diagram's fifth node is the terminal <c>[*]</c>, which is not a
/// state a record can be in.
/// </remarks>
public enum MiningPhase
{
    /// <summary>No unfinished progress and the mech is not extracting.</summary>
    Available = 0,

    /// <summary>Forward progress is accruing, or is held during the exit grace.</summary>
    Extracting = 1,

    /// <summary>The exit grace has expired and unfinished progress is decaying.</summary>
    Decaying = 2,

    /// <summary>Every installment has been paid; the point is spent for the rest of the run.</summary>
    Complete = 3,
}
