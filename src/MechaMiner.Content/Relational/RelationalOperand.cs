using System;

namespace MechaMiner.Content.Relational;

/// <summary>
/// One value a relational constraint compares, named by the definition it comes from
/// and the pointer into that definition.
/// </summary>
/// <remarks>
/// <para>
/// A relational diagnostic names every operand with its pointer, not only the field
/// that failed. <c>docs/technical/40-content-data-and-validation.md</c> § Compilation
/// pipeline requires "relevant related IDs" for exactly this reason: when a relation
/// between three numbers in three files does not hold, the fix could be to any of them,
/// and a diagnostic naming one sends the reader to the wrong file two times in three.
/// </para>
/// </remarks>
public sealed class RelationalOperand
{
    /// <summary>Names one operand.</summary>
    /// <param name="definitionId">The stable ID of the definition holding it.</param>
    /// <param name="pointer">The JSON pointer within that definition.</param>
    /// <param name="value">The value read, or null when the definition was not loaded.</param>
    /// <exception cref="ArgumentException">The ID or pointer is blank.</exception>
    public RelationalOperand(string definitionId, string pointer, double? value)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            throw new ArgumentException("an operand names the definition it comes from",
                nameof(definitionId));
        }

        if (string.IsNullOrWhiteSpace(pointer))
        {
            throw new ArgumentException("an operand names the field it comes from", nameof(pointer));
        }

        DefinitionId = definitionId;
        Pointer = pointer;
        Value = value;
    }

    /// <summary>The definition the value comes from.</summary>
    public string DefinitionId { get; }

    /// <summary>The JSON pointer within that definition.</summary>
    public string Pointer { get; }

    /// <summary>The value, or null when the definition was not loaded.</summary>
    public double? Value { get; }

    /// <summary>True when the operand could be read.</summary>
    public bool IsResolved => Value is not null;

    /// <inheritdoc/>
    public override string ToString()
    {
        return DefinitionId + "#" + Pointer + " = "
            + (Value is null
                ? "<not loaded>"
                : Value.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }
}
