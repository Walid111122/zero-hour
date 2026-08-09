using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ZeroHour.EditorTools
{
    /// <summary>
    /// Scripts the Android player settings and the three quality tiers (`18 §8`).
    ///
    /// These live in code rather than as a "click these boxes" list in the docs because project
    /// settings are one big serialized asset: a setting changed deliberately and one changed by
    /// accident look identical in a diff. Both entry points are idempotent.
    /// </summary>
    public static class PlayerSettingsSetup
    {
        [MenuItem("Zero Hour/Setup/Configure Android Player Settings")]
        public static void ConfigureAndroidPlayerSettings()
        {
            NamedBuildTarget android = NamedBuildTarget.Android;

            // Portrait only — the UI is designed one-thumb vertical (`19 §1`), so the autorotate
            // flags are cleared rather than left at their permissive defaults.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // IL2CPP + ARM64. Mono cannot produce a 64-bit Android binary and Play has required
            // 64-bit since 2019, so this is a store gate rather than a performance preference.
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Unity 6 clamps the minimum to API 26: asking for 24 silently yields 26, which
            // only surfaced by reading the value back. So request what the editor will actually
            // honour rather than a number it discards. The Phase 0 checklist asked for 24 and
            // is now unachievable on this editor; the practical cost is dropping Android 7.x.
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;

            PlayerSettings.SetApplicationIdentifier(android, "com.zerohour.game");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // Managed stripping drops unreferenced code. It cannot see types reached only by
            // reflection, so anything deserialised dynamically will need a link.xml entry —
            // worth recalling at the first "works in editor, null on device" bug.
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.gcIncremental = true;

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[PlayerSettings] Android: portrait, IL2CPP, ARM64, " +
                $"minSdk {(int)PlayerSettings.Android.minSdkVersion}, " +
                $"targetSdk {(int)PlayerSettings.Android.targetSdkVersion}");
        }

        /// <summary>
        /// Forces Active Input Handling to a single backend (Input System package).
        ///
        /// "Both" (`activeInputHandler: 2`) is what the project was left on, and Unity **refuses
        /// to build Android with it** — `BuildPlayer` throws
        /// "Active Input Handling is set to Both, this is unsupported on Android ... Cancelling".
        /// WebGL and the editor accept it happily, so nothing catches this until a build targets
        /// the shipping platform.
        ///
        /// There is no `PlayerSettings` property for this flag, so it goes through the serialized
        /// singleton like the quality tiers. Two things to know:
        ///  - the change needs an **editor restart** to take effect, and Unity asks for that with
        ///    a modal dialog, so this cannot run unattended from the bridge;
        ///  - a modal is also how the build failure was reported, which is why a build driven
        ///    through the bridge can appear to hang for hours with the answer on screen.
        /// </summary>
        [MenuItem("Zero Hour/Setup/Force Single Input Handling")]
        public static void ForceSingleInputHandling()
        {
            const int inputSystemPackage = 1;

            Object singleton = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            SerializedObject so = new(singleton);

            SerializedProperty handler = so.FindProperty("activeInputHandler");
            if (handler == null)
            {
                Debug.LogError(
                    "[PlayerSettings] activeInputHandler missing — Unity changed the asset layout. " +
                    "Set Project Settings > Player > Active Input Handling to 'Input System Package' by hand.");
                return;
            }

            if (handler.intValue == inputSystemPackage)
            {
                Debug.Log("[PlayerSettings] Active Input Handling already 'Input System Package' (1) — no change.");
                return;
            }

            int previous = handler.intValue;
            handler.intValue = inputSystemPackage;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Debug.LogWarning(
                $"[PlayerSettings] Active Input Handling {previous} -> {inputSystemPackage} (Input System Package). " +
                "RESTART THE EDITOR before building: the backend switch only takes effect on reload.");
        }

        // Quality levels can only be added or removed through the serialized asset. The runtime
        // API cannot do it: QualitySettings.names returns a *copy*, so assigning into it silently
        // does nothing, and Increase/DecreaseLevel change which level is active rather than how
        // many exist. Both mistakes compile and appear to work.
        [MenuItem("Zero Hour/Setup/Create Quality Tiers")]
        public static void CreateQualityTiers()
        {
            Object singleton = Unsupported.GetSerializedAssetInterfaceSingleton("QualitySettings");
            SerializedObject so = new(singleton);

            SerializedProperty levels = so.FindProperty("m_QualitySettings");
            if (levels == null)
            {
                Debug.LogError("[Quality] m_QualitySettings missing — Unity changed the asset layout.");
                return;
            }

            levels.arraySize = 3;

            // Low — under 3 GB RAM. Half-res textures, no shadows, no AA.
            ApplyTier(levels.GetArrayElementAtIndex(0), "Low",
                textureLimit: 1, antiAliasing: 0, shadows: 0,
                shadowDistance: 0f, softParticles: false, reflectionProbes: false);

            // Medium — 3-6 GB. Full textures, hard shadows only.
            ApplyTier(levels.GetArrayElementAtIndex(1), "Medium",
                textureLimit: 0, antiAliasing: 0, shadows: 1,
                shadowDistance: 40f, softParticles: false, reflectionProbes: false);

            // High — 6 GB+. Everything on.
            ApplyTier(levels.GetArrayElementAtIndex(2), "High",
                textureLimit: 0, antiAliasing: 2, shadows: 2,
                shadowDistance: 80f, softParticles: true, reflectionProbes: true);

            SerializedProperty current = so.FindProperty("m_CurrentQuality");
            if (current != null)
            {
                current.intValue = 1;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            Debug.Log($"[Quality] Tiers rebuilt: [{string.Join(", ", QualitySettings.names)}], default Medium");

            // The 30/60 fps caps from `18 §8` are deliberately absent. Frame rate is
            // Application.targetFrameRate — a runtime value with no per-tier slot in this asset.
            // It belongs to the runtime quality service in Phase 8; setting it from an editor
            // script would look configured and do nothing.
        }

        static void ApplyTier(
            SerializedProperty tier,
            string name,
            int textureLimit,
            int antiAliasing,
            int shadows,
            float shadowDistance,
            bool softParticles,
            bool reflectionProbes)
        {
            SerializedProperty nameProp = Find(tier, "name");
            if (nameProp != null)
            {
                nameProp.stringValue = name;
            }

            // Renamed in Unity 6 from the long-standing "textureQuality". Try both so an editor
            // upgrade cannot make this quietly stop applying.
            SetInt(tier, textureLimit, "globalTextureMipmapLimit", "textureQuality");

            SetInt(tier, antiAliasing, "antiAliasing");
            SetInt(tier, shadows, "shadows");
            SetInt(tier, shadows == 0 ? 0 : 2, "shadowResolution");
            SetFloat(tier, shadowDistance, "shadowDistance");
            SetBool(tier, softParticles, "softParticles");
            SetBool(tier, reflectionProbes, "realtimeReflectionProbes");

            // vSync off on mobile: the platform paces to the display already, and a non-zero
            // count fights the frame cap the runtime service will apply.
            SetInt(tier, 0, "vSyncCount");
        }

        static SerializedProperty Find(SerializedProperty parent, params string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                SerializedProperty found = parent.FindPropertyRelative(candidate);
                if (found != null)
                {
                    return found;
                }
            }

            Debug.LogWarning($"[Quality] none of [{string.Join(", ", candidates)}] exist on this tier — skipped");
            return null;
        }

        static void SetInt(SerializedProperty parent, int value, params string[] candidates)
        {
            SerializedProperty p = Find(parent, candidates);
            if (p != null)
            {
                p.intValue = value;
            }
        }

        static void SetFloat(SerializedProperty parent, float value, params string[] candidates)
        {
            SerializedProperty p = Find(parent, candidates);
            if (p != null)
            {
                p.floatValue = value;
            }
        }

        static void SetBool(SerializedProperty parent, bool value, params string[] candidates)
        {
            SerializedProperty p = Find(parent, candidates);
            if (p != null)
            {
                p.boolValue = value;
            }
        }

        /// <summary>
        /// Reads the settings back out of the project rather than trusting that the setters ran,
        /// so the bridge can confirm them from outside the editor.
        /// </summary>
        [MenuItem("Zero Hour/Setup/Verify Settings")]
        public static void VerifySettings()
        {
            NamedBuildTarget android = NamedBuildTarget.Android;
            List<string> problems = new();

            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                problems.Add($"orientation {PlayerSettings.defaultInterfaceOrientation}, expected Portrait");
            }

            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(android);
            if (backend != ScriptingImplementation.IL2CPP)
            {
                problems.Add($"backend {backend}, expected IL2CPP");
            }

            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                problems.Add($"architectures {PlayerSettings.Android.targetArchitectures}, expected ARM64");
            }

            if (PlayerSettings.Android.minSdkVersion != AndroidSdkVersions.AndroidApiLevel26)
            {
                problems.Add($"minSdk {PlayerSettings.Android.minSdkVersion}, expected 26");
            }

            if (PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevel35)
            {
                problems.Add($"targetSdk {PlayerSettings.Android.targetSdkVersion}, expected 35");
            }

            // Read straight from the serialized asset: there is no PlayerSettings accessor, and
            // this is the setting that cancels an Android build outright.
            SerializedObject playerSettings = new(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            SerializedProperty handler = playerSettings.FindProperty("activeInputHandler");
            if (handler == null)
            {
                problems.Add("activeInputHandler not found — cannot verify Active Input Handling");
            }
            else if (handler.intValue == 2)
            {
                problems.Add("Active Input Handling is 'Both' (2), which Android refuses to build — run Force Single Input Handling");
            }

            string[] names = QualitySettings.names;
            if (names.Length != 3 || names[0] != "Low" || names[1] != "Medium" || names[2] != "High")
            {
                problems.Add($"tiers [{string.Join(", ", names)}], expected [Low, Medium, High]");
            }

            if (problems.Count == 0)
            {
                Debug.Log(
                    "[Verify] OK — portrait, IL2CPP, ARM64, " +
                    $"SDK {(int)PlayerSettings.Android.minSdkVersion}-{(int)PlayerSettings.Android.targetSdkVersion}, " +
                    $"input handling {(handler == null ? "?" : handler.intValue.ToString())}, " +
                    $"tiers [{string.Join(", ", names)}]");
            }
            else
            {
                Debug.LogError($"[Verify] FAILED: {string.Join(" | ", problems)}");
            }
        }
    }
}
