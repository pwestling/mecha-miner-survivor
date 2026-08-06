// Deliberately invalid: proves Nullable=enable is on and warnings are errors.
// Expected: error CS8600 (VER-FND-001-006). Never compiled by MechaMiner.sln.

namespace MechaMiner.PolicyFixtures.NullableChecking;

internal static class NullablePolicyFixture
{
    internal static int LengthOfNull()
    {
        string value = null;
        return value.Length;
    }
}
