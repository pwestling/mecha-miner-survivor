using System;
using System.Globalization;

namespace MechaMiner.Simulation.Events;

/// <summary>
/// An event's typed payload: a schema version plus the integer, scalar, and content-reference
/// components its <see cref="EventKind"/> interprets.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Domain and presentation events: events carry a
/// "typed payload", and "Event schemas are versioned when written to diagnostic artifacts."
/// </para>
/// <para>
/// <b>What types it.</b> The pair of the event's <see cref="EventKind"/> and this payload's
/// <see cref="SchemaVersion"/>. That pairing is what lets a later gameplay package add a kind
/// without editing a file here, which is the same reason <see cref="EventKind"/> is not an
/// enumeration; and it is what lets a recorded diagnostic artifact be read back by a build that
/// interprets a different version, which doc 20 requires.
/// </para>
/// <para>
/// <b>Why the components are fixed rather than an object.</b> doc 115 § Cross-boundary contract
/// registry: "Cross-boundary payloads never expose mutable collections", and
/// <c>docs/technical/22-combat-and-weapon-runtime.md</c> § Performance and capacity requires
/// "zero steady-state managed allocation" in the combat path that emits most events. A boxed
/// payload would allocate once per event; three inline components do not.
/// </para>
/// <para>
/// Every component is required. There is no factory that defaults one, so an event cannot be
/// emitted with a payload field nobody set - which is what <c>VER-SIM-006-004</c> means by "an
/// event missing any required field cannot be constructed rather than being emitted with a
/// default".
/// </para>
/// </remarks>
public readonly struct EventPayload : IEquatable<EventPayload>
{
    /// <summary>
    /// The content ID an event with no content reference carries, so "no content" is explicit
    /// rather than blank.
    /// </summary>
    /// <remarks>
    /// doc 20 § Numeric and unit conventions makes the analogous rule for direction: "zero
    /// direction is explicit". A blank string would be indistinguishable from a field nobody
    /// filled in.
    /// </remarks>
    public const string NoContentId = "none";

    /// <summary>The schema version of the very first payload shape.</summary>
    public const int InitialSchemaVersion = 1;

    private readonly int _schemaVersion;
    private readonly long _quantity;
    private readonly double _magnitude;
    private readonly string? _contentId;

    private EventPayload(int schemaVersion, long quantity, double magnitude, string contentId)
    {
        _schemaVersion = schemaVersion;
        _quantity = quantity;
        _magnitude = magnitude;
        _contentId = contentId;
    }

    /// <summary>The payload schema version, which together with the event kind types the payload.</summary>
    public int SchemaVersion => _schemaVersion;

    /// <summary>
    /// The integer component: Hull, currency, ranks, counts.
    /// </summary>
    /// <remarks>
    /// doc 20 § Numeric and unit conventions represents "Hull, Armor, resources, ranks, counts"
    /// as integers, so the integer component is a <see cref="long"/> and never a rounded double.
    /// </remarks>
    public long Quantity => _quantity;

    /// <summary>
    /// The scalar component: normalized progress, a multiplier, or a derived stat.
    /// </summary>
    /// <remarks>
    /// Double precision, per doc 20 § Numeric and unit conventions for "accumulated schedules,
    /// cooldown phase, extraction work, and derived stat calculations".
    /// </remarks>
    public double Magnitude => _magnitude;

    /// <summary>
    /// The content reference, or <see cref="NoContentId"/>.
    /// </summary>
    /// <remarks>
    /// doc 20 § Scope and invariants: "every content reference resolves through the immutable run
    /// content registry". This carries the stable content ID, never a resolved object.
    /// </remarks>
    public string ContentId => _contentId ?? string.Empty;

    /// <summary>True when this payload was constructed rather than defaulted.</summary>
    public bool IsTyped => _schemaVersion > 0 && ContentId.Length > 0;

    /// <summary>Constructs a payload. Every component is required.</summary>
    /// <param name="schemaVersion">The payload schema version. Must be positive.</param>
    /// <param name="quantity">The integer component.</param>
    /// <param name="magnitude">The scalar component. Must be a finite number.</param>
    /// <param name="contentId">The content reference, or <see cref="NoContentId"/>. Must not be blank.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="schemaVersion"/> is not positive, or <paramref name="magnitude"/> is not finite.</exception>
    /// <exception cref="ArgumentException"><paramref name="contentId"/> is blank.</exception>
    public static EventPayload Typed(int schemaVersion, long quantity, double magnitude, string contentId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentId);
        if (!double.IsFinite(magnitude))
        {
            throw new ArgumentOutOfRangeException(
                nameof(magnitude),
                magnitude,
                "a payload magnitude must be finite; a NaN or infinity would propagate into a "
                    + "HUD view model and into a diagnostic artifact");
        }

        return new EventPayload(schemaVersion, quantity, magnitude, contentId);
    }

    /// <summary>Compares two payloads for exact equality of every component.</summary>
    public static bool operator ==(EventPayload left, EventPayload right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two payloads for inequality.</summary>
    public static bool operator !=(EventPayload left, EventPayload right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(EventPayload other)
    {
        return _schemaVersion == other._schemaVersion
            && _quantity == other._quantity
            && _magnitude.Equals(other._magnitude)
            && string.Equals(ContentId, other.ContentId, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is EventPayload other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _schemaVersion,
            _quantity,
            _magnitude,
            StringComparer.Ordinal.GetHashCode(ContentId));
    }

    /// <summary>Renders the payload as canonical invariant text.</summary>
    public override string ToString()
    {
        return "v"
            + _schemaVersion.ToString(CultureInfo.InvariantCulture)
            + " quantity="
            + _quantity.ToString(CultureInfo.InvariantCulture)
            + " magnitude="
            + _magnitude.ToString("R", CultureInfo.InvariantCulture)
            + " content="
            + ContentId;
    }
}
