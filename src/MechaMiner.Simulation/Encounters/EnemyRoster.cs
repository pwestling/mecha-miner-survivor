namespace MechaMiner.Simulation.Encounters;

/// <summary>
/// The authored ordinary-enemy rows this slice carries, and the shared reference the body scales
/// multiply.
/// </summary>
/// <remarks>
/// <para>
/// <b>One row: <c>EN-01</c> Skitterling.</b> Not because the other nine are undecided -
/// <c>docs/31-initial-alien-roster.md</c> § Ordinary roster overview:37-48 states all ten - but
/// because <c>docs/32-standard-wave-and-beacon-schedule.md</c> § Complete 35-minute schedule:56
/// composes minute 0 from "Skitterling 100%", and minute 1 is the first row that needs a second
/// identity. Authoring the other nine here would be authoring content for a schedule row this
/// package does not execute, and each row would be an unexercised claim about a document.
/// </para>
/// <para>
/// <b>Why these numbers live in code at all.</b> The typed content layer (<c>DAT-002</c>) does not
/// exist on this ref, and <c>content/</c> here holds only <c>.gitkeep</c> - the authored
/// <c>content/enemies/EN-01.json</c> exists on <c>master</c> and is not in this branch's ancestry.
/// So every number below carries its document line, in the same style
/// <c>MechaMiner.Simulation.Player.PlayerBaseline</c> uses, and this file is the single place the
/// data-driven pass has to visit.
/// </para>
/// </remarks>
public static class EnemyRoster
{
    /// <summary>
    /// The Ripper's contact diameter in gameplay meters: <c>0.80</c>.
    /// </summary>
    /// <remarks>
    /// <c>docs/31-initial-alien-roster.md</c>:35 - "Body scale multiplies the Ripper's <b>0.80M</b>
    /// contact diameter, not its decorative mesh." <c>M</c> is one unmodified mech collision
    /// diameter, which <c>docs/72-player-survivability-and-damage-baseline.md</c>:40 and :47 fix at
    /// <c>1.0</c> m, so <c>0.80M</c> is <c>0.80</c> m. This is the shared reference every body scale
    /// in the roster is expressed against, which is why it lives here rather than inside the Ripper
    /// row that does not exist yet.
    /// </remarks>
    public const double RipperContactDiameterMeters = 0.80;

    /// <summary>The <c>EN-01</c> Skitterling's fixed maximum Hull: <c>20</c>.</summary>
    /// <remarks><c>docs/31-initial-alien-roster.md</c>:39, Hull column.</remarks>
    public const int SkitterlingMaximumHull = 20;

    /// <summary>The <c>EN-01</c> Skitterling's contact damage: <c>5</c>.</summary>
    /// <remarks>
    /// <c>docs/31-initial-alien-roster.md</c>:39, Contact column. doc 31:31 calls the roster's
    /// numbers "initial prototype values against a 100-Hull baseline", which is the baseline
    /// <c>PlayerBaseline.MaximumHull</c> carries.
    /// </remarks>
    public const int SkitterlingContactDamage = 5;

    /// <summary>
    /// The <c>EN-01</c> Skitterling's movement share of the unmodified mech speed: <c>0.42</c>.
    /// </summary>
    /// <remarks>
    /// <c>docs/31-initial-alien-roster.md</c>:39 gives <c>42%</c>, and
    /// <c>docs/72-player-survivability-and-damage-baseline.md</c>:65 states the product this
    /// implies: "Skitterling | 42% | 1.26M/s".
    /// </remarks>
    public const double SkitterlingMovementShare = 0.42;

    /// <summary>The <c>EN-01</c> Skitterling's body scale: <c>0.55</c>.</summary>
    /// <remarks>
    /// <c>docs/31-initial-alien-roster.md</c>:39, Body column - <c>0.55x</c>, which against the
    /// <c>0.80</c> m Ripper diameter is a <c>0.44</c> m contact diameter. That derived figure is
    /// also the first data column of <c>docs/data/contact-damage-pressure.csv</c>:2, which reads
    /// <c>EN-01,Skitterling,ordinary,0.44,...</c>.
    /// </remarks>
    public const double SkitterlingBodyScale = 0.55;

    /// <summary>The <c>EN-01</c> Skitterling: the roster's baseline fragile pursuer.</summary>
    /// <remarks><c>docs/31-initial-alien-roster.md</c> § EN-01 - Skitterling:52-54.</remarks>
    public static EnemyProfile Skitterling => EnemyProfile.Authored(
        "EN-01",
        SkitterlingMaximumHull,
        SkitterlingContactDamage,
        SkitterlingMovementShare,
        SkitterlingBodyScale);
}
