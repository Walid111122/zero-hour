#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ZeroHour.EditorTools
{
    /// <summary>
    /// One-shot package installer, invoked from the command line so the Phase 0 package set
    /// is reproducible rather than a sequence of clicks somebody has to remember.
    ///
    /// Package names are deliberately unversioned: the Package Manager then resolves the
    /// newest version compatible with this editor, which avoids pinning a version that does
    /// not exist for 6000.5 and failing the resolve outright.
    /// </summary>
    public static class PackageSetup
    {
        // Phase 0 set only. Localization, Mobile Notifications, WebRTC (Phase 4) and
        // Google Play Billing (Phase 8) are added when the phase that needs them starts —
        // an unused package is still import time on every project open.
        private static readonly string[] Required =
        {
            "com.unity.render-pipelines.universal",
            "com.unity.inputsystem",
            "com.unity.addressables",
            "com.unity.test-framework",
            "com.unity.nuget.newtonsoft-json",
            "com.unity.ugui",
        };

        public static void Install()
        {
            Debug.Log("[PackageSetup] Installing " + Required.Length + " packages...");

            AddAndRemoveRequest request = Client.AddAndRemove(Required, Array.Empty<string>());

            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }

            if (request.Status == StatusCode.Failure)
            {
                Debug.LogError("[PackageSetup] FAILED: " + request.Error.message);
                EditorApplication.Exit(1);
                return;
            }

            foreach (var package in request.Result.OrderBy(p => p.name))
            {
                Debug.Log("[PackageSetup]   " + package.name + " @ " + package.version);
            }

            Debug.Log("[PackageSetup] Done.");
            EditorApplication.Exit(0);
        }
    }
}
#endif
