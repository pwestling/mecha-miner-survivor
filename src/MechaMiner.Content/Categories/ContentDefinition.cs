using System;
using MechaMiner.Content.Envelope;

namespace MechaMiner.Content.Categories;

/// <summary>
/// The base of every typed, immutable category model: a validated envelope plus the
/// kind whose field table produced it.
/// </summary>
/// <remarks>
/// <para>
/// A model of this type only ever exists for a definition that passed every check its
/// kind declares. That is the point of separating it from the transport DTO: the DTO
/// has settable properties and nullable everything so that a malformed value becomes a
/// diagnostic rather than an exception, and nothing outside a reader ever sees one.
/// A consumer holding a <see cref="ContentDefinition"/> does not have to ask whether a
/// field was validated.
/// </para>
/// </remarks>
public abstract class ContentDefinition
{
    /// <summary>Creates a definition from a validated envelope.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="envelope"/> is null.</exception>
    protected ContentDefinition(DefinitionEnvelope envelope, DefinitionKind kind)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        Envelope = envelope;
        Kind = kind;
    }

    /// <summary>The validated common envelope.</summary>
    public DefinitionEnvelope Envelope { get; }

    /// <summary>The kind whose field table this definition satisfies.</summary>
    public DefinitionKind Kind { get; }

    /// <summary>The stable ID, as authored.</summary>
    public string Id => Envelope.Id.Value;

    /// <inheritdoc/>
    public override string ToString()
    {
        return Kind + " " + Id;
    }
}
