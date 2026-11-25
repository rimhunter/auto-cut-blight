# Verification of Auto-Cut Blight (rimhunter)

Record of the in-game verification: every scenario of this repository was run in the real game on RimWorld 1.7 and on RimWorld 1.6 through the shared scenario runner, and each was run again with `--no-mod-under-test`, which must fail it. This folder is not part of the shipped mod (`scripts/package` copies an allow-list and `scripts/test-package` checks that `docs` is absent).

- RimWorld 1.7: `docs/verification/1.7/README.md`, build `1.7.5014 rev1195`.
- RimWorld 1.6: `docs/verification/1.6/README.md`, build `1.6.4871 rev598` (the game reports rev600).
- Each folder holds one screenshot a release note can cite (the mod list warning when an original Auto-Cut Blight is also enabled). Log excerpts are in the tables below.

## Result

The repository now has 27 headless scenarios plus 1 windowed scenario; the first 26 headless ones and the windowed one were run on both versions as described here, and `blight-incident-is-cut` (see below) on 1.7 only. 26 headless scenarios plus 1 windowed scenario. All pass on 1.7 and on 1.6 (26 of 26 in the full launches, the windowed one on its own run). The 26 headless scenarios are the rows of `scripts/run-all --list-launches` that run the game: 16 in launch 0 and one each in launches 1 to 10 (launch 11 is `scripts/test-translations`). All 27 fail without the mod on both versions. The flaky scenario `vanilla-blight-behavior-intact` (3 failures in 17 runs before the plants-skill fix) passed 6 of 6 on 1.7 and 6 of 6 on 1.6 in this pass. Six clean runs do not prove the earlier rate gone (a rate of 3 in 17 would give six clean runs about 3 times in 10), they show it was not seen.

| Scenario | 1.7 | 1.7 control | 1.6 | 1.6 control |
|---|---|---|---|---|
| `any-plant-kind` | pass | FAIL | pass | FAIL |
| `blight-before-ordinary-cutting` | pass | FAIL | pass | FAIL |
| `blight-before-ordinary-cutting-when-not-top-priority` | pass | FAIL | pass | FAIL |
| `blight-is-cut-without-orders` | pass | FAIL | pass | FAIL |
| `blight-is-cut-without-orders-with-all-five-dlc` | pass | FAIL | pass | FAIL |
| `blight-is-cut-without-orders-with-anomaly` | pass | FAIL | pass | FAIL |
| `blight-is-cut-without-orders-with-biotech` | pass | FAIL | pass | FAIL |
| `blight-is-cut-without-orders-with-factional-war` | pass | FAIL (weak, see limits) | pass | FAIL (weak, see limits) |
| `blight-is-cut-without-orders-with-ideology` | pass | FAIL | pass | FAIL |
| `blight-is-cut-without-orders-with-odyssey` | pass | FAIL | pass | FAIL |
| `blight-is-cut-without-orders-with-royalty` | pass | FAIL | pass | FAIL |
| `blight-preempts-sowing` | pass | FAIL | pass | FAIL |
| `blight-present-at-load` | pass | FAIL | pass | FAIL |
| `blight-spreads-and-is-cut` | pass | FAIL | pass | FAIL |
| `canceled-order-stays-canceled` | pass | FAIL (weak, see limits) | pass | FAIL (weak, see limits) |
| `existing-designation-kept` | pass | FAIL | pass | FAIL |
| `five-plants-all-designated` | pass | FAIL | pass | FAIL |
| `healthy-plants-untouched` | pass | FAIL | pass | FAIL |
| `loads-with-no-errors` | pass | FAIL (weak, see limits) | pass | FAIL (weak, see limits) |
| `mod-list-does-not-warn-when-nothing-else-is-enabled` | pass | FAIL (weak, see limits) | pass | FAIL (weak, see limits) |
| `mod-list-warns-for-each-original-auto-cut-blight-build` | pass | FAIL (weak, see limits) | pass | FAIL (weak, see limits) |
| `non-home-map-ignored` | pass | FAIL | pass | FAIL |
| `reblighted-plant-is-ordered-again` | pass | FAIL | pass | FAIL |
| `two-hundred-plants` | pass | FAIL | pass | FAIL |
| `vanilla-blight-behavior-intact` | pass | FAIL | pass | FAIL |
| `work-tab-respected` | pass | FAIL | pass | FAIL |
| `screenshot-mod-list-warns-when-an-original-auto-cut-blight-is-also-enabled` (windowed) | pass | FAIL (weak) | pass | FAIL (weak) |

## The two open items of the research note

