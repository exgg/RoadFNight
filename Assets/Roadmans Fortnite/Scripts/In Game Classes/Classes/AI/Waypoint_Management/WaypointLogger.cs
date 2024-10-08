using System;
using System.Collections;
using System.Collections.Generic;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management
{
    public class WaypointLogger : MonoBehaviour
    {
        public List<GameObject> waypoints = new List<GameObject>();

        [Header("Road Cross Point Configuration")]
        public bool IsRoadCrossPoint;
        
        private float searchRadius = 20f;
        private float axisThreshold = 5f;  // Maximum alignment threshold for X or Z axis alignment
        private float existingDirectionTolerance = 10f;  // Tolerance angle to ignore existing directions

        [Tooltip("Search radius for finding nearby traffic lights.")]
        public float trafficLightSearchRadius = 50f;  // Radius within which to search for traffic lights

        public TrafficLightSystem NearestTrafficLight;
        private void OnDrawGizmos()
        {
            // Draw the waypoints with different colors based on number of connections
            Gizmos.color = waypoints.Count switch
            {
                0 => Color.red,
                1 => Color.yellow,
                2 => Color.green,
                3 => Color.magenta,
                4 => new Color(0f, 0.5f, 0f),
                _ => Color.black,
            };
            
            Gizmos.DrawSphere(transform.position, 1f);
            
            foreach (var waypoint in waypoints)
            {
                if (waypoint != null)
                {
                    // Draw connection lines in red or green based on reciprocal linking
                    Gizmos.color = waypoint.GetComponent<WaypointLogger>().waypoints.Contains(gameObject) ? Color.green : Color.red;
                    Gizmos.DrawLine(transform.position, waypoint.transform.position);
                }
            }
        }

        private void Start()
        {
            StartCoroutine(InitalizeAfterDelay());
        }

        IEnumerator InitalizeAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            InitializeWaypoints();
        }

        /// <summary>
        /// Finds and initializes waypoints within a defined search radius using X and Z plane checks.
        /// </summary>
        private void InitializeWaypoints()
        {
            if (!IsRoadCrossPoint) return;

            // Get all WaypointLogger components in the scene
            WaypointLogger[] allWaypoints = FindObjectsOfType<WaypointLogger>();
            foreach (var nearbyWaypoint in allWaypoints)
            {
                // Ensure the waypoint is not itself and is within the search radius
                if (nearbyWaypoint != this && nearbyWaypoint.IsRoadCrossPoint)
                {
                    float distance = CalculateDistanceOnXZPlane(transform.position, nearbyWaypoint.transform.position);
                    
                    // If within the search radius, check for alignment on X or Z axis
                    if (distance <= searchRadius && IsOnSameAxis(transform.position, nearbyWaypoint.transform.position))
                    {
                        // Check if this new direction is already covered by an existing direction
                        if (IsDirectionCovered(nearbyWaypoint.transform.position)) continue;

                        // Check if this crossing is already linked to the other crossing
                        if (!waypoints.Contains(nearbyWaypoint.gameObject))
                        {
                            // Link the waypoints
                            LinkToWaypoint(nearbyWaypoint);
                        }
                    }
                }
            }
            
            FindNearestTrafficLight();
        }

        /// <summary>
        /// Finds the nearest traffic light system within a specified search radius.
        /// </summary>
        private void FindNearestTrafficLight()
        {
            // Find all TrafficLightSystem components in the scene
            TrafficLightSystem[] allTrafficLights = FindObjectsOfType<TrafficLightSystem>();

            // Variable to store the nearest traffic light and its distance
            TrafficLightSystem nearestTrafficLight = null;
            float closestDistance = float.MaxValue;

            // Iterate through all traffic lights to find the closest one
            foreach (var trafficLight in allTrafficLights)
            {
                // Calculate distance to the traffic light on the XZ plane
                float distance = CalculateDistanceOnXZPlane(transform.position, trafficLight.transform.position);

                // If within the search radius and closer than the previous closest light, set it as the nearest
                if (distance <= trafficLightSearchRadius && distance < closestDistance)
                {
                    closestDistance = distance;
                    nearestTrafficLight = trafficLight;
                }
            }

            // Debugging: log the name of the nearest traffic light, if found
            if (nearestTrafficLight != null)
            {
                //Debug.Log($"Nearest Traffic Light to {gameObject.name} is {nearestTrafficLight.name} at a distance of {closestDistance}");
                NearestTrafficLight = nearestTrafficLight;
            }
            else
            {
                //Debug.Log($"No traffic light found within {trafficLightSearchRadius} units of {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Helper method to check if two waypoints are aligned on either the X or Z axis with a threshold.
        /// </summary>
        private bool IsOnSameAxis(Vector3 pointA, Vector3 pointB)
        {
            // Return true if the two points are aligned on either the X axis or the Z axis within the threshold
            return (Mathf.Abs(pointA.x - pointB.x) <= axisThreshold || Mathf.Abs(pointA.z - pointB.z) <= axisThreshold);
        }

        /// <summary>
        /// Helper method to calculate the distance between two points on the XZ plane.
        /// </summary>
        private float CalculateDistanceOnXZPlane(Vector3 pointA, Vector3 pointB)
        {
            // Only calculate distance in 2D, ignoring the Y axis
            return Vector2.Distance(new Vector2(pointA.x, pointA.z), new Vector2(pointB.x, pointB.z));
        }

        /// <summary>
        /// Checks if a given direction is already covered by an existing waypoint in the list.
        /// </summary>
        private bool IsDirectionCovered(Vector3 targetPosition)
        {
            foreach (var existingWaypoint in waypoints)
            {
                if (CalculateDistanceOnXZPlane(targetPosition, existingWaypoint.transform.position) < axisThreshold)
                {
                    //Debug.Log($"Position {targetPosition} is too close to {existingWaypoint.transform.position} and considered covered.");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Links the current waypoint to another waypoint.
        /// </summary>
        private void LinkToWaypoint(WaypointLogger targetWaypoint)
        {
            waypoints.Add(targetWaypoint.gameObject);

            // Ensure the other waypoint also references this one
            if (!targetWaypoint.waypoints.Contains(gameObject))
            {
                targetWaypoint.waypoints.Add(gameObject);
            }

            //Debug.Log($"Linked {gameObject.name} to {targetWaypoint.gameObject.name}");
        }
    }
}
