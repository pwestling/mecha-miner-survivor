using System;
using System.Globalization;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// The explicit, named policy under which presentation events of one kind may be merged. Absence of
/// a rule means verbatim delivery.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: "Presentation events
/// may be coalesced by an explicit visual policy; domain events may not be dropped."
/// <c>CTR-SIM-002</c> in doc 115 § Cross-boundary contract registry: the batch "may carry coalescing
/// policy".
/// </para>
/// <para>
/// <b>Explicit means opt-in per kind, and named.</b> A policy without a rule for a kind delivers that
/// kind verbatim, so coalescing can never happen by omission - the failure mode a default-on policy
/// would have. Each rule carries its own name so a merged batch can say which visual decision merged
/// it, which is what makes the policy reviewable rather than a boolean.
/// </para>
/// <para>
/// <b>Immutable, and built by returning new instances.</b> A policy that could be edited after a tick
/// began would make the batch's provenance unreproducible.
/// </para>
/// <para>
/// <b>No dictionary.</b> Rules live in a fixed array scanned linearly. A hash container would be a
/// legitimate lookup index, but it is not needed at this size and its absence removes any chance of
/// enumeration order reaching the batch - and the batch is ordered before merging anyway, precisely
/// so that merging walks the authoritative order rather than the policy's.
/// </para>
/// </remarks>
public sealed class PresentationCoalescingPolicy
{
    private readonly string _name;
    private readonly EventKind[] _kinds;
    private readonly string[] _ruleNames;

    private PresentationCoalescingPolicy(string name, EventKind[] kinds, string[] ruleNames)
    {
        _name = name;
        _kinds = kinds;
        _ruleNames = ruleNames;
    }

    /// <summary>
    /// The policy that merges nothing: every kind is delivered verbatim.
    /// </summary>
    /// <remarks>
    /// The default a caller should reach for, and the one a test uses to establish that coalescing is
    /// opt-in rather than ambient.
    /// </remarks>
    public static PresentationCoalescingPolicy Verbatim { get; } = new(
        "verbatim",
        Array.Empty<EventKind>(),
        Array.Empty<string>());

    /// <summary>This policy's name, carried into the published batch.</summary>
    public string Name => _name;

    /// <summary>How many kinds this policy merges.</summary>
    public int RuleCount => _kinds.Length;

    /// <summary>Starts a named policy with no rules.</summary>
    /// <param name="name">The policy name. Must not be blank.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is blank.</exception>
    public static PresentationCoalescingPolicy Named(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new PresentationCoalescingPolicy(name, Array.Empty<EventKind>(), Array.Empty<string>());
    }

    /// <summary>
    /// Returns a new policy that also merges <paramref name="kind"/>, under a named visual rule.
    /// </summary>
    /// <param name="kind">The declared kind to merge.</param>
    /// <param name="ruleName">Why this kind may be merged. Must not be blank.</param>
    /// <exception cref="ArgumentException">The kind is undeclared, the rule name is blank, or the kind already has a rule.</exception>
    public PresentationCoalescingPolicy WithMerge(EventKind kind, string ruleName)
    {
        if (!kind.IsDeclared)
        {
            throw new ArgumentException("a coalescing rule needs a declared kind", nameof(kind));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        if (TryGetMergeRule(kind, out string existing))
        {
            throw new ArgumentException(
                "kind "
                    + kind.ToString()
                    + " already merges under rule '"
                    + existing
                    + "'; two rules for one kind would make the applied rule ambiguous",
                nameof(kind));
        }

        EventKind[] kinds = new EventKind[_kinds.Length + 1];
        string[] ruleNames = new string[_ruleNames.Length + 1];
        Array.Copy(_kinds, kinds, _kinds.Length);
        Array.Copy(_ruleNames, ruleNames, _ruleNames.Length);
        kinds[^1] = kind;
        ruleNames[^1] = ruleName;
        return new PresentationCoalescingPolicy(_name, kinds, ruleNames);
    }

    /// <summary>Whether this policy merges <paramref name="kind"/>, and under which named rule.</summary>
    /// <param name="kind">The kind to test.</param>
    /// <param name="ruleName">The rule's name when one exists, otherwise empty.</param>
    /// <returns><see langword="false"/> when no rule exists, which means verbatim delivery.</returns>
    public bool TryGetMergeRule(EventKind kind, out string ruleName)
    {
        for (int index = 0; index < _kinds.Length; index++)
        {
            if (_kinds[index] == kind)
            {
                ruleName = _ruleNames[index];
                return true;
            }
        }

        ruleName = string.Empty;
        return false;
    }

    /// <summary>Renders the policy as canonical invariant text.</summary>
    public override string ToString()
    {
        return "policy:"
            + _name
            + " rules="
            + _kinds.Length.ToString(CultureInfo.InvariantCulture);
    }
}
