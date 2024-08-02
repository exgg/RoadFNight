using System.Collections.Generic;
using Mirror;
using Roadmans_Fortnite.EditorClasses;
using Unity.VisualScripting;
using UnityEngine;

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
        
        private void Awake()
        {
            
        }

        private void Start()
        {
            InitializeClasses();

            var tester = GetClass<global::Player>(ClassReference.Category.Player, ClassReference.Keys.PlayerBase);

            if (tester == null) return;
            
            Debug.Log("Funds are" + tester.funds + " Player username " + tester.username);
        }
        
        private void Update()
        {
            
        }

        private void FixedUpdate()
        {
            
        }

        private void LateUpdate()
        {
            
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