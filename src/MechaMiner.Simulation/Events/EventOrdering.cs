using System;
using System.Globalization;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// The single comparison and the single sort used for every event batch: system phase, then
/// emission sequence, then the full entity ID.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering: "Simultaneous
/// outcomes use documented stable ordering rather than collection or thread timing."
/// <c>docs/technical/20-simulation-core.md</c> § Entity identity: "Stable ordering uses the full
/// entity ID after a system's authored priority keys." § Boundary and tie ordering repeats the
/// pattern for damage: "resolve by system phase, explicit attack sequence, target ID, source ID,
/// then insertion sequence."
/// </para>
/// <para>
/// One comparison in one place, so the domain batch and the presentation batch cannot drift apart
/// and so a reviewer has one thing to check. Both sorts are in-place over a caller-owned array and
/// allocate nothing, which the combat path needs
/// (<c>docs/technical/22-combat-and-weapon-runtime.md</c> § Performance and capacity: "zero
/// steady-state managed allocation").
/// </para>
/// <para>
/// <b>Heapsort rather than a library sort with a comparer.</b> A comparison delegate would allocate
/// on first use and put an indirect call in the inner loop; more importantly, an in-place algorithm
/// written here is auditable against the documented rule. Stability is irrelevant because the
/// comparison is a total order over any batch whose (tick, sequence) pairs are unique -
/// <see cref="AssertTotalOrder"/> proves that they are rather than assuming it.
/// </para>
/// </remarks>
public static class EventOrdering
{
    /// <summary>The documented comparison for two domain events.</summary>
    public static int Compare(DomainEvent left, DomainEvent right)
    {
        return EventProvenance.Compare(left.Provenance, right.Provenance);
    }

    /// <summary>The documented comparison for two presentation events.</summary>
    public static int Compare(PresentationEvent left, PresentationEvent right)
    {
        return EventProvenance.Compare(left.Provenance, right.Provenance);
    }

    /// <summary>
    /// Sorts the first <paramref name="count"/> elements of <paramref name="events"/> in place into
    /// the documented order.
    /// </summary>
    /// <param name="events">The caller-owned buffer to sort.</param>
    /// <param name="count">How many leading elements are live.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative or exceeds the buffer.</exception>
    public static void Sort(DomainEvent[] events, int count)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, events.Length);

        for (int root = (count / 2) - 1; root >= 0; root--)
        {
            SiftDown(events, root, count);
        }

        for (int end = count - 1; end > 0; end--)
        {
            (events[0], events[end]) = (events[end], events[0]);
            SiftDown(events, 0, end);
        }
    }

    /// <summary>
    /// Sorts the first <paramref name="count"/> elements of <paramref name="events"/> in place into
    /// the documented order.
    /// </summary>
    /// <param name="events">The caller-owned buffer to sort.</param>
    /// <param name="count">How many leading elements are live.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative or exceeds the buffer.</exception>
    public static void Sort(PresentationEvent[] events, int count)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, events.Length);

        for (int root = (count / 2) - 1; root >= 0; root--)
        {
            SiftDown(events, root, count);
        }

        for (int end = count - 1; end > 0; end--)
        {
            (events[0], events[end]) = (events[end], events[0]);
            SiftDown(events, 0, end);
        }
    }

    /// <summary>
    /// Asserts that a sorted batch is a total order: no two adjacent records compare equal.
    /// </summary>
    /// <param name="events">The already-sorted buffer.</param>
    /// <param name="count">How many leading elements are live.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Two records share a tick, phase, sequence, and emitting entity, so their relative order would
    /// be decided by nothing.
    /// </exception>
    /// <remarks>
    /// Checked rather than assumed, and checked on the resulting batch rather than at the call that
    /// appended: a duplicate sequence is exactly the defect that would let collection order decide
    /// an outcome, and it is invisible until two records land next to each other. Linear, so it is
    /// affordable every tick.
    /// </remarks>
    public static void AssertTotalOrder(DomainEvent[] events, int count)
    {
        ArgumentNullException.ThrowIfNull(events);
        for (int index = 1; index < count; index++)
        {
            if (Compare(events[index - 1], events[index]) == 0)
            {
                throw new InvalidOperationException(BuildTieMessage(
                    events[index].Provenance.Tick,
                    events[index].Provenance.Sequence,
                    "domain"));
            }
        }
    }

    /// <summary>
    /// Asserts that a sorted presentation batch is a total order: no two adjacent records compare
    /// equal.
    /// </summary>
    /// <param name="events">The already-sorted buffer.</param>
    /// <param name="count">How many leading elements are live.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Two records would be ordered by nothing.</exception>
    public static void AssertTotalOrder(PresentationEvent[] events, int count)
    {
        ArgumentNullException.ThrowIfNull(events);
        for (int index = 1; index < count; index++)
        {
            if (Compare(events[index - 1], events[index]) == 0)
            {
                throw new InvalidOperationException(BuildTieMessage(
                    events[index].Provenance.Tick,
                    events[index].Provenance.Sequence,
                    "presentation"));
            }
        }
    }

    private static string BuildTieMessage(long tick, long sequence, string channel)
    {
        return "two "
            + channel
            + " events share tick "
            + tick.ToString(CultureInfo.InvariantCulture)
            + " and emission sequence "
            + sequence.ToString(CultureInfo.InvariantCulture)
            + ", so their relative order would be decided by collection timing. doc 10 § System "
            + "phase ordering forbids that; CMP-SIM-003 owns the sequence and must issue each one "
            + "once.";
    }

    private static void SiftDown(DomainEvent[] events, int root, int end)
    {
        int current = root;
        while (true)
        {
            int left = (2 * current) + 1;
            if (left >= end)
            {
                return;
            }

            int largest = left;
            int right = left + 1;
            if (right < end && Compare(events[right], events[left]) > 0)
            {
                largest = right;
            }

            if (Compare(events[largest], events[current]) <= 0)
            {
                return;
            }

            (events[current], events[largest]) = (events[largest], events[current]);
            current = largest;
        }
    }

    private static void SiftDown(PresentationEvent[] events, int root, int end)
    {
        int current = root;
        while (true)
        {
            int left = (2 * current) + 1;
            if (left >= end)
            {
                return;
            }

            int largest = left;
            int right = left + 1;
            if (right < end && Compare(events[right], events[left]) > 0)
            {
                largest = right;
            }

            if (Compare(events[largest], events[current]) <= 0)
            {
                return;
            }

            (events[current], events[largest]) = (events[largest], events[current]);
            current = largest;
        }
    }
}
