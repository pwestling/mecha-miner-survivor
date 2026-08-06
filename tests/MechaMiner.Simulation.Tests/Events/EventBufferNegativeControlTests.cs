using System.Collections.Generic;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// Proves the loss and ordering gates can fail, by running the same assertions the real gates run against
/// stubs that are deliberately wrong.
/// </summary>
/// <remarks>
/// <para>
/// Verification: <c>VER-SIM-006-010</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence requires evidence that a gate can
/// fail. The stubs are ordinary valid C# that behaves incorrectly, not a deliberately invalid fixture, which
/// <c>docs/technical/delivery-waves.md</c> forbids inside a compiled project.
/// </para>
/// <para>
/// The assertions come from <see cref="EventContractAssertions"/>, the same code
/// <see cref="DomainEventBufferTests"/>, <see cref="EventOrderingTests"/>, and
/// <see cref="EventOrderingPropertyTests"/> use, so weakening one turns the real gates and this control red
/// together.
/// </para>
/// <para>
/// Note what this control can and cannot be. The real <c>DomainEventBuffer</c> has no drop branch at all -
/// no eviction, no overwrite-on-full, no discard - so there is nothing in it to perturb. That is the
/// stronger position, and it is why the stub here is a separate type that <em>does</em> have such a branch:
/// the control shows the assertion catches loss, while
/// <c>DomainEventBufferTests.NoRemovalPathExistsBeyondAConsumedRelease</c> shows the real type has no way to
/// produce it.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class EventBufferNegativeControlTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-006-010</c>.
    ///
    /// A buffer that silently drops on overflow fails the no-loss assertion; a buffer that publishes in
    /// hash-container enumeration order fails the ordering assertion; and the real buffer passes both.
    /// </summary>
    [Test]
    public void LossAndOrderingAssertionsFailAgainstDeliberatelyBrokenStubs()
    {
        AssertSilentlyDroppingBufferFailsTheNoLossGate();
        AssertHashOrderedBufferFailsTheOrderingGate();
        AssertUnreconciledTotalsFailTheNoLossGate();
        AssertTheRealBuffersPassBothGates();
    }

    /// <summary>
    /// A stub that discards on overflow instead of growing: the batch is short, and the assertion names the
    /// missing records.
    /// </summary>
    private static void AssertSilentlyDroppingBufferFailsTheNoLossGate()
    {
        List<DomainEvent> emitted = BuildRecords(6);
        DroppingDomainBuffer broken = new(capacity: 4);
        foreach (DomainEvent record in emitted)
        {
            broken.Append(record);
        }

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => EventContractAssertions.NoDomainEventWasLost(
                "a stub buffer that silently drops on overflow",
                emitted,
                broken.Delivered,
                appendedInRun: emitted.Count,
                accountedInRun: broken.Delivered.Count));

        Assert.That(
            failure.Message,
            Does.Contain("are absent from the delivered batch"),
            "the no-loss gate must be the assertion that failed, and it must name what went missing");
    }

    /// <summary>
    /// A stub that publishes in hash-container enumeration order agrees with itself run to run only by
    /// accident, and disagrees with the documented comparison.
    /// </summary>
    private static void AssertHashOrderedBufferFailsTheOrderingGate()
    {
        List<DomainEvent> emitted = BuildRecords(8);

        string correctRendering = EventContractAssertions.RenderDomainBatch(PublishThroughRealBuffer(emitted));
        string hashRendering = EventContractAssertions.RenderDomainBatch(PublishThroughHashContainer(emitted));

        Assert.That(
            hashRendering,
            Is.Not.EqualTo(correctRendering),
            "the fixture must be one where hash enumeration order genuinely differs from the documented "
                + "order, or the control proves nothing");

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => EventContractAssertions.BatchOrderMatchesTheDocumentedComparison(
                "a stub buffer that publishes in hash-container enumeration order",
                correctRendering,
                hashRendering,
                hashRendering));

        Assert.That(
            failure.Message,
            Does.Contain("tick, then emission sequence, and by nothing further"),
            "the ordering gate must be the assertion that failed");
    }

    /// <summary>
    /// A stub whose per-tick batch is complete but whose run-long totals do not reconcile still fails: a
    /// record can be lost between ticks as easily as within one.
    /// </summary>
    private static void AssertUnreconciledTotalsFailTheNoLossGate()
    {
        List<DomainEvent> emitted = BuildRecords(3);

        MultipleAssertException failure = Expect.Throws<MultipleAssertException>(
            () => EventContractAssertions.NoDomainEventWasLost(
                "a stub buffer whose run-long totals disagree",
                emitted,
                emitted,
                appendedInRun: 10,
                accountedInRun: 3));

        Assert.That(
            failure.Message,
            Does.Contain("run-long appended and accounted totals must agree"),
            "the run-long half of the invariant must be able to fail on its own");
    }

    /// <summary>Both assertions must pass against the real buffer, or the control is vacuous.</summary>
    private static void AssertTheRealBuffersPassBothGates()
    {
        List<DomainEvent> emitted = BuildRecords(9);
        List<DomainEvent> reversed = new(emitted);
        reversed.Reverse();

        DomainEventBuffer buffer = new(initialCapacity: 2, hardMaximumCapacity: 256);
        buffer.BeginTick(emitted[0].Provenance.Tick);
        foreach (DomainEvent record in emitted)
        {
            buffer.Append(record);
        }

        DomainEvent[] batch = new DomainEvent[buffer.Count];
        int written = buffer.CopyOrderedTo(batch);
        List<DomainEvent> delivered = new(written);
        for (int index = 0; index < written; index++)
        {
            delivered.Add(batch[index]);
        }

        Expect.DoesNotThrow(() => EventContractAssertions.NoDomainEventWasLost(
            "the real domain buffer",
            emitted,
            delivered,
            appendedInRun: buffer.AppendedInRun,
            accountedInRun: buffer.ReleasedInRun + written));

        string ascending = EventContractAssertions.RenderDomainBatch(PublishThroughRealBuffer(emitted));
        string descending = EventContractAssertions.RenderDomainBatch(PublishThroughRealBuffer(reversed));

        Expect.DoesNotThrow(() => EventContractAssertions.BatchOrderMatchesTheDocumentedComparison(
            "the real domain buffer",
            ascending,
            ascending,
            descending));

        buffer.RecordAllConsumed();
        buffer.Release();
    }

    private static List<DomainEvent> BuildRecords(int count)
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        EntityId[] emitters = new EntityId[3];
        for (int index = 0; index < emitters.Length; index++)
        {
            Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out emitters[index]), Is.True);
        }

        Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId subject), Is.True);

        // Two constraints pull in opposite directions here and both matter.
        //
        // The phase must not decrease as the emission sequence rises: the sequence is issued at
        // emission and emission happens in phase order, so a decreasing phase is an input the
        // simulation cannot produce and EventOrdering.AssertPhaseAgreesWithSequenceWithinTick rejects
        // it. A control needs the *stub* to be wrong, not its input - an illegal fixture would make
        // the real buffer throw and the control would be proving the wrong thing.
        //
        // But the append order must also differ from the documented order, or the hash-ordered stub
        // has nothing to disagree with and the control proves nothing either. Assigning sequence =
        // loop index satisfies the first and breaks the second, and the control's own precondition
        // catches that - so the sequences are appended out of order while the phase stays a
        // non-decreasing function of the sequence rather than of the append position.
        int[] appendSequences = [4, 0, 7, 2, 8, 1, 5, 3, 6];
        int[] phaseOfSequence = [3, 3, 5, 8, 8, 10, 10, 11, 11];
        Assert.That(
            count,
            Is.LessThanOrEqualTo(appendSequences.Length),
            "the append ladder must cover every requested record, or the sequences would have to wrap "
                + "and the fixture would stop being a legal input");

        List<DomainEvent> records = new(count);
        for (int index = 0; index < count; index++)
        {
            int sequence = appendSequences[index];
            records.Add(EventFixture.Domain(
                sequence % 2 == 0 ? EventFixture.EntityDefeated : EventFixture.ResourceAwarded,
                tick: 2,
                systemPhase: phaseOfSequence[sequence],
                sequence: sequence,
                emitter: emitters[sequence % emitters.Length],
                subject: subject,
                quantity: sequence + 1));
        }

        return records;
    }

    private static List<DomainEvent> PublishThroughRealBuffer(List<DomainEvent> appendOrder)
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
    /// Publishes in the enumeration order of a hash container, which is exactly the failure doc 10 § System
    /// phase ordering forbids: "Simultaneous outcomes use documented stable ordering rather than collection
    /// or thread timing."
    /// </summary>
    private static List<DomainEvent> PublishThroughHashContainer(List<DomainEvent> emitted)
    {
        HashSet<DomainEvent> container = new();
        foreach (DomainEvent record in emitted)
        {
            container.Add(record);
        }

        List<DomainEvent> published = new(container.Count);
        foreach (DomainEvent record in container)
        {
            published.Add(record);
        }

        return published;
    }

    /// <summary>
    /// A deliberately broken buffer that discards records once full, which the real
    /// <c>DomainEventBuffer</c> has no branch to do.
    /// </summary>
    /// <remarks>
    /// Valid code that behaves incorrectly. It exists so the no-loss assertion can be shown to catch loss;
    /// nothing depends on it and it is never production behaviour.
    /// </remarks>
    private sealed class DroppingDomainBuffer
    {
        private readonly List<DomainEvent> _delivered;
        private readonly int _capacity;

        internal DroppingDomainBuffer(int capacity)
        {
            _capacity = capacity;
            _delivered = new List<DomainEvent>(capacity);
        }

        /// <summary>Whatever survived the drop.</summary>
        internal IReadOnlyList<DomainEvent> Delivered => _delivered;

        /// <summary>Appends, or silently discards once full.</summary>
        internal void Append(in DomainEvent record)
        {
            if (_delivered.Count >= _capacity)
            {
                return;
            }

            _delivered.Add(record);
        }
    }
}
