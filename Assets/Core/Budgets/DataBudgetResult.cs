namespace MissionCore
{
    public class DataBudgetResult
    {
        public double GeneratedBitsPerDay { get; }
        public double ContactsPerDay { get; }
        public double ContactTimeSPerDay { get; }
        public double LongestGapS { get; }
        public double DownlinkBitsPerS { get; }
        public double StorageNeededBits { get; }
        public double StorageCapacityBits { get; }

        // Data can only be sent while a ground station is visible.
        public double DownlinkBitsPerDay { get => DownlinkBitsPerS * ContactTimeSPerDay; }
        public double ReserveBitsPerDay { get => DownlinkBitsPerDay - GeneratedBitsPerDay; }
        public double ReserveFraction { get => GeneratedBitsPerDay > 0 ? ReserveBitsPerDay / GeneratedBitsPerDay : 0; }
        public bool StorageOk { get => StorageNeededBits <= StorageCapacityBits; }

        public DataBudgetResult(double generatedBitsPerDay, double contactsPerDay, double contactTimeSPerDay,
                                double longestGapS, double downlinkBitsPerS,
                                double storageNeededBits, double storageCapacityBits)
        {
            GeneratedBitsPerDay = generatedBitsPerDay;
            ContactsPerDay = contactsPerDay;
            ContactTimeSPerDay = contactTimeSPerDay;
            LongestGapS = longestGapS;
            DownlinkBitsPerS = downlinkBitsPerS;
            StorageNeededBits = storageNeededBits;
            StorageCapacityBits = storageCapacityBits;
        }
    }
}
