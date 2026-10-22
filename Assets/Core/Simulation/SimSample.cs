namespace MissionCore.Simulation
{
    // Snapshot for the battery and storage curves.
    public readonly struct SimSample
    {
        public readonly double TimeS, BatterySoc, StorageBits, AltitudeM;

        public SimSample(double timeS, double batterySoc, double storageBits, double altitudeM)
        {
            TimeS = timeS;
            BatterySoc = batterySoc;
            StorageBits = storageBits;
            AltitudeM = altitudeM;
        }
    }
}