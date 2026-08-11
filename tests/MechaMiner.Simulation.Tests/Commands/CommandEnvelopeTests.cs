using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Time;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Commands;

/// <summary>
/// Pins the run-session fence of <c>CTR-RUN-002</c>: a foreign envelope is refused on identity alone, and its
/// payload is not reachable at all.
/// </summary>
/// <remarks>
/// Verification: <c>VER-SIM-004-004</c>.
///
/// <c>docs/technical/10-runtime-architecture.md</c> § Commands and mutations: "Commands that can cross an
/// asynchronous boundary carry a run-session identity and monotonic command sequence."
/// <c>CTR-RUN-002</c> in <c>docs/technical/115-component-contract-and-schema-registry.md</c> §
/// Cross-boundary contract registry: "run ID, target tick, monotonic sequence, normalized payload" with
/// "stale/duplicate/invalid commands return typed rejection/no change".
/// </remarks>
[TestFixture]
internal sealed class CommandEnvelopeTests
{
    /// <summary>A raw component chosen so it is recognizable in any rendering that leaked it.</summary>
    private const double MarkerRawInput = 0.123456789;

    /// <summary>
    /// Verification: <c>VER-SIM-004-004</c>.
    ///
    /// A foreign envelope is refused with <see cref="CommandRejectionReason.ForeignRunSession"/> even when it
    /// is also closed, out of order, and unnormalizable; its payload cannot be normalized or rendered at all;
    /// and the identical payload and window from the owning run produce a different reason, so the fence is
    /// what decided it.
    /// </summary>
    [Test]
    public void AForeignRunIdentityIsRejectedBeforePayloadInspection()
    {
        AssertTheFenceOutranksEveryOtherRefusal();
        AssertAForeignEnvelopeCannotNormalizeEvenAPerfectPayload();
        AssertNoMemberYieldsThePayloadWithoutTheRunSession();
        AssertTheRenderingCarriesIdentityOnly();
    }

    /// <summary>
    /// The three-way contrast that makes the ordering claim non-vacuous: one payload, three run/window
    /// combinations, three different reasons.
    /// </summary>
    private static void AssertTheFenceOutranksEveryOtherRefusal()
    {
        CommandFixture fixture = new();

        // No window open, an unnormalizable payload, and a sequence that would regress if it got that far.
        CommandEnvelope foreign = CommandFixture.ForeignEnvelope(
            targetTick: 0,
            sequence: 0,
            rawInputX: double.NaN,
            rawInputY: 0.0);
        Assert.That(fixture.Gate.TryAdmit(foreign, out CommandRejection foreignRejection), Is.False);

        // The same payload from the owning run, with no window open: a different reason, so the fence is not
        // merely the first thing that happens to fail.
        CommandEnvelope ownWhileClosed = CommandFixture.Envelope(
            targetTick: 0,
            sequence: 0,
            rawInputX: double.NaN,
            rawInputY: 0.0);
        Assert.That(fixture.Gate.TryAdmit(ownWhileClosed, out CommandRejection closedRejection), Is.False);

        // The same payload from the owning run with the window open: now the payload is the only thing left
        // to refuse, which proves the payload check exists and is reachable.
        fixture.Gate.BeginTick(SimulationTick.Zero);
        Assert.That(fixture.Gate.TryAdmit(ownWhileClosed, out CommandRejection invalidRejection), Is.False);

        Expect.Multiple(() =>
        {
            Assert.That(
                foreignRejection.Reason,
                Is.EqualTo(CommandRejectionReason.ForeignRunSession),
                "a foreign run identity is refused on identity alone, ahead of the closed window and the "
                    + "unnormalizable payload it also carries");
            Assert.That(
                closedRejection.Reason,
                Is.EqualTo(CommandRejectionReason.AdmissionClosed),
                "the same payload from the owning run with no window open reports the window instead");
            Assert.That(
                invalidRejection.Reason,
                Is.EqualTo(CommandRejectionReason.InvalidPayload),
                "and with the window open it reports the payload, so all three reasons are reachable");
            Assert.That(
                foreignRejection.RunSession,
                Is.EqualTo(CommandFixture.ForeignRunSession),
                "the rejection names the identity that was refused");
            Assert.That(
                fixture.Gate.RejectionCount(CommandRejectionReason.ForeignRunSession),
                Is.EqualTo(1L),
                "exactly one foreign-run refusal was counted");
            Assert.That(
                fixture.Gate.AdmittedInRun,
                Is.Zero,
                "and nothing was admitted by any of the three");
        });
    }

