using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// Proves that simultaneous events use the documented stable ordering, and that buffers are tick-local and
/// start empty.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-006-011</c>, <c>VER-SIM-006-008</c>. <c>VER-SIM-006-011</c> supersedes the
/// retired <c>VER-SIM-006-003</c>, which enumerated a three-key comparator - system phase, then
/// emission sequence, then the full entity ID - that no longer exists.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering: "Simultaneous outcomes use
/// documented stable ordering rather than collection or thread timing."
/// <c>docs/technical/20-simulation-core.md</c> § Boundary and tie ordering and § Tick transaction.
/// </remarks>
[TestFixture]
internal sealed class EventOrderingTests
{
    /// <summary>
    /// The provenance header committed with <c>events-simultaneous-ordering.txt</c>.
    /// </summary>
    /// <remarks>
    /// doc 91 § Determinism and fixture policy: "Golden outputs are canonical, ordered, and reviewable
    /// text". A reviewer needs the rule and the fixture, not only the bytes.
    /// </remarks>
    private const string GoldenHeader =
        "# events-simultaneous-ordering\n"
        + "#\n"
        + "# Rule under test: doc 10 § System phase ordering - \"Simultaneous outcomes use\n"
        + "# documented stable ordering rather than collection or thread timing.\" For an\n"
        + "# event batch the keys are: tick, then explicit emission sequence. That is all\n"
        + "# of them.\n"
        + "#\n"
        + "# Two keys, because the emission sequence is per-tick global: CMP-SIM-003 issues\n"
        + "# it monotonically across the whole tick regardless of phase or emitter, so\n"
        + "# (tick, sequence) is a total order on its own. Neither the emitting system phase\n"
        + "# nor the emitting entity ID can discriminate any legal pair, so neither is a\n"
        + "# sort key. Both survive as recorded provenance, and the two facts that made them\n"
        + "# redundant are now checked instead of assumed:\n"
        + "#\n"
        + "#   1. Sequence is unique within a tick. A duplicate is an impossible input - a\n"
        + "#      defect in the issuer, not a tie - so it fails loudly rather than falling\n"
        + "#      through to a further key that would silently order it and hide the bug.\n"
        + "#      Enforced by EventOrdering.AssertTotalOrder: with a two-key comparator two\n"
        + "#      records sharing a tick and a sequence compare equal, so the adjacency scan\n"
        + "#      is itself the uniqueness check.\n"
        + "#   2. Phase agrees with sequence. Sequence is assigned at emission and emission\n"
        + "#      happens in phase order, so along ascending sequence within one tick the\n"
        + "#      phase must be non-decreasing. This is precisely what makes phase redundant\n"
        + "#      as a sort key, so removing the key without checking the fact would have\n"
        + "#      thrown the fact away. A system emitting out of phase order is a real\n"
        + "#      defect and this names it. Enforced by\n"
        + "#      EventOrdering.AssertPhaseAgreesWithSequenceWithinTick.\n"
        + "#\n"
        + "# This scoping is events only. Doc 20 § Boundary and tie ordering defines a\n"
        + "# separate five-key sort for damage instances - system phase, explicit attack\n"
        + "# sequence, target ID, source ID, then insertion sequence - and that is untouched\n"
        + "# by the rule above.\n"
        + "#\n"
        + "# Two cases, so that each of the two keys is the sole discriminator in at least\n"
        + "# one of them and neither key is dead:\n"
        + "#\n"
        + "# 1. one-tick-published-batch - sole discriminator: emission sequence.\n"
        + "#    Run session 0xE7E00001, tick 7, eight domain events across four system\n"
        + "#    phases (3, 8, 10, 11), two emissions per phase. Sequence ascends 0 to 7 and\n"
        + "#    phase is non-decreasing with it, so the fixture is a legal input. The events\n"
        + "#    are appended in sequence order 5,0,3,6,1,7,2,4 - and that batch is also\n"
        + "#    published reversed - so neither append order is the documented order and the\n"
        + "#    sort cannot be a no-op. Every row shares tick 7, so this case is blind to a\n"
        + "#    comparator with no tick key at all: deleting that key left the whole suite\n"
        + "#    green, which is why case 2 exists and why the negative control asserts this\n"
        + "#    blindness rather than leaving it implied.\n"
        + "#\n"
        + "# 2. retained-multi-tick-records - sole discriminator: tick.\n"
        + "#    A retained record set, not a published batch: a published batch cannot\n"
        + "#    produce this shape, because doc 20 § Domain and presentation events makes the\n"
        + "#    sequence \"global to one tick\" and a DomainEventBuffer refuses a record from\n"
        + "#    another tick outright. The collection that legitimately holds several ticks'\n"
        + "#    events at once is the diagnostic artifact the same section names: \"Event\n"
        + "#    schemas are versioned when written to diagnostic artifacts\". Because the\n"
        + "#    sequence restarts at zero every tick, across ticks the sequence alone is not\n"
        + "#    an order at all and the tick is what supplies one. Rows 0 and 1 differ only\n"
        + "#    on tick - same phase, same sequence, same emitter, same subject, same kind,\n"
        + "#    same quantity - so for that pair the tick is the only component that can\n"
        + "#    decide anything. The tick-8 row carries sequence 0, lower than every other\n"
        + "#    row's, and still sorts last: that is the direct proof that tick precedes\n"
        + "#    sequence rather than the other way round.\n"
        + "#\n"
        + "# The previous fixture achieved case 1's non-no-op property by pairing phase 11\n"
        + "# with sequence 0 and phase 3 with sequence 1, deliberately disagreeing with\n"
        + "# phase order. Under the contract above that is an input the system cannot\n"
        + "# produce, so the old golden pinned an impossible state. Every observable field\n"
        + "# here is now a function of the emission sequence rather than of the append\n"
        + "# position, in both cases, so the rows stay derivable once append order stops\n"
        + "# matching them. That is also what makes case 2's tick-only pair possible: two\n"
        + "# rows at one sequence in different ticks agree in every other field by\n"
        + "# construction rather than by being written out twice.\n"
        + "#\n"
        + "# The stored phase numerals are correct by contract, not by coincidence: doc 10 §\n"
        + "# System phase ordering numbers a fixed fourteen phases and those numerals are\n"
        + "# stable normative identifiers. Renumbering is forbidden; a new phase takes the\n"
        + "# next unused number and a subdivision keeps its parent's. So a fixture may store\n"
        + "# 3, 8, 10, 11 and rely on their meaning.\n"
        + "#\n"
        + "# Every identity in either case - the four emitters and the shared subject - is\n"
        + "# drawn from the OrdinaryEnemy partition, which begins immediately after Player.\n"
        + "# Player's capacity is the doc 20 § Scope and invariants invariant \"exactly one\n"
        + "# player entity exists until terminal resolution\", so the OrdinaryEnemy offset is\n"
        + "# 1 by simulation invariant and no doc 22 § Performance and capacity combat\n"
        + "# ceiling precedes it. The fixture asserts that computed containment rather than\n"
        + "# assuming it. An earlier fixture drew its subject from Pickup, which sits after\n"
        + "# all three doc 22 ceilings, which is why the literal 3830 was baked in here and\n"
        + "# would have made this ordering golden fail when the enemy-projectile ceiling\n"
        + "# moved.\n"
        + "#\n"
        + "# Derived by: the documented rule read off doc 10 and doc 20, computed in an\n"
        + "# independent Python reference before any C# ran, and cross-checked against an\n"
        + "# independent list sort in EventOrderingTests. Not by accepting whatever the\n"
        + "# buffer emitted.\n"
        + "#\n";

