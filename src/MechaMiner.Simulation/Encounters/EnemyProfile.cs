using System;
using System.Globalization;

namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// One ordinary enemy identity's fixed profile: the whole of what an ordinary pursuer is.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/31-initial-alien-roster.md</c> § Shared ordinary-enemy rules:24 - "Every identity uses
/// one fixed base profile throughout the standard run. Reappearance later does not invisibly scale
/// it." That sentence is why this is an immutable value read from a static roster rather than a
/// mutable record an instance owns: an instance that could carry its own multiplied Hull is an
/// instance that can be scaled, and the rule would then be a convention rather than a property of
/// the type.
/// </para>
/// <para>
/// The elite treatment of doc 31 § Elite treatment is deliberately absent. Elites are
/// <c>ENC-005</c>, they are a separate population category
/// (<c>MechaMiner.Simulation.Entities.PopulationCategory.Elite</c>), and a profile carrying a
/// nullable elite multiplier would be a half-built version of that package sitting where the
/// ordinary rule belongs.
/// </para>
/// </remarks>
public readonly struct EnemyProfile : IEquatable<EnemyProfile>
{
    private readonly string? _contentId;
    private readonly int _maximumHull;
    private readonly int _contactDamage;
    private readonly double _movementShare;
    private readonly double _bodyScale;

    private EnemyProfile(
        string contentId,
        int maximumHull,
        int contactDamage,
        double movementShare,
        double bodyScale)
    {
        _contentId = contentId;
        _maximumHull = maximumHull;
        _contactDamage = contactDamage;
        _movementShare = movementShare;
        _bodyScale = bodyScale;
    }

    /// <summary>The accepted gameplay ID, for example <c>EN-01</c>.</summary>
    /// <remarks>
    /// <c>docs/technical/40-content-data-and-validation.md</c>:67 requires accepted gameplay IDs to
    /// be reused exactly, naming <c>EN-01</c> among them.
    /// </remarks>
    public string ContentId => _contentId ?? string.Empty;

    /// <summary>The identity's fixed maximum Hull.</summary>
    public int MaximumHull => _maximumHull;

    /// <summary>The contact damage one eligible contact instance deals to the mech.</summary>
    public int ContactDamage => _contactDamage;

    /// <summary>
    /// The identity's movement speed as a fraction of the unmodified mech speed.
    /// </summary>
    /// <remarks>
    /// doc 31 § Ordinary roster overview:35 - "Movement speed is shown as a percentage of the
    /// shared 3.0M/s unmodified mech movement speed." Stored as the fraction rather than the
    /// percentage so no call site has to remember to divide by a hundred.
    /// </remarks>
    public double MovementShare => _movementShare;

    /// <summary>
    /// The identity's body scale, which multiplies the Ripper's contact diameter.
    /// </summary>
    /// <remarks>
    /// doc 31 § Ordinary roster overview:35 - "Body scale multiplies the Ripper's 0.80M contact
    /// diameter, not its decorative mesh."
    /// </remarks>
    public double BodyScale => _bodyScale;

    /// <summary>Whether this profile was built from a roster row rather than defaulted.</summary>
    public bool IsAuthored => ContentId.Length > 0 && _maximumHull > 0;

    /// <summary>
    /// The identity's world speed in gameplay meters per second.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="MovementShare"/> and the shared baseline speed rather than stored,
    /// because doc 72 § Movement and Speed Modifiers:61-71 tabulates exactly these products and a
    /// second stored copy could disagree with the share it is supposed to be a product of. The
    /// Skitterling row of that table, 1.26 M/s, is what
    /// <c>EnemyRosterTests</c> checks this against.
    /// </remarks>
    public double WorldSpeedMetersPerSecond =>
        _movementShare * Player.PlayerBaseline.BaseMovementSpeedMetersPerSecond;

    /// <summary>
    /// The identity's contact footprint radius in gameplay meters.
    /// </summary>
    /// <remarks>
    /// <see cref="BodyScale"/> times <see cref="EnemyRoster.RipperContactDiameterMeters"/>, halved.
    /// doc 31:26 makes the footprint a contact test only: ordinary enemies "do not collide with the
    /// mech, one another, mining points, pickups, or other enemies".
    /// </remarks>
    public double ContactRadiusMeters =>
        (_bodyScale * EnemyRoster.RipperContactDiameterMeters) / 2.0;

    /// <summary>
    /// Builds a profile from one authored roster row.
    /// </summary>
    /// <param name="contentId">The accepted gameplay ID.</param>
    /// <param name="maximumHull">The identity's fixed maximum Hull. Must be positive.</param>
    /// <param name="contactDamage">The listed contact damage. Must be positive.</param>
    /// <param name="movementShare">The movement share as a fraction. Must be positive and finite.</param>
    /// <param name="bodyScale">The body scale as a multiplier. Must be positive and finite.</param>
    /// <exception cref="ArgumentException"><paramref name="contentId"/> is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A number is outside its domain.</exception>
    public static EnemyProfile Authored(
        string contentId,
        int maximumHull,
        int contactDamage,
        double movementShare,
        double bodyScale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumHull, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(contactDamage, 1);
        RequirePositiveFinite(movementShare, nameof(movementShare));
        RequirePositiveFinite(bodyScale, nameof(bodyScale));

        return new EnemyProfile(contentId, maximumHull, contactDamage, movementShare, bodyScale);
    }

    /// <inheritdoc/>
    public static bool operator ==(EnemyProfile left, EnemyProfile right)
    {
        return left.Equals(right);
    }

    /// <inheritdoc/>
    public static bool operator !=(EnemyProfile left, EnemyProfile right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(EnemyProfile other)
    {
        return string.Equals(ContentId, other.ContentId, StringComparison.Ordinal)
            && _maximumHull == other._maximumHull
            && _contactDamage == other._contactDamage
            && _movementShare.Equals(other._movementShare)
            && _bodyScale.Equals(other._bodyScale);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EnemyProfile other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.Ordinal.GetHashCode(ContentId),
            _maximumHull,
            _contactDamage,
            _movementShare,
            _bodyScale);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return ContentId
            + "(hull="
            + _maximumHull.ToString(CultureInfo.InvariantCulture)
            + ",contact="
            + _contactDamage.ToString(CultureInfo.InvariantCulture)
            + ",speed="
            + WorldSpeedMetersPerSecond.ToString("0.####", CultureInfo.InvariantCulture)
            + "m/s,r="
            + ContactRadiusMeters.ToString("0.####", CultureInfo.InvariantCulture)
            + "m)";
    }

    private static void RequirePositiveFinite(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "a roster multiplier is a finite positive number; doc 31 § Ordinary roster overview "
                    + "lists no zero or negative move share or body scale, so one here is an "
                    + "authoring defect rather than a degenerate enemy");
        }
    }
}
