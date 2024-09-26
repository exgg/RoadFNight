using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class WalkingState : BaseState
    {
        public PathfinderState pathfinderState;
        public InitialPathfinderState initialPathfinderState;
    
        // Tolerance level for floating point precision and multiple agents attempting to reach point 
        private readonly float _destinationTolerance = 2f;

        public override BaseState Tick(StateHandler stateHandler, AIStats aiStats, AIAnimationHandler animationHandler)
        {
            if (!stateHandler.currentPathPoint)
            {
                Debug.LogError("There is no path point setup");
                return initialPathfinderState;
            }

            stateHandler.agent.destination = stateHandler.currentPathPoint.transform.position;

            // Use Vector3.Distance to handle floating-point precision for arrival
            float distanceToTarget = Vector3.Distance(stateHandler.transform.position, stateHandler.currentPathPoint.transform.position);

            if (distanceToTarget <= _destinationTolerance)
            {
                Debug.Log("Reached destination moving on");

                // Now set the current path as the previous one, ensuring we don't double back
                stateHandler.previousPathPoint = stateHandler.currentPathPoint;

                return pathfinderState; // Move to next state to find a new path
            }
            else
            {
                Debug.Log("Approaching destination");
                return this;
            }
        }
    }
}
