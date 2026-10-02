using MissionCore;
using NUnit.Framework;

public class KeplerOrbitTests
{
    const double MuEarth = 3.986004418e14, REarth = 6378137;

    [Test]
    public void CircularOrbit400km_Period()
    {
        var orbit = KeplerOrbit.Circular(400e3, 0, 0, REarth, MuEarth);
        Assert.That(orbit.Period, Is.EqualTo(5553.6).Within(1.0));   // seconds
    }

    [Test]
    public void SolveKepler_ReferenceValue()
    {
        Assert.That(KeplerOrbit.SolveKepler(1.0, 0.5), Is.EqualTo(1.4987011335).Within(1e-9));
    }

    [Test]
    public void AfterOnePeriod_SamePosition()
    {
        var o = new KeplerOrbit(7e6, 0.5, 0.3, 1.0, 2.0, 0.5, 0, MuEarth);
        Assert.That(Vec3d.Distance(o.PositionAt(0), o.PositionAt(o.Period)), Is.LessThan(1.0));
    }
}