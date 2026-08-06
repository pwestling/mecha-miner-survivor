using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;

namespace MechaMiner.Content.Categories;

/// <summary>
/// The values the compiler derives, keyed by the JSON pointer at which authoring one
/// would be a build error.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Enemies and bosses:
/// "Derived geometry is never authored... An author who types a derived value into a
/// definition creates a second source of truth that silently disagrees with the first
/// the moment either operand changes, which is exactly how a gameplay table and a
/// technical table came to disagree by 0.004 M on one enemy."
/// </para>
/// <para>
/// <b>Why a register rather than simply leaving the field undeclared.</b> An
/// undeclared field already fails the unknown-field check, so the register does not
/// exist to catch it - it exists to say <em>why</em>. An author who typed
/// <c>contact_and_weapon_hurt_diameter_m</c> onto an enemy and got "this field is not
/// declared" would reasonably conclude the schema was incomplete and add it. Getting
/// "this value is derived from body_scale_multiplier and the reference diameter; the
/// compiler emits it into the derived report" tells them the field is absent on
/// purpose and where the value went. The register runs before the unknown-field pass
/// so the more specific diagnostic wins.
/// </para>
/// <para>
/// <b>The route, and what it misses.</b> The match is on the exact JSON pointer. A
/// derived value smuggled in under a different field name is not caught here; nothing
/// short of comparing authored numbers against recomputed ones would catch that, which
/// is the analytical layer's work and not this one's.
/// </para>
/// <para>
/// <b>The enemy/boss asymmetry is declared here, in one place.</b> An enemy authors
/// <c>body_scale_multiplier</c> and the compiler derives both its contact diameter and
/// its contact-begin centre distance. A boss authors its contact diameter directly -
/// no accepted document gives a boss a body scale - and the compiler derives only the
/// centre distance. Two entries differ; nothing else does.
/// </para>
/// </remarks>
public sealed class DerivedFieldRegister
{
    private readonly Dictionary<JsonPointer, DerivedField> _byPointer;

    /// <summary>Builds a register from its entries.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is null.</exception>
    public DerivedFieldRegister(IReadOnlyList<DerivedField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        Fields = new ReadOnlyCollection<DerivedField>(new List<DerivedField>(fields));
        _byPointer = new Dictionary<JsonPointer, DerivedField>(fields.Count);
        foreach (DerivedField field in fields)
        {
            if (!_byPointer.TryAdd(field.Pointer, field))
            {
                throw new InvalidOperationException(
                    "the derived field at '" + field.Pointer.Value + "' is registered twice");
            }
        }
    }

    /// <summary>A register with no entries, for a kind that derives nothing.</summary>
    public static DerivedFieldRegister Empty { get; } = new(Array.Empty<DerivedField>());

    /// <summary>Every registered derived field.</summary>
    public IReadOnlyList<DerivedField> Fields { get; }

    /// <summary>
    /// Reports every registered pointer the document authors. Returns true when none
    /// was found.
    /// </summary>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public bool Check(
        DocumentOutline outline,
        CategoryReadContext context,
        string? contentId,
        DiagnosticBag bag)
    {
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(bag);

        bool clean = true;
        foreach (DerivedField field in Fields)
        {
            if (!outline.Contains(field.Pointer))
            {
                continue;
            }

            clean = false;
            bag.Add(ContentDiagnostic.CreateError(
                ContentDiagnosticCodes.DerivedValueAuthored,
                context.SourcePath,
                field.Pointer,
                contentId,
                "this value is derived, not authored: " + field.Derivation
                    + ". The compiler emits it into the derived report with its source operands "
                    + "and calculation version; authoring it here creates a second writer that "
                    + "disagrees with the first the moment an operand changes",
                field.Operands));
        }

        return clean;
    }

    /// <summary>Looks up the registered derivation at <paramref name="pointer"/>.</summary>
    public bool TryGet(JsonPointer pointer, out DerivedField? field)
    {
        return _byPointer.TryGetValue(pointer, out field);
    }
}
