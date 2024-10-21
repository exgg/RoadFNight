using System;
using System.Threading.Tasks;
using Opsive.UltimateCharacterController.Items.Actions.Impact;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.PrejudiceEngine;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;
using UnityEngine.AI;

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
        private bool _isCalculatingState;

        private void Awake()
        {
            _animationHandler = GetComponentInChildren<AIAnimationHandler>();
            _aiStats = GetComponent<Pedestrian>();
            agent = GetComponent<NavMeshAgent>();
        }

        // Main state management function without threading
        public void HandleMovementStateMachine()
        {
            // Check if AI is alive and there is a current state
            if (_aiStats.currenHealthStatus == HealthStatus.Died || _aiStats.health <= 0)
            {
                BeginRagdoll();
                return;
            }

            if (!_isCalculatingState)
            {
                // Directly calculate next state on the main thread
                CalculateNextState();
            }
        }

        private void CalculateNextState()
        {
            _isCalculatingState = true;

            if (currentTarget != null && currentTarget.GetComponent<Pedestrian>().currenHealthStatus == HealthStatus.Alive)
            {
                _hasTarget = true;
                _nextState = currentAggressiveState.Tick(this, _aiStats, _animationHandler);
            }
            else
            {
                _hasTarget = false;
                _nextState = currentState.Tick(this, _aiStats, _animationHandler);
            }

            // Now switch to the next state
            SwitchToNextState(_nextState);
            
            _isCalculatingState = false;
        }

        public void SwitchToNextState(BaseState state)
        {
            if (_hasTarget)
                currentAggressiveState = state;
            else
                currentState = state;
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
