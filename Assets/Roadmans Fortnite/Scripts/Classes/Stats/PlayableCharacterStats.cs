using UnityEngine;
using Roadmans_Fortnite.Scripts.Classes.ScriptableObjects.Characters;
using Roadmans_Fortnite.Scripts.Classes.ScriptableObjects.Characters.Player_Characters;
using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;

namespace Roadmans_Fortnite.Scripts.Classes.Stats
{
    public class PlayableCharacterStats : CharacterStats
    {
        public string CharacterName { get; private set; }
        
        public GameObject CharacterPrefab { get; private set; }
        
        public int Charisma { get; private set; }
        public int Agility { get; private set; }
        public int Strength { get; private set; }
        public float Accuracy { get; private set; }
        
        
        public int ShootingSkill { get; private set; }
        public int TradingSkill { get; private set; } // these are just examples, I will figure this out tomorrow after the meeting
        
        
        
        public PlayableCharacterStats(BasePlayerStats bPs) : base(bPs)
        {
            CharacterName = bPs.characterName;

            CharacterPrefab = bPs.characterPrefab;

            Charisma = bPs.charisma;
            Agility = bPs.agility;
            Strength = bPs.strength;
            Accuracy = bPs.accuracy;

            ShootingSkill = bPs.shootingSkill;
            TradingSkill = bPs.tradingSkill;
        }
    }
}
