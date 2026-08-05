// Deliberately invalid: proves the built-in .NET analyzers run at the pinned
// AnalysisLevel and their warnings are errors.
// Expected: error CA2200 (VER-FND-001-007). Never compiled by MechaMiner.sln.

using System;

namespace MechaMiner.PolicyFixtures.Analyzers;

internal static class AnalyzerPolicyFixture
{
    internal static void RethrowLosingStackDetails()
    {
        try
        {
            throw new InvalidOperationException("policy fixture");
        }
        catch (InvalidOperationException error)
        {
            throw error;
        }
    }
}
