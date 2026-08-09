using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ZeroHour.Bridge
{
    /// <summary>
    /// Writes the single bridge response file.
    ///
    /// Extracted from <see cref="BridgeWatcher"/> once `run_tests` needed to answer from a
    /// separate callback object. Two copies of this would be two chances to get the atomic
    /// write wrong, and a half-written response is precisely the failure the temp-file-and-move
    /// below exists to prevent.
    /// </summary>
    public static class BridgeResponder
    {
        public static void Respond(string id, bool ok, string message, string extraJson = null)
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
