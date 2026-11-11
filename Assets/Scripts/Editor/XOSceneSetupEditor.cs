#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using XOPlayable.Ads;
using XOPlayable.UI;
using XOPlayable.Visuals;

namespace XOPlayable.EditorTools
{
    [InitializeOnLoad]
    public static class XOSceneSetupEditor
    {
        static XOSceneSetupEditor()
        {
            XOStorageUnityBootstrap.Configure();
            EditorApplication.delayCall += EnsureManagerInActiveScene;
        }

        private static void EnsureManagerInActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            XOGameController existing = Object.FindFirstObjectByType<XOGameController>();
            if (existing == null)
            {
                GameObject go = new GameObject("XOGameManager");
                go.AddComponent<XOGameController>();
                go.AddComponent<XOPlayableAdsBridge>();
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("[XO Playable] Automatically added XOGameManager to Scene! Press Play in Unity to start.");
            }
        }

        [MenuItem("XO Playable/1. Setup GameObject in Active Scene", false, 10)]
        public static void SetupSceneObject()
        {
            XOGameController existing = Object.FindFirstObjectByType<XOGameController>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("XO Playable", "XOGameManager is already present in the current Scene!", "OK");
                return;
            }

            GameObject go = new GameObject("XOGameManager");
            go.AddComponent<XOGameController>();
            go.AddComponent<XOPlayableAdsBridge>();

            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[XO Playable] Created XOGameManager in Scene! Press Play to test.");
        }

        [MenuItem("XO Playable/2. Grant +500 Coins & +5 Skills (Test)", false, 20)]
        public static void GrantTestResources()
        {
            PlayerPrefs.SetInt("XO_Coins", PlayerPrefs.GetInt("XO_Coins", 150) + 500);
            PlayerPrefs.SetInt("XO_UndoStock", PlayerPrefs.GetInt("XO_UndoStock", 2) + 5);
            PlayerPrefs.SetInt("XO_HintStock", PlayerPrefs.GetInt("XO_HintStock", 2) + 5);
            PlayerPrefs.SetInt("XO_BombStock", PlayerPrefs.GetInt("XO_BombStock", 1) + 5);
            PlayerPrefs.SetInt("XO_FreezeStock", PlayerPrefs.GetInt("XO_FreezeStock", 1) + 5);
            PlayerPrefs.SetInt("XO_SwapStock", PlayerPrefs.GetInt("XO_SwapStock", 1) + 5);
            PlayerPrefs.Save();

            if (Application.isPlaying && XOGameController.Instance != null)
            {
                XOGameController.Instance.Coins = PlayerPrefs.GetInt("XO_Coins", 150);
                XOGameController.Instance.UndoStock = PlayerPrefs.GetInt("XO_UndoStock", 2);
                XOGameController.Instance.HintStock = PlayerPrefs.GetInt("XO_HintStock", 2);
                XOGameController.Instance.BombStock = PlayerPrefs.GetInt("XO_BombStock", 1);
                XOGameController.Instance.FreezeStock = PlayerPrefs.GetInt("XO_FreezeStock", 1);
                XOGameController.Instance.SwapStock = PlayerPrefs.GetInt("XO_SwapStock", 1);
            }

            Debug.Log("[XO Playable] Granted +500 Coins, +5 Undos, +5 Hints, +5 Bombs, +5 Freezes, +5 Swaps!");
        }

        [MenuItem("XO Playable/3. Reset All Save Data", false, 30)]
        public static void ResetSaveData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[XO Playable] All PlayerPrefs save data has been reset to defaults!");
        }

        [MenuItem("XO Playable/4. Optimize PlayerSettings for Google Playable / WebGL", false, 40)]
        public static void OptimizePlayerSettingsForPlayable()
        {
            ApplyPlayableOptimizationSettings(showDialog: true);
        }

        private const long PlayableSizeBudgetBytes = 5L * 1024L * 1024L; // 5 MB network playable budget

        private static void ApplyPlayableOptimizationSettings(bool showDialog)
        {
            PlayerSettings.companyName = "MyGStudio";
            PlayerSettings.productName = "Caro XO Pro";
            PlayerSettings.runInBackground = false; // Mandatory for Google/YouTube Playables to pause audio in background
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;

            // Optimize WebGL / Mobile bundle size for <= 5 MB Playable upload
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.dataCaching = false;

            AssetDatabase.SaveAssets();
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Google Playable Optimization Complete!",
                    "Configured ultra-lightweight WebGL Playable settings:\n" +
                    "- Managed Stripping Level: High\n" +
                    "- Strip Engine Code: Enabled\n" +
                    "- IL2CPP Code Generation: Optimize Size\n" +
                    "- WebGL Exceptions & Debug Symbols: None / Off\n" +
                    "- Compression: Brotli + Decompression Fallback\n" +
                    "- Run In Background: False (Auto-mute on tab blur)",
                    "Awesome!");
            }
        }

        [MenuItem("XO Playable/5. Bake All Icons & UI Sprites to Assets/Resources/XOUI", false, 50)]
        public static void BakeSpritesToResourcesFolder()
        {
            string folder = "Assets/Resources/XOUI";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            SaveSpriteToPng(XOProceduralAssets.Button3DSprite, Path.Combine(folder, "Button3D.png"));
            SaveSpriteToPng(XOProceduralAssets.Pill3DSprite, Path.Combine(folder, "Pill3D.png"));
            SaveSpriteToPng(XOProceduralAssets.CardPanelSprite, Path.Combine(folder, "CardPanel.png"));
            SaveSpriteToPng(XOProceduralAssets.RecessedSlotSprite, Path.Combine(folder, "RecessedSlot.png"));
            SaveSpriteToPng(XOProceduralAssets.SoftCellSprite, Path.Combine(folder, "SoftCell.png"));
            SaveSpriteToPng(XOProceduralAssets.XPieceSprite, Path.Combine(folder, "Piece_CrossX.png"));
            SaveSpriteToPng(XOProceduralAssets.OPieceSprite, Path.Combine(folder, "Piece_RingO.png"));
            SaveSpriteToPng(XOProceduralAssets.IconCoin, Path.Combine(folder, "Icon_Coin.png"));
            SaveSpriteToPng(XOProceduralAssets.IconChest, Path.Combine(folder, "Icon_Chest.png"));
            SaveSpriteToPng(XOProceduralAssets.IconHint, Path.Combine(folder, "Icon_Hint.png"));
            SaveSpriteToPng(XOProceduralAssets.IconUndo, Path.Combine(folder, "Icon_Undo.png"));
            SaveSpriteToPng(XOProceduralAssets.IconBomb, Path.Combine(folder, "Icon_Bomb.png"));
            SaveSpriteToPng(XOProceduralAssets.IconFreeze, Path.Combine(folder, "Icon_Freeze.png"));
            SaveSpriteToPng(XOProceduralAssets.IconSkinShop, Path.Combine(folder, "Icon_SkinShop.png"));
            SaveSpriteToPng(XOProceduralAssets.IconVideoAd, Path.Combine(folder, "Icon_VideoAd.png"));
            SaveSpriteToPng(XOProceduralAssets.IconTrophy, Path.Combine(folder, "Icon_Trophy.png"));
            SaveSpriteToPng(XOProceduralAssets.IconCrown, Path.Combine(folder, "Icon_Crown.png"));
            SaveSpriteToPng(XOProceduralAssets.IconFlame, Path.Combine(folder, "Icon_Flame.png"));
            SaveSpriteToPng(XOProceduralAssets.IconSwords, Path.Combine(folder, "Icon_Swords.png"));
            SaveSpriteToPng(XOProceduralAssets.IconShield, Path.Combine(folder, "Icon_Shield.png"));
            SaveSpriteToPng(XOProceduralAssets.IconSoundOn, Path.Combine(folder, "Icon_SoundOn.png"));
            SaveSpriteToPng(XOProceduralAssets.IconSoundOff, Path.Combine(folder, "Icon_SoundOff.png"));
            SaveSpriteToPng(XOProceduralAssets.IconNewGame, Path.Combine(folder, "Icon_NewGame.png"));
            SaveSpriteToPng(XOProceduralAssets.IconSwap, Path.Combine(folder, "Icon_Swap.png"));
            SaveSpriteToPng(XOProceduralAssets.IconLightning, Path.Combine(folder, "Icon_Lightning.png"));
            SaveSpriteToPng(XOProceduralAssets.IconRock, Path.Combine(folder, "Icon_Rock.png"));
            SaveSpriteToPng(XOProceduralAssets.IconLeaderboard, Path.Combine(folder, "Icon_Leaderboard.png"));
            SaveSpriteToPng(XOProceduralAssets.IconLuckyWheel, Path.Combine(folder, "Icon_LuckyWheel.png"));

            AssetDatabase.Refresh();
            Debug.Log("[XO Playable] Baked 28 high-res icons & 3D UI sprites into Assets/Resources/XOUI/!");
        }

        [MenuItem("XO Playable/6. Build WebGL Playable Package (Builds/WebGL_Playable)", false, 60)]
        public static void BuildWebGLPlayablePackage()
        {
            BuildWebGLPlayableInternal(interactive: true);
        }

        // Headless CI entrypoint: Unity -batchmode -quit -executeMethod XOPlayable.EditorTools.XOSceneSetupEditor.BuildWebGLPlayableCI
        public static void BuildWebGLPlayableCI()
        {
            bool ok = BuildWebGLPlayableInternal(interactive: false);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        [MenuItem("XO Playable/10. Validate Release Checklist", false, 80)]
        public static void ValidateReleaseChecklistMenu()
        {
            bool ok = ValidatePlayableReleaseChecklist(out List<string> issues, out string summary);
            Debug.Log(summary);
            EditorUtility.DisplayDialog(
                ok ? "Playable Release Checklist Passed" : "Playable Release Checklist Issues",
                ok ? summary : string.Join("\n", issues),
                "OK");
        }

        public static bool ValidatePlayableReleaseChecklist(out List<string> issues, out string summary)
        {
            issues = new List<string>();
            if (PlayerSettings.runInBackground)
                issues.Add("PlayerSettings.runInBackground must be false for mute-on-blur.");
            if (!PlayerSettings.stripEngineCode)
                issues.Add("PlayerSettings.stripEngineCode should be enabled.");
            if (PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL) != ManagedStrippingLevel.High)
                issues.Add("WebGL ManagedStrippingLevel should be High.");
            if (PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL) != Il2CppCodeGeneration.OptimizeSize)
                issues.Add("WebGL IL2CPP code generation should be OptimizeSize.");
            if (PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Brotli)
                issues.Add("WebGL compressionFormat should be Brotli to fit <= 5 MB playable caps.");
            if (PlayerSettings.WebGL.exceptionSupport != WebGLExceptionSupport.None)
                issues.Add("WebGL exceptionSupport should be None.");

            XOPlayableAdsBridge bridge = Object.FindFirstObjectByType<XOPlayableAdsBridge>();
            if (bridge == null)
                issues.Add("XOPlayableAdsBridge not found in active scene.");
            else if (bridge.HasInvalidStoreUrlConfiguration())
                issues.Add("XOPlayableAdsBridge has invalid store URL configuration (generic placeholder or empty Store Ad URL).");

            string jslibPath = "Assets/Plugins/WebGL/XOPlayableWebGLBridge.jslib";
            if (!File.Exists(jslibPath))
            {
                issues.Add("Missing WebGL JS bridge: " + jslibPath);
            }
            else
            {
                string js = File.ReadAllText(jslibPath);
                if (!js.Contains("firstFrameReady") || !js.Contains("gameReady") || !js.Contains("visibilitychange") || !js.Contains("ExitApi.exit"))
                    issues.Add("XOPlayableWebGLBridge.jslib is missing required lifecycle/visibility/CTA hooks.");
            }

            long buildPayloadBytes = GetWebGLBuildPayloadBytes("Builds/WebGL_Playable");
            if (buildPayloadBytes > PlayableSizeBudgetBytes)
                issues.Add($"WebGL Build payload is {buildPayloadBytes / (1024f * 1024f):F2} MB (exceeds 5.00 MB budget).");

            var sb = new StringBuilder();
            sb.AppendLine("[XO Playable Release Checklist]");
            sb.AppendLine($"- Distribution: {(bridge != null && bridge.standalonePlayableOnly ? "Standalone Playable" : "Store CTA")}");
            sb.AppendLine($"- Compression: {PlayerSettings.WebGL.compressionFormat} | IL2CPP: {PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL)} | Strip: {PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL)}");
            if (buildPayloadBytes > 0)
                sb.AppendLine($"- Current WebGL Build/ size: {buildPayloadBytes / (1024f * 1024f):F2} MB (Budget: 5.00 MB)");
            sb.AppendLine(issues.Count == 0 ? "- Result: ALL CHECKS PASSED" : $"- Result: {issues.Count} ISSUE(S):\n  * " + string.Join("\n  * ", issues));
            summary = sb.ToString();
            return issues.Count == 0;
        }

        private static long GetWebGLBuildPayloadBytes(string buildDir)
        {
            string buildSubDir = Path.Combine(buildDir, "Build");
            if (!Directory.Exists(buildSubDir)) return 0;

            long total = 0;
            foreach (string file in Directory.GetFiles(buildSubDir, "*", SearchOption.AllDirectories))
                total += new FileInfo(file).Length;
            return total;
        }

        private static bool BuildWebGLPlayableInternal(bool interactive)
        {
            ApplyPlayableOptimizationSettings(showDialog: false);
            if (interactive)
                EditorSceneManager.SaveOpenScenes();

            string buildDir = "Builds/WebGL_Playable";
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string scenePath = EditorSceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = "Assets/Scenes/SampleScene.unity";
            }

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = buildDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            AssetDatabase.SaveAssets();
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == BuildResult.Succeeded)
            {
                PatchPlayableIndexHtml(Path.Combine(buildDir, "index.html"));
                CreateRunPreviewBat(buildDir);

                long payloadBytes = GetWebGLBuildPayloadBytes(buildDir);
                string zipPath = "Builds/XO_Playable_WebGL.zip";
                try
                {
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    ZipFile.CreateFromDirectory(buildDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
                    Debug.Log($"[XO Playable] WebGL Playable Build Succeeded! Build/={payloadBytes / (1024f * 1024f):F2} MB | Zip={new FileInfo(zipPath).Length / (1024f * 1024f):F2} MB | Output: {Path.GetFullPath(buildDir)}");
                    if (interactive)
                        EditorUtility.RevealInFinder(zipPath);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[XO Playable] Note: Could not update zip file ({ex.Message}), but WebGL files are completely ready in folder: {Path.GetFullPath(buildDir)}");
                }

                ValidatePlayableReleaseChecklist(out List<string> issues, out string summary);
                if (issues.Count > 0)
                    Debug.LogWarning(summary);
                else
                    Debug.Log(summary);

                return true;
            }

            Debug.LogError("[XO Playable] WebGL Build failed or was cancelled. Ensure the Unity WebGL Build Support module is installed in Unity Hub.");
            return false;
        }

        [MenuItem("XO Playable/7. Preview WebGL in Browser (Localhost)", false, 65)]
        public static void RunWebGLPlayableInBrowser()
        {
            string buildDir = Path.GetFullPath("Builds/WebGL_Playable");
            string indexPath = Path.Combine(buildDir, "index.html");
            if (!File.Exists(indexPath))
            {
                EditorUtility.DisplayDialog("Build Not Found", "Please build the WebGL package first via 'XO Playable -> 6. Build WebGL Playable Package'!", "OK");
                return;
            }

            string batPath = Path.Combine(buildDir, "Run_Preview.bat");
            if (!File.Exists(batPath))
            {
                CreateRunPreviewBat(buildDir);
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = batPath,
                WorkingDirectory = buildDir,
                UseShellExecute = true
            });
        }

        private static void CreateRunPreviewBat(string buildDir)
        {
            string jsPath = Path.Combine(buildDir, "server.js");
            string jsContent = @"const http = require('http');
const fs = require('fs');
const path = require('path');
const { exec } = require('child_process');

const PORT = 8088;
const DIR = __dirname;

const MIME_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.htm': 'text/html; charset=utf-8',
  '.js': 'application/javascript',
  '.json': 'application/json',
  '.css': 'text/css',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.ico': 'image/x-icon',
  '.wasm': 'application/wasm'
};

