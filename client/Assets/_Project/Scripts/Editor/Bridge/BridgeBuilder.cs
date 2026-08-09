using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZeroHour.Bridge
{
    /// <summary>
    /// Player builds driven from the bridge (docs/28 §2): <c>build_android</c> and
    /// <c>build_webgl</c>.
    ///
    /// The point of these is not to produce a shippable artifact — it is to catch the class of
    /// breakage that only appears when the real player pipeline runs. IL2CPP conversion,
    /// managed-code stripping and platform-only compile errors are all invisible to both
    /// <c>compile</c> and the test runner, because those run against the editor's Mono
    /// assemblies with the editor's define symbols.
    ///
    /// <para><b>These block the editor.</b> <c>BuildPipeline.BuildPlayer</c> is synchronous, so
    /// the response is only written when the build finishes, minutes later. That is why callers
    /// need a long timeout rather than the few seconds every other command needs.</para>
    /// </summary>
    internal static class BridgeBuilder
    {
        /// <summary>
        /// Runs a player build and responds with the summary from Unity's own build report.
        /// </summary>
        internal static void Begin(string id, string command, string argString)
        {
            BuildTarget target;
            BuildTargetGroup group;

            switch (command)
            {
                case "build_android":
                    target = BuildTarget.Android;
                    group = BuildTargetGroup.Android;
                    break;

                case "build_webgl":
                    target = BuildTarget.WebGL;
                    group = BuildTargetGroup.WebGL;
                    break;

                default:
                    BridgeResponder.Respond(id, false, "Unknown build command '" + command + "'.");
                    return;
            }

            // A build cannot run while playing, and the failure if you try is obscure.
            if (EditorApplication.isPlaying)
            {
                BridgeResponder.Respond(id, false,
                    "Cannot build while in play mode. Send exit_play first.");
                return;
            }

            // Without this, a missing module surfaces later as an unrelated-looking error about
            // the target not being found. Both modules are installed here, but a fresh machine
            // following docs/27 §2 is exactly where this goes wrong.
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                BridgeResponder.Respond(id, false,
                    "Build target " + target + " is not supported by this editor install. "
                    + "Add the module via Unity Hub (docs/27 §2).");
                return;
            }

            string[] scenes = EnabledScenes();
            if (scenes.Length == 0)
            {
                // Unity builds a player with no scenes quite happily, and it then fails at
                // runtime with a black screen. Refuse rather than produce that.
                BridgeResponder.Respond(id, false,
                    "No enabled scenes in Build Settings; a player with no scenes would build "
                    + "and then fail at runtime.");
                return;
            }

            bool development = string.Equals(argString, "dev", StringComparison.OrdinalIgnoreCase)
                || string.Equals(argString, "development", StringComparison.OrdinalIgnoreCase);

            string outputDirectory = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, "Build", target.ToString());

            string locationPath = target == BuildTarget.Android
                ? Path.Combine(outputDirectory, "zerohour.apk")
                : outputDirectory;

            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                BridgeResponder.Respond(id, false,
                    "Could not create output directory: " + ex.GetType().Name + ": " + ex.Message);
                return;
            }

            // APK rather than AAB: this artifact exists to be installed on a device and to prove
            // the pipeline runs. Signing and bundles belong with the release pipeline in Phase 9.
            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = false;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                target = target,
                targetGroup = group,
                locationPathName = locationPath,
                options = development
                    ? BuildOptions.Development | BuildOptions.AllowDebugging
                    : BuildOptions.None,
            };

            BridgeWatcher.Trace("build starting: " + target + (development ? " (development)" : ""));

            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            catch (Exception ex)
            {
                BridgeWatcher.Trace("build threw: " + ex.GetType().Name + ": " + ex.Message);
                BridgeResponder.Respond(id, false,
                    "Build threw " + ex.GetType().Name + ": " + ex.Message);
                return;
            }

            if (report == null)
            {
                // Reported as a failure rather than a success with empty numbers: a null report
                // is indistinguishable from a clean build if you only look at the error count.
                BridgeResponder.Respond(id, false, "Build returned no report.");
                return;
            }

            BuildSummary summary = report.summary;
            bool succeeded = summary.result == BuildResult.Succeeded;

            // Trust the bytes on disk over the summary's own count, for the same reason the
            // screenshot command stats its output: a size reported by the thing that claims to
            // have written the file is not independent evidence that the file exists.
            long bytesOnDisk = MeasureOutput(summary.outputPath);

            BridgeWatcher.Trace("build " + summary.result + " in " + summary.totalTime
                + "; " + bytesOnDisk + " bytes at " + summary.outputPath);

            string seconds = summary.totalTime.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture);

            string extra =
                "\"target\":" + Json.Str(target.ToString())
                + ",\"result\":" + Json.Str(summary.result.ToString())
                + ",\"development\":" + (development ? "true" : "false")
                + ",\"errorCount\":" + Json.Num(summary.totalErrors)
                + ",\"warningCount\":" + Json.Num(summary.totalWarnings)
                + ",\"durationSeconds\":" + seconds
                + ",\"outputPath\":" + Json.Str(summary.outputPath ?? string.Empty)
                + ",\"outputBytes\":" + Json.Num(bytesOnDisk)
                + ",\"sceneCount\":" + Json.Num(scenes.Length);

            // A "succeeded" build that wrote nothing is a failure worth surfacing loudly, and
            // is the same trap as a test run that matched zero tests: all-zero looks like green.
            if (succeeded && bytesOnDisk == 0)
            {
                BridgeResponder.Respond(id, false,
                    "Build reported success but wrote no bytes to '" + summary.outputPath + "'.",
                    extra);
                return;
            }

            string message = succeeded
                ? "Built " + target + " in " + seconds + "s (" + FormatBytes(bytesOnDisk) + ")."
                : "Build " + summary.result + " with " + summary.totalErrors + " error(s).";

            BridgeResponder.Respond(id, succeeded, message, extra);
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

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L)
            {
                return (bytes / (1024.0 * 1024.0)).ToString("F1", CultureInfo.InvariantCulture) + " MB";
            }

            if (bytes >= 1024L)
            {
                return (bytes / 1024.0).ToString("F1", CultureInfo.InvariantCulture) + " KB";
            }

            return bytes + " B";
        }
    }
}
