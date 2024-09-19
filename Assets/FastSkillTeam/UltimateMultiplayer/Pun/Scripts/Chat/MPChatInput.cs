namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using Opsive.Shared.Game;
    using Opsive.Shared.UI;
    using Opsive.Shared.Events;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TextAlignment = Opsive.Shared.UI.TextAlignment;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

    public class MPChatInput : MonoBehaviour
    {
        [SerializeField] int m_MaxGlobalMessages = 50;
        [SerializeField] int m_MaxGameMessages = 20;
        [SerializeField] private bool m_HideUntilConnected = true;
        [SerializeField] private bool m_CanOpenWithEnterKey = true;
        private static MPChatInput Instance = null;
        [SerializeField] private Button m_OpenChatButton;
        [SerializeField] private Button m_CloseChatButton;
        [SerializeField] private GameObject m_MainChatPanel;
        [SerializeField] private Transform m_GlobalChatContent;
        [SerializeField] private Transform m_GameChatContent;
        [SerializeField] private GameObject m_TextPrefab;

        [Tooltip("NOTE: Cannot be this GameObject. If NULL will fetch child (0)")]
        [SerializeField] GameObject m_ChatRoot;

        [SerializeField] private int m_FontSize = 16;
        [SerializeField] private InputField m_InputField = null;

        private readonly List<Message> m_MessageListGlobal = new List<Message>();
        private readonly List<Message> m_MessageListGame = new List<Message>();
        private GameObject m_GameObject = null;
        public GameObject GameObject => m_GameObject;

        private bool m_Visible = true;

        public enum MessageType { Player, Remote, Debug }
        [SerializeField] protected Color m_PlayerMessageColour = Color.black;
        [SerializeField] protected Color m_RemoteMessageColour = Color.blue;
        [SerializeField] protected Color m_DebugMessageColour = Color.red;

        public class Message
        {
            public string text;
            public TextComponent textComponent;
        }

        private void Update()
        {
            if (!m_CanOpenWithEnterKey)
                return;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
            if (Keyboard.current.enterKey.wasPressedThisFrame)
                ShowChatWindow(true);
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                ShowChatWindow(false);
#else
            if (Input.GetKeyDown(KeyCode.Return))
                ShowChatWindow(true);
            else if (Input.GetKeyDown(KeyCode.Escape))
                ShowChatWindow(false);
#endif
        }


        private void Awake()
        {
            if (Instance)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            m_GameObject = gameObject;

            if (m_ChatRoot == null)
                m_ChatRoot = transform.GetChild(0).gameObject;

            if (m_InputField)
                m_InputField.onEndEdit.AddListener((string s) => Send(s));

            if (m_OpenChatButton)
                m_OpenChatButton.onClick.AddListener(() => ShowChatWindow(true));

            if (m_CloseChatButton)
                m_CloseChatButton.onClick.AddListener(() => ShowChatWindow(false));

            EventHandler.RegisterEvent<bool>("MenuEditing", OnMenuEdit);
            EventHandler.RegisterEvent<bool>("ActivateChat", SetActive);
            EventHandler.RegisterEvent("ClearGameChat", OnClearGameChat);
            EventHandler.RegisterEvent("ClearGlobalChat", OnClearGlobalChat);
            EventHandler.RegisterEvent<string, MessageType, bool>("OnGetMessage", AddChatMessage);
            if (m_HideUntilConnected)
                m_ChatRoot.SetActive(false);
        }

        public void SetActive(bool active)
        {
            m_Visible = active; m_ChatRoot.SetActive(m_Visible);
        }

        private void OnMenuEdit(bool editing)
        {
            Debug.Log("Editing in menu = " + editing);
            if (editing)
                m_CanShowChatWindow = false;
            else Scheduler.Schedule(0.1f, () => m_CanShowChatWindow = true);
        }
        private bool m_IsChatWindowOpen = false;
        private bool m_CanShowChatWindow = true;
        public void ShowChatWindow(bool show)
        {
            if (!m_CanShowChatWindow)
                show = false;

            if (m_IsChatWindowOpen == show)
                return;

            Debug.Log("ShowChatWindow " + show);

            m_IsChatWindowOpen = show;

            m_MainChatPanel.SetActive(show);
            m_OpenChatButton.interactable = !show;

            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(show ? m_InputField.gameObject : null);

            EventHandler.ExecuteEvent<bool>("OnShowChatWindow", show);

            //  if (!show)
            //     Utility.Utility.LockCursor = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (m_InputField)
                m_InputField.onEndEdit.RemoveListener((string s) => Send(s));

            if (m_OpenChatButton)
                m_OpenChatButton.onClick.RemoveListener(() => ShowChatWindow(true));

            if (m_CloseChatButton)
                m_CloseChatButton.onClick.RemoveListener(() => ShowChatWindow(false));

            EventHandler.UnregisterEvent<bool>("MenuEditing", OnMenuEdit);
            EventHandler.UnregisterEvent<bool>("ActivateChat", SetActive);
            EventHandler.UnregisterEvent("ClearGameChat", OnClearGameChat);
            EventHandler.UnregisterEvent("ClearGlobalChat", OnClearGlobalChat);
            EventHandler.UnregisterEvent<string, MessageType, bool>("OnGetMessage", AddChatMessage);
        }

        private void Send(string mssg)
        {
            if (string.IsNullOrEmpty(mssg))
                return;

            if (!m_GameChatContent.gameObject.activeInHierarchy)
                MPChatConnection.Instance.Send(Photon.Pun.PhotonNetwork.NickName + ": " + mssg, "Global");
            else MPGameChat.AddMessage(Photon.Pun.PhotonNetwork.NickName + ": " + mssg);
            m_InputField.text = "";

            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(m_InputField.gameObject);
        }

        private Color GetChatMessageColor(MessageType messageType)
        {
            Color c;

            switch (messageType)
            {

                case MessageType.Player:
                    c = m_PlayerMessageColour;
                    break;
                case MessageType.Remote:
                    c = m_RemoteMessageColour;
                    break;
                case MessageType.Debug:
                    c = m_DebugMessageColour;
                    break;
                default:
                    c = Color.black;
                    break;
            }

            return c;
        }

        public void AddChatMessage(string mssg, MessageType messageType, bool global)
        {
            if (global)
            {
                if (m_MessageListGlobal.Count >= m_MaxGlobalMessages)
                {
                    Destroy(m_MessageListGlobal[0].textComponent.gameObject);
                    m_MessageListGlobal.RemoveAt(0);
                }
            }
            else
            {
                if (m_MessageListGame.Count >= m_MaxGameMessages)
                {
                    Destroy(m_MessageListGame[0].textComponent.gameObject);
                    m_MessageListGame.RemoveAt(0);
                }
            }

            GameObject newText = Instantiate(m_TextPrefab, global ? m_GlobalChatContent : m_GameChatContent);

            Message m = new Message { text = mssg, textComponent = newText.GetComponent<TextComponent>() };

            m.textComponent.color = GetChatMessageColor(messageType);

            m.textComponent.text = m.text;
            m.textComponent.fontSize = m_FontSize;

            m.textComponent.alignment = messageType == MessageType.Player ? TextAlignment.TopRight : TextAlignment.TopLeft;

            if (global)
                m_MessageListGlobal.Add(m);
            else m_MessageListGame.Add(m);
        }

        private void OnClearGameChat()
        {
            if (m_MessageListGame.Count > 0)
            {
                for (int i = 0; i < m_MessageListGame.Count; i++)
                    Destroy(m_MessageListGame[i].textComponent.gameObject);
                m_MessageListGame.Clear();
            }
        }

        private void OnClearGlobalChat()
        {
            if (m_MessageListGlobal.Count > 0)
            {
                for (int i = 0; i < m_MessageListGlobal.Count; i++)
                    Destroy(m_MessageListGlobal[i].textComponent.gameObject);
                m_MessageListGlobal.Clear();
            }
        }

        public static void Initialize()
        {
            Instance.Initialized();
        }
        private void Initialized()
        {
            m_ChatRoot.SetActive(true);
        }
    }
}