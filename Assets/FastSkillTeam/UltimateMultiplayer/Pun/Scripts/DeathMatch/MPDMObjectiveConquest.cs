/////////////////////////////////////////////////////////////////////////////////
//
//  MPDMObjectiveConquest.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	An example of how to extend MPDMObjectiveBase.cs for
//	                Conquest/Zone Control style gameplay.
//
/////////////////////////////////////////////////////////////////////////////////

using Photon.Pun;
using Photon.Realtime;

namespace FastSkillTeam.UltimateMultiplayer.Pun
{
#if PHOTON_UNITY_NETWORKING
    using Photon.Pun;
    using Photon.Realtime;
#endif
#if ANTICHEAT
    using CodeStage.AntiCheat.ObscuredTypes;
#endif
    using UnityEngine;
    using FastSkillTeam.UltimateMultiplayer.Shared;
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;

    public class MPDMObjectiveConquest : MPDMObjectiveBase
    {
        [Tooltip("The position the flag will be at when it is captured.")]
        [SerializeField] protected Transform m_TargetRaised;
        [Tooltip("The position the flag will be at when it is neutral.")]
        [SerializeField] protected Transform m_TargetFallen;
#if !ANTICHEAT
        [Tooltip("The interval for bonus scoring while a team owns the flag.")]
        [SerializeField] protected float m_ScoreUpdateInterval = 3f;
        [Tooltip("The speed at which the flag will rise or fall, multiplied by the amount of players capturing.")]
        [SerializeField] protected float m_SpeedPerPlayer = 1f;
        [Tooltip("If true, objective renderer color will be set to the defending teams color as per MPTeamManager.\nIf false the renderer will be blue for defenders and red for everyone else (potential attackers).\nBest left at default (false) for clarity.")]
        [SerializeField] protected bool m_UseDefendingTeamColor = false;
#else
        [Tooltip("The interval for bonus scoring while a team owns the flag.")]
        [SerializeField] protected ObscuredFloat m_ScoreUpdateInterval = 3f;
        [Tooltip("The speed at which the flag will rise or fall, multiplied by the amount of players capturing.")]
        [SerializeField] protected ObscuredFloat m_SpeedPerPlayer = 1f;
        [Tooltip("If true, objective renderer color will be set to the defending teams color as per MPTeamManager.\nIf false the renderer will be blue for defenders and red for everyone else (potential attackers).\nBest left at default (false) for clarity.")]
        [SerializeField] protected ObscuredBool m_UseDefendingTeamColor = false;
#endif

        //work variables
        private enum State { Captured, Neutral,/* Capturing,*/ Falling }
        private State m_LastFlagState = State.Neutral;
        private bool m_HasBeenCapturedOnce = false;
        private Color m_TeamColor = Color.white;
        private float m_NextCapturedScoreAddTime;
        private float m_NextPlayerScoreAddTime;

        // Values that will be synced over network
        private Vector3 m_LatestFlagPos;
        private State m_FlagState = State.Neutral;

        // Lag compensation
        private Vector3 m_FlagPositionAtLastPacket = Vector3.zero;

