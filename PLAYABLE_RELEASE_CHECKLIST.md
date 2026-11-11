# Playable Release Checklist

Use this checklist before tagging a release (`git tag vX.Y.Z && git push origin vX.Y.Z`) or uploading a WebGL build to a playable container.

## 1. Distribution & CTA Path per Network
- **Current project mode:** `standalonePlayableOnly = true` (no store app listing).
  - Verify `XOPlayableAdsBridge.standalonePlayableOnly == true` and `googlePlayStoreUrl` is blank (`""`) in [XOPlayableAdsBridge.cs](Assets/Scripts/Ads/XOPlayableAdsBridge.cs) and [SampleScene.unity](Assets/Scenes/SampleScene.unity).
  - Verify `XOGameController.showStoreInstallButton == false` unless intentionally shipping a Store Ad Demo.
  - **If shipping a Store Ad Demo later:** set `standalonePlayableOnly = false` and set `googlePlayStoreUrl` to the real Play Store / App Store URL (never `https://play.google.com/store/apps`).
- Verify CTA routing in [XOPlayableWebGLBridge.jslib](Assets/Plugins/WebGL/XOPlayableWebGLBridge.jslib):
  - **Google Ads Playable:** calls `window.ExitApi.exit()`
  - **MRAID 2.0 (AppLovin / Unity Ads / Vungle):** calls `window.mraid.open(url)` only when `url` is non-empty
  - **ironSource DAPI:** calls `window.dapi.openStoreUrl()`
  - **Meta Playable Ad:** calls `window.FbPlayableAd.onCTAClick()`
  - **YouTube Playables / Standalone Web:** no blank-tab popup when `url` is empty

## 2. SDK Initialization & Host Engagement
- `XOPlayableAdsBridge.Start()` passes `gameObject.name` dynamically into `XOPlayable_InitSDK(gameObject.name)` so `SendMessage` works both with scene `XOGameManager` and runtime auto-bootstrap.
- When `window.ytgame` is present:
  - `window.ytgame.game.firstFrameReady()` fires once initialized.
  - `window.ytgame.game.gameReady()` fires once initialized.
  - `window.ytgame.engagement.sendScore({ value })` fires via `ReportPlayableScore(int)`.

## 3. Mute-on-Blur & Host Pause
- `ProjectSettings.runInBackground` is `false` (`0` in `ProjectSettings.asset`).
- `XOPlayableAdsBridge` tracks independent host/browser states (`_hasWindowFocus`, `_isApplicationPaused`, `_isHostAudioEnabled`, `_isHostPaused`, `_isBrowserHidden`) via `ApplyHostLifecycleState()` so regaining focus never overrides an active host mute (`onAudioEnabledChange(false)` or `onPause`).
- [XOPlayableWebGLBridge.jslib](Assets/Plugins/WebGL/XOPlayableWebGLBridge.jslib) checks initial `ytgame.system.isAudioEnabled()` on startup and subscribes to DOM `visibilitychange`, `blur`, `focus`, and `pagehide` events (`OnBrowserVisibilityChanged`).

## 4. WebGL Size Budget (<= 5 MB)
- Compression format: **Brotli** (`WebGLCompressionFormat.Brotli`, `decompressionFallback: true`).
- Managed Stripping Level: **High** (`WebGL: 3`), **Strip Engine Code:** enabled, **WebGL Exceptions:** `None`, **Debug Symbols:** `Off`, **IL2CPP Code Generation:** `OptimizeSize`.
- Run Unity Menu **XO Playable > 11. Trim Unused Packages (<5MB WebGL)** (or `XOPackageTrimUtility.TrimUnusedPackages` headlessly) to strip URP, InputSystem, VisualScripting, Timeline, 2D tools, and unused built-in modules.
- Run **XO Playable > 10. Validate Release Checklist** and confirm `Builds/WebGL_Playable/Build/` is **<= 5.00 MB**.

## 5. CI Gates & GitHub Secrets Setup
- **CoreCLR + Puzzle Gate** (`.github/workflows/engine-tests.yml`): runs automatically on every push/PR (`dotnet run --project Tools/EngineTests -c Release` + `Puzzles_AllPatterns` gate across 10x10 and 15x15).
- **Unity EditMode (GameCI) & Release Workflows** (`.github/workflows/engine-tests.yml`, `.github/workflows/playable-release.yml`):
  1. Generate a Unity activation license (`.ulf`) locally or via `game-ci/unity-request-activation-file`.
  2. In GitHub **Settings → Secrets and variables → Actions**, add repository secret **`UNITY_LICENSE`** (or `UNITY_EMAIL` + `UNITY_PASSWORD` + `UNITY_SERIAL`).
  3. Enable **GitHub Pages** (`Source: GitHub Actions`) under **Settings → Pages** to auto-publish playable builds on pushes to `main`.
  4. Push a version tag (`git tag v1.0.0 && git push origin v1.0.0`) to create a GitHub Release with `XO_Playable_WebGL.zip` attached.

