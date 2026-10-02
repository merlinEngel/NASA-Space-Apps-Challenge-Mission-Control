namespace MissionCore
{
    public interface ICelestialBody
    {
        double Mu { get; }
        Vec3d WorldPositionAt(double t);
    }
}