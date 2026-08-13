namespace MechaMiner.Content.Envelope;

/// <summary>
/// A definition's lifecycle state.
/// </summary>
/// <remarks>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Common definition
/// envelope: "development, enabled, disabled, or retired; release bundles exclude
/// development/disabled unless configured". The four are exhaustive; there is no
/// default, because a definition with no declared status is a definition nobody
/// decided to ship.
/// </remarks>
public enum DefinitionStatus
{
    /// <summary>Being authored. Excluded from a release bundle unless configured otherwise.</summary>
    Development = 0,

    /// <summary>Shipping.</summary>
    Enabled = 1,

    /// <summary>Authored and complete but deliberately not shipping.</summary>
    Disabled = 2,

    /// <summary>
    /// Withdrawn. The definition remains so that saves and diagnostic seeds naming its
    /// ID still resolve; its ID is never reassigned
    /// (doc 40 § Stable ID policy).
    /// </summary>
    Retired = 3,
}
