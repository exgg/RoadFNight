using Mirror;

using Roadmans_Fortnite.Scripts.Classes.Player.Managers;

namespace Roadmans_Fortnite.EditorClasses
{
    [System.Serializable]
    public class ClassReference
    {
        public NetworkBehaviour aClass;
   
        public enum Category
        {
            Building,
            Movement,
            Weapon,
            Chat,
            Inventory,
            Player,
        }
   
        public enum Keys
        {
            PlayerBase,
            PlayerController,
      
        }

        public string className;
        public Keys key;
        public Category category;
      
   
   
        // lookup example :
        /*   var shootingClass = ClassReference.GetClass<Shooting>(ClassReference.Category.Shooting,
         ClassReference.Key.ShootingClass1);*/
        /*   if (shootingClass != null)
        /*   {
        /*       shootingClass.SomeMethod();
        /*   }
        */
    }
}