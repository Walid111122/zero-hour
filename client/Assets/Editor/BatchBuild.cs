using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless batch-mode entry points for player builds: <c>BuildAndroid()</c>.
///
/// Batch mode does not show modal dialogs, which is the whole point: the GUI editor reported
/// the Android input-handling failure through a modal, so <c>BuildPipeline.BuildPlayer</c>
/// never returned and the bridge sat waiting 3h23m for a click. Headless, the same failure
/// surfaces as an exception and a non-zero exit code.
///
/// It also removes the need to restart the editor by hand after switching Active Input Handling:
/// every batch invocation is a fresh process, so setting it in one run and building in the next
/// is enough.
///
/// Guards before the build rather than 20 minutes in: rejects <c>activeInputHandler == 2</c>
/// up front, and fails on zero enabled scenes. Wraps everything in try/catch and calls
/// <c>EditorApplication.Exit</c> explicitly, since <c>-quit</c> alone exits 0 even when the
/// build failed.
/// </summary>
public static class BatchBuild
{
    /// <summary>
    /// Invoked via Unity batch mode:
    /// <c>Unity -batchmode -quit -projectPath "..." -executeMethod BatchBuild.BuildAndroid</c>
    /// </summary>
    public static void BuildAndroid()
    {
        BuildTarget target = BuildTarget.Android;
        BuildTargetGroup group = BuildTargetGroup.Android;

        try
        {
            Console.WriteLine("[BatchBuild] target=" + target);

            // Fail immediately if activeInputHandler is 2 ("Both"), which Android refuses to build.
            // The bridge-driven build catches this later with an exception; here we check up front
            // and exit before IL2CPP spends 18 minutes stripping.
            if (!VerifyInputHandler())
            {
                EditorApplication.Exit(2);
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                Console.Error.WriteLine(
                    "[BatchBuild] Build target " + target + " is not supported by this editor install. " +
                    "Add the module via Unity Hub (docs/27 §2).");
                EditorApplication.Exit(3);
                return;
            }

            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                Console.Error.WriteLine(
                    "[BatchBuild] No enabled scenes in Build Settings; a player with no scenes " +
                    "would build and then fail at runtime.");
                EditorApplication.Exit(4);
                return;
            }

            Console.WriteLine("[BatchBuild] scenes=" + scenes.Length + " [" + string.Join(", ",
                scenes.Select(s => Path.GetFileNameWithoutExtension(s))) + "]");

            string outputDirectory = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, "Build", target.ToString());

            string locationPath = Path.Combine(outputDirectory, "zerohour.apk");

            Directory.CreateDirectory(outputDirectory);

            // APK rather than AAB: this artifact exists to be installed on a device and to prove
            // the pipeline runs. Signing and bundles belong with the release pipeline in Phase 9.
            EditorUserBuildSettings.buildAppBundle = false;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                targetGroup = group,
                locationPathName = locationPath,
                options = BuildOptions.None,
            };

            Console.WriteLine("[BatchBuild] building...");
            DateTime start = DateTime.UtcNow;

            BuildReport report = BuildPipeline.BuildPlayer(options);

            TimeSpan elapsed = DateTime.UtcNow - start;

            if (report == null)
            {
                Console.Error.WriteLine("[BatchBuild] Build returned no report.");
                EditorApplication.Exit(5);
                return;
            }

            BuildSummary summary = report.summary;
            bool succeeded = summary.result == BuildResult.Succeeded;

            long bytesOnDisk = MeasureOutput(summary.outputPath);

            Console.WriteLine(
                "[BatchBuild] result=" + summary.result
                + " elapsed=" + elapsed.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture) + "s"
                + " errors=" + summary.totalErrors
                + " warnings=" + summary.totalWarnings
                + " bytes=" + bytesOnDisk
                + " path=" + summary.outputPath);

            if (succeeded && bytesOnDisk == 0)
            {
                Console.Error.WriteLine(
                    "[BatchBuild] Build reported success but wrote no bytes to '" +
                    summary.outputPath + "'.");
                EditorApplication.Exit(6);
                return;
            }

            if (!succeeded)
            {
                Console.Error.WriteLine(
                    "[BatchBuild] Build " + summary.result + " with " + summary.totalErrors + " error(s).");
                EditorApplication.Exit(7);
                return;
            }

            Console.WriteLine("[BatchBuild] SUCCESS");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[BatchBuild] EXCEPTION: " + ex.GetType().Name + ": " + ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            EditorApplication.Exit(99);
        }
    }

    /// <summary>
    /// Checks that Active Input Handling is not set to "Both" (value 2), which Android refuses
    /// to build. Returns true if OK, false if the value is 2.
    /// </summary>
    private static bool VerifyInputHandler()
    {
        try
        {
            var singleton = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");

            var so = new SerializedObject(singleton);
            SerializedProperty handler = so.FindProperty("activeInputHandler");

            if (handler == null)
            {
                Console.Error.WriteLine(
                    "[BatchBuild] activeInputHandler not found — Unity changed the asset layout. " +
                    "Cannot verify. Proceeding anyway.");
                return true;
            }

            int value = handler.intValue;
            Console.WriteLine("[BatchBuild] activeInputHandler=" + value);

            if (value == 2)
            {
                Console.Error.WriteLine(
                    "[BatchBuild] Active Input Handling is set to 'Both' (2), which Android refuses to build. " +
                    "Run 'Unity -batchmode -quit -projectPath \"...\" " +
                    "-executeMethod ZeroHour.EditorTools.PlayerSettingsSetup.ForceSingleInputHandling' first, " +
                    "then invoke this build again (the setting change needs a fresh process).");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                "[BatchBuild] Could not read activeInputHandler: " + ex.GetType().Name + ": " +
                ex.Message + " — proceeding anyway.");
            return true;
        }
    }

    /// <summary>
    /// The enabled scenes from Build Settings, in order.
    /// </summary>
    private static string[] EnabledScenes()
    {
        var paths = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene != null && scene.enabled && !string.IsNullOrEmpty(scene.path))
            {
                paths.Add(scene.path);
            }
        }

        return paths.ToArray();
    }

    /// <summary>
    /// Size of the build output, whether it is a single file (APK) or a directory (WebGL).
    /// </summary>
    private static long MeasureOutput(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        try
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }

            if (!Directory.Exists(path))
            {
                return 0;
            }

            long total = 0;
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                total += new FileInfo(file).Length;
            }

            return total;
        }
        catch
        {
            // Measuring must not turn a successful build into a failed command.
            return 0;
        }
    }
}
