using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using static RimhunterTestRunner.Scenarios.BlightHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class NonHomeMap
    {
        [Scenario("non-home-map-ignored", MapSize = 40)]
        static void NonHomeMapIgnored(ScenarioContext s) => InWarmWeather(s, () => NonHomeMapIgnoredBody(s));

        static void NonHomeMapIgnoredBody(ScenarioContext s) {
            PlanetTile tile;
            do tile = TileFinder.RandomStartingTile();
            while (Find.WorldObjects.AnyWorldObjectAt(tile));
            var camp = (MapParent) WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Camp);
            camp.Tile = tile;
            camp.SetFaction(Faction.OfPlayer);
            Find.WorldObjects.Add(camp);
            var other = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(40, 1, 40), null);
            Current.Game.CurrentMap = s.Map;
            s.Check(s.Map.IsPlayerHome, "the scenario map is a player home map");
            s.Check(!other.IsPlayerHome, "the camp map is not a player home map");

            var camper = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.PlayerStarter, forceGenerateNewPawn: true, canGeneratePawnRelations: false));
            GenSpawn.Spawn(camper, other.Center + new IntVec3(0, 0, 5), other);

            var cell = other.Center;
            foreach (var t in cell.GetThingList(other).ToList()) if (t.def.category == ThingCategory.Plant) t.Destroy();
            other.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            var campPlant = SpawnBlighted(other, "Plant_Potato", cell);
            var homePlant = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(2, 0));
            s.Check(campPlant.Blighted && homePlant.Blighted, "both potatoes are blighted");

            s.Tick(600);
            s.Check(Find.Maps.Contains(other), "the camp map still exists after the run");
            s.AreEqual("CutPlant", DesignationsOn(s.Map, homePlant), "the blight on the home map is designated");
            s.AreEqual("", DesignationsOn(other, campPlant), "the blight on the non-home camp map gets no designation");

            Current.Game.DeinitAndRemoveMap(other, false);
            Find.WorldObjects.Remove(camp);
            s.Tick(180);
            s.Check(!Find.Maps.Contains(other), "the camp map is removed");
            s.RequireNoLogErrors();
        }
    }
}
