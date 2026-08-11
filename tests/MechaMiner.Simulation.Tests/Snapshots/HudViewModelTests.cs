using System;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Snapshots;

/// <summary>
/// Proves that every displayed whole value equals the authoritative value after the documented rounding,
/// exactly and with no tolerance, and that the view model is versioned.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-007-008</c>.
///
/// <c>docs/technical/91-verification-strategy.md</c> § Numeric tolerance: "Derived displayed whole Hull
/// values require exact equality after documented rounding", and "Integer currency, ranks, ticks, counts,
/// schedule boundaries, and IDs require exact equality."
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot: HUD view model numbers "already
/// reflect authoritative calculation and rounding".
///
/// The documented rounding rule under test: a displayed whole value is the authoritative value clamped to
/// its domain and then truncated toward zero, so a displayed number never overstates the authoritative one.
/// No <c>NumericAssert.AreEqualWithin</c> appears in this file, deliberately: doc 91 permits no tolerance
/// here.
/// </remarks>
[TestFixture]
internal sealed class HudViewModelTests
{
    /// <summary>
    /// Verification: <c>VER-SIM-007-008</c>.
    ///
    /// Displayed whole values equal the authoritative values after the documented rounding exactly, and the
    /// version advances only when a displayed value changes.
    /// </summary>
    [Test]
    public void DisplayedWholeValuesEqualAuthoritativeValuesAfterDocumentedRounding()
    {
        Expect.Multiple(() =>
        {
            // The rule: truncate toward zero after clamping at zero. Exact equality, no tolerance.
            NumericAssert.AreExactlyEqual(
                100L,
                HudViewModel.RoundDisplayedWhole(100.0),
                "an exact whole value");
            NumericAssert.AreExactlyEqual(
                0L,
                HudViewModel.RoundDisplayedWhole(0.4),
                "0.4 Hull displays as 0, never as 1: a HUD that said 1 would promise a hit the player cannot "
                    + "survive");
            NumericAssert.AreExactlyEqual(
                0L,
                HudViewModel.RoundDisplayedWhole(0.5),
                "and a half value truncates too, so the rule has no midpoint case to disagree about");
            NumericAssert.AreExactlyEqual(
                0L,
                HudViewModel.RoundDisplayedWhole(0.9999999),
                "just under one displays as zero");
            NumericAssert.AreExactlyEqual(
                99L,
                HudViewModel.RoundDisplayedWhole(99.9999999),
                "just under a hundred displays as ninety-nine");
            NumericAssert.AreExactlyEqual(
                0L,
                HudViewModel.RoundDisplayedWhole(-3.7),
                "a negative derived value clamps to zero; doc 20 § Scope and invariants forbids a negative "
                    + "authoritative quantity, so the HUD shows the floor of the domain rather than a "
                    + "negative number");

            // Progress: 100 only at exactly complete.
            NumericAssert.AreExactlyEqual(0L, HudViewModel.RoundDisplayedPercent(0.0), "zero progress");
            NumericAssert.AreExactlyEqual(0L, HudViewModel.RoundDisplayedPercent(0.009), "sub-one-percent progress");
            NumericAssert.AreExactlyEqual(50L, HudViewModel.RoundDisplayedPercent(0.5), "half progress");
            NumericAssert.AreExactlyEqual(
                99L,
                HudViewModel.RoundDisplayedPercent(0.999999),
                "99.9999% displays as 99, so the HUD never claims a completion that has not happened");
            NumericAssert.AreExactlyEqual(100L, HudViewModel.RoundDisplayedPercent(1.0), "complete progress");
            NumericAssert.AreExactlyEqual(
                100L,
                HudViewModel.RoundDisplayedPercent(1.5),
                "and progress above one clamps rather than overflowing the display");
            NumericAssert.AreExactlyEqual(0L, HudViewModel.RoundDisplayedPercent(-0.5), "negative progress clamps");

            Expect.Throws<ArgumentOutOfRangeException>(() => HudViewModel.RoundDisplayedWhole(double.NaN));
            Expect.Throws<ArgumentOutOfRangeException>(
                () => HudViewModel.RoundDisplayedPercent(double.PositiveInfinity));
        });

        // Every field of a real model equals its authoritative value after the rule, exactly.
        HudViewModel model = HudViewModel.Next(
            HudViewModel.Unpublished,
            authoritativeHull: 87.9999,
            authoritativeArmor: 4.5,
            bankedCommonOre: 1_234,
            bankedHyperGold: 2_100,
            runClockSeconds: 1_234.75,
            extractionProgress: 0.4999);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(87L, model.DisplayedHull, "displayed Hull");
            NumericAssert.AreExactlyEqual(4L, model.DisplayedArmor, "displayed Armor");
            NumericAssert.AreExactlyEqual(
                1_234L,
                model.DisplayedCommonOre,
                "banked common ore is authoritatively integral, so it is displayed unchanged");
            NumericAssert.AreExactlyEqual(2_100L, model.DisplayedHyperGold, "banked Hyper Gold");
            NumericAssert.AreExactlyEqual(1_234L, model.DisplayedRunClockSeconds, "displayed run-clock seconds");
            NumericAssert.AreExactlyEqual(49L, model.DisplayedExtractionPercent, "displayed extraction percent");
            NumericAssert.AreExactlyEqual(1L, model.Version, "the first published model is version one");
            Assert.That(model.IsPublished, Is.True);
        });

        AssertVersionAdvancesOnlyOnAChangedDisplayedValue(model);

        Expect.Multiple(() =>
        {
            Assert.That(HudViewModel.Unpublished.IsPublished, Is.False);
            NumericAssert.AreExactlyEqual(0L, HudViewModel.Unpublished.Version, "the unpublished version");
            Expect.Throws<ArgumentOutOfRangeException>(() => HudViewModel.Next(
                HudViewModel.Unpublished, 1.0, 1.0, -1, 0, 0.0, 0.0));
            Expect.Throws<ArgumentOutOfRangeException>(() => HudViewModel.Next(
                HudViewModel.Unpublished, 1.0, 1.0, 0, -1, 0.0, 0.0));
        });
    }

    /// <summary>
    /// The version advances when a displayed value changes and stays put when only an authoritative value
    /// below the rounding granularity does, so a consumer can detect a change without diffing fields.
    /// </summary>
    private static void AssertVersionAdvancesOnlyOnAChangedDisplayedValue(HudViewModel model)
    {
        // A different authoritative Hull that rounds to the same displayed value: no visible change.
        HudViewModel unchanged = HudViewModel.Next(
            model,
            authoritativeHull: 87.0001,
            authoritativeArmor: 4.9,
            bankedCommonOre: 1_234,
            bankedHyperGold: 2_100,
            runClockSeconds: 1_234.01,
            extractionProgress: 0.4901);

        // One displayed value differs: a visible change.
        HudViewModel changed = HudViewModel.Next(
            unchanged,
            authoritativeHull: 86.0,
            authoritativeArmor: 4.9,
            bankedCommonOre: 1_234,
            bankedHyperGold: 2_100,
            runClockSeconds: 1_234.01,
            extractionProgress: 0.4901);

        Expect.Multiple(() =>
        {
            NumericAssert.AreExactlyEqual(
                model.Version,
                unchanged.Version,
                "the version must not advance when every displayed value is unchanged, so a HUD can skip a "
                    + "rebind without diffing fields");
            Assert.That(
                unchanged,
                Is.EqualTo(model),
                "and the whole model must compare equal, or the version would be lying");
            NumericAssert.AreExactlyEqual(
                model.Version + 1,
                changed.Version,
                "the version must advance when a displayed value changes");
            NumericAssert.AreExactlyEqual(86L, changed.DisplayedHull, "the changed displayed Hull");
            Assert.That(changed, Is.Not.EqualTo(unchanged));
            Assert.That(
                changed.ToString(),
                Does.Contain("hull=86"),
                "the rendering carries the displayed values, for evidence and goldens");
        });
    }
}
