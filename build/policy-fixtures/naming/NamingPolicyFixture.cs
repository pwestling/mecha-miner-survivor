// Deliberately invalid: the private field does not use the required _camelCase
// naming style from .editorconfig, proving EnforceCodeStyleInBuild applies the
// naming policy during compilation.
// Expected: error IDE1006 (VER-FND-001-008). Never compiled by MechaMiner.sln.

namespace MechaMiner.PolicyFixtures.Naming;

internal sealed class NamingPolicyFixture
{
    private readonly int wronglyNamedField;

    internal NamingPolicyFixture(int seed)
    {
        wronglyNamedField = seed;
    }

    internal int Seed
    {
        get { return wronglyNamedField; }
    }
}
