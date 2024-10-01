using System;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
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

        [Header("Navmesh")] 
        public NavMeshAgent agent;

        [Space(1)]
        [Header("State")]
        public BaseState currentState;

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
        
        public GameObject currentPathPoint;
        public GameObject previousPathPoint; // this will be used to prevent double backing on themselves

        public GameObject myLeader;
        
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
            Debug.Log($"I am running from {transform.name}");
            
            if(_aiStats.currenHealthStatus != HealthStatus.Alive)
                return;
            
            if(!currentState)
                return;
            
            
            BaseState nextState = currentState.Tick(this, _aiStats, _animationHandler);
            
            if(nextState != null)
                SwitchToNextState(nextState);
        }

        private void SwitchToNextState(BaseState state)
        {
            currentState = state;
        }
    }
}
