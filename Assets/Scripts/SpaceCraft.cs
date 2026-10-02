using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MissionCore;
using UnityEngine;

namespace MissionGame
{
    public class SpaceCraft : MonoBehaviour, ISimulated
    {
        public int SimOrder { get; } = 1;

        [SerializeField] PlanetManager planetManager;
        [SerializeField] int rk4Step = 10;
        public float startAltitude = 800e3f;
        public float mass = 100f;
        public float fuelMass = 100f;
        public double isp = 220;

        public SpacecraftState state;

        public Planet orbitingPlanet;
        List<ICelestialBody> otherBodies;

        public OrbitInfo OrbitInfo { get; private set; }
        public double altitudeKM;
        public double speedMS;

        public List<ScheduledBurn> scheduledBurns = new();

        public event Action BurnScheduled;
        public event Action BurnExecuted;

        public void ScheduleBurn(double dvPrograde, double dvNormal, double dvRadial, double executeAt = -1)
        {
            ScheduleBurn(new(dvPrograde, dvNormal, dvRadial, executeAt));
        }
        public void ScheduleBurn(ScheduledBurn burn)
        {
            scheduledBurns.Add(burn);
            BurnScheduled?.Invoke();
        }

        void OnEnable()
        {
            planetManager.RegisterSimulatedObject(this);
        }
        void OnDisable()
        {
            planetManager.UnregisterSimulatedObject(this);
        }

        public void Start()
        {
            SimClock Clock = planetManager.Clock;
            otherBodies = planetManager.planetData.Where(p => p != orbitingPlanet).ToList<ICelestialBody>();

            KeplerOrbit orbit = KeplerOrbit.Circular(startAltitude, 51.6 * Math.PI / 180, 0, orbitingPlanet.radius * 1000, orbitingPlanet.Mu, Clock.SimTime);
            var (r0, v0) = orbit.StateAt(Clock.SimTime);
            state = new SpacecraftState(r0, v0, mass + fuelMass, mass);
        }

        void LateUpdate()
        {
            RenderStep();
        }

        public void SimStep(double dt)
        {
            for (int i = 0; i < dt; i += rk4Step)
            {
                for (int j = scheduledBurns.Count - 1; j >= 0; j--)
                {
                    var burn = scheduledBurns[j];
                    if (burn.executeAt <= planetManager.Clock.SimTime + i)
                    {
                        if (Maneuvers.TryApplyImpulse(state, burn.dvPrograde, burn.dvNormal, burn.dvRadial, isp, out state))
                            Debug.Log("Maneuver Successfull!");
                        else
                            Debug.Log("Not enough Fuel for Maneuver!");

                        scheduledBurns.RemoveAt(j);
                        BurnExecuted?.Invoke();
                    }
                }

                state = Integrator.RK4Step(state, planetManager.Clock.SimTime + i, rk4Step, orbitingPlanet, otherBodies);
            }
        }

        public void RenderStep()
        {
            SpacecraftState tempState = Integrator.RK4Step(state, planetManager.Clock.SimTime, planetManager.Clock.RenderTime - planetManager.Clock.SimTime, orbitingPlanet, otherBodies);
            transform.position = orbitingPlanet.WorldPositionAt(planetManager.Clock.RenderTime).ToUnity(RenderScale.PosScale)
            + tempState.relPos.ToUnity(RenderScale.PosScale * planetManager.bodyScale);
            transform.LookAt(orbitingPlanet.WorldPositionAt(planetManager.Clock.RenderTime).ToUnity(RenderScale.PosScale), tempState.velocity.ToUnity(1).normalized);

            OrbitInfo = OrbitInfo.FromState(tempState.relPos, tempState.velocity, orbitingPlanet.Mu, orbitingPlanet.Axis.Normalized);
            altitudeKM = (tempState.relPos.Length - orbitingPlanet.radius * 1000) / 1000;
            speedMS = tempState.velocity.Length;
        }
    }

    public readonly struct ScheduledBurn
    {
        public readonly double dvPrograde;
        public readonly double dvNormal;
        public readonly double dvRadial;
        public readonly double executeAt;

        public ScheduledBurn(double dvPrograde, double dvNormal, double dvRadial, double executeAt)
        {
            this.dvPrograde = dvPrograde;
            this.dvNormal = dvNormal;
            this.dvRadial = dvRadial;
            this.executeAt = executeAt;
        }

        public double TotalDeltaV => Math.Sqrt(dvPrograde * dvPrograde + dvNormal * dvNormal + dvRadial * dvRadial);

        public override string ToString()
        {
            string when = executeAt < 0
                ? "Now"
                : Constants.J2000DateTime.AddSeconds(executeAt).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            return $"{when}  {DeltaVText()}";
        }

        // With a countdown instead of a date, e.g. "T-00:12:30  Δv 282.5 m/s  (P +282.5 | N +0.0 | R +0.0)"
        public string ToString(double currentSimTime)
        {
            if (executeAt < 0)
                return $"Now  {DeltaVText()}";

            TimeSpan left = TimeSpan.FromSeconds(Math.Max(0, executeAt - currentSimTime));
            string countdown = left.Days > 0
                ? $"T-{left.Days}d {left:hh\\:mm\\:ss}"
                : $"T-{left:hh\\:mm\\:ss}";
            return $"{countdown}  {DeltaVText()}";
        }

        string DeltaVText() => string.Format(CultureInfo.InvariantCulture,
            "Δv {0:F1} m/s  (P {1:+0.0;-0.0} | N {2:+0.0;-0.0} | R {3:+0.0;-0.0})",
            TotalDeltaV, dvPrograde, dvNormal, dvRadial);
    }
}