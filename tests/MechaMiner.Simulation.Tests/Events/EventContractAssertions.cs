using System;
using System.Collections.Generic;
using System.Globalization;
using MechaMiner.Simulation.Events;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Events;

/// <summary>
/// The loss and ordering assertions of <c>SIM-006</c>, written once so the real buffers and the
/// deliberately broken stubs are judged by literally the same code.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports <c>VER-SIM-006-001</c>, <c>VER-SIM-006-003</c>, and <c>VER-SIM-006-010</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence requires a negative control to
/// prove the gate can fail. Sharing the assertion is what makes the control about the registered gate
/// rather than about a paraphrase of it.
/// </para>
/// </remarks>
internal static class EventContractAssertions
{
    /// <summary>
    /// Asserts a no-loss invariant over the resulting state: every appended record is present in the
    /// delivered batch, in the same multiset, and the run-long appended and delivered totals agree.
    /// </summary>
    /// <param name="subject">What is being judged, for the failure message.</param>
    /// <param name="appended">Every record the caller appended, in append order.</param>
    /// <param name="delivered">The batch the buffer delivered.</param>
    /// <param name="appendedInRun">The buffer's run-long appended total.</param>
    /// <param name="accountedInRun">The buffer's run-long delivered-and-accounted total.</param>
    /// <remarks>
    /// An invariant over the resulting state, not an assertion about the calls that produced it: it
    /// compares multisets rather than checking that no particular drop path was taken, so a loss through a
    /// route nobody enumerated still fails it. doc 20 § Domain and presentation events: "domain events may
    /// not be dropped."
    /// </remarks>
    internal static void NoDomainEventWasLost(
        string subject,
        IReadOnlyList<DomainEvent> appended,
        IReadOnlyList<DomainEvent> delivered,
        long appendedInRun,
        long accountedInRun)
    {
        ArgumentNullException.ThrowIfNull(appended);
        ArgumentNullException.ThrowIfNull(delivered);

        Dictionary<DomainEvent, int> remaining = new(appended.Count);
        foreach (DomainEvent record in appended)
        {
            remaining.TryGetValue(record, out int count);
            remaining[record] = count + 1;
        }

        List<string> missing = new();
        List<string> unexpected = new();
        foreach (DomainEvent record in delivered)
        {
            if (remaining.TryGetValue(record, out int count) && count > 0)
            {
                remaining[record] = count - 1;
                continue;
            }

            unexpected.Add(record.ToString());
        }

        foreach (KeyValuePair<DomainEvent, int> entry in remaining)
        {
            for (int repeat = 0; repeat < entry.Value; repeat++)
            {
                missing.Add(entry.Key.ToString());
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                appended,
                Is.Not.Empty,
                subject + ": the fixture must append at least one record, or no-loss is vacuous");
            Assert.That(
                delivered,
                Has.Count.EqualTo(appended.Count),
                subject + ": the delivered batch must hold exactly as many records as were appended");
            Assert.That(
                missing,
                Is.Empty,
                subject + ": these appended domain events are absent from the delivered batch: "
                    + string.Join("; ", missing));
            Assert.That(
                unexpected,
                Is.Empty,
                subject + ": these delivered records were never appended: "
                    + string.Join("; ", unexpected));
            Assert.That(
                accountedInRun,
                Is.EqualTo(appendedInRun),
                subject + ": the run-long appended and accounted totals must agree, so no record can "
                    + "vanish between ticks either");
        });
    }

    /// <summary>
    /// Asserts that a batch is in the documented stable order and that a different append order produced
    /// the identical batch.
    /// </summary>
    /// <param name="subject">What is being judged, for the failure message.</param>
    /// <param name="expectedRendering">The order an independent reference comparison produces.</param>
    /// <param name="firstRendering">The batch published after one append order.</param>
    /// <param name="secondRendering">The batch published after a different append order over the same events.</param>
    /// <remarks>
    /// doc 10 § System phase ordering: "Simultaneous outcomes use documented stable ordering rather than
    /// collection or thread timing." Both halves matter: agreement between the two append orders catches
    /// order leaking out of arrival, and agreement with the reference catches an order that is stable but
    /// wrong.
    /// </remarks>
    internal static void BatchOrderMatchesTheDocumentedComparison(
        string subject,
        string expectedRendering,
        string firstRendering,
        string secondRendering)
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                secondRendering,
                Is.EqualTo(firstRendering),
                subject + ": two runs that emit the same events in different append order must publish "
                    + "identical batches, so no observable order comes from collection or thread timing");
            Assert.That(
                firstRendering,
                Is.EqualTo(expectedRendering),
                subject + ": the batch must be ordered by system phase, then emission sequence, then the "
                    + "full entity ID");
        });
    }

    /// <summary>Renders a domain batch as canonical invariant text.</summary>
    internal static string RenderDomainBatch(IReadOnlyList<DomainEvent> batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        System.Text.StringBuilder builder = new();
        for (int index = 0; index < batch.Count; index++)
        {
            builder
                .Append(index.ToString(CultureInfo.InvariantCulture).PadLeft(3))
                .Append("  ")
                .Append(batch[index].ToString())
                .Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Renders a presentation batch as canonical invariant text.</summary>
    internal static string RenderPresentationBatch(IReadOnlyList<PresentationEvent> batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        System.Text.StringBuilder builder = new();
        for (int index = 0; index < batch.Count; index++)
        {
            builder
                .Append(index.ToString(CultureInfo.InvariantCulture).PadLeft(3))
                .Append("  ")
                .Append(batch[index].ToString())
                .Append('\n');
        }

        return builder.ToString();
    }
}
