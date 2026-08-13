namespace MechaMiner.Simulation.Commands;

/// <summary>
/// Why an active command envelope was refused admission.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/115-component-contract-and-schema-registry.md</c> § Cross-boundary contract
/// registry, <c>CTR-RUN-002</c>: "stale/duplicate/invalid commands return typed rejection/no change".
/// <c>docs/technical/10-runtime-architecture.md</c> § Commands and mutations: "A command is applied at
/// most once. Commands that can cross an asynchronous boundary carry a run-session identity and
/// monotonic command sequence."
/// </para>
/// <para>
/// <b>Separate from <see cref="TransactionRejectionReason"/> on purpose.</b> <c>CTR-RUN-002</c> and
/// <c>CTR-RUN-003</c> are two registered contracts with different failure vocabularies. One shared enum
/// would let a paused transaction answer <see cref="Duplicate"/> where only
/// <see cref="TransactionRejectionReason.AlreadyApplied"/> is meaningful - and
/// <see cref="TransactionRejectionReason.AlreadyApplied"/> is a rejection that still carries an applied
/// result, which <see cref="Duplicate"/> never is.
/// </para>
/// <para>
/// There is deliberately no "none" member. A reason is only ever read from a
/// <see cref="CommandRejection"/> that is a rejection, and that type refuses to hand one out otherwise,
/// so a "no reason" value would be a state nothing can produce.
/// </para>
/// <para>
/// Cross-boundary consumer (doc 115 § Component registry): the input adapter in <c>game/</c> - the
/// producer of <c>CTR-RUN-002</c> - presents the refusal to the player or logs it, and
/// <c>MechaMiner.Game.Tests</c> asserts on it. Hence <c>public</c>.
/// </para>
/// </remarks>
public enum CommandRejectionReason
{
    /// <summary>
    /// The envelope targets a tick that has already been frozen or has already passed, so admitting it
    /// would change a tick that is over.
    /// </summary>
    Stale = 0,

    /// <summary>
    /// This exact envelope identity - run session, target tick, and sequence - was already admitted.
    /// </summary>
    /// <remarks>
    /// The specific diagnosis for doc 10 § Commands and mutations' "A command is applied at most once",
    /// and it is reported in preference to <see cref="SequenceRegression"/> or <see cref="Stale"/>
    /// however late the resubmission arrives.
    /// </remarks>
    Duplicate = 1,

    /// <summary>
    /// The envelope carries another run session's identity, so it belongs to no run this gate speaks
    /// for.
    /// </summary>
    /// <remarks>
    /// Checked before anything else, including before the payload can be normalized at all - see
    /// <see cref="CommandEnvelope.TryNormalizePayload"/>.
    /// </remarks>
    ForeignRunSession = 2,

    /// <summary>
    /// The sequence is at or below the highest already-admitted sequence, or reuses a sequence already
    /// spent on a different target tick.
    /// </summary>
    SequenceRegression = 3,

    /// <summary>The payload cannot be normalized: a movement component is not a finite number.</summary>
    InvalidPayload = 4,

    /// <summary>
    /// No admission window is open for the envelope's target tick: either the tick's window was frozen,
    /// or the window for that later tick has not opened yet.
    /// </summary>
    /// <remarks>
    /// doc 10 § System phase ordering makes admission phase 1 of a tick, so a window exists for exactly
    /// one tick at a time.
    /// </remarks>
    AdmissionClosed = 5,
}
