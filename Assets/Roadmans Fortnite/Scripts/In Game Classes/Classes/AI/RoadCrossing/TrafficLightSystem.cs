using System.Collections.Generic;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing
{
    public enum TrafficLightState
    {
        Red,
        Green
    }

    public enum TrafficDirection
    {
        X,
        Y
    }

    public class TrafficLightSystem : MonoBehaviour
    {
        public TrafficLightSystem xLightPair;  // Closest traffic light on the x-axis
        public TrafficLightSystem yLightPair;  // Closest traffic light on the y-axis

        public bool canCrossX;
        public bool canCrossY;

        [Header("State Management")]
        public TrafficLightState xLightState = TrafficLightState.Red;  // State for the X-axis light
        public TrafficLightState yLightState = TrafficLightState.Red;  // State for the Y-axis light

        [Header("Search Settings")]
        [Tooltip("Distance threshold to consider for pairing along X or Y axis.")]
        public float pairDistanceThreshold = 50f;  // Maximum distance for pairing

        private static List<TrafficLightSystem> allTrafficLights;

        private void Start()
        {
            // Initialize the list of all traffic lights if not done already
            if (allTrafficLights == null)
                allTrafficLights = new List<TrafficLightSystem>(FindObjectsOfType<TrafficLightSystem>());

            // Find and set pairs for this traffic light
            FindPairs();
        }

        /// <summary>
        /// Finds the closest traffic lights on the X and Y axis within the distance threshold.
        /// </summary>
        private void FindPairs()
        {
            float closestXDistance = float.MaxValue;  // Track the closest light on the X-axis
            float closestYDistance = float.MaxValue;  // Track the closest light on the Y-axis

            foreach (var trafficLight in allTrafficLights)
            {
                // Skip self
                if (trafficLight == this) continue;

                // Calculate the direction to the other light
                Vector3 directionToLight = trafficLight.transform.position - transform.position;

                // Check alignment along X-axis: Same z-position and within distance threshold on the x-axis
                if (Mathf.Abs(directionToLight.z) <= 1.0f && Mathf.Abs(directionToLight.x) < closestXDistance && Mathf.Abs(directionToLight.x) <= pairDistanceThreshold)
                {
                    closestXDistance = Mathf.Abs(directionToLight.x);  // Update closest distance on the X-axis
                    xLightPair = trafficLight;  // Set the xLightPair to this traffic light
                }

                // Check alignment along Y-axis: Same x-position and within distance threshold on the y-axis
                if (Mathf.Abs(directionToLight.x) <= 1.0f && Mathf.Abs(directionToLight.z) < closestYDistance && Mathf.Abs(directionToLight.z) <= pairDistanceThreshold)
                {
                    closestYDistance = Mathf.Abs(directionToLight.z);  // Update closest distance on the Y-axis
                    yLightPair = trafficLight;  // Set the yLightPair to this traffic light
                }
            }

            // Debug log to show which pairs were found
            //Debug.Log($"Traffic Light: {gameObject.name} | X Pair: {(xLightPair != null ? xLightPair.name : "None")} | Y Pair: {(yLightPair != null ? yLightPair.name : "None")}");
        }

        /// <summary>
        /// Sets the current state of this traffic light.
        /// Ensures that crossing is only allowed for either X or Y axis, but not both.
        /// </summary>
        /// <param name="state">The new state to set for this traffic light.</param>
        /// <param name="axis">Indicates which axis is currently being controlled (either 'X' or 'Y').</param>
        public void SetTrafficLightState(TrafficLightState state, TrafficDirection axis)
        {
            if (axis == TrafficDirection.X)
            {
                xLightState = state;
                canCrossX = (state == TrafficLightState.Green);
            }
            else if (axis == TrafficDirection.Y)
            {
                yLightState = state;
                canCrossY = (state == TrafficLightState.Green);
            }

            //Debug.Log($"Traffic Light {name} set to {state} state on axis {axis}. Crossing enabled: X - {canCrossX}, Y - {canCrossY}");
        }
    }
}
