using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class WaitingState : BaseState
    {
        public InitialPathfinderState initialPathfinderState;
        public WalkingState walkingState;
        
        public bool _isWaiting; // Controlled by waiting signal e.g. traffic light

        private WaypointLogger _waypointLogger;
        private TrafficLightSystem _trafficLightSystem;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            if (!stateHandler.currentPathPoint)
            {
                Debug.LogError("There is no path point setup");
                return initialPathfinderState;
            }

            // Get the waypoint logger from the current path point
            _waypointLogger = stateHandler.currentPathPoint.GetComponent<WaypointLogger>();

            if (!_waypointLogger)
            {
                Debug.LogError("No WaypointLogger found on the current path point.");
                return initialPathfinderState;
            }

            // Check the direction and access traffic light accordingly
            bool canCross = CanCrossRoad(stateHandler);

            if (canCross)
            {
                Debug.Log("Traffic light is green. Proceeding to walking state.");
                stateHandler.agent.isStopped = false;
                return walkingState;
            }
            else
            {
                // Set the waiting animation if AI has to wait
                _isWaiting = true;
                animationHandler.SetWaitingAnimation(aiStats.myGender);
                Debug.Log("Waiting at traffic light.");
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
            _trafficLightSystem = _waypointLogger.NearestTrafficLight;

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
