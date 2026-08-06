using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Runtime;

/// <summary>
/// The pause-reason set contract: exactly seven reasons, an immutable and idempotent set, and a
/// one-way terminal transition.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-002-001</c>, <c>VER-SIM-002-004</c>, <c>VER-SIM-002-008</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Pause contract: "Pause is represented as a set
/// of reasons rather than a single toggle. Initial blocking reasons are general pause, fabrication,
/// relic resolution, blocking tutorial/modal, focus loss, operating-system suspension, and terminal
/// transition."
/// </remarks>
[TestFixture]
internal sealed class PauseReasonSetTests
{
    /// <summary>
    /// The seven reasons doc 10 § Pause contract lists, written out here independently of the
    /// production enum so that the test states the document rather than restating the code.
    /// </summary>
    private static readonly string[] DocumentedReasonNames =
    {
        "GeneralPause",
        "Fabrication",
        "RelicResolution",
        "BlockingTutorialOrModal",
        "FocusLoss",
        "OperatingSystemSuspension",
        "TerminalTransition",
    };

    /// <summary>
    /// Verification: <c>VER-SIM-002-001</c>.
    ///
    /// Exactly the seven reasons of doc 10 § Pause contract are defined, in that order, with no
    /// eighth and none missing, each a distinct single bit, and with no zero-valued member that would
    /// let "no reason" masquerade as a reason.
    /// </summary>
    [Test]
    public void ExactlyTheSevenBlockingReasonsAreDefined()
    {
        PauseReason[] declared = (PauseReason[])Enum.GetValues(typeof(PauseReason));
        string[] declaredNames = Enum.GetNames(typeof(PauseReason));
        List<int> values = new();
        foreach (PauseReason reason in declared)
        {
            values.Add((int)reason);
        }

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(7L, declared.Length, "the enum defines exactly seven reasons");
            NumericAssert.AreExactlyEqual(
                7L,
                PauseReasonSet.ReasonCount,
                "and the set agrees on how many there are");
            Assert.That(
                declaredNames,
                Is.EqualTo(DocumentedReasonNames).AsCollection,
                "the reasons are exactly doc 10 § Pause contract's list, in the order it lists them");
            Assert.That(
                PauseReasonSet.AllReasons,
                Is.EqualTo(declared).AsCollection,
                "the set's authoritative ordering is the enum's declaration order");

            foreach (int value in values)
            {
                Assert.That(
                    value,
                    Is.GreaterThan(0),
                    "no reason is zero-valued: \"no reason\" is the empty set, not a reason");
                Assert.That(
                    value & (value - 1),
                    Is.EqualTo(0),
                    "each reason is a distinct single bit, so a set of them is a mask: value "
                        + value.ToString(CultureInfo.InvariantCulture));
            }

            Assert.That(
                values,
                Is.Unique,
                "no two reasons share a value, or one would silently imply the other");
            NumericAssert.AreExactlyEqual(
                128L,
                PauseReasonSet.SubsetCount,
                "seven single-bit reasons make 128 subsets, which is the sweep VER-SIM-002-003 covers");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-004</c>.
    ///
    /// The set is immutable and idempotent: adding a reason already present or clearing one absent
    /// yields an equal set and is not an error, and a set handed to a consumer cannot be changed by
    /// that consumer or by a later change to the run's own set.
    /// </summary>
    [Test]
    public void SetIsImmutableAndIdempotent()
    {
        PauseReasonSet withFabrication = PauseReasonSet.Empty.With(PauseReason.Fabrication);
        PauseReasonSet addedTwice = withFabrication.With(PauseReason.Fabrication);
        PauseReasonSet clearedAbsent = withFabrication.Without(PauseReason.FocusLoss);
        PauseReasonSet cleared = withFabrication.Without(PauseReason.Fabrication);

        // A consumer holds a set value; the run then changes its own set. The held value must not
        // move - which is what makes the set safe to publish across the boundary (TR-CTR-004).
        RunClock clock = new();
        clock.Raise(PauseReason.GeneralPause);
        PauseReasonSet handedToConsumer = clock.BlockingReasons;
        clock.Raise(PauseReason.RelicResolution);
        clock.Clear(PauseReason.GeneralPause);

        Expect.Multiple(() =>
        {
            Assert.That(
                addedTwice,
                Is.EqualTo(withFabrication),
                "adding a reason already present yields an equal set and is not an error");
            Assert.That(
                clearedAbsent,
                Is.EqualTo(withFabrication),
                "clearing a reason that is absent yields an equal set and is not an error");
            NumericAssert.AreExactlyEqual(1L, addedTwice.Count, "and does not double-count the reason");
            Assert.That(cleared, Is.EqualTo(PauseReasonSet.Empty), "clearing the only reason empties the set");
            Assert.That(
                withFabrication.Contains(PauseReason.Fabrication),
                "the original value is unchanged by either operation: it is a value, not a container");

            Assert.That(
                handedToConsumer,
                Is.EqualTo(PauseReasonSet.Of(PauseReason.GeneralPause)),
                "a set handed to a consumer is unaffected by later changes to the run's own set");
            Assert.That(
                clock.BlockingReasons,
                Is.EqualTo(PauseReasonSet.Of(PauseReason.RelicResolution)),
                "while the run's own set did change");

            Assert.That(
                typeof(PauseReasonSet).IsValueType,
                "the set is a value type, so publishing one cannot hand out a mutable reference");
            foreach (System.Reflection.PropertyInfo property in typeof(PauseReasonSet).GetProperties())
            {
                Assert.That(
                    property.CanWrite,
                    Is.False,
                    "no member of the set is writable: " + property.Name);
            }
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-008</c>.
    ///
    /// The terminal transition is one-way: the writer refuses to clear it back into an active run and
    /// says so, rather than ignoring the request silently. The set value type itself stays a pure set,
    /// because a set that sometimes refuses a member is not a set - the refusal belongs to the single
    /// writer doc 115 § Mutable-state ownership matrix requires.
    /// </summary>
    [Test]
    public void TerminalTransitionCannotBeClearedBackIntoAnActiveRun()
    {
        RunClock clock = new();
        clock.Raise(PauseReason.GeneralPause);
        PauseTransitionResult raised = clock.Raise(PauseReason.TerminalTransition);
        PauseTransitionResult refused = clock.Clear(PauseReason.TerminalTransition);
        PauseTransitionResult clearedOther = clock.Clear(PauseReason.GeneralPause);
        PauseTransitionResult refusedAgain = clock.Clear(PauseReason.TerminalTransition);

        // The value type is unaffected by the rule: it will happily produce a set without the
        // terminal transition, because that is what a set does. Only the writer refuses.
        PauseReasonSet pureSetWithoutTerminal =
            PauseReasonSet.Of(PauseReason.TerminalTransition).Without(PauseReason.TerminalTransition);

        Expect.Multiple(() =>
        {
            Assert.That(
                raised.Outcome,
                Is.EqualTo(PauseTransitionOutcome.Raised),
                "the terminal transition is raised like any other reason");

            Assert.That(
                refused.Outcome,
                Is.EqualTo(PauseTransitionOutcome.RefusedTerminalTransitionIsOneWay),
                "clearing it is rejected rather than silently ignored (VER-SIM-002-008)");
            Assert.That(refused.WasRefused, "and the refusal is observable as such");
            Assert.That(
                refused.ChangedTheSet,
                Is.False,
                "a refused transition changes nothing; doc 20 § Scope and invariants: \"a run terminal "
                    + "result is assigned once and is immutable\"");
            Assert.That(
                clock.BlockingReasons.Contains(PauseReason.TerminalTransition),
                "so the reason is still present afterwards");

            Assert.That(
                clearedOther.Outcome,
                Is.EqualTo(PauseTransitionOutcome.Cleared),
                "unrelated reasons still clear normally");
            Assert.That(
                clock.IsBlocking,
                "and the run stays blocked, because the terminal transition remains");
            Assert.That(
                refusedAgain.Outcome,
                Is.EqualTo(PauseTransitionOutcome.RefusedTerminalTransitionIsOneWay),
                "the refusal is not a one-time guard: it holds for every attempt");
            Expect.Throws<InvalidOperationException>(() => clock.CommitTick());

            Assert.That(
                pureSetWithoutTerminal,
                Is.EqualTo(PauseReasonSet.Empty),
                "the set value type stays a pure set; the one-way rule lives in the writer, which doc 115 "
                    + "§ Mutable-state ownership matrix makes the sole owner of terminal state");
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-001</c>.
    ///
    /// An unregistered reason value fails closed rather than being silently ignored, so a stale or
    /// corrupted value cannot resume a run that should stay blocked.
    /// </summary>
    [Test]
    public void AnUnregisteredReasonIsRefused()
    {
        const PauseReason unregistered = (PauseReason)128;

        Expect.Multiple(() =>
        {
            Expect.Throws<ArgumentOutOfRangeException>(() => PauseReasonSet.Empty.With(unregistered));
            Expect.Throws<ArgumentOutOfRangeException>(() => PauseReasonSet.Empty.Without(unregistered));
            Expect.Throws<ArgumentOutOfRangeException>(() => PauseReasonSet.Empty.Contains(unregistered));
            Expect.Throws<ArgumentOutOfRangeException>(() => PauseReasonSet.FromMask(-1));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => PauseReasonSet.FromMask(PauseReasonSet.SubsetCount));
        });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-002-004</c>.
    ///
    /// Every one of the 128 subsets round-trips through its mask and renders its reasons in doc 10's
    /// order regardless of the order they were added in.
    /// </summary>
    [Test]
    public void EverySubsetRoundTripsAndRendersInDocumentOrder()
    {
        Expect.Multiple(() =>
        {
            for (int mask = 0; mask < PauseReasonSet.SubsetCount; mask++)
            {
                PauseReasonSet subset = PauseReasonSet.FromMask(mask);
                ImmutableArray<PauseReason> ordered = subset.ToOrderedArray();

                PauseReasonSet rebuiltForwards = PauseReasonSet.Empty;
                foreach (PauseReason reason in ordered)
                {
                    rebuiltForwards = rebuiltForwards.With(reason);
                }

                PauseReasonSet rebuiltBackwards = PauseReasonSet.Empty;
                for (int index = ordered.Length - 1; index >= 0; index--)
                {
                    rebuiltBackwards = rebuiltBackwards.With(ordered[index]);
                }

                Assert.That(rebuiltForwards, Is.EqualTo(subset), "mask round-trip, forwards");
                Assert.That(
                    rebuiltBackwards,
                    Is.EqualTo(subset),
                    "mask round-trip, backwards: insertion order cannot change the set");
                Assert.That(
                    rebuiltBackwards.ToString(),
                    Is.EqualTo(subset.ToString()).Using(StringComparer.Ordinal),
                    "nor the canonical rendering");
                NumericAssert.AreExactlyEqual(
                    ordered.Length,
                    subset.Count,
                    "the count is the number of reasons present");
                Assert.That(
                    subset.IsBlocking,
                    Is.EqualTo(mask != 0),
                    "blocking if and only if non-empty (doc 10 § Pause contract)");
            }
        });
    }
}
