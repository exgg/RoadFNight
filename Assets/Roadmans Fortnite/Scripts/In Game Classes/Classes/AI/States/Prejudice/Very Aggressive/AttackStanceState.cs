using System.Threading.Tasks;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class AttackStanceState : BaseState
    {
        // References to other states
        public AttackState attackState;
        public PursueTargetState pursueTargetState;
        public WalkingState walkingState;

        // Time variables to simulate a wind-up before attack
        private readonly float _windUpTime = 1f;  // How long the AI will stay in the attack stance before attacking
        private float _currentWindUpTime; // Initialize this at zero

        private bool _isCalculating; // Track if decision-making is running

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // If already calculating, don't start a new calculation
            if (_isCalculating) return this;

            // Start a new threaded task to handle the decision-making process
            HandleAttackStanceLogicAsync(stateHandler, aiStats, animationHandler);

            return this; // Stay in this state until the calculation is complete
        }

        private async void HandleAttackStanceLogicAsync(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            _isCalculating = true;

            bool shouldAttack = await Task.Run(() =>
            {
                // Check if there's no valid target or the target is dead
                if (!stateHandler.currentTarget || stateHandler.currentTarget.GetComponent<Pedestrian>().currenHealthStatus == HealthStatus.Died || stateHandler.currentTarget.GetComponent<Pedestrian>().health <= 0)
                {
                    Debug.Log("No target available, returning to PursueTargetState.");
                    return false; // Return false, indicating we need to stop the attack
                }

                // Calculate the distance to the target
                GameObject target = stateHandler.currentTarget;
                float distanceToTarget = Vector3.Distance(stateHandler.transform.position, target.transform.position);

                // If the target is outside the attack range, return to PursueTargetState
                if (distanceToTarget > 1f) 
                {
                    Debug.Log("Target moved out of range, returning to PursueTargetState.");
                    return false; // Return false, indicating to stop and pursue the target
                }

                // Handle wind-up time asynchronously
                while (_currentWindUpTime < _windUpTime)
                {
                    _currentWindUpTime += Time.deltaTime;
                    Task.Delay(10).Wait(); // Simulate some delay
                }

                // Wind-up complete, ready to attack
                return true; // Return true, indicating the AI should attack
            });

            _isCalculating = false;

            // Once calculations are done, update the state based on the result
            if (shouldAttack)
            {
                // Reset wind-up time for next attack and transition to attack state
                _currentWindUpTime = 0f;
                stateHandler.SwitchToNextState(attackState);
            }
            else
            {
                // If target is gone or out of range, reset to pursue target
                stateHandler.SwitchToNextState(pursueTargetState);
            }
        }
    }
}
