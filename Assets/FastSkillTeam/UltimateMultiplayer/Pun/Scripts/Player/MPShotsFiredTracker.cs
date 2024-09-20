/////////////////////////////////////////////////////////////////////////////////
//
//  MPShotsFiredTracker.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	A simple way to track shots fired for multiplayer stats.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using Opsive.UltimateCharacterController.Items.Actions;
    using Opsive.UltimateCharacterController.Items.Actions.Modules.Shootable;
    using System;
    using UnityEngine;

    [Serializable]
    public class MPShotsFiredTracker : ShootableFireEffectModule
    {
        private MPPlayer m_NetworkPlayer;
        public override void Initialize(GameObject gameObject)
        {
            base.Initialize(gameObject);
            m_NetworkPlayer = gameObject.GetComponentInParent<MPPlayer>();
        }

        public override void InvokeEffects(ShootableUseDataStream dataStream)
        {
            if (m_NetworkPlayer)
                m_NetworkPlayer.Stats.Set("Shots", (int)m_NetworkPlayer.Stats.Get("Shots") + 1);
        }
    }

    [Serializable]
    public class MPStatsBridge : ShootableFireEffectModule
    {
        [SerializeField] private string m_Stat = "Shots";
        [SerializeField] private int m_Value = 1;
        private MPPlayer m_NetworkPlayer;
        public override void Initialize(GameObject gameObject)
        {
            base.Initialize(gameObject);
            m_NetworkPlayer = gameObject.GetComponentInParent<MPPlayer>();
        }

        public override void InvokeEffects(ShootableUseDataStream dataStream)
        {
            if (m_NetworkPlayer == null)
                return;
            if (string.IsNullOrEmpty(m_Stat))
                return;

            m_NetworkPlayer.Stats.Set(m_Stat, (int)m_NetworkPlayer.Stats.Get(m_Stat) + m_Value);
        }
    }

    /*    

        [Serializable]
        public class MPStatsStringBridge : ShootableFireEffectModule
        {
            [SerializeField] private string m_Stat = "";
            [SerializeField] private string m_Value = "";
            private MPPlayer m_NetworkPlayer;
            public override void Initialize(GameObject gameObject)
            {
                base.Initialize(gameObject);
                m_NetworkPlayer = gameObject.GetComponentInParent<MPPlayer>();
            }

            public override void InvokeEffects(ShootableUseDataStream dataStream)
            {
                if (m_NetworkPlayer == null)
                    return;
                if (string.IsNullOrEmpty(m_Stat))
                    return;

                m_NetworkPlayer.Stats.Set(m_Stat, m_Value);
            }
        }

        [Serializable]
        public class MPStatsFloatBridge : ShootableFireEffectModule
        {
            [SerializeField] private string m_Stat = "";
            [SerializeField] private float m_Value = 1f;
            private MPPlayer m_NetworkPlayer;
            public override void Initialize(GameObject gameObject)
            {
                base.Initialize(gameObject);
                m_NetworkPlayer = gameObject.GetComponentInParent<MPPlayer>();
            }

            public override void InvokeEffects(ShootableUseDataStream dataStream)
            {
                if (m_NetworkPlayer == null)
                    return;
                if (string.IsNullOrEmpty(m_Stat))
                    return;

                m_NetworkPlayer.Stats.Set(m_Stat, (float)m_NetworkPlayer.Stats.Get(m_Stat) + m_Value);
            }
        }*/
}
