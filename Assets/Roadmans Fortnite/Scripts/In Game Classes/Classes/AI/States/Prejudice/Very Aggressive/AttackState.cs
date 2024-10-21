using System.Threading.Tasks;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class AttackState : BaseState
    {
        public float attackCooldown = 1.5f;
        private float currentCooldownTime = 0f;

        [Header("States")]
        public PursueTargetState pursueTargetState;
        public AttackStanceState attackStanceState;
        public WalkingState walkingState;

        private bool _isCalculating = false; // To track if calculations are running

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // If already calculating, don't start a new calculation
            if (_isCalculating) return this;

            // Start a new threaded task to handle the attack logic
            HandleAttackAsync(stateHandler, aiStats, animationHandler);

            return this; // Remain in this state until calculation is complete
        }

        private async void HandleAttackAsync(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            _isCalculating = true;

            bool shouldAttack = await Task.Run(() =>
            {
                // Ensure there's a valid target
                if (stateHandler.currentTarget == null)
                {
                    Debug.LogWarning("No target available for attack or target is dead");
                    return false;
                }

                // Check if the target is dead
                var targetPedestrian = stateHandler.currentTarget.GetComponent<Pedestrian>();
                if (targetPedestrian != null)
                {
                    if (targetPedestrian.health <= 0 || targetPedestrian.currenHealthStatus == HealthStatus.Died)
                    {
                        Debug.Log("Target is dead, abandoning attack.");
                        stateHandler.currentTarget = null;
                        return false;
                    }
                }

                // Handle cooldown between attacks
                if (currentCooldownTime > 0)
                {
                    currentCooldownTime -= Time.deltaTime;
                    return false; // Cooldown still in progress, skip the attack
                }

                return true; // Cooldown is complete, we can attack
            });

            if (shouldAttack)
            {
                // Attack and reset cooldown (executed on the main thread)
                PerformAttack(stateHandler.currentTarget, animationHandler, stateHandler);
                currentCooldownTime = attackCooldown;

                // Transition back to the attack stance after the attack
                stateHandler.SwitchToNextState(attackStanceState);
            }

            _isCalculating = false;
        }

        private void PerformAttack(GameObject target, AIAnimationHandler animationHandler, StateHandler stateHandler)
        {
            // Play the attack animation (Unity-specific, should remain on the main thread)
            Debug.Log("Playing attack animation.");
            animationHandler.PlaySpecificAnimation("Attack");

            // Apply damage to the target (Unity-specific, so done on the main thread)
            var targetPedestrian = target.GetComponent<Pedestrian>();
            if (targetPedestrian != null)
            {
                Debug.Log("Attacking target.");
                targetPedestrian.health -= Random.Range(5, 30); // Apply damage

                // Check if the target should retaliate
                var targetStateHandler = target.GetComponent<StateHandler>();
                if (!targetStateHandler.currentTarget)
                    targetStateHandler.currentTarget = stateHandler.gameObject;

                // Mark the target as dead if health falls to 0 or below
                if (targetPedestrian.health <= 0)
                {
                    targetPedestrian.currenHealthStatus = HealthStatus.Died;
                    targetStateHandler.BeginRagdoll();
                    Debug.Log($"{targetPedestrian.name} is dead.");
                    walkingState.startedWalking = false;
                    stateHandler.currentTarget = null;
                }
            }
        }

        private void FaceTarget(StateHandler stateHandler)
        {
            // Rotate towards the target (Unity-specific, so should remain on the main thread)
            Vector3 direction = (stateHandler.currentTarget.transform.position - stateHandler.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            stateHandler.transform.rotation = Quaternion.Slerp(stateHandler.transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}
