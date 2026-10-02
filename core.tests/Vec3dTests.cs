using System;
using NUnit.Framework;
using static MissionCore.Tests.TestHelpers;

namespace MissionCore.Tests
{
    public class Vec3dTests
    {
        [Test]
        public void Operators()
        {
            var a = new Vec3d(1, 2, 3);
            var b = new Vec3d(4, -5, 6);
            AssertVec(new Vec3d(5, -3, 9), a + b, 1e-12);
            AssertVec(new Vec3d(-3, 7, -3), a - b, 1e-12);
            AssertVec(new Vec3d(-1, -2, -3), -a, 1e-12);
            AssertVec(new Vec3d(2, 4, 6), a * 2, 1e-12);
            AssertVec(new Vec3d(2, 4, 6), 2 * a, 1e-12);
            AssertVec(new Vec3d(0.5, 1, 1.5), a / 2, 1e-12);
        }

        [Test]
        public void DotProduct()
        {
            Assert.That(new Vec3d(1, 2, 3).Dot(new Vec3d(4, -5, 6)), Is.EqualTo(12).Within(1e-12));
            Assert.That(Vec3d.UnitX.Dot(Vec3d.UnitY), Is.EqualTo(0));
        }

        [Test]
        public void CrossProduct_IsRightHanded()
        {
            AssertVec(Vec3d.UnitZ, Vec3d.UnitX.Cross(Vec3d.UnitY), 1e-12);
            AssertVec(Vec3d.UnitX, Vec3d.UnitY.Cross(Vec3d.UnitZ), 1e-12);
            AssertVec(-Vec3d.UnitZ, Vec3d.UnitY.Cross(Vec3d.UnitX), 1e-12);
        }

        [Test]
        public void CrossProduct_IsPerpendicular()
        {
            var a = new Vec3d(1, 2, 3);
            var b = new Vec3d(-2, 0.5, 4);
            var c = a.Cross(b);
            Assert.That(c.Dot(a), Is.EqualTo(0).Within(1e-12));
            Assert.That(c.Dot(b), Is.EqualTo(0).Within(1e-12));
        }

        [Test]
        public void Length()
        {
            var v = new Vec3d(3, 4, 12);
            Assert.That(v.SqrLength, Is.EqualTo(169).Within(1e-12));
            Assert.That(v.Length, Is.EqualTo(13).Within(1e-12));
            Assert.That(Vec3d.Distance(new Vec3d(1, 1, 1), new Vec3d(4, 5, 1)), Is.EqualTo(5).Within(1e-12));
        }

        [Test]
        public void Normalized_HasLengthOne()
        {
            var n = new Vec3d(3, -4, 12).Normalized;
            Assert.That(n.Length, Is.EqualTo(1).Within(1e-12));
            // also for orbital radii in meters
            Assert.That(new Vec3d(1.5e11, -2e10, 7e9).Normalized.Length, Is.EqualTo(1).Within(1e-12));
        }

        [Test]
        public void Normalized_ZeroVectorGivesZeroInsteadOfNaN()
        {
            var n = Vec3d.Zero.Normalized;
            Assert.That(n.IsFinite, Is.True);
            AssertVec(Vec3d.Zero, n, 1e-12);
        }

        [Test]
        public void Angle()
        {
            Assert.That(Vec3d.Angle(Vec3d.UnitX, Vec3d.UnitY), Is.EqualTo(Math.PI / 2).Within(1e-12));
            Assert.That(Vec3d.Angle(Vec3d.UnitX, -Vec3d.UnitX), Is.EqualTo(Math.PI).Within(1e-12));
            Assert.That(Vec3d.Angle(Vec3d.UnitX, new Vec3d(1, 1, 0)), Is.EqualTo(Math.PI / 4).Within(1e-12));
            // very small angle: Acos would be inaccurate here, Atan2 is not
            Assert.That(Vec3d.Angle(Vec3d.UnitX, new Vec3d(1, 1e-9, 0)), Is.EqualTo(1e-9).Within(1e-15));
        }

        [Test]
        public void ProjectionAndRemainder_GiveOriginal()
        {
            var v = new Vec3d(3, 4, 5);
            var dir = new Vec3d(0, 2, 0);   // intentionally not normalized
            AssertVec(new Vec3d(0, 4, 0), v.ProjectOn(dir), 1e-12);
            AssertVec(new Vec3d(3, 0, 5), v.RejectFrom(dir), 1e-12);
            AssertVec(v, v.ProjectOn(dir) + v.RejectFrom(dir), 1e-12);
        }

        [Test]
        public void Rotations_Counterclockwise()
        {
            AssertVec(Vec3d.UnitY, Vec3d.UnitX.RotateZ(Math.PI / 2), 1e-12);
            AssertVec(Vec3d.UnitZ, Vec3d.UnitY.RotateX(Math.PI / 2), 1e-12);
            // rotation does not change the length
            var v = new Vec3d(1, 2, 3);
            Assert.That(v.RotateZ(0.7).RotateX(-1.3).Length, Is.EqualTo(v.Length).Within(1e-12));
        }

        [Test]
        public void Lerp()
        {
            var a = new Vec3d(0, 0, 0);
            var b = new Vec3d(10, -10, 2);
            AssertVec(a, Vec3d.Lerp(a, b, 0), 1e-12);
            AssertVec(b, Vec3d.Lerp(a, b, 1), 1e-12);
            AssertVec(new Vec3d(5, -5, 1), Vec3d.Lerp(a, b, 0.5), 1e-12);
        }

        [Test]
        public void IsFinite_DetectsNaN()
        {
            Assert.That(new Vec3d(1, double.NaN, 0).IsFinite, Is.False);
            Assert.That(new Vec3d(1, double.PositiveInfinity, 0).IsFinite, Is.False);
            Assert.That(new Vec3d(1, 2, 3).IsFinite, Is.True);
        }
    }
}
