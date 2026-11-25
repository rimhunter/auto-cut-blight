using System.Collections;
using System.Linq;
using RimWorld;
using Verse;

namespace RimhunterTestRunner.Scenarios
{
    static class IdentityModList
    {
        const string OurPackageId = "rimhunter.autocutblight";
        const string Razor = "razor2.3.AnotherRimworldMod.AutoCutBlight";
        const string Tr = "tr.AutoCutBlight";
        const string Defi = "Defi.AutoCutBlight";
        const string AllFixtures = "TestRunner/Fixtures/razor2-3-anotherrimworldmod-autocutblight TestRunner/Fixtures/tr-autocutblight TestRunner/Fixtures/defi-autocutblight";

        static ModMetaData Mod(string packageId) {
            var mod = ModLister.GetModWithIdentifier(packageId, ignorePostfix: true);
            if (mod == null) throw new ScenarioFailure("the stand-in mod " + packageId + " is not in the Mods folder");
            return mod;
        }

        [Scenario("screenshot-mod-list-warns-when-an-original-auto-cut-blight-is-also-enabled", MapSize = 40, Windowed = true, MaxSeconds = 150,
            StageMods = "TestRunner/Fixtures/razor2-3-anotherrimworldmod-autocutblight")]
        static IEnumerator ModListWarningOriginal(ScenarioContext s) {
            var page = new Page_ModsConfig();
            Find.WindowStack.Add(page);
            yield return null;
            var original = Mod(Razor);
            var ours = Mod(OurPackageId);
            s.Check(!original.Active, "the stand-in for the original should start inactive");
            try {
                ModsConfig.SetActive(original, true);
                page.SelectMod(ours);
                var shot = s.Screenshot("01-mod-list-incompatibility-warning");
                yield return shot;
                ModsConfig.GetModWarnings().TryGetValue(ours.PackageId, out var warning);
                if (warning == null) s.Fail("the game gave no mod warning for " + ours.PackageId + " with the original Auto-Cut Blight enabled");
                else s.Check(warning.IndexOf(original.Name, System.StringComparison.OrdinalIgnoreCase) >= 0, "the game's warning for this mod should name the stand-in " + original.Name + ", was: " + warning);
                var incompatible = "ModIncompatibleWith".Translate("").Resolve().Trim();
                s.Check(shot.ShowsText(incompatible), "the mod list should show \"" + incompatible + "\" for this mod; it showed: " + shot.TextsMatching("incompatible"));
                s.Check(shot.Texts.Count(t => t.Text == original.Name) >= 2, "the mod list should show the stand-in among the enabled mods and in the incompatibility row; it showed: " + shot.TextsMatching("stand-in"));
            }
            finally {
                ModsConfig.SetActive(original, false);
                page.Close(doCloseSound: false);
            }
            yield return null;
            s.Check(!page.IsOpen, "the mod list should close without saving once the stand-in is turned off again");
        }

        [Scenario("mod-list-warns-for-each-original-auto-cut-blight-build", MapSize = 30, MaxSeconds = 90, StageMods = AllFixtures)]
        static void ModListWarnsForEach(ScenarioContext s) {
            var ours = Mod(OurPackageId);
            foreach (var id in new[] { Razor, Tr, Defi }) {
                var other = Mod(id);
                try {
                    s.Check(!other.Active, id + " should start inactive");
                    ModsConfig.SetActive(other, true);
                    ModsConfig.GetModWarnings().TryGetValue(ours.PackageId, out var warning);
                    if (warning == null) s.Fail("the game gave no mod warning for " + ours.PackageId + " with " + id + " enabled");
                    else s.Check(warning.IndexOf(other.Name, System.StringComparison.OrdinalIgnoreCase) >= 0, "the game's warning for this mod should name " + id + " (" + other.Name + "), was: " + warning);
                }
                finally {
                    ModsConfig.SetActive(other, false);
                }
            }
        }

        [Scenario("mod-list-does-not-warn-when-nothing-else-is-enabled", MapSize = 30, MaxSeconds = 90)]
        static void NoWarningAlone(ScenarioContext s) {
            var ours = Mod(OurPackageId);
            ModsConfig.GetModWarnings().TryGetValue(ours.PackageId, out var warning);
            s.Check(string.IsNullOrEmpty(warning), "the mod list should not warn about this mod when no other Auto-Cut Blight is enabled, was: " + warning);
        }
    }
}
