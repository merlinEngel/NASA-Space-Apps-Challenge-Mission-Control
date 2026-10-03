using System;
using System.Collections.Generic;

namespace MissionCore
{
    public static class DataBudget
    {
        public static DataBudgetResult Evaluate(Catalog catalog, MissionDesign design)
        {
            double generatedBitsPerDay = GetGeneratedBitsPerDay(catalog, design);

            double downlinkBitsPerS = 0;
            var stations = new List<GroundStationSpec>();
            if (!string.IsNullOrEmpty(design.CommsId) && catalog.Comms.TryGetValue(design.CommsId, out CommsSpec commsSpec))
            {
                downlinkBitsPerS = commsSpec.dataRateBpS;
                stations = GetCompatibleStations(catalog, design, commsSpec.band);
            }

            ContactStats contacts = GroundContact.Scan(design.AltitudeM, design.InclinationDeg, stations);

            // Everything measured during the longest gap without contact has to wait on board.
            double storageNeededBits = generatedBitsPerDay / Constants.SecondsPerDay * contacts.LongestGapS;

            double storageCapacityBits = 0;
            if (!string.IsNullOrEmpty(design.PlatformId) && catalog.Platforms.TryGetValue(design.PlatformId, out PlatformSpec platformSpec))
                storageCapacityBits = platformSpec.storageBits;

            return new DataBudgetResult(
                generatedBitsPerDay,
                contacts.ContactsPerDay,
                contacts.ContactTimeSPerDay,
                contacts.LongestGapS,
                downlinkBitsPerS,
                storageNeededBits,
                storageCapacityBits
            );
        }

        public static double GetGeneratedBitsPerDay(Catalog catalog, MissionDesign design)
        {
            double bitsPerDay = 0;
            foreach (string instrumentId in design.InstrumentIds)
            {
                if (!string.IsNullOrEmpty(instrumentId) && catalog.Instruments.TryGetValue(instrumentId, out InstrumentSpec instrumentSpec))
                    bitsPerDay += instrumentSpec.dataRateBpS * instrumentSpec.dutyCycle * Constants.SecondsPerDay;
            }
            return bitsPerDay;
        }

        // Selected ground stations that can receive the given band.
        public static List<GroundStationSpec> GetCompatibleStations(Catalog catalog, MissionDesign design, string band)
        {
            var stations = new List<GroundStationSpec>();
            if (string.IsNullOrEmpty(band)) return stations;
            foreach (string stationId in design.GroundStationIds)
            {
                if (!string.IsNullOrEmpty(stationId) && catalog.GroundStations.TryGetValue(stationId, out GroundStationSpec stationSpec)
                    && stationSpec.bands != null && Array.Exists(stationSpec.bands, b => string.Equals(b, band, StringComparison.OrdinalIgnoreCase)))
                    stations.Add(stationSpec);
            }
            return stations;
        }
    }
}
