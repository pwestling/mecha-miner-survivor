// Valid on purpose. This is the one fixture that must compile: the policy under
// test is reproducibility, so verify-policies.sh builds it twice and compares
// SHA-256 of the produced assembly, then repeats with Deterministic=false as a
// negative control.
// Verification: VER-FND-001-011. Never compiled by MechaMiner.sln.

namespace MechaMiner.PolicyFixtures.DeterministicBuild;

internal static class DeterministicPolicyFixture
{
    internal static int Answer()
    {
        return 42;
    }
}
