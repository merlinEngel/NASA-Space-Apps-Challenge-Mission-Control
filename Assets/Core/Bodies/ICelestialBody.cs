namespace MissionCore
{
    public interface ICelestialBody
    {
        double Mu { get; }
        /// <summary>
        /// Gets the absolute position of the Body in world space
        /// </summary>
        /// <param name="t">time in seconds</param>
        /// <returns>absolute position in world space</returns>
        Vec3d WorldPositionAt(double t);
    }
}