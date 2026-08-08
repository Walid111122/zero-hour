using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Creates the Boot and Main scenes and registers them in Build Settings (docs/17 §1).
///
/// Scenes are generated rather than hand-authored because a <c>.unity</c> file is YAML full
/// of file GUIDs and local ids; writing that by hand produces a file that opens but has
/// subtly broken references. Running the editor's own API is the only reliable way to do
/// this headlessly.
///
/// Idempotent: existing scenes are left alone, so re-running never clobbers work.
/// </summary>
public static class SceneSetup
{
    private const string SceneFolder = "Assets/_Project/Scenes";

    [MenuItem("ZeroHour/Setup/Create Core Scenes")]
    public static void CreateCoreScenes()
    {
        Directory.CreateDirectory(SceneFolder);

        string bootPath = SceneFolder + "/Boot.unity";
        string mainPath = SceneFolder + "/Main.unity";

        CreateBootScene(bootPath);
        CreateMainScene(mainPath);

        // Boot must be index 0: it is the composition root, and anything else first means
        // gameplay code hits an empty ServiceLocator.
        RegisterInBuildSettings(new[] { bootPath, mainPath });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SceneSetup] Boot and Main are ready and registered in Build Settings.");
    }

    private static void CreateBootScene(string path)
    {
        if (File.Exists(path))
        {
            Debug.Log($"[SceneSetup] {Path.GetFileName(path)} already exists, leaving it as is.");
            return;
        }

        // Boot needs no camera or light: it exists for a frame, wires services, then loads Main.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bootstrapObject = new GameObject("Bootstrap");
        System.Type bootstrapType = FindType("ZeroHour.Core.Bootstrap");

        if (bootstrapType != null)
        {
            bootstrapObject.AddComponent(bootstrapType);
        }
        else
        {
            // Happens if this runs before ZeroHour.Core has compiled. Say so plainly rather
            // than writing a scene with a silently missing component.
            Debug.LogWarning("[SceneSetup] ZeroHour.Core.Bootstrap not found. " +
                             "Compile first, then re-run to attach it.");
        }

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[SceneSetup] Created {path}");
    }

    private static void CreateMainScene(string path)
    {
        if (File.Exists(path))
        {
            Debug.Log($"[SceneSetup] {Path.GetFileName(path)} already exists, leaving it as is.");
            return;
        }

        // Main is the persistent scene: camera rig, UI root, and the services that need a
        // MonoBehaviour host all live here.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var uiRoot = new GameObject("UIRoot");
        var servicesRoot = new GameObject("Services");

        // Portrait mobile: the camera is orthographic and sized in Phase 1 once the runner
        // lane dimensions are settled.
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);
        }

        Undo.ClearAll();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);

        Debug.Log($"[SceneSetup] Created {path} with {uiRoot.name} and {servicesRoot.name}");
    }

    private static void RegisterInBuildSettings(IEnumerable<string> scenePaths)
    {
        var scenes = new List<EditorBuildSettingsScene>();

        foreach (string path in scenePaths)
        {
            if (File.Exists(path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        // Replace rather than append: re-running should not accumulate duplicates.
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static System.Type FindType(string fullName)
    {
        foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            System.Type type = assembly.GetType(fullName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
