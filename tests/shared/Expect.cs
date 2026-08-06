using System;
using NUnit.Framework;

namespace MechaMiner.Tests.Support;

/// <summary>
/// Single-overload wrappers around the NUnit assertions whose lambda calls are
/// ambiguous.
/// </summary>
/// <remarks>
/// NUnit 4.6 declares both a <c>TestDelegate</c> and an <see cref="Action"/> overload
/// of <c>Assert.Multiple</c>, <c>Assert.Throws</c>, and <c>Assert.That</c>, and marks
/// <c>TestDelegate</c> obsolete. Passing a lambda literal to any of them is therefore
/// <c>error CS0121</c> under this repository's warnings-as-errors policy. These
/// wrappers take exactly the non-obsolete <see cref="Action"/>, which makes the call
/// site unambiguous without a cast at every use and without suppressing a
/// diagnostic.
/// </remarks>
internal static class Expect
{
    /// <summary>Runs several assertions and reports every failure, not only the first.</summary>
    internal static void Multiple(Action assertions)
    {
        Assert.Multiple(assertions);
    }

    /// <summary>Asserts that <paramref name="code"/> throws exactly <typeparamref name="TException"/>.</summary>
    internal static TException Throws<TException>(Action code)
        where TException : Exception
    {
        return Assert.Throws<TException>(code)!;
    }

    /// <summary>Asserts that <paramref name="code"/> throws nothing.</summary>
    internal static void DoesNotThrow(Action code)
    {
        Assert.DoesNotThrow(code);
    }
}
