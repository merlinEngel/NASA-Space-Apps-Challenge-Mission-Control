using System;

namespace MissionCore
{
    public enum ChangeKind
    {
        Replace,
        Add,
        Remove,
        Value
    }

    public enum DesignField
    {
        // catalog ids: show the part name
        Platform,
        Instruments,
        Launcher,
        Propulsion,
        Comms,
        Battery,
        SolarCell,
        GroundStations,
        OrbitPreset,
        BatteryCount,
        SolarArea,
        PropellantMass,
        Altitude,
        Inclination
    }

    public static class DesignFieldExtensions
    {
        public static string TextKey(this DesignField field) => field switch
        {
            DesignField.Platform => "tab.platform",
            DesignField.Instruments => "ui.col.instrument",
            DesignField.Launcher => "ui.col.launcher",
            DesignField.Propulsion => "ui.col.propulsion",
            DesignField.Comms => "ui.col.transmitter",
            DesignField.Battery => "ui.col.battery",
            DesignField.SolarCell => "ui.col.cell_type",
            DesignField.GroundStations => "ui.col.station",
            DesignField.OrbitPreset => "ui.col.preset",
            DesignField.BatteryCount => "ui.battery_count",
            DesignField.SolarArea => "ui.solar_area",
            DesignField.PropellantMass => "ui.propellant",
            DesignField.Altitude => "ui.altitude",
            DesignField.Inclination => "ui.inclination",
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };
    }

    public readonly struct DesignChange
    {
        public readonly ChangeKind Kind;
        public readonly DesignField Field;
        public readonly string BeforeId;
        public readonly string AfterId;
        public readonly MetricDelta? Value;

        public DesignChange(ChangeKind kind, DesignField field, string beforeId, string afterId)
        {
            Kind = kind;
            Field = field;
            BeforeId = beforeId;
            AfterId = afterId;
            Value = null;
        }
        public DesignChange(ChangeKind kind, DesignField field, MetricDelta value)
        {
            Kind = kind;
            Field = field;
            BeforeId = null;
            AfterId = null;
            Value = value;
        }

        public string Format(TextTable texts, DisplayFormatter formatter)
        {
            return Kind switch
            {
                ChangeKind.Replace => texts.Get("change.replace", texts.Get("part." + AfterId), texts.Get("part." + BeforeId)),
                ChangeKind.Add => texts.Get("change.add", texts.Get("part." + AfterId)),
                ChangeKind.Remove => texts.Get("change.remove", texts.Get("part." + BeforeId)),
                ChangeKind.Value => Value != null ? texts.Get("change.value", texts.Get(Field.TextKey()), formatter.Value(Value.Value.Metric, Value.Value.After)) : "",
                _ => "Not Implemented"
            };
        }
    }
}