function detectUnityEncoding(filePath) {
  if (filePath.endsWith('.gz')) return 'gzip';
  if (filePath.endsWith('.br')) return 'br';
  if (!filePath.endsWith('.unityweb')) return null;

  const fd = fs.openSync(filePath, 'r');
  try {
    const head = Buffer.alloc(2);
    const bytesRead = fs.readSync(fd, head, 0, 2, 0);
    if (bytesRead === 2 && head[0] === 0x1f && head[1] === 0x8b) {
      return 'gzip';
    }
    return 'br';
  } finally {
    fs.closeSync(fd);
  }
}

const server = http.createServer((req, res) => {
  let urlPath;
  try {
    urlPath = decodeURIComponent(req.url.split('?')[0]);
  } catch (e) {
    res.statusCode = 400;
    res.setHeader('Content-Type', 'text/plain; charset=utf-8');
    res.end('400 Bad Request');
    return;
  }
  urlPath = urlPath.replace(/^\/+/, '');
  if (!urlPath || urlPath === 'index.htm') urlPath = 'index.html';

  // Never serve anything outside the build folder (blocks ../ and ..%2f traversal).
  const filePath = path.resolve(DIR, urlPath);
  if (filePath !== DIR && !filePath.startsWith(DIR + path.sep)) {
    res.statusCode = 403;
    res.setHeader('Content-Type', 'text/plain; charset=utf-8');
    res.end('403 Forbidden');
    return;
  }
  if (!fs.existsSync(filePath) || fs.statSync(filePath).isDirectory()) {
    res.statusCode = 404;
    res.setHeader('Content-Type', 'text/plain; charset=utf-8');
    res.end('404 Not Found: ' + urlPath);
    return;
  }

  const ext = path.extname(filePath).toLowerCase();
  let contentType = MIME_TYPES[ext] || 'application/octet-stream';

  if (filePath.includes('.wasm')) {
    contentType = 'application/wasm';
  } else if (filePath.includes('.data')) {
    contentType = 'application/octet-stream';
  } else if (filePath.includes('.framework.js')) {
    contentType = 'application/javascript';
  }

  const encoding = detectUnityEncoding(filePath);
  if (encoding) {
    res.setHeader('Content-Encoding', encoding);
  }

  res.setHeader('Content-Type', contentType);
  res.setHeader('Cache-Control', 'no-cache');

  fs.createReadStream(filePath).pipe(res);
});

