using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects
{
    [System.Serializable]
    [CreateAssetMenu(menuName = "Pedestrian/Block/Weight-map")]
    public class PopulationControl : ScriptableObject
    {
        [Header("Wealth Class Percentages")]
        [Range(0f, 100)] public float lowerClass;
        [Range(0f, 100)] public float middleClass;
        [Range(0f, 100)] public float highClass;
        [Range(0f, 100)] public float gangsterClass;
        
        [Header("Race Category Percentages")]
        [Space(1)]
        [Range(0f, 100)] public float negroid;
        [Range(0f, 100)] public float caucasoid;
        [Range(0f, 100)] public float mongoloid;
        
        [Header("Religion Percentages")]
        [Range(0f, 100)] public float pagan;
        [Range(0f, 100)] public float christian;
        [Range(0f, 100)] public float rastafarian;
        [Range(0f, 100)] public float atheist;
        [Range(0f, 100)] public float muslim;
        [Range(0f, 100)] public float buddhist;
        [Range(0f, 100)] public float jewish;
        
        [Header("Sexuality Percentages")]
        [Space(1)]
        [Range(0f, 100)] public float heterosexual;
        [Range(0f, 100)] public float homosexual;
        [Range(0f, 100)] public float bisexual;
        [Range(0f, 100)] public float transsexual;

        [Header("Gender Percentages")]
        [Space(1)]
        [Range(0f, 100)] public float male;
        [Range(0f, 100)] public float female;
        [Range(0f, 100)] public float transMale;
        [Range(0f, 100)] public float transFemale;

        [Header("Behaviour Type Percentages")]
        [Space(1)]
        [Range(0f, 100)] public float standardBehaviour;
        [Range(0f, 100)] public float racistBehaviour;
        [Range(0f, 100)] public float drunkBehaviour;
        [Range(0f, 100)] public float homelessBehaviour;
        [Range(0f, 100)] public float druggyBehaviour;

        [Header("Body Type Percentages")]
        [Space(1)]
        [Range(0f, 100)] public float fat;
        [Range(0f, 100)] public float slim;
        [Range(0f, 100)] public float muscular;
    }
}
