using System.Collections.Generic;
using System.IO;
using MechaMiner.Content.Codec;
using MechaMiner.Content.Diagnostics;
using MechaMiner.Content.Envelope;
using MechaMiner.Content.Ids;
using MechaMiner.Tests.Support;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// The DAT-001 fixture corpus: which file proves which diagnostic, and under what
/// policy.
/// </summary>
/// <remarks>
/// <para>
/// The expectation table lives in C# rather than in a manifest file so that every
/// expected code is the <see cref="ContentDiagnosticCodes"/> constant itself. Renaming
/// a constant is then a compile error instead of a corpus that silently stops
/// asserting anything.
/// </para>
/// <para>
/// What the table cannot check is itself. Every gate iterates it, so a file in
/// <c>Fixtures/invalid/</c> with no row here runs no test at all, and a code proved by
/// two fixtures can quietly drop to one. <see cref="FixtureCorpusCoverageTests"/> checks
/// the table against the directory in both directions and states the code-to-fixtures
/// roster independently of this table.
/// </para>
/// <para>
/// Verification: <c>VER-DAT-001-001</c> through <c>VER-DAT-001-007</c>,
/// <c>VER-DAT-001-012</c>, <c>VER-DAT-001-014</c> through <c>VER-DAT-001-019</c>,
/// <c>VER-DAT-001-022</c>, <c>VER-DAT-001-024</c>, <c>VER-DAT-001-028</c>.
/// </para>
/// </remarks>
internal static class FixtureCorpus
{
    /// <summary>
    /// A registry with one tombstone, so the retired-ID gate has something to catch.
    /// Nothing has actually shipped, so <c>RetiredIdRegistry.Shipped</c> is empty and a
    /// test-local registry is the only way to exercise the rule.
    /// </summary>
    internal static RetiredIdRegistry TestRetiredIds { get; } = new(new[]
    {
        new RetiredId(
            "W-EF",
            ContentCategory.Weapon,
            retiredInContentVersion: 4,
            replacedBy: "W-AB",
            rationale: "fixture tombstone: proves a retired ID cannot be reused"),
    });

    /// <summary>The absolute path of the <c>Fixtures</c> directory.</summary>
    internal static string Root { get; } =
        Path.Combine(TestArtifacts.TestProjectDirectory, "Fixtures");

    /// <summary>Every valid fixture. These must produce zero diagnostics.</summary>
    internal static IReadOnlyList<ValidFixture> Valid { get; } = new[]
    {
        new ValidFixture("valid/envelope-minimal.json", ContentCategory.Weapon),
        new ValidFixture("valid/envelope-maximal.json", ContentCategory.Weapon),
        new ValidFixture("valid/envelope-optionals-omitted.json", ContentCategory.Weapon),
        new ValidFixture("valid/envelope-aggregate.json", ContentCategory.Encounter),
    };

    /// <summary>
    /// Every invalid fixture, with the one diagnostic code it must provoke.
    /// </summary>
    /// <remarks>
    /// The <c>limit-*</c> entries carry their own reduced limits. A fixture that had to
    /// exceed the shipped one-megabyte ceiling would be a megabyte of generated JSON
    /// that no reviewer would read; the shipped defaults are asserted separately, and
    /// the at-limit/over-limit boundary is exercised in
    /// <c>StrictJsonLimitsTests</c> with documents built in code.
    /// </remarks>
    internal static IReadOnlyList<InvalidFixture> Invalid { get; } = new[]
    {
        // --- codec ---------------------------------------------------------
        Bad("codec-comment.json", ContentDiagnosticCodes.Comment),
        Bad("codec-trailing-comma.json", ContentDiagnosticCodes.TrailingComma),
        Bad("codec-duplicate-property.json", ContentDiagnosticCodes.DuplicateProperty),
        Bad("codec-nonfinite-nan.json", ContentDiagnosticCodes.NonfiniteNumber),
        Bad("codec-nonfinite-overflow.json", ContentDiagnosticCodes.NonfiniteNumber),
        Bad("codec-null-value.json", ContentDiagnosticCodes.NullValue),
        Bad("codec-null-nested.json", ContentDiagnosticCodes.NullValue),
        Bad("codec-camel-case-property.json", ContentDiagnosticCodes.PropertyNameNotSnakeCase),
        Bad("codec-malformed.json", ContentDiagnosticCodes.MalformedJson),
        Bad("codec-root-not-object.json", ContentDiagnosticCodes.RootNotObject),

        // --- limits, each under a policy reduced to make this file cross one --
        Bad("limit-document-bytes.json", ContentDiagnosticCodes.DocumentTooLarge,
            limits: Limits(documentBytes: 64)),
        Bad("limit-depth.json", ContentDiagnosticCodes.DepthLimitExceeded,
            limits: Limits(depth: 2)),
        Bad("limit-object-properties.json", ContentDiagnosticCodes.ObjectPropertyLimitExceeded,
            limits: Limits(objectProperties: 7)),
        Bad("limit-array-elements.json", ContentDiagnosticCodes.ArrayElementLimitExceeded,
            limits: Limits(arrayElements: 3)),
        Bad("limit-node-count.json", ContentDiagnosticCodes.NodeCountLimitExceeded,
            limits: Limits(nodeCount: 7)),
        Bad("limit-string-length.json", ContentDiagnosticCodes.StringTooLong,
            limits: Limits(stringLength: 32)),

        // --- structural ----------------------------------------------------
        Bad("structural-unknown-field.json", ContentDiagnosticCodes.UnknownField),
        Bad("structural-missing-required.json", ContentDiagnosticCodes.RequiredFieldMissing),
        Bad("structural-wrong-type.json", ContentDiagnosticCodes.FieldTypeMismatch),
        Bad("structural-unknown-status.json", ContentDiagnosticCodes.UnknownStatus),
        Bad("version-non-integer.json", ContentDiagnosticCodes.VersionNotPositiveInteger),
        Bad("version-nonpositive.json", ContentDiagnosticCodes.VersionNotPositiveInteger),
        Bad("structural-tag-outside-vocabulary.json", ContentDiagnosticCodes.TagOutsideVocabulary),
        Bad("structural-name-key-literal-text.json", ContentDiagnosticCodes.LocalizationKeyMalformed),
        Bad("structural-name-key-role-mismatch.json", ContentDiagnosticCodes.LocalizationKeyRoleMismatch),
        Bad("structural-empty-optional.json", ContentDiagnosticCodes.EmptyOptionalField),

        // --- identity ------------------------------------------------------
        Bad("identity-bad-id-for-category.json", ContentDiagnosticCodes.IdMalformedForCategory),
        Bad("identity-retired-id-reused.json", ContentDiagnosticCodes.RetiredIdReused),

        // --- traceability --------------------------------------------------
        Bad("traceability-source-ref-malformed.json", ContentDiagnosticCodes.SourceRefMalformed),
        Bad("traceability-source-ref-path-line.json", ContentDiagnosticCodes.SourceRefPathLine),
        Bad("traceability-scope-unresolved.json", ContentDiagnosticCodes.SourceRefScopeUnresolved),
    };

