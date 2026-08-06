using System;
using System.Collections.Generic;

namespace MechaMiner.Content.Tests.Schema;

/// <summary>
/// Checks, document by document, that every schema in a corpus either carries at least
/// one numeric bound or is named in an enumerated list of documents declared bound-free.
/// </summary>
/// <remarks>
/// <para>
/// The check the aggregate walk cannot make. <see cref="SchemaBoundWalk.OfAll"/> sums one
/// <c>BoundsSeen</c> over the whole corpus, so a single schema with a bound vouches for
/// every other schema: a document could lose all of its bounds and the total would stay
/// positive. Splitting the count per document turns that into a question with an answer
/// per file, and the exemption list is what lets the question be asked at all - without
/// it, the first legitimately bound-free document in the corpus would have to be answered
/// by weakening the assertion back to an aggregate.
/// </para>
/// <para>
/// Three findings, not one. A list of exemptions rots in two directions besides the one
/// it was written for, and both are silent:
/// </para>
/// <list type="bullet">
///   <item>
///     <see cref="Result.UndeclaredBoundFree"/> - a document with no bound that nobody
///     declared. The finding the list exists to force.
///   </item>
///   <item>
///     <see cref="Result.StaleExemptions"/> - a name on the list matching no document. An
///     exemption for nothing is not harmless: it is a standing waiver for whatever file
///     later takes that name, granted before anyone looked at it.
///   </item>
///   <item>
///     <see cref="Result.UnnecessaryExemptions"/> - a name on the list whose document does
///     carry bounds. The list is a factual claim about the corpus, and this one is false;
///     left in place it would absorb that document losing every bound it has.
///   </item>
/// </list>
/// </remarks>
internal static class SchemaBoundCoverage
{
    /// <summary>Checks a corpus against an enumerated list of bound-free document names.</summary>
    /// <param name="documents">
    /// Every document in the corpus, named as the list names them.
    /// </param>
    /// <param name="declaredBoundFree">
    /// The document names declared to carry no bound, enumerated one by one.
    /// </param>
    internal static Result Of(
        IEnumerable<Document> documents,
        IReadOnlyList<string> declaredBoundFree)
    {
        HashSet<string> declared = new(declaredBoundFree, StringComparer.Ordinal);
        HashSet<string> unmatched = new(declared, StringComparer.Ordinal);
        Result result = new();

        foreach (Document document in documents)
        {
            result.DocumentsChecked++;
            unmatched.Remove(document.Name);

            int bounds = SchemaBoundWalk.Of(document.Bytes).BoundsSeen;
            bool exempt = declared.Contains(document.Name);

            if (bounds == 0 && !exempt)
            {
                result.UndeclaredBoundFree.Add(document.Name);
            }
            else if (bounds > 0 && exempt)
            {
                result.UnnecessaryExemptions.Add(document.Name);
            }
        }

        foreach (string name in declaredBoundFree)
        {
            if (unmatched.Contains(name))
            {
                result.StaleExemptions.Add(name);
            }
        }

        return result;
    }

    /// <summary>One document of a corpus, and the name the exemption list would use.</summary>
    internal sealed class Document
    {
        internal Document(string name, byte[] bytes)
        {
            Name = name;
            Bytes = bytes;
        }

        /// <summary>
        /// What identifies this document to the exemption list.
        /// </summary>
        /// <remarks>
        /// A name has to be unique across the corpus or an exemption is not for one
        /// document. A caller globbing a directory tree must therefore pass a path and not
        /// a file name; the caller in this repository passes the repository-relative path.
        /// </remarks>
        internal string Name { get; }

        /// <summary>The document's raw JSON.</summary>
        internal byte[] Bytes { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>What the per-document check found.</summary>
    internal sealed class Result
    {
        /// <summary>Documents carrying no bound and named by no exemption.</summary>
        internal List<string> UndeclaredBoundFree { get; } = new();

        /// <summary>Exempted names matching no document in the corpus.</summary>
        internal List<string> StaleExemptions { get; } = new();

        /// <summary>Exempted names whose document does carry a bound.</summary>
        internal List<string> UnnecessaryExemptions { get; } = new();

        /// <summary>How many documents the check read.</summary>
        internal int DocumentsChecked { get; set; }
    }
}
