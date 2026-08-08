using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ZeroHour.Bridge
{
    /// <summary>
    /// Mirrors the Unity console to <c>bridge/console.log</c> and keeps a rolling in-memory
    /// buffer for the <c>get_logs</c> command.
    ///
    /// Without this, every iteration costs a human round-trip: run the game, read the console,
    /// paste the error back. Writing logs where the agent can read them collapses that loop.
    /// </summary>
    [InitializeOnLoad]
    public static class BridgeConsole
    {
        private const int MaxBufferedEntries = 500;
        private const long MaxLogBytes = 4 * 1024 * 1024;

        private static readonly List<BridgeLogEntry> Buffer = new List<BridgeLogEntry>(MaxBufferedEntries);
        private static readonly object Gate = new object();

        static BridgeConsole()
        {
            // Unsubscribe first: a domain reload can otherwise leave a stale delegate attached
            // and every log line gets written twice.
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
        }

        public static IReadOnlyList<BridgeLogEntry> Recent(int count)
        {
            lock (Gate)
            {
                if (Buffer.Count == 0)
                {
                    return System.Array.Empty<BridgeLogEntry>();
                }

                int take = Mathf.Clamp(count, 1, Buffer.Count);
                return Buffer.GetRange(Buffer.Count - take, take);
            }
        }

        public static int ErrorCount()
        {
            lock (Gate)
            {
                int errors = 0;
                foreach (BridgeLogEntry entry in Buffer)
                {
                    if (entry.severity == "Error" || entry.severity == "Exception" || entry.severity == "Assert")
                    {
                        errors++;
                    }
                }

                return errors;
            }
        }

        public static void Clear()
        {
            lock (Gate)
            {
                Buffer.Clear();

                try
                {
                    if (File.Exists(BridgePaths.ConsoleLog))
                    {
                        File.Delete(BridgePaths.ConsoleLog);
                    }
                }
                catch (IOException)
                {
                    // A locked log file is not worth failing the command over.
                }
            }
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            var entry = new BridgeLogEntry
            {
                severity = SeverityOf(type),
                message = message,
                stackTrace = type == LogType.Log || type == LogType.Warning ? string.Empty : stackTrace,
                timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            };

            // This fires from background threads, so both the buffer and the file stay
            // behind one lock.
            lock (Gate)
            {
                Buffer.Add(entry);
                if (Buffer.Count > MaxBufferedEntries)
                {
                    Buffer.RemoveRange(0, Buffer.Count - MaxBufferedEntries);
                }

                try
                {
                    BridgePaths.EnsureDirectory();
                    string path = BridgePaths.ConsoleLog;

                    // Roll rather than truncate: a runaway log loop would otherwise fill the disk.
                    var info = new FileInfo(path);
                    if (info.Exists && info.Length > MaxLogBytes)
                    {
                        File.Copy(path, path + ".1", true);
                        File.Delete(path);
                    }

                    var line = new StringBuilder()
                        .Append('[').Append(entry.timestamp).Append("] ")
                        .Append(entry.severity.ToUpperInvariant()).Append(": ")
                        .Append(entry.message);

                    if (!string.IsNullOrEmpty(entry.stackTrace))
                    {
                        line.AppendLine().Append(entry.stackTrace.TrimEnd());
                    }

                    File.AppendAllText(path, line.AppendLine().ToString(), Encoding.UTF8);
                }
                catch (IOException)
                {
                    // A locked or unavailable log file must never take the editor down with it.
                }
            }
        }

        private static string SeverityOf(LogType type)
        {
            switch (type)
            {
                case LogType.Error:     return "Error";
                case LogType.Assert:    return "Assert";
                case LogType.Warning:   return "Warning";
                case LogType.Exception: return "Exception";
                default:                return "Log";
            }
        }
    }
}
