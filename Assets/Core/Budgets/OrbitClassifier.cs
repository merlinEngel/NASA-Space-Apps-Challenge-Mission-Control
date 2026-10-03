namespace MissionCore
{
    public static class OrbitClassifier
    {
        public static LaunchOrbit? Classify(double altitudeM, double inclinationDeg)
        {
            if (altitudeM <= 2000000)
            {
                if (new ValueRange(96, 99).Contains(inclinationDeg))
                    return LaunchOrbit.Sso;
                else
                    return LaunchOrbit.Leo;
            }
            return null;
        }
    }
}