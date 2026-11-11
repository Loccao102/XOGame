# Roadmap

Where Caro XO Pro (Tower of Anomalies) goes next, in priority order. Each phase lists what to do and how we know it is done.
Dates are intentionally absent: phases are gated by their exit criteria, not by the calendar.

## Done (this review)

Rule-engine fixes, each covered by a test in `Tools/EngineTests/RegressionTests.cs`:

- Undo, Phoenix Sigil and Revive now restore every side effect of a move: locked win lines, consumed special tiles, mine-destroyed pieces and freeze countdowns. Snapshots are not needed because each `MoveRecord` carries a `MoveUndoInfo`.
- A Warp Portal twin is undone together with the move that spawned it, so one Undo no longer hands the player an extra turn.
- A Gobblet board that is full is no longer a draw while either side can still gobble.
- GravityDrop: a frozen cell blocks its column instead of letting pieces float above it, and `HasAnyLegalMove` understands columns.
- `XORandom.SetSeed` plus seeding from the run seed make a run reproducible; the engine accepts an injected `RelicQuery` instead of reading global state.
- Puzzle stages 5 and 6 allowed too few moves (no forced win vs the Hard AI in 3 and 5); limits are now 4 and 6, verified by search.
- `server.js`: no path traversal, no crash on malformed URLs, no wildcard CORS.
- Repo hygiene: portable test runner, CI workflow, `Assets/_Recovery` untracked, silent `PlayerPrefs` write failures now logged once, warning when `googlePlayStoreUrl` is still the placeholder.

## Open items to decide

- **Lightning tiles on Classic boards:** the spawner places 1, the old test expected 2. The test now matches the code; change `SpawnBalancedSpecialTiles` if 2 was the intended balance.
- **`googlePlayStoreUrl`:** set the real store listing before any release build.
- **Unity-side verification:** the engine fixes are verified under .NET with stubs. Run the EditMode suite once in the Unity Editor (Mono) to confirm identical results.

## Phase 1 - Ship a stable playable

Goal: a build that passes ad-network review and does not regress silently.

- [x] Replace the placeholder store URL; add a release checklist (URL, SDK init, mute-on-blur, CTA path per network): defaulted `XOPlayableAdsBridge` to `standalonePlayableOnly = true` (blank store URL, guarded `mraid.open`/`window.open`), added DOM `visibilitychange`/`blur`/`focus`/`pagehide` mute-on-blur hooks + initial `ytgame.system.isAudioEnabled()` sync, and documented [PLAYABLE_RELEASE_CHECKLIST.md](PLAYABLE_RELEASE_CHECKLIST.md) with Unity menu validation.
- [ ] Fit the size budget (`<= 5 MB` WebGL): configured Brotli compression (`decompressionFallback: true`), IL2CPP `OptimizeSize`, Legacy Input fallback, removed URP `Assets/Settings` + `SampleScene` URP components, and added `XO Playable > 11. Trim Unused Packages (<5MB WebGL)` (`XOPackageTrimUtility`). **Pending final step:** run package trim + WebGL build inside Unity Editor (or CI once `UNITY_LICENSE` is set) and verify `Builds/WebGL_Playable/Build/` is `<= 5 MB`.
- [x] Stop committing `Builds/`: untracked `Builds/` via `git rm -r --cached Builds`, added `/[Bb]uilds/` to `.gitignore`, and added [playable-release.yml](.github/workflows/playable-release.yml) for GitHub Pages (on `main`) and GitHub Releases (on `v*` tags).
- [~] Unity EditMode tests in CI (GameCI) next to the existing .NET job: added GameCI `unity-editmode` + Mono vs CoreCLR result parity comparison in [engine-tests.yml](.github/workflows/engine-tests.yml) (runs automatically once `UNITY_LICENSE` or `UNITY_EMAIL`/`UNITY_PASSWORD` secret is added).
- [x] A puzzle gate in CI: updated `Puzzles_AllPatterns_LoadCleanly_AndAreSolvableVsHardAI` to verify every stage (`1..6`) on both `10x10` and `15x15` and wired it into [engine-tests.yml](.github/workflows/engine-tests.yml).

Exit: CoreCLR + Puzzle Gate active locally/CI; remaining sign-off is adding `UNITY_LICENSE` in GitHub Secrets, running package trim + Brotli build (`<= 5 MB`), and tagging the release.

## Phase 2 - Make the code easy to change

Goal: new modes and relics without touching a 6,000-line file.

- Split `XOGameController` (about 6,000 lines) into per-screen presenters (menu, match, tower map, shop, modals) with a small shared state object. Replace the per-frame modal polling in `Update()` with an `IsAnyModalOpen` flag set when a modal opens or closes.
- Decouple `Core` from `Visuals`: relic/rarity data should not reference `Sprite` or `XOProceduralAssets`. Move icons and colours to a presentation lookup, then turn `Core` into an asmdef with no `UnityEngine` dependency and delete the test stubs.
- Replace static globals (`XORoguelikeRunManager`, `XORandom`) with a run context passed into the engine.
- Return a `MoveResult` from `PlacePiece` (triggered tile, callouts, blasted cells, win lines) instead of several `out` parameters, so UI effects stop re-deriving what happened.
- Add tests alongside each refactor; keep the regression suite green throughout.

