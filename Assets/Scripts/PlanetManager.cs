using System.Collections.Generic;
using MissionCore;
using UnityEngine;

namespace MissionGame
{
    public class PlanetManager : Singleton<PlanetManager>
    {

        [Header("Time")]
        public int startTimeSinceJ2000 = 0;
        public double timeScale = 86400;      // sim seconds per real-time second (86400 = 1 day/s)
        public double stepSeconds = 60;       // fixed simulation step
        public int maxStepsPerFrame = 2000;   // protection against freezing at huge time warp

        [Header("Rendering")]
        public GameObject planetObjectPrefab;
        public List<Planet> planetData = new(); // fill in the Inspector
        public float bodyScale = 1f;            // draw planets enlarged (1 = real size)
        public Planet cameraTarget;             // body the camera follows (e.g. Earth)
        public float cameraHeight = 3f;       // camera distance above the target in million km
        public float cloudTurnsPerDay = 0.35f;

        const float SunRadiusKm = 695700f;

        readonly Dictionary<Planet, (Transform holder, Transform body, Quaternion tilt)> planets = new();
        public GameObject sunPrefab;
        GameObject sunObject;

        public SimClock Clock { get; private set; }
        public int StepsLastFrame { get; private set; }
        public bool paused;

        public List<ISimulated> simulatedObjects = new();

        public void RegisterSimulatedObject(ISimulated obj)
        {
            simulatedObjects.Add(obj);
            simulatedObjects.Sort((x, y) => x.SimOrder.CompareTo(y.SimOrder));
        }
        public void UnregisterSimulatedObject(ISimulated obj)
        {
            simulatedObjects.Remove(obj);
        }

        void Awake()
        {
            base.Awake();
            Clock = new SimClock(stepSeconds, startTimeSinceJ2000);
        }

        void Start()
        {
            sunObject = Instantiate(sunPrefab, transform);
            sunObject.transform.position = Vector3.zero;
            sunObject.transform.localScale = Vector3.one * (2 * SunRadiusKm * RenderScale.SizeScale * bodyScale);

            foreach (Planet planet in planetData)
            {
                // The unscaled holder moves, the sphere below it is only scaled.
                // That way the camera as a child is not scaled along.
                Transform holder = new GameObject(planet.name).transform;
                holder.SetParent(transform, false);

                GameObject body = Instantiate(planetObjectPrefab, holder);
                body.name = planet.name + " (Sphere)";
                body.GetComponent<MeshRenderer>().sharedMaterial = planet.material;
                body.transform.localScale = Vector3.one * (2 * planet.radius * RenderScale.SizeScale * bodyScale);

                Quaternion tilt = Quaternion.FromToRotation(Vector3.up, planet.Axis.ToUnity(1));

                planets[planet] = (holder, body.transform, tilt);

                if (planet == cameraTarget && Camera.main != null)
                {
                    Transform cam = Camera.main.transform;
                    cam.SetParent(holder, false);
                    cam.SetLocalPositionAndRotation(new Vector3(0, cameraHeight, 0), Quaternion.Euler(90, 0, 0));
                }
            }
            UpdatePositions();
        }

        void Update()
        {
            Clock.TimeScale = timeScale;
            Clock.MaxStepsPerAdvance = maxStepsPerFrame;
            Clock.Paused = paused;

            int steps = Clock.Advance(Time.deltaTime);
            for (int i = 0; i < steps; i++)
            {
                SimStep(Clock.StepSeconds);
                Clock.Tick();
            }

            StepsLastFrame = steps;
            UpdatePositions();
            UpdateRotations();
        }

        void LateUpdate()
        {
            double turns = Clock.RenderTime / 86400.0 * cloudTurnsPerDay;
            Shader.SetGlobalFloat("_CloudOffset", (float)(turns % 1.0));
            Shader.SetGlobalVector("_SunPos", sunObject.transform.position - Camera.main.transform.position);
        }

        void SimStep(double dt)
        {
            foreach(ISimulated obj in simulatedObjects)
            {
                obj.SimStep(dt);
            }
        }

        void UpdatePositions()
        {
            double t = Clock.RenderTime;
            foreach (var kv in planets)
                kv.Value.holder.position = kv.Key.WorldPositionAt(t).ToUnity(RenderScale.PosScale);
        }
        
        void UpdateRotations()
        {
            double t = Clock.RenderTime;
            foreach (var kv in planets)
            {
                float rot = (float)(t/kv.Key.rotationPeriodSeconds%1)*360f;
                kv.Value.body.rotation = kv.Value.tilt * Quaternion.AngleAxis(-rot, Vector3.up);
            }
        }
    }
}