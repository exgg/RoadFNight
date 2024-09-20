/////////////////////////////////////////////////////////////////////////////////
//
//  MPKillFeed.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	Displays kill data generally seen in multiplayer games. 
//                  Used in conjunction with MPKillInfoContainer.cs
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun.UI
{
    using Opsive.Shared.Events;
    using Opsive.Shared.Game;
    using Photon.Pun;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class MPKillFeed : MonoBehaviour
    {
        [System.Serializable]
        public class WeaponIconData
        {
            [Tooltip("The name of the weapon.")]
            [SerializeField] protected string m_WeaponName;
            [Tooltip("The icon to display.")]
            [SerializeField] protected Sprite m_Icon;
            [Tooltip("The scale of the icon.")]
            [SerializeField] protected Vector3 m_Scale = new Vector3(1f,1f,1f);
            [Tooltip("The rotation of the icon.")]
            [SerializeField] protected Vector3 m_Rotation = new Vector3(0f, 0f, 0f);
            public string WeaponName => m_WeaponName;
            public Sprite Icon => m_Icon;
            public Vector3 Scale => m_Scale;
            public Vector3 Rotation => m_Rotation;
        }

        [Tooltip("The UI ScrollRect that will controlled.")]
        [SerializeField] private ScrollRect m_MpKillFeedScrollRect;
        [Tooltip("The UI gameobject that will be parent of MpKillInfoPrefabs that are spawned.")]
        [SerializeField] private GameObject m_MpKillInfoContent;
        [Tooltip("The prefab with the MPKillInfoContainer component attached.")]
        [SerializeField] private GameObject m_MpKillInfoPrefab;
        [Tooltip("The duration the kill feed will display the kill data.")]
        [SerializeField] private float m_KillInfoLifeTime = 2f;
        [Tooltip("The data used for the kill feed.")]
        [SerializeField] private List<WeaponIconData> m_WeaponIconData = new List<WeaponIconData>();
        private PhotonView m_PhotonView;

        private void Start()
        {
            m_PhotonView = GetComponent<PhotonView>();
            if (!m_MpKillFeedScrollRect)
                m_MpKillFeedScrollRect = GetComponent<ScrollRect>();
            EventHandler.RegisterEvent<string, string, string>("ReportMPKill", ReportKill);
        }

        private void OnDestroy()
        {
            EventHandler.UnregisterEvent<string, string, string>("ReportMPKill", ReportKill);
        }

        private void ReportKill(string attacker, string attackerWeapon, string target)
        {
            //Debug.Log("Got Kill info with attacker weapon name : " + attackerWeapon);
            for (int i = 0; i < m_WeaponIconData.Count; i++)
            {
                if (attackerWeapon.Contains(m_WeaponIconData[i].WeaponName))
                {
                    GameObject g = ObjectPool.Instantiate(m_MpKillInfoPrefab, m_MpKillInfoContent.transform);
                    if (m_KillInfoLifeTime > 0)
                        Scheduler.Schedule(m_KillInfoLifeTime, delegate { ObjectPool.Destroy(g); });
                    g.transform.SetAsFirstSibling();
                    g.GetCachedComponent<MPKillInfoContainer>().SetKillData(attacker, m_WeaponIconData[i], target);

                    m_MpKillFeedScrollRect.verticalNormalizedPosition = 1f;
                    break;
                }
            }
            string[] data = new string[] { attacker, attackerWeapon, target };
            m_PhotonView.RPC("ReportKillRPC", RpcTarget.Others, attacker, attackerWeapon, target);
        }

        [PunRPC]
        public void ReportKillRPC(string attacker, string attackerWeapon, string target)
        {
            //Debug.Log("Got Kill info with attacker weapon name : " + attackerWeapon);
            for (int i = 0; i < m_WeaponIconData.Count; i++)
            {
                if (attackerWeapon.Contains(m_WeaponIconData[i].WeaponName))
                {
                    GameObject g = ObjectPool.Instantiate(m_MpKillInfoPrefab, m_MpKillInfoContent.transform);
                    if (m_KillInfoLifeTime > 0)
                        Scheduler.Schedule(m_KillInfoLifeTime, delegate { ObjectPool.Destroy(g); });
                    g.transform.SetAsFirstSibling();
                    g.GetCachedComponent<MPKillInfoContainer>().SetKillData(attacker, m_WeaponIconData[i], target);
                    m_MpKillFeedScrollRect.verticalNormalizedPosition = 1f;
                    break;
                }
            }
        }
    }
}