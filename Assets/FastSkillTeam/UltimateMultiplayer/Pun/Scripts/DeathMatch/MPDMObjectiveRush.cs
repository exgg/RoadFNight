/////////////////////////////////////////////////////////////////////////////////
//
//  MPDMObjectiveRush.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	An example of how to extend MPDMObjectiveBase.cs for
//	                Rush/Arm/Diffuse style gameplay.
//
/////////////////////////////////////////////////////////////////////////////////

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
    using Opsive.UltimateCharacterController.Traits.Damage;
    using Opsive.Shared.Events;
    using Opsive.UltimateCharacterController.Events;
    using FastSkillTeam.Shared.Objects;
    using Opsive.Shared.Game;
    using FastSkillTeam.UltimateMultiplayer.Shared;
    using FastSkillTeam.UltimateMultiplayer.Shared.Game;

    public class MPDMObjectiveRush : MPDMObjectiveBase, IDamageTarget
    {
#if !ANTICHEAT
        [Tooltip("This should be true on any objective that should be active when a round starts. When the round resets, the 'first set' will be activated, the rest are deactivated.")]
        [SerializeField] protected bool m_IsPartOfFirstSet = false;
        [Tooltip("Can this objective be destroyed by damage?")]
        [SerializeField] protected bool m_Invincible = false;
        [Tooltip("The maximum health of this objective, also the starting health.")]
        [SerializeField] protected float m_MaxHealth = 100f;
        [Tooltip("Can the objective be damaged by the defending team?")]
        [SerializeField] protected bool m_AllowFriendlyDamage = true;
        [Tooltip("Disable this gameObject when the objective is destroyed?")]
        [SerializeField] protected bool m_DisableOnDeath = true;
        [Tooltip("If true, objective renderer color will be set to the defending teams color as per MPTeamManager.\nIf false the renderer will be blue for defenders and red for everyone else (potential attackers).\nBest left at default (false) for clarity.")]
        [SerializeField] protected bool m_UseDefendingTeamColor = false;
#else
        [Tooltip("This should be true on any objective that should be active when a round starts. When the round resets, the 'first set' will be activated, the rest are deactivated.")]
        [SerializeField] protected ObscuredBool m_IsPartOfFirstSet = false;
        [Tooltip("Can this objective be destroyed by damage?")]
        [SerializeField] protected ObscuredBool m_Invincible = false;
        [Tooltip("The maximum health of this objective, also the starting health.")]
        [SerializeField] protected ObscuredFloat m_MaxHealth = 100f;
        [Tooltip("Can the objective be damaged by the defending team?")]
        [SerializeField] protected ObscuredBool m_AllowFriendlyDamage = true;
        [Tooltip("Disable this gameObject when the objective is destroyed?")]
        [SerializeField] protected ObscuredBool m_DisableOnDeath = true;
        [Tooltip("If true, objective renderer color will be set to the defending teams color as per MPTeamManager.\nIf false the renderer will be blue for defenders and red for everyone else (potential attackers).\nBest left at default (false) for clarity.")]
        [SerializeField] protected ObscuredBool m_UseDefendingTeamColor = false;
#endif

        [Tooltip("Objectives that will be activated when this objective is destroyed.")]
        [SerializeField] protected MPDMObjectiveRush[] m_NextObjectiveSet;
        [Tooltip("Other objectives that need to be destroyed to progress. If null progression will occur when this objective is destroyed.")]
        [SerializeField] protected MPDMObjectiveRush[] m_DependentObjectives;

        [Tooltip("Any objects that should spawn when this objective is destroyed.")]
        [SerializeField] protected GameObject[] m_SpawnOnDeath = new GameObject[0];
        [Tooltip("Any objects that should be destroyed when this objective is destroyed.")]
        [SerializeField] protected GameObject[] m_DestroyOnDeath = new GameObject[0];

        [Tooltip("Any objects that should be set active when this objective is destroyed.")]
        [SerializeField] protected GameObject[] m_SetActiveOnDeath = new GameObject[0];
        [Tooltip("Any objects that should be set deactivated when this objective is destroyed.")]
        [SerializeField] protected GameObject[] m_DeactivateOnDeath = new GameObject[0];

#if !ANTICHEAT
        [Tooltip("The UI message that should be displayed when the objective can be armed.")]
        [SerializeField] protected string m_ArmMessage = "Arm";
        [Tooltip("The UI message that should be displayed when the objective can be disarmed.")]
        [SerializeField] protected string m_DisarmMessage = "Disarm";
        [Tooltip("How long players must interact until the objective is armed.")]
        [SerializeField] protected float m_ArmDuration = 5f;
        [Tooltip("How long players must interact until the objective is disarmed.")]
        [SerializeField] protected float m_DisarmDuration = 5f;
        [Tooltip("How long the objective can stay armed until is is destroyed.")]
        [SerializeField] protected float m_ArmedDuration = 10f;
        [Tooltip("When the objective is armed, the player that armed it will be awarded this much score.")]
        [SerializeField] protected int m_ArmScoreAmount = 25;
        [Tooltip("When the objective is disarmed, the player that disarmed it will be awarded this much score.")]
        [SerializeField] protected int m_DisarmScoreAmount = 25;
#else
        [Tooltip("The UI message that should be displayed when the objective can be armed.")]
        [SerializeField] protected ObscuredString m_ArmMessage = "Arm";//NOTE: if not concerned about hacking simple ui things like this, then it can just be a string.
        [Tooltip("The UI message that should be displayed when the objective can be disarmed.")]
        [SerializeField] protected ObscuredString m_DisarmMessage = "Disarm";//NOTE: if not concerned about hacking simple ui things like this, then it can just be a string.
        [Tooltip("How long players must interact until the objective is armed.")]
        [SerializeField] protected ObscuredFloat m_ArmDuration = 5f;
        [Tooltip("How long players must interact until the objective is disarmed.")]
        [SerializeField] protected ObscuredFloat m_DisarmDuration = 5f;
        [Tooltip("How long the objective can stay armed until is is destroyed.")]
        [SerializeField] protected ObscuredFloat m_ArmedDuration = 10f;
        [Tooltip("When the objective is armed, the player that armed it will be awarded this much score.")]
        [SerializeField] protected ObscuredInt m_ArmScoreAmount = 25;
        [Tooltip("When the objective is disarmed, the player that disarmed it will be awarded this much score.")]
        [SerializeField] protected ObscuredInt m_DisarmScoreAmount = 25;
#endif

        [Tooltip("Optional audio clip for alerting players when the objective is armed.")]
        [SerializeField] protected AudioClip m_ArmedAlertAudioClip;
        [Tooltip("Optional pulsing light/s for alerting players when the objective is armed.")]
        [SerializeField] protected PulsingLight[] m_PulsingLights;
        [Tooltip("Optional pulsing emission/s for alerting players when the objective is armed.")]
        [SerializeField] protected PulsingEmission[] m_PulsingEmissions;

#if !ANTICHEAT
        [SerializeField] protected bool m_ExecuteOnDeathEvent = false;
#else
        [SerializeField] protected ObscuredBool m_ExecuteOnDeathEvent = false;
#endif

        [Tooltip("Unity event invoked when taking damage.")]
        [SerializeField] protected UnityFloatVector3Vector3GameObjectEvent m_OnDamageEvent;
        [Tooltip("Unity event invoked when healing.")]
        [SerializeField] protected UnityFloatEvent m_OnHealEvent;
        [Tooltip("Unity event invoked when the object dies.")]
        [SerializeField] protected UnityVector3Vector3GameObjectEvent m_OnDeathEvent;

        public float ArmDuration => m_ArmDuration;//Obscured variable will be correctly retrieved if using ACTK
        public float DisarmDuration => m_DisarmDuration;//Obscured variable will be correctly retrieved if using ACTK

        private ScheduledEventBase m_ScheduledDestruction = null;

        //IDamageTarget implementaton
        public GameObject Owner => gameObject;
        public GameObject HitGameObject => gameObject;

        public bool Invincible { get => m_Invincible; set => m_Invincible = value; }//Obscured variable will be correctly retrieved/set if using ACTK

        // Values that will be synced over network
        private bool m_IsArmed;
        private float m_CurrentHealth;

        //Audio
        private AudioSource m_AudioSource;

        public override void Awake()
        {
            if (MPMaster.Instance is MPDMMaster)
            {
                if ((MPMaster.Instance as MPDMMaster).CurrentGameType != GameType.Rush)
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

            m_CurrentHealth = m_MaxHealth;

            m_AudioSource = GetComponent<AudioSource>();
            if (m_AudioSource)
            {
                m_AudioSource.playOnAwake = false;
                m_AudioSource.loop = true;
                if (m_AudioSource.isPlaying)
                    m_AudioSource.Stop();

                if (m_ArmedAlertAudioClip)
                    m_AudioSource.clip = m_ArmedAlertAudioClip;
            }


            for (int i = 0; i < m_NextObjectiveSet.Length; i++)
                m_NextObjectiveSet[i].gameObject.SetActive(false);

            if (m_PulsingLights != null && m_PulsingLights.Length > 0)
            {
                for (int i = 0; i < m_PulsingLights.Length; i++)
                {
                    if (m_PulsingLights[i] == null)
                        continue;
                    m_PulsingLights[i].SyncEnabled = false;
                    m_PulsingLights[i].Pulse(false);
                }
            }
            if (m_PulsingEmissions != null && m_PulsingEmissions.Length > 0)
            {
                for (int i = 0; i < m_PulsingEmissions.Length; i++)
                {
                    if (m_PulsingEmissions[i] == null)
                        continue;
                    m_PulsingEmissions[i].SyncEnabled = false;
                    m_PulsingEmissions[i].Pulse(false);
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetAlarm(false);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetAlarm(false);
        }

        private void SetAlarm(bool active)
        {
            if (m_PulsingLights != null && m_PulsingLights.Length > 0)
            {
                for (int i = 0; i < m_PulsingLights.Length; i++)
                {
                    if (m_PulsingLights[i] == null)
                        continue;

                    m_PulsingLights[i].Pulse(active);
                }
            }
            if (m_PulsingEmissions != null && m_PulsingEmissions.Length > 0)
            {
                for (int i = 0; i < m_PulsingEmissions.Length; i++)
                {
                    if (m_PulsingEmissions[i] == null)
                        continue;

                    m_PulsingEmissions[i].Pulse(active);
                }
            }
            if (active)
            {
                if (m_AudioSource && m_AudioSource.clip && m_AudioSource.isPlaying == false)
                    m_AudioSource.Play();
            }
            else
            {
                if (m_AudioSource && m_AudioSource.isPlaying == true)
                    m_AudioSource.Stop();
            }
        }

#if PHOTON_UNITY_NETWORKING
        protected override void OnPlayerEnteredRoom(Player player, GameObject character)
        {
            base.OnPlayerEnteredRoom(player, character);
            if (player == PhotonNetwork.LocalPlayer && MPLocalPlayer.Instance)
                SetObjectiveColor();
        }
#endif

        private void SetObjectiveColor()
        {
            Color color = m_UseDefendingTeamColor ? MPTeamManager.GetTeamColor(m_DefendingTeamNumber) : (MPLocalPlayer.Instance.TeamNumber == m_DefendingTeamNumber ? Color.blue : Color.red);
            base.SetColor(color);
        }

        public void InitNext(Color color)
        {
            gameObject.SetActive(true);
            base.SetColor(color);
        }

        public string AbilityMessage()
        {
            return m_IsArmed ? m_DisarmMessage : m_ArmMessage;
        }

        public bool Heal(float amount)
        {
            if (m_CurrentHealth == m_MaxHealth)
                return false;

            float adjust = m_CurrentHealth + amount;
            if (adjust >= m_MaxHealth)
                adjust = m_MaxHealth;

            m_CurrentHealth = adjust;

            if (m_OnHealEvent != null)
            {
                m_OnHealEvent.Invoke(amount);
            }

            return true;
        }

        public bool IsAlive()
        {
            return m_CurrentHealth > 0;
        }

        public void Damage(DamageData damageData)
        {
            if (m_Invincible == true)
                return;

            if (Gameplay.IsMaster)
            {
                /*  if (m_DamageThreshold > 0 && damageData.Amount < m_DamageThreshold)//NOTE: Now using CustomDamageProcessor
                      return;*/

                //  Debug.Log("damage: " + damageData.Amount);
                if (m_OnDamageEvent != null)
                    m_OnDamageEvent.Invoke(damageData.Amount, damageData.Position, damageData.Direction * damageData.ForceMagnitude, damageData.DamageSource.SourceOwner);

                GameObject a = null;
                if (damageData.DamageSource != null)
                    a = damageData.DamageSource.SourceOwner;
                else if (damageData.ImpactContext != null)
                    a = damageData.ImpactContext.ImpactCollisionData.SourceCharacterLocomotion.gameObject;

                if (a == null)
                    return;

                MPPlayer attacker = MPPlayer.Get(a.transform);

                if (attacker == null)
                    return;

                if (m_AllowFriendlyDamage == false && attacker.TeamNumber == m_DefendingTeamNumber)
                    return;

                m_CurrentHealth -= damageData.Amount;

                if (m_CurrentHealth <= 0)
                {
                    if (m_OnDeathEvent != null)
                        m_OnDeathEvent.Invoke(damageData.Position, damageData.Direction * damageData.ForceMagnitude, damageData.DamageSource.SourceOwner);
                   
                    UpdatePlayerScore(attacker, m_CaptureScoreAmount + (int)damageData.Amount);

                }
                else if(damageData.Amount > 0)
                    UpdatePlayerScore(attacker, m_CapturingScoreAmount + (int)damageData.Amount);
            }
        }

        private void UpdatePlayerScore(MPPlayer player , int amount)
        {
#if PHOTON_UNITY_NETWORKING
            photonView.RPC("UpdatePlayerScoreRPC", RpcTarget.All, player.ID, amount, m_CurrentHealth);
#endif
        }
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        private void UpdatePlayerScoreRPC(int playerActorNumber, int score, float health)
        {
            MPPlayer attacker = MPPlayer.Get(playerActorNumber);
            if (attacker)
            {
                m_CurrentHealth = health;

                if (attacker.TeamNumber == m_DefendingTeamNumber)
                    attacker.Stats.Set("Score", (int)attacker.Stats.Get("Score") - score);
                else attacker.Stats.Set("Score", (int)attacker.Stats.Get("Score") + score);

                if (m_CurrentHealth <= 0)
                {
                    m_CurrentHealth = 0;

                    ObjectiveDestroyed(attacker);
                }
                else
                {
                    MPTeamManager.Instance.RefreshTeams();
                }
            }
        }

        private void Arm(MPPlayer player)
        {
            //award the attacker with the arm score amount.
            player.Stats.Set("Score", (int)player.Stats.Get("Score") + m_ArmScoreAmount);

            SetAlarm(true);

            //TODO: grab the sender info for remotes and minus the sent packet time for perfect sync with low data amount.
            if (m_ScheduledDestruction == null || m_ScheduledDestruction != null && m_ScheduledDestruction.Active == false)
                m_ScheduledDestruction = Scheduler.Schedule<MPPlayer>(m_ArmedDuration, ObjectiveDestroyed, player);

            MPTeamManager.Instance.RefreshTeams();
        }

        private void Disarm(MPPlayer player)
        {
            //award the defender with the disarm score amount.
            player.Stats.Set("Score", (int)player.Stats.Get("Score") + m_DisarmScoreAmount);

            SetAlarm(false);

            if (m_ScheduledDestruction != null && m_ScheduledDestruction.Active)
            {
                Scheduler.Cancel(m_ScheduledDestruction);
                m_ScheduledDestruction = null;
            }

            MPTeamManager.Instance.RefreshTeams();
        }

        private void ObjectiveDestroyed(MPPlayer attacker)
        {
            if (m_ScheduledDestruction != null && m_ScheduledDestruction.Active)
            {
                Scheduler.Cancel(m_ScheduledDestruction);
                m_ScheduledDestruction = null;
            }

            //award the attacker with the capture score amount.
            attacker.Stats.Set("Score", (int)attacker.Stats.Get("Score") + m_CaptureScoreAmount);

            //award the team with the capture score amount.
            (MPTeamManager.Instance.Teams[attacker.TeamNumber] as MPDMTeam).ExtraScore += m_CaptureScoreAmount;

            MPTeamManager.Instance.RefreshTeams();

            for (int i = 0; i < m_SpawnOnDeath.Length; i++)
                ObjectPoolBase.Instantiate(m_SpawnOnDeath[i], transform.position, transform.rotation);

            for (int i = 0; i < m_DestroyOnDeath.Length; i++)
                Destroy(m_DestroyOnDeath[i]);

            for (int i = 0; i < m_SetActiveOnDeath.Length; i++)
                m_SetActiveOnDeath[i].SetActive(true);

            for (int i = 0; i < m_DeactivateOnDeath.Length; i++)
                m_DeactivateOnDeath[i].SetActive(false);

            if (m_NextObjectiveSet != null && m_NextObjectiveSet.Length > 0)
            {
                //check for dependencies that are yet to be destroyed.
                bool nextSet = true;
                if (m_DependentObjectives != null && m_DependentObjectives.Length > 0)
                {
                    for (int i = 0; i < m_DependentObjectives.Length; i++)
                    {
                        if (m_DependentObjectives[i] == this)
                            continue;
                        if (!m_DependentObjectives[i].gameObject.activeInHierarchy)
                            continue;

                        nextSet = false;
                        break;
                    }
                }

                if (nextSet) //no active dependencies were found, init the next set.
                {
                    for (int i = 0; i < m_NextObjectiveSet.Length; i++)
                        m_NextObjectiveSet[i].InitNext(m_CurrentColor);
                }
            }
            else
            {
                //check for dependencies that are yet to be destroyed.
                bool gameOver = true;
                if (m_DependentObjectives != null && m_DependentObjectives.Length > 0)
                {
                    for (int i = 0; i < m_DependentObjectives.Length; i++)
                    {
                        if (m_DependentObjectives[i] == this)
                            continue;
                        if (!m_DependentObjectives[i].gameObject.activeInHierarchy)
                            continue;

                        gameOver = false;
                        break;
                    }
                }

                if (gameOver) //no active dependencies were found, game over.
                    MPMaster.Instance.StopGame();
            }

            if (m_DisableOnDeath)
                gameObject.SetActive(false);

            if (m_ExecuteOnDeathEvent == true)
                EventHandler.ExecuteEvent<Vector3, Vector3, GameObject>(m_ObjectiveGameObject, "OnDeath", m_ObjectiveTransform.position, Vector3.zero, attacker.GameObject);
        }

        public override void FullReset()
        {
            base.FullReset();

            m_CurrentHealth = m_MaxHealth;
            m_IsArmed = false;

            if (m_IsPartOfFirstSet)
                gameObject.SetActive(true);
            else if (gameObject.activeSelf)
                gameObject.SetActive(false);
            SetAlarm(false);
        }

        public bool CanInteract()
        {
            if (m_IsArmed && MPLocalPlayer.Instance.TeamNumber == m_DefendingTeamNumber)
            {
                return true;
            }

            if (!m_IsArmed && MPLocalPlayer.Instance.TeamNumber != m_DefendingTeamNumber)
            {
                return true;
            }

            return false;
        }

        public void Interact(GameObject character)
        {
            m_IsArmed = !m_IsArmed;

            MPPlayer p = character.GetComponent<MPPlayer>();
#if PHOTON_UNITY_NETWORKING
            if (p.photonView.IsMine)
                photonView.RPC("InteractRPC", RpcTarget.Others, p.ID);
#endif
            if (m_IsArmed)
                Arm(p);
            else Disarm(p);
        }
#if PHOTON_UNITY_NETWORKING
        [PunRPC]
#endif
        private void InteractRPC(int playerActorNumber)
        {
            MPPlayer p = MPPlayer.Get(playerActorNumber);
            if (p == null)
            {
                return;
            }

            var characterLocomotion = p.GetUltimateCharacterLocomotion;
            if (characterLocomotion == null)
            {
                return;
            }

            Interact(p.gameObject);
        }
    }
}
