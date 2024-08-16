using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChatSystem : MonoBehaviour
{
    public InputField chatMessage;
    public Text chatHistory;
    public Scrollbar scrollbar;
    [HideInInspector]
    public bool isChatOpen = false;

    public void Awake()
    {
        NetPlayer.OnMessage += OnPlayerMessage;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            if(isChatOpen == false)
            {
                isChatOpen = true;
                GetComponent<Animator>().Play("ChatIn");
                TPController.TPCameraController.LockCursor(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                chatMessage.ActivateInputField();
            }
            else
            {
                isChatOpen = false;
                GetComponent<Animator>().Play("ChatOut");
                TPController.TPCameraController.LockCursor(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                chatMessage.DeactivateInputField();
            }
        }
        if (isChatOpen == false)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if(chatMessage.text != "")
                {
                    OnSend();
                }
            }
        }
    }

    void OnPlayerMessage(NetPlayer netPlayer, string message)
    {
        string prettyMessage = netPlayer.isLocalPlayer ?
            $"<color=blue>{netPlayer.username}: </color> {message}" :
            $"<color=blue>{netPlayer.username}: </color> {message}";
        AppendMessage(prettyMessage);
    }

    public void OnSend()
    {
        if (chatMessage.text.Trim() == "")
            return;

        // get our player
        NetPlayer netPlayer = NetworkClient.connection.identity.GetComponent<NetPlayer>();

        // send a message
        netPlayer.CmdSend(chatMessage.text.Trim());

        chatMessage.text = "";
    }

    internal void AppendMessage(string message)
    {
        StartCoroutine(AppendAndScroll(message));
    }

    IEnumerator AppendAndScroll(string message)
    {
        chatHistory.text += message + "\n";

        yield return null;
        yield return null;

        // slam the scrollbar down
        scrollbar.value = 0;
    }
}
