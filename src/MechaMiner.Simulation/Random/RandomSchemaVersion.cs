using System;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// The random schema version an authoritative stream was derived under, carried as a value
/// rather than referenced as a bare constant.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// derivation chain: "The current random schema version is <c>1</c>." Doc 20 § Authoritative
/// random-number contract: "Changing any operation increments the random schema version and
/// invalidates incompatible recovery rather than silently changing a compatible run."
/// </para>
/// <para>
/// A <c>const int</c> cannot be carried inside a persisted artifact or compared against one,
/// and <c>VER-SIM-005-014</c> requires a recovery artifact carrying a
/// <em>different</em> version to be rejected. The version therefore travels inside
/// <see cref="RandomStreamRecoveryState"/> as this type.
/// </para>
/// <para>
/// Public rather than internal because <c>game/</c>, <c>MechaMiner.Tools</c>, and the test
/// assemblies all consume this contract across the assembly boundary and there is no
/// <c>InternalsVisibleTo</c> in this repository.
/// </para>
/// </remarks>
public readonly struct RandomSchemaVersion : IEquatable<RandomSchemaVersion>
{
    /// <summary>The current random schema version (doc 20 § Authoritative random-number
    /// contract).</summary>
    public static readonly RandomSchemaVersion Current = new(1);

    /// <summary>Creates a schema version from its integer value.</summary>
    /// <param name="value">The version number. Versions start at one and only increase.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> is less than one. Version zero is the uninitialized default and
    /// is never a derived schema.
    /// </exception>
    public RandomSchemaVersion(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "a random schema version starts at 1 (doc 20 § Authoritative random-number contract); 0 is the uninitialized default");
        }

        this.Value = value;
    }

    /// <summary>The version number.</summary>
    public int Value { get; }

    /// <summary>
    /// Whether this value was constructed rather than left at the struct default.
    /// </summary>
    public bool IsSpecified => this.Value >= 1;

    /// <summary>Compares two versions for equality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when both carry the same version number.</returns>
    public static bool operator ==(RandomSchemaVersion left, RandomSchemaVersion right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two versions for inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when the version numbers differ.</returns>
    public static bool operator !=(RandomSchemaVersion left, RandomSchemaVersion right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(RandomSchemaVersion other)
    {
        return this.Value == other.Value;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is RandomSchemaVersion other && this.Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.Value;
    }

    /// <summary>Renders the version number in the invariant culture.</summary>
    /// <returns>The decimal version number.</returns>
    public override string ToString()
    {
        return this.Value.ToString(CultureInfo.InvariantCulture);
    }
}
