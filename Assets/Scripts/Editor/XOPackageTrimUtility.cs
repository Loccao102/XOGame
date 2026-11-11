#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace XOPlayable.EditorTools
{
    /// <summary>
    /// Removes unused UPM packages and built-in modules via Unity Package Manager (Client.AddAndRemove)
    /// to keep the WebGL playable under the 5 MB network budget.
    /// </summary>
    public static class XOPackageTrimUtility
    {
        private static readonly string[] CandidatePackagesToRemove =
        {
            "com.unity.render-pipelines.universal",
            "com.unity.inputsystem",
            "com.unity.visualscripting",
            "com.unity.timeline",
            "com.unity.multiplayer.center",
            "com.unity.collab-proxy",
            "com.unity.ide.rider",
            "com.unity.2d.animation",
            "com.unity.2d.aseprite",
            "com.unity.2d.psdimporter",
            "com.unity.2d.sprite",
            "com.unity.2d.spriteshape",
            "com.unity.2d.tilemap",
            "com.unity.2d.tilemap.extras",
            "com.unity.2d.tooling",
            "com.unity.modules.accessibility",
            "com.unity.modules.adaptiveperformance",
            "com.unity.modules.ai",
            "com.unity.modules.androidjni",
            "com.unity.modules.animation",
            "com.unity.modules.assetbundle",
            "com.unity.modules.cloth",
            "com.unity.modules.director",
            "com.unity.modules.particlesystem",
            "com.unity.modules.physics",
            "com.unity.modules.physics2d",
            "com.unity.modules.screencapture",
            "com.unity.modules.terrain",
            "com.unity.modules.terrainphysics",
            "com.unity.modules.tilemap",
            "com.unity.modules.uielements",
            "com.unity.modules.umbra",
            "com.unity.modules.unityanalytics",
            "com.unity.modules.unitywebrequest",
            "com.unity.modules.unitywebrequestassetbundle",
            "com.unity.modules.unitywebrequestaudio",
            "com.unity.modules.unitywebrequesttexture",
            "com.unity.modules.unitywebrequestwww",
            "com.unity.modules.vectorgraphics",
            "com.unity.modules.vehicles",
            "com.unity.modules.video",
            "com.unity.modules.vr",
            "com.unity.modules.wind",
            "com.unity.modules.xr"
        };

        private const double TimeoutSeconds = 300.0;
        private static AddAndRemoveRequest _request;
        private static double _deadline;
        private static bool _exitEditorOnFinish;

        [MenuItem("XO Playable/11. Trim Unused Packages (<5MB WebGL)", false, 85)]
        public static void TrimUnusedPackagesInteractive()
        {
            if (_request != null && !_request.IsCompleted)
            {
                Debug.LogWarning("[XOPackageTrim] Package trim request is already running.");
                return;
            }
            StartTrim(exitOnFinish: false);
        }

        // Headless entrypoint: Unity -batchmode -projectPath . -executeMethod XOPlayable.EditorTools.XOPackageTrimUtility.TrimUnusedPackages (NO -quit)
        public static void TrimUnusedPackages()
        {
            StartTrim(exitOnFinish: true);
        }

        private static void StartTrim(bool exitOnFinish)
        {
            _exitEditorOnFinish = exitOnFinish;
            string manifestPath = Path.GetFullPath("Packages/manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[XOPackageTrim] Packages/manifest.json not found.");
                if (_exitEditorOnFinish) EditorApplication.Exit(1);
                return;
            }

            string manifestText = File.ReadAllText(manifestPath);
            List<string> toRemove = CandidatePackagesToRemove
                .Where(id => manifestText.Contains($"\"{id}\""))
                .ToList();

            if (toRemove.Count == 0)
            {
                Debug.Log("[XOPackageTrim] All unused packages are already removed.");
                if (_exitEditorOnFinish) EditorApplication.Exit(0);
                return;
            }

            Debug.Log($"[XOPackageTrim] Removing {toRemove.Count} unused packages: {string.Join(", ", toRemove)}");
            _request = Client.AddAndRemove(Array.Empty<string>(), toRemove.ToArray());
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (_request == null) return;

            if (!_request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("[XOPackageTrim] Timed out waiting for Unity Package Manager.");
                    if (_exitEditorOnFinish) EditorApplication.Exit(2);
                }
                return;
            }

            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
            {
                string remaining = string.Join(", ", _request.Result.Select(p => $"{p.name}@{p.version}"));
                Debug.Log($"[XOPackageTrim] Completed successfully. Direct+resolved packages: {remaining}");
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
                if (_exitEditorOnFinish) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[XOPackageTrim] Failed: {_request.Error?.message}");
                if (_exitEditorOnFinish) EditorApplication.Exit(1);
            }
        }
    }
}
#endif

