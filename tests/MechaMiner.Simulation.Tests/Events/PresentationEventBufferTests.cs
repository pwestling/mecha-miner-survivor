using System;
using System.Collections.Generic;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// Proves that presentation events coalesce only under an explicit named policy, and that discarding an
/// entire tick's presentation batch changes nothing authoritative.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-006-002</c>, <c>VER-SIM-006-007</c>.
///
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "Presentation events may
/// be coalesced by an explicit visual policy; domain events may not be dropped", and "Consumers never infer
/// authoritative state solely from presentation events."
/// <c>CTR-SIM-002</c> in doc 115 § Cross-boundary contract registry: "noncritical visual/audio event may
/// degrade; authority unaffected".
/// </remarks>
[TestFixture]
internal sealed class PresentationEventBufferTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-006-002</c>.
    ///
    /// A kind with no policy rule is delivered verbatim; a policy never merges across kinds or across
    /// provenances; and a merged record reports how many source events it stands for.
    /// </summary>
    [Test]
    public void CoalescingHappensOnlyUnderAnExplicitNamedPolicy()
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId firstTarget), Is.True);
        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId secondTarget), Is.True);

        PresentationCoalescingPolicy merging = PresentationCoalescingPolicy
            .Named("hit-spark-burst")
            .WithMerge(EventFixture.HitConfirmed, "one spark burst per weapon per target per tick");

        // Three hits on one target from one origin, two hits on another target from the same origin, and
        // two warnings from the same origin. Only the hits have a rule.
        List<PresentationEvent> emitted =
        [
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 8, 0, allocator.PlayerId, firstTarget, 1),
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 8, 1, allocator.PlayerId, firstTarget, 2),
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 8, 2, allocator.PlayerId, firstTarget, 3),
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 8, 3, allocator.PlayerId, secondTarget, 4),
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 8, 4, allocator.PlayerId, secondTarget, 5),
            EventFixture.Presentation(EventFixture.Warning, 0, 8, 5, allocator.PlayerId, firstTarget, 6),
            EventFixture.Presentation(EventFixture.Warning, 0, 8, 6, allocator.PlayerId, firstTarget, 7),
        ];

        List<PresentationEvent> merged = PublishWith(emitted, merging);
        List<PresentationEvent> verbatim = PublishWith(emitted, PresentationCoalescingPolicy.Verbatim);

        Expect.Multiple(() =>
        {
            Assert.That(
                verbatim,
                Has.Count.EqualTo(emitted.Count),
                "a policy with no rules must deliver every kind verbatim, so coalescing can never happen "
                    + "by omission");
            foreach (PresentationEvent record in verbatim)
            {
                Assert.That(
                    record.SourceEventCount,
                    Is.EqualTo(1),
                    "a verbatim record stands for exactly one emission");
                Assert.That(record.IsCoalesced, Is.False);
            }
        });

        int mergedHitRecords = 0;
        int mergedHitSources = 0;
        int warningRecords = 0;
        foreach (PresentationEvent record in merged)
        {
            if (record.Kind == EventFixture.HitConfirmed)
            {
                mergedHitRecords++;
                mergedHitSources += record.SourceEventCount;
            }
            else
            {
                warningRecords++;
                Assert.That(
                    record.SourceEventCount,
                    Is.EqualTo(1),
                    "the warning kind has no rule, so it must stay verbatim even though another kind merged");
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                mergedHitRecords,
                Is.EqualTo(2),
                "the five hits share a kind and an origin but split across two subjects, so they merge into "
                    + "one record per subject and never across subjects");
            Assert.That(
                mergedHitSources,
                Is.EqualTo(5),
                "the coalesced batch must account for every source event, so a consumer scaling by count is "
                    + "not misled");
            Assert.That(
                warningRecords,
                Is.EqualTo(2),
                "a policy must never merge a kind it has no rule for");
            Assert.That(merged, Has.Count.EqualTo(4));
            Assert.That(
                merging.Name,
                Is.EqualTo("hit-spark-burst"),
                "the policy is named, so the batch can say which visual decision merged it");
            Assert.That(
                merging.TryGetMergeRule(EventFixture.HitConfirmed, out string ruleName),
                Is.True);
            Assert.That(ruleName, Is.EqualTo("one spark burst per weapon per target per tick"));
            Assert.That(
                merging.TryGetMergeRule(EventFixture.Warning, out string absent),
                Is.False,
                "absence of a rule is what means verbatim");
            Assert.That(absent, Is.Empty);
        });

        AssertDifferentProvenanceNeverMerges(allocator, merging, firstTarget);

        Expect.Multiple(() =>
        {
            Assert.That(
                PresentationCoalescingPolicy.Verbatim.RuleCount,
                Is.EqualTo(0),
                "the verbatim policy merges nothing");
            Expect.Throws<ArgumentException>(
                () => PresentationCoalescingPolicy.Named(" ").WithMerge(EventFixture.HitConfirmed, "rule"));
            Expect.Throws<ArgumentException>(
                () => PresentationCoalescingPolicy.Named("unnamed-rule").WithMerge(EventFixture.HitConfirmed, " "));
            Expect.Throws<ArgumentException>(
                () => merging.WithMerge(EventFixture.HitConfirmed, "a second rule for the same kind"));
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-006-007</c>.
    ///
    /// Discarding a whole tick's presentation batch leaves the committed state, that tick's domain events,
    /// and the following tick's behaviour bit-identical to a run that published the batch.
    /// </summary>
    [Test]
    public void DiscardingThePresentationBatchChangesNothingAuthoritative()
    {
        string published = RunTwoTicks(discardPresentationOnFirstTick: false);
        string discarded = RunTwoTicks(discardPresentationOnFirstTick: true);

        Expect.Multiple(() =>
        {
            Assert.That(
                discarded,
                Is.EqualTo(published),
                "the authoritative rendering - committed snapshot state and domain batch, for both ticks - "
                    + "must be identical whether or not the presentation batch was published, so no "
                    + "authority can leak into the presentation channel");
            Assert.That(
                published,
                Does.Contain("domain-batch count=2"),
                "the comparison must actually cover domain events, or it proves nothing");
            Assert.That(
                published,
                Does.Not.Contain("presentation-batch"),
                "the authoritative rendering must exclude the presentation batch by construction");
        });
    }

    /// <summary>
    /// Two events from different phases share a kind but not an origin, so no policy may merge them.
    /// </summary>
    private static void AssertDifferentProvenanceNeverMerges(
        EntityIdAllocator allocator,
        PresentationCoalescingPolicy merging,
        EntityId subject)
    {
        List<PresentationEvent> acrossPhases =
        [
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 8, 0, allocator.PlayerId, subject, 1),
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 9, 1, allocator.PlayerId, subject, 2),
        ];

        List<PresentationEvent> acrossSources =
        [
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 8, 0, allocator.PlayerId, subject, 1, "W-AA"),
            EventFixture.Presentation(EventFixture.HitConfirmed, 0, 8, 1, allocator.PlayerId, subject, 2, "W-BB"),
        ];

        Expect.Multiple(() =>
        {
            Assert.That(
                PublishWith(acrossPhases, merging),
                Has.Count.EqualTo(2),
                "two different emitting phases are two different origins, so they must not merge");
            Assert.That(
                PublishWith(acrossSources, merging),
                Has.Count.EqualTo(2),
                "two different source content IDs are two different origins, so they must not merge");
        });
    }

    private static List<PresentationEvent> PublishWith(
        List<PresentationEvent> emitted,
        PresentationCoalescingPolicy policy)
    {
        PresentationEventBuffer buffer = new(initialCapacity: 4, hardMaximumCapacity: 256);
        buffer.BeginTick(emitted[0].Provenance.Tick);
        foreach (PresentationEvent record in emitted)
        {
            Assert.That(buffer.TryAppend(record), Is.True, "the fixture must fit inside the buffer");
        }

        PresentationEvent[] batch = new PresentationEvent[buffer.Count];
        int written = buffer.PublishOrderedTo(policy, batch);
        buffer.Release();

        List<PresentationEvent> result = new(written);
        for (int index = 0; index < written; index++)
        {
            result.Add(batch[index]);
        }

        return result;
    }

    /// <summary>
    /// Runs two ticks through the real publisher and returns only the authoritative rendering.
    /// </summary>
    /// <remarks>
    /// Driving the real <c>SnapshotPublisher</c> rather than a stand-in is what makes the comparison mean
    /// something: doc 20 § Tick transaction publishes snapshot and events as one result, so a discard that
    /// leaked into authority would show up in the committed snapshot too.
    /// </remarks>
    private static string RunTwoTicks(bool discardPresentationOnFirstTick)
    {
        EntityIdAllocator allocator = EventFixture.NewAllocator(EventFixture.RunSession);
        SnapshotPublisher publisher = new(
            EventFixture.RunSession,
            visibleEntityCapacity: 8,
            domainEventCapacity: 32,
            presentationEventCapacity: 32);
        DomainEventBuffer domain = new(initialCapacity: 8, hardMaximumCapacity: 256);
        PresentationEventBuffer presentation = new(initialCapacity: 8, hardMaximumCapacity: 256);

        Assert.That(allocator.TryAllocate(PopulationCategory.OrdinaryEnemy, out EntityId enemy), Is.True);
        System.Text.StringBuilder rendering = new();
        HudViewModel hud = HudViewModel.Unpublished;

        for (long tick = 0; tick < 2; tick++)
        {
            publisher.BeginTick(tick);
            domain.BeginTick(tick);
            presentation.BeginTick(tick);

            publisher.StagePlayer(tick * 0.25, tick * -0.5, tick * 0.1);
            hud = HudViewModel.Next(hud, 100.0 - tick, 5.0, 200 + tick, 25, tick / 60.0, tick * 0.25);
            publisher.StageHud(hud);
            publisher.StageVisibleEntity(SnapshotEntity.Create(
                enemy,
                PopulationCategory.OrdinaryEnemy,
                tick * 1.5,
                tick * 2.5,
                0.0,
                presentationFlags: 1));

            for (int index = 0; index < 2; index++)
            {
                domain.Append(EventFixture.Domain(
                    EventFixture.ResourceAwarded,
                    tick,
                    systemPhase: 11,
                    sequence: publisher.NextEventSequence(),
                    emitter: allocator.PlayerId,
                    subject: enemy,
                    quantity: (tick * 10) + index));
            }

            for (int index = 0; index < 3; index++)
            {
                Assert.That(
                    presentation.TryAppend(EventFixture.Presentation(
                        EventFixture.HitConfirmed,
                        tick,
                        systemPhase: 8,
                        sequence: publisher.NextEventSequence(),
                        emitter: allocator.PlayerId,
                        subject: enemy,
                        quantity: index)),
                    Is.True);
            }

            if (discardPresentationOnFirstTick && tick == 0)
            {
                // The presentation batch is never read. doc 20 § Domain and presentation events makes
                // presentation events disposable, so this must change nothing.
                presentation.Discard();
                presentation.BeginTick(tick);
            }

            TickPublication publication = publisher.Publish(
                domain,
                presentation,
                PresentationCoalescingPolicy.Verbatim);
            rendering.Append(publication.RenderAuthoritative());
            publisher.ReleaseTick(domain, presentation);
        }

        return rendering.ToString();
    }
}
