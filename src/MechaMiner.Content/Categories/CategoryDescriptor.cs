using System;
using MechaMiner.Content.Ids;

namespace MechaMiner.Content.Categories;

/// <summary>
/// One definition kind: its authoring category, its schema document, its field table,
/// and the values the compiler derives for it.
/// </summary>
public sealed class CategoryDescriptor
{
    internal CategoryDescriptor(
        DefinitionKind kind,
        ContentCategory category,
        string schemaFileName,
        DefinitionShape shape,
        DerivedFieldRegister derived,
        bool omitsNameKey)
    {
        Kind = kind;
        Category = category;
        SchemaFileName = schemaFileName;
        Shape = shape;
        Derived = derived;
        OmitsNameKey = omitsNameKey;
    }

    /// <summary>The definition kind.</summary>
    public DefinitionKind Kind { get; }

    /// <summary>The authoring category, and so the directory and ID grammar.</summary>
    public ContentCategory Category { get; }

    /// <summary>The file name beneath <c>content/schemas/</c>.</summary>
    public string SchemaFileName { get; }

    /// <summary>The declared field table, in schema-declared order.</summary>
    public DefinitionShape Shape { get; }

    /// <summary>The values the compiler derives for this kind.</summary>
    public DerivedFieldRegister Derived { get; }

    /// <summary>
    /// True when this kind is an aggregate players never see named, and so omits
    /// <c>name_key</c> and <c>summary_key</c>.
    /// </summary>
    /// <remarks>
    /// Doc 40 § Declared-optional envelope fields: "A definition players never see
    /// named - an aggregate schedule or a generation contract - omits it. The
    /// localization catalog holds strings players read; internal aggregate titles do
    /// not belong in it." <c>WAV-01</c> and <c>MGC-01</c> are named in that sentence;
    /// <c>ELT-01</c>, <c>FORMULA-01</c> and <c>PLAYER-01</c> are aggregates or
    /// contracts on the same terms.
    /// <para>
    /// This is a positive assertion and not merely permission to omit: a name key on an
    /// aggregate would put an internal title into the string catalog, and the catalog
    /// is checked for orphans, so the string would be either read by nobody or read on
    /// a surface that does not exist.
    /// </para>
    /// </remarks>
    public bool OmitsNameKey { get; }

    /// <summary>The repository-relative path of this kind's schema document.</summary>
    public string SchemaPath => "content/schemas/" + SchemaFileName;

    /// <inheritdoc/>
    public override string ToString()
    {
        return Kind + " -> " + SchemaPath;
    }

    /// <summary>Throws when this kind is asked for a shape it does not have.</summary>
    internal static CategoryDescriptor Undeclared(DefinitionKind kind)
    {
        throw new ArgumentOutOfRangeException(
            nameof(kind), kind, "no field table is declared for this definition kind");
    }
}
