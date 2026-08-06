using System;
using System.Globalization;

namespace MechaMiner.Simulation.Random;

/// <summary>
/// One instantiated authoritative stream's recovery record: its schema version, its identity,
/// its state, and its odd increment.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract the
/// recovery rule: "Stream state and odd increment are included in run recovery for every
/// instantiated authoritative stream." Doc 20 § Authoritative random-number contract: changing
/// any operation "increments the random schema version and invalidates incompatible recovery
/// rather than silently changing a compatible run", which is why the version travels in the
/// record itself.
/// </para>
/// <para>
/// The increment is carried rather than re-derived. Re-deriving it would make recovery depend
/// on the derivation chain still producing the same selector, so a schema change would resume a
/// run onto a different stream instead of being detected — and a carried increment is what lets
/// <c>VER-SIM-005-013</c> prove the restored stream continues the identical sequence rather
/// than a re-derived look-alike.
/// </para>
/// <para>
/// The presentation-only family <c>0xF000</c> never appears here. Doc 20 § Authoritative
/// random-number contract: presentation variation is "never serialized into authoritative
/// state"; the presentation-isolation rule: "no presentation draw or state is read by
/// simulation".
/// </para>
/// </remarks>
public readonly struct RandomStreamRecoveryState : IEquatable<RandomStreamRecoveryState>
{
    /// <summary>Creates a recovery record.</summary>
    /// <param name="schemaVersion">The random schema version the stream was derived
    /// under.</param>
    /// <param name="familyKey">The registered family key.</param>
    /// <param name="instanceKey">The instance key.</param>
    /// <param name="state">The stream's 64-bit state.</param>
    /// <param name="increment">The stream's odd increment.</param>
    /// <exception cref="ArgumentException"><paramref name="increment"/> is even.</exception>
    public RandomStreamRecoveryState(
        RandomSchemaVersion schemaVersion,
        ushort familyKey,
        ulong instanceKey,
        ulong state,
        ulong increment)
    {
        if ((increment & 1UL) == 0UL)
        {
            throw new ArgumentException(
                "a recovered PCG32 increment is odd (doc 20 § Authoritative random-number contract); an even increment is not a stream this contract can produce",
                nameof(increment));
        }

        this.SchemaVersion = schemaVersion;
        this.FamilyKey = familyKey;
        this.InstanceKey = instanceKey;
        this.State = state;
        this.Increment = increment;
    }

    /// <summary>The random schema version this record was written under.</summary>
    public RandomSchemaVersion SchemaVersion { get; }

    /// <summary>The registered family key of doc 20 § Authoritative random-number
    /// contract.</summary>
    public ushort FamilyKey { get; }

    /// <summary>The instance key that separates streams within the family.</summary>
    public ulong InstanceKey { get; }

    /// <summary>The stream's 64-bit state (doc 20 § Authoritative random-number
    /// contract).</summary>
    public ulong State { get; }

    /// <summary>The stream's per-stream odd increment (doc 20 § Authoritative random-number
    /// contract).</summary>
    public ulong Increment { get; }

    /// <summary>Compares two records for equality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when every field matches.</returns>
    public static bool operator ==(RandomStreamRecoveryState left, RandomStreamRecoveryState right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two records for inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns><see langword="true"/> when any field differs.</returns>
    public static bool operator !=(RandomStreamRecoveryState left, RandomStreamRecoveryState right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(RandomStreamRecoveryState other)
    {
        return this.SchemaVersion == other.SchemaVersion
            && this.FamilyKey == other.FamilyKey
            && this.InstanceKey == other.InstanceKey
            && this.State == other.State
            && this.Increment == other.Increment;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is RandomStreamRecoveryState other && this.Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            this.SchemaVersion,
            this.FamilyKey,
            this.InstanceKey,
            this.State,
            this.Increment);
    }

    /// <summary>Renders the record as canonical reviewable text.</summary>
    /// <returns>Schema version, family key, instance key, state, and increment.</returns>
    public override string ToString()
    {
        return "v" + this.SchemaVersion.ToString()
            + " 0x" + this.FamilyKey.ToString("X4", CultureInfo.InvariantCulture)
            + "/0x" + this.InstanceKey.ToString("X16", CultureInfo.InvariantCulture)
            + " state=0x" + this.State.ToString("X16", CultureInfo.InvariantCulture)
            + " increment=0x" + this.Increment.ToString("X16", CultureInfo.InvariantCulture);
    }
}
