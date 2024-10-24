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
                _isWaiting = false;
                return initialPathfinderState;
            }

            // Cache the WaypointLogger and TrafficLightSystem for optimization
            if (_waypointLogger == null)
            {
                _waypointLogger = stateHandler.currentPathPoint.GetComponent<WaypointLogger>();

                if (_waypointLogger == null)
                {
                    _isWaiting = false;
                    return initialPathfinderState;
                }

                _trafficLightSystem = _waypointLogger.NearestTrafficLight;

                if (_trafficLightSystem == null)
                {
                    return walkingState; // Allow crossing if there's no traffic light system
                }
            }

            // Check if AI can cross the road based on the traffic light system
            if (CanCrossRoad(stateHandler))
            {
                stateHandler.agent.isStopped = false;
                _isWaiting = false;
                walkingState.startedWalking = false;
                walkingState.isCrossingRoad = true;
                return walkingState;
            }

            // Set waiting animation and wait for green light
            if (!_isWaiting)
            {
                _isWaiting = true;
                stateHandler.agent.isStopped = true;
                animationHandler.SetWaitingAnimation(aiStats.myGender);
            }

            return this; // Stay in the WaitingState if cannot cross
        }

        /// <summary>
        /// Checks if the AI can cross the road based on the traffic light system.
        /// </summary>
        private bool CanCrossRoad(StateHandler stateHandler)
        {
            // Calculate the direction to the next point only once
            Vector3 directionToNextPoint = (stateHandler.currentPathPoint.transform.position - stateHandler.previousPathPoint.transform.position).normalized;

            // Determine the primary movement direction (X or Z axis)
            bool isMovingAlongX = Mathf.Abs(directionToNextPoint.x) > Mathf.Abs(directionToNextPoint.z);

            // Return if AI can cross based on the current light system for X or Y axis
            return isMovingAlongX ? _trafficLightSystem.canCrossX : _trafficLightSystem.canCrossY;
        }
    }
}
