using System;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// A registered family key plus an instance key, validated against that family's registered
/// instance-key rule.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// derivation chain makes the instance key an input to the state-seed derivation, and the
/// family table of doc 20 § Authoritative random-number contract registers what each family's
/// instance key means.
/// </para>
/// <para>
/// Validation happens here rather than at the derivation, so an unregistered family key or an
/// instance key the family does not admit cannot reach the generator at all
/// (<c>VER-SIM-005-010</c>, <c>VER-SIM-005-012</c>).
/// </para>
/// </remarks>
public readonly struct RandomStreamKey : IEquatable<RandomStreamKey>
{
    private RandomStreamKey(RandomStreamFamily family, ulong instanceKey)
    {
        this.Family = family;
        this.InstanceKey = instanceKey;
    }

    /// <summary>The registered family this stream belongs to.</summary>
    public RandomStreamFamily Family { get; }

    /// <summary>The instance key that separates streams within the family.</summary>
    public ulong InstanceKey { get; }

    /// <summary>The registered family key.</summary>
    public ushort FamilyKey => this.Family.Key;

    /// <summary>Creates a validated stream key from a registered family key.</summary>
    /// <param name="familyKey">One of the 23 registered family keys of doc 20 § Authoritative
    /// random-number contract.</param>
    /// <param name="instanceKey">The instance key, checked against the family's rule.</param>
    /// <returns>The validated key.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="familyKey"/> is unregistered, or <paramref name="instanceKey"/> is not a
    /// value the family's registered rule admits.
    /// </exception>
    public static RandomStreamKey Create(ushort familyKey, ulong instanceKey)
    {
        return Create(RandomStreamFamilies.Get(familyKey), instanceKey);
    }

    /// <summary>Creates a validated stream key from a registered family.</summary>
    /// <param name="family">A family obtained from <see cref="RandomStreamFamilies"/>.</param>
    /// <param name="instanceKey">The instance key, checked against the family's rule.</param>
    /// <returns>The validated key.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="family"/> is not registered, or <paramref name="instanceKey"/> is not a
    /// value the family's registered rule admits.
    /// </exception>
    public static RandomStreamKey Create(RandomStreamFamily family, ulong instanceKey)
    {
        if (!family.IsRegistered)
        {
            throw new ArgumentOutOfRangeException(
                nameof(family),
                family.Key,
                "a stream key is built from a registered family (doc 20 § Authoritative random-number contract), never from a default value");
        }

        if (!family.AllowsInstanceKey(instanceKey))
        {
            throw new ArgumentOutOfRangeException(
                nameof(instanceKey),
                instanceKey,
                "family " + family.ToString() + " registers its instance key as "
                    + family.InstanceKeyRule.ToString()
                    + " (doc 20 § Authoritative random-number contract), which does not admit 0x"
                    + instanceKey.ToString("X16", CultureInfo.InvariantCulture)
                    + ". Deriving it would create an unregistered stream no golden vector pins");
        }

        return new RandomStreamKey(family, instanceKey);
    }

    /// <summary>Compares two keys for equality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when both name the same stream.</returns>
    public static bool operator ==(RandomStreamKey left, RandomStreamKey right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two keys for inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when the keys name different streams.</returns>
    public static bool operator !=(RandomStreamKey left, RandomStreamKey right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(RandomStreamKey other)
    {
        return this.Family.Key == other.Family.Key && this.InstanceKey == other.InstanceKey;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is RandomStreamKey other && this.Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(this.Family.Key, this.InstanceKey);
    }

    /// <summary>Renders the key as its hex family key and hex instance key.</summary>
    /// <returns>For example <c>0x0220/0x0000000000000003</c>.</returns>
    public override string ToString()
    {
        return "0x" + this.Family.Key.ToString("X4", CultureInfo.InvariantCulture)
            + "/0x" + this.InstanceKey.ToString("X16", CultureInfo.InvariantCulture);
    }
}
