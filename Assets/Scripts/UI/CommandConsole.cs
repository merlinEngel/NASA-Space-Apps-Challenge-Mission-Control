using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MissionCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MissionGame
{
    public class CommandConsole : MonoBehaviour
    {
        public SpaceCraft spaceCraft;
        public PlanetManager planetManager;

        record Command(string Usage, string Help, Func<string[], string> Run);
        readonly Dictionary<string, Command> commands = new(StringComparer.OrdinalIgnoreCase);

        public TMP_Text outputText;
        public ScrollRect scrollRect;
        readonly Queue<string> lines = new();
        const int MaxLines = 100;

        public void Log(string line)
        {
            lines.Enqueue(line);
            while (lines.Count > MaxLines) lines.Dequeue();   // drop old lines
            outputText.text = string.Join("\n", lines);
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0;         // scroll to the bottom
        }

        void Awake()
        {
            commands["impulse"] = new("impulse <P> <N> <R> [in <Time> | at apo|peri]", "Schedules an Impulse Burn", CmdImpulse);
            commands["help"] = new("help", "Shows all Commands", _ => string.Join("\n", commands.Select(c => c.Value.Usage)));
            commands["hohmann"] = new("hohmann <A> [in <Time> | at apo|peri]", "Executes a Hohmann Transfer", CmdHohmann);
        }

        public string Execute(string input)
        {
            input = input.Trim();
            if (input.Length == 0) return "";

            string[] tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string name = tokens[0];

            if (!commands.TryGetValue(name, out var command))
                return $"Unknown command '{name}'. Type 'help'.";

            string[] args = tokens.Skip(1).ToArray();     // everything except the command name

            try
            {
                return command.Run(args);
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;             // a bug in a command does not crash the game
            }
        }

        string CmdHohmann(string[] args)
        {
            if (args.Length != 1 && args.Length != 3)
                return "Error: Usage: " + commands["hohmann"].Usage;

            if (!TryNumber(args[0], out double newAltitude))
                return "Error: A must be a number (target altitude in km), for example 2000";
            if (newAltitude <= 0)
                return "Error: Target altitude must be above the surface (> 0 km)";

            SpacecraftState state = spaceCraft.state;
            double mu = spaceCraft.orbitingPlanet.Mu;

            // Hohmann only works from a circular orbit
            if (OrbitInfo.Eccentricity(state.relPos, state.velocity, mu) > 0.01)
                return "Error: Orbit is not circular – circularize first";

            double r1 = state.relPos.Length;                                        // current radius [m]
            double r2 = spaceCraft.orbitingPlanet.radius * 1000 + newAltitude * 1000; // target radius [m]

            (double dv1, double dv2, double transferTime) = Maneuvers.HohmannDeltaVs(r1, r2, mu);

            // Check fuel for both burns up front, otherwise the craft gets stuck on the transfer ellipse
            double needed = Math.Abs(dv1) + Math.Abs(dv2);
            double available = Maneuvers.AvailableDeltaV(state, spaceCraft.isp);
            if (needed > available)
                return $"Error: Needs {needed.ToString("F1", CultureInfo.InvariantCulture)} m/s, only {available.ToString("F1", CultureInfo.InvariantCulture)} m/s available";

            double now = planetManager.Clock.SimTime;
            double executeAt = -1;   // -1 = now (at the next step)

            if (args.Length == 3)
            {
                string mode = args[1].ToLowerInvariant();
                string value = args[2].ToLowerInvariant();

                if (mode == "in")
                {
                    if (!TryDuration(value, out double seconds))
                        return "Error: Provide time for example as 90s, 30m, 2h or 1d";
                    executeAt = now + seconds;
                }
                else if (mode == "at")
                {
                    double timeTo;

                    if (value == "apo" || value == "apoapsis")
                        timeTo = OrbitInfo.TimeToApoapsis(state.relPos, state.velocity, mu);
                    else if (value == "peri" || value == "periapsis")
                        timeTo = OrbitInfo.TimeToPeriapsis(state.relPos, state.velocity, mu);
                    else
                        return "Error: After 'at' use 'apo' or 'peri'";

                    // NaN only on an escape trajectory (circular orbit returns 0 = now)
                    if (double.IsNaN(timeTo))
                        return "Error: Orbit escape trajectory – There is no Apoapsis and Periapsis";

                    executeAt = now + timeTo;   // remaining time → absolute time
                }
                else
                    return "Error: Expected 'in' or 'at', not '" + args[1] + "'";
            }

            // Burn 1 turns the current point into the periapsis of the transfer ellipse,
            // so its apoapsis (= target altitude) is reached exactly half an ellipse later.
            double burn1Time = executeAt < 0 ? now : executeAt;
            ScheduledBurn burn1 = new(dv1, 0, 0, executeAt);
            ScheduledBurn burn2 = new(dv2, 0, 0, burn1Time + transferTime);

            spaceCraft.ScheduleBurn(burn1);
            spaceCraft.ScheduleBurn(burn2);

            return "Hohmann transfer scheduled:\n" + burn1.ToString(now) + "\n" + burn2.ToString(now);
        }

        string CmdImpulse(string[] args)
        {
            if (args.Length != 3 && args.Length != 5)
                return "Error: Usage: " + commands["impulse"].Usage;

            if (!TryNumber(args[0], out double dvPrograde) ||
                !TryNumber(args[1], out double dvNormal) ||
                !TryNumber(args[2], out double dvRadial))
                return "Error: P, N and R must be numbers, for example 282.5";

            // No time given: now (-1 = at the next step)
            double executeAt = -1;

            if (args.Length == 5)
            {
                double now = planetManager.Clock.SimTime;
                string mode = args[3].ToLowerInvariant();
                string value = args[4].ToLowerInvariant();

                if (mode == "in")
                {
                    if (!TryDuration(value, out double seconds))
                        return "Error: Provide time for example as 90s, 30m, 2h or 1d";
                    executeAt = now + seconds;
                }
                else if (mode == "at")
                {
                    SpacecraftState s = spaceCraft.state;
                    double mu = spaceCraft.orbitingPlanet.Mu;
                    double timeTo;

                    if (value == "apo" || value == "apoapsis")
                        timeTo = OrbitInfo.TimeToApoapsis(s.relPos, s.velocity, mu);
                    else if (value == "peri" || value == "periapsis")
                        timeTo = OrbitInfo.TimeToPeriapsis(s.relPos, s.velocity, mu);
                    else
                        return "Error: After 'at' use 'apo' or 'peri'";

                    // NaN only on an escape trajectory (circular orbit returns 0 = now)
                    if (double.IsNaN(timeTo))
                        return "Error: Orbit escape trajectory – There is no Apoapsis and Periapsis";

                    executeAt = now + timeTo;   // remaining time → absolute time
                }
                else
                    return "Error: Expected 'in' or 'at', not '" + args[3] + "'";
            }

            spaceCraft.ScheduleBurn(dvPrograde, dvNormal, dvRadial, executeAt);
            var burn = new ScheduledBurn(dvPrograde, dvNormal, dvRadial, executeAt);
            return "Burn scheduled: " + burn.ToString(planetManager.Clock.SimTime);
        }

        // Number always with a dot as decimal separator, regardless of the system language
        static bool TryNumber(string text, out double value) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        // "90s", "30m", "2h", "1d" (or just a number = seconds) → seconds
        static bool TryDuration(string text, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrEmpty(text)) return false;

            double factor = 1;
            switch (text[^1])
            {
                case 's': factor = 1; text = text[..^1]; break;
                case 'm': factor = 60; text = text[..^1]; break;
                case 'h': factor = 3600; text = text[..^1]; break;
                case 'd': factor = 86400; text = text[..^1]; break;
            }

            if (!TryNumber(text, out double amount) || amount < 0) return false;
            seconds = amount * factor;
            return true;
        }
    }
}