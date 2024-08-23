using System.Collections.Generic;
using Mirror;
using Roadmans_Fortnite.EditorClasses;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using Roadmans_Fortnite.Scripts.Classes.Player.Input;
using Roadmans_Fortnite.Scripts.Classes.ScriptableObjects.Characters.Player_Characters;
using Roadmans_Fortnite.Scripts.Classes.Stats;
using Roadmans_Fortnite.Scripts.Classes.Player.Controllers;
using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using StarterAssets;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Roadmans_Fortnite.Scripts.Classes.Player.Managers
{
    public class PlayerManager : NetworkBehaviour
    {
        // PSEUDO
        // Consolidate all Update, Fixed Update, Late Update, OnDestroy and OnEnable within here

        [Header("Classes In Order")] 
        public List<ClassReference> classRef = new List<ClassReference>();

        public static Dictionary<ClassReference.Category, Dictionary<ClassReference.Keys, NetworkBehaviour>> CategorisedClasses;
    
        private ClassReference _classReference;

        public BasePlayerStats chosenCharacter;
        public PlayableCharacterStats PlayerStats;
        public ThirdPersonController thirdPersonController;

        private InputHandler _inputHandler;

        private BulletType _bulletUSed;
        
        private void Awake()
        {
            if (isLocalPlayer) // make sure this only occurs within the local player
            {
                PlayerStats = new PlayableCharacterStats(chosenCharacter);

                _inputHandler = GetComponent<InputHandler>();
                thirdPersonController = GetComponent<ThirdPersonController>();
            
                print($"Player Health: {PlayerStats.Health} Player Agility: {PlayerStats.Agility} Player Charisma: {PlayerStats.Charisma}");
            }
        }

        private void Start()
        {
            if (isLocalPlayer)
            {
                InitializeClasses();
            
                thirdPersonController.TPStart();
            
                var tester = GetClass<global::NetPlayer>(ClassReference.Category.Player, ClassReference.Keys.PlayerBase);

                if (tester == null) return;
            
                Debug.Log("Funds are" + tester.funds + " Player username " + tester.username);
            }
            
        }
        
        private void Update()
        {
            if (isLocalPlayer)
            {
                var delta = Time.deltaTime;
            
                _inputHandler.TickInput(delta);
                thirdPersonController.TickUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (isLocalPlayer)
            {
                
            }
        }

        private void LateUpdate()
        {

            thirdPersonController.TickLateUpdate();

           
        }

        private void OnDestroy()
        {
          
        }

        private void OnEnable()
        {
            
        }
        
        
        private void InitializeClasses()
        {
            CategorisedClasses = new Dictionary<ClassReference.Category, Dictionary<ClassReference.Keys, NetworkBehaviour>>();
            foreach (var classReference in classRef)
            {
                if (!CategorisedClasses.ContainsKey(classReference.category))
                {
                    CategorisedClasses[classReference.category] = new Dictionary<ClassReference.Keys, NetworkBehaviour>();
                }

                if (classReference.aClass != null)
                {
                    CategorisedClasses[classReference.category][classReference.key] = classReference.aClass;
                }
            }
        }

        public T GetClass<T>(ClassReference.Category category, ClassReference.Keys key) where T : NetworkBehaviour
        {
            if (CategorisedClasses.TryGetValue(category, out var classDict))
            {
                if (classDict.TryGetValue(key, out var classAsset))
                {
                    return classAsset as T;
                }
            }
            return null;
        }
    }
}