namespace MechaMiner.Content.Categories;

/// <summary>
/// Which of the two array treatments the canonical writer gives a declared array field.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § JSON codec and schema
/// baseline states the whole distinction in one clause: the canonical writer emits
/// "stable-ID sets in canonical ID order, and semantically ordered arrays in their
/// authored/explicit order". That sentence is the contract. This enum is a name for it
/// and nothing more, so a reader who wants the rule should cite doc 40 rather than this
/// file.
/// </para>
/// <para>
/// <b>The member names are the writer's on purpose.</b>
/// <see cref="Codec.CanonicalJsonWriter.WriteIdSet"/> and
/// <c>CanonicalJsonWriter.WriteOrderedArray</c> already implement the two treatments, so
/// a declaration that named them differently would be a third vocabulary for one
/// concept: the field table says <see cref="IdSet"/> and the writer answers with
/// <c>WriteIdSet</c>. The type's own name is this stream's invention; the two member
/// names are not.
/// </para>
/// <para>
/// <b>Doc 40 classifies no individual field.</b> It gives the discriminator and leaves
/// every field's answer to whoever declares it, which is why the answer is stated at the
/// declaration and not inferred at the writer. The rule "the elements are stable IDs, so
/// it is a set" is known false in this tree: <c>recipe_pair_material_ids</c> holds two
/// resource IDs and its order spells the weapon's own ID, which
/// <see cref="Relational.CatalogChecks.RecipeLettersSpellTheWeaponId"/> checks and
/// sorting would delete.
/// </para>
/// </remarks>
public enum ArrayOrder
{
    /// <summary>Reserved so a default-initialised order is never a real one.</summary>
    /// <remarks>
    /// The same reservation <see cref="FieldShape.Unspecified"/> makes, for the same
    /// reason. A non-array field carries this value because array order does not apply to
    /// it; the array factories on <see cref="DefinitionField"/> reject it, so a declared
    /// array always states one of the two real classes.
    /// </remarks>
    Unspecified = 0,

    /// <summary>
    /// A set of stable IDs, emitted in canonical ID order (doc 40: "stable-ID sets in
    /// canonical ID order").
    /// </summary>
    /// <remarks>
    /// Canonical ID order is a total order over the tokens, so the field's authored order
    /// carries nothing and two authorings of the same set produce the same bytes. The
    /// treatment also forbids a repeated element, because a repeated element makes the
    /// value not a set: <see cref="Codec.CanonicalJsonWriter.WriteIdSet"/> throws on one.
    /// </remarks>
    IdSet,

    /// <summary>
    /// A semantically ordered array, emitted in its authored order unchanged (doc 40:
    /// "semantically ordered arrays in their authored/explicit order").
    /// </summary>
    /// <remarks>
    /// The order is part of what the definition says - a rank index, a minute, a slot, a
    /// timestamp, a transcription whose sequence is its traceability to the accepted
    /// document it was read from - so sorting it would change the content.
    /// </remarks>
    OrderedArray,
}
