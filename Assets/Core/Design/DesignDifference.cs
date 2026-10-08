using System.Collections.Generic;

namespace MissionCore
{
    public static class DesignDifference
    {
        public static List<DesignChange> Between(MissionDesign before, MissionDesign after)
        {
            List<DesignChange> changes = new();

            if (PartChanged(before.OrbitPreset, after.OrbitPreset, out ChangeKind? kind)) changes.Add(new((ChangeKind)kind, DesignField.OrbitPreset, before.OrbitPreset, after.OrbitPreset));
            if (before.AltitudeM != after.AltitudeM)
                changes.Add(new(ChangeKind.Value, DesignField.Altitude, new(Metric.AltitudeM, before.AltitudeM, after.AltitudeM)));
            if (before.InclinationDeg != after.InclinationDeg)
                changes.Add(new(ChangeKind.Value, DesignField.Inclination, new(Metric.InclinationDeg, before.InclinationDeg, after.InclinationDeg)));
            if (before.PropellantMassKg != after.PropellantMassKg)
                changes.Add(new(ChangeKind.Value, DesignField.PropellantMass, new(Metric.MassKg, before.PropellantMassKg, after.PropellantMassKg)));
            if (before.SolarAreaM2 != after.SolarAreaM2)
                changes.Add(new(ChangeKind.Value, DesignField.SolarArea, new(Metric.Area, before.SolarAreaM2, after.SolarAreaM2)));
            if (before.BatteryCount != after.BatteryCount)
                changes.Add(new(ChangeKind.Value, DesignField.BatteryCount, new(Metric.Raw, before.BatteryCount, after.BatteryCount)));

            if (PartChanged(before.PlatformId, after.PlatformId, out kind)) changes.Add(new((ChangeKind)kind, DesignField.Platform, before.PlatformId, after.PlatformId));
            if (PartChanged(before.CommsId, after.CommsId, out kind)) changes.Add(new((ChangeKind)kind, DesignField.Comms, before.CommsId, after.CommsId));
            if (PartChanged(before.SolarCellId, after.SolarCellId, out kind)) changes.Add(new((ChangeKind)kind, DesignField.SolarCell, before.SolarCellId, after.SolarCellId));
            if (PartChanged(before.BatteryId, after.BatteryId, out kind)) changes.Add(new((ChangeKind)kind, DesignField.Battery, before.BatteryId, after.BatteryId));
            if (PartChanged(before.LauncherId, after.LauncherId, out kind)) changes.Add(new((ChangeKind)kind, DesignField.Launcher, before.LauncherId, after.LauncherId));
            if (PartChanged(before.PropulsionId, after.PropulsionId, out kind)) changes.Add(new((ChangeKind)kind, DesignField.Propulsion, before.PropulsionId, after.PropulsionId));

            changes.AddRange(PartsChanges(before.InstrumentIds, after.InstrumentIds, DesignField.Instruments));
            changes.AddRange(PartsChanges(before.GroundStationIds, after.GroundStationIds, DesignField.GroundStations));

            return changes;
        }

        private static bool PartChanged(string before, string after, out ChangeKind? kind)
        {
            kind = null;

            if (string.IsNullOrEmpty(before) && !string.IsNullOrEmpty(after)) { kind = ChangeKind.Add; return true; }
            else if (!string.IsNullOrEmpty(before) && string.IsNullOrEmpty(after)) { kind = ChangeKind.Remove; return true; }
            else if (!string.IsNullOrEmpty(before) && !string.IsNullOrEmpty(after) && before != after) { kind = ChangeKind.Replace; return true; }
            else return false;
        }

        private static List<DesignChange> PartsChanges(List<string> before, List<string> after, DesignField field)
        {
            before ??= new();
            after ??= new();

            List<DesignChange> changes = new();

            foreach (string id in before)
            {
                if (!after.Contains(id)) changes.Add(new(ChangeKind.Remove, field, id, null));
            }
            foreach (string id in after)
            {
                if (!before.Contains(id)) changes.Add(new(ChangeKind.Add, field, null, id));
            }

            return changes;
        }
    }
}