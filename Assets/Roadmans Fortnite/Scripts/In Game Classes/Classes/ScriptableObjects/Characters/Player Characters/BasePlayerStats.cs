using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Classes.ScriptableObjects.Characters.Player_Characters
{
    [CreateAssetMenu(menuName = "Character/Playable")]
    public class BasePlayerStats : BaseCharacterStats
    {
        public string characterName;
        public GameObject characterPrefab;
        
        [Header("Base Stats")]
        public int charisma; // ability to bargain, sell items etc
        public int agility; // move speed amp
        public int strength; // carry capacity, carry more goods, ammo etc
        public float accuracy; // ability to shoot

        [Header("Trainable Stats")]
        public int shootingSkill;

        public int tradingSkill;

        [Header("Infamy")] 
        [Range(0, 100)] public int infamyLevel;

        // add more skills here;
    }
}
