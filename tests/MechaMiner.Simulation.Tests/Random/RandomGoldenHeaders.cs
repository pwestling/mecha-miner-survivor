namespace MechaMiner.Simulation.Tests.Random;

/// <summary>
/// The verbatim <c>#</c> header block of each committed golden vector file.
/// </summary>
/// <remarks>
/// <para>
/// <c>MechaMiner.Tests.Support.GoldenText</c> compares a whole file, so a renderer that
/// emitted only data rows would leave every header line unpinned - including the two
/// decisions doc 20:49-91 leaves open, which
/// <c>tests/MechaMiner.Simulation.Tests/Goldens/README.md</c> records in those headers
/// rather than in code comments. Holding the prose here instead of reading it back out of
/// the file under test is deliberate: reading it would make the header self-satisfying,
/// so a header edit could never fail a gate.
/// </para>
/// <para>
/// These strings are therefore not documentation of the implementation. They are the
/// committed provenance of vectors produced by an independent pure-Python reference before
/// any C# existed (doc 91 § Reference models), and they are reproduced byte for byte so
/// that editing a golden's authority, schema version, or fixed decision is a red test.
/// </para>
/// </remarks>
internal static class RandomGoldenHeaders
{
    /// <summary>The header of <c>random-seed-derivation.txt</c>: the d0/d1/state-seed/selector chain of doc 20:53.</summary>
    internal const string SeedDerivation = """
        # authority: docs/technical/20-simulation-core.md § Authoritative random-number contract
        # random schema version: 1
        # derived by: an independent pure-Python PCG-XSH-RR 64/32 + SplitMix64 reference,
        #   not by the C# implementation under test. See Goldens/README.md.
        #
        # Seed derivation, doc 20:53. For each (masterSeed, familyKey, instanceKey):
        #   d0        = Mix(masterSeed XOR (1 * 0xD1B54A32D192ED03))
        #   d1        = Mix(d0 XOR familyKey)
        #   stateSeed = Mix(d1 XOR (instanceKey * 0x9E3779B97F4A7C15))
        #   selector  = Mix(stateSeed XOR 0x94D049BB133111EB)
        # All arithmetic wraps modulo 2^64. Mix is SplitMix64 next().
        #
        # Edge cases included: masterSeed 0, masterSeed 0xFFFFFFFFFFFFFFFF,
        # instanceKey 0, and nonzero instanceKey (including all-ones).
        #
        # columns (tab separated): masterSeed, familyKey, instanceKey, d0, d1, stateSeed, selector
        # entries=9
        """ + "\n";

    /// <summary>The header of <c>random-stream-initialization.txt</c>: the increment, primed state, and first 16 outputs of doc 20:51 and doc 20:55.</summary>
    internal const string StreamInitialization = """
        # authority: docs/technical/20-simulation-core.md § Authoritative random-number contract
        # random schema version: 1
        # derived by: an independent pure-Python PCG-XSH-RR 64/32 + SplitMix64 reference,
        #   not by the C# implementation under test. See Goldens/README.md.
        #
        # Initialization and first outputs, doc 20:51 and doc 20:55.
        # Initialization: state zero; increment (selector << 1) | 1; advance once;
        # add stateSeed to state modulo 2^64; advance once again. Both priming outputs
        # are discarded and are NOT counted as caller-visible draws. 'primed-state' is
        # the state after the second priming advance, i.e. the state the first
        # caller-visible draw reads.
        #
        # Output transformation reads the state BEFORE the advance:
        #   xorshifted = (uint32)(((prior >> 18) XOR prior) >> 27)
        #   rot        = (int)(prior >> 59)
        #   result     = rotate-right(xorshifted, rot)
        #
        # The last two streams differ only in instanceKey (3 vs 4); their outputs must
        # share no prefix, which is what makes instance separation visible.
        #
        # columns: 'output', zero-based index, value as hex uint32
        # streams=5
        """ + "\n";

    /// <summary>The header of <c>random-bounded-conversion.txt</c>: the rejection-sampled bounded integers of doc 20:86.</summary>
    internal const string BoundedConversion = """
        # authority: docs/technical/20-simulation-core.md § Authoritative random-number contract
        # random schema version: 1
        # derived by: an independent pure-Python PCG-XSH-RR 64/32 + SplitMix64 reference,
        #   not by the C# implementation under test. See Goldens/README.md.
        #
        # Bounded integer conversion, doc 20:86: "Unbiased bounded integers use
        # rejection sampling rather than modulo reduction."
        #
        # DECISION fixed here (doc 20:86 mandates rejection sampling but does not state
        # the threshold): threshold = (2^32 - bound) mod bound. A draw r is REJECTED
        # when r < threshold; otherwise the result is r mod bound. Exactly 2^32 -
        # threshold values are accepted and that count is an exact multiple of bound, so
        # every residue is produced by an equal number of accepted draws: unbiased.
        # This is pcg32_boundedrand_r from the PCG reference C implementation.
        #
        # bound 1 yields threshold 0, so the first draw is always accepted and ONE draw
        # is consumed. The no-draw rule of doc 20:89 is a property of selection, not of
        # this primitive; see random-degenerate-selection.txt.
        #
        # 'draws' is the number of 32-bit draws consumed to produce that one result, so
        # rejection behaviour is pinned and not merely the accepted outputs. bound
        # 0xC0000000 rejects one draw in four and exists to make draws>1 observable.
        #
        # Each block restarts a fresh stream from
        #   master=0x0123456789ABCDEF family=0x0100 instance=0x0000000000000000
        # so every block is independently reproducible.
        #
        # columns: 'result', zero-based index, value (decimal), draws consumed
        # blocks=10
        """ + "\n";

