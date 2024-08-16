#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Roadmans_Fortnite.Editor.ServerCreation
{
    /// <summary>
    /// This creates a Unity editor tab for building the Master Server. 
    /// The Master Server manages lobbies, matchmaking, and the creation of sub-servers for game sessions.
    /// </summary>
    public static class MasterServerBuild
    {
        public static BuildTargetGroup DefaultBuildTargetGroup;
        public static BuildTarget DefaultBuildTarget;

        public static string ServerRunArguments;

        public const string UniqueSeparator = "Server~Creator";

        #region Save & Load

        public static void Save()
        {
            PlayerPrefs.SetInt(nameof(DefaultBuildTargetGroup), (int)DefaultBuildTargetGroup);
            PlayerPrefs.SetInt(nameof(DefaultBuildTarget), (int)DefaultBuildTarget);
            PlayerPrefs.SetString(nameof(ServerRunArguments), ServerRunArguments);
        }

        [InitializeOnLoadMethod]
        private static void Load()
        {
            DefaultBuildTargetGroup = (BuildTargetGroup)PlayerPrefs.GetInt(nameof(DefaultBuildTargetGroup), (int)BuildTargetGroup.Standalone);
            DefaultBuildTarget = (BuildTarget)PlayerPrefs.GetInt(nameof(DefaultBuildTarget), (int)BuildTarget.StandaloneWindows64);
            ServerRunArguments = PlayerPrefs.GetString(nameof(ServerRunArguments), string.Empty);
        }

        #endregion

        private static void Build(BuildTargetGroup targetGroup, BuildTarget target, BuildOptions options, bool run)
        {
            Load();

            string dirName = "LocalMasterServer";
            string fileName = "LocalMasterServer.exe";

            if (!System.IO.Directory.Exists("Builds"))
            {
                System.IO.Directory.CreateDirectory("Builds");
            }

            BuildPlayerOptions playerOptions = new()
            {
                locationPathName = System.IO.Path.Combine("Builds", dirName, fileName),
                options = options | BuildOptions.EnableHeadlessMode, // Enable headless mode
                targetGroup = targetGroup,
                target = target,
                scenes = new string[] { "Assets/Scenes/Optimized Scenes/Server.unity" }
            };

            BuildInfo.Instance.UpdateLastBuildTimestamp();
            BuildInfo.Instance.Save();

            BuildReport buildReport = BuildPipeline.BuildPlayer(playerOptions);

            if (buildReport.summary.result == BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(playerOptions.locationPathName);

                if (run)
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo(playerOptions.locationPathName, ServerRunArguments)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    Process process = Process.Start(processStartInfo);
                    Debug.Log("[Build] Master Server build succeeded.");
                }
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(DefaultBuildTargetGroup, DefaultBuildTarget);
        }

        [MenuItem("Local Master Server/Build/Master Server/Build Master Server", priority = 200)]
        public static void BuildMasterServer()
        {
            Build(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, BuildOptions.None, false);
        }

        [MenuItem("Local Master Server/Build/Master Server/Build and Run Master Server", priority = 201)]
        public static void BuildAndRunMasterServer()
        {
            Build(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, BuildOptions.None, true);
        }
    }
}
#endif
