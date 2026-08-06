using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MechaMiner.Simulation.Entities;
using MechaMiner.Simulation.Events;
using MechaMiner.Simulation.Runtime;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Simulation.Time;

namespace MechaMiner.Simulation.Commands;

/// <summary>
/// <c>CMP-SIM-002</c>: the one gate every piece of authoritative external intent passes through, whether it
/// arrives as an active command for a tick or as a paused transaction between ticks.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Component registry,
/// <c>CMP-SIM-002</c>: state "admitted sequence/idempotency history", input "input commands and paused
/// transaction requests", output "accepted normalized commands or typed rejection", timing "before tick or
/// atomically between ticks", and explicitly not "UI presentation or partial mutations".
/// <c>docs/technical/10-runtime-architecture.md</c> § Commands and mutations: "All authoritative external
/// intent crosses into the run through typed commands or paused transactions", and "A command is applied at
/// most once."
/// § System phase ordering, phase 1: "Admit and normalize commands for the tick."
/// § Pause contract: a transaction "mutates the frozen simulation atomically between ticks and publishes a
/// replacement snapshot before resumption".
/// </para>
/// <para>
/// <b>One component, so one type.</b> doc 115 names <c>CMP-SIM-002</c> the "Command/transaction gate" and
/// gives it one row of owned state. Splitting the active and paused paths into two classes would give that
/// row two writers, which doc 115 § Mutable-state ownership matrix forbids: "Each mutable datum has exactly
/// one row owner."
/// </para>
/// <para>
/// <b>Double application is unavailable, not merely tested against.</b> A command reaches the tick's
/// admitted set through exactly one path - the tail of <see cref="TryAdmit"/> - and every statement before
/// that tail is a refusal. Two independent structures make re-admission unreachable there:
/// <see cref="HighestAdmittedSequence"/>, a high-water mark that no sequence at or below can pass, and the
/// complete sequence-to-tick history, which recognizes the exact envelope identity however late it arrives.
/// Either alone would already make re-admission impossible; the history exists so that a re-submission is
/// diagnosed as <see cref="CommandRejectionReason.Duplicate"/> rather than as a bare regression. The same
/// shape holds for paused transactions: <see cref="Apply"/> has one commit block, reaches it only after the
/// idempotency history has been consulted, and the commit itself is <see cref="CommitApplied"/>, which
/// cannot reach a mutation until that commit's own duplicate-key precondition has been established.
/// </para>
/// <para>
/// <b>The <c>Dictionary.Add</c> on each history is a backstop, not a third defence - and still must not be
/// relaxed to an indexer.</b> Both histories are written with <c>Add</c> rather than with an indexer
/// assignment, so a duplicate key throws instead of quietly overwriting the first entry. What that buys is
/// narrower than it looks, and the narrower reading is the accurate one. Disabling the two checks above and
/// leaving only the <c>Add</c> was probed directly: in <see cref="Apply"/> the <c>Add</c> is the last
/// statement of the commit, so it fires after the domain event has been appended, after the replacement
/// snapshot has been published, and after the state version has advanced. It turns a second application that
/// has already completed into an exception after the fact; it refuses nothing, and an exception thrown once a
/// state has been published is the case <c>docs/technical/20-simulation-core.md</c> § Tick transaction rules
/// out, since an invariant failure there belongs "before commit" and "never publishes a partial state". The
/// refusal is the idempotency check, and inside the commit it is the precondition
/// <see cref="CommitApplied"/> is called behind; both run before anything moves. The <c>Add</c> stays
/// because a history nothing may rewrite should not have a write that can silently rewrite it, which is a
/// last-resort invariant worth keeping even once it is unreachable - it is simply not what makes "applied at
/// most once" hold.
/// </para>
/// <para>
/// <b>The history is never evicted.</b> A bounded history would let a duplicate become admissible again once
/// its entry aged out, which is exactly the failure "applied at most once" names. At 60 Hz over the
/// 35-minute run of doc 20 § Boundary and tie ordering that is a bounded 126,000 entries for a
/// one-command-per-tick stream - a known, small, run-scoped cost, paid so that the guarantee does not have a
/// time limit.
/// </para>
/// <para>
/// <b>The dictionaries are lookup indexes only.</b> Authoritative order lives in the admission-order lists
/// and in the frozen <see cref="AdmittedCommandSet"/>; every rendering that walks a dictionary sorts its
/// keys first, so no output depends on hash iteration order.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): the input adapter and <c>CMP-UI-001</c> in
/// <c>game/</c> submit to this gate through the application coordinator, <c>MechaMiner.Tools</c> drives it
/// from scenario replays, and <c>MechaMiner.Game.Tests</c> asserts on it. Hence <c>public</c>.
/// </para>
/// </remarks>
public sealed class CommandAdmissionGate
{
    /// <summary>The authoritative state version a run starts at, before any transaction has been applied.</summary>
    public const long InitialTransactionStateVersion = 1;

