using System.Collections.Generic;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing
{
    public class TrafficLightSystem : MonoBehaviour
    {
        public GameObject xLightPair;  // Closest traffic light on the x-axis
        public GameObject yLightPair;  // Closest traffic light on the y-axis

        [Header("Search Settings")]
        [Tooltip("Distance threshold to consider for pairing along X or Y axis.")]
        public float pairDistanceThreshold = 50f;  // Maximum distance for pairing

        // List to keep track of all traffic lights in the scene
        private static List<TrafficLightSystem> allTrafficLights;

        private void Start()
        {
            // Initialize the list of all traffic lights if not done already
            if (allTrafficLights == null)
                allTrafficLights = new List<TrafficLightSystem>(FindObjectsOfType<TrafficLightSystem>());

            // Find and set pairs for this traffic light
            FindPairs();
        }

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
                if (Mathf.Abs(directionToLight.z) < 0.5f && Mathf.Abs(directionToLight.x) < closestXDistance && Mathf.Abs(directionToLight.x) <= pairDistanceThreshold)
                {
                    closestXDistance = Mathf.Abs(directionToLight.x);  // Update closest distance on the X-axis
                    xLightPair = trafficLight.gameObject;  // Set the xLightPair to this traffic light
                }

                // Check alignment along Y-axis: Same x-position and within distance threshold on the y-axis
                if (Mathf.Abs(directionToLight.x) < 0.5f && Mathf.Abs(directionToLight.z) < closestYDistance && Mathf.Abs(directionToLight.z) <= pairDistanceThreshold)
                {
                    closestYDistance = Mathf.Abs(directionToLight.z);  // Update closest distance on the Y-axis
                    yLightPair = trafficLight.gameObject;  // Set the yLightPair to this traffic light
                }
            }

            // Debug log to show which pairs were found
            Debug.Log($"Traffic Light: {gameObject.name} | X Pair: {(xLightPair != null ? xLightPair.name : "None")} | Y Pair: {(yLightPair != null ? yLightPair.name : "None")}");
        }
    }
}
