namespace MechaMiner.Simulation.World;

/// <summary>
/// The fourteen stable tick-phase identifiers. <b>These numbers are contract and are never
/// renumbered.</b>
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/10-runtime-architecture.md</c> § System phase ordering, lines 144-157, and
/// that section's own rule about itself, verbatim: "The numbers in the list below are the stable
/// phase identifiers, not a rendering of list position: command admission is phase 1 and
/// publication is phase 14, and every stored, serialized, logged, or asserted phase value uses
/// exactly these numbers. Phases are never renumbered - not to close a gap, not to make room,
/// and not as a side effect of an editorial rewrite. A new phase takes the next unused number
/// and states where in the order it runs; a subdivision keeps its parent phase's number."
/// </para>
/// <para>
/// This type exists so that a phase value in code is that number rather than a position in a
/// list. An enum whose members were reordered, or a sequence of unnamed statements, would let an
/// editorial change to the world's body silently renumber a contract the same document says
/// cannot be renumbered - the exact failure the doc warns about, since "renumbering an unchanged
/// order is invisible to any test that asserts only relative order while it silently invalidates
/// every fixture that stores a literal phase value".
/// </para>
/// <para>
/// Most of these phases are empty in this slice and are named anyway. A phase that exists as a
/// named, ordered, empty step is a phase the next package fills in the right place; a phase that
/// does not exist yet is one the next package inserts wherever it happens to be working.
/// </para>
/// </remarks>
public static class TickPhase
{
    /// <summary>Phase 1: admit and normalize commands for the tick.</summary>
    /// <remarks>doc 10:144.</remarks>
    public const int AdmitCommands = 1;

    /// <summary>
    /// Phase 2: evaluate authored schedule boundaries for the current tick; the 35:00 terminal
    /// boundary is handled before another tick can begin.
    /// </summary>
    /// <remarks>doc 10:145. The authored schedule belongs to the encounter packages.</remarks>
    public const int EvaluateScheduleBoundaries = 2;

    /// <summary>Phase 3: materialize queued spawns that have capacity and valid positions.</summary>
    /// <remarks>doc 10:146.</remarks>
    public const int MaterializeSpawns = 3;

    /// <summary>Phase 4: resolve player intent and enemy steering.</summary>
    /// <remarks>doc 10:147.</remarks>
    public const int ResolveIntentAndSteering = 4;

    /// <summary>Phase 5: integrate movement and enforce terrain/world constraints.</summary>
    /// <remarks>doc 10:148.</remarks>
    public const int IntegrateMovement = 5;

    /// <summary>Phase 6: rebuild or incrementally update spatial-query structures.</summary>
    /// <remarks>doc 10:149.</remarks>
    public const int UpdateSpatialStructures = 6;

    /// <summary>Phase 7: acquire automatic-weapon targets and advance attack schedules.</summary>
    /// <remarks>doc 10:150.</remarks>
    public const int AcquireTargets = 7;

    /// <summary>
    /// Phase 8: simulate projectiles, beams, zones, pulses, drones, and weapon contacts.
    /// </summary>
    /// <remarks>doc 10:151.</remarks>
    public const int SimulateWeapons = 8;

    /// <summary>Phase 9: collect collision, overlap, and damage candidates.</summary>
    /// <remarks>doc 10:152.</remarks>
    public const int CollectDamageCandidates = 9;

    /// <summary>
    /// Phase 10: resolve damage, status changes, deaths, and boss/resource consequences in stable
    /// order.
    /// </summary>
    /// <remarks>doc 10:153.</remarks>
    public const int ResolveDamage = 10;

    /// <summary>
    /// Phase 11: advance mining, extraction progress/decay, resource payouts, pickups, and
    /// run-local transactions caused by gameplay.
    /// </summary>
    /// <remarks>doc 10:154.</remarks>
    public const int AdvanceMining = 11;

    /// <summary>Phase 12: apply deferred entity creation/removal and capacity queues.</summary>
    /// <remarks>doc 10:155.</remarks>
    public const int ApplyDeferredStructuralChanges = 12;

    /// <summary>Phase 13: evaluate death or extraction terminal conditions.</summary>
    /// <remarks>doc 10:156.</remarks>
    public const int EvaluateTerminalConditions = 13;

    /// <summary>Phase 14: publish metrics, ordered events, and the presentation snapshot.</summary>
    /// <remarks>doc 10:157.</remarks>
    public const int Publish = 14;

    /// <summary>The lowest phase identifier.</summary>
    public const int First = AdmitCommands;

    /// <summary>The highest phase identifier.</summary>
    public const int Last = Publish;
}
