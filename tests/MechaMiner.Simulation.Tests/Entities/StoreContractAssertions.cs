using System;
using System.Globalization;
using MechaMiner.Simulation.Entities;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Entities;

/// <summary>
/// The identity and ordering assertions of <c>SIM-003</c>, written once so that the real
/// store and the deliberately broken stubs are judged by literally the same code.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports <c>VER-SIM-003-002</c>, <c>VER-SIM-003-010</c>, and
/// <c>VER-SIM-003-012</c>.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence requires a
/// negative control to prove the gate can fail. A control that re-expresses the assertion
/// in its own words proves only that <em>an</em> assertion fails, not that the registered
/// one does. Passing the subject in as a delegate makes the shared assertion the single
/// thing under test, so weakening it turns both the real gate and the control red.
/// </para>
/// </remarks>
internal static class StoreContractAssertions
{
    /// <summary>
    /// Asserts that a generation-mismatched reference fails closed while the live reference
    /// for the same slot resolves, and that exactly one diagnostic is counted.
    /// </summary>
    /// <param name="subject">What is being judged, for the failure message.</param>
    /// <param name="staleId">The identity issued before the slot was recycled.</param>
    /// <param name="liveId">The identity issued for the same slot after recycling.</param>
    /// <param name="resolves">Resolution under test. Must not throw.</param>
    /// <param name="staleReferenceCounter">Reads the failed-resolution counter.</param>
    /// <remarks>
    /// doc 20 § Entity identity: "Invalid, expired, or generation-mismatched references fail
    /// closed and produce a diagnostic counter." The precondition assertions are not
    /// decoration: without them a stub that never recycles a slot would pass vacuously
    /// because the two identities would be unrelated.
    /// </remarks>
    internal static void GenerationMismatchFailsClosed(
        string subject,
        EntityId staleId,
        EntityId liveId,
        Func<EntityId, bool> resolves,
        Func<long> staleReferenceCounter)
    {
        ArgumentNullException.ThrowIfNull(resolves);
        ArgumentNullException.ThrowIfNull(staleReferenceCounter);

        long before = staleReferenceCounter();
        bool staleResolved = resolves(staleId);
        long afterStale = staleReferenceCounter();
        bool liveResolved = resolves(liveId);

        Expect.Multiple(() =>
        {
            Assert.That(
                staleId.Index,
                Is.EqualTo(liveId.Index),
                subject + ": the two identities must name the same recycled slot, or the "
                    + "assertion proves nothing about generations");
            Assert.That(
                staleId.Generation,
                Is.Not.EqualTo(liveId.Generation),
                subject + ": recycling a slot must have incremented its generation");
            Assert.That(
                staleResolved,
                Is.False,
                subject + ": a generation-mismatched reference must fail closed, not resolve "
                    + "to the live record now occupying the slot");
            Assert.That(
                liveResolved,
                Is.True,
                subject + ": the current identity for that slot must still resolve, or "
                    + "failing closed is indistinguishable from resolving nothing at all");
            Assert.That(
                afterStale - before,
                Is.EqualTo(1L),
                subject + ": exactly one diagnostic counter increment per failed resolution");
        });
    }

