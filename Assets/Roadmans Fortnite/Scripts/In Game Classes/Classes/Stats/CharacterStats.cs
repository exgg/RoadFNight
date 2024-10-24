using Mirror;
using Roadmans_Fortnite.Scripts.Classes.ScriptableObjects.Characters;
using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;

namespace Roadmans_Fortnite.Scripts.Classes.Stats
{
    public abstract class CharacterStats
    {

        [SyncVar] public int Health;
        
        public float WalkSpeed { get; private set; }
        public float RunModifier { get; private set; }

        public Race MyRace { get; private set; }
        public Sexuality MySexuality { get; private set; }
       
        public CharacterStats(BaseCharacterStats cS)
        {
            Health = cS.health;

            WalkSpeed = cS.walkSpeed;
            RunModifier = cS.runModifier;

            MyRace = cS.race;
            MySexuality = cS.sexuality;
        }
    }
}
