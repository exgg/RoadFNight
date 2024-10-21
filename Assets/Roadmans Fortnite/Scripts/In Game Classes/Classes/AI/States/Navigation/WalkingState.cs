using System.Threading.Tasks;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class WalkingState : BaseState
    {
        public PathfinderState pathfinderState;
        public InitialPathfinderState initialPathfinderState;
        public WaitingState waitingState;
        private TrafficLightSystem _trafficLightSystem;
        private WaypointLogger _waypointLogger;

        private readonly float _destinationTolerance = 2f;
        public bool startedWalking;
        private bool _isCalculating;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            if (!stateHandler.currentPathPoint)
            {
                Debug.LogError("There is no path point setup");
                startedWalking = false;
                return initialPathfinderState;
            }

            // Start walking only once
            if (!startedWalking)
            {
                string walkingStyle = aiStats.CheckWalkingStyle();
                animationHandler.SetWalkingAnimation(walkingStyle);
                startedWalking = true;
            }

            // Calculate path and road-crossing asynchronously if not already calculating
            if (!_isCalculating)
            {
                HandleMovementAsync(stateHandler).ConfigureAwait(false);
            }

            return this;
        }

        private async Task HandleMovementAsync(StateHandler stateHandler)
        {
            _isCalculating = true;

            // Perform distance check and path-finding logic in a background thread
            bool shouldStopWalking = await Task.Run(() =>
            {
                float distanceToTarget = Vector3.Distance(stateHandler.transform.position, stateHandler.currentPathPoint.transform.position);

                // Handle road-crossing logic and return if waiting state should trigger
                if (stateHandler.previousPathPoint != null && stateHandler.currentPathPoint != null)
                {
                    WaypointLogger previousLogger = stateHandler.previousPathPoint.GetComponent<WaypointLogger>();
                    WaypointLogger currentLogger = stateHandler.currentPathPoint.GetComponent<WaypointLogger>();

                    if (previousLogger != null && currentLogger != null)
                    {
                        if (previousLogger.IsRoadCrossPoint && currentLogger.IsRoadCrossPoint && !CanCrossRoad(stateHandler))
                        {
                            return true; // Stop walking, enter waiting state
                        }
                    }
                }

                // Check if the agent has reached the destination
                return distanceToTarget <= _destinationTolerance;
            });

            // Back on the main thread: update NavMeshAgent destination and transition states
            stateHandler.agent.destination = stateHandler.currentPathPoint.transform.position;

            if (shouldStopWalking)
            {
                // Now set the current path as the previous one, ensuring we don't double back
                stateHandler.previousPathPoint = stateHandler.currentPathPoint;
                startedWalking = false;
                stateHandler.SwitchToNextState(pathfinderState); // Move to next state to find a new path
            }
            else
            {
                // Check if it should enter the waiting state
                if (CanCrossRoad(stateHandler))
                {
                    stateHandler.SwitchToNextState(waitingState);
                }
            }

            _isCalculating = false;
        }

        /// <summary>
        /// Checks if the AI can cross the road based on the traffic light system.
        /// </summary>
        /// <param name="stateHandler">The state handler controlling this AI.</param>
        /// <returns>True if the AI can cross, otherwise false.</returns>
        private bool CanCrossRoad(StateHandler stateHandler)
        {
            Vector3 directionToNextPoint = (stateHandler.currentPathPoint.transform.position - stateHandler.previousPathPoint.transform.position).normalized;

            // Determine if the AI is primarily moving along the X or Z axis
            bool isMovingAlongX = Mathf.Abs(directionToNextPoint.x) > Mathf.Abs(directionToNextPoint.z);

            _trafficLightSystem = stateHandler.currentPathPoint.GetComponent<WaypointLogger>().NearestTrafficLight;

            if (!_trafficLightSystem)
            {
                Debug.LogError("No TrafficLightSystem found on the WaypointLogger.");
                return true; // Allow to proceed if no traffic light system is found
            }

            // Check if the traffic light is green for the direction AI is moving
            return isMovingAlongX ? _trafficLightSystem.canCrossX : _trafficLightSystem.canCrossY;
        }

        public void ResetWalking()
        {
            startedWalking = false;
        }
    }
}