    /// <summary>
    /// The emitting phase each emission sequence belongs to, indexed by sequence.
    /// </summary>
    /// <remarks>
    /// Two emissions per phase across four phases, with phase non-decreasing as the sequence rises,
    /// which is the legality condition
    /// <c>EventOrdering.AssertPhaseAgreesWithSequenceWithinTick</c> enforces.
    /// </remarks>
    private static readonly int[] PhaseOfSequence = [3, 3, 8, 8, 10, 10, 11, 11];

    /// <summary>
    /// The order the fixture's events are appended in, as emission sequences.
    /// </summary>
    /// <remarks>
    /// Neither this order nor its reverse is sequence order, so the sort cannot be a no-op - which is
    /// what the fixture needs, and it no longer costs an illegal input to get.
    /// </remarks>
    private static readonly int[] AppendSequences = [5, 0, 3, 6, 1, 7, 2, 4];

    /// <summary>
    /// The retained cross-tick case's rows, in an arrival order that is neither the documented order nor its
    /// reverse.
    /// </summary>
    /// <remarks>
    /// Ticks 6 and 7 both carry sequence 4 at phase 10, so those two rows differ only on tick and the tick is
    /// the only component that can order them. Tick 8 carries sequence 0, the lowest in the set, and still
    /// sorts last, which is what makes the tick's precedence over the sequence observable rather than assumed.
    /// Within tick 7 the phase does not decrease as the sequence rises - phase 10 at sequence 4, phase 11 at
    /// sequence 9 - so the set is a legal input on the same terms case 1 is.
    /// </remarks>
    private static readonly MultiTickRow[] MultiTickRows =
    [
        new MultiTickRow(7L, 9L, 11),
        new MultiTickRow(6L, 4L, 10),
        new MultiTickRow(8L, 0L, 3),
        new MultiTickRow(7L, 4L, 10),
    ];

