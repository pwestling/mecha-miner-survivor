using System;
using MechaMiner.Simulation.Geometry;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Geometry;

/// <summary>
/// The coordinate presentation adapter contract: east to world +X, north to world -Z, height to
/// world +Y.
/// </summary>
/// <remarks>
/// Verification: <c>VER-GEO-003-001</c>, <c>VER-GEO-003-002</c>.
///
/// <c>docs/technical/decisions/TDR-005-simulate-gameplay-on-a-two-dimensional-plane.md</c>
/// § Coordinate contract, verbatim: presentation "maps simulation east to world positive X, north
/// to world negative Z, and vertical height to world positive Y."
///
/// The sign on the northward axis is the whole content of the mapping and the one thing a
/// hand-written conversion drops, which produces a world mirrored north-to-south that still
/// renders and still moves.
/// </remarks>
[TestFixture]
internal sealed class PresentationGroundMappingTests
{
    [Test]
    public void EastMapsToWorldPositiveX()
    {
        PresentationGroundMapping.ToPresentationWorld(
            PlanarVector.East,
            0.0,
            out double worldX,
            out double worldY,
            out double worldZ);

        Expect.Multiple(() =>
        {
            Assert.That(worldX, Is.EqualTo(1.0), "simulation east is world +X");
            Assert.That(worldY, Is.EqualTo(0.0));
            Assert.That(worldZ, Is.EqualTo(0.0));
        });
    }

    /// <summary>The sign that a hand-written conversion drops.</summary>
    [Test]
    public void NorthMapsToWorldNegativeZ()
    {
        PresentationGroundMapping.ToPresentationWorld(
            PlanarVector.North,
            0.0,
            out double worldX,
            out double worldY,
            out double worldZ);

        Expect.Multiple(() =>
        {
            Assert.That(
                worldZ,
                Is.EqualTo(-1.0),
                "simulation north is world NEGATIVE Z per TDR-005. A positive Z here is a world "
                    + "mirrored north-to-south that renders and moves and still has north pointing the "
                    + "wrong way");
            Assert.That(worldX, Is.EqualTo(0.0));
            Assert.That(worldY, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void HeightMapsToWorldPositiveYAndIsCarriedUnchanged()
    {
        PresentationGroundMapping.ToPresentationWorld(
            PlanarVector.Zero,
            2.75,
            out double worldX,
            out double worldY,
            out double worldZ);

        Expect.Multiple(() =>
        {
            Assert.That(worldY, Is.EqualTo(2.75), "height is world +Y");
            Assert.That(worldX, Is.EqualTo(0.0));
            Assert.That(worldZ, Is.EqualTo(0.0));
        });
    }

    [Test]
    public void EveryQuadrantAndBothAxesMapWithTheDocumentedSigns()
    {
        double[] samples = { -7.5, -1.0, 0.0, 1.0, 7.5 };

        foreach (double x in samples)
        {
            foreach (double y in samples)
            {
                PresentationGroundMapping.ToPresentationWorld(
                    PlanarVector.FromComponents(x, y),
                    0.5,
                    out double worldX,
                    out double worldY,
                    out double worldZ);

                Assert.That(worldX, Is.EqualTo(x), "worldX at (" + x + "," + y + ")");
                Assert.That(worldZ, Is.EqualTo(-y), "worldZ at (" + x + "," + y + ")");
                Assert.That(worldY, Is.EqualTo(0.5), "height at (" + x + "," + y + ")");
            }
        }
    }

    /// <summary>
    /// <c>VER-GEO-003-002</c>: the authoritative pair cannot drift through a presentation round trip.
    /// </summary>
    [Test]
    public void MappingIsInvertibleOnTheGroundPlane()
    {
        double[] samples = { -20.0, -3.25, 0.0, 0.05, 3.25, 20.0 };

        foreach (double x in samples)
        {
            foreach (double y in samples)
            {
                PlanarVector original = PlanarVector.FromComponents(x, y);

                PresentationGroundMapping.ToPresentationWorld(
                    original,
                    1.25,
                    out double worldX,
                    out double worldY,
                    out double worldZ);

                PlanarVector recovered = PresentationGroundMapping.ToSimulationPlane(worldX, worldZ);

                Assert.That(
                    recovered,
                    Is.EqualTo(original),
                    "the round trip must be exact, not approximate: the mapping is a sign flip and a "
                        + "relabelling, so any drift at all would be a defect rather than rounding. "
                        + "Failed at " + original.ToString());

                // Height is discarded on the way back, which is what makes the inverse incapable of
                // carrying a presentation-only value into gameplay.
                Assert.That(worldY, Is.EqualTo(1.25));
            }
        }
    }

    [Test]
    public void TheInverseDiscardsHeightSoPresentationCannotSmuggleItIntoGameplay()
    {
        PlanarVector fromLow = PresentationGroundMapping.ToSimulationPlane(3.0, -4.0);
        PlanarVector fromHigh = PresentationGroundMapping.ToSimulationPlane(3.0, -4.0);

        Expect.Multiple(() =>
        {
            Assert.That(fromLow, Is.EqualTo(PlanarVector.FromComponents(3.0, 4.0)));
            Assert.That(
                fromHigh,
                Is.EqualTo(fromLow),
                "the inverse takes no height parameter at all, so there is no route by which a model's "
                    + "resting elevation could reach the simulation plane");
        });
    }

    [Test]
    public void ANonFiniteHeightIsRefused()
    {
        Assert.That(
            Expect.Throws<ArgumentOutOfRangeException>(
                () => PresentationGroundMapping.ToPresentationWorld(
                    PlanarVector.Zero,
                    double.NaN,
                    out _,
                    out _,
                    out _)).ParamName,
            Is.EqualTo("height"));
    }

    [Test]
    public void ANonFiniteWorldComponentIsRefusedByTheInverse()
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PresentationGroundMapping.ToSimulationPlane(double.NaN, 0.0)).ParamName,
                Is.EqualTo("x"),
                "the inverse refuses through PlanarVector's own validation");
            Assert.That(
                Expect.Throws<ArgumentOutOfRangeException>(
                    () => PresentationGroundMapping.ToSimulationPlane(0.0, double.PositiveInfinity))
                    .ParamName,
                Is.EqualTo("y"));
        });
    }
}
