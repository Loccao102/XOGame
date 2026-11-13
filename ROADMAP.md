# Roadmap

Where Gomoku Tower goes next, in priority order. Each phase lists what to do and how we know it is done.
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
- [~] Fit the size budget (`<= 5 MB` WebGL): trimmed `Packages/manifest.json` + `Packages/packages-lock.json` down to the 8 direct + `com.unity.ext.nunit` transitive dependencies actually used (removed URP, InputSystem, 2D tools, VisualScripting, Timeline, and unused built-in modules), deleted orphaned `DefaultVolumeProfile.asset` & `UniversalRenderPipelineGlobalSettings.asset`, cleared `GraphicsSettings` URP global settings map, set `webGLCompressionFormat: 2` (Brotli) and `activeInputHandler: 0` (Old Input) in `ProjectSettings.asset`, excluded local preview scripts from `XO_Playable_WebGL.zip` (`server.ps1` syntax fixed too), and guarded `[InitializeOnLoad]` in `XOSceneSetupEditor`. **Pending final step:** run a Unity WebGL build (`BuildWebGLPlayableCI` or Menu > 6) once Unity Editor / `UNITY_LICENSE` CI is run and confirm `Builds/WebGL_Playable/Build/` is `<= 5 MB`.
- [x] Stop committing `Builds/`: untracked `Builds/` via `git rm -r --cached Builds`, added `/[Bb]uilds/` to `.gitignore`, and added [playable-release.yml](.github/workflows/playable-release.yml) for GitHub Pages (on `main`) and GitHub Releases (on `v*` tags).
- [~] Unity EditMode tests in CI (GameCI) next to the existing .NET job: fixed `"noEngineReferences": false` in `XOPlayable.Tests.Editor.asmdef` so `UnityEngine.Debug` in `XOEngineTests` compiles in Unity EditMode; GameCI `unity-editmode` + Mono vs CoreCLR result parity runs automatically in [engine-tests.yml](.github/workflows/engine-tests.yml) once `UNITY_LICENSE` or `UNITY_EMAIL`/`UNITY_PASSWORD` secret is added.
- [x] A puzzle gate in CI: updated `Puzzles_AllPatterns_LoadCleanly_AndAreSolvableVsHardAI` to verify every stage (`1..6`) on both `10x10` and `15x15` and wired it into [engine-tests.yml](.github/workflows/engine-tests.yml).

Exit: CoreCLR + Puzzle Gate active locally/CI and packages/ProjectSettings pre-trimmed in repo; remaining sign-off is adding `UNITY_LICENSE` in GitHub Secrets, executing a WebGL Brotli build (`<= 5 MB`), and tagging the release.

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

UI & Post-Merge Stabilization (`refactor/ui-redesign` merged into `main`, verified cleanly via `.NET` `VerifyEditor`, `EngineTests`, `QARunner`; open Unity Editor / Play Mode when ready to visually inspect and verify runtime WebGL size):

- Fixed: the modal open animation never ran, because `XOModalTracker` looked for a child called `ModalCard` while the builder names it `Card`. The builder now hands the card to the tracker directly (`SetCard`).
- Done: `XOUIColors` holds the 46 accent colours that used to be 102 inline `Hex("#...")` literals, named by Tailwind family and shade (`Amber400`, `Slate700`). Pastel Clay base colours stay in `XOProceduralAssets`.
- Done: `SetAnchor` extension replaces 62 repeated `anchorMin` / `anchorMax` / `pivot` blocks. Stretch anchors (min differs from max) are left as they were.
- Done: `XOUIBuilder.CreateModalHeader` builds the icon, title and close button for the Mode, Tower, Caravan, Shop and Leaderboard modals (plus `resizeTextForBestFit` so long titles never clip).
- Done: safe-area handling across native (`Screen.safeArea`) and WebGL CSS `env(safe-area-inset-*)` via `XOPlayable_GetSafeAreaInsetNorm` in `XOPlayableWebGLBridge.jslib` + `viewport-fit=cover` HTML patching + `XOResponsiveLayoutService` integration.
- Fixed (post-merge bug sweep):
  - `XOBattlePresenter` top HUD scoreboard uses a `HorizontalLayoutGroup` (`TopHudRow`) so `PlayerXPill`, `PlayerOPill`, and `Rules` never overlap on Landscape (`540px` panels), dynamically sizes Action Deck cells (`_actionsDeckLayout`), and respects safe-area margins.
  - `XOCaroEngine.UndoMove` handles `GameMode.TowerClimb`, `XOTacticalSkillsController` refreshes action badges after Undo/Hint and decrements `PlayerMovesThisMatch` in Tower Climb, and 2P Local Bomb/Swap target the active opponent (`X` or `O`) cleanly across `XOTacticalSkillsController` & `XOCellView`.
  - `XOMatchSessionController.AITurnRoutine` forces `CellPiece.O` before `ThunderHammer`/`OmniCrown` double move so Boss `O` never places a Player `X` piece.
  - `XOGameController.Update()` pauses the turn timer while `XOPlayableAdsBridge.IsShowingAd` is true and registers `AdCardRT` in `_layout`.
