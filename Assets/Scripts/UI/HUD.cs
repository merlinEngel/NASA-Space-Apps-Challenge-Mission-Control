using System;
using System.Collections.Generic;
using System.Globalization;
using MissionCore;
using TMPro;
using UnityEngine;

namespace MissionGame
{
    public class HUD : MonoBehaviour
    {
        public TMP_Text simDateTimeText;
        public TMP_Text altitudeText;
        public TMP_Text speedText;
        public TMP_Text apoapsisText;
        public TMP_Text periapsisText;
        public TMP_Text orbitalPeriodText;
        public CommandConsole commandConsole;

        public TMP_Text scheduledBurnPrefab;
        public List<(TMP_Text text, ScheduledBurn burn)> scheduledBurnTexts = new();

        public GameObject scheduledBurnsContainer;

        public TMP_InputField spacecraftCommandInputField;

        public SpaceCraft spaceCraft;

        double deltaTime;

        void OnEnable() { spaceCraft.BurnScheduled += RebuildBurnList; spaceCraft.BurnExecuted += RebuildBurnList; }
        void OnDisable() { spaceCraft.BurnScheduled -= RebuildBurnList; spaceCraft.BurnExecuted -= RebuildBurnList; }

        public void UpdateSpacecraftStats(SpaceCraft spaceCraft)
        {
            double radiusM = spaceCraft.orbitingPlanet.radius * 1000;
            double apoapsisKM = (spaceCraft.OrbitInfo.apoapsis - radiusM) / 1000;
            double periapsisKM = (spaceCraft.OrbitInfo.periapsis - radiusM) / 1000;
            double speedMS = spaceCraft.speedMS;
            double orbitalPeriodSeconds = spaceCraft.OrbitInfo.orbitalPeriod;
            bool escape = double.IsNaN(orbitalPeriodSeconds) || orbitalPeriodSeconds <= 0;

            altitudeText.text = $"Altitude: {spaceCraft.altitudeKM:F1} km";
            speedText.text = $"Speed: {speedMS:F1} m/s";
            apoapsisText.text = escape ? "Apoapsis: Escape" : $"Apoapsis: {apoapsisKM:F1} km";
            periapsisText.text = $"Periapsis: {periapsisKM:F1} km";
            orbitalPeriodText.text = escape
                ? "Orbital Period: Escape"
                : $"Orbital Period: {Math.Floor(orbitalPeriodSeconds / 3600)}h {Math.Floor(orbitalPeriodSeconds % 3600 / 60):00}m";
        }
        public void Update()
        {
            deltaTime += Time.deltaTime;
            if (deltaTime >= 0.5)
            {
                deltaTime -= 0.5;
                UpdateSpacecraftStats(spaceCraft);
                foreach ((TMP_Text text, ScheduledBurn burn) in scheduledBurnTexts)
                {
                    text.text = burn.ToString(PlanetManager.Instance.Clock.RenderTime);
                }
            }
            simDateTimeText.text = Constants.J2000DateTime.AddSeconds(PlanetManager.Instance.Clock.SimTime).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        void Start()
        {
            spacecraftCommandInputField.onSubmit.AddListener(OnCommandSubmitted);
        }

        void OnCommandSubmitted(string text)
        {
            commandConsole.Log("> " + text);                    // show the input
            string result = commandConsole.Execute(text);       // execute the command
            if (result.Length > 0) commandConsole.Log(result);  // show the response

            spacecraftCommandInputField.text = "";              // clear the field
            spacecraftCommandInputField.ActivateInputField();   // keep typing right away
        }

        private void RebuildBurnList()
        {
            foreach (var (text, _) in scheduledBurnTexts) Destroy(text.gameObject);
            scheduledBurnTexts.Clear();
            foreach (ScheduledBurn burn in spaceCraft.scheduledBurns)
            {
                TMP_Text burnText = Instantiate(scheduledBurnPrefab, scheduledBurnsContainer.transform);
                burnText.text = burn.ToString(PlanetManager.Instance.Clock.RenderTime);

                scheduledBurnTexts.Add((burnText, burn));
            }
        }
    }
}