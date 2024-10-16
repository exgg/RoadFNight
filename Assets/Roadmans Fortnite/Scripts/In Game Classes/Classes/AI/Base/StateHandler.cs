using System;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.PrejudiceEngine;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States;
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

        private bool _overrideStates;
        
        
        public GameObject currentPathPoint;
        public GameObject previousPathPoint; // this will be used to prevent double backing on themselves

        public GameObject myLeader;

        private bool _hasTarget;
        
        private void Awake()
        {
            _animationHandler = GetComponentInChildren<AIAnimationHandler>();
            _aiStats = GetComponent<Pedestrian>();
            agent = GetComponent<NavMeshAgent>();
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
            // Check if AI is alive and there is a current state
            if(_aiStats.currenHealthStatus != HealthStatus.Alive)
                return;
            if(!currentState)
                return;

            // Check if there's a valid target
            if (currentTarget && currentTarget.GetComponent<Pedestrian>().currenHealthStatus == HealthStatus.Alive)
            {
                _nextState = currentAggressiveState.Tick(this, _aiStats, _animationHandler);
                _hasTarget = true; // Set the flag that the AI has a target
            }
            else
            {
                _nextState = currentState.Tick(this, _aiStats, _animationHandler);
                _hasTarget = false; // Set the flag that the AI does not have a target
            }

            if (_nextState)
                SwitchToNextState(_nextState);
        }

        private void SwitchToNextState(BaseState state)
        {
            if (_hasTarget) // If there's a target, update aggressive state
            {
                currentAggressiveState = state;
            }
            else // Otherwise, update the regular state
            {
                currentState = state;
            }
        }
        
    }
}
