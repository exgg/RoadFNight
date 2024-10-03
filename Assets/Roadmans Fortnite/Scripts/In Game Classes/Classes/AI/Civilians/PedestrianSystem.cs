using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians
{
    /// <summary>
    /// Highest level brain for managing pedestrian groups
    /// </summary>
    public class PedestrianSystem : MonoBehaviour
    {
        public CityGen myCity;
        public List<GameObject> playerLst = new List<GameObject>();
        public float visibleThreshold = 10;
        public List<PedestrianGroup> groupLst = new List<PedestrianGroup>();

        private void Start()
        {
            // Initialize the group list by finding all PedestrianGroup components in child objects
            groupLst = new List<PedestrianGroup>(GetComponentsInChildren<PedestrianGroup>(true));

            // Debug message to check the group count
            Debug.Log($"Found {groupLst.Count} pedestrian groups.");

            // Initialize pedestrians within city blocks
            InitBlockPedestrian();

            // Start background coroutine for distance checks
            StartCoroutine(UpdateDistancesInIntervals());
        }

        private void FixedUpdate()
        {
            // Update core behaviors like state handling every physics frame
            UpdateGroupBehaviors();
        }

        // Coroutine to update distances every 2 seconds, separated from core behavior updates
        private IEnumerator UpdateDistancesInIntervals()
        {
            // Loop to continuously update distances
            while (true)
            {
                UpdateGroupDistances();
                yield return new WaitForSeconds(2);
            }
        }

        // Updates group behaviors like movement, state handling, etc.
        private void UpdateGroupBehaviors()
        {
            foreach (var group in groupLst)
            {
                // Only update behaviors and states (excluding distance checks)
                group.UpdateMemberStates(); // New method for states only
            }
        }

        // Updates distances between players and pedestrians in each group
        private void UpdateGroupDistances()
        {
            foreach (var group in groupLst)
            {
                // Update the distances for visibility or other purposes
                group.UpdateMemberDistances(playerLst, visibleThreshold);
            }
        }

        // Initializes pedestrians within each block of the city
        private void InitBlockPedestrian()
        {
            foreach (var block in myCity.listOfBlocks)
            {
                foreach (var pedestrian in GetComponentsInChildren<Pedestrian>(true))
                {
                    block.try_add_pedestrian(pedestrian);
                }
            }
        }
    }
}