Progress (verified with .NET `VerifyEditor`, `EngineTests`, `Puzzles_AllPatterns` and `QARunner`; Unity Editor play/EditMode verification still recommended on next Editor launch):

- Done: `XOPlayable.Core` decoupled from `UnityEngine` (`XOPlayable.Core.asmdef` with `noEngineReferences: true`), organized into `Primitives/` (`XOCellPos`, `XOColor`, `XOMath`, `XORandomStream`, `XOStorage`), `Models/`, `Progression/`, and `Engine/`. Zero static Unity dependencies remain in `Core`.
- Done: Per-run roguelike state moved into instanceable `XORoguelikeRunState` (accepting injected `IXOKeyValueStorage` + seeded `XORandomStream`), consumed by `XOCaroEngine` via optional `RunContext` and `RelicQuery`.
- Done: `XOCaroEngine.TryPlace` returns full `PlaceResult` value object (`LandingCell`, `TriggeredSpecial`, `TacticalCallout`, `CalloutBonusCoins`, `VanishedCell`, `TwinCell`, `BlastedPieces`, `WinningLine`, `CompletedNewWinLine`, `StrictBlockedFive`, `GrantedExtraTurn`, `ResultState`).
- Done: `XOPlayable.Visuals` (`XOPlayable.Visuals.asmdef`) split from monolithic `XOProceduralAssets` into focused sprite/audio primitives (`XOTexturePrimitives`, `XOPieceSprites`, `XOHudIcons`, `XOActionIcons`, `XORewardIcons`, `XOProceduralAudio`, `XORelicVisuals`, `XOUnityExtensions`) plus `XOPlayerPrefsStorage` runtime bootstrap.
- Done: `XOGameController` refactored into a thin scene coordinator (473 lines) delegating to `XOUIBuilder`, `XOGameStateStore`, `XOAudioManager`, `XOBattleEffectsPresenter`, `XOResponsiveLayoutService`, `XOLobbyPresenter`, `XOBattlePresenter`, `XOTacticalSkillsController`, `XOMatchOutcomeService`, `XOMatchSessionController`, and 7 dedicated Modal classes under `Assets/Scripts/UI/`.
- Done: `XOModalState` + `XOModalTracker` pause turn timer while any modal is open without per-frame polling; assembly definitions (`*.asmdef`) added across `Core`, `Visuals`, `Ads`, `UI`, `Editor`, and `Tests`, with reflection-based `InputSystemUIInputModule` lookup so `XOPackageTrimUtility` does not break UI compilation.

Exit: `XOGameController` 473 lines (`< 500`), `Core` compiles with no `UnityEngine` reference, engine accepts injected run context & deterministic RNG seed.

UI refactor (branch `refactor/ui-redesign`; not compiled or run in the Unity Editor, so open the project and check the Console first):

- Fixed: the modal open animation never ran, because `XOModalTracker` looked for a child called `ModalCard` while the builder names it `Card`. The builder now hands the card to the tracker directly (`SetCard`).
- Done: `XOUIColors` holds the 46 accent colours that used to be 102 inline `Hex("#...")` literals, named by Tailwind family and shade (`Amber400`, `Slate700`). Pastel Clay base colours stay in `XOProceduralAssets`.
- Done: `SetAnchor` extension replaces 62 repeated `anchorMin` / `anchorMax` / `pivot` blocks. Stretch anchors (min differs from max) are left as they were.
- Done: `XOUIBuilder.CreateModalHeader` builds the icon, title and close button for the Mode, Tower, Caravan, Shop and Leaderboard modals. The icon now sits next to the title through a layout group instead of a hard-coded offset, and all close buttons share one size and position (the Shop and Leaderboard ones moved 2 px).
- Not done: safe-area handling. In WebGL `Screen.safeArea` is the whole window, so it would only help native builds; for a browser playable the insets have to come from CSS `env(safe-area-inset-*)` through the jslib bridge.
- Not done: moving legacy `Text` to TextMeshPro. TMP essentials and a font asset would eat into the 5 MB budget.

## Phase 3 - Depth and replayability

Goal: reasons to come back after the first clear.

- **Daily seed:** one shared seed per day with a local best score. This is now cheap because runs are reproducible from the shown seed.
- **Solver-verified content:** generate puzzle stages with the existing solver, keep only the ones with a unique or short solution, and add a small editor for hand-made stages.
- **Stronger Hard AI:** iterative-deepening alpha-beta with threat-space search, time-sliced so WebGL stays at 60 fps; keep the current heuristic AI as the easier tiers and give each bot a distinct style.
- **More Tower content:** new relics, boss blinds, and floors; balance with automated bot-vs-bot batches that report win rate per relic and per variant.
- Telemetry for balance (relic pick and win rates, floor death rates), opt-in and aggregate only.

Exit: daily seed live, at least 20 verified puzzle stages, Hard AI beats the heuristic AI in a bot-vs-bot batch.

## Phase 4 - Reach and retention

Goal: a game people can share and keep playing across devices.

- Cloud save: sync progression through a small backend or IndexedDB-backed storage on WebGL, replacing bare `PlayerPrefs`.
- Async PvP and a seed leaderboard.
- Localisation (Vietnamese and English first) and an accessibility pass: colour-blind-safe piece shapes, scalable text, full keyboard and touch parity.
- Monetisation experiments (rewarded ads placement, shop pricing) behind remote config and A/B tests.

Exit: progress survives a device change, two languages shipped, leaderboard live.
