using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ZeroHour.Bridge
{
    /// <summary>
    /// `run_tests` for the Cline ↔ Unity bridge (docs/28 §2).
    ///
    /// Kept out of <see cref="BridgeWatcher"/> because a test run is the only bridge command
    /// with a genuinely asynchronous lifecycle: the call that starts it returns immediately,
    /// results arrive on a callback interface, and both edit- and play-mode runs can reload
    /// the domain in between.
    ///
    /// That reload is the whole difficulty. `TestRunnerApi.RegisterCallbacks` stores callbacks
    /// in a static registry, and statics do not survive a reload — so a run that reloads
    /// mid-flight completes with nobody listening, and the caller waits forever. The same
    /// class of bug already bit `compile` and `exit_play` here. The fix is the documented one:
    /// callbacks live on a <see cref="ScriptableObject"/> and re-register from
    /// <see cref="InitializeOnLoadMethodAttribute"/> whenever a run is still marked pending,
    /// so the listener is rebuilt on the far side of the reload.
    /// </summary>
    public static class BridgeTestRunner
    {
        // SessionState survives domain reloads but not editor restarts, which is exactly the
        // lifetime a pending run should have: a run cannot outlive the editor process.
        private const string PendingIdKey = "ZeroHour.Bridge.Tests.PendingId";
        private const string PendingModeKey = "ZeroHour.Bridge.Tests.PendingMode";

        [InitializeOnLoadMethod]
        private static void ReattachAfterDomainReload()
        {
            if (string.IsNullOrEmpty(SessionState.GetString(PendingIdKey, string.Empty)))
            {
                return;
            }

            // A run is in flight and the reload just destroyed its listener. Rebuild it.
            GetApi().RegisterCallbacks(ScriptableObject.CreateInstance<BridgeTestCallbacks>());
        }

        public static void Begin(string id, string mode)
        {
            // Default to edit mode: it is the fast path, and the one worth running on every
            // iteration. Play mode is opt-in because it takes seconds and enters play mode.
            string requested = string.IsNullOrEmpty(mode) ? "edit" : mode.Trim().ToLowerInvariant();

            TestMode testMode;
            switch (requested)
            {
                case "edit":
                case "editmode":
                    testMode = TestMode.EditMode;
                    break;

                case "play":
                case "playmode":
                    testMode = TestMode.PlayMode;
                    break;

                default:
                    BridgeResponder.Respond(id, false,
                        "Unknown test mode '" + requested + "'. Use 'edit' or 'play'.");
                    return;
            }

            if (!string.IsNullOrEmpty(SessionState.GetString(PendingIdKey, string.Empty)))
            {
                // Two concurrent runs would race to write the single response file, and the
                // second registration would report the first run's results.
                BridgeResponder.Respond(id, false,
                    "A test run is already in progress. Wait for it to finish.");
                return;
            }

            if (Application.isPlaying)
            {
                // Same reasoning as `compile` refusing during play mode: starting a run from
                // inside play mode fights the framework for control of the play state.
                BridgeResponder.Respond(id, false,
                    "Cannot start a test run while in play mode. Call exit_play first.");
                return;
            }

            SessionState.SetString(PendingIdKey, id);
            SessionState.SetString(PendingModeKey, requested);

            var filter = new Filter { testMode = testMode };

            // Restrict discovery to this project's own test assemblies. An unfiltered run also
            // executes tests shipped inside packages — Addressables contributes one — which
            // means a package author's test could turn the build red for code we do not own.
            string[] assemblies = ProjectTestAssemblies();
            if (assemblies.Length > 0)
            {
                filter.assemblyNames = assemblies;
            }

            TestRunnerApi api = GetApi();
            api.RegisterCallbacks(ScriptableObject.CreateInstance<BridgeTestCallbacks>());
            api.Execute(new ExecutionSettings(filter));
        }

        /// <summary>
        /// Names the test assemblies that live under <c>Assets/</c>, identified by their
        /// reference to NUnit.
        ///
        /// Derived from the compilation pipeline rather than hardcoded, so adding a second test
        /// assembly does not silently stop it being run — the failure mode of a hardcoded list
        /// is tests that quietly never execute, which is worse than no filter at all.
        ///
        /// Returns empty if nothing matches, and the caller then runs unfiltered; a run that
        /// matches no tests is already reported as a failure rather than a pass.
        /// </summary>
        private static string[] ProjectTestAssemblies()
        {
            var names = new List<string>();

            foreach (Assembly assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor))
            {
                if (assembly.sourceFiles == null || assembly.sourceFiles.Length == 0)
                {
                    continue;
                }

                string first = assembly.sourceFiles[0].Replace('\\', '/');
                if (!first.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string reference in assembly.compiledAssemblyReferences)
                {
                    if (reference.Replace('\\', '/').Contains("nunit.framework"))
                    {
                        names.Add(assembly.name);
                        break;
                    }
                }
            }

            return names.ToArray();
        }

        internal static TestRunnerApi GetApi()
        {
            return ScriptableObject.CreateInstance<TestRunnerApi>();
        }

        internal static string ClaimPendingId()
        {
            string id = SessionState.GetString(PendingIdKey, string.Empty);

            // Claim before responding, so neither a second callback nor the reload path can
            // answer the same request twice.
            SessionState.EraseString(PendingIdKey);
            return id;
        }

        internal static string PendingMode()
        {
            return SessionState.GetString(PendingModeKey, "edit");
        }
    }

    /// <summary>
    /// Receives the test run result. A ScriptableObject rather than a plain class so it can be
    /// recreated and re-registered after a domain reload — see the note on
    /// <see cref="BridgeTestRunner"/>.
    /// </summary>
    public class BridgeTestCallbacks : ScriptableObject, ICallbacks
    {
        // Per-test callbacks are deliberately ignored. Accumulating them would mean carrying a
        // buffer across the domain reload; the finished result already contains the full tree,
        // so it is walked once at the end instead.
        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            string id = BridgeTestRunner.ClaimPendingId();
            if (string.IsNullOrEmpty(id))
            {
                // Nothing is waiting — a run started from the Test Runner window rather than
                // the bridge. Writing a response here would corrupt an unrelated request.
                return;
            }

            var failures = new List<string>();
            var names = new List<string>();
            CollectFailures(result, failures);
            CollectNames(result, names);

            int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;

            // Zero tests is reported as a failure on purpose. A filter that matches nothing
            // returns all-zero counts, which is indistinguishable from a green run if you only
            // look at FailCount — the same trap the Postgres suite hit in CI, where 8 skipped
            // tests looked exactly like 8 passing ones.
            bool ok = result.FailCount == 0 && total > 0;

            string message = total == 0
                ? "No tests matched. Check that a test assembly exists for this mode."
                : (result.FailCount == 0
                    ? "All " + total + " tests passed."
                    : result.FailCount + " of " + total + " tests failed.");

            BridgeResponder.Respond(id, ok, message,
                "\"mode\":" + Json.Str(BridgeTestRunner.PendingMode())
                + ",\"total\":" + Json.Num(total)
                + ",\"passed\":" + Json.Num(result.PassCount)
                + ",\"failed\":" + Json.Num(result.FailCount)
                + ",\"skipped\":" + Json.Num(result.SkipCount)
                + ",\"inconclusive\":" + Json.Num(result.InconclusiveCount)
                + ",\"durationSeconds\":" + result.Duration.ToString("F3", CultureInfo.InvariantCulture)
                + ",\"failures\":" + Json.Array(failures)
                + ",\"tests\":" + Json.StringArray(names));
        }

        /// <summary>
        /// Lists every leaf test that ran, passed or not.
        ///
        /// Added because a run once reported five passing tests against four authored ones, and
        /// a bare count gives you no way to tell whether that is a harmless synthetic node or a
        /// stale duplicate assembly being discovered. Names make the discrepancy answer itself.
        /// </summary>
        private static void CollectNames(ITestResultAdaptor result, List<string> into)
        {
            const int MaxNames = 200;

            if (into.Count >= MaxNames)
            {
                return;
            }

            if (result.HasChildren)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    CollectNames(child, into);
                }

                return;
            }

            // A suite with no children is still a suite, not a test. An empty play-mode run
            // reported `client [Passed]` — the project root node, childless because nothing
            // matched — which reads as a phantom passing test in an otherwise empty run.
            if (result.Test.IsSuite)
            {
                return;
            }

            into.Add(result.Test.FullName + " [" + result.TestStatus + "]");
        }

        private static void CollectFailures(ITestResultAdaptor result, List<string> into)
        {
            // Cap the report. A broken assembly can fail hundreds of tests, and a response too
            // large to read is barely better than no response.
            const int MaxFailures = 25;

            if (into.Count >= MaxFailures)
            {
                return;
            }

            if (result.HasChildren)
            {
                foreach (ITestResultAdaptor child in result.Children)
                {
                    CollectFailures(child, into);
                }

                return;
            }

            if (result.TestStatus != TestStatus.Failed)
            {
                return;
            }

            var sb = new StringBuilder("{\"test\":").Append(Json.Str(result.Test.FullName))
                .Append(",\"message\":").Append(Json.Str(Truncate(result.Message, 500)))
                .Append(",\"stackTrace\":").Append(Json.Str(Truncate(result.StackTrace, 800)))
                .Append('}');

            into.Add(sb.ToString());
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max)
            {
                return value;
            }

            return value.Substring(0, max) + "… (truncated)";
        }
    }
}
