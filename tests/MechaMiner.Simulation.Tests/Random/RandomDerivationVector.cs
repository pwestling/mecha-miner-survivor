namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// The four intermediate values of doc 20 § Authoritative random-number contract's derivation
/// chain for one (master seed, family key, instance key) triple.
/// </summary>
/// <remarks>
/// All four are carried because
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/random-seed-derivation.txt</c> pins all four. A
/// chain that only reported its final selector could hide a compensating pair of errors in the
/// two middle steps.
/// </remarks>
internal readonly struct RandomDerivationVector
{
    /// <summary>Creates a derivation vector.</summary>
    /// <param name="d0">Step one, from the master seed and schema version.</param>
    /// <param name="d1">Step two, from <paramref name="d0"/> and the family key.</param>
    /// <param name="stateSeed">Step three, from <paramref name="d1"/> and the instance
    /// key.</param>
    /// <param name="selector">Step four, from <paramref name="stateSeed"/>.</param>
    internal RandomDerivationVector(ulong d0, ulong d1, ulong stateSeed, ulong selector)
    {
        this.D0 = d0;
        this.D1 = d1;
        this.StateSeed = stateSeed;
        this.Selector = selector;
    }

    /// <summary><c>d0 = Mix(master seed XOR (schema version ×
    /// 0xD1B54A32D192ED03))</c>.</summary>
    internal ulong D0 { get; }

    /// <summary><c>d1 = Mix(d0 XOR family key)</c>.</summary>
    internal ulong D1 { get; }

    /// <summary><c>state seed = Mix(d1 XOR (instance key × 0x9E3779B97F4A7C15))</c>.</summary>
    internal ulong StateSeed { get; }

    /// <summary><c>selector = Mix(state seed XOR 0x94D049BB133111EB)</c>.</summary>
    internal ulong Selector { get; }
}
