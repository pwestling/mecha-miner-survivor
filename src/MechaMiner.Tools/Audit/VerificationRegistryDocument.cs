using System.Collections.Generic;

namespace MechaMiner.Tools.Audit;

/// <summary>
/// A <c>SCH-QUA-001</c> work-package verification registry, as
/// <c>docs/technical/91-verification-strategy.md</c> § Verification registry defines it.
/// </summary>
/// <remarks>
/// <para>
/// Doc 91: "Every entry contains a stable <c>VER-&lt;WORK-PACKAGE&gt;-###</c> ID,
/// summary, cited <c>TR-*</c> requirements and gameplay sources, automated test
/// selectors or manual/device procedure ID, fixture/seed/scenario IDs, evidence artifact
/// kinds, applicable platforms/tier, and current status."
/// </para>
/// <para>
/// The DTO is deliberately permissive about nullability so a malformed fixture reaches
/// the validator's rules instead of failing deserialization with a message that names a
/// JSON path rather than a registry rule. Unknown fields are still rejected, per doc 40
/// § JSON codec and schema baseline: a field nobody reads is either a typo or a contract
/// change that was not registered.
/// </para>
/// </remarks>
internal sealed class VerificationRegistryDocument
{
    /// <summary>Stable schema identity. Must be <c>SCH-QUA-001</c>.</summary>
    public string? Schema { get; set; }

    /// <summary>Version of the registry's shape.</summary>
    public int SchemaVersion { get; set; }

    /// <summary>The work package that owns this file.</summary>
    public string? WorkPackage { get; set; }

    /// <summary>The work package's deliverable title.</summary>
    public string? WorkPackageTitle { get; set; }

    /// <summary>Editorial notes recording decisions a reader would otherwise re-derive.</summary>
    public List<string> Notes { get; set; } = new();

    /// <summary>The verification entries, in ascending ID order.</summary>
    public List<VerificationEntry> Entries { get; set; } = new();
}

/// <summary>One <c>VER-*</c> verification entry.</summary>
internal sealed class VerificationEntry
{
    /// <summary>The stable <c>VER-&lt;WORK-PACKAGE&gt;-###</c> identifier.</summary>
    public string? Id { get; set; }

    /// <summary>What the entry proves, in observable terms.</summary>
    public string? Summary { get; set; }

    /// <summary>The implementation task this entry covers.</summary>
    public string? Task { get; set; }

    /// <summary>Cited <c>TR-*</c> requirement identifiers.</summary>
    public List<string> Requirements { get; set; } = new();

    /// <summary>Cited technical document sections, as <c>path#anchor</c>.</summary>
    public List<string> TechnicalSources { get; set; } = new();

    /// <summary>Cited gameplay document sections, as <c>path#anchor</c>.</summary>
    public List<string> GameplaySources { get; set; } = new();

    /// <summary>How the verification is executed.</summary>
    public VerificationSelector? Selector { get; set; }

    /// <summary>Fixture, corpus, or seed identities the entry uses.</summary>
    public List<string> Fixtures { get; set; } = new();

    /// <summary>Accepted scenario identities such as <c>PERF-*</c> and <c>WB-*</c>.</summary>
    public List<string> Scenarios { get; set; } = new();

    /// <summary>The kinds of evidence artifact the entry produces.</summary>
    public List<string> EvidenceKinds { get; set; } = new();

    /// <summary>Platforms the entry applies to.</summary>
    public List<string> Platforms { get; set; } = new();

    /// <summary>The suite tier that runs it.</summary>
    public string? Tier { get; set; }

    /// <summary>Current status.</summary>
    public string? Status { get; set; }

    /// <summary>
    /// The successor work package or task, required on a retired entry and allowed on an
    /// entry whose approach a later package replaces.
    /// </summary>
    public string? Successor { get; set; }

    /// <summary>
    /// The work package an entry's execution is deferred to, when the entry is registered
    /// here but cannot run until another package provides what it needs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MODELLED SO THE FIELD CAN BE READ, AND FOR NO OTHER PURPOSE. Nothing keys behaviour
    /// on this value: it is not validated, not required, not cross-checked against the
    /// work-package register, and no rule consults it. Adding a member is what makes the
    /// registries carrying it parseable, and any rule about what a legitimate deferral
    /// looks like belongs to whoever owns that rule, not to this DTO.
    /// </para>
    /// <para>
    /// It is modelled rather than ignored because
    /// <c>src/MechaMiner.Tools/Audit/ToolsJsonContextAccess.cs</c> sets
    /// <c>UnmappedMemberHandling.Disallow</c>, which the class remarks above state as
    /// deliberate: an unregistered field is a hard deserialization error rather than a
    /// silent drop. That is the correct default and is kept. The consequence was that four
    /// committed registries - <c>PRE-001</c>, <c>SIM-003</c>, <c>SIM-007</c> and
    /// <c>UI-002</c> - could not be read AT ALL, so every entry in them went uncounted and
    /// unvalidated rather than one field going unread. Seven entries across those four
    /// files carry it today, with values <c>OPS-001</c>, <c>FND-005</c> and <c>QUA-005</c>.
    /// Registering the field here is the "contract change that was not registered" half of
    /// that rule being discharged, which is the remedy the rule asks for.
    /// </para>
    /// </remarks>
    public string? DeferredTo { get; set; }
}

/// <summary>How one verification entry is executed.</summary>
internal sealed class VerificationSelector
{
    /// <summary>One of <c>command</c>, <c>script</c>, <c>nunit</c>, or <c>manual</c>.</summary>
    public string? Kind { get; set; }

    /// <summary>The command line, script path, test selector, or manual procedure ID.</summary>
    public string? Value { get; set; }
}
