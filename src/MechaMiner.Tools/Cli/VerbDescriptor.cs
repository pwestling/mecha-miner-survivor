using System;
using System.Collections.Immutable;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// One registered verb of the standard command surface.
/// </summary>
/// <remarks>
/// <para>
/// Registration is an explicit table in <see cref="VerbRegistry"/>. There is no
/// reflection, attribute discovery, or assembly scanning: doc 100 § C# project
/// standards requires that "generated/explicit registries make missing behavior a
/// build error", and doc 114 § C# and domain defaults forbids a runtime registry.
/// </para>
/// <para>
/// A verb whose behavior is owned by a work package that has not landed is still
/// registered, with <see cref="Handler"/> left null. Invoking it validates the
/// argument contract and then returns a typed nonzero status naming
/// <see cref="OwningWorkPackage"/>, which is FND-002's completion gate in
/// <c>docs/technical/110-implementation-plan-for-ai-agents.md</c>
/// § Foundation work packages.
/// </para>
/// </remarks>
internal sealed class VerbDescriptor
{
    private VerbDescriptor(
        string name,
        string requiredEffect,
        string owningWorkPackage,
        ImmutableArray<VerbArgument> arguments,
        Func<VerbContext, VerbOutcome>? handler)
    {
        Name = name;
        RequiredEffect = requiredEffect;
        OwningWorkPackage = owningWorkPackage;
        Arguments = arguments;
        Handler = handler;
    }

    /// <summary>The verb name exactly as doc 100 § Standard command surface spells it.</summary>
    internal string Name { get; }

    /// <summary>The required effect quoted from doc 100's verb table.</summary>
    internal string RequiredEffect { get; }

    /// <summary>The work package that owns this verb's behavior.</summary>
    internal string OwningWorkPackage { get; }

    /// <summary>The declared argument contract, in usage order.</summary>
    internal ImmutableArray<VerbArgument> Arguments { get; }

    /// <summary>The implementation, or null while the owning work package has not landed.</summary>
    internal Func<VerbContext, VerbOutcome>? Handler { get; }

    /// <summary>Whether the verb's behavior is available in this revision.</summary>
    internal bool IsImplemented => Handler is not null;

    /// <summary>Declares an implemented verb.</summary>
    internal static VerbDescriptor Implemented(
        string name,
        string requiredEffect,
        string owningWorkPackage,
        Func<VerbContext, VerbOutcome> handler,
        params VerbArgument[] arguments)
    {
        return new VerbDescriptor(
            name,
            requiredEffect,
            owningWorkPackage,
            ImmutableArray.Create(arguments),
            handler);
    }

    /// <summary>
    /// Declares a verb whose argument contract is stable now but whose behavior
    /// belongs to a work package that has not landed.
    /// </summary>
    internal static VerbDescriptor AwaitingOwner(
        string name,
        string requiredEffect,
        string owningWorkPackage,
        params VerbArgument[] arguments)
    {
        return new VerbDescriptor(
            name,
            requiredEffect,
            owningWorkPackage,
            ImmutableArray.Create(arguments),
            null);
    }

    /// <summary>Renders <c>verb arg arg</c> exactly as the usage table prints it.</summary>
    internal string ToInvocationText()
    {
        if (Arguments.IsEmpty)
        {
            return Name;
        }

        string[] parts = new string[Arguments.Length];
        for (int index = 0; index < Arguments.Length; index++)
        {
            parts[index] = Arguments[index].ToUsageText();
        }

        return Name + " " + string.Join(" ", parts);
    }
}
