using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using StarterAssets;

public class EmoteWheel : NetworkBehaviour
{
    [Header("Ui")]
    public GameObject EmoteWheelUi;
    RectTransform rectTransform;
    private static bool isEmoteWheelActive = false;
    public bool inEmoteWheel = false;
    bool hasClosedEmoteWheel = true;
    public GameObject lineUiElement;
    [Header("Emotes")]
    public List<EmoteWheelItem> emotes;
    public TMPro.TMP_Text currentEmoteName;
    public TMPro.TMP_Text currentEmoteInfo;
    public bool isPlayingAnimation = false;
    [Header("Areas")]
    [HideInInspector]
    public bool Top;
    [HideInInspector]
    public bool Down;
    [HideInInspector]
    public bool Right;
    [HideInInspector]
    public bool Left;

    private StarterAssets.StarterAssetsInputs _input;

    private void Start()
    {
        foreach(EmoteWheelItem emoteItem in emotes)
        {
            emoteItem.Load();
        }
    }

    private bool CheckActions()
    {
        return //!PlayerInventoryModule.inMenu &&  !PlayerInventoryModule.inWeaponWheel &&
               !GetComponent<PlayerInventoryModule>().inShop &&
               !GetComponent<PlayerInventoryModule>().chatWindow.isChatOpen;
    }
    private void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (_input == null)
            _input = GameObject.FindGameObjectWithTag("InputManager").GetComponent<StarterAssets.StarterAssetsInputs>();

