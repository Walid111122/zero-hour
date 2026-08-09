using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
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

        // How long to wait for a requested compile to actually begin before concluding there
        // was nothing to build. Unity queues compilation on a later editor update, so this only
        // needs to outlast a few frames; it is not a timeout on the compile itself.
        private const double CompileStartGraceSeconds = 4.0;

        private const string PendingIdKey = "ZeroHour.Bridge.PendingId";
        private const string PendingCommandKey = "ZeroHour.Bridge.PendingCommand";

        // Tracks how far a pending compile has got: "requested" until compilation is observed
        // to start, then "started". Held in SessionState rather than a static because the whole
        // point is to still know this on the far side of a domain reload.
        private const string CompileStateKey = "ZeroHour.Bridge.CompileState";

        private static double _nextPoll;

        // Deadline for a requested compile to begin. Only meaningful before compilation starts,
        // which is also the only window in which no reload can have happened, so a static is
        // safe here.
        private static double _compileStartDeadline;

        /// <summary>
        /// Appends a line to <c>bridge/trace.log</c>.
        ///
        /// Kept after it earned its place: diagnosing anything that spans a domain reload needs
        /// a sink that survives one, which rules out both the console buffer (a static, wiped by
        /// the reload) and Debug.Log (Editor.log was not being flushed while the editor ran
        /// unfocused). This is what identified the dropped-callback bug in the compile path, and
        /// the next reload-related bug will need it just as much.
        /// </summary>
        internal static void Trace(string line)
        {
            try
            {
                BridgePaths.EnsureDirectory();
                File.AppendAllText(BridgePaths.TraceLog,
                    DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                    + "  " + line + Environment.NewLine);
            }
            catch
            {
                // Tracing must never break the command it is tracing.
            }
        }

        static BridgeWatcher()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            Trace("domain loaded; pendingId='"
                + SessionState.GetString(PendingIdKey, string.Empty) + "' pendingCommand='"
                + SessionState.GetString(PendingCommandKey, string.Empty) + "'");

            // Note there is deliberately no `delayCall` hook for finishing a compile. That was
            // the original design and it silently dropped responses: delayCall is single-shot,
            // and a compile here reliably produces *two* domain reloads in close succession, so
            // the callback registered by the first reload is discarded by the second before it
            // ever runs. Completion is polled from Tick instead, which cannot be lost.

            // Play-mode transitions are completed from the state-change event rather than the
            // domain reload above. "Enter Play Mode Options" lets the project disable the
            // reload entirely, and the reload on *leaving* play mode is not something to rely
            // on either; this event fires either way.
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode
                && change != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            string id = SessionState.GetString(PendingIdKey, string.Empty);
            string command = SessionState.GetString(PendingCommandKey, string.Empty);

            if (string.IsNullOrEmpty(id) || (command != "enter_play" && command != "exit_play"))
            {
                return;
            }

            // Claim the pending id before responding, so the reload path cannot answer twice.
            SessionState.EraseString(PendingIdKey);
            SessionState.EraseString(PendingCommandKey);

            bool playing = change == PlayModeStateChange.EnteredPlayMode;
            bool wanted = command == "enter_play";

            Respond(id, playing == wanted,
                playing ? "Entered play mode." : "Exited play mode.",
                "\"errorCount\":" + Json.Num(BridgeConsole.ErrorCount()));
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll)
            {
                return;
            }

            _nextPoll = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            CheckPendingCompile();

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
                    SetPlaying(request.id, true);
                    break;

                case "exit_play":
                    SetPlaying(request.id, false);
                    break;

                case "run_tests":
                    // argString selects the mode ("edit" / "play"); empty defaults to edit.
                    BridgeTestRunner.Begin(request.id, request.argString);
                    break;

                case "open_scene":
                    OpenSceneByPath(request.id, request.argString);
                    break;

                case "build_android":
                case "build_webgl":
                    // Synchronous and slow: this blocks the editor for minutes and the response
                    // is written only when the build finishes. argString "dev" adds a
                    // development build with debugging enabled.
                    BridgeBuilder.Begin(request.id, request.command, request.argString);
                    break;

                default:
                    // Closed allowlist: unknown commands are refused, not guessed at.
                    Respond(request.id, false, "Unknown command '" + request.command + "'.");
                    break;
            }
        }

        // ---------- play mode ----------

        /// <summary>
        /// Enters or leaves play mode, answering immediately when already in the target state.
        ///
        /// The deferred path relies on <see cref="OnPlayModeChanged"/> firing. Asking for a state
        /// the editor is already in produces no transition and therefore no event, so parking the
        /// id would leave the caller waiting for a response that is never written.
        /// </summary>
        private static void SetPlaying(string id, bool play)
        {
            if (EditorApplication.isPlaying == play)
            {
                Respond(id, true, play ? "Already in play mode." : "Already stopped.",
                    "\"errorCount\":" + Json.Num(BridgeConsole.ErrorCount()));
                return;
            }

            SessionState.SetString(PendingIdKey, id);
            SessionState.SetString(PendingCommandKey, play ? "enter_play" : "exit_play");
            EditorApplication.isPlaying = play;
        }

        // ---------- scenes ----------

        /// <summary>
        /// Opens a scene by project-relative path, e.g. "Assets/_Project/Scenes/Boot.unity".
        ///
        /// Play mode runs whichever scene is currently open rather than build index 0, so
        /// driving play from outside the editor is only meaningful if the scene can be selected
        /// first. The path is constrained to .unity files under Assets/ because the bridge reads
        /// its input from a file that any local process can write.
        /// </summary>
        private static void OpenSceneByPath(string id, string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                Respond(id, false, "open_scene requires argString with a project-relative scene path.");
                return;
            }

            string normalised = scenePath.Replace('\\', '/');

            if (!normalised.StartsWith("Assets/", StringComparison.Ordinal)
                || !normalised.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                || normalised.Contains(".."))
            {
                Respond(id, false, "Scene path must be a .unity file under Assets/ with no '..' segments.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(normalised) == null)
            {
                Respond(id, false, "No scene asset at '" + normalised + "'.");
                return;
            }

            try
            {
                Scene opened = EditorSceneManager.OpenScene(normalised, OpenSceneMode.Single);
                Respond(id, opened.IsValid(),
                    opened.IsValid() ? "Opened scene '" + opened.name + "'." : "Failed to open scene.",
                    "\"scene\":" + Json.Str(opened.name) + ",\"path\":" + Json.Str(normalised));
            }
            catch (Exception ex)
            {
                Respond(id, false, "OpenScene threw: " + ex.Message);
            }
        }

        // ---------- compile ----------

        private static void BeginCompile(string id)
        {
            // Unity defers script compilation while playing, so the domain reload this waits on
            // never arrives and the caller hangs until it times out. Refusing loudly beats
            // failing silently.
            if (EditorApplication.isPlaying)
            {
                Respond(id, false, "Cannot compile during play mode. Send exit_play first.");
                return;
            }

            SessionState.SetString(PendingIdKey, id);
            SessionState.SetString(PendingCommandKey, "compile");
            SessionState.SetString(CompileStateKey, "requested");

            Trace("BeginCompile id=" + id + "; requesting compilation");

            _compileStartDeadline = EditorApplication.timeSinceStartup + CompileStartGraceSeconds;

            AssetDatabase.Refresh();
            CompilationPipeline.RequestScriptCompilation();
        }

        /// <summary>
        /// Drives a pending `compile` to a response, polled from <see cref="Tick"/>.
        ///
        /// Polling rather than a completion callback because every callback-shaped option is
        /// destroyed by the very domain reload it is waiting on. There are three outcomes and
        /// all of them must produce a response:
        ///
        /// - compilation starts, then finishes (possibly across several reloads) → real result;
        /// - compilation never starts because everything is up to date → "nothing to compile";
        /// - the editor is mid-reload → keep waiting.
        /// </summary>
        private static void CheckPendingCompile()
        {
            string state = SessionState.GetString(CompileStateKey, string.Empty);
            if (string.IsNullOrEmpty(state))
            {
                return;
            }

            if (state == "requested")
            {
                if (EditorApplication.isCompiling)
                {
                    Trace("compile started");
                    SessionState.SetString(CompileStateKey, "started");
                    return;
                }

                // The deadline is a static, so a reload resets it to zero and this branch is
                // skipped — correct, because a reload proves compilation did happen.
                if (_compileStartDeadline > 0
                    && EditorApplication.timeSinceStartup >= _compileStartDeadline)
                {
                    Trace("no compile started within grace; reporting up to date");
                    FinishCompile(true, "Assemblies already up to date; nothing to compile.", false);
                }

                return;
            }

            // state == "started": wait for compilation to drain, then report.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            Trace("compile finished; writing response");
            FinishCompile(null, null, true);
        }

        /// <summary>
        /// Writes the response for a pending compile and clears its state.
        /// <paramref name="ok"/> and <paramref name="message"/> are null when the outcome should
        /// be derived from the console buffer.
        /// </summary>
        private static void FinishCompile(bool? ok, string message, bool recompiled)
        {
            string id = SessionState.GetString(PendingIdKey, string.Empty);

            SessionState.EraseString(PendingIdKey);
            SessionState.EraseString(PendingCommandKey);
            SessionState.EraseString(CompileStateKey);

            if (string.IsNullOrEmpty(id))
            {
                return;
            }

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

            string extra = "\"recompiled\":" + Json.Bool(recompiled)
                         + ",\"errorCount\":" + Json.Num(errors.Count)
                         + ",\"warningCount\":" + Json.Num(warnings.Count)
                         + ",\"errors\":" + Json.Array(errors)
                         + ",\"warnings\":" + Json.Array(warnings);

            Respond(id,
                ok ?? errors.Count == 0,
                message ?? (errors.Count == 0 ? "Compiled cleanly." : "Compilation produced errors."),
                extra);
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

        /// <summary>
        /// Renders a camera to a texture and writes a PNG, synchronously.
        ///
        /// <c>ScreenCapture.CaptureScreenshot</c> was the obvious choice and does not work here:
        /// it targets the Game view and defers to end of frame, which in the editor produced no
        /// file at all in either edit or play mode. Rendering a camera explicitly means the file
        /// exists before this method returns, so the response can assert it rather than promise it.
        /// </summary>
        private static void TakeScreenshot(string id)
        {
            const int Width = 1080;
            const int Height = 1920;

            BridgePaths.EnsureDirectory();
            string path = BridgePaths.Screenshot;

            Camera camera = Camera.main;
            if (camera == null)
            {
                foreach (Camera candidate in Camera.allCameras)
                {
                    if (candidate != null && candidate.isActiveAndEnabled)
                    {
                        camera = candidate;
                        break;
                    }
                }
            }

            if (camera == null)
            {
                Respond(id, false, "No active camera in the loaded scene to capture.");
                return;
            }

            RenderTexture target = null;
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousCameraTarget = camera.targetTexture;
            Texture2D image = null;

            try
            {
                target = new RenderTexture(Width, Height, 24);
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();

                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                // Restore first: leaving a camera pointed at a destroyed target breaks the
                // editor's own rendering, which is a far worse failure than a missing PNG.
                camera.targetTexture = previousCameraTarget;
                RenderTexture.active = previousActive;

                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }

                if (image != null)
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }

            var written = new FileInfo(path);
            Respond(id, written.Exists && written.Length > 0,
                written.Exists ? "Screenshot written." : "Screenshot failed to write.",
                "\"path\":" + Json.Str(path)
                + ",\"bytes\":" + Json.Num((int)(written.Exists ? written.Length : 0))
                + ",\"camera\":" + Json.Str(camera.name));
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

        // Now a thin forward to BridgeResponder, which `run_tests` also answers through. Kept
        // as a local method so the many call sites above read unchanged.
        private static void Respond(string id, bool ok, string message, string extraJson = null)
        {
            BridgeResponder.Respond(id, ok, message, extraJson);
        }
    }
}
