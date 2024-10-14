using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class WalkingState : BaseState
    {
        public PathfinderState pathfinderState;
        public InitialPathfinderState initialPathfinderState;
    
        public WaitingState waitingState;
        private TrafficLightSystem _trafficLightSystem;
        private WaypointLogger _waypointLogger;
        
        // Tolerance level for floating point precision and multiple agents attempting to reach point 
        private readonly float _destinationTolerance = 2f;

        private bool _startedWalking;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            if (!stateHandler.currentPathPoint)
            {
                Debug.LogError("There is no path point setup");
                _startedWalking = false;
                return initialPathfinderState;
            }

            stateHandler.agent.destination = stateHandler.currentPathPoint.transform.position;

            // Use Vector3.Distance to handle floating-point precision for arrival
            float distanceToTarget = Vector3.Distance(stateHandler.transform.position, stateHandler.currentPathPoint.transform.position);

            if (!_startedWalking)
            {
                string walkingStyle = aiStats.CheckWalkingStyle();
                
                animationHandler.SetWalkingAnimation(walkingStyle);

                _startedWalking = true;
            }
            
            if (stateHandler.previousPathPoint != null && stateHandler.currentPathPoint != null)
            {
                WaypointLogger previousLogger = stateHandler.previousPathPoint.GetComponent<WaypointLogger>();
                WaypointLogger currentLogger = stateHandler.currentPathPoint.GetComponent<WaypointLogger>();

                Debug.Log($"currentLoggerIs a pathpoint {currentLogger.IsRoadCrossPoint} previousLoggerIs a pathpoint {previousLogger.IsRoadCrossPoint}");
                
                if (previousLogger == null || currentLogger == null)
                {
                    //Debug.LogError("[PathfinderState] One of the path points is missing the WaypointLogger component.");
                }
                else
                {
                    //Debug.Log($"[PathfinderState] Previous Path Point: {stateHandler.previousPathPoint.name}, IsRoadCrossPoint: {previousLogger.IsRoadCrossPoint}");
                    //Debug.Log($"[PathfinderState] Current Path Point: {stateHandler.currentPathPoint.name}, IsRoadCrossPoint: {currentLogger.IsRoadCrossPoint}");

                    if (previousLogger.IsRoadCrossPoint && currentLogger.IsRoadCrossPoint && !CanCrossRoad(stateHandler))
                    {
                        //Debug.Log($"[PathfinderState] Transitioning to WaitingState as both points are road crossing points.");
                        return waitingState;
                    }
                }
            }
            
            if (distanceToTarget <= _destinationTolerance)
            {
                //Debug.Log("Reached destination moving on");

                // Now set the current path as the previous one, ensuring we don't double back
                stateHandler.previousPathPoint = stateHandler.currentPathPoint;

                _startedWalking = false;
                return pathfinderState; // Move to next state to find a new path
            }
            else
            {
                //Debug.Log("Approaching destination");
                return this;
            }
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

            // Get the traffic light system for the appropriate axis
            
            _trafficLightSystem = stateHandler.currentPathPoint.GetComponent<WaypointLogger>().NearestTrafficLight;
    
            if (!_trafficLightSystem)
            {
                Debug.LogError("No TrafficLightSystem found on the WaypointLogger.");
                return true; // Allow to proceed if no traffic light system is found
            }

            // Check if the traffic light is green for the direction AI is moving
            if (isMovingAlongX)
            {
                // Moving along X axis
                return _trafficLightSystem.canCrossX;
            }
            else
            {
                // Moving along Z axis
                return _trafficLightSystem.canCrossY;
            }
        }
    }
}
