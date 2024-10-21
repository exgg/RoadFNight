using System.Threading.Tasks;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class PursueTargetState : BaseState
    {
        public float attackRange = 2f; // The range at which the AI can start attacking

        public InitialPathfinderState initialPathfinderState;
        public FollowingState followingState;
        public AttackStanceState attackStanceState; // Reference to the next state

        private bool _isCalculating = false;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // If AI is already calculating, return and don't run again.
            if (_isCalculating) return this;

            // Check if the AI has a current target
            if (stateHandler.currentTarget == null)
            {
                Debug.LogWarning("No target assigned for pursuing.");
                return ReturnCorrectState(aiStats);
            }

            // Start a threaded task to perform decision-making logic
            HandlePursueLogicAsync(stateHandler, aiStats, animationHandler);
            return this; // Stay in the pursue state while calculations are being made.
        }

        private async void HandlePursueLogicAsync(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            _isCalculating = true;

            // Use Task.Run to offload distance calculation and decision logic to another thread
            bool shouldAttack = await Task.Run(() =>
            {
                // Get the target's position
                GameObject target = stateHandler.currentTarget;

                // Check if the target is alive
                var targetPedestrian = target.GetComponent<Pedestrian>();
                if (targetPedestrian == null || targetPedestrian.currenHealthStatus == HealthStatus.Died)
                {
                    Debug.Log("Target is dead, abandoning pursuit.");
                    return false; // Abort attack if the target is dead
                }

                // Calculate the distance to the target (thread-safe since it's just reading data)
                float distanceToTarget = Vector3.Distance(stateHandler.transform.position, target.transform.position);

                return distanceToTarget <= attackRange; // Return whether AI should attack
            });

            _isCalculating = false;

            // Once the decision is made, move back to the main thread to handle Unity API calls
            if (shouldAttack)
            {
                // Set the agent's destination on the main thread
                stateHandler.agent.SetDestination(stateHandler.currentTarget.transform.position);

                // Switch to AttackStanceState on the main thread
                stateHandler.SwitchToNextState(attackStanceState);
            }
            else
            {
                // Continue pursuing the target
                stateHandler.agent.SetDestination(stateHandler.currentTarget.transform.position);
                animationHandler.SetWalkingAnimation("Running");
            }
        }

        private BaseState ReturnCorrectState(Pedestrian aiStats)
        {
            if (aiStats.myGroupControlType == GroupControlType.Leader)
                return initialPathfinderState;
            else
                return followingState;
        }
    }
}
