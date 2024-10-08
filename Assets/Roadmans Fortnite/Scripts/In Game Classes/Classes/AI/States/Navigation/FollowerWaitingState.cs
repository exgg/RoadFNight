using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians;
using UnityEngine;
using UnityEngine.AI;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class FollowerWaitingState : BaseState
    {
        public FollowingState followingState;
        private readonly float _baseDestinationTolerance = 2f;  // Base distance tolerance to leader or AI in front
        private readonly float _groupSpacingTolerance = 1.5f;   // Additional spacing tolerance for large groups

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            NavMeshAgent agent = stateHandler.agent;

            // Calculate dynamic tolerance based on group size
            float dynamicTolerance = CalculateDynamicTolerance(stateHandler);

            if (CalculateNeedToMove(stateHandler, dynamicTolerance))
            {
                return followingState; // Move to next state to find a new path
            }
            else
            {
                animationHandler.SetWaitingAnimation(aiStats.myGender);
                return this;
            }
        }

        private bool CalculateNeedToMove(StateHandler stateHandler, float tolerance)
        {
            return Vector3.Distance(stateHandler.transform.position, stateHandler.myLeader.transform.position) > tolerance;
        }

        /// <summary>
        /// Calculate a dynamic tolerance value based on the group size.
        /// Larger groups will have a higher tolerance to avoid congestion.
        /// </summary>
        private float CalculateDynamicTolerance(StateHandler stateHandler)
        {
            // Get the group size
            PedestrianGroup group = stateHandler.myLeader.GetComponentInParent<PedestrianGroup>();
            int groupSize = group.allMembers.Count;

            // Adjust tolerance dynamically based on group size
            return _baseDestinationTolerance + (groupSize * _groupSpacingTolerance * 0.1f);
        }
    }
}
