using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZeroHour.Bridge
{
    /// <summary>
    /// Polls <c>bridge/request.json</c>, executes an allowlisted command, and writes
    /// <c>bridge/response.json</c> (docs/28 §2).
    ///
    /// A file protocol rather than a socket, for two reasons: it survives the domain reloads
    /// that a compile triggers (a socket would drop mid-command), and it leaves an inspectable
    /// transcript on disk when something misbehaves.
    ///
    /// There is deliberately no "run arbitrary C#" command. The command set is a closed
    /// allowlist; anything outside it is rejected. An editor extension that evaluates
    /// arbitrary code is a remote-execution hole in a process that holds signing keys.
    /// </summary>
    [InitializeOnLoad]
    public static class BridgeWatcher
    {
        private const double PollIntervalSeconds = 0.5;
        private const string PendingIdKey = "ZeroHour.Bridge.PendingId";
        private const string PendingCommandKey = "ZeroHour.Bridge.PendingCommand";

        private static double _nextPoll;

        static BridgeWatcher()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            // A compile or play-mode change reloads the domain and wipes this class's state,
            // so a command that spans a reload parks its id in SessionState and completes here.
            EditorApplication.delayCall += CompletePendingAcrossReload;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }

            _nextPoll = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            // Never consume a request mid-compile: the response would describe a stale state.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            string requestPath = BridgePaths.Request;
            if (!File.Exists(requestPath))
            {
                return;
            }

            BridgeRequest request;
            try
            {
                string json = File.ReadAllText(requestPath);
                File.Delete(requestPath);
                request = JsonUtility.FromJson<BridgeRequest>(json);
            }
            catch (Exception ex)
            {
                Respond("unknown", false, "Malformed request: " + ex.Message);
                return;
            }

            if (request == null || string.IsNullOrEmpty(request.command))
            {
                Respond("unknown", false, "Request missing a command.");
                return;
            }

            try
            {
                Execute(request);
            }
            catch (Exception ex)
            {
                Respond(request.id, false, ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void Execute(BridgeRequest request)
        {
            switch (request.command)
            {
                case "ping":
                    Respond(request.id, true, "pong", "\"unityVersion\":" + Json.Str(Application.unityVersion));
                    break;

                case "refresh":
                    AssetDatabase.Refresh();
                    Respond(request.id, true, "Asset database refreshed.");
                    break;

                case "compile":
                    BeginCompile(request.id);
                    break;

                case "get_logs":
                    RespondWithLogs(request.id, request.argInt > 0 ? request.argInt : 50);
                    break;

                case "clear_logs":
                    BridgeConsole.Clear();
                    Respond(request.id, true, "Console buffer cleared.");
                    break;

                case "screenshot":
                    TakeScreenshot(request.id);
                    break;

                case "scene_dump":
                    DumpScene(request.id);
                    break;

                case "enter_play":
                    SessionState.SetString(PendingIdKey, request.id);
                    SessionState.SetString(PendingCommandKey, "enter_play");
                    EditorApplication.isPlaying = true;
                    break;

                case "exit_play":
                    SessionState.SetString(PendingIdKey, request.id);
                    SessionState.SetString(PendingCommandKey, "exit_play");
                    EditorApplication.isPlaying = false;
                    break;

                default:
                    // Closed allowlist: unknown commands are refused, not guessed at.
                    Respond(request.id, false, "Unknown command '" + request.command + "'.");
                    break;
            }
        }

        // ---------- compile ----------

        private static void BeginCompile(string id)
        {
            SessionState.SetString(PendingIdKey, id);
            SessionState.SetString(PendingCommandKey, "compile");

            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();

            // The domain reload lands next; CompletePendingAcrossReload writes the response
            // once the editor is back.
        }

        private static void CompletePendingAcrossReload()
        {
            string id = SessionState.GetString(PendingIdKey, string.Empty);
            string command = SessionState.GetString(PendingCommandKey, string.Empty);

            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            SessionState.EraseString(PendingIdKey);
            SessionState.EraseString(PendingCommandKey);

            switch (command)
            {
                case "compile":
                {
                    var errors = new List<string>();
                    var warnings = new List<string>();

                    foreach (BridgeLogEntry entry in BridgeConsole.Recent(200))
                    {
                        if (entry.message.Contains("error CS"))
                        {
                            errors.Add(Json.Str(entry.message));
                        }
                        else if (entry.message.Contains("warning CS"))
                        {
                            warnings.Add(Json.Str(entry.message));
                        }
                    }

                    string extra = "\"errorCount\":" + Json.Num(errors.Count)
                                 + ",\"warningCount\":" + Json.Num(warnings.Count)
                                 + ",\"errors\":" + Json.Array(errors)
                                 + ",\"warnings\":" + Json.Array(warnings);

                    Respond(id, errors.Count == 0,
                        errors.Count == 0 ? "Compiled cleanly." : "Compilation produced errors.",
                        extra);
                    break;
                }

                case "enter_play":
                    Respond(id, Application.isPlaying,
                        Application.isPlaying ? "Entered play mode." : "Failed to enter play mode.");
                    break;

                case "exit_play":
                    Respond(id, !Application.isPlaying, "Exited play mode.",
                        "\"errorCount\":" + Json.Num(BridgeConsole.ErrorCount()));
                    break;
            }
        }

        // ---------- logs ----------

        private static void RespondWithLogs(string id, int count)
        {
            var entries = new List<string>();
            foreach (BridgeLogEntry entry in BridgeConsole.Recent(count))
            {
                entries.Add("{\"severity\":" + Json.Str(entry.severity)
                          + ",\"message\":" + Json.Str(entry.message)
                          + ",\"stackTrace\":" + Json.Str(entry.stackTrace)
                          + ",\"timestamp\":" + Json.Str(entry.timestamp) + "}");
            }

            Respond(id, true, "Returned " + entries.Count + " entries.",
                "\"entries\":" + Json.Array(entries));
        }

        // ---------- screenshot ----------

        private static void TakeScreenshot(string id)
        {
            BridgePaths.EnsureDirectory();
            string path = BridgePaths.Screenshot;

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);

            // The capture lands at end of frame, so confirmation is deferred rather than
            // asserted here — claiming success immediately would sometimes be a lie.
            Respond(id, true, "Screenshot requested.",
                "\"path\":" + Json.Str(path)
                + ",\"note\":" + Json.Str("Written at end of frame; confirm the file exists before reading."));
        }

        // ---------- scene dump ----------

        private static void DumpScene(string id)
        {
            Scene scene = SceneManager.GetActiveScene();
            var roots = new List<string>();

            foreach (GameObject go in scene.GetRootGameObjects())
            {
                roots.Add(DescribeHierarchy(go.transform, 0));
            }

            Respond(id, true, "Dumped scene '" + scene.name + "'.",
                "\"scene\":" + Json.Str(scene.name)
                + ",\"path\":" + Json.Str(scene.path)
                + ",\"rootCount\":" + Json.Num(roots.Count)
                + ",\"hierarchy\":" + Json.Array(roots));
        }

        private static string DescribeHierarchy(Transform transform, int depth)
        {
            // Depth cap: a deep UI tree would otherwise produce a response too large to be useful.
            const int MaxDepth = 8;

            var components = new List<string>();
            foreach (Component component in transform.GetComponents<Component>())
            {
                components.Add(component == null ? "<missing script>" : component.GetType().Name);
            }

            var sb = new StringBuilder("{\"name\":").Append(Json.Str(transform.name))
                .Append(",\"active\":").Append(Json.Bool(transform.gameObject.activeSelf))
                .Append(",\"components\":").Append(Json.StringArray(components));

            if (depth < MaxDepth && transform.childCount > 0)
            {
                var children = new List<string>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    children.Add(DescribeHierarchy(transform.GetChild(i), depth + 1));
                }

                sb.Append(",\"children\":").Append(Json.Array(children));
            }

            return sb.Append('}').ToString();
        }

        // ---------- response ----------

        private static void Respond(string id, bool ok, string message, string extraJson = null)
        {
            BridgePaths.EnsureDirectory();

            var sb = new StringBuilder("{")
                .Append("\"id\":").Append(Json.Str(id))
                .Append(",\"ok\":").Append(Json.Bool(ok))
                .Append(",\"message\":").Append(Json.Str(message))
                .Append(",\"timestamp\":").Append(Json.Str(
                    DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));

            if (!string.IsNullOrEmpty(extraJson))
            {
                sb.Append(',').Append(extraJson);
            }

            sb.Append('}');

            // Write beside the target then move: a reader polling for the response must never
            // observe a half-written file.
            string finalPath = BridgePaths.Response;
            string tempPath = finalPath + ".tmp";
            File.WriteAllText(tempPath, sb.ToString(), Encoding.UTF8);

            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }

            File.Move(tempPath, finalPath);
        }
    }
}
