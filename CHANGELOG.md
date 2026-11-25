# Changelog

Auto-Cut Blight (rimhunter) is written from scratch by rimhunter. Nothing of any build of the original Auto-Cut Blight was read, copied or used.

Add each new change under "Unreleased" with its date as `- YYYY-MM-DD: what changed`. At release, rename the heading to the version and release date.

## 1.0.0 (2026-11-25)

What the mod does:

- A plant that becomes blighted on a player home map gets one plant-cut order, with no click. Blight already on a home map when the mod is added (a loaded save) gets its orders too.
- A colonist whose plant cutting priority is at or above their best ordinary work priority cuts blighted plants first, ahead of any further sow job or ordinary cutting. The game's own emergency work mechanism does it (a new work giver, like firefighting).
- A canceled order stays canceled, also after a save and a load. A plant that already has an order keeps it. A plant that is blighted again gets a new order.
- It does not touch vanilla blight or the Cut all blight button, has no settings, shows no letter, and has English text only. The mod adds no interruption of its own: it is an emergency work giver, and the game decides when a colonist picks a new job. In the tests the colonist cut after the sow job in hand ended. No dependencies: RimWorld 1.6 and 1.7 from one subscription. `incompatibleWith` lists the three builds of the original Auto-Cut Blight.

What was verified (`docs/verification/`): every in-game scenario of the repository passed on RimWorld 1.7.5014 and on 1.6.4871, and each failed when run without the mod. Some of those controls fail on the mod being absent rather than on the behavior; the verification record lists them. One run per scenario and version, except the one scenario that was flaky before a fix. No picture shows a blighted plant with its order.

Changes, with their dates:

- 2026-11-24: the mod skeleton for RimWorld 1.6 and 1.7: identity, load folders, build, scripts and the load scenario. It does nothing yet.
- 2026-11-24: blighted plants on a player home map get one plant-cut order automatically, including blight already on the map at load; a canceled order stays canceled, also after a save and a load.
- 2026-11-24: colonists whose plant cutting priority is at or above their best ordinary work priority cut blighted plants first, ahead of any further sow job or ordinary cutting, through the game's own emergency work mechanism (a new work giver, like firefighting). English only.
- 2026-11-25: new `About/Preview.png`, drawn by rimhunter with `scripts/make-art`; no image of any build of the original was used. `scripts/test-identity` allows exactly this png (and the pngs under `docs/verification/`); `scripts/test-package` requires it in the package and still rejects any other png.
- 2026-11-25: new `WORKSHOP.md` (the Steam BBCode page text, not packaged) and the release script `scripts/publish-github`, which publishes a merged release commit to the public GitHub repository with the noreply author and checks every file against the source commit; it touches GitHub only with `--publish`.
- 2026-11-25: new scenario `blight-incident-is-cut` fires the game's own crop blight incident and checks every blighted plant gets one cut order; the `any-plant-kind` scenario now says that vanilla cannot blight a wild plant; the verification record lists the scan costs. Test-only, no mod change.