    /// <summary>
    /// The system phase a paused transaction's domain event is stamped with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Phase 1 of doc 10 § System phase ordering - "Admit and normalize commands for the tick" - because a
    /// paused transaction is the between-ticks form of exactly that act and <c>CMP-SIM-002</c> is the phase-1
    /// owner. <c>EventProvenance</c> requires a phase in 1..14, so a transaction cannot be stamped "no
    /// phase"; naming the admitting phase is truer than borrowing phase 11, which belongs to the run-local
    /// transactions "caused by gameplay".
    /// </para>
    /// <para>
    /// <b>The phase is provenance now, not an ordering key, so this choice is cheap to revisit.</b>
    /// <c>EventOrdering</c> sorts an event batch by tick and emission sequence and by nothing further: the
    /// sequence is per-tick global, so the phase cannot discriminate any pair and is not consulted by the
    /// comparison at all. Choosing 1 over 11 therefore changes what the event <em>says about itself</em> and
    /// changes no observable order, which is worth stating because the argument above reads like an ordering
    /// argument and is not one. The one thing the phase does have to satisfy is
    /// <c>EventOrdering.AssertPhaseAgreesWithSequenceWithinTick</c>: within a tick the phase must not
    /// decrease as the sequence rises. A transaction opens its own publication, so its emission sequence
    /// starts at zero and its event is the only record in the batch that check runs over, which any single
    /// phase value satisfies. Revisiting the choice costs a reading of the event's provenance, not a
    /// regression in ordering.
    /// </para>
    /// </remarks>
    public const int TransactionCommitSystemPhase = 1;

    private static readonly int RejectionReasonCount = Enum.GetValues<CommandRejectionReason>().Length;

    private static readonly int TransactionRejectionReasonCount =
        Enum.GetValues<TransactionRejectionReason>().Length;

    private readonly ulong _runSession;
    private readonly List<long> _openTickSequences = new();
    private readonly List<MovementIntent> _openTickIntents = new();
    private readonly Dictionary<long, long> _admittedTickBySequence = new();
    private readonly long[] _rejectionCounts;
    private readonly long[] _transactionRejectionCounts;

    private readonly Dictionary<string, TransactionAction> _transactionActions =
        new(StringComparer.Ordinal);

    private readonly Dictionary<long, PausedTransactionResult> _appliedByClientCommandSequence = new();

    private bool _isAdmissionOpen;
    private SimulationTick _openTick;
    private long _lastFrozenTickIndex = -1;
    private long _highestAdmittedSequence = CommandEnvelope.FirstSequence - 1;
    private AdmittedCommandSet _frozenSet;
    private long _admittedInRun;
    private long _rejectedInRun;
    private long _transactionStateVersion = InitialTransactionStateVersion;
    private long _appliedTransactionCount;

