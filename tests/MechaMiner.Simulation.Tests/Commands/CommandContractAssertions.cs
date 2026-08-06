using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Commands;

/// <summary>
/// The <c>SIM-004</c> contract assertions, shared by the real gates and by the negative control.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports <c>VER-SIM-004-001</c>, <c>-002</c>, <c>-007</c>, <c>-008</c>, <c>-011</c>, and
/// <c>-012</c>.
/// </para>
/// <para>
/// They live here rather than inline so that the negative control runs the identical assertion the real
/// tests run: weakening one turns both red together, which is what
/// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence asks a control to demonstrate.
/// Every one uses <see cref="Expect.Multiple"/>, so a failure is a <c>MultipleAssertException</c> the
/// control can catch and inspect by message.
/// </para>
/// </remarks>
internal static class CommandContractAssertions
{
    /// <summary>
    /// Asserts that no command identity was applied twice.
    /// </summary>
    /// <param name="subject">What is being asserted about, named in the failure message.</param>
    /// <param name="appliedSequences">Every sequence that was applied, in application order.</param>
    /// <remarks>
    /// <c>docs/technical/10-runtime-architecture.md</c> § Commands and mutations: "A command is applied at
    /// most once."
    /// </remarks>
    internal static void ACommandWasAppliedAtMostOnce(string subject, IReadOnlyList<long> appliedSequences)
    {
        ArgumentNullException.ThrowIfNull(appliedSequences);

        HashSet<long> seen = new();
        List<long> repeated = new();
        for (int index = 0; index < appliedSequences.Count; index++)
        {
            if (!seen.Add(appliedSequences[index]))
            {
                repeated.Add(appliedSequences[index]);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                repeated,
                Is.Empty,
                subject
                    + ": doc 10 § Commands and mutations requires that a command is applied at most once, "
                    + "but sequence(s) "
                    + RenderSequences(repeated)
                    + " were applied more than once out of "
                    + appliedSequences.Count.ToString(CultureInfo.InvariantCulture)
                    + " application(s) "
                    + RenderSequences(appliedSequences));
        });
    }

    /// <summary>
    /// Asserts that a whole-state rendering is byte-identical before and after a refusal.
    /// </summary>
    /// <param name="subject">What is being asserted about, named in the failure message.</param>
    /// <param name="before">The rendering taken before the refused submission.</param>
    /// <param name="after">The rendering taken after it.</param>
    /// <remarks>
    /// <c>CTR-RUN-002</c> and <c>CTR-RUN-003</c> in doc 115 § Cross-boundary contract registry: a refusal
    /// returns a "typed rejection/no change" and a "stale preview changes nothing". Comparing a whole-state
    /// rendering rather than a list of fields is deliberate: a mutation nobody predicted still shows up as a
    /// text difference, whereas a field list only catches the fields its author thought of.
    /// </remarks>
    internal static void NothingAuthoritativeChanged(string subject, string before, string after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        Expect.Multiple(() =>
        {
            Assert.That(
                after,
                Is.EqualTo(before).Using(StringComparer.Ordinal),
                subject
                    + ": a refused submission returns a typed rejection with no mutation, so the whole "
                    + "authoritative state rendering must be byte-identical before and after it. Before:\n"
                    + before
                    + "\nAfter:\n"
                    + after);
        });
    }

    /// <summary>
    /// Asserts that the admitted sequence equals a reference model's, element for element.
    /// </summary>
    /// <param name="subject">What is being asserted about, named in the failure message.</param>
    /// <param name="reference">The reference model's rendering.</param>
    /// <param name="actual">The gate's rendering.</param>
    /// <remarks>
    /// <c>docs/technical/91-verification-strategy.md</c> § Reference models. The reference is a deliberately
    /// simple linear scan, written against the documented rules rather than against the implementation.
    /// </remarks>
    internal static void AdmittedSequenceMatchesTheReferenceModel(
        string subject,
        string reference,
        string actual)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(actual);

        Expect.Multiple(() =>
        {
            Assert.That(
                actual,
                Is.EqualTo(reference).Using(StringComparer.Ordinal),
                subject
                    + ": the admitted sequence must equal the reference model's. Reference:\n"
                    + reference
                    + "\nGate:\n"
                    + actual);
        });
    }

    /// <summary>
    /// Asserts that an authoritative state version advanced by exactly one.
    /// </summary>
    /// <param name="subject">What is being asserted about, named in the failure message.</param>
    /// <param name="before">The version before the transaction.</param>
    /// <param name="after">The version after it.</param>
    /// <remarks>
    /// doc 20 § Paused transactions: an accepted transaction returns "a new complete state/version", one of
    /// them, so a version that jumped by two would mean the commit ran twice.
    /// </remarks>
    internal static void StateVersionAdvancedExactlyOnce(string subject, long before, long after)
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                after,
                Is.EqualTo(before + 1),
                subject
                    + ": an accepted paused transaction publishes exactly one new state version, but the "
                    + "version went from "
                    + before.ToString(CultureInfo.InvariantCulture)
                    + " to "
                    + after.ToString(CultureInfo.InvariantCulture));
        });
    }

    /// <summary>Renders a sequence list as canonical invariant text for a failure message.</summary>
    /// <param name="sequences">The sequences to render.</param>
    internal static string RenderSequences(IReadOnlyList<long> sequences)
    {
        ArgumentNullException.ThrowIfNull(sequences);
        if (sequences.Count == 0)
        {
            return "[]";
        }

        StringBuilder builder = new("[");
        for (int index = 0; index < sequences.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(sequences[index].ToString(CultureInfo.InvariantCulture));
        }

        return builder.Append(']').ToString();
    }
}
