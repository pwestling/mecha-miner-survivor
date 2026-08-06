// Deliberately invalid: proves AllowUnsafeBlocks=false, so unsafe code cannot be
// introduced without the isolated ownership, tests, and TDR that doc 100 requires.
// Expected: error CS0227 (VER-FND-001-010). Never compiled by MechaMiner.sln.

namespace MechaMiner.PolicyFixtures.UnsafeCode;

internal static class UnsafePolicyFixture
{
    internal static unsafe int FirstByte(byte[] data)
    {
        fixed (byte* pointer = data)
        {
            return *pointer;
        }
    }
}
