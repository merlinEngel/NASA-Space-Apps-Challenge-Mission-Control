using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using MissionCore.Catalogs;
using MissionCore.Mission;
using MissionCore.Numerics;
using MissionCore.Simulation;

public static class SimRun
{
    // simrun <design> [--seed N] [--data <dir>]
    // Returns 0 on success, 1 on bad arguments or missing files.
    public static int Run(string[] args, TextWriter output)
    {
        bool showTime = false;
        string designName = null;
        int seed = 42;
        string dataDir = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed":
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out seed))
                    {
                        output.WriteLine("--seed needs a whole number, e.g. --seed 42");
                        return 1;
                    }
                    break;
                case "--data":
                    if (i + 1 >= args.Length) { output.WriteLine("--data needs a folder"); return 1; }
                    dataDir = args[++i];
                    break;
                case "--time": showTime = true; break;
                default:
                    designName = args[i];   // everything that is not an option is the design
                    break;
            }
        }

        dataDir ??= FindDataDir();
        if (dataDir == null || !Directory.Exists(dataDir))
        {
            output.WriteLine("data folder not found; run from inside the project or pass --data <dir>");
            return 1;
        }

        if (designName == null)
        {
            output.WriteLine("usage: simrun <design> [--seed N] [--data <dir>]");
            return 1;
        }
        Catalog catalog = CatalogLoader.LoadAll(file =>
        {
            string p = Path.Combine(dataDir, file);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        });

        string designPath = File.Exists(designName) ? designName : Path.Combine(dataDir, "TestDesigns", designName + ".json");
        string designFileContent = File.Exists(designPath) ? File.ReadAllText(designPath) : null;
        if (designFileContent == null) {
            output.WriteLine($"design '{designName}' not found");
            return 1;
        }
        MissionDesign design = CatalogLoader.LoadObject<MissionDesign>(designFileContent, Path.GetFileName(designPath));

        Stopwatch watch = Stopwatch.StartNew();
        MissionSimulation sim = MissionSimulation.Simulate(catalog, design, seed);
        watch.Stop();

        SimSummary summary = SimSummary.From(sim);

        var parts = new List<string>();
        foreach (ScheduledEvent e in sim.EventSchedule)
            parts.Add(string.Format(CultureInfo.InvariantCulture, "{0} on day {1:0.00}", e.Type, e.TimeS / Constants.SecondsPerDay));

        string eventsText = parts.Count > 0 ? string.Join(", ", parts) : "none";

        const int labelWidth = 8;
        output.WriteLine($"{"design:",-labelWidth} {design.Name} (seed {seed})");
        output.WriteLine($"{"result:",-labelWidth} {summary}");
        output.WriteLine($"{"end:",-labelWidth} {summary.EndReason}, altitude {string.Format(CultureInfo.InvariantCulture, "{0:0.0}", summary.FinalAltitudeM/1000)} km");
        output.WriteLine($"{"events:",-labelWidth} {eventsText}");
        if (showTime) output.WriteLine($"{"time:",-labelWidth} {watch.ElapsedMilliseconds} ms");

        return 0;
    }

    // Walks up from the current folder until Assets/StreamingAssets/Data exists.
    static string FindDataDir()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Assets", "StreamingAssets", "Data");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }
}