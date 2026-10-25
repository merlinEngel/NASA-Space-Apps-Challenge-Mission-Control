using System;
using System.Collections.Generic;
using MissionCore.Design;
using MissionCore.Mission;
using MissionCore.Simulation;
using MissionCore.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MissionGame.Design
{
    public enum SessionStage { Design, Review, Launch, Operations, Result }
    public enum Screen { Design, Review, Launch, Operations, Result }

    public static class SessionStageExtensions
    {
        public static Screen GetScreen(this SessionStage stage)
        {
            return stage switch
            {
                SessionStage.Design => Screen.Design,
                SessionStage.Launch => Screen.Launch,
                SessionStage.Review => Screen.Review,
                SessionStage.Operations => Screen.Operations,
                SessionStage.Result => Screen.Result,
                _ => throw new ArgumentException()
            };
        }
    }

    public static class Screens
    {
        public static void LoadScreen(Screen screen)
        {
            SceneManager.LoadScene(screen switch
            {
                Screen.Design => "Design Scene",
                Screen.Launch => "Launch Scene",
                Screen.Review => "FRR Scene",
                Screen.Operations => "Operations Scene",
                Screen.Result => "Result Scene",
                _ => throw new ArgumentException()
            });
        }
    }

    [DefaultExecutionOrder(-90)]
    public class MissionSession : Singleton<MissionSession>
    {
        [SerializeField] int fixedSeed = 0;   // 0 = new seed each launch; e.g. 42 to replay a run in the editor

        public SessionStage Stage { get; private set; } = SessionStage.Design;
        public MissionDesign Design { get; private set; }      // null until the player leaves the design screen once
        public DisplayMode Mode { get; private set; }
        public int Seed { get; private set; }
        public ReviewResult Review { get; private set; }
        public LaunchOutcome Launch { get; private set; }
        public MissionSimulation Simulation { get; private set; }


        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            DontDestroyOnLoad(this);
        }


        public void BeginReview(MissionDesign design)
        {
            Design = design.DeepCopy();
            Mode = CatalogManager.Instance.Formatter.Mode;
            Review = FlightReadinessReview.Review(CatalogManager.Instance.Catalog, Design);
            Stage = SessionStage.Review;

            Screens.LoadScreen(Screen.Review);
        }

        public void BackToDesign()
        {
            Stage = SessionStage.Design;
            Review = null;
            Launch = default;
            Simulation = null;

            Screens.LoadScreen(Screen.Design);
        }

        public bool TryLaunch(IEnumerable<ReviewCode> confirmed)
        {
            if (!Review.CanLaunchWith(confirmed)) return false;

            Seed = fixedSeed != 0 ? fixedSeed : Environment.TickCount;
            Launch = LaunchRoll.Roll(CatalogManager.Instance.Catalog.Launchers[Design.LauncherId], Mode, Seed);
            Stage = SessionStage.Launch;
            Screens.LoadScreen(Screen.Launch);
            return true;
        }

        public void FinishLaunch()
        {
            if (Launch.Success)
            {
                Simulation = MissionSimulation.Start(CatalogManager.Instance.Catalog, Design, Seed);
                Stage = SessionStage.Operations;
                Screens.LoadScreen(Screen.Operations);
            }
            else
            {
                Stage = SessionStage.Result;
                Screens.LoadScreen(Screen.Result);
            }
        }
    }
}