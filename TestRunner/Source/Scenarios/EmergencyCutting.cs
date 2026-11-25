using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.BlightHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class EmergencyCutting
    {
        static void SetWork(Pawn pawn, int plantCutting, int growing) {
            pawn.workSettings.SetPriority(WorkTypeDefOf.PlantCutting, plantCutting);
            pawn.workSettings.SetPriority(WorkTypeDefOf.Growing, growing);
        }

        static Pawn Grower(ScenarioContext s, IntVec3 cell) {
            var pawn = s.SpawnColonist(cell);
            pawn.skills.GetSkill(SkillDefOf.Plants).Level = 20;
            return pawn;
        }

        static void Retire(Pawn pawn) {
            foreach (var w in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                if (!pawn.WorkTypeIsDisabled(w)) pawn.workSettings.SetPriority(w, 0);
        }

        static string Priorities(Pawn pawn) =>
            "cut " + pawn.workSettings.GetPriority(WorkTypeDefOf.PlantCutting) + " grow " + pawn.workSettings.GetPriority(WorkTypeDefOf.Growing);

        static int SownCells(ScenarioContext s, CellRect rect) =>
            rect.Cells.Count(c => s.Map.thingGrid.ThingsListAt(c).Any(t => t is Plant));

        static string JobList(ScenarioContext s, Pawn pawn) => string.Join(" | ", s.JobsOf(pawn));

        static bool IsWork(RecordedJob j) => !j.Def.StartsWith("Wait") && j.Def != "GotoWander";

        static List<RecordedJob> WorkAfter(ScenarioContext s, Pawn pawn, int tick) =>
            s.JobsOf(pawn).Where(j => j.Tick > tick && IsWork(j)).ToList();

        static List<RecordedJob> CutJobs(ScenarioContext s, Pawn pawn) =>
            s.JobsOf(pawn).Where(j => j.Def == "CutPlantDesignated").ToList();

        static bool Sowing(Pawn pawn) => pawn.CurJob != null && pawn.CurJob.def.defName == "Sow";

        static void WithWorkPriorities(ScenarioContext s, Action body) {
            var before = Find.PlaySettings.useWorkPriorities;
            Find.PlaySettings.useWorkPriorities = true;
            try {
                var potato = ThingDef.Named("Plant_Potato");
                WithWarmOutdoors(s, () => PlantUtility.GrowthSeasonNow(s.Center, s.Map, potato),
                    "the map should be warm enough to sow potatoes within 1,200 ticks of the constant 20 C outdoor temperature", body);
            } finally {
                Find.PlaySettings.useWorkPriorities = before;
            }
        }

        [Scenario("blight-preempts-sowing", MapSize = 50)]
        static void BlightPreemptsSowing(ScenarioContext s) => WithWorkPriorities(s, () => {
            var zoneRect = CellRect.FromLimits(s.Cell(-3, 8), s.Cell(3, 9));
            GrowingZone(s, zoneRect);
            var sick = SpawnPlant(s.Map, "Plant_Potato", s.Cell(12, -8));
            var pawn = Grower(s, s.Cell(0, 5));
            SetWork(pawn, 1, 1);
            s.AreEqual("cut 1 grow 1", Priorities(pawn), "the colonist has plant cutting and growing both at priority 1");
            s.AreEqual(0, SownCells(s, zoneRect), "the 14 cell growing zone holds no plant before sowing");

            var sowing = s.RunUntil(() => Sowing(pawn), 1500);
            s.Check(sowing, "the colonist should start sowing the zone; the jobs are: " + JobList(s, pawn));
            s.AreEqual("", DesignationsOn(s.Map, sick), "the healthy potato at (12,-8) has no designation");

            sick.CropBlighted();
            s.Check(sick.Blighted, "the potato at (12,-8) should be blighted");
            var designated = s.RunUntil(() => IsCutDesignated(s.Map, sick), 200);
            s.Check(designated, "the blighted potato at (12,-8) should be designated within 200 ticks; it has: " + DesignationsOn(s.Map, sick));
            var designatedAt = Find.TickManager.TicksGame;

            var cutStarted = s.RunUntil(() => WorkAfter(s, pawn, designatedAt).Count > 0, 3000);
            s.Check(cutStarted, "the colonist should start a work job within 3,000 ticks of the order (vanilla decides at the next job boundary, the end of the sow job in hand); the jobs are: " + JobList(s, pawn));
            var next = WorkAfter(s, pawn, designatedAt).Take(1).ToList();
            s.AreEqual("CutPlantDesignated Thing_" + sick.ThingID, string.Join(" | ", next.Select(j => j.Def + " " + j.TargetA)),
                "at the next job boundary after the blight order, the first work job (waits and wandering do not count) is the cut of the blighted potato, ahead of any further sow job; the jobs are: " + JobList(s, pawn));

            s.RunUntil(() => !sick.Spawned, 3000);
            s.Check(!sick.Spawned, "the colonist should cut the blighted potato at (12,-8)");
            var resumed = s.RunUntil(() => s.JobsOf(pawn).Any(j => j.Tick > designatedAt && j.Def == "Sow"), 3000);
            s.Check(resumed, "the colonist should go back to sowing after the cut; the jobs are: " + JobList(s, pawn));
            s.RequireNoLogErrors();
        });

        [Scenario("blight-before-ordinary-cutting", MapSize = 50, MaxSeconds = 150)]
        static void BlightBeforeOrdinaryCutting(ScenarioContext s) => WithWorkPriorities(s, () => {
            var tree = SpawnPlant(s.Map, "Plant_TreeOak", s.Cell(-3, 0));
            s.Map.designationManager.AddDesignation(new Designation(tree, DesignationDefOf.CutPlant));
            var sick = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(12, -3));
            var designated = s.RunUntil(() => IsCutDesignated(s.Map, sick), 200);
            s.Check(designated, "the blighted potato at (12,-3) should be designated within 200 ticks; it has: " + DesignationsOn(s.Map, sick));
            s.AreEqual("CutPlant", DesignationsOn(s.Map, tree), "the healthy oak at (-3,0) carries the player's cut order");

            var pawn = Grower(s, s.Cell(0, 0));
            SetWork(pawn, 1, 0);
            s.AreEqual("cut 1 grow 0", Priorities(pawn), "the colonist has plant cutting at priority 1 and growing off");

            var bothCut = s.RunUntil(() => !sick.Spawned && !tree.Spawned, 9000);
            s.Check(bothCut, "both orders should be carried out within 9,000 ticks; the jobs are: " + JobList(s, pawn));
            var targets = string.Join(" ", CutJobs(s, pawn).Select(j => j.TargetA));
            s.AreEqual("Thing_" + sick.ThingID + " Thing_" + tree.ThingID, targets,
                "the blighted potato (12 cells away) is cut before the healthy oak (3 cells away); the jobs are: " + JobList(s, pawn));
            s.RequireNoLogErrors();
        });

        [Scenario("blight-before-ordinary-cutting-when-not-top-priority", MapSize = 50, MaxSeconds = 150)]
        static void BlightBeforeOrdinaryCuttingWhenNotTopPriority(ScenarioContext s) => WithWorkPriorities(s, () => {
            var tree = SpawnPlant(s.Map, "Plant_TreeOak", s.Cell(-3, 0));
            s.Map.designationManager.AddDesignation(new Designation(tree, DesignationDefOf.CutPlant));
            var sick = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(12, -3));
            var designated = s.RunUntil(() => IsCutDesignated(s.Map, sick), 200);
            s.Check(designated, "the blighted potato at (12,-3) should be designated within 200 ticks; it has: " + DesignationsOn(s.Map, sick));
            s.AreEqual("CutPlant", DesignationsOn(s.Map, tree), "the healthy oak at (-3,0) carries the player's cut order");

            var pawn = Grower(s, s.Cell(0, 0));
            SetWork(pawn, 2, 1);
            s.AreEqual("cut 2 grow 1", Priorities(pawn), "the colonist has plant cutting at 2 and growing at 1, so the blight giver is in the ordinary list, not the emergency one");
            var emergency = pawn.workSettings.WorkGiversInOrderEmergency.Select(w => w.def.defName);
            var ordinary = pawn.workSettings.WorkGiversInOrderNormal.Select(w => w.def.defName).ToList();
            s.Check(!emergency.Contains("AcbCutBlight"), "the blight giver is not in the colonist's emergency list; it is: " + string.Join(" ", emergency));
            s.Check(ordinary.Contains("AcbCutBlight") && ordinary.Contains("PlantsCut"), "the blight giver and the vanilla plant cut giver are both in the ordinary list; it is: " + string.Join(" ", ordinary));

            var bothCut = s.RunUntil(() => !sick.Spawned && !tree.Spawned, 9000);
            s.Check(bothCut, "both orders should be carried out within 9,000 ticks; the jobs are: " + JobList(s, pawn));
            var targets = string.Join(" ", CutJobs(s, pawn).Select(j => j.TargetA));
            s.AreEqual("Thing_" + sick.ThingID + " Thing_" + tree.ThingID, targets,
                "the blighted potato (12 cells away) is cut before the healthy oak (3 cells away) because the blight giver is listed above the vanilla one in plant cutting; the jobs are: " + JobList(s, pawn));
            s.RequireNoLogErrors();
        });

        [Scenario("work-tab-respected", MapSize = 50, MaxSeconds = 150)]
        static void WorkTabRespected(ScenarioContext s) => WithWorkPriorities(s, () => {
            var sick = SpawnBlighted(s.Map, "Plant_Potato", s.Cell(12, -8));
            var off = s.SpawnColonist(s.Cell(0, -6));
            SetWork(off, 0, 0);
            s.AreEqual("cut 0 grow 0", Priorities(off), "the first colonist has plant cutting turned off");
            var designated = s.RunUntil(() => IsCutDesignated(s.Map, sick), 200);
            s.Check(designated, "the blighted potato at (12,-8) should be designated within 200 ticks; it has: " + DesignationsOn(s.Map, sick));
            s.Tick(600);
            s.AreEqual(0, CutJobs(s, off).Count, "the colonist with plant cutting off takes no cut job in 600 ticks; the jobs are: " + JobList(s, off));
            s.Check(sick.Spawned && IsCutDesignated(s.Map, sick), "the blighted potato is still standing and still designated after 600 ticks");

            var lowerRect = CellRect.FromLimits(s.Cell(-3, 8), s.Cell(-1, 8));
            GrowingZone(s, lowerRect);
            var lower = Grower(s, s.Cell(0, 5));
            SetWork(lower, 2, 1);
            s.AreEqual("cut 2 grow 1", Priorities(lower), "the second colonist has plant cutting at 2 and growing at 1");
            var done = s.RunUntil(() => !sick.Spawned, 3000);
            s.Check(done, "the colonist with plant cutting below growing should cut the potato once the sowing is done; the jobs are: " + JobList(s, lower));
            s.AreEqual(3, SownCells(s, lowerRect), "the 3 cell zone is fully sown");
            var jobs = s.JobsOf(lower).ToList();
            var lastSow = jobs.FindLastIndex(j => j.Def == "Sow");
            var firstCut = jobs.FindIndex(j => j.Def == "CutPlantDesignated");
            s.Check(lastSow >= 0 && firstCut > lastSow, "no cut job starts before the last sow job (last sow job at index " + lastSow + ", first cut job at index " + firstCut + "); the jobs are: " + JobList(s, lower));
            s.AreEqual(0, CutJobs(s, off).Count, "the colonist with plant cutting off still took no cut job; the jobs are: " + JobList(s, off));

            Retire(lower);
            var higherRect = CellRect.FromLimits(s.Cell(3, 8), s.Cell(5, 9));
            GrowingZone(s, higherRect);
            var sick2 = SpawnPlant(s.Map, "Plant_Potato", s.Cell(-12, -8));
            var higher = Grower(s, s.Cell(4, 5));
            SetWork(higher, 1, 2);
            s.AreEqual("cut 1 grow 2", Priorities(higher), "the third colonist has plant cutting at 1 and growing at 2");
            var sowing = s.RunUntil(() => Sowing(higher), 1500);
            s.Check(sowing, "the third colonist should start sowing; the jobs are: " + JobList(s, higher));
            sick2.CropBlighted();
            var designated2 = s.RunUntil(() => IsCutDesignated(s.Map, sick2), 200);
            s.Check(designated2, "the second blighted potato at (-12,-8) should be designated within 200 ticks; it has: " + DesignationsOn(s.Map, sick2));
            var designatedAt = Find.TickManager.TicksGame;
            s.RunUntil(() => WorkAfter(s, higher, designatedAt).Count > 0, 3000);
            var next = WorkAfter(s, higher, designatedAt).Take(1).ToList();
            s.AreEqual("CutPlantDesignated Thing_" + sick2.ThingID, string.Join(" | ", next.Select(j => j.Def + " " + j.TargetA)),
                "at the next job boundary the colonist with plant cutting above growing cuts instead of sowing again; the jobs are: " + JobList(s, higher));
            s.AreEqual(0, CutJobs(s, off).Count, "the colonist with plant cutting off took no cut job in the whole scenario; the jobs are: " + JobList(s, off));
            s.RequireNoLogErrors();
        });
    }
}
