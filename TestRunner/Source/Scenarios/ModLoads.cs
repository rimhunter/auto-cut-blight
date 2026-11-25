using System.Linq;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    static class ModLoads
    {
        [Scenario("loads-with-no-errors", MapSize = 40)]
        static void LoadsWithNoErrors(ScenarioContext s) {
            s.Check(Current.ProgramState == ProgramState.Playing, "the game should reach a map");
            s.Check(Find.CurrentMap == s.Map, "the scenario map should be the current map");

            var running = LoadedModManager.RunningModsListForReading.Select(m => m.PackageId).ToList();
            s.Check(running.Contains("rimhunter.autocutblight"), "the mod rimhunter.autocutblight should be running; the running mods are: " + string.Join(", ", running));

            s.RequireNoLogErrors();
            s.RequireNoPlayerLogErrors();
        }
    }
}