server.listen(PORT, '127.0.0.1', () => {
  console.log('======================================================================');
  console.log('  CARO XO PRO - WEBGL PLAYABLE PREVIEW SERVER');
  console.log(`  Local URL: http://localhost:${PORT}/index.html`);
  console.log('  (Press Ctrl+C in this window to stop server)');
  console.log('======================================================================');

  const openCmd = process.platform === 'win32'
    ? `start http://localhost:${PORT}/index.html`
    : (process.platform === 'darwin' ? `open http://localhost:${PORT}/index.html` : `xdg-open http://localhost:${PORT}/index.html`);
  exec(openCmd);
});";
            File.WriteAllText(jsPath, jsContent);

            string psPath = Path.Combine(buildDir, "server.ps1");
            string psContent = @"$port = 8088
$dir = $PSScriptRoot
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add(""http://127.0.0.1:$port/"")

try {
    $listener.Start()
    Write-Host ""======================================================================""""
    Write-Host ""  CARO XO PRO - WEBGL PREVIEW SERVER (POWERSHELL)""
    Write-Host ""  URL: http://localhost:$port/index.html""
    Write-Host ""======================================================================""""
    Start-Process ""http://localhost:$port/index.html""

    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response
        $relPath = $request.Url.LocalPath.TrimStart('/')
        if ([string]::IsNullOrEmpty($relPath) -or $relPath -eq ""index.htm"") { $relPath = ""index.html"" }
        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $dir $relPath))

        if (-not $fullPath.StartsWith($dir, [System.StringComparison]::OrdinalIgnoreCase)) {
            $response.StatusCode = 403
        } elseif (Test-Path $fullPath -PathType Leaf) {
            $bytes = [System.IO.File]::ReadAllBytes($fullPath)
            if ($fullPath -like ""*.wasm*"") { $response.ContentType = ""application/wasm"" }
            elseif ($fullPath -like ""*.framework.js*"" -or $fullPath.EndsWith("".js"")) { $response.ContentType = ""application/javascript"" }
            elseif ($fullPath.EndsWith("".html"") -or $fullPath.EndsWith("".htm"")) { $response.ContentType = ""text/html; charset=utf-8"" }
            elseif ($fullPath.EndsWith("".css"")) { $response.ContentType = ""text/css"" }
            else { $response.ContentType = ""application/octet-stream"" }

            if ($fullPath.EndsWith("".br"")) { $response.AddHeader(""Content-Encoding"", ""br"") }
            elseif ($fullPath.EndsWith("".gz"")) { $response.AddHeader(""Content-Encoding"", ""gzip"") }
            elseif ($fullPath.EndsWith("".unityweb"")) {
                $enc = if ($bytes.Length -ge 2 -and $bytes[0] -eq 0x1F -and $bytes[1] -eq 0x8B) { ""gzip"" } else { ""br"" }
                $response.AddHeader(""Content-Encoding"", $enc)
            }

            $response.ContentLength64 = $bytes.Length
            $response.OutputStream.Write($bytes, 0, $bytes.Length)
        } else {
            $response.StatusCode = 404
        }
        $response.OutputStream.Close()
    }
} catch {
    Write-Host ""Server error: $($_.Exception.Message)""
}";
            File.WriteAllText(psPath, psContent);

            string batPath = Path.Combine(buildDir, "Run_Preview.bat");
            string batContent = @"@echo off
