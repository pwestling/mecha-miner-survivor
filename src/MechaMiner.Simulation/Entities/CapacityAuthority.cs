namespace MechaMiner.Simulation.Entities;

/// <summary>
/// Where a store's capacity number comes from, and therefore what has to change for the
/// number to change.
/// </summary>
/// <remarks>
/// <para>
/// This distinction is load-bearing rather than descriptive. A capacity taken verbatim
/// from <c>docs/technical/22-combat-and-weapon-runtime.md</c> § Performance and capacity
/// must move whenever that section moves, because that section is a normative subsystem
/// contract and re-deriving it here would create the second source of truth
/// <c>docs/technical/40-content-data-and-validation.md</c> § Unit and numeric policy
/// warns about. A capacity derived from the encounter schedule must <em>not</em> move
/// when doc 22 moves, because it never depended on doc 22 in the first place.
/// </para>
/// <para>
/// Carrying it as data rather than as prose is what makes a stale figure findable: a
/// reviewer asking "which rows does this doc-22 change reach?" gets an answer from
/// <c>StoreCapacityTests</c> instead of from a re-reading of every derivation comment.
/// </para>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Capacity and overload behavior:
/// "Initial capacities are derived from the encounter schedule plus a documented margin
/// in each specialized system rather than selected as arbitrary powers of two." The
/// doc-22 rows are not a violation of that: doc 22 labels its values "provisional
/// authoritative hard capacities for stress validation ... intentionally above current
/// gameplay maxima" and "safety ceilings, not design allowances", with a stated revision
/// path ("legal maximum-output analysis must tighten or expand them"). They are ceilings
/// with a purpose and an owner, not round numbers standing in for a derivation.
/// </para>
/// </remarks>
public enum CapacityAuthority
{
    /// <summary>
    /// A simulation-wide invariant fixes the number; no derivation and no margin is
    /// admissible.
    /// </summary>
    /// <remarks>
    /// doc 20 § Scope and invariants: "exactly one player entity exists until terminal
    /// resolution."
    /// </remarks>
    SimulationInvariant = 0,

    /// <summary>
    /// Derived from the authored encounter schedule and beacon response tables, plus a
    /// documented margin.
    /// </summary>
    /// <remarks>
    /// doc 20 § Capacity and overload behavior; <c>docs/technical/23-encounter-director-and-enemy-runtime.md</c>
    /// § Population classes; <c>docs/32-standard-wave-and-beacon-schedule.md</c>.
    /// A doc 22 revision does not reach these rows.
    /// </remarks>
    EncounterSchedule = 1,

    /// <summary>
    /// Taken verbatim from doc 22 § Performance and capacity. <b>Moves when doc 22
    /// moves.</b>
    /// </summary>
    /// <remarks>
    /// doc 22 § Performance and capacity is a normative technical subsystem contract
    /// (<c>docs/technical/conventions.md</c> § Requirement sources and precedence) and it
    /// owns its own revision path: "Profiling and legal maximum-output analysis must
    /// tighten or expand them before content complete." A row with this authority is
    /// never re-derived locally; it is re-read.
    /// </remarks>
    CombatRuntimeCeiling = 2,

    /// <summary>
    /// Fixed by the validated map manifest at generation time and unable to change during
    /// a run, so no margin is admissible.
    /// </summary>
    /// <remarks>
    /// <c>docs/technical/50-procedural-map-generation.md</c> § Generated manifest and
    /// <c>docs/51-standard-map-generation-contract.md</c>. doc 115 § Cross-boundary
    /// contract registry <c>CTR-MAP-002</c>: "one immutable canonical manifest published
    /// only after all validators".
    /// </remarks>
    MapManifest = 3,

    /// <summary>
    /// Derived from gameplay rates because no document bounds the population, so the
    /// figure is the smallest defensible basis rather than a stated ceiling.
    /// </summary>
    /// <remarks>
    /// <c>docs/technical/conventions.md</c> § Certainty requires a provisional baseline
    /// to name its validation gate. A row with this authority also sets
    /// <c>StoreCapacity.IsWeaklySourced</c> and states which input is missing.
    /// </remarks>
    DerivedFromGameplayRates = 4,
}
