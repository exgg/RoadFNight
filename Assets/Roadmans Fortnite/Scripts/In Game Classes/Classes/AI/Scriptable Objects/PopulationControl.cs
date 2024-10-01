using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Pedestrian/Block/Weightmap")]
    public class PopulationControl : ScriptableObject
    {
        [Header("Wealth Class Percentages")]
        
        [Range(0f, 100)]public float lowerClass;
        [Range(0f, 100)]public float middleClass;
        [Range(0f, 100)]public float highClass;
        [Range(0f, 100)]public float gangsterClass;

        [Header("Race Percentages")]
        [Space (1)]
        
        [Range(0f, 100)]public float black;
        [Range(0f, 100)]public float white;
        [Range(0f, 100)]public float asian;
        [Range(0f, 100)]public float mixedRace;
        
        [Header("Sexuality Percentages")]
        [Space (1)]

        [Range(0f, 100)]public float heterosexual;
        [Range(0f, 100)]public float homosexual;
        [Range(0f, 100)]public float bisexual;
        [Range(0f, 100)]public float transsexual;
        
        [Header("Behaviour Type Percentages")]
        [Space (1)]

        [Range(0f, 100)]public float standardBehaviour;
        [Range(0f, 100)]public float racistBehaviour;
        [Range(0f, 100)]public float drunkBehaviour;
        [Range(0f, 100)]public float homelessBehaviour;
        [Range(0f, 100)]public float druggyBehaviour;
        
        
        [Header("Body Type Percentages")]
        [Space (1)]
        
        [Range(0f, 100)]public float fat;
        [Range(0f, 100)]public float slim;
        [Range(0f, 100)]public float muscular;
    }
}
