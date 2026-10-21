using System;
using System.Collections.Generic;
using MissionCore.Bodies;
using MissionCore.Budgets;
using MissionCore.Catalogs;
using MissionCore.Design;
using MissionCore.Earth;
using MissionCore.Mission;
using MissionCore.Numerics;
using MissionCore.Text;
using TMPro;
using UnityEngine;

namespace MissionGame.UI.Design
{
    // 3D preview of the chosen orbit: one line with a day/night gradient, ground stations as markers,
    // beta angle only in Realistic mode. Called by OrbitTab.OnDesignChanged.
    public class OrbitPreview : MonoBehaviour
    {
        private const double PreviewScale = 1e-6;   // 1 unit = 1000 km, Earth radius = 6.378 units
        private const int PointCount = 180;
        private const float Fade = 0.02f;           // colour fade at shadow entry/exit, as fraction of the orbit

        [SerializeField] private LineRenderer orbitLine;
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private TMP_Text betaAngleLabel;

        [SerializeField] private Color dayColor = new(0.93f, 0.91f, 0.86f);    // EDE8DC
        [SerializeField] private Color nightColor = new(0.29f, 0.32f, 0.30f);  // 4A524D

        // Should be the camera pivot (or contain the light as well), so the orbit keeps its place relative to the sun.
        [SerializeField] private Transform previewParent;
        [SerializeField] private float rotationSpeed = 10f;  // degrees per second

        private readonly List<GameObject> markers = new();

        private void Update()
        {
            if (previewParent != null)
                previewParent.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
        }

        public void Show(MissionDesign design, MissionTemplate mission)
        {
            if (design == null || mission == null || design.AltitudeM <= 0)
            {
                Clear();
                return;
            }

            double epochS = mission.EpochSeconds;
            double raanRad = mission.raanDeg * Constants.DegToRad;
            KeplerOrbit orbit = KeplerOrbit.Circular(
                design.AltitudeM,
                design.InclinationDeg * Constants.DegToRad,
                raanRad,
                Constants.RadiusEarth,
                Constants.MuEarth,
                epochS);

            Vec3d sun = Sun.Direction(epochS);
            OrbitPoint[] points = OrbitTrack.Sample(orbit, epochS, PointCount, sun);

            SetLine(orbitLine, points);
            orbitLine.colorGradient = MakeGradient(points);

            UpdateStations(design, mission);
            UpdateBetaLabel(design, raanRad, sun);
        }

        private void Clear()
        {
            orbitLine.positionCount = 0;
            foreach (GameObject marker in markers) marker.SetActive(false);
            betaAngleLabel.gameObject.SetActive(false);
        }

        private Gradient MakeGradient(OrbitPoint[] points)
        {
            var gradient = new Gradient();
            var alpha = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) };

            // OrbitTrack.Sample puts the shadow arc at the end of the array, so it starts at firstDark
            int firstDark = Array.FindIndex(points, p => p.InShadow);
            if (firstDark < 0)
            {
                gradient.SetKeys(new[] { new GradientColorKey(dayColor, 0f), new GradientColorKey(dayColor, 1f) }, alpha);
                return gradient;
            }

            float shadowStart = firstDark / (float)points.Length;   // 0..1 along the line
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(dayColor, 0f),
                    new GradientColorKey(dayColor, Mathf.Max(0f, shadowStart - Fade)),
                    new GradientColorKey(nightColor, shadowStart),
                    new GradientColorKey(nightColor, Mathf.Max(shadowStart, 1f - Fade)),
                    new GradientColorKey(dayColor, 1f),
                },
                alpha);
            return gradient;
        }

        private void UpdateStations(MissionDesign design, MissionTemplate mission)
        {
            List<GroundStationSpec> specs = new();
            foreach (string id in design.GroundStationIds)
                if (CatalogManager.Instance.Catalog.GroundStations.TryGetValue(id, out GroundStationSpec spec))
                    specs.Add(spec);

            double rotation = EarthRotation.Gmst(mission.EpochSeconds);

            for (int i = 0; i < specs.Count; i++)
            {
                if (i >= markers.Count) markers.Add(Instantiate(markerPrefab, transform));

                GameObject marker = markers[i];
                marker.SetActive(true);
                marker.transform.localPosition = GroundContact
                    .StationPosition(specs[i].latDeg, specs[i].lonDeg, rotation)
                    .ToUnity(PreviewScale);
            }

            // hide the markers that are left over from a previous, longer list
            for (int i = specs.Count; i < markers.Count; i++)
                markers[i].SetActive(false);
        }

        private void UpdateBetaLabel(MissionDesign design, double raanRad, Vec3d sun)
        {
            DisplayFormatter formatter = CatalogManager.Instance.Formatter;
            bool realistic = formatter.Mode == DisplayMode.Realistic;
            betaAngleLabel.gameObject.SetActive(realistic);
            if (!realistic) return;

            double betaDeg = Shadow.BetaAngle(design.InclinationDeg * Constants.DegToRad, raanRad, sun) * Constants.RadToDeg;
            betaAngleLabel.text = CatalogManager.Instance.Texts.Get("orbit.beta_angle", formatter.Value(Metric.InclinationDeg, betaDeg));
        }

        private static void SetLine(LineRenderer line, OrbitPoint[] points)
        {
            var positions = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
                positions[i] = points[i].PositionM.ToUnity(PreviewScale);

            line.positionCount = positions.Length;
            line.SetPositions(positions);
            line.loop = true;
        }
    }
}
