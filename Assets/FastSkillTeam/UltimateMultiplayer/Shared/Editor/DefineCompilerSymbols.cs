namespace FastSkillTeam.UltimateMultiplayer.Installer.Utility
{
    using Opsive.Shared.Utility;
    using UnityEditor;

    [UnityEngine.ExecuteInEditMode]
    public class AntiCheatMenu : Editor
    {
        private static string s_AntiCheatSymbol = "ANTICHEAT";
#if ANTICHEAT
        [MenuItem("Tools/FastSkillTeam/UltimateMultiplayer/DisableAntiCheat")]
        public static void DisableAntiCheat()
        {
            DefineCompilerSymbols.RemoveSymbol(s_AntiCheatSymbol);
        }
#else
        [MenuItem("Tools/FastSkillTeam/UltimateMultiplayer/EnableAntiCheat")]
        public static void EnableAntiCheat()
        {
            var antiCheatExists = TypeUtility.GetType("CodeStage.AntiCheat.ObscuredTypes.ObscuredBigInteger") != null;
            if (antiCheatExists)
                DefineCompilerSymbols.AddSymbol(s_AntiCheatSymbol);
            else UnityEngine.Debug.LogError("CodeStage AntiCheat Tool Kit Must be installed first!");
        }
#endif
    }

    /// <summary>
    /// Editor script which will define or remove the Ultimate Seating Controller compiler symbols so the components are aware of the asset import status.
    /// </summary>
    [InitializeOnLoad]
    public class DefineCompilerSymbols
    {
#if ULTIMATE_SEATING_CONTROLLER
        private static string s_UltimateSeatingControllerSymbol = "ULTIMATE_SEATING_CONTROLLER";
#endif
#if !ULTIMATE_MULTIPLAYER
        private static string s_UltimateMultiplayerSymbol = "ULTIMATE_MULTIPLAYER";
#endif
        /// <summary>
        /// If the specified classes exist then the compiler symbol should be defined, otherwise the symbol should be removed.
        /// </summary>
        static DefineCompilerSymbols()
        {
#if !ULTIMATE_MULTIPLAYER
            AddSymbol(s_UltimateMultiplayerSymbol);
#endif

#if ULTIMATE_SEATING_CONTROLLER //Defined when the Ultimate Seating Controller asset is imported
            // The BoardSource will exist when the Ultimate Seating Controller asset is imported.
            var ultimateSeatingControllerExists = TypeUtility.GetType("FastSkillTeam.UltimateSeatingController.BoardSource") != null;
            if (!ultimateSeatingControllerExists)
                RemoveSymbol(s_UltimateSeatingControllerSymbol);
#endif
        }
        /// <summary>
        /// Adds the specified symbol to the compiler definitions.
        /// </summary>
        /// <param name="symbol">The symbol to add.</param>
        public static void AddSymbol(string symbol)
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (symbols.Contains(symbol))
            {
                return;
            }
            symbols += (";" + symbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
        }

        /// <summary>
        /// Remove the specified symbol from the compiler definitions.
        /// </summary>
        /// <param name="symbol">The symbol to remove.</param>
        public static void RemoveSymbol(string symbol)
        {
            var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!symbols.Contains(symbol))
            {
                return;
            }
            if (symbols.Contains(";" + symbol))
            {
                symbols = symbols.Replace(";" + symbol, "");
            }
            else
            {
                symbols = symbols.Replace(symbol, "");
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, symbols);
        }
    }
}