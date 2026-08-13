using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MechaMiner.Tools.Cli;

/// <summary>How an argument appears on the command line.</summary>
internal enum VerbArgumentKind
{
    /// <summary>A bare value in declaration order, for example <c>scenario &lt;id&gt;</c>.</summary>
    Positional,

    /// <summary>A named <c>--name value</c> pair, for example <c>map --seed &lt;seed&gt;</c>.</summary>
    Option,
}

/// <summary>
/// One argument of one verb. The declared set is part of the wrapper contract:
/// <c>docs/technical/100-build-dependencies-and-release-operations.md</c>
/// § Standard command surface requires both root wrappers to expose "identical
/// verbs and argument names", and both wrappers reach this table through the same
/// process, so the argument names cannot diverge between shell languages.
/// </summary>
internal sealed class VerbArgument
{
    private VerbArgument(
        string name,
        VerbArgumentKind kind,
        bool required,
        ImmutableArray<string> allowedValues,
        string? defaultValue)
    {
        Name = name;
        Kind = kind;
        Required = required;
        AllowedValues = allowedValues;
        DefaultValue = defaultValue;
    }

    /// <summary>The argument name without the <c>--</c> prefix.</summary>
    internal string Name { get; }

    /// <summary>Whether the argument is positional or a named option.</summary>
    internal VerbArgumentKind Kind { get; }

    /// <summary>Whether omitting the argument is an invalid invocation.</summary>
    internal bool Required { get; }

    /// <summary>The closed value set, or empty when any value is accepted.</summary>
    internal ImmutableArray<string> AllowedValues { get; }

    /// <summary>The value used when an optional argument is omitted.</summary>
    internal string? DefaultValue { get; }

    /// <summary>Declares a required positional argument with an open value set.</summary>
    internal static VerbArgument Positional(string name)
    {
        return new VerbArgument(name, VerbArgumentKind.Positional, required: true, ImmutableArray<string>.Empty, null);
    }

    /// <summary>Declares a required positional argument with a closed value set.</summary>
    internal static VerbArgument PositionalChoice(string name, params string[] allowedValues)
    {
        return new VerbArgument(
            name,
            VerbArgumentKind.Positional,
            required: true,
            ImmutableArray.Create(allowedValues),
            null);
    }

    /// <summary>Declares a required <c>--name value</c> option.</summary>
    internal static VerbArgument RequiredOption(string name)
    {
        return new VerbArgument(name, VerbArgumentKind.Option, required: true, ImmutableArray<string>.Empty, null);
    }

    /// <summary>Declares an optional <c>--name value</c> option with a closed value set and a default.</summary>
    internal static VerbArgument OptionalChoice(string name, string defaultValue, params string[] allowedValues)
    {
        return new VerbArgument(
            name,
            VerbArgumentKind.Option,
            required: false,
            ImmutableArray.Create(allowedValues),
            defaultValue);
    }

    /// <summary>
    /// Renders the argument the way the usage table and the parity fixture read it.
    /// The text is part of the wrapper contract compared by
    /// <c>build/verify-wrapper-parity.sh</c>.
    /// </summary>
    internal string ToUsageText()
    {
        string valueText = AllowedValues.IsEmpty
            ? "<" + Name + ">"
            : "<" + string.Join("|", AllowedValues) + ">";

        string body = Kind == VerbArgumentKind.Option
            ? "--" + Name + " " + valueText
            : valueText;

        return Required ? body : "[" + body + "]";
    }

    /// <summary>Returns whether <paramref name="value"/> is inside the declared value set.</summary>
    internal bool Accepts(string value)
    {
        if (AllowedValues.IsEmpty)
        {
            return value.Length > 0 && !value.StartsWith("--", StringComparison.Ordinal);
        }

        foreach (string allowed in AllowedValues)
        {
            if (string.Equals(allowed, value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the declared value set as a reviewable, ordered list.</summary>
    internal IReadOnlyList<string> AllowedValueList()
    {
        return AllowedValues;
    }
}
