// Deliberately invalid: uses the C# 13 escape-sequence feature, which the pinned
// LangVersion 12.0 rejects. Proves the language version is pinned rather than
// preview or latest.
// Expected: error CS9202 (VER-FND-001-009). Never compiled by MechaMiner.sln.

namespace MechaMiner.PolicyFixtures.LanguageVersion;

internal static class LangVersionPolicyFixture
{
    internal const string Reset = "\e[0m";
}
