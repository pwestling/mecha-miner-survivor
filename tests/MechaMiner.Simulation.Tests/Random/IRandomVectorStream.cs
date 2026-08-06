namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// One initialized stream, as the golden-vector renderer needs to observe it.
/// </summary>
/// <remarks>
/// Two implementations exist: one over the production types and one over the independent
/// reference implementation of doc 20 § Authoritative random-number contract. Every arithmetic
/// decision the goldens pin - the output transformation, the rejection threshold, the 53-bit
/// layout, and the zero-draw selection rule - is implemented separately on each side, so
/// <c>VER-SIM-005-004</c> compares two derivations rather than one implementation with itself.
/// </remarks>
internal interface IRandomVectorStream
{
    /// <summary>The stream's odd increment (doc 20 § Authoritative random-number
    /// contract).</summary>
    ulong Increment { get; }

    /// <summary>The state the next draw will read.</summary>
    ulong State { get; }

    /// <summary>How many caller-visible draws have been consumed.</summary>
    ulong DrawCount { get; }

    /// <summary>Draws the next raw 32-bit output.</summary>
    /// <returns>The output.</returns>
    uint NextUInt32();

    /// <summary>Draws an unbiased bounded integer by rejection sampling (doc 20 § Authoritative
    /// random-number contract).</summary>
    /// <param name="bound">The exclusive upper bound.</param>
    /// <returns>A value in <c>[0, bound)</c>.</returns>
    uint NextBounded(uint bound);

    /// <summary>Draws a <c>[0,1)</c> double from 53 bits (doc 20 § Authoritative random-number
    /// contract).</summary>
    /// <returns>The value.</returns>
    double NextUnitDouble();

    /// <summary>
    /// Selects an index into a canonically ordered candidate list, consuming no draw when the
    /// outcome is already determined (doc 20 § Authoritative random-number contract).
    /// </summary>
    /// <param name="candidateCount">How many candidates there are.</param>
    /// <param name="selectedIndex">The selected index, when there was one.</param>
    /// <returns><see langword="false"/> only for an empty candidate list.</returns>
    bool TrySelectIndex(int candidateCount, out int selectedIndex);
}
