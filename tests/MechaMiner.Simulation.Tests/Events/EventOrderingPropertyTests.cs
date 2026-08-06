using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// Compares the published batch against a deliberately simple reference ordering over randomized
/// multi-system emission sequences.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-006-009</c>.
///
/// <c>docs/technical/91-verification-strategy.md</c> § Reference models;
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events.
/// </remarks>
[TestFixture]
internal sealed class EventOrderingPropertyTests
{
    private const int DeclaredSeed = 611_006;

    /// <summary>
    /// Verification: <c>VER-SIM-006-009</c>.
    ///
    /// Over randomized emission sequences the published batch equals the reference ordering and the domain
    /// record count equals the emitted count exactly.
    /// </summary>
    [Test]
    public void OrderedBatchMatchesTheReferenceOrdering()
    {
        PropertyCase.ForAll(
            "events-ordered-batch-matches-reference",
            DeclaredSeed,
            caseCount: 96,
            generate: random =>
            {
                // Each element encodes one emission: the low digit selects the emitting phase from the
                // fourteen-phase order, the next selects the emitter, so a crude integer array is enough
                // for Shrinkers.Int32Array to minimize a failure into something readable.
                int[] emissions = new int[random.Next(1, 40)];
                for (int index = 0; index < emissions.Length; index++)
                {
                    emissions[index] = random.Next(0, 140);
                }

                return emissions;
            },
            shrink: Shrinkers.Int32Array,
            render: emissions => "[" + string.Join(",", emissions) + "]",
            property: RunEmissions);
    }

    private static void RunEmissions(int[] emissions)
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        EntityId[] emitters = new EntityId[4];
        for (int index = 0; index < emitters.Length; index++)
        {
            Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out emitters[index]), Is.True);
        }

        Assert.That(allocator.TryAllocate(PopulationCategory.Pickup, out EntityId subject), Is.True);

        DomainEventBuffer buffer = new(initialCapacity: 2, hardMaximumCapacity: 512);
        buffer.BeginTick(3);

        List<DomainEvent> emitted = new(emissions.Length);
        for (int index = 0; index < emissions.Length; index++)
        {
            int phase = 1 + (emissions[index] % EventProvenance.LastSystemPhase);
            EntityId emitter = emitters[(emissions[index] / EventProvenance.LastSystemPhase) % emitters.Length];
            DomainEvent record = EventFixture.Domain(
                emissions[index] % 2 == 0 ? EventFixture.EntityDefeated : EventFixture.ResourceAwarded,
                tick: 3,
                systemPhase: phase,
                sequence: index,
                emitter: emitter,
                subject: subject,
                quantity: index + 1);
            buffer.Append(record);
            emitted.Add(record);
        }

        DomainEvent[] batch = new DomainEvent[buffer.Count];
        int written = buffer.CopyOrderedTo(batch);

        List<DomainEvent> published = new(written);
        for (int index = 0; index < written; index++)
        {
            published.Add(batch[index]);
        }

        List<DomainEvent> reference = new(emitted);
        reference.Sort(CompareByDocumentedKeys);

        Assert.That(
            written,
            Is.EqualTo(emitted.Count),
            "the domain record count must equal the emitted count exactly: "
                + written.ToString(CultureInfo.InvariantCulture)
                + " published, "
                + emitted.Count.ToString(CultureInfo.InvariantCulture)
                + " emitted");

        Assert.That(
            EventContractAssertions.RenderDomainBatch(published),
            Is.EqualTo(EventContractAssertions.RenderDomainBatch(reference)),
            "the published batch must equal the reference ordering");

        // The no-loss invariant needs at least one record to be about. The generator always produces
        // one, but the shrinker legitimately offers the empty array as a candidate, and an empty tick
        // is a valid tick that loses nothing - so it is asserted as such rather than treated as a
        // failure the shrinker would then latch onto.
        if (emitted.Count > 0)
        {
            EventContractAssertions.NoDomainEventWasLost(
                "the randomized emission sequence",
                emitted,
                published,
                appendedInRun: buffer.AppendedInRun,
                accountedInRun: buffer.ReleasedInRun + written);
        }
        else
        {
            Assert.That(written, Is.EqualTo(0), "an empty tick publishes an empty batch");
            Assert.That(buffer.AppendedInRun, Is.EqualTo(0L));
        }

        buffer.RecordAllConsumed();
        buffer.Release();
        Assert.That(
            buffer.ReleasedInRun,
            Is.EqualTo(buffer.AppendedInRun),
            "and the run-long totals must reconcile after release");
    }

    /// <summary>
    /// A deliberately simple comparison over the documented keys, independent of <c>EventOrdering</c>.
    /// </summary>
    /// <remarks>
    /// Three keys: tick, phase, sequence. The emission sequence is per-tick global, so no identity
    /// tiebreak follows - see <c>EventProvenance.Compare</c> for why a fourth key would be
    /// unreachable and harmful rather than merely redundant.
    /// </remarks>
    private static int CompareByDocumentedKeys(DomainEvent left, DomainEvent right)
    {
        int byTick = left.Provenance.Tick.CompareTo(right.Provenance.Tick);
        if (byTick != 0)
        {
            return byTick;
        }

        int byPhase = left.Provenance.SystemPhase.CompareTo(right.Provenance.SystemPhase);
        if (byPhase != 0)
        {
            return byPhase;
        }

        return left.Provenance.Sequence.CompareTo(right.Provenance.Sequence);
    }
}
