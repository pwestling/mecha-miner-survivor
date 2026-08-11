namespace MechaMiner.Simulation.Random;

/// <summary>
/// What a registered stream family's instance key means, and therefore which instance keys it
/// accepts.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// family table gives every registered family an instance-key column. Those twelve distinct
/// rules are data, not prose: <c>VER-SIM-005-012</c> requires a family whose registered
/// instance key is zero to <em>reject</em> a nonzero instance key rather than derive an
/// unregistered stream, which a bare key constant cannot express.
/// </para>
/// <para>
/// Doc 20 § Authoritative random-number contract: "Stable generated IDs and ordinals come from
/// canonical manifest/order rules, never dictionary or scene enumeration." The
/// identifier-shaped rules below are therefore unbounded here — their canonical derivation
/// belongs to the owning system — while the rules doc 20 states as a closed range are bounded.
/// </para>
/// </remarks>
public enum InstanceKeyRule
{
    /// <summary>Zero. The family has exactly one stream (doc 20 § Authoritative random-number
    /// contract, :60, :61, :65-:67, :69-:71, :74).</summary>
    Zero = 0,

    /// <summary>A stable generated region ID (doc 20 § Authoritative random-number
    /// contract).</summary>
    RegionId = 1,

    /// <summary>A canonical material ordinal <c>0-5</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    MaterialOrdinal = 2,

    /// <summary>
    /// Zero for placement, or a stable rock ID for that rock's one drop roll (doc 20 §
    /// Authoritative random-number contract).
    /// </summary>
    RockId = 3,

    /// <summary>A schedule-row / minute index (doc 20 § Authoritative random-number
    /// contract).</summary>
    ScheduleRowIndex = 4,

    /// <summary>A stable Hyper Gold site ID (doc 20 § Authoritative random-number
    /// contract).</summary>
    SiteId = 5,

    /// <summary>A scheduled boss index <c>0-3</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    BossIndex = 6,

    /// <summary>A weapon slot ordinal <c>0-3</c> (doc 20 § Authoritative random-number
    /// contract).</summary>
    WeaponSlotOrdinal = 7,

    /// <summary>
    /// A stable spawning source plus entity generation, encoded as one instance key (doc 20 §
    /// Authoritative random-number contract).
    /// </summary>
    SourceAndGeneration = 8,

    /// <summary>A stable reward-source ID (doc 20 § Authoritative random-number
    /// contract).</summary>
    RewardSourceId = 9,

    /// <summary>
    /// The selected profile ordinal plus the region-count ordinal (doc 20 § Authoritative
    /// random-number contract).
    /// </summary>
    ProfileAndRegionCountOrdinal = 10,

    /// <summary>
    /// A presentation binding identity. Doc 20 § Authoritative random-number contract: "never
    /// serialized into authoritative state".
    /// </summary>
    PresentationBinding = 11,
}