title Caro XO Pro - WebGL Playable Preview Server
cd /d ""%~dp0""
echo ======================================================================
echo   CARO XO PRO - WEBGL PLAYABLE PREVIEW SERVER
echo ======================================================================
echo   Local Web URL: http://localhost:8088/index.html
echo   (Note: Must access http://localhost:8088 - not plain localhost!)
echo ======================================================================

where node >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    node server.js
) else (
    echo Starting PowerShell Web Server at http://localhost:8088 ...
    powershell -ExecutionPolicy Bypass -NoProfile -File ""%~dp0server.ps1""
)
pause";
            File.WriteAllText(batPath, batContent);
        }

        private static void PatchPlayableIndexHtml(string indexHtmlPath)
        {
            if (!File.Exists(indexHtmlPath)) return;
            string html = File.ReadAllText(indexHtmlPath);

            // Inject full-screen responsive playable CSS & prevent mobile touch-scroll bounce
            string playableCss = @"
  <style>
    * { box-sizing: border-box; }
    html, body {
      margin: 0 !important;
      padding: 0 !important;
      width: 100% !important;
      height: 100% !important;
      overflow: hidden !important;
      background: #0B1120 !important;
      touch-action: none !important;
      user-select: none !important;
      -webkit-user-select: none !important;
    }
    #unity-container, #unity-container.unity-desktop, #unity-container.unity-mobile {
      position: fixed !important;
      top: 0 !important;
      left: 0 !important;
      width: 100% !important;
      height: 100% !important;
      transform: none !important;
      margin: 0 !important;
      display: flex !important;
      align-items: center !important;
      justify-content: center !important;
      background: #0B1120 !important;
    }
    #unity-canvas, #unity-canvas.unity-mobile {
      width: 100% !important;
      height: 100% !important;
      max-width: 100% !important;
      max-height: 100% !important;
      display: block !important;
      position: absolute !important;
      top: 0 !important;
      left: 0 !important;
      margin: auto !important;
      background: #0B1120 !important;
    }
    #unity-loading-bar {
      position: absolute !important;
      left: 50% !important;
      top: 50% !important;
      transform: translate(-50%, -50%) !important;
      z-index: 100 !important;
    }
    #unity-footer {
      display: none !important;
    }
  </style>";

            if (html.Contains("</head>"))
            {
                if (html.Contains("<style>") && html.Contains("touch-action: none"))
                {
                    int s = html.IndexOf("<style>");
                    int e = html.IndexOf("</style>") + 8;
                    html = html.Remove(s, e - s).Insert(s, playableCss.Trim());
                }
                else
                {
                    html = html.Replace("</head>", playableCss + "\n</head>");
                }
            }

            // Override hardcoded desktop resolution in Unity's JS template (960px / 600px)
            html = html.Replace("canvas.style.width = \"960px\";", "canvas.style.width = \"100%\";");
            html = html.Replace("canvas.style.height = \"600px\";", "canvas.style.height = \"100%\";");

            // Inject local file:// protocol helper warning if opened directly
            string fileWarningJs = @"
      if (window.location.protocol === 'file:') {
        var warnDiv = document.createElement('div');
        warnDiv.style.cssText = 'position:fixed;inset:0;background:rgba(11,17,32,0.96);z-index:99999;display:flex;align-items:center;justify-content:center;padding:24px;font-family:system-ui,-apple-system,sans-serif;color:#F8FAFC;text-align:center;';
        warnDiv.innerHTML = '<div style=""max-width:560px;background:#1E293B;border:2px solid #F59E0B;border-radius:18px;padding:28px 24px;box-shadow:0 20px 40px rgba(0,0,0,0.6);"">'
          + '<div style=""font-size:44px;margin-bottom:12px;"">⚠️</div>'
          + '<h2 style=""margin:0 0 12px 0;font-size:22px;color:#F59E0B;font-weight:bold;"">Browser Blocks WebGL via file://</h2>'
          + '<p style=""margin:0 0 18px 0;font-size:15px;line-height:1.6;color:#CBD5E1;"">Chrome and Edge block WebAssembly (.wasm) from local file paths due to CORS security policies.</p>'
          + '<div style=""background:#0F172A;border-radius:12px;padding:16px;text-align:left;font-size:14px;line-height:1.7;color:#94A3B8;"">'
          + '👉 <b>How to run preview:</b> Double-click <b><code style=""color:#38BDF8;"">Run_Preview.bat</code></b> in this folder (runs local web server at <code>http://localhost:8088</code>).<br>'
          + '👉 Or open browser at: <b><code>http://localhost:8088/index.html</code></b>.'
          + '</div></div>';
        document.body.appendChild(warnDiv);
      }
