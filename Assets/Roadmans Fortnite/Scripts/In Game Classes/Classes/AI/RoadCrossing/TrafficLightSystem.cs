using System;
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
        public TrafficLightSystem xLightPair;
        public TrafficLightSystem yLightPair;

        public bool canCrossX;
        public bool canCrossY;

        [Header("State Management")]
        public TrafficLightState xLightState = TrafficLightState.Red;
        public TrafficLightState yLightState = TrafficLightState.Red;

        [Header("Search Settings")]
        [Tooltip("Distance threshold to consider for pairing along X or Y axis.")]
        public float pairDistanceThreshold = 50f;

        private static List<TrafficLightSystem> allTrafficLights;

        // Add events for state changes
        public event Action<TrafficDirection> OnLightChanged;

        private void Start()
        {
            if (allTrafficLights == null)
                allTrafficLights = new List<TrafficLightSystem>(FindObjectsOfType<TrafficLightSystem>());

            FindPairs();
        }

        private void FindPairs()
        {
            float closestXDistance = float.MaxValue;
            float closestYDistance = float.MaxValue;

            foreach (var trafficLight in allTrafficLights)
            {
                if (trafficLight == this) continue;

                Vector3 directionToLight = trafficLight.transform.position - transform.position;

                if (Mathf.Abs(directionToLight.z) <= 1.0f && Mathf.Abs(directionToLight.x) < closestXDistance && Mathf.Abs(directionToLight.x) <= pairDistanceThreshold)
                {
                    closestXDistance = Mathf.Abs(directionToLight.x);
                    xLightPair = trafficLight;
                }

                if (Mathf.Abs(directionToLight.x) <= 1.0f && Mathf.Abs(directionToLight.z) < closestYDistance && Mathf.Abs(directionToLight.z) <= pairDistanceThreshold)
                {
                    closestYDistance = Mathf.Abs(directionToLight.z);
                    yLightPair = trafficLight;
                }
            }
        }

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

            // Trigger the event when the light changes
            OnLightChanged?.Invoke(axis);
        }
    }
}