    /// <summary>The header of <c>random-unit-double-conversion.txt</c>: the 53-bit [0,1) conversion of doc 20:87.</summary>
    internal const string UnitDoubleConversion = """
        # authority: docs/technical/20-simulation-core.md § Authoritative random-number contract
        # random schema version: 1
        # derived by: an independent pure-Python PCG-XSH-RR 64/32 + SplitMix64 reference,
        #   not by the C# implementation under test. See Goldens/README.md.
        #
        # Unit-interval double conversion, doc 20:87: "A [0,1) double is built from 53
        # random bits under one golden-tested conversion."
        #
        # DECISION fixed here (doc 20:87 does not state the bit layout): two 32-bit
        # draws are concatenated with the FIRST draw as the HIGH half, and the top 53
        # bits of that 64-bit value form the mantissa:
        #   hi    = next_uint32()            first draw
        #   lo    = next_uint32()            second draw
        #   bits  = ((ulong)hi << 32) | lo
        #   m53   = bits >> 11               top 53 bits; the low 11 bits are discarded
        #   value = m53 * (1.0 / 9007199254740992.0)     9007199254740992 = 2^53
        # Chosen over the MT19937 genrand_res53 27/26 split because (x >> 11) * 2^-53 is
        # the dominant published construction for a [0,1) double from a 64-bit source,
        # it is one obvious C# expression, and "the first draw is the more significant
        # half" is the natural reading of draw order. Each value consumes exactly two
        # draws.
        #
        # The comparison authority for a value is 'ieee754', the raw bit pattern of the
        # double (BitConverter.DoubleToUInt64Bits), so the conversion is pinned
        # independently of any double-to-text formatting. 'decimal' is the shortest
        # round-trippable rendering (C# ToString("R", InvariantCulture) on .NET Core,
        # which equals Python repr) and is present for human review.
        #
        # Stream: master=0x0123456789ABCDEF family=0x0100 instance=0x0000000000000000
        #
        # columns: index, m53 (hex, 53 bits), ieee754 (hex bit pattern), decimal
        # entries=16
        """ + "\n";

    /// <summary>The header of <c>random-stream-independence.txt</c>: the 23 registered families of doc 20:57-81.</summary>
    internal const string StreamIndependence = """
        # authority: docs/technical/20-simulation-core.md § Authoritative random-number contract
        # random schema version: 1
        # derived by: an independent pure-Python PCG-XSH-RR 64/32 + SplitMix64 reference,
        #   not by the C# implementation under test. See Goldens/README.md.
        #
        # Stream independence, doc 20:57-81. One master seed across every registered
        # stream family, in canonical family-key order (ascending), instanceKey 0
        # uniformly. A family-key collision, a missing family, or an off-by-one in the
        # registry table is visible here as a duplicated or shifted first output.
        #
        # instanceKey is 0 for every row, including families whose production instance
        # key is a region/site/rock/slot identifier, so that this fixture isolates the
        # family key as the only varying input.
        #
        # Master seed: 0x0123456789ABCDEF
        #
        # columns: familyKey, stateSeed, selector, increment, primed-state,
        #   first output (hex uint32), stream family name
        # families=23
        """ + "\n";

    /// <summary>The header of <c>random-degenerate-selection.txt</c>: the zero-draw selection rule of doc 20:88-89.</summary>
    internal const string DegenerateSelection = """
        # authority: docs/technical/20-simulation-core.md § Authoritative random-number contract
        # random schema version: 1
        # derived by: an independent pure-Python PCG-XSH-RR 64/32 + SplitMix64 reference,
        #   not by the C# implementation under test. See Goldens/README.md.
        #
        # Degenerate selection, doc 20:88-89: "Selection from a collection first
        # establishes canonical candidate order, then draws an index." and "An
        # empty/singleton selection consumes no draw; this convention is fixture-
        # pinned." This file is that fixture.
        #
        # An empty or singleton selection short-circuits BEFORE any bounded draw, so it
        # consumes zero draws and leaves the stream state bit-identical. The two-
        # candidate and three-candidate cases are included as controls: they do consume
        # a draw and do change the state, which is what makes the zero-draw claim
        # meaningful rather than vacuous.
        #
        # Candidates are given as authored input order; 'ordered' is the canonical
        # candidate order actually selected from (ordinal ascending), establishing that
        # canonical order is applied before the index draw and that authored input order
        # does not reach the generator.
        #
        # Every case starts from a freshly initialized stream
        #   master=0x0123456789ABCDEF family=0x0205 instance=0x0000000000000000
        # so state-before is the primed state in every case.
        #
        # columns: 'case', candidate count, draws consumed, state-before, state-after,
        #   'unchanged'/'advanced', selected value
        # cases=6
        """ + "\n";
}
