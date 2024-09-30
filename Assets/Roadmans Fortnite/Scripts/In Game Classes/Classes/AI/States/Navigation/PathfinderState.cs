using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class PathfinderState : BaseState
    {
        public WalkingState walkingState;

        private const float FloatingPointThreshold = 3f; // Minimum distance to consider two points different
        private GameObject _lastChosenPathPoint = null; // Temporary reference for the last chosen path point

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            var randomPath = ChooseRandomPath(stateHandler);
            stateHandler.currentPathPoint = randomPath;

            if (stateHandler.currentPathPoint)
            {
                // Set lastChosenPathPoint to track where the AI is walking
                _lastChosenPathPoint = stateHandler.previousPathPoint; // IMPORTANT: Track the last chosen path properly
                return walkingState;
            }
            else
            {
                Debug.LogError("No valid path point found, staying in PathfinderState.");
                return this;
            }
        }

        private GameObject ChooseRandomPath(StateHandler stateHandler)
        {
            List<GameObject> waypointLoggerList =
                stateHandler.previousPathPoint.GetComponent<WaypointLogger>().waypoints;

            GameObject chosenPath = null;
            int safetyCounter = 0; // Safety to avoid infinite loop

            do
            {
                // Choose a random path from the waypoint list
                int randomChoice = Random.Range(0, waypointLoggerList.Count);
                chosenPath = waypointLoggerList[randomChoice];

                // Log the positions for debugging
                Debug.Log($"Previous path location: {stateHandler.previousPathPoint.transform.position}");
                Debug.Log(
                    $"Current path location: {stateHandler.currentPathPoint?.transform.position ?? Vector3.zero}");
                Debug.Log($"Chosen path location: {chosenPath.transform.position}");

                // Increment the safety counter to avoid infinite loops
                safetyCounter++;

                // Break if we exceed the safety limit (for very rare edge cases)
                if (safetyCounter > 100)
                {
                    Debug.LogWarning("Safety break: Could not find a valid path different from the previous one.");
                    break;
                }
            }
            // Ensure that the chosen path is not too close to the previous path, the current path, or the last chosen path
            while (
                Vector3.Distance(stateHandler.previousPathPoint.transform.position, chosenPath.transform.position) <
                FloatingPointThreshold || // Compare to previous path
                (stateHandler.currentPathPoint != null &&
                 Vector3.Distance(stateHandler.currentPathPoint.transform.position, chosenPath.transform.position) <
                 FloatingPointThreshold) || // Compare to current path
                (_lastChosenPathPoint != null &&
                 Vector3.Distance(_lastChosenPathPoint.transform.position, chosenPath.transform.position) <
                 FloatingPointThreshold) // Compare to last chosen path
            );

            return chosenPath;
        }
    }
}
