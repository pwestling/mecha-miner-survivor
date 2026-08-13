using MechaMiner.Content.Envelope;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Content.Tests.Envelope;

/// <summary>
/// The closed tags vocabulary. Verification: <c>VER-DAT-001-017</c>.
/// </summary>
/// <remarks>
/// Doc 40 § <c>tags</c> vocabulary: the vocabulary "starts <b>empty</b> and gains a term
/// only when a concrete query or tooling need requires it; the term is added to the
/// vocabulary in the same change that first uses it."
/// </remarks>
[TestFixture]
internal sealed class TagVocabularyTests
{
    [Test]
    public void TheVocabularyIsClosedAndCurrentlyEmpty()
    {
        Expect.Multiple(() =>
        {
            Assert.That(TagVocabulary.IsEmpty, Is.True);
            Assert.That(TagVocabulary.Terms, Is.Empty);
        });
    }

    /// <summary>
    /// Every one of the 138 accepted definitions authors <c>"tags": []</c>, so an empty
    /// vocabulary blocks nothing today. This records that fact as a test rather than an
    /// assumption: the day it stops being true, this fails and someone adds the term
    /// deliberately.
    /// </summary>
    [TestCase("projectile")]
    [TestCase("elite")]
    [TestCase("ui")]
    [TestCase("")]
    public void EveryTermIsRejectedWhileTheVocabularyIsEmpty(string tag)
    {
        Assert.That(TagVocabulary.Accepts(tag), Is.False);
    }

    [Test]
    public void TheDescriptionTellsAnAuthorHowToAddATerm()
    {
        Assert.That(
            TagVocabulary.Describe(),
            Does.Contain("added to the vocabulary in the same change that first uses it"));
    }
}
