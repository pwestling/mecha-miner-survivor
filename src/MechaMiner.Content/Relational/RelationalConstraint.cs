using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Relational;

/// <summary>
/// A declared relation between values that live in different definitions.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why these are a category and not two range checks.</b> Each of the constraints
/// declared here compares two numbers that each pass any plausible range check on their
/// own - and pass it just as happily with the relation inverted. Inverting one produces
/// two individually-valid numbers and one broken game. That is the test for whether a
/// constraint belongs here rather than in the semantic layer: <em>two range checks that
/// would both pass inverted</em>.
/// </para>
/// <para>
/// <b>Why they run after every definition is loaded.</b>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Relational scopes this
/// stage to "references, uniqueness, graph coverage", which is work no single file can
/// do: a geode does not know a utility's rank-3 percentage, and a map contract does not
/// know a site's extraction radius. Evaluating one during the per-file pass would mean
/// reading whichever operands happened to be loaded first, which makes the verdict
/// depend on source enumeration order - the property § JSON codec and schema baseline
/// forbids the pipeline from having. Threading lazy operand evaluation through the
/// per-file pass would produce the same instability with the ordering hidden instead of
/// declared.
/// </para>
/// <para>
/// A constraint that cannot resolve an operand reports the gap rather than skipping,
/// because a skipped relational check and a passing one are indistinguishable from the
/// outside.
/// </para>
/// </remarks>
public sealed class RelationalConstraint
{
    private readonly Func<IReadOnlyList<RelationalOperand>, bool> _holds;

    internal RelationalConstraint(
        string id,
        string subject,
        string requirement,
        string authority,
        IReadOnlyList<string> requiredDefinitionIds,
        Func<RelationalCatalog, IReadOnlyList<RelationalOperand>> operands,
        Func<IReadOnlyList<RelationalOperand>, bool> holds)
    {
        Id = id;
        Subject = subject;
        Requirement = requirement;
        Authority = authority;
        RequiredDefinitionIds =
            new ReadOnlyCollection<string>(new List<string>(requiredDefinitionIds));
        Operands = operands;
        _holds = holds;
    }

    /// <summary>The constraint's stable identifier within this package.</summary>
    public string Id { get; }

    /// <summary>The definition whose field a diagnostic points at.</summary>
    public string Subject { get; }

    /// <summary>What the relation requires, in plain language.</summary>
    public string Requirement { get; }

    /// <summary>The documents that grant the relation.</summary>
    public string Authority { get; }

    /// <summary>
    /// Every definition that must be loaded before this constraint can be evaluated.
    /// </summary>
    public IReadOnlyList<string> RequiredDefinitionIds { get; }

    /// <summary>Reads this constraint's operands out of a loaded catalog.</summary>
    public Func<RelationalCatalog, IReadOnlyList<RelationalOperand>> Operands { get; }

    /// <summary>True when the relation holds over resolved operands.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is null.</exception>
    /// <exception cref="InvalidOperationException">An operand is unresolved.</exception>
    public bool Holds(IReadOnlyList<RelationalOperand> operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        foreach (RelationalOperand operand in operands)
        {
            if (!operand.IsResolved)
            {
                throw new InvalidOperationException(
                    "the constraint " + Id + " cannot be evaluated with an unresolved operand ("
                        + operand + "); the evaluator reports the gap rather than asking for a "
                        + "verdict it cannot reach");
            }
        }

        return _holds(operands);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return Id + ": " + Requirement;
    }
}
