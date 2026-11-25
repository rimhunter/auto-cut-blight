# Contributing

The mod builds from this repository with the .NET SDK: `dotnet build Source/ModAutoCutBlight.csproj -p:GameVersion=1.7` produces the 1.7 load folder, and `-p:GameVersion=1.6` the 1.6 one. `Build.proj` builds both and also needs the shared test runner. `dotnet test --project Tests/AutoCutBlight.Tests` runs the unit tests. The in-game scenarios in `TestRunner/` need the shared test runner, which is not part of this repository; the maintainers run them on every change. The scenario that runs this mod together with Factional War (rimhunter) loads a build of Factional War's `origin/main` from `external/factional-war` (not committed): set `RIMHUNTER_FACTIONALWAR_SRC` to a Factional War checkout and `scripts/run-all` runs `scripts/prepare-factional-war` first; by hand, run that script, then `scripts/run-scenarios -s blight-is-cut-without-orders-with-factional-war --mod external/factional-war`. A windowed scenario needs a display: pass the display variable of your session to the game with `--game-env` when the shell has none.

`scripts/test-translations` checks that English text exists for every def text and every key the code uses. It sees only a key written as a string literal with `.Translate(...)`, `.TryTranslate(...)` or `.TranslateWithBackup(...)` directly on it, such as `"AcbKey".Translate()`. A key held in a constant or built at run time is not seen, so write every key as a literal.

Each change is listed with its date in `CHANGELOG.md`. A pull request adds its own line there.

By contributing you agree that your change is licensed under the same terms as the rest of the mod: the Apache License 2.0 (see `LICENSE`).
