using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.RoadCrossing
{
    public class TrafficLightController : MonoBehaviour
    {
        public List<TrafficLightSystem> TrafficLightSystems = new List<TrafficLightSystem>();

        [Tooltip("Time in seconds each light stays green before toggling.")]
        private readonly float _greenLightDuration = 10;

        private void Start()
        {
            // Find all traffic lights in the scene
            FindAllTrafficLights();

            // Start the traffic light toggling process
            StartCoroutine(ToggleTrafficLights());
        }

        /// <summary>
        /// Finds all traffic light systems in the scene and adds them to the TrafficLightSystems list.
        /// </summary>
        private void FindAllTrafficLights()
        {
            TrafficLightSystems.AddRange(FindObjectsOfType<TrafficLightSystem>());
        }

        /// <summary>
        /// Coroutine to toggle the traffic lights between X and Y axes.
        /// </summary>
        private IEnumerator ToggleTrafficLights()
        {
            while (true)
            {
                // Activate X lights and deactivate Y lights
                SetTrafficLightStateForAll(TrafficDirection.X, TrafficLightState.Green);
                SetTrafficLightStateForAll(TrafficDirection.Y, TrafficLightState.Red);

                yield return new WaitForSeconds(_greenLightDuration);

                // Activate Y lights and deactivate X lights
                SetTrafficLightStateForAll(TrafficDirection.X, TrafficLightState.Red);
                SetTrafficLightStateForAll(TrafficDirection.Y, TrafficLightState.Green);

                yield return new WaitForSeconds(_greenLightDuration);
            }
        }

        /// <summary>
        /// Sets the state of all lights on a specific axis.
        /// </summary>
        /// <param name="axis">The axis to set the light state (X or Y).</param>
        /// <param name="state">The state to set (Red or Green).</param>
        private void SetTrafficLightStateForAll(TrafficDirection axis, TrafficLightState state)
        {
            foreach (var trafficLightSystem in TrafficLightSystems)
            {
                // Set the state for both the individual light and its pair (if exists)
                trafficLightSystem.SetTrafficLightState(state, axis);

                // Ensure that the pairs are also updated correctly
                if (axis == TrafficDirection.X && trafficLightSystem.xLightPair != null)
                {
                    trafficLightSystem.xLightPair.SetTrafficLightState(state, TrafficDirection.X);
                }
                else if (axis == TrafficDirection.Y && trafficLightSystem.yLightPair != null)
                {
                    trafficLightSystem.yLightPair.SetTrafficLightState(state, TrafficDirection.Y);
                }
            }

             // Debug.Log($"All {axis} Lights set to {state}");
        }
    }
}
