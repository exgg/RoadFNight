/////////////////////////////////////////////////////////////////////////////////
//
//  MPRoomInfoDataPun.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	Add to a prefab that will be spawned by MPConnection to display
//                  room information and a button to join the room.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun.UI
{
    using Photon.Realtime;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    public class MPRoomInfoDataPun : MonoBehaviour
    {
        [SerializeField] protected Opsive.Shared.UI.Text m_MapNameText;
        [SerializeField] protected Opsive.Shared.UI.Text m_ModeNameText;
        [SerializeField] protected Opsive.Shared.UI.Text m_PlayerCountText;
        [SerializeField] protected Button m_JoinButton;
        public void Initialize(UnityAction joinButtonAction, string mapName, string modeName, RoomInfo info)
        {
            int playerCount = info.PlayerCount;
            int maxPlayers = info.MaxPlayers;
            m_MapNameText.text = mapName;
            m_ModeNameText.text = modeName;
            m_PlayerCountText.text = playerCount.ToString() + " / " + maxPlayers.ToString();
            if (m_JoinButton)
            {
                m_JoinButton.onClick.RemoveAllListeners();
                m_JoinButton.interactable = playerCount < maxPlayers;
                m_JoinButton.onClick.AddListener(joinButtonAction);
            }
        }
    }
}