    /// <summary>The name of the golden's first case, the one a published batch can produce.</summary>
    private const string OneTickCaseName = "one-tick-published-batch";

    /// <summary>The name of the golden's second case, the retained set that spans ticks.</summary>
    private const string MultiTickCaseName = "retained-multi-tick-records";

    /// <summary>
    /// Verification: <c>VER-SIM-006-011</c> (successor to the retired <c>VER-SIM-006-003</c>).
    ///
    /// Two runs that emit the same events in different append order produce identical batches, ordered
    /// by tick then emission sequence; a duplicate sequence within a tick fails loudly; and phase must
    /// not decrease as the sequence rises.
    /// </summary>
    [Test]
    public void SimultaneousEventsUseDocumentedStableOrdering()
    {
        List<DomainEvent> events = BuildSimultaneousEvents();

        List<DomainEvent> ascending = new(events);
        List<DomainEvent> reversed = new(events);
        reversed.Reverse();

        string firstRendering = EventContractAssertions.RenderDomainBatch(PublishOrdered(ascending));
        string secondRendering = EventContractAssertions.RenderDomainBatch(PublishOrdered(reversed));
        string expectedRendering = EventContractAssertions.RenderDomainBatch(ReferenceSort(events));

        EventContractAssertions.BatchOrderMatchesTheDocumentedComparison(
            "the domain event batch",
            expectedRendering,
            firstRendering,
            secondRendering);

        Expect.Multiple(() =>
        {
            Assert.That(
                firstRendering,
                Is.Not.EqualTo(EventContractAssertions.RenderDomainBatch(ascending)),
                "the fixture's append order must differ from the documented order, or sorting is a no-op "
                    + "and the gate proves nothing");
            Assert.That(
                firstRendering,
                Is.Not.EqualTo(EventContractAssertions.RenderDomainBatch(reversed)),
                "and it must differ from the reversed append order too");
        });

        AssertTheFixtureIsALegalInput(events);
        AssertOutOfPhaseOrderEmissionIsRejected();

        string multiTickRendering = RenderMultiTickCase();
        AssertEachCaseDetectsTheKeyItIsThereFor(events);

        GoldenText.Matches(
            "events-simultaneous-ordering.txt",
            GoldenHeader
                + RenderCase(OneTickCaseName, "emission sequence", firstRendering)
                + RenderCase(MultiTickCaseName, "tick", multiTickRendering));
    }

    /// <summary>Heads a case's block with its name and the component it leaves as the sole discriminator.</summary>
    /// <param name="caseName">The case's stable name.</param>
    /// <param name="soleDiscriminator">The key this case leaves as the only live one.</param>
    /// <param name="rendering">The case's ordered rows.</param>
    private static string RenderCase(string caseName, string soleDiscriminator, string rendering)
    {
        return "## case " + caseName + ": sole discriminator = " + soleDiscriminator + "\n" + rendering;
    }

    /// <summary>
    /// Orders the retained cross-tick set through the production comparison and returns its rows, after
    /// checking that the set really has the shape the case depends on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This case cannot go through a <c>DomainEventBuffer</c>, and that is the finding rather than an
    /// inconvenience: a buffer is tick-local and refuses a record from another tick, so every batch it can
    /// publish shares one tick and no batch reaches the comparator's leading key. The set is therefore sorted
    /// through <c>EventOrdering.Sort</c> directly, which is the same production comparison
    /// <c>CopyOrderedTo</c> uses, and <c>EventOrdering.AssertTotalOrder</c> is run over the result so the
    /// case still passes through the uniqueness and phase-agreement invariants a published batch would.
    /// </para>
    /// <para>
    /// Two arrival orders are ordered and compared, so the rows record the rule rather than the order the
    /// fixture happened to list.
    /// </para>
    /// </remarks>
    private static string RenderMultiTickCase()
    {
        List<DomainEvent> arrival = BuildMultiTickRecords();
        List<DomainEvent> reversed = new(arrival);
        reversed.Reverse();

        string rendering = EventContractAssertions.RenderDomainBatch(DocumentedSort(arrival));
        string reversedRendering = EventContractAssertions.RenderDomainBatch(DocumentedSort(reversed));

        Expect.Multiple(() =>
        {
            Assert.That(
                reversedRendering,
                Is.EqualTo(rendering).Using(StringComparer.Ordinal),
                "the documented comparison must be a total order over this set, so a reversed arrival order "
                    + "produces the identical result");
            Assert.That(
                rendering,
                Is.Not.EqualTo(EventContractAssertions.RenderDomainBatch(arrival)).Using(StringComparer.Ordinal),
                "the arrival order must differ from the documented order, or sorting is a no-op here too");
            Assert.That(
                rendering,
                Is.Not.EqualTo(EventContractAssertions.RenderDomainBatch(reversed)).Using(StringComparer.Ordinal),
                "and so must its reverse");
            AssertTheMultiTickSetHasThePairItClaims(arrival);
        });

        return rendering;
    }