    /// <summary>
    /// Asserts that iteration order is the documented comparison and not insertion order.
    /// </summary>
    /// <param name="subject">What is being judged, for the failure message.</param>
    /// <param name="expectedRendering">
    /// The order an independent reference comparison produces: authored priority key
    /// ascending, then the full entity ID.
    /// </param>
    /// <param name="firstRendering">The order observed after one insertion sequence.</param>
    /// <param name="secondRendering">The order observed after a different insertion sequence over the same members.</param>
    /// <remarks>
    /// doc 20 § Entity identity: "Stable ordering uses the full entity ID after a system's
    /// authored priority keys." doc 10 § System phase ordering: "Simultaneous outcomes use
    /// documented stable ordering rather than collection or thread timing." Both halves are
    /// asserted: agreement between two insertion sequences catches order leaking out of
    /// storage, and agreement with the reference comparison catches an order that is stable
    /// but wrong.
    /// </remarks>
    internal static void IterationOrderMatchesTheDocumentedComparison(
        string subject,
        string expectedRendering,
        string firstRendering,
        string secondRendering)
    {
        Expect.Multiple(() =>
        {
            // Without this, three empty strings satisfy both assertions below and the gate passes
            // having compared nothing: a store that iterated zero records would agree with a
            // reference that sorted zero records. doc 91 § Acceptance evidence wants a gate that can
            // fail, and a gate that is vacuous on empty input cannot.
            Assert.That(
                firstRendering,
                Is.Not.Empty,
                subject + ": the fixture must render at least one record, or this assertion compares "
                    + "nothing and passes whatever the ordering rule does");
            Assert.That(
                secondRendering,
                Is.EqualTo(firstRendering),
                subject + ": two stores holding the same members inserted in different "
                    + "orders must iterate identically, so no observable order comes from "
                    + "insertion or collection enumeration");
            Assert.That(
                firstRendering,
                Is.EqualTo(expectedRendering),
                subject + ": order must be authored priority key ascending, then the full "
                    + "entity ID");
        });
    }

    /// <summary>
    /// Renders an ordered identity sequence as canonical invariant text, with storage indices
    /// relative to their partition.
    /// </summary>
    /// <param name="identifiers">The identities in the order observed.</param>
    /// <param name="priorityKeyOf">Reads the authored priority key an identity was admitted with.</param>
    /// <param name="partitionLabel">The partition's short name, such as <c>pickup</c>.</param>
    /// <param name="partitionOffset">
    /// The partition's first slot index, computed from the capacity table by the caller.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">An identity lies below <paramref name="partitionOffset"/>.</exception>
    /// <remarks>
    /// <para>
    /// Includes the priority key so a golden diff shows <em>why</em> an order changed rather
    /// than only that it did, which doc 91 § Determinism and fixture policy means by
    /// "reviewable".
    /// </para>
    /// <para>
    /// <b>Partition-relative, not absolute.</b> A partition offset is the running sum of the hard
    /// capacities above it in doc 20 § Authoritative population categories order, and three of
    /// those rows - enemy projectile, weapon actor, damage zone - are
    /// <c>docs/technical/22-combat-and-weapon-runtime.md</c> § Performance and capacity ceilings that
    /// section reserves the right to move: "Profiling and legal maximum-output analysis must tighten
    /// or expand them before content complete." An absolute index in an <em>ordering</em> golden
    /// therefore fails whenever a combat capacity moves, which is an uninformative failure about
    /// something the golden does not test - and an uninformative golden failure is what gets a
    /// golden regenerated instead of investigated. Rendering <c>pickup+1</c> makes the file depend on
    /// the ordering rule alone.
    /// </para>
    /// </remarks>
    internal static string RenderOrder(
        System.Collections.Generic.IReadOnlyList<EntityId> identifiers,
        Func<EntityId, long> priorityKeyOf,
        string partitionLabel,
        int partitionOffset)
    {
        ArgumentNullException.ThrowIfNull(identifiers);
        ArgumentNullException.ThrowIfNull(priorityKeyOf);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionLabel);

        System.Text.StringBuilder builder = new();
        for (int index = 0; index < identifiers.Count; index++)
        {
            EntityId identity = identifiers[index];
            int relative = identity.Index - partitionOffset;
            ArgumentOutOfRangeException.ThrowIfNegative(relative, nameof(identifiers));

            builder
                .Append(index.ToString(CultureInfo.InvariantCulture).PadLeft(3))
                .Append("  priority=")
                .Append(priorityKeyOf(identity).ToString(CultureInfo.InvariantCulture).PadLeft(6))
                .Append("  ")
                .Append(partitionLabel)
                .Append('+')
                .Append(relative.ToString(CultureInfo.InvariantCulture))
                .Append("/g")
                .Append(identity.Generation.ToString(CultureInfo.InvariantCulture))
                .Append("@run")
                .Append(identity.RunSession.ToString(CultureInfo.InvariantCulture))
                .Append('\n');
        }

        return builder.ToString();
    }
}
