namespace MissionCore
{
    public class Unit
    {
        public static Unit[] PowerUnits { get; } = new Unit[] { ("W", 1, 0, 2), ("kW", 1000, 1, 2) };
        public static Unit[] MoneyUnits { get; } = new Unit[] { ("USD", 1, 0, 2), ("M USD", 1000000, 1, 2) };
        public static Unit[] DeltaVMUnits { get; } = new Unit[] { ("m/s", 1, 0, 0) };
        public static Unit[] DataStorageUnits { get; } = new Unit[] { ("b", 1, 0, 0), ("B", 8, 0, 0), ("kB", 8000, 1, 2), ("MB", 8e+6, 1, 2), ("GB", 8e+9, 1, 2), ("TB", 8e+12, 1, 2) };
        public static Unit[] DataRateUnits { get; } = new Unit[] { ("b/s", 1, 0, 0), ("kbit/s", 1000, 1, 2), ("Mbit/s", 1e+6, 1, 2), ("Gbit/s", 1e+9, 1, 2), ("Tbit/s", 1e+12, 1, 2) };
        public static Unit[] MassUnits { get; } = new Unit[] { ("kg", 1, 1, 2), ("t", 1000, 2, 3) };


        public Unit(string name, double factor, int simpleDecimalPlaces, int realisticDecimalPlaces)
        {
            Name = name;
            Factor = factor;
            SimpleDecimalPlaces = simpleDecimalPlaces;
            RealisticDecimalPlaces = realisticDecimalPlaces;
        }

        public string Name { get; }
        public double Factor { get; }
        public int SimpleDecimalPlaces { get; }
        public int RealisticDecimalPlaces { get; }

        public static implicit operator Unit((string name, double factor, int simpleDecimalPlaces, int realisticDecimalPlaces) v)
        {
            return new(v.name, v.factor, v.simpleDecimalPlaces, v.realisticDecimalPlaces);
        }
    }
}