- Not done: moving legacy `Text` to TextMeshPro. TMP essentials and a font asset would eat into the 5 MB budget.

## Phase 3 - Depth and replayability

Goal: reasons to come back after the first clear.

- **Daily seed:** one shared seed per day with a local best score. This is now cheap because runs are reproducible from the shown seed.
- **Solver-verified content:** generate puzzle stages with the existing solver, keep only the ones with a unique or short solution, and add a small editor for hand-made stages.
- **Stronger Hard AI:** iterative-deepening alpha-beta with threat-space search, time-sliced so WebGL stays at 60 fps; keep the current heuristic AI as the easier tiers and give each bot a distinct style.
- **More Tower content:** new relics, boss blinds, and floors; balance with automated bot-vs-bot batches that report win rate per relic and per variant.
- Telemetry for balance (relic pick and win rates, floor death rates), opt-in and aggregate only.

Exit: daily seed live, at least 20 verified puzzle stages, Hard AI beats the heuristic AI in a bot-vs-bot batch.

## Relics v2 (build depth, Balatro-style)

Goal: make relics decisions, not +1 bonuses. Today there are 15 relics, mostly one flat effect each.

1. **Archetypes and synergies:** group relics into Attack, Defence, Economy and Terrain (5-6 each), add about 10 relics that reward a theme ("per attack relic owned, ..."), set bonuses at 3 of a kind, and relics that grow per floor or are lost on a defeat.
2. **Risk for power:** strong relics with a cost (boss starts with an extra piece, fewer relic slots), plus rental/cursed relics that vanish after 3 floors.
3. **Boss preview in the shop:** show the next floor's boss, blind, omen and hands so the player can buy a counter. *Done.*
4. **Counter relics:** answers to specific blinds and omens (Fog of War, Storm Colosseum, Elite, Short hands).
5. **Consumable shop:** a second shelf for one-use skill packs and skill upgrades (for example Bomb 3x3, Hint shows the winning move).
6. **Meta progression:** unlock relics through goals, pick a starter relic per run, tie it to the daily seed.
7. **Shop depth:** merge two of a kind, swap a relic for another of the same rarity, omen-driven prices.

Order: 3 (done), then 1 and 2, then 4, then the rest. Measure each batch with `Tools/BalanceSim` relic mode, and smooth the floor 6+ difficulty cliff first or the new relics cannot be evaluated.

## Phase 4 - Reach and retention

Goal: a game people can share and keep playing across devices.

- Cloud save: sync progression through a small backend or IndexedDB-backed storage on WebGL, replacing bare `PlayerPrefs`.
- Async PvP and a seed leaderboard.
- Localisation (Vietnamese and English first) and an accessibility pass: colour-blind-safe piece shapes, scalable text, full keyboard and touch parity.
- Monetisation experiments (rewarded ads placement, shop pricing) behind remote config and A/B tests.

Exit: progress survives a device change, two languages shipped, leaderboard live.


