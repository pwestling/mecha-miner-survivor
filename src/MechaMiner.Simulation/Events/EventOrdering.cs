using System;
using System.Globalization;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// The single comparison and the single sort used for every event batch: tick, then emission
/// sequence.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering: "Simultaneous
/// outcomes use documented stable ordering rather than collection or thread timing."
/// </para>
/// <para>
/// One comparison in one place, so the domain batch and the presentation batch cannot drift apart
/// and so a reviewer has one thing to check. Both sorts are in-place over a caller-owned array and
/// allocate nothing, which the combat path needs
/// (<c>docs/technical/22-combat-and-weapon-runtime.md</c> § Performance and capacity: "zero
/// steady-state managed allocation").
/// </para>
/// <para>
/// <b>Two keys.</b> The emission sequence is per-tick global - <c>CMP-SIM-003</c> issues it
/// monotonically across the whole tick regardless of phase or emitter - so <c>(tick, sequence)</c>
/// is a total order by itself and nothing after it can discriminate a legal pair. Earlier revisions
/// carried a phase key and then an entity-ID key after the sequence; both were unreachable for every
/// legal input, and the entity-ID key was actively harmful because it meant a duplicate sequence
/// quietly received an order instead of being reported.
/// </para>
/// <para>
/// <b>Each removed key left a fact behind, and each fact is now checked.</b> The entity-ID key was
/// redundant because the sequence is unique within a tick, and with a two-key comparator two records
/// sharing a tick and a sequence compare equal, so
/// <see cref="AssertTotalOrder(DomainEvent[], int)"/>'s adjacency scan <em>is</em> that uniqueness
/// check. The phase key was redundant because phase never decreases as the sequence rises, and
/// <see cref="AssertPhaseAgreesWithSequenceWithinTick(DomainEvent[], int)"/> is that one. Removing a
/// key while keeping the reason it was removable is the whole point; deleting both together would
/// have thrown away two real invariants.
/// </para>
/// <para>
/// <b>This is the event rule only.</b> <c>docs/technical/20-simulation-core.md</c> § Boundary and
/// tie ordering defines a separate five-key sort for damage instances - "resolve by system phase,
/// explicit attack sequence, target ID, source ID, then insertion sequence" - which does carry
/// identity keys. Nothing here applies to it.
/// </para>
/// <para>
/// <b>Heapsort rather than a library sort with a comparer.</b> A comparison delegate would allocate
/// on first use and put an indirect call in the inner loop; more importantly, an in-place algorithm
/// written here is auditable against the documented rule. Stability is irrelevant because the
/// comparison is a total order over any batch whose (tick, sequence) pairs are unique -
/// <see cref="AssertTotalOrder(DomainEvent[], int)"/> proves that they are rather than assuming it.
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
    /// affordable every tick. With a two-key comparator this adjacency scan is the complete
    /// uniqueness check: two records sharing a tick and a sequence compare equal and therefore land
    /// next to each other, whatever their phases or emitters. Then delegates to
    /// <see cref="AssertPhaseAgreesWithSequenceWithinTick(DomainEvent[], int)"/>, the other invariant
    /// the batch's order depends on.
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

        AssertPhaseAgreesWithSequenceWithinTick(events, count);
    }

    /// <summary>
    /// Asserts that within each tick the system phase is non-decreasing along ascending emission
    /// sequence.
    /// </summary>
    /// <param name="events">The already-sorted buffer.</param>
    /// <param name="count">How many leading elements are live.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// A later-sequenced event carries an earlier phase than one before it in the same tick, so some
    /// system emitted out of phase order.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>This is the fact that lets the phase leave the comparator, so it is checked rather than
    /// assumed.</b> The sequence is issued at emission and emission happens in phase order, so a
    /// lower phase in one tick always carries a lower sequence. That is exactly why sorting by
    /// <c>(tick, sequence)</c> already produces phase order and why a phase key would be dead. Drop
    /// the key without checking the fact and the fact is simply gone - which is how a comparator ends
    /// up with keys nobody can explain.
    /// </para>
    /// <para>
    /// It is also a useful check in its own right, not merely bookkeeping: a system that emits during
    /// a phase it does not belong to, or that caches a sequence across a phase boundary and emits it
    /// later, produces exactly this shape. doc 10 § System phase ordering fixes the phase order and
    /// says "observable ordering changes require regression tests", and this is what notices.
    /// </para>
    /// <para>
    /// One linear pass with two variables over a batch already sorted by <c>(tick, sequence)</c>, so
    /// it allocates nothing and costs a comparison per record.
    /// </para>
    /// </remarks>
    public static void AssertPhaseAgreesWithSequenceWithinTick(DomainEvent[] events, int count)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, events.Length);

        for (int index = 1; index < count; index++)
        {
            EventProvenance previous = events[index - 1].Provenance;
            EventProvenance current = events[index].Provenance;
            if (previous.Tick != current.Tick)
            {
                continue;
            }

            if (current.SystemPhase < previous.SystemPhase)
            {
                throw new InvalidOperationException(BuildPhaseDisagreementMessage(
                    current.Tick,
                    previous.SystemPhase,
                    previous.Sequence,
                    current.SystemPhase,
                    current.Sequence,
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

        AssertPhaseAgreesWithSequenceWithinTick(events, count);
    }

    /// <summary>
    /// Asserts that within each tick the system phase is non-decreasing along ascending emission
    /// sequence.
    /// </summary>
    /// <param name="events">The already-sorted buffer.</param>
    /// <param name="count">How many leading elements are live.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Some system emitted out of phase order.</exception>
    /// <remarks>
    /// The same check as
    /// <see cref="AssertPhaseAgreesWithSequenceWithinTick(DomainEvent[], int)"/>, whose remarks give
    /// the reason. Duplicated per event type rather than unified behind an interface for the same
    /// reason <see cref="Sort(DomainEvent[], int)"/> is: an indirect call per comparison is not
    /// affordable on this path.
    /// </remarks>
    public static void AssertPhaseAgreesWithSequenceWithinTick(PresentationEvent[] events, int count)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, events.Length);

        for (int index = 1; index < count; index++)
        {
            EventProvenance previous = events[index - 1].Provenance;
            EventProvenance current = events[index].Provenance;
            if (previous.Tick != current.Tick)
            {
                continue;
            }

            if (current.SystemPhase < previous.SystemPhase)
            {
                throw new InvalidOperationException(BuildPhaseDisagreementMessage(
                    current.Tick,
                    previous.SystemPhase,
                    previous.Sequence,
                    current.SystemPhase,
                    current.Sequence,
                    "presentation"));
            }
        }
    }

    private static string BuildPhaseDisagreementMessage(
        long tick,
        int previousPhase,
        long previousSequence,
        int currentPhase,
        long currentSequence,
        string channel)
    {
        return "in tick "
            + tick.ToString(CultureInfo.InvariantCulture)
            + " a "
            + channel
            + " event from system phase "
            + currentPhase.ToString(CultureInfo.InvariantCulture)
            + " carries emission sequence "
            + currentSequence.ToString(CultureInfo.InvariantCulture)
            + ", which is later than sequence "
            + previousSequence.ToString(CultureInfo.InvariantCulture)
            + " from phase "
            + previousPhase.ToString(CultureInfo.InvariantCulture)
            + ". The sequence is issued at emission and emission happens in phase order, so phase "
            + "must not decrease as the sequence rises; doc 10 § System phase ordering fixes that "
            + "order. Either a system emitted outside its phase or it held a sequence across a "
            + "phase boundary before emitting.";
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