- **Vanilla leaves a blighted plant undesignated.** Shown on both versions: with the mod removed, `blight-is-cut-without-orders` ends with "the blighted potato at (2,0) should carry exactly one CutPlant designation within 2,500 ticks; it has: " (nothing), and the colonist's job list holds only wandering and waiting, no `CutPlantDesignated` (1.7 and 1.6). `five-plants-all-designated`, `healthy-plants-untouched` and `two-hundred-plants` show the same for 25, 4 and 200 blighted plants (0 of 200 designated).
- **The emergency scenario fails without the giver.** `blight-preempts-sowing` fails on both versions without the mod (no designation, so the colonist keeps sowing) and fails with only `<emergency>` set to `false` on both versions: the first work job after the blight order is `Sow`, not `CutPlantDesignated`. The other two ordering scenarios do not depend on the emergency list and keep passing in that run.

## Game versions

The scenarios were run on RimWorld 1.7.5014 rev1195 and on RimWorld 1.6.4871 rev598 (the game's own version string reads rev600), every 1.7 run first and then every 1.6 run. `blight-incident-is-cut`, added afterward, was run on 1.7.5014 only.

## Scenario added after the full runs

`blight-incident-is-cut` fires the game's own crop blight incident (`IncidentDef` `CropBlight`, `IncidentWorker.TryExecute` with 500 points) on a home map with a growing zone of 20 grown potatoes and 5 grown corn plants and a colonist with plants skill 20, in warm weather. The incident picks one kind at random and blights every plant of that kind in the zone (blight chance 1 within 8 cells), so the scenario expects 20 potatoes or 5 corn blighted, and every blighted plant to carry exactly one CutPlant designation within 2,500 ticks, the other plants none, and no log error. 1.7.5014: pass with the mod (59 ticks); `--no-mod-under-test` FAIL (the blighted corn stayed undesignated for 2,500 ticks). It was not run on 1.6: the symbols it uses (`IncidentWorker_CropBlight`, `Plant.BlightableNow`, `IncidentWorker.TryExecute`) are identical in the 1.6.4871 and 1.7.5005 decompiles, but no 1.6 run was made.

## Scan cost

Measured by `two-hundred-plants` on 1.7.5014 (recorded as metrics, not gated): one scan of the watcher costs 0.031 ms with 200 handled blights and 0.046 ms with 400; the first scan that creates 200 orders costs 0.92 ms. The 60-tick scan interval is kept.

## Plant kinds

`any-plant-kind` blights two crops (potato, corn) and a tree (oak) and checks each gets a cut order. Vanilla can never blight a wild plant (`PlantProperties.Blightable` needs a sowable, harvestable plant), so no scenario blights one. The grass line only proves that a blight thing on a plant that is not blightable (`Blight.Plant` is null) is skipped without an error.

## Known limits

- **Runner limits**: (1) the runner has no list of expected vanilla log errors, so any vanilla `Log.Error` fails a scenario; none was seen in any run recorded here, so this limit stopped no check. (2) The runner does not pass a display variable to windowed 1.7 launches, so the windowed 1.7 run needed a display variable set for the game process on this setup. 1.6 windowed launches use X11 through Xwayland and need nothing. (3) The click helper is 1.7 only; no scenario of this mod uses it, so it stopped no check.
- **Weak controls.** Every scenario fails without the mod, but not every one fails on the behavior: `loads-with-no-errors`, `mod-list-does-not-warn-when-nothing-else-is-enabled`, `mod-list-warns-for-each-original-auto-cut-blight-build` and the windowed mod-list scenario look the mod up first and throw or fail because it is absent; `blight-is-cut-without-orders-with-factional-war` fails on that and also on the missing designation; `canceled-order-stays-canceled` fails on game-log errors from loading a save made with the mod (the saved map components cannot be resolved), not on the cancel check. A control that shows the cancel behavior would need vanilla to hold a canceled order, which a save made with the mod cannot give. Nothing here passed without the mod, so nothing was removed or strengthened.
- The controls of the DLC and Factional War scenarios load the same expansions and Factional War without the mod; the controls of launches 1, 9 and 10 stage the same fixtures or make the same two-launch save as the real runs.
- Not run: the runner's own proofs (`selftest-*`, `runner-api-vanilla-haul`), the unit tests (`dotnet test`, CI runs them), `scripts/test-runner-lifetime`, `scripts/test-map-determinism`, `scripts/test-crash-report`. The game was started only through the runner with `RIMWORLD_GAME_LOCK` set.
- Each scenario that is not the flaky one ran once per version with the mod; one failure would be a finding, and none occurred. One run is not a failure rate.
- What could not be shown: the windowed screenshots show only the mod-list incompatibility warning; no scenario of this repo is windowed for the blight order itself, so there is no picture of a blighted plant carrying its cut order, only the log lines of the runs. The game was never run with a real second Auto-Cut Blight build (the stand-in fixtures are used; the original has no license and was not opened). Colonist skill and work settings are set by the scenarios (plants skill 20, cold-weather handling), as the earlier tickets recorded.
