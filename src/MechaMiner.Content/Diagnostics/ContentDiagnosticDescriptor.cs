using System;

namespace MechaMiner.Content.Diagnostics;

/// <summary>One declared diagnostic code and what it means.</summary>
public sealed class ContentDiagnosticDescriptor
{
    internal ContentDiagnosticDescriptor(
        string code,
        string name,
        ContentValidationStage stage,
        string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "every diagnostic code carries a human description in its declaration",
                nameof(description));
        }

        Code = code;
        Name = name;
        Stage = stage;
        Description = description;
    }

    /// <summary>The stable code, for example <c>MMC-1003</c>.</summary>
    public string Code { get; }

    /// <summary>The constant's name, so a report can print a symbol rather than a number.</summary>
    public string Name { get; }

    /// <summary>The compilation stage that emits the code.</summary>
    public ContentValidationStage Stage { get; }

    /// <summary>What the code means, in one sentence.</summary>
    public string Description { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Code + " " + Name + ": " + Description;
    }
}
