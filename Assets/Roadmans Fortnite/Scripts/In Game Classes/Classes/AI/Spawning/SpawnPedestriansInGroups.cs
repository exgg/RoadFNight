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

        [SerializeField] private PrejudiceSettings caucasoid;
        [SerializeField] private PrejudiceSettings mongoloid;
        [SerializeField] private PrejudiceSettings negroid;
        
        [SerializeField] private GameObject pedestrianGroupPrefab; // reference to pedestrianGroupPrefab
        [SerializeField] private GameObject pedestrianPrefab; // Reference to the pedestrian prefab

        [SerializeField, Range(50,500)] private int pedestrianCap;
        [SerializeField, Range(1,20)] private int pedestrianGroupCap;

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

            // Debug.Log($"Spawned a group of {groupSize} pedestrians in {block.name}");
        }

        private void InitializePedestrian(GameObject pedestrianObject, PopulationControl populationControl)
        {
            var pedestrian = pedestrianObject.GetComponent<Pedestrian>();

            // Randomize pedestrian stats based on the block's population weight map
            pedestrian.myWealthClass = RandomizeWealth(populationControl);
            pedestrian.myRace = RandomizeRace(populationControl);
            pedestrian.myReligion = RandomizeReligion(populationControl);
            pedestrian.mySexuality = RandomizeSexuality(populationControl);
            pedestrian.myGender = RandomizeGender(populationControl);
            pedestrian.myBehaviourType = RandomizedBehaviourType(populationControl);
            
            switch (pedestrian.myRace)
            {
                case RaceCategory.Caucasoid:
                    pedestrian.prejudice = caucasoid;
                    break;
                case RaceCategory.Mongoloid:
                    pedestrian.prejudice = mongoloid;
                    break;
                case RaceCategory.Negroid:
                    pedestrian.prejudice = negroid;
                    break;
            }
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
            float randomValue = Random.Range(1,100);
         
            return randomValue switch
            {
                var n when n <= populationControl.lowerClass => WealthClass.LowerClass,
                var n when n <= (populationControl.lowerClass + populationControl.middleClass) => WealthClass.MiddleClass,
                var n when n <= (populationControl.lowerClass + populationControl.middleClass + populationControl.highClass) => WealthClass.UpperClass,
                var n when n <= (populationControl.lowerClass + populationControl.middleClass + populationControl.highClass + populationControl.gangsterClass) => WealthClass.GangsterClass,
              
                _ => WealthClass.MiddleClass
            };
        }

        private Gender RandomizeGender(PopulationControl populationControl)
        {
            float randomValue = Random.Range(1, 100);
            
            return randomValue switch
            {
                var n when n <= populationControl.male => Gender.Male,
                var n when n <= (populationControl.male + populationControl.female) => Gender.Female,
                var n when n <= (populationControl.male + populationControl.female + populationControl.transMale) => Gender.TransMale,
                var n when n <= (populationControl.male + populationControl.female + populationControl.transMale + populationControl.transFemale) => Gender.TransFemale,
                
                _ => Gender.Male
            };
        }

        private BehaviourType RandomizedBehaviourType(PopulationControl populationControl)
        {
            float randomValue = Random.Range(1, 100);
            
            return randomValue switch
            {
                var n when n <= populationControl.standardBehaviour => BehaviourType.Standard,
                var n when n <= populationControl.standardBehaviour + populationControl.racistBehaviour => BehaviourType.Racist,
                var n when n <= populationControl.standardBehaviour + populationControl.racistBehaviour + populationControl.drunkBehaviour => BehaviourType.Drunk,
                var n when n <= populationControl.standardBehaviour + populationControl.racistBehaviour + populationControl.drunkBehaviour + populationControl.homelessBehaviour => BehaviourType.Homeless,
                var n when n <= populationControl.standardBehaviour + populationControl.racistBehaviour + populationControl.drunkBehaviour + populationControl.homelessBehaviour + populationControl.druggyBehaviour => BehaviourType.Druggy,
                
                _ => BehaviourType.Standard
            };
        }
        
        private RaceCategory RandomizeRace(PopulationControl populationControl)
        {
            float randomValue = Random.Range(1,100);

            return randomValue switch
            {
                var n when n <= populationControl.negroid => RaceCategory.Negroid,
                var n when n <= (populationControl.negroid + populationControl.caucasoid) => RaceCategory.Caucasoid,
                var n when n <= (populationControl.negroid + populationControl.caucasoid + populationControl.mongoloid) => RaceCategory.Mongoloid,
                _ => RaceCategory.Caucasoid
            };
        }

        private Religion RandomizeReligion(PopulationControl populationControl)
        {
           // Generate a random value between 0 and total weight.
            float randomValue = Random.Range(0, 100);

            // Use a switch expression to select the religion based on randomValue.
            return randomValue switch
            {
                var n when n <= populationControl.pagan => Religion.Pagan,
                var n when n <= populationControl.pagan + populationControl.christian => Religion.Christian,
                var n when n <= populationControl.pagan + populationControl.christian + populationControl.rastafarian => Religion.Rastafarian,
                var n when n <= populationControl.pagan + populationControl.christian + populationControl.rastafarian + populationControl.atheist => Religion.Atheist,
                var n when n <= populationControl.pagan + populationControl.christian + populationControl.rastafarian + populationControl.atheist + populationControl.muslim => Religion.Muslim,
                var n when n <= populationControl.pagan + populationControl.christian + populationControl.rastafarian + populationControl.atheist + populationControl.muslim + populationControl.buddhist => Religion.Buddhist,
                _ => Religion.Jewish
            };
        }
        
        private Sexuality RandomizeSexuality(PopulationControl populationControl)
        {
            float randomValue = Random.Range(1,100);
            
            return randomValue switch
            {
                var n when n <= populationControl.heterosexual => Sexuality.Heterosexual,
                var n when n <= (populationControl.heterosexual + populationControl.homosexual) => Sexuality.Homosexual,
                var n when n <= (populationControl.heterosexual + populationControl.homosexual + populationControl.bisexual) => Sexuality.Bisexual,
                var n when n <= (populationControl.heterosexual + populationControl.homosexual + populationControl.bisexual + populationControl.transsexual) => Sexuality.Transsexual,
                _ => Sexuality.Heterosexual
            };
        }
    }
}