### UI overhaul (branch `ui/overhaul`)
- Lobby: loose header chips, self-playing 7x7 hero board (`XOLobbyBoardDemo`), wordmark, large Play CTA with rules/rival subtitle, mission strip, four tiles, tower status line.
- Battle: back button, player cards, rules chip, turn banner with the timer bar inside, status banner, six square skill buttons (3x2 in landscape).
- Both screens are laid out in a nominal frame (1080x1920 / 1920x1080) with top-left coordinates and scaled to fit.
- Not compiled yet: open Unity and check the Console first.

## Tower v2 (boss AI, omens, hands)

- Done: `AIDifficulty.Boss` uses `XOBossAI` (pattern scoring, VCF search, sleeper-VCF check, 2-ply replies). Boss Elo now drives strength; the search is capped by a deterministic per-move node budget (worst move 243 ms -> 54 ms in `Tools/BalanceSim`).
- Done: every floor has a hand limit (`XORoguelikeCatalog.DefaultMoveLimit`, Sand Hourglass +6) and a Tower draw counts as a loss.
- Done: boss omens (`XOBossOmens`): Elite, Relentless, Hasty, Short hands, Wealthy. Rolled per run and floor from the run seed, applied to a cloned blind, so `GetActiveBlind()` carries them and rewards scale automatically.
- Done: battle HUD shows hands left and the boss omen.
- Next: tighten hands on late floors (45 -> 30), per-relic win-rate batches in `BalanceSim`, a boss that is not beaten by a pure blocker as second mover (VCT search beyond depth 1).
- Done: `Tools/BalanceSim` relic mode (`dotnet run -c Release --project Tools/BalanceSim -- relics <games> <proxyElo> <floors> <slip> [baseline|all|Relic,Relic]`): paired per-relic win rates against the real floor configs and boss kits.
- Finding: the Boss tier is a cliff. A noise-free Elo 2400 proxy wins about 33-70% on floors 1-5 but 0-10% on floors 6-15, so relics can only be measured on floors 1-5 until the curve is smoothed.
- Finding: Frost Staff freezes the player's best threat cell BEFORE the boss moves, so the boss cannot take that cell either. Floor 3: proxy wins 33% as shipped, 7% if the freeze happens after the boss move. Because of this, Thaw Charm currently lowers the player's win rate (33% -> 14%, 150 games). Decide the intended freeze timing before tuning either.


- Relic shop between floors: clearing a Tower floor now leads to the relic shop (buy or skip, Balatro style). "Next floor" or the close button starts the next fight; losing a floor just offers a rematch.

### Relics v2 prerequisite: difficulty curve (branch `feature/relics-v2`)

- Done: Boss AI strength is now a ladder of knobs by Elo (`XOBossAI.TuneForElo`, level 0-8): search depth and node budget, human-like slips (30% at Elo 1200 down to 0% at 2400), look-ahead defence from level 3, threat-space forced-win detection from level 5. Immediate wins and blocks are played at every level (tested).
- Done: boss Elo curve lowered for floors 6-15 (1300 ... 2450) and gravity floors get 80 and 70 hands (they were 45 and 28, which made them unwinnable: floor 6 0% -> 25%, floor 12 0% -> 42% for the Elo 2400 proxy).
- Finding: the remaining 0% floors (7 Gobblet, 8/10/13/15 two-line wins) are a limit of the proxy, not proof of difficulty: neither side's AI plans a second five, so the stalled board ends on hands. Next step is a proxy and boss that understand `RequiredWinLines` and Gobblet gobbling; until then relics are measured on floors 1-6, 9, 11, 12, 14.

### Frost Staff / Thaw Charm (decided: freeze after the boss moves)

- Done: Frost Staff and Omni Crown now cast AFTER the boss move (UI and `BalanceSim`); Flame Blade and Thorn Armor still cast before it. The freeze lasts 2 ticks (locks the cell for one player turn and one boss turn).
- Finding: freeze length must be even. Odd lengths (1, 3) hand the thawed cell to the boss first: floor 3 player win rate 8% vs 40% with 2 ticks. A player-only freeze is far too strong (5-10%).
- Done: engine supports one-sided freezes (`FrozenFor`, `FreezeCellFor`, `IsFrozenFor`). Thaw Charm now turns the boss's ice on the boss: the cell is locked for the boss only. This strictly adds options for the player; the proxy cannot show it (deterministic proxy, results flip with any change of path), so judge it with real players.