    /// <summary>Reads a fixture's bytes.</summary>
    internal static byte[] Read(string relativePath)
    {
        return File.ReadAllBytes(Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    /// <summary>The repository-relative path a diagnostic reports for a fixture.</summary>
    internal static string SourcePathOf(string relativePath)
    {
        return TestArtifacts.Relative(
            Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static InvalidFixture Bad(
        string name,
        string expectedCode,
        ContentCategory category = ContentCategory.Weapon,
        StrictJsonLimits? limits = null)
    {
        return new InvalidFixture("invalid/" + name, expectedCode, category, limits);
    }

    /// <summary>
    /// The shipped defaults with exactly one ceiling lowered, so a fixture crosses the
    /// limit it is named for and no other.
    /// </summary>
    private static StrictJsonLimits Limits(
        int? documentBytes = null,
        int? depth = null,
        int? objectProperties = null,
        int? arrayElements = null,
        int? nodeCount = null,
        int? stringLength = null)
    {
        return new StrictJsonLimits(
            documentBytes ?? StrictJsonLimits.DefaultMaximumDocumentBytes,
            depth ?? StrictJsonLimits.DefaultMaximumDepth,
            objectProperties ?? StrictJsonLimits.DefaultMaximumObjectProperties,
            arrayElements ?? StrictJsonLimits.DefaultMaximumArrayElements,
            nodeCount ?? StrictJsonLimits.DefaultMaximumNodeCount,
            stringLength ?? StrictJsonLimits.DefaultMaximumStringLength);
    }

    /// <summary>A fixture that must validate cleanly.</summary>
    internal sealed class ValidFixture
    {
        internal ValidFixture(string path, ContentCategory category)
        {
            Path = path;
            Category = category;
        }

        internal string Path { get; }

        internal ContentCategory Category { get; }

        internal EnvelopeReadContext Context()
        {
            return new EnvelopeReadContext(
                SourcePathOf(Path),
                Category,
                StrictJsonPolicy.Definitions,
                TestRetiredIds);
        }

        public override string ToString()
        {
            return Path;
        }
    }

    /// <summary>A fixture that must fail with exactly one named diagnostic code.</summary>
    internal sealed class InvalidFixture
    {
        internal InvalidFixture(
            string path,
            string expectedCode,
            ContentCategory category,
            StrictJsonLimits? limits)
        {
            Path = path;
            ExpectedCode = expectedCode;
            Category = category;
            Limits = limits;
        }

        internal string Path { get; }

        internal string ExpectedCode { get; }

        internal ContentCategory Category { get; }

        internal StrictJsonLimits? Limits { get; }

        internal EnvelopeReadContext Context()
        {
            StrictJsonPolicy policy = Limits is null
                ? StrictJsonPolicy.Definitions
                : StrictJsonPolicy.Definitions.WithLimits(Limits);

            return new EnvelopeReadContext(SourcePathOf(Path), Category, policy, TestRetiredIds);
        }

        public override string ToString()
        {
            return Path + " -> " + ExpectedCode;
        }
    }
}
