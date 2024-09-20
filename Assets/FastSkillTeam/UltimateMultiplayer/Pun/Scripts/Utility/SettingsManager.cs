using UnityEngine;
namespace FastSkillTeam.UltimateMultiplayer.Pun.Utility {
    public static class SettingsManager
    {
        private const string LOOK_SENSITIVITY = "LookSensitivity";
        private const string USE_SCOPES_IN_THIRDPERSON = "UseScopesInThirdPerson";
        private const string PLAYER_NAME = "PlayerName";
        private const string SELECTED_KIT = "SelectedKit";

        private const string VOLUME_MASTER = "MasterVolume";
        private const string VOLUME_MUSIC = "MusicVolume";
        private const string VOLUME_FX = "FxVolume";

        private const string IS_MIC_MUTE = "IsMicMute";
        private const string INVERT_LOOK = "InvertLook";
        private const string DIFFICULTY = "Difficulty";

        private const string BRIGHTNESS = "Brightness";
        private const string QUALITY_LEVEL = "QualityLevel";
        private const string TARGET_FRAMERATE = "TargetFrameRate";
        private const string KILL_COUNT = "Kills";
        private const string DEATH_COUNT = "Deaths";
        private const string SELECTED_CHARACTER_START_DATA_INDEX = "SelectedCharacterStartDataIndex";
        private const string SELECTED_SCENE_INDEX = "SelectedSceneIndex";
        private const string SELECTED_GAME_MODE_INDEX = "SelectedGameModeIndex";

        public static string PlayerName
        {
            get { return PlayerPrefs.GetString(PLAYER_NAME, "Player"); }
            set { PlayerPrefs.SetString(PLAYER_NAME, value); }
        }

        public static int Difficulty
        {
            get { return PlayerPrefs.GetInt(DIFFICULTY, 0); }
            set { PlayerPrefs.SetInt(DIFFICULTY, value); }
        }

        public static float FxVolume
        {
            get { return PlayerPrefs.GetFloat(VOLUME_FX, 0.75f); }
            set { PlayerPrefs.SetFloat(VOLUME_FX, value); }
        }

        public static float MasterVolume
        {
            get { return PlayerPrefs.GetFloat(VOLUME_MASTER, 1f); }
            set { PlayerPrefs.SetFloat(VOLUME_MASTER, value); }
        }

        public static float MusicVolume
        {
            get { return PlayerPrefs.GetFloat(VOLUME_MUSIC, 0.75f); }
            set { PlayerPrefs.SetFloat(VOLUME_MUSIC, value); }
        }

        public static bool InvertLook
        {
            get { return PlayerPrefs.GetInt(INVERT_LOOK, 0) == 1; }
            set { PlayerPrefs.SetInt(INVERT_LOOK, value == true ? 1 : 0); }
        }

        public static bool UseScopesInThirdPerson
        {
            get { return PlayerPrefs.GetInt(USE_SCOPES_IN_THIRDPERSON, 0) == 1; }
            set { PlayerPrefs.SetInt(USE_SCOPES_IN_THIRDPERSON, value == true ? 1 : 0); }
        }

        public static float LookSensitivity
        {
            get { return PlayerPrefs.GetFloat(LOOK_SENSITIVITY, 5f); }
            set { PlayerPrefs.SetFloat(LOOK_SENSITIVITY, value); }
        }

        public static float Brightness
        {
            get { return PlayerPrefs.GetFloat(BRIGHTNESS, 1f); }
            set { PlayerPrefs.SetFloat(BRIGHTNESS, value); }
        }

        public static int QualityLevel
        {
            get { return PlayerPrefs.GetInt(QUALITY_LEVEL, 4); }
            set { PlayerPrefs.SetInt(QUALITY_LEVEL, value <= 4 ? value : 0); }
        }

        public static int TargetFrameRate
        {
            get { return PlayerPrefs.GetInt(TARGET_FRAMERATE, 60); }
            set { PlayerPrefs.SetInt(TARGET_FRAMERATE, value == 60 ? 60 : value == 30 ? 30 : 60); }
        }

        public static int DeathCount
        {
            get { return PlayerPrefs.GetInt(DEATH_COUNT, 0); }
            set { PlayerPrefs.SetInt(DEATH_COUNT, value); }
        }
        public static int KillCount
        {
            get { return PlayerPrefs.GetInt(KILL_COUNT, 0); }
            set { PlayerPrefs.SetInt(KILL_COUNT, value); }
        }
        public static int SelectedKit
        {
            get { return PlayerPrefs.GetInt(SELECTED_KIT, 0); }
            set { PlayerPrefs.SetInt(SELECTED_KIT, value); }
        }

        public static int SelectedCharacterStartDataIndex
        {
            get { return PlayerPrefs.GetInt(SELECTED_CHARACTER_START_DATA_INDEX, 0); }
            set { PlayerPrefs.SetInt(SELECTED_CHARACTER_START_DATA_INDEX, value); }
        }

        public static int SelectedSceneIndex
        {
            get { return PlayerPrefs.GetInt(SELECTED_SCENE_INDEX, 0); }
            set { PlayerPrefs.SetInt(SELECTED_SCENE_INDEX, value); }
        }

        public static int SelectedGameModeIndex
        {
            get { return PlayerPrefs.GetInt(SELECTED_GAME_MODE_INDEX, 0); }
            set { PlayerPrefs.SetInt(SELECTED_GAME_MODE_INDEX, value); }
        }

        public static bool IsMicMute
        {
            get { return PlayerPrefs.GetInt(IS_MIC_MUTE, 0) == 1; }
            set { PlayerPrefs.SetInt(IS_MIC_MUTE, value == true ? 1 : 0); }
        }

        public static void SetDefaults()
        {
            LookSensitivity = 5f;
            PlayerName = "Player";
            IsMicMute = false;
            Difficulty = 0;
            MasterVolume = 1f;
            FxVolume = 0.75f;
            MusicVolume = 0.75f;
            InvertLook = false;
            Brightness = 1f;
            SelectedKit = 0;
            SelectedCharacterStartDataIndex = 0;
        }

        public static void ResetStats()
        {
            KillCount = 0;
            DeathCount = 0;
        }
    }
}
