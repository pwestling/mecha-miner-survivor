using System;
using System.Globalization;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// The single comparison and the single sort used for every event batch: tick, then system phase,
/// then emission sequence.
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
/// <b>Three keys, not four.</b> The emission sequence is per-tick global - <c>CMP-SIM-003</c>
/// issues it monotonically across the whole tick regardless of phase or emitter - so
/// <c>(tick, sequence)</c> is a total order by itself and no identity tiebreak can be reached by a
/// legal input. An earlier revision ended the comparison with the full emitting entity ID; that key
/// was unreachable, and worse, it meant a duplicate sequence quietly received an order instead of
/// being reported. The uniqueness it presupposed is now checked instead of assumed, by
/// <see cref="AssertSequenceUniqueWithinTick(DomainEvent[], int)"/>.
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
    /// <summary>
    /// How many contiguous same-phase runs one tick's sorted batch can contain.
    /// </summary>
    /// <remarks>
    /// doc 10 § System phase ordering numbers a fixed fourteen phases, and those numerals are stable
    /// normative identifiers: renumbering is forbidden, a new phase takes the next unused number,
    /// and a subdivision keeps its parent's. Fourteen is therefore a contract bound rather than a
    /// guess, which is what lets the uniqueness check size its cursors on the stack and allocate
    /// nothing.
    /// </remarks>
    private const int MaximumPhaseRuns = EventProvenance.LastSystemPhase - EventProvenance.FirstSystemPhase + 1;

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
    /// affordable every tick. Delegates to
    /// <see cref="AssertSequenceUniqueWithinTick(DomainEvent[], int)"/> for the duplicates the
    /// adjacency scan cannot see, because the batch is a total order only if that holds.
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

        AssertSequenceUniqueWithinTick(events, count);
    }

    /// <summary>
    /// Asserts that no two records in a sorted batch share a tick and an emission sequence.
    /// </summary>
    /// <param name="events">The already-sorted buffer.</param>
    /// <param name="count">How many leading elements are live.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Two records in one tick carry the same emission sequence, so <c>CMP-SIM-003</c> issued one
    /// sequence twice.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the invariant that replaced the old fourth ordering key. The sequence being per-tick
    /// global is what makes three keys a total order, so it has to be checked somewhere; leaving it
    /// as an assumption is what made the removed key look necessary while being unreachable.
    /// </para>
    /// <para>
    /// <b>Why the adjacency scan in <see cref="AssertTotalOrder(DomainEvent[], int)"/> is not
    /// enough.</b> The batch is sorted by tick, then phase, then sequence, so two records sharing a
    /// tick and a sequence are adjacent only when they also share a phase. A duplicate issued to two
    /// different phases lands in two different blocks and the adjacency scan walks straight past it.
    /// That is the whole failure mode: a check that only intercepts the routes someone enumerated.
    /// </para>
    /// <para>
    /// <b>Method.</b> Within one tick the sorted batch is at most
    /// <see cref="MaximumPhaseRuns"/> contiguous same-phase blocks, each with strictly increasing
    /// sequences. Merging those blocks by sequence yields every sequence in the tick in ascending
    /// order, and equal values necessarily come out consecutively, so one comparison against the
    /// previous emitted value finds any duplicate wherever it came from. The cursors are a
    /// <see langword="stackalloc"/> span bounded by a documented constant, so this allocates nothing
    /// on the managed heap and stays inside the tick's allocation budget.
    /// </para>
    /// </remarks>
    public static void AssertSequenceUniqueWithinTick(DomainEvent[] events, int count)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, events.Length);

        Span<int> cursors = stackalloc int[MaximumPhaseRuns];
        Span<int> ends = stackalloc int[MaximumPhaseRuns];

        int tickStart = 0;
        while (tickStart < count)
        {
            long tick = events[tickStart].Provenance.Tick;
            int tickEnd = tickStart;
            while (tickEnd < count && events[tickEnd].Provenance.Tick == tick)
            {
                tickEnd++;
            }

            int runs = 0;
            int runStart = tickStart;
            for (int index = tickStart + 1; index <= tickEnd; index++)
            {
                if (index < tickEnd
                    && events[index].Provenance.SystemPhase == events[runStart].Provenance.SystemPhase)
                {
                    continue;
                }

                if (runs == MaximumPhaseRuns)
                {
                    throw new InvalidOperationException(BuildPhaseRunMessage(tick, "domain"));
                }

                cursors[runs] = runStart;
                ends[runs] = index;
                runs++;
                runStart = index;
            }

            long previous = 0;
            bool hasPrevious = false;
            for (int emitted = tickStart; emitted < tickEnd; emitted++)
            {
                int chosen = -1;
                long smallest = 0;
                for (int run = 0; run < runs; run++)
                {
                    if (cursors[run] >= ends[run])
                    {
                        continue;
                    }

                    long candidate = events[cursors[run]].Provenance.Sequence;
                    if (chosen < 0 || candidate < smallest)
                    {
                        chosen = run;
                        smallest = candidate;
                    }
                }

                if (hasPrevious && smallest == previous)
                {
                    throw new InvalidOperationException(BuildTieMessage(tick, smallest, "domain"));
                }

                previous = smallest;
                hasPrevious = true;
                cursors[chosen]++;
            }

            tickStart = tickEnd;
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

        AssertSequenceUniqueWithinTick(events, count);
    }

    /// <summary>
    /// Asserts that no two records in a sorted presentation batch share a tick and an emission
    /// sequence.
    /// </summary>
    /// <param name="events">The already-sorted buffer.</param>
    /// <param name="count">How many leading elements are live.</param>
    /// <exception cref="ArgumentNullException"><paramref name="events"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Two records in one tick share an emission sequence.</exception>
    /// <remarks>
    /// The same check as <see cref="AssertSequenceUniqueWithinTick(DomainEvent[], int)"/>, whose
    /// remarks give the method and the reason. Duplicated per event type rather than unified behind
    /// an interface for the same reason <see cref="Sort(DomainEvent[], int)"/> is: an indirect call
    /// per comparison is not affordable on this path.
    /// </remarks>
    public static void AssertSequenceUniqueWithinTick(PresentationEvent[] events, int count)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, events.Length);

        Span<int> cursors = stackalloc int[MaximumPhaseRuns];
        Span<int> ends = stackalloc int[MaximumPhaseRuns];

        int tickStart = 0;
        while (tickStart < count)
        {
            long tick = events[tickStart].Provenance.Tick;
            int tickEnd = tickStart;
            while (tickEnd < count && events[tickEnd].Provenance.Tick == tick)
            {
                tickEnd++;
            }

            int runs = 0;
            int runStart = tickStart;
            for (int index = tickStart + 1; index <= tickEnd; index++)
            {
                if (index < tickEnd
                    && events[index].Provenance.SystemPhase == events[runStart].Provenance.SystemPhase)
                {
                    continue;
                }

                if (runs == MaximumPhaseRuns)
                {
                    throw new InvalidOperationException(BuildPhaseRunMessage(tick, "presentation"));
                }

                cursors[runs] = runStart;
                ends[runs] = index;
                runs++;
                runStart = index;
            }

            long previous = 0;
            bool hasPrevious = false;
            for (int emitted = tickStart; emitted < tickEnd; emitted++)
            {
                int chosen = -1;
                long smallest = 0;
                for (int run = 0; run < runs; run++)
                {
                    if (cursors[run] >= ends[run])
                    {
                        continue;
                    }

                    long candidate = events[cursors[run]].Provenance.Sequence;
                    if (chosen < 0 || candidate < smallest)
                    {
                        chosen = run;
                        smallest = candidate;
                    }
                }

                if (hasPrevious && smallest == previous)
                {
                    throw new InvalidOperationException(BuildTieMessage(tick, smallest, "presentation"));
                }

                previous = smallest;
                hasPrevious = true;
                cursors[chosen]++;
            }

            tickStart = tickEnd;
        }
    }

    private static string BuildPhaseRunMessage(long tick, string channel)
    {
        return "the "
            + channel
            + " batch for tick "
            + tick.ToString(CultureInfo.InvariantCulture)
            + " contains more than "
            + MaximumPhaseRuns.ToString(CultureInfo.InvariantCulture)
            + " contiguous same-phase runs, so either the batch is not sorted by phase or a record "
            + "carries a phase outside doc 10 § System phase ordering's fourteen stable numerals.";
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