        public override void Awake()
        {
            if (MPMaster.Instance is MPDMMaster)
            {
                if ((MPMaster.Instance as MPDMMaster).CurrentGameType != GameType.Conquest)
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

            base.Awake();

            m_FlagPositionAtLastPacket = m_LatestFlagPos = m_ObjectiveGameObject.transform.position;
        }

        public override void Update()
        {
            base.Update();   
            UpdateFlag();
        }

        private void UpdateFlag()
        {
            if (m_Active == false)
                return;

            if (Gameplay.IsMaster == false)
            {
                // Lag compensation
                double timeToReachGoal = m_CurrentPacketTime - m_LastPacketTime;
                m_CurrentTime += Time.deltaTime;

                float interp = (float)(m_CurrentTime / timeToReachGoal);

                // Update flag position
                m_ObjectiveGameObject.transform.position = Vector3.Lerp(m_FlagPositionAtLastPacket, m_LatestFlagPos, interp);
                return;
            }

            int[] teamStrength = new int[MPTeamManager.TeamCount];
            int best = 0;
            int strongestTeam = m_DefendingTeamNumber;
            bool tie = false;
            int amount = 0;

            if (m_Players.Count > 0)
            {
                //Players are within the zone, find out which team has the strongest presence of players.
                for (int teamNumber = 0; teamNumber < teamStrength.Length; teamNumber++)
                {
                    for (int i = 0; i < m_Players.Count; i++)
                    {
                        if (m_Players[i].TeamNumber == teamNumber)
                        {
                            teamStrength[teamNumber]++;
                        }
                    }

                    if (teamStrength[teamNumber] == best)
                    {
                        tie = true;
                    }
                    else if (teamStrength[teamNumber] > best)
                    {
                        tie = false;
                        amount = teamStrength[teamNumber] - best;
                        best = teamStrength[teamNumber];
                        strongestTeam = teamNumber;
                    }

                }
            }

            Vector3 pos = m_ObjectiveGameObject.transform.position;
            if (tie)
            {
                m_ObjectiveGameObject.transform.position = pos;
                if (m_FlagState == State.Neutral)
                {
                    m_DefendingTeamNumber = -1;
                    SetColor(-2);
                    //m_TeamColor = Color.grey;
                }
            }
            else
            {
                float tgt = m_ObjectiveGameObject.transform.position.y;

                if (m_FlagState == State.Neutral)
                {
                    m_DefendingTeamNumber = -1;
                    SetColor(-1);
                    //m_TeamColor = Color.white;

                    if (m_Players.Count > 0)
                    {
                        if (Time.time > m_NextPlayerScoreAddTime)
                        {
                            for (int i = 0; i < m_Players.Count; i++)
                            {
                                if (m_Players[i].TeamNumber == strongestTeam)
                                {
                                    UpdatePlayerScore(m_Players[i], m_CapturingScoreAmount);
                                }
                            }

                            m_NextPlayerScoreAddTime = Time.time + m_ScoreUpdateInterval;
                        }

                        tgt = Mathf.MoveTowards(m_ObjectiveGameObject.transform.position.y, m_TargetRaised.position.y, amount * m_SpeedPerPlayer * Time.deltaTime);
                        if (tgt >= m_TargetRaised.position.y)
                        {
                            tgt = m_TargetRaised.position.y;
                            m_DefendingTeamNumber = strongestTeam;
                            SetColor(m_DefendingTeamNumber);
                            m_FlagState = State.Captured;


                            for (int i = 0; i < m_Players.Count; i++)
                            {
                                if (m_Players[i].TeamNumber == strongestTeam)
                                {
                                    UpdatePlayerScore(m_Players[i], m_CaptureScoreAmount);
                                }
                            }
                        }
                    }
                    else
                    {
                        tgt = Mathf.MoveTowards(m_ObjectiveGameObject.transform.position.y, m_TargetFallen.position.y, m_SpeedPerPlayer * Time.deltaTime);
                        if (tgt <= m_TargetFallen.position.y)
                            tgt = m_TargetFallen.position.y;
                    }
                }


/*                if (m_FlagState == State.Capturing)
                {
                    tgt = Mathf.MoveTowards(m_ObjectiveGameObject.transform.position.y, m_TargetRaised.position.y, amount * m_SpeedPerPlayer * Time.deltaTime);

                    if (tgt >= m_TargetRaised.position.y)
                    {
                        tgt = m_TargetRaised.position.y;
                        if (m_Players.Count > 0)
                            m_DefendingTeamNumber = strongestTeam;
                        m_FlagState = State.Captured;
                    }
                }*/

                if (m_FlagState == State.Falling)
                {
                    if (m_Players.Count > 0)
                    { 
                        //Players are capturing an "owned" flag, sort out which team has the strongest count of players in the zone.
                        if (m_DefendingTeamNumber != strongestTeam)
                        {
                            //drop the flag if the "owner" team is not the strongest(they may spawn back and make a comeback!)
                            tgt = Mathf.MoveTowards(m_ObjectiveGameObject.transform.position.y, m_TargetFallen.position.y, amount * m_SpeedPerPlayer * Time.deltaTime);

                            if (tgt <= m_TargetFallen.position.y)
                            {
                                //The strongest team neutralised the flag.
                                tgt = m_TargetFallen.position.y;
                                m_FlagState = State.Neutral;
                            }
                        }
                        else
                        {
                            //The defending team is making a comeback or all the strongest team players are dead before the flag was neutralised. Raise the flag.
                            tgt = Mathf.MoveTowards(m_ObjectiveGameObject.transform.position.y, m_TargetRaised.position.y, amount * m_SpeedPerPlayer * Time.deltaTime);

                            if (tgt >= m_TargetRaised.position.y)
                            {
                                m_FlagState = State.Captured;
                                tgt = m_TargetRaised.position.y;
                            }
                        }

                        //Award bonus score for those that are on the strongest team, at intervals defined in the inspector.
                        if (Time.time > m_NextPlayerScoreAddTime)
                        {
                            for (int i = 0; i < m_Players.Count; i++)
                            {
                                if (m_Players[i].TeamNumber == strongestTeam)
                                {
                                    UpdatePlayerScore(m_Players[i], m_CapturingScoreAmount);
                                }
                            }

                            m_NextPlayerScoreAddTime = Time.time + m_ScoreUpdateInterval;
                        }
                    }
                    else //All players within the zone are dead!
                    {
                        //First check if this has ever even been captured, an early bout may have occured without any prevailing team!
                        if (m_HasBeenCapturedOnce)
                        {
                            //Start to raise the flag back to the owned position.
                            tgt = Mathf.MoveTowards(m_ObjectiveGameObject.transform.position.y, m_TargetRaised.position.y, m_SpeedPerPlayer * Time.deltaTime);

                            if (tgt >= m_TargetRaised.position.y)
                            {
                                m_FlagState = State.Captured;
                                tgt = m_TargetRaised.position.y;
                            }
                        }
                        else
                        {
                            //Not controlled, reset to neutral state. 
                            m_FlagState = State.Neutral;
                        }
                    }
                }

                if (m_FlagState == State.Captured)
                {
                    m_HasBeenCapturedOnce = true;
                    SetColor(m_DefendingTeamNumber);
                    tgt = m_TargetRaised.position.y;
                    if (m_Players.Count > 0)
                    {
                        if (m_DefendingTeamNumber != strongestTeam)
                            m_FlagState = State.Falling;
                    }
                    if (Time.time > m_NextCapturedScoreAddTime)
                    {
                        m_NextCapturedScoreAddTime = Time.time + m_ScoreUpdateInterval;
                        UpdateTeamScore(m_DefendingTeamNumber);
                    }
                }

                if (m_LastFlagState != m_FlagState)
                {
                    m_LastFlagState = m_FlagState;
                    Debug.Log(m_FlagState);
                }

                pos.y = tgt;
                m_ObjectiveGameObject.transform.position = pos;

                //just for handovers, TODO: use the INTERNALLY relayed OnMasterSwitched
                m_LatestFlagPos = pos;
            }
        }
        protected override void OnPlayerEnteredRoom(Player player, GameObject character)
        {
           base.OnPlayerEnteredRoom(player, character);
           photonView.RPC("UpdateParamsRPC", player, m_DefendingTeamNumber);
        }
        private bool m_CanRecieve = false;
        private int m_LastTeamColorID = -1;
        [PunRPC]
        private void UpdateParamsRPC(int teamNumber)
        {
            m_CanRecieve = true;
            m_DefendingTeamNumber = teamNumber;
            SetColor(teamNumber);
        }
        private void SetColor(int teamNumber)
        {
            if (teamNumber == m_LastTeamColorID)
                return;

            m_LastTeamColorID = teamNumber;

            if (teamNumber == -2)
                m_TeamColor = Color.grey;
            else
                m_TeamColor = m_UseDefendingTeamColor ? MPTeamManager.GetTeamColor(teamNumber) : teamNumber == -1 ? Color.white : (MPLocalPlayer.Instance.TeamNumber == teamNumber ? Color.blue : Color.red);

            base.SetColor(m_TeamColor);
        }


        private void UpdatePlayerScore(MPPlayer p, int score)
        {
            if (m_Active == false)
                return;
#if PHOTON_UNITY_NETWORKING
            photonView.RPC("UpdatePlayerScoreRPC", RpcTarget.All, p.ID, score);
#endif
        }

        private void UpdateTeamScore(int teamNumber)
        {
            if (m_Active == false)
                return;
#if PHOTON_UNITY_NETWORKING
            photonView.RPC("UpdateTeamScoreRPC", RpcTarget.All, teamNumber);
#endif
        }
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        private void UpdatePlayerScoreRPC(int playerActorNumber, int score)
        {
            MPPlayer player = MPPlayer.Get(playerActorNumber);
            if (player)
            {
                player.Stats.Set("Score", (int)player.Stats.Get("Score") + score);
                MPTeamManager.Instance.RefreshTeams();
            }
        }
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        private void UpdateTeamScoreRPC(int team)
        {
            (MPTeamManager.Instance as MPDMTeamManager).AddExtraScore(MPTeamManager.Instance.Teams[team] as MPDMTeam, m_TeamScoreAmount);
            MPTeamManager.Instance.RefreshTeams();
        }

#if PHOTON_UNITY_NETWORKING
        public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            base.OnPhotonSerializeView(stream, info);

            if (stream.IsWriting)
            {
                stream.SendNext(m_LatestFlagPos);
                stream.SendNext(m_DefendingTeamNumber);
                stream.SendNext(m_LastTeamColorID);
                stream.SendNext(m_FlagState);
              //  stream.SendNext(m_TeamColor);

            }
            else 
            {
                // Network player, receive data
                m_LatestFlagPos = (Vector3)stream.ReceiveNext();

                // Lag compensation
                m_FlagPositionAtLastPacket = m_ObjectiveGameObject.transform.position;

                m_DefendingTeamNumber = (int)stream.ReceiveNext();
                int teamColorID = (int)stream.ReceiveNext();
                m_FlagState = (State)stream.ReceiveNext();
             //   m_TeamColor = (Color)stream.ReceiveNext();

               /* if (m_FlagState == State.Neutral)
                {
                    m_TeamColor = Color.white;
                }*/

                if (m_FlagState == State.Captured)
                {
                    m_HasBeenCapturedOnce = true;
                   
                }
                //  m_TeamColor = m_UseDefendingTeamColor ? MPTeamManager.GetTeamColor(m_DefendingTeamNumber) : m_DefendingTeamNumber == -1 ? Color.white : (MPLocalPlayer.Instance ? (MPLocalPlayer.Instance.TeamNumber == m_DefendingTeamNumber ? Color.blue : Color.red) : Color.white);
                //   base.SetColor(m_TeamColor);
                if (m_CanRecieve)
                    SetColor(teamColorID);
            }
        }
#endif

        public override void FullReset()
        {
            base.FullReset();

            m_NextCapturedScoreAddTime = m_NextPlayerScoreAddTime = 0;
            m_LastFlagState = m_FlagState = State.Neutral;
            m_TeamColor = Color.white;
            m_DefendingTeamNumber = -1;
            m_FlagPositionAtLastPacket = m_LatestFlagPos = m_ObjectiveGameObject.transform.position = m_TargetFallen.position;
            m_HasBeenCapturedOnce = false;
        }
    }
}