/////////////////////////////////////////////////////////////////////////////////
//
//  MPMenu.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	The main multiplayer menu example, the core connection depends 
//                  on this for character, map and gamemode selection.
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun.UI
{
    using Photon.Pun;
    using Photon.Realtime;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Opsive.UltimateCharacterController.Character;
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;

    /// <summary>
    /// The main multiplayer menu example, the core connection depends on this for character, map and gamemode selection.
    /// </summary>
    public class MPMenu : MonoBehaviour
    {
        [System.Serializable]
        public class SceneData
        {
            [Tooltip("Display name (for UI purpose) of the scene that will be loaded.")]
            [SerializeField] protected string m_DisplayName;
            [Tooltip("Actual name of the scene that will be loaded.")]
            [SerializeField] protected string m_SceneName;
            [Tooltip("Display image (for UI purpose) of the scene that will be loaded.")]
            [SerializeField] protected Texture m_SceneThumbnail;
            [SerializeField] protected bool m_Active = true;
            /// <summary>
            /// Display name (for UI purpose) of the scene that will be loaded.
            /// </summary>
            public string DisplayName => m_DisplayName;
            /// <summary>
            /// Actual name of the scene that will be loaded.
            /// </summary>
            public string SceneName => m_SceneName;
            /// <summary>
            /// Display image (for UI purpose) of the scene that will be loaded.
            /// </summary>
            public Texture SceneThumbnail => m_SceneThumbnail;

            public bool Active { get => m_Active; set => m_Active = value; }
        }

        [System.Serializable]
        public class CharacterStartData
        {
            [Tooltip("Display name (for UI purpose) of the character model that will be selected.")]
            [SerializeField] protected string m_DisplayName;
            [Tooltip("Display image (for UI purpose) of the character model that will be selected.")]
            [SerializeField] protected Texture m_Thumbnail;
            [Tooltip("The index of the model that will be selected in the Model Manager component of the character.")]
            [SerializeField] protected int m_ModelIndex;
            [Tooltip("WIP Under Construction!. Use at your own risk, Prone to changes using rules instead. Defines what items this model carries. False will spawn all items the character prefab has been set up with.")]
            [SerializeField] protected bool m_UseStartingLoadout = false;
            [Tooltip("WIP Under Construction!. Use at your own risk, Prone to changes using rules instead. Defines what items this model carries.")]
            [SerializeField] protected Opsive.UltimateCharacterController.Inventory.ItemIdentifierAmount[] m_StartingLoadout;
            [SerializeField] protected bool m_Active = true;

            public bool Active { get => m_Active; set => m_Active = value; }
            public bool UseStartingLoadout => m_UseStartingLoadout;
            /// <summary>
            /// WIP Under Construction!. Use at your own risk, Prone to changes using rules instead. Defines what items this model carries.
            /// </summary>
            public Opsive.UltimateCharacterController.Inventory.ItemIdentifierAmount[] StartingLoadout => m_StartingLoadout;
            /// <summary>
            /// Display name (for UI purpose) of the character model that will be selected.
            /// </summary>
            public string DisplayName => m_DisplayName;
            /// <summary>
            /// Display image (for UI purpose) of the character model that will be selected.
            /// </summary>
            public Texture Thumbnail => m_Thumbnail;
            /// <summary>
            /// The index of the model that will be selected in the Model Manager component of the character.
            /// </summary>
            public int ModelIndex => m_ModelIndex;
        }

#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
        [Tooltip("Specifies the perspective that the character should start in if there is no perspective selection GameObject.")]
        [SerializeField] protected bool m_DefaultFirstPersonStart = true;
#endif
        [Tooltip("A reference to the first or third person perspective toggle.")]
        [SerializeField] protected Toggle m_PerspectiveToggle;

        [SerializeField] protected bool m_AllowTextChat = true;
        [SerializeField] protected bool m_ClearGameChat = false;
        [SerializeField] protected bool m_ClearGlobalChat = false;
        [SerializeField] protected List<CharacterStartData> m_CharacterStartData = new List<CharacterStartData>();
        [SerializeField] protected List<SceneData> m_SceneData = new List<SceneData>();
        [SerializeField] protected Button m_ConnectButton;
        [SerializeField] protected Button m_NextGameModeButton;
        [SerializeField] protected Button m_PreviousGameModeButton;
        [SerializeField] protected Button m_NextMapButton;
        [SerializeField] protected Button m_PreviousMapButton;
        [SerializeField] protected Button m_NextCharacterButton;
        [SerializeField] protected Button m_PreviousCharacterButton;
        [SerializeField] protected Button m_FullscreenButton;
        [SerializeField] protected Button m_QualityButton;
        [SerializeField] protected Button m_BackToMenuButton;
        [SerializeField] protected Button m_ResumeButton;
        [SerializeField] protected Button m_QuitGameButton;
        [SerializeField] protected GameObject m_PausePanel;
        [SerializeField] protected GameObject m_MainPanel;
        [SerializeField] protected Button m_RoomListButton;
        [SerializeField] protected Button m_CloseRoomListButton;
        [SerializeField] protected GameObject m_RoomListPanel;
        [SerializeField] protected bool m_RequireSetName = true;
        [SerializeField] protected Opsive.Shared.UI.Text m_StatusText;
        [SerializeField] protected Opsive.Shared.UI.Text m_FullScreenButtonText;
        [SerializeField] protected Opsive.Shared.UI.Text m_QualityButtonText;
        [SerializeField] protected Opsive.Shared.UI.Text m_MapNameLabel;
        [SerializeField] protected Opsive.Shared.UI.Text m_GameModeLabel;
        [SerializeField] protected Opsive.Shared.UI.Text m_MutedMicText;
        [SerializeField] protected Opsive.Shared.UI.Text m_CharacterNameLabel;
        [SerializeField] protected RawImage m_SceneThumbnail;
        [SerializeField] protected RawImage m_CharacterThumbnail;

#if TEXTMESH_PRO_PRESENT
        [SerializeField] protected TMPro.TMP_InputField m_NameInputField;
#else
        [SerializeField] protected InputField m_NameInputField;
#endif

        [SerializeField] protected bool m_SendMapSelectionEvent = false;
        [SerializeField] protected bool m_SendModeSelectionEvent = false;
        [SerializeField] protected bool m_SendCharacterSelectionEvent = false;
        [SerializeField] protected bool m_SendPlayerNameEvent = false;

        private int m_SelectedCharacterStartDataIndex = 0;
        private bool m_IsChatOpen = false;
        protected Color m_OriginalNameColor = Color.white;
        protected string m_DefaultStatusText = "Press Connect to join...";
        protected string m_Status = "Enter Name...";
        protected string m_DefaultPlayerName = "Player";
        protected string[] m_Qualitys = new string[] { "Fastest", "Fast", "Good", "Beautiful", "Fantastic" };
        protected int m_Quality = 0;
        private Transform m_GameModesParent;
        public Transform GameModesParent { get { return m_GameModesParent; } }

        //gamemode switch vars
        private int m_SelectedGameModeIndex = 0;
        private string m_SelectedGameModeName = "";// auto set by gamemode GameObject.name when modeIndex is adjusted or set
        private int m_GameModeCount = 0;// used to track the amount of available modes

        //map switch map index
        private int m_SelectedSceneIndex = 0;

        //voice chat
        private bool m_IsMicMute = false;

        private MPGameChat m_TextChat = null;
        private MPGameChat TextChat { get { if (!m_TextChat) m_TextChat = FindObjectOfType<MPGameChat>(); return m_TextChat; } }

        private static MPMenu m_Instance = null;
       // public static MPMenu Instance => m_Instance;

        private string m_MPMenuName;

        private bool m_CanOpenMainPanel = true;
        private bool m_IsPaused = false;
        //  public enum MenuState { MainMenu, InGame, Paused}

        public string MPMenuName => m_MPMenuName;
        public int SelectedModelIndex => m_CharacterStartData[m_SelectedCharacterStartDataIndex].ModelIndex;

        protected virtual void Awake()
        {
            if (m_Instance)
            {
                DestroyImmediate(gameObject);
                return;
            }
            else
            {
                m_Instance = this;
            }

            if (m_RoomListPanel != null)
                m_RoomListPanel.SetActive(false);

            m_MPMenuName = SceneManager.GetActiveScene().name;
            m_IsMicMute = Utility.SettingsManager.IsMicMute;

            m_MutedMicText.enabled = m_IsMicMute;

            if (m_PausePanel && m_PausePanel.activeInHierarchy)
                m_PausePanel.SetActive(false);

            AddListeners();
            EventHandler.RegisterEvent<MPMaster>("InitGameModes", OnInitGameModes);
            EventHandler.RegisterEvent("OnMatchEnd", ExitToMenu);
            EventHandler.RegisterEvent("OnJoinedRoom", ShowProgress);
            m_OriginalNameColor = m_NameInputField.textComponent.color;

            m_FullScreenButtonText.text = (Screen.fullScreen ? "Fullscreen" : "Windowed");


#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
            m_DefaultFirstPersonStart = PlayerPrefs.GetInt("START_PERSPECTIVE", m_DefaultFirstPersonStart ? 1 : 0) == 1;
            if (m_PerspectiveToggle != null)
                m_PerspectiveToggle.isOn = m_DefaultFirstPersonStart;
#endif
        }

        protected virtual void OnDestroy()
        {
            if (m_Instance == this)
                m_Instance = null;

            RemoveListeners();

            EventHandler.UnregisterEvent<MPMaster>("InitGameModes", OnInitGameModes);
            EventHandler.UnregisterEvent("OnMatchEnd", ExitToMenu);
            EventHandler.UnregisterEvent("OnJoinedRoom", ShowProgress);
        }

        protected virtual void Start()
        {

            m_StatusText.text = m_Status;

            string playerName = Utility.SettingsManager.PlayerName;

            if (m_RequireSetName && playerName == m_DefaultPlayerName)
            {
                m_NameInputField.textComponent.color = Color.red;
                Scheduler.Schedule(1, delegate ()
                {
                    m_NameInputField.textComponent.color = m_OriginalNameColor;
                });
                Scheduler.Schedule(2, delegate ()
                {
                    m_NameInputField.textComponent.color = Color.red;
                });
                Scheduler.Schedule(3, delegate ()
                {
                    m_NameInputField.textComponent.color = m_OriginalNameColor;
                });
                Scheduler.Schedule(4, delegate ()
                {
                    m_NameInputField.textComponent.color = Color.red;
                });
                Scheduler.Schedule(5, delegate ()
                {
                    m_NameInputField.textComponent.color = m_OriginalNameColor;
                });
            }
            else m_Status = m_DefaultStatusText;

            m_NameInputField.text = playerName;
            m_StatusText.text = m_Status;

            if (TextChat)
                TextChat.enabled = false;

            //  Application.targetFrameRate = SettingsManager.TargetFrameRate;//no ui for this yet, defualt is 60

            m_Quality = Utility.SettingsManager.QualityLevel;

            m_SelectedCharacterStartDataIndex = Utility.SettingsManager.SelectedCharacterStartDataIndex;
            SelectCharacter();

            QualitySettings.SetQualityLevel(m_Quality, false);

            m_QualityButtonText.text = "Quality: " + m_Qualitys[QualitySettings.GetQualityLevel()];

            if (m_SendPlayerNameEvent)
                EventHandler.ExecuteEvent<string>("OnSetPlayerName", playerName);
            MPConnection.Instance.LocalPlayerName = playerName;
        }

        protected virtual void OnSelectNameInput(string s)
        {
            EventHandler.ExecuteEvent("MenuEditing", true);
        }

        protected virtual void OnEnable()
        {
          //  EventHandler.RegisterEvent<bool>("OnPauseGame", ShowPausePanel); //TODO: Make standalone input handler for ui 

            EventHandler.RegisterEvent<bool>("OnShowChatWindow", OnShowChat);
            EventHandler.RegisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
            EventHandler.RegisterEvent("DisableMultiplayerGUI", delegate () { gameObject.SetActive(false); }); // called from 'MPSinglePlayerTest'
            EventHandler.RegisterEvent("Disconnected", delegate () { ResetInternal(); });			// called from 'MPConnection' when player disconnects
        }

        protected virtual void OnDisable()
        {
            // EventHandler.UnregisterEvent<bool>("OnPauseGame", ShowPausePanel);//TODO: Make standalone input handler for ui 

            EventHandler.UnregisterEvent<bool>("OnShowChatWindow", OnShowChat);
            EventHandler.UnregisterEvent<Player, GameObject>("OnPlayerEnteredRoom", OnPlayerEnteredRoom);
            EventHandler.UnregisterEvent("DisableMultiplayerGUI", delegate () { gameObject.SetActive(false); });
            EventHandler.UnregisterEvent("Disconnected",delegate() { ResetInternal(); });
        }

        protected void OnPlayerEnteredRoom(Player player, GameObject character)
        {
            if (m_CharacterStartData[m_SelectedCharacterStartDataIndex].UseStartingLoadout == true)//WIP
            {
                if (player.IsLocal)
                    SchedulerBase.Schedule(1f, () => SelectCharacter());
            }

#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
            if (player.IsLocal)
            {
             
                SelectStartingPerspective(m_DefaultFirstPersonStart, false);
            }
#endif
        }
#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
        /// <summary>
        /// Sets the starting perspective on the character.
        /// </summary>
        /// <param name="firstPersonPerspective">Should the character start in a first person perspective?</param>
        /// <param name="teleport">Should the character be teleported to the demo zone?</param>
        protected void SelectStartingPerspective(bool firstPersonPerspective, bool teleport)
        {
            // Set the perspective on the camera.
            var foundCamera = Opsive.Shared.Camera.CameraUtility.FindCamera(null);
            var cameraController = foundCamera.GetComponent<Opsive.UltimateCharacterController.Camera.CameraController>();
            // Ensure the camera starts with the correct view type.
            cameraController.FirstPersonViewTypeFullName = GetViewTypeFullName(true);
            cameraController.ThirdPersonViewTypeFullName = GetViewTypeFullName(false);
            cameraController.SetPerspective(firstPersonPerspective, true);
           // cameraController.Character = m_Character;
        }

        /// <summary>
        /// Returns the full name of the view type for the specified perspective.
        /// </summary>
        /// <param name="firstPersonPerspective">Should the first person perspective be returned?</param>
        /// <returns>The full name of the view type for the specified perspective.</returns>
        protected virtual string GetViewTypeFullName(bool firstPersonPerspective)
        {
            return firstPersonPerspective ? "Opsive.UltimateCharacterController.FirstPersonController.Camera.ViewTypes.Combat" :
                                            "Opsive.UltimateCharacterController.ThirdPersonController.Camera.ViewTypes.Combat";
        }

        /// <summary>
        /// The perspective toggle has changed.
        /// </summary>
        /// <param name="value">The new value of the perspective toggle.</param>
        public void PerspectiveChanged(bool value)
        {
            m_DefaultFirstPersonStart = value;
            PlayerPrefs.SetInt("START_PERSPECTIVE", m_DefaultFirstPersonStart == true ? 1 : 0);
        }
#endif

        protected virtual void AddListeners()
        {
            if (m_CloseRoomListButton)
                m_CloseRoomListButton.onClick.AddListener(() => OnClickShowRoomList(false));
            if (m_RoomListButton)
                m_RoomListButton.onClick.AddListener(() => OnClickShowRoomList(true));
            if (m_BackToMenuButton)
                m_BackToMenuButton.onClick.AddListener(() => OnClickExitToMenu());
            if (m_ResumeButton)
                m_ResumeButton.onClick.AddListener(() => OnClickResumePlayButton());
            if (m_QuitGameButton)
                m_QuitGameButton.onClick.AddListener(() => Gameplay.Quit());
            if (m_FullscreenButton)
                m_FullscreenButton.onClick.AddListener(() => OnClickFullScreen());
            if (m_ConnectButton)
                m_ConnectButton.onClick.AddListener(() => OnClickFindMatch());
            if (m_NextMapButton)
                m_NextMapButton.onClick.AddListener(() => OnClickChangeMap(true));
            if (m_PreviousMapButton)
                m_PreviousMapButton.onClick.AddListener(() => OnClickChangeMap(false));
            if (m_NextGameModeButton)
                m_NextGameModeButton.onClick.AddListener(() => OnClickGameMode(true));
            if (m_NextGameModeButton)
                m_PreviousGameModeButton.onClick.AddListener(() => OnClickGameMode(false));
            if (m_QualityButton)
                m_QualityButton.onClick.AddListener(() => OnClickQuality());
            if (m_NameInputField)
            {
#if TEXTMESH_PRO_PRESENT
                m_NameInputField.onSelect.AddListener((string s) => OnSelectNameInput(s));
#endif
                m_NameInputField.onEndEdit.AddListener((string s) => SetPlayerName(s));
            }
            if (m_NextCharacterButton)
                m_NextCharacterButton.onClick.AddListener(() => OnClickChangeCharacter(true));
            if (m_PreviousCharacterButton)
                m_PreviousCharacterButton.onClick.AddListener(() => OnClickChangeCharacter(false));
#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
            if (m_PerspectiveToggle)
                m_PerspectiveToggle.onValueChanged.AddListener((bool b) => PerspectiveChanged(b));
#endif
        }

        protected virtual void RemoveListeners()
        {
            if (m_CloseRoomListButton)
                m_CloseRoomListButton.onClick.RemoveListener(() => OnClickShowRoomList(false));
            if (m_RoomListButton)
                m_RoomListButton.onClick.RemoveListener(() => OnClickShowRoomList(true));
            if (m_BackToMenuButton)
                m_BackToMenuButton.onClick.RemoveListener(() => OnClickExitToMenu());
            if (m_ResumeButton)
                m_ResumeButton.onClick.RemoveListener(() => OnClickResumePlayButton());
            if (m_QuitGameButton)
                m_QuitGameButton.onClick.RemoveListener(() => Gameplay.Quit());
            if (m_FullscreenButton)
                m_FullscreenButton.onClick.RemoveListener(() => OnClickFullScreen());
            if (m_ConnectButton)
                m_ConnectButton.onClick.RemoveListener(() => OnClickFindMatch());
            if (m_NextMapButton)
                m_NextMapButton.onClick.RemoveListener(() => OnClickChangeMap(true));
            if (m_PreviousMapButton)
                m_PreviousMapButton.onClick.RemoveListener(() => OnClickChangeMap(false));
            if (m_NextGameModeButton)
                m_NextGameModeButton.onClick.RemoveListener(() => OnClickGameMode(true));
            if (m_NextGameModeButton)
                m_PreviousGameModeButton.onClick.RemoveListener(() => OnClickGameMode(false));
            if (m_QualityButton)
                m_QualityButton.onClick.RemoveListener(() => OnClickQuality());
            if (m_NameInputField)
            {
#if TEXTMESH_PRO_PRESENT
                m_NameInputField.onSelect.RemoveListener((string s) => OnSelectNameInput(s));
#endif
                m_NameInputField.onEndEdit.RemoveListener((string s) => SetPlayerName(s));
            }
            if (m_NextCharacterButton)
                m_NextCharacterButton.onClick.RemoveListener(() => OnClickChangeCharacter(true));
            if (m_PreviousCharacterButton)
                m_PreviousCharacterButton.onClick.RemoveListener(() => OnClickChangeCharacter(false));

#if FIRST_PERSON_CONTROLLER && THIRD_PERSON_CONTROLLER
            if (m_PerspectiveToggle)
                m_PerspectiveToggle.onValueChanged.RemoveListener((bool b) => PerspectiveChanged(b));
#endif

        }

        private void OnClickShowRoomList(bool show)
        {
            if (m_RoomListPanel)
                m_RoomListPanel.SetActive(show);
        }

        private void OnShowChat(bool show)
        {
            //Debug.Log("OnShowChat: " + show);
            m_IsChatOpen = show;
        }

        /// <summary>
        /// This event is fired when the menu scene is loaded.
        /// </summary>
        /// <param name="master"></param>
        private void OnInitGameModes(MPMaster master)
        {
            //    Debug.Log("Init Gamemodes");
            m_GameModesParent = master.transform.parent;
            m_GameModeCount = m_GameModesParent.childCount;

            m_SelectedGameModeIndex = Utility.SettingsManager.SelectedGameModeIndex;
            if(m_SelectedGameModeIndex > m_GameModeCount - 1)
            {
                m_SelectedGameModeIndex = m_GameModeCount - 1;
                Utility.SettingsManager.SelectedGameModeIndex = m_SelectedGameModeIndex;
            }

            m_SelectedSceneIndex = Utility.SettingsManager.SelectedSceneIndex;
            if (m_SelectedSceneIndex > m_SceneData.Count - 1)
            {
                m_SelectedSceneIndex = m_SceneData.Count - 1;
                Utility.SettingsManager.SelectedSceneIndex = m_SelectedSceneIndex;
            }

            SelectMap();
            SelectGameMode();
        }

        private void Update()
        {
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
            if (m_IsChatOpen == false && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                if (Gameplay.CurrentLevelName != m_MPMenuName)
                    ShowPausePanel(!m_IsPaused);
#else
            if (Input.GetKeyDown(KeyCode.Escape) && m_IsChatOpen == false)
                if (Gameplay.CurrentLevelName != m_MPMenuName)
                    ShowPausePanel(!m_IsPaused);
#endif

            Paused = m_IsPaused;
            CursorLockMode = Cursor.lockState;
            CursorVisible = Cursor.visible;
        }
        //for debugging values easily...
        public CursorLockMode CursorLockMode;
        public bool Paused;
        public bool CursorVisible;
/*        private bool PausedInternal
        {
            set
            {
                // Don't pause if the game is over.
                if (MPLocalPlayer.Instance == null || m_IsPaused == value)
                {
                    return;
                }
                m_IsPaused = value;
                EventHandler.ExecuteEvent("OnPauseGame", m_IsPaused);
                EventHandler.ExecuteEvent<bool>(MPLocalPlayer.Instance.GameObject, "OnShowUI", !m_IsPaused);
                EventHandler.ExecuteEvent(MPLocalPlayer.Instance.GameObject, "OnEnableGameplayInput", !m_IsPaused);
                Cursor.visible = m_IsPaused;
                Cursor.lockState = m_IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
            }
        }*/
        public void OnClickResumePlayButton()
        {
                ShowPausePanel(false);
         //   EventHandler.ExecuteEvent<bool>(MPLocalPlayer.Instance.GameObject, "OnEnableGameplayInput", true);
          //  Utility.Utility.LockCursor = true;
        }

        public void OnClickGameMode(bool forward)
        {
            int selected = m_SelectedGameModeIndex;
            if (forward)
            {
                selected++;
                if (selected > m_GameModeCount - 1)
                    selected = 0;
            }
            else
            {
                selected--;
                if (selected < 0)
                    selected = m_GameModeCount - 1;
            }

            m_SelectedGameModeIndex = selected;

            Utility.SettingsManager.SelectedGameModeIndex = m_SelectedGameModeIndex;

            SelectGameMode();
        }

        private void SelectGameMode()
        {
            for (int i = 0; i < m_GameModeCount; i++)
            {
                if (i == m_SelectedGameModeIndex)
                {
                    GameObject g = m_GameModesParent.GetChild(i).gameObject;
                    m_SelectedGameModeName = g.name;
                   
                    m_GameModeLabel.text = m_SelectedGameModeName;
                    g.SetActive(true);
                    continue;
                }
                m_GameModesParent.GetChild(i).gameObject.SetActive(false);
            }

            if (m_SendModeSelectionEvent)
                EventHandler.ExecuteEvent<string>("OnSelectGameMode", m_SelectedGameModeName);
            MPConnection.Instance.GameModeDisplayName = m_SelectedGameModeName;
        }

        protected virtual void OnClickChangeMap(bool forward)
        {
            int selected = m_SelectedSceneIndex;
            if (forward)
            {
                selected++;
                if (selected > m_SceneData.Count - 1)
                    selected = 0;
            }
            else
            {
                selected--;
                if (selected < 0)
                    selected = m_SceneData.Count - 1;
            }

            if (m_SceneData[selected].Active == false)
            {
                int count = m_SceneData.Count;
                while (count > 0 && m_SceneData[selected].Active == false)
                {
                    count--;
                    if (forward)
                    {
                        selected++;
                        if (selected > m_SceneData.Count - 1)
                            selected = 0;
                    }
                    else
                    {
                        selected--;
                        if (selected < 0)
                            selected = m_SceneData.Count - 1;
                    }
                }

                if (count == 0)
                {
                    Debug.LogWarning("No Active Scene Data, please ensure there is at least one Scene Data with an Active status.");
                    selected = 0;
                }
            }


            m_SelectedSceneIndex = selected;

            Utility.SettingsManager.SelectedSceneIndex = m_SelectedSceneIndex;

            SelectMap();
        }

        protected virtual void OnClickChangeCharacter(bool forward)
        {
            int selected = m_SelectedCharacterStartDataIndex;
            if (forward)
            {
                selected++;
                if (selected > m_CharacterStartData.Count - 1)
                    selected = 0;
            }
            else
            {
                selected--;
                if (selected < 0)
                    selected = m_CharacterStartData.Count - 1;
            }

            if (m_CharacterStartData[selected].Active == false)
            {
                int count = m_CharacterStartData.Count;
                while (count > 0 && m_CharacterStartData[selected].Active == false)
                {
                    count--;
                    if (forward)
                    {
                        selected++;
                        if (selected > m_CharacterStartData.Count - 1)
                            selected = 0;
                    }
                    else
                    {
                        selected--;
                        if (selected < 0)
                            selected = m_CharacterStartData.Count - 1;
                    }
                }

                if (count == 0)
                {
                    Debug.LogWarning("No Active Character Data, please ensure there is at least one Character Start Data with an Active status.");
                    selected = 0;
                }
            }

            m_SelectedCharacterStartDataIndex = selected;
            Utility.SettingsManager.SelectedCharacterStartDataIndex = m_SelectedCharacterStartDataIndex;

            SelectCharacter();
        }

        private void SelectCharacter()
        {
            CharacterStartData data = m_CharacterStartData[m_SelectedCharacterStartDataIndex];
            string displayName = data.DisplayName;

            if (m_CharacterNameLabel.gameObject != null)
                m_CharacterNameLabel.text = displayName;

            if (m_CharacterThumbnail != null && data.Thumbnail != null)
                m_CharacterThumbnail.texture = data.Thumbnail;

            if (m_SendCharacterSelectionEvent)
                EventHandler.ExecuteEvent<int>("OnSelectCharacter", data.ModelIndex);
            MPConnection.Instance.SelectedModelIndex = data.ModelIndex;

            if (SceneManager.GetActiveScene().name != m_MPMenuName)
            {
                ModelManager m = MPLocalPlayer.Instance.GetComponentInChildren<ModelManager>();
                if (m)
                    m.ChangeModels(m.AvailableModels[data.ModelIndex]);
                
                if (data.UseStartingLoadout == true)//WIP. TODO: Register respawns and reset kits...
                {
                    var loadout = data.StartingLoadout;
                    if (loadout != null && loadout.Length > 0)
                    {
                        var manager = MPLocalPlayer.Instance.GetComponent<Opsive.UltimateCharacterController.Inventory.ItemSetManager>();
                        manager.UnEquipAllItems(true, true);
                        manager.CharacterInventory.RemoveAllItems(false);
                        var ids = manager.CharacterInventory.GetAllItemIdentifiers();
                        for (int i = 0; i < ids.Count; i++)
                        {
                            manager.CharacterInventory.RemoveItemIdentifierAmount(ids[i], manager.CharacterInventory.GetItemIdentifierAmount(ids[i]));
                        }
                        for (int i = 0; i < loadout.Length; i++)
                        {
                            manager.CharacterInventory.AddItemIdentifierAmount(loadout[i].ItemIdentifier, loadout[i].Amount);
                        }
                        manager.UpdateItemSets();
                        AnimatorMonitor[] ms = MPLocalPlayer.Instance.GetComponentsInChildren<AnimatorMonitor>(true);
                        for (int i = 0; i < ms.Length; i++)
                        {
                            ms[i].UpdateItemAbilityAnimatorParameters(true, true);
                            ms[i].UpdateAbilityAnimatorParameters(true);
                        }
                        manager.EquipItem(loadout[0].ItemIdentifier, -1, false, false);
                    }
                }
            }

        }

        private void SelectMap()
        {
            string displayName = m_SceneData[m_SelectedSceneIndex].DisplayName;
            m_MapNameLabel.text = displayName;
            m_SceneThumbnail.texture = m_SceneData[m_SelectedSceneIndex].SceneThumbnail;

            string sceneToLoad = m_SceneData[m_SelectedSceneIndex].SceneName;
            //  double roundStartDelay = _SceneInfo[scene].RoundStartDelay;
            if (m_SendMapSelectionEvent)
                EventHandler.ExecuteEvent<string, string>("OnSelectMap", displayName, sceneToLoad);
            MPConnection.Instance.SceneToLoadName = sceneToLoad;
            MPConnection.Instance.MapDisplayName = displayName;
            for (int i = 0; i < m_GameModeCount; i++)
            {
                MPMaster master = m_GameModesParent.GetChild(i).GetComponent<MPMaster>();
                master.CurrentLevel = sceneToLoad;
                //  master.RoundStartDelay = roundStartDelay;
            }
        }

        protected virtual void OnClickFindMatch()
        {
            //   Debug.Log("OnClickFindMatch()");

            string pName = m_NameInputField.text;
            if (string.IsNullOrEmpty(pName))
                pName = m_DefaultPlayerName;

            if (m_RequireSetName == true && pName == m_DefaultPlayerName)
            {
                m_NameInputField.textComponent.color = Color.red;
                Scheduler.Schedule(1, delegate ()
                {
                    m_NameInputField.textComponent.color = m_OriginalNameColor;
                });
                return;
            }

            if (FindObjectOfType<MPConnection>() == null)
            {
                m_Status = "No MP scripts ...";
                Scheduler.Schedule(1, delegate ()
                {
                    m_Status = m_DefaultStatusText;
                });
                return;
            }

            m_Status = "Connecting ...";
            EnableInteractables(false);
            MPConnection.Instance.TryJoinRoom();
            if (m_AllowTextChat && TextChat)
                TextChat.enabled = true;
            while (pName.Length < 3)
            {
                pName = "#" + pName;
            }
            PhotonNetwork.LocalPlayer.NickName = pName;
            Utility.SettingsManager.PlayerName = pName;
            m_StatusText.text = m_Status;
        }


        protected virtual void EnableInteractables(bool enable)
        {
            m_ConnectButton.interactable = enable;
            m_NameInputField.interactable = enable;
            if (m_RoomListButton)
                m_RoomListButton.interactable = enable;
            if (m_FullscreenButton)
                m_FullscreenButton.interactable = enable;
            if (m_NextMapButton)
                m_NextMapButton.interactable = enable;
            if (m_PreviousMapButton)
                m_PreviousMapButton.interactable = enable;
            if (m_NextGameModeButton)
                m_NextGameModeButton.interactable = enable;
            if (m_PreviousGameModeButton)
                m_PreviousGameModeButton.interactable = enable;
            if (m_NextCharacterButton)
                m_NextCharacterButton.interactable = enable;
            if (m_PreviousCharacterButton)
                m_PreviousCharacterButton.interactable = enable;
            if (m_QualityButton)
                m_QualityButton.interactable = enable;
            if (m_PerspectiveToggle)
                m_PerspectiveToggle.interactable = enable;
        }

        public virtual void ShowProgress()
        {
            if (MPConnection.Instance.LoadSceneOnJoinRoom)
                StartCoroutine(ProgressDisplay());
        }
       private IEnumerator ProgressDisplay()
        {
            while (PhotonNetwork.LevelLoadingProgress < 0.99f)
            {
                m_StatusText.text = "Loading ...   " + (PhotonNetwork.LevelLoadingProgress * 100).ToString() + "%";
                yield return 0;
            }
            DisableMainPanel();
            yield break;
        }

        protected virtual void DisableMainPanel()
        {
         //   Debug.Log("Disable Main Panel");
            m_CanOpenMainPanel = false;
            m_MainPanel.SetActive(false);
        }

        public void OnClickQuality()
        {
            m_QualityButtonText.text = "Please wait ...";
            m_Quality++;
            if (m_Quality > 4)
                m_Quality = 0;
            Scheduler.Schedule(0.1f, delegate ()
            {
                Utility.SettingsManager.QualityLevel = m_Quality;
                QualitySettings.SetQualityLevel(m_Quality, false);
                m_QualityButtonText.text = "Quality: " + m_Qualitys[QualitySettings.GetQualityLevel()];
            });
        }
        protected virtual void OnClickFullScreen()
        {
            ToggleFullscreen();
            m_FullScreenButtonText.text = (Screen.fullScreen ? "Fullscreen" : "Windowed");
        }

        //<<---------------------------------------------------VOICE SETTINGS-------------------------------------------->>\\
        public void OnClickMuteMic()
        {
            // SettingManager.mutedMic
            m_IsMicMute = !m_IsMicMute;

            m_MutedMicText.enabled = m_IsMicMute;
            Utility.SettingsManager.IsMicMute = m_IsMicMute;

          //  GetComponent<MPVoiceSettings>().ToggleVoiceSetting(MPVoiceSettings.VoiceSettingToChange.Mute, m_IsMicMute);
        }

        public void OnClickMuteVoiceAudio()
        {
            //TODO
        }

        public void OnClickCalibrateMic()
        {
          //  GetComponent<MPVoiceSettings>().CalibrateButtonOnClick();
        }
        //end voice settings

        public void OnClickExitGame()
        {
            Application.Quit();
        }

        public void OnClickExitToMenu()
        {
            ExitToMenu();
        }

        public void ExitToMenu()
        {
            m_CanOpenMainPanel = true;
            m_IsPaused = true;//reset flag to show menu
            ShowPausePanel(false);//show menu

            MPConnection.Instance.LeaveRoomAndGoToMenu(MPLocalPlayer.Instance.gameObject);
            Cleanup();
            ResetInternal();
        }

        protected virtual void Cleanup()
        {
            if (TextChat)
            {
                //TODO: should not be required here!
                TextChat.enabled = false;
            }

            Debug.Log("--- Cleanup() --- ::::: --- Destroying GameModesParent ---");

            Destroy(m_GameModesParent.gameObject);
        }
        public void OnClickSettingsButton()
        {
            ShowPausePanel(true);
        }

        public virtual void ShowPausePanel(bool show)
        {
            if (m_IsPaused == show)
            {
                return;
            }

            m_IsPaused = show;

            // Debug.Log("Show Pause Panel: " + m_IsPaused);
            if (m_BackToMenuButton)
                m_BackToMenuButton.gameObject.SetActive(SceneManagerHelper.ActiveSceneName != m_MPMenuName);
            if (m_PausePanel)
                m_PausePanel.SetActive(m_IsPaused);
            m_MainPanel.SetActive(!m_IsPaused && m_CanOpenMainPanel);

            if (MPLocalPlayer.Instance != null)
                EventHandler.ExecuteEvent<bool>(MPLocalPlayer.Instance.GameObject, "OnEnableGameplayInput", !show);

            Cursor.visible = m_IsPaused;
            Cursor.lockState = m_IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void SetPlayerName(string playerName)
        {
            string s = playerName;

            s = s.Replace("\n", "");
            s = s.Replace(".", "");
            s = s.Replace(",", "");
            s = s.Replace("|", "");

            Utility.SettingsManager.PlayerName = s;

            EventHandler.ExecuteEvent("MenuEditing", false);
            if (m_SendPlayerNameEvent)
                EventHandler.ExecuteEvent<string>("OnSetPlayerName", playerName);
            MPConnection.Instance.LocalPlayerName = playerName;
        }

        protected virtual void ToggleFullscreen()
        {
            if (!Screen.fullScreen)
            {
                Resolution k = new Resolution();
                foreach (Resolution r in Screen.resolutions)
                {
                    if (r.width > k.width)
                    {
                        k.width = r.width;
                        k.height = r.height;
                    }
                }
                Screen.SetResolution(k.width, k.height, true);
            }
            else
                Screen.SetResolution(800, 600, false);
        }



        protected virtual void ResetInternal()
        {
            Debug.Log("ResetInternal()");
            m_Status = m_DefaultStatusText;
            if (m_AllowTextChat)
            {
                if (m_ClearGameChat)
                    EventHandler.ExecuteEvent("ClearGameChat");
                if (m_ClearGlobalChat)
                    EventHandler.ExecuteEvent("ClearGlobalChat");
            }
            m_StatusText.text = m_Status;
            EnableInteractables(true);
            Utility.Utility.LockCursor = false;
            if(m_RoomListPanel != null)
                m_RoomListPanel.SetActive(false);
        }
    }
}