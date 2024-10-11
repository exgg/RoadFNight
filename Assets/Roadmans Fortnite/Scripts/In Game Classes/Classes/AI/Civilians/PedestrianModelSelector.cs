using System;
using System.Collections.Generic;
using UnityEngine;
using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Random = UnityEngine.Random;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians
{
    public class PedestrianModelSelector : MonoBehaviour
    {
        
        [Header("Pedestrian Model Configuration")]
        [Tooltip("Assign pedestrian models based on Gender, Race and Wealth Class")]
        [SerializeField] private PedestrianModelData pedestrianModelData;
        
        // Nested dictionary to access models dynamically
        private Dictionary<Gender, Dictionary<RaceCategory, Dictionary<WealthClass, GameObject[]>>> _modelDictionary;
        private Pedestrian _pedestrian;
        
        private void Awake()
        {
            InitializeModelDictionary();
        }

        private void Start()
        {
            _pedestrian = GetComponent<Pedestrian>();
            
            PedestrianModel(_pedestrian.myGender, _pedestrian.myRace, _pedestrian.myWealthClass).SetActive(true);
        }

        /// <summary>
        /// Initialize the dictionary with all parameter requirements for feedback for the classes and races
        /// </summary>
        private void InitializeModelDictionary()
        {
            _modelDictionary = new Dictionary<Gender, Dictionary<RaceCategory, Dictionary<WealthClass, GameObject[]>>>
            {
                [Gender.Male] = new Dictionary<RaceCategory, Dictionary<WealthClass, GameObject[]>>
                {
                    [RaceCategory.Caucasoid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassMaleCaucasoid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassMaleCaucasoid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassMaleCaucasoid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassMaleCaucasoid
                    },
                    [RaceCategory.Mongoloid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassMaleMongoloid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassMaleMongoloid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassMaleMongoloid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassMaleMongoloid
                    },
                    [RaceCategory.Negroid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassMaleNegroid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassMaleNegroid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassMaleNegroid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassMaleNegroid
                    }
                },

                [Gender.TransMale] = new Dictionary<RaceCategory, Dictionary<WealthClass, GameObject[]>>
                {
                    [RaceCategory.Caucasoid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassMaleCaucasoid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassMaleCaucasoid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassMaleCaucasoid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassMaleCaucasoid
                    },
                    [RaceCategory.Mongoloid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassMaleMongoloid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassMaleMongoloid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassMaleMongoloid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassMaleMongoloid
                    },
                    [RaceCategory.Negroid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassMaleNegroid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassMaleNegroid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassMaleNegroid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassMaleNegroid
                    }
                },
                
                [Gender.Female] = new Dictionary<RaceCategory, Dictionary<WealthClass, GameObject[]>>
                {
                    [RaceCategory.Caucasoid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassFemaleCaucasoid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassFemaleCaucasoid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassFemaleCaucasoid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassFemaleCaucasoid
                    },
                    [RaceCategory.Mongoloid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassFemaleMongoloid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassFemaleMongoloid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassFemaleMongoloid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassFemaleMongoloid
                    },
                    [RaceCategory.Negroid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassFemaleNegroid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassFemaleNegroid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassFemaleNegroid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassFemaleNegroid
                    }
                },
                
                [Gender.TransFemale] = new Dictionary<RaceCategory, Dictionary<WealthClass, GameObject[]>>
                {
                    [RaceCategory.Caucasoid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassFemaleCaucasoid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassFemaleCaucasoid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassFemaleCaucasoid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassFemaleCaucasoid
                    },
                    [RaceCategory.Mongoloid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassFemaleMongoloid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassFemaleMongoloid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassFemaleMongoloid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassFemaleMongoloid
                    },
                    [RaceCategory.Negroid] = new Dictionary<WealthClass, GameObject[]>
                    {
                        [WealthClass.LowerClass] = pedestrianModelData.lowerClassFemaleNegroid,
                        [WealthClass.MiddleClass] = pedestrianModelData.middleClassFemaleNegroid,
                        [WealthClass.UpperClass] = pedestrianModelData.upperClassFemaleNegroid,
                        [WealthClass.GangsterClass] = pedestrianModelData.gangsterClassFemaleNegroid
                    }
                },
            };
        }
        
        /// <summary>
        /// Selects a pedestrian model based on the given gender, race and wealth class
        /// </summary>
        /// <returns></returns>
        private GameObject PedestrianModel(Gender gender, RaceCategory race, WealthClass wealthClass)
        {
            if (_modelDictionary.TryGetValue(gender, out var raceDictionary) &&
                raceDictionary.TryGetValue(race, out var wealthDictionary) &&
                wealthDictionary.TryGetValue(wealthClass, out var models) &&
                models.Length > 0)
            {
                return models[Random.Range(0, models.Length -1)];
            }
            
            Debug.LogError($"No model found for {gender}, {race}, {wealthClass}");

            return null;
        }
    }
}
