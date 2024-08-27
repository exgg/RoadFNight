using Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Managers;
using UnityEngine;

namespace Roadmans_Fortnite.Scripts.In_Game_Classes.Classes.Player.Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        public GameObject[] weapons;

        public int currentWeaponIndex;
        
        public TpManagerNew tpManagerNew;

        public int bulletCount;
        
        public void Initialize()
        {
            tpManagerNew = GetComponent<TpManagerNew>();
            foreach (var weapon in weapons)
            {
                weapon.SetActive(false);
                weapon.GetComponent<WeaponManager>().enabled = false;
            }
        }

        public void ActivateWeapon(int index)
        {
            // deactivate all weapons

            foreach (var weapon in weapons)
            {
                weapon.SetActive(false);
                weapon.GetComponent<WeaponManager>().enabled = false;
            }

            if (index >= 0 && index < weapons.Length)
            {
                currentWeaponIndex = index;
                weapons[index].SetActive(true);
                weapons[index].GetComponent<WeaponManager>().enabled = true;
                tpManagerNew.currentWeaponManager = weapons[index].GetComponent<WeaponManager>();
            }
        }
        
        public void LoadWeapons()
        {
            
        }

        public void LoadCash()
        {
            
        }
    }
}
