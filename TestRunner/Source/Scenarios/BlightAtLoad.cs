using System.Linq;
using Verse;
using static RimhunterTestRunner.Scenarios.BlightHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class BlightAtLoad
    {
        [Scenario("blight-present-at-load", MapSize = 40, SaveWithMods = "", SetupInSaveLaunch = true)]
        static void BlightPresentAtLoad(ScenarioContext s) => InWarmWeather(s, () => BlightPresentAtLoadBody(s));

        static void BlightPresentAtLoadBody(ScenarioContext s) {
            var cells = new[] { s.Cell(-3, 0), s.Cell(0, 2), s.Cell(5, -1) };
            if (s.Stage == SaveStage.Setup) {
                foreach (var c in cells) SpawnBlighted(s.Map, "Plant_Potato", c);
                s.Tick(300);
                s.AreEqual(3, cells.Count(c => c.GetPlant(s.Map)?.Blighted == true), "setup: three potatoes are blighted in the save");
                s.AreEqual(0, cells.Count(c => s.Map.designationManager.HasMapDesignationOn(c.GetPlant(s.Map))), "setup: the save was made without the mod, so nothing is designated");
                return;
            }

            s.AreEqual(3, cells.Count(c => c.GetPlant(s.Map)?.Blighted == true), "the loaded map still has three blighted potatoes");
            s.AreEqual(0, cells.Count(c => s.Map.designationManager.HasMapDesignationOn(c.GetPlant(s.Map))), "before the first tick no potato is designated");
            var done = s.RunUntil(() => cells.All(c => IsCutDesignated(s.Map, c.GetPlant(s.Map))), 2500);
            s.Check(done, "the blight that was on the map at load should carry a cut order within 2,500 ticks: " + string.Join(" ", cells.Select(c => Offset(s, c) + "=" + DesignationsOn(s.Map, c.GetPlant(s.Map)))));
            s.RequireNoLogErrors();
        }
    }
}
