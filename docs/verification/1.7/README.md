# Verification, RimWorld 1.7.5014 rev1195

Every scenario of this mod was run in the real game on RimWorld 1.7 (`Version.txt`: 1.7.5014 rev1195) on 2026-11-24 and 2026-11-25 through the shared scenario runner (a git submodule of the development repository, not part of the shipped mod). All 26 headless scenarios and the windowed screenshot scenario pass, and every one of them also fails with `--no-mod-under-test`. The windowed launch needed a display variable passed to the game with `--game-env` in a shell without a display.

## Commands

- Headless, one launch per row of `scripts/run-all --list-launches`: `scripts/run-all --launches 0 --timeout 170`, `scripts/run-all --launches 1-8` and `scripts/run-all --launches 9-11` (launches 2 to 8 need `RIMHUNTER_FACTIONALWAR_SRC=<Factional War checkout>`). Launch 11 is `scripts/test-translations`, pass.
- Windowed: `scripts/run-scenarios --windowed --game-env <display variable> --stage-mod TestRunner/Fixtures/razor2-3-anotherrimworldmod-autocutblight --save-screenshots docs/verification/1.7 -s screenshot-mod-list-warns-when-an-original-auto-cut-blight-is-also-enabled`. The first attempt on 1.7 failed because `--stage-mod` was missing (a command error, not a result).
- Controls: launch 0 as one launch with `scripts/run-scenarios --no-mod-under-test`; each other scenario by name with `--no-mod-under-test` and the same `--mod` / `--stage-mod` flags as its real launch (`--mod external/factional-war`, `--mod ludeon.rimworld.<dlc>`).
- Flaky scenario `vanilla-blight-behavior-intact` (3 failures in 17 runs before the colonist's plants skill was set to 20): 0 of 6 failed on 1.7 (1 pass inside launch 0, then 5 separate runs with `scripts/run-scenarios --no-build -s vanilla-blight-behavior-intact`).
- Emergency flag off: with `<emergency>true</emergency>` changed to `false` in `Defs/WorkGiverDef/WorkGivers.xml` (restored afterward), `blight-preempts-sowing` FAILS on 1.7 (the first work job after the blight order is another `Sow`, not `CutPlantDesignated`); `blight-before-ordinary-cutting` and `work-tab-respected` still pass, as they do not depend on the emergency list.

## Results

"Ticks" is the scenario's tick count in the passing run. The control column is the first failure message of the run without the mod. A pass here means the scenario's own checks passed; a "weak" control is explained under Known limits.

| Scenario | Launch | Result | Ticks | Control (`--no-mod-under-test`) |
|---|---|---|---|---|
| `any-plant-kind` | 0 | pass | 130 | FAIL: the blighted crop (Plant_Potato, a sown crop) is designated: expected <CutPlant> but was <> |
| `blight-before-ordinary-cutting` | 0 | pass | 1510 | FAIL: the blighted potato at (12,-3) should be designated within 200 ticks; it has: |
| `blight-before-ordinary-cutting-when-not-top-priority` | 0 | pass | 1383 | FAIL: the blighted potato at (12,-3) should be designated within 200 ticks; it has: |
| `blight-incident-is-cut` | 0 | pass | 59 | FAIL: every blighted plant should carry exactly one CutPlant designation within 2,500 ticks; the state is: Plant_Corn@(2,-2)= Plant_Corn@(2,-1)= Plant_Corn@(2,0)= Plant_Corn@(2,1)= Plant_Corn@(2,2)= |
| `blight-is-cut-without-orders` | 0 | pass | 380 | FAIL: the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: |
| `blight-is-cut-without-orders-with-all-five-dlc` | 4 | pass | 444 | FAIL: the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: |
| `blight-is-cut-without-orders-with-anomaly` | 3 | pass | 640 | FAIL: the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: |
| `blight-is-cut-without-orders-with-biotech` | 5 | pass | 563 | FAIL: the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: |
| `blight-is-cut-without-orders-with-factional-war` | 2 | pass | 339 | FAIL: rimhunter.autocutblight should be running (weak: fails on the mod-running check and also on the missing designation and job) |
| `blight-is-cut-without-orders-with-ideology` | 6 | pass | 468 | FAIL: the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: |
| `blight-is-cut-without-orders-with-odyssey` | 7 | pass | 376 | FAIL: the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: |
| `blight-is-cut-without-orders-with-royalty` | 8 | pass | 345 | FAIL: the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: |
| `blight-preempts-sowing` | 0 | pass | 619 | FAIL: the blighted potato at (12,-8) should be designated within 200 ticks; it has: |
| `blight-present-at-load` | 9 | pass | 51 | FAIL: the blight that was on the map at load should carry a cut order within 2,500 ticks: (-3,0)= (0,2)= (5,-1)= |
| `blight-spreads-and-is-cut` | 0 | pass | 417 | FAIL: the first blighted plant should be designated |
| `canceled-order-stays-canceled` | 10 | pass | 300 | FAIL: 4 error(s) reached the game log: Could not find class Rimhunter.AutoCutBlight.BlightWatcher while resolving node li. Trying to use Verse.MapComponent  (weak: fails on 4 game-log errors, `Could not find class Rimhunter.AutoCutBlight.BlightWatcher`, from loading a save made with the mod; the behavior check itself is not what fails) |
| `existing-designation-kept` | 0 | pass | 300 | FAIL: the blighted plant with no order gets one cut order: expected <CutPlant> but was <> |
| `five-plants-all-designated` | 0 | pass | 120 | FAIL: all 25 blighted plants should carry one CutPlant designation within 2,500 ticks |
| `healthy-plants-untouched` | 0 | pass | 1058 | FAIL: the blighted plant at (-1,-1) has a cut order: expected <CutPlant> but was <> |
| `loads-with-no-errors` | 0 | pass | 0 | FAIL: the mod rimhunter.autocutblight should be running (weak: fails because the mod is not running (the scenario checks that first)) |
| `mod-list-does-not-warn-when-nothing-else-is-enabled` | 0 | pass | 0 | FAIL: scenario threw: RimhunterTestRunner.ScenarioFailure: the stand-in mod rimhunter.autocutblight is not in the Mods folder (weak: fails because the mod is not in the Mods folder (the scenario looks it up)) |
| `mod-list-warns-for-each-original-auto-cut-blight-build` | 1 | pass | 0 | FAIL: scenario threw: RimhunterTestRunner.ScenarioFailure: the stand-in mod rimhunter.autocutblight is not in the Mods folder (weak: fails because the mod is not in the Mods folder (the scenario looks it up)) |
| `non-home-map-ignored` | 0 | pass | 870 | FAIL: the blight on the home map is designated: expected <CutPlant> but was <> |
| `reblighted-plant-is-ordered-again` | 0 | pass | 292 | FAIL: the blighted potato at (2,0) should get a cut order; it has: |
| `two-hundred-plants` | 0 | pass | 60 | FAIL: all 200 blighted plants should carry exactly one CutPlant designation within 2,500 ticks; undesignated: 200 |
| `vanilla-blight-behavior-intact` | 0 | pass | 858 | FAIL: the mod designates the four blighted plants: expected <4> but was <0> |
| `work-tab-respected` | 0 | pass | 1714 | FAIL: the blighted potato at (12,-8) should be designated within 200 ticks; it has: |
| `screenshot-mod-list-warns-when-an-original-auto-cut-blight-is-also-enabled` | windowed | pass | 0 | FAIL: scenario threw: the stand-in mod rimhunter.autocutblight is not in the Mods folder (weak: fails because the mod is not in the Mods folder) |

## Screenshot

| File | Scenario | What it shows |
|---|---|---|
| `01-mod-list-incompatibility-warning.png` | screenshot-mod-list-warns-when-an-original-auto-cut-blight-is-also-enabled | The mod list with a stand-in for an original Auto-Cut Blight enabled: this mod's row is marked with a red warning, and its detail panel shows the red box "Incompatible with: Razor ACB stand-in" and the mod's description. Game versions read 1.7 and 1.6. The three colonist portraits at the top are generated pawns. |

The scenario asserts the text "Incompatible with" and the stand-in's name from what the game drew before the image is kept. The game window is captured, not the desktop; it shows no personal name, Steam name or home path.

## Known limits

- **Runner limits**: (1) the runner has no list of expected vanilla log errors, so any vanilla `Log.Error` fails a scenario; none was seen in any run recorded here, so this limit stopped no check. (2) The runner does not pass a display variable to windowed 1.7 launches, so the windowed 1.7 run needed a display variable set for the game process on this setup. 1.6 windowed launches use X11 through Xwayland and need nothing. (3) The click helper is 1.7 only; no scenario of this mod uses it, so it stopped no check.
- **Weak controls.** Every scenario fails without the mod, but not every one fails on the behavior: `loads-with-no-errors`, `mod-list-does-not-warn-when-nothing-else-is-enabled`, `mod-list-warns-for-each-original-auto-cut-blight-build` and the windowed mod-list scenario look the mod up first and throw or fail because it is absent; `blight-is-cut-without-orders-with-factional-war` fails on that and also on the missing designation; `canceled-order-stays-canceled` fails on game-log errors from loading a save made with the mod (the saved map components cannot be resolved), not on the cancel check. A control that shows the cancel behavior would need vanilla to hold a canceled order, which a save made with the mod cannot give. A weak control could hide a scenario that passes without the mod doing the work; none did, because every one of them failed without the mod, but for these scenarios the failure is the mod being absent, not the behavior missing, and the stronger evidence is separate. Per kind: `loads-with-no-errors` and the mod-list scenarios fail without the mod because the mod is absent; `canceled-order-stays-canceled` fails on four game-log errors from loading a save made with the mod ("Could not find class Rimhunter.AutoCutBlight.BlightWatcher") before the cancel check runs; the Factional War scenario fails first on the mod-running check. Stronger evidence that bounds this: mutation checks were run during development on 1.7.5014 and are not recorded as run logs here. Dropping the `IsPlayerHome` line turned `non-home-map-ignored` red; dropping the `Scribe_Collections.Look` of the handled ids turned `canceled-order-stays-canceled` red at the `ReloadedInSameLaunch` stage ("the canceled order on (2,0) stays canceled: expected <> but was <CutPlant>"); with the `incompatibleWith` block removed from About.xml both warning scenarios failed (the failing message was not recorded). No mutation check was run on 1.6. Nothing here passed without the mod, so nothing was removed or strengthened.
- The controls of the DLC and Factional War scenarios load the same expansions and Factional War without the mod; the controls of launches 1, 9 and 10 stage the same fixtures or make the same two-launch save as the real runs.
- Not run: the runner's own proofs (`selftest-*`, `runner-api-vanilla-haul`), the unit tests (`dotnet test`, CI runs them), `scripts/test-runner-lifetime`, `scripts/test-map-determinism`, `scripts/test-crash-report`. The game was started only through the runner with `RIMWORLD_GAME_LOCK` set.
- Each scenario that is not the flaky one ran once per version with the mod; one failure would be a finding, and none occurred. One run is not a failure rate.
- What could not be shown: the windowed screenshots show only the mod-list incompatibility warning; no scenario of this repo is windowed for the blight order itself, so there is no picture of a blighted plant carrying its cut order, only the log lines of the runs. The game was never run with a real second Auto-Cut Blight build (the stand-in fixtures are used; the original has no license and was not opened). Colonist skill and work settings are set by the scenarios (plants skill 20, cold-weather handling), as the earlier tickets recorded.

