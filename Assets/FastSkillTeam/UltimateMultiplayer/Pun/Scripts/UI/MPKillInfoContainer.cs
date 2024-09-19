/////////////////////////////////////////////////////////////////////////////////
//
//  MPKillInfoContainer.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	A simple script to relay data to this prefab at runtime.
//                  Must be attached to a prefab that is spawed by MPKillFeed.cs
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun.UI
{
    using UnityEngine;

    public class MPKillInfoContainer : MonoBehaviour
    {
        [SerializeField] private Opsive.Shared.UI.Text m_AttackerText;
        [SerializeField] private UnityEngine.UI.Image m_AttackerWeaponImage;
        [SerializeField] private Opsive.Shared.UI.Text m_TargetText;

        public void SetKillData(string attackerName, MPKillFeed.WeaponIconData weaponIconData, string targetName)
        {
            if (m_AttackerText.TextMeshProText != null || m_AttackerText.UnityText != null)
                m_AttackerText.text = attackerName;
            if (m_AttackerWeaponImage != null)
                m_AttackerWeaponImage.sprite = weaponIconData.Icon;

            m_AttackerWeaponImage.rectTransform.eulerAngles = weaponIconData.Rotation;
            m_AttackerWeaponImage.rectTransform.localScale = weaponIconData.Scale;

            if (m_TargetText.TextMeshProText != null || m_TargetText.UnityText != null)
                m_TargetText.text = targetName;
        }
    }
}