    /// <summary>
    /// Asserts the retained set contains a pair of records differing only on tick, and a record whose tick is
    /// highest while its sequence is lowest.
    /// </summary>
    /// <param name="records">The retained set, in arrival order.</param>
    /// <remarks>
    /// Without the first fact the case does not reach the tick key at all and is exactly as vacuous as the
    /// one-tick case it was added to fix. Without the second, the case would not show that the tick
    /// <em>precedes</em> the sequence rather than merely being consulted.
    /// </remarks>
    private static void AssertTheMultiTickSetHasThePairItClaims(List<DomainEvent> records)
    {
        int tickOnlyPairs = 0;
        for (int left = 0; left < records.Count; left++)
        {
            for (int right = left + 1; right < records.Count; right++)
            {
                if (DiffersOnlyOnTick(records[left], records[right]))
                {
                    tickOnlyPairs++;
                }
            }
        }

        long highestTick = long.MinValue;
        long sequenceAtHighestTick = 0L;
        long lowestSequence = long.MaxValue;
        foreach (DomainEvent record in records)
        {
            if (record.Provenance.Tick > highestTick)
            {
                highestTick = record.Provenance.Tick;
                sequenceAtHighestTick = record.Provenance.Sequence;
            }

            if (record.Provenance.Sequence < lowestSequence)
            {
                lowestSequence = record.Provenance.Sequence;
            }
        }

        Assert.That(
            tickOnlyPairs,
            Is.EqualTo(1),
            "the retained set must hold exactly one pair of records that differ only on tick, or the tick key "
                + "is not the sole discriminator for anything in it");
        Assert.That(
            sequenceAtHighestTick,
            Is.EqualTo(lowestSequence),
            "the highest tick must carry the lowest sequence in the set, or nothing here shows that the tick "
                + "precedes the sequence rather than merely following it");
    }

    /// <summary>Whether two records agree in every rendered field except the tick.</summary>
    /// <param name="left">One record.</param>
    /// <param name="right">The other.</param>
    /// <remarks>
    /// Compared through the production rendering with the tick text substituted out, rather than field by
    /// field: a field added to <c>DomainEvent</c> then has to agree too, instead of quietly escaping a
    /// hand-written list.
    /// </remarks>
    private static bool DiffersOnlyOnTick(DomainEvent left, DomainEvent right)
    {
        if (left.Provenance.Tick == right.Provenance.Tick)
        {
            return false;
        }

        return string.Equals(
            WithoutTickText(left),
            WithoutTickText(right),
            StringComparison.Ordinal);
    }

    private static string WithoutTickText(DomainEvent record)
    {
        return record.ToString().Replace(
            "tick=" + record.Provenance.Tick.ToString(CultureInfo.InvariantCulture) + " ",
            "tick=* ",
            StringComparison.Ordinal);
    }

    /// <summary>
    /// Asserts which case notices each single-key degradation of the comparator, and which is blind to it.
    /// </summary>
    /// <param name="oneTickEvents">The one-tick case's records, in arrival order.</param>
    /// <remarks>
    /// <para>
    /// doc 91 § Acceptance evidence wants evidence a gate can fail. For an ordering golden the sharp question
    /// is whether its inputs reach every key, and the answer for the one-tick case was no: deleting the tick
    /// key from the comparator left all 138 tests green, because every row in the golden and every
    /// event-ordering fixture held the tick constant. The blindness is asserted here rather than described, so
    /// that a fixture change which silently removes the coverage the second case adds turns this red.
    /// </para>
    /// <para>
    /// Both degradations are expressed as reference comparisons over a list, independently of
    /// <c>EventOrdering</c>, for the same reason <see cref="ReferenceSort"/> is: the claim is about the rule,
    /// not about the implementation agreeing with itself.
    /// </para>
    /// </remarks>
    private static void AssertEachCaseDetectsTheKeyItIsThereFor(List<DomainEvent> oneTickEvents)
    {
        List<DomainEvent> multiTickEvents = BuildMultiTickRecords();

        Expect.Multiple(() =>
        {
            Assert.That(
                DetectsDegradation(oneTickEvents, SortWithoutTickKey),
                Is.False,
                "the one-tick case must be blind to a comparator with no tick key: every row shares tick 7, "
                    + "so deleting that key changes nothing there. This is the hole the retained case fills, "
                    + "and if it ever stops being a hole the reasoning behind that case has changed");
            Assert.That(
                DetectsDegradation(oneTickEvents, SortWithoutSequenceKey),
                Is.True,
                "and it must notice a comparator with no sequence key, which is the key it is there for");
            Assert.That(
                DetectsDegradation(multiTickEvents, SortWithoutTickKey),
                Is.True,
                "the retained case must notice a comparator with no tick key, which is the key it is there "
                    + "for and the defect this case was added to control");
            Assert.That(
                DetectsDegradation(multiTickEvents, SortWithoutSequenceKey),
                Is.True,
                "and it notices a missing sequence key too, because two of its rows share a tick");
        });
    }

