using System;
using Opsive.UltimateCharacterController.Objects.CharacterAssist;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base
{
    public class StateHandler : MonoBehaviour
    {
        private AIAnimationHandler _animationHandler;
        private AIStats _aiStats;

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
        
        // TODO: 
            // for prejudice engine, we will possibly need to handle 2 states at once.
            // 1 for movement and pathfinding, the other for visual for the AI
                // allowing us to continue moving while also looking for people we like/dislike in terms of ethnicity, gender etc
                
        private void Awake()
        {
            _animationHandler = GetComponentInChildren<AIAnimationHandler>();
            _aiStats = GetComponent<AIStats>();
            agent = GetComponent<NavMeshAgent>();
        }

        private void FixedUpdate()
        {
            HandleMovementStateMachine();
        }

        private void HandleMovementStateMachine()
        {
            if(_aiStats.isDead)
                return;
            if(!currentState)
            {
                Debug.LogWarning("The AI has not been given a starting state");
            }

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
