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

- Replace the placeholder store URL; add a release checklist (URL, SDK init, mute-on-blur, CTA path per network).
- Fit the size budget: the committed WebGL build is about 14 MB; most playable networks cap near 5 MB. Strip unused code, compress textures and audio, move to Brotli.
- Stop committing `Builds/`: publish builds via GitHub Releases or Pages, or use Git LFS.
- Unity EditMode tests in CI (GameCI) next to the existing .NET job, so Mono and CoreCLR results are compared on every push.
- A puzzle gate in CI: every generated stage must be solvable within its move limit, using the same solver as the test.

Exit: green CI on both runtimes, build under the network size cap, one tagged release.

## Phase 2 - Make the code easy to change

Goal: new modes and relics without touching a 6,000-line file.

- Split `XOGameController` (about 6,000 lines) into per-screen presenters (menu, match, tower map, shop, modals) with a small shared state object. Replace the per-frame modal polling in `Update()` with an `IsAnyModalOpen` flag set when a modal opens or closes.
- Decouple `Core` from `Visuals`: relic/rarity data should not reference `Sprite` or `XOProceduralAssets`. Move icons and colours to a presentation lookup, then turn `Core` into an asmdef with no `UnityEngine` dependency and delete the test stubs.
- Replace static globals (`XORoguelikeRunManager`, `XORandom`) with a run context passed into the engine.
- Return a `MoveResult` from `PlacePiece` (triggered tile, callouts, blasted cells, win lines) instead of several `out` parameters, so UI effects stop re-deriving what happened.
- Add tests alongside each refactor; keep the regression suite green throughout.

Progress (not yet compiled or run in the Unity Editor; verify first):

- Done: `XOGameController` is a `partial class` split by feature (`Lobby`, `Battle`, `Tower`, `Caravan`, `ModeSelect`, `Layout`, `ResultModals`, `ThemeShop`, `Leaderboard`, `LuckyWheel`, `MatchFlow`, `Skills`, `Refresh`, `Effects`, `UIHelpers`). The main file is about 650 lines. Code was moved verbatim, no logic changed. This is a stepping stone: real presenters with their own state are still to do.
- Done: modal polling replaced by `XOModalState` + `XOModalTracker` (every modal made by `CreateModalBackdrop` counts itself in and out). Tower and caravan modals now also pause the turn timer.
- Done: `XOCaroEngine.TryPlace` returns a `PlaceResult` (landing cell, triggered tile, callout, bonus coins). The `out` overloads of `PlacePiece` remain as wrappers for existing tests and tools. Blasted cells and win lines are not in the result yet.
- Open: `Core` still depends on `UnityEngine` (`Vector2Int`, `Mathf`, `Sprite`), and `XORoguelikeRunManager` / `XORandom` are still static.

Exit: `XOGameController` under about 1,500 lines, `Core` compiles with no Unity reference, no static mutable state in the engine.

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
