using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class WaitingState : BaseState
    {
        public InitialPathfinderState initialPathfinderState;
        public WalkingState walkingState;
        
        private bool _isWaiting;
        private TrafficLightSystem _trafficLightSystem;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            if (!stateHandler.currentPathPoint)
            {
                Debug.LogError("No path point setup.");
                _isWaiting = false;
                return initialPathfinderState;
            }

            // Get the waypoint logger from the current path point
            WaypointLogger waypointLogger = stateHandler.currentPathPoint.GetComponent<WaypointLogger>();
            if (!waypointLogger)
            {
                Debug.LogError("No WaypointLogger found on the current path point.");
                _isWaiting = false;
                return initialPathfinderState;
            }

            _trafficLightSystem = waypointLogger.NearestTrafficLight;

            if (!_trafficLightSystem)
            {
                Debug.LogError("No TrafficLightSystem found on the WaypointLogger.");
                _isWaiting = false;
                return initialPathfinderState;
            }

            if (!_isWaiting)
            {
                // Subscribe to traffic light state change event
                _trafficLightSystem.OnLightChanged += HandleLightChange;

                _isWaiting = true;
                animationHandler.SetWaitingAnimation(aiStats.myGender);
                Debug.Log("Waiting at traffic light.");
            }

            return this; // Stay in waiting state until the light changes
        }

        private void HandleLightChange(TrafficDirection axis)
        {
            if ((_trafficLightSystem.canCrossX && axis == TrafficDirection.X) || (_trafficLightSystem.canCrossY && axis == TrafficDirection.Y))
            {
                Debug.Log("Traffic light is green. Proceeding to walking state.");
                _isWaiting = false;

                // Unsubscribe from the event
                _trafficLightSystem.OnLightChanged -= HandleLightChange;

                // Transition back to walking state
                var stateHandler = GetComponentInParent<StateHandler>();
                stateHandler.SwitchToNextState(walkingState);
            }
        }
    }
}
