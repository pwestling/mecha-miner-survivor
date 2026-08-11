using System.Collections.Generic;

namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// An implementation of doc 20 § Authoritative random-number contract that the shared
/// golden-vector renderer can drive.
/// </summary>
/// <remarks>
/// <para>
/// The renderer owns the text layout so the two implementations cannot disagree about
/// formatting instead of about arithmetic; each engine owns every number.
/// </para>
/// <para>
/// <c>docs/technical/91-verification-strategy.md</c> § Reference models requires deliberately
/// simple independent logic wherever one implementation could repeat its own bug. This seam is
/// how <c>VER-SIM-005-004</c> gets that: the committed vectors, the production types, and a
/// second implementation written from doc 20 alone all have to agree, and the gate fails if any
/// one of them changes alone.
/// </para>
/// </remarks>
internal interface IRandomVectorEngine
{
    /// <summary>A short name for this engine, used in assertion messages.</summary>
    string Name { get; }

    /// <summary>
    /// This engine's own copy of the registered family table of doc 20 § Authoritative
    /// random-number contract, in ascending key order.
    /// </summary>
    /// <remarks>
    /// Duplicated per engine on purpose: the independence golden pins the family table itself,
    /// so transcribing it twice is what makes a mistyped key or a missing row a red test rather
    /// than a shared misreading.
    /// </remarks>
    IReadOnlyList<KeyValuePair<ushort, string>> Families { get; }

    /// <summary>Runs the four-step derivation chain of doc 20 § Authoritative random-number
    /// contract.</summary>
    /// <param name="masterSeed">The deployment master seed.</param>
    /// <param name="familyKey">A registered family key.</param>
    /// <param name="instanceKey">The instance key.</param>
    /// <returns>All four intermediate values.</returns>
    RandomDerivationVector Derive(ulong masterSeed, ushort familyKey, ulong instanceKey);

    /// <summary>Derives and initializes one stream per the derivation chain of doc 20 §
    /// Authoritative random-number contract and the initialization rule of doc 20 §
    /// Authoritative random-number contract.</summary>
    /// <param name="masterSeed">The deployment master seed.</param>
    /// <param name="familyKey">A registered family key.</param>
    /// <param name="instanceKey">The instance key.</param>
    /// <returns>A primed stream whose next draw is its first caller-visible value.</returns>
    IRandomVectorStream OpenStream(ulong masterSeed, ushort familyKey, ulong instanceKey);
}
