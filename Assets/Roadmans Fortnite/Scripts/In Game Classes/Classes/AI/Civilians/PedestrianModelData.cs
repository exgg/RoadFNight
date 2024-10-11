using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Civilians
{
    [System.Serializable]
    public class PedestrianModelData
    {
        [Header("Caucasoid Male")]
        public GameObject[] upperClassMaleCaucasoid;
        public GameObject[] middleClassMaleCaucasoid;
        public GameObject[] lowerClassMaleCaucasoid;
        public GameObject[] gangsterClassMaleCaucasoid;
        
        [Space(2)]
        [Header("Caucasoid Female")]

        public GameObject[] upperClassFemaleCaucasoid;
        public GameObject[] middleClassFemaleCaucasoid;
        public GameObject[] lowerClassFemaleCaucasoid;
        public GameObject[] gangsterClassFemaleCaucasoid;

        [Space(2)]
        [Header("Mongoloid Male")]
        
        public GameObject[] upperClassMaleMongoloid;
        public GameObject[] middleClassMaleMongoloid;
        public GameObject[] lowerClassMaleMongoloid;
        public GameObject[] gangsterClassMaleMongoloid;
        
        [Space(2)]
        [Header("Mongoloid Female")]
        
        public GameObject[] upperClassFemaleMongoloid;
        public GameObject[] middleClassFemaleMongoloid;
        public GameObject[] lowerClassFemaleMongoloid;
        public GameObject[] gangsterClassFemaleMongoloid;
        
        [Space(2)]
        [Header("Negroid Male")]
        
        public GameObject[] upperClassMaleNegroid;
        public GameObject[] middleClassMaleNegroid;
        public GameObject[] lowerClassMaleNegroid;
        public GameObject[] gangsterClassMaleNegroid;
        
        [Space(2)]
        [Header("Negroid Female")]
        
        public GameObject[] upperClassFemaleNegroid;
        public GameObject[] middleClassFemaleNegroid;
        public GameObject[] lowerClassFemaleNegroid;
        public GameObject[] gangsterClassFemaleNegroid;
    }
}
