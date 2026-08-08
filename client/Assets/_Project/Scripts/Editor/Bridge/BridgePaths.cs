using System.IO;
using UnityEngine;

namespace ZeroHour.Bridge
{
    /// <summary>
    /// Resolves the <c>bridge/</c> folder at the repository root — one level above the Unity
    /// project, so the transcript sits next to the solution rather than inside Assets where
    /// Unity would try to import it as content.
    ///
    /// Note the gitignore rule for that folder must be anchored (<c>/bridge/</c>). Unanchored,
    /// it also matches this source directory and silently excludes the Bridge itself.
    /// </summary>
    public static class BridgePaths
    {
        /// <summary>Repository root: the parent of the Unity project folder.</summary>
        public static string RepoRoot
        {
            get
            {
                // Application.dataPath is <repo>/client/Assets.
                DirectoryInfo assets = new DirectoryInfo(Application.dataPath);
                return assets.Parent?.Parent?.FullName ?? assets.FullName;
            }
        }

        public static string Root       => Path.Combine(RepoRoot, "bridge");
        public static string Request    => Path.Combine(Root, "request.json");
        public static string Response   => Path.Combine(Root, "response.json");
        public static string ConsoleLog => Path.Combine(Root, "console.log");
        public static string Screenshot => Path.Combine(Root, "screenshot.png");

        public static void EnsureDirectory()
        {
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }
        }
    }
}
