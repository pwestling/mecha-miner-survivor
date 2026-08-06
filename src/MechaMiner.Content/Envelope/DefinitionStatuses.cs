using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MechaMiner.Content.Envelope;

/// <summary>
/// The authored tokens for <see cref="DefinitionStatus"/>.
/// </summary>
/// <remarks>
/// The mapping is exact and case-sensitive. Doc 40 § JSON codec and schema baseline:
/// "stable enum/kind/ID tokens remain exact case-sensitive ASCII". <c>Enabled</c> is
/// not <c>enabled</c>, and accepting both would mean the canonical bytes of a
/// definition depended on which spelling its author happened to type.
/// </remarks>
public static class DefinitionStatuses
{
    private static readonly (string Token, DefinitionStatus Status)[] Declared =
    {
        ("development", DefinitionStatus.Development),
        ("enabled", DefinitionStatus.Enabled),
        ("disabled", DefinitionStatus.Disabled),
        ("retired", DefinitionStatus.Retired),
    };

    private static readonly Dictionary<string, DefinitionStatus> ByToken = BuildTokenIndex();

    private static readonly Dictionary<DefinitionStatus, string> ByStatus = BuildStatusIndex();

    /// <summary>Every accepted token, in lifecycle order.</summary>
    public static IReadOnlyList<string> Tokens { get; } = BuildTokenList();

    /// <summary>Resolves an authored token.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is null.</exception>
    public static bool TryParse(string token, out DefinitionStatus status)
    {
        ArgumentNullException.ThrowIfNull(token);
        return ByToken.TryGetValue(token, out status);
    }

    /// <summary>The authored token for <paramref name="status"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> has no token.</exception>
    public static string ToToken(DefinitionStatus status)
    {
        if (!ByStatus.TryGetValue(status, out string? token))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "no authored token is declared for this status");
        }

        return token;
    }

    private static IReadOnlyList<string> BuildTokenList()
    {
        List<string> tokens = new(Declared.Length);
        foreach ((string token, DefinitionStatus _) in Declared)
        {
            tokens.Add(token);
        }

        return new ReadOnlyCollection<string>(tokens);
    }

    private static Dictionary<string, DefinitionStatus> BuildTokenIndex()
    {
        Dictionary<string, DefinitionStatus> index =
            new(Declared.Length, StringComparer.Ordinal);
        foreach ((string token, DefinitionStatus status) in Declared)
        {
            index.Add(token, status);
        }

        return index;
    }

    private static Dictionary<DefinitionStatus, string> BuildStatusIndex()
    {
        Dictionary<DefinitionStatus, string> index = new(Declared.Length);
        foreach ((string token, DefinitionStatus status) in Declared)
        {
            index.Add(status, token);
        }

        return index;
    }
}
