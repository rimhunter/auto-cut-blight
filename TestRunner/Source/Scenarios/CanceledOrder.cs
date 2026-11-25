using System.Linq;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.BlightHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class CanceledOrder
    {
        [Scenario("canceled-order-stays-canceled", MapSize = 40, SaveWithMods = "{mod}", SetupInSaveLaunch = true, ReloadInSaveLaunch = true)]
        static void CanceledOrderStaysCanceled(ScenarioContext s) => InWarmWeather(s, () => CanceledOrderStaysCanceledBody(s));

        static void CanceledOrderStaysCanceledBody(ScenarioContext s) {
            if (s.Stage == SaveStage.Setup) {
                SpawnBlighted(s.Map, "Plant_Potato", s.Cell(2, 0));
                SpawnBlighted(s.Map, "Plant_Potato", s.Cell(-4, 0));
                SpawnBlighted(s.Map, "Plant_Potato", s.Cell(8, 0));
                var cutOrders = s.RunUntil(() => new[] { s.Cell(2, 0), s.Cell(-4, 0), s.Cell(8, 0) }.All(c => IsCutDesignated(s.Map, c.GetPlant(s.Map))), 2500);
                s.Check(cutOrders, "setup: all three blighted potatoes should get a cut order");
                s.Map.designationManager.RemoveAllDesignationsOn(s.Cell(2, 0).GetPlant(s.Map));
                s.AreEqual("", DesignationsOn(s.Map, s.Cell(2, 0).GetPlant(s.Map)), "setup: the order on (2,0) is canceled");
                s.Tick(2500);
            }
            else s.Tick(300);

            var canceled = s.Cell(2, 0).GetPlant(s.Map);
            var keptA = s.Cell(-4, 0).GetPlant(s.Map);
            var keptB = s.Cell(8, 0).GetPlant(s.Map);
            var stage = s.Stage.ToString();
            s.Check(canceled != null && canceled.Blighted, stage + ": the potato at (2,0) is still there and blighted");
            s.AreEqual("", canceled == null ? "missing" : DesignationsOn(s.Map, canceled), stage + ": the canceled order on (2,0) stays canceled");
            s.AreEqual("CutPlant", keptA == null ? "missing" : DesignationsOn(s.Map, keptA), stage + ": the order on (-4,0), made before the save, is still there");
            s.AreEqual("CutPlant", keptB == null ? "missing" : DesignationsOn(s.Map, keptB), stage + ": the order on (8,0), made before the save, is still there");
            s.RequireNoLogErrors();
        }

        [Scenario("reblighted-plant-is-ordered-again", MapSize = 40)]
        static void ReblightedPlantIsOrderedAgain(ScenarioContext s) => InWarmWeather(s, () => ReblightedPlantIsOrderedAgainBody(s));

        static void ReblightedPlantIsOrderedAgainBody(ScenarioContext s) {
            var plant = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(2, 0));
            s.Check(s.RunUntil(() => IsCutDesignated(s.Map, plant), 2500), "the blighted potato at (2,0) should get a cut order; it has: " + DesignationsOn(s.Map, plant));

            s.Map.designationManager.RemoveAllDesignationsOn(plant);
            s.AreEqual("", DesignationsOn(s.Map, plant), "the order on (2,0) is canceled");
            s.Tick(130);
            s.AreEqual("", DesignationsOn(s.Map, plant), "the canceled order stays canceled while the same blight is still there");

            foreach (var blight in s.Map.thingGrid.ThingsListAt(plant.Position).Where(t => t.def == ThingDefOf.Blight).ToList()) blight.Destroy();
            s.Check(!plant.Blighted, "the blight on (2,0) is cleared");
            s.Tick(ScanTicks + 30);
            s.AreEqual("", DesignationsOn(s.Map, plant), "a plant with its blight cleared has no order");

            plant.CropBlighted();
            s.Check(plant.Blighted, "the same potato at (2,0) is blighted again");
            var again = s.RunUntil(() => IsCutDesignated(s.Map, plant), 2500);
            s.Check(again, "the re-blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: " + DesignationsOn(s.Map, plant));
            s.RequireNoLogErrors();
        }

        const int ScanTicks = 60;
    }
}
