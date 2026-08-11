using System;
using System.Globalization;

namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// A versioned HUD view model whose numbers already reflect authoritative calculation and rounding.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Presentation snapshot: the snapshot includes
/// "versioned HUD view models whose numbers already reflect authoritative calculation and rounding".
/// <c>docs/technical/60-ui-input-and-accessibility.md</c> § HUD implementation: "The HUD binds to one
/// immutable view model per simulation snapshot".
/// </para>
/// <para>
/// <b>The documented rounding rule, in one sentence:</b> a displayed whole value is the authoritative
/// value truncated toward zero after clamping to its authoritative domain, so a displayed number never
/// overstates the authoritative one.
/// </para>
/// <para>
/// <b>Why truncation and not nearest.</b> No accepted document states a HUD rounding rule, so one is
/// adopted here and stated rather than left implicit;
/// <c>docs/technical/91-verification-strategy.md</c> § Numeric tolerance requires "Derived displayed
/// whole Hull values require exact equality after documented rounding", which needs a rule to be exact
/// against. Truncation is chosen because every quantity on this model is one a player acts on, and in
/// each case overstating is the harmful direction: a Hull readout of 1 when the authoritative value is
/// 0.4 promises a hit the player cannot survive; a progress readout of 100% before completion promises
/// an extraction that has not happened; a clock reading ahead of the authoritative clock promises time
/// that is already spent. Nearest-value rounding overstates in half of all cases. doc 20 § Numeric and
/// unit conventions already adopts the same never-early principle for schedules: "Content seconds
/// convert to schedules that never complete earlier than authored."
/// </para>
/// <para>
/// Hull, Armor, and banked resources are authoritatively integral (doc 20 § Numeric and unit
/// conventions: "signed or unsigned integers with checked conversion and validated nonnegative
/// domain"), so for those the rule applies to a <em>derived</em> value - a mitigated Hull mid-tick, a
/// pending payout - rather than to the stored one, which needs no rounding at all.
/// </para>
/// <para>
/// <b>Versioned so a consumer detects change without diffing.</b> <see cref="Version"/> advances only
/// when a displayed value differs, so a HUD can skip a rebind on an unchanged tick and cannot skip one
/// on a changed tick.
/// </para>
/// </remarks>
public readonly struct HudViewModel : IEquatable<HudViewModel>
{
    private readonly long _version;
    private readonly int _displayedHull;
    private readonly int _displayedArmor;
    private readonly long _displayedCommonOre;
    private readonly long _displayedHyperGold;
    private readonly int _displayedRunClockSeconds;
    private readonly int _displayedExtractionPercent;

    private HudViewModel(
        long version,
        int displayedHull,
        int displayedArmor,
        long displayedCommonOre,
        long displayedHyperGold,
        int displayedRunClockSeconds,
        int displayedExtractionPercent)
    {
        _version = version;
        _displayedHull = displayedHull;
        _displayedArmor = displayedArmor;
        _displayedCommonOre = displayedCommonOre;
        _displayedHyperGold = displayedHyperGold;
        _displayedRunClockSeconds = displayedRunClockSeconds;
        _displayedExtractionPercent = displayedExtractionPercent;
    }

    /// <summary>The version before anything has been displayed.</summary>
    public static HudViewModel Unpublished => default;

    /// <summary>The model version, advancing only when a displayed value changes.</summary>
    public long Version => _version;

    /// <summary>The displayed whole Hull.</summary>
    public int DisplayedHull => _displayedHull;

    /// <summary>The displayed whole Armor.</summary>
    public int DisplayedArmor => _displayedArmor;

    /// <summary>The displayed banked common ore.</summary>
    public long DisplayedCommonOre => _displayedCommonOre;

    /// <summary>The displayed banked Hyper Gold.</summary>
    public long DisplayedHyperGold => _displayedHyperGold;

    /// <summary>The displayed whole run-clock seconds.</summary>
    public int DisplayedRunClockSeconds => _displayedRunClockSeconds;

    /// <summary>The displayed extraction progress, as a whole percent in 0..100.</summary>
    public int DisplayedExtractionPercent => _displayedExtractionPercent;

    /// <summary>True when this model has been published at least once.</summary>
    public bool IsPublished => _version > 0;

    /// <summary>
    /// Applies the documented rounding rule to a derived nonnegative value: clamp at zero, then
    /// truncate toward zero.
    /// </summary>
    /// <param name="authoritativeValue">The derived authoritative value. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="authoritativeValue"/> is not finite.</exception>
    /// <remarks>
    /// Exposed rather than private so a test can assert the rule directly, and so any later view model
    /// applies the same one rather than reinventing it.
    /// </remarks>
    public static long RoundDisplayedWhole(double authoritativeValue)
    {
        if (!double.IsFinite(authoritativeValue))
        {
            throw new ArgumentOutOfRangeException(
                nameof(authoritativeValue),
                authoritativeValue,
                "a displayed value must derive from a finite authoritative value");
        }

        double clamped = Math.Max(0.0, authoritativeValue);
        return (long)Math.Floor(clamped);
    }

    /// <summary>
    /// Applies the documented rounding rule to normalized progress, producing a whole percent in
    /// 0..100.
    /// </summary>
    /// <param name="normalizedProgress">Progress in <c>[0,1]</c>. Must be finite.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="normalizedProgress"/> is not finite.</exception>
    /// <remarks>
    /// doc 20 § Numeric and unit conventions represents progress as a "normalized <c>[0,1]</c> value".
    /// Truncation means 99.9% displays as 99 and only exactly 1.0 displays as 100, so the HUD never
    /// claims a completion that has not happened.
    /// </remarks>
    public static int RoundDisplayedPercent(double normalizedProgress)
    {
        if (!double.IsFinite(normalizedProgress))
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalizedProgress),
                normalizedProgress,
                "displayed progress must derive from a finite normalized value");
        }

        double clamped = Math.Clamp(normalizedProgress, 0.0, 1.0);
        return (int)Math.Floor(clamped * 100.0);
    }

    /// <summary>
    /// Produces the next model from authoritative values, advancing the version only if a displayed
    /// value changed.
    /// </summary>
    /// <param name="previous">The model currently published, or <see cref="Unpublished"/>.</param>
    /// <param name="authoritativeHull">The derived Hull. Rounded by the documented rule.</param>
    /// <param name="authoritativeArmor">The derived Armor. Rounded by the documented rule.</param>
    /// <param name="bankedCommonOre">Banked common ore, authoritatively integral.</param>
    /// <param name="bankedHyperGold">Banked Hyper Gold, authoritatively integral.</param>
    /// <param name="runClockSeconds">Derived run-clock seconds. Rounded by the documented rule.</param>
    /// <param name="extractionProgress">Normalized extraction progress in <c>[0,1]</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is not finite, or a banked resource is negative.</exception>
    /// <remarks>
    /// doc 20 § Scope and invariants: "no currency, equipment slot, stat rank, branch, relic, pickup, or
    /// persistent reward becomes negative", so a negative banked resource is rejected rather than
    /// clamped - clamping would hide the invariant failure the HUD is the first place to notice.
    /// </remarks>
    public static HudViewModel Next(
        HudViewModel previous,
        double authoritativeHull,
        double authoritativeArmor,
        long bankedCommonOre,
        long bankedHyperGold,
        double runClockSeconds,
        double extractionProgress)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bankedCommonOre);
        ArgumentOutOfRangeException.ThrowIfNegative(bankedHyperGold);

        int hull = checked((int)RoundDisplayedWhole(authoritativeHull));
        int armor = checked((int)RoundDisplayedWhole(authoritativeArmor));
        int seconds = checked((int)RoundDisplayedWhole(runClockSeconds));
        int percent = RoundDisplayedPercent(extractionProgress);

        bool unchanged = previous.IsPublished
            && previous._displayedHull == hull
            && previous._displayedArmor == armor
            && previous._displayedCommonOre == bankedCommonOre
            && previous._displayedHyperGold == bankedHyperGold
            && previous._displayedRunClockSeconds == seconds
            && previous._displayedExtractionPercent == percent;

        return new HudViewModel(
            unchanged ? previous._version : previous._version + 1,
            hull,
            armor,
            bankedCommonOre,
            bankedHyperGold,
            seconds,
            percent);
    }

    /// <summary>Compares two models for exact equality of every field, version included.</summary>
    public static bool operator ==(HudViewModel left, HudViewModel right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two models for inequality.</summary>
    public static bool operator !=(HudViewModel left, HudViewModel right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(HudViewModel other)
    {
        return _version == other._version
            && _displayedHull == other._displayedHull
            && _displayedArmor == other._displayedArmor
            && _displayedCommonOre == other._displayedCommonOre
            && _displayedHyperGold == other._displayedHyperGold
            && _displayedRunClockSeconds == other._displayedRunClockSeconds
            && _displayedExtractionPercent == other._displayedExtractionPercent;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is HudViewModel other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _version,
            _displayedHull,
            _displayedArmor,
            _displayedCommonOre,
            _displayedHyperGold,
            _displayedRunClockSeconds,
            _displayedExtractionPercent);
    }

    /// <summary>Renders the model as canonical invariant text.</summary>
    public override string ToString()
    {
        return "hud v"
            + _version.ToString(CultureInfo.InvariantCulture)
            + " hull="
            + _displayedHull.ToString(CultureInfo.InvariantCulture)
            + " armor="
            + _displayedArmor.ToString(CultureInfo.InvariantCulture)
            + " ore="
            + _displayedCommonOre.ToString(CultureInfo.InvariantCulture)
            + " hypergold="
            + _displayedHyperGold.ToString(CultureInfo.InvariantCulture)
            + " clock="
            + _displayedRunClockSeconds.ToString(CultureInfo.InvariantCulture)
            + " extraction="
            + _displayedExtractionPercent.ToString(CultureInfo.InvariantCulture)
            + "%";
    }
}
