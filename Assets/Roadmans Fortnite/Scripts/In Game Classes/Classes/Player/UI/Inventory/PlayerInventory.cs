using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

namespace Roadmans_Fortnite.Scripts.Classes.Player.UI.Inventory
{
    public class PlayerInventory : Combatant
    {

        public static int maxSlots = 30;

        public static bool inMenu = false;

        public int ammoAmount;

        public static int maxWeapons = 10;

        private GameObject[] weapons = new GameObject[maxWeapons];

        Dictionary<GameObject, int> inventory = new Dictionary<GameObject, int>();

        // Start is called before the first frame update
        void Start()
        {
            currentWeapon = weapons[0];
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void AddToInventory() //parameters: item
        {
            //inventory.Add(item, id);
        }

        public void RemoveFromInventory() //parameters: item
        {
            //inventory.Remove(id);
        }

        public int LoadWeapon(int ammoInWeapon)
        {
            if (ammoInWeapon > ammoAmount)
            {
                ammoInWeapon = ammoAmount;
                ammoAmount = 0;
                return ammoInWeapon;
            }
            else
            {
                ammoAmount -= ammoInWeapon;
                return ammoInWeapon;
            }

        }

        public void ChangeWeapon(int weaponNumber)
        {
            currentWeapon = weapons[weaponNumber];
        }

    }
}