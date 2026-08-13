using System;
using System.Collections.Generic;

namespace MechaMiner.Tools.Cli;

/// <summary>
/// The result of validating one invocation's arguments against a verb's declared
/// argument contract.
/// </summary>
internal sealed class ParsedArguments
{
    private readonly Dictionary<string, string> _values;

    private ParsedArguments(Dictionary<string, string> values, string? error)
    {
        _values = values;
        Error = error;
    }

    /// <summary>The invalid-argument message, or null when the invocation is valid.</summary>
    internal string? Error { get; }

    /// <summary>Whether the invocation matched the declared argument contract.</summary>
    internal bool IsValid => Error is null;

    /// <summary>Returns the value of a declared argument, or its default.</summary>
    internal string Value(string name)
    {
        return _values.TryGetValue(name, out string? value) ? value : string.Empty;
    }

    /// <summary>
    /// Validates <paramref name="arguments"/> against <paramref name="descriptor"/>.
    /// </summary>
    /// <remarks>
    /// The parser is deliberately small and explicit: positional values in
    /// declaration order, then <c>--name value</c> options. Anything else is an
    /// invalid invocation. Doc 100 § Standard command surface: "Unknown
    /// verbs/arguments fail with usage."
    /// </remarks>
    internal static ParsedArguments Parse(VerbDescriptor descriptor, IReadOnlyList<string> arguments)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        List<VerbArgument> positionals = new();
        Dictionary<string, VerbArgument> options = new(StringComparer.Ordinal);

        foreach (VerbArgument argument in descriptor.Arguments)
        {
            if (argument.Kind == VerbArgumentKind.Positional)
            {
                positionals.Add(argument);
            }
            else
            {
                options.Add(argument.Name, argument);
            }

            if (argument.DefaultValue is not null)
            {
                values[argument.Name] = argument.DefaultValue;
            }
        }

        int positionalIndex = 0;
        for (int index = 0; index < arguments.Count; index++)
        {
            string token = arguments[index];

            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                string name = token[2..];
                if (!options.TryGetValue(name, out VerbArgument? option))
                {
                    return Invalid(
                        "unknown argument '" + token + "' for verb '" + descriptor.Name + "'");
                }

                if (index + 1 >= arguments.Count)
                {
                    return Invalid("argument '" + token + "' requires a value");
                }

                string value = arguments[index + 1];
                index++;
                if (!option.Accepts(value))
                {
                    return Invalid(DescribeRejectedValue(token, value, option));
                }

                values[name] = value;
                continue;
            }

            if (positionalIndex >= positionals.Count)
            {
                return Invalid(
                    "unexpected argument '" + token + "' for verb '" + descriptor.Name + "'");
            }

            VerbArgument positional = positionals[positionalIndex];
            if (!positional.Accepts(token))
            {
                return Invalid(DescribeRejectedValue("<" + positional.Name + ">", token, positional));
            }

            values[positional.Name] = token;
            positionalIndex++;
        }

        foreach (VerbArgument argument in descriptor.Arguments)
        {
            if (argument.Required && !values.ContainsKey(argument.Name))
            {
                string shape = argument.Kind == VerbArgumentKind.Option
                    ? "--" + argument.Name + " <value>"
                    : "<" + argument.Name + ">";
                return Invalid(
                    "verb '" + descriptor.Name + "' requires " + shape);
            }
        }

        return new ParsedArguments(values, null);
    }

    private static string DescribeRejectedValue(string argumentText, string value, VerbArgument argument)
    {
        IReadOnlyList<string> allowed = argument.AllowedValueList();
        if (allowed.Count == 0)
        {
            return "argument " + argumentText + " requires a value, got '" + value + "'";
        }

        return "argument " + argumentText + " must be one of ["
            + string.Join(", ", allowed) + "], got '" + value + "'";
    }

    private static ParsedArguments Invalid(string error)
    {
        return new ParsedArguments(new Dictionary<string, string>(StringComparer.Ordinal), error);
    }
}