        if (_input != null && _input.emoteWheel && !BSystemUI.Instance.Active &&  !GetComponent<PlayerInventoryModule>().inShop && GetComponent<Health>().isDeath == false 
            && !GetComponent<PlayerInventoryModule>().inCar && !GetComponent<PlayerInventoryModule>().usesParachute 
            && !GetComponent<PlayerInventoryModule>().isAiming && 
            !GetComponent<PlayerInventoryModule>().chatWindow.isChatOpen) //!PlayerInventoryModule.inMenu && !PlayerInventoryModule.inWeaponWheel && 
        {
            if (!isEmoteWheelActive)
            {
                inEmoteWheel = !inEmoteWheel;
                if (inEmoteWheel)
                {
                    isEmoteWheelActive = true;
                    if (BSystem.BSystem.inMenu)
                    {
                        BSystem.BSystem.inMenu = false;
                        BSystemUI.Instance.SetActive(false);

                    }
                    EmoteWheelUi.SetActive(true);
                    TPController.TPCameraController.LockCursor(false);
                }
                else
                {
                    EmoteWheelUi.SetActive(false);
                    TPController.TPCameraController.LockCursor(true);
                    isEmoteWheelActive = false;
                }
            }
            if (isEmoteWheelActive)
            {
                hasClosedEmoteWheel = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (Top)
                {
                    currentEmoteName.text = emotes[0].EmoteName;
                    currentEmoteInfo.text = emotes[0].InfoText;
                    foreach(EmoteWheelItem emoteItem in emotes)
                    {
                        if(emoteItem.EmoteName != emotes[0].EmoteName)
                            emoteItem.Deselect();
                    }
                    emotes[0].Select();
                }
                else if (Down)
                {
                    currentEmoteName.text = emotes[1].EmoteName;
                    currentEmoteInfo.text = emotes[1].InfoText;
                    foreach (EmoteWheelItem emoteItem in emotes)
                    {
                        if (emoteItem.EmoteName != emotes[1].EmoteName)
                            emoteItem.Deselect();
                    }
                    emotes[1].Select();
                }
                else if (Right)
                {
                    currentEmoteName.text = emotes[2].EmoteName;
                    currentEmoteInfo.text = emotes[2].InfoText;
                    foreach (EmoteWheelItem emoteItem in emotes)
                    {
                        if (emoteItem.EmoteName != emotes[2].EmoteName)
                            emoteItem.Deselect();
                    }
                    emotes[2].Select();
                }
                else if (Left)
                {
                    currentEmoteName.text = emotes[3].EmoteName;
                    currentEmoteInfo.text = emotes[3].InfoText;
                    foreach (EmoteWheelItem emoteItem in emotes)
                    {
                        if (emoteItem.EmoteName != emotes[3].EmoteName)
                            emoteItem.Deselect();
                    }
                    emotes[3].Select();
                }
            }
        }
        if (!_input.emoteWheel)
        {
            EmoteWheelUi.SetActive(false);
            if (CheckActions())
            {
                TPController.TPCameraController.LockCursor(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            isEmoteWheelActive = false;
            inEmoteWheel = false;
            if (hasClosedEmoteWheel == false)
            {
                hasClosedEmoteWheel = true;
                if (Top)
                {
                    isPlayingAnimation = false;
                    StopCoroutine(EndEmote(0, ""));
                    GetComponent<Animator>().ResetTrigger(emotes[0].EmoteAnimationTriggerName);
                    PlayEmote(emotes[0].EmoteAnimationTriggerName, emotes[0].EmoteAnimationLength, emotes[0].isOnlyUpperBodyAnimation);
                }
                else if (Down)
                {
                    isPlayingAnimation = false;
                    StopCoroutine(EndEmote(0, ""));
                    GetComponent<Animator>().ResetTrigger(emotes[1].EmoteAnimationTriggerName);
                    PlayEmote(emotes[1].EmoteAnimationTriggerName, emotes[1].EmoteAnimationLength, emotes[1].isOnlyUpperBodyAnimation);
                }
                else if (Right)
                {
                    isPlayingAnimation = false;
                    StopCoroutine(EndEmote(0, ""));
                    GetComponent<Animator>().ResetTrigger(emotes[2].EmoteAnimationTriggerName);
                    PlayEmote(emotes[2].EmoteAnimationTriggerName, emotes[2].EmoteAnimationLength, emotes[2].isOnlyUpperBodyAnimation);
                }
                else if (Left)
                {
                    isPlayingAnimation = false;
                    StopCoroutine(EndEmote(0, ""));
                    GetComponent<Animator>().ResetTrigger(emotes[3].EmoteAnimationTriggerName);
                    PlayEmote(emotes[3].EmoteAnimationTriggerName, emotes[3].EmoteAnimationLength, emotes[3].isOnlyUpperBodyAnimation);
                }
                Left = false;
                Right = false;
                Down = false;
                Top = false;
            }
        }
    }

    /// <summary>
    /// Plays an emote based on what is pushed through from the input within the UI of the emote wheel, these are currently
    /// parsed via the update method
    /// </summary>
    /// <param name="_animationTriggerName"></param>
    /// <param name="_animationLength"></param>
    /// <param name="_isOnlyUpperBodyAnimation"></param>
    public void PlayEmote(string _animationTriggerName, float _animationLength, bool _isOnlyUpperBodyAnimation)
    {
        isPlayingAnimation = true;
        if(!_isOnlyUpperBodyAnimation)
            BlockPlayer(true, false);
        else
            GetComponent<Animator>().SetLayerWeight(2, 1);
        if (hasAuthority)
            CmdPlayEmote(_animationTriggerName, _isOnlyUpperBodyAnimation);

        StartCoroutine(EndEmote(_animationLength, _animationTriggerName));
    }

    /// <summary>
    /// Push to the server via RPC which has the exact same structure but the client cannot directly communicate
    /// to the server without a remote event
    /// </summary>
    /// <param name="_animationTriggerName"></param>
    /// <param name="_isOnlyUpperBodyAnimation"></param>
    [Command]
    void CmdPlayEmote(string _animationTriggerName, bool _isOnlyUpperBodyAnimation)
    {
        RpcPlayEmote(_animationTriggerName, _isOnlyUpperBodyAnimation);
    }

    /// <summary>
    /// Remote event to display the animation on the server as well as the client
    /// </summary>
    /// <param name="_animationTriggerName"></param>
    /// <param name="_isOnlyUpperBodyAnimation"></param>
    [ClientRpc]
    void RpcPlayEmote(string _animationTriggerName, bool _isOnlyUpperBodyAnimation)
    {
        if(_isOnlyUpperBodyAnimation)
            GetComponent<Animator>().SetLayerWeight(2, 1);
        GetComponent<Animator>().SetTrigger(_animationTriggerName);
    }

    /// <summary>
    /// Coroutine to end the animation after the time of the animation length has passed
    /// </summary>
    /// <param name="_animationLength"></param>
    /// <param name="_animationTriggerName"></param>
    /// <returns></returns>
    IEnumerator EndEmote(float _animationLength, string _animationTriggerName)
    {
        yield return new WaitForSeconds(_animationLength);

        GetComponent<Animator>().SetLayerWeight(2, 0);
        isPlayingAnimation = false;
        GetComponent<Animator>().ResetTrigger(_animationTriggerName);
        BlockPlayer(false, false);
        StopCoroutine(EndEmote(0, ""));
    }

    /// <summary>
    /// Cancel an animation, is not called in this class, however it seem to be called but an attempt to add a blend between how animations interact
    /// </summary>
    public void CancelAnimation()
    {
        isPlayingAnimation = false;
        GetComponent<Animator>().Play("Idle Walk Run Blend");
        BlockPlayer(false, false);
    }

    /// <summary>
    /// Again more expensive calls to block the player, which should be referenced and cashed on start
    /// </summary>
    /// <param name="block"></param>
    /// <param name="blockCamera"></param>
    void BlockPlayer(bool block, bool blockCamera = true)
    {
        GetComponent<ThirdPersonController>().BlockPlayer(hasAuthority ? block : false, blockCamera);
        GetComponent<CharacterController>().enabled = !block;
    }
}

[System.Serializable]
public class EmoteWheelItem
{
    public string EmoteName;
    [Space]
    public string EmoteAnimationTriggerName;
    public float EmoteAnimationLength = 1;
    [Space]
    [TextArea]
    public string InfoText;
    [Space]
    public bool isOnlyUpperBodyAnimation = false;
    [Space]
    [Header("Ui")]
    public Sprite EmoteIcon;
    public UnityEngine.UI.Image EmoteButtonImage;
    public UnityEngine.UI.Image EmoteButtonIconImage;
    public Color ButtonDeselectedColor;
    public Color ButtonSelectedColor;

    /// <summary>
    /// Loads the animation icon to the wheel
    /// </summary>
    public void Load()
    {
        EmoteButtonIconImage.sprite = EmoteIcon;
    }

    /// <summary>
    /// Sets the colour once deselected back to the core colour of the Icon
    /// </summary>
    public void Deselect()
    {
        EmoteButtonImage.color = ButtonDeselectedColor;
    }

    /// <summary>
    /// Highlights the UI button for visual feedback it is selected
    /// </summary>
    public void Select()
    {
        EmoteButtonImage.color = ButtonSelectedColor;
    }
}
