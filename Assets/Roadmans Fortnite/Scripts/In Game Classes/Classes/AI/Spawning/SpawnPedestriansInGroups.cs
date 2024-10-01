using System.Collections.Generic;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians;
using UnityEngine;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Waypoint_Management;
using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Spawning
{
    public class SpawnPedestriansInGroups : MonoBehaviour
    {
        private List<_block> _blocks = new List<_block>();

        [SerializeField] private GameObject pedestrianGroupPrefab; // reference to pedestrianGroupPrefab
        [SerializeField] private GameObject pedestrianPrefab; // Reference to the pedestrian prefab

        [SerializeField, Range(50,500)] private int pedestrianCap;
        [SerializeField, Range(2,20)] private int pedestrianGroupCap;

        private int _pedestrianCount = 0;
        private int _pedestrianGroupMin = 1;

        [SerializeField] private PedestrianSystem masterBrain;
        
        private void Start()
        {
            // Gather all blocks at the start

            masterBrain = GetComponent<PedestrianSystem>();
            
            foreach (var block in FindObjectsOfType<_block>())
            {
                _blocks.Add(block);
            }
            // Start the pedestrian creation process
            CreatePedestriansInGroups();
        }

        private void CreatePedestriansInGroups()
        {
            while (_pedestrianCount < pedestrianCap)
            {
                // Determine the size of the current group to spawn (random between min and cap)
                int groupSize = Random.Range(_pedestrianGroupMin, pedestrianGroupCap + 1);

                // Ensure that we don't exceed the total pedestrian cap
                if (_pedestrianCount + groupSize > pedestrianCap)
                {
                    groupSize = pedestrianCap - _pedestrianCount; // Adjust the group size to stay within the cap
                }

                // Choose a random block for this group
                _block chosenBlock = _blocks[Random.Range(0, _blocks.Count)];

                // Spawn the group in the chosen block
                SpawnPedestrianGroup(chosenBlock, groupSize);

                // Update the overall pedestrian count
                _pedestrianCount += groupSize;
            }
        }

        private void SpawnPedestrianGroup(_block block, int groupSize)
        {
            // List of path points within the block
            List<WaypointLogger> pathPoints = block.pedestrianPathPoints;

            // Choose a random path point within the block as the group's central spawn location
            WaypointLogger centralPathPoint = pathPoints[Random.Range(0, pathPoints.Count)];
            Vector3 centralPosition = centralPathPoint.transform.position;

            GameObject newGroup = Instantiate(pedestrianGroupPrefab, centralPosition, Quaternion.identity);
            PedestrianGroup pedestrianGroup = newGroup.GetComponent<PedestrianGroup>();
            newGroup.transform.name = "Group (" + masterBrain.groupLst.Count + ")";
            newGroup.transform.parent = masterBrain.transform;
            
            masterBrain.groupLst.Add(pedestrianGroup);
            
          
            // Randomize the number of pedestrians in the group and their positions
            for (int i = 0; i < groupSize; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * 2f; // Random scatter within a radius of 2 units
                randomOffset.y = 0; // Keep pedestrians on the ground level

                Vector3 spawnPosition = centralPosition + randomOffset;

                // Instantiate the pedestrian at the calculated position
                GameObject pedestrianObject = Instantiate(pedestrianPrefab, spawnPosition, Quaternion.identity);
                pedestrianObject.transform.parent = newGroup.transform; // Set the block as the parent for the pedestrian
                pedestrianObject.name = "Pedestrian (" + _pedestrianCount + pedestrianGroup.allMembers.Count + ")";
                
                pedestrianGroup.AddMember(pedestrianObject.GetComponent<Pedestrian>());
                
                if (i == 0)
                {
                    pedestrianObject.GetComponent<Pedestrian>().myGroupControlType = GroupControlType.Leader;
                    pedestrianGroup.leader = pedestrianObject.GetComponent<Pedestrian>();
                }
                else
                {
                    pedestrianObject.GetComponent<Pedestrian>().myGroupControlType = GroupControlType.Follower;
                }

                
                // Initialize pedestrian's stats using the block's population control
                InitializePedestrian(pedestrianObject, block.populationWeightMap);

                // Optional: handle collision detection and reposition if needed
                // This will prevent overlap if pedestrians are spawned in the same location
                AvoidOverlap(pedestrianObject, block.pedestrian_lst);
            }

            Debug.Log($"Spawned a group of {groupSize} pedestrians in {block.name}");
        }

        private void InitializePedestrian(GameObject pedestrianObject, PopulationControl populationControl)
        {
            var pedestrian = pedestrianObject.GetComponent<Pedestrian>();

            // Randomize pedestrian stats based on the block's population weight map
            pedestrian.myWealthClass = RandomizeWealth(populationControl);
            pedestrian.myNationality = RandomizeRace(populationControl);
            pedestrian.mySexuality = RandomizeSexuality(populationControl);

            // Add any other customization code for the pedestrian here
        }

        private void AvoidOverlap(GameObject pedestrianObject, List<Pedestrian> pedestrianList)
        {
            var pedestrianTransform = pedestrianObject.transform;
            foreach (var existingPedestrian in pedestrianList)
            {
                if (Vector3.Distance(existingPedestrian.transform.position, pedestrianTransform.position) < 1f)
                {
                    // If too close to an existing pedestrian, apply a small random offset to avoid overlap
                    pedestrianTransform.position += Random.insideUnitSphere * 1.5f;
                }
            }

            // After adjusting, add pedestrian to the list to track it
            pedestrianList.Add(pedestrianObject.GetComponent<Pedestrian>());
        }

        private WealthClass RandomizeWealth(PopulationControl populationControl)
        {
            // Example randomization logic based on population weight
            float randomValue = Random.value;
            if (randomValue <= populationControl.lowerClass) return WealthClass.LowerClass;
            if (randomValue <= populationControl.middleClass) return WealthClass.MiddleClass;
            if (randomValue <= populationControl.highClass) return WealthClass.UpperClass;
            return WealthClass.GangsterClass;
        }

        private Nationality RandomizeRace(PopulationControl populationControl)
        {
            float randomValue = Random.value;
            if (randomValue <= populationControl.black) return Nationality.Nigerian;
            if (randomValue <= populationControl.white) return Nationality.English;
            if (randomValue <= populationControl.asian) return Nationality.Chinese;
            return Nationality.English;
        }

        private Sexuality RandomizeSexuality(PopulationControl populationControl)
        {
            float randomValue = Random.value;
            if (randomValue <= populationControl.heterosexual) return Sexuality.Heterosexual;
            if (randomValue <= populationControl.homosexual) return Sexuality.Homosexual;
            if (randomValue <= populationControl.bisexual) return Sexuality.Bisexual;
            return Sexuality.Transsexual;
        }
    }
}
