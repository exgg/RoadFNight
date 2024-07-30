#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEngine {

	/// <summary>
	/// This creates a unity editor tab for building the games servers, it needs to have 2 servers running along side
	/// Unity in order to allow local connection. This therefor will allow for players or a dev to connect into the server once initialized on
	/// the command prompt
	/// </summary>
	public static class EditorBuild {

		public static BuildTargetGroup defaultBuildTargetGroup;
		public static BuildTarget defaultBuildTarget;

		public static string serverRunArguments;

        public const string UNIQUE_SEPARATOR = "Advanced~Multiplayer~Template";

		#region Save & Load
		
		/// <summary>
		/// Save the current build settings to player prefs
		/// </summary>
		public static void Save() {
			PlayerPrefs.SetInt(nameof(defaultBuildTargetGroup), (int)defaultBuildTargetGroup);
			PlayerPrefs.SetInt(nameof(defaultBuildTarget), (int)defaultBuildTarget);

			PlayerPrefs.SetString(nameof(serverRunArguments), serverRunArguments);

			if (BuildStripper.clientDirs.Count > 0) {
				PlayerPrefs.SetString(nameof(BuildStripper.clientDirs), string.Join(UNIQUE_SEPARATOR, BuildStripper.clientDirs));
			} else {
				PlayerPrefs.DeleteKey(nameof(BuildStripper.clientDirs));
			}
		}

		/// <summary>
		/// Load the saved build settings from PlayerPrefs when the editor loads
		/// </summary>
		[InitializeOnLoadMethod]
		private static void Load() {
			defaultBuildTargetGroup = (BuildTargetGroup)PlayerPrefs.GetInt(nameof(defaultBuildTargetGroup), (int)BuildTargetGroup.Standalone);
			defaultBuildTarget = (BuildTarget)PlayerPrefs.GetInt(nameof(defaultBuildTarget), (int)BuildTarget.StandaloneWindows64);

			serverRunArguments = PlayerPrefs.GetString(nameof(serverRunArguments), string.Empty);

			if (PlayerPrefs.HasKey(nameof(BuildStripper.clientDirs))) {
				BuildStripper.clientDirs =
					new(PlayerPrefs.GetString(nameof(BuildStripper.clientDirs)).Split(UNIQUE_SEPARATOR, System.StringSplitOptions.None));
			}
		}
		#endregion

		/// <summary>
		/// Adds a "[Build]" Tag to text
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string GetTaggedText(string text) {
			return string.Concat("[Build] ", text);
		}

		/// <summary>
		/// Build Project Server with specific options
		/// </summary>
		private static void Build(BuildTargetGroup targetGroup, BuildTarget target, BuildOptions options, bool server, string fileFormat, bool revealInFinder, bool run, bool masterServer) {
			Load();

			// dir & file names
			string dirName = target.ToString();
			if (server) {
				dirName += "Server";
			}
            if (masterServer){
                dirName += "MasterServer";
            }
            string fileName = server ? "Server" : Application.productName;//?
            if(masterServer)
                fileName = server ? "MasterServer" : Application.productName;//?
            fileName += fileFormat;

			// avoid the "the path "..." does not exist" error
			if (!System.IO.Directory.Exists("Builds")) {
				System.IO.Directory.CreateDirectory("Builds");
			}

			BuildPlayerOptions playerOptions = new() {
				locationPathName = System.IO.Path.Combine("Builds", dirName, fileName),
				options = options
			};

			// sub
			if (server) {
				_ = EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Server, target);
				EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
				playerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
			} else {
				_ = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
				EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
				playerOptions.subtarget = (int)StandaloneBuildSubtarget.Player;
			}

			// additional options
			playerOptions.targetGroup = targetGroup;
			playerOptions.target = target;
            if(masterServer)
                playerOptions.scenes = new string[1] {
            "Assets/Advanced Multiplayer Template/MasterServer/Scenes/MasterServerScene.unity"
        };
            else
                playerOptions.scenes = playerOptions.scenes = new string[] {
            "Assets/Scenes/TestScene.unity"
        };

            // set UTC build timestamp
            BuildInfo.Instance.UpdateLastBuildTimestamp();
			BuildInfo.Instance.Save();

			// strip
			BuildStripper.Strip(server);

			BuildReport buildReport = BuildPipeline.BuildPlayer(playerOptions);

			// revert
			BuildStripper.RevertStrip();

			if (buildReport.summary.result == BuildResult.Succeeded) {
				if (revealInFinder) {
					EditorUtility.RevealInFinder(playerOptions.locationPathName);
				}

				if (run) {
					_ = Process.Start(new ProcessStartInfo(playerOptions.locationPathName, server ? serverRunArguments : string.Empty));
				}

				// increment & log
				string reportLogTags = GetTaggedText(string.Concat('[', target.ToString(), "] "));

				if (!server) {
					string buildNumberStr = null;

					switch (target) {
						case BuildTarget.StandaloneWindows:
						case BuildTarget.StandaloneWindows64:
							buildNumberStr = BuildInfo.Instance.winClientBuildNumber++.ToString();
							break;
						case BuildTarget.StandaloneLinux64:
							buildNumberStr = BuildInfo.Instance.linuxClientBuildNumber++.ToString();
							break;
						case BuildTarget.Android:
							buildNumberStr = BuildInfo.Instance.androidClientBuildNumber++.ToString();
							break;
					}

					BuildInfo.Instance.Save();

					if (buildNumberStr != null) {
						reportLogTags = string.Concat(reportLogTags, '[', buildNumberStr, "] ");
					}
				}

				Debug.Log(string.Concat(reportLogTags, "Success. Ignore \".meta\" warnings (if any)"));
			}

			// switch to default target
			_ = EditorUserBuildSettings.SwitchActiveBuildTarget(defaultBuildTargetGroup, defaultBuildTarget);
			EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
		}

		public const string
			WINDOWS_EXTENSION = ".exe",
			LINUX_EXTENSION = ".x86_64",
			ANDROID_BUNDLE_EXTENSION = ".aab",
			ANDROID_EXTENSION = ".apk";

		#region Windows
		private static void BuildWindows(bool server, bool run, bool dev, bool masterServer) {
			Build(BuildTargetGroup.Standalone,
				BuildTarget.StandaloneWindows64,
				dev ? BuildOptions.Development : BuildOptions.None,
				server,
				WINDOWS_EXTENSION,
				!run,
				run,
                masterServer);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Windows (x64)/Client", priority = 100)]
		public static void BuildWindowsClient() {
			BuildWindows(false, false, false, false);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Windows (x64)/Client (Run)", priority = 101)]
		public static void BuildWindowsClientRun() {
			BuildWindows(false, true, false, false);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Windows (x64)/Server", priority = 103)]
		public static void BuildWindowsServer() {
			BuildWindows(true, false, false, false);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Windows (x64)/Server (Run)", priority = 104)]
		public static void BuildWindowsServerRun() {
			BuildWindows(true, true, false, false);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Windows (x64)/Client and Server", priority = 105)]
		public static void BuildWindowsBoth() {
			BuildWindowsServer();
			BuildWindowsClient();
		}

        [MenuItem("Advanced Multiplayer Template/Build/Windows (x64)/Master Server", priority = 106)]
        public static void BuildWindowsMasterServer(){
            BuildWindows(true, false, false, true);
        }

        [MenuItem("Advanced Multiplayer Template/Build/Windows (x64)/Master Server (Run)", priority = 107)]
        public static void BuildWindowsMasterServerRun()
        {
            BuildWindows(true, true, false, true);
        }
        #endregion

        #region Linux
        private static void BuildLinux(bool server, bool masterServer) {
			Build(BuildTargetGroup.Standalone,
				BuildTarget.StandaloneLinux64,
				BuildOptions.None,
				server,
				LINUX_EXTENSION,
				true,
				false,
                masterServer);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Linux (x64)/Client", priority = 110)]
		public static void BuildLinuxClient() {
			BuildLinux(false, false);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Linux (x64)/Server", priority = 111)]
		public static void BuildLinuxServer() {
			BuildLinux(true, false);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Linux (x64)/Client and Server", priority = 112)]
		public static void BuildLinuxBoth() {
			BuildLinuxServer();
			BuildLinuxClient();
		}

        [MenuItem("Advanced Multiplayer Template/Build/Linux (x64)/Master Server", priority = 113)]
        public static void BuildLinuxMasterServer()
        {
            BuildLinux(true, true);
        }
        #endregion

        #region Android
        private static void BuildAndroid(bool bundle, BuildOptions options) {
			EditorUserBuildSettings.buildAppBundle = bundle;
			Build(BuildTargetGroup.Android,
				BuildTarget.Android,
				options,
				false,
				bundle ? ANDROID_BUNDLE_EXTENSION : ANDROID_EXTENSION,
				true,
				false,
                false);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Android/AAB", priority = 120)]
		public static void BuildAndroidBundle() {
			BuildAndroid(true, BuildOptions.UncompressedAssetBundle);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Android/APK", priority = 121)]
		public static void BuildAndroid() {
			BuildAndroid(false, BuildOptions.None);
		}

		[MenuItem("Advanced Multiplayer Template/Build/Android/Development (Run)", priority = 122)]
		public static void BuildAndroidRun() {
			BuildAndroid(false, BuildOptions.AutoRunPlayer | BuildOptions.Development);
		}
		#endregion
	}
}
#endif
