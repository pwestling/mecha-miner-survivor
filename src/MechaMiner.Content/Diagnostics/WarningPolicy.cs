using System;

namespace MechaMiner.Content.Diagnostics;

/// <summary>
/// The owner and expiry that make a content warning legitimate rather than a
/// permanent exception.
/// </summary>
/// <remarks>
/// <c>docs/technical/40-content-data-and-validation.md</c> § Compilation pipeline:
/// "Warnings have an owner and expiration; release builds treat unresolved content
/// warnings as errors unless allowlisted with rationale." A warning with no owner is
/// nobody's job and a warning with no expiry never becomes urgent, so this type
/// cannot be constructed without both. That is the same discipline
/// <c>tests/shared/Tolerance.cs</c> applies to a tolerance, and for the same reason.
/// </remarks>
public sealed class WarningPolicy
{
    /// <summary>Declares who owns a warning and when it expires.</summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="owner"/> or <paramref name="rationale"/> is blank.
    /// </exception>
    public WarningPolicy(string owner, DateOnly expiresOn, string rationale)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException(
                "a content warning names its owner; doc 40 § Compilation pipeline: \"Warnings "
                    + "have an owner and expiration\"",
                nameof(owner));
        }

        if (string.IsNullOrWhiteSpace(rationale))
        {
            throw new ArgumentException(
                "a content warning states why it is tolerated, so a later reader can tell a "
                    + "scheduled remediation from a silently accepted defect",
                nameof(rationale));
        }

        Owner = owner;
        ExpiresOn = expiresOn;
        Rationale = rationale;
    }

    /// <summary>The work package or role accountable for resolving the warning.</summary>
    public string Owner { get; }

    /// <summary>The date after which the warning is overdue.</summary>
    public DateOnly ExpiresOn { get; }

    /// <summary>Why the warning is tolerated until then.</summary>
    public string Rationale { get; }

    /// <summary>True when <paramref name="asOf"/> is strictly after <see cref="ExpiresOn"/>.</summary>
    public bool IsExpired(DateOnly asOf)
    {
        return asOf > ExpiresOn;
    }
}
