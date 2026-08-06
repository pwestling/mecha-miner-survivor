using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MechaMiner.Content.Codec;

namespace MechaMiner.Content.Categories;

/// <summary>
/// One value the compiler derives, with the operands it derives it from.
/// </summary>
/// <remarks>
/// The operand list is not decoration. Doc 40 § Unit and numeric policy: "Derived
/// values include source operands and calculation version in reports for
/// auditability", and § Compilation pipeline requires every diagnostic to carry
/// "relevant related IDs". An author told only that a field is derived still has to
/// find out from what; told the operands, they can check whether the value they typed
/// disagrees with them, which is usually the actual question.
/// </remarks>
public sealed class DerivedField
{
    /// <summary>Registers one derived value.</summary>
    /// <param name="pointer">The pointer at which authoring the value is an error.</param>
    /// <param name="derivation">How the value follows from its operands, in plain language.</param>
    /// <param name="operands">The pointers or stable IDs the value is derived from.</param>
    /// <exception cref="ArgumentException"><paramref name="derivation"/> is blank.</exception>
    public DerivedField(JsonPointer pointer, string derivation, params string[] operands)
    {
        if (string.IsNullOrWhiteSpace(derivation))
        {
            throw new ArgumentException(
                "a derived field states its derivation; 'this is derived' without the arithmetic "
                    + "tells an author nothing they can check",
                nameof(derivation));
        }

        ArgumentNullException.ThrowIfNull(operands);

        Pointer = pointer;
        Derivation = derivation;
        Operands = new ReadOnlyCollection<string>(new List<string>(operands));
    }

    /// <summary>Where the value would be authored.</summary>
    public JsonPointer Pointer { get; }

    /// <summary>How the value follows from its operands.</summary>
    public string Derivation { get; }

    /// <summary>The operands, as pointers or stable IDs.</summary>
    public IReadOnlyList<string> Operands { get; }

    /// <summary>Builds a register entry addressed by a root property name.</summary>
    public static DerivedField At(string field, string derivation, params string[] operands)
    {
        ArgumentNullException.ThrowIfNull(field);
        return new DerivedField(JsonPointer.Root.AppendProperty(field), derivation, operands);
    }

    /// <summary>Builds a register entry addressed by a nested property path.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    public static DerivedField Nested(string[] path, string derivation, params string[] operands)
    {
        ArgumentNullException.ThrowIfNull(path);

        JsonPointer pointer = JsonPointer.Root;
        foreach (string segment in path)
        {
            pointer = pointer.AppendProperty(segment);
        }

        return new DerivedField(pointer, derivation, operands);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Pointer.Value + " = " + Derivation;
    }
}
