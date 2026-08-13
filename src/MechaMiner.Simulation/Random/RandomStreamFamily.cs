using System;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// One registered stream family: its family key, the name doc 20 registers it under, its
/// instance-key rule, and whether it is authoritative.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// family table is the registry; the key-registration rule: "New authoritative randomness
/// receives a unique registered family key in this table; keys are never repurposed. A category
/// retry or an added visual draw cannot consume another family's sequence."
/// </para>
/// <para>
/// This type is separate from <see cref="RandomStreamFamilies"/> because a bare set of key
/// constants cannot express "this family's instance key must be zero", which
/// <c>VER-SIM-005-012</c> asserts by requiring a nonzero instance key on a zero-key family to
/// be rejected. The value type carries the rule; the static class is the closed registry.
/// </para>
/// </remarks>
public readonly struct RandomStreamFamily : IEquatable<RandomStreamFamily>
{
    internal RandomStreamFamily(ushort key, string name, InstanceKeyRule instanceKeyRule, bool isAuthoritative)
    {
        this.Key = key;
        this.Name = name;
        this.InstanceKeyRule = instanceKeyRule;
        this.IsAuthoritative = isAuthoritative;
    }

    /// <summary>The registered family key of doc 20 § Authoritative random-number
    /// contract.</summary>
    public ushort Key { get; }

    /// <summary>The stream family name the family table of doc 20 § Authoritative random-number
    /// contract registers this key under.</summary>
    public string? Name { get; }

    /// <summary>What this family's instance key means, and which values it accepts.</summary>
    public InstanceKeyRule InstanceKeyRule { get; }

    /// <summary>
    /// Whether this family is authoritative and therefore included in run recovery.
    /// </summary>
    /// <remarks>
    /// False only for the presentation-only family <c>0xF000</c>. Doc 20 § Authoritative
    /// random-number contract: presentation variation is "never serialized into authoritative
    /// state", and the recovery rule of doc 20 § Authoritative random-number contract includes
    /// in recovery "every instantiated <em>authoritative</em> stream".
    /// </remarks>
    public bool IsAuthoritative { get; }

    /// <summary>
    /// Whether this value came from the registry rather than being the struct default.
    /// </summary>
    public bool IsRegistered => this.Name is not null;

    /// <summary>Compares two families for equality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when both are the same registered family.</returns>
    public static bool operator ==(RandomStreamFamily left, RandomStreamFamily right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two families for inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when the families differ.</returns>
    public static bool operator !=(RandomStreamFamily left, RandomStreamFamily right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Whether <paramref name="instanceKey"/> is a value this family's registered instance-key
    /// rule permits.
    /// </summary>
    /// <param name="instanceKey">The candidate instance key.</param>
    /// <returns><see langword="true"/> when the rule admits the value.</returns>
    public bool AllowsInstanceKey(ulong instanceKey)
    {
        switch (this.InstanceKeyRule)
        {
            case InstanceKeyRule.Zero:
                return instanceKey == 0UL;
            case InstanceKeyRule.MaterialOrdinal:
                return instanceKey <= 5UL;
            case InstanceKeyRule.BossIndex:
                return instanceKey <= 3UL;
            case InstanceKeyRule.WeaponSlotOrdinal:
                return instanceKey <= 3UL;
            default:
                return true;
        }
    }

    /// <inheritdoc/>
    public bool Equals(RandomStreamFamily other)
    {
        return this.Key == other.Key
            && this.InstanceKeyRule == other.InstanceKeyRule
            && this.IsAuthoritative == other.IsAuthoritative
            && string.Equals(this.Name, other.Name, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is RandomStreamFamily other && this.Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.Key;
    }

    /// <summary>Renders the family as its hex key and registered name.</summary>
    /// <returns>For example <c>0x0100 resource-profile selection</c>.</returns>
    public override string ToString()
    {
        return "0x" + this.Key.ToString("X4", CultureInfo.InvariantCulture)
            + " " + (this.Name ?? "<unregistered>");
    }
}
