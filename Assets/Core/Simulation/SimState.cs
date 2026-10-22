using MissionCore.Numerics;

namespace MissionCore.Simulation
{
    public sealed class SimState
    {
        public SimState(double missionTimeS, Vec3d positionM, double altitudeM, bool inShadow, bool inContact, double batteryWh, double batteryCapacityWh, double storageBits, double storageCapacityBits, double deliveredBits, double lostBits, double propellantKg, bool instrumentsOn, bool safeMode, EndReason endReason)
        {
            MissionTimeS = missionTimeS;
            PositionM = positionM;
            AltitudeM = altitudeM;
            InShadow = inShadow;
            InContact = inContact;
            BatteryWh = batteryWh;
            BatteryCapacityWh = batteryCapacityWh;
            StorageBits = storageBits;
            StorageCapacityBits = storageCapacityBits;
            DeliveredBits = deliveredBits;
            LostBits = lostBits;
            PropellantKg = propellantKg;
            InstrumentsOn = instrumentsOn;
            SafeMode = safeMode;
            EndReason = endReason;
        }

        public double MissionTimeS { get; internal set; }
        public Vec3d PositionM { get; internal set; }     // inertial, Earth-centred
        public double AltitudeM { get; internal set; }
        public bool InShadow { get; internal set; }
        public bool InContact { get; internal set; }

        public double BatteryWh { get; internal set; }
        public double BatteryCapacityWh { get; internal set; }
        public double BatterySoc => BatteryCapacityWh > 0 ? BatteryWh / BatteryCapacityWh : 0;

        public double StorageBits { get; internal set; }
        public double StorageCapacityBits { get; internal set; }
        public double DeliveredBits { get; internal set; }
        public double LostBits { get; internal set; }

        public double PropellantKg { get; internal set; }

        public bool InstrumentsOn { get; internal set; }
        public bool SafeMode { get; internal set; }
        public EndReason EndReason { get; internal set; }
        public bool Ended => EndReason != EndReason.None;
    }
}