    /// <summary>
    /// Whether a degraded comparison fails to reproduce the documented order for at least one arrival order.
    /// </summary>
    /// <param name="records">The case's records, in arrival order.</param>
    /// <param name="degraded">The degraded sort.</param>
    /// <remarks>
    /// Two arrival orders are tried, because a dropped key shows up as a tie and a tie under a stable sort is
    /// only visible as a disagreement between permutations.
    /// </remarks>
    private static bool DetectsDegradation(
        List<DomainEvent> records,
        Func<List<DomainEvent>, List<DomainEvent>> degraded)
    {
        List<DomainEvent> reversed = new(records);
        reversed.Reverse();

        string expected = EventContractAssertions.RenderDomainBatch(ReferenceSort(records));
        string ascending = EventContractAssertions.RenderDomainBatch(degraded(records));
        string descending = EventContractAssertions.RenderDomainBatch(degraded(reversed));

        return !string.Equals(ascending, expected, StringComparison.Ordinal)
            || !string.Equals(descending, expected, StringComparison.Ordinal);
    }

    /// <summary>The comparison with the tick key deleted: emission sequence only.</summary>
    /// <param name="events">The records to sort.</param>
    private static List<DomainEvent> SortWithoutTickKey(List<DomainEvent> events)
    {
        List<DomainEvent> sorted = new(events);
        sorted.Sort((left, right) =>
            left.Provenance.Sequence.CompareTo(right.Provenance.Sequence));
        return sorted;
    }

    /// <summary>The comparison with the sequence key deleted: tick only.</summary>
    /// <param name="events">The records to sort.</param>
    private static List<DomainEvent> SortWithoutSequenceKey(List<DomainEvent> events)
    {
        List<DomainEvent> sorted = new(events);
        sorted.Sort((left, right) => left.Provenance.Tick.CompareTo(right.Provenance.Tick));
        return sorted;
    }

    /// <summary>
    /// Sorts a record set through the production <c>EventOrdering</c> and runs the batch invariants over the
    /// result.
    /// </summary>
    /// <param name="records">The records, in whatever order they arrived.</param>
    private static List<DomainEvent> DocumentedSort(List<DomainEvent> records)
    {
        DomainEvent[] batch = new DomainEvent[records.Count];
        for (int index = 0; index < records.Count; index++)
        {
            batch[index] = records[index];
        }

        EventOrdering.Sort(batch, batch.Length);
        EventOrdering.AssertTotalOrder(batch, batch.Length);

        List<DomainEvent> sorted = new(batch.Length);
        foreach (DomainEvent record in batch)
        {
            sorted.Add(record);
        }

        return sorted;
    }