    /// <summary>
    /// The fence is about identity, not about the payload: a foreign envelope carrying a perfectly
    /// normalizable payload still cannot produce an intent.
    /// </summary>
    private static void AssertAForeignEnvelopeCannotNormalizeEvenAPerfectPayload()
    {
        CommandEnvelope foreign = CommandFixture.ForeignEnvelope(
            targetTick: 3,
            sequence: 4,
            rawInputX: 1.0,
            rawInputY: 0.0);
        CommandEnvelope own = CommandFixture.Envelope(
            targetTick: 3,
            sequence: 4,
            rawInputX: 1.0,
            rawInputY: 0.0);

        Expect.Multiple(() =>
        {
            Assert.That(
                foreign.TryNormalizePayload(CommandFixture.RunSession, out MovementIntent fromForeign),
                Is.False,
                "a foreign envelope refuses to normalize a payload that is not in any way invalid");
            Assert.That(
                fromForeign,
                Is.EqualTo(MovementIntent.Stop),
                "and it hands back the stop rather than the intent it declined to produce");
            Assert.That(
                own.TryNormalizePayload(CommandFixture.RunSession, out MovementIntent fromOwn),
                Is.True,
                "while the identical payload from the owning run normalizes, so the refusal above was the "
                    + "fence and not the payload");
            Assert.That(fromOwn.X, Is.EqualTo(1.0), "with the direction the producer sampled");
            Assert.That(
                foreign.BelongsTo(CommandFixture.RunSession),
                Is.False,
                "and the fence itself reports the mismatch");
            Assert.That(
                own.BelongsTo(CommandFixture.RunSession),
                Is.True,
                "and the match");
            Assert.That(
                default(CommandEnvelope).BelongsTo(CommandFixture.RunSession),
                Is.False,
                "a defaulted envelope belongs to no run at all");
        });
    }

    /// <summary>
    /// The structural half: there is no member of <see cref="CommandEnvelope"/> that yields a
    /// <see cref="MovementIntent"/> without being handed a run session, so the fence cannot be bypassed by a
    /// caller that forgets it.
    /// </summary>
    private static void AssertNoMemberYieldsThePayloadWithoutTheRunSession()
    {
        Type envelopeType = typeof(CommandEnvelope);
        Type intentType = typeof(MovementIntent);
        Type intentByRefType = intentType.MakeByRefType();
        const BindingFlags surface =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        List<string> unfencedMembers = new();
        List<string> fencedMembers = new();

        foreach (PropertyInfo property in envelopeType.GetProperties(surface))
        {
            if (property.PropertyType == intentType)
            {
                unfencedMembers.Add("property " + property.Name);
            }
        }

        foreach (MethodInfo method in envelopeType.GetMethods(surface))
        {
            ParameterInfo[] parameters = method.GetParameters();
            bool yieldsIntent = method.ReturnType == intentType;
            bool takesRunSession = false;
            foreach (ParameterInfo parameter in parameters)
            {
                if (parameter.ParameterType == intentByRefType)
                {
                    yieldsIntent = true;
                }

                if (parameter.ParameterType == typeof(ulong))
                {
                    takesRunSession = true;
                }
            }

            if (!yieldsIntent)
            {
                continue;
            }

            if (takesRunSession)
            {
                fencedMembers.Add("method " + method.Name);
            }
            else
            {
                unfencedMembers.Add("method " + method.Name);
            }
        }

        Expect.Multiple(() =>
        {
            Assert.That(
                unfencedMembers,
                Is.Empty,
                "no public member of CommandEnvelope may yield a MovementIntent without being handed the "
                    + "expected run session, or VER-SIM-004-004's \"before payload inspection\" would be an "
                    + "ordering the gate has to remember rather than a property of the type. Unfenced: "
                    + string.Join(", ", unfencedMembers));
            Assert.That(
                fencedMembers,
                Is.EqualTo(new[] { "method TryNormalizePayload" }),
                "and there is exactly one fenced route to the payload, so a second one cannot be added "
                    + "without this test going red");
        });
    }

    /// <summary>
    /// The rendering carries identity only. If diagnostics could print the raw payload, "the payload is
    /// unreachable without the fence" would be true of one method and false of the type.
    /// </summary>
    private static void AssertTheRenderingCarriesIdentityOnly()
    {
        CommandEnvelope envelope = CommandFixture.Envelope(
            targetTick: 7,
            sequence: 11,
            rawInputX: MarkerRawInput,
            rawInputY: -MarkerRawInput);
        string rendered = envelope.ToString();
        string marker = MarkerRawInput.ToString("R", CultureInfo.InvariantCulture);

        Expect.Multiple(() =>
        {
            Assert.That(
                rendered,
                Does.Not.Contain(marker).IgnoreCase,
                "the rendering must not carry the raw payload; it read: " + rendered);
            Assert.That(rendered, Does.Contain("tick=7"), "but it does carry the target tick");
            Assert.That(rendered, Does.Contain("seq=11"), "and the sequence");
            Assert.That(
                rendered,
                Does.Contain(CommandFixture.RunSession.ToString("X16", CultureInfo.InvariantCulture)),
                "and the run session");
            Assert.That(
                default(CommandEnvelope).ToString(),
                Is.EqualTo("envelope(none)").Using(StringComparer.Ordinal),
                "and a defaulted envelope renders as no envelope rather than as run zero");
        });
    }
}
