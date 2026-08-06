using System;
using System.Collections.Generic;
using System.Reflection;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Commands;

/// <summary>
/// Pins the active command path of <c>CMP-SIM-002</c>: at-most-once application, typed rejection with no
/// mutation, a monotonic sequence that is never reordered, and a tick's admitted set frozen once phase 1 ends.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-004-001</c>, <c>VER-SIM-004-002</c>, <c>VER-SIM-004-003</c>,
/// <c>VER-SIM-004-006</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Commands and mutations and § System phase ordering;
/// <c>docs/technical/20-simulation-core.md</c> § Tick transaction;
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry
/// (<c>CMP-SIM-002</c>) and § Cross-boundary contract registry (<c>CTR-RUN-002</c>).
/// </remarks>
[TestFixture]
internal sealed class CommandAdmissionGateTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-004-001</c>.
    ///
    /// Resubmitting an envelope with the same run identity, target tick, and sequence is refused as a
    /// duplicate and mutates nothing - immediately, and again several ticks later - while a genuinely new
    /// envelope in the same later tick is still admitted.
    /// </summary>
    [Test]
    public void ACommandIsAppliedAtMostOnce()
    {
        CommandFixture fixture = new();
        CommandAdmissionGate gate = fixture.Gate;
        List<long> applied = new();

        CommandEnvelope first = CommandFixture.Envelope(targetTick: 0, sequence: 0, rawInputX: 1.0, rawInputY: 0.0);
        gate.BeginTick(SimulationTick.Zero);
        Assert.That(gate.TryAdmit(first, out CommandRejection none), Is.True, "the first submission is admitted");
        applied.Add(first.Sequence);

        string afterAdmission = gate.RenderAuthoritative();
        Assert.That(gate.TryAdmit(first, out CommandRejection immediate), Is.False);
        string afterImmediateResubmission = gate.RenderAuthoritative();

        gate.FreezeTick();

        // Three more ticks pass. The resubmission now arrives long after the tick it names was frozen, which
        // is the "including when the resubmission arrives in a later tick" half of the entry.
        for (long tick = 1; tick <= 3; tick++)
        {
            gate.BeginTick(new SimulationTick(tick));
            gate.FreezeTick();
        }

        gate.BeginTick(new SimulationTick(4));
        string beforeLateResubmission = gate.RenderAuthoritative();
        Assert.That(gate.TryAdmit(first, out CommandRejection late), Is.False);
        string afterLateResubmission = gate.RenderAuthoritative();

        // The contrast: the gate is not simply refusing everything after the first admission.
        CommandEnvelope fresh = CommandFixture.Envelope(targetTick: 4, sequence: 1, rawInputX: 0.0, rawInputY: -1.0);
        Assert.That(gate.TryAdmit(fresh, out CommandRejection _), Is.True, "a genuinely new envelope is admitted");
        applied.Add(fresh.Sequence);

        CommandContractAssertions.ACommandWasAppliedAtMostOnce("the real admission gate", applied);
        CommandContractAssertions.NothingAuthoritativeChanged(
            "an immediate resubmission of an admitted envelope",
            afterAdmission,
            afterImmediateResubmission);
        CommandContractAssertions.NothingAuthoritativeChanged(
            "a resubmission four ticks after the envelope's tick was frozen",
            beforeLateResubmission,
            afterLateResubmission);

        Expect.Multiple(() =>
        {
            Assert.That(none.IsRejection, Is.False, "an admitted envelope yields no rejection");
            Assert.That(
                immediate.Reason,
                Is.EqualTo(CommandRejectionReason.Duplicate),
                "an immediate resubmission is a duplicate");
            Assert.That(
                late.Reason,
                Is.EqualTo(CommandRejectionReason.Duplicate),
                "and so is one that arrives four ticks later, rather than becoming merely stale");
            Assert.That(
                gate.AdmittedInRun,
                Is.EqualTo(2L),
                "exactly two distinct commands were admitted across the whole run");
            Assert.That(
                gate.RejectionCount(CommandRejectionReason.Duplicate),
                Is.EqualTo(2L),
                "and both resubmissions were counted as duplicates");
            Assert.That(
                gate.IdempotencyHistoryCount,
                Is.EqualTo(2),
                "the never-evicted history holds one entry per admitted command");
        });

        // Reading a reason from a non-rejection is refused rather than reporting the zero member.
        Expect.Throws<InvalidOperationException>(() => { _ = none.Reason; });
    }

    /// <summary>
    /// Verification: <c>VER-SIM-004-002</c>.
    ///
    /// A stale, duplicate, or invalid envelope returns a typed reason and leaves the whole authoritative state
    /// rendering byte-identical - while an admitted envelope changes it, so the comparison is sensitive rather
    /// than trivially satisfied.
    /// </summary>
    [Test]
    public void StaleDuplicateAndInvalidEnvelopesRejectWithoutMutation()
    {
        CommandFixture fixture = new();
        CommandAdmissionGate gate = fixture.Gate;

        gate.BeginTick(SimulationTick.Zero);
        CommandEnvelope admitted = CommandFixture.Envelope(0, 0, 0.5, 0.5);
        Assert.That(gate.TryAdmit(admitted, out CommandRejection _), Is.True);
        AdmittedCommandSet frozenTickZero = gate.FreezeTick();

        gate.BeginTick(new SimulationTick(1));

        // The sensitivity control first: an admission must change the rendering, or "byte-identical after a
        // rejection" would hold no matter what the gate did.
        string beforeAdmission = gate.RenderAuthoritative();
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(1, 1, 1.0, 0.0), out CommandRejection _), Is.True);
        string afterAdmission = gate.RenderAuthoritative();
        Assert.That(
            afterAdmission,
            Is.Not.EqualTo(beforeAdmission).Using(StringComparer.Ordinal),
            "an admission must change the authoritative rendering, or the no-mutation comparison below would "
                + "be vacuous");

        AssertRejectionChangesNothing(
            gate,
            "a stale envelope naming an already-frozen tick",
            CommandFixture.Envelope(targetTick: 0, sequence: 2, rawInputX: 1.0, rawInputY: 0.0),
            CommandRejectionReason.Stale,
            frozenTickZero);

        AssertRejectionChangesNothing(
            gate,
            "a duplicate of an already-admitted envelope",
            admitted,
            CommandRejectionReason.Duplicate,
            frozenTickZero);

        AssertRejectionChangesNothing(
            gate,
            "an envelope whose payload has no normalized value",
            CommandFixture.Envelope(targetTick: 1, sequence: 3, rawInputX: double.NaN, rawInputY: 1.0),
            CommandRejectionReason.InvalidPayload,
            frozenTickZero);

        AssertRejectionChangesNothing(
            gate,
            "an envelope from another run session",
            CommandFixture.ForeignEnvelope(targetTick: 1, sequence: 4, rawInputX: 1.0, rawInputY: 0.0),
            CommandRejectionReason.ForeignRunSession,
            frozenTickZero);

        AssertRejectionChangesNothing(
            gate,
            "an envelope for a tick whose admission window has not opened",
            CommandFixture.Envelope(targetTick: 9, sequence: 5, rawInputX: 1.0, rawInputY: 0.0),
            CommandRejectionReason.AdmissionClosed,
            frozenTickZero);

        Expect.Multiple(() =>
        {
            Assert.That(gate.AdmittedInRun, Is.EqualTo(2L), "only the two real admissions took effect");
            Assert.That(gate.RejectedInRun, Is.EqualTo(5L), "and all five refusals were counted");
            Assert.That(
                gate.OpenTickAdmittedCount,
                Is.EqualTo(1),
                "the open tick still holds exactly the one command it admitted");
        });
    }

    /// <summary>
    /// Verification: supports <c>VER-SIM-004-002</c>.
    ///
    /// The three ways of missing the admission window carry three different details under one reason code: a
    /// tick ahead of the open window, a tick behind it that was never frozen, and no open window at all.
    /// </summary>
    /// <remarks>
    /// The reason code is deliberately one value, because a caller branching on it is deciding whether to
    /// resubmit and the answer is the same in all three. The detail is what a human reads, and these are three
    /// different mistakes: a caller running early, a caller aimed at a tick the run skipped, and a caller
    /// submitting outside phase 1. Asserting that the three strings differ is what keeps a later
    /// simplification from collapsing them back into one sentence.
    /// </remarks>
    [Test]
    public void TheThreeWaysOfMissingTheAdmissionWindowReadDifferently()
    {
        CommandFixture fixture = new();
        CommandAdmissionGate gate = fixture.Gate;

        gate.BeginTick(SimulationTick.Zero);
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(0, 0, 1.0, 0.0), out CommandRejection _), Is.True);
        gate.FreezeTick();

        // Tick 5 opens without tick 3 having been opened or frozen, which is what makes a tick behind the
        // window but ahead of the frozen high-water mark reachable at all.
        gate.BeginTick(new SimulationTick(5));
        string ahead = AdmissionClosedDetail(gate, CommandFixture.Envelope(9, 1, 1.0, 0.0));
        string behind = AdmissionClosedDetail(gate, CommandFixture.Envelope(3, 2, 1.0, 0.0));

        gate.FreezeTick();
        string noWindow = AdmissionClosedDetail(gate, CommandFixture.Envelope(6, 3, 1.0, 0.0));

        Expect.Multiple(() =>
        {
            Assert.That(
                ahead,
                Does.Contain("is ahead of the open admission window"),
                "a caller running early must be told that its envelope is early rather than wrong");
            Assert.That(
                behind,
                Does.Contain("is behind the open admission window"),
                "a caller aimed at a tick the run skipped must be told no window for it will ever open");
            Assert.That(
                noWindow,
                Does.Contain("no admission window is open"),
                "and a caller submitting outside phase 1 must be told that");
            Assert.That(
                new[] { ahead, behind, noWindow },
                Is.Unique,
                "three different mistakes must read differently, or the detail carries no more information "
                    + "than the reason code already did");
            Assert.That(
                gate.RejectionCount(CommandRejectionReason.AdmissionClosed),
                Is.EqualTo(3L),
                "all three are the same reason code, which is what a caller branches on");
        });
    }

    /// <summary>
    /// Submits an envelope expected to be refused as <see cref="CommandRejectionReason.AdmissionClosed"/> and
    /// returns the refusal's detail.
    /// </summary>
    /// <param name="gate">The gate to submit to.</param>
    /// <param name="envelope">The envelope to submit.</param>
    private static string AdmissionClosedDetail(CommandAdmissionGate gate, in CommandEnvelope envelope)
    {
        Assert.That(
            gate.TryAdmit(envelope, out CommandRejection rejection),
            Is.False,
            "the envelope must be refused, or there is no detail to read");
        Assert.That(
            rejection.Reason,
            Is.EqualTo(CommandRejectionReason.AdmissionClosed),
            "the refusal must be the admission-window one and not some earlier check");
        return rejection.Detail;
    }

    /// <summary>
    /// Verification: <c>VER-SIM-004-003</c>.
    ///
    /// A sequence at or below the highest already-admitted sequence is refused, a gap is admitted without
    /// being backfilled, the high-water mark never decreases even across refusals, and the admitted order is
    /// the submission order.
    /// </summary>
    [Test]
    public void SequenceIsMonotonicAndNeverReordered()
    {
        CommandFixture fixture = new();
        CommandAdmissionGate gate = fixture.Gate;

        gate.BeginTick(SimulationTick.Zero);
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(0, 0, 1.0, 0.0), out CommandRejection _), Is.True);

        // A gap: sequence 5 is admitted with 1 to 4 never having arrived.
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(0, 5, 0.0, 1.0), out CommandRejection _), Is.True);

        long highWaterAfterGap = gate.HighestAdmittedSequence;

        // A sequence inside the gap has never been admitted, so it is a pure regression rather than a
        // duplicate - which is what distinguishes the two reasons.
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(0, 3, 1.0, 0.0), out CommandRejection regression), Is.False);

        // A sequence spent on a different tick is a regression too: reusing it would make the run's sequence
        // ambiguous, but it is not the same envelope, so it is not a duplicate.
        AdmittedCommandSet tickZero = gate.FreezeTick();
        gate.BeginTick(new SimulationTick(1));
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(1, 5, 1.0, 0.0), out CommandRejection reused), Is.False);
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(1, 6, -1.0, 0.0), out CommandRejection _), Is.True);
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(1, 9, 0.0, -1.0), out CommandRejection _), Is.True);
        AdmittedCommandSet tickOne = gate.FreezeTick();

        Expect.Multiple(() =>
        {
            Assert.That(
                regression.Reason,
                Is.EqualTo(CommandRejectionReason.SequenceRegression),
                "a never-admitted sequence below the high-water mark is a regression, not a duplicate");
            Assert.That(
                reused.Reason,
                Is.EqualTo(CommandRejectionReason.SequenceRegression),
                "and so is a sequence already spent on a different tick");
            Assert.That(
                gate.HighestAdmittedSequence,
                Is.GreaterThanOrEqualTo(highWaterAfterGap),
                "the high-water mark never decreases, whatever is refused");
            Assert.That(tickZero.Count, Is.EqualTo(2), "the gap was admitted, not backfilled");
            Assert.That(tickZero.SequenceAt(0), Is.EqualTo(0L), "and the admitted order is the submission order");
            Assert.That(tickZero.SequenceAt(1), Is.EqualTo(5L), "with the gap left as a gap");
            Assert.That(
                tickZero.ContainsSequence(3),
                Is.False,
                "nothing filled in the missing sequences");
            Assert.That(tickOne.Count, Is.EqualTo(2), "the next tick admitted its own two");
            Assert.That(tickOne.SequenceAt(0), Is.EqualTo(6L), "in submission order");
            Assert.That(tickOne.SequenceAt(1), Is.EqualTo(9L), "ascending across the whole run");
            Assert.That(tickOne.HighestSequence, Is.EqualTo(9L), "and the highest is the last, not the first");
            Assert.That(
                tickOne.LatestIntent.X,
                Is.EqualTo(0.0),
                "the tick's effective intent is the last admitted one, which was (0,-1)");
            Assert.That(
                tickOne.LatestIntent.Y,
                Is.EqualTo(-1.0),
                "and not the (-1,0) admitted before it");
        });

        // Reordering is not merely absent from the observed output: the frozen set refuses to hold a
        // non-ascending order at all, so a copy that reordered would throw rather than publish.
        ArgumentException reordered = Expect.Throws<ArgumentException>(
            () => AdmittedCommandSet.Freeze(
                CommandFixture.RunSession,
                SimulationTick.Zero,
                new long[] { 5, 0 },
                new[] { MovementIntent.Stop, MovementIntent.Stop }));
        Assert.That(
            reordered.Message,
            Does.Contain("strictly increasing"),
            "freezing a reordered set is refused, so \"never reordered\" survives the copy out of the gate");
    }

    /// <summary>
    /// Verification: <c>VER-SIM-004-006</c>.
    ///
    /// A command admitted for tick N is in tick N's set and not in tick N-1's, a set already handed out does
    /// not change when the next tick admits, a post-freeze submission for that tick is refused, and the set
    /// type declares no member that could alter it.
    /// </summary>
    [Test]
    public void AdmittedCommandsAreFrozenForTheTickTheyTarget()
    {
        CommandFixture fixture = new();
        CommandAdmissionGate gate = fixture.Gate;

        gate.BeginTick(SimulationTick.Zero);
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(0, 0, 1.0, 0.0), out CommandRejection _), Is.True);
        AdmittedCommandSet tickZero = gate.FreezeTick();
        string tickZeroAsFrozen = tickZero.Render();

        gate.BeginTick(new SimulationTick(1));
        Assert.That(gate.TryAdmit(CommandFixture.Envelope(1, 1, 0.0, 1.0), out CommandRejection _), Is.True);

        // Tick 1 has admitted, but tick 0's set was handed out before that happened. If the set shared the
        // gate's working storage, this rendering would already have changed.
        string tickZeroWhileTickOneIsOpen = tickZero.Render();

        AdmittedCommandSet tickOne = gate.FreezeTick();

        // A later phase of tick 1 trying to append: refused, and the set it holds is untouched.
        Assert.That(
            gate.TryAdmit(CommandFixture.Envelope(1, 2, -1.0, 0.0), out CommandRejection lateAppend),
            Is.False);

        gate.BeginTick(new SimulationTick(2));
        string tickZeroAfterTwoMoreTicks = tickZero.Render();
        string tickOneAfterTwoMoreTicks = tickOne.Render();

        Expect.Multiple(() =>
        {
            Assert.That(tickZero.TargetTick.Index, Is.EqualTo(0L), "tick 0's set names tick 0");
            Assert.That(tickOne.TargetTick.Index, Is.EqualTo(1L), "and tick 1's names tick 1");
            Assert.That(
                tickOne.ContainsSequence(1),
                Is.True,
                "a command admitted for tick 1 is visible to tick 1");
            Assert.That(
                tickZero.ContainsSequence(1),
                Is.False,
                "and is not visible to tick 0, which was frozen before it arrived");
            Assert.That(tickZero.ContainsSequence(0), Is.True, "tick 0 still holds its own command");
            Assert.That(tickOne.ContainsSequence(0), Is.False, "and tick 1 does not hold tick 0's");
            Assert.That(
                tickZeroWhileTickOneIsOpen,
                Is.EqualTo(tickZeroAsFrozen).Using(StringComparer.Ordinal),
                "a set handed out does not change when a later tick admits");
            Assert.That(
                tickZeroAfterTwoMoreTicks,
                Is.EqualTo(tickZeroAsFrozen).Using(StringComparer.Ordinal),
                "nor after the working storage has been cleared and refilled twice");
            Assert.That(
                lateAppend.Reason,
                Is.EqualTo(CommandRejectionReason.Stale),
                "a submission for a frozen tick is refused as stale");
            Assert.That(
                tickOneAfterTwoMoreTicks,
                Is.EqualTo(tickOne.Render()).Using(StringComparer.Ordinal),
                "and the refused append left tick 1's set exactly as it was");
            Assert.That(tickOne.Count, Is.EqualTo(1), "with its one command and no more");
            Assert.That(
                AdmittedCommandSet.Unfrozen.IsFrozen,
                Is.False,
                "the default value is distinguishable from an empty frozen tick");
        });

        // A tick that admitted nothing is a frozen set that is empty, not an absent one.
        AdmittedCommandSet emptyTick = gate.FreezeTick();
        Expect.Multiple(() =>
        {
            Assert.That(emptyTick.IsFrozen, Is.True, "a tick that admitted nothing still froze");
            Assert.That(emptyTick.IsEmpty, Is.True, "and its set is empty");
            Assert.That(emptyTick.HighestSequence, Is.EqualTo(-1L), "with no highest sequence");
            Assert.That(
                emptyTick.LatestIntent,
                Is.EqualTo(MovementIntent.Stop),
                "and doc 20 § Active commands' stop-on-zero as its effective intent");
        });

        AssertNoMemberCanAlterAFrozenSet();
    }

    /// <summary>
    /// Submits one envelope that must be refused, and asserts both the typed reason and that nothing
    /// authoritative moved.
    /// </summary>
    private static void AssertRejectionChangesNothing(
        CommandAdmissionGate gate,
        string subject,
        CommandEnvelope envelope,
        CommandRejectionReason expectedReason,
        AdmittedCommandSet heldSet)
    {
        string before = gate.RenderAuthoritative();
        string heldBefore = heldSet.Render();
        bool admitted = gate.TryAdmit(envelope, out CommandRejection rejection);
        string after = gate.RenderAuthoritative();
        string heldAfter = heldSet.Render();
        long refusedSequence = envelope.Sequence;

        Expect.Multiple(() =>
        {
            Assert.That(admitted, Is.False, subject + " must not be admitted");
            Assert.That(rejection.IsRejection, Is.True, subject + " must yield a typed rejection");
            Assert.That(rejection.Reason, Is.EqualTo(expectedReason), subject + " must report its reason");
            Assert.That(
                rejection.Sequence,
                Is.EqualTo(refusedSequence),
                subject + "'s rejection must name the identity it refused");
            Assert.That(
                rejection.Detail,
                Is.Not.Empty,
                subject + "'s rejection must say why, for UI presentation");
            Assert.That(
                heldAfter,
                Is.EqualTo(heldBefore).Using(StringComparer.Ordinal),
                subject + " must not reach a set that was already frozen");
        });

        CommandContractAssertions.NothingAuthoritativeChanged(subject, before, after);
    }

    /// <summary>
    /// The structural half of <c>VER-SIM-004-006</c>: <see cref="AdmittedCommandSet"/> declares no mutator and
    /// no settable property, so "no later phase can alter or append to that tick's admitted set" is a property
    /// of the type rather than a rule phases 2 to 14 have to obey.
    /// </summary>
    private static void AssertNoMemberCanAlterAFrozenSet()
    {
        const BindingFlags surface =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        string[] mutatingNames = { "Add", "Insert", "Remove", "RemoveAt", "Clear", "Set", "Sort", "Reverse" };

        List<string> settableProperties = new();
        List<string> mutators = new();

        foreach (PropertyInfo property in typeof(AdmittedCommandSet).GetProperties(surface))
        {
            if (property.CanWrite)
            {
                settableProperties.Add(property.Name);
            }
        }

        foreach (MethodInfo method in typeof(AdmittedCommandSet).GetMethods(surface))
        {
            foreach (string mutatingName in mutatingNames)
            {
                if (method.Name.StartsWith(mutatingName, StringComparison.Ordinal))
                {
                    mutators.Add(method.Name);
                }
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                settableProperties,
                Is.Empty,
                "AdmittedCommandSet must declare no settable property: " + string.Join(", ", settableProperties));
            Assert.That(
                mutators,
                Is.Empty,
                "and no member whose name suggests mutation: " + string.Join(", ", mutators));
            Assert.That(
                typeof(AdmittedCommandSet).IsValueType,
                Is.True,
                "and it must be a value type, so a consumer receives a copy rather than the gate's own set");
        });

        // A frozen set's indexed accessors refuse to be read outside the set, so a phase cannot reach past
        // the tick's commands into whatever storage lies beyond.
        AdmittedCommandSet single = AdmittedCommandSet.Freeze(
            CommandFixture.RunSession,
            SimulationTick.Zero,
            new long[] { 4 },
            new[] { MovementIntent.Normalize(1.0, 0.0) });

        Expect.Multiple(() =>
        {
            Assert.That(single.SequenceAt(0), Is.EqualTo(4L), "the one element reads back");
            Assert.That(
                single.IntentAt(0).Magnitude,
                Is.EqualTo(1.0).Within(1e-15),
                "with its normalized intent");
        });

        // Reading past the set, or before it, is refused.
        Expect.Throws<ArgumentOutOfRangeException>(() => single.SequenceAt(1));
        Expect.Throws<ArgumentOutOfRangeException>(() => single.IntentAt(-1));
    }

    /// <summary>
    /// Verification: supports <c>VER-SIM-004-006</c>.
    ///
    /// The five refusals a caller outside this assembly can actually reach are typed, say which rule they
    /// enforce, and leave the gate's authoritative state byte-identical.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each of these is a documented guard on a public member with no test of its own, which is a different
    /// gap from an unreachable one: nothing about them is hard to reach, so nothing but the absence of a test
    /// explains their absence from the evidence. They are asserted together because each is one call and one
    /// refusal, and grouping them keeps the whole-state comparison around all five.
    /// </para>
    /// <para>
    /// <see cref="CommandAdmissionGate.BeginTick(SimulationTick)"/>'s frozen-tick guard is the load-bearing
    /// one: doc 10 § System phase ordering admits once per tick, in phase 1, and
    /// <c>VER-SIM-004-006</c>'s "no later phase can alter or append to that tick's admitted set" rests on a
    /// freeze that admission cannot reopen. Its still-open sibling is the other half of "once per tick".
    /// </para>
    /// </remarks>
    [Test]
    public void TheGatesReachableRefusalsAreTypedAndChangeNothing()
    {
        CommandFixture fixture = new();
        CommandAdmissionGate gate = fixture.Gate;

        gate.BeginTick(SimulationTick.Zero);
        gate.TryAdmit(CommandFixture.Envelope(0, 0, 1.0, 0.0), out CommandRejection _);
        gate.FreezeTick();

        // The still-open refusal is asserted while a window is open, because it is the check that runs
        // first; the frozen-tick refusal below is only reachable with the window closed, which is itself
        // the ordering VER-SIM-004-006 depends on.
        gate.BeginTick(new SimulationTick(1));
        InvalidOperationException secondWindow = Expect.Throws<InvalidOperationException>(
            () => gate.BeginTick(new SimulationTick(2)));
        gate.FreezeTick();

        string before = gate.RenderAuthoritative();

        InvalidOperationException reopenedFrozenTick = Expect.Throws<InvalidOperationException>(
            () => gate.BeginTick(SimulationTick.Zero));
        InvalidOperationException nothingToFreeze = Expect.Throws<InvalidOperationException>(
            () => gate.FreezeTick());
        ArgumentException duplicateAction = Expect.Throws<ArgumentException>(
            () => gate.RegisterTransactionAction(
                CommandFixture.InstallActionId,
                fixture.ItemInstalled,
                requiresConfirmation: false,
                domainValidator: _ => true));
        ArgumentOutOfRangeException undefinedReason = Expect.Throws<ArgumentOutOfRangeException>(
            () => gate.RejectionCount((CommandRejectionReason)(-1)));
        ArgumentException defaultedRequest = Expect.Throws<ArgumentException>(
            () => gate.Apply(
                default,
                PauseReasonSet.Of(PauseReason.GeneralPause),
                fixture.StageReplacementState,
                fixture.Publisher,
                fixture.DomainEvents,
                fixture.PresentationEvents,
                PresentationCoalescingPolicy.Verbatim));

        string after = gate.RenderAuthoritative();

        CommandContractAssertions.NothingAuthoritativeChanged(
            "five refused calls on the gate",
            before,
            after);

        Expect.Multiple(() =>
        {
            Assert.That(
                reopenedFrozenTick.Message,
                Does.Contain("was already frozen"),
                "a frozen tick's admitted set is final, so admission cannot reopen for it: this is what "
                    + "makes the freeze final rather than conventional");
            Assert.That(
                secondWindow.Message,
                Does.Contain("is still open"),
                "and a second window while one is open would be a second phase 1 for the same tick");
            Assert.That(
                nothingToFreeze.Message,
                Does.Contain("no admission window is open"),
                "freezing without a window would produce an admitted set for a tick that had no phase 1");
            Assert.That(
                duplicateAction.ParamName,
                Is.EqualTo("actionId"),
                "one rule per action identity, or the applied outcome would depend on registration order");
            Assert.That(
                undefinedReason.ParamName,
                Is.EqualTo("reason"),
                "an undefined reason has no counter, and answering zero for it would report a refusal "
                    + "category that does not exist");
            Assert.That(
                defaultedRequest.ParamName,
                Is.EqualTo("request"),
                "a defaulted request names no run and no action, so it is refused before the run fence is "
                    + "even consulted");
            Assert.That(
                gate.IsAdmissionOpen,
                Is.False,
                "no window was opened by any of the refused calls");
            Assert.That(
                gate.LastFrozenTickIndex,
                Is.EqualTo(1L),
                "and the last frozen tick is still the one the run actually froze");
            Assert.That(
                gate.RegisteredTransactionActionCount,
                Is.EqualTo(2),
                "the refused registration added nothing");
        });

        // The contrast: a tick the run has not passed still opens, so the frozen-tick refusal was about the
        // tick being frozen and not about the gate having stopped admitting.
        Expect.DoesNotThrow(() => gate.BeginTick(new SimulationTick(2)));
    }
}
