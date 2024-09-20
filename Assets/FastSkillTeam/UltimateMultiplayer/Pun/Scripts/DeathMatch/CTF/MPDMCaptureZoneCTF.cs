namespace FastSkillTeam.UltimateMultiplayer.Pun
{
    using FastSkillTeam.UltimateMultiplayer.Shared;
    using UnityEngine;

    public class MPDMCaptureZoneCTF : MonoBehaviour
    {
        [SerializeField] protected int m_TeamId = 0;
        [SerializeField, Range(0,1)] protected float m_Alpha = 0.15f;
        public int TeamId => m_TeamId;

        private void Awake()
        {
            if (MPMaster.Instance is MPDMMaster)
            {
                if ((MPMaster.Instance as MPDMMaster).CurrentGameType != GameType.CTF)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            Color c = MPTeamManager.GetTeamColor(m_TeamId);
            c.a = m_Alpha;
            GetComponent<Renderer>().material.color = c;
        }
    }
}