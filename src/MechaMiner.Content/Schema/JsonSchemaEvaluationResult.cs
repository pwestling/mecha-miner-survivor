using System.Collections.Generic;

namespace MechaMiner.Content.Schema;

/// <summary>The outcome of evaluating one instance against a schema.</summary>
public sealed class JsonSchemaEvaluationResult
{
    internal JsonSchemaEvaluationResult(IReadOnlyList<JsonSchemaError> errors)
    {
        Errors = errors;
    }

    /// <summary>Every assertion the instance failed.</summary>
    public IReadOnlyList<JsonSchemaError> Errors { get; }

    /// <summary>True when the instance satisfied the schema.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <inheritdoc/>
    public override string ToString()
    {
        return Errors.Count == 0
            ? "valid"
            : string.Join(System.Environment.NewLine, Errors);
    }
}
