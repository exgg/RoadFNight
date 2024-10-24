using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.Classes.ScriptableObjects.Characters
{
    public class BaseCharacterStats : ScriptableObject
    {
        [Header("Health Stats")]
        public int health;

        [Header("Movement Stats")]
        public float walkSpeed;
        public float runModifier;

        [Header("Game Interaction Stats")]
        public Race race;
        public Sexuality sexuality;
    }
}
