using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    static class DlcAndFactionalWar
    {
        const string FactionalWarFolder = "external/factional-war";
        const string FactionalWarPackageId = "rimhunter.factionalwar";
        const string OurPackageId = "rimhunter.autocutblight";

        [Scenario("blight-is-cut-without-orders-with-royalty", MapSize = 40, Mods = "ludeon.rimworld.royalty")]
        static void Royalty(ScenarioContext s) => With(s, "Royalty");

        [Scenario("blight-is-cut-without-orders-with-ideology", MapSize = 40, Mods = "ludeon.rimworld.ideology")]
        static void Ideology(ScenarioContext s) => With(s, "Ideology");

        [Scenario("blight-is-cut-without-orders-with-biotech", MapSize = 40, Mods = "ludeon.rimworld.biotech")]
        static void Biotech(ScenarioContext s) => With(s, "Biotech");

        [Scenario("blight-is-cut-without-orders-with-anomaly", MapSize = 40, Mods = "ludeon.rimworld.anomaly")]
        static void Anomaly(ScenarioContext s) => With(s, "Anomaly");

        [Scenario("blight-is-cut-without-orders-with-odyssey", MapSize = 40, Mods = "ludeon.rimworld.odyssey")]
        static void Odyssey(ScenarioContext s) => With(s, "Odyssey");

        [Scenario("blight-is-cut-without-orders-with-all-five-dlc", MapSize = 40, MaxSeconds = 150,
            Mods = "ludeon.rimworld.royalty ludeon.rimworld.ideology ludeon.rimworld.biotech ludeon.rimworld.anomaly ludeon.rimworld.odyssey")]
        static void AllFive(ScenarioContext s) => With(s, "Royalty Ideology Biotech Anomaly Odyssey");

        static void With(ScenarioContext s, string expected) {
            var loaded = new[] {
                ("Royalty", ModsConfig.RoyaltyActive), ("Ideology", ModsConfig.IdeologyActive), ("Biotech", ModsConfig.BiotechActive),
                ("Anomaly", ModsConfig.AnomalyActive), ("Odyssey", ModsConfig.OdysseyActive)
            };
            s.AreEqual(expected, string.Join(" ", loaded.Where(o => o.Item2).Select(o => o.Item1)), "the expansions loaded in this launch");
            s.Metric("expansions_loaded", loaded.Count(o => o.Item2));
            AutomaticCutOrders.CutWithoutOrders(s);
            s.RequireNoPlayerLogErrors();
        }

        [Scenario("blight-is-cut-without-orders-with-factional-war", MapSize = 40, MaxSeconds = 120, Mods = FactionalWarFolder)]
        static void FactionalWar(ScenarioContext s) {
            var running = LoadedModManager.RunningModsListForReading.Select(m => m.PackageId).ToList();
            s.Check(running.Contains(OurPackageId), OurPackageId + " should be running; the running mods are: " + string.Join(", ", running));
            s.Check(running.Contains(FactionalWarPackageId), FactionalWarPackageId + " should be running; the running mods are: " + string.Join(", ", running));
            var theirs = LoadedModManager.RunningModsListForReading.FirstOrDefault(m => m.PackageId == FactionalWarPackageId);
            if (theirs == null) return;
            var built = Path.Combine(theirs.RootDir, ".built-from");
            s.Metric("factional_war_build_known", File.Exists(built) ? 1 : 0);
            s.Check(File.Exists(built), "the Factional War build should record the commit it was built from in " + built);
            s.RequireNoLogErrors();
            AutomaticCutOrders.CutWithoutOrders(s);
            s.RequireNoPlayerLogErrors();
        }
    }
}
