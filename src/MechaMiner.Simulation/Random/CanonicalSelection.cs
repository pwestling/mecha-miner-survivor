using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// Selection from a collection: establish canonical candidate order, then draw an index. An
/// empty or singleton selection consumes no draw.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// selection rules: "Selection from a collection first establishes canonical candidate order,
/// then draws an index"; the selection rules: "An empty/singleton selection consumes no draw;
/// this convention is fixture-pinned." The fixture is
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-degenerate-selection.txt</c>.
/// </para>
/// <para>
/// The zero-draw rule matters because it makes a guaranteed-outcome selection free: adding a
/// selection over one candidate cannot shift any later value in the stream, so content can gain
/// a degenerate choice without invalidating a recorded run. Note the split doc 20 draws —
/// <em>selection</em> short-circuits, while
/// <see cref="BoundedRandom.NextBounded"/> with a bound of one still consumes its draw because
/// it is the canonical PCG primitive rather than a selection.
/// </para>
/// <para>
/// The canonical order must be a strict total order over the candidates. A comparer that
/// reports two candidates equal would leave their relative position decided by authored input
/// order, which is precisely what doc 20 § Authoritative random-number contract forbids: stable
/// ordinals "come from canonical manifest/order rules, never dictionary or scene enumeration".
/// A tie is therefore refused rather than silently resolved.
/// </para>
/// </remarks>
public static class CanonicalSelection
{
    /// <summary>
    /// Orders the candidates canonically and then draws one, consuming no draw when the outcome
    /// is already determined.
    /// </summary>
    /// <typeparam name="TCandidate">The candidate type.</typeparam>
    /// <param name="source">The stream to draw the index from.</param>
    /// <param name="candidates">The candidates in authored input order.</param>
    /// <param name="canonicalOrder">The strict total order that defines canonical
    /// order.</param>
    /// <param name="selected">The selected candidate, when there was one.</param>
    /// <returns><see langword="false"/> only when <paramref name="candidates"/> is
    /// empty.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="canonicalOrder"/> reports two candidates equal, so it is not a strict
    /// total order and authored input order would decide the outcome.
    /// </exception>
    public static bool TrySelect<TCandidate>(
        IRandomSource source,
        IReadOnlyList<TCandidate> candidates,
        IComparer<TCandidate> canonicalOrder,
        [MaybeNullWhen(false)] out TCandidate? selected)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(canonicalOrder);

        if (candidates.Count == 0)
        {
            selected = default;
            return false;
        }

        if (candidates.Count == 1)
        {
            selected = candidates[0];
            return true;
        }

        TCandidate[] ordered = Order(candidates, canonicalOrder);
        uint index = BoundedRandom.NextBounded(source, (uint)ordered.Length);
        selected = ordered[index];
        return true;
    }

    /// <summary>
    /// Draws one candidate from a list already in canonical order, consuming no draw when the
    /// outcome is already determined.
    /// </summary>
    /// <typeparam name="TCandidate">The candidate type.</typeparam>
    /// <param name="source">The stream to draw the index from.</param>
    /// <param name="orderedCandidates">The candidates, already canonically ordered.</param>
    /// <param name="selected">The selected candidate, when there was one.</param>
    /// <returns><see langword="false"/> only when <paramref name="orderedCandidates"/> is
    /// empty.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <remarks>
    /// For callers that already hold a canonical manifest order and must not allocate to
    /// re-establish it. The caller owns the ordering guarantee doc 20 § Authoritative
    /// random-number contract requires.
    /// </remarks>
    public static bool TrySelectFromCanonicalOrder<TCandidate>(
        IRandomSource source,
        IReadOnlyList<TCandidate> orderedCandidates,
        [MaybeNullWhen(false)] out TCandidate? selected)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(orderedCandidates);

        if (orderedCandidates.Count == 0)
        {
            selected = default;
            return false;
        }

        if (orderedCandidates.Count == 1)
        {
            selected = orderedCandidates[0];
            return true;
        }

        uint index = BoundedRandom.NextBounded(source, (uint)orderedCandidates.Count);
        selected = orderedCandidates[(int)index];
        return true;
    }

    /// <summary>
    /// Returns the candidates in canonical order, refusing an order that is not strict.
    /// </summary>
    /// <typeparam name="TCandidate">The candidate type.</typeparam>
    /// <param name="candidates">The candidates in authored input order.</param>
    /// <param name="canonicalOrder">The strict total order that defines canonical
    /// order.</param>
    /// <returns>A new array in canonical order.</returns>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="canonicalOrder"/> reports two candidates equal.
    /// </exception>
    public static TCandidate[] Order<TCandidate>(
        IReadOnlyList<TCandidate> candidates,
        IComparer<TCandidate> canonicalOrder)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(canonicalOrder);

        TCandidate[] ordered = new TCandidate[candidates.Count];
        for (int index = 0; index < candidates.Count; index++)
        {
            ordered[index] = candidates[index];
        }

        Array.Sort(ordered, canonicalOrder);

        for (int index = 1; index < ordered.Length; index++)
        {
            if (canonicalOrder.Compare(ordered[index - 1], ordered[index]) == 0)
            {
                throw new InvalidOperationException(
                    "canonical candidate order is not a strict total order: two of the "
                        + ordered.Length.ToString(CultureInfo.InvariantCulture)
                        + " candidates compare equal, so authored input order would decide the draw. "
                        + "doc 20 § Authoritative random-number contract requires canonical manifest/order rules, never enumeration order");
            }
        }

        return ordered;
    }
}