    /// <summary>
    /// The retained cross-tick records, built from <see cref="MultiTickRows"/> by the same field derivation
    /// case 1 uses.
    /// </summary>
    /// <remarks>
    /// Every observable field is a function of the emission sequence, which is what lets two rows at one
    /// sequence in different ticks agree in every field but the tick by construction rather than by being
    /// spelled out twice.
    /// </remarks>
    private static List<DomainEvent> BuildMultiTickRecords()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        EntityId[] emitters = new EntityId[4];
        for (int index = 0; index < emitters.Length; index++)
        {
            Assert.That(
                allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out emitters[index]),
                Is.True);
        }

        Assert.That(
            allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject),
            Is.True);

        AssertEveryIdentityIsIndependentOfCombatCeilings(allocator, emitters, subject);

        List<DomainEvent> records = new(MultiTickRows.Length);
        foreach (MultiTickRow row in MultiTickRows)
        {
            records.Add(EventFixture.Domain(
                row.Sequence % 2 == 0 ? EventFixture.EntityDefeated : EventFixture.ResourceAwarded,
                tick: row.Tick,
                systemPhase: row.Phase,
                sequence: row.Sequence,
                emitter: emitters[row.Sequence % emitters.Length],
                subject: subject,
                quantity: row.Sequence + 1));
        }

        return records;
    }

    /// <summary>One row of the retained cross-tick case.</summary>
    /// <param name="Tick">The tick the record belongs to.</param>
    /// <param name="Sequence">The per-tick emission sequence.</param>
    /// <param name="Phase">The emitting system phase.</param>
    private readonly record struct MultiTickRow(long Tick, long Sequence, int Phase);

    /// <summary>
    /// Asserts the fixture satisfies both invariants the two-key comparator rests on, so the golden
    /// pins a state the simulation can actually produce.
    /// </summary>
    /// <param name="events">The fixture's events, in append order.</param>
    /// <remarks>
    /// The previous fixture failed this: it paired a phase-11 emission with sequence 0 and a phase-3
    /// emission with sequence 1. Checking legality here is what stops that recurring, since the
    /// property is easy to break while still producing a stable, golden-matching batch - a fixture can
    /// be perfectly deterministic and still describe nothing real.
    /// </remarks>
    private static void AssertTheFixtureIsALegalInput(List<DomainEvent> events)
    {
        List<DomainEvent> bySequence = ReferenceSort(events);
        HashSet<string> pairs = new(StringComparer.Ordinal);
        Expect.Multiple(() =>
        {
            // Both loops below are no-ops on an empty list, so an empty fixture would satisfy every
            // legality check here. Assert the fixture is non-empty rather than letting it be exempt.
            Assert.That(
                events,
                Is.Not.Empty,
                "the fixture must contain records, or its legality is unfalsifiable");

            foreach (DomainEvent record in events)
            {
                string pair = record.Provenance.Tick.ToString(CultureInfo.InvariantCulture)
                    + "/"
                    + record.Provenance.Sequence.ToString(CultureInfo.InvariantCulture);
                Assert.That(
                    pairs.Add(pair),
                    Is.True,
                    "the fixture must not contain a duplicate tick and sequence: " + pair);
            }

            for (int index = 1; index < bySequence.Count; index++)
            {
                Assert.That(
                    bySequence[index].Provenance.SystemPhase,
                    Is.GreaterThanOrEqualTo(bySequence[index - 1].Provenance.SystemPhase),
                    "the fixture's phase must not decrease as the emission sequence rises, or it is "
                        + "an input the simulation cannot produce and the golden pins an impossible "
                        + "state");
            }
        });
    }

    /// <summary>
    /// Proves the phase-agreement invariant can fail: a batch whose phase decreases as the sequence
    /// rises is rejected, naming both offending records.
    /// </summary>
    /// <remarks>
    /// This is the control for the invariant that replaced the phase sort key. Without it, removing
    /// phase from the comparator would have silently discarded the reason phase was removable, and
    /// nothing would notice a system emitting outside its phase.
    /// </remarks>
    private static void AssertOutOfPhaseOrderEmissionIsRejected()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId emitter), Is.True);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject), Is.True);

        DomainEventBuffer buffer = new(initialCapacity: 4, hardMaximumCapacity: 16);
        buffer.BeginTick(7);

        // Phase 11 emitted at sequence 0 and phase 3 at sequence 1: the sequences are unique, so the
        // uniqueness invariant is satisfied and this is specifically the phase check firing.
        buffer.Append(EventFixture.Domain(
            EventFixture.EntityDefeated, 7, 11, 0, emitter, subject, 1));
        buffer.Append(EventFixture.Domain(
            EventFixture.EntityDefeated, 7, 3, 1, emitter, subject, 2));

        DomainEvent[] batch = new DomainEvent[buffer.Count];
        InvalidOperationException failure = Expect.Throws<InvalidOperationException>(
            () => buffer.CopyOrderedTo(batch));

        Expect.Multiple(() =>
        {
            Assert.That(
                failure.Message,
                Does.Contain("phase must not decrease as the sequence rises"),
                "the phase-agreement invariant must be what failed, not the uniqueness check");
            Assert.That(
                failure.Message,
                Does.Contain("system phase 3"),
                "the failure must name the offending phase so the emitting system is findable");
            Assert.That(
                failure.Message,
                Does.Contain("from phase 11"),
                "and the phase it disagreed with");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-006-008</c>.
    ///
    /// Every buffer is empty at the start of each tick, an event appended during tick N never appears in
    /// tick N+1's batch, and an event belonging to another tick cannot be appended at all.
    /// </summary>
    [Test]
    public void BuffersAreTickLocalAndStartEmpty()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject), Is.True);
        DomainEventBuffer domain = new(initialCapacity: 4, hardMaximumCapacity: 64);
        PresentationEventBuffer presentation = new(initialCapacity: 4, hardMaximumCapacity: 64);

        Expect.Multiple(() =>
        {
            Assert.That(domain.Count, Is.EqualTo(0), "a fresh domain buffer is empty");
            Assert.That(presentation.Count, Is.EqualTo(0), "a fresh presentation buffer is empty");
            Assert.That(domain.IsOpenForTick, Is.False, "and neither is open for a tick");
            Assert.That(presentation.IsOpenForTick, Is.False);
            Expect.Throws<InvalidOperationException>(
                () => domain.Append(EventFixture.Domain(
                    EventFixture.EntityDefeated, 0, 10, 0, allocator.PlayerId, subject, 1)));
        });

        DomainEvent tickZeroRecord = EventFixture.Domain(
            EventFixture.EntityDefeated, 0, 10, 0, allocator.PlayerId, subject, 1);

        domain.BeginTick(0);
        presentation.BeginTick(0);
        Assert.That(domain.Count, Is.EqualTo(0), "opening a tick starts empty");
        Assert.That(presentation.Count, Is.EqualTo(0));

        domain.Append(tickZeroRecord);
        Assert.That(
            presentation.TryAppend(EventFixture.Presentation(
                EventFixture.HitConfirmed, 0, 8, 1, allocator.PlayerId, subject, 1)),
            Is.True);

        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentException>(
                () => domain.Append(EventFixture.Domain(
                    EventFixture.EntityDefeated, 1, 10, 2, allocator.PlayerId, subject, 2)));
            Expect.Throws<ArgumentException>(
                () => presentation.TryAppend(EventFixture.Presentation(
                    EventFixture.HitConfirmed, 1, 8, 3, allocator.PlayerId, subject, 2)));
        });

        DomainEvent[] tickZeroBatch = new DomainEvent[domain.Count];
        int tickZeroWritten = domain.CopyOrderedTo(tickZeroBatch);
        domain.RecordAllConsumed();
        domain.Release();
        presentation.Release();

        domain.BeginTick(1);
        presentation.BeginTick(1);

        DomainEvent[] tickOneBatch = new DomainEvent[8];
        int tickOneWritten = domain.CopyOrderedTo(tickOneBatch);

        Expect.Multiple(() =>
        {
            Assert.That(tickZeroWritten, Is.EqualTo(1), "tick zero published its one record");
            Assert.That(tickZeroBatch[0], Is.EqualTo(tickZeroRecord));
            Assert.That(domain.Count, Is.EqualTo(0), "tick one starts empty");
            Assert.That(presentation.Count, Is.EqualTo(0));
            Assert.That(
                tickOneWritten,
                Is.EqualTo(0),
                "an event appended during tick zero must not appear in tick one's batch");
            Assert.That(domain.Tick, Is.EqualTo(1L));
            Assert.That(presentation.Tick, Is.EqualTo(1L));
        });

        domain.Release();
        presentation.Release();
    }

    /// <summary>
    /// Eight legal events across four phases, appended in an order that is neither the documented
    /// order nor its reverse.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every observable field is a function of the emission sequence - phase, emitter, kind, quantity,
    /// position - so the golden's rows stay derivable from the sequence alone once the append order
    /// stops matching them. <see cref="AppendSequences"/> supplies the append order and
    /// <see cref="PhaseOfSequence"/> the phase mapping.
    /// </para>
    /// <para>
    /// <b>This fixture is legal, and the previous one was not.</b> The earlier version paired phase 11
    /// with sequence 0 and phase 3 with sequence 1 so that "sorting cannot be a no-op". But the
    /// sequence is issued at emission and emission happens in phase order, so a phase-11 emission
    /// cannot precede a phase-3 emission in the same tick: that golden pinned a state the simulation
    /// cannot produce, which is worth no more than a golden that pins nothing. The non-no-op property
    /// is now obtained from the append order instead, which costs nothing and stays legal.
    /// </para>
    /// </remarks>
    private static List<DomainEvent> BuildSimultaneousEvents()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        EntityId[] emitters = new EntityId[4];
        for (int index = 0; index < emitters.Length; index++)
        {
            Assert.That(
                allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out emitters[index]),
                Is.True);
        }

        // The subject is a fifth ordinary enemy, not a pickup. See
        // AssertEveryIdentityIsIndependentOfCombatCeilings for why that matters.
        Assert.That(
            allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId subject),
            Is.True);

        AssertEveryIdentityIsIndependentOfCombatCeilings(allocator, emitters, subject);

        List<DomainEvent> events = new(AppendSequences.Length);
        foreach (int sequence in AppendSequences)
        {
            events.Add(EventFixture.Domain(
                sequence % 2 == 0 ? EventFixture.EntityDefeated : EventFixture.ResourceAwarded,
                tick: 7,
                systemPhase: PhaseOfSequence[sequence],
                sequence: sequence,
                emitter: emitters[sequence % emitters.Length],
                subject: subject,
                quantity: sequence + 1));
        }

        return events;
    }

    /// <summary>
    /// Asserts that every absolute storage index this golden renders is computed from partitions no
    /// <c>docs/technical/22-combat-and-weapon-runtime.md</c> ceiling can move.
    /// </summary>
    /// <param name="allocator">The fixture's allocator.</param>
    /// <param name="emitters">The four emitting identities.</param>
    /// <param name="subject">The shared subject identity.</param>
    /// <remarks>
    /// <para>
    /// The golden renders absolute storage indices, because it renders through production
    /// <c>DomainEvent.ToString()</c> and pinning that rendering is worth keeping. The cost of an
    /// absolute index is that it moves whenever any partition above it moves, and doc 22 § Performance
    /// and capacity explicitly reserves the right to move three of them: "Profiling and legal
    /// maximum-output analysis must tighten or expand them before content complete."
    /// </para>
    /// <para>
    /// So the independence is asserted rather than hoped for. Every identity here comes from the
    /// OrdinaryEnemy partition, and every category the canonical order places above OrdinaryEnemy must
    /// carry an authority other than <see cref="CapacityAuthority.CombatRuntimeCeiling"/>. If someone
    /// later inserts a combat-ceilinged category above OrdinaryEnemy, this fails with a message about
    /// partition offsets - which is the diagnosis - instead of the golden failing with a byte diff,
    /// which is the symptom that gets a golden regenerated instead of investigated.
    /// </para>
    /// </remarks>
    private static void AssertEveryIdentityIsIndependentOfCombatCeilings(
        EntityIdAllocator allocator,
        EntityId[] emitters,
        EntityId subject)
    {
        int enemyOffset = allocator.SlotOffsetFor(PopulationCategory.OrdinaryEnemy);
        int enemyEnd = enemyOffset + allocator.CapacityFor(PopulationCategory.OrdinaryEnemy).HardCapacity;

        List<string> ceilingedAbove = new();
        int computedOffset = 0;
        foreach (PopulationCategory category in StoreCapacities.Categories)
        {
            if (category == PopulationCategory.OrdinaryEnemy)
            {
                break;
            }

            StoreCapacity capacity = allocator.CapacityFor(category);
            if (capacity.Authority == CapacityAuthority.CombatRuntimeCeiling)
            {
                ceilingedAbove.Add(category.ToString());
            }

            computedOffset += capacity.HardCapacity;
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                ceilingedAbove,
                Is.Empty,
                "no doc 22 § Performance and capacity ceiling may sit above the OrdinaryEnemy "
                    + "partition, or the absolute storage indices this golden renders move whenever "
                    + "doc 22 moves and an ordering golden fails for a capacity reason: "
                    + string.Join(", ", ceilingedAbove));
            Assert.That(
                enemyOffset,
                Is.EqualTo(computedOffset),
                "the partition offset must be the running sum of the capacity table above it, "
                    + "computed here rather than written down as a literal");
            Assert.That(
                subject.Index,
                Is.InRange(enemyOffset, enemyEnd - 1),
                "the subject must live in the OrdinaryEnemy partition");
            foreach (EntityId emitter in emitters)
            {
                Assert.That(
                    emitter.Index,
                    Is.InRange(enemyOffset, enemyEnd - 1),
                    "every emitter must live in the OrdinaryEnemy partition");
            }
        });
    }

    private static List<DomainEvent> PublishOrdered(List<DomainEvent> appendOrder)
    {
        DomainEventBuffer buffer = new(initialCapacity: 2, hardMaximumCapacity: 256);
        buffer.BeginTick(appendOrder[0].Provenance.Tick);
        foreach (DomainEvent record in appendOrder)
        {
            buffer.Append(record);
        }

        DomainEvent[] batch = new DomainEvent[buffer.Count];
        int written = buffer.CopyOrderedTo(batch);
        buffer.RecordAllConsumed();
        buffer.Release();

        List<DomainEvent> result = new(written);
        for (int index = 0; index < written; index++)
        {
            result.Add(batch[index]);
        }

        return result;
    }

    /// <summary>
    /// Sorts by the documented keys with a deliberately simple list sort, independently of
    /// <c>EventOrdering</c>.
    /// </summary>
    /// <remarks>
    /// doc 91 § Reference models: agreement is then evidence about the rule, not about the
    /// implementation agreeing with itself. Two keys, matching the rule: the emission sequence is
    /// per-tick global, so neither phase nor identity follows it.
    /// </remarks>
    private static List<DomainEvent> ReferenceSort(List<DomainEvent> events)
    {
        List<DomainEvent> sorted = new(events);
        sorted.Sort((left, right) =>
        {
            int byTick = left.Provenance.Tick.CompareTo(right.Provenance.Tick);
            return byTick != 0
                ? byTick
                : left.Provenance.Sequence.CompareTo(right.Provenance.Sequence);
        });

        return sorted;
    }
}
