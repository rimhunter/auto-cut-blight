using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    static class BlightHelpers
    {
        public static Plant SpawnPlant(Map map, string defName, IntVec3 cell) {
            var plant = (Plant) ThingMaker.MakeThing(ThingDef.Named(defName));
            plant.Growth = 1f;
            plant.sown = true;
            GenSpawn.Spawn(plant, cell, map);
            return plant;
        }

        public static Plant SpawnBlighted(Map map, string defName, IntVec3 cell) {
            var plant = SpawnPlant(map, defName, cell);
            plant.CropBlighted();
            return plant;
        }

        public static Zone_Growing GrowingZone(ScenarioContext s, CellRect rect) {
            var zone = new Zone_Growing(s.Map.zoneManager);
            s.Map.zoneManager.RegisterZone(zone);
            foreach (var c in rect) zone.AddCell(c);
            return zone;
        }

        public static string DesignationsOn(Map map, Thing plant) =>
            string.Join("+", map.designationManager.AllDesignationsOn(plant).Select(d => d.def.defName).OrderBy(n => n));

        public static bool IsCutDesignated(Map map, Thing plant) =>
            plant.Spawned && DesignationsOn(map, plant) == "CutPlant";

        public static string Offset(ScenarioContext s, IntVec3 cell) => "(" + (cell.x - s.Center.x) + "," + (cell.z - s.Center.z) + ")";

        public static string DesignationReport(ScenarioContext s, IEnumerable<Plant> plants) =>
            string.Join(" ", plants.OrderBy(p => p.Position.z).ThenBy(p => p.Position.x)
                .Select(p => p.def.defName + "@" + Offset(s, p.Position) + "=" + (p.Spawned ? DesignationsOn(s.Map, p) : "gone")));

        public static void WithWarmOutdoors(ScenarioContext s, Func<bool> isWarm, string expectation, Action body) {
            var biome = s.Map.Biome;
            var before = biome.constantOutdoorTemperature;
            biome.constantOutdoorTemperature = 20f;
            try {
                var warm = s.RunUntil(isWarm, 1200);
                s.Check(warm, expectation + "; the cell temperature is " + s.Center.GetTemperature(s.Map));
                body();
            } finally {
                biome.constantOutdoorTemperature = before;
            }
        }

        public static void InWarmWeather(ScenarioContext s, Action body) =>
            WithWarmOutdoors(s, () => s.Center.GetTemperature(s.Map) > 15f,
                "the map should be warm within 1,200 ticks of the constant 20 C outdoor temperature, so no crop dies of cold when it is spawned", body);

        public static void DisableGrowing(Pawn pawn) {
            pawn.workSettings.SetPriority(WorkTypeDefOf.Growing, 0);
        }
    }
}
