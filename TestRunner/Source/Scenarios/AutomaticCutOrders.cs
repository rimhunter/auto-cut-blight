using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.BlightHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class AutomaticCutOrders
    {
        [Scenario("blight-is-cut-without-orders", MapSize = 40)]
        static void BlightIsCut(ScenarioContext s) => CutWithoutOrders(s);

        internal static void CutWithoutOrders(ScenarioContext s) => InWarmWeather(s, () => CutBlightedPotato(s));

        static void CutBlightedPotato(ScenarioContext s) {
            GrowingZone(s, CellRect.FromLimits(s.Cell(-5, -1), s.Cell(3, 1)));
            var sick = SpawnPlant(s.Map, "Plant_Potato", s.Cell(2, 0));
            var healthy = SpawnPlant(s.Map, "Plant_Potato", s.Cell(-4, 0));
            var pawn = s.SpawnColonist(s.Cell(0, 6));
            DisableGrowing(pawn);
            pawn.skills.GetSkill(SkillDefOf.Plants).Level = 20;

            s.AreEqual("", DesignationsOn(s.Map, sick), "the potato at (2,0) has no designation before it is blighted");
            sick.CropBlighted();
            s.Check(sick.Blighted, "the potato at (2,0) should be blighted");
            s.AreEqual("", DesignationsOn(s.Map, sick), "no designation on the new blight before the first tick");

            var designated = s.RunUntil(() => IsCutDesignated(s.Map, sick), 2500);
            s.Check(designated, "the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: " + DesignationsOn(s.Map, sick));

            var sickId = "Thing_" + sick.ThingID;
            var cutJob = s.RunUntil(() => s.Jobs.Any(j => j.Def == "CutPlantDesignated" && j.TargetA == sickId), 2500);
            s.Check(cutJob, "the colonist should start a CutPlantDesignated job on the potato at (2,0); the jobs are: " + string.Join(" | ", s.Jobs));

            s.RunUntil(() => !sick.Spawned, 2500);
            s.Check(!sick.Spawned, "the colonist should cut the blighted potato");
            s.Check(healthy.Spawned, "the healthy potato at (-4,0) should still be spawned");
            s.AreEqual("", DesignationsOn(s.Map, healthy), "the healthy potato at (-4,0) has no designation");
            s.Check(!s.Jobs.Any(j => j.TargetA == "Thing_" + healthy.ThingID), "no job should target the healthy potato; the jobs are: " + string.Join(" | ", s.Jobs));
            s.RequireNoLogErrors();
        }

        static List<Plant> Block(ScenarioContext s, int centerZ) {
            var plants = new List<Plant>();
            for (var dz = -1; dz <= 1; dz++)
                for (var dx = -1; dx <= 1; dx++)
                    plants.Add(SpawnPlant(s.Map, "Plant_Potato", s.Cell(dx, centerZ + dz)));
            return plants;
        }

        static bool EveryBlightedPlantIsCutDesignated(ScenarioContext s, List<Plant> plants) =>
            plants.Where(p => p.Blighted).All(p => IsCutDesignated(s.Map, p));

        [Scenario("blight-spreads-and-is-cut", MapSize = 40)]
        static void BlightSpreads(ScenarioContext s) => InWarmWeather(s, () => BlightSpreadsBody(s));

        static void BlightSpreadsBody(ScenarioContext s) {
            var plants = Block(s, 0);
            plants[4].CropBlighted();
            s.Check(s.RunUntil(() => EveryBlightedPlantIsCutDesignated(s, plants), 200), "the first blighted plant should be designated");
            s.AreEqual(1, plants.Count(p => p.Blighted), "one plant is blighted at the start");

            for (var step = 1; step <= 5; step++) {
                var before = plants.Where(p => p.Blighted).ToList();
                before.First().Blight.TryReproduceNow();
                var fresh = plants.Where(p => p.Blighted && !before.Contains(p)).ToList();
                s.AreEqual(1, fresh.Count, "spread step " + step + " blights exactly one more plant");
                if (fresh.Count != 1) break;
                s.AreEqual("", DesignationsOn(s.Map, fresh[0]), "spread step " + step + ": the newly blighted plant is undesignated at the moment it appears");
                var done = s.RunUntil(() => IsCutDesignated(s.Map, fresh[0]), 130);
                s.Check(done, "spread step " + step + ": the newly blighted plant at " + Offset(s, fresh[0].Position) + " should be designated within 130 ticks; it has: " + DesignationsOn(s.Map, fresh[0]));
            }

            s.AreEqual(6, plants.Count(p => p.Blighted), "five spreads from one plant leave six blighted plants");
            s.AreEqual(6, plants.Count(p => IsCutDesignated(s.Map, p)), "all six blighted plants carry one CutPlant designation: " + DesignationReport(s, plants));
            s.RequireNoLogErrors();
        }

        [Scenario("five-plants-all-designated", MapSize = 50)]
        static void FivePlantsAllDesignated(ScenarioContext s) => InWarmWeather(s, () => FivePlantsAllDesignatedBody(s));

        static void FivePlantsAllDesignatedBody(ScenarioContext s) {
            var row = new List<Plant>();
            for (var dx = -2; dx <= 2; dx++) row.Add(SpawnBlighted(s.Map, "Plant_Potato", s.Cell(dx, 12)));

            var basins = new List<Building>();
            for (var dx = -2; dx <= 2; dx++) basins.Add(s.SpawnBuilding("HydroponicsBasin", s.Cell(dx, 0)));
            var cells = basins.SelectMany(b => b.OccupiedRect().Cells).ToList();
            var expectedCells = new List<string>();
            for (var dx = -2; dx <= 2; dx++)
                for (var dz = -1; dz <= 2; dz++)
                    expectedCells.Add("(" + dx + "," + dz + ")");
            s.AreEqual(string.Join(" ", expectedCells.OrderBy(c => c)), string.Join(" ", cells.Select(c => Offset(s, c)).OrderBy(c => c)), "five side by side hydroponics basins cover 20 cells");

            var basinPlants = cells.Select(c => SpawnBlighted(s.Map, "Plant_Rice", c)).ToList();
            var all = row.Concat(basinPlants).ToList();
            s.AreEqual(25, all.Count(p => p.Blighted), "25 plants are blighted: 5 in a row and 20 on the basins");
            s.AreEqual(0, all.Count(p => s.Map.designationManager.HasMapDesignationOn(p)), "no plant is designated before the first tick");

            var done = s.RunUntil(() => all.All(p => IsCutDesignated(s.Map, p)), 2500);
            s.Check(done, "all 25 blighted plants should carry one CutPlant designation within 2,500 ticks; the state is: " + DesignationReport(s, all.Where(p => !IsCutDesignated(s.Map, p))));
            s.AreEqual(5, row.Count(p => IsCutDesignated(s.Map, p)), "all five plants of the row are designated, not the first three");
            s.AreEqual(20, basinPlants.Count(p => IsCutDesignated(s.Map, p)), "all 20 hydroponics plants are designated");
            s.RequireNoLogErrors();
        }

        [Scenario("healthy-plants-untouched", MapSize = 40)]
        static void HealthyPlantsUntouched(ScenarioContext s) => InWarmWeather(s, () => HealthyPlantsUntouchedBody(s));

        static void HealthyPlantsUntouchedBody(ScenarioContext s) {
            var plants = Block(s, 0);
            var tree = SpawnPlant(s.Map, "Plant_TreeOak", s.Cell(12, 12));
            var crop = SpawnPlant(s.Map, "Plant_Potato", s.Cell(-12, 12));
            plants[4].CropBlighted();
            for (var step = 0; step < 3; step++) {
                plants.Where(p => p.Blighted).First().Blight.TryReproduceNow();
                s.Tick(130);
            }
            s.Tick(600);
            s.AreEqual(4, plants.Count(p => p.Blighted), "the block ends with four blighted plants");
            s.AreEqual(5, plants.Count(p => !p.Blighted), "the block ends with five healthy plants");
            foreach (var p in plants.Where(p => !p.Blighted))
                s.AreEqual("", DesignationsOn(s.Map, p), "the healthy plant at " + Offset(s, p.Position) + " has no designation");
            foreach (var p in plants.Where(p => p.Blighted))
                s.AreEqual("CutPlant", DesignationsOn(s.Map, p), "the blighted plant at " + Offset(s, p.Position) + " has a cut order");
            s.AreEqual("", DesignationsOn(s.Map, tree), "the healthy oak far away has no designation");
            s.AreEqual("", DesignationsOn(s.Map, crop), "the healthy potato far away has no designation");
            s.RequireNoLogErrors();
        }

        [Scenario("existing-designation-kept", MapSize = 40)]
        static void ExistingDesignationKept(ScenarioContext s) => InWarmWeather(s, () => ExistingDesignationKeptBody(s));

        static void ExistingDesignationKeptBody(ScenarioContext s) {
            var harvest = SpawnPlant(s.Map, "Plant_Potato", s.Cell(-4, 0));
            var cut = SpawnPlant(s.Map, "Plant_Potato", s.Cell(0, 0));
            var plain = SpawnPlant(s.Map, "Plant_Potato", s.Cell(4, 0));
            s.Map.designationManager.AddDesignation(new Designation(harvest, DesignationDefOf.HarvestPlant));
            s.Map.designationManager.AddDesignation(new Designation(cut, DesignationDefOf.CutPlant));
            foreach (var p in new[] { harvest, cut, plain }) p.CropBlighted();

            s.AreEqual("HarvestPlant", DesignationsOn(s.Map, harvest), "before the first scan the harvest plant has its harvest order");
            s.Tick(300);
            s.AreEqual("HarvestPlant", DesignationsOn(s.Map, harvest), "a blighted plant with a harvest order keeps exactly that order and gets no cut order");
            s.AreEqual("CutPlant", DesignationsOn(s.Map, cut), "a blighted plant that already has a cut order keeps one cut order, not two");
            s.AreEqual("CutPlant", DesignationsOn(s.Map, plain), "the blighted plant with no order gets one cut order");
            s.RequireNoLogErrors();
        }

        [Scenario("any-plant-kind", MapSize = 40)]
        static void AnyPlantKind(ScenarioContext s) => InWarmWeather(s, () => AnyPlantKindBody(s));

        static void AnyPlantKindBody(ScenarioContext s) {
            var crop = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(-6, 0));
            var otherCrop = SpawnBlighted(s.Map, "Plant_Corn", s.Cell(-2, 0));
            var tree = SpawnBlighted(s.Map, "Plant_TreeOak", s.Cell(2, 0));
            var grass = SpawnPlant(s.Map, "Plant_Grass", s.Cell(6, 0));
            grass.CropBlighted();
            s.Tick(130);
            s.AreEqual("CutPlant", DesignationsOn(s.Map, crop), "the blighted crop (Plant_Potato, a sown crop) is designated");
            s.AreEqual("CutPlant", DesignationsOn(s.Map, otherCrop), "the blighted second crop (Plant_Corn, a sown crop) is designated");
            s.AreEqual("CutPlant", DesignationsOn(s.Map, tree), "the blighted tree (Plant_TreeOak, a sown tree) is designated");
            s.AreEqual(1, s.Map.thingGrid.ThingsListAt(grass.Position).Count(t => t.def == ThingDefOf.Blight), "a blight thing sits on the grass cell");
            s.Check(!grass.Blighted, "grass is not blightable under vanilla's rule, so it does not count as blighted");
            s.AreEqual("", DesignationsOn(s.Map, grass), "the grass is not designated: Blight.Plant is null for it, so the watcher skips it (this line proves only the null-plant path; vanilla cannot blight a wild plant, so no wild plant is blighted here)");
            s.RequireNoLogErrors();
        }

        [Scenario("blight-incident-is-cut", MapSize = 40)]
        static void BlightIncidentIsCut(ScenarioContext s) => InWarmWeather(s, () => BlightIncidentBody(s));

        static void BlightIncidentBody(ScenarioContext s) {
            var zone = CellRect.FromLimits(s.Cell(-2, -2), s.Cell(2, 2));
            GrowingZone(s, zone);
            var potatoes = new List<Plant>();
            foreach (var c in zone) if (c.x != s.Center.x + 2) potatoes.Add(SpawnPlant(s.Map, "Plant_Potato", c));
            var corn = new List<Plant>();
            for (var dz = -2; dz <= 2; dz++) corn.Add(SpawnPlant(s.Map, "Plant_Corn", s.Cell(2, dz)));
            var pawn = s.SpawnColonist(s.Cell(0, 8));
            DisableGrowing(pawn);
            pawn.skills.GetSkill(SkillDefOf.Plants).Level = 20;

            s.AreEqual(20, potatoes.Count, "the growing zone holds 20 potatoes (4 columns of 5)");
            s.AreEqual(0, potatoes.Concat(corn).Count(p => p.Blighted), "no plant is blighted before the incident");
            s.Check(potatoes.Concat(corn).All(p => p.BlightableNow), "every plant in the zone is blightable now (sown, grown, a crop)");

            var incident = DefDatabase<IncidentDef>.GetNamed("CropBlight");
            var fired = incident.Worker.TryExecute(new IncidentParms { target = s.Map, points = 500f });
            s.Check(fired, "the game's crop blight incident should fire on the home map");

            var blighted = potatoes.Concat(corn).Where(p => p.Blighted).ToList();
            s.Check(blighted.Count == potatoes.Count || blighted.Count == corn.Count, "the incident should blight every plant of the one kind it picked (20 potatoes or 5 corn); the blighted plants are: " + DesignationReport(s, blighted));
            s.Check(blighted.Count > 0, "at least one plant is blighted by the incident");
            var healthy = potatoes.Concat(corn).Where(p => !p.Blighted).ToList();

            var designated = s.RunUntil(() => blighted.All(p => IsCutDesignated(s.Map, p)), 2500);
            s.Check(designated, "every blighted plant should carry exactly one CutPlant designation within 2,500 ticks; the state is: " + DesignationReport(s, blighted.Where(p => !IsCutDesignated(s.Map, p))));
            s.AreEqual(blighted.Count, blighted.Count(p => IsCutDesignated(s.Map, p)), "all blighted plants are designated");
            foreach (var p in healthy)
                s.AreEqual("", DesignationsOn(s.Map, p), "the healthy plant at " + Offset(s, p.Position) + " has no designation");
            s.RequireNoLogErrors();
        }

        [Scenario("vanilla-blight-behavior-intact", MapSize = 50)]
        static void VanillaBlightBehaviorIntact(ScenarioContext s) => InWarmWeather(s, () => VanillaBlightBehaviorIntactBody(s));

        static void VanillaBlightBehaviorIntactBody(ScenarioContext s) {
            var g1 = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(-10, -10));
            var g2 = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(-6, -10));
            var g3 = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(-10, -6));
            var gizmo = g1.GetGizmos().OfType<Command_Action>().FirstOrDefault(c => c.defaultLabel == "CutAllBlight".Translate().ToString());
            s.Check(gizmo != null, "a blighted plant without a cut order shows the Cut all blight button");
            if (gizmo != null) gizmo.action();
            s.AreEqual("CutPlant CutPlant CutPlant", string.Join(" ", new[] { g1, g2, g3 }.Select(p => DesignationsOn(s.Map, p))), "the Cut all blight button designates every blighted plant");
            s.Tick(300);
            s.AreEqual("CutPlant CutPlant CutPlant", string.Join(" ", new[] { g1, g2, g3 }.Select(p => DesignationsOn(s.Map, p))), "still one cut order each after the mod scans");
            foreach (var p in new[] { g1, g2, g3 }) p.Destroy();

            var a = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(0, 5));
            var b = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(1, 5));
            var c = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(2, 5));
            var d = SpawnPlant(s.Map, "Plant_Potato", s.Cell(3, 5));
            var e = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(-8, 5));
            s.Tick(130);
            s.AreEqual(4, new[] { a, b, c, e }.Count(p => IsCutDesignated(s.Map, p)), "the mod designates the four blighted plants");
            foreach (var p in new[] { b, c, e }) s.Map.designationManager.RemoveAllDesignationsOn(p);

            var pawn = s.SpawnColonist(s.Cell(0, 9));
            DisableGrowing(pawn);
            pawn.skills.GetSkill(SkillDefOf.Plants).Level = 20;
            var done = s.RunUntil(() => !a.Spawned && !b.Spawned && !c.Spawned, 2500);
            s.Check(done, "cutting the designated plant at (0,5) should extend the order to the touching blighted plants at (1,5) and (2,5): " + DesignationReport(s, new[] { a, b, c, d, e }));
            s.Check(d.Spawned && DesignationsOn(s.Map, d) == "", "the healthy plant at (3,5) next to the cut ones is neither cut nor designated");
            s.Check(e.Spawned && DesignationsOn(s.Map, e) == "", "the blighted plant at (-8,5) that is not touching and whose order was canceled stays undesignated");
            s.RequireNoLogErrors();
        }
    }
}
