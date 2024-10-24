using Roadmans_Fortnite.Data.Enums.NPCEnums;
using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using UnityEngine;

using Gender = Roadmans_Fortnite.Data.Enums.NPCEnums.Gender;
using Sexuality = Roadmans_Fortnite.Data.Enums.NPCEnums.Sexuality;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.AI.Scriptable_Objects
{
    [CreateAssetMenu(menuName = "Prejudice/Map")]
    public class PrejudiceSettings : ScriptableObject
    {
        [Header("Nationality Preferences")]
        public Nationality[] dislikedNationalities;
        public Nationality[] likedNationalities;

        [Space(2)]
        [Header("Gender Preferences")]
        public Gender[] dislikedGenders;
        public Gender[] likedGenders;

        [Space(2)]
        [Header("Sexuality Preferences")]
        public Sexuality[] dislikedSexualities;
        public Sexuality[] likedSexualities;

        [Space(2)]
        [Header("Race Preferences")]
        public RaceCategory[] dislikedRaces;
        public RaceCategory[] likedRaces;

        [Space(2)]
        [Header("Body Type Preferences")]
        public BodyType[] dislikedBodyType;
        public BodyType[] likedBodyType;
    }
}
