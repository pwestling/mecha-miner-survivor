namespace MechaMiner.Content.Diagnostics;

/// <summary>
/// The compilation stage a diagnostic code belongs to.
/// </summary>
/// <remarks>
/// <para>
/// The stages follow <c>docs/technical/40-content-data-and-validation.md</c>
/// § Compilation pipeline and § Validation layers. The stage is encoded in the
/// thousands digit of every code, so a reader can place a code without a lookup - the
/// same trick <c>src/MechaMiner.Tools/Cli/DiagnosticCodes.cs</c> uses to encode the
/// exit class in its numeric part.
/// </para>
/// <para>
/// Later DAT packages own the stages this enum does not yet declare: semantic
/// (<c>DAT-002</c>, <c>DAT-003</c>), relational (<c>DAT-005</c>), and analytical
/// (<c>DAT-008</c>). Their code bands are reserved in
/// <see cref="ContentDiagnosticCodes"/> and are deliberately not declared here, so
/// that the enum lists only stages that exist.
/// </para>
/// </remarks>
public enum ContentValidationStage
{
    /// <summary>Strict JSON codec policy, including the size, depth, and count ceilings. Band 1xxx.</summary>
    Codec = 1,

    /// <summary>
    /// Structural schema validation: required fields, allowed properties, types, enum
    /// vocabulary, and numeric integrality. Band 2xxx.
    /// </summary>
    Structural = 2,

    /// <summary>Stable identity: ID grammar per category and retirement. Band 3xxx.</summary>
    Identity = 3,

    /// <summary>Traceability: the <c>source_refs</c> grammar and scope resolution. Band 4xxx.</summary>
    Traceability = 4,

    /// <summary>
    /// A fault in the schema infrastructure itself rather than in content: a schema
    /// that cannot be loaded or uses a keyword the evaluator does not implement. Band
    /// 5xxx.
    /// </summary>
    SchemaInfrastructure = 5,
}
