using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Prejudice.Very_Aggressive
{
    public class AttackState : BaseState
    {
        public float attackCooldown = 1.5f;
        private float _currentCooldownTime = 0f;

        [Header("States")]
        public PursueTargetState pursueTargetState;
        public AttackStanceState attackStanceState;
        public WalkingState walkingState;

        private Pedestrian _targetPedestrian;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // Ensure there's a valid target
            if (stateHandler.currentTarget == null || IsTargetDead(stateHandler))
            {
                stateHandler.currentTarget = null;
                return attackStanceState;
            }

            // Face the target
            FaceTarget(stateHandler);

            // Handle cooldown between attacks
            if (_currentCooldownTime > 0)
            {
                _currentCooldownTime -= Time.deltaTime;
                return this; // Stay in attack state until cooldown is complete
            }

            // Perform the attack
            PerformAttack(stateHandler.currentTarget, animationHandler, stateHandler);

            // Reset the cooldown after the attack
            _currentCooldownTime = attackCooldown;

            return attackStanceState; // Return to attack stance after the attack
        }

        private void PerformAttack(GameObject target, AIAnimationHandler animationHandler, StateHandler stateHandler)
        {
            if (_targetPedestrian == null) return;

            // Play the attack animation
            animationHandler.PlaySpecificAnimation("Attack");

            // Apply damage to the target
            _targetPedestrian.health -= Random.Range(5, 30); // Apply damage

            if (_targetPedestrian.health <= 0)
            {
                // Mark the target as dead
                _targetPedestrian.currenHealthStatus = HealthStatus.Died;

                // Trigger ragdoll for the dead target
                var targetStateHandler = target.GetComponent<StateHandler>();
                if (targetStateHandler != null)
                {
                    targetStateHandler.BeginRagdoll();
                }

                // Clear target and log death
                Debug.Log($"{_targetPedestrian.name} is dead.");
                walkingState.startedWalking = false;
                stateHandler.currentTarget = null;
            }
        }

        private bool IsTargetDead(StateHandler stateHandler)
        {
            // Cache the target Pedestrian component once
            if (_targetPedestrian == null)
            {
                _targetPedestrian = stateHandler.currentTarget.GetComponent<Pedestrian>();
            }

            return _targetPedestrian == null || _targetPedestrian.currenHealthStatus == HealthStatus.Died || _targetPedestrian.health <= 0;
        }

        private void FaceTarget(StateHandler stateHandler)
        {
            if (_targetPedestrian == null) return;

            // Rotate towards the target if they exist
            Vector3 direction = (_targetPedestrian.transform.position - stateHandler.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

            // Only rotate when necessary (when the direction changes significantly)
            if (Vector3.Dot(stateHandler.transform.forward, direction) < 0.99f)
            {
                stateHandler.transform.rotation = Quaternion.Slerp(stateHandler.transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }
    }
}
