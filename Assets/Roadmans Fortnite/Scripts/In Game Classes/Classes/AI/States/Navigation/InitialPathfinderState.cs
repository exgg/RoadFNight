using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Base;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.States.Navigation
{
    public class InitialPathfinderState : BaseState
    {
        public WalkingState walkingState;
        public FollowingState followingState;
        private readonly float _axisThreshold = 3f;

        public override BaseState Tick(StateHandler stateHandler, Pedestrian aiStats, AIAnimationHandler animationHandler)
        {
            // If not a leader, move to the following state
            
            Debug.Log("Testing if I am running this state");
            if (aiStats.myGroupControlType != GroupControlType.Leader)
            {
                return followingState;
            }
            
            // Move the heavy computation (finding nearest path points) to a separate thread
            CalculatePathPointsAsync(stateHandler);
            // Stay in the current state until path points are processed
            return this;
        }
        
        
        private async void CalculatePathPointsAsync(StateHandler stateHandler)
        {
            // Gather path point data from Unity on the main thread
            
            Debug.Log("Attempting to locate pathpoints");
            
            var loggerList = FindObjectsOfType<WaypointLogger>();  // Must be done on main thread
            Debug.Log($"There are {loggerList.Length} waypoint loggers in the scene");
            
            Vector3 aiPosition = stateHandler.transform.position; // Must be done on main thread
            Debug.Log($"Ai position is {aiPosition}");
            
            Debug.Log("Potato");
            
            // Perform the heavy computation on a background thread
            var nearestPathPoints = await Task.Run(() =>
            {
                return FindNearestPathPoint(aiPosition, loggerList);
            });

            // Now, back on the main thread, assign the result
            if (nearestPathPoints.Count > 0)
            {
                // Switch back to the main thread before assigning values
                await Task.Yield();

                stateHandler.currentPathPoint = nearestPathPoints[0];
                stateHandler.previousPathPoint = stateHandler.currentPathPoint;

                Debug.Log("Assigned path points, switching to walking state.");

                // Transition to walking state on the main thread
                stateHandler.SwitchToNextState(walkingState);
            }
        }
        
        // Method to find the nearest path points (thread-safe)
        private List<GameObject> FindNearestPathPoint(Vector3 aiPosition, WaypointLogger[] pathPoints)
        {
            return pathPoints
                .OrderBy(p => CalculateDistanceOnXZPlane(aiPosition, p.transform.position))  // Sort by distance on the X and Z axes
                .Select(p => p.gameObject) // Return the GameObject from the WaypointLogger
                .ToList();
        }

        // Helper method to check if two points are aligned on the X or Z axis within the threshold
        private bool IsOnSameAxis(Vector3 pointA, Vector3 pointB)
        {
            // Return true if the two points are aligned on either the X axis or the Z axis within the threshold
            return (Mathf.Abs(pointA.x - pointB.x) <= _axisThreshold || Mathf.Abs(pointA.z - pointB.z) <= _axisThreshold);
        }

        // Helper method to calculate the distance between two points on the XZ plane (ignoring the Y axis)
        private float CalculateDistanceOnXZPlane(Vector3 pointA, Vector3 pointB)
        {
            // Calculate distance in 2D (XZ plane), ignoring the Y axis
            return Vector2.Distance(new Vector2(pointA.x, pointA.z), new Vector2(pointB.x, pointB.z));
        }
    }
}