";
            if (!html.Contains("window.location.protocol === 'file:'") && html.Contains("var canvas = document.querySelector(\"#unity-canvas\");"))
            {
                html = html.Replace("var canvas = document.querySelector(\"#unity-canvas\");", "var canvas = document.querySelector(\"#unity-canvas\");\n" + fileWarningJs);
            }

            File.WriteAllText(indexHtmlPath, html);
        }

        private static void SaveSpriteToPng(Sprite sprite, string path)
        {
            if (sprite == null || sprite.texture == null) return;
            byte[] png = sprite.texture.EncodeToPNG();
            if (png != null)
            {
                File.WriteAllBytes(path, png);
            }
        }
    }

    [CustomEditor(typeof(XOGameController))]
    public class XOGameControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "CARO X-O PRO (STANDALONE GOOGLE / YOUTUBE PLAYABLE READY - 100% ENGLISH)\n" +
                "- 7 Game Modes: 10x10 Fast, 15x15 Pro, 3x3 Infinite Vanishing, 4x4 Gobblet Stack, 8x8 Gravity Drop, Strict Caro & Chaos Minefield.\n" +
                "- 28 Multi-color Vector Icons, Procedural Ambient BGM Loop, Haptic Vibration, Lucky Spin Wheel, 6 Named AI Rivals & 7 Claimable Achievements.\n" +
                "- Full Retention & Ad Monetization Loop: Undo, AI Hint, Bomb, Freeze, Swap, Second Chance Revive, Rank Shield, Win Chests & 2-Tab Skin Shop.\n" +
                "- Built-in WebGL JS Bridge & 1-Click WebGL Playable Packager.",
                MessageType.Info);

            DrawDefaultInspector();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Quick Test & Playable Build Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+500 Coins & +5 All Skills", GUILayout.Height(32)))
            {
                XOSceneSetupEditor.GrantTestResources();
            }
            if (GUILayout.Button("Reset Save Data", GUILayout.Height(32)))
            {
                XOSceneSetupEditor.ResetSaveData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bake 28 Icons to Assets/Resources/XOUI", GUILayout.Height(32)))
            {
                XOSceneSetupEditor.BakeSpritesToResourcesFolder();
            }
            if (GUILayout.Button("Optimize Playable Settings", GUILayout.Height(32)))
            {
                XOSceneSetupEditor.OptimizePlayerSettingsForPlayable();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Build WebGL Playable Package (Builds/XO_Playable_WebGL.zip)", GUILayout.Height(36)))
            {
                XOSceneSetupEditor.BuildWebGLPlayablePackage();
            }

            if (GUILayout.Button("Validate Playable Release Checklist", GUILayout.Height(28)))
            {
                XOSceneSetupEditor.ValidateReleaseChecklistMenu();
            }
        }
    }
}
#endif
