using System;

namespace MissionCore
{
    public readonly struct Vec3d
    {
        public readonly double x, y, z;
        public Vec3d(double x = 0, double y = 0, double z = 0) { this.x = x; this.y = y; this.z = z; }

        public static readonly Vec3d Zero = new(0, 0, 0);
        public static readonly Vec3d UnitX = new(1, 0, 0);
        public static readonly Vec3d UnitY = new(0, 1, 0);
        public static readonly Vec3d UnitZ = new(0, 0, 1);

        // Operators
        public static Vec3d operator +(Vec3d a, Vec3d b) => new(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vec3d operator -(Vec3d a, Vec3d b) => new(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vec3d operator -(Vec3d a) => new(-a.x, -a.y, -a.z);
        public static Vec3d operator *(Vec3d a, double s) => new(a.x * s, a.y * s, a.z * s);
        public static Vec3d operator *(double s, Vec3d a) => a * s;
        public static Vec3d operator /(Vec3d a, double s) => new(a.x / s, a.y / s, a.z / s);

        // Products
        public double Dot(Vec3d v) => x * v.x + y * v.y + z * v.z;
        public Vec3d Cross(Vec3d v) => new(y * v.z - z * v.y, z * v.x - x * v.z, x * v.y - y * v.x);

        // Length and direction
        public double SqrLength => x * x + y * y + z * z;
        public double Length => Math.Sqrt(SqrLength);
        public Vec3d Normalized { get { double l = Length; return l > 1e-300 ? this / l : Zero; } }

        // Distance and angle
        public static double Distance(Vec3d a, Vec3d b) => (a - b).Length;
        public static double Angle(Vec3d a, Vec3d b) => Math.Atan2(a.Cross(b).Length, a.Dot(b));

        // Decompose along a direction (shadow model)
        public Vec3d ProjectOn(Vec3d dir) => dir * (Dot(dir) / dir.SqrLength);
        public Vec3d RejectFrom(Vec3d dir) => this - ProjectOn(dir);

        // Rotations (angle in rad, right-handed, counterclockwise)
        public Vec3d RotateX(double a) { double c = Math.Cos(a), s = Math.Sin(a); return new(x, c * y - s * z, s * y + c * z); }
        public Vec3d RotateZ(double a) { double c = Math.Cos(a), s = Math.Sin(a); return new(c * x - s * y, s * x + c * y, z); }

        // Interpolation
        public static Vec3d Lerp(Vec3d a, Vec3d b, double t) => a + (b - a) * t;

        // Checks
        public bool ApproxEquals(Vec3d o, double eps) => (this - o).Length <= eps;
        public bool IsFinite => double.IsFinite(x) && double.IsFinite(y) && double.IsFinite(z);

        public void Deconstruct(out double x, out double y, out double z) { x = this.x; y = this.y; z = this.z; }
        public override string ToString() => $"({x:G6}, {y:G6}, {z:G6})";
    }
}