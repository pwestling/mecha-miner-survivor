namespace MechaMiner.Simulation.Random;

/// <summary>
/// The 32-bit draw seam every authoritative consumer reads randomness through.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Authoritative random-number contract, the
/// injection rule: "Tests may inject a scripted source, but production content cannot select an
/// alternate algorithm."
/// </para>
/// <para>
/// That split is structural here, not documentary. The only production implementation is
/// private to <see cref="RandomStreamSet"/> and is handed out by
/// <see cref="RandomStreamSet.Source"/>, which derives its stream from the master seed, the
/// schema version, and a registered <see cref="RandomStreamKey"/> and takes no algorithm,
/// generator, or source argument anywhere in its surface (<c>VER-SIM-005-015</c>). A consumer
/// that wants determinism in a test is given a
/// <see cref="ScriptedRandomSource"/> instead; a consumer cannot ask the stream set for a
/// different algorithm because no such parameter exists.
/// </para>
/// <para>
/// A source is deliberately <em>not</em> a <see cref="Pcg32"/>: a mutable struct implementing
/// an interface would be boxed at every call site, and a boxed copy is a forked stream.
/// </para>
/// </remarks>
public interface IRandomSource
{
    /// <summary>
    /// How many caller-visible 32-bit draws this source has produced. Draw accounting is part
    /// of the seam because doc 20 § Authoritative random-number contract's no-draw rule and the
    /// bounded-conversion fixtures are stated in consumed draws.
    /// </summary>
    ulong DrawCount { get; }

    /// <summary>Produces the next 32-bit draw.</summary>
    /// <returns>The draw.</returns>
    uint NextUInt32();
}
