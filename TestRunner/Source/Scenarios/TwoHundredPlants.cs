using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using Verse;
using static RimhunterTestRunner.Scenarios.BlightHelpers;

namespace RimhunterTestRunner.Scenarios
{
    static class TwoHundredPlants
    {
        const string ModWatcherTypeName = "Rimhunter.AutoCutBlight.BlightWatcher";
        const int Scans = 2000;

        static List<Plant> Batch(ScenarioContext s, int firstZ) {
            var plants = new List<Plant>();
            for (var dz = 0; dz < 10; dz++)
                for (var dx = -10; dx < 10; dx++)
                    plants.Add(SpawnBlighted(s.Map, "Plant_Potato", s.Cell(dx, firstZ + dz)));
            return plants;
        }

        static void AlignToScan(ScenarioContext s) {
            while (Find.TickManager.TicksGame % 60 != 0) s.Tick(1);
        }

        static double Millis(Stopwatch w) => w.Elapsed.TotalMilliseconds;

        [Scenario("two-hundred-plants", MapSize = 100, MaxSeconds = 120)]
        static void TwoHundred(ScenarioContext s) => InWarmWeather(s, () => Measure(s));

        static void Measure(ScenarioContext s) {
            s.AreEqual(100, s.Map.Size.x, "the map is 100 cells wide");
            s.AreEqual(100, s.Map.Size.z, "the map is 100 cells deep");

            var first = Batch(s, -30);
            s.AreEqual(200, first.Count(p => p.Blighted), "200 plants are blighted");
            s.AreEqual(0, first.Count(p => s.Map.designationManager.HasMapDesignationOn(p)), "no plant has an order before the first tick");
            var lettersBefore = Find.LetterStack.LettersListForReading.Count;
            var startTick = Find.TickManager.TicksGame;
            var done = s.RunUntil(() => first.All(p => IsCutDesignated(s.Map, p)), 2500);
            var took = Find.TickManager.TicksGame - startTick;
            s.Metric("ticks_to_designate_200", took);
            s.Check(done, "all 200 blighted plants should carry exactly one CutPlant designation within 2,500 ticks; undesignated: " + first.Count(p => !IsCutDesignated(s.Map, p)));
            s.AreEqual(200, s.Map.designationManager.AllDesignations.Count(d => d.def == DesignationDefOf.CutPlant), "the map holds 200 cut orders in all");
            s.AreEqual(lettersBefore, Find.LetterStack.LettersListForReading.Count, "200 cut orders send the player no letter");

            var watcher = s.Map.components.FirstOrDefault(c => c.GetType().FullName == ModWatcherTypeName);
            if (watcher == null) { s.Fail("this scenario finds the mod's map component by the type name " + ModWatcherTypeName + " because it cannot reference the mod assembly; no such component is on the map, so the class was renamed or removed and this name must follow it"); return; }
            var tick = watcher.GetType().GetMethod("MapComponentTick");
            if (tick == null) { s.Fail(ModWatcherTypeName + " has no MapComponentTick method to time"); return; }
            var scan = (Action) Delegate.CreateDelegate(typeof(Action), watcher, tick);

            AlignToScan(s);
            var steady = Stopwatch.StartNew();
            for (var i = 0; i < Scans; i++) scan();
            steady.Stop();
            s.Metric("ms_per_scan_200_handled_blights", Millis(steady) / Scans);

            var second = Batch(s, 10);
            AlignToScan(s);
            var firstScan = Stopwatch.StartNew();
            scan();
            firstScan.Stop();
            s.Metric("ms_first_scan_200_new_blights", Millis(firstScan));
            s.AreEqual(200, second.Count(p => IsCutDesignated(s.Map, p)), "one scan designates the second 200 blighted plants");

            var steady400 = Stopwatch.StartNew();
            for (var i = 0; i < Scans; i++) scan();
            steady400.Stop();
            s.Metric("ms_per_scan_400_handled_blights", Millis(steady400) / Scans);
            s.AreEqual(400, s.Map.designationManager.AllDesignations.Count(d => d.def == DesignationDefOf.CutPlant), "the map holds 400 cut orders in all");
            s.RequireNoLogErrors();
        }
    }
}