    /// <summary>Creates a gate fenced to one run session.</summary>
    /// <param name="runSession">The run session. Must not be zero.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="runSession"/> is zero.</exception>
    public CommandAdmissionGate(ulong runSession)
    {
        if (runSession == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runSession),
                runSession,
                "run session zero is reserved to mean 'no run'");
        }

        _runSession = runSession;
        _rejectionCounts = new long[RejectionReasonCount];
        _transactionRejectionCounts = new long[TransactionRejectionReasonCount];
    }

    /// <summary>The run session every envelope and request is fenced to.</summary>
    public ulong RunSession => _runSession;

    /// <summary>Whether an admission window is open.</summary>
    public bool IsAdmissionOpen => _isAdmissionOpen;

    /// <summary>The tick the open admission window belongs to.</summary>
    /// <remarks>Meaningful only while <see cref="IsAdmissionOpen"/>.</remarks>
    public SimulationTick OpenTick => _openTick;

    /// <summary>The index of the most recently frozen tick, or <c>-1</c> before the first freeze.</summary>
    public long LastFrozenTickIndex => _lastFrozenTickIndex;

    /// <summary>
    /// The highest sequence ever admitted in this run, or one below
    /// <see cref="CommandEnvelope.FirstSequence"/> before the first admission.
    /// </summary>
    /// <remarks>
    /// The monotonic high-water mark of doc 10 § Commands and mutations. It never decreases, and no envelope
    /// at or below it is admissible, so it is the first of the two structures that make a second application
    /// of one command unreachable.
    /// </remarks>
    public long HighestAdmittedSequence => _highestAdmittedSequence;

    /// <summary>How many commands the open admission window has admitted so far.</summary>
    public int OpenTickAdmittedCount => _openTickSequences.Count;

    /// <summary>The most recently frozen tick's immutable admitted set.</summary>
    /// <remarks>
    /// A value, so reading it copies it. A caller holding the set for tick <c>N</c> keeps exactly that set
    /// however many ticks are frozen afterwards, which is what <c>VER-SIM-004-006</c> asserts.
    /// </remarks>
    public AdmittedCommandSet FrozenSet => _frozenSet;

    /// <summary>How many commands have been admitted in this run.</summary>
    public long AdmittedInRun => _admittedInRun;

    /// <summary>How many envelopes have been refused in this run.</summary>
    public long RejectedInRun => _rejectedInRun;

    /// <summary>How many entries the never-evicted idempotency history holds.</summary>
    public int IdempotencyHistoryCount => _admittedTickBySequence.Count;

    /// <summary>The authoritative state version paused transactions are validated against.</summary>
    /// <remarks>
    /// <para>
    /// Owned by <c>CMP-SIM-002</c> rather than by a domain component because doc 115 § Component registry
    /// gives <c>CMP-SIM-002</c> the timing "atomically between ticks": whoever advances the version has to do
    /// so inside the same indivisible step that applies the transaction, and this is that step. The general
    /// form of the rule is that whatever makes a step indivisible must own the counter that marks the step
    /// happened, because a counter advanced by anything outside that step can disagree with it. The domain
    /// components own the state's <em>content</em>; a second version counter beside this one would be the
    /// two-writer arrangement doc 115 forbids.
    /// </para>
    /// <para>
    /// <b>This is not <c>CMP-PRG-001</c>'s "loadout versions", and the two must not be merged.</b> That is
    /// the nearest-looking counter in doc 115 § Component registry and the place a progression owner would
    /// naturally reach when a paused install needs a version, so the ruling is recorded here rather than left
    /// to be rediscovered. They count different things. This counter marks that one indivisible
    /// between-ticks step occurred, whatever the step touched, which is why an optimistic-concurrency check
    /// against it is meaningful: a caller that captured a view at version <c>N</c> and submits against
    /// <c>N</c> is asserting that nothing at all has been committed since. <c>CMP-PRG-001</c>'s loadout
    /// versions version the loadout's <em>content</em>, and a loadout can be reshaped by things that are not
    /// paused transactions at all. A progression component that needs this counter reads it here; it does not
    /// mint its own and does not repurpose its loadout version to stand in for it, because either would give
    /// the indivisibility marker a second writer.
    /// </para>
    /// </remarks>
    public long TransactionStateVersion => _transactionStateVersion;

    /// <summary>How many paused transactions have been applied in this run.</summary>
    public long AppliedTransactionCount => _appliedTransactionCount;

    /// <summary>How many transaction actions are registered.</summary>
    public int RegisteredTransactionActionCount => _transactionActions.Count;

    /// <summary>How many envelopes were refused for <paramref name="reason"/>.</summary>
    /// <param name="reason">The reason to count.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is not a defined member.</exception>
    public long RejectionCount(CommandRejectionReason reason)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "undefined rejection reason");
        }

        return _rejectionCounts[(int)reason];
    }

    /// <summary>How many transaction submissions were answered with <paramref name="reason"/>.</summary>
    /// <param name="reason">The reason to count.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="reason"/> is not a defined member.</exception>
    public long TransactionRejectionCount(TransactionRejectionReason reason)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "undefined rejection reason");
        }

        return _transactionRejectionCounts[(int)reason];
    }

    /// <summary>Opens the admission window for one tick, which is phase 1 of that tick.</summary>
    /// <param name="tick">The tick to admit for.</param>
    /// <exception cref="InvalidOperationException">
    /// A window is still open, or <paramref name="tick"/> has already been frozen.
    /// </exception>
    /// <remarks>
    /// Refusing to reopen a frozen tick is what makes the freeze final. doc 10 § System phase ordering runs
    /// admission once per tick, so a second window for the same tick would be a second phase 1.
    /// </remarks>
    public void BeginTick(SimulationTick tick)
    {
        if (_isAdmissionOpen)
        {
            throw new InvalidOperationException(
                "the admission window for tick "
                    + _openTick.ToString()
                    + " is still open; freeze it before opening another. doc 10 § System phase ordering "
                    + "admits once per tick, in phase 1");
        }

        if (tick.Index <= _lastFrozenTickIndex)
        {
            throw new InvalidOperationException(
                "tick "
                    + tick.ToString()
                    + " was already frozen (last frozen: "
                    + _lastFrozenTickIndex.ToString(CultureInfo.InvariantCulture)
                    + "); a frozen tick's admitted set is final, so admission cannot reopen for it");
        }

        _openTick = tick;
        _isAdmissionOpen = true;
        _openTickSequences.Clear();
        _openTickIntents.Clear();
    }

    /// <summary>
    /// Admits one envelope for the open tick, or returns the typed reason it was refused.
    /// </summary>
    /// <param name="envelope">The envelope as received.</param>
    /// <param name="rejection">
    /// The typed refusal, or <see cref="CommandRejection.None"/> when the envelope was admitted.
    /// </param>
    /// <returns><see langword="true"/> when the envelope was admitted.</returns>
    /// <remarks>
    /// <para>
    /// <b>The order of the checks is the contract, not a convenience.</b>
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// The run fence first, so <c>VER-SIM-004-004</c>'s "rejected on identity alone" holds even for an
    /// envelope that is also closed, out of order, and unnormalizable. The payload is unreachable without
    /// the fence in any case - see <see cref="CommandEnvelope.TryNormalizePayload"/> - so this ordering
    /// decides which reason is reported, not whether the payload was touched.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The idempotency history second, so a resubmission of an already-admitted envelope is
    /// <see cref="CommandRejectionReason.Duplicate"/> however many ticks later it arrives - which is exactly
    /// what <c>VER-SIM-004-001</c> requires, and it is why the history is consulted before the staleness of
    /// the tick it names.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The monotonic high-water mark third: a fresh envelope whose sequence has been overtaken is a
    /// <see cref="CommandRejectionReason.SequenceRegression"/>, and doc 10 § Commands and mutations'
    /// monotonic sequence is what makes gaps admissible while regressions are not.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Then the tick window: a target tick already frozen or passed is
    /// <see cref="CommandRejectionReason.Stale"/>; any other tick than the open one is
    /// <see cref="CommandRejectionReason.AdmissionClosed"/>, whose detail distinguishes the three
    /// distinct mistakes that reach it (see <see cref="BuildAdmissionClosedDetail"/>).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Normalization last, because it is the only check that inspects the payload, and
    /// <see cref="CommandRejectionReason.InvalidPayload"/> must not pre-empt any identity or ordering
    /// refusal.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// Everything after those checks is the single admission tail. There is no other method that appends to
    /// the tick's admitted commands, advances the high-water mark, or writes the history, so "applied at most
    /// once" does not depend on callers using the right entry point.
    /// </para>
    /// </remarks>
    public bool TryAdmit(in CommandEnvelope envelope, out CommandRejection rejection)
    {
        if (!envelope.BelongsTo(_runSession))
        {
            return Reject(
                CommandRejectionReason.ForeignRunSession,
                envelope,
                "the envelope names run "
                    + envelope.RunSession.ToString("X16", CultureInfo.InvariantCulture)
                    + " but this gate speaks for run "
                    + _runSession.ToString("X16", CultureInfo.InvariantCulture),
                out rejection);
        }

        if (_admittedTickBySequence.TryGetValue(envelope.Sequence, out long alreadyAdmittedTick))
        {
            if (alreadyAdmittedTick == envelope.TargetTick.Index)
            {
                return Reject(
                    CommandRejectionReason.Duplicate,
                    envelope,
                    "this exact envelope identity was already admitted for tick "
                        + alreadyAdmittedTick.ToString(CultureInfo.InvariantCulture)
                        + "; doc 10 § Commands and mutations applies a command at most once",
                    out rejection);
            }

            return Reject(
                CommandRejectionReason.SequenceRegression,
                envelope,
                "sequence "
                    + envelope.Sequence.ToString(CultureInfo.InvariantCulture)
                    + " was already spent on tick "
                    + alreadyAdmittedTick.ToString(CultureInfo.InvariantCulture)
                    + ", so reusing it for tick "
                    + envelope.TargetTick.ToString()
                    + " would make the run's command sequence ambiguous",
                out rejection);
        }

        if (envelope.Sequence <= _highestAdmittedSequence)
        {
            return Reject(
                CommandRejectionReason.SequenceRegression,
                envelope,
                "sequence "
                    + envelope.Sequence.ToString(CultureInfo.InvariantCulture)
                    + " is at or below the highest already admitted, "
                    + _highestAdmittedSequence.ToString(CultureInfo.InvariantCulture)
                    + "; doc 10 § Commands and mutations requires a monotonic command sequence",
                out rejection);
        }

        if (envelope.TargetTick.Index <= _lastFrozenTickIndex)
        {
            return Reject(
                CommandRejectionReason.Stale,
                envelope,
                "tick "
                    + envelope.TargetTick.ToString()
                    + " was frozen at or before tick "
                    + _lastFrozenTickIndex.ToString(CultureInfo.InvariantCulture)
                    + ", so its admitted set is final",
                out rejection);
        }

        if (!_isAdmissionOpen || envelope.TargetTick != _openTick)
        {
            return Reject(
                CommandRejectionReason.AdmissionClosed,
                envelope,
                BuildAdmissionClosedDetail(envelope),
                out rejection);
        }

        if (!envelope.TryNormalizePayload(_runSession, out MovementIntent intent))
        {
            return Reject(
                CommandRejectionReason.InvalidPayload,
                envelope,
                "the raw movement payload has no normalized value; doc 20 § Active commands normalizes to a "
                    + "planar vector with magnitude [0,1] and neither NaN nor infinity has one",
                out rejection);
        }

        // The single admission path. Every statement above is a refusal, so reaching this point means the
        // idempotency history and the monotonic high-water mark have both been consulted and both admitted
        // this envelope. There is no other way into these five writes.
        //
        // The history write goes first, and the order matters. Its Add throws on a duplicate key, so it is the
        // only one of the five that can fail; the other four are unconditional. Written first, a throw leaves
        // every other datum exactly as it was, which is the shape docs/technical/20-simulation-core.md
        // § Tick transaction asks for - an invariant failure before anything has moved. Written third, as it
        // once was, a throw would leave the open tick's two working lists already carrying the envelope. That
        // was never observable here, because TryAdmit publishes nothing and the two checks above make the
        // duplicate unreachable, so it is not the § Tick transaction violation the paused-transaction commit
        // had; the ordering is simply free, and none of the four other writes reads the history.
        _admittedTickBySequence.Add(envelope.Sequence, envelope.TargetTick.Index);
        _openTickSequences.Add(envelope.Sequence);
        _openTickIntents.Add(intent);
        _highestAdmittedSequence = envelope.Sequence;
        _admittedInRun++;

        rejection = CommandRejection.None;
        return true;
    }

    /// <summary>Closes the open admission window and returns the tick's immutable admitted set.</summary>
    /// <returns>The frozen set, which is also retained as <see cref="FrozenSet"/>.</returns>
    /// <exception cref="InvalidOperationException">No admission window is open.</exception>
    /// <remarks>
    /// The set is copied out of the working lists into immutable storage, so the next tick's
    /// <see cref="BeginTick"/> clearing those lists cannot reach a set already handed out. That copy is what
    /// makes <c>VER-SIM-004-006</c>'s "no later phase can alter or append to that tick's admitted set" a
    /// property of the data rather than of a rule phases 2 to 14 have to obey.
    /// </remarks>
    public AdmittedCommandSet FreezeTick()
    {
        if (!_isAdmissionOpen)
        {
            throw new InvalidOperationException(
                "no admission window is open, so there is nothing to freeze");
        }

        _frozenSet = AdmittedCommandSet.Freeze(
            _runSession,
            _openTick,
            _openTickSequences,
            _openTickIntents);
        _lastFrozenTickIndex = _openTick.Index;
        _isAdmissionOpen = false;
        return _frozenSet;
    }

    /// <summary>Registers one paused-transaction action and the domain rule that may refuse it.</summary>
    /// <param name="actionId">The action's stable content ID. Must not be blank.</param>
    /// <param name="appliedEventKind">The declared domain event kind an application emits.</param>
    /// <param name="requiresConfirmation">
    /// Whether the action is irreversible and therefore requires a confirmation token.
    /// </param>
    /// <param name="domainValidator">
    /// The owning domain component's rule. Returning <see langword="false"/> becomes
    /// <see cref="TransactionRejectionReason.DomainRefused"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="domainValidator"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="actionId"/> is blank, <paramref name="appliedEventKind"/> is undeclared, or the action
    /// is already registered.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The seam by which the packages that own fabrication, relics, and PowerUps supply the domain
    /// validations doc 20 § Paused transactions lists - ownership, availability, slot capacity, duplication,
    /// cost, prerequisites, branch exclusivity, integer overflow - without this shell guessing at them. It is
    /// a decision only: the validator does not mutate and does not emit, so it cannot leave a partial change
    /// behind if it refuses. That is why <see cref="TransactionRejectionReason.DomainRefused"/> can be the
    /// last check before the commit block and still be a rejection with no mutation.
    /// </para>
    /// <para>
    /// A duplicate registration throws rather than replacing, on the same reasoning as
    /// <c>CTR-CNT-002</c>'s "one implementation per kind": two rules for one action identity would make the
    /// applied outcome depend on registration order.
    /// </para>
    /// </remarks>
    public void RegisterTransactionAction(
        string actionId,
        EventKind appliedEventKind,
        bool requiresConfirmation,
        Func<PausedTransactionRequest, bool> domainValidator)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        ArgumentNullException.ThrowIfNull(domainValidator);
        if (!appliedEventKind.IsDeclared)
        {
            throw new ArgumentException(
                "the applied event kind must be declared through EventKind.Declare",
                nameof(appliedEventKind));
        }

        if (_transactionActions.ContainsKey(actionId))
        {
            throw new ArgumentException(
                "action '" + actionId + "' is already registered; one rule per action identity",
                nameof(actionId));
        }

        _transactionActions.Add(
            actionId,
            new TransactionAction(appliedEventKind, requiresConfirmation, domainValidator));
    }

    /// <summary>Whether an action with <paramref name="actionId"/> is registered.</summary>
    /// <param name="actionId">The action's stable content ID.</param>
    /// <exception cref="ArgumentException"><paramref name="actionId"/> is blank.</exception>
    public bool IsTransactionActionRegistered(string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        return _transactionActions.ContainsKey(actionId);
    }

    /// <summary>
    /// Applies one paused transaction atomically between ticks, publishing its replacement snapshot before
    /// returning, or refuses it with a typed reason and changes nothing.
    /// </summary>
    /// <param name="request">The request as received.</param>
    /// <param name="blockingReasons">
    /// The run's blocking reasons. Must be non-empty: doc 20 § Paused transactions commits "outside active
    /// ticks".
    /// </param>
    /// <param name="stageReplacementState">
    /// Stages the post-transaction authoritative state onto the publisher. Invoked on the accept path only.
    /// </param>
    /// <param name="publisher">The run's <c>CMP-SIM-003</c> publisher. Must be fenced to the same run.</param>
    /// <param name="domainEvents">The domain event buffer the applied fact is appended to.</param>
    /// <param name="presentationEvents">The presentation buffer the publication needs.</param>
    /// <param name="coalescingPolicy">The explicit presentation coalescing policy.</param>
    /// <returns>The typed result: applied, replayed, or refused.</returns>
    /// <exception cref="ArgumentNullException">A reference argument is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> was defaulted, or <paramref name="publisher"/> belongs to another run.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The run is not blocked, or a tick's admission window is open - either way a tick could be in flight,
    /// and a transaction commits only between ticks.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>"No tick executes while the transaction is in flight" is structural three times over.</b> This
    /// method refuses to run unless the run is blocked; it refuses to run while an admission window is open;
    /// and the publication it performs calls <c>SnapshotPublisher.BeginTick</c>, which itself throws if a
    /// tick is already open. A caller cannot interleave a tick with a transaction because there is no point
    /// during this call at which control returns to it - the staging callback is the only outward call, and
    /// it is handed a publisher whose tick is already open for the replacement publication.
    /// </para>
    /// <para>
    /// <b>"Before resumption" needs no ordering discipline either.</b> The pause reasons are the caller's to
    /// clear, and it cannot clear them until this method returns - by which time the replacement snapshot has
    /// already been published and its version is in the result. doc 10 § Pause contract's "publishes a
    /// replacement snapshot before resumption" is therefore not a sequence to preserve but the only sequence
    /// available.
    /// </para>
    /// <para>
    /// <b>Order of the checks.</b> Identity, then idempotency, then registration and confirmation, then the
    /// expected state version, then the domain rule. Idempotency precedes the version check because a replay
    /// of an applied transaction necessarily carries a now-stale expected version, and
    /// <c>VER-SIM-004-009</c> requires the replay to observe the applied result rather than to be told its
    /// view is old.
    /// </para>
    /// <para>
    /// <b>What "all-or-nothing" means when the commit block itself throws.</b> Every refusal happens before
    /// the first mutation, so a refused transaction changes nothing at all. If the staging callback or the
    /// publication throws part way through the commit, doc 20 § Tick transaction already fixes the answer: the
    /// transaction "never publishes a partial state" and the run ends through the safe technical-failure path.
    /// The state version is advanced only after the publication has succeeded, so a failure mid-commit leaves
    /// the authoritative version where it was.
    /// </para>
    /// <para>
    /// <b>The commit's precondition is checked here, not at the write that records the result.</b> A second
    /// application of one idempotency key would be the one failure doc 20 § Tick transaction forbids outright,
    /// because the commit publishes before it records, so a duplicate detected at the recording write would be
    /// detected after a partial state had been published. The check therefore sits between the last validation
    /// and the call to <see cref="CommitApplied"/>, where nothing has moved yet, and it is an invariant failure
    /// rather than a rejection reason: the idempotency check above answers every submission a caller can
    /// actually make, so reaching this point with the key present means the history was consulted and then
    /// contradicted, which is a defect in this type and not input a player supplied.
    /// </para>
    /// </remarks>
    public PausedTransactionResult Apply(
        in PausedTransactionRequest request,
        PauseReasonSet blockingReasons,
        Action<SnapshotPublisher> stageReplacementState,
        SnapshotPublisher publisher,
        DomainEventBuffer domainEvents,
        PresentationEventBuffer presentationEvents,
        PresentationCoalescingPolicy coalescingPolicy)
    {
        ArgumentNullException.ThrowIfNull(stageReplacementState);
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(domainEvents);
        ArgumentNullException.ThrowIfNull(presentationEvents);
        ArgumentNullException.ThrowIfNull(coalescingPolicy);

        if (!request.IsPresent)
        {
            throw new ArgumentException(
                "a defaulted request names no run and no action; use PausedTransactionRequest.Create",
                nameof(request));
        }

        if (!blockingReasons.IsBlocking)
        {
            throw new InvalidOperationException(
                "the run is not blocked, so a tick may be in flight. doc 20 § Paused transactions uses "
                    + "\"typed transactions outside active ticks\" and doc 10 § Pause contract commits "
                    + "\"atomically between ticks\"");
        }

        if (_isAdmissionOpen)
        {
            throw new InvalidOperationException(
                "the admission window for tick "
                    + _openTick.ToString()
                    + " is open, so that tick has not finished; a transaction commits between ticks");
        }

        if (publisher.RunSession != _runSession)
        {
            throw new ArgumentException(
                "the publisher is fenced to run "
                    + publisher.RunSession.ToString("X16", CultureInfo.InvariantCulture)
                    + " but this gate speaks for run "
                    + _runSession.ToString("X16", CultureInfo.InvariantCulture),
                nameof(publisher));
        }

        // ---- validation: every path below returns without having changed anything ----
        if (request.RunSession != _runSession)
        {
            return RejectTransaction(
                TransactionRejectionReason.ForeignRunSession,
                request,
                "the request names run "
                    + request.RunSession.ToString("X16", CultureInfo.InvariantCulture)
                    + " but this gate speaks for run "
                    + _runSession.ToString("X16", CultureInfo.InvariantCulture));
        }

        if (_appliedByClientCommandSequence.TryGetValue(
            request.ClientCommandSequence,
            out PausedTransactionResult applied))
        {
            _transactionRejectionCounts[(int)TransactionRejectionReason.AlreadyApplied]++;
            return PausedTransactionResult.Replayed(applied);
        }

        if (!_transactionActions.TryGetValue(request.ActionId, out TransactionAction action))
        {
            return RejectTransaction(
                TransactionRejectionReason.UnknownAction,
                request,
                "no transaction action '" + request.ActionId + "' is registered");
        }

        if (action.RequiresConfirmation && !request.HasConfirmationToken)
        {
            return RejectTransaction(
                TransactionRejectionReason.ConfirmationRequired,
                request,
                "action '"
                    + request.ActionId
                    + "' is irreversible and doc 20 § Paused transactions requires its confirmation token");
        }

        if (request.ExpectedStateVersion != _transactionStateVersion)
        {
            return RejectTransaction(
                TransactionRejectionReason.StaleExpectedStateVersion,
                request,
                "the request expects state version "
                    + request.ExpectedStateVersion.ToString(CultureInfo.InvariantCulture)
                    + " but the authoritative version is "
                    + _transactionStateVersion.ToString(CultureInfo.InvariantCulture)
                    + "; doc 10 § Pause contract: a stale transaction changes nothing");
        }

        if (!action.DomainValidator(request))
        {
            return RejectTransaction(
                TransactionRejectionReason.DomainRefused,
                request,
                "the owning domain component refused action '"
                    + request.ActionId
                    + "' for selection '"
                    + request.SelectionId
                    + "'");
        }

        // ---- the commit's precondition, established before control can reach any mutation ----
        if (_appliedByClientCommandSequence.ContainsKey(request.ClientCommandSequence))
        {
            throw new InvalidOperationException(
                "client command sequence "
                    + request.ClientCommandSequence.ToString(CultureInfo.InvariantCulture)
                    + " is already in the applied-transaction history, so the idempotency check above should "
                    + "have answered this submission with the applied result. Refusing here, before the commit "
                    + "has moved anything: doc 20 § Tick transaction ends the run through the safe "
                    + "technical-failure path on an invariant failure before commit, and never publishes a "
                    + "partial state");
        }

        return CommitApplied(
            request,
            action,
            stageReplacementState,
            publisher,
            domainEvents,
            presentationEvents,
            coalescingPolicy);
    }

    /// <summary>
    /// The whole of the mutating commit for one accepted paused transaction: it publishes the replacement
    /// snapshot, advances the authoritative state version, and records the applied result in the idempotency
    /// history.
    /// </summary>
    /// <param name="request">The request, already validated by <see cref="Apply"/>.</param>
    /// <param name="action">The registered action <paramref name="request"/> names.</param>
    /// <param name="stageReplacementState">Stages the post-transaction authoritative state.</param>
    /// <param name="publisher">The run's <c>CMP-SIM-003</c> publisher.</param>
    /// <param name="domainEvents">The domain event buffer the applied fact is appended to.</param>
    /// <param name="presentationEvents">The presentation buffer the publication needs.</param>
    /// <param name="coalescingPolicy">The explicit presentation coalescing policy.</param>
    /// <returns>The accepted result, which is also recorded in the idempotency history.</returns>
    /// <remarks>
    /// <para>
    /// <b>Why this is a method and not the tail of <see cref="Apply"/>.</b> Every mutation a commit performs is
    /// inside this body, and this body has exactly one call site, which <see cref="Apply"/> places after the
    /// duplicate-key precondition. There is therefore no statement position anywhere in the commit that is
    /// ahead of that precondition: the ordering is a scope boundary crossed by a call rather than a convention
    /// about which adjacent statements come first, so a mutation cannot be added ahead of the check without
    /// moving code out of this method. That is the difference between a second application being caught and its
    /// being unrepresentable, and it is worth the boundary: when the precondition was only the
    /// <c>Dictionary.Add</c> at the end of this body, a duplicate was detected after the event had been
    /// appended, the snapshot published, and the version advanced.
    /// </para>
    /// <para>
    /// It is not a rollback and does not claim to be one. <c>docs/technical/20-simulation-core.md</c> § Tick
    /// transaction already fixes the answer for a failure raised inside the commit rather than before it: the
    /// run ends through the safe technical-failure path. What this shape guarantees is the antecedent - that a
    /// duplicate is never the thing that fails inside the commit, because it cannot get in.
    /// </para>
    /// </remarks>
    private PausedTransactionResult CommitApplied(
        in PausedTransactionRequest request,
        TransactionAction action,
        Action<SnapshotPublisher> stageReplacementState,
        SnapshotPublisher publisher,
        DomainEventBuffer domainEvents,
        PresentationEventBuffer presentationEvents,
        PresentationCoalescingPolicy coalescingPolicy)
    {
        long pausedAtTick = publisher.Latest?.Tick ?? 0L;
        long newStateVersion = checked(_transactionStateVersion + 1);

        publisher.BeginTick(pausedAtTick);
        stageReplacementState(publisher);
        domainEvents.BeginTick(pausedAtTick);
        presentationEvents.BeginTick(pausedAtTick);

        DomainEvent appliedEvent = DomainEvent.Create(
            action.AppliedEventKind,
            EventProvenance.Create(
                pausedAtTick,
                TransactionCommitSystemPhase,
                publisher.NextEventSequence(),
                EntityId.NoEntityIn(_runSession),
                request.ActionId),
            EntityId.NoEntityIn(_runSession),
            positionX: 0.0,
            positionY: 0.0,
            EventPayload.Typed(
                EventPayload.InitialSchemaVersion,
                request.SelectionOrdinal,
                newStateVersion,
                request.SelectionId));
        domainEvents.Append(appliedEvent);

        TickPublication publication = publisher.Publish(
            domainEvents,
            presentationEvents,
            coalescingPolicy);
        publisher.ReleaseTick(domainEvents, presentationEvents);

        _transactionStateVersion = newStateVersion;
        _appliedTransactionCount++;

        PausedTransactionResult result = PausedTransactionResult.Accepted(
            request,
            newStateVersion,
            appliedEvent,
            publication.DomainEventCount,
            publication.Version,
            "action '"
                + request.ActionId
                + "' applied at state version "
                + newStateVersion.ToString(CultureInfo.InvariantCulture)
                + " and published snapshot "
                + publication.Version.ToString());
        // Add rather than an indexer, so this history can never be silently rewritten. The precondition in
        // Apply is what refuses a duplicate; this write is the last-resort invariant behind it, and reaching it
        // with the key present is already the double-apply rather than a defence against one.
        _appliedByClientCommandSequence.Add(request.ClientCommandSequence, result);
        return result;
    }

    /// <summary>
    /// Renders every piece of state a rejection must leave untouched, as canonical invariant text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The comparison target for <c>VER-SIM-004-002</c>, <c>VER-SIM-004-007</c>, and
    /// <c>VER-SIM-004-008</c>: a whole-state rendering, taken before and after a refusal, is byte-identical
    /// or the refusal changed something. Rendering everything rather than asserting on the fields a test
    /// author thought of is the point - a mutation nobody predicted still shows up as a text difference.
    /// </para>
    /// <para>
    /// The rejection counters are deliberately excluded, and appear in <see cref="Render"/> instead: a
    /// refusal <em>must</em> increment them, so including them here would make the assertion impossible to
    /// state. They are diagnostics rather than authoritative state - doc 90 § Frame metrics, not doc 20.
    /// </para>
    /// <para>
    /// Both dictionaries are walked in sorted key order, because a hash iteration order would make this text
    /// unstable for reasons that have nothing to do with the state it describes.
    /// </para>
    /// </remarks>
    public string RenderAuthoritative()
    {
        StringBuilder builder = new();
        builder
            .Append("gate run=")
            .Append(_runSession.ToString("X16", CultureInfo.InvariantCulture))
            .Append(" open=")
            .Append(_isAdmissionOpen ? _openTick.ToString() : "none")
            .Append(" lastFrozen=")
            .Append(_lastFrozenTickIndex.ToString(CultureInfo.InvariantCulture))
            .Append(" highestSeq=")
            .Append(_highestAdmittedSequence.ToString(CultureInfo.InvariantCulture))
            .Append(" admitted=")
            .Append(_admittedInRun.ToString(CultureInfo.InvariantCulture))
            .Append(" stateVersion=")
            .Append(_transactionStateVersion.ToString(CultureInfo.InvariantCulture))
            .Append(" appliedTransactions=")
            .Append(_appliedTransactionCount.ToString(CultureInfo.InvariantCulture))
            .Append('\n');

        builder.Append("open-tick");
        for (int index = 0; index < _openTickSequences.Count; index++)
        {
            builder
                .Append("\n  ")
                .Append(_openTickSequences[index].ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(_openTickIntents[index].ToString());
        }

        builder.Append('\n').Append(_frozenSet.Render()).Append('\n');

        builder.Append("history");
        foreach (long sequence in SortedKeys(_admittedTickBySequence.Keys))
        {
            builder
                .Append("\n  ")
                .Append(sequence.ToString(CultureInfo.InvariantCulture))
                .Append("->")
                .Append(_admittedTickBySequence[sequence].ToString(CultureInfo.InvariantCulture));
        }

        builder.Append('\n').Append("transactions");
        foreach (long clientSequence in SortedKeys(_appliedByClientCommandSequence.Keys))
        {
            builder
                .Append("\n  ")
                .Append(_appliedByClientCommandSequence[clientSequence].Render());
        }

        return builder.Append('\n').ToString();
    }

    /// <summary>Renders the authoritative state plus the rejection diagnostics.</summary>
    public string Render()
    {
        StringBuilder builder = new(RenderAuthoritative());
        builder.Append("rejected=").Append(_rejectedInRun.ToString(CultureInfo.InvariantCulture));
        foreach (CommandRejectionReason reason in Enum.GetValues<CommandRejectionReason>())
        {
            builder
                .Append("\n  ")
                .Append(reason.ToString())
                .Append('=')
                .Append(_rejectionCounts[(int)reason].ToString(CultureInfo.InvariantCulture));
        }

        builder.Append('\n').Append("transaction-rejections");
        foreach (TransactionRejectionReason reason in Enum.GetValues<TransactionRejectionReason>())
        {
            builder
                .Append("\n  ")
                .Append(reason.ToString())
                .Append('=')
                .Append(_transactionRejectionCounts[(int)reason].ToString(CultureInfo.InvariantCulture));
        }

        return builder.Append('\n').ToString();
    }

    private static List<long> SortedKeys(Dictionary<long, long>.KeyCollection keys)
    {
        List<long> sorted = new(keys);
        sorted.Sort();
        return sorted;
    }

    private static List<long> SortedKeys(Dictionary<long, PausedTransactionResult>.KeyCollection keys)
    {
        List<long> sorted = new(keys);
        sorted.Sort();
        return sorted;
    }

    /// <summary>
    /// Builds the detail for an <see cref="CommandRejectionReason.AdmissionClosed"/> refusal, saying which
    /// of the three ways the window was missed.
    /// </summary>
    /// <param name="envelope">The refused envelope.</param>
    /// <remarks>
    /// <para>
    /// One reason code, three messages. The outcome is the same and the reason code stays one value, because
    /// a caller branching on the code is deciding whether to resubmit and the answer is the same in all
    /// three. But the <em>mistakes</em> are different, and the detail is what a human reads when working out
    /// why a command vanished, so collapsing them into "the window is for tick X, not tick Y" would throw
    /// away the only part of the refusal that distinguishes them.
    /// </para>
    /// <para>
    /// Ahead of the window is a caller running early: it built a command for a tick the run has not reached,
    /// so a window for that tick will open later and the same envelope will be admissible then. Behind the
    /// window is a different fault: doc 10 § System phase ordering opens a window in phase 1 of every tick,
    /// and a tick before the open one was either never opened or was closed without freezing, so no window
    /// for it will ever exist and the envelope has to be rebuilt against a reachable tick. The stale check
    /// above already claimed the frozen ticks, so this branch is specifically the unfrozen past, which is
    /// the shape a gap in the tick sequence produces.
    /// </para>
    /// </remarks>
    private string BuildAdmissionClosedDetail(in CommandEnvelope envelope)
    {
        if (!_isAdmissionOpen)
        {
            return "no admission window is open; doc 10 § System phase ordering admits in phase 1 of a tick";
        }

        if (envelope.TargetTick.Index > _openTick.Index)
        {
            return "tick "
                + envelope.TargetTick.ToString()
                + " is ahead of the open admission window at tick "
                + _openTick.ToString()
                + "; a window opens in phase 1 of the tick it admits for, so this envelope is early rather "
                + "than wrong and the same identity is admissible once that tick opens";
        }

        return "tick "
            + envelope.TargetTick.ToString()
            + " is behind the open admission window at tick "
            + _openTick.ToString()
            + " and was never frozen, so no window for it was ever opened and none ever will be; the "
            + "envelope has to be rebuilt against a tick the run has not passed";
    }

    private bool Reject(
        CommandRejectionReason reason,
        in CommandEnvelope envelope,
        string detail,
        out CommandRejection rejection)
    {
        rejection = CommandRejection.Of(reason, envelope, detail);
        _rejectedInRun++;
        _rejectionCounts[(int)reason]++;
        return false;
    }

    private PausedTransactionResult RejectTransaction(
        TransactionRejectionReason reason,
        in PausedTransactionRequest request,
        string detail)
    {
        _transactionRejectionCounts[(int)reason]++;
        return PausedTransactionResult.Rejected(reason, request, _transactionStateVersion, detail);
    }

    /// <summary>One registered transaction action: its event kind, its confirmation rule, and its domain rule.</summary>
    /// <remarks>
    /// A private readonly struct rather than three parallel dictionaries, so an action's three facts cannot
    /// get out of step with each other.
    /// </remarks>
    private readonly struct TransactionAction
    {
        internal TransactionAction(
            EventKind appliedEventKind,
            bool requiresConfirmation,
            Func<PausedTransactionRequest, bool> domainValidator)
        {
            AppliedEventKind = appliedEventKind;
            RequiresConfirmation = requiresConfirmation;
            DomainValidator = domainValidator;
        }

        /// <summary>The declared domain event kind an application of this action emits.</summary>
        internal EventKind AppliedEventKind { get; }

        /// <summary>Whether this action is irreversible and therefore requires a confirmation token.</summary>
        internal bool RequiresConfirmation { get; }

        /// <summary>The owning domain component's decision rule.</summary>
        internal Func<PausedTransactionRequest, bool> DomainValidator { get; }
    }
}
