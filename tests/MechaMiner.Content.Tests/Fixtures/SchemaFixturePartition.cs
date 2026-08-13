using System;
using System.Collections.Generic;

namespace MechaMiner.Content.Tests.Fixtures;

/// <summary>
/// Sorts every file in a fixture directory into exactly one of two classes - claimed by a
/// verification-registry entry, or declared unclaimed on purpose - and reports every file
/// and every declaration that lands in neither.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a partition and not an orphan scan.</b> "Report the files nobody cites" has a
/// silencer built into it: the way to make the report go away is to add the file to
/// whatever list the scan consults, and afterwards nothing distinguishes a fixture that is
/// deliberately uncited from one somebody quietened. A partition asks the question the
/// other way round. Every file has to be classified, the classification is forced on
/// whoever adds the file, and the two classes are checked against each other rather than
/// one being an escape from the other.
/// </para>
/// <para>
/// <b>Why the caller states the claimed count as a literal.</b> Every finding below is of
/// the form "this list is empty", and the correlated deletion satisfies all of them at
/// once: delete a fixture and its registry citation in the same change and the corpus is
/// consistent, smaller, and silent. Nothing derived from what is present can see that -
/// the file set and the claim set agree with each other, which is precisely the problem.
/// An expected count written out by hand is the one statement that does not shrink when
/// the corpus does. <see cref="Result.Claimed"/> is returned so the caller can compare it
/// against that literal.
/// </para>
/// <para>
/// <b>Why declarations are checked against the directory too.</b>
/// <see cref="Result.StaleUnclaimedDeclarations"/> is the same rot
/// <c>SchemaBoundCoverage.Result.StaleExemptions</c> reports: a declaration naming no file
/// classifies nothing today and pre-classifies whatever file next takes that name,
/// decided by somebody who never saw it.
/// </para>
/// </remarks>
internal static class SchemaFixturePartition
{
    /// <summary>Partitions a fixture directory.</summary>
    /// <param name="fixtures">
    /// Every file in the directory, named as the other two arguments name them.
    /// </param>
    /// <param name="claimedByTheRegistry">
    /// The fixture names cited by some verification-registry entry.
    /// </param>
    /// <param name="declaredUnclaimed">
    /// The fixture names declared to be deliberately uncited, enumerated one by one.
    /// </param>
    internal static Result Of(
        IEnumerable<string> fixtures,
        IEnumerable<string> claimedByTheRegistry,
        IReadOnlyList<string> declaredUnclaimed)
    {
        HashSet<string> claimed = new(claimedByTheRegistry, StringComparer.Ordinal);
        HashSet<string> declared = new(declaredUnclaimed, StringComparer.Ordinal);
        HashSet<string> unmatched = new(declared, StringComparer.Ordinal);
        Result result = new();

        foreach (string fixture in fixtures)
        {
            result.FilesChecked++;
            unmatched.Remove(fixture);

            bool isClaimed = claimed.Contains(fixture);
            bool isDeclared = declared.Contains(fixture);

            if (isClaimed && isDeclared)
            {
                // Not counted as claimed. The two classes are exclusive, and a fixture in
                // both is a fixture whose classification nobody can read off the lists.
                result.ClaimedYetDeclaredUnclaimed.Add(fixture);
            }
            else if (isClaimed)
            {
                result.Claimed.Add(fixture);
            }
            else if (!isDeclared)
            {
                result.Unclassified.Add(fixture);
            }
        }

        foreach (string name in declaredUnclaimed)
        {
            if (unmatched.Contains(name))
            {
                result.StaleUnclaimedDeclarations.Add(name);
            }
        }

        return result;
    }

    /// <summary>What the partition found.</summary>
    internal sealed class Result
    {
        /// <summary>Files a registry entry claims, and no declaration contradicts.</summary>
        internal List<string> Claimed { get; } = new();

        /// <summary>Files in neither class: cited by nobody and declared by nobody.</summary>
        internal List<string> Unclassified { get; } = new();

        /// <summary>Files in both classes, which is a classification nobody can read.</summary>
        internal List<string> ClaimedYetDeclaredUnclaimed { get; } = new();

        /// <summary>Declarations matching no file in the directory.</summary>
        internal List<string> StaleUnclaimedDeclarations { get; } = new();

        /// <summary>How many files the partition read.</summary>
        internal int FilesChecked { get; set; }
    }
}
