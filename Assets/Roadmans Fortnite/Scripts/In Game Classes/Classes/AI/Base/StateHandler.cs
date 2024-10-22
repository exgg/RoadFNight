using System;
using Opsive.UltimateCharacterController.Items.Actions.Impact;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.PrejudiceEngine;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base
{
    public class StateHandler : MonoBehaviour
    {
        private AIAnimationHandler _animationHandler;
        private Pedestrian _aiStats;
        private AIScanner _aiScanner;
        
        [Header("Navmesh")] 
        public NavMeshAgent agent;

        [Space(1)]
        [Header("States")]
        public BaseState currentState;
        public BaseState currentAggressiveState;
        public BaseState ragdollState;
        
        private BaseState _nextState;
        
        [Space(1)]
        [Header("Pre-Checks")]
        public bool isPerformingAction;
        public bool isInteracting;

        [Space(1)]
        [Header("AI Settings")] 
        public float maxViewConeAngle;
        public float minViewConeAngle;
        public float detectionDistance;

        [Space(1)] 
        [Header("Debug Displays")]
        public GameObject currentTarget; // change this to character stats later down the line

        [Space(1)] [Header("Rag Dolling")]
        public MonoBehaviour[] classes;
        public Rigidbody initialForceApplication;
        
        private bool _overrideStates;
        
        [Space(1)] [Header("Path Debugging")]
        public GameObject currentPathPoint;
        public GameObject previousPathPoint; // this will be used to prevent double backing on themselves

        public GameObject myLeader;

        private bool _hasTarget;
        private CapsuleCollider _capsuleCollider;
        
        private Pedestrian _cachedTargetPedestrian;
        
        private void Awake()
        {
            _animationHandler = GetComponentInChildren<AIAnimationHandler>();
            _aiStats = GetComponent<Pedestrian>();
            agent = GetComponent<NavMeshAgent>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
        }

        private void Start()
        {
            if (_aiStats.myGroupControlType != GroupControlType.Leader)
            {
                foreach (Pedestrian pedestrian in transform.parent.gameObject.GetComponentsInChildren<Pedestrian>())
                {
                    if (pedestrian.myGroupControlType == GroupControlType.Leader)
                    {
                        myLeader = pedestrian.gameObject;
                    }
                }
            }
        }

          public void HandleMovementStateMachine()
        {
            // Validate the current target and check its state
            HandleTargetValidation();

            // Exit if no current state is defined
            if (!currentState) return;

            // Decide the next state based on target status
            DecideNextState();

            // Switch to the next state if available
            if (_nextState) SwitchToNextState(_nextState);
        }

        /// <summary>
        /// Handles the validation of the current target to check if it's alive or needs to be cleared.
        /// </summary>
        private void HandleTargetValidation()
        {
            // Check if a target exists
            if (currentTarget)
            {
                CacheTargetPedestrianComponent();

                // Invalidate the target if it's dead
                if (IsTargetDead())
                {
                    ClearCurrentTarget();
                }
            }
        }

        /// <summary>
        /// Caches the Pedestrian component of the current target to avoid repeated GetComponent calls.
        /// </summary>
        private void CacheTargetPedestrianComponent()
        {
            if (!_cachedTargetPedestrian || _cachedTargetPedestrian.gameObject != currentTarget)
            {
                _cachedTargetPedestrian = currentTarget.GetComponent<Pedestrian>();
            }
        }

        /// <summary>
        /// Checks if the current target is dead.
        /// </summary>
        private bool IsTargetDead()
        {
            return _cachedTargetPedestrian != null && _cachedTargetPedestrian.health <= 0;
        }

        /// <summary>
        /// Clears the current target and resets the cached pedestrian.
        /// </summary>
        private void ClearCurrentTarget()
        {
            currentTarget = null;
            _cachedTargetPedestrian = null;
        }

        /// <summary>
        /// Decides the next state based on whether a valid target exists.
        /// </summary>
        private void DecideNextState()
        {
            if (HasValidTarget())
            {
                _nextState = currentAggressiveState.Tick(this, _aiStats, _animationHandler);
                _hasTarget = true;
            }
            else
            {
                _nextState = currentState.Tick(this, _aiStats, _animationHandler);
                _hasTarget = false;
            }
        }

        /// <summary>
        /// Checks if the current target is valid and alive.
        /// </summary>
        private bool HasValidTarget()
        {
            return currentTarget && _cachedTargetPedestrian != null && _cachedTargetPedestrian.currenHealthStatus == HealthStatus.Alive;
        }

        /// <summary>
        /// Switches to the next state if needed.
        /// </summary>
        /// <param name="state">The next state to switch to.</param>
        private void SwitchToNextState(BaseState state)
        {
            if (_hasTarget)
            {
                currentAggressiveState = state;
            }
            else
            {
                currentState = state;
            }
        }

        public void BeginRagdoll()
        {
            _animationHandler.animator.enabled = false;
            foreach(var nClass in classes)
            {
                nClass.enabled = false;
            }

            agent.enabled = false;
            _capsuleCollider.enabled = false;
            
            initialForceApplication.AddForce(0, 5, 0);

            var masterBrain = FindObjectOfType<PedestrianSystem>();
            
            masterBrain.groupLst.Remove(transform.parent.GetComponent<PedestrianGroup>());
            
            enabled = false;
        }
    